using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;

namespace WebLaptopBE.Controllers
{
    [Route("api/Login")]
    [ApiController]
    public class LoginAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

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

                var customer = _db.Customers
                    .AsNoTracking()
                    .FirstOrDefault(c =>
                        (c.Email == credential || c.Username == credential) &&
                        c.Password == password &&
                        (c.Active == null || c.Active.Value));

                if (customer == null)
                {
                    return Unauthorized(new { message = "Email/Tên đăng nhập hoặc mật khẩu không đúng" });
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