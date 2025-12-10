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
    [Route("api/admin/suppliers")]
    [ApiController]
    public class ManageSupplierAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _context;
        private readonly HistoryService _historyService;

        public ManageSupplierAPIController(Testlaptop36Context context, HistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        private string? GetEmployeeId()
        {
            return HttpContext.Request.Headers.TryGetValue("X-EmployeeId", out var employeeId) ? employeeId.ToString() : null;
        }

        // GET: api/admin/suppliers
        // Lấy danh sách nhà cung cấp có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<SupplierDTO>>> GetSuppliers(
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

                var query = _context.Suppliers
                    .Include(s => s.StockImports)
                    .AsQueryable();

                // Tìm kiếm theo mã nhà cung cấp, tên, số điện thoại, email
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(s =>
                        s.SupplierId.ToLower().Contains(searchTerm) ||
                        (s.SupplierName != null && s.SupplierName.ToLower().Contains(searchTerm)) ||
                        (s.PhoneNumber != null && s.PhoneNumber.ToLower().Contains(searchTerm)) ||
                        (s.Email != null && s.Email.ToLower().Contains(searchTerm)));
                }

                // Lọc theo trạng thái active
                if (active.HasValue)
                {
                    query = query.Where(s => s.Active == active.Value);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var suppliers = await query
                    .OrderBy(s => s.SupplierId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var supplierDTOs = suppliers.Select(s => new SupplierDTO
                {
                    SupplierId = s.SupplierId,
                    SupplierName = s.SupplierName,
                    PhoneNumber = s.PhoneNumber,
                    Address = s.Address,
                    Email = s.Email,
                    Active = s.Active,
                    StockImportCount = s.StockImports != null ? s.StockImports.Count : 0
                }).ToList();

                var result = new PagedResult<SupplierDTO>
                {
                    Items = supplierDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách nhà cung cấp", error = ex.Message });
            }
        }

        // GET: api/admin/suppliers/{id}
        // Lấy chi tiết một nhà cung cấp
        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierDTO>> GetSupplier(string id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.StockImports)
                    .FirstOrDefaultAsync(s => s.SupplierId == id);

                if (supplier == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhà cung cấp" });
                }

                var supplierDTO = new SupplierDTO
                {
                    SupplierId = supplier.SupplierId,
                    SupplierName = supplier.SupplierName,
                    PhoneNumber = supplier.PhoneNumber,
                    Address = supplier.Address,
                    Email = supplier.Email,
                    Active = supplier.Active,
                    StockImportCount = supplier.StockImports != null ? supplier.StockImports.Count : 0
                };

                return Ok(supplierDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin nhà cung cấp", error = ex.Message });
            }
        }

        // POST: api/admin/suppliers
        // Tạo mới nhà cung cấp
        [HttpPost]
        public async Task<ActionResult<SupplierDTO>> CreateSupplier([FromBody] SupplierCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.SupplierName))
                {
                    return BadRequest(new { message = "Tên nhà cung cấp không được để trống" });
                }

                // Tạo mã nhà cung cấp nếu chưa có
                string supplierId = dto.SupplierId ?? GenerateSupplierId();

                // Kiểm tra mã đã tồn tại chưa
                var existing = await _context.Suppliers
                    .FirstOrDefaultAsync(s => s.SupplierId == supplierId);
                if (existing != null)
                {
                    return BadRequest(new { message = "Mã nhà cung cấp đã tồn tại" });
                }

                var supplier = new Supplier
                {
                    SupplierId = supplierId,
                    SupplierName = dto.SupplierName.Trim(),
                    PhoneNumber = !string.IsNullOrWhiteSpace(dto.PhoneNumber) ? dto.PhoneNumber.Trim() : null,
                    Address = !string.IsNullOrWhiteSpace(dto.Address) ? dto.Address.Trim() : null,
                    Email = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email.Trim() : null,
                    Active = true // Mặc định nhà cung cấp mới tạo sẽ active
                };

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                // Log history
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Thêm nhà cung cấp: {supplier.SupplierId} - {supplier.SupplierName}");
                }

                var result = new SupplierDTO
                {
                    SupplierId = supplier.SupplierId,
                    SupplierName = supplier.SupplierName,
                    PhoneNumber = supplier.PhoneNumber,
                    Address = supplier.Address,
                    Email = supplier.Email,
                    Active = supplier.Active,
                    StockImportCount = 0
                };

                return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo nhà cung cấp", error = ex.Message });
            }
        }

        // PUT: api/admin/suppliers/{id}
        // Cập nhật nhà cung cấp
        [HttpPut("{id}")]
        public async Task<ActionResult<SupplierDTO>> UpdateSupplier(string id, [FromBody] SupplierUpdateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.SupplierName))
                {
                    return BadRequest(new { message = "Tên nhà cung cấp không được để trống" });
                }

                var supplier = await _context.Suppliers
                    .Include(s => s.StockImports)
                    .FirstOrDefaultAsync(s => s.SupplierId == id);

                if (supplier == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhà cung cấp" });
                }

                // Cập nhật thông tin (không cập nhật Active - chỉ xóa mới set active = false)
                supplier.SupplierName = dto.SupplierName.Trim();
                supplier.PhoneNumber = !string.IsNullOrWhiteSpace(dto.PhoneNumber) ? dto.PhoneNumber.Trim() : null;
                supplier.Address = !string.IsNullOrWhiteSpace(dto.Address) ? dto.Address.Trim() : null;
                supplier.Email = !string.IsNullOrWhiteSpace(dto.Email) ? dto.Email.Trim() : null;

                await _context.SaveChangesAsync();

                // Log history
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Cập nhật nhà cung cấp: {id} - {supplier.SupplierName}");
                }

                var result = new SupplierDTO
                {
                    SupplierId = supplier.SupplierId,
                    SupplierName = supplier.SupplierName,
                    PhoneNumber = supplier.PhoneNumber,
                    Address = supplier.Address,
                    Email = supplier.Email,
                    Active = supplier.Active,
                    StockImportCount = supplier.StockImports != null ? supplier.StockImports.Count : 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật nhà cung cấp", error = ex.Message });
            }
        }

        // DELETE: api/admin/suppliers/{id}
        // Ẩn nhà cung cấp (set active = false) thay vì xóa thực sự
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(string id)
        {
            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.StockImports)
                    .FirstOrDefaultAsync(s => s.SupplierId == id);

                if (supplier == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhà cung cấp" });
                }

                // Kiểm tra xem nhà cung cấp có phiếu nhập hàng nào không
                if (supplier.StockImports != null && supplier.StockImports.Count > 0)
                {
                    return BadRequest(new { message = "Không thể xóa nhà cung cấp vì đang có phiếu nhập hàng liên quan" });
                }

                // Set active = false thay vì xóa
                supplier.Active = false;
                await _context.SaveChangesAsync();

                // Log history
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Xóa nhà cung cấp: {id} - {supplier.SupplierName}");
                }

                return Ok(new { message = "Đã ẩn nhà cung cấp thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi ẩn nhà cung cấp", error = ex.Message });
            }
        }

        // POST: api/admin/suppliers/{id}/restore
        // Khôi phục nhà cung cấp (set active = true)
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreSupplier(string id)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhà cung cấp" });
                }

                // Set active = true
                supplier.Active = true;
                await _context.SaveChangesAsync();

                // Log history
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Khôi phục nhà cung cấp: {id} - {supplier.SupplierName}");
                }

                return Ok(new { message = "Khôi phục nhà cung cấp thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi khôi phục nhà cung cấp", error = ex.Message });
            }
        }

        // Helper methods
        private string GenerateSupplierId()
        {
            // Tìm số lớn nhất trong các ID có format SUP001, SUP002... (6 ký tự: SUP + 3 số)
            var allIds = _context.Suppliers
                .Select(s => s.SupplierId)
                .Where(id => id != null && id.StartsWith("SUP") && id.Length == 6)
                .ToList();

            int maxNumber = 0;
            foreach (var id in allIds)
            {
                // Lấy phần số sau ký tự "SUP" (từ vị trí 3)
                if (id.Length >= 4 && int.TryParse(id.Substring(3), out int num))
                {
                    maxNumber = Math.Max(maxNumber, num);
                }
            }

            // Trả về ID tiếp theo
            return $"SUP{(maxNumber + 1):D3}";
        }
    }
}
