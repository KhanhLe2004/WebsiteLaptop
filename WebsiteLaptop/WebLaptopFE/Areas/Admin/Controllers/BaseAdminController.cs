using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BaseAdminController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Cho phép truy cập SignIn và ForgetPassword mà không cần đăng nhập
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (controllerName == "SignIn" || controllerName == "ForgetPassword" || controllerName == "Home")
            {
                base.OnActionExecuting(context);
                return;
            }

            // Kiểm tra session cho các controller khác
            if (HttpContext.Session.GetString("EmployeeId") == null)
            {
                context.Result = RedirectToAction("Index", "SignIn", new { area = "Admin" });
                return;
            }

            // Lấy RoleId từ session
            var roleId = HttpContext.Session.GetString("RoleId");

            // Lưu RoleId vào ViewBag để sử dụng trong view
            ViewBag.RoleId = roleId;

            // Kiểm tra quyền truy cập
            if (!PermissionHelper.HasPermission(roleId, controllerName))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                context.Result = RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Helper class để kiểm tra quyền truy cập
    /// </summary>
    public static class PermissionHelper
    {
        /// <summary>
        /// Kiểm tra quyền truy cập dựa trên RoleId và Controller name
        /// </summary>
        public static bool HasPermission(string? roleId, string? controllerName)
        {
            // Nếu không có RoleId, từ chối truy cập
            if (string.IsNullOrEmpty(roleId) || string.IsNullOrEmpty(controllerName))
            {
                return false;
            }

            // Dashboard, ManageProfile, History và Chat: Tất cả nhân viên đều có quyền truy cập
            if (controllerName == "Dashboard" || controllerName == "ManageProfile" )
            {
                return true;
            }

            // ADM và CCH: Quyền truy cập tất cả các trang
            if (roleId == "ADM" || roleId == "CCH")
            {
                return true;
            }

            // ST (Nhân viên kho): Quyền truy cập Quản lý nhập hàng và Quản lý xuất hàng
            if (roleId == "ST")
            {
                return controllerName == "ManageStockImport" || controllerName == "ManageStockExport";
            }

            // TE (Kỹ thuật viên): Quyền truy cập Quản lý bảo hành
            if (roleId == "TE")
            {
                return controllerName == "ManageWarranty";
            }

            // SL (Nhân viên bán hàng): Quyền truy cập Quản lý hóa đơn
            if (roleId == "SL")
            {
                return controllerName == "ManageSaleInvoice" || controllerName == "Chat";
            }

            // Các RoleId khác không có quyền truy cập
            return false;
        }
    }
}

