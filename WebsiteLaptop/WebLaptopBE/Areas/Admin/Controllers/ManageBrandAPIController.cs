using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/brands")]
    [ApiController]
    public class ManageBrandAPIController : ControllerBase
    {
        private readonly Testlaptop29Context _context;

        public ManageBrandAPIController(Testlaptop29Context context)
        {
            _context = context;
        }

        // GET: api/admin/brands
        // Lấy danh sách hãng có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<BrandDTO>>> GetBrands(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Brands
                    .Include(b => b.Products)
                    .AsQueryable();

                // Tìm kiếm theo mã hãng hoặc tên hãng
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(b =>
                        b.BrandId.ToLower().Contains(searchTerm) ||
                        (b.BrandName != null && b.BrandName.ToLower().Contains(searchTerm)));
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var brands = await query
                    .OrderBy(b => b.BrandId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var brandDTOs = brands.Select(b => new BrandDTO
                {
                    BrandId = b.BrandId,
                    BrandName = b.BrandName,
                    ProductCount = b.Products != null ? b.Products.Count : 0
                }).ToList();

                var result = new PagedResult<BrandDTO>
                {
                    Items = brandDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách hãng", error = ex.Message });
            }
        }

        // GET: api/admin/brands/{id}
        // Lấy chi tiết một hãng
        [HttpGet("{id}")]
        public async Task<ActionResult<BrandDTO>> GetBrand(string id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.BrandId == id);

                if (brand == null)
                {
                    return NotFound(new { message = "Không tìm thấy hãng" });
                }

                var brandDTO = new BrandDTO
                {
                    BrandId = brand.BrandId,
                    BrandName = brand.BrandName,
                    ProductCount = brand.Products != null ? brand.Products.Count : 0
                };

                return Ok(brandDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin hãng", error = ex.Message });
            }
        }

        // POST: api/admin/brands
        // Tạo mới hãng
        [HttpPost]
        public async Task<ActionResult<BrandDTO>> CreateBrand([FromBody] BrandCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.BrandName))
                {
                    return BadRequest(new { message = "Tên hãng không được để trống" });
                }

                // Tạo mã hãng nếu chưa có
                string brandId = dto.BrandId ?? GenerateBrandId();

                // Kiểm tra mã đã tồn tại chưa
                var existing = await _context.Brands
                    .FirstOrDefaultAsync(b => b.BrandId == brandId);
                if (existing != null)
                {
                    return BadRequest(new { message = "Mã hãng đã tồn tại" });
                }

                // Kiểm tra tên hãng đã tồn tại chưa
                var existingName = await _context.Brands
                    .FirstOrDefaultAsync(b => b.BrandName != null && b.BrandName.Trim().ToLower() == dto.BrandName.Trim().ToLower());
                if (existingName != null)
                {
                    return BadRequest(new { message = "Tên hãng đã tồn tại" });
                }

                var brand = new Brand
                {
                    BrandId = brandId,
                    BrandName = dto.BrandName.Trim()
                };

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();

                var result = new BrandDTO
                {
                    BrandId = brand.BrandId,
                    BrandName = brand.BrandName,
                    ProductCount = 0
                };

                return CreatedAtAction(nameof(GetBrand), new { id = brand.BrandId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo hãng", error = ex.Message });
            }
        }

        // PUT: api/admin/brands/{id}
        // Cập nhật hãng
        [HttpPut("{id}")]
        public async Task<ActionResult<BrandDTO>> UpdateBrand(string id, [FromBody] BrandUpdateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.BrandName))
                {
                    return BadRequest(new { message = "Tên hãng không được để trống" });
                }

                var brand = await _context.Brands
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.BrandId == id);

                if (brand == null)
                {
                    return NotFound(new { message = "Không tìm thấy hãng" });
                }

                // Kiểm tra tên hãng đã tồn tại chưa (trừ chính nó)
                var existingName = await _context.Brands
                    .FirstOrDefaultAsync(b => b.BrandId != id && 
                        b.BrandName != null && 
                        b.BrandName.Trim().ToLower() == dto.BrandName.Trim().ToLower());
                if (existingName != null)
                {
                    return BadRequest(new { message = "Tên hãng đã tồn tại" });
                }

                // Cập nhật thông tin
                brand.BrandName = dto.BrandName.Trim();

                await _context.SaveChangesAsync();

                var result = new BrandDTO
                {
                    BrandId = brand.BrandId,
                    BrandName = brand.BrandName,
                    ProductCount = brand.Products != null ? brand.Products.Count : 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật hãng", error = ex.Message });
            }
        }

        // DELETE: api/admin/brands/{id}
        // Xóa hãng
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(string id)
        {
            try
            {
                var brand = await _context.Brands
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.BrandId == id);

                if (brand == null)
                {
                    return NotFound(new { message = "Không tìm thấy hãng" });
                }

                // Kiểm tra xem hãng có sản phẩm nào không
                if (brand.Products != null && brand.Products.Count > 0)
                {
                    return BadRequest(new { message = "Không thể xóa hãng vì đang có sản phẩm thuộc hãng này" });
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa hãng thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa hãng", error = ex.Message });
            }
        }

        // Helper methods
        private string GenerateBrandId()
        {
            // Tìm số lớn nhất trong các ID có format B001, B002... (4 ký tự: B + 3 số)
            var allIds = _context.Brands
                .Select(b => b.BrandId)
                .Where(id => id != null && id.StartsWith("B") && id.Length == 4)
                .ToList();

            int maxNumber = 0;
            foreach (var id in allIds)
            {
                // Lấy phần số sau ký tự "B" (từ vị trí 1)
                if (id.Length >= 2 && int.TryParse(id.Substring(1), out int num))
                {
                    maxNumber = Math.Max(maxNumber, num);
                }
            }

            // Trả về ID tiếp theo
            return $"B{(maxNumber + 1):D3}";
        }
    }
}
