using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/products")]
    [ApiController]
    public class TableProductAPIController : ControllerBase
    {
        private readonly Testlaptop20Context _context;
        private readonly IWebHostEnvironment _environment;

        public TableProductAPIController(Testlaptop20Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/admin/products
        // Lấy danh sách sản phẩm có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<ProductDTO>>> GetProducts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? brandId = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Giới hạn tối đa

                var query = _context.Products
                    .Include(p => p.Brand)
                    .AsQueryable();

                // Tìm kiếm theo tên hoặc model
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(p => 
                        p.ProductName!.ToLower().Contains(searchTerm) || 
                        p.ProductModel!.ToLower().Contains(searchTerm) ||
                        p.ProductId.ToLower().Contains(searchTerm));
                }

                // Lọc theo thương hiệu
                if (!string.IsNullOrWhiteSpace(brandId))
                {
                    query = query.Where(p => p.BrandId == brandId);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var products = await query
                    .OrderByDescending(p => p.ProductId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ProductModel = p.ProductModel,
                        WarrantyPeriod = p.WarrantyPeriod,
                        OriginalSellingPrice = p.OriginalSellingPrice,
                        SellingPrice = p.SellingPrice,
                        Screen = p.Screen,
                        Camera = p.Camera,
                        Connect = p.Connect,
                        Weight = p.Weight,
                        Pin = p.Pin,
                        BrandId = p.BrandId,
                        BrandName = p.Brand != null ? p.Brand.BrandName : null,
                        Avatar = p.Avatar
                    })
                    .ToListAsync();

                var result = new PagedResult<ProductDTO>
                {
                    Items = products,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sản phẩm", error = ex.Message });
            }
        }

        // GET: api/admin/products/{id}
        // Lấy chi tiết một sản phẩm
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(string id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Brand)
                    .Where(p => p.ProductId == id)
                    .Select(p => new ProductDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ProductModel = p.ProductModel,
                        WarrantyPeriod = p.WarrantyPeriod,
                        OriginalSellingPrice = p.OriginalSellingPrice,
                        SellingPrice = p.SellingPrice,
                        Screen = p.Screen,
                        Camera = p.Camera,
                        Connect = p.Connect,
                        Weight = p.Weight,
                        Pin = p.Pin,
                        BrandId = p.BrandId,
                        BrandName = p.Brand != null ? p.Brand.BrandName : null,
                        Avatar = p.Avatar
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin sản phẩm", error = ex.Message });
            }
        }

        // POST: api/admin/products
        // Tạo mới sản phẩm
        [HttpPost]
        public async Task<ActionResult<ProductDTO>> CreateProduct([FromForm] ProductCreateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kiểm tra sản phẩm đã tồn tại
                if (await _context.Products.AnyAsync(p => p.ProductId == dto.ProductId))
                {
                    return Conflict(new { message = "Mã sản phẩm đã tồn tại" });
                }

                // Kiểm tra thương hiệu có tồn tại không
                if (!string.IsNullOrEmpty(dto.BrandId))
                {
                    var brandExists = await _context.Brands.AnyAsync(b => b.BrandId == dto.BrandId);
                    if (!brandExists)
                    {
                        return BadRequest(new { message = "Thương hiệu không tồn tại" });
                    }
                }

                // Xử lý upload ảnh
                string? avatarFileName = null;
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    avatarFileName = await SaveImageAsync(dto.AvatarFile);
                }

                var product = new Product
                {
                    ProductId = dto.ProductId,
                    ProductName = dto.ProductName,
                    ProductModel = dto.ProductModel,
                    WarrantyPeriod = dto.WarrantyPeriod,
                    OriginalSellingPrice = dto.OriginalSellingPrice,
                    SellingPrice = dto.SellingPrice,
                    Screen = dto.Screen,
                    Camera = dto.Camera,
                    Connect = dto.Connect,
                    Weight = dto.Weight,
                    Pin = dto.Pin,
                    BrandId = dto.BrandId,
                    Avatar = avatarFileName
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var result = await _context.Products
                    .Include(p => p.Brand)
                    .Where(p => p.ProductId == product.ProductId)
                    .Select(p => new ProductDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ProductModel = p.ProductModel,
                        WarrantyPeriod = p.WarrantyPeriod,
                        OriginalSellingPrice = p.OriginalSellingPrice,
                        SellingPrice = p.SellingPrice,
                        Screen = p.Screen,
                        Camera = p.Camera,
                        Connect = p.Connect,
                        Weight = p.Weight,
                        Pin = p.Pin,
                        BrandId = p.BrandId,
                        BrandName = p.Brand != null ? p.Brand.BrandName : null,
                        Avatar = p.Avatar
                    })
                    .FirstAsync();

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo sản phẩm", error = ex.Message });
            }
        }

        // PUT: api/admin/products/{id}
        // Cập nhật sản phẩm
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, [FromForm] ProductUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                // Kiểm tra thương hiệu có tồn tại không
                if (!string.IsNullOrEmpty(dto.BrandId))
                {
                    var brandExists = await _context.Brands.AnyAsync(b => b.BrandId == dto.BrandId);
                    if (!brandExists)
                    {
                        return BadRequest(new { message = "Thương hiệu không tồn tại" });
                    }
                }

                // Xử lý upload ảnh mới
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(product.Avatar))
                    {
                        DeleteImage(product.Avatar);
                    }

                    product.Avatar = await SaveImageAsync(dto.AvatarFile);
                }

                // Cập nhật thông tin
                product.ProductName = dto.ProductName;
                product.ProductModel = dto.ProductModel;
                product.WarrantyPeriod = dto.WarrantyPeriod;
                product.OriginalSellingPrice = dto.OriginalSellingPrice;
                product.SellingPrice = dto.SellingPrice;
                product.Screen = dto.Screen;
                product.Camera = dto.Camera;
                product.Connect = dto.Connect;
                product.Weight = dto.Weight;
                product.Pin = dto.Pin;
                product.BrandId = dto.BrandId;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật sản phẩm", error = ex.Message });
            }
        }

        // DELETE: api/admin/products/{id}
        // Xóa sản phẩm
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Không tìm thấy sản phẩm" });
                }

                // Kiểm tra ràng buộc với các bảng khác
                var hasCartDetails = await _context.CartDetails.AnyAsync(cd => cd.ProductId == id);
                var hasSaleInvoiceDetails = await _context.SaleInvoiceDetails.AnyAsync(sid => sid.ProductId == id);
                
                if (hasCartDetails || hasSaleInvoiceDetails)
                {
                    return BadRequest(new { message = "Không thể xóa sản phẩm vì đã có trong giỏ hàng hoặc hóa đơn" });
                }

                // Xóa ảnh nếu có
                if (!string.IsNullOrEmpty(product.Avatar))
                {
                    DeleteImage(product.Avatar);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa sản phẩm", error = ex.Message });
            }
        }

        // GET: api/admin/products/brands
        // Lấy danh sách thương hiệu (để dropdown)
        [HttpGet("brands")]
        public async Task<ActionResult<List<Brand>>> GetBrands()
        {
            try
            {
                var brands = await _context.Brands
                    .OrderBy(b => b.BrandName)
                    .ToListAsync();
                return Ok(brands);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách thương hiệu", error = ex.Message });
            }
        }

        // Hàm hỗ trợ lưu ảnh
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            try
            {
                // Tạo tên file unique
                var fileExtension = Path.GetExtension(file.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                
                // Đường dẫn thư mục lưu ảnh
                var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "wwwroot", "image");
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Lưu file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu ảnh: {ex.Message}");
            }
        }

        // Hàm hỗ trợ xóa ảnh
        private void DeleteImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "image", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch
            {
                // Không throw exception nếu xóa file thất bại
            }
        }
    }
}
