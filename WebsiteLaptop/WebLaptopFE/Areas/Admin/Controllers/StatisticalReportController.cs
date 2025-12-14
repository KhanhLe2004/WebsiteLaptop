using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StatisticalReportController : BaseAdminController
    {
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/AllViews/StatisticalReport.cshtml");
        }
    }
}
