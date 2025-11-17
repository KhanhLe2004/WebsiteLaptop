using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WebLaptopBE.Models;

namespace WebLaptopBE.Controllers
{
    [Route("api/FogetPasssword")]
    [ApiController]
    public class FogetPassswordAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

        [HttpPost]
        public IActionResult SendReset([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email không được để trống" });
                }

                var email = request.Email.Trim().ToLowerInvariant();
                var customer = _db.Customers.FirstOrDefault(c => c.Email == email);

                if (customer == null)
                {
                    // Trả về thông báo chung để đảm bảo bảo mật
                    return Ok(new
                    {
                        message = "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu"
                    });
                }

                // TODO: Gửi email thực tế
                return Ok(new
                {
                    message = "Liên kết đặt lại mật khẩu đã được gửi tới email của bạn (mô phỏng)"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Lỗi khi xử lý yêu cầu",
                    error = ex.Message
                });
            }
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }
    }
}