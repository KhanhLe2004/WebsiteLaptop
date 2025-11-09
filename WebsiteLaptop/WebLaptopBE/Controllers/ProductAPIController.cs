using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductAPIController : ControllerBase
    {
        private Testlaptop20Context db = new Testlaptop20Context();
        
        [HttpGet("all")]
        public IActionResult GetAllProducts()
        {
            var lstSanPham = db.Products.AsNoTracking().ToList();
            return Ok(lstSanPham);
        }
    }
}
