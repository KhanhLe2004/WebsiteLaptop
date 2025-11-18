using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    public class ForgetPasswordController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
