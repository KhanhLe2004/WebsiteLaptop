using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using WebLaptopBE.Services;
using WebLaptopBE.AI.Data;

namespace WebLaptopBE.AI.Plugins;

/// <summary>
/// Plugin để tìm kiếm policy documents
/// Sử dụng PolicyData để lấy chính sách đầy đủ với keyword matching đa dạng
/// Fallback sang Qdrant nếu cần
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
    /// Function để tìm kiếm policy documents với keyword matching mạnh mẽ
    /// </summary>
    [KernelFunction]
    [Description("Tìm kiếm thông tin về chính sách bảo hành, bảo mật, thanh toán dựa trên câu hỏi của người dùng. Trả về FULL TEXT chính sách liên quan.")]
    public async Task<string> SearchPolicies(
        [Description("Câu hỏi hoặc từ khóa về chính sách (ví dụ: 'chính sách bảo hành', 'thanh toán', 'bảo mật thông tin').")] 
        string query,
        
        [Description("Số lượng kết quả tối đa (mặc định: 3).")] 
        int limit = 3
    )
    {
        try
        {
            _logger.LogInformation("PolicyRetrievalPlugin được gọi với query: {Query}, limit: {Limit}", query, limit);

            // Bước 1: Tìm kiếm từ PolicyData (keyword-based, nhanh và chính xác)
            var matchedPolicies = PolicyData.SearchPolicies(query);
            
            if (matchedPolicies.Count > 0)
            {
                _logger.LogInformation("Tìm thấy {Count} policy documents từ PolicyData", matchedPolicies.Count);
                
                // Format kết quả với FULL TEXT
                var formattedResults = matchedPolicies.Take(limit).Select(p => new
                {
                    policyId = p.PolicyId,
                    title = p.Title,
                    category = p.Category.ToString(),
                    content = p.Content, // FULL TEXT - không truncate
                    keywords = p.Keywords
                });

                var json = JsonSerializer.Serialize(formattedResults, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                return json;
            }

            // Bước 2: Fallback sang Qdrant nếu không tìm thấy từ PolicyData
            _logger.LogInformation("Không tìm thấy từ PolicyData, fallback sang Qdrant");
            try
            {
                var results = await _qdrantService.SearchPoliciesAsync("warranty_policies", query, limit);
                
                if (results.Count > 0)
                {
                    var formattedResults = results.Select(r => new
                    {
                        content = r.Content,
                        score = r.Score,
                        policyType = r.Metadata.TryGetValue("policy_type", out var type) ? type.ToString() : "unknown"
                    });

                    var json = JsonSerializer.Serialize(formattedResults, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _logger.LogInformation("Tìm thấy {Count} policy documents từ Qdrant", results.Count);
                    return json;
                }
            }
            catch (Exception qdrantEx)
            {
                _logger.LogWarning(qdrantEx, "Lỗi khi query Qdrant, trả về tất cả policies");
            }

            // Bước 3: Nếu cả 2 đều fail, trả về tất cả chính sách
            var allPolicies = PolicyData.GetAllPolicies();
            var allFormattedResults = allPolicies.Take(limit).Select(p => new
            {
                policyId = p.PolicyId,
                title = p.Title,
                category = p.Category.ToString(),
                content = p.Content,
                keywords = p.Keywords
            });

            var allJson = JsonSerializer.Serialize(allFormattedResults, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            _logger.LogInformation("Trả về tất cả {Count} policies", allPolicies.Count);
            return allJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong PolicyRetrievalPlugin");
            
            // Fallback cuối cùng: trả về tất cả policies
            try
            {
                var allPolicies = PolicyData.GetAllPolicies();
                var allFormattedResults = allPolicies.Select(p => new
                {
                    policyId = p.PolicyId,
                    title = p.Title,
                    category = p.Category.ToString(),
                    content = p.Content
                });

                return JsonSerializer.Serialize(allFormattedResults, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                return "[]";
            }
        }
    }
}



