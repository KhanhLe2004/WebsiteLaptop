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
            return View("~/Areas/Admin/Views/AllViews/ForgetPassword.cshtml");
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
                    return View("~/Areas/Admin/Views/AllViews/ForgetPassword.cshtml");
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

                // Kiểm tra status code và success flag
                if (response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.OK && success)
                {
                    TempData["SuccessMessage"] = message ?? "Mật khẩu mới đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
                    return View("~/Areas/Admin/Views/AllViews/ForgetPassword.cshtml");
                }
                else
                {
                    // Xử lý các trường hợp lỗi (404, 400, 500, etc.)
                    var errorMessage = message ?? "Đã xảy ra lỗi. Vui lòng thử lại.";
                    TempData["ErrorMessage"] = errorMessage;
                    return View("~/Areas/Admin/Views/AllViews/ForgetPassword.cshtml");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Areas/Admin/Views/AllViews/ForgetPassword.cshtml");
            }
        }
    }
}
