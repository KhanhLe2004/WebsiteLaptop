using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/stock-imports")]
    [ApiController]
    public class ManageStockImportAPIController : ControllerBase
    {
        private readonly Data.Testlaptop36Context _context;

        public ManageStockImportAPIController(Data.Testlaptop36Context context)
        {
            _context = context;
        }

        // GET: api/admin/stock-imports
        // Lấy danh sách phiếu nhập hàng có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<StockImportDTO>>> GetStockImports(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.StockImports
                    .Include(si => si.Supplier)
                    .Include(si => si.Employee)
                    .AsQueryable();

                // Tìm kiếm theo mã phiếu nhập, tên nhà cung cấp, tên nhân viên
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(si =>
                        si.StockImportId.ToLower().Contains(searchTerm) ||
                        (si.Supplier != null && si.Supplier.SupplierName != null && si.Supplier.SupplierName.ToLower().Contains(searchTerm)) ||
                        (si.Employee != null && si.Employee.EmployeeName != null && si.Employee.EmployeeName.ToLower().Contains(searchTerm)));
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var stockImports = await query
                    .OrderByDescending(si => si.Time)
                    .ThenByDescending(si => si.StockImportId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var stockImportDTOs = stockImports.Select(si => new StockImportDTO
                {
                    StockImportId = si.StockImportId,
                    SupplierId = si.SupplierId,
                    SupplierName = si.Supplier?.SupplierName,
                    EmployeeId = si.EmployeeId,
                    EmployeeName = si.Employee?.EmployeeName,
                    Time = si.Time,
                    TotalAmount = si.TotalAmount
                }).ToList();

                var result = new PagedResult<StockImportDTO>
                {
                    Items = stockImportDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách phiếu nhập hàng", error = ex.Message });
            }
        }

        // GET: api/admin/stock-imports/suppliers
        // Lấy danh sách nhà cung cấp
        [HttpGet("suppliers")]
        public async Task<ActionResult<List<SupplierDTO>>> GetSuppliers()
        {
            try
            {
                // Lấy tất cả nhà cung cấp (Active = true hoặc null, không lấy Active = false)
                // Nếu không có dữ liệu, có thể thử lấy tất cả không phân biệt Active
                var suppliers = await _context.Suppliers
                    .Where(s => s.Active == null || s.Active == true)
                    .OrderBy(s => s.SupplierName)
                    .Select(s => new SupplierDTO
                    {
                        SupplierId = s.SupplierId,
                        SupplierName = s.SupplierName
                    })
                    .ToListAsync();

                // Nếu không có nhà cung cấp nào, thử lấy tất cả (để test)
                if (suppliers.Count == 0)
                {
                    suppliers = await _context.Suppliers
                        .OrderBy(s => s.SupplierName)
                        .Select(s => new SupplierDTO
                        {
                            SupplierId = s.SupplierId,
                            SupplierName = s.SupplierName
                        })
                        .ToListAsync();
                }

                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách nhà cung cấp", error = ex.Message });
            }
        }

        // GET: api/admin/stock-imports/products
        // Lấy danh sách sản phẩm
        [HttpGet("products")]
        public async Task<ActionResult<List<ProductSelectDTO>>> GetProducts()
        {
            try
            {
                // Lấy tất cả sản phẩm (Active = true hoặc null, không lấy Active = false)
                var products = await _context.Products
                    .Where(p => p.Active == null || p.Active == true)
                    .OrderBy(p => p.ProductName)
                    .ThenBy(p => p.ProductModel)
                    .Select(p => new ProductSelectDTO
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        ProductModel = p.ProductModel,
                        DisplayName = (p.ProductName ?? "") + (p.ProductModel != null ? " - " + p.ProductModel : "")
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sản phẩm", error = ex.Message });
            }
        }

        // GET: api/admin/stock-imports/product-configurations/{productId}
        // Lấy danh sách cấu hình của sản phẩm
        [HttpGet("product-configurations/{productId}")]
        public async Task<ActionResult<List<ProductConfigurationDTO>>> GetProductConfigurations(string productId)
        {
            try
            {
                var configurations = await _context.ProductConfigurations
                    .Where(pc => pc.ProductId == productId)
                    .OrderBy(pc => pc.ConfigurationId)
                    .Select(pc => new ProductConfigurationDTO
                    {
                        ConfigurationId = pc.ConfigurationId,
                        Cpu = pc.Cpu,
                        Ram = pc.Ram,
                        Rom = pc.Rom,
                        Card = pc.Card,
                        Price = pc.Price,
                        Quantity = pc.Quantity,
                        ProductId = pc.ProductId
                    })
                    .ToListAsync();

                return Ok(configurations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách cấu hình sản phẩm", error = ex.Message });
            }
        }

        // GET: api/admin/stock-imports/{id}
        // Lấy chi tiết một phiếu nhập hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<StockImportDTO>> GetStockImport(string id)
        {
            try
            {
                var stockImport = await _context.StockImports
                    .Include(si => si.Supplier)
                    .Include(si => si.Employee)
                    .Include(si => si.StockImportDetails)
                        .ThenInclude(detail => detail.Product)
                    .FirstOrDefaultAsync(si => si.StockImportId == id);

                if (stockImport == null)
                {
                    return NotFound(new { message = "Không tìm thấy phiếu nhập hàng" });
                }

                var stockImportDTO = new StockImportDTO
                {
                    StockImportId = stockImport.StockImportId,
                    SupplierId = stockImport.SupplierId,
                    SupplierName = stockImport.Supplier?.SupplierName,
                    EmployeeId = stockImport.EmployeeId,
                    EmployeeName = stockImport.Employee?.EmployeeName,
                    Time = stockImport.Time,
                    TotalAmount = stockImport.TotalAmount,
                    Details = stockImport.StockImportDetails?.Select(detail => new StockImportDetailDTO
                    {
                        StockImportDetailId = detail.StockImportDetailId,
                        StockImportId = detail.StockImportId,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product?.ProductName,
                        ProductModel = detail.Product?.ProductModel,
                        Specifications = detail.Specifications,
                        Quantity = detail.Quantity,
                        Price = detail.Price
                    }).ToList()
                };

                return Ok(stockImportDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin phiếu nhập hàng", error = ex.Message });
            }
        }

        // POST: api/admin/stock-imports
        // Tạo mới phiếu nhập hàng
        [HttpPost]
        public async Task<ActionResult<StockImportDTO>> CreateStockImport([FromBody] StockImportCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                // Tự động lấy EmployeeId từ request header nếu không có trong DTO
                // Frontend có thể gửi EmployeeId trong header hoặc trong body
                if (string.IsNullOrWhiteSpace(dto.EmployeeId))
                {
                    // Có thể lấy từ header nếu frontend gửi
                    if (Request.Headers.ContainsKey("X-Employee-Id"))
                    {
                        dto.EmployeeId = Request.Headers["X-Employee-Id"].ToString();
                    }
                }

                // Tạo mã phiếu nhập nếu chưa có
                string stockImportId = dto.StockImportId ?? GenerateStockImportId();

                // Kiểm tra mã đã tồn tại chưa
                var existing = await _context.StockImports
                    .FirstOrDefaultAsync(si => si.StockImportId == stockImportId);
                if (existing != null)
                {
                    return BadRequest(new { message = "Mã phiếu nhập hàng đã tồn tại" });
                }

                // Tính tổng tiền từ chi tiết
                decimal totalAmount = 0;
                if (dto.Details != null && dto.Details.Any())
                {
                    totalAmount = dto.Details.Sum(d => (d.Quantity ?? 0) * (d.Price ?? 0));
                }

                // Xử lý Time: Parse time từ client (có thể là string hoặc DateTime)
                DateTime? importTime = dto.Time;
                if (importTime.HasValue)
                {
                    // Nếu là UTC, convert về local time
                    if (importTime.Value.Kind == DateTimeKind.Utc)
                    {
                        importTime = importTime.Value.ToLocalTime();
                    }
                    // Nếu là Unspecified (từ string parse), giữ nguyên (đã là local time)
                    else if (importTime.Value.Kind == DateTimeKind.Unspecified)
                    {
                        // Giữ nguyên, coi như local time
                        importTime = DateTime.SpecifyKind(importTime.Value, DateTimeKind.Local);
                    }
                }
                else
                {
                    importTime = DateTime.Now;
                }

                var stockImport = new StockImport
                {
                    StockImportId = stockImportId,
                    SupplierId = dto.SupplierId,
                    EmployeeId = dto.EmployeeId,
                    Time = importTime.Value,
                    TotalAmount = dto.TotalAmount ?? totalAmount
                };

                _context.StockImports.Add(stockImport);

                // Thêm chi tiết với ID tự động và cập nhật Quantity của ProductConfiguration
                if (dto.Details != null && dto.Details.Any())
                {
                    // Lấy số lớn nhất hiện có để tạo ID tuần tự
                    int startNumber = GetMaxStockImportDetailNumber();
                    
                    int detailIndex = 0;
                    foreach (var detailDto in dto.Details)
                    {
                        string detailId = $"STID{(startNumber + detailIndex + 1):D4}";
                        var detail = new StockImportDetail
                        {
                            StockImportDetailId = detailId,
                            StockImportId = stockImportId,
                            ProductId = detailDto.ProductId,
                            Specifications = detailDto.Specifications,
                            Quantity = detailDto.Quantity,
                            Price = detailDto.Price
                        };
                        _context.StockImportDetails.Add(detail);
                        
                        // Cập nhật Quantity của ProductConfiguration
                        if (!string.IsNullOrEmpty(detailDto.ProductId) && detailDto.Quantity.HasValue && detailDto.Quantity.Value > 0)
                        {
                            await UpdateProductConfigurationQuantity(detailDto.ProductId, detailDto.Specifications, detailDto.Quantity.Value);
                            
                            // Tạo ProductSerial cho mỗi đơn vị sản phẩm
                            await CreateProductSerials(
                                detailDto.ProductId, 
                                detailDto.Specifications, 
                                detailDto.Quantity.Value, 
                                stockImport.Time ?? DateTime.Now);
                        }
                        
                        detailIndex++;
                    }
                }

                await _context.SaveChangesAsync();

                // Load lại để lấy thông tin đầy đủ
                await _context.Entry(stockImport)
                    .Reference(si => si.Supplier)
                    .LoadAsync();
                await _context.Entry(stockImport)
                    .Reference(si => si.Employee)
                    .LoadAsync();
                await _context.Entry(stockImport)
                    .Collection(si => si.StockImportDetails)
                    .Query()
                    .Include(d => d.Product)
                    .LoadAsync();

                var result = new StockImportDTO
                {
                    StockImportId = stockImport.StockImportId,
                    SupplierId = stockImport.SupplierId,
                    SupplierName = stockImport.Supplier?.SupplierName,
                    EmployeeId = stockImport.EmployeeId,
                    EmployeeName = stockImport.Employee?.EmployeeName,
                    Time = stockImport.Time,
                    TotalAmount = stockImport.TotalAmount,
                    Details = stockImport.StockImportDetails?.Select(detail => new StockImportDetailDTO
                    {
                        StockImportDetailId = detail.StockImportDetailId,
                        StockImportId = detail.StockImportId,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product?.ProductName,
                        ProductModel = detail.Product?.ProductModel,
                        Specifications = detail.Specifications,
                        Quantity = detail.Quantity,
                        Price = detail.Price
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetStockImport), new { id = stockImport.StockImportId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo phiếu nhập hàng", error = ex.Message });
            }
        }

        // PUT: api/admin/stock-imports/{id}
        // Cập nhật phiếu nhập hàng
        [HttpPut("{id}")]
        public async Task<ActionResult<StockImportDTO>> UpdateStockImport(string id, [FromBody] StockImportUpdateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                var stockImport = await _context.StockImports
                    .Include(si => si.StockImportDetails)
                    .FirstOrDefaultAsync(si => si.StockImportId == id);

                if (stockImport == null)
                {
                    return NotFound(new { message = "Không tìm thấy phiếu nhập hàng" });
                }

                // Cập nhật thông tin
                stockImport.SupplierId = dto.SupplierId ?? stockImport.SupplierId;
                stockImport.EmployeeId = dto.EmployeeId ?? stockImport.EmployeeId;
                if (dto.Time.HasValue)
                {
                    // Xử lý Time: Parse time từ client (có thể là string hoặc DateTime)
                    DateTime updateTime = dto.Time.Value;
                    if (updateTime.Kind == DateTimeKind.Utc)
                    {
                        updateTime = updateTime.ToLocalTime();
                    }
                    // Nếu là Unspecified (từ string parse), giữ nguyên (đã là local time)
                    else if (updateTime.Kind == DateTimeKind.Unspecified)
                    {
                        updateTime = DateTime.SpecifyKind(updateTime, DateTimeKind.Local);
                    }
                    stockImport.Time = updateTime;
                }

                // Xóa ProductSerial và trừ lại Quantity của ProductConfiguration từ chi tiết cũ trước khi xóa
                if (stockImport.StockImportDetails != null && stockImport.StockImportDetails.Any())
                {
                    DateTime oldImportDate = stockImport.Time ?? DateTime.Now;
                    
                    foreach (var oldDetail in stockImport.StockImportDetails)
                    {
                        if (!string.IsNullOrEmpty(oldDetail.ProductId) && 
                            oldDetail.Quantity.HasValue && 
                            oldDetail.Quantity.Value > 0 &&
                            !string.IsNullOrEmpty(oldDetail.Specifications))
                        {
                            // Xóa ProductSerial của chi tiết cũ
                            await DeleteProductSerials(
                                oldDetail.ProductId, 
                                oldDetail.Specifications, 
                                oldDetail.Quantity.Value, 
                                oldImportDate);
                            
                            // Trừ lại Quantity của ProductConfiguration
                            await UpdateProductConfigurationQuantity(
                                oldDetail.ProductId, 
                                oldDetail.Specifications, 
                                -oldDetail.Quantity.Value); // Trừ lại
                        }
                    }
                }

                // Xóa chi tiết cũ
                _context.StockImportDetails.RemoveRange(stockImport.StockImportDetails);

                // Tính tổng tiền từ chi tiết mới
                decimal totalAmount = 0;
                if (dto.Details != null && dto.Details.Any())
                {
                    totalAmount = dto.Details.Sum(d => (d.Quantity ?? 0) * (d.Price ?? 0));
                }

                stockImport.TotalAmount = dto.TotalAmount ?? totalAmount;

                // Thêm chi tiết mới với ID tự động và cập nhật Quantity của ProductConfiguration
                if (dto.Details != null && dto.Details.Any())
                {
                    // Lấy số lớn nhất hiện có để tạo ID tuần tự
                    int startNumber = GetMaxStockImportDetailNumber();
                    
                    int detailIndex = 0;
                    foreach (var detailDto in dto.Details)
                    {
                        string detailId = $"STID{(startNumber + detailIndex + 1):D4}";
                        var detail = new StockImportDetail
                        {
                            StockImportDetailId = detailId,
                            StockImportId = id,
                            ProductId = detailDto.ProductId,
                            Specifications = detailDto.Specifications,
                            Quantity = detailDto.Quantity,
                            Price = detailDto.Price
                        };
                        _context.StockImportDetails.Add(detail);
                        
                        // Cập nhật Quantity của ProductConfiguration và tạo ProductSerial mới
                        if (!string.IsNullOrEmpty(detailDto.ProductId) && detailDto.Quantity.HasValue && detailDto.Quantity.Value > 0)
                        {
                            await UpdateProductConfigurationQuantity(detailDto.ProductId, detailDto.Specifications, detailDto.Quantity.Value);
                            
                            // Tạo ProductSerial mới cho chi tiết mới
                            DateTime newImportDate = stockImport.Time ?? DateTime.Now;
                            await CreateProductSerials(
                                detailDto.ProductId, 
                                detailDto.Specifications, 
                                detailDto.Quantity.Value, 
                                newImportDate);
                        }
                        
                        detailIndex++;
                    }
                }

                await _context.SaveChangesAsync();

                // Load lại để lấy thông tin đầy đủ
                await _context.Entry(stockImport)
                    .Reference(si => si.Supplier)
                    .LoadAsync();
                await _context.Entry(stockImport)
                    .Reference(si => si.Employee)
                    .LoadAsync();
                await _context.Entry(stockImport)
                    .Collection(si => si.StockImportDetails)
                    .Query()
                    .Include(d => d.Product)
                    .LoadAsync();

                var result = new StockImportDTO
                {
                    StockImportId = stockImport.StockImportId,
                    SupplierId = stockImport.SupplierId,
                    SupplierName = stockImport.Supplier?.SupplierName,
                    EmployeeId = stockImport.EmployeeId,
                    EmployeeName = stockImport.Employee?.EmployeeName,
                    Time = stockImport.Time,
                    TotalAmount = stockImport.TotalAmount,
                    Details = stockImport.StockImportDetails?.Select(detail => new StockImportDetailDTO
                    {
                        StockImportDetailId = detail.StockImportDetailId,
                        StockImportId = detail.StockImportId,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product?.ProductName,
                        ProductModel = detail.Product?.ProductModel,
                        Specifications = detail.Specifications,
                        Quantity = detail.Quantity,
                        Price = detail.Price
                    }).ToList()
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật phiếu nhập hàng", error = ex.Message });
            }
        }

        // DELETE: api/admin/stock-imports/{id}
        // Xóa phiếu nhập hàng
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStockImport(string id)
        {
            try
            {
                var stockImport = await _context.StockImports
                    .Include(si => si.StockImportDetails)
                    .FirstOrDefaultAsync(si => si.StockImportId == id);

                if (stockImport == null)
                {
                    return NotFound(new { message = "Không tìm thấy phiếu nhập hàng" });
                }

                // Xóa ProductSerial tương ứng và trừ lại Quantity của ProductConfiguration từ chi tiết trước khi xóa
                if (stockImport.StockImportDetails != null && stockImport.StockImportDetails.Any())
                {
                    DateTime importDate = stockImport.Time ?? DateTime.Now;
                    
                    foreach (var detail in stockImport.StockImportDetails)
                    {
                        if (!string.IsNullOrEmpty(detail.ProductId) && 
                            detail.Quantity.HasValue && 
                            detail.Quantity.Value > 0 &&
                            !string.IsNullOrEmpty(detail.Specifications))
                        {
                            // Xóa ProductSerial tương ứng
                            await DeleteProductSerials(
                                detail.ProductId, 
                                detail.Specifications, 
                                detail.Quantity.Value, 
                                importDate);
                            
                            // Trừ lại Quantity của ProductConfiguration
                            await UpdateProductConfigurationQuantity(
                                detail.ProductId, 
                                detail.Specifications, 
                                -detail.Quantity.Value); // Trừ lại
                        }
                    }
                }

                // Xóa chi tiết trước
                _context.StockImportDetails.RemoveRange(stockImport.StockImportDetails);
                // Xóa phiếu nhập
                _context.StockImports.Remove(stockImport);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa phiếu nhập hàng thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa phiếu nhập hàng", error = ex.Message });
            }
        }

        // Helper methods
        private string GenerateStockImportId()
        {
            // Tìm số lớn nhất trong các ID có format SI001, SI002...
            var allIds = _context.StockImports
                .Select(si => si.StockImportId)
                .Where(id => id != null && id.StartsWith("SI") && id.Length == 5)
                .ToList();

            int maxNumber = 0;
            foreach (var id in allIds)
            {
                if (id.Length >= 3 && int.TryParse(id.Substring(2), out int num))
                {
                    maxNumber = Math.Max(maxNumber, num);
                }
            }

            // Trả về ID tiếp theo
            return $"SI{(maxNumber + 1):D3}";
        }

        // Lấy số lớn nhất trong các ID chi tiết có format STID0001, STID0002...
        private int GetMaxStockImportDetailNumber()
        {
            var allDetailIds = _context.StockImportDetails
                .Select(d => d.StockImportDetailId)
                .Where(id => id != null && id.StartsWith("STID") && id.Length == 8)
                .ToList();

            int maxNumber = 0;
            foreach (var id in allDetailIds)
            {
                if (id.Length >= 5 && int.TryParse(id.Substring(4), out int num))
                {
                    maxNumber = Math.Max(maxNumber, num);
                }
            }

            return maxNumber;
        }

        private string GenerateStockImportDetailId()
        {
            // Tìm số lớn nhất và trả về ID tiếp theo
            int maxNumber = GetMaxStockImportDetailNumber();
            return $"STID{(maxNumber + 1):D4}";
        }

        // Cập nhật Quantity của ProductConfiguration khi nhập hàng
        private async Task UpdateProductConfigurationQuantity(string productId, string? specifications, int importQuantity)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(specifications))
            {
                return;
            }

            try
            {
                // Parse Specifications string (format: "CPU: Intel, RAM: 8GB, ROM: 256GB, Card: NVIDIA")
                var specDict = ParseSpecifications(specifications);
                
                // Tìm ProductConfiguration khớp với ProductId và các thông số
                var query = _context.ProductConfigurations
                    .Where(pc => pc.ProductId == productId);

                // Lọc theo CPU nếu có
                if (specDict.ContainsKey("CPU") && !string.IsNullOrEmpty(specDict["CPU"]))
                {
                    query = query.Where(pc => pc.Cpu == specDict["CPU"]);
                }

                // Lọc theo RAM nếu có
                if (specDict.ContainsKey("RAM") && !string.IsNullOrEmpty(specDict["RAM"]))
                {
                    query = query.Where(pc => pc.Ram == specDict["RAM"]);
                }

                // Lọc theo ROM nếu có
                if (specDict.ContainsKey("ROM") && !string.IsNullOrEmpty(specDict["ROM"]))
                {
                    query = query.Where(pc => pc.Rom == specDict["ROM"]);
                }

                // Lọc theo Card nếu có
                if (specDict.ContainsKey("Card") && !string.IsNullOrEmpty(specDict["Card"]))
                {
                    query = query.Where(pc => pc.Card == specDict["Card"]);
                }

                var productConfig = await query.FirstOrDefaultAsync();

                if (productConfig != null)
                {
                    // Cập nhật Quantity (cộng thêm số lượng nhập vào)
                    productConfig.Quantity = (productConfig.Quantity ?? 0) + importQuantity;
                }
                else
                {
                    // Nếu không tìm thấy, có thể log warning hoặc tạo mới (tùy business logic)
                    // Ở đây chỉ log warning
                    System.Diagnostics.Debug.WriteLine($"Warning: Không tìm thấy ProductConfiguration cho ProductId: {productId}, Specifications: {specifications}");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không ảnh hưởng đến việc tạo phiếu nhập hàng
                System.Diagnostics.Debug.WriteLine($"Error updating ProductConfiguration Quantity: {ex.Message}");
            }
        }

        // Parse Specifications string thành dictionary
        private Dictionary<string, string> ParseSpecifications(string specifications)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(specifications))
            {
                return result;
            }

            // Format: "CPU: Intel, RAM: 8GB, ROM: 256GB, Card: NVIDIA"
            var parts = specifications.Split(',');
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                var colonIndex = trimmedPart.IndexOf(':');
                
                if (colonIndex > 0 && colonIndex < trimmedPart.Length - 1)
                {
                    var key = trimmedPart.Substring(0, colonIndex).Trim();
                    var value = trimmedPart.Substring(colonIndex + 1).Trim();
                    
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        result[key] = value;
                    }
                }
            }

            return result;
        }

        // Lấy ConfigurationId từ ProductId và Specifications
        private async Task<string?> GetConfigurationId(string productId, string? specifications)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(specifications))
            {
                return null;
            }

            try
            {
                // Parse Specifications string
                var specDict = ParseSpecifications(specifications);
                
                // Tìm ProductConfiguration khớp với ProductId và các thông số
                var query = _context.ProductConfigurations
                    .Where(pc => pc.ProductId == productId);

                // Lọc theo CPU nếu có
                if (specDict.ContainsKey("CPU") && !string.IsNullOrEmpty(specDict["CPU"]))
                {
                    query = query.Where(pc => pc.Cpu == specDict["CPU"]);
                }

                // Lọc theo RAM nếu có
                if (specDict.ContainsKey("RAM") && !string.IsNullOrEmpty(specDict["RAM"]))
                {
                    query = query.Where(pc => pc.Ram == specDict["RAM"]);
                }

                // Lọc theo ROM nếu có
                if (specDict.ContainsKey("ROM") && !string.IsNullOrEmpty(specDict["ROM"]))
                {
                    query = query.Where(pc => pc.Rom == specDict["ROM"]);
                }

                // Lọc theo Card nếu có
                if (specDict.ContainsKey("Card") && !string.IsNullOrEmpty(specDict["Card"]))
                {
                    query = query.Where(pc => pc.Card == specDict["Card"]);
                }

                var productConfig = await query.FirstOrDefaultAsync();
                return productConfig?.ConfigurationId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting ConfigurationId: {ex.Message}");
                return null;
            }
        }

        // Tạo ProductSerial cho mỗi đơn vị sản phẩm
        private async Task CreateProductSerials(string productId, string? specifications, int quantity, DateTime importDate)
        {
            if (string.IsNullOrEmpty(productId) || quantity <= 0)
            {
                return;
            }

            try
            {
                // Lấy ConfigurationId
                var configurationId = await GetConfigurationId(productId, specifications);
                
                if (string.IsNullOrEmpty(configurationId))
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Không tìm thấy ConfigurationId cho ProductId: {productId}, Specifications: {specifications}");
                    return;
                }

                // Tìm số lớn nhất của SerialID có format SR+ProductId+ConfigurationId+001
                string prefix = $"SR{productId}{configurationId}";
                var existingSerials = await _context.ProductSerials
                    .Where(ps => ps.SerialId != null && ps.SerialId.StartsWith(prefix))
                    .Select(ps => ps.SerialId)
                    .ToListAsync();

                int maxNumber = 0;
                foreach (var serialId in existingSerials)
                {
                    if (serialId != null && serialId.StartsWith(prefix) && serialId.Length > prefix.Length)
                    {
                        string numberPart = serialId.Substring(prefix.Length);
                        if (int.TryParse(numberPart, out int num))
                        {
                            maxNumber = Math.Max(maxNumber, num);
                        }
                    }
                }

                // Tạo ProductSerial cho mỗi đơn vị
                for (int i = 1; i <= quantity; i++)
                {
                    int serialNumber = maxNumber + i;
                    string serialId = $"{prefix}{serialNumber:D3}";

                    var productSerial = new ProductSerial
                    {
                        SerialId = serialId,
                        ProductId = productId,
                        Specifications = specifications,
                        StockExportDetailId = null,
                        Status = "in stock",
                        ImportDate = importDate,
                        ExportDate = null,
                        WarrantyStartDate = null,
                        WarrantyEndDate = null,
                        Note = null
                    };

                    _context.ProductSerials.Add(productSerial);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating ProductSerials: {ex.Message}");
            }
        }

        // Xóa ProductSerial tương ứng với phiếu nhập hàng
        private async Task DeleteProductSerials(string productId, string? specifications, int quantity, DateTime importDate)
        {
            if (string.IsNullOrEmpty(productId) || quantity <= 0)
            {
                return;
            }

            try
            {
                // Tìm các ProductSerial có ProductId, Specifications, ImportDate khớp và Status = "in stock"
                // Chỉ xóa những serial chưa được xuất (in stock)
                var productSerials = await _context.ProductSerials
                    .Where(ps => ps.ProductId == productId &&
                                 ps.Specifications == specifications &&
                                 ps.ImportDate.HasValue &&
                                 ps.ImportDate.Value.Date == importDate.Date &&
                                 ps.Status == "in stock")
                    .OrderBy(ps => ps.SerialId) // Sắp xếp để xóa theo thứ tự
                    .Take(quantity) // Chỉ lấy đúng số lượng cần xóa
                    .ToListAsync();

                if (productSerials.Count > 0)
                {
                    _context.ProductSerials.RemoveRange(productSerials);
                    System.Diagnostics.Debug.WriteLine($"Deleted {productSerials.Count} ProductSerials for ProductId: {productId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Không tìm thấy ProductSerial để xóa cho ProductId: {productId}, Specifications: {specifications}, ImportDate: {importDate}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting ProductSerials: {ex.Message}");
            }
        }

    }

    // DTO cho sản phẩm (select)
    public class ProductSelectDTO
    {
        public string ProductId { get; set; } = null!;
        public string? ProductName { get; set; }
        public string? ProductModel { get; set; }
        public string DisplayName { get; set; } = null!;
    }

}
