using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ForgetPasswordAPIController : ControllerBase
    {
        private readonly Testlaptop33Context _context;
        private readonly IConfiguration _configuration;

        public ForgetPasswordAPIController(Testlaptop33Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/ForgetPasswordAPI
        [HttpPost]
        public async Task<ActionResult<ForgetPasswordResponseDTO>> ForgetPassword([FromBody] ForgetPasswordRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ForgetPasswordResponseDTO
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ"
                    });
                }

                // Tìm nhân viên theo email
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email != null && e.Email.ToLower() == request.Email.ToLower());

                // Luôn trả về thành công để bảo mật (không tiết lộ email có tồn tại hay không)
                if (employee == null)
                {
                    return Ok(new ForgetPasswordResponseDTO
                    {
                        Success = true,
                        Message = "Nếu email tồn tại trong hệ thống, mật khẩu mới đã được gửi đến email của bạn."
                    });
                }

                // Kiểm tra tài khoản có đang active không
                if (employee.Active != true)
                {
                    return Ok(new ForgetPasswordResponseDTO
                    {
                        Success = true,
                        Message = "Nếu email tồn tại trong hệ thống, mật khẩu mới đã được gửi đến email của bạn."
                    });
                }

                // Tạo mật khẩu mới ngẫu nhiên
                string newPassword = GenerateRandomPassword();

                // Cập nhật mật khẩu mới vào database
                employee.Password = newPassword;
                await _context.SaveChangesAsync();

                // Gửi email chứa mật khẩu mới
                bool emailSent = await SendPasswordResetEmail(employee.Email!, employee.EmployeeName ?? "Nhân viên", newPassword);

                if (!emailSent)
                {
                    // Nếu gửi email thất bại, vẫn trả về thành công để bảo mật
                    return Ok(new ForgetPasswordResponseDTO
                    {
                        Success = true,
                        Message = "Nếu email tồn tại trong hệ thống, mật khẩu mới đã được gửi đến email của bạn."
                    });
                }

                return Ok(new ForgetPasswordResponseDTO
                {
                    Success = true,
                    Message = "Mật khẩu mới đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư."
                });
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng vẫn trả về thành công để bảo mật
                Console.WriteLine($"Error in ForgetPassword: {ex.Message}");
                return Ok(new ForgetPasswordResponseDTO
                {
                    Success = true,
                    Message = "Nếu email tồn tại trong hệ thống, mật khẩu mới đã được gửi đến email của bạn."
                });
            }
        }

        // Hàm tạo mật khẩu ngẫu nhiên (6 ký tự)
        private string GenerateRandomPassword(int length = 6)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var password = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                password.Append(validChars[random.Next(validChars.Length)]);
            }

            return password.ToString();
        }

        // Hàm gửi email
        private async Task<bool> SendPasswordResetEmail(string toEmail, string employeeName, string newPassword)
        {
            try
            {
                // Lấy cấu hình email từ appsettings.json
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:Username"] ?? "";
                var smtpPassword = _configuration["EmailSettings:Password"] ?? "";
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "Hệ thống quản lý laptop";

                // Nếu không có cấu hình email, chỉ log và trả về false
                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    Console.WriteLine("Email configuration not found. Password reset email not sent.");
                    Console.WriteLine($"New password for {toEmail}: {newPassword}");
                    return false;
                }

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                    var emailBodyBuilder = new StringBuilder();
                    emailBodyBuilder.AppendLine("Xin chào " + employeeName + ",");
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
                    emailBodyBuilder.AppendLine("Hệ thống quản lý laptop");
                    var emailBody = emailBodyBuilder.ToString();

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Khôi phục mật khẩu - Hệ thống quản lý laptop",
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
                Console.WriteLine($"Error sending email: {ex.Message}");
                Console.WriteLine($"New password for {toEmail}: {newPassword}");
                return false;
            }
        }
    }
}
