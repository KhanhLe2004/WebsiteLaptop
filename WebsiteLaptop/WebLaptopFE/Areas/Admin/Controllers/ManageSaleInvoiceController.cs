using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageSaleInvoiceController : Controller
    {
        public IActionResult ManageSaleInvoice()
        {
            return View();
        }
    }
}
