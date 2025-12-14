using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // Kiểm tra nếu đã đăng nhập, chuyển đến Dashboard
            if (HttpContext.Session.GetString("EmployeeId") != null)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            
            // Chưa đăng nhập, chuyển đến trang đăng nhập
            return RedirectToAction("Index", "SignIn", new { area = "Admin" });
        }

    }
}

