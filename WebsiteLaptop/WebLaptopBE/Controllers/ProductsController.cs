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
            [FromQuery] string? priceRanges, // Comma-separated price ranges like "500-1000,1500-2000"
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
                bool hasPriceFilter = !string.IsNullOrWhiteSpace(priceRanges);
                bool hasRamFilter = !string.IsNullOrWhiteSpace(ramOptions);
                bool hasStorageFilter = !string.IsNullOrWhiteSpace(storageOptions);

                // 1) Build base product set applying brand and sellingPrice filters
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
                    var ranges = priceRanges!
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r =>
                        {
                            var parts = r.Split('-', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2 &&
                                decimal.TryParse(parts[0], out var min) &&
                                decimal.TryParse(parts[1], out var max))
                            {
                                return new { Min = min, Max = max };
                            }
                            return null;
                        })
                        .Where(x => x != null)
                        .ToList();

                    // [trang] - price
                    if (ranges.Any())
                    {
                        // Tạo list sản phẩm thỏa ít nhất một khoảng giá
                        var productIdsMatchingPrice = new List<string>();

                        foreach (var range in ranges)
                        {
                            var matchedIds = _db.Products
                                .AsNoTracking()
                                .Where(p => p.SellingPrice != null && p.SellingPrice >= range!.Min && p.SellingPrice <= range!.Max)
                                .Select(p => p.ProductId!)
                                .ToList();

                            productIdsMatchingPrice.AddRange(matchedIds);
                        }

                        productIdsMatchingPrice = productIdsMatchingPrice.Distinct().ToList();

                        if (!productIdsMatchingPrice.Any())
                        {
                            return Ok(new
                            {
                                products = new List<object>(),
                                totalCount = 0,
                                page = page,
                                pageSize = pageSize,
                                totalPages = 0
                            });
                        }

                        // Chỉ giữ các productId trong filter price
                        productsBaseQuery = productsBaseQuery.Where(p => productIdsMatchingPrice.Contains(p.ProductId!));
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
                            configQuery = configQuery.Where(pc =>
                                pc.Ram != null &&
                                ramList.Contains(pc.Ram!.ToUpper().Replace(" ", "")));
                        }
                    }

                    // Storage filter
                    if (hasStorageFilter)
                    {
                        var requestedStorage = storageOptions!
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();

                        if (requestedStorage.Any())
                        {
                            var candidateConfigs = configQuery
                                .Select(pc => new { pc.ProductId, pc.Rom })
                                .ToList();

                            string normalize(string? v)
                            {
                                if (string.IsNullOrWhiteSpace(v)) return string.Empty;
                                var cleaned = Regex.Replace(v!.Trim(), @"\s+", "");
                                cleaned = cleaned.Replace("/", "").Replace("-", "").ToUpperInvariant();
                                return cleaned;
                            }

                            var normalizedRequested = requestedStorage
                                .Select(s => normalize(s))
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();

                            var matchedProductIds = candidateConfigs
                                .Where(c => !string.IsNullOrWhiteSpace(c.Rom))
                                .Where(c =>
                                {
                                    var romNorm = normalize(c.Rom);
                                    return normalizedRequested.Any(req => romNorm.Contains(req));
                                })
                                .Select(c => c.ProductId!)
                                .Distinct()
                                .ToList();

                            if (!matchedProductIds.Any())
                            {
                                return Ok(new
                                {
                                    products = new List<object>(),
                                    totalCount = 0,
                                    page = page,
                                    pageSize = pageSize,
                                    totalPages = 0
                                });
                            }

                            productIdsFromConfigs = matchedProductIds;
                        }
                        else
                        {
                            productIdsFromConfigs = configQuery
                                .Select(pc => pc.ProductId!)
                                .Distinct()
                                .ToList();
                        }
                    }
                    else
                    {
                        productIdsFromConfigs = configQuery
                            .Select(pc => pc.ProductId!)
                            .Distinct()
                            .ToList();
                    }
                }

                // 3) Determine final product IDs
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
                        SellingPrice = p.SellingPrice ?? 0m,
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

        private static string NormalizeSpecString(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var cleaned = Regex.Replace(s.Trim(), @"\s+", " ");
            cleaned = cleaned.Replace(" ", "").ToUpperInvariant();
            return cleaned;
        }
    }
}
