using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;
using WebLaptopBE.Data;
namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListProductAPIController : ControllerBase
    {
        private readonly Testlaptop37Context _db = new();
        
        [HttpGet("all")]
        public IActionResult GetAllProducts()
        {
            var lstSanPham = _db.Products
                .AsNoTracking()
                .Include(p => p.Brand)
                .Where(p => p.Active == true)
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
                    .Where(p => p.ProductId == id && p.Active == true)
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

                // Lấy danh sách đơn hàng "Chờ xử lý" và "Đang xử lý"
                var ordersInProgress = _db.SaleInvoices
                    .Where(si => si.Status == "Chờ xử lý" || si.Status == "Đang xử lý")
                    .Select(si => si.SaleInvoiceId)
                    .ToList();

                var configurations = _db.ProductConfigurations
                    .AsNoTracking()
                    .Where(pc => pc.ProductId == id)
                    .ToList()
                    .Select(pc =>
                    {
                        // Tính số lượng có sẵn cho mỗi configuration
                        int stockQuantity = pc.Quantity ?? 0;
                        int orderedQuantity = 0;

                        if (ordersInProgress.Any() && stockQuantity > 0)
                        {
                            // Tạo specifications string từ configuration
                            var specParts = new List<string>();
                            if (!string.IsNullOrWhiteSpace(pc.Cpu)) specParts.Add(pc.Cpu);
                            if (!string.IsNullOrWhiteSpace(pc.Ram)) specParts.Add(pc.Ram);
                            if (!string.IsNullOrWhiteSpace(pc.Rom)) specParts.Add(pc.Rom);
                            if (!string.IsNullOrWhiteSpace(pc.Card)) specParts.Add(pc.Card);
                            string specifications = string.Join(" / ", specParts);

                            // Tính tổng số lượng đã đặt cho configuration này
                            var orderDetails = _db.SaleInvoiceDetails
                                .Where(sid => ordersInProgress.Contains(sid.SaleInvoiceId) &&
                                              sid.ProductId == id)
                                .ToList();

                            foreach (var orderDetail in orderDetails)
                            {
                                string orderSpec = orderDetail.Specifications ?? "";
                                if (specifications == orderSpec)
                                {
                                    orderedQuantity += orderDetail.Quantity ?? 0;
                                }
                            }
                        }

                        int availableQuantity = stockQuantity - orderedQuantity;
                        if (availableQuantity < 0) availableQuantity = 0;

                        return new
                        {
                            pc.ConfigurationId,
                            pc.Cpu,
                            pc.Ram,
                            pc.Rom,
                            pc.Card,
                            pc.Price,
                            pc.ProductId,
                            Quantity = pc.Quantity, // Giữ lại để backward compatibility
                            AvailableQuantity = availableQuantity // Số lượng có sẵn thực tế
                        };
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

        [HttpGet("search")]
        public IActionResult SearchProducts([FromQuery] string keyword, [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return Ok(new List<object>());
                }

                var searchTerm = keyword.Trim().ToLower();
                
                var products = _db.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Include(p => p.ProductConfigurations)
                    .Where(p => p.Active == true &&
                                (p.ProductName != null && p.ProductName.ToLower().Contains(searchTerm) ||
                                 p.ProductModel != null && p.ProductModel.ToLower().Contains(searchTerm) ||
                                 (p.Brand != null && p.Brand.BrandName != null && p.Brand.BrandName.ToLower().Contains(searchTerm))))
                    .OrderBy(p => p.ProductName)
                    .Take(limit)
                    .Select(p => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.ProductModel,
                        p.Avatar,
                        p.SellingPrice,
                        p.OriginalSellingPrice,
                        Brand = p.Brand != null ? new
                        {
                            p.Brand.BrandId,
                            p.Brand.BrandName
                        } : null,
                        // Lấy RAM và Storage từ configurations (lấy giá trị đầu tiên hoặc unique)
                        Ram = p.ProductConfigurations
                            .Where(pc => !string.IsNullOrEmpty(pc.Ram))
                            .Select(pc => pc.Ram)
                            .FirstOrDefault() ?? "",
                        Storage = p.ProductConfigurations
                            .Where(pc => !string.IsNullOrEmpty(pc.Rom))
                            .Select(pc => pc.Rom)
                            .FirstOrDefault() ?? "",
                        // Lấy tất cả các giá trị RAM và Storage unique để filter
                        RamOptions = p.ProductConfigurations
                            .Where(pc => !string.IsNullOrEmpty(pc.Ram))
                            .Select(pc => pc.Ram)
                            .Distinct()
                            .ToList(),
                        StorageOptions = p.ProductConfigurations
                            .Where(pc => !string.IsNullOrEmpty(pc.Rom))
                            .Select(pc => pc.Rom)
                            .Distinct()
                            .ToList()
                    })
                    .ToList();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tìm kiếm sản phẩm", error = ex.Message });
            }
        }
    }
}
