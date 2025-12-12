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
    [Route("api/admin/deliveries")]
    [ApiController]
    public class ManageDeliveryAPIController : ControllerBase
    {
        private readonly Testlaptop37Context _context;
        private readonly HistoryService _historyService;

        public ManageDeliveryAPIController(Testlaptop37Context context, HistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        // Helper method để lấy EmployeeId từ header
        private string? GetEmployeeId()
        {
            return Request.Headers["X-Employee-Id"].FirstOrDefault();
        }

        // GET: api/admin/deliveries
        // Lấy danh sách đơn hàng có trạng thái "Chờ vận chuyển" với phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<SaleInvoiceDTO>>> GetDeliveries(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? dateFrom = null, // Thêm filter ngày bắt đầu
            [FromQuery] DateTime? dateTo = null) // Thêm filter ngày kết thúc
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

                // Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(si => si.Status == status);
                }
                else
                {
                    // Nếu không có filter, chỉ lấy "Chờ vận chuyển" và "Đang vận chuyển"
                    query = query.Where(si => si.Status == "Chờ vận chuyển" || si.Status == "Đang vận chuyển");
                }

                // Tìm kiếm theo mã hóa đơn, tên khách hàng, tên nhân viên
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(si =>
                        si.SaleInvoiceId.ToLower().Contains(searchTerm) ||
                        (si.Customer != null && si.Customer.CustomerName != null && si.Customer.CustomerName.ToLower().Contains(searchTerm)) ||
                        (si.Employee != null && si.Employee.EmployeeName != null && si.Employee.EmployeeName.ToLower().Contains(searchTerm)));
                }

                // Lọc theo ngày tạo
                if (dateFrom.HasValue)
                {
                    query = query.Where(si => si.TimeCreate != null && si.TimeCreate.Value.Date >= dateFrom.Value.Date);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(si => si.TimeCreate != null && si.TimeCreate.Value.Date <= dateTo.Value.Date);
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

                var saleInvoiceDTOs = new List<SaleInvoiceDTO>();
                foreach (var si in saleInvoices)
                {
                    // Load EmployeeShip nếu có
                    Employee? employeeShip = null;
                    if (!string.IsNullOrEmpty(si.EmployeeShip))
                    {
                        employeeShip = await _context.Employees
                            .FirstOrDefaultAsync(e => e.EmployeeId == si.EmployeeShip);
                    }
                    
                    saleInvoiceDTOs.Add(new SaleInvoiceDTO
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
                        CustomerName = si.Customer?.CustomerName,
                        CustomerPhone = si.Phone ?? si.Customer?.PhoneNumber, // Ưu tiên lấy từ SaleInvoice.Phone
                        EmployeeShip = si.EmployeeShip,
                        EmployeeShipName = employeeShip?.EmployeeName,
                        TimeShip = si.TimeShip
                    });
                }

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
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách đơn hàng vận chuyển", error = ex.Message });
            }
        }

        // GET: api/admin/deliveries/{id}
        // Lấy chi tiết một đơn hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<SaleInvoiceDTO>> GetDelivery(string id)
        {
            try
            {
                var saleInvoice = await _context.SaleInvoices
                    .Include(si => si.Customer)
                    .Include(si => si.Employee)
                    .Include(si => si.SaleInvoiceDetails)
                        .ThenInclude(detail => detail.Product)
                    .FirstOrDefaultAsync(si => si.SaleInvoiceId == id && (si.Status == "Chờ vận chuyển" || si.Status == "Đang vận chuyển"));

                if (saleInvoice == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng" });
                }

                // Load EmployeeShip nếu có
                Employee? employeeShipForDetail = null;
                if (!string.IsNullOrEmpty(saleInvoice.EmployeeShip))
                {
                    employeeShipForDetail = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == saleInvoice.EmployeeShip);
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
                    CustomerPhone = saleInvoice.Phone ?? saleInvoice.Customer?.PhoneNumber, // Ưu tiên lấy từ SaleInvoice.Phone
                    EmployeeShip = saleInvoice.EmployeeShip,
                    EmployeeShipName = employeeShipForDetail?.EmployeeName,
                    TimeShip = saleInvoice.TimeShip,
                    Details = saleInvoice.SaleInvoiceDetails?.Select(detail => new SaleInvoiceDetailDTO
                    {
                        SaleInvoiceDetailId = detail.SaleInvoiceDetailId,
                        SaleInvoiceId = detail.SaleInvoiceId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        ProductId = detail.ProductId,
                        ProductName = detail.Product?.ProductName,
                        ProductModel = detail.Product?.ProductModel,
                        Specifications = detail.Specifications
                    }).ToList()
                };

                return Ok(saleInvoiceDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin đơn hàng", error = ex.Message });
            }
        }

        // GET: api/admin/deliveries/inventory/total
        // Lấy tổng số lượng tồn kho
        [HttpGet("inventory/total")]
        public async Task<ActionResult<int>> GetTotalInventory()
        {
            try
            {
                var totalQuantity = await _context.ProductConfigurations
                    .Where(pc => pc.Quantity.HasValue)
                    .SumAsync(pc => pc.Quantity.Value);

                return Ok(new { totalQuantity });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy tổng số lượng tồn kho", error = ex.Message });
            }
        }

        // GET: api/admin/deliveries/inventory/low-stock
        // Lấy danh sách sản phẩm có số lượng <= 3
        [HttpGet("inventory/low-stock")]
        public async Task<ActionResult<List<LowStockProductDTO>>> GetLowStockProducts()
        {
            try
            {
                var lowStockProducts = await _context.ProductConfigurations
                    .Include(pc => pc.Product)
                        .ThenInclude(p => p!.Brand)
                    .Where(pc => pc.Quantity.HasValue && pc.Quantity.Value <= 3)
                    .Select(pc => new LowStockProductDTO
                    {
                        ProductId = pc.ProductId ?? "",
                        ProductName = pc.Product != null ? pc.Product.ProductName : "",
                        ProductModel = pc.Product != null ? pc.Product.ProductModel : "",
                        BrandName = pc.Product != null && pc.Product.Brand != null ? pc.Product.Brand.BrandName : "",
                        ConfigurationId = pc.ConfigurationId ?? "",
                        Cpu = pc.Cpu ?? "",
                        Ram = pc.Ram ?? "",
                        Rom = pc.Rom ?? "",
                        Card = pc.Card ?? "",
                        Quantity = pc.Quantity ?? 0
                    })
                    .OrderBy(p => p.Quantity)
                    .ThenBy(p => p.ProductName)
                    .ToListAsync();

                return Ok(lowStockProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sản phẩm tồn kho thấp", error = ex.Message });
            }
        }

        // PUT: api/admin/deliveries/{id}/status
        // Cập nhật trạng thái đơn hàng sang "Đang vận chuyển"
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateDeliveryStatus(string id, [FromBody] UpdateDeliveryStatusDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Status))
                {
                    return BadRequest(new { message = "Trạng thái không được để trống" });
                }

                var saleInvoice = await _context.SaleInvoices
                    .FirstOrDefaultAsync(si => si.SaleInvoiceId == id && (si.Status == "Chờ vận chuyển" || si.Status == "Đang vận chuyển"));

                if (saleInvoice == null)
                {
                    return NotFound(new { message = "Không tìm thấy đơn hàng hoặc đơn hàng không ở trạng thái 'Chờ vận chuyển' hoặc 'Đang vận chuyển'" });
                }

                // Chỉ cho phép cập nhật sang "Đang vận chuyển" hoặc "Hoàn thành"
                if (dto.Status != "Đang vận chuyển" && dto.Status != "Hoàn thành")
                {
                    return BadRequest(new { message = "Chỉ có thể cập nhật trạng thái sang 'Đang vận chuyển' hoặc 'Hoàn thành'" });
                }

                // Kiểm tra logic chuyển trạng thái
                if (saleInvoice.Status == "Chờ vận chuyển" && dto.Status == "Hoàn thành")
                {
                    return BadRequest(new { message = "Không thể chuyển trực tiếp từ 'Chờ vận chuyển' sang 'Hoàn thành'. Vui lòng cập nhật thành 'Đang vận chuyển' trước." });
                }

                // Lưu trạng thái cũ
                string? oldStatus = saleInvoice.Status;

                // Cập nhật trạng thái
                saleInvoice.Status = dto.Status;
                
                // Lưu employeeShip khi cập nhật thành "Đang vận chuyển"
                if (dto.Status == "Đang vận chuyển")
                {
                    var employeeShipId = GetEmployeeId() ?? dto.EmployeeId;
                    if (!string.IsNullOrEmpty(employeeShipId))
                    {
                        saleInvoice.EmployeeShip = employeeShipId;
                    }
                }
                
                // Lưu timeShip khi cập nhật thành "Hoàn thành"
                if (dto.Status == "Hoàn thành")
                {
                    saleInvoice.TimeShip = DateTime.Now;
                }
                
                await _context.SaveChangesAsync();

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
                    await _historyService.LogHistoryAsync(employeeId, $"Cập nhật trạng thái đơn hàng vận chuyển: {id} - {oldStatus} → {dto.Status}");
                }

                // Load EmployeeShip nếu có
                Employee? employeeShip = null;
                if (!string.IsNullOrEmpty(saleInvoice.EmployeeShip))
                {
                    employeeShip = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == saleInvoice.EmployeeShip);
                }

                // Load EmployeeShip nếu có
                Employee? employeeShipForResult = null;
                if (!string.IsNullOrEmpty(saleInvoice.EmployeeShip))
                {
                    employeeShipForResult = await _context.Employees
                        .FirstOrDefaultAsync(e => e.EmployeeId == saleInvoice.EmployeeShip);
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
                    CustomerName = saleInvoice.Customer?.CustomerName,
                    CustomerPhone = saleInvoice.Phone ?? saleInvoice.Customer?.PhoneNumber, // Ưu tiên lấy từ SaleInvoice.Phone
                    EmployeeShip = saleInvoice.EmployeeShip,
                    EmployeeShipName = employeeShipForResult?.EmployeeName,
                    TimeShip = saleInvoice.TimeShip
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái đơn hàng", error = ex.Message });
            }
        }

    }

    // DTO cho cập nhật trạng thái đơn hàng vận chuyển
    public class UpdateDeliveryStatusDTO
    {
        public string Status { get; set; } = null!;
        public string? EmployeeId { get; set; }
    }

    // DTO cho sản phẩm tồn kho thấp
    public class LowStockProductDTO
    {
        public string ProductId { get; set; } = null!;
        public string ProductName { get; set; } = "";
        public string ProductModel { get; set; } = "";
        public string BrandName { get; set; } = "";
        public string ConfigurationId { get; set; } = "";
        public string Cpu { get; set; } = "";
        public string Ram { get; set; } = "";
        public string Rom { get; set; } = "";
        public string Card { get; set; } = "";
        public int Quantity { get; set; }
    }
}
