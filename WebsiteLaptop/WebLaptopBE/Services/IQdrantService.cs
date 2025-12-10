namespace WebLaptopBE.Services;

/// <summary>
/// Interface cho QdrantService - Service này dùng để lưu trữ và tìm kiếm policy documents trong Qdrant (vector database)
/// </summary>
public interface IQdrantService
{
    /// <summary>
    /// Kiểm tra xem collection (bảng) đã tồn tại chưa
    /// </summary>
    Task<bool> CollectionExistsAsync(string collectionName);
    
    /// <summary>
    /// Tạo collection mới trong Qdrant
    /// </summary>
    Task CreateCollectionAsync(string collectionName);
    
    /// <summary>
    /// Thêm policy document vào Qdrant (sẽ tự động tạo embedding)
    /// </summary>
    /// <param name="collectionName">Tên collection (ví dụ: "warranty_policies")</param>
    /// <param name="policyText">Nội dung policy (văn bản)</param>
    /// <param name="metadata">Thông tin bổ sung (loại policy, ngày hiệu lực, v.v.)</param>
    Task InsertPolicyAsync(string collectionName, string policyText, Dictionary<string, object> metadata);
    
    /// <summary>
    /// Tìm kiếm policy documents dựa trên câu hỏi của user (semantic search)
    /// </summary>
    /// <param name="collectionName">Tên collection</param>
    /// <param name="query">Câu hỏi của user (ví dụ: "chính sách bảo hành")</param>
    /// <param name="limit">Số lượng kết quả tối đa (mặc định: 3)</param>
    /// <returns>Danh sách các policy documents liên quan</returns>
    Task<List<PolicySearchResult>> SearchPoliciesAsync(string collectionName, string query, int limit = 3);
}

/// <summary>
/// Kết quả tìm kiếm policy từ Qdrant
/// </summary>
public class PolicySearchResult
{
    /// <summary>
    /// Nội dung policy document
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Điểm số similarity (0-1, càng cao càng giống)
    /// </summary>
    public float Score { get; set; }
    
    /// <summary>
    /// Thông tin bổ sung (loại policy, ngày hiệu lực, v.v.)
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}




