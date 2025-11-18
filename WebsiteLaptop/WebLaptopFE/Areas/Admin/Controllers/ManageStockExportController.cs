using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageStockExportController : Controller
    {
        // GET: Admin/ManageStockExport
        public IActionResult Index()
        {
            return View("ManageStockExport");
        }
    }
}
