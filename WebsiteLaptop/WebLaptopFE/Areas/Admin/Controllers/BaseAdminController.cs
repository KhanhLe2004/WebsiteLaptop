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

            base.OnActionExecuting(context);
        }
    }
}

