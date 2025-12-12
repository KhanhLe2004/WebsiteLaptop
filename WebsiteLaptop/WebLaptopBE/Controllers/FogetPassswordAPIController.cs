using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebLaptopBE.Models;
using WebLaptopBE.Data;

namespace WebLaptopBE.Controllers
{
    [Route("api/FogetPasssword")]
    [ApiController]
    public class FogetPassswordAPIController : ControllerBase
    {
        private readonly Testlaptop37Context _db;
        private readonly IConfiguration _configuration;

        public FogetPassswordAPIController(Testlaptop37Context db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> SendReset([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new { message = "Email không được để trống" });
                }

                var email = request.Email.Trim().ToLowerInvariant();
                var customer = await _db.Customers
                    .FirstOrDefaultAsync(c => c.Email != null && c.Email.ToLower() == email);

                // Kiểm tra tài khoản có tồn tại không
                if (customer == null)
                {
                    return NotFound(new
                    {
                        message = "Tài khoản không tồn tại. Vui lòng kiểm tra lại email hoặc đăng ký tài khoản mới."
                    });
                }

                // Kiểm tra tài khoản có đang active không
                if (customer.Active != true)
                {
                    return BadRequest(new
                    {
                        message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ."
                    });
                }

                // Tạo mật khẩu mới ngẫu nhiên
                string newPassword = GenerateRandomPassword();

                // Cập nhật mật khẩu mới vào database
                customer.Password = newPassword;
                await _db.SaveChangesAsync();

                // Gửi email chứa mật khẩu mới
                bool emailSent = await SendPasswordResetEmail(customer.Email!, customer.CustomerName ?? "Khách hàng", newPassword);

                if (!emailSent)
                {
                    // Nếu gửi email thất bại, trả về lỗi
                    return StatusCode(500, new
                    {
                        message = "Không thể gửi email. Vui lòng thử lại sau hoặc liên hệ quản trị viên."
                    });
                }

                return Ok(new
                {
                    message = "Mật khẩu mới đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư."
                });
            }
            catch (Exception ex)
            {
                // Log lỗi và trả về lỗi
                System.Diagnostics.Debug.WriteLine($"Error in SendReset: {ex.Message}");
                return StatusCode(500, new
                {
                    message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau.",
                    error = ex.Message
                });
            }
        }

        // Hàm tạo mật khẩu ngẫu nhiên (6 ký tự)
        private string GenerateRandomPassword(int length = 6)
        {
            const string uppercaseChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lowercaseChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";
            const string specialChars = "!@#$%^&*";
            const string allChars = uppercaseChars + lowercaseChars + numberChars + specialChars;
            
            var random = new Random();
            var password = new StringBuilder(length);

            // Đảm bảo có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt
            password.Append(uppercaseChars[random.Next(uppercaseChars.Length)]);
            password.Append(lowercaseChars[random.Next(lowercaseChars.Length)]);
            password.Append(numberChars[random.Next(numberChars.Length)]);
            password.Append(specialChars[random.Next(specialChars.Length)]);

            // Điền các ký tự còn lại ngẫu nhiên
            for (int i = 4; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Trộn ngẫu nhiên các ký tự trong mật khẩu
            var passwordArray = password.ToString().ToCharArray();
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
            }

            return new string(passwordArray);
        }

        // Hàm gửi email
        private async Task<bool> SendPasswordResetEmail(string toEmail, string customerName, string newPassword)
        {
            try
            {
                // Lấy cấu hình email từ appsettings.json
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:Username"] ?? "";
                var smtpPassword = _configuration["EmailSettings:Password"] ?? "";
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "TenTech";

                // Nếu không có cấu hình email, chỉ log và trả về false
                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    System.Diagnostics.Debug.WriteLine("Email configuration not found. Password reset email not sent.");
                    System.Diagnostics.Debug.WriteLine($"New password for {toEmail}: {newPassword}");
                    return false;
                }

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var emailBodyBuilder = new StringBuilder();
                    emailBodyBuilder.AppendLine("Xin chào " + customerName + ",");
                    emailBodyBuilder.AppendLine("");
                    emailBodyBuilder.AppendLine("Bạn đã yêu cầu khôi phục mật khẩu cho tài khoản của mình.");
                    emailBodyBuilder.AppendLine("");
                    emailBodyBuilder.AppendLine("Mật khẩu mới của bạn là: " + newPassword);
                    emailBodyBuilder.AppendLine("");
                    emailBodyBuilder.AppendLine("Vui lòng đăng nhập và đổi mật khẩu ngay sau khi nhận được email này để đảm bảo an toàn.");
                    emailBodyBuilder.AppendLine("");
                    emailBodyBuilder.AppendLine("Nếu bạn không yêu cầu khôi phục mật khẩu, vui lòng liên hệ với quản trị viên ngay lập tức.");
                    emailBodyBuilder.AppendLine("");
                    emailBodyBuilder.AppendLine("Trân trọng,");
                    emailBodyBuilder.AppendLine("TenTech");
                    var emailBody = emailBodyBuilder.ToString();

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Khôi phục mật khẩu - TenTech Laptop",
                        Body = emailBody,
                        IsBodyHtml = false
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending email: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"New password for {toEmail}: {newPassword}");
                return false;
            }
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }
    }
}