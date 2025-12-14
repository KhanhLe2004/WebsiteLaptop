namespace WebLaptopBE.Services;

public interface IQdrantService
{

    Task<bool> CollectionExistsAsync(string collectionName);
    Task CreateCollectionAsync(string collectionName);
    Task InsertPolicyAsync(string collectionName, string policyText, Dictionary<string, object> metadata);
    Task<List<PolicySearchResult>> SearchPoliciesAsync(string collectionName, string query, int limit = 3);
}

public class PolicySearchResult
{

    public string Content { get; set; } = string.Empty;
    public float Score { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}




