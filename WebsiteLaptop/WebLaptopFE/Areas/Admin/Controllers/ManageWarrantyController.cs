using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageWarrantyController : Controller
    {
        public IActionResult Index()
        {
            return View("ManageWarranty");
        }
    }
}
