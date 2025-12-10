using Microsoft.SemanticKernel;
using System.ComponentModel;
using WebLaptopBE.Services;

namespace WebLaptopBE.AI.Plugins;

/// <summary>
/// Plugin để tìm kiếm policy documents từ Qdrant (vector database)
/// Plugin này sẽ được Semantic Kernel gọi khi LLM cần tìm thông tin về chính sách
/// </summary>
public class PolicyRetrievalPlugin
{
    private readonly IQdrantService _qdrantService;
    private readonly ILogger<PolicyRetrievalPlugin> _logger;

    public PolicyRetrievalPlugin(IQdrantService qdrantService, ILogger<PolicyRetrievalPlugin> logger)
    {
        _qdrantService = qdrantService;
        _logger = logger;
    }

    /// <summary>
    /// Function để tìm kiếm policy documents
    /// </summary>
    [KernelFunction]
    [Description("Tìm kiếm thông tin về chính sách bảo hành, đổi trả, hoàn tiền dựa trên câu hỏi của người dùng. Trả về các đoạn văn bản liên quan dưới dạng JSON.")]
    public async Task<string> SearchPolicies(
        [Description("Câu hỏi hoặc từ khóa về chính sách (ví dụ: 'chính sách bảo hành', 'đổi trả hàng', 'hoàn tiền').")] 
        string query,
        
        [Description("Số lượng kết quả tối đa (mặc định: 3).")] 
        int limit = 3
    )
    {
        try
        {
            _logger.LogInformation("PolicyRetrievalPlugin được gọi với query: {Query}, limit: {Limit}", query, limit);

            // Gọi QdrantService để tìm kiếm
            var results = await _qdrantService.SearchPoliciesAsync("warranty_policies", query, limit);
            
            // Format kết quả để LLM dễ đọc
            var formattedResults = results.Select(r => new
            {
                content = r.Content,
                score = r.Score,
                policyType = r.Metadata.TryGetValue("policy_type", out var type) ? type.ToString() : "unknown"
            });

            // Chuyển đổi sang JSON
            var json = System.Text.Json.JsonSerializer.Serialize(formattedResults, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Tìm thấy {Count} policy documents", results.Count);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong PolicyRetrievalPlugin");
            return "[]"; // Trả về mảng rỗng nếu có lỗi
        }
    }
}



