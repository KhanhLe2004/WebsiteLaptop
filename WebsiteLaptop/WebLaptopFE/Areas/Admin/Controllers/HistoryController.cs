using Microsoft.AspNetCore.Mvc;

namespace WebLaptopFE.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HistoryController : BaseAdminController
    {
        // GET: Admin/History
        public IActionResult Index()
        {
            return View("History");
        }
    }
}

