using Microsoft.SemanticKernel;
using System.ComponentModel;
using WebLaptopBE.Services;

namespace WebLaptopBE.AI.Plugins;

/// <summary>
/// Plugin để tìm kiếm sản phẩm từ database
/// Plugin này sẽ được Semantic Kernel gọi khi LLM cần tìm sản phẩm
/// </summary>
public class ProductSearchPlugin
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductSearchPlugin> _logger;

    public ProductSearchPlugin(IProductService productService, ILogger<ProductSearchPlugin> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Function để tìm kiếm sản phẩm
    /// [KernelFunction] = đánh dấu function này có thể được Semantic Kernel gọi
    /// [Description] = mô tả cho LLM biết function này làm gì
    /// </summary>
    [KernelFunction]
    [Description("Tìm kiếm sản phẩm laptop dựa trên các tiêu chí. Trả về danh sách sản phẩm phù hợp dưới dạng JSON.")]
    public async Task<string> SearchProducts(
        [Description("Thương hiệu (ví dụ: Dell, HP, Lenovo, Asus). Có thể để null nếu không cần lọc theo thương hiệu.")] 
        string? brand = null,
        
        [Description("Giá tối thiểu (VND). Ví dụ: 10000000 cho 10 triệu.")] 
        decimal? minPrice = null,
        
        [Description("Giá tối đa (VND). Ví dụ: 20000000 cho 20 triệu.")] 
        decimal? maxPrice = null,
        
        [Description("CPU (ví dụ: Intel Core i5, AMD Ryzen 7, Intel Core i7). Có thể để null.")] 
        string? cpu = null,
        
        [Description("RAM (ví dụ: 8GB, 16GB, 32GB). Có thể để null.")] 
        string? ram = null,
        
        [Description("Ổ cứng (ví dụ: 256GB SSD, 512GB SSD, 1TB SSD). Có thể để null.")] 
        string? rom = null,
        
        [Description("Card đồ họa (ví dụ: NVIDIA RTX 3060, NVIDIA RTX 4060). Có thể để null.")] 
        string? card = null,
        
        [Description("Từ khóa tìm kiếm (tìm trong tên sản phẩm, model). Có thể để null.")] 
        string? searchTerm = null
    )
    {
        try
        {
            _logger.LogInformation("ProductSearchPlugin được gọi với: brand={Brand}, minPrice={MinPrice}, maxPrice={MaxPrice}, cpu={Cpu}, ram={Ram}, rom={Rom}, card={Card}, searchTerm={SearchTerm}",
                brand, minPrice, maxPrice, cpu, ram, rom, card, searchTerm);

            // Tạo search criteria từ các tham số
            var criteria = new ProductSearchCriteria
            {
                BrandId = brand,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Cpu = cpu,
                Ram = ram,
                Rom = rom,
                Card = card,
                SearchTerm = searchTerm
            };

            // Gọi ProductService để tìm kiếm
            var products = await _productService.SearchProductsAsync(criteria);
            
            // Chuyển đổi sang JSON để LLM có thể đọc
            var json = System.Text.Json.JsonSerializer.Serialize(products, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Tìm thấy {Count} sản phẩm", products.Count);
            return json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong ProductSearchPlugin");
            return "[]"; // Trả về mảng rỗng nếu có lỗi
        }
    }
}



