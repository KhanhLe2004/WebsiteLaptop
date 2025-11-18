using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageStockImportController : BaseAdminController
    {
        // GET: Admin/ManageStockImport
        public IActionResult Index()
        {
            return View("ManageStockImport");
        }
    }
}
