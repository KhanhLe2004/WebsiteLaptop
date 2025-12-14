using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebLaptopBE.AI.SemanticKernel;
using WebLaptopBE.DTOs;
using WebLaptopBE.Data;

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
    private readonly IServiceProvider _serviceProvider;
    
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
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _qdrantVectorService = qdrantVectorService;
        _semanticKernelService = semanticKernelService;
        _productService = productService;
        _logger = logger;
        _configuration = configuration;
        _inputValidationService = inputValidationService;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    public async Task<RAGChatResponse> ProcessUserMessageAsync(string userMessage, string? customerId = null)
    {
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

            // Bước 0.5: Kiểm tra brand được hỏi TRƯỚC KHI search để phát hiện brand không có sản phẩm
            string? unavailableBrandInfo = null;
            var searchTermLower = userMessage.ToLower();
            var allBrandKeywords = new Dictionary<string, string[]>
            {
                { "dell", new[] { "dell" } },
                { "lenovo", new[] { "lenovo" } },
                { "hp", new[] { "hp", "hewlett packard" } },
                { "asus", new[] { "asus", "rog" } },
                { "apple", new[] { "apple", "macbook", "mac", "iphone" } },
                { "samsung", new[] { "samsung", "galaxy" } },
                { "acer", new[] { "acer" } },
                { "msi", new[] { "msi" } },
                { "gigabyte", new[] { "gigabyte", "giga" } },
                { "sony", new[] { "sony", "vaio" } },
                { "huawei", new[] { "huawei", "matebook" } },
                { "xiaomi", new[] { "xiaomi", "mi" } },
                { "lg", new[] { "lg" } },
                { "toshiba", new[] { "toshiba" } },
                { "fujitsu", new[] { "fujitsu" } }
            };
            
            // Phát hiện brand được hỏi trong câu và kiểm tra xem có trong database không
            // QUAN TRỌNG: Chỉ set unavailableBrandInfo khi brand KHÔNG có trong database hoặc không có sản phẩm
            foreach (var brandPair in allBrandKeywords)
            {
                var brandName = brandPair.Key;
                var keywords = brandPair.Value;
                
                // Nếu câu hỏi có chứa brand này (kiểm tra từng keyword)
                bool brandMentioned = false;
                foreach (var keyword in keywords)
                {
                    if (searchTermLower.Contains(keyword))
                    {
                        brandMentioned = true;
                        break;
                    }
                }
                
                if (brandMentioned)
                {
                    _logger.LogInformation("Detected brand mention in query: {BrandName}", brandName);
                    
                    // Kiểm tra xem brand này có trong database không
                    try
                    {
                        var dbContext = _serviceProvider.GetService<Data.WebLaptopTenTechContext>();
                        if (dbContext != null)
                        {
                            // Tìm brand trong database (so sánh không phân biệt hoa thường)
                            // QUAN TRỌNG: So sánh chính xác brand name (không dùng Contains để tránh false positive)
                            var brandEntity = await dbContext.Brands
                                .FirstOrDefaultAsync(b => b.BrandName != null && 
                                    b.BrandName.ToLower().Trim() == brandName.ToLower().Trim());
                            
                            // Nếu không tìm thấy chính xác, thử tìm bằng Contains (nhưng ưu tiên chính xác)
                            if (brandEntity == null)
                            {
                                brandEntity = await dbContext.Brands
                                    .FirstOrDefaultAsync(b => b.BrandName != null && 
                                        (b.BrandName.ToLower().Trim().Contains(brandName.ToLower().Trim()) ||
                                         brandName.ToLower().Trim().Contains(b.BrandName.ToLower().Trim())));
                            }
                            
                            // Nếu brand không tồn tại trong database → cửa hàng không kinh doanh
                            if (brandEntity == null)
                            {
                                unavailableBrandInfo = brandName;
                                _logger.LogWarning("⚠️⚠️⚠️ Brand '{BrandName}' NOT FOUND in database - store does NOT sell this brand. Setting unavailableBrandInfo = '{UnavailableBrand}'. AI will be informed to tell customer store does not sell this brand.", brandName, unavailableBrandInfo);
                                break; // Dừng lại khi tìm thấy brand không có
                            }
                            else
                            {
                                // Brand có trong database, kiểm tra xem có sản phẩm active không
                                var hasProducts = await dbContext.Products
                                    .AnyAsync(p => p.BrandId == brandEntity.BrandId && p.Active == true);
                                
                                if (!hasProducts)
                                {
                                    unavailableBrandInfo = brandEntity.BrandName ?? brandName;
                                    _logger.LogInformation("Brand '{BrandName}' exists but has NO active products - store does not sell this brand", unavailableBrandInfo);
                                    break; // Dừng lại khi tìm thấy brand không có sản phẩm
                                }
                                else
                                {
                                    // Brand có trong database và có sản phẩm → brand có sẵn
                                    _logger.LogInformation("Brand '{BrandName}' is AVAILABLE with products", brandEntity.BrandName);
                                    // Không set unavailableBrandInfo, tiếp tục tìm kiếm bình thường
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking brand availability: {BrandName} - {ErrorMessage}", brandName, ex.Message);
                        // Nếu có lỗi khi kiểm tra brand, giả sử brand không có để an toàn
                        // Nhưng chỉ set nếu đã detect brand được mention
                        if (brandMentioned)
                        {
                            unavailableBrandInfo = brandName;
                            _logger.LogWarning("⚠️ Error checking brand '{BrandName}' - assuming unavailable for safety", brandName);
                            break;
                        }
                    }
                    
                    // Nếu đã xác định brand không có, dừng lại
                    if (!string.IsNullOrEmpty(unavailableBrandInfo))
                    {
                        break; // Dừng lại khi đã xác định brand không có
                    }
                }
            }
            
            // Log kết quả kiểm tra brand
            if (!string.IsNullOrEmpty(unavailableBrandInfo))
            {
                _logger.LogWarning("⚠️⚠️⚠️ Final result: Brand '{BrandName}' is UNAVAILABLE - store does NOT sell this brand. Will SKIP product search and inform AI.", unavailableBrandInfo);
            }

            // Bước 1 & 2: Parallelize products và policies search với timeout tổng
            List<VectorSearchResult> productResults = new List<VectorSearchResult>();
            List<VectorSearchResult> policyResults = new List<VectorSearchResult>();
            
            // Detect use case từ userMessage để optimize search
            var detectedUseCase = DetectUseCaseFromMessage(userMessage);

            // QUAN TRỌNG: Nếu brand không có sản phẩm, SKIP product search hoàn toàn
            Task<List<VectorSearchResult>> productSearchTask;
            if (!string.IsNullOrEmpty(unavailableBrandInfo))
            {
                // Brand không có sản phẩm → không cần search, trả về empty list ngay
                _logger.LogWarning("⚠️ Brand '{BrandName}' is unavailable - SKIPPING product search completely", unavailableBrandInfo);
                productSearchTask = Task.FromResult(new List<VectorSearchResult>());
            }
            else
            {
                // Brand có sản phẩm → search bình thường
                productSearchTask = SearchProductsWithFallbackAsync(userMessage);
            }
            
            // QUAN TRỌNG: Nếu brand không có sản phẩm, SKIP policy search hoàn toàn
            Task<List<VectorSearchResult>> policySearchTask;
            if (!string.IsNullOrEmpty(unavailableBrandInfo))
            {
                // Brand không có sản phẩm → không cần search policy, trả về empty list ngay
                _logger.LogWarning("⚠️ Brand '{BrandName}' is unavailable - SKIPPING policy search completely", unavailableBrandInfo);
                policySearchTask = Task.FromResult(new List<VectorSearchResult>());
            }
            else
            {
                // Brand có sản phẩm → search policy bình thường
                policySearchTask = _qdrantVectorService.SearchPoliciesAsync(userMessage, topK: 3);
            }
            
            // Chạy song song products và policies search với timeout tổng 8 giây
            using var searchCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

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
                
                _logger.LogInformation("Found {ProductCount} product results and {PolicyCount} policy results", 
                    productResults?.Count ?? 0, policyResults?.Count ?? 0);
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

            // QUAN TRỌNG: Nếu brand không có sản phẩm, KHÔNG gọi GetFallbackPolicies
            // Chỉ gọi GetFallbackPolicies khi brand có sản phẩm
            if (string.IsNullOrEmpty(unavailableBrandInfo))
            {
                // Nếu không lấy được policy từ Qdrant, fallback sang bộ policy mặc định (không cần vector DB)
                if (policyResults == null || policyResults.Count == 0)
                {
                    policyResults = GetFallbackPolicies(userMessage);
                    if (policyResults.Count > 0)
                    {
                        _logger.LogWarning("Using fallback policies because Qdrant policy search returned no results");
                    }
                }
            }
            else
            {
                // Brand không có sản phẩm → đảm bảo policyResults rỗng
                policyResults = new List<VectorSearchResult>();
                _logger.LogWarning("⚠️ Brand '{BrandName}' is unavailable - ensuring policyResults is empty, will NOT call GetFallbackPolicies", unavailableBrandInfo);
            }

            // Bước 3: Đảm bảo productResults và policyResults rỗng nếu brand không có sản phẩm
            // QUAN TRỌNG: Phải clear cả productResults và policyResults TRƯỚC khi build context
            if (!string.IsNullOrEmpty(unavailableBrandInfo))
            {
                productResults = new List<VectorSearchResult>(); // Clear results để AI biết không có sản phẩm
                policyResults = new List<VectorSearchResult>(); // Clear policy results để AI không hiển thị chính sách
                _logger.LogWarning("⚠️⚠️⚠️ Brand '{BrandName}' is UNAVAILABLE - ensuring productResults and policyResults are empty. AI MUST respond that store does NOT sell this brand, WITHOUT showing policies or suggesting products.", unavailableBrandInfo);
            }
            
            // Bước 3: Build context từ search results (có thể include use case info)
            // QUAN TRỌNG: Log để debug
            if (!string.IsNullOrEmpty(unavailableBrandInfo))
            {
                _logger.LogWarning("⚠️⚠️⚠️ Building context with unavailableBrandInfo = '{BrandName}'. ProductContext will contain 'CỬA HÀNG KHÔNG KINH DOANH' message.", unavailableBrandInfo);
            }
            
            var productContext = BuildProductContext(productResults, detectedUseCase, unavailableBrandInfo);
            
            // QUAN TRỌNG: Nếu brand không có sản phẩm, KHÔNG hiển thị policy context
            // Chỉ trả lời ngắn gọn rằng sản phẩm không được kinh doanh
            string policyContext;
            if (!string.IsNullOrEmpty(unavailableBrandInfo))
            {
                policyContext = ""; // Clear policy context khi sản phẩm không được kinh doanh
                _logger.LogWarning("⚠️ Brand '{BrandName}' is unavailable - clearing policy context. AI should only respond that product is not sold, without showing policies.", unavailableBrandInfo);
            }
            else
            {
                policyContext = BuildPolicyContext(policyResults);
            }
            
            // Log để debug - kiểm tra xem context có đúng không
            if (!string.IsNullOrEmpty(unavailableBrandInfo))
            {
                if (productContext.Contains("CỬA HÀNG KHÔNG KINH DOANH"))
                {
                    _logger.LogWarning("✅ ProductContext correctly contains 'CỬA HÀNG KHÔNG KINH DOANH' message. AI should respond correctly without showing policies.");
                }
                else
                {
                    _logger.LogError("❌ ERROR: ProductContext does NOT contain 'CỬA HÀNG KHÔNG KINH DOANH' message even though unavailableBrandInfo = '{BrandName}'. This is a bug!", unavailableBrandInfo);
                }
            }

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
                _logger.LogInformation("Generated response from LLM, length: {Length}", 
                    response?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Semantic Kernel/OpenAI: {ErrorType} - {ErrorMessage}", 
                    ex.GetType().Name, ex.Message);
                
                // QUAN TRỌNG: Nếu brand không có sản phẩm, trả lời trực tiếp không cần LLM
                if (!string.IsNullOrEmpty(unavailableBrandInfo))
                {
                    var brandDisplayName = char.ToUpper(unavailableBrandInfo[0]) + unavailableBrandInfo.Substring(1).ToLower();
                    response = $"Em xin lỗi, hiện tại cửa hàng TenTech không kinh doanh laptop {brandDisplayName} ạ.";
                    llmSucceeded = true; // Đánh dấu là đã có response
                    _logger.LogWarning("⚠️ LLM failed but brand is unavailable - using direct response without LLM");
                }
                else
                {
                    // GRACEFUL DEGRADATION: Tạo response từ dữ liệu có sẵn thay vì fail hoàn toàn
                    response = BuildFallbackResponse(userMessage, productResults, policyResults);
                }
            }

            // Bước 6: Parse suggested products từ productResults
            // QUAN TRỌNG: Nếu brand không có sản phẩm, KHÔNG parse suggested products
            List<ProductDTO>? productDTOs = null;
            if (string.IsNullOrEmpty(unavailableBrandInfo))
            {
                // Chỉ parse suggested products khi brand có sản phẩm
                try
                {
                    productDTOs = await ParseSuggestedProductsAsync(productResults);
                    
                    // Nếu không parse được từ vector results, thử fallback search từ SQL
                    if (productDTOs == null || productDTOs.Count == 0)
                    {
                        _logger.LogInformation("No products parsed from vector results, trying SQL fallback");
                        var sqlProducts = await FallbackSearchFromSqlAsync(userMessage);
                        if (sqlProducts != null && sqlProducts.Count > 0)
                        {
                            productDTOs = sqlProducts;
                            _logger.LogInformation("SQL fallback found {Count} products", productDTOs.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing suggested products, will try SQL fallback");
                    // Thử fallback search từ SQL nếu parse fail
                    try
                    {
                        var sqlProducts = await FallbackSearchFromSqlAsync(userMessage);
                        if (sqlProducts != null && sqlProducts.Count > 0)
                        {
                            productDTOs = sqlProducts;
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "SQL fallback also failed");
                    }
                }
            }
            else
            {
                // Brand không có sản phẩm → không parse suggested products
                productDTOs = null;
                _logger.LogWarning("⚠️ Brand '{BrandName}' is unavailable - SKIPPING suggested products parsing. Will NOT suggest any products.", unavailableBrandInfo);
            }

            // Convert ProductDTO to ProductSuggestion
            // Validate and sanitize response
            var sanitizedResponse = SanitizeResponse(response);
            
            return new RAGChatResponse
            {
                Answer = sanitizedResponse,
                SuggestedProducts = productDTOs != null && productDTOs.Count > 0 
                    ? ConvertToProductSuggestions(productDTOs) 
                    : null,
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

1. KHI TƯ VẤN SẢN PHẨM (QUAN TRỌNG - ĐỌC KỸ):
   ✅ LUÔN gợi ý sản phẩm từ danh sách 'THÔNG TIN SẢN PHẨM CÓ SẴN' được cung cấp - KHÔNG bịa sản phẩm không có trong danh sách
   ✅ Khi khách hỏi MỘT CÂU DÀI với nhiều yêu cầu (ví dụ: 'Tôi muốn mua laptop Dell có CPU i7, RAM 16GB, giá dưới 25 triệu để chơi game'):
      - Phân tích TẤT CẢ các yêu cầu trong câu (thương hiệu, CPU, RAM, giá, mục đích sử dụng)
      - Đề xuất sản phẩm phù hợp với TẤT CẢ các yêu cầu đó
      - Nếu không có sản phẩm phù hợp 100% → đề xuất sản phẩm gần nhất và giải thích sự khác biệt
      - Highlight từng yêu cầu: '✅ CPU i7', '✅ RAM 16GB', '✅ Giá dưới 25 triệu', '✅ Phù hợp gaming'
      - Giải thích tại sao sản phẩm phù hợp với từng yêu cầu
   ✅ Khi khách hỏi về thương hiệu cụ thể (ví dụ: 'máy Dell', 'laptop HP'): 
      - Nếu có sản phẩm của thương hiệu đó trong danh sách → Đề xuất NGAY các sản phẩm đó
      - Highlight các sản phẩm phù hợp với yêu cầu
      - Không hỏi lại nếu đã có sản phẩm trong danh sách
   ✅ Khi khách hỏi về MODEL/SERIES CỤ THỂ (ví dụ: 'HP Omen', 'Dell XPS', 'ASUS ROG', 'Lenovo ThinkPad'): 
      - QUAN TRỌNG: Ưu tiên đề xuất các sản phẩm có tên/model chứa đúng model/series đó
      - Nếu có sản phẩm đúng model/series trong danh sách → CHỈ đề xuất các sản phẩm đó, KHÔNG đề xuất các model khác của cùng thương hiệu
      - Ví dụ: Khách hỏi 'HP Omen' → CHỈ đề xuất laptop HP Omen, KHÔNG đề xuất HP Pavilion, HP EliteBook, v.v.
      - Ví dụ: Khách hỏi 'Dell XPS' → CHỈ đề xuất laptop Dell XPS, KHÔNG đề xuất Dell Inspiron, Dell Vostro, v.v.
      - Nếu không có sản phẩm đúng model/series → Thông báo rõ ràng và đề xuất các model tương tự hoặc hỏi khách có muốn xem các model khác không
   ✅ Khi khách hỏi về giá rẻ (ví dụ: 'máy rẻ', 'laptop giá rẻ'):
      - Nếu có sản phẩm giá rẻ trong danh sách → Đề xuất NGAY các sản phẩm đó (sắp xếp từ rẻ nhất)
      - Highlight giá cả và giá trị nhận được
      - Giải thích tại sao sản phẩm này có giá tốt
   ✅ Khi khách hỏi về cấu hình (CPU, RAM, ROM, Card):
      - Nếu có sản phẩm phù hợp trong danh sách → Đề xuất NGAY các sản phẩm đó
      - LIỆT KÊ CHI TIẾT cấu hình của từng sản phẩm (CPU, RAM, ROM, Card)
      - Giải thích ý nghĩa của từng thông số (ví dụ: 'Intel Core i5 phù hợp văn phòng', '16GB RAM đủ cho đa nhiệm')
      - So sánh cấu hình giữa các sản phẩm nếu có nhiều sản phẩm
      - Nếu khách hỏi 'laptop có CPU i7' → chỉ đề xuất sản phẩm có CPU i7
      - Nếu khách hỏi 'laptop có RAM 16GB' → chỉ đề xuất sản phẩm có RAM 16GB
      - Nếu khách hỏi 'laptop có card rời' → chỉ đề xuất sản phẩm có card đồ họa rời (RTX, GTX)
   ✅ Khi khách hỏi về mục đích sử dụng (gaming, văn phòng, đồ họa, học tập, lập trình):
      - Đề xuất sản phẩm phù hợp với mục đích đó
      - Giải thích tại sao sản phẩm phù hợp (ví dụ: 'Card RTX 3060 mạnh mẽ, phù hợp gaming')
      - Nếu có nhiều yêu cầu kết hợp → ưu tiên sản phẩm đáp ứng nhiều yêu cầu nhất
   ✅ Đề xuất 2-10 sản phẩm phù hợp nhất với yêu cầu của khách hàng (nhiều hơn nếu câu hỏi dài, có nhiều tiêu chí)
   ✅ So sánh điểm mạnh/yếu của từng sản phẩm một cách khách quan
   ✅ Đề cập đến giá cả và giá trị nhận được (ví dụ: 'Sản phẩm này có giá tốt so với cấu hình')
   ✅ Kết thúc bằng câu hỏi mở để tiếp tục tư vấn (ví dụ: 'Anh/chị có muốn xem thêm sản phẩm nào khác không?')

2. KHI KHÁCH HỎI MƠ HỒ HOẶC THIẾU THÔNG TIN:
   ✅ Nếu khách chỉ hỏi chung chung (ví dụ: 'laptop', 'máy tính', 'máy', 'PC', 'notebook'):
      - Đây là các từ khóa đồng nghĩa, đều có nghĩa là sản phẩm laptop
      - Nếu có sản phẩm trong danh sách → Đề xuất NGAY các sản phẩm tốt nhất (top 5-10)
      - Giới thiệu đa dạng sản phẩm (nhiều thương hiệu, nhiều phân khúc giá)
      - Sau đó hỏi thêm: 'Anh/chị muốn laptop để làm gì chủ yếu ạ? (gaming, văn phòng, đồ họa...)'
   ✅ Nếu khách hỏi mơ hồ nhưng có một số thông tin:
      - Đặt câu hỏi làm rõ một cách tự nhiên:
        • 'Anh/chị muốn laptop để làm gì chủ yếu ạ? (gaming, văn phòng, đồ họa...)'
        • 'Ngân sách của anh/chị khoảng bao nhiêu ạ?'
        • 'Anh/chị có thương hiệu nào yêu thích không?'
      - Đưa ra gợi ý cụ thể: 'Nếu anh/chị cần laptop văn phòng, em có thể đề xuất...'
      - Không để khách hàng cảm thấy bị tra hỏi, mà như đang được tư vấn

3. KHI KHÔNG CÓ THÔNG TIN HOẶC KHÔNG CHẮC CHẮN:
   ✅ Thành thật: 'Em xin lỗi, hiện tại em chưa có thông tin chi tiết về...'
   ✅ Đề xuất giải pháp: 'Anh/chị có thể liên hệ hotline hoặc đến cửa hàng để được tư vấn trực tiếp'
   ✅ Không bịa thông tin, không hứa hẹn những gì không chắc chắn

4. KHI CỬA HÀNG KHÔNG KINH DOANH SẢN PHẨM (⚠️⚠️⚠️ CỰC KỲ QUAN TRỌNG - ĐỌC KỸ):
   ⚠️⚠️⚠️ NẾU trong 'THÔNG TIN SẢN PHẨM CÓ SẴN' có thông báo '⚠️⚠️⚠️ CỬA HÀNG KHÔNG KINH DOANH' hoặc 'CỬA HÀNG KHÔNG KINH DOANH':
      → ĐÂY KHÔNG PHẢI là trường hợp 'không tìm thấy sản phẩm phù hợp'
      → ĐÂY LÀ tình huống cửa hàng KHÔNG KINH DOANH brand đó (ví dụ: Acer, Apple, Samsung, MSI, Gigabyte)
      → BẮT BUỘC phải trả lời NGAY, rõ ràng, lịch sự theo ĐÚNG format trong context
      → KHÔNG được bịa sản phẩm, KHÔNG được nói mơ hồ như 'có thể có' hoặc 'để em kiểm tra'
      → KHÔNG được đề xuất sản phẩm từ brand không có trong kho
      → ⚠️⚠️⚠️ QUAN TRỌNG: KHÔNG được hiển thị thông tin chính sách bảo hành, bảo mật, hoặc bất kỳ thông tin nào khác
      → CHỈ trả lời ngắn gọn rằng sản phẩm không được kinh doanh, theo ĐÚNG format trong context
      → PHẢI trả lời theo ĐÚNG format trong context, KHÔNG tự ý thay đổi
      → Ví dụ format: 'Em xin lỗi, hiện tại cửa hàng TenTech không kinh doanh laptop [tên brand] ạ.'
   ⚠️⚠️⚠️ LƯU Ý: Nếu context có thông báo 'CỬA HÀNG KHÔNG KINH DOANH', bạn PHẢI trả lời theo ĐÚNG format trong context, KHÔNG được tự ý thay đổi, KHÔNG được bịa sản phẩm, và KHÔNG được hiển thị thông tin chính sách

5. KHI TRẢ LỜI VỀ CHÍNH SÁCH (QUAN TRỌNG - ĐỌC KỸ):
   ✅ HIỂN THỊ FULL TEXT CHÍNH SÁCH từ context được cung cấp - KHÔNG tóm tắt, KHÔNG rút gọn
   ✅ Nếu có nhiều chính sách liên quan, hiển thị TẤT CẢ các chính sách đó
   ✅ Giữ nguyên cấu trúc, định dạng, và nội dung chi tiết của chính sách
   ✅ Giải thích thêm nếu khách hàng yêu cầu, nhưng vẫn phải hiển thị full text trước
   ✅ Đề cập đến thông tin liên hệ (địa chỉ, hotline, email) nếu có trong chính sách

6. KHI SO SÁNH SẢN PHẨM:
   ✅ So sánh khách quan, không thiên vị
   ✅ Nêu rõ điểm mạnh/yếu của từng sản phẩm
   ✅ Đưa ra lời khuyên dựa trên nhu cầu cụ thể của khách hàng
   ✅ Giải thích tại sao sản phẩm này phù hợp hơn sản phẩm kia trong trường hợp cụ thể

📝 ĐỊNH DẠNG TRẢ LỜI:
- KHI HIỂN THỊ SẢN PHẨM: PHẢI hiển thị đầy đủ thông tin theo format sau (QUAN TRỌNG):
  + Tên sản phẩm: Hiển thị TÊN SẢN PHẨM KÈM MODEL (nếu có model trong context)
  + Ví dụ: Nếu context có Dell Alienware và model 16X Aurora AC2025 thì hiển thị: **Dell Alienware 16X Aurora AC2025**
  + Thương hiệu: Hiển thị Thương hiệu: [tên brand]
  + Giá: Hiển thị Giá: [giá] VND
  + Format đúng: 
    • **Dell Alienware 16X Aurora AC2025**
      Thương hiệu: Dell
      Giá: 68,990,000 VND
- Sử dụng bullet points (•) cho danh sách sản phẩm hoặc thông tin quan trọng
- In đậm tên sản phẩm hoặc thông tin quan trọng (dùng **text**)
- Chia đoạn rõ ràng, không viết dài dòng một đoạn
- Độ dài: 
  + Câu trả lời về SẢN PHẨM: 100-200 từ cho câu trả lời thông thường, 300-400 từ khi so sánh nhiều sản phẩm
  + Câu trả lời về CHÍNH SÁCH: HIỂN THỊ FULL TEXT, không giới hạn độ dài (có thể 500-1000 từ)
- Sử dụng số liệu cụ thể (giá, cấu hình) để tăng độ tin cậy
- KHÔNG lạm dụng icon/emoji - chỉ dùng khi thực sự cần thiết

✅ VÍ DỤ TRẢ LỜI TỐT:

VÍ DỤ 1 - Khách hỏi về SẢN PHẨM CỤ THỂ:
Khách: 'Tôi muốn mua máy Dell'
Bot: 'Chào anh/chị! Em rất vui được tư vấn về laptop Dell cho anh/chị. 

Em đã tìm thấy một số laptop Dell phù hợp trong kho hàng:

• **Dell Alienware 16X Aurora AC2025**
  Thương hiệu: Dell
  Giá: 68,990,000 VND
  Cấu hình: Intel Core i7, 16GB RAM, 512GB SSD, RTX 4060
  Phù hợp: Gaming, đồ họa, hiệu năng cao
  Điểm nổi bật: Card đồ họa mạnh, màn hình 240Hz

• **Dell Inspiron 15 3520**
  Thương hiệu: Dell
  Giá: 15,900,000 VND
  Cấu hình: Intel Core i5, 8GB RAM, 256GB SSD
  Phù hợp: Văn phòng, học tập, công việc hàng ngày
  Điểm nổi bật: Giá tốt, hiệu năng ổn định

Anh/chị có thể xem chi tiết từng sản phẩm bên dưới hoặc cho em biết thêm về nhu cầu sử dụng để em tư vấn chính xác hơn ạ!'

VÍ DỤ 2 - Khách hỏi về MÁY RẺ:
Khách: 'Tôi muốn mua loại máy rẻ'
Bot: 'Chào anh/chị! Em hiểu anh/chị đang tìm laptop giá tốt. Em đã tìm thấy một số sản phẩm phù hợp với ngân sách:

• **Laptop A** - 12,500,000 VND
  Cấu hình: Intel Core i3, 8GB RAM, 256GB SSD
  Phù hợp: Học tập, văn phòng cơ bản
  Điểm nổi bật: Giá rẻ nhất, đủ dùng cho công việc hàng ngày

• **Laptop B** - 14,900,000 VND
  Cấu hình: Intel Core i5, 8GB RAM, 256GB SSD
  Phù hợp: Văn phòng, học tập
  Điểm nổi bật: CPU mạnh hơn, giá vẫn rất hợp lý

Anh/chị có thể xem chi tiết từng sản phẩm bên dưới. Nếu cần tư vấn thêm, em sẵn sàng hỗ trợ ạ!'

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
        // hasProducts = false nếu không có sản phẩm HOẶC brand không có sản phẩm
        // QUAN TRỌNG: Kiểm tra cả "CỬA HÀNG KHÔNG KINH DOANH" (có 1, 2, hoặc 3 dấu cảnh báo)
        var hasProducts = !productContext.Contains("Không tìm thấy") && 
                         !productContext.Contains("CỬA HÀNG KHÔNG KINH DOANH") &&
                         !productContext.Contains("KHÔNG CÓ trong kho hàng");
        var hasPolicies = !policyContext.Contains("Không tìm thấy");
        
        var prompt = $@"Câu hỏi của khách hàng: {userMessage}

📊 PHÂN TÍCH CÂU HỎI:
- Loại câu hỏi: {intent}
{(clarificationNeeded ? "- ⚠️ CẦN LÀM RÕ: Câu hỏi này cần được làm rõ thêm. Hãy đặt câu hỏi một cách tự nhiên để hiểu rõ nhu cầu của khách hàng (nhu cầu sử dụng, ngân sách, thương hiệu yêu thích)." : "- ✅ Câu hỏi đã đủ rõ ràng")}

📦 THÔNG TIN SẢN PHẨM CÓ SẴN:
{(productContext.Contains("CỬA HÀNG KHÔNG KINH DOANH") ? productContext : (hasProducts ? productContext : "⚠️ Không tìm thấy sản phẩm phù hợp trong kho hàng. Hãy hỏi khách hàng về nhu cầu cụ thể để tìm kiếm tốt hơn."))}

⚠️⚠️⚠️⚠️⚠️ CỰC KỲ QUAN TRỌNG - ĐỌC KỸ: Nếu trong 'THÔNG TIN SẢN PHẨM CÓ SẴN' có thông báo '⚠️⚠️⚠️ CỬA HÀNG KHÔNG KINH DOANH' hoặc 'CỬA HÀNG KHÔNG KINH DOANH', điều này có nghĩa là:
- ⚠️⚠️⚠️ ĐÂY KHÔNG PHẢI là trường hợp 'không tìm thấy sản phẩm phù hợp'
- ⚠️⚠️⚠️ ĐÂY LÀ tình huống cửa hàng KHÔNG KINH DOANH brand đó (ví dụ: Acer, Apple, Samsung, MSI, Gigabyte)
- ⚠️⚠️⚠️ BẮT BUỘC phải trả lời NGAY, rõ ràng, lịch sự rằng cửa hàng không kinh doanh sản phẩm đó
- ⚠️⚠️⚠️ KHÔNG được bịa sản phẩm, KHÔNG được nói mơ hồ như 'có thể có' hoặc 'để em kiểm tra'
- ⚠️⚠️⚠️ KHÔNG được đề xuất sản phẩm từ brand không có trong kho
- ⚠️⚠️⚠️ KHÔNG được hiển thị thông tin chính sách bảo hành, bảo mật, hoặc bất kỳ thông tin nào khác
- ⚠️⚠️⚠️ CHỈ trả lời ngắn gọn rằng sản phẩm không được kinh doanh, theo ĐÚNG format trong context
- ⚠️⚠️⚠️ PHẢI trả lời theo ĐÚNG format trong context, KHÔNG tự ý thay đổi
- ⚠️⚠️⚠️ Ví dụ: Nếu khách hỏi 'máy Acer' và context có 'CỬA HÀNG KHÔNG KINH DOANH: Acer' → Trả lời: 'Em xin lỗi, hiện tại cửa hàng TenTech không kinh doanh laptop Acer ạ. Cửa hàng chúng em chuyên về các thương hiệu như Dell, Lenovo, HP, ASUS. Anh/chị có muốn em tư vấn về các sản phẩm tương tự từ các thương hiệu này không ạ?'
- ⚠️⚠️⚠️ LƯU Ý: Nếu bạn không trả lời đúng theo format trong context, bạn đang làm sai. Hãy đọc kỹ format trong context và trả lời ĐÚNG. KHÔNG hiển thị thông tin chính sách.

📋 THÔNG TIN VỀ CÁC THƯƠNG HIỆU CỬA HÀNG KINH DOANH:
Cửa hàng TenTech hiện đang kinh doanh các thương hiệu sau:
- **Dell**: Alienware, Inspiron, XPS
- **Lenovo**: ThinkPad, Legion, LOQ
- **HP**: Omen, Pavilion
- **ASUS**: ExpertBook, TUF Gaming, ROG

Nếu khách hỏi về thương hiệu khác (ví dụ: Apple, Samsung, Acer, MSI, Gigabyte), hãy trả lời rõ ràng rằng cửa hàng không kinh doanh thương hiệu đó.

📋 THÔNG TIN CHÍNH SÁCH:
{((productContext.Contains("CỬA HÀNG KHÔNG KINH DOANH") || string.IsNullOrEmpty(policyContext)) ? "⚠️ Không hiển thị thông tin chính sách khi sản phẩm không được kinh doanh." : (hasPolicies ? policyContext : "⚠️ Không tìm thấy thông tin chính sách liên quan."))}

🎯 HƯỚNG DẪN TRẢ LỜI:

{(intent == "product_search" ? @"- QUAN TRỌNG: Nếu có sản phẩm trong danh sách 'THÔNG TIN SẢN PHẨM CÓ SẴN':
  + LUÔN đề xuất NGAY các sản phẩm đó (2-10 sản phẩm tùy theo yêu cầu)
  + KHÔNG hỏi lại nếu đã có sản phẩm trong danh sách
  + ⚠️⚠️⚠️ FORMAT HIỂN THỊ SẢN PHẨM (BẮT BUỘC): 
    → Hiển thị TÊN SẢN PHẨM KÈM MODEL (nếu có model trong context)
    → Ví dụ: Context có Dell Alienware và model 16X Aurora AC2025 thì hiển thị: **Dell Alienware 16X Aurora AC2025**
    → Sau đó hiển thị: Thương hiệu: Dell và Giá: 68,990,000 VND
    → KHÔNG được format đơn giản như • Dell Alienware - Giá: 68,990,000 VND
    → PHẢI hiển thị đầy đủ: tên + model, thương hiệu, giá
  + Highlight các sản phẩm phù hợp với yêu cầu cụ thể của khách hàng
  + Nếu khách hỏi chung chung (ví dụ: 'laptop', 'máy tính', 'máy', 'PC', 'notebook'):
    → Đây là các từ khóa đồng nghĩa, đều có nghĩa là sản phẩm laptop
    → Đề xuất đa dạng sản phẩm (nhiều thương hiệu, nhiều phân khúc giá)
    → Giới thiệu 5-10 sản phẩm tốt nhất, đa dạng
    → Sau đó hỏi thêm về nhu cầu cụ thể
  + Nếu khách hỏi về thương hiệu (ví dụ: 'máy Dell') → chỉ đề xuất sản phẩm của thương hiệu đó
  + Nếu khách hỏi về MODEL/SERIES CỤ THỂ (ví dụ: 'HP Omen', 'Dell XPS', 'ASUS ROG') → CHỈ đề xuất sản phẩm có tên/model chứa đúng model/series đó, KHÔNG đề xuất các model khác của cùng thương hiệu
  + Nếu khách hỏi về giá rẻ → chỉ đề xuất sản phẩm giá rẻ, sắp xếp từ rẻ nhất
  + Nếu khách hỏi về mục đích sử dụng (gaming, văn phòng, đồ họa, học tập, lập trình):
    → Đề xuất sản phẩm phù hợp với mục đích đó
    → Giải thích tại sao sản phẩm phù hợp (ví dụ: 'Card RTX 3060 mạnh mẽ, phù hợp gaming')
    → Nếu sản phẩm không phù hợp 100% → vẫn đề xuất và giải thích điểm khác biệt
  + Giải thích lý do tại sao sản phẩm phù hợp, so sánh điểm mạnh/yếu
  + Đề cập giá cả, cấu hình, và điểm nổi bật
- Nếu không có sản phẩm: Hỏi rõ nhu cầu (mục đích sử dụng, ngân sách) để tìm kiếm tốt hơn
- ⚠️⚠️⚠️ QUAN TRỌNG CỰC KỲ: Nếu có thông báo '⚠️ CỬA HÀNG KHÔNG KINH DOANH' trong 'THÔNG TIN SẢN PHẨM CÓ SẴN':
  → Đây là tình huống cửa hàng KHÔNG KINH DOANH brand/sản phẩm đó (ví dụ: Acer, Apple, Samsung)
  → BẮT BUỘC trả lời rõ ràng, lịch sự rằng cửa hàng không kinh doanh
  → KHÔNG được bịa sản phẩm, KHÔNG được nói mơ hồ, KHÔNG được đề xuất sản phẩm từ brand không có
  → Đề xuất các brand có sẵn (Dell, Lenovo, HP, ASUS)
  → Trả lời theo ĐÚNG format trong context, KHÔNG tự ý thay đổi
  → Ví dụ: Khách hỏi 'máy Acer' → Trả lời: 'Em xin lỗi, hiện tại cửa hàng TenTech không kinh doanh laptop Acer ạ. Cửa hàng chúng em chuyên về các thương hiệu như Dell, Lenovo, HP, ASUS. Anh/chị có muốn em tư vấn về các sản phẩm tương tự từ các thương hiệu này không ạ?'
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

{(intent == "use_case_gaming" ? @"- QUAN TRỌNG: Khi khách hỏi về laptop cho GAMING:
  + LUÔN đề xuất sản phẩm từ danh sách 'THÔNG TIN SẢN PHẨM CÓ SẴN' - KHÔNG bịa sản phẩm
  + Nếu có sản phẩm trong danh sách → Đề xuất NGAY các sản phẩm phù hợp gaming (hoặc gần nhất)
  + Highlight các đặc điểm quan trọng cho gaming:
    • Card đồ họa rời (RTX, GTX) - QUAN TRỌNG cho gaming
    • CPU mạnh (i7, i9, Ryzen 7, Ryzen 9) - Xử lý game tốt
    • RAM lớn (16GB+) - Chạy game mượt mà
    • Màn hình tốt (144Hz, 240Hz) nếu có thông tin
  + Giải thích tại sao sản phẩm phù hợp gaming (ví dụ: 'Card RTX 3060 mạnh mẽ, chơi game AAA mượt mà')
  + Nếu sản phẩm không có card rời nhưng có CPU mạnh → giải thích: 'Mặc dù không có card rời, nhưng CPU mạnh vẫn có thể chơi được nhiều game ở mức trung bình'
  + So sánh các sản phẩm gaming với nhau
  + Đề cập đến giá cả và giá trị nhận được
  + Nếu không có sản phẩm gaming lý tưởng → vẫn đề xuất sản phẩm gần nhất và giải thích điểm khác biệt" : "")}

{(intent == "use_case_office" ? @"- QUAN TRỌNG: Khi khách hỏi về laptop cho VĂN PHÒNG:
  + LUÔN đề xuất sản phẩm từ danh sách 'THÔNG TIN SẢN PHẨM CÓ SẴN' - KHÔNG bịa sản phẩm
  + Nếu có sản phẩm trong danh sách → Đề xuất NGAY các sản phẩm phù hợp văn phòng (hoặc gần nhất)
  + Highlight các đặc điểm quan trọng cho văn phòng:
    • CPU ổn định (i3, i5, i7, Ryzen 3, Ryzen 5, Ryzen 7) - Đủ mạnh cho công việc
    • RAM 4GB trở lên (8GB+ tốt hơn) - Đa nhiệm tốt
    • Pin tốt, nhẹ - Dễ mang theo
    • Giá hợp lý - Phù hợp ngân sách văn phòng
  + Giải thích tại sao sản phẩm phù hợp văn phòng (ví dụ: 'CPU i5 đủ mạnh cho Word, Excel, trình duyệt')
  + So sánh các sản phẩm văn phòng với nhau
  + Đề cập đến giá cả và giá trị nhận được
  + Nếu sản phẩm có cấu hình cao hơn cần thiết → giải thích: 'Cấu hình này mạnh hơn cần thiết cho văn phòng, nhưng sẽ dùng mượt mà và tương lai không cần nâng cấp'
  + Nếu không có sản phẩm phù hợp 100% → vẫn đề xuất sản phẩm gần nhất và giải thích" : "")}

{(intent == "use_case_design" ? @"- QUAN TRỌNG: Khi khách hỏi về laptop cho ĐỒ HỌA:
  + Nếu có sản phẩm trong danh sách → Đề xuất NGAY các sản phẩm phù hợp đồ họa
  + Highlight các đặc điểm quan trọng cho đồ họa:
    • CPU mạnh (i7, i9, Ryzen 7, Ryzen 9) - Render nhanh
    • RAM lớn (16GB+) - Xử lý file lớn
    • Card đồ họa tốt (RTX, GTX) - Render, chỉnh sửa video
    • Màn hình đẹp (4K, QHD, OLED) nếu có thông tin
  + Giải thích tại sao sản phẩm phù hợp đồ họa
  + So sánh các sản phẩm đồ họa với nhau" : "")}

{(intent == "use_case_student" ? @"- QUAN TRỌNG: Khi khách hỏi về laptop cho HỌC TẬP:
  + Nếu có sản phẩm trong danh sách → Đề xuất NGAY các sản phẩm phù hợp học tập
  + Highlight các đặc điểm quan trọng cho học tập:
    • Giá rẻ (dưới 20 triệu) - Phù hợp ngân sách học sinh/sinh viên
    • CPU ổn định (i3, i5, Ryzen 3, Ryzen 5) - Đủ dùng cho học tập
    • RAM 8GB - Đủ cho học tập, xem video, làm bài tập
    • Pin tốt - Dùng cả ngày ở trường
  + Giải thích tại sao sản phẩm phù hợp học tập
  + So sánh các sản phẩm học tập với nhau" : "")}

{(intent == "use_case_programming" ? @"- QUAN TRỌNG: Khi khách hỏi về laptop cho LẬP TRÌNH:
  + Nếu có sản phẩm trong danh sách → Đề xuất NGAY các sản phẩm phù hợp lập trình
  + Highlight các đặc điểm quan trọng cho lập trình:
    • CPU mạnh (i5, i7, Ryzen 5, Ryzen 7) - Compile code nhanh
    • RAM lớn (16GB+) - Chạy nhiều IDE, Docker, VM
    • Ổ cứng SSD - Khởi động nhanh, compile nhanh
  + Giải thích tại sao sản phẩm phù hợp lập trình
  + So sánh các sản phẩm lập trình với nhau" : "")}

{(intent == "spec_inquiry" ? @"- QUAN TRỌNG: Khi khách hỏi về cấu hình (CPU, RAM, ROM, Card):
  + Nếu có sản phẩm trong danh sách → LIỆT KÊ CHI TIẾT cấu hình của từng sản phẩm
  + Giải thích ý nghĩa của từng thông số (ví dụ: 'Intel Core i5 phù hợp văn phòng', '16GB RAM đủ cho đa nhiệm')
  + So sánh cấu hình giữa các sản phẩm nếu có nhiều sản phẩm
  + Đề xuất sản phẩm phù hợp dựa trên cấu hình khách hàng yêu cầu
  + Nếu khách hỏi 'laptop có CPU i7' → chỉ đề xuất sản phẩm có CPU i7
  + Nếu khách hỏi 'laptop có RAM 16GB' → chỉ đề xuất sản phẩm có RAM 16GB
  + Nếu khách hỏi 'laptop có card rời' → chỉ đề xuất sản phẩm có card đồ họa rời (RTX, GTX)
  + LUÔN trả lời chi tiết, không chỉ nói chung chung" : "")}

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
        
        // Detect câu hỏi về use case (gaming, văn phòng, đồ họa) - ƯU TIÊN TRƯỚC
        if (messageLower.Contains("gaming") || messageLower.Contains("game") || 
            messageLower.Contains("chơi game") || messageLower.Contains("choi game") ||
            messageLower.Contains("chơi") || messageLower.Contains("choi"))
        {
            return "use_case_gaming";
        }
        
        if (messageLower.Contains("văn phòng") || messageLower.Contains("van phong") ||
            messageLower.Contains("office") || messageLower.Contains("công việc") ||
            messageLower.Contains("cong viec") || messageLower.Contains("làm việc") ||
            messageLower.Contains("lam viec") || messageLower.Contains("công việc văn phòng"))
        {
            return "use_case_office";
        }
        
        if (messageLower.Contains("đồ họa") || messageLower.Contains("do hoa") ||
            messageLower.Contains("design") || messageLower.Contains("thiết kế") ||
            messageLower.Contains("thiet ke") || messageLower.Contains("render") ||
            messageLower.Contains("video") || messageLower.Contains("editing"))
        {
            return "use_case_design";
        }
        
        if (messageLower.Contains("học tập") || messageLower.Contains("hoc tap") ||
            messageLower.Contains("student") || messageLower.Contains("sinh viên") ||
            messageLower.Contains("sinh vien") || messageLower.Contains("học sinh") ||
            messageLower.Contains("hoc sinh"))
        {
            return "use_case_student";
        }
        
        if (messageLower.Contains("lập trình") || messageLower.Contains("lap trinh") ||
            messageLower.Contains("programming") || messageLower.Contains("code") ||
            messageLower.Contains("developer") || messageLower.Contains("dev"))
        {
            return "use_case_programming";
        }
        
        // Detect câu hỏi về cấu hình (CPU, RAM, ROM, Card)
        if (messageLower.Contains("cpu") || messageLower.Contains("processor") || 
            messageLower.Contains("intel") || messageLower.Contains("amd") ||
            messageLower.Contains("core i") || messageLower.Contains("ryzen") ||
            messageLower.Contains("ram") || messageLower.Contains("bộ nhớ") ||
            messageLower.Contains("rom") || messageLower.Contains("ổ cứng") ||
            messageLower.Contains("ssd") || messageLower.Contains("hdd") ||
            messageLower.Contains("card") || messageLower.Contains("vga") ||
            messageLower.Contains("rtx") || messageLower.Contains("gtx") ||
            messageLower.Contains("cấu hình") || messageLower.Contains("cau hinh") ||
            messageLower.Contains("thông số") || messageLower.Contains("thong so") ||
            messageLower.Contains("spec") || messageLower.Contains("config"))
        {
            return "spec_inquiry";
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
    /// Detect use case từ message để optimize context building
    /// </summary>
    private string? DetectUseCaseFromMessage(string message)
    {
        var messageLower = message.ToLower();
        
        if (messageLower.Contains("gaming") || messageLower.Contains("game") || 
            messageLower.Contains("chơi game") || messageLower.Contains("choi game"))
            return "gaming";
        
        if (messageLower.Contains("văn phòng") || messageLower.Contains("van phong") ||
            messageLower.Contains("office") || messageLower.Contains("công việc") ||
            messageLower.Contains("cong viec") || messageLower.Contains("làm việc") ||
            messageLower.Contains("lam viec"))
            return "office";
        
        if (messageLower.Contains("đồ họa") || messageLower.Contains("do hoa") ||
            messageLower.Contains("design") || messageLower.Contains("thiết kế") ||
            messageLower.Contains("thiet ke"))
            return "design";
        
        if (messageLower.Contains("học tập") || messageLower.Contains("hoc tap") ||
            messageLower.Contains("student") || messageLower.Contains("sinh viên") ||
            messageLower.Contains("sinh vien"))
            return "student";
        
        if (messageLower.Contains("lập trình") || messageLower.Contains("lap trinh") ||
            messageLower.Contains("programming") || messageLower.Contains("code"))
            return "programming";
        
        return null;
    }
    
    /// <summary>
    /// Build product context từ search results - Format đẹp và đầy đủ thông tin
    /// </summary>
    private string BuildProductContext(List<VectorSearchResult> results, string? useCase = null, string? unavailableBrand = null)
    {
        // Nếu có brand không có sản phẩm, thông báo rõ ràng (QUAN TRỌNG: ưu tiên cao nhất)
        if (!string.IsNullOrEmpty(unavailableBrand))
        {
            // Chuẩn hóa tên brand (viết hoa chữ cái đầu)
            var brandDisplayName = unavailableBrand;
            if (!string.IsNullOrEmpty(brandDisplayName))
            {
                brandDisplayName = char.ToUpper(brandDisplayName[0]) + brandDisplayName.Substring(1).ToLower();
            }
            
            return $"⚠️⚠️⚠️ CỬA HÀNG KHÔNG KINH DOANH: Thương hiệu '{brandDisplayName}' hiện KHÔNG CÓ trong kho hàng của cửa hàng TenTech.\n\n" +
                   $"ĐÂY KHÔNG PHẢI là trường hợp 'không tìm thấy sản phẩm phù hợp', mà là cửa hàng KHÔNG KINH DOANH brand {brandDisplayName}.\n\n" +
                   $"BẮT BUỘC: Bạn PHẢI trả lời rõ ràng, lịch sự rằng cửa hàng không kinh doanh sản phẩm {brandDisplayName}.\n\n" +
                   $"KHÔNG được bịa sản phẩm, KHÔNG được nói mơ hồ như 'có thể có' hoặc 'để em kiểm tra'.\n\n" +
                   $"KHÔNG được đề xuất hoặc gợi ý BẤT KỲ sản phẩm nào (kể cả từ các brand khác như Dell, Lenovo, HP, ASUS).\n\n" +
                   $"KHÔNG được hiển thị thông tin chính sách bảo hành, bảo mật, hoặc bất kỳ thông tin nào khác.\n\n" +
                   $"BẮT BUỘC trả lời theo format sau (SAO CHÉP NGUYÊN VĂN, KHÔNG thay đổi):\n\n" +
                   $"'Em xin lỗi, hiện tại cửa hàng TenTech không kinh doanh laptop {brandDisplayName} ạ.'\n\n" +
                   $"LƯU Ý CỰC KỲ QUAN TRỌNG:\n" +
                   $"- Đây là tình huống cửa hàng KHÔNG KINH DOANH brand này\n" +
                   $"- KHÔNG phải là không tìm thấy sản phẩm phù hợp\n" +
                   $"- Bạn PHẢI trả lời rõ ràng rằng cửa hàng không kinh doanh\n" +
                   $"- KHÔNG được nói mơ hồ, KHÔNG được bịa sản phẩm\n" +
                   $"- KHÔNG được gợi ý hoặc đề xuất bất kỳ sản phẩm nào\n" +
                   $"- KHÔNG được hiển thị thông tin chính sách\n" +
                   $"- Trả lời theo ĐÚNG format trên, KHÔNG tự ý thay đổi";
        }
        
        if (results == null || results.Count == 0)
        {
            return "Không tìm thấy sản phẩm phù hợp trong kho hàng hiện tại.";
        }

        var context = new System.Text.StringBuilder();
        
        // Thêm thông tin về use case nếu có
        if (!string.IsNullOrEmpty(useCase))
        {
            var useCaseText = useCase switch
            {
                "gaming" => "GAMING",
                "office" => "VĂN PHÒNG",
                "design" => "ĐỒ HỌA",
                "student" => "HỌC TẬP",
                "programming" => "LẬP TRÌNH",
                _ => useCase.ToUpper()
            };
            context.AppendLine($"🎯 Tìm thấy {results.Count} sản phẩm phù hợp cho {useCaseText}:\n");
        }
        else
        {
            context.AppendLine($"Tìm thấy {results.Count} sản phẩm liên quan:\n");
        }

        int index = 1;
        foreach (var result in results)
        {
            if (result.Metadata != null)
            {
                var name = result.Metadata.GetValueOrDefault("name", "N/A")?.ToString() ?? "N/A";
                var brand = result.Metadata.GetValueOrDefault("brand", "")?.ToString() ?? "";
                var model = result.Metadata.GetValueOrDefault("model", "")?.ToString() ?? "";
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
                
                // Format tên sản phẩm: nếu có model thì ghép với name
                var displayName = name;
                if (!string.IsNullOrEmpty(model))
                {
                    displayName = $"{name} {model}";
                }
                
                context.AppendLine($"{index}. **{displayName}**");
                
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
                
                // Cấu hình chi tiết - LUÔN hiển thị đầy đủ
                context.AppendLine($"   Cấu hình chi tiết:");
                    if (!string.IsNullOrEmpty(cpu))
                    context.AppendLine($"     • CPU: {cpu} {GetCpuDescription(cpu)}");
                else
                    context.AppendLine($"     • CPU: (Chưa có thông tin)");
                    
                    if (!string.IsNullOrEmpty(ram))
                    context.AppendLine($"     • RAM: {ram} {GetRamDescription(ram)}");
                else
                    context.AppendLine($"     • RAM: (Chưa có thông tin)");
                    
                    if (!string.IsNullOrEmpty(rom))
                    context.AppendLine($"     • Ổ cứng: {rom} {GetStorageDescription(rom)}");
                else
                    context.AppendLine($"     • Ổ cứng: (Chưa có thông tin)");
                    
                    if (!string.IsNullOrEmpty(card))
                    context.AppendLine($"     • Card đồ họa: {card} {GetCardDescription(card)}");
                else
                    context.AppendLine($"     • Card đồ họa: Tích hợp (phù hợp văn phòng, học tập)");
                
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
            if (!string.IsNullOrEmpty(useCase))
            {
                var useCaseText = useCase switch
                {
                    "gaming" => "gaming",
                    "office" => "văn phòng",
                    "design" => "đồ họa",
                    "student" => "học tập",
                    "programming" => "lập trình",
                    _ => useCase
                };
                context.AppendLine($"💡 Gợi ý: Có thể so sánh các sản phẩm trên về giá cả, cấu hình, và mức độ phù hợp cho {useCaseText}.");
            }
            else
            {
                context.AppendLine("💡 Gợi ý: Có thể so sánh các sản phẩm trên về giá cả, cấu hình, và phù hợp với nhu cầu sử dụng.");
            }
        }

        return context.ToString();
    }
    
    /// <summary>
    /// Mô tả CPU để AI hiểu rõ hơn
    /// </summary>
    private string GetCpuDescription(string? cpu)
    {
        if (string.IsNullOrEmpty(cpu)) return "";
        
        var cpuLower = cpu.ToLower();
        if (cpuLower.Contains("i3") || cpuLower.Contains("core i3"))
            return "(phù hợp văn phòng, học tập cơ bản)";
        else if (cpuLower.Contains("i5") || cpuLower.Contains("core i5"))
            return "(phù hợp văn phòng, học tập, đa nhiệm tốt)";
        else if (cpuLower.Contains("i7") || cpuLower.Contains("core i7"))
            return "(mạnh mẽ, phù hợp gaming, đồ họa, lập trình)";
        else if (cpuLower.Contains("i9") || cpuLower.Contains("core i9"))
            return "(flagship, hiệu năng tối đa, phù hợp công việc chuyên nghiệp)";
        else if (cpuLower.Contains("ryzen 3"))
            return "(phù hợp văn phòng, học tập)";
        else if (cpuLower.Contains("ryzen 5"))
            return "(cân bằng hiệu năng và giá, phù hợp đa mục đích)";
        else if (cpuLower.Contains("ryzen 7"))
            return "(mạnh mẽ, phù hợp gaming, đồ họa)";
        else if (cpuLower.Contains("ryzen 9"))
            return "(flagship AMD, hiệu năng tối đa)";
        
        return "";
    }
    
    /// <summary>
    /// Mô tả RAM để AI hiểu rõ hơn
    /// </summary>
    private string GetRamDescription(string? ram)
    {
        if (string.IsNullOrEmpty(ram)) return "";
        
        var ramLower = ram.ToLower();
        if (ramLower.Contains("4gb") || ramLower.Contains("4 gb"))
            return "(đủ dùng cho công việc cơ bản)";
        else if (ramLower.Contains("8gb") || ramLower.Contains("8 gb"))
            return "(phù hợp văn phòng, học tập, đa nhiệm tốt)";
        else if (ramLower.Contains("16gb") || ramLower.Contains("16 gb"))
            return "(tốt cho gaming, đồ họa, lập trình, đa nhiệm mạnh)";
        else if (ramLower.Contains("32gb") || ramLower.Contains("32 gb"))
            return "(rất mạnh, phù hợp công việc chuyên nghiệp, render video)";
        
        return "";
    }
    
    /// <summary>
    /// Mô tả Storage để AI hiểu rõ hơn
    /// </summary>
    private string GetStorageDescription(string? rom)
    {
        if (string.IsNullOrEmpty(rom)) return "";
        
        var romLower = rom.ToLower();
        if (romLower.Contains("128gb"))
            return "(hạn chế, chỉ đủ cho hệ điều hành và vài ứng dụng)";
        else if (romLower.Contains("256gb"))
            return "(đủ dùng cho văn phòng, học tập)";
        else if (romLower.Contains("512gb"))
            return "(tốt, đủ cho hầu hết nhu cầu)";
        else if (romLower.Contains("1tb") || romLower.Contains("1024gb"))
            return "(rộng rãi, phù hợp lưu trữ nhiều dữ liệu)";
        
        if (romLower.Contains("ssd"))
            return "(tốc độ nhanh, khởi động nhanh)";
        else if (romLower.Contains("hdd"))
            return "(dung lượng lớn, giá rẻ, tốc độ chậm hơn SSD)";
        
        return "";
    }
    
    /// <summary>
    /// Mô tả Card đồ họa để AI hiểu rõ hơn
    /// </summary>
    private string GetCardDescription(string? card)
    {
        if (string.IsNullOrEmpty(card)) return "";
        
        var cardLower = card.ToLower();
        if (cardLower.Contains("rtx"))
            return "(card rời NVIDIA, mạnh mẽ, phù hợp gaming, đồ họa, AI)";
        else if (cardLower.Contains("gtx"))
            return "(card rời NVIDIA, phù hợp gaming, đồ họa)";
        else if (cardLower.Contains("radeon") || cardLower.Contains("amd"))
            return "(card rời AMD, phù hợp gaming, đồ họa)";
        else if (cardLower.Contains("rời") || cardLower.Contains("roi"))
            return "(card đồ họa rời, hiệu năng cao hơn card tích hợp)";
        else if (cardLower.Contains("tích hợp") || cardLower.Contains("integrated"))
            return "(card tích hợp, phù hợp văn phòng, học tập)";
        
        return "";
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
    /// Cải thiện để xử lý tốt hơn các câu hỏi về use case (gaming, văn phòng)
    /// </summary>
    private async Task<List<VectorSearchResult>> SearchProductsWithFallbackAsync(string userMessage)
    {
        bool qdrantSearchFailed = false;
        List<VectorSearchResult> productResults = new List<VectorSearchResult>();

        // Parse use case sớm để quyết định strategy
        var searchTerm = userMessage.ToLower();
        bool hasUseCase = searchTerm.Contains("gaming") || searchTerm.Contains("game") || 
                         searchTerm.Contains("chơi game") || searchTerm.Contains("choi game") ||
                         searchTerm.Contains("văn phòng") || searchTerm.Contains("van phong") ||
                         searchTerm.Contains("office") || searchTerm.Contains("công việc") ||
                         searchTerm.Contains("cong viec") || searchTerm.Contains("làm việc") ||
                         searchTerm.Contains("lam viec") || searchTerm.Contains("đồ họa") ||
                         searchTerm.Contains("do hoa") || searchTerm.Contains("học tập") ||
                         searchTerm.Contains("hoc tap") || searchTerm.Contains("lập trình") ||
                         searchTerm.Contains("lap trinh");
        
        // Nếu có use case rõ ràng, ưu tiên search từ SQL với criteria cụ thể
        // Vì vector search có thể không match tốt với use case
        if (hasUseCase)
        {
            _logger.LogInformation("Detected use case in message, prioritizing SQL search with criteria");
            try
            {
                var sqlProducts = await FallbackSearchFromSqlAsync(userMessage);
                if (sqlProducts != null && sqlProducts.Count > 0)
                {
                    // Convert ProductDTO to VectorSearchResult format với metadata đầy đủ
                    productResults = sqlProducts.Select(p => 
                    {
                        var firstConfig = p.Configurations?.FirstOrDefault();
                        return new VectorSearchResult
                        {
                            Content = $"{p.ProductName} - {p.SellingPrice:N0} VND",
                            Score = 0.9f, // Higher score vì match use case
                            Metadata = new Dictionary<string, object>
                            {
                                ["productId"] = p.ProductId ?? "",
                                ["name"] = p.ProductName ?? "",
                                ["model"] = p.ProductModel ?? "",
                                ["price"] = p.SellingPrice ?? 0,
                                ["brand"] = p.BrandName ?? "",
                                ["cpu"] = firstConfig?.Cpu ?? "",
                                ["ram"] = firstConfig?.Ram ?? "",
                                ["rom"] = firstConfig?.Rom ?? "",
                                ["card"] = firstConfig?.Card ?? "",
                                ["warrantyPeriod"] = p.WarrantyPeriod ?? 0,
                                ["description"] = $"Laptop {p.ProductName} với giá {p.SellingPrice:N0} VND"
                            }
                        };
                    }).ToList();
                    _logger.LogInformation("SQL search with use case found {Count} products", productResults.Count);
                    return productResults; // Return ngay, không cần Qdrant
                }
                else
                {
                    _logger.LogWarning("SQL search with use case returned no products, will try Qdrant");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in SQL search with use case, will try Qdrant: {Error}", ex.Message);
            }
        }

        // Thử search từ Qdrant (nếu chưa có kết quả từ SQL)
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
        if ((qdrantSearchFailed || productResults.Count == 0) && !hasUseCase)
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
                            ["model"] = p.ProductModel ?? "",
                            ["price"] = p.SellingPrice ?? 0,
                            ["brand"] = p.BrandName ?? "",
                            ["cpu"] = p.Configurations?.FirstOrDefault()?.Cpu ?? "",
                            ["ram"] = p.Configurations?.FirstOrDefault()?.Ram ?? "",
                            ["rom"] = p.Configurations?.FirstOrDefault()?.Rom ?? "",
                            ["card"] = p.Configurations?.FirstOrDefault()?.Card ?? "",
                            ["warrantyPeriod"] = p.WarrantyPeriod ?? 0,
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
    /// Cải thiện để parse tốt hơn các yêu cầu như "máy rẻ", "máy Dell"
    /// Normalize các từ khóa sản phẩm (laptop, máy tính, máy, PC, notebook)
    /// </summary>
    private async Task<List<ProductDTO>?> FallbackSearchFromSqlAsync(string userMessage)
    {
        try
        {
            var searchTerm = userMessage.ToLower();
            var criteria = new ProductSearchCriteria();
            bool isCheapRequest = false;
            bool sortByPriceAscending = false;
            
            // Normalize các từ khóa sản phẩm - loại bỏ các từ chung chung
            // Các từ này đều có nghĩa là "sản phẩm" nên không cần search theo chúng
            var productKeywords = new[] { 
                "laptop", "máy tính", "may tinh", "máy", "may", 
                "pc", "notebook", "sản phẩm", "san pham", 
                "máy tính xách tay", "may tinh xach tay", "mtxt",
                "computer", "máy vi tính", "may vi tinh"
            };
            
            // Loại bỏ các từ khóa sản phẩm chung chung khỏi searchTerm
            var normalizedSearchTerm = searchTerm;
            foreach (var keyword in productKeywords)
            {
                normalizedSearchTerm = normalizedSearchTerm.Replace(keyword, " ").Trim();
            }
            normalizedSearchTerm = System.Text.RegularExpressions.Regex.Replace(normalizedSearchTerm, @"\s+", " ").Trim();
            
            // Nếu sau khi normalize chỉ còn các từ chung chung hoặc rỗng
            // → Đây là câu hỏi chung về sản phẩm, không cần filter
            bool isGeneralProductQuery = string.IsNullOrWhiteSpace(normalizedSearchTerm) || 
                                        normalizedSearchTerm.Split(' ').Length <= 1;
            
            _logger.LogInformation("Original search term: '{Original}', Normalized: '{Normalized}', IsGeneral: {IsGeneral}", 
                userMessage, normalizedSearchTerm, isGeneralProductQuery);
            
            // 1. Parse "máy rẻ", "rẻ", "giá rẻ", "rẻ tiền" → tìm sản phẩm giá thấp
            if (searchTerm.Contains("rẻ") || searchTerm.Contains("re") || 
                searchTerm.Contains("giá rẻ") || searchTerm.Contains("gia re") ||
                searchTerm.Contains("rẻ tiền") || searchTerm.Contains("re tien") ||
                searchTerm.Contains("giá thấp") || searchTerm.Contains("gia thap"))
            {
                isCheapRequest = true;
                sortByPriceAscending = true;
                // Giới hạn giá tối đa 15 triệu cho "máy rẻ"
                criteria.MaxPrice = 15000000;
                _logger.LogInformation("Detected 'cheap laptop' request, setting maxPrice = 15,000,000");
            }
            
            // 2. Parse price range
            decimal? minPrice = null;
            decimal? maxPrice = null;
            
            // Extract "dưới X triệu" -> maxPrice
            var underMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"dưới\s*(\d+)\s*triệu");
            if (underMatch.Success && decimal.TryParse(underMatch.Groups[1].Value, out var underValue))
            {
                maxPrice = underValue * 1000000;
                criteria.MaxPrice = maxPrice;
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
                    criteria.MinPrice = minPrice;
                    criteria.MaxPrice = maxPrice;
                }
            }
            
            // Extract "khoảng X triệu" -> ±20% range
            var aroundMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"khoảng\s*(\d+)\s*triệu");
            if (aroundMatch.Success && decimal.TryParse(aroundMatch.Groups[1].Value, out var aroundValue))
            {
                var targetPrice = aroundValue * 1000000;
                criteria.MinPrice = targetPrice * 0.8m; // -20%
                criteria.MaxPrice = targetPrice * 1.2m; // +20%
            }
            
            // Extract "trên X triệu" hoặc "từ X triệu trở lên" -> minPrice
            var aboveMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"(trên|từ)\s*(\d+)\s*triệu\s*(trở lên|trở lên)?");
            if (aboveMatch.Success && decimal.TryParse(aboveMatch.Groups[2].Value, out var aboveValue))
            {
                criteria.MinPrice = aboveValue * 1000000;
            }
            
            // Extract "trên X triệu" -> minPrice (pattern khác)
            var overMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"trên\s*(\d+)\s*triệu");
            if (overMatch.Success && !criteria.MinPrice.HasValue && 
                decimal.TryParse(overMatch.Groups[1].Value, out var overValue))
            {
                criteria.MinPrice = overValue * 1000000;
            }
            
            // 3. Parse cấu hình (CPU, RAM, ROM, Card) - Cải thiện để parse từ câu dài
            // Parse CPU - Ưu tiên model cụ thể trước
            if (searchTerm.Contains("i9") || searchTerm.Contains("core i9"))
                criteria.Cpu = "i9";
            else if (searchTerm.Contains("i7") || searchTerm.Contains("core i7"))
                criteria.Cpu = "i7";
            else if (searchTerm.Contains("i5") || searchTerm.Contains("core i5"))
                criteria.Cpu = "i5";
            else if (searchTerm.Contains("i3") || searchTerm.Contains("core i3"))
                criteria.Cpu = "i3";
            else if (searchTerm.Contains("ryzen 9"))
                criteria.Cpu = "Ryzen 9";
            else if (searchTerm.Contains("ryzen 7"))
                criteria.Cpu = "Ryzen 7";
            else if (searchTerm.Contains("ryzen 5"))
                criteria.Cpu = "Ryzen 5";
            else if (searchTerm.Contains("ryzen 3"))
                criteria.Cpu = "Ryzen 3";
            else if (searchTerm.Contains("cpu") || searchTerm.Contains("processor") || 
                     searchTerm.Contains("intel") || searchTerm.Contains("amd") ||
                     searchTerm.Contains("core i") || searchTerm.Contains("ryzen"))
            {
                // Nếu chỉ có "intel" hoặc "amd" mà không có model cụ thể
                if (searchTerm.Contains("intel") && !searchTerm.Contains("core i"))
                    criteria.Cpu = "Intel";
                else if (searchTerm.Contains("amd") && !searchTerm.Contains("ryzen"))
                    criteria.Cpu = "AMD";
            }
            
            if (!string.IsNullOrEmpty(criteria.Cpu))
                _logger.LogInformation("Detected CPU requirement: {Cpu}", criteria.Cpu);
            
            // Parse RAM - Cải thiện regex để parse tốt hơn từ câu dài
            // Ưu tiên parse số lớn trước (32GB > 16GB > 8GB)
            var ramPatterns = new[]
            {
                @"(\d+)\s*gb\s*ram|ram\s*(\d+)\s*gb|(\d+)\s*gb\s*bộ nhớ|bộ nhớ\s*(\d+)\s*gb",
                @"32\s*gb|32gb",
                @"16\s*gb|16gb",
                @"8\s*gb|8gb",
                @"4\s*gb|4gb"
            };
            
            bool ramFound = false;
            foreach (var pattern in ramPatterns)
            {
                var ramMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (ramMatch.Success)
                {
                    var ramValue = ramMatch.Groups[1].Value;
                    if (string.IsNullOrEmpty(ramValue))
                        ramValue = ramMatch.Groups[2].Value;
                    if (string.IsNullOrEmpty(ramValue))
                        ramValue = ramMatch.Groups[3].Value;
                    if (string.IsNullOrEmpty(ramValue))
                        ramValue = ramMatch.Groups[4].Value;
                    
                    // Nếu pattern là số cụ thể (32gb, 16gb, 8gb)
                    if (string.IsNullOrEmpty(ramValue) && pattern.Contains("32"))
                        ramValue = "32";
                    else if (string.IsNullOrEmpty(ramValue) && pattern.Contains("16"))
                        ramValue = "16";
                    else if (string.IsNullOrEmpty(ramValue) && pattern.Contains("8"))
                        ramValue = "8";
                    else if (string.IsNullOrEmpty(ramValue) && pattern.Contains("4"))
                        ramValue = "4";
                    
                    if (!string.IsNullOrEmpty(ramValue))
                    {
                        criteria.Ram = $"{ramValue}GB";
                        _logger.LogInformation("Detected RAM requirement: {Ram}", criteria.Ram);
                        ramFound = true;
                        break; // Dừng khi tìm thấy
                    }
                }
            }
            
            // Fallback: Tìm "ram" hoặc "bộ nhớ" trong câu
            if (!ramFound && (searchTerm.Contains("ram") || searchTerm.Contains("bộ nhớ") || 
                             searchTerm.Contains("bo nho") || searchTerm.Contains("memory")))
            {
                // Nếu có từ "ram" hoặc "bộ nhớ" nhưng không tìm thấy số → không set criteria.Ram
                // Để search rộng hơn
            }
            
            // Parse ROM/Storage
            if (searchTerm.Contains("rom") || searchTerm.Contains("ổ cứng") || 
                searchTerm.Contains("o cung") || searchTerm.Contains("ssd") || 
                searchTerm.Contains("hdd") || searchTerm.Contains("storage"))
            {
                // Extract storage size
                var storageMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, 
                    @"(\d+)\s*(gb|tb)\s*(ssd|hdd|rom|ổ cứng)|(ssd|hdd)\s*(\d+)\s*(gb|tb)");
                if (storageMatch.Success)
                {
                    var size = storageMatch.Groups[1].Value;
                    var unit = storageMatch.Groups[2].Value;
                    var type = storageMatch.Groups[3].Value;
                    
                    if (string.IsNullOrEmpty(size))
                    {
                        size = storageMatch.Groups[5].Value;
                        unit = storageMatch.Groups[6].Value;
                        type = storageMatch.Groups[4].Value;
                    }
                    
                    if (!string.IsNullOrEmpty(size) && !string.IsNullOrEmpty(unit))
                    {
                        criteria.Rom = $"{size}{unit.ToUpper()} {type.ToUpper()}";
                        _logger.LogInformation("Detected storage requirement: {Rom}", criteria.Rom);
                    }
                }
                else
                {
                    // Default storage keywords
                    if (searchTerm.Contains("256gb") || searchTerm.Contains("256 gb"))
                        criteria.Rom = "256GB SSD";
                    else if (searchTerm.Contains("512gb") || searchTerm.Contains("512 gb"))
                        criteria.Rom = "512GB SSD";
                    else if (searchTerm.Contains("1tb") || searchTerm.Contains("1 tb"))
                        criteria.Rom = "1TB";
                }
            }
            
            // Parse Card/GPU
            if (searchTerm.Contains("card") || searchTerm.Contains("vga") || 
                searchTerm.Contains("gpu") || searchTerm.Contains("đồ họa") ||
                searchTerm.Contains("do hoa") || searchTerm.Contains("graphics"))
            {
                // Extract GPU model
                if (searchTerm.Contains("rtx"))
                {
                    var rtxMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"rtx\s*(\d+)");
                    if (rtxMatch.Success)
                        criteria.Card = $"RTX {rtxMatch.Groups[1].Value}";
                    else
                        criteria.Card = "RTX";
                }
                else if (searchTerm.Contains("gtx"))
                {
                    var gtxMatch = System.Text.RegularExpressions.Regex.Match(searchTerm, @"gtx\s*(\d+)");
                    if (gtxMatch.Success)
                        criteria.Card = $"GTX {gtxMatch.Groups[1].Value}";
                    else
                        criteria.Card = "GTX";
                }
                else if (searchTerm.Contains("card rời") || searchTerm.Contains("card roi") ||
                         searchTerm.Contains("đồ họa rời") || searchTerm.Contains("do hoa roi"))
                {
                    criteria.Card = "rời"; // Tìm card rời (RTX, GTX, Radeon)
                }
                
                if (!string.IsNullOrEmpty(criteria.Card))
                    _logger.LogInformation("Detected GPU requirement: {Card}", criteria.Card);
            }
            
            // 4. Extract brand names và model/series names (QUAN TRỌNG: Ưu tiên model/series trước)
            string? brandId = null;
            string? modelSeries = null; // Lưu model/series name để search chính xác
            
            // Dictionary: brand -> [keywords, model/series names]
            // DỰA TRÊN DỮ LIỆU THỰC TẾ TỪ DATABASE (test.sql)
            // Brands có trong database: Dell (B001), Lenovo (B002), HP (B003), ASUS (B004)
            var brandKeywords = new Dictionary<string, (string[] Keywords, string[] ModelSeries)>
            {
                { "dell", (new[] { "dell" }, new[] { "alienware", "inspiron", "xps" }) },
                { "lenovo", (new[] { "lenovo" }, new[] { "thinkpad", "legion", "loq" }) },
                { "hp", (new[] { "hp", "hewlett packard" }, new[] { "omen", "pavilion" }) },
                { "asus", (new[] { "asus", "rog" }, new[] { "expertbook", "tuf gaming", "tuf", "rog" }) }
            };
            
            // BƯỚC 1: Tìm model/series name trước (ưu tiên cao nhất)
            foreach (var brandPair in brandKeywords)
            {
                var brandName = brandPair.Key;
                var keywords = brandPair.Value.Keywords;
                var modelSeriesList = brandPair.Value.ModelSeries;
                
                // Kiểm tra xem có model/series name trong câu hỏi không
                foreach (var model in modelSeriesList)
                {
                    if (searchTerm.Contains(model))
                    {
                        modelSeries = model;
                        _logger.LogInformation("Detected model/series: {ModelSeries} for brand: {BrandName}", modelSeries, brandName);
                        
                        // Tìm brandId
                        try
                        {
                            var dbContext = _serviceProvider.GetService<Data.WebLaptopTenTechContext>();
                            if (dbContext != null)
                            {
                                var brandEntity = await dbContext.Brands
                                    .FirstOrDefaultAsync(b => b.BrandName != null && 
                                        b.BrandName.ToLower().Contains(brandName));
                                if (brandEntity != null && brandEntity.BrandId != null)
                                {
                                    brandId = brandEntity.BrandId;
                                    criteria.BrandId = brandId;
                                    _logger.LogInformation("Found brand in database: {BrandName}, BrandId: {BrandId}", 
                                        brandEntity.BrandName, brandId);
                                    break; // Đã tìm thấy model và brand, dừng lại
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error querying brand: {BrandName}", brandName);
                        }
                        break; // Đã tìm thấy model, dừng lại
                    }
                }
                
                if (!string.IsNullOrEmpty(modelSeries))
                    break; // Đã tìm thấy model, không cần tìm tiếp
            }
            
            // BƯỚC 2: Nếu không tìm thấy model/series, tìm brand thông thường
            string? detectedBrandName = null; // Lưu tên brand được detect để kiểm tra sau
            if (string.IsNullOrEmpty(modelSeries))
            {
                foreach (var brandPair in brandKeywords)
                {
                    var brandName = brandPair.Key;
                    var keywords = brandPair.Value.Keywords;
                    
                    if (keywords.Any(keyword => searchTerm.Contains(keyword)))
                    {
                        detectedBrandName = brandName; // Lưu tên brand được detect
                        
                        // Query database để lấy BrandId thực tế
                        try
                        {
                            // Lấy DbContext từ service provider
                            var dbContext = _serviceProvider.GetService<Data.WebLaptopTenTechContext>();
                            if (dbContext != null)
                            {
                                var brandEntity = await dbContext.Brands
                                    .FirstOrDefaultAsync(b => b.BrandName != null && 
                                        b.BrandName.ToLower().Contains(brandName));
                                if (brandEntity != null && brandEntity.BrandId != null)
                                {
                                    brandId = brandEntity.BrandId;
                                    criteria.BrandId = brandId;
                                    _logger.LogInformation("Found brand in database: {BrandName}, BrandId: {BrandId}", 
                                        brandEntity.BrandName, brandId);
                                    break;
                                }
                                else
                                {
                                    // Brand không tồn tại trong database
                                    _logger.LogInformation("Brand '{BrandName}' not found in database", brandName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error querying brand: {BrandName}", brandName);
                        }
                    }
                }
            }
            
            // BƯỚC 3: Nếu có model/series, ưu tiên search theo model/series trong tên sản phẩm
            if (!string.IsNullOrEmpty(modelSeries))
            {
                // Set SearchTerm để tìm chính xác model/series trong ProductName hoặc ProductModel
                criteria.SearchTerm = modelSeries;
                _logger.LogInformation("Prioritizing search for model/series: {ModelSeries}", modelSeries);
            }
            
            // 4. Extract use case (gaming, văn phòng, đồ họa) để filter sản phẩm phù hợp
            // Use case sẽ được dùng để filter sau khi có kết quả
            string? useCase = null;
            if (searchTerm.Contains("gaming") || searchTerm.Contains("game") || 
                searchTerm.Contains("chơi game") || searchTerm.Contains("choi game") ||
                searchTerm.Contains("chơi") || searchTerm.Contains("choi"))
            {
                useCase = "gaming";
                // Gaming thường cần card rời, nếu chưa có thì set criteria
                if (string.IsNullOrEmpty(criteria.Card))
                {
                    // Không set criteria.Card = "rời" vì sẽ filter quá strict
                    // Thay vào đó sẽ filter sau khi có kết quả
                }
            }
            else if (searchTerm.Contains("văn phòng") || searchTerm.Contains("van phong") ||
                     searchTerm.Contains("office") || searchTerm.Contains("công việc") ||
                     searchTerm.Contains("cong viec") || searchTerm.Contains("làm việc") ||
                     searchTerm.Contains("lam viec") || searchTerm.Contains("công việc văn phòng") ||
                     searchTerm.Contains("cong viec van phong"))
            {
                useCase = "office";
            }
            else if (searchTerm.Contains("đồ họa") || searchTerm.Contains("do hoa") ||
                     searchTerm.Contains("design") || searchTerm.Contains("thiết kế") ||
                     searchTerm.Contains("thiet ke") || searchTerm.Contains("render") ||
                     searchTerm.Contains("video") || searchTerm.Contains("editing"))
            {
                useCase = "design";
            }
            else if (searchTerm.Contains("học tập") || searchTerm.Contains("hoc tap") ||
                     searchTerm.Contains("student") || searchTerm.Contains("sinh viên") ||
                     searchTerm.Contains("sinh vien") || searchTerm.Contains("học sinh") ||
                     searchTerm.Contains("hoc sinh"))
            {
                useCase = "student";
            }
            else if (searchTerm.Contains("lập trình") || searchTerm.Contains("lap trinh") ||
                     searchTerm.Contains("programming") || searchTerm.Contains("code") ||
                     searchTerm.Contains("developer") || searchTerm.Contains("dev"))
            {
                useCase = "programming";
            }
            
            if (!string.IsNullOrEmpty(useCase))
                _logger.LogInformation("Detected use case: {UseCase}", useCase);
            
            // 5. Log tất cả các tiêu chí đã parse được
            _logger.LogInformation("Parsed search criteria - BrandId: {BrandId}, CPU: {Cpu}, RAM: {Ram}, ROM: {Rom}, Card: {Card}, " +
                "MinPrice: {MinPrice}, MaxPrice: {MaxPrice}, UseCase: {UseCase}",
                criteria.BrandId, criteria.Cpu, criteria.Ram, criteria.Rom, criteria.Card,
                criteria.MinPrice, criteria.MaxPrice, useCase);
            
            // 6. Set SearchTerm
            // Nếu là câu hỏi chung về sản phẩm (chỉ có "laptop", "máy tính", v.v.) → không set SearchTerm
            // Nếu có từ khóa cụ thể → dùng normalizedSearchTerm
            if (!isGeneralProductQuery && !string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                // Chỉ set SearchTerm nếu không có brand, price, hoặc spec filters
                if (string.IsNullOrEmpty(criteria.BrandId) && 
                    !criteria.MinPrice.HasValue && !criteria.MaxPrice.HasValue &&
                    string.IsNullOrEmpty(criteria.Cpu) && string.IsNullOrEmpty(criteria.Ram) &&
                    string.IsNullOrEmpty(criteria.Rom) && string.IsNullOrEmpty(criteria.Card))
                {
                    criteria.SearchTerm = normalizedSearchTerm;
                }
            }
            // Nếu là câu hỏi chung và không có filters → không set SearchTerm để lấy tất cả sản phẩm

            // 7. Search products với tất cả các tiêu chí đã parse
            var products = await _productService.SearchProductsAsync(criteria);
            
            // 7.5. Nếu có model/series, ưu tiên sản phẩm có tên/model chứa đúng model/series
            if (!string.IsNullOrEmpty(modelSeries) && products.Any())
            {
                var modelSeriesLower = modelSeries.ToLower();
                var exactMatches = products.Where(p => 
                    (!string.IsNullOrEmpty(p.ProductName) && p.ProductName.ToLower().Contains(modelSeriesLower)) ||
                    (!string.IsNullOrEmpty(p.ProductModel) && p.ProductModel.ToLower().Contains(modelSeriesLower))
                ).ToList();
                
                if (exactMatches.Any())
                {
                    _logger.LogInformation("Found {Count} exact model/series matches for '{ModelSeries}', prioritizing them", 
                        exactMatches.Count, modelSeries);
                    products = exactMatches; // Chỉ giữ lại các sản phẩm đúng model/series
                }
                else
                {
                    _logger.LogWarning("No exact model/series matches found for '{ModelSeries}', using all {Count} products", 
                        modelSeries, products.Count);
                }
            }
            
            // 8. Nếu có use case nhưng không tìm được sản phẩm → search lại với criteria relaxed
            if (!string.IsNullOrEmpty(useCase) && products.Count == 0)
            {
                _logger.LogInformation("No products found with strict criteria for use case: {UseCase}, trying relaxed search", useCase);
                
                // Relax criteria: chỉ giữ brand, price, và modelSeries nếu có, bỏ các spec filters
                var relaxedCriteria = new ProductSearchCriteria
                {
                    BrandId = criteria.BrandId,
                    MinPrice = criteria.MinPrice,
                    MaxPrice = criteria.MaxPrice,
                    SearchTerm = criteria.SearchTerm // Giữ nguyên modelSeries nếu có
                };
                
                products = await _productService.SearchProductsAsync(relaxedCriteria);
                _logger.LogInformation("Relaxed search found {Count} products", products.Count);
            }
            
            // 9. Filter theo use case nếu có (sau khi search)
            // QUAN TRỌNG: Filter linh hoạt, không quá strict
            if (!string.IsNullOrEmpty(useCase) && products.Any())
            {
                var filteredProducts = new List<ProductDTO>();
                var allProducts = products.ToList(); // Backup để dùng nếu filter không có kết quả
                
                foreach (var product in products)
                {
                    bool matchesUseCase = false;
                    
                    switch (useCase)
                    {
                        case "gaming":
                            // Gaming: ưu tiên card rời (RTX, GTX), nhưng cũng chấp nhận CPU mạnh
                            var hasGamingCard = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Card) && 
                                (c.Card.Contains("RTX") || c.Card.Contains("GTX") || 
                                 c.Card.Contains("Radeon"))) ?? false;
                            var hasGamingCpu = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Cpu) && 
                                (c.Cpu.Contains("i7") || c.Cpu.Contains("i9") || 
                                 c.Cpu.Contains("Ryzen 7") || c.Cpu.Contains("Ryzen 9"))) ?? false;
                            // Relax: chấp nhận cả i5 nếu có RAM lớn
                            var hasGamingCpuRelaxed = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Cpu) && 
                                (c.Cpu.Contains("i5") || c.Cpu.Contains("Ryzen 5")) &&
                                !string.IsNullOrEmpty(c.Ram) && 
                                (c.Ram.Contains("16GB") || c.Ram.Contains("32GB"))) ?? false;
                            matchesUseCase = hasGamingCard || hasGamingCpu || hasGamingCpuRelaxed;
                            break;
                            
                        case "office":
                            // Văn phòng: CPU i3 trở lên, RAM 4GB trở lên (rất relax)
                            matchesUseCase = product.Configurations?.Any(c => 
                                (!string.IsNullOrEmpty(c.Cpu) && 
                                 (c.Cpu.Contains("i3") || c.Cpu.Contains("i5") || 
                                  c.Cpu.Contains("i7") || c.Cpu.Contains("Ryzen 3") || 
                                  c.Cpu.Contains("Ryzen 5") || c.Cpu.Contains("Ryzen 7"))) &&
                                (!string.IsNullOrEmpty(c.Ram) && 
                                 (c.Ram.Contains("4GB") || c.Ram.Contains("8GB") || 
                                  c.Ram.Contains("16GB") || c.Ram.Contains("32GB")))) ?? false;
                            // Nếu không match, vẫn chấp nhận nếu có CPU
                            if (!matchesUseCase)
                            {
                                matchesUseCase = product.Configurations?.Any(c => 
                                    !string.IsNullOrEmpty(c.Cpu) && 
                                    (c.Cpu.Contains("i3") || c.Cpu.Contains("i5") || 
                                     c.Cpu.Contains("i7") || c.Cpu.Contains("Ryzen"))) ?? false;
                            }
                            break;
                            
                        case "design":
                            // Đồ họa: ưu tiên RAM lớn (16GB+), nhưng cũng chấp nhận 8GB nếu CPU mạnh
                            var hasDesignRam = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Ram) && 
                                (c.Ram.Contains("16GB") || c.Ram.Contains("32GB"))) ?? false;
                            var hasDesignCpu = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Cpu) && 
                                (c.Cpu.Contains("i7") || c.Cpu.Contains("i9") || 
                                 c.Cpu.Contains("Ryzen 7") || c.Cpu.Contains("Ryzen 9"))) ?? false;
                            var hasDesignCpuWith8GB = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Ram) && c.Ram.Contains("8GB")) ?? false;
                            matchesUseCase = hasDesignRam || (hasDesignCpu && hasDesignCpuWith8GB);
                            // Relax: chấp nhận i5 với RAM 8GB+
                            if (!matchesUseCase)
                            {
                                matchesUseCase = product.Configurations?.Any(c => 
                                    !string.IsNullOrEmpty(c.Cpu) && 
                                    (c.Cpu.Contains("i5") || c.Cpu.Contains("Ryzen 5")) &&
                                    !string.IsNullOrEmpty(c.Ram) && 
                                    (c.Ram.Contains("8GB") || c.Ram.Contains("16GB"))) ?? false;
                            }
                            break;
                            
                        case "student":
                            // Học tập: giá rẻ (< 25 triệu), CPU i3-i5, RAM 4GB+ (relax)
                            var hasStudentConfig = product.Configurations?.Any(c => 
                                (!string.IsNullOrEmpty(c.Cpu) && 
                                 (c.Cpu.Contains("i3") || c.Cpu.Contains("i5") || 
                                  c.Cpu.Contains("Ryzen 3") || c.Cpu.Contains("Ryzen 5"))) &&
                                (!string.IsNullOrEmpty(c.Ram) && 
                                 (c.Ram.Contains("4GB") || c.Ram.Contains("8GB") || 
                                  c.Ram.Contains("16GB")))) ?? false;
                            matchesUseCase = (product.SellingPrice ?? 0) < 25000000 && hasStudentConfig;
                            // Relax: nếu giá < 30 triệu vẫn chấp nhận
                            if (!matchesUseCase && (product.SellingPrice ?? 0) < 30000000)
                            {
                                matchesUseCase = hasStudentConfig;
                            }
                            break;
                            
                        case "programming":
                            // Lập trình: ưu tiên RAM lớn (16GB+), nhưng cũng chấp nhận 8GB nếu CPU mạnh
                            var hasProgRam = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Ram) && 
                                (c.Ram.Contains("16GB") || c.Ram.Contains("32GB"))) ?? false;
                            var hasProgCpu = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Cpu) && 
                                (c.Cpu.Contains("i5") || c.Cpu.Contains("i7") || 
                                 c.Cpu.Contains("Ryzen 5") || c.Cpu.Contains("Ryzen 7"))) ?? false;
                            var hasProgCpuWith8GB = product.Configurations?.Any(c => 
                                !string.IsNullOrEmpty(c.Ram) && c.Ram.Contains("8GB")) ?? false;
                            matchesUseCase = hasProgRam || (hasProgCpu && hasProgCpuWith8GB);
                            // Relax: chấp nhận i3 với RAM 8GB
                            if (!matchesUseCase)
                            {
                                matchesUseCase = product.Configurations?.Any(c => 
                                    !string.IsNullOrEmpty(c.Cpu) && 
                                    (c.Cpu.Contains("i3") || c.Cpu.Contains("Ryzen 3")) &&
                                    !string.IsNullOrEmpty(c.Ram) && 
                                    (c.Ram.Contains("8GB") || c.Ram.Contains("16GB"))) ?? false;
                            }
                            break;
                    }
                    
                    if (matchesUseCase)
                    {
                        filteredProducts.Add(product);
                    }
                }
                
                // Nếu filter có kết quả → dùng filtered
                if (filteredProducts.Any())
                {
                    products = filteredProducts;
                    _logger.LogInformation("Filtered {Count} products by use case: {UseCase}", 
                        products.Count, useCase);
                }
                else
                {
                    // Không filter được → dùng tất cả products và log warning
                    // AI sẽ giải thích rằng sản phẩm có thể không phù hợp 100% nhưng vẫn có thể dùng
                    _logger.LogWarning("No products matched use case filter: {UseCase}, using all {Count} products. AI will explain suitability.", 
                        useCase, allProducts.Count);
                    products = allProducts; // Dùng tất cả để AI có thể giải thích
                }
            }
            
            // 6. Nếu không có kết quả và có use case → search lại với criteria rất relaxed
            if (products.Count == 0 && !string.IsNullOrEmpty(useCase))
            {
                _logger.LogInformation("No products found with criteria for use case: {UseCase}, trying very relaxed search", useCase);
                
                // Search với criteria rất relaxed: chỉ filter theo use case requirements
                // NHƯNG vẫn ưu tiên modelSeries nếu có
                var veryRelaxedCriteria = new ProductSearchCriteria
                {
                    SearchTerm = criteria.SearchTerm, // Giữ nguyên modelSeries nếu có
                    BrandId = criteria.BrandId // Giữ nguyên brand nếu có
                };
                
                // Set criteria cơ bản theo use case
                switch (useCase)
                {
                    case "gaming":
                        // Gaming: tìm card rời hoặc CPU mạnh
                        veryRelaxedCriteria.Card = "RTX"; // Tìm RTX, GTX
                        break;
                    case "office":
                        // Văn phòng: không cần filter gì, lấy tất cả
                        break;
                    case "design":
                        // Đồ họa: ưu tiên RAM lớn
                        veryRelaxedCriteria.Ram = "16GB";
                        break;
                    case "student":
                        // Học tập: giá rẻ
                        veryRelaxedCriteria.MaxPrice = 25000000;
                        break;
                    case "programming":
                        // Lập trình: RAM lớn
                        veryRelaxedCriteria.Ram = "16GB";
                        break;
                }
                
                products = await _productService.SearchProductsAsync(veryRelaxedCriteria);
                
                // Nếu vẫn không có → lấy top sản phẩm
                if (products.Count == 0)
                {
                    _logger.LogInformation("Still no products found, fetching top products");
                    var allProducts = await _productService.SearchProductsAsync(new ProductSearchCriteria());
                    products = allProducts
                        .Where(p => p.SellingPrice.HasValue)
                        .OrderByDescending(p => p.SellingPrice)
                        .Take(10)
                        .ToList();
                }
            }
            // Nếu không có kết quả và là câu hỏi chung → lấy top sản phẩm
            else if (products.Count == 0 && isGeneralProductQuery)
            {
                _logger.LogInformation("No products found with criteria, fetching top products for general query");
                // Lấy top 10 sản phẩm bán chạy hoặc mới nhất
                var allProducts = await _productService.SearchProductsAsync(new ProductSearchCriteria());
                products = allProducts
                    .Where(p => p.SellingPrice.HasValue)
                    .OrderByDescending(p => p.SellingPrice) // Sắp xếp theo giá (có thể thay bằng số lượng bán)
                    .Take(10)
                    .ToList();
            }
            
            // 7. Sort nếu là yêu cầu "máy rẻ"
            if (sortByPriceAscending)
            {
                products = products
                    .Where(p => p.SellingPrice.HasValue)
                    .OrderBy(p => p.SellingPrice)
                    .ToList();
            }
            // Nếu không có sort cụ thể và là câu hỏi chung → sort theo giá giảm dần (sản phẩm tốt nhất)
            else if (isGeneralProductQuery && products.Any())
            {
                products = products
                    .Where(p => p.SellingPrice.HasValue)
                    .OrderByDescending(p => p.SellingPrice)
                    .ToList();
            }
            
            // 10. Limit to top 5-10 results 
            // (10 nếu là "máy rẻ", câu hỏi chung, hoặc câu dài có nhiều tiêu chí để có nhiều lựa chọn)
            var hasMultipleCriteria = (!string.IsNullOrEmpty(criteria.BrandId) ? 1 : 0) +
                                     (!string.IsNullOrEmpty(criteria.Cpu) ? 1 : 0) +
                                     (!string.IsNullOrEmpty(criteria.Ram) ? 1 : 0) +
                                     (!string.IsNullOrEmpty(criteria.Rom) ? 1 : 0) +
                                     (!string.IsNullOrEmpty(criteria.Card) ? 1 : 0) +
                                     (criteria.MinPrice.HasValue || criteria.MaxPrice.HasValue ? 1 : 0) +
                                     (!string.IsNullOrEmpty(useCase) ? 1 : 0);
            
            var limit = (isCheapRequest || isGeneralProductQuery || hasMultipleCriteria >= 3) ? 10 : 5;
            return products.Take(limit).ToList();
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
                    var name = product.Metadata.GetValueOrDefault("name", "N/A")?.ToString() ?? "N/A";
                    var model = product.Metadata.GetValueOrDefault("model", "")?.ToString() ?? "";
                    var brand = product.Metadata.GetValueOrDefault("brand", "")?.ToString() ?? "";
                    var price = product.Metadata.TryGetValue("price", out var priceObj) ? priceObj : null;
                    
                    // Format tên sản phẩm: nếu có model thì ghép với name
                    var displayName = name;
                    if (!string.IsNullOrEmpty(model))
                    {
                        displayName = $"{name} {model}";
                    }
                    
                    sb.Append($"\n• **{displayName}**");
                    
                    if (!string.IsNullOrEmpty(brand))
                    {
                        sb.Append($"\n  Thương hiệu: {brand}");
                    }
                    
                    if (price != null)
                    {
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
                        
                        if (priceValue > 0)
                        {
                            sb.Append($"\n  Giá: {priceValue:N0} VND");
                        }
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
                // Nếu Avatar đã là URL đầy đủ (http/https), dùng trực tiếp
                if (p.Avatar.StartsWith("http"))
                {
                    imageUrl = p.Avatar;
                }
                // Nếu Avatar đã có /imageProducts/, dùng trực tiếp
                else if (p.Avatar.StartsWith("/imageProducts/"))
                {
                    imageUrl = $"{backendUrl}{p.Avatar}";
                }
                // Nếu Avatar chỉ là tên file (ví dụ: "abc.jpg"), thêm /imageProducts/
                else if (!p.Avatar.Contains("/"))
                {
                    imageUrl = $"{backendUrl}/imageProducts/{p.Avatar}";
                }
                // Trường hợp khác (có thể là đường dẫn tương đối khác)
                else
                {
                    imageUrl = $"{backendUrl}{(p.Avatar.StartsWith("/") ? "" : "/")}{p.Avatar}";
                }
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

            // Format tên sản phẩm: nếu có model thì ghép với name để hiển thị
            var displayName = p.ProductName ?? "";
            if (!string.IsNullOrEmpty(p.ProductModel))
            {
                displayName = $"{displayName} {p.ProductModel}";
            }
            
            return new ProductSuggestion
            {
                ProductId = p.ProductId ?? "",
                Name = displayName, // Tên đã bao gồm model
                ProductModel = p.ProductModel, // Vẫn giữ model riêng để frontend có thể dùng
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


