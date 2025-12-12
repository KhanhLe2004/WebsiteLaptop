using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;
using WebLaptopBE.Services;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/stock-exports")]
    [ApiController]
    public class ManageStockExportAPIController : ControllerBase
    {
        private readonly Testlaptop37Context _context;
        private readonly HistoryService _historyService;

        public ManageStockExportAPIController(Testlaptop37Context context, HistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        // Helper method để lấy EmployeeId từ header
        private string? GetEmployeeId()
        {
            return Request.Headers["X-Employee-Id"].FirstOrDefault();
        }

        // GET: api/admin/stock-exports
        // Lấy danh sách phiếu xuất hàng có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<StockExportDTO>>> GetStockExports(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.StockExports
                    .Include(se => se.SaleInvoice)
                    .Include(se => se.Employee)
                    .AsQueryable();

                // Tìm kiếm theo mã phiếu xuất, mã hóa đơn bán, tên nhân viên
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(se =>
                        se.StockExportId.ToLower().Contains(searchTerm) ||
                        (se.SaleInvoice != null && se.SaleInvoice.SaleInvoiceId != null && se.SaleInvoice.SaleInvoiceId.ToLower().Contains(searchTerm)) ||
                        (se.Employee != null && se.Employee.EmployeeName != null && se.Employee.EmployeeName.ToLower().Contains(searchTerm)));
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var stockExports = await query
                    .OrderByDescending(se => se.Time)
                    .ThenByDescending(se => se.StockExportId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var stockExportDTOs = new List<StockExportDTO>();
                foreach (var se in stockExports)
                {
                    // Load details để tính tổng tiền
                    await _context.Entry(se)
                        .Collection(s => s.StockExportDetails)
                        .LoadAsync();
                    
                    decimal totalAmount = 0;
                    int totalQuantity = 0;
                    if (se.StockExportDetails != null && se.StockExportDetails.Any())
                    {
                        foreach (var detail in se.StockExportDetails)
                        {
                            // Lấy giá từ ProductConfiguration
                            var price = await GetProductPrice(detail.ProductId, detail.Specifications);
                            totalAmount += (detail.Quantity ?? 0) * (price ?? 0);
                            totalQuantity += detail.Quantity ?? 0;
                        }
                    }
                    
                    stockExportDTOs.Add(new StockExportDTO
                    {
                        StockExportId = se.StockExportId,
                        SaleInvoiceId = se.SaleInvoiceId,
                        EmployeeId = se.EmployeeId,
                        EmployeeName = se.Employee?.EmployeeName,
                        Time = se.Time,
                        TotalAmount = totalAmount,
                        TotalQuantity = totalQuantity,
                        Status = se.Status
                    });
                }

                var result = new PagedResult<StockExportDTO>
                {
                    Items = stockExportDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách phiếu xuất hàng", error = ex.Message });
            }
        }

        // GET: api/admin/stock-exports/sale-invoices
        // Lấy danh sách hóa đơn bán
        [HttpGet("sale-invoices")]
        public async Task<ActionResult<List<SaleInvoiceSelectDTO>>> GetSaleInvoices()
        {
            try
            {
                // Lấy tất cả hóa đơn bán
                var saleInvoices = await _context.SaleInvoices
                    .OrderByDescending(si => si.TimeCreate)
                    .Select(si => new SaleInvoiceSelectDTO
                    {
                        SaleInvoiceId = si.SaleInvoiceId,
                        DisplayName = si.SaleInvoiceId
                    })
                    .ToListAsync();

                return Ok(saleInvoices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách hóa đơn bán", error = ex.Message });
            }
        }

        // GET: api/admin/stock-exports/products
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

        // GET: api/admin/stock-exports/product-configurations/{productId}
        // Lấy danh sách cấu hình của sản phẩm
        [HttpGet("product-configurations/{productId}")]
        public async Task<ActionResult<List<ProductConfigurationDTO>>> GetProductConfigurations(string productId)
        {
            try
            {
                var configurations = await _context.ProductConfigurations
                    .Where(pc => pc.ProductId == productId && (pc.Quantity ?? 0) > 0) // Chỉ lấy cấu hình còn hàng
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

        // GET: api/admin/stock-exports/{id}
        // Lấy chi tiết một phiếu xuất hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<StockExportDTO>> GetStockExport(string id)
        {
            try
            {
                var stockExport = await _context.StockExports
                    .Include(se => se.SaleInvoice)
                    .Include(se => se.Employee)
                    .Include(se => se.StockExportDetails)
                    .FirstOrDefaultAsync(se => se.StockExportId == id);

                if (stockExport == null)
                {
                    return NotFound(new { message = "Không tìm thấy phiếu xuất hàng" });
                }

                // Load Product cho mỗi detail
                var detailsList = new List<StockExportDetailDTO>();
                decimal totalAmount = 0;
                int totalQuantity = 0;
                
                if (stockExport.StockExportDetails != null)
                {
                    foreach (var detail in stockExport.StockExportDetails)
                    {
                        // Load Product nếu có ProductId
                        Product? product = null;
                        if (!string.IsNullOrEmpty(detail.ProductId))
                        {
                            product = await _context.Products
                                .FirstOrDefaultAsync(p => p.ProductId == detail.ProductId);
                        }
                        
                        // Lấy giá từ ProductConfiguration
                        var price = await GetProductPrice(detail.ProductId, detail.Specifications);
                        totalAmount += (detail.Quantity ?? 0) * (price ?? 0);
                        totalQuantity += detail.Quantity ?? 0;
                        
                        detailsList.Add(new StockExportDetailDTO
                        {
                            StockExportDetailId = detail.StockExportDetailId,
                            StockExportId = detail.StockExportId,
                            ProductId = detail.ProductId,
                            ProductName = product?.ProductName,
                            ProductModel = product?.ProductModel,
                            Specifications = detail.Specifications,
                            Quantity = detail.Quantity,
                            Price = price
                        });
                    }
                }
                
                var stockExportDTO = new StockExportDTO
                {
                    StockExportId = stockExport.StockExportId,
                    SaleInvoiceId = stockExport.SaleInvoiceId,
                    EmployeeId = stockExport.EmployeeId,
                    EmployeeName = stockExport.Employee?.EmployeeName,
                    Time = stockExport.Time,
                    TotalAmount = totalAmount,
                    TotalQuantity = totalQuantity,
                    Status = stockExport.Status,
                    Details = detailsList
                };

                return Ok(stockExportDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin phiếu xuất hàng", error = ex.Message });
            }
        }

        // POST: api/admin/stock-exports
        // Tạo mới phiếu xuất hàng
        [HttpPost]
        public async Task<ActionResult<StockExportDTO>> CreateStockExport([FromBody] StockExportCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                // Tạo mã phiếu xuất nếu chưa có
                string stockExportId = dto.StockExportId ?? GenerateStockExportId();

                // Kiểm tra mã đã tồn tại chưa
                var existing = await _context.StockExports
                    .FirstOrDefaultAsync(se => se.StockExportId == stockExportId);
                if (existing != null)
                {
                    return BadRequest(new { message = "Mã phiếu xuất hàng đã tồn tại" });
                }

                // Xử lý Time: Parse time từ client (có thể là string hoặc DateTime)
                // Note: TotalAmount sẽ được tính lại từ ProductConfiguration sau khi save
                DateTime? exportTime = dto.Time;
                if (exportTime.HasValue)
                {
                    // Nếu là UTC, convert về local time
                    if (exportTime.Value.Kind == DateTimeKind.Utc)
                    {
                        exportTime = exportTime.Value.ToLocalTime();
                    }
                    // Nếu là Unspecified (từ string parse), giữ nguyên (đã là local time)
                    else if (exportTime.Value.Kind == DateTimeKind.Unspecified)
                    {
                        // Giữ nguyên, coi như local time
                        exportTime = DateTime.SpecifyKind(exportTime.Value, DateTimeKind.Local);
                    }
                }
                else
                {
                    exportTime = DateTime.Now;
                }

                // Lấy SaleInvoiceId từ DTO
                string? saleInvoiceId = dto.SaleInvoiceId;
                
                // Lấy Status từ DTO, mặc định là "Chờ xử lý" nếu không có
                string status = !string.IsNullOrEmpty(dto.Status) ? dto.Status : "Chờ xử lý";

                var stockExport = new StockExport
                {
                    StockExportId = stockExportId,
                    SaleInvoiceId = saleInvoiceId,
                    EmployeeId = dto.EmployeeId,
                    Time = exportTime.Value,
                    Status = status
                };

                _context.StockExports.Add(stockExport);

                // Thêm chi tiết với ID tự động và cập nhật Quantity của ProductConfiguration
                if (dto.Details != null && dto.Details.Any())
                {
                    // Lấy số lớn nhất hiện có để tạo ID tuần tự
                    int startNumber = GetMaxStockExportDetailNumber();
                    
                    int detailIndex = 0;
                    foreach (var detailDto in dto.Details)
                    {
                        string detailId = $"STED{(startNumber + detailIndex + 1):D4}";
                        var detail = new StockExportDetail
                        {
                            StockExportDetailId = detailId,
                            StockExportId = stockExportId,
                            ProductId = detailDto.ProductId,
                            Specifications = detailDto.Specifications,
                            Quantity = detailDto.Quantity
                            // Note: StockExportDetail model không có Price field
                            // Giá sẽ được lấy từ ProductConfiguration khi cần
                        };
                        _context.StockExportDetails.Add(detail);
                        
                        // Cập nhật Quantity của ProductConfiguration (trừ đi)
                        if (!string.IsNullOrEmpty(detailDto.ProductId) && detailDto.Quantity.HasValue && detailDto.Quantity.Value > 0)
                        {
                            // Chỉ trừ quantity và cập nhật ProductSerial khi status là "Hoàn thành"
                            if (status == "Hoàn thành")
                            {
                                // Kiểm tra số lượng tồn kho
                                var availableQuantity = await GetAvailableQuantity(detailDto.ProductId, detailDto.Specifications);
                                if (availableQuantity < detailDto.Quantity.Value)
                                {
                                    return BadRequest(new { message = $"Số lượng tồn kho không đủ cho sản phẩm {detailDto.ProductId}. Tồn kho: {availableQuantity}, Yêu cầu: {detailDto.Quantity.Value}" });
                                }

                                // Trừ quantity của ProductConfiguration
                                await UpdateProductConfigurationQuantity(detailDto.ProductId, detailDto.Specifications, -detailDto.Quantity.Value);
                                
                                // Cập nhật ProductSerial
                                await UpdateProductSerials(
                                    detailDto.ProductId, 
                                    detailDto.Specifications, 
                                    detailDto.Quantity.Value, 
                                    detailId,
                                    exportTime.Value);
                            }
                        }
                        
                        detailIndex++;
                    }
                }

                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId() ?? dto.EmployeeId;
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Thêm phiếu xuất hàng: {stockExportId}");
                }

                // Load lại để lấy thông tin đầy đủ
                await _context.Entry(stockExport)
                    .Reference(se => se.SaleInvoice)
                    .LoadAsync();
                await _context.Entry(stockExport)
                    .Reference(se => se.Employee)
                    .LoadAsync();
                await _context.Entry(stockExport)
                    .Collection(se => se.StockExportDetails)
                    .LoadAsync();

                // Load Product và tính giá cho mỗi detail
                var detailsList = new List<StockExportDetailDTO>();
                decimal totalAmount = 0;
                int totalQuantity = 0;
                
                if (stockExport.StockExportDetails != null)
                {
                    foreach (var detail in stockExport.StockExportDetails)
                    {
                        // Load Product nếu có ProductId
                        Product? product = null;
                        if (!string.IsNullOrEmpty(detail.ProductId))
                        {
                            product = await _context.Products
                                .FirstOrDefaultAsync(p => p.ProductId == detail.ProductId);
                        }
                        
                        // Lấy giá từ ProductConfiguration
                        var price = await GetProductPrice(detail.ProductId, detail.Specifications);
                        totalAmount += (detail.Quantity ?? 0) * (price ?? 0);
                        totalQuantity += detail.Quantity ?? 0;
                        
                        detailsList.Add(new StockExportDetailDTO
                        {
                            StockExportDetailId = detail.StockExportDetailId,
                            StockExportId = detail.StockExportId,
                            ProductId = detail.ProductId,
                            ProductName = product?.ProductName,
                            ProductModel = product?.ProductModel,
                            Specifications = detail.Specifications,
                            Quantity = detail.Quantity,
                            Price = price
                        });
                    }
                }

                var result = new StockExportDTO
                {
                    StockExportId = stockExport.StockExportId,
                    SaleInvoiceId = stockExport.SaleInvoiceId,
                    EmployeeId = stockExport.EmployeeId,
                    EmployeeName = stockExport.Employee?.EmployeeName,
                    Time = stockExport.Time,
                    TotalAmount = totalAmount,
                    TotalQuantity = totalQuantity,
                    Status = stockExport.Status,
                    Details = detailsList
                };

                return CreatedAtAction(nameof(GetStockExport), new { id = stockExport.StockExportId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo phiếu xuất hàng", error = ex.Message });
            }
        }

        // PUT: api/admin/stock-exports/{id}
        // Cập nhật phiếu xuất hàng
        [HttpPut("{id}")]
        public async Task<ActionResult<StockExportDTO>> UpdateStockExport(string id, [FromBody] StockExportUpdateDTO dto)
        {
            // Sử dụng transaction để đảm bảo tính nhất quán
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                var stockExport = await _context.StockExports
                    .Include(se => se.StockExportDetails)
                    .FirstOrDefaultAsync(se => se.StockExportId == id);

                if (stockExport == null)
                {
                    await transaction.RollbackAsync();
                    return NotFound(new { message = "Không tìm thấy phiếu xuất hàng" });
                }

                // Lưu status cũ để xử lý ProductSerial
                string? oldStatus = stockExport.Status;
                
                // Cập nhật thông tin chung
                stockExport.SaleInvoiceId = dto.SaleInvoiceId ?? stockExport.SaleInvoiceId;
                
                // Cập nhật EmployeeId: ưu tiên từ dto, nếu không có thì lấy từ header, nếu không có thì giữ nguyên
                if (!string.IsNullOrEmpty(dto.EmployeeId))
                {
                    stockExport.EmployeeId = dto.EmployeeId;
                }
                else
                {
                    var employeeIdFromHeader = GetEmployeeId();
                    if (!string.IsNullOrEmpty(employeeIdFromHeader))
                    {
                        stockExport.EmployeeId = employeeIdFromHeader;
                    }
                    // Nếu không có cả hai, giữ nguyên EmployeeId hiện tại
                }
                
                stockExport.Status = dto.Status ?? stockExport.Status;
                
                // Cập nhật thời gian khi trạng thái chuyển sang "Hoàn thành"
                if (oldStatus != stockExport.Status && stockExport.Status == "Hoàn thành")
                {
                    stockExport.Time = DateTime.Now;
                }
                
                // Xử lý ProductSerial khi thay đổi status (không có thay đổi chi tiết)
                bool statusChanged = oldStatus != stockExport.Status;
                if (statusChanged && (dto.Details == null || !dto.Details.Any()))
                {
                    // Nếu chuyển từ "Chờ xử lý" sang "Hoàn thành" → trừ quantity và cập nhật ProductSerial
                    if (oldStatus == "Chờ xử lý" && stockExport.Status == "Hoàn thành")
                    {
                        // Load chi tiết để trừ quantity và cập nhật ProductSerial
                        if (stockExport.StockExportDetails != null && stockExport.StockExportDetails.Any())
                        {
                            foreach (var detail in stockExport.StockExportDetails)
                            {
                                if (!string.IsNullOrEmpty(detail.ProductId) && 
                                    detail.Quantity.HasValue && 
                                    detail.Quantity.Value > 0 &&
                                    !string.IsNullOrEmpty(detail.Specifications))
                                {
                                    // Kiểm tra số lượng tồn kho
                                    var availableQuantity = await GetAvailableQuantity(detail.ProductId, detail.Specifications);
                                    if (availableQuantity < detail.Quantity.Value)
                                    {
                                        await transaction.RollbackAsync();
                                        return BadRequest(new { message = $"Số lượng tồn kho không đủ cho sản phẩm {detail.ProductId}. Tồn kho: {availableQuantity}, Yêu cầu: {detail.Quantity.Value}" });
                                    }

                                    // Trừ quantity của ProductConfiguration
                                    await UpdateProductConfigurationQuantity(detail.ProductId, detail.Specifications, -detail.Quantity.Value);
                                    
                                    // Cập nhật ProductSerial
                                    DateTime exportDate = stockExport.Time ?? DateTime.Now;
                                    await UpdateProductSerials(
                                        detail.ProductId,
                                        detail.Specifications,
                                        detail.Quantity.Value,
                                        detail.StockExportDetailId,
                                        exportDate);
                                }
                            }
                        }
                        
                        // Cập nhật trạng thái đơn hàng thành "Chờ vận chuyển" khi phiếu xuất hàng hoàn thành
                        if (!string.IsNullOrEmpty(stockExport.SaleInvoiceId))
                        {
                            var saleInvoice = await _context.SaleInvoices
                                .FirstOrDefaultAsync(si => si.SaleInvoiceId == stockExport.SaleInvoiceId);
                            
                            if (saleInvoice != null && saleInvoice.Status == "Đang xử lý")
                            {
                                saleInvoice.Status = "Chờ vận chuyển";
                                
                                // Ghi log lịch sử cho đơn hàng
                                var employeeIdForInvoice = GetEmployeeId() ?? stockExport.EmployeeId;
                                if (!string.IsNullOrEmpty(employeeIdForInvoice))
                                {
                                    await _historyService.LogHistoryAsync(employeeIdForInvoice, 
                                        $"Cập nhật trạng thái đơn hàng tự động: {saleInvoice.SaleInvoiceId} - Đang xử lý → Chờ vận chuyển (do phiếu xuất hàng {stockExport.StockExportId} hoàn thành)");
                                }
                            }
                        }
                    }
                    // Nếu chuyển từ "Hoàn thành" sang "Chờ xử lý" → cộng lại quantity và khôi phục ProductSerial
                    else if (oldStatus == "Hoàn thành" && stockExport.Status == "Chờ xử lý")
                    {
                        // Khôi phục ProductSerial và quantity từ chi tiết
                        if (stockExport.StockExportDetails != null && stockExport.StockExportDetails.Any())
                        {
                            foreach (var detail in stockExport.StockExportDetails)
                            {
                                if (!string.IsNullOrEmpty(detail.StockExportDetailId))
                                {
                                    // RestoreProductSerials sẽ tự động cộng lại quantity
                                    await RestoreProductSerials(detail.StockExportDetailId);
                                }
                            }
                        }
                    }
                }
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
                    stockExport.Time = updateTime;
                }

                // Chỉ cập nhật chi tiết sản phẩm nếu dto.Details được cung cấp và không rỗng
                // Nếu dto.Details là null hoặc empty, giữ nguyên chi tiết cũ
                if (dto.Details != null && dto.Details.Any())
                {
                    // Khôi phục ProductSerial và cộng lại Quantity của ProductConfiguration từ chi tiết cũ trước khi xóa
                    // Chỉ khôi phục nếu status cũ là "Hoàn thành" (vì chỉ khi đó quantity mới bị trừ)
                    if (oldStatus == "Hoàn thành" && 
                        stockExport.StockExportDetails != null && 
                        stockExport.StockExportDetails.Any())
                    {
                        foreach (var oldDetail in stockExport.StockExportDetails)
                        {
                            if (!string.IsNullOrEmpty(oldDetail.StockExportDetailId))
                            {
                                // RestoreProductSerials sẽ tự động cộng lại quantity
                                await RestoreProductSerials(oldDetail.StockExportDetailId);
                            }
                        }
                        
                        // Save changes để đảm bảo các thay đổi được áp dụng trước khi kiểm tra số lượng tồn kho
                        await _context.SaveChangesAsync();
                    }

                    // Kiểm tra số lượng tồn kho cho tất cả chi tiết mới (chỉ khi status mới là "Hoàn thành")
                    if (stockExport.Status == "Hoàn thành")
                    {
                        // Tính tổng số lượng cho từng sản phẩm và specifications để kiểm tra chính xác
                        // Nhóm chi tiết theo ProductId và Specifications để tính tổng số lượng
                        var groupedDetails = dto.Details
                            .Where(d => !string.IsNullOrEmpty(d.ProductId) && d.Quantity.HasValue && d.Quantity.Value > 0)
                            .GroupBy(d => new { d.ProductId, d.Specifications })
                            .Select(g => new
                            {
                                ProductId = g.Key.ProductId,
                                Specifications = g.Key.Specifications,
                                TotalQuantity = g.Sum(d => d.Quantity.Value)
                            })
                            .ToList();

                        foreach (var groupedDetail in groupedDetails)
                        {
                            // Kiểm tra số lượng tồn kho (sau khi đã cộng lại từ chi tiết cũ nếu status cũ là "Hoàn thành")
                            var availableQuantity = await GetAvailableQuantity(groupedDetail.ProductId, groupedDetail.Specifications);
                            if (availableQuantity < groupedDetail.TotalQuantity)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new { message = $"Số lượng tồn kho không đủ cho sản phẩm {groupedDetail.ProductId}. Tồn kho: {availableQuantity}, Yêu cầu: {groupedDetail.TotalQuantity}" });
                            }
                        }
                    }

                    // Xóa chi tiết cũ
                    _context.StockExportDetails.RemoveRange(stockExport.StockExportDetails);

                    // Thêm chi tiết mới với ID tự động và cập nhật Quantity của ProductConfiguration
                    // Note: TotalAmount sẽ được tính lại từ ProductConfiguration sau khi save
                    // Lấy số lớn nhất hiện có để tạo ID tuần tự
                    int startNumber = GetMaxStockExportDetailNumber();
                    
                    int detailIndex = 0;
                    foreach (var detailDto in dto.Details)
                    {
                        string detailId = $"STED{(startNumber + detailIndex + 1):D4}";
                        var detail = new StockExportDetail
                        {
                            StockExportDetailId = detailId,
                            StockExportId = id,
                            ProductId = detailDto.ProductId,
                            Specifications = detailDto.Specifications,
                            Quantity = detailDto.Quantity
                            // Note: StockExportDetail model không có Price field
                            // Giá sẽ được lấy từ ProductConfiguration khi cần
                        };
                        _context.StockExportDetails.Add(detail);
                        
                        // Chỉ trừ quantity và cập nhật ProductSerial khi status là "Hoàn thành"
                        if (!string.IsNullOrEmpty(detailDto.ProductId) && detailDto.Quantity.HasValue && detailDto.Quantity.Value > 0)
                        {
                            if (stockExport.Status == "Hoàn thành")
                            {
                                // Kiểm tra số lượng tồn kho
                                var availableQuantity = await GetAvailableQuantity(detailDto.ProductId, detailDto.Specifications);
                                if (availableQuantity < detailDto.Quantity.Value)
                                {
                                    await transaction.RollbackAsync();
                                    return BadRequest(new { message = $"Số lượng tồn kho không đủ cho sản phẩm {detailDto.ProductId}. Tồn kho: {availableQuantity}, Yêu cầu: {detailDto.Quantity.Value}" });
                                }

                                // Trừ quantity của ProductConfiguration
                                await UpdateProductConfigurationQuantity(detailDto.ProductId, detailDto.Specifications, -detailDto.Quantity.Value);
                                
                                // Cập nhật ProductSerial
                                DateTime newExportDate = stockExport.Time ?? DateTime.Now;
                                await UpdateProductSerials(
                                    detailDto.ProductId, 
                                    detailDto.Specifications, 
                                    detailDto.Quantity.Value, 
                                    detailId,
                                    newExportDate);
                            }
                        }
                        
                        detailIndex++;
                    }
                }
                
                // Cập nhật trạng thái đơn hàng thành "Chờ vận chuyển" khi phiếu xuất hàng chuyển thành "Hoàn thành"
                if (statusChanged && oldStatus == "Chờ xử lý" && stockExport.Status == "Hoàn thành")
                {
                    if (!string.IsNullOrEmpty(stockExport.SaleInvoiceId))
                    {
                        var saleInvoice = await _context.SaleInvoices
                            .FirstOrDefaultAsync(si => si.SaleInvoiceId == stockExport.SaleInvoiceId);
                        
                        if (saleInvoice != null && saleInvoice.Status == "Đang xử lý")
                        {
                            saleInvoice.Status = "Chờ vận chuyển";
                            
                            // Ghi log lịch sử cho đơn hàng
                            var employeeIdForInvoice = GetEmployeeId() ?? stockExport.EmployeeId;
                            if (!string.IsNullOrEmpty(employeeIdForInvoice))
                            {
                                await _historyService.LogHistoryAsync(employeeIdForInvoice, 
                                    $"Cập nhật trạng thái đơn hàng tự động: {saleInvoice.SaleInvoiceId} - Đang xử lý → Chờ vận chuyển (do phiếu xuất hàng {stockExport.StockExportId} hoàn thành)");
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId() ?? dto.EmployeeId;
                if (!string.IsNullOrEmpty(employeeId))
                {
                    string logMessage = $"Sửa phiếu xuất hàng: {id}";
                    if (statusChanged)
                    {
                        logMessage += $" - Trạng thái: {oldStatus} → {stockExport.Status}";
                    }
                    await _historyService.LogHistoryAsync(employeeId, logMessage);
                }

                // Load lại để lấy thông tin đầy đủ
                await _context.Entry(stockExport)
                    .Reference(se => se.SaleInvoice)
                    .LoadAsync();
                await _context.Entry(stockExport)
                    .Reference(se => se.Employee)
                    .LoadAsync();
                await _context.Entry(stockExport)
                    .Collection(se => se.StockExportDetails)
                    .LoadAsync();

                // Load Product và tính giá cho mỗi detail
                var detailsList = new List<StockExportDetailDTO>();
                decimal totalAmount = 0;
                int totalQuantity = 0;
                
                if (stockExport.StockExportDetails != null)
                {
                    foreach (var detail in stockExport.StockExportDetails)
                    {
                        // Load Product nếu có ProductId
                        Product? product = null;
                        if (!string.IsNullOrEmpty(detail.ProductId))
                        {
                            product = await _context.Products
                                .FirstOrDefaultAsync(p => p.ProductId == detail.ProductId);
                        }
                        
                        // Lấy giá từ ProductConfiguration
                        var price = await GetProductPrice(detail.ProductId, detail.Specifications);
                        totalAmount += (detail.Quantity ?? 0) * (price ?? 0);
                        totalQuantity += detail.Quantity ?? 0;
                        
                        detailsList.Add(new StockExportDetailDTO
                        {
                            StockExportDetailId = detail.StockExportDetailId,
                            StockExportId = detail.StockExportId,
                            ProductId = detail.ProductId,
                            ProductName = product?.ProductName,
                            ProductModel = product?.ProductModel,
                            Specifications = detail.Specifications,
                            Quantity = detail.Quantity,
                            Price = price
                        });
                    }
                }

                var result = new StockExportDTO
                {
                    StockExportId = stockExport.StockExportId,
                    SaleInvoiceId = stockExport.SaleInvoiceId,
                    EmployeeId = stockExport.EmployeeId,
                    EmployeeName = stockExport.Employee?.EmployeeName,
                    Time = stockExport.Time,
                    TotalAmount = totalAmount,
                    TotalQuantity = totalQuantity,
                    Status = stockExport.Status,
                    Details = detailsList
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Ignore rollback errors
                }
                
                // Log chi tiết lỗi để debug
                System.Diagnostics.Debug.WriteLine($"Error in UpdateStockExport: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                
                // Trả về thông báo lỗi chi tiết hơn
                var errorMessage = "Lỗi khi cập nhật phiếu xuất hàng";
                if (ex.Message.Contains("ProductSerial"))
                {
                    errorMessage = ex.Message;
                }
                else if (ex.Message.Contains("Quantity") || ex.Message.Contains("tồn kho"))
                {
                    errorMessage = ex.Message;
                }
                else
                {
                    errorMessage = $"Lỗi khi cập nhật phiếu xuất hàng: {ex.Message}";
                }
                
                return StatusCode(500, new { message = errorMessage, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // Helper methods
        private string GenerateStockExportId()
        {
            // Tìm số lớn nhất trong các ID có format SE001, SE002...
            var allIds = _context.StockExports
                .Select(se => se.StockExportId)
                .Where(id => id != null && id.StartsWith("SE") && id.Length == 5)
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
            return $"SE{(maxNumber + 1):D3}";
        }

        // Lấy số lớn nhất trong các ID chi tiết có format STED0001, STED0002...
        private int GetMaxStockExportDetailNumber()
        {
            var allDetailIds = _context.StockExportDetails
                .Select(d => d.StockExportDetailId)
                .Where(id => id != null && id.StartsWith("STED") && id.Length == 8)
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

        private string GenerateStockExportDetailId()
        {
            // Tìm số lớn nhất và trả về ID tiếp theo
            int maxNumber = GetMaxStockExportDetailNumber();
            return $"STED{(maxNumber + 1):D4}";
        }

        // Lấy giá sản phẩm từ ProductConfiguration
        private async Task<decimal?> GetProductPrice(string? productId, string? specifications)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(specifications))
            {
                return null;
            }

            try
            {
                // Parse Specifications string
                var specDict = ParseSpecifications(specifications);
                
                // Tìm ProductConfiguration khớp
                var query = _context.ProductConfigurations
                    .Where(pc => pc.ProductId == productId);

                if (specDict.ContainsKey("CPU") && !string.IsNullOrEmpty(specDict["CPU"]))
                {
                    query = query.Where(pc => pc.Cpu == specDict["CPU"]);
                }
                if (specDict.ContainsKey("RAM") && !string.IsNullOrEmpty(specDict["RAM"]))
                {
                    query = query.Where(pc => pc.Ram == specDict["RAM"]);
                }
                if (specDict.ContainsKey("ROM") && !string.IsNullOrEmpty(specDict["ROM"]))
                {
                    query = query.Where(pc => pc.Rom == specDict["ROM"]);
                }
                if (specDict.ContainsKey("Card") && !string.IsNullOrEmpty(specDict["Card"]))
                {
                    query = query.Where(pc => pc.Card == specDict["Card"]);
                }

                var productConfig = await query.FirstOrDefaultAsync();
                return productConfig?.Price;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting product price: {ex.Message}");
                return null;
            }
        }

        // Lấy số lượng tồn kho khả dụng
        private async Task<int> GetAvailableQuantity(string productId, string? specifications)
        {
            if (string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(specifications))
            {
                return 0;
            }

            try
            {
                // Parse Specifications string
                var specDict = ParseSpecifications(specifications);
                
                // Tìm ProductConfiguration khớp
                var query = _context.ProductConfigurations
                    .Where(pc => pc.ProductId == productId);

                if (specDict.ContainsKey("CPU") && !string.IsNullOrEmpty(specDict["CPU"]))
                {
                    query = query.Where(pc => pc.Cpu == specDict["CPU"]);
                }
                if (specDict.ContainsKey("RAM") && !string.IsNullOrEmpty(specDict["RAM"]))
                {
                    query = query.Where(pc => pc.Ram == specDict["RAM"]);
                }
                if (specDict.ContainsKey("ROM") && !string.IsNullOrEmpty(specDict["ROM"]))
                {
                    query = query.Where(pc => pc.Rom == specDict["ROM"]);
                }
                if (specDict.ContainsKey("Card") && !string.IsNullOrEmpty(specDict["Card"]))
                {
                    query = query.Where(pc => pc.Card == specDict["Card"]);
                }

                var productConfig = await query.FirstOrDefaultAsync();
                return productConfig?.Quantity ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting available quantity: {ex.Message}");
                return 0;
            }
        }

        // Cập nhật Quantity của ProductConfiguration khi xuất hàng (trừ đi)
        private async Task UpdateProductConfigurationQuantity(string productId, string? specifications, int exportQuantity)
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
                    // Cập nhật Quantity (trừ đi số lượng xuất)
                    productConfig.Quantity = (productConfig.Quantity ?? 0) + exportQuantity; // exportQuantity có thể âm khi khôi phục
                }
                else
                {
                    // Nếu không tìm thấy, có thể log warning
                    System.Diagnostics.Debug.WriteLine($"Warning: Không tìm thấy ProductConfiguration cho ProductId: {productId}, Specifications: {specifications}");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không throw để không ảnh hưởng đến việc tạo phiếu xuất hàng
                System.Diagnostics.Debug.WriteLine($"Error updating ProductConfiguration Quantity: {ex.Message}");
            }
        }

        // Parse Specifications string thành dictionary
        // Hỗ trợ 2 format:
        // 1. "CPU: Intel, RAM: 8GB, ROM: 256GB, Card: NVIDIA" (có prefix)
        // 2. "Intel / 8GB / 256GB / NVIDIA" (không có prefix, dùng "/")
        private Dictionary<string, string> ParseSpecifications(string specifications)
        {
            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(specifications))
            {
                return result;
            }

            // Kiểm tra xem có format "CPU:", "RAM:", "ROM:", "Card:" không
            if (specifications.Contains("CPU:") || specifications.Contains("RAM:") || 
                specifications.Contains("ROM:") || specifications.Contains("Card:"))
            {
                // Format có prefix: "CPU: Intel, RAM: 8GB, ROM: 256GB, Card: NVIDIA"
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
            }
            else
            {
                // Format không có prefix: "Intel / 8GB / 256GB / NVIDIA"
                // Cần tìm ProductConfiguration để lấy thứ tự các giá trị
                // Tạm thời, giả sử thứ tự là: CPU / RAM / ROM / Card
                var parts = specifications.Split(new[] { '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();
                
                // Nếu có đủ 4 phần, gán theo thứ tự: CPU, RAM, ROM, Card
                if (parts.Count >= 1) result["CPU"] = parts[0];
                if (parts.Count >= 2) result["RAM"] = parts[1];
                if (parts.Count >= 3) result["ROM"] = parts[2];
                if (parts.Count >= 4) result["Card"] = parts[3];
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

        // Cập nhật ProductSerial khi xuất hàng (đánh dấu đã bán - chỉ khi status là "Hoàn thành")
        private async Task UpdateProductSerials(string productId, string? specifications, int quantity, string stockExportDetailId, DateTime exportDate)
        {
            if (string.IsNullOrEmpty(productId) || quantity <= 0)
            {
                return;
            }

            try
            {
                // Lấy thông tin Product để lấy WarrantyPeriod
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductId == productId);
                
                int warrantyPeriodMonths = product?.WarrantyPeriod ?? 12; // Mặc định 12 tháng nếu không có

                // Tìm các ProductSerial có ProductId, Specifications khớp và Status = "in stock" hoặc null/empty
                // Lấy tất cả ProductSerial khớp trước, sau đó filter trong memory để so sánh specifications linh hoạt hơn
                var allProductSerials = await _context.ProductSerials
                    .Where(ps => ps.ProductId == productId &&
                                 (ps.Status == "in stock" || ps.Status == null || ps.Status == ""))
                    .OrderBy(ps => ps.SerialId) // Sắp xếp để xuất theo thứ tự
                    .ToListAsync();

                // So sánh specifications linh hoạt
                // Convert specifications về format chuẩn: "CPU / RAM / ROM / Card" (không có prefix)
                string NormalizeSpec(string? spec)
                {
                    if (string.IsNullOrWhiteSpace(spec)) return "";
                    
                    spec = spec.Trim();
                    
                    // Nếu có format "CPU: ..., RAM: ..., ROM: ..., Card: ..." (có prefix)
                    if (spec.Contains("CPU:") || spec.Contains("RAM:") || spec.Contains("ROM:") || spec.Contains("Card:"))
                    {
                        // Parse và extract giá trị
                        var specDict = ParseSpecifications(spec);
                        var parts = new List<string>();
                        if (specDict.ContainsKey("CPU") && !string.IsNullOrEmpty(specDict["CPU"]))
                            parts.Add(specDict["CPU"]);
                        if (specDict.ContainsKey("RAM") && !string.IsNullOrEmpty(specDict["RAM"]))
                            parts.Add(specDict["RAM"]);
                        if (specDict.ContainsKey("ROM") && !string.IsNullOrEmpty(specDict["ROM"]))
                            parts.Add(specDict["ROM"]);
                        if (specDict.ContainsKey("Card") && !string.IsNullOrEmpty(specDict["Card"]))
                            parts.Add(specDict["Card"]);
                        
                        return string.Join(" / ", parts);
                    }
                    else
                    {
                        // Đã là format "CPU / RAM / ROM / Card" hoặc format khác, normalize khoảng trắng
                        // Normalize dấu phân cách (có thể là "/" hoặc ",")
                        spec = System.Text.RegularExpressions.Regex.Replace(spec, @"\s*/\s*", " / ");
                        spec = System.Text.RegularExpressions.Regex.Replace(spec, @"\s*,\s*", " / ");
                        // Normalize khoảng trắng
                        spec = System.Text.RegularExpressions.Regex.Replace(spec, @"\s+", " ");
                        return spec.Trim();
                    }
                }
                
                string normalizedSpec = NormalizeSpec(specifications);
                // So sánh case-insensitive để tránh vấn đề với ký tự đặc biệt
                var productSerials = allProductSerials
                    .Where(ps => string.Equals(NormalizeSpec(ps.Specifications), normalizedSpec, StringComparison.OrdinalIgnoreCase))
                    .Take(quantity) // Chỉ lấy đúng số lượng cần xuất
                    .ToList();

                if (productSerials.Count < quantity)
                {
                    var availableCount = allProductSerials.Count;
                    var matchingCount = allProductSerials.Count(ps => 
                        string.Equals(NormalizeSpec(ps.Specifications), normalizedSpec, StringComparison.OrdinalIgnoreCase));
                    
                    // Log tất cả ProductSerial có sẵn để debug
                    var availableSerials = allProductSerials.Take(10).Select(ps => new
                    {
                        SerialId = ps.SerialId,
                        ProductId = ps.ProductId,
                        Specifications = ps.Specifications,
                        Status = ps.Status,
                        NormalizedSpec = NormalizeSpec(ps.Specifications)
                    }).ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"ProductSerial search failed:");
                    System.Diagnostics.Debug.WriteLine($"  ProductId: {productId}");
                    System.Diagnostics.Debug.WriteLine($"  Specifications: {specifications}");
                    System.Diagnostics.Debug.WriteLine($"  Normalized Specifications: {normalizedSpec}");
                    System.Diagnostics.Debug.WriteLine($"  Required: {quantity}");
                    System.Diagnostics.Debug.WriteLine($"  Available (all): {availableCount}");
                    System.Diagnostics.Debug.WriteLine($"  Matching specs: {matchingCount}");
                    System.Diagnostics.Debug.WriteLine($"  Found: {productSerials.Count}");
                    foreach (var serial in availableSerials)
                    {
                        System.Diagnostics.Debug.WriteLine($"    - SerialId: {serial.SerialId}, Spec: '{serial.Specifications}', Normalized: '{serial.NormalizedSpec}', Status: {serial.Status}");
                    }
                    
                    throw new Exception($"Không đủ số lượng ProductSerial để xuất. Yêu cầu: {quantity}, Có sẵn: {productSerials.Count}. ProductId: {productId}, Specifications: {specifications}");
                }

                // Cập nhật từng ProductSerial
                foreach (var productSerial in productSerials)
                {
                    productSerial.Status = "sold"; // Thay đổi từ "exported" thành "sold"
                    productSerial.StockExportDetailId = stockExportDetailId;
                    productSerial.ExportDate = exportDate;
                    productSerial.WarrantyStartDate = exportDate;
                    productSerial.WarrantyEndDate = exportDate.AddMonths(warrantyPeriodMonths);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ProductSerials: {ex.Message}");
                throw; // Throw lại để báo lỗi khi không đủ số lượng
            }
        }

        // Khôi phục ProductSerial khi xóa hoặc cập nhật phiếu xuất hàng
        private async Task RestoreProductSerials(string stockExportDetailId)
        {
            if (string.IsNullOrEmpty(stockExportDetailId))
            {
                return;
            }

            try
            {
                // Tìm các ProductSerial có StockExportDetailId khớp và Status = "sold" hoặc "exported"
                var productSerials = await _context.ProductSerials
                    .Where(ps => ps.StockExportDetailId == stockExportDetailId &&
                                 (ps.Status == "sold" || ps.Status == "exported"))
                    .ToListAsync();

                if (productSerials.Count > 0)
                {
                    // Nhóm ProductSerial theo ProductId và Specifications để cập nhật quantity
                    var groupedSerials = productSerials
                        .Where(ps => !string.IsNullOrEmpty(ps.ProductId) && !string.IsNullOrEmpty(ps.Specifications))
                        .GroupBy(ps => new { ps.ProductId, ps.Specifications })
                        .Select(g => new
                        {
                            ProductId = g.Key.ProductId,
                            Specifications = g.Key.Specifications,
                            Count = g.Count()
                        })
                        .ToList();

                    // Khôi phục từng ProductSerial
                    foreach (var productSerial in productSerials)
                    {
                        productSerial.Status = "in stock";
                        productSerial.StockExportDetailId = null;
                        productSerial.ExportDate = null;
                        productSerial.WarrantyStartDate = null;
                        productSerial.WarrantyEndDate = null;
                    }

                    // Khôi phục quantity của ProductConfiguration (cộng lại số lượng đã bị trừ)
                    foreach (var group in groupedSerials)
                    {
                        await UpdateProductConfigurationQuantity(
                            group.ProductId,
                            group.Specifications,
                            group.Count); // Cộng lại (số dương)
                    }

                    System.Diagnostics.Debug.WriteLine($"Restored {productSerials.Count} ProductSerials for StockExportDetailId: {stockExportDetailId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Không tìm thấy ProductSerial để khôi phục cho StockExportDetailId: {stockExportDetailId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restoring ProductSerials: {ex.Message}");
            }
        }

    }

}
