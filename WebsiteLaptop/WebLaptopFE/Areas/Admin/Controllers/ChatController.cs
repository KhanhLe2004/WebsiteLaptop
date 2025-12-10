using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChatController : Controller
    {
        public IActionResult Index()
        {
            return View("Chat");
        }
    }
}

