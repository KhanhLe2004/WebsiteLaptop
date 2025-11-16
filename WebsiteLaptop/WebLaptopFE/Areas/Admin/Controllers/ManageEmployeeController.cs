using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageEmployeeController : Controller
    {
        public IActionResult Index()
        {
            return View("ManageEmployee");
        }
    }
}
