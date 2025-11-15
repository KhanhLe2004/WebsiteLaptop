using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageStockImportController : Controller
    {
        public IActionResult ManageStockImport()
        {
            return View();
        }
    }
}
