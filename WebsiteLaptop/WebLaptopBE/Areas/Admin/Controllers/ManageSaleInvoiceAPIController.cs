using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/sale-invoices")]
    [ApiController]
    public class ManageSaleInvoiceAPIController : ControllerBase
    {
        private readonly Testlaptop30Context _context;

        public ManageSaleInvoiceAPIController(Testlaptop30Context context)
        {
            _context = context;
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
                    .FirstOrDefaultAsync(si => si.SaleInvoiceId == id);

                if (saleInvoice == null)
                {
                    return NotFound(new { message = "Không tìm thấy hóa đơn" });
                }

                // Cập nhật chỉ trạng thái
                saleInvoice.Status = dto.Status;
                await _context.SaveChangesAsync();

                // Reload để lấy thông tin đầy đủ
                await _context.Entry(saleInvoice)
                    .Reference(si => si.Customer)
                    .LoadAsync();
                await _context.Entry(saleInvoice)
                    .Reference(si => si.Employee)
                    .LoadAsync();

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

        // DTO cho cập nhật trạng thái
        public class UpdateStatusDTO
        {
            public string Status { get; set; } = null!;
        }
    }
}
