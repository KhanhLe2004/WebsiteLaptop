using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageStockExportController : BaseAdminController
    {
        // GET: Admin/ManageStockExport
        public IActionResult Index()
        {
            return View("ManageStockExport");
        }
    }
}
