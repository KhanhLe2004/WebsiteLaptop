using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;
using WebLaptopBE.Services;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/sale-invoices")]
    [ApiController]
    public class ManageSaleInvoiceAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _context;
        private readonly HistoryService _historyService;

        public ManageSaleInvoiceAPIController(Testlaptop36Context context, HistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        // Helper method để lấy EmployeeId từ header
        private string? GetEmployeeId()
        {
            return Request.Headers["X-Employee-Id"].FirstOrDefault();
        }

        // GET: api/admin/sale-invoices
        // Lấy danh sách hóa đơn có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<SaleInvoiceDTO>>> GetSaleInvoices(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var query = _context.SaleInvoices
                    .Include(si => si.Customer)
                    .Include(si => si.Employee)
                    .AsQueryable();

                // Tìm kiếm theo mã hóa đơn, tên khách hàng, tên nhân viên
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(si =>
                        si.SaleInvoiceId.ToLower().Contains(searchTerm) ||
                        (si.Customer != null && si.Customer.CustomerName != null && si.Customer.CustomerName.ToLower().Contains(searchTerm)) ||
                        (si.Employee != null && si.Employee.EmployeeName != null && si.Employee.EmployeeName.ToLower().Contains(searchTerm)));
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(si => si.Status == status);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var saleInvoices = await query
                    .OrderByDescending(si => si.TimeCreate)
                    .ThenByDescending(si => si.SaleInvoiceId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var saleInvoiceDTOs = saleInvoices.Select(si => new SaleInvoiceDTO
                {
                    SaleInvoiceId = si.SaleInvoiceId,
                    PaymentMethod = si.PaymentMethod,
                    TotalAmount = si.TotalAmount,
                    TimeCreate = si.TimeCreate,
                    Status = si.Status,
                    DeliveryFee = si.DeliveryFee,
                    DeliveryAddress = si.DeliveryAddress,
                    EmployeeId = si.EmployeeId,
                    EmployeeName = si.Employee?.EmployeeName,
                    CustomerId = si.CustomerId,
                    CustomerName = si.Customer?.CustomerName
                }).ToList();

                var result = new PagedResult<SaleInvoiceDTO>
                {
                    Items = saleInvoiceDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách hóa đơn", error = ex.Message });
            }
        }

        // GET: api/admin/sale-invoices/{id}
        // Lấy chi tiết một hóa đơn
        [HttpGet("{id}")]
        public async Task<ActionResult<SaleInvoiceDTO>> GetSaleInvoice(string id)
        {
            try
            {
                var saleInvoice = await _context.SaleInvoices
                    .Include(si => si.Customer)
                    .Include(si => si.Employee)
                    .Include(si => si.SaleInvoiceDetails)
                        .ThenInclude(detail => detail.Product)
                    .FirstOrDefaultAsync(si => si.SaleInvoiceId == id);

                if (saleInvoice == null)
                {
                    return NotFound(new { message = "Không tìm thấy hóa đơn" });
                }

                var saleInvoiceDTO = new SaleInvoiceDTO
                {
                    SaleInvoiceId = saleInvoice.SaleInvoiceId,
                    PaymentMethod = saleInvoice.PaymentMethod,
                    TotalAmount = saleInvoice.TotalAmount,
                    TimeCreate = saleInvoice.TimeCreate,
                    Status = saleInvoice.Status,
                    DeliveryFee = saleInvoice.DeliveryFee,
                    DeliveryAddress = saleInvoice.DeliveryAddress,
                    Discount = saleInvoice.Discount,
                    EmployeeId = saleInvoice.EmployeeId,
                    EmployeeName = saleInvoice.Employee?.EmployeeName,
                    CustomerId = saleInvoice.CustomerId,
                    CustomerName = saleInvoice.Customer?.CustomerName,
                    Details = saleInvoice.SaleInvoiceDetails?.Select(detail => new SaleInvoiceDetailDTO
                    {
                        SaleInvoiceDetailId = detail.SaleInvoiceDetailId,
                        SaleInvoiceId = detail.SaleInvoiceId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product?.ProductName,
                        Specifications = detail.Specifications
                    }).ToList()
                };

                return Ok(saleInvoiceDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin hóa đơn", error = ex.Message });
            }
        }

        // PUT: api/admin/sale-invoices/{id}/status
        // Cập nhật trạng thái hóa đơn
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateSaleInvoiceStatus(string id, [FromBody] UpdateStatusDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Status))
                {
                    return BadRequest(new { message = "Trạng thái không được để trống" });
                }

                var saleInvoice = await _context.SaleInvoices
                    .Include(si => si.SaleInvoiceDetails)
                    .FirstOrDefaultAsync(si => si.SaleInvoiceId == id);

                if (saleInvoice == null)
                {
                    return NotFound(new { message = "Không tìm thấy hóa đơn" });
                }

                // Kiểm tra nếu trạng thái hiện tại là "Hoàn thành" hoặc "Đã hủy" thì không cho cập nhật
                if (saleInvoice.Status == "Hoàn thành")
                {
                    return BadRequest(new { message = "Không thể cập nhật trạng thái cho đơn hàng đã hoàn thành" });
                }
                if (saleInvoice.Status == "Đã hủy")
                {
                    return BadRequest(new { message = "Không thể cập nhật trạng thái cho đơn hàng đã hủy" });
                }
                // Lưu trạng thái cũ
                string? oldStatus = saleInvoice.Status;

                // Cập nhật trạng thái và nhân viên
                saleInvoice.Status = dto.Status;
                if (!string.IsNullOrEmpty(dto.EmployeeId))
                {
                    saleInvoice.EmployeeId = dto.EmployeeId;
                }
                await _context.SaveChangesAsync();

                // Tạo phiếu xuất hàng khi trạng thái chuyển thành "Đang xử lý"
                if (oldStatus != "Đang xử lý" && dto.Status == "Đang xử lý")
                {
                    await CreateStockExportForSaleInvoice(saleInvoice);
                }

                // Reload để lấy thông tin đầy đủ
                await _context.Entry(saleInvoice)
                    .Reference(si => si.Customer)
                    .LoadAsync();
                await _context.Entry(saleInvoice)
                    .Reference(si => si.Employee)
                    .LoadAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId() ?? dto.EmployeeId;
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Cập nhật trạng thái hóa đơn: {id} - {oldStatus} → {dto.Status}");
                }

                var result = new SaleInvoiceDTO
                {
                    SaleInvoiceId = saleInvoice.SaleInvoiceId,
                    PaymentMethod = saleInvoice.PaymentMethod,
                    TotalAmount = saleInvoice.TotalAmount,
                    TimeCreate = saleInvoice.TimeCreate,
                    Status = saleInvoice.Status,
                    DeliveryFee = saleInvoice.DeliveryFee,
                    DeliveryAddress = saleInvoice.DeliveryAddress,
                    EmployeeId = saleInvoice.EmployeeId,
                    EmployeeName = saleInvoice.Employee?.EmployeeName,
                    CustomerId = saleInvoice.CustomerId,
                    CustomerName = saleInvoice.Customer?.CustomerName
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái hóa đơn", error = ex.Message });
            }
        }

        // Tạo phiếu xuất hàng khi trạng thái hóa đơn chuyển thành "Đang xử lý"
        private async Task CreateStockExportForSaleInvoice(SaleInvoice saleInvoice)
        {
            try
            {
                // Kiểm tra xem đã có phiếu xuất hàng cho hóa đơn này chưa
                bool existingStockExport = await _context.StockExports
                    .AnyAsync(se => se.SaleInvoiceId == saleInvoice.SaleInvoiceId);

                if (existingStockExport)
                {
                    // Đã có phiếu xuất hàng, không tạo mới
                    return;
                }

                // Tạo mã phiếu xuất hàng
                string stockExportId = GenerateStockExportId();
                
                // Kiểm tra mã phiếu xuất đã tồn tại chưa và tạo lại nếu trùng
                int maxExportAttempts = 50;
                int exportAttempts = 0;
                int baseNumber = GetMaxStockExportNumber();
                while (_context.StockExports.Any(se => se.StockExportId == stockExportId) && exportAttempts < maxExportAttempts)
                {
                    baseNumber++;
                    stockExportId = $"SE{baseNumber:D3}";
                    exportAttempts++;
                }
                
                if (exportAttempts >= maxExportAttempts)
                {
                    System.Diagnostics.Debug.WriteLine("Không thể tạo mã phiếu xuất hàng");
                    return;
                }

                // Tạo phiếu xuất hàng
                var stockExport = new StockExport
                {
                    StockExportId = stockExportId,
                    SaleInvoiceId = saleInvoice.SaleInvoiceId,
                    EmployeeId = null, // Nhân viên null như yêu cầu
                    Status = "Chờ xử lý",
                    Time = DateTime.Now
                };
                _context.StockExports.Add(stockExport);

                // Lấy ID lớn nhất hiện có một lần duy nhất trước khi tạo chi tiết phiếu xuất
                int startExportDetailNumber = GetMaxStockExportDetailNumber();
                int exportDetailIndex = 0;

                // Tạo chi tiết phiếu xuất hàng dựa trên chi tiết hóa đơn
                if (saleInvoice.SaleInvoiceDetails != null && saleInvoice.SaleInvoiceDetails.Any())
                {
                    foreach (var saleInvoiceDetail in saleInvoice.SaleInvoiceDetails)
                    {
                        // Tạo ID tuần tự: STED0001, STED0002...
                        string exportDetailId = $"STED{(startExportDetailNumber + exportDetailIndex + 1):D4}";
                        
                        var stockExportDetail = new StockExportDetail
                        {
                            StockExportDetailId = exportDetailId,
                            StockExportId = stockExport.StockExportId,
                            ProductId = saleInvoiceDetail.ProductId,
                            Quantity = saleInvoiceDetail.Quantity,
                            Specifications = saleInvoiceDetail.Specifications
                        };
                        _context.StockExportDetails.Add(stockExportDetail);
                        
                        exportDetailIndex++;
                    }
                }

                // Lưu phiếu xuất hàng
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating stock export: {ex.Message}");
                // Không throw exception để không ảnh hưởng đến việc cập nhật trạng thái hóa đơn
            }
        }

        // Helper methods cho phiếu xuất hàng
        private int GetMaxStockExportNumber()
        {
            try
            {
                var allIds = _context.StockExports
                    .Where(se => se.StockExportId != null && 
                                 se.StockExportId.StartsWith("SE") && 
                                 se.StockExportId.Length == 5)
                    .Select(se => se.StockExportId)
                    .ToList();

                int maxNumber = 0;
                foreach (var id in allIds)
                {
                    if (id.Length >= 3 && int.TryParse(id.Substring(2), out int num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }

                return maxNumber;
            }
            catch
            {
                return 0;
            }
        }

        private string GenerateStockExportId()
        {
            int maxNumber = GetMaxStockExportNumber();
            return $"SE{(maxNumber + 1):D3}";
        }

        // Lấy số lớn nhất trong các ID chi tiết có format STED0001, STED0002...
        private int GetMaxStockExportDetailNumber()
        {
            try
            {
                var allDetailIds = _context.StockExportDetails
                    .Where(d => d.StockExportDetailId != null && 
                                d.StockExportDetailId.StartsWith("STED") && 
                                d.StockExportDetailId.Length == 8)
                    .Select(d => d.StockExportDetailId)
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
            catch
            {
                return 0;
            }
        }

        // DTO cho cập nhật trạng thái
        public class UpdateStatusDTO
        {
            public string Status { get; set; } = null!;
            public string? EmployeeId { get; set; }
        }
    }
}
