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
                    .Select(pc => pc.Price!.Value)
                    .ToList();

                var minPrice = priceRange.Any() ? priceRange.Min() : 0;
                var maxPrice = priceRange.Any() ? priceRange.Max() : 0;

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
            [FromQuery] int pageSize = 12)
        {
            try
            {
                // Lấy tất cả ProductConfigurations với filters
                var configQuery = _db.ProductConfigurations
                    .AsNoTracking()
                    .Include(pc => pc.Product)
                    .Where(pc => pc.Product != null);

                // Filter by brand
                if (!string.IsNullOrWhiteSpace(brandIds))
                {
                    var brandIdList = brandIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(b => b.Trim())
                        .ToList();
                    configQuery = configQuery.Where(pc =>
                        pc.Product != null &&
                        pc.Product.BrandId != null &&
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
                        .Select(r => r.Trim().ToUpperInvariant())
                        .ToList();

                    if (ramList.Any())
                    {
                        configQuery = configQuery.Where(pc =>
                            pc.Ram != null &&
                            ramList.Contains(pc.Ram!.ToUpper()));
                    }
                }

                // Filter by Storage
                if (!string.IsNullOrWhiteSpace(storageOptions))
                {
                    var storageList = storageOptions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().ToUpperInvariant())
                        .ToList();

                    if (storageList.Any())
                    {
                        configQuery = configQuery.Where(pc =>
                            pc.Rom != null &&
                            storageList.Any(storage =>
                                EF.Functions.Like(pc.Rom!.ToUpper(), $"%{storage}%")));
                    }
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
    }
}


