using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerAccountAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

        // GET: api/CustomerAccount/{customerId}
        // Lấy thông tin khách hàng theo CustomerId hoặc Username
        [HttpGet("{identifier}")]
        public IActionResult GetCustomerInfo(string identifier)
        {
            try
            {
                var customer = _db.Customers
                    .AsNoTracking()
                    .Where(c => c.CustomerId == identifier || c.Username == identifier)
                    .Select(c => new
                    {
                        c.CustomerId,
                        c.CustomerName,
                        c.DateOfBirth,
                        c.PhoneNumber,
                        c.Email,
                        c.Address,
                        c.Avatar,
                        c.Username,
                        c.Active
                    })
                    .FirstOrDefault();

                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy thông tin khách hàng",
                    error = ex.Message
                });
            }
        }

        // GET: api/CustomerAccount/{customerId}/orders
        // Lấy danh sách đơn hàng của khách hàng (chưa hoàn thành)
        [HttpGet("{customerId}/orders")]
        public IActionResult GetCustomerOrders(string customerId)
        {
            try
            {
                var orders = _db.SaleInvoices
                    .AsNoTracking()
                    .Where(si => si.CustomerId == customerId && 
                                 si.Status != null && 
                                 !si.Status.ToLower().Contains("hoàn thành"))
                    .OrderByDescending(si => si.TimeCreate)
                    .Select(si => new
                    {
                        si.SaleInvoiceId,
                        si.TimeCreate,
                        si.Status,
                        si.TotalAmount,
                        si.DeliveryAddress,
                        si.DeliveryFee,
                        si.PaymentMethod
                    })
                    .ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy danh sách đơn hàng",
                    error = ex.Message
                });
            }
        }

        // GET: api/CustomerAccount/{customerId}/history
        // Lấy lịch sử mua hàng (đã hoàn thành)
        [HttpGet("{customerId}/history")]
        public IActionResult GetCustomerHistory(string customerId)
        {
            try
            {
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
                        si.DeliveryAddress,
                        si.DeliveryFee,
                        si.PaymentMethod
                    })
                    .ToList();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy lịch sử mua hàng",
                    error = ex.Message
                });
            }
        }

        // GET: api/CustomerAccount/order/{orderId}
        // Lấy chi tiết đơn hàng
        [HttpGet("order/{orderId}")]
        public IActionResult GetOrderDetail(string orderId)
        {
            try
            {
                var order = _db.SaleInvoices
                    .AsNoTracking()
                    .Include(si => si.SaleInvoiceDetails)
                        .ThenInclude(sid => sid.Product)
                    .Where(si => si.SaleInvoiceId == orderId)
                    .Select(si => new
                    {
                        si.SaleInvoiceId,
                        si.TimeCreate,
                        si.Status,
                        si.TotalAmount,
                        si.DeliveryAddress,
                        si.DeliveryFee,
                        si.PaymentMethod,
                        Items = si.SaleInvoiceDetails.Select(sid => new
                        {
                            sid.SaleInvoiceDetailId,
                            ProductName = sid.Product != null ? sid.Product.ProductName : "N/A",
                            sid.Quantity,
                            sid.UnitPrice,
                            sid.Specifications,
                            Subtotal = (sid.Quantity ?? 0) * (sid.UnitPrice ?? 0)
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
                return StatusCode(500, new
                {
                    message = "Lỗi khi lấy chi tiết đơn hàng",
                    error = ex.Message
                });
            }
        }

        // PUT: api/CustomerAccount/{customerId}/profile
        // Cập nhật thông tin khách hàng
        [HttpPut("{customerId}/profile")]
        public IActionResult UpdateCustomerProfile(string customerId, [FromBody] UpdateProfileRequest request)
        {
            try
            {
                var customer = _db.Customers
                    .FirstOrDefault(c => c.CustomerId == customerId);

                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Cập nhật thông tin
                if (!string.IsNullOrWhiteSpace(request.CustomerName))
                    customer.CustomerName = request.CustomerName;

                if (!string.IsNullOrWhiteSpace(request.Email))
                    customer.Email = request.Email;

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    customer.PhoneNumber = request.PhoneNumber;

                if (request.DateOfBirth.HasValue)
                    customer.DateOfBirth = request.DateOfBirth.Value;
                else if (!string.IsNullOrWhiteSpace(request.DateOfBirthString))
                {
                    if (DateOnly.TryParse(request.DateOfBirthString, out var parsedDate))
                        customer.DateOfBirth = parsedDate;
                }

                if (!string.IsNullOrWhiteSpace(request.Address))
                    customer.Address = request.Address;

                if (!string.IsNullOrWhiteSpace(request.Avatar))
                    customer.Avatar = request.Avatar;

                _db.SaveChanges();

                return Ok(new { message = "Cập nhật thông tin thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi cập nhật thông tin",
                    error = ex.Message
                });
            }
        }

        // PUT: api/CustomerAccount/{customerId}/password
        // Đổi mật khẩu
        [HttpPut("{customerId}/password")]
        public IActionResult ChangePassword(string customerId, [FromBody] ChangePasswordRequest request)
        {
            try
            {
                var customer = _db.Customers
                    .FirstOrDefault(c => c.CustomerId == customerId);

                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Kiểm tra mật khẩu hiện tại
                if (customer.Password != request.CurrentPassword)
                {
                    return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
                }

                // Kiểm tra mật khẩu mới và xác nhận
                if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new { message = "Mật khẩu mới và xác nhận không khớp" });
                }

                // Validate mật khẩu mới (tối đa 6 ký tự, gồm chữ và số)
                if (request.NewPassword.Length > 6 || 
                    !request.NewPassword.Any(char.IsLetter) || 
                    !request.NewPassword.Any(char.IsDigit))
                {
                    return BadRequest(new { message = "Mật khẩu mới phải tối đa 6 ký tự và gồm cả chữ và số" });
                }

                customer.Password = request.NewPassword;
                _db.SaveChanges();

                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi đổi mật khẩu",
                    error = ex.Message
                });
            }
        }
    }

    // DTOs
    public class UpdateProfileRequest
    {
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? DateOfBirthString { get; set; } // For JSON deserialization
        public string? Address { get; set; }
        public string? Avatar { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }
}

