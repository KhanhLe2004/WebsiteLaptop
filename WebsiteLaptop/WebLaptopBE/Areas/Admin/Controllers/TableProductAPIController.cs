using Microsoft.AspNetCore.Mvc;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableProductAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _db = new();

    }
}
