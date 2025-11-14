using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        // GET: Admin/Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
