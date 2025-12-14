using WebLaptopBE.DTOs;

namespace WebLaptopBE.Services;
public interface IProductService
{
    Task<List<ProductDTO>> SearchProductsAsync(ProductSearchCriteria criteria);
    Task<ProductDTO?> GetProductByIdAsync(string productId);
    Task<List<ProductDTO>> GetProductsByBrandAsync(string brandId);
    Task<List<ProductDTO>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<List<ProductDTO>> GetProductsBySpecsAsync(ProductSpecs specs);
    Task<List<ProductDTO>> GetProductsByIdsAsync(List<string> productIds);
}

public class ProductSearchCriteria
{
    public string? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Rom { get; set; }
    public string? Card { get; set; }
    public int? MinWarrantyPeriod { get; set; }
    public string? SearchTerm { get; set; }
}

public class ProductSpecs
{
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Rom { get; set; }
    public string? Card { get; set; }
}




