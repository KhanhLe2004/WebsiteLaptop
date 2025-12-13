using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Services;

/// <summary>
/// Enhanced Product Service - Khai thác tối đa database cho chatbot
/// Bổ sung các tính năng tìm kiếm nâng cao, so sánh, gợi ý
/// </summary>
public interface IEnhancedProductService
{
    // Tìm kiếm nâng cao
    Task<List<ProductDTO>> SearchByScreenAsync(string screenQuery);
    Task<List<ProductDTO>> SearchByWeightAsync(decimal maxWeight);
    Task<List<ProductDTO>> SearchByBatteryAsync(string batteryQuery);
    Task<List<ProductDTO>> SearchByWarrantyAsync(int minMonths);
    
    // Use case recommendations
    Task<List<ProductDTO>> RecommendByUseCaseAsync(string useCase);
    
    // Tính toán giá trị
    Task<List<ProductWithDiscountDTO>> GetProductsWithDiscountAsync();
    
    // Đánh giá và khuyến mãi
    Task<ProductWithRatingDTO?> GetProductWithRatingAsync(string productId);
    Task<List<ProductDTO>> GetProductsWithPromotionAsync();
    
    // So sánh và gợi ý
    Task<ProductComparisonDTO?> CompareProductsAsync(string productId1, string productId2);
    Task<List<ProductDTO>> GetSimilarProductsAsync(string productId, int count = 5);
    
    // Kiểm tra tồn kho
    Task<bool> CheckStockAsync(string productId, string? specifications = null);
    Task<int> GetAvailableQuantityAsync(string productId, string? specifications = null);
}

public class EnhancedProductService : IEnhancedProductService
{
    private readonly Testlaptop38Context _dbContext;
    private readonly ILogger<EnhancedProductService> _logger;

