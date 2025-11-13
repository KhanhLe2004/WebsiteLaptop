using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly Testlaptop20Context _db;

        public ProductsController()
        {
            _db = new Testlaptop20Context();
        }

        // GET /api/products/category/{brandId}?productName={productName}
        [HttpGet("category/{brandId}")]
        public IActionResult GetByCategory(string brandId, [FromQuery] string? productName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(brandId))
                {
                    return BadRequest(new { message = "brandId is required" });
                }

                var query = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Where(p => p.BrandId == brandId);

                if (!string.IsNullOrWhiteSpace(productName))
                {
                    var normalized = productName.Trim();
                    query = query.Where(p => p.ProductName != null &&
                                             EF.Functions.Like(p.ProductName, normalized));
                }

                var products = query
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.ProductModel,
                        p.WarrantyPeriod,
                        p.OriginalSellingPrice,
                        p.SellingPrice,
                        p.Screen,
                        p.Camera,
                        p.Connect,
                        p.Weight,
                        p.Pin,
                        p.BrandId,
                        p.Avatar,
                        Brand = p.Brand != null ? new
                        {
                            p.Brand.BrandId,
                            p.Brand.BrandName
                        } : null
                    })
                    .ToList();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách sản phẩm theo danh mục",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // GET /api/products/shop/filters - Lấy danh sách filters (brands, price range, RAM, storage)
        [HttpGet("shop/filters")]
        public IActionResult GetShopFilters()
        {
            try
            {
                // Lấy tất cả brands
                var brands = _db.Brands
                    .AsNoTracking()
                    .Select(b => new
                    {
                        b.BrandId,
                        b.BrandName
                    })
                    .OrderBy(b => b.BrandName)
                    .ToList();

                // Lấy min và max price từ ProductConfigurations
                var priceRange = _db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.Price != null && pc.Price > 0)
                    .Select(pc => pc.Price)
                    .ToList();

                var minPrice = priceRange.Any() ? (decimal)priceRange.Min() : 0;
                var maxPrice = priceRange.Any() ? (decimal)priceRange.Max() : 0;

                // Lấy tất cả specifications để parse RAM và Storage
                var allSpecs = _db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.Specifications != null && pc.Specifications.Trim() != "")
                    .Select(pc => pc.Specifications)
                    .ToList();

                var ramSet = new HashSet<string>();
                var storageSet = new HashSet<string>();

                foreach (var spec in allSpecs)
                {
                    if (string.IsNullOrWhiteSpace(spec)) continue;

                    // Parse RAM - tìm pattern như "8GB", "16GB", "32GB" (chỉ lấy RAM, không lấy storage GB)
                    // Tránh match với storage như "512GB SSD" - chỉ lấy khi không có SSD/HDD sau đó
                    var ramMatches = Regex.Matches(spec, @"\b(\d+)\s*GB\b(?!\s*(?:SSD|HDD|NVMe|M\.2))", RegexOptions.IgnoreCase);
                    foreach (Match ramMatch in ramMatches)
                    {
                        if (ramMatch.Success)
                        {
                            var ramValue = ramMatch.Groups[1].Value + "GB";
                            // Chỉ thêm nếu không phải là storage (thường RAM < 128GB)
                            if (int.TryParse(ramMatch.Groups[1].Value, out var ramSize) && ramSize <= 128)
                            {
                                ramSet.Add(ramValue);
                            }
                        }
                    }

                    // Parse Storage - tìm pattern như "256GB SSD", "512GB SSD", "1TB HDD", etc.
                    var storageMatches = Regex.Matches(spec, @"\b(\d+)\s*(GB|TB)\s*(SSD|HDD|NVMe|M\.2|M2)\b", RegexOptions.IgnoreCase);
                    foreach (Match storageMatch in storageMatches)
                    {
                        if (storageMatch.Success)
                        {
                            var storageValue = storageMatch.Groups[1].Value + storageMatch.Groups[2].Value;
                            var storageType = storageMatch.Groups[3].Value.ToUpper();
                            if (storageType == "M2" || storageType == "M.2")
                            {
                                storageType = "M.2";
                            }
                            storageSet.Add(storageValue + " " + storageType);
                        }
                    }
                    
                    // Cũng tìm storage không có type (chỉ GB/TB lớn hơn 128GB)
                    var storageWithoutTypeMatches = Regex.Matches(spec, @"\b(1[3-9]\d|[2-9]\d{2}|\d{4,})\s*(GB|TB)\b(?!\s*(?:SSD|HDD|NVMe|M\.2|M2))", RegexOptions.IgnoreCase);
                    foreach (Match storageMatch in storageWithoutTypeMatches)
                    {
                        if (storageMatch.Success)
                        {
                            var storageValue = storageMatch.Groups[1].Value + storageMatch.Groups[2].Value;
                            storageSet.Add(storageValue);
                        }
                    }
                }

                var ramList = ramSet.OrderBy(r =>
                {
                    var num = Regex.Match(r, @"\d+").Value;
                    return int.TryParse(num, out var n) ? n : 0;
                }).ToList();

                var storageList = storageSet.OrderBy(s =>
                {
                    var match = Regex.Match(s, @"(\d+)\s*(GB|TB)");
                    if (match.Success)
                    {
                        var num = int.Parse(match.Groups[1].Value);
                        var unit = match.Groups[2].Value.ToUpper();
                        return unit == "TB" ? num * 1024 : num; // Convert TB to GB for sorting
                    }
                    return 0;
                }).ToList();

                return Ok(new
                {
                    brands = brands,
                    priceRange = new
                    {
                        min = minPrice,
                        max = maxPrice
                    },
                    ramOptions = ramList,
                    storageOptions = storageList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách filters",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // GET /api/products/shop/search - Tìm kiếm sản phẩm với filters
        [HttpGet("shop/search")]
        public IActionResult SearchProducts(
            [FromQuery] string? brandIds, // Comma-separated brand IDs
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? ramOptions, // Comma-separated RAM values like "8GB,16GB"
            [FromQuery] string? storageOptions, // Comma-separated storage values
            [FromQuery] string? sortBy = "price", // price, price_desc, name
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                // Lấy tất cả ProductConfigurations với filters
                var configQuery = _db.ProductConfigurations
                    .AsNoTracking()
                    .Include(pc => pc.Product)
                        .ThenInclude(p => p.Brand)
                    .Where(pc => pc.Product != null);

                // Filter by brand
                if (!string.IsNullOrWhiteSpace(brandIds))
                {
                    var brandIdList = brandIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(b => b.Trim())
                        .ToList();
                    configQuery = configQuery.Where(pc => pc.Product != null && 
                        brandIdList.Contains(pc.Product.BrandId));
                }

                // Filter by price
                if (minPrice.HasValue && minPrice.Value > 0)
                {
                    configQuery = configQuery.Where(pc => pc.Price >= minPrice.Value);
                }
                if (maxPrice.HasValue && maxPrice.Value > 0)
                {
                    configQuery = configQuery.Where(pc => pc.Price <= maxPrice.Value);
                }

                // Filter by RAM
                if (!string.IsNullOrWhiteSpace(ramOptions))
                {
                    var ramList = ramOptions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList();
                    
                    configQuery = configQuery.Where(pc => 
                        pc.Specifications != null &&
                        ramList.Any(ram => pc.Specifications.Contains(ram, StringComparison.OrdinalIgnoreCase)));
                }

                // Filter by Storage
                if (!string.IsNullOrWhiteSpace(storageOptions))
                {
                    var storageList = storageOptions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList();
                    
                    configQuery = configQuery.Where(pc => 
                        pc.Specifications != null &&
                        storageList.Any(storage => 
                            pc.Specifications.Contains(storage, StringComparison.OrdinalIgnoreCase) ||
                            pc.Specifications.Contains(storage.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)));
                }

                // Get distinct products from configurations
                var productIds = configQuery.Select(pc => pc.ProductId).Distinct().ToList();

                // Get products with their min price from configurations
                var productsQuery = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Where(p => productIds.Contains(p.ProductId))
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.ProductModel,
                        p.WarrantyPeriod,
                        p.OriginalSellingPrice,
                        p.SellingPrice,
                        p.Screen,
                        p.Camera,
                        p.Connect,
                        p.Weight,
                        p.Pin,
                        p.BrandId,
                        p.Avatar,
                        Brand = p.Brand != null ? new
                        {
                            p.Brand.BrandId,
                            p.Brand.BrandName
                        } : null,
                        MinConfigPrice = _db.ProductConfigurations
                            .Where(pc => pc.ProductId == p.ProductId && pc.Price != null && pc.Price > 0)
                            .Select(pc => pc.Price)
                            .DefaultIfEmpty(p.SellingPrice ?? 0)
                            .Min()
                    });

                // Sort
                switch (sortBy?.ToLower())
                {
                    case "price":
                        productsQuery = productsQuery.OrderBy(p => p.MinConfigPrice);
                        break;
                    case "price_desc":
                        productsQuery = productsQuery.OrderByDescending(p => p.MinConfigPrice);
                        break;
                    case "name":
                        productsQuery = productsQuery.OrderBy(p => p.ProductName);
                        break;
                    default:
                        productsQuery = productsQuery.OrderBy(p => p.MinConfigPrice);
                        break;
                }

                var totalCount = productsQuery.Count();
                var products = productsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    products = products,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi tìm kiếm sản phẩm",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}

