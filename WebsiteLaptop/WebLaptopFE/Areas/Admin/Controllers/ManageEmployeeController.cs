using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageEmployeeController : BaseAdminController
    {
        // GET: Admin/ManageEmployee
        public IActionResult Index()
        {
            return View("ManageEmployee");
        }
    }
}
