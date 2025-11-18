using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ForgetPasswordController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ForgetPasswordController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View("ForgetPassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgetPassword(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập email";
                    return View("ForgetPassword");
                }

                // Lấy base URL của API từ configuration
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5068";
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(apiBaseUrl);

                // Tạo request body
                var requestBody = new
                {
                    email = email
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Gọi API quên mật khẩu
                var response = await client.PostAsync("/api/ForgetPasswordAPI", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
                var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;

                if (response.IsSuccessStatusCode && success)
                {
                    TempData["SuccessMessage"] = message ?? "Mật khẩu mới đã được gửi đến email của bạn.";
                    return View("ForgetPassword");
                }
                else
                {
                    TempData["ErrorMessage"] = message ?? "Đã xảy ra lỗi. Vui lòng thử lại.";
                    return View("ForgetPassword");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View("ForgetPassword");
            }
        }
    }
}
