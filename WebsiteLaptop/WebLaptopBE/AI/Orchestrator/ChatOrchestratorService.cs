using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using WebLaptopBE.AI.Plugins;
using WebLaptopBE.AI.SemanticKernel;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;
using WebLaptopBE.Services;

namespace WebLaptopBE.AI.Orchestrator;

/// <summary>
/// Interface cho ChatOrchestratorService
/// Service này là "não trung tâm" điều phối toàn bộ flow của chatbot
/// </summary>
public interface IChatOrchestratorService
{
    /// <summary>
    /// Xử lý message từ user và trả về response
    /// </summary>
    Task<ChatResponse> ProcessMessageAsync(ChatRequest request);
}

/// <summary>
/// Service điều phối chatbot AI
/// Flow: User Message → Intent Detection → Route Decision → Data Retrieval → Response Generation
/// </summary>
public class ChatOrchestratorService : IChatOrchestratorService
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly IProductService _productService;
    private readonly IQdrantService _qdrantService;
    private readonly Testlaptop38Context _dbContext;
    private readonly ILogger<ChatOrchestratorService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Kernel _kernel;

    public ChatOrchestratorService(
        ISemanticKernelService semanticKernelService,
        IProductService productService,
        IQdrantService qdrantService,
        Testlaptop38Context dbContext,
        ILogger<ChatOrchestratorService> logger,
        ILoggerFactory loggerFactory)
    {
        _semanticKernelService = semanticKernelService;
        _productService = productService;
        _qdrantService = qdrantService;
        _dbContext = dbContext;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _kernel = semanticKernelService.GetKernel();

        // Đăng ký các plugins vào kernel với dependencies đã inject
        // Kiểm tra xem plugin đã tồn tại chưa để tránh lỗi duplicate key
        if (!_kernel.Plugins.Any(p => p.Name == "ProductSearchPlugin"))
        {
            var productPlugin = new ProductSearchPlugin(_productService, 
                _loggerFactory.CreateLogger<ProductSearchPlugin>());
            _kernel.Plugins.AddFromObject(productPlugin, "ProductSearchPlugin");
        }

        if (!_kernel.Plugins.Any(p => p.Name == "PolicyRetrievalPlugin"))
        {
            var policyPlugin = new PolicyRetrievalPlugin(_qdrantService, 
                _loggerFactory.CreateLogger<PolicyRetrievalPlugin>());
            _kernel.Plugins.AddFromObject(policyPlugin, "PolicyRetrievalPlugin");
        }

        if (!_kernel.Plugins.Any(p => p.Name == "IntentDetectionPlugin"))
        {
            var intentPlugin = new IntentDetectionPlugin(_semanticKernelService, 
                _loggerFactory.CreateLogger<IntentDetectionPlugin>());
            _kernel.Plugins.AddFromObject(intentPlugin, "IntentDetectionPlugin");
        }
    }

    /// <summary>
    /// Xử lý message từ user
    /// </summary>
    public async Task<ChatResponse> ProcessMessageAsync(ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Processing message: {Message}", request.Message);

            // Bước 1: Tạo hoặc lấy session ID
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            // Bước 2: Phát hiện intent
            var intentResult = await DetectIntentAsync(request.Message);

            // Bước 3: Kiểm tra confidence - nếu quá thấp thì hỏi lại
            if (intentResult.Confidence < 0.5f)
            {
                return new ChatResponse
                {
                    Response = "Xin lỗi, tôi chưa hiểu rõ câu hỏi của bạn. Bạn có thể làm rõ hơn không? Bạn muốn tìm sản phẩm hay hỏi về chính sách?",
                    Intent = intentResult.Intent,
                    Confidence = intentResult.Confidence,
                    SessionId = sessionId,
                    RequiresClarification = true
                };
            }

            // Bước 4: Route dựa trên intent
            string response;
            List<ProductDTO>? products = null;
            List<PolicySearchResultDTO>? policies = null;

            switch (intentResult.Intent.ToLower())
            {
                case "product_inquiry":
                    // Tìm kiếm sản phẩm
                    var productResult = await HandleProductInquiryAsync(request.Message, intentResult);
                    response = productResult.Response;
                    products = productResult.Products;
                    break;

                case "policy_inquiry":
                    // Tìm kiếm policy
                    var policyResult = await HandlePolicyInquiryAsync(request.Message);
                    response = policyResult.Response;
                    policies = policyResult.Policies;
                    break;

                case "warranty_check":
                    // Kiểm tra bảo hành
                    response = await HandleWarrantyCheckAsync(request.Message, intentResult);
                    break;

                case "greeting":
                    // Chào hỏi
                    response = "Xin chào! Tôi là chatbot tư vấn bán hàng. Tôi có thể giúp bạn:\n" +
                               "- Tìm kiếm laptop phù hợp\n" +
                               "- Tư vấn về chính sách bảo hành, đổi trả\n" +
                               "- Kiểm tra tình trạng bảo hành\n\n" +
                               "Bạn cần hỗ trợ gì?";
                    break;

                default:
                    response = "Xin lỗi, tôi chưa thể trả lời câu hỏi này. Bạn có thể hỏi về sản phẩm hoặc chính sách bảo hành không?";
                    break;
            }

            // Bước 5: Lưu chat history vào database
            await SaveChatHistoryAsync(sessionId, request.Message, response, intentResult.Intent, request.CustomerId);

            // Bước 6: Trả về response
            return new ChatResponse
            {
                Response = response,
                Intent = intentResult.Intent,
                Confidence = intentResult.Confidence,
                SessionId = sessionId,
                Products = products,
                Policies = policies,
                RequiresClarification = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return new ChatResponse
            {
                Response = "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.",
                Intent = "error",
                Confidence = 0.0f,
                SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
                RequiresClarification = false
            };
        }
    }

    /// <summary>
    /// Phát hiện intent từ message
    /// </summary>
    private async Task<IntentResult> DetectIntentAsync(string message)
    {
        try
        {
            var intentPlugin = _kernel.Plugins["IntentDetectionPlugin"];
            var result = await _kernel.InvokeAsync<string>(intentPlugin["DetectIntent"], new KernelArguments
            {
                ["userMessage"] = message
            });

            if (string.IsNullOrWhiteSpace(result))
            {
                return new IntentResult
                {
                    Intent = "other",
                    Confidence = 0.0f,
                    ExtractedInfo = new Dictionary<string, object>()
                };
            }

            var intentResult = JsonSerializer.Deserialize<IntentResult>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return intentResult ?? new IntentResult
            {
                Intent = "other",
                Confidence = 0.0f,
                ExtractedInfo = new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting intent");
            return new IntentResult
            {
                Intent = "other",
                Confidence = 0.0f,
                ExtractedInfo = new Dictionary<string, object>()
            };
        }
    }

    /// <summary>
    /// Xử lý product inquiry - tìm kiếm sản phẩm
    /// </summary>
    private async Task<(string Response, List<ProductDTO>? Products)> HandleProductInquiryAsync(
        string message, IntentResult intentResult)
    {
        try
        {
            // Trích xuất thông tin từ intent result
            var extractedInfo = intentResult.ExtractedInfo;
            var brand = extractedInfo.TryGetValue("brand", out var brandValue) ? brandValue?.ToString() : null;
            var priceRange = extractedInfo.TryGetValue("price_range", out var priceValue) ? priceValue?.ToString() : null;
            var specs = extractedInfo.TryGetValue("specs", out var specsValue) ? specsValue?.ToString() : null;

            // Parse price range (ví dụ: "dưới 20 triệu" -> maxPrice = 20000000)
            decimal? minPrice = null;
            decimal? maxPrice = null;
            if (!string.IsNullOrEmpty(priceRange))
            {
                var priceInfo = ParsePriceRange(priceRange);
                minPrice = priceInfo.MinPrice;
                maxPrice = priceInfo.MaxPrice;
            }

            // Gọi ProductSearchPlugin để tìm sản phẩm
            var productPlugin = _kernel.Plugins["ProductSearchPlugin"];
            var searchResult = await _kernel.InvokeAsync<string>(productPlugin["SearchProducts"], new KernelArguments
            {
                ["brand"] = brand,
                ["minPrice"] = minPrice,
                ["maxPrice"] = maxPrice,
                ["cpu"] = specs?.Contains("CPU") == true ? specs : null,
                ["ram"] = specs?.Contains("RAM") == true ? specs : null,
                ["rom"] = specs?.Contains("ROM") == true ? specs : null,
                ["searchTerm"] = message
            });

            // Parse products từ JSON
            List<ProductDTO> products = new List<ProductDTO>();
            if (!string.IsNullOrWhiteSpace(searchResult))
            {
                products = JsonSerializer.Deserialize<List<ProductDTO>>(searchResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<ProductDTO>();
            }

            // Tạo prompt để LLM generate response
            var responsePrompt = $@"
Bạn là nhân viên tư vấn bán laptop chuyên nghiệp.

Người dùng hỏi: {message}

Bạn đã tìm thấy {products.Count} sản phẩm phù hợp. Dữ liệu sản phẩm (JSON):
{searchResult}

Hãy tạo câu trả lời tư vấn:
1. Chào hỏi và xác nhận yêu cầu
2. Giới thiệu các sản phẩm phù hợp (tên, giá, cấu hình nổi bật)
3. Gợi ý sản phẩm tốt nhất
4. Kết thúc bằng câu hỏi xem có cần thêm thông tin không

Lưu ý:
- Chỉ đề cập đến sản phẩm có trong dữ liệu, KHÔNG bịa thông tin
- Nếu không có sản phẩm nào, hãy xin lỗi và đề xuất tiêu chí khác
- Viết bằng tiếng Việt, thân thiện, chuyên nghiệp
- Giữ response ngắn gọn, dễ đọc (khoảng 150-200 từ)";

            var response = await _semanticKernelService.GenerateResponseAsync(responsePrompt);

            return (response, products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling product inquiry");
            return ("Xin lỗi, tôi không thể tìm kiếm sản phẩm lúc này. Vui lòng thử lại sau.", null);
        }
    }

    /// <summary>
    /// Xử lý policy inquiry - tìm kiếm chính sách
    /// </summary>
    private async Task<(string Response, List<PolicySearchResultDTO>? Policies)> HandlePolicyInquiryAsync(string message)
    {
        try
        {
            // Gọi PolicyRetrievalPlugin để tìm policy
            var policyPlugin = _kernel.Plugins["PolicyRetrievalPlugin"];
            var searchResult = await _kernel.InvokeAsync<string>(policyPlugin["SearchPolicies"], new KernelArguments
            {
                ["query"] = message,
                ["limit"] = 3
            });

            // Parse policies từ JSON
            List<PolicySearchResultDTO> policiesJson = new List<PolicySearchResultDTO>();
            if (!string.IsNullOrWhiteSpace(searchResult))
            {
                policiesJson = JsonSerializer.Deserialize<List<PolicySearchResultDTO>>(searchResult, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<PolicySearchResultDTO>();
            }

            // Tạo prompt để LLM generate response
            var responsePrompt = $@"
Bạn là nhân viên tư vấn chính sách bảo hành, đổi trả.

Người dùng hỏi: {message}

Bạn đã tìm thấy thông tin chính sách liên quan. Dữ liệu chính sách (JSON):
{searchResult}

Hãy tạo câu trả lời:
1. Xác nhận câu hỏi
2. Trả lời dựa trên thông tin chính sách đã tìm được
3. Nếu cần, giải thích thêm chi tiết
4. Kết thúc bằng câu hỏi xem có cần thêm thông tin không

Lưu ý:
- Chỉ trả lời dựa trên thông tin chính sách có trong dữ liệu, KHÔNG bịa thông tin
- Nếu không tìm thấy thông tin, hãy xin lỗi và đề xuất liên hệ nhân viên
- Viết bằng tiếng Việt, rõ ràng, dễ hiểu
- Giữ response ngắn gọn (khoảng 100-150 từ)";

            var response = await _semanticKernelService.GenerateResponseAsync(responsePrompt);

            return (response, policiesJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling policy inquiry");
            return ("Xin lỗi, tôi không thể tìm thông tin chính sách lúc này. Vui lòng liên hệ nhân viên để được hỗ trợ.", null);
        }
    }

    /// <summary>
    /// Xử lý warranty check - kiểm tra bảo hành
    /// </summary>
    private async Task<string> HandleWarrantyCheckAsync(string message, IntentResult intentResult)
    {
        try
        {
            // Trích xuất serial number nếu có
            var extractedInfo = intentResult.ExtractedInfo;
            var serialNumber = extractedInfo.TryGetValue("serial_number", out var serialValue) 
                ? serialValue?.ToString() 
                : null;

            if (string.IsNullOrEmpty(serialNumber))
            {
                return "Để kiểm tra bảo hành, vui lòng cung cấp số serial của sản phẩm. Bạn có thể tìm số serial trên hóa đơn hoặc trên sản phẩm.";
            }

            // Query database để kiểm tra warranty
            var productSerial = await _dbContext.ProductSerials
                .Include(ps => ps.Product)
                .Where(ps => ps.SerialId == serialNumber)
                .FirstOrDefaultAsync();

            if (productSerial == null)
            {
                return $"Không tìm thấy sản phẩm với serial số: {serialNumber}. Vui lòng kiểm tra lại số serial.";
            }

            var warrantyStatus = productSerial.Status ?? "Unknown";
            var warrantyStartDate = productSerial.WarrantyStartDate;
            var warrantyEndDate = productSerial.WarrantyEndDate;
            var productName = productSerial.Product?.ProductName ?? "Unknown";

            var response = $"Thông tin bảo hành cho sản phẩm {productName} (Serial: {serialNumber}):\n\n" +
                          $"Trạng thái: {warrantyStatus}\n" +
                          $"Ngày bắt đầu bảo hành: {(warrantyStartDate?.ToString("dd/MM/yyyy") ?? "N/A")}\n" +
                          $"Ngày kết thúc bảo hành: {(warrantyEndDate?.ToString("dd/MM/yyyy") ?? "N/A")}\n\n";

            if (warrantyEndDate.HasValue && warrantyEndDate.Value < DateTime.Now)
            {
                response += "⚠️ Sản phẩm đã hết hạn bảo hành.";
            }
            else if (warrantyEndDate.HasValue && warrantyEndDate.Value >= DateTime.Now)
            {
                response += "✅ Sản phẩm vẫn còn trong thời hạn bảo hành.";
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling warranty check");
            return "Xin lỗi, không thể kiểm tra bảo hành lúc này. Vui lòng thử lại sau hoặc liên hệ nhân viên.";
        }
    }

    /// <summary>
    /// Parse price range từ text (ví dụ: "dưới 20 triệu" -> maxPrice = 20000000)
    /// </summary>
    private (decimal? MinPrice, decimal? MaxPrice) ParsePriceRange(string priceRange)
    {
        var text = priceRange.ToLower();
        decimal? minPrice = null;
        decimal? maxPrice = null;

        // "dưới X triệu" -> maxPrice = X * 1000000
        if (text.Contains("dưới") || text.Contains("dưới"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)");
            if (match.Success && decimal.TryParse(match.Value, out var value))
            {
                maxPrice = value * 1000000; // triệu -> VND
            }
        }
        // "từ X đến Y triệu" -> minPrice = X * 1000000, maxPrice = Y * 1000000
        else if (text.Contains("từ") && text.Contains("đến"))
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"(\d+)");
            if (matches.Count >= 2)
            {
                if (decimal.TryParse(matches[0].Value, out var min) && decimal.TryParse(matches[1].Value, out var max))
                {
                    minPrice = min * 1000000;
                    maxPrice = max * 1000000;
                }
            }
        }
        // "trên X triệu" -> minPrice = X * 1000000
        else if (text.Contains("trên") || text.Contains("từ"))
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)");
            if (match.Success && decimal.TryParse(match.Value, out var value))
            {
                minPrice = value * 1000000;
            }
        }

        return (minPrice, maxPrice);
    }

    /// <summary>
    /// Lưu chat history vào database
    /// </summary>
    private async Task SaveChatHistoryAsync(string sessionId, string message, string response, string intent, string? customerId)
    {
        try
        {
            var chat = new Chat
            {
                ChatId = Guid.NewGuid().ToString(),
                ContentDetail = $"User: {message}\nBot: {response}",
                Time = DateTime.Now,
                Status = intent,
                CustomerId = customerId,
                EmployeeId = null // AI chatbot, không có employee
            };

            _dbContext.Chats.Add(chat);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Saved chat history: {ChatId}", chat.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chat history");
            // Không throw exception để không ảnh hưởng đến response
        }
    }
}



