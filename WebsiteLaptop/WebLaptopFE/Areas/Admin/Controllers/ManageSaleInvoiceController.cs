using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageSaleInvoiceController : BaseAdminController
    {
        // GET: Admin/ManageSaleInvoice
        public IActionResult Index()
        {
            return View("ManageSaleInvoice");
        }
    }
}
