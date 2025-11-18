using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;

namespace WebLaptopBE.Controllers
{
    [Route("api/CustomerAccount")]
    [ApiController]
    public class CustomerAccountAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

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
                    customer.DateOfBirth
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

                var orders = _db.SaleInvoices
                    .AsNoTracking()
                    .Where(si => si.CustomerId == customerId &&
                                 (si.Status == null || !si.Status.ToLower().Contains("hoàn thành")))
                    .OrderByDescending(si => si.TimeCreate)
                    .Select(si => new
                    {
                        si.SaleInvoiceId,
                        si.TimeCreate,
                        si.Status,
                        si.TotalAmount,
                        si.PaymentMethod,
                        si.DeliveryAddress,
                        si.DeliveryFee
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

                var orders = _db.SaleInvoices
                    .AsNoTracking()
                    .Where(si => si.CustomerId == customerId &&
                                 si.Status != null &&
                                 si.Status.ToLower().Contains("hoàn thành"))
                    .OrderByDescending(si => si.TimeCreate)
                    .Select(si => new
                    {
                        si.SaleInvoiceId,
                        si.TimeCreate,
                        si.Status,
                        si.TotalAmount,
                        si.PaymentMethod,
                        si.DeliveryAddress,
                        si.DeliveryFee
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
                        Items = si.SaleInvoiceDetails.Select(d => new
                        {
                            d.SaleInvoiceDetailId,
                            ProductName = d.Product != null ? d.Product.ProductName : "N/A",
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
    }
}