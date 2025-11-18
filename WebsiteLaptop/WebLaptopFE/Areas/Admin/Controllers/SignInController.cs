using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebLaptopFE.Areas.Admin.Models;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SignInController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SignInController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            // Nếu đã đăng nhập, chuyển hướng về trang chủ admin
            if (HttpContext.Session.GetString("EmployeeId") != null)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            return View("SignIn");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(string usernameOrEmail, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin";
                    return View("SignIn");
                }

                // Lấy base URL của API từ configuration
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5068";
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(apiBaseUrl);

                // Tạo request body
                var requestBody = new
                {
                    usernameOrEmail = usernameOrEmail,
                    password = password,
                    rememberMe = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Gọi API đăng nhập
                var response = await client.PostAsync("/api/SignInAPI", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var signInResponse = JsonSerializer.Deserialize<SignInResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (signInResponse != null && signInResponse.Success && signInResponse.Employee != null)
                    {
                        // Lưu thông tin vào session
                        HttpContext.Session.SetString("EmployeeId", signInResponse.Employee.EmployeeId);
                        HttpContext.Session.SetString("EmployeeName", signInResponse.Employee.EmployeeName ?? "");
                        HttpContext.Session.SetString("Email", signInResponse.Employee.Email ?? "");
                        HttpContext.Session.SetString("Username", signInResponse.Employee.Username ?? "");
                        HttpContext.Session.SetString("Avatar", signInResponse.Employee.Avatar ?? "");
                        HttpContext.Session.SetString("RoleId", signInResponse.Employee.RoleId ?? "");
                        HttpContext.Session.SetString("RoleName", signInResponse.Employee.RoleName ?? "");
                        HttpContext.Session.SetString("BranchesId", signInResponse.Employee.BranchesId ?? "");
                        HttpContext.Session.SetString("BranchesName", signInResponse.Employee.BranchesName ?? "");

                        TempData["SuccessMessage"] = "Đăng nhập thành công";
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                }

                // Đọc error message từ response
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<SignInResponse>(errorContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                TempData["ErrorMessage"] = errorResponse?.Message ?? "Đăng nhập thất bại. Vui lòng thử lại.";
                return View("SignIn");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View("SignIn");
            }
        }

        [HttpPost]
        public IActionResult SignOut()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất thành công";
            return RedirectToAction("Index");
        }
    }
}
