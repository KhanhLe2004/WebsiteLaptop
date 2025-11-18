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
    [Route("api/admin/promotions")]
    [ApiController]
    public class ManagePromotionAPIController : ControllerBase
    {
        private readonly Testlaptop33Context _context;

        public ManagePromotionAPIController(Testlaptop33Context context)
        {
            _context = context;
        }

        // GET: api/admin/promotions
        // Lấy danh sách khuyến mại có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<PromotionDTO>>> GetPromotions(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? productId = null,
            [FromQuery] string? type = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Promotions
                    .Include(p => p.Product)
                    .AsQueryable();

                // Tìm kiếm theo mã khuyến mại, tên sản phẩm, loại, hoặc nội dung
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(p =>
                        p.PromotionId.ToLower().Contains(searchTerm) ||
                        (p.Product != null && p.Product.ProductName != null && p.Product.ProductName.ToLower().Contains(searchTerm)) ||
                        (p.Type != null && p.Type.ToLower().Contains(searchTerm)) ||
                        (p.ContentDetail != null && p.ContentDetail.ToLower().Contains(searchTerm)));
                }

                // Lọc theo sản phẩm
                if (!string.IsNullOrWhiteSpace(productId))
                {
                    query = query.Where(p => p.ProductId == productId);
                }

                // Lọc theo loại khuyến mại
                if (!string.IsNullOrWhiteSpace(type))
                {
                    query = query.Where(p => p.Type != null && p.Type.ToLower() == type.ToLower());
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var promotions = await query
                    .OrderByDescending(p => p.PromotionId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var promotionDTOs = promotions.Select(p => new PromotionDTO
                {
                    PromotionId = p.PromotionId,
                    ProductId = p.ProductId,
                    ProductName = p.Product != null ? p.Product.ProductName : null,
                    ProductModel = p.Product != null ? p.Product.ProductModel : null,
                    Type = p.Type,
                    ContentDetail = p.ContentDetail
                }).ToList();

                var result = new PagedResult<PromotionDTO>
                {
                    Items = promotionDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khuyến mại", error = ex.Message });
            }
        }

        // GET: api/admin/promotions/{id}
        // Lấy chi tiết một khuyến mại
        [HttpGet("{id}")]
        public async Task<ActionResult<PromotionDTO>> GetPromotion(string id)
        {
            try
            {
                var promotion = await _context.Promotions
                    .Include(p => p.Product)
                    .FirstOrDefaultAsync(p => p.PromotionId == id);

                if (promotion == null)
                {
                    return NotFound(new { message = "Không tìm thấy khuyến mại" });
                }

                var promotionDTO = new PromotionDTO
                {
                    PromotionId = promotion.PromotionId,
                    ProductId = promotion.ProductId,
                    ProductName = promotion.Product != null ? promotion.Product.ProductName : null,
                    ProductModel = promotion.Product != null ? promotion.Product.ProductModel : null,
                    Type = promotion.Type,
                    ContentDetail = promotion.ContentDetail
                };

                return Ok(promotionDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin khuyến mại", error = ex.Message });
            }
        }

        // POST: api/admin/promotions
        // Tạo mới khuyến mại
        [HttpPost]
        public async Task<ActionResult<PromotionDTO>> CreatePromotion([FromBody] PromotionCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.ProductId))
                {
                    return BadRequest(new { message = "Sản phẩm không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(dto.Type))
                {
                    return BadRequest(new { message = "Loại khuyến mại không được để trống" });
                }

                // Kiểm tra sản phẩm có tồn tại không
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);
                if (product == null)
                {
                    return BadRequest(new { message = "Sản phẩm không tồn tại" });
                }

                // Tạo mã khuyến mại nếu chưa có
                string promotionId = dto.PromotionId ?? GeneratePromotionId();

                // Kiểm tra mã đã tồn tại chưa
                var existing = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromotionId == promotionId);
                if (existing != null)
                {
                    return BadRequest(new { message = "Mã khuyến mại đã tồn tại" });
                }

                var promotion = new Promotion
                {
                    PromotionId = promotionId,
                    ProductId = dto.ProductId.Trim(),
                    Type = dto.Type.Trim(),
                    ContentDetail = !string.IsNullOrWhiteSpace(dto.ContentDetail) ? dto.ContentDetail.Trim() : null
                };

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                var result = new PromotionDTO
                {
                    PromotionId = promotion.PromotionId,
                    ProductId = promotion.ProductId,
                    ProductName = product.ProductName,
                    ProductModel = product.ProductModel,
                    Type = promotion.Type,
                    ContentDetail = promotion.ContentDetail
                };

                return CreatedAtAction(nameof(GetPromotion), new { id = promotion.PromotionId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo khuyến mại", error = ex.Message });
            }
        }

        // PUT: api/admin/promotions/{id}
        // Cập nhật khuyến mại
        [HttpPut("{id}")]
        public async Task<ActionResult<PromotionDTO>> UpdatePromotion(string id, [FromBody] PromotionUpdateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.ProductId))
                {
                    return BadRequest(new { message = "Sản phẩm không được để trống" });
                }

                if (string.IsNullOrWhiteSpace(dto.Type))
                {
                    return BadRequest(new { message = "Loại khuyến mại không được để trống" });
                }

                var promotion = await _context.Promotions
                    .Include(p => p.Product)
                    .FirstOrDefaultAsync(p => p.PromotionId == id);

                if (promotion == null)
                {
                    return NotFound(new { message = "Không tìm thấy khuyến mại" });
                }

                // Kiểm tra sản phẩm có tồn tại không
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);
                if (product == null)
                {
                    return BadRequest(new { message = "Sản phẩm không tồn tại" });
                }

                // Cập nhật thông tin
                promotion.ProductId = dto.ProductId.Trim();
                promotion.Type = dto.Type.Trim();
                promotion.ContentDetail = !string.IsNullOrWhiteSpace(dto.ContentDetail) ? dto.ContentDetail.Trim() : null;

                await _context.SaveChangesAsync();

                var result = new PromotionDTO
                {
                    PromotionId = promotion.PromotionId,
                    ProductId = promotion.ProductId,
                    ProductName = product.ProductName,
                    ProductModel = product.ProductModel,
                    Type = promotion.Type,
                    ContentDetail = promotion.ContentDetail
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật khuyến mại", error = ex.Message });
            }
        }

        // DELETE: api/admin/promotions/{id}
        // Xóa khuyến mại
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(string id)
        {
            try
            {
                var promotion = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.PromotionId == id);

                if (promotion == null)
                {
                    return NotFound(new { message = "Không tìm thấy khuyến mại" });
                }

                _context.Promotions.Remove(promotion);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa khuyến mại thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa khuyến mại", error = ex.Message });
            }
        }

        // POST: api/admin/promotions/batch
        // Tạo khuyến mại hàng loạt cho nhiều sản phẩm
        [HttpPost("batch")]
        public async Task<ActionResult<object>> CreatePromotionsBatch([FromBody] PromotionBatchCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (dto.ProductIds == null || dto.ProductIds.Count == 0)
                {
                    return BadRequest(new { message = "Phải chọn ít nhất một sản phẩm" });
                }

                // Loại bỏ các ID rỗng hoặc null
                dto.ProductIds = dto.ProductIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(id => id.Trim())
                    .Distinct()
                    .ToList();

                if (dto.ProductIds.Count == 0)
                {
                    return BadRequest(new { message = "Danh sách sản phẩm không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.Type))
                {
                    return BadRequest(new { message = "Loại khuyến mại không được để trống" });
                }

                // Kiểm tra tất cả sản phẩm có tồn tại không
                var products = await _context.Products
                    .Where(p => dto.ProductIds.Contains(p.ProductId))
                    .ToListAsync();

                if (products.Count != dto.ProductIds.Count)
                {
                    var foundIds = products.Select(p => p.ProductId).ToList();
                    var notFoundIds = dto.ProductIds.Except(foundIds).ToList();
                    return BadRequest(new { message = $"Các sản phẩm sau không tồn tại: {string.Join(", ", notFoundIds)}" });
                }

                var createdPromotions = new List<PromotionDTO>();
                var errors = new List<string>();

                // Lấy tất cả ID hiện có để tránh trùng lặp
                var existingIds = await _context.Promotions
                    .Select(p => p.PromotionId)
                    .ToListAsync();

                // Tạo tất cả ID trước để tránh trùng lặp
                var promotionIds = GeneratePromotionIds(dto.ProductIds.Count, existingIds);

                // Tạo khuyến mại cho từng sản phẩm
                for (int i = 0; i < dto.ProductIds.Count; i++)
                {
                    var productId = dto.ProductIds[i];
                    var promotionId = promotionIds[i];
                    
                    try
                    {

                        var promotion = new Promotion
                        {
                            PromotionId = promotionId,
                            ProductId = productId.Trim(),
                            Type = dto.Type.Trim(),
                            ContentDetail = !string.IsNullOrWhiteSpace(dto.ContentDetail) ? dto.ContentDetail.Trim() : null
                        };

                        _context.Promotions.Add(promotion);

                        var product = products.FirstOrDefault(p => p.ProductId == productId);
                        if (product != null)
                        {
                            createdPromotions.Add(new PromotionDTO
                            {
                                PromotionId = promotionId,
                                ProductId = productId,
                                ProductName = product.ProductName,
                                ProductModel = product.ProductModel,
                                Type = promotion.Type,
                                ContentDetail = promotion.ContentDetail
                            });
                        }
                        else
                        {
                            errors.Add($"Không tìm thấy thông tin sản phẩm {productId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Lỗi khi tạo khuyến mại cho sản phẩm {productId}: {ex.Message}");
                    }
                }

                if (createdPromotions.Count > 0)
                {
                    await _context.SaveChangesAsync();
                }
                else if (errors.Count > 0)
                {
                    return BadRequest(new { message = "Không thể tạo khuyến mại nào", errors = errors });
                }

                return Ok(new
                {
                    message = $"Đã tạo thành công {createdPromotions.Count} khuyến mại",
                    createdCount = createdPromotions.Count,
                    totalCount = dto.ProductIds.Count,
                    promotions = createdPromotions,
                    errors = errors
                });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi để debug
                var errorDetails = new
                {
                    message = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                };
                return StatusCode(500, new { message = "Lỗi khi tạo khuyến mại hàng loạt", error = ex.Message, details = errorDetails });
            }
        }

        // GET: api/admin/promotions/products
        // Lấy danh sách sản phẩm để chọn trong dropdown
        [HttpGet("products")]
        public async Task<ActionResult<List<object>>> GetProducts()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.Active == true)
                    .OrderBy(p => p.ProductName)
                    .Select(p => new
                    {
                        productId = p.ProductId,
                        productName = p.ProductName,
                        productModel = p.ProductModel
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sản phẩm", error = ex.Message });
            }
        }

        // Helper methods
        private string GeneratePromotionId()
        {
            // Tìm số lớn nhất trong các ID có format KM001, KM002... (5 ký tự: KM + 3 số)
            var allIds = _context.Promotions
                .Select(p => p.PromotionId)
                .Where(id => id != null && id.StartsWith("KM") && id.Length == 5)
                .ToList();

            int maxNumber = 0;
            foreach (var id in allIds)
            {
                // Lấy phần số sau ký tự "KM" (từ vị trí 2)
                if (id.Length >= 3 && int.TryParse(id.Substring(2), out int num))
                {
                    maxNumber = Math.Max(maxNumber, num);
                }
            }

            // Trả về ID tiếp theo
            return $"KM{(maxNumber + 1):D3}";
        }

        // Tạo nhiều ID khuyến mại cùng lúc
        private List<string> GeneratePromotionIds(int count, List<string> existingIds)
        {
            var newIds = new List<string>();
            
            // Tìm số lớn nhất trong các ID hiện có (format KM001, KM002...)
            int maxNumber = 0;
            foreach (var id in existingIds)
            {
                if (id != null && id.StartsWith("KM") && id.Length == 5)
                {
                    if (int.TryParse(id.Substring(2), out int num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }
            }

            // Tạo các ID tiếp theo
            for (int i = 1; i <= count; i++)
            {
                string newId = $"KM{(maxNumber + i):D3}";
                newIds.Add(newId);
            }

            return newIds;
        }
    }
}
