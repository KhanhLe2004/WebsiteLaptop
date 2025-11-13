using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListProductAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();
        
        [HttpGet("all")]
        public IActionResult GetAllProducts()
        {
            var lstSanPham = _db.Products
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
                var product = _db.Products
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

                var images = _db.ProductImages
                    .AsNoTracking()
                    .Where(pi => pi.ProductId == id)
                    .Select(pi => new
                    {
                        pi.ImageId,
                        pi.ProductId
                    })
                    .ToList();

                var configurations = _db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.ProductId == id)
                    .Select(pc => new
                    {
                        pc.ConfigurationId,
                        pc.Cpu,
                        pc.Ram,
                        pc.Rom,
                        pc.Card,
                        pc.Price,
                        pc.ProductId,
                        pc.Quantity
                    })
                    .ToList();

                var reviews = _db.ProductReviews
                    .AsNoTracking()
                    .Include(pr => pr.Customer)
                    .Where(pr => pr.ProductId == id)
                    .OrderByDescending(pr => pr.Time)
                    .Select(pr => new
                    {
                        pr.ProductReviewId,
                        pr.Rate,
                        pr.ContentDetail,
                        pr.Time,
                        pr.CustomerId,
                        Customer = pr.Customer != null ? new
                        {
                            pr.Customer.CustomerId,
                            pr.Customer.CustomerName,
                            pr.Customer.Avatar
                        } : null
                    })
                    .ToList();

                var averageRating = reviews.Any() ? reviews.Average(r => r.Rate ?? 0) : 0;

                var result = new
                {
                    product,
                    images,
                    configurations,
                    reviews,
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
