using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        // GET: Admin/Product
        public IActionResult Index()
        {
            return View();
        }
    }
}

