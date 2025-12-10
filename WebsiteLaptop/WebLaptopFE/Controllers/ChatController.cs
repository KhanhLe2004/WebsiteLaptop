using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Controllers;

public class ChatController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

