using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListProductAPIController : ControllerBase
    {
        private Testlaptop20Context db = new Testlaptop20Context();
        
        [HttpGet("all")]
        public IActionResult GetAllProducts()
        {
            var lstSanPham = db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
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
            return Ok(lstSanPham);
        }

        [HttpGet("detail/{id}")]
        public IActionResult GetProductDetail(string id)
        {
            try
            {
                var product = db.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Where(p => p.ProductId == id)
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
                    .FirstOrDefault();

                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                var images = db.ProductImages
                    .AsNoTracking()
                    .Where(pi => pi.ProductId == id)
                    .Select(pi => new
                    {
                        pi.ImageId,
                        pi.ProductId
                    })
                    .ToList();

                var configurations = db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.ProductId == id)
                    .Select(pc => new
                    {
                        pc.ConfigurationId,
                        pc.Specifications,
                        pc.Price,
                        pc.ProductId,
                        pc.Quantity
                    })
                    .ToList();

                var reviews = db.ProductReviews
                    .AsNoTracking()
                    .Where(pr => pr.ProductId == id)
                    .OrderByDescending(pr => pr.Time)
                    .Select(pr => new
                    {
                        pr.Username,
                        pr.Rate,
                        pr.ContentDetail,
                        pr.Time,
                        pr.ProductId
                    })
                    .ToList();

                var averageRating = reviews.Any() ? reviews.Average(r => r.Rate ?? 0) : 0;

                var result = new
                {
                    product = product,
                    images = images,
                    configurations = configurations,
                    reviews = reviews,
                    averageRating = Math.Round(averageRating, 1),
                    totalReviews = reviews.Count
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin sản phẩm", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
}
