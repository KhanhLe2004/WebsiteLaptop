using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ManagePromotionController : BaseAdminController
    {
        public IActionResult Index()
        {
            return View("~/Areas/Admin/Views/AllViews/ManagePromotion.cshtml");
        }
    }
}
