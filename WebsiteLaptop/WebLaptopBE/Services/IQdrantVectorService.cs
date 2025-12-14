namespace WebLaptopBE.Services;

public interface IQdrantVectorService
{

    Task<bool> CollectionExistsAsync(string collectionName);
    Task CreateCollectionAsync(string collectionName);
    Task UpsertProductAsync(ProductEmbedding product);
    Task UpsertPolicyAsync(PolicyEmbedding policy);
    Task<List<VectorSearchResult>> SearchProductsAsync(string query, int topK = 5);
    Task<List<VectorSearchResult>> SearchPoliciesAsync(string query, int topK = 3);
}
public class ProductEmbedding
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class PolicyEmbedding
{
    public string PolicyId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class VectorSearchResult
{
    public string Content { get; set; } = string.Empty;
    public float Score { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}


