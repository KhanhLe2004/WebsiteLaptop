using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageProductController : Controller
    {
        // GET: Admin/Product
        public IActionResult ManageProduct()
        {
            return View();
        }
    }
}

