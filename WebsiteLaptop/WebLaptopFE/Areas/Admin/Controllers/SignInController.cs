using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SignInController : Controller
    {
        public IActionResult Index()
        {
            // Nếu đã đăng nhập, chuyển hướng về trang chủ admin
            if (HttpContext.Session.GetString("EmployeeId") != null)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return View("SignIn");
        }

        // POST: Admin/SignIn/SetSession
        // Endpoint để set session sau khi đăng nhập thành công qua API
        [HttpPost]
        public IActionResult SetSession([FromBody] JsonElement employeeData)
        {
            try
            {
                if (employeeData.ValueKind == JsonValueKind.Object)
                {
                    // Lưu thông tin vào session
                    if (employeeData.TryGetProperty("employeeId", out var employeeIdElement))
                        HttpContext.Session.SetString("EmployeeId", employeeIdElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("employeeName", out var employeeNameElement))
                        HttpContext.Session.SetString("EmployeeName", employeeNameElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("email", out var emailElement))
                        HttpContext.Session.SetString("Email", emailElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("username", out var usernameElement))
                        HttpContext.Session.SetString("Username", usernameElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("avatar", out var avatarElement))
                        HttpContext.Session.SetString("Avatar", avatarElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("roleId", out var roleIdElement))
                        HttpContext.Session.SetString("RoleId", roleIdElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("roleName", out var roleNameElement))
                        HttpContext.Session.SetString("RoleName", roleNameElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("branchesId", out var branchesIdElement))
                        HttpContext.Session.SetString("BranchesId", branchesIdElement.GetString() ?? "");
                    
                    if (employeeData.TryGetProperty("branchesName", out var branchesNameElement))
                        HttpContext.Session.SetString("BranchesName", branchesNameElement.GetString() ?? "");

                    return Ok(new { success = true, message = "Session đã được thiết lập" });
                }

                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi khi thiết lập session: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SignOut()
        {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất thành công";
            return RedirectToAction("Index");
        }
    }
}
