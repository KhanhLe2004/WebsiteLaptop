using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using WebLaptopBE.AI.SemanticKernel;
using WebLaptopBE.DTOs;

namespace WebLaptopBE.AI.Plugins;

/// <summary>
/// Plugin để phát hiện intent (ý định) của user từ câu hỏi
/// Intent có thể là: product_inquiry, policy_inquiry, warranty_check, greeting, other
/// </summary>
public class IntentDetectionPlugin
{
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly ILogger<IntentDetectionPlugin> _logger;

    public IntentDetectionPlugin(ISemanticKernelService semanticKernelService, ILogger<IntentDetectionPlugin> logger)
    {
        _semanticKernelService = semanticKernelService;
        _logger = logger;
    }

    /// <summary>
    /// Function để phát hiện intent
    /// </summary>
    [KernelFunction]
    [Description("Phân loại intent (ý định) của câu hỏi người dùng. Trả về JSON với intent type, confidence score, và thông tin đã trích xuất.")]
    public async Task<string> DetectIntent(
        [Description("Câu hỏi của người dùng")] 
        string userMessage
    )
    {
        try
        {
            _logger.LogInformation("IntentDetectionPlugin được gọi với message: {Message}", userMessage);

            // Tạo prompt cho LLM để phân loại intent
            var prompt = $@"
Bạn là một AI chuyên phân loại intent (ý định) của khách hàng trong hệ thống bán laptop.

Phân loại câu hỏi sau đây và trả về JSON với format:
{{
    ""intent"": ""product_inquiry|policy_inquiry|warranty_check|greeting|other"",
    ""confidence"": 0.0-1.0,
    ""extracted_info"": {{
        ""brand"": ""nếu có thương hiệu (ví dụ: Dell, HP, Lenovo)"",
        ""price_range"": ""nếu có khoảng giá (ví dụ: dưới 20 triệu, từ 15-25 triệu)"",
        ""specs"": ""nếu có thông tin cấu hình (ví dụ: CPU, RAM, Card)"",
        ""serial_number"": ""nếu có số serial""
    }}
}}

Các loại intent:
- product_inquiry: Khách hàng hỏi về sản phẩm, muốn tìm laptop (ví dụ: ""Tôi muốn mua laptop gaming"", ""Laptop nào giá dưới 20 triệu?"")
- policy_inquiry: Khách hàng hỏi về chính sách (ví dụ: ""Chính sách bảo hành như thế nào?"", ""Có được đổi trả không?"")
- warranty_check: Khách hàng muốn kiểm tra bảo hành (ví dụ: ""Kiểm tra bảo hành serial ABC123"")
- greeting: Chào hỏi (ví dụ: ""Xin chào"", ""Hello"")
- other: Khác

Câu hỏi: {userMessage}

Chỉ trả về JSON, không có text khác.";

            // Gọi LLM
            var result = await _semanticKernelService.GenerateResponseAsync(prompt);
            
            // Parse JSON response
            try
            {
                var intentResult = JsonSerializer.Deserialize<IntentResult>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (intentResult != null)
                {
                    _logger.LogInformation("Intent detected: {Intent}, Confidence: {Confidence}", 
                        intentResult.Intent, intentResult.Confidence);
                    return result;
                }
            }
            catch (JsonException)
            {
                // Nếu không parse được JSON, thử extract JSON từ response
                _logger.LogWarning("Không parse được JSON từ LLM response, thử extract...");
            }

            // Fallback: trả về intent "other" nếu không parse được
            _logger.LogWarning("Fallback to 'other' intent");
            return JsonSerializer.Serialize(new IntentResult
            {
                Intent = "other",
                Confidence = 0.5f,
                ExtractedInfo = new Dictionary<string, object>()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi detect intent");
            // Trả về intent "other" nếu có lỗi
            return JsonSerializer.Serialize(new IntentResult
            {
                Intent = "other",
                Confidence = 0.0f,
                ExtractedInfo = new Dictionary<string, object>()
            });
        }
    }
}



