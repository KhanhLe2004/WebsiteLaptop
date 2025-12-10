namespace WebLaptopBE.Services;

/// <summary>
/// Interface cho Qdrant Vector Service - Mở rộng từ IQdrantService
/// Service này dùng để lưu trữ và tìm kiếm products + policies trong Qdrant
/// </summary>
public interface IQdrantVectorService
{
    /// <summary>
    /// Kiểm tra collection đã tồn tại chưa
    /// </summary>
    Task<bool> CollectionExistsAsync(string collectionName);
    
    /// <summary>
    /// Tạo collection mới
    /// </summary>
    Task CreateCollectionAsync(string collectionName);
    
    /// <summary>
    /// Upsert product vào Qdrant
    /// </summary>
    Task UpsertProductAsync(ProductEmbedding product);
    
    /// <summary>
    /// Upsert policy vào Qdrant
    /// </summary>
    Task UpsertPolicyAsync(PolicyEmbedding policy);
    
    /// <summary>
    /// Tìm kiếm products bằng vector search
    /// </summary>
    Task<List<VectorSearchResult>> SearchProductsAsync(string query, int topK = 5);
    
    /// <summary>
    /// Tìm kiếm policies bằng vector search
    /// </summary>
    Task<List<VectorSearchResult>> SearchPoliciesAsync(string query, int topK = 3);
}

/// <summary>
/// Product embedding data để lưu vào Qdrant
/// </summary>
public class ProductEmbedding
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Policy embedding data để lưu vào Qdrant
/// </summary>
public class PolicyEmbedding
{
    public string PolicyId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Kết quả tìm kiếm từ Qdrant
/// </summary>
public class VectorSearchResult
{
    public string Content { get; set; } = string.Empty;
    public float Score { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}


