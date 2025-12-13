using System.Text.Json;
using Microsoft.SemanticKernel;
using WebLaptopBE.AI.SemanticKernel;
using WebLaptopBE.DTOs;

namespace WebLaptopBE.Services;

/// <summary>
/// RAG Chat Service - Thực hiện Retrieval-Augmented Generation
/// Flow:
/// 1. Tạo embedding từ userMessage
/// 2. Search Qdrant (products + policies)
/// 3. Combine context
/// 4. Gọi Semantic Kernel với prompt
/// 5. Return response
/// </summary>
public class RAGChatService : IRAGChatService
{
    private readonly IQdrantVectorService _qdrantVectorService;
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly IProductService _productService;
    private readonly ILogger<RAGChatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AI.Services.IInputValidationService _inputValidationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    // Cache Frontend URL
    private string? _frontendUrl;
    private string FrontendUrl => _frontendUrl ??= _configuration["FrontendUrl"] ?? "http://localhost:5253";

    public RAGChatService(
        IQdrantVectorService qdrantVectorService,
        ISemanticKernelService semanticKernelService,
        IProductService productService,
        ILogger<RAGChatService> logger,
        IConfiguration configuration,
        AI.Services.IInputValidationService inputValidationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _qdrantVectorService = qdrantVectorService;
        _semanticKernelService = semanticKernelService;
        _productService = productService;
        _logger = logger;
        _configuration = configuration;
        _inputValidationService = inputValidationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<RAGChatResponse> ProcessUserMessageAsync(string userMessage, string? customerId = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Processing RAG chat message: {Message}", userMessage);

            // BƯỚC 0: Validate input trước khi xử lý
            var validationResult = _inputValidationService.ValidateUserInput(userMessage);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Input validation failed: {ErrorType} - {Message}", 
                    validationResult.ErrorType, validationResult.Message);
                
                return new RAGChatResponse
                {
                    Answer = validationResult.Message,
                    SuggestedProducts = null,
                    Timestamp = DateTime.UtcNow
                };
            }

            // Bước 1 & 2: Parallelize products và policies search với timeout tổng
            List<VectorSearchResult> productResults = new List<VectorSearchResult>();
            List<VectorSearchResult> policyResults = new List<VectorSearchResult>();

            // Chạy song song products và policies search với timeout tổng 8 giây
            using var searchCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            
            var productSearchTask = SearchProductsWithFallbackAsync(userMessage);
            var policySearchTask = _qdrantVectorService.SearchPoliciesAsync(userMessage, topK: 3);

