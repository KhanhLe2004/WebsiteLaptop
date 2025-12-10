using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;
using WebLaptopBE.Data;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductReviewAPIController : ControllerBase
    {
        private readonly Testlaptop35Context _db = new();

        // POST: api/ProductReviewAPI/create
        [HttpPost("create")]
        public IActionResult CreateReview([FromBody] CreateReviewRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                // Kiểm tra CustomerId
                if (string.IsNullOrWhiteSpace(request.CustomerId))
                {
                    return BadRequest(new { message = "Bạn cần đăng nhập để đánh giá sản phẩm" });
                }

                // Kiểm tra ProductId
                if (string.IsNullOrWhiteSpace(request.ProductId))
                {
                    return BadRequest(new { message = "Mã sản phẩm không hợp lệ" });
                }

                // Kiểm tra Rate (1-5)
                if (!request.Rate.HasValue || request.Rate.Value < 1 || request.Rate.Value > 5)
                {
                    return BadRequest(new { message = "Đánh giá phải từ 1 đến 5 sao" });
                }

                // Kiểm tra ContentDetail
                if (string.IsNullOrWhiteSpace(request.ContentDetail))
                {
                    return BadRequest(new { message = "Vui lòng nhập nội dung đánh giá" });
                }

                // Kiểm tra Customer tồn tại và đang hoạt động
                var customer = _db.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c => c.CustomerId == request.CustomerId && 
                                         (c.Active == null || c.Active.Value));
                
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin khách hàng. Vui lòng đăng nhập lại." });
                }

                // Kiểm tra Product tồn tại
                var product = _db.Products
                    .AsNoTracking()
                    .FirstOrDefault(p => p.ProductId == request.ProductId && 
                                         (p.Active == null || p.Active.Value));
                
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                // Generate ProductReviewId
                string reviewId = GenerateProductReviewId();

                // Tạo ProductReview
                var productReview = new ProductReview
                {
                    ProductReviewId = reviewId,
                    CustomerId = request.CustomerId,
                    ProductId = request.ProductId,
                    Rate = request.Rate.Value,
                    ContentDetail = request.ContentDetail.Trim(),
                    Time = DateTime.Now
                };

                // Thêm vào database
                _db.ProductReviews.Add(productReview);
                _db.SaveChanges();

                // Lấy review vừa tạo với thông tin Customer
                var createdReview = _db.ProductReviews
                    .AsNoTracking()
                    .Include(pr => pr.Customer)
                    .Where(pr => pr.ProductReviewId == reviewId)
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
                    .FirstOrDefault();

                return Ok(new
                {
                    message = "Đánh giá đã được gửi thành công!",
                    review = createdReview
                });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lưu đánh giá vào database",
                    error = dbEx.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi tạo đánh giá",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // Helper method để generate ProductReviewId (format: PRV001, PRV002, ...)
        private string GenerateProductReviewId()
        {
            try
            {
                // Lấy tất cả các ID có format PRV001, PRV002... (6 ký tự: PRV + 3 số)
                var lastReview = _db.ProductReviews
                    .Where(pr => pr.ProductReviewId != null && 
                                 pr.ProductReviewId.StartsWith("PRV") && 
                                 pr.ProductReviewId.Length == 6)
                    .OrderByDescending(pr => pr.ProductReviewId)
                    .FirstOrDefault();

                if (lastReview == null)
                {
                    return "PRV001";
                }

                // Lấy phần số sau "PRV"
                var match = Regex.Match(lastReview.ProductReviewId, @"\d+");
                if (match.Success && int.TryParse(match.Value, out int number))
                {
                    return $"PRV{(number + 1):D3}";
                }

                // Nếu không parse được, tìm số lớn nhất
                int maxNumber = 0;
                var allReviews = _db.ProductReviews
                    .Where(pr => pr.ProductReviewId != null && pr.ProductReviewId.StartsWith("PRV"))
                    .Select(pr => pr.ProductReviewId)
                    .ToList();

                foreach (var id in allReviews)
                {
                    var m = Regex.Match(id, @"\d+");
                    if (m.Success && int.TryParse(m.Value, out int num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }

                return $"PRV{(maxNumber + 1):D3}";
            }
            catch
            {
                // Fallback: tìm số lớn nhất và tăng lên
                try
                {
                    int maxNumber = 0;
                    var allReviews = _db.ProductReviews
                        .Where(pr => pr.ProductReviewId != null && pr.ProductReviewId.StartsWith("PRV"))
                        .Select(pr => pr.ProductReviewId)
                        .ToList();

                    foreach (var id in allReviews)
                    {
                        var m = Regex.Match(id, @"\d+");
                        if (m.Success && int.TryParse(m.Value, out int num))
                        {
                            maxNumber = Math.Max(maxNumber, num);
                        }
                    }

                    return $"PRV{(maxNumber + 1):D3}";
                }
                catch
                {
                    // Nếu vẫn lỗi, trả về PRV001
                    return "PRV001";
                }
            }
        }
    }

    // Request model cho CreateReview
    public class CreateReviewRequest
    {
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int? Rate { get; set; }
        public string? ContentDetail { get; set; }
    }
}
