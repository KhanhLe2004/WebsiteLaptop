using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using WebLaptopBE.Models;
using WebLaptopBE.Data;
namespace WebLaptopBE.Controllers
{
    [Route("api/CustomerAccount")]
    [ApiController]
    public class CustomerAccountAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _db;
        private readonly IWebHostEnvironment _environment;

        public CustomerAccountAPIController(Testlaptop36Context db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        // GET: api/CustomerAccount/{identifier}
        [HttpGet("{identifier}")]
        public IActionResult GetCustomerInfo(string identifier)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identifier))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                var customer = _db.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c => c.CustomerId == identifier || c.Username == identifier || c.Email == identifier);

                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                return Ok(new
                {
                    customer.CustomerId,
                    customer.CustomerName,
                    customer.Email,
                    customer.PhoneNumber,
                    customer.Address,
                    customer.Username,
                    customer.Avatar,
                    customer.Active,
                    customer.DateOfBirth,
                    // Thêm password để frontend kiểm tra loại đăng nhập (không trả về password thực)
                    IsOAuthLogin = customer.Password == "GOOGLE_OAUTH" || customer.Password == "FACEBOOK_OAUTH"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin khách hàng", error = ex.Message });
            }
        }

        // GET: api/CustomerAccount/{customerId}/orders
        [HttpGet("{customerId}/orders")]
        public IActionResult GetCustomerOrders(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                // Chỉ lấy đơn hàng có trạng thái "Chờ xử lý" hoặc "Đang xử lý"
                var orders = _db.SaleInvoices
                    .AsNoTracking()
                    .Include(si => si.SaleInvoiceDetails)
                        .ThenInclude(d => d.Product)
                    .Where(si => si.CustomerId == customerId &&
                                 si.Status != null &&
                                 (si.Status.Contains("Chờ xử lý") ||
                                  si.Status.Contains("Đang xử lý") ||
                                  si.Status.Contains("Đang vận chuyển")))
                    .OrderByDescending(si => si.TimeCreate)
                    .Select(si => new
                    {
                        si.SaleInvoiceId,
                        si.TimeCreate,
                        si.Status, // Lấy trực tiếp từ database, không thay đổi
                        si.TotalAmount,
                        si.PaymentMethod,
                        si.DeliveryAddress,
                        si.DeliveryFee,
                        si.Discount,
                        Products = si.SaleInvoiceDetails
                            .Where(d => d.Product != null)
                            .OrderBy(d => d.SaleInvoiceDetailId)
                            .Select(d => new
                            {
                                ProductName = d.Product.ProductName ?? "N/A",
                                ProductModel = d.Product.ProductModel ?? "",
                                Avatar = d.Product.Avatar ?? "",
                                Specifications = d.Specifications ?? "",
                                Quantity = d.Quantity ?? 0
                            })
                            .ToList()
                    })
                    .ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách đơn hàng", error = ex.Message });
            }
        }

        // GET: api/CustomerAccount/{customerId}/history
        [HttpGet("{customerId}/history")]
        public IActionResult GetCustomerHistory(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                // Chỉ lấy đơn hàng có trạng thái "Hoàn thành" hoặc "Đã hủy"
                var orders = _db.SaleInvoices
                    .AsNoTracking()
                    .Include(si => si.SaleInvoiceDetails)
                        .ThenInclude(d => d.Product)
                    .Where(si => si.CustomerId == customerId &&
                                 si.Status != null &&
                                 (si.Status.Contains("Hoàn thành") ||
                                  si.Status.Contains("Đã hủy")))
                    .OrderByDescending(si => si.TimeCreate)
                    .Select(si => new
                    {
                        si.SaleInvoiceId,
                        si.TimeCreate,
                        si.Status, // Lấy trực tiếp từ database, không thay đổi
                        si.TotalAmount,
                        si.PaymentMethod,
                        si.DeliveryAddress,
                        si.DeliveryFee,
                        si.Discount,
                        Products = si.SaleInvoiceDetails
                            .Where(d => d.Product != null)
                            .OrderBy(d => d.SaleInvoiceDetailId)
                            .Select(d => new
                            {
                                ProductName = d.Product.ProductName ?? "N/A",
                                ProductModel = d.Product.ProductModel ?? "",
                                Avatar = d.Product.Avatar ?? "",
                                Specifications = d.Specifications ?? "",
                                Quantity = d.Quantity ?? 0
                            })
                            .ToList()
                    })
                    .ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy lịch sử mua hàng", error = ex.Message });
            }
        }

        // GET: api/CustomerAccount/order/{orderId}
        [HttpGet("order/{orderId}")]
        public IActionResult GetOrderDetail(string orderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return BadRequest(new { message = "Mã đơn hàng không hợp lệ" });
                }

                var order = _db.SaleInvoices
                    .AsNoTracking()
                    .Include(si => si.SaleInvoiceDetails)
                        .ThenInclude(d => d.Product)
                    .Where(si => si.SaleInvoiceId == orderId)
                    .Select(si => new
                    {
                        si.SaleInvoiceId,
                        si.TimeCreate,
                        si.Status,
                        si.TotalAmount,
                        si.PaymentMethod,
                        si.DeliveryAddress,
                        si.DeliveryFee,
                        si.Discount,
                        Items = si.SaleInvoiceDetails.Select(d => new
                        {
                            d.SaleInvoiceDetailId,
                            ProductName = d.Product != null ? d.Product.ProductName : "N/A",
                            ProductModel = d.Product != null ? (d.Product.ProductModel ?? "") : "",
                            d.Quantity,
                            d.UnitPrice,
                            d.Specifications,
                            Subtotal = (d.Quantity ?? 0) * (d.UnitPrice ?? 0)
                        }).ToList()
                    })
                    .FirstOrDefault();

                if (order == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy chi tiết đơn hàng", error = ex.Message });
            }
        }

        // PUT: api/CustomerAccount/{customerId}/profile
        [HttpPut("{customerId}/profile")]
        public IActionResult UpdateCustomerProfile(string customerId, [FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                if (request == null)
                {
                    return BadRequest(new { message = "Dữ liệu cập nhật không hợp lệ" });
                }

                var customer = _db.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                if (!string.IsNullOrWhiteSpace(request.Email))
                {
                    var email = request.Email.Trim().ToLowerInvariant();
                    if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    {
                        return BadRequest(new { message = "Email không hợp lệ" });
                    }

                    var emailExists = _db.Customers.Any(c => c.Email == email && c.CustomerId != customerId);
                    if (emailExists)
                    {
                        return Conflict(new { message = "Email đã được sử dụng bởi tài khoản khác" });
                    }

                    customer.Email = email;
                }

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                {
                    var phone = request.PhoneNumber.Trim();
                    if (!Regex.IsMatch(phone, @"^\d{9,15}$"))
                    {
                        return BadRequest(new { message = "Số điện thoại không hợp lệ" });
                    }

                    var phoneExists = _db.Customers.Any(c => c.PhoneNumber == phone && c.CustomerId != customerId);
                    if (phoneExists)
                    {
                        return Conflict(new { message = "Số điện thoại đã được sử dụng" });
                    }

                    customer.PhoneNumber = phone;
                }

                if (!string.IsNullOrWhiteSpace(request.CustomerName))
                {
                    customer.CustomerName = request.CustomerName.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Address))
                {
                    customer.Address = request.Address.Trim();
                }
                else
                {
                    // Nếu address là null hoặc empty, set về null
                    customer.Address = null;
                }

                DateOnly? dob = request.DateOfBirth;
                if (!dob.HasValue && !string.IsNullOrWhiteSpace(request.DateOfBirthString))
                {
                    if (DateOnly.TryParse(request.DateOfBirthString, out var parsed))
                    {
                        dob = parsed;
                    }
                    else
                    {
                        return BadRequest(new { message = "Ngày sinh không hợp lệ" });
                    }
                }
                customer.DateOfBirth = dob;

                if (!string.IsNullOrWhiteSpace(request.Avatar))
                {
                    customer.Avatar = request.Avatar.Trim();
                }

                _db.SaveChanges();
                return Ok(new { message = "Cập nhật thông tin thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật thông tin", error = ex.Message });
            }
        }

        // PUT: api/CustomerAccount/order/{orderId}/cancel
        [HttpPut("order/{orderId}/cancel")]
        public IActionResult CancelOrder(string orderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(orderId))
                {
                    return BadRequest(new { message = "Mã đơn hàng không hợp lệ" });
                }

                var order = _db.SaleInvoices.FirstOrDefault(si => si.SaleInvoiceId == orderId);
                if (order == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng" });
                }

                // Kiểm tra trạng thái hiện tại
                var currentStatus = order.Status?.Trim() ?? "";
               
                
                // Kiểm tra xem đơn hàng đã bị hủy chưa
                if (currentStatus.Contains("Đã hủy"))
                {
                    return BadRequest(new { message = "Đơn hàng này đã bị hủy" });
                }
                
                // Chỉ cho phép hủy khi trạng thái là "Chờ xử lý"
                if (!currentStatus.Contains("Chờ xử lý"))
                {
                    return BadRequest(new { message = "Chỉ có thể hủy đơn hàng khi trạng thái là 'Chờ xử lý'" });
                }

                // Cập nhật trạng thái thành "Đã hủy" và lưu thời gian hủy vào TimeCreate
                // (Sử dụng TimeCreate để lưu thời gian hủy cho đơn hàng đã hủy)
                order.Status = "Đã hủy";
                order.TimeCreate = DateTime.Now; // Cập nhật thời gian hủy
                _db.SaveChanges();

                return Ok(new { message = "Hủy đơn hàng thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi hủy đơn hàng", error = ex.Message });
            }
        }

        // POST: api/CustomerAccount/{customerId}/avatar
        [HttpPost("{customerId}/avatar")]
        public async Task<IActionResult> UploadAvatar(string customerId, IFormFile file)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                var customer = _db.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn file ảnh" });
                }

                if (!file.ContentType.StartsWith("image/"))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file ảnh" });
                }

                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    return BadRequest(new { message = "Kích thước file không được vượt quá 10MB" });
                }

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(customer.Avatar))
                {
                    DeleteImage(customer.Avatar);
                }

                // Lưu ảnh mới
                var fileName = await SaveImageAsync(file);
                customer.Avatar = fileName;
                _db.SaveChanges();

                return Ok(new { message = "Cập nhật avatar thành công", avatar = fileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi upload avatar", error = ex.Message });
            }
        }

        // PUT: api/CustomerAccount/{customerId}/password
        [HttpPut("{customerId}/password")]
        public IActionResult ChangePassword(string customerId, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "CustomerId không hợp lệ" });
                }

                if (request == null ||
                    string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                    string.IsNullOrWhiteSpace(request.NewPassword) ||
                    string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    return BadRequest(new { message = "Vui lòng nhập đầy đủ thông tin mật khẩu" });
                }

                var customer = _db.Customers.FirstOrDefault(c => c.CustomerId == customerId);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                if (!string.Equals(customer.Password, request.CurrentPassword.Trim(), StringComparison.Ordinal))
                {
                    return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
                }

                var newPassword = request.NewPassword.Trim();
                if (newPassword != request.ConfirmPassword.Trim())
                {
                    return BadRequest(new { message = "Mật khẩu mới và xác nhận không khớp" });
                }

                if (newPassword.Length < 6 ||
                    !newPassword.Any(char.IsLetter) ||
                    !newPassword.Any(char.IsDigit))
                {
                    return BadRequest(new { message = "Mật khẩu mới phải tối thiểu 6 ký tự và gồm cả chữ và số" });
                }

                customer.Password = newPassword;
                _db.SaveChanges();

                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đổi mật khẩu", error = ex.Message });
            }
        }

        public class UpdateProfileRequest
        {
            public string? CustomerName { get; set; }
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Address { get; set; }
            public DateOnly? DateOfBirth { get; set; }
            public string? DateOfBirthString { get; set; }
            public string? Avatar { get; set; }
        }

        public class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Helper methods
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty");
                }

                var fileExtension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    fileExtension = file.ContentType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/gif" => ".gif",
                        "image/webp" => ".webp",
                        _ => ".jpg"
                    };
                }

                const int maxTotalLength = 20;
                var extLength = fileExtension.Length;
                var baseNameMaxLength = Math.Max(1, maxTotalLength - extLength);

                string GenerateRandomBaseName(int length)
                {
                    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    var rnd = new Random();
                    var buffer = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        buffer[i] = chars[rnd.Next(chars.Length)];
                    }
                    return new string(buffer);
                }

                var baseName = GenerateRandomBaseName(baseNameMaxLength);
                var fileName = $"{baseName}{fileExtension}";
                
                var webRootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                var uploadsFolder = Path.Combine(webRootPath, "imageCustomers");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu ảnh: {ex.Message}", ex);
            }
        }

        private void DeleteImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "imageCustomers", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch
            {
                // Không throw exception nếu xóa file thất bại
            }
        }
    }
}