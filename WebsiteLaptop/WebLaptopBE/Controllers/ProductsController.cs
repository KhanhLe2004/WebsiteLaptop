using System;
using System.Linq;
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
    }
}