    public EnhancedProductService(
        Testlaptop38Context dbContext,
        ILogger<EnhancedProductService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Tìm kiếm theo màn hình
    /// Ví dụ: "16 inch", "QHD+", "240Hz", "OLED"
    /// </summary>
    public async Task<List<ProductDTO>> SearchByScreenAsync(string screenQuery)
    {
        try
        {
            var query = _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Where(p => p.Active == true && 
                           p.Screen != null && 
                           p.Screen.Contains(screenQuery))
                .OrderBy(p => p.SellingPrice);

            var products = await query.ToListAsync();
            return ConvertToDTOs(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by screen: {Query}", screenQuery);
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// Tìm kiếm theo trọng lượng (tối đa)
    /// </summary>
    public async Task<List<ProductDTO>> SearchByWeightAsync(decimal maxWeight)
    {
        try
        {
            var products = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Where(p => p.Active == true && 
                           p.Weight != null && 
                           p.Weight <= maxWeight)
                .OrderBy(p => p.Weight)
                .ToListAsync();

            return ConvertToDTOs(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by weight: {MaxWeight}", maxWeight);
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// Tìm kiếm theo pin
    /// Ví dụ: "lâu", "99Wh", "pin tốt"
    /// </summary>
    public async Task<List<ProductDTO>> SearchByBatteryAsync(string batteryQuery)
    {
        try
        {
            var query = _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Where(p => p.Active == true && p.Pin != null);

            // Parse battery query
            if (batteryQuery.Contains("99") || batteryQuery.Contains("100") || batteryQuery.Contains("lâu") || batteryQuery.Contains("tốt"))
            {
                query = query.Where(p => p.Pin != null && 
                    (p.Pin.Contains("99") || p.Pin.Contains("100") || p.Pin.Contains("97")));
            }
            else if (batteryQuery.Contains("80") || batteryQuery.Contains("90"))
            {
                query = query.Where(p => p.Pin != null && 
                    (p.Pin.Contains("80") || p.Pin.Contains("90")));
            }

            var products = await query
                .OrderByDescending(p => p.Pin)
                .ToListAsync();

            return ConvertToDTOs(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by battery: {Query}", batteryQuery);
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// Tìm kiếm theo thời gian bảo hành (tối thiểu)
    /// </summary>
    public async Task<List<ProductDTO>> SearchByWarrantyAsync(int minMonths)
    {
        try
        {
            var products = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Where(p => p.Active == true && 
                           p.WarrantyPeriod != null && 
                           p.WarrantyPeriod >= minMonths)
                .OrderByDescending(p => p.WarrantyPeriod)
                .ToListAsync();

            return ConvertToDTOs(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching by warranty: {MinMonths}", minMonths);
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// Gợi ý sản phẩm theo use case
    /// </summary>
    public async Task<List<ProductDTO>> RecommendByUseCaseAsync(string useCase)
    {
        try
        {
            var query = _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Where(p => p.Active == true)
                .AsQueryable();

            switch (useCase.ToLower())
            {
                case "gaming":
                case "game":
                    query = query.Where(p => 
                        p.ProductConfigurations.Any(c => 
                            c.Card != null && 
                            (c.Card.Contains("RTX") || c.Card.Contains("GTX"))));
                    break;

                case "office":
                case "văn phòng":
                case "van phong":
                    query = query.Where(p => 
                        p.Weight <= 2.0m && 
                        p.ProductConfigurations.Any(c => 
                            c.Ram != null && c.Ram.Contains("8GB")));
                    break;

                case "design":
                case "đồ họa":
                case "do hoa":
                    query = query.Where(p => 
                        p.Screen != null && 
                        (p.Screen.Contains("4K") || p.Screen.Contains("OLED") || p.Screen.Contains("QHD")) &&
                        p.ProductConfigurations.Any(c => 
                            c.Ram != null && c.Ram.Contains("16GB")));
                    break;

                case "student":
                case "học sinh":
                case "hoc sinh":
                case "sinh viên":
                case "sinh vien":
                    query = query.Where(p => 
                        p.SellingPrice <= 20000000 && 
                        p.Weight <= 2.0m);
                    break;

                case "programming":
                case "lập trình":
                case "lap trinh":
                    query = query.Where(p => 
                        p.ProductConfigurations.Any(c => 
                            c.Ram != null && (c.Ram.Contains("16GB") || c.Ram.Contains("32GB"))));
                    break;
            }

            var products = await query
                .OrderBy(p => p.SellingPrice)
                .Take(10)
                .ToListAsync();

            return ConvertToDTOs(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending by use case: {UseCase}", useCase);
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// Lấy sản phẩm có giảm giá
    /// </summary>
    public async Task<List<ProductWithDiscountDTO>> GetProductsWithDiscountAsync()
    {
        try
        {
            var products = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Where(p => p.Active == true &&
                           p.OriginalSellingPrice != null &&
                           p.SellingPrice != null &&
                           p.OriginalSellingPrice > p.SellingPrice)
                .ToListAsync();

            return products.Select(p => new ProductWithDiscountDTO
            {
                Product = ConvertToDTO(p),
                DiscountPercent = p.OriginalSellingPrice > 0
                    ? ((p.OriginalSellingPrice.Value - p.SellingPrice.Value) / p.OriginalSellingPrice.Value) * 100
                    : 0,
                DiscountAmount = p.OriginalSellingPrice.Value - p.SellingPrice.Value
            })
            .OrderByDescending(p => p.DiscountPercent)
            .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products with discount");
            return new List<ProductWithDiscountDTO>();
        }
    }

    /// <summary>
    /// Lấy sản phẩm kèm đánh giá
    /// </summary>
    public async Task<ProductWithRatingDTO?> GetProductWithRatingAsync(string productId)
    {
        try
        {
            var product = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductReviews)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return null;

            var reviews = product.ProductReviews?.ToList() ?? new List<ProductReview>();
            var avgRating = reviews.Any() && reviews.Any(r => r.Rate.HasValue)
                ? reviews.Where(r => r.Rate.HasValue).Average(r => r.Rate!.Value)
                : 0;
            var topReview = reviews
                .OrderByDescending(r => r.Rate)
                .FirstOrDefault()?.ContentDetail;

            return new ProductWithRatingDTO
            {
                Product = ConvertToDTO(product),
                AverageRating = avgRating,
                ReviewCount = reviews.Count,
                TopReview = topReview
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product with rating: {ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Lấy sản phẩm có khuyến mãi
    /// </summary>
    public async Task<List<ProductDTO>> GetProductsWithPromotionAsync()
    {
        try
        {
            var products = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Include(p => p.Promotions)
                .Where(p => p.Active == true &&
                           p.Promotions != null &&
                           p.Promotions.Any())
                .ToListAsync();

            return ConvertToDTOs(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products with promotion");
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// So sánh 2 sản phẩm
    /// </summary>
    public async Task<ProductComparisonDTO?> CompareProductsAsync(string productId1, string productId2)
    {
        try
        {
            var p1 = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .FirstOrDefaultAsync(p => p.ProductId == productId1);
            var p2 = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .FirstOrDefaultAsync(p => p.ProductId == productId2);

            if (p1 == null || p2 == null) return null;

            var differences = new Dictionary<string, string>();

            if (p1.SellingPrice != p2.SellingPrice)
                differences["Giá"] = $"{p1.SellingPrice:N0}đ vs {p2.SellingPrice:N0}đ";

            if (p1.Screen != p2.Screen)
                differences["Màn hình"] = $"{p1.Screen} vs {p2.Screen}";

            if (p1.Weight != p2.Weight)
                differences["Trọng lượng"] = $"{p1.Weight}kg vs {p2.Weight}kg";

            if (p1.Pin != p2.Pin)
                differences["Pin"] = $"{p1.Pin} vs {p2.Pin}";

            if (p1.WarrantyPeriod != p2.WarrantyPeriod)
                differences["Bảo hành"] = $"{p1.WarrantyPeriod} tháng vs {p2.WarrantyPeriod} tháng";

            // So sánh config đầu tiên
            var config1 = p1.ProductConfigurations?.FirstOrDefault();
            var config2 = p2.ProductConfigurations?.FirstOrDefault();

            if (config1 != null && config2 != null)
            {
                if (config1.Cpu != config2.Cpu)
                    differences["CPU"] = $"{config1.Cpu} vs {config2.Cpu}";
                if (config1.Ram != config2.Ram)
                    differences["RAM"] = $"{config1.Ram} vs {config2.Ram}";
                if (config1.Rom != config2.Rom)
                    differences["Ổ cứng"] = $"{config1.Rom} vs {config2.Rom}";
                if (config1.Card != config2.Card)
                    differences["Card đồ họa"] = $"{config1.Card} vs {config2.Card}";
            }

            return new ProductComparisonDTO
            {
                Product1 = ConvertToDTO(p1),
                Product2 = ConvertToDTO(p2),
                Differences = differences
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing products: {Id1} vs {Id2}", productId1, productId2);
            return null;
        }
    }

    /// <summary>
    /// Lấy sản phẩm tương tự
    /// </summary>
    public async Task<List<ProductDTO>> GetSimilarProductsAsync(string productId, int count = 5)
    {
        try
        {
            var product = await _dbContext.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return new List<ProductDTO>();

            var brandId = product.BrandId;
            var priceRange = product.SellingPrice ?? 0;
            var minPrice = priceRange * 0.8m;
            var maxPrice = priceRange * 1.2m;

            var products = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Include(p => p.ProductImages)
                .Where(p => p.ProductId != productId &&
                           p.BrandId == brandId &&
                           p.SellingPrice >= minPrice &&
                           p.SellingPrice <= maxPrice &&
                           p.Active == true)
                .Take(count)
                .ToListAsync();

            return ConvertToDTOs(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting similar products: {ProductId}", productId);
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// Kiểm tra tồn kho
    /// </summary>
    public async Task<bool> CheckStockAsync(string productId, string? specifications = null)
    {
        try
        {
            var query = _dbContext.ProductConfigurations
                .Where(c => c.ProductId == productId);

            if (!string.IsNullOrEmpty(specifications))
            {
                query = query.Where(c => c.ConfigurationId == specifications);
            }

            var config = await query.FirstOrDefaultAsync();
            return config != null && config.Quantity > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock: {ProductId}", productId);
            return false;
        }
    }

    /// <summary>
    /// Lấy số lượng tồn kho
    /// </summary>
    public async Task<int> GetAvailableQuantityAsync(string productId, string? specifications = null)
    {
        try
        {
            var query = _dbContext.ProductConfigurations
                .Where(c => c.ProductId == productId);

            if (!string.IsNullOrEmpty(specifications))
            {
                query = query.Where(c => c.ConfigurationId == specifications);
            }

            var totalQuantity = await query.SumAsync(c => c.Quantity ?? 0);
            return totalQuantity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available quantity: {ProductId}", productId);
            return 0;
        }
    }

    // Helper methods
    private List<ProductDTO> ConvertToDTOs(List<Product> products)
    {
        return products.Select(ConvertToDTO).ToList();
    }

    private ProductDTO ConvertToDTO(Product product)
    {
        return new ProductDTO
        {
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            ProductModel = product.ProductModel,
            WarrantyPeriod = product.WarrantyPeriod,
            OriginalSellingPrice = product.OriginalSellingPrice,
            SellingPrice = product.SellingPrice,
            Screen = product.Screen,
            Camera = product.Camera,
            Connect = product.Connect,
            Weight = product.Weight,
            Pin = product.Pin,
            BrandId = product.BrandId,
            BrandName = product.Brand?.BrandName,
            Avatar = product.Avatar,
            Active = product.Active,
            Configurations = product.ProductConfigurations?.Select(c => new ProductConfigurationDTO
            {
                ConfigurationId = c.ConfigurationId,
                Cpu = c.Cpu,
                Ram = c.Ram,
                Rom = c.Rom,
                Card = c.Card,
                Price = c.Price,
                Quantity = c.Quantity,
                ProductId = c.ProductId
            }).ToList() ?? new List<ProductConfigurationDTO>(),
            Images = product.ProductImages?.Select(img => new ProductImageDTO
            {
                ImageId = img.ImageId,
                ProductId = img.ProductId,
                ImageUrl = $"/imageProducts/{img.ImageId}"
            }).ToList() ?? new List<ProductImageDTO>()
        };
    }
}

// DTOs
public class ProductWithDiscountDTO
{
    public ProductDTO Product { get; set; } = null!;
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
}

public class ProductWithRatingDTO
{
    public ProductDTO Product { get; set; } = null!;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? TopReview { get; set; }
}

public class ProductComparisonDTO
{
    public ProductDTO Product1 { get; set; } = null!;
    public ProductDTO Product2 { get; set; } = null!;
    public Dictionary<string, string> Differences { get; set; } = new();
}



