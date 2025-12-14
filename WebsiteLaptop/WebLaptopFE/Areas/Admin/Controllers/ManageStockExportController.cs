using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManageStockExportController : BaseAdminController
    {
        // GET: Admin/ManageStockExport
        public IActionResult Index()
        {
            // Lấy EmployeeId từ session để truyền vào view
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            var employeeName = HttpContext.Session.GetString("EmployeeName");
            
            ViewBag.EmployeeId = employeeId;
            ViewBag.EmployeeName = employeeName;
            
            return View("~/Areas/Admin/Views/AllViews/ManageStockExport.cshtml");
        }
    }
}
