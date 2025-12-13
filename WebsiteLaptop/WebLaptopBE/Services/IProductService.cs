using WebLaptopBE.DTOs;

namespace WebLaptopBE.Services;

/// <summary>
/// Interface cho ProductService - Service này dùng để tìm kiếm sản phẩm từ database
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Tìm kiếm sản phẩm dựa trên nhiều tiêu chí (thương hiệu, giá, cấu hình, v.v.)
    /// </summary>
    Task<List<ProductDTO>> SearchProductsAsync(ProductSearchCriteria criteria);
    
    /// <summary>
    /// Lấy thông tin chi tiết 1 sản phẩm theo ID
    /// </summary>
    Task<ProductDTO?> GetProductByIdAsync(string productId);
    
    /// <summary>
    /// Lấy danh sách sản phẩm theo thương hiệu
    /// </summary>
    Task<List<ProductDTO>> GetProductsByBrandAsync(string brandId);
    
    /// <summary>
    /// Lấy danh sách sản phẩm trong khoảng giá
    /// </summary>
    Task<List<ProductDTO>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    
    /// <summary>
    /// Lấy danh sách sản phẩm theo cấu hình (CPU, RAM, ROM, Card)
    /// </summary>
    Task<List<ProductDTO>> GetProductsBySpecsAsync(ProductSpecs specs);
    
    /// <summary>
    /// Lấy nhiều sản phẩm theo danh sách IDs (batch query để tối ưu hiệu năng)
    /// </summary>
    Task<List<ProductDTO>> GetProductsByIdsAsync(List<string> productIds);
}

/// <summary>
/// Class chứa các tiêu chí tìm kiếm sản phẩm
/// </summary>
public class ProductSearchCriteria
{
    /// <summary>
    /// ID thương hiệu (ví dụ: "B001" cho Dell)
    /// </summary>
    public string? BrandId { get; set; }
    
    /// <summary>
    /// Giá tối thiểu (VND)
    /// </summary>
    public decimal? MinPrice { get; set; }
    
    /// <summary>
    /// Giá tối đa (VND)
    /// </summary>
    public decimal? MaxPrice { get; set; }
    
    /// <summary>
    /// CPU (ví dụ: "Intel Core i5", "AMD Ryzen 7")
    /// </summary>
    public string? Cpu { get; set; }
    
    /// <summary>
    /// RAM (ví dụ: "8GB", "16GB")
    /// </summary>
    public string? Ram { get; set; }
    
    /// <summary>
    /// Ổ cứng (ví dụ: "256GB SSD", "512GB SSD")
    /// </summary>
    public string? Rom { get; set; }
    
    /// <summary>
    /// Card đồ họa (ví dụ: "NVIDIA RTX 3060")
    /// </summary>
    public string? Card { get; set; }
    
    /// <summary>
    /// Thời gian bảo hành tối thiểu (tháng)
    /// </summary>
    public int? MinWarrantyPeriod { get; set; }
    
    /// <summary>
    /// Từ khóa tìm kiếm (tìm trong tên sản phẩm, model, thương hiệu)
    /// </summary>
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Class chứa thông tin cấu hình sản phẩm để tìm kiếm
/// </summary>
public class ProductSpecs
{
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Rom { get; set; }
    public string? Card { get; set; }
}