            try
            {
                // Đợi cả 2 tasks hoàn thành song song với timeout
                var combinedTask = Task.WhenAll(productSearchTask, policySearchTask);
                var completedTask = await Task.WhenAny(combinedTask, Task.Delay(8000, searchCts.Token));
                
                if (completedTask == combinedTask)
                {
                    productResults = await productSearchTask;
                    policyResults = await policySearchTask;
                }
                else
                {
                    _logger.LogWarning("Search timeout after 8 seconds, using available results");
                    // Lấy kết quả từ các task đã hoàn thành
                    if (productSearchTask.IsCompletedSuccessfully)
                    {
                        productResults = await productSearchTask;
                    }
                    if (policySearchTask.IsCompletedSuccessfully)
                    {
                        policyResults = await policySearchTask;
                    }
                }
                
                _logger.LogInformation("Found {ProductCount} product results and {PolicyCount} policy results in {ElapsedMs}ms", 
                    productResults?.Count ?? 0, policyResults?.Count ?? 0, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in parallel search, continuing with available results");
                // Lấy kết quả từ task đã hoàn thành (nếu có)
                try
                {
                    if (productSearchTask.IsCompletedSuccessfully)
                    {
                        productResults = await productSearchTask;
                    }
                }
                catch { }
                
                try
                {
                    if (policySearchTask.IsCompletedSuccessfully)
                    {
                        policyResults = await policySearchTask;
                    }
                }
                catch
                {
                    policyResults = new List<VectorSearchResult>();
                }
            }

            // Nếu không lấy được policy từ Qdrant, fallback sang bộ policy mặc định (không cần vector DB)
            if (policyResults == null || policyResults.Count == 0)
            {
                policyResults = GetFallbackPolicies(userMessage);
                if (policyResults.Count > 0)
                {
                    _logger.LogWarning("Using fallback policies because Qdrant policy search returned no results");
                }
            }

            // Bước 3: Build context từ search results
            var productContext = BuildProductContext(productResults);
            var policyContext = BuildPolicyContext(policyResults);

            // Bước 4: Tạo prompt cho LLM
            var systemPrompt = BuildSystemPrompt();
            var userPrompt = BuildUserPrompt(userMessage, productContext, policyContext);

            // Bước 5: Gọi Semantic Kernel để generate response với timeout
            string response;
            bool llmSucceeded = false;
            
            try
            {
                var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";
                
                // Wrap LLM call với timeout 10 giây
                _logger.LogDebug("Calling LLM for response generation...");
                var llmTask = _semanticKernelService.GenerateResponseAsync(fullPrompt);
                var completedInTime = await Task.WhenAny(llmTask, Task.Delay(10000)) == llmTask;
                
                if (!completedInTime)
                {
                    _logger.LogWarning("LLM generation timeout after 10 seconds");
                    throw new TimeoutException("LLM generation timeout after 10 seconds");
                }
                
                response = await llmTask;
                llmSucceeded = !string.IsNullOrEmpty(response);
                _logger.LogInformation("Generated response from LLM in {ElapsedMs}ms, length: {Length}", 
                    stopwatch.ElapsedMilliseconds, response?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Semantic Kernel/OpenAI: {ErrorType} - {ErrorMessage}", 
                    ex.GetType().Name, ex.Message);
                
                // GRACEFUL DEGRADATION: Tạo response từ dữ liệu có sẵn thay vì fail hoàn toàn
                response = BuildFallbackResponse(userMessage, productResults, policyResults);
            }

            // Bước 6: Parse suggested products từ productResults
            List<ProductDTO>? productDTOs = null;
            try
            {
                productDTOs = await ParseSuggestedProductsAsync(productResults);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing suggested products");
            }

            // Convert ProductDTO to ProductSuggestion (không thể convert trực tiếp)
            // RAGChatResponse cũ dùng ProductDTO, giữ nguyên cho backward compatibility
            // Validate and sanitize response
            var sanitizedResponse = SanitizeResponse(response);
            
            return new RAGChatResponse
            {
                Answer = sanitizedResponse,
                SuggestedProducts = productDTOs != null ? ConvertToProductSuggestions(productDTOs) : null,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error processing RAG chat message: {ErrorType} - {ErrorMessage}", 
                ex.GetType().Name, ex.Message);
            return new RAGChatResponse
            {
                Answer = "Xin lỗi, hiện tại hệ thống đang gặp sự cố. Anh/chị vui lòng thử lại sau hoặc liên hệ nhân viên để được hỗ trợ.",
                SuggestedProducts = null,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Fallback policies khi Qdrant hoặc OpenAI không khả dụng
    /// Sử dụng PolicyData để lấy chính sách đầy đủ
    /// </summary>
    private List<VectorSearchResult> GetFallbackPolicies(string userMessage)
    {
        // Lấy chính sách từ PolicyData
        var policies = AI.Data.PolicyData.SearchPolicies(userMessage);
        
        // Convert sang VectorSearchResult
        var results = policies.Select(p => new VectorSearchResult
        {
            Content = p.Content, // FULL TEXT
            Score = 0.9f, // High score vì đây là exact match
            Metadata = new Dictionary<string, object>
            {
                ["policyId"] = p.PolicyId,
                ["policy_type"] = p.Category.ToString().ToLower(),
                ["title"] = p.Title
            }
        }).ToList();

        return results;
    }

    /// <summary>
    /// Build system prompt cho LLM - Tối ưu để chatbot phản hồi như nhân viên sale xuất sắc
    /// </summary>
    private string BuildSystemPrompt()
    {
        return @"Bạn là nhân viên tư vấn bán laptop chuyên nghiệp tại cửa hàng TenTech, với nhiều năm kinh nghiệm và am hiểu sâu về công nghệ. Bạn có khả năng giao tiếp tự nhiên, thân thiện, và luôn đặt lợi ích khách hàng lên hàng đầu.

VAI TRÒ VÀ TRÁCH NHIỆM:
- Tư vấn khách hàng chọn laptop phù hợp nhất với nhu cầu và ngân sách
- Giải thích thông tin kỹ thuật một cách dễ hiểu, không dùng thuật ngữ khó
- So sánh sản phẩm một cách khách quan, trung thực
- Hỗ trợ về chính sách bảo hành, bảo mật, thanh toán
- Tạo trải nghiệm mua sắm tích cực, khiến khách hàng cảm thấy được quan tâm

PHONG CÁCH GIAO TIẾP:
- Xưng hô: 'em' với khách hàng, 'anh/chị' với khách (tự nhiên, thân thiện)
- Tone: Chuyên nghiệp nhưng không quá formal, nhiệt tình nhưng không quá thân mật
- SỬ DỤNG ICON/EMOJI CỰC KỲ HẠN CHẾ: Chỉ sử dụng khi thực sự cần thiết (tối đa 1-2 icon mỗi câu trả lời)
- Trả lời bằng tiếng Việt tự nhiên, dễ hiểu, không dùng từ ngữ quá kỹ thuật
- Thể hiện sự quan tâm chân thành đến nhu cầu của khách hàng

📋 QUY TẮC TRẢ LỜI THEO TỪNG TÌNH HUỐNG:

1. KHI TƯ VẤN SẢN PHẨM:
   ✅ Luôn hỏi rõ nhu cầu sử dụng trước khi đề xuất (gaming, văn phòng, đồ họa, học tập, lập trình...)
   ✅ Đề xuất 2-3 sản phẩm phù hợp với giải thích rõ ràng lý do tại sao phù hợp
   ✅ So sánh điểm mạnh/yếu của từng sản phẩm một cách khách quan
   ✅ Đề cập đến giá cả và giá trị nhận được (ví dụ: 'Sản phẩm này có giá tốt so với cấu hình')
   ✅ Gợi ý sản phẩm tốt nhất dựa trên nhu cầu, không chỉ dựa trên giá
   ✅ Kết thúc bằng câu hỏi mở để tiếp tục tư vấn (ví dụ: 'Anh/chị có muốn xem thêm sản phẩm nào khác không?')

2. KHI KHÁCH HỎI MƠ HỒ HOẶC THIẾU THÔNG TIN:
   ✅ Đặt câu hỏi làm rõ một cách tự nhiên:
      - 'Anh/chị muốn laptop để làm gì chủ yếu ạ? (gaming, văn phòng, đồ họa...)'
      - 'Ngân sách của anh/chị khoảng bao nhiêu ạ?'
      - 'Anh/chị có thương hiệu nào yêu thích không?'
   ✅ Đưa ra gợi ý cụ thể: 'Nếu anh/chị cần laptop văn phòng, em có thể đề xuất...'
   ✅ Không để khách hàng cảm thấy bị tra hỏi, mà như đang được tư vấn

3. KHI KHÔNG CÓ THÔNG TIN HOẶC KHÔNG CHẮC CHẮN:
   ✅ Thành thật: 'Em xin lỗi, hiện tại em chưa có thông tin chi tiết về...'
   ✅ Đề xuất giải pháp: 'Anh/chị có thể liên hệ hotline hoặc đến cửa hàng để được tư vấn trực tiếp'
   ✅ Không bịa thông tin, không hứa hẹn những gì không chắc chắn

4. KHI TRẢ LỜI VỀ CHÍNH SÁCH (QUAN TRỌNG - ĐỌC KỸ):
   ✅ HIỂN THỊ FULL TEXT CHÍNH SÁCH từ context được cung cấp - KHÔNG tóm tắt, KHÔNG rút gọn
   ✅ Nếu có nhiều chính sách liên quan, hiển thị TẤT CẢ các chính sách đó
   ✅ Giữ nguyên cấu trúc, định dạng, và nội dung chi tiết của chính sách
   ✅ Giải thích thêm nếu khách hàng yêu cầu, nhưng vẫn phải hiển thị full text trước
   ✅ Đề cập đến thông tin liên hệ (địa chỉ, hotline, email) nếu có trong chính sách

5. KHI SO SÁNH SẢN PHẨM:
   ✅ So sánh khách quan, không thiên vị
   ✅ Nêu rõ điểm mạnh/yếu của từng sản phẩm
   ✅ Đưa ra lời khuyên dựa trên nhu cầu cụ thể của khách hàng
   ✅ Giải thích tại sao sản phẩm này phù hợp hơn sản phẩm kia trong trường hợp cụ thể

📝 ĐỊNH DẠNG TRẢ LỜI:
- Sử dụng bullet points (•) cho danh sách sản phẩm hoặc thông tin quan trọng
- In đậm tên sản phẩm hoặc thông tin quan trọng (dùng **text**)
- Chia đoạn rõ ràng, không viết dài dòng một đoạn
- Độ dài: 
  + Câu trả lời về SẢN PHẨM: 100-200 từ cho câu trả lời thông thường, 300-400 từ khi so sánh nhiều sản phẩm
  + Câu trả lời về CHÍNH SÁCH: HIỂN THỊ FULL TEXT, không giới hạn độ dài (có thể 500-1000 từ)
- Sử dụng số liệu cụ thể (giá, cấu hình) để tăng độ tin cậy
- KHÔNG lạm dụng icon/emoji - chỉ dùng khi thực sự cần thiết

✅ VÍ DỤ TRẢ LỜI TỐT:

VÍ DỤ 1 - Khách hỏi về SẢN PHẨM:
Khách: 'Laptop Dell'
Bot: 'Chào anh/chị! Em rất vui được tư vấn về laptop Dell cho anh/chị. 

Để em đề xuất sản phẩm phù hợp nhất, anh/chị cho em biết:
• Anh/chị cần laptop để làm gì chủ yếu? (văn phòng, gaming, đồ họa, học tập...)
• Ngân sách của anh/chị khoảng bao nhiêu ạ?

Hiện tại em có một số dòng Dell phổ biến:
- **Dell XPS**: Dòng cao cấp, màn hình đẹp, phù hợp đồ họa và công việc chuyên nghiệp
- **Dell Inspiron**: Tầm trung, cân bằng hiệu năng và giá cả, phù hợp đa mục đích
- **Dell Vostro**: Dòng văn phòng, giá tốt, phù hợp công việc hàng ngày

Anh/chị muốn xem sản phẩm nào cụ thể ạ?'

VÍ DỤ 2 - Khách hỏi về CHÍNH SÁCH:
Khách: 'Chính sách bảo hành như thế nào?'
Bot: 'Dạ em xin gửi anh/chị thông tin đầy đủ về chính sách bảo hành của TenTech:

CHÍNH SÁCH BẢO HÀNH TẠI TENTECH

*Lưu ý: Các thiết bị bảo hành phải trong thời gian bảo hành và còn nguyên tem của TenTech!

1. BẢO HÀNH 01 ĐỔI 01
   - Nếu linh kiện thay thế không có sẵn, cần đặt hàng thì TenTech sẽ giải quyết trong tối đa 07 ngày làm việc...
   (Hiển thị FULL TEXT các điều khoản chi tiết)

THÔNG TIN LIÊN HỆ BẢO HÀNH:
Địa chỉ: TenTech, 3 Đ. Cầu Giấy, Ngọc Khánh, Đống Đa, Hà Nội
Thời gian tiếp nhận: 8h00 - 21h00 tất cả các ngày trong tuần (trừ Lễ Tết)
Điện thoại: 024.7106.9999

Anh/chị có thắc mắc gì về chính sách bảo hành không ạ?'

❌ VÍ DỤ TRẢ LỜI KHÔNG TỐT:
'Có laptop Dell. Giá từ 10-30 triệu.' (Quá ngắn, không tư vấn)
'Chính sách bảo hành là 12 tháng.' (Không hiển thị full text, thiếu thông tin chi tiết)

🚫 LƯU Ý QUAN TRỌNG:
- KHÔNG bịa thông tin không có trong context
- KHÔNG đưa ra lời khuyên về sản phẩm không có trong danh sách
- KHÔNG hứa hẹn về giá cả, khuyến mãi nếu không có trong context
- KHÔNG nói xấu đối thủ hoặc sản phẩm khác
- LUÔN ưu tiên trải nghiệm khách hàng, giúp họ đưa ra quyết định đúng đắn
- LUÔN thể hiện sự chuyên nghiệp và nhiệt tình
- KHÔNG lạm dụng icon/emoji - chỉ dùng khi thực sự cần thiết (1-2 icon tối đa)
- KHI KHÁCH HỎI VỀ CHÍNH SÁCH: HIỂN THỊ FULL TEXT, KHÔNG tóm tắt";
    }

    /// <summary>
    /// Build user prompt với context - Có intent detection và clarification
    /// </summary>
    private string BuildUserPrompt(string userMessage, string productContext, string policyContext)
    {
        // Phân tích intent từ userMessage
        var intent = DetectIntent(userMessage);
        var clarificationNeeded = NeedsClarification(userMessage, productContext);
        var hasProducts = !productContext.Contains("Không tìm thấy");
        var hasPolicies = !policyContext.Contains("Không tìm thấy");
        
        var prompt = $@"Câu hỏi của khách hàng: {userMessage}

📊 PHÂN TÍCH CÂU HỎI:
- Loại câu hỏi: {intent}
{(clarificationNeeded ? "- ⚠️ CẦN LÀM RÕ: Câu hỏi này cần được làm rõ thêm. Hãy đặt câu hỏi một cách tự nhiên để hiểu rõ nhu cầu của khách hàng (nhu cầu sử dụng, ngân sách, thương hiệu yêu thích)." : "- ✅ Câu hỏi đã đủ rõ ràng")}

📦 THÔNG TIN SẢN PHẨM CÓ SẴN:
{(hasProducts ? productContext : "⚠️ Không tìm thấy sản phẩm phù hợp trong kho hàng. Hãy hỏi khách hàng về nhu cầu cụ thể để tìm kiếm tốt hơn.")}

📋 THÔNG TIN CHÍNH SÁCH:
{(hasPolicies ? policyContext : "⚠️ Không tìm thấy thông tin chính sách liên quan.")}

🎯 HƯỚNG DẪN TRẢ LỜI:

{(intent == "product_search" ? @"- Nếu có sản phẩm phù hợp: Đề xuất 2-3 sản phẩm tốt nhất, giải thích lý do tại sao phù hợp, so sánh điểm mạnh/yếu
- Nếu không có sản phẩm: Hỏi rõ nhu cầu (mục đích sử dụng, ngân sách) để tìm kiếm tốt hơn
- Luôn kết thúc bằng câu hỏi mở để tiếp tục tư vấn" : "")}

{(intent == "comparison" ? @"- So sánh các sản phẩm một cách khách quan, nêu rõ điểm mạnh/yếu của từng sản phẩm
- Đưa ra lời khuyên dựa trên nhu cầu cụ thể của khách hàng
- Giải thích tại sao sản phẩm này phù hợp hơn sản phẩm kia trong trường hợp cụ thể" : "")}

{(intent == "consultation" ? @"- Hỏi rõ nhu cầu sử dụng (gaming, văn phòng, đồ họa, học tập...)
- Hỏi về ngân sách
- Đề xuất sản phẩm phù hợp dựa trên thông tin đã có
- Giải thích lý do tại sao sản phẩm đó phù hợp" : "")}

{(intent == "price_inquiry" ? @"- Cung cấp giá cả chính xác từ context
- Nếu có nhiều cấu hình, liệt kê giá của từng cấu hình
- Đề cập đến giá trị nhận được so với giá bán" : "")}

{(intent == "policy_inquiry" ? @"- Trích dẫn chính xác từ context chính sách
- Giải thích rõ ràng, dễ hiểu
- Đề cập đến thời gian, điều kiện cụ thể
- Làm rõ các trường hợp đặc biệt nếu có" : "")}

Hãy trả lời câu hỏi của khách hàng một cách tự nhiên, chuyên nghiệp, như một nhân viên tư vấn xuất sắc. Luôn thể hiện sự nhiệt tình và quan tâm đến nhu cầu của khách hàng.";
        
        return prompt;
    }
    
    /// <summary>
    /// Phát hiện intent từ câu hỏi của người dùng
    /// </summary>
    private string DetectIntent(string message)
    {
        var messageLower = message.ToLower();
        
        if (messageLower.Contains("so sánh") || messageLower.Contains("khác nhau") || 
            messageLower.Contains("nên chọn") || messageLower.Contains("tốt hơn"))
        {
            return "comparison";
        }
        
        if (messageLower.Contains("bảo hành") || messageLower.Contains("đổi trả") || 
            messageLower.Contains("hoàn tiền") || messageLower.Contains("chính sách"))
        {
            return "policy_inquiry";
        }
        
        if (messageLower.Contains("tư vấn") || messageLower.Contains("nên mua") || 
            messageLower.Contains("phù hợp") || messageLower.Contains("cho tôi") ||
            messageLower.Contains("giúp tôi"))
        {
            return "consultation";
        }
        
        if (messageLower.Contains("giá") || messageLower.Contains("bao nhiêu") || 
            messageLower.Contains("cost") || messageLower.Contains("price"))
        {
            return "price_inquiry";
        }
        
        return "product_search";
    }
    
    /// <summary>
    /// Kiểm tra xem câu hỏi có cần được làm rõ không
    /// </summary>
    private bool NeedsClarification(string message, string context)
    {
        // Nếu không tìm thấy sản phẩm
        if (context.Contains("Không tìm thấy"))
        {
            return true;
        }
        
        // Nếu câu hỏi quá ngắn và mơ hồ (chỉ có tên thương hiệu hoặc từ khóa đơn giản)
        var messageLower = message.ToLower().Trim();
        var words = messageLower.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Nếu chỉ có 1-2 từ và không có thông tin về nhu cầu
        if (words.Length <= 2)
        {
            var hasUseCase = messageLower.Contains("gaming") || messageLower.Contains("văn phòng") || 
                            messageLower.Contains("đồ họa") || messageLower.Contains("học tập") ||
                            messageLower.Contains("lập trình") || messageLower.Contains("văn phòng");
            var hasBudget = messageLower.Contains("giá") || messageLower.Contains("triệu") || 
                           messageLower.Contains("dưới") || messageLower.Contains("khoảng");
            
            if (!hasUseCase && !hasBudget)
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Build product context từ search results - Format đẹp và đầy đủ thông tin
    /// </summary>
    private string BuildProductContext(List<VectorSearchResult> results)
    {
        if (results == null || results.Count == 0)
        {
            return "Không tìm thấy sản phẩm phù hợp trong kho hàng hiện tại.";
        }

        var context = new System.Text.StringBuilder();
        context.AppendLine($"Tìm thấy {results.Count} sản phẩm liên quan:\n");

        int index = 1;
        foreach (var result in results)
        {
            if (result.Metadata != null)
            {
                var name = result.Metadata.GetValueOrDefault("name", "N/A")?.ToString() ?? "N/A";
                var brand = result.Metadata.GetValueOrDefault("brand", "")?.ToString() ?? "";
                var price = result.Metadata.GetValueOrDefault("price", 0);
                var cpu = result.Metadata.GetValueOrDefault("cpu", "")?.ToString() ?? "";
                var ram = result.Metadata.GetValueOrDefault("ram", "")?.ToString() ?? "";
                var rom = result.Metadata.GetValueOrDefault("rom", "")?.ToString() ?? "";
                var card = result.Metadata.GetValueOrDefault("card", "")?.ToString() ?? "";
                var warranty = result.Metadata.GetValueOrDefault("warrantyPeriod", 0);
                var description = result.Metadata.GetValueOrDefault("description", "")?.ToString() ?? "";
                
                // Khai báo priceValue để sử dụng trong toàn bộ scope
                decimal priceValue = 0;
                if (price is decimal priceDecimal)
                {
                    priceValue = priceDecimal;
                }
                else if (price is int priceInt)
                {
                    priceValue = priceInt;
                }
                else if (price is long priceLong)
                {
                    priceValue = priceLong;
                }
                
                context.AppendLine($"{index}. **{name}**");
                
                if (!string.IsNullOrEmpty(brand))
                {
                    context.AppendLine($"   Thương hiệu: {brand}");
                }
                
                if (priceValue > 0)
                {
                    context.AppendLine($"   Giá: {priceValue:N0} VND");
                    // Thêm phân khúc giá
                    if (priceValue < 10000000)
                        context.AppendLine($"   Phân khúc: Tầm trung, phù hợp học sinh/sinh viên");
                    else if (priceValue < 20000000)
                        context.AppendLine($"   Phân khúc: Tầm trung cao, phù hợp văn phòng và học tập");
                    else if (priceValue < 30000000)
                        context.AppendLine($"   Phân khúc: Cao cấp, phù hợp gaming và đồ họa");
                    else
                        context.AppendLine($"   Phân khúc: Flagship, hiệu năng tối đa");
                }
                
                // Cấu hình chi tiết
                if (!string.IsNullOrEmpty(cpu) || !string.IsNullOrEmpty(ram) || !string.IsNullOrEmpty(rom))
                {
                    context.AppendLine($"   Cấu hình:");
                    if (!string.IsNullOrEmpty(cpu))
                        context.AppendLine($"     • CPU: {cpu}");
                    if (!string.IsNullOrEmpty(ram))
                        context.AppendLine($"     • RAM: {ram}");
                    if (!string.IsNullOrEmpty(rom))
                        context.AppendLine($"     • Ổ cứng: {rom}");
                    if (!string.IsNullOrEmpty(card))
                        context.AppendLine($"     • Card đồ họa: {card}");
                }
                
                if (warranty is int warrantyValue && warrantyValue > 0)
                {
                    context.AppendLine($"   Bảo hành: {warrantyValue} tháng");
                }
                
                if (!string.IsNullOrEmpty(description) && (priceValue == 0 || description != $"Laptop {name} với giá {priceValue:N0} VND"))
                {
                    context.AppendLine($"   Mô tả: {description}");
                }
                
                // Thêm điểm nổi bật dựa trên cấu hình
                var highlights = GetProductHighlights(cpu, ram, card, price);
                if (!string.IsNullOrEmpty(highlights))
                {
                    context.AppendLine($"   Điểm nổi bật: {highlights}");
                }
                
                context.AppendLine();
                index++;
            }
        }
        
        // Thêm gợi ý so sánh nếu có nhiều sản phẩm
        if (results.Count > 1)
        {
            context.AppendLine("💡 Gợi ý: Có thể so sánh các sản phẩm trên về giá cả, cấu hình, và phù hợp với nhu cầu sử dụng.");
        }

        return context.ToString();
    }
    
    /// <summary>
    /// Tạo điểm nổi bật cho sản phẩm dựa trên cấu hình
    /// </summary>
    private string GetProductHighlights(string? cpu, string? ram, string? card, object? price)
    {
        var highlights = new List<string>();
        
        if (!string.IsNullOrEmpty(cpu))
        {
            var cpuLower = cpu.ToLower();
            if (cpuLower.Contains("i7") || cpuLower.Contains("i9") || cpuLower.Contains("ryzen 7") || cpuLower.Contains("ryzen 9"))
            {
                highlights.Add("CPU mạnh");
            }
        }
        
        if (!string.IsNullOrEmpty(ram))
        {
            var ramLower = ram.ToLower();
            if (ramLower.Contains("16") || ramLower.Contains("32"))
            {
                highlights.Add("RAM lớn, đa nhiệm tốt");
            }
        }
        
        if (!string.IsNullOrEmpty(card))
        {
            var cardLower = card.ToLower();
            if (cardLower.Contains("rtx") || cardLower.Contains("gtx") || cardLower.Contains("radeon"))
            {
                highlights.Add("Card đồ họa rời, gaming tốt");
            }
        }
        
        if (price is decimal priceValue)
        {
            if (priceValue < 15000000)
            {
                highlights.Add("Giá tốt");
            }
        }
        
        return highlights.Any() ? string.Join(", ", highlights) : "";
    }

    /// <summary>
    /// Build policy context từ search results
    /// LƯU Ý: Giữ nguyên FULL TEXT chính sách, KHÔNG tóm tắt
    /// </summary>
    private string BuildPolicyContext(List<VectorSearchResult> results)
    {
        if (results == null || results.Count == 0)
        {
            return "Không tìm thấy thông tin chính sách liên quan.";
        }

        var context = new System.Text.StringBuilder();
        context.AppendLine("=== THÔNG TIN CHÍNH SÁCH (FULL TEXT) ===\n");
        context.AppendLine("LƯU Ý: Hiển thị TOÀN BỘ nội dung chính sách cho khách hàng, KHÔNG rút gọn.\n");

        foreach (var result in results)
        {
            if (!string.IsNullOrEmpty(result.Content))
            {
                // Hiển thị full text, không truncate
                context.AppendLine(result.Content);
                context.AppendLine("\n" + new string('-', 80) + "\n");
            }
        }

        return context.ToString();
    }

    /// <summary>
    /// Search products với fallback mechanism (internal helper để parallelize)
    /// </summary>
    private async Task<List<VectorSearchResult>> SearchProductsWithFallbackAsync(string userMessage)
    {
        bool qdrantSearchFailed = false;
        List<VectorSearchResult> productResults = new List<VectorSearchResult>();

        // Thử search từ Qdrant trước
        try
        {
            productResults = await _qdrantVectorService.SearchProductsAsync(userMessage, topK: 5);
            _logger.LogInformation("Found {Count} product results from Qdrant", productResults?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching products from Qdrant, will try SQL fallback: {Error}", ex.Message);
            productResults = new List<VectorSearchResult>();
            qdrantSearchFailed = true;
        }

        // Fallback: Nếu Qdrant fail hoặc không có kết quả, search từ SQL Server
        if (qdrantSearchFailed || productResults.Count == 0)
        {
            try
            {
                _logger.LogInformation("Attempting SQL fallback search for: {Message}", userMessage);
                var sqlProducts = await FallbackSearchFromSqlAsync(userMessage);
                if (sqlProducts != null && sqlProducts.Count > 0)
                {
                    // Convert ProductDTO to VectorSearchResult format for consistency
                    productResults = sqlProducts.Select(p => new VectorSearchResult
                    {
                        Content = $"{p.ProductName} - {p.SellingPrice:N0} VND",
                        Score = 0.8f, // Default score for SQL results
                        Metadata = new Dictionary<string, object>
                        {
                            ["productId"] = p.ProductId ?? "",
                            ["name"] = p.ProductName ?? "",
                            ["price"] = p.SellingPrice ?? 0,
                            ["description"] = $"Laptop {p.ProductName} với giá {p.SellingPrice:N0} VND"
                        }
                    }).ToList();
                    _logger.LogInformation("SQL fallback found {Count} products", productResults.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SQL fallback search");
                // Continue with empty results
            }
        }

        return productResults;
    }

    /// <summary>
    /// Parse suggested products từ search results - OPTIMIZED với batch query
    /// </summary>
    private async Task<List<ProductDTO>?> ParseSuggestedProductsAsync(List<VectorSearchResult> results)
    {
        if (results == null || results.Count == 0)
        {
            return null;
        }

        // Extract tất cả product IDs trước
        var productIds = new List<string>();
        foreach (var result in results)
        {
            if (result.Metadata != null && result.Metadata.TryGetValue("productId", out var productIdObj))
            {
                var productId = productIdObj?.ToString();
                if (!string.IsNullOrEmpty(productId))
                {
                    productIds.Add(productId);
                }
            }
        }

        if (productIds.Count == 0)
        {
            return null;
        }

        // Batch query: Lấy tất cả products trong 1 query thay vì N queries
        try
        {
            var products = await _productService.GetProductsByIdsAsync(productIds);
            
            // Giữ nguyên thứ tự theo results
            var orderedProducts = new List<ProductDTO>();
            foreach (var productId in productIds)
            {
                var product = products.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    orderedProducts.Add(product);
                }
            }

            return orderedProducts.Count > 0 ? orderedProducts : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch product query");
            return null;
        }
    }

    /// <summary>
    /// Fallback search từ SQL Server khi Qdrant fail hoặc không có kết quả
    /// </summary>
    private async Task<List<ProductDTO>?> FallbackSearchFromSqlAsync(string userMessage)
    {
        try
        {
            // Simple keyword extraction from user message
            var searchTerm = userMessage.ToLower();
            
            // Try to extract price range
            decimal? minPrice = null;
            decimal? maxPrice = null;
            
            // Extract "dưới X triệu" -> maxPrice
            var underMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"dưới\s*(\d+)\s*triệu");
            if (underMatch.Success && decimal.TryParse(underMatch.Groups[1].Value, out var underValue))
            {
                maxPrice = underValue * 1000000;
            }
            
            // Extract "từ X đến Y triệu" -> minPrice, maxPrice
            var rangeMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"từ\s*(\d+)\s*đến\s*(\d+)\s*triệu");
            if (rangeMatch.Success)
            {
                if (decimal.TryParse(rangeMatch.Groups[1].Value, out var min) && 
                    decimal.TryParse(rangeMatch.Groups[2].Value, out var max))
                {
                    minPrice = min * 1000000;
                    maxPrice = max * 1000000;
                }
            }
            
            // Extract brand names
            string? brandId = null;
            var brands = new[] { "dell", "hp", "lenovo", "asus", "acer", "msi", "gigabyte" };
            foreach (var brand in brands)
            {
                if (searchTerm.Contains(brand))
                {
                    // Try to find brand ID (this is a simplified approach)
                    // In real implementation, you'd query the database for brand IDs
                    brandId = brand; // This would need to be mapped to actual brand IDs
                    break;
                }
            }

            // Build search criteria
            var criteria = new ProductSearchCriteria
            {
                SearchTerm = userMessage,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                // BrandId = brandId // Uncomment when brand mapping is implemented
            };

            // Search products
            var products = await _productService.SearchProductsAsync(criteria);
            
            // Limit to top 5 results
            return products.Take(5).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback SQL search");
            return null;
        }
    }

    /// <summary>
    /// Sanitize và validate response từ LLM
    /// </summary>
    private string SanitizeResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return "Xin lỗi, tôi không thể tạo phản hồi lúc này. Vui lòng thử lại sau.";
        }

        // Trim
        var sanitized = response.Trim();
        
        // Giới hạn độ dài response - chỉ cắt khi THỰC SỰ quá dài bất thường (> 15000 ký tự)
        // Chính sách có thể dài 5000-8000 ký tự, nên không cắt ở mức 2000
        const int maxLength = 15000;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength) + "\n\n... (Nội dung quá dài, vui lòng liên hệ nhân viên để biết thêm chi tiết)";
            _logger.LogWarning("Response truncated from {OriginalLength} to {MaxLength} characters", 
                response.Length, maxLength);
        }

        return sanitized;
    }

    /// <summary>
    /// Build fallback response khi LLM fail - vẫn cung cấp thông tin hữu ích từ data có sẵn
    /// </summary>
    private string BuildFallbackResponse(string userMessage, List<VectorSearchResult> productResults, List<VectorSearchResult> policyResults)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Xin chào! Em là trợ lý tư vấn của cửa hàng.");
        
            // Nếu có sản phẩm tìm được
        if (productResults != null && productResults.Count > 0)
        {
            sb.AppendLine($"\nEm đã tìm thấy {productResults.Count} sản phẩm phù hợp với yêu cầu của anh/chị:");
            
            // HIỂN THỊ ĐẦY ĐỦ - không Take(3) nữa
            foreach (var product in productResults)
            {
                if (product.Metadata != null)
                {
                    var name = product.Metadata.GetValueOrDefault("name", "N/A");
                    var price = product.Metadata.TryGetValue("price", out var priceObj) ? priceObj : null;
                    
                    sb.Append($"\n• {name}");
                    if (price != null)
                    {
                        sb.Append($" - Giá: {price:N0} VND");
                    }
                }
            }
            
            sb.AppendLine("\n\nAnh/chị có thể xem chi tiết sản phẩm bên dưới hoặc liên hệ nhân viên để được tư vấn thêm!");
        }
        // Nếu hỏi về chính sách
        else if (policyResults != null && policyResults.Count > 0)
        {
            sb.AppendLine("\nThông tin chính sách liên quan:");
            
            foreach (var policy in policyResults.Take(2))
            {
                if (!string.IsNullOrEmpty(policy.Content))
                {
                    sb.AppendLine($"\n{policy.Content}");
                }
            }
        }
        // Không tìm được gì
        else
        {
            sb.AppendLine("\nHiện tại hệ thống đang gặp sự cố tạm thời. Anh/chị vui lòng:");
            sb.AppendLine("• Thử lại sau vài giây");
            sb.AppendLine("• Hoặc liên hệ nhân viên để được hỗ trợ trực tiếp");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Convert ProductDTO sang ProductSuggestion với URLs
    /// </summary>
    private List<ProductSuggestion> ConvertToProductSuggestions(List<ProductDTO> products)
    {
        // Lấy Backend URL cho ảnh
        var httpContext = _httpContextAccessor.HttpContext;
        var backendUrl = httpContext != null 
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : "http://localhost:5068";

        return products.Select(p => 
        {
            // Build image URL (dùng Backend URL)
            var imageUrl = $"{backendUrl}/imageProducts/default.jpg";
            if (!string.IsNullOrEmpty(p.Avatar))
            {
                imageUrl = p.Avatar.StartsWith("http") 
                    ? p.Avatar 
                    : $"{backendUrl}{(p.Avatar.StartsWith("/") ? "" : "/")}{p.Avatar}";
            }
            else if (p.Images != null && p.Images.Count > 0)
            {
                var firstImage = p.Images[0];
                if (!string.IsNullOrEmpty(firstImage.ImageId))
                {
                    imageUrl = $"{backendUrl}/imageProducts/{firstImage.ImageId}";
                }
            }

            // Build detail URL - Phải trỏ về FRONTEND (parameter phải là 'id' theo HomeController)
            var detailUrl = $"{FrontendUrl}/Home/ProductDetail?id={p.ProductId}";

            // Lấy config đầu tiên
            var firstConfig = p.Configurations?.FirstOrDefault();

            return new ProductSuggestion
            {
                ProductId = p.ProductId ?? "",
                Name = p.ProductName ?? "",
                Price = p.SellingPrice ?? 0,
                ImageUrl = imageUrl,
                DetailUrl = detailUrl,
                Brand = p.BrandId,
                Cpu = firstConfig?.Cpu,
                Ram = firstConfig?.Ram,
                Storage = firstConfig?.Rom
            };
        }).ToList();
    }
}

