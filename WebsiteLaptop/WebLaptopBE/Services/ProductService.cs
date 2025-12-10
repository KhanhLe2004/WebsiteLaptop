using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Services;

/// <summary>
/// Service để tìm kiếm và lấy thông tin sản phẩm từ database
/// Service này sẽ được gọi bởi ChatOrchestrator khi user hỏi về sản phẩm
/// </summary>
public class ProductService : IProductService
{
    private readonly Testlaptop35Context _context;
    private readonly ILogger<ProductService> _logger;

    /// <summary>
    /// Constructor - Nhận vào DbContext và Logger từ Dependency Injection
    /// </summary>
    public ProductService(Testlaptop35Context context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Tìm kiếm sản phẩm với nhiều tiêu chí
    /// </summary>
    public async Task<List<ProductDTO>> SearchProductsAsync(ProductSearchCriteria criteria)
    {
        try
        {
            // Bắt đầu với query lấy tất cả sản phẩm đang active (đang bán)
            var query = _context.Products
                .Include(p => p.Brand) // Load thông tin thương hiệu
                .Include(p => p.ProductConfigurations) // Load các cấu hình
                .Where(p => p.Active == true) // Chỉ lấy sản phẩm đang bán
                .AsQueryable();

            // Nếu có BrandId, lọc theo thương hiệu
            if (!string.IsNullOrEmpty(criteria.BrandId))
            {
                query = query.Where(p => p.BrandId == criteria.BrandId);
            }

            // Nếu có giá tối thiểu, lọc sản phẩm có giá >= MinPrice
            if (criteria.MinPrice.HasValue)
            {
                query = query.Where(p => p.SellingPrice != null && p.SellingPrice >= criteria.MinPrice);
            }

            // Nếu có giá tối đa, lọc sản phẩm có giá <= MaxPrice
            if (criteria.MaxPrice.HasValue)
            {
                query = query.Where(p => p.SellingPrice != null && p.SellingPrice <= criteria.MaxPrice);
            }

            // Nếu có CPU, tìm trong ProductConfiguration
            if (!string.IsNullOrEmpty(criteria.Cpu))
            {
                query = query.Where(p => p.ProductConfigurations.Any(pc => 
                    pc.Cpu != null && pc.Cpu.Contains(criteria.Cpu)));
            }

            // Nếu có RAM, tìm trong ProductConfiguration
            if (!string.IsNullOrEmpty(criteria.Ram))
            {
                query = query.Where(p => p.ProductConfigurations.Any(pc => 
                    pc.Ram != null && pc.Ram.Contains(criteria.Ram)));
            }

            // Nếu có ROM, tìm trong ProductConfiguration
            if (!string.IsNullOrEmpty(criteria.Rom))
            {
                query = query.Where(p => p.ProductConfigurations.Any(pc => 
                    pc.Rom != null && pc.Rom.Contains(criteria.Rom)));
            }

            // Nếu có Card, tìm trong ProductConfiguration
            if (!string.IsNullOrEmpty(criteria.Card))
            {
                query = query.Where(p => p.ProductConfigurations.Any(pc => 
                    pc.Card != null && pc.Card.Contains(criteria.Card)));
            }

            // Nếu có thời gian bảo hành tối thiểu
            if (criteria.MinWarrantyPeriod.HasValue)
            {
                query = query.Where(p => p.WarrantyPeriod >= criteria.MinWarrantyPeriod);
            }

            // Nếu có từ khóa tìm kiếm, tìm trong tên, model, hoặc thương hiệu
            if (!string.IsNullOrEmpty(criteria.SearchTerm))
            {
                var searchTerm = criteria.SearchTerm.ToLower();
                query = query.Where(p => 
                    (p.ProductName != null && p.ProductName.ToLower().Contains(searchTerm)) ||
                    (p.ProductModel != null && p.ProductModel.ToLower().Contains(searchTerm)) ||
                    (p.Brand != null && p.Brand.BrandName != null && 
                     p.Brand.BrandName.ToLower().Contains(searchTerm)));
            }

            // Thực hiện query và chuyển đổi sang DTO
            var products = await query
                .Select(p => new ProductDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductModel = p.ProductModel,
                    SellingPrice = p.SellingPrice,
                    OriginalSellingPrice = p.OriginalSellingPrice,
                    WarrantyPeriod = p.WarrantyPeriod,
                    Screen = p.Screen,
                    Weight = p.Weight,
                    Pin = p.Pin,
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.BrandName : null,
                    Avatar = p.Avatar,
                    // Lấy thông tin cấu hình (CPU, RAM, ROM, Card, Giá, Số lượng)
                    Configurations = p.ProductConfigurations.Select(pc => new ProductConfigurationDTO
                    {
                        ConfigurationId = pc.ConfigurationId,
                        Cpu = pc.Cpu,
                        Ram = pc.Ram,
                        Rom = pc.Rom,
                        Card = pc.Card,
                        Price = pc.Price,
                        Quantity = pc.Quantity,
                        ProductId = pc.ProductId
                    }).ToList()
                })
                .ToListAsync();

            _logger.LogInformation("Found {Count} products matching criteria", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            // Ghi log lỗi và trả về danh sách rỗng thay vì crash
            _logger.LogError(ex, "Error searching products with criteria: {Criteria}", criteria);
            return new List<ProductDTO>();
        }
    }

    /// <summary>
    /// Lấy thông tin chi tiết 1 sản phẩm theo ID
    /// </summary>
    public async Task<ProductDTO?> GetProductByIdAsync(string productId)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Where(p => p.ProductId == productId && p.Active == true)
                .Select(p => new ProductDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductModel = p.ProductModel,
                    SellingPrice = p.SellingPrice,
                    OriginalSellingPrice = p.OriginalSellingPrice,
                    WarrantyPeriod = p.WarrantyPeriod,
                    Screen = p.Screen,
                    Weight = p.Weight,
                    Pin = p.Pin,
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.BrandName : null,
                    Avatar = p.Avatar,
                    Configurations = p.ProductConfigurations.Select(pc => new ProductConfigurationDTO
                    {
                        ConfigurationId = pc.ConfigurationId,
                        Cpu = pc.Cpu,
                        Ram = pc.Ram,
                        Rom = pc.Rom,
                        Card = pc.Card,
                        Price = pc.Price,
                        Quantity = pc.Quantity,
                        ProductId = pc.ProductId
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by ID: {ProductId}", productId);
            return null;
        }
    }

    /// <summary>
    /// Lấy danh sách sản phẩm theo thương hiệu
    /// </summary>
    public async Task<List<ProductDTO>> GetProductsByBrandAsync(string brandId)
    {
        var criteria = new ProductSearchCriteria { BrandId = brandId };
        return await SearchProductsAsync(criteria);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm trong khoảng giá
    /// </summary>
    public async Task<List<ProductDTO>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        var criteria = new ProductSearchCriteria 
        { 
            MinPrice = minPrice, 
            MaxPrice = maxPrice 
        };
        return await SearchProductsAsync(criteria);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm theo cấu hình
    /// </summary>
    public async Task<List<ProductDTO>> GetProductsBySpecsAsync(ProductSpecs specs)
    {
        var criteria = new ProductSearchCriteria
        {
            Cpu = specs.Cpu,
            Ram = specs.Ram,
            Rom = specs.Rom,
            Card = specs.Card
        };
        return await SearchProductsAsync(criteria);
    }

    /// <summary>
    /// Lấy nhiều sản phẩm theo danh sách IDs (batch query để tối ưu hiệu năng)
    /// Thay vì gọi GetProductByIdAsync nhiều lần, dùng 1 query duy nhất
    /// </summary>
    public async Task<List<ProductDTO>> GetProductsByIdsAsync(List<string> productIds)
    {
        try
        {
            if (productIds == null || productIds.Count == 0)
            {
                return new List<ProductDTO>();
            }

            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Where(p => productIds.Contains(p.ProductId) && p.Active == true)
                .Select(p => new ProductDTO
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    ProductModel = p.ProductModel,
                    SellingPrice = p.SellingPrice,
                    OriginalSellingPrice = p.OriginalSellingPrice,
                    WarrantyPeriod = p.WarrantyPeriod,
                    Screen = p.Screen,
                    Weight = p.Weight,
                    Pin = p.Pin,
                    BrandId = p.BrandId,
                    BrandName = p.Brand != null ? p.Brand.BrandName : null,
                    Avatar = p.Avatar,
                    Configurations = p.ProductConfigurations.Select(pc => new ProductConfigurationDTO
                    {
                        ConfigurationId = pc.ConfigurationId,
                        Cpu = pc.Cpu,
                        Ram = pc.Ram,
                        Rom = pc.Rom,
                        Card = pc.Card,
                        Price = pc.Price,
                        Quantity = pc.Quantity,
                        ProductId = pc.ProductId
                    }).ToList()
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} products by IDs (batch query)", products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by IDs");
            return new List<ProductDTO>();
        }
    }
}



