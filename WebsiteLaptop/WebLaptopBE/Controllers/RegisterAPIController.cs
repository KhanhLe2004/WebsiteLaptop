using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using WebLaptopBE.Models;
using WebLaptopBE.Data;
namespace WebLaptopBE.Controllers
{
    [Route("api/Register")]
    [ApiController]
    public class RegisterAPIController : ControllerBase
    {
        private readonly Testlaptop35Context _db = new();

        [HttpPost]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            try
            {
                var validationError = ValidateRequest(request);
                if (!string.IsNullOrEmpty(validationError))
                {
                    return BadRequest(new { message = validationError });
                }

                var email = request.Email!.Trim().ToLowerInvariant();
                var phone = request.Phone!.Trim();

                if (_db.Customers.Any(c => c.Email == email))
                {
                    return Conflict(new { message = "Email đã được đăng ký" });
                }

                if (_db.Customers.Any(c => c.PhoneNumber == phone))
                {
                    return Conflict(new { message = "Số điện thoại đã được đăng ký" });
                }

                var customer = new Customer
                {
                    CustomerId = GenerateCustomerId(),
                    CustomerName = request.FullName!.Trim(),
                    Email = email,
                    PhoneNumber = phone,
                    Address = string.Empty, // Không yêu cầu địa chỉ khi đăng ký
                    Password = request.Password!.Trim(),
                    Username = email,
                    Active = true
                };

                _db.Customers.Add(customer);
                _db.SaveChanges();

                return Ok(new
                {
                    message = "Đăng ký thành công",
                    customer = new
                    {
                        customer.CustomerId,
                        customer.CustomerName,
                        customer.Email,
                        customer.PhoneNumber,
                        customer.Username,
                        customer.Active
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi đăng ký",
                    error = ex.Message
                });
            }
        }

        private static string? ValidateRequest(RegisterRequest request)
        {
            if (request == null)
            {
                return "Dữ liệu đăng ký không hợp lệ";
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return "Họ và tên không được để trống";
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !Regex.IsMatch(request.Email.Trim(),
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase))
            {
                return "Email không hợp lệ";
            }

            if (string.IsNullOrWhiteSpace(request.Phone) || !Regex.IsMatch(request.Phone.Trim(), @"^\d{9,15}$"))
            {
                return "Số điện thoại không hợp lệ";
            }

            if (string.IsNullOrWhiteSpace(request.Password) ||
                request.Password.Length < 6 ||
                request.Password.Length > 20 ||
                !Regex.IsMatch(request.Password, @"^(?=.*[A-Za-z])(?=.*\d).+$"))
            {
                return "Mật khẩu phải từ 6-20 ký tự và bao gồm cả chữ và số";
            }

            return null;
        }

        private string GenerateCustomerId()
        {
            var lastId = _db.Customers
                .OrderByDescending(c => c.CustomerId)
                .Select(c => c.CustomerId)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(lastId) || lastId.Length < 2)
            {
                return "C001";
            }

            var numericPart = lastId.Substring(1);
            if (!int.TryParse(numericPart, out var number))
            {
                number = 0;
            }

            return $"C{number + 1:000}";
        }

        

        public class RegisterRequest
        {
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Password { get; set; }
        }
    }
}
