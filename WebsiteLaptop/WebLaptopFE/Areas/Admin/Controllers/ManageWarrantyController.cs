using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageWarrantyController : BaseAdminController
    {
        public IActionResult Index()
        {
            return View("ManageWarranty");
        }
    }
}
