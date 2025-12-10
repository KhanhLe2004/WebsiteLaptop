using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;
using WebLaptopBE.Services;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/brands")]
    [ApiController]
    public class ManageBrandAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _context;
        private readonly HistoryService _historyService;

        public ManageBrandAPIController(Testlaptop36Context context, HistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        // Helper method để lấy EmployeeId từ header
        private string? GetEmployeeId()
        {
            return Request.Headers["X-Employee-Id"].FirstOrDefault();
        }

        // GET: api/admin/brands
        // Lấy danh sách hãng có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<BrandDTO>>> GetBrands(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? active = null)
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

                // Lọc theo trạng thái active
                if (active.HasValue)
                {
                    query = query.Where(b => b.Active == active.Value);
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
                    Active = b.Active,
                    ProductCount = b.Products != null ? b.Products.Count(p => p.Active == true) : 0
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
                    Active = brand.Active,
                    ProductCount = brand.Products != null ? brand.Products.Count(p => p.Active == true) : 0
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
                    BrandName = dto.BrandName.Trim(),
                    Active = true // Mặc định hãng mới tạo sẽ active
                };

                _context.Brands.Add(brand);
                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Thêm thương hiệu: {brand.BrandId} - {brand.BrandName}");
                }

                var result = new BrandDTO
                {
                    BrandId = brand.BrandId,
                    BrandName = brand.BrandName,
                    Active = brand.Active,
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

                // Cập nhật thông tin (không cập nhật Active - chỉ xóa mới set active = false)
                brand.BrandName = dto.BrandName.Trim();

                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Sửa thương hiệu: {id} - {brand.BrandName}");
                }

                var result = new BrandDTO
                {
                    BrandId = brand.BrandId,
                    BrandName = brand.BrandName,
                    Active = brand.Active,
                    ProductCount = brand.Products != null ? brand.Products.Count(p => p.Active == true) : 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật hãng", error = ex.Message });
            }
        }

        // DELETE: api/admin/brands/{id}
        // Ẩn hãng (set active = false) thay vì xóa thực sự
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

                // Kiểm tra xem hãng có sản phẩm đang hoạt động nào không (chỉ đếm sản phẩm active = true)
                if (brand.Products != null && brand.Products.Any(p => p.Active == true))
                {
                    var activeProductCount = brand.Products.Count(p => p.Active == true);
                    return BadRequest(new { message = $"Không thể xóa hãng vì đang có {activeProductCount} sản phẩm đang hoạt động thuộc hãng này" });
                }

                // Set active = false thay vì xóa
                brand.Active = false;
                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Xóa thương hiệu: {id} - {brand.BrandName}");
                }

                return Ok(new { message = "Đã ẩn hãng thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi ẩn hãng", error = ex.Message });
            }
        }

        // POST: api/admin/brands/{id}/restore
        // Khôi phục hãng (set active = true)
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreBrand(string id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id);
                if (brand == null)
                {
                    return NotFound(new { message = "Không tìm thấy hãng" });
                }

                // Set active = true
                brand.Active = true;
                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Khôi phục thương hiệu: {id} - {brand.BrandName}");
                }

                return Ok(new { message = "Khôi phục hãng thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi khôi phục hãng", error = ex.Message });
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
