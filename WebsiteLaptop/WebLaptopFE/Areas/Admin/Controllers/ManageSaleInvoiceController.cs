using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageSaleInvoiceController : BaseAdminController
    {
        // GET: Admin/ManageSaleInvoice
        public IActionResult Index()
        {
            // Lấy EmployeeId từ session để truyền vào view
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            var employeeName = HttpContext.Session.GetString("EmployeeName");
            
            ViewBag.EmployeeId = employeeId ?? "";
            ViewBag.EmployeeName = employeeName ?? "";
            
            return View("ManageSaleInvoice");
        }
    }
}
