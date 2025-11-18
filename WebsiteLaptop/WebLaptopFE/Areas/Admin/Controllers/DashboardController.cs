using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : BaseAdminController
    {
        // GET: Admin/Dashboard
        public IActionResult Index()
        {
            return View("Dashboard");
        }
    }
}
