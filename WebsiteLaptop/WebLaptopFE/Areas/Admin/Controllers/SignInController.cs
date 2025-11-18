using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(responseContent);
                var root = jsonDoc.RootElement;

                var success = root.TryGetProperty("success", out var successElement) ? successElement.GetBoolean() : false;
                var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : null;

                if (response.IsSuccessStatusCode && success && root.TryGetProperty("employee", out var employeeElement))
                {
                    var employee = employeeElement;
                    
                    // Lưu thông tin vào session
                    if (employee.TryGetProperty("employeeId", out var employeeIdElement))
                        HttpContext.Session.SetString("EmployeeId", employeeIdElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("employeeName", out var employeeNameElement))
                        HttpContext.Session.SetString("EmployeeName", employeeNameElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("email", out var emailElement))
                        HttpContext.Session.SetString("Email", emailElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("username", out var usernameElement))
                        HttpContext.Session.SetString("Username", usernameElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("avatar", out var avatarElement))
                        HttpContext.Session.SetString("Avatar", avatarElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("roleId", out var roleIdElement))
                        HttpContext.Session.SetString("RoleId", roleIdElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("roleName", out var roleNameElement))
                        HttpContext.Session.SetString("RoleName", roleNameElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("branchesId", out var branchesIdElement))
                        HttpContext.Session.SetString("BranchesId", branchesIdElement.GetString() ?? "");
                    
                    if (employee.TryGetProperty("branchesName", out var branchesNameElement))
                        HttpContext.Session.SetString("BranchesName", branchesNameElement.GetString() ?? "");

                    TempData["SuccessMessage"] = "Đăng nhập thành công";
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                TempData["ErrorMessage"] = message ?? "Đăng nhập thất bại. Vui lòng thử lại.";
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
