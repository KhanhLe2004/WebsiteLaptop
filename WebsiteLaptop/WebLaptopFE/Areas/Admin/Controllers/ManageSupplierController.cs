using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageSupplierController : Controller
    {
        public IActionResult Index()
        {
            return View("ManageSupplier");
        }
    }
}
