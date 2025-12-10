using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;
using WebLaptopBE.Data;
namespace WebLaptopBE.Controllers
{
    [Route("api/Login")]
    [ApiController]
    public class LoginAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _db = new();

        [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (request == null ||
                    string.IsNullOrWhiteSpace(request.EmailOrUsername) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { message = "Email/Tên đăng nhập và mật khẩu không được để trống" });
                }

                var credential = request.EmailOrUsername.Trim();
                var password = request.Password.Trim();

                if (password.Length > 50)
                {
                    return BadRequest(new { message = "Mật khẩu không hợp lệ" });
                }

                // Tìm customer theo email hoặc username (không kiểm tra Active ở đây)
                var customer = _db.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c =>
                        (c.Email == credential || c.Username == credential));

                if (customer == null)
                {
                    return Unauthorized(new { message = "Email/Tên đăng nhập hoặc mật khẩu không đúng" });
                }

                // Kiểm tra mật khẩu
                if (customer.Password != password)
                {
                    return Unauthorized(new { message = "Email/Tên đăng nhập hoặc mật khẩu không đúng" });
                }

                // Kiểm tra trạng thái Active
                if (customer.Active == false)
                {
                    return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa" });
                }

                return Ok(new
                {
                    message = "Đăng nhập thành công",
                    customer = new
                    {
                        customer.CustomerId,
                        customer.CustomerName,
                        customer.Email,
                        customer.PhoneNumber,
                        customer.Address,
                        customer.Username,
                        customer.Avatar,
                        customer.Active
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi đăng nhập",
                    error = ex.Message
                });
            }
        }

        public class LoginRequest
        {
            public string EmailOrUsername { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}