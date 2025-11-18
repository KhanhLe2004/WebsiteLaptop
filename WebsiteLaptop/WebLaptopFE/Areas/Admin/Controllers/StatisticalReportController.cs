using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StatisticalReportController : Controller
    {
        public IActionResult Index()
        {
            return View("StatisticalReport");
        }
    }
}
