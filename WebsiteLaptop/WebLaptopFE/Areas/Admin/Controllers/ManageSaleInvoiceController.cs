using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageSaleInvoiceController : Controller
    {
        // GET: Admin/ManageSaleInvoice
        public IActionResult Index()
        {
            return View("ManageSaleInvoice");
        }
    }
}
