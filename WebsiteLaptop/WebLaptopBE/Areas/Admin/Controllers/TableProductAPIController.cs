using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebLaptopBE.Data;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableProductAPIController : ControllerBase
    {
        private Testlaptop20Context db = new Testlaptop20Context();

    }
}
