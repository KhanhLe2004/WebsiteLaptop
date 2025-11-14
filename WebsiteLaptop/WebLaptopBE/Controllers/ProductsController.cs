using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

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
                    var normalized = $"%{productName.Trim()}%";
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

                // Lấy min và max price từ Products.SellingPrice (theo yêu cầu dùng sellingPrice để filter)
                var sellingPrices = _db.Products
                    .AsNoTracking()
                    .Where(p => p.SellingPrice != null && p.SellingPrice > 0)
                    .Select(p => p.SellingPrice!.Value)
                    .ToList();

                var minPrice = sellingPrices.Any() ? sellingPrices.Min() : 0;
                var maxPrice = sellingPrices.Any() ? sellingPrices.Max() : 0;

                var ramList = _db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.Ram != null && pc.Ram.Trim() != string.Empty)
                    .Select(pc => pc.Ram!.Trim())
                    .Distinct()
                    .ToList()
                    .OrderBy(r => ParseCapacityToGb(r))
                    .ThenBy(r => r, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var storageList = _db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.Rom != null && pc.Rom.Trim() != string.Empty)
                    .Select(pc => pc.Rom!.Trim())
                    .Distinct()
                    .ToList()
                    .OrderBy(s => ParseCapacityToGb(s))
                    .ThenBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .ToList();

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
            [FromQuery] int pageSize = 100)
        {
            try
            {
                // Normalize input presence
                bool hasBrandFilter = !string.IsNullOrWhiteSpace(brandIds);
                bool hasPriceFilter = (minPrice.HasValue && minPrice.Value > 0) || (maxPrice.HasValue && maxPrice.Value > 0);
                bool hasRamFilter = !string.IsNullOrWhiteSpace(ramOptions);
                bool hasStorageFilter = !string.IsNullOrWhiteSpace(storageOptions);

                // 1) Build base product set applying brand and sellingPrice filters (since price filter uses SellingPrice)
                var productsBaseQuery = _db.Products.AsNoTracking();

                if (hasBrandFilter)
                {
                    var brandIdList = brandIds!
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(b => b.Trim())
                        .Where(b => !string.IsNullOrEmpty(b))
                        .ToList();

                    if (brandIdList.Any())
                    {
                        productsBaseQuery = productsBaseQuery.Where(p => p.BrandId != null && brandIdList.Contains(p.BrandId));
                    }
                }

                if (hasPriceFilter)
                {
                    if (minPrice.HasValue && minPrice.Value > 0)
                    {
                        productsBaseQuery = productsBaseQuery.Where(p => p.SellingPrice != null && p.SellingPrice >= minPrice.Value);
                    }
                    if (maxPrice.HasValue && maxPrice.Value > 0)
                    {
                        productsBaseQuery = productsBaseQuery.Where(p => p.SellingPrice != null && p.SellingPrice <= maxPrice.Value);
                    }
                }

                // materialize product ids from the base (brand+price) filters
                var productIdsFromProducts = productsBaseQuery
                    .Where(p => p.ProductId != null)
                    .Select(p => p.ProductId!)
                    .Distinct()
                    .ToList();

                // 2) If RAM or Storage filters present, compute productIds that match those config filters
                List<string> productIdsFromConfigs = new List<string>();
                if (hasRamFilter || hasStorageFilter)
                {
                    var configQuery = _db.ProductConfigurations.AsNoTracking().Where(pc => pc.ProductId != null);

                    // RAM filter
                    if (hasRamFilter)
                    {
                        var ramList = ramOptions!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(r => r.Trim().ToUpper().Replace(" ", ""))
                            .Where(r => !string.IsNullOrEmpty(r))
                            .ToList();

                        if (ramList.Any())
                        {
                            // Use normalized comparison using ToUpper().Replace(" ", "")
                            configQuery = configQuery.Where(pc =>
                                pc.Ram != null &&
                                ramList.Contains(pc.Ram!.ToUpper().Replace(" ", "")));
                        }
                    }

                    // Storage filter
                    if (hasStorageFilter)
                    {
                        var storageList = storageOptions!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToUpper().Replace(" ", "").Replace("-", "").Replace("/", ""))
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();

                        if (storageList.Any())
                        {
                            configQuery = configQuery.Where(pc =>
                                pc.Rom != null &&
                                storageList.Any(st =>
                                    EF.Functions.Like(pc.Rom!.ToUpper().Replace(" ", "").Replace("-", "").Replace("/", ""), $"%{st}%")
                                ));
                        }
                    }

                    productIdsFromConfigs = configQuery
                        .Select(pc => pc.ProductId!)
                        .Distinct()
                        .ToList();
                }

                // 3) Determine final product IDs:
                // - If we had both product-based filters and config-based filters, take intersection.
                // - If only one side provided IDs, use that side.
                // - If neither (no filters at all), take all products.
                List<string> finalProductIds;
                bool noFiltersAtAll = !(hasBrandFilter || hasPriceFilter || hasRamFilter || hasStorageFilter);

                if (noFiltersAtAll)
                {
                    finalProductIds = _db.Products
                        .AsNoTracking()
                        .Where(p => p.ProductId != null)
                        .Select(p => p.ProductId!)
                        .Distinct()
                        .ToList();
                }
                else if (productIdsFromProducts.Any() && productIdsFromConfigs.Any())
                {
                    // intersection
                    finalProductIds = productIdsFromProducts.Intersect(productIdsFromConfigs).ToList();
                }
                else if (productIdsFromProducts.Any())
                {
                    finalProductIds = productIdsFromProducts;
                }
                else if (productIdsFromConfigs.Any())
                {
                    finalProductIds = productIdsFromConfigs;
                }
                else
                {
                    // No matching products
                    return Ok(new
                    {
                        products = new List<object>(),
                        totalCount = 0,
                        page = page,
                        pageSize = pageSize,
                        totalPages = 0
                    });
                }

                // 4) Materialize product records for finalProductIds
                var productsList = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Where(p => finalProductIds.Contains(p.ProductId))
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.ProductModel,
                        p.WarrantyPeriod,
                        p.OriginalSellingPrice, // will be used for crossed-out display
                        p.SellingPrice,         // primary price (used for filtering)
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

                // 5) Compute MinConfigPrice lookup (optional, useful if you still want min config price info)
                var minPriceLookup = _db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.ProductId != null && finalProductIds.Contains(pc.ProductId) && pc.Price != null && pc.Price > 0)
                    .GroupBy(pc => pc.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key!,
                        MinPrice = g.Min(pc => pc.Price!.Value)
                    })
                    .ToList()
                    .ToDictionary(x => x.ProductId, x => x.MinPrice);

                // 6) Build final results with MinConfigPrice fallback to SellingPrice
                var finalProducts = productsList.Select(p =>
                {
                    decimal minConfigPrice = 0;
                    if (minPriceLookup.TryGetValue(p.ProductId, out var mp))
                    {
                        minConfigPrice = mp;
                    }
                    else
                    {
                        minConfigPrice = p.SellingPrice ?? 0m;
                    }

                    return new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.ProductModel,
                        p.WarrantyPeriod,
                        OriginalSellingPrice = p.OriginalSellingPrice,
                        SellingPrice = p.SellingPrice ?? 0m,     // main price (used on UI)
                        p.Screen,
                        p.Camera,
                        p.Connect,
                        p.Weight,
                        p.Pin,
                        p.BrandId,
                        p.Avatar,
                        Brand = p.Brand,
                        MinConfigPrice = minConfigPrice
                    };
                }).ToList();

                // 7) Sort finalProducts: by SellingPrice when sortBy == price (as per your request),
                // otherwise price_desc uses SellingPrice desc, name sorts by ProductName.
                switch (sortBy?.ToLower())
                {
                    case "price":
                        finalProducts = finalProducts.OrderBy(x => x.SellingPrice).ToList();
                        break;
                    case "price_desc":
                        finalProducts = finalProducts.OrderByDescending(x => x.SellingPrice).ToList();
                        break;
                    case "name":
                        finalProducts = finalProducts.OrderBy(x => x.ProductName).ToList();
                        break;
                    default:
                        finalProducts = finalProducts.OrderBy(x => x.SellingPrice).ToList();
                        break;
                }

                // If user didn't apply ANY filters, return the FULL list (no paging) as requested.
                if (noFiltersAtAll)
                {
                    var total = finalProducts.Count;
                    return Ok(new
                    {
                        products = finalProducts,
                        totalCount = total,
                        page = 1,
                        pageSize = total,
                        totalPages = 1
                    });
                }

                // 8) Paging in-memory (only when some filters were applied)
                var totalCount = finalProducts.Count;
                var paged = finalProducts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    products = paged,
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

        private static int ParseCapacityToGb(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return int.MaxValue;
            }

            var match = Regex.Match(value, @"(\d+)");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var number))
            {
                return int.MaxValue;
            }

            var upper = value.ToUpperInvariant();
            return upper.Contains("TB") ? number * 1024 : number;
        }

        // helper to normalize spec strings (remove spaces and uppercase)
        private static string NormalizeSpecString(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var cleaned = Regex.Replace(s.Trim(), @"\s+", " ");
            cleaned = cleaned.Replace(" ", "").ToUpperInvariant();
            return cleaned;
        }
    }
}
