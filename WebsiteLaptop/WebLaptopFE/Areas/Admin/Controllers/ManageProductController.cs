using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageProductController : BaseAdminController
    {
        // GET: Admin/ManageProduct
        public IActionResult Index()
        {
            return View("ManageProduct");
        }
    }
}

