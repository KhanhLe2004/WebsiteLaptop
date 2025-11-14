using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageCustomerController : Controller
    {
        // GET: Admin/Customer
        public IActionResult ManageCustomer()
        {
            return View();
        }
    }
}
