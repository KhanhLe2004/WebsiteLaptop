using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManagePromotionController : Controller
    {
        public IActionResult Index()
        {
            return View("ManagePromotion");
        }
    }
}
