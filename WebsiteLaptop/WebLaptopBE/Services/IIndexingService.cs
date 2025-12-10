namespace WebLaptopBE.Services;

/// <summary>
/// Interface cho Indexing Service
/// Service này đưa dữ liệu từ SQL Server → Qdrant (vector database)
/// </summary>
public interface IIndexingService
{
    /// <summary>
    /// Index tất cả products từ database vào Qdrant
    /// </summary>
    Task IndexAllProductsAsync();
    
    /// <summary>
    /// Index tất cả policies từ database/file vào Qdrant
    /// </summary>
    Task IndexAllPoliciesAsync();
    
    /// <summary>
    /// Index một product cụ thể
    /// </summary>
    Task IndexProductAsync(string productId);
}


