using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    public class SignInController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
