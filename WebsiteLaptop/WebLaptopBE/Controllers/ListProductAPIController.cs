using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;

namespace WebLaptopBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListProductAPIController : ControllerBase
    {
        private readonly Testlaptop30Context _context;

        public ListProductAPIController(Testlaptop30Context context)
        {
            _context = context;
        }
        
        [HttpGet("all")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products.AsNoTracking().ToListAsync();
            return Ok(products);
        }
    }
}
