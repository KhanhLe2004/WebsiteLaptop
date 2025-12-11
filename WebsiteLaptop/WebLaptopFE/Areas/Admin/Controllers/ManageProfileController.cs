using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageProfileController : BaseAdminController
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public ManageProfileController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy EmployeeId từ session (đã được kiểm tra bởi BaseAdminController)
                var employeeId = HttpContext.Session.GetString("EmployeeId");
                if (string.IsNullOrEmpty(employeeId))
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem thông tin cá nhân";
                    return RedirectToAction("Index", "SignIn", new { area = "Admin" });
                }

                // Lấy base URL của API từ configuration
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5068";
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(apiBaseUrl);

                // Gọi API để lấy thông tin nhân viên
                var response = await client.GetAsync($"/api/ManageProfileAPI/{employeeId}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var employee = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Lưu thông tin vào ViewBag để hiển thị
                    ViewBag.Employee = employee;
                    ViewBag.EmployeeJson = responseContent; // Lưu JSON string để dùng trong view
                    ViewBag.EmployeeId = employeeId;
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tải thông tin cá nhân";
                }

                return View("~/Areas/Admin/Views/AllViews/ManageProfile.cshtml");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return View("~/Areas/Admin/Views/AllViews/ManageProfile.cshtml");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string employeeName,
            string? dateOfBirth,
            string? phoneNumber,
            string? email,
            string? username,
            string? provinceCode,
            string? communeCode,
            string? addressDetail,
            IFormFile? avatarFile,
            bool? avatarToDelete)
        {
            try
            {
                // Lấy EmployeeId từ session (đã được kiểm tra bởi BaseAdminController)
                var employeeId = HttpContext.Session.GetString("EmployeeId");
                if (string.IsNullOrEmpty(employeeId))
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để cập nhật thông tin";
                    return RedirectToAction("Index", "SignIn", new { area = "Admin" });
                }

                if (string.IsNullOrWhiteSpace(employeeName))
                {
                    TempData["ErrorMessage"] = "Tên nhân viên là bắt buộc";
                    return RedirectToAction("Index");
                }

                // Lấy base URL của API từ configuration
                var apiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5068";
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(apiBaseUrl);

                // Tạo FormData để gửi file
                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(employeeName), "EmployeeName");

                if (!string.IsNullOrWhiteSpace(dateOfBirth))
                {
                    formData.Add(new StringContent(dateOfBirth), "DateOfBirth");
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    formData.Add(new StringContent(phoneNumber), "PhoneNumber");
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    formData.Add(new StringContent(email), "Email");
                }

                if (!string.IsNullOrWhiteSpace(username))
                {
                    formData.Add(new StringContent(username), "Username");
                }

                if (!string.IsNullOrWhiteSpace(provinceCode))
                {
                    formData.Add(new StringContent(provinceCode), "ProvinceCode");
                }

                if (!string.IsNullOrWhiteSpace(communeCode))
                {
                    formData.Add(new StringContent(communeCode), "CommuneCode");
                }

                if (!string.IsNullOrWhiteSpace(addressDetail))
                {
                    formData.Add(new StringContent(addressDetail), "AddressDetail");
                }

                if (avatarToDelete == true)
                {
                    formData.Add(new StringContent("true"), "AvatarToDelete");
                }

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var fileContent = new StreamContent(avatarFile.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(avatarFile.ContentType);
                    formData.Add(fileContent, "AvatarFile", avatarFile.FileName);
                }

                // Gọi API để cập nhật thông tin
                var response = await client.PutAsync($"/api/ManageProfileAPI/{employeeId}", formData);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    // Cập nhật session nếu có thay đổi
                    if (root.TryGetProperty("employeeName", out var employeeNameElement))
                    {
                        HttpContext.Session.SetString("EmployeeName", employeeNameElement.GetString() ?? "");
                    }

                    if (root.TryGetProperty("email", out var emailElement))
                    {
                        HttpContext.Session.SetString("Email", emailElement.GetString() ?? "");
                    }

                    if (root.TryGetProperty("username", out var usernameElement))
                    {
                        HttpContext.Session.SetString("Username", usernameElement.GetString() ?? "");
                    }

                    if (root.TryGetProperty("avatar", out var avatarElement))
                    {
                        HttpContext.Session.SetString("Avatar", avatarElement.GetString() ?? "");
                    }

                    TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công";
                }
                else
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        if (jsonDoc.RootElement.TryGetProperty("message", out var messageElement))
                        {
                            TempData["ErrorMessage"] = messageElement.GetString() ?? "Cập nhật thông tin thất bại";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Cập nhật thông tin thất bại";
                        }
                    }
                    catch
                    {
                        TempData["ErrorMessage"] = "Cập nhật thông tin thất bại";
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
    }
}

