using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageCustomerController : BaseAdminController
    {
        // GET: Admin/ManageCustomer
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/AllViews/ManageCustomer.cshtml");
        }
    }
}
