using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageSupplierController : BaseAdminController
    {
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/AllViews/ManageSupplier.cshtml");
        }
    }
}
