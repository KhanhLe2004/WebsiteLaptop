using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Account()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Register()
        {
            return View();
        }
        public IActionResult ForgetPassword()
        {
            return View();
        }
        
        public IActionResult FacebookCallback()
        {
            return View();
        }
    }
}
