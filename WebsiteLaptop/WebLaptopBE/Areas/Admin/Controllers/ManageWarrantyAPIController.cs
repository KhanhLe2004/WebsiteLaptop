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
    [Route("api/admin/warranties")]
    [ApiController]
    public class ManageWarrantyAPIController : ControllerBase
    {
        private readonly Testlaptop37Context _context;
        private readonly HistoryService _historyService;

        public ManageWarrantyAPIController(Testlaptop37Context context, HistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        private string? GetEmployeeId()
        {
            return HttpContext.Request.Headers.TryGetValue("X-EmployeeId", out var employeeId) ? employeeId.ToString() : null;
        }

        // GET: api/admin/warranties
        // Lấy danh sách bảo hành có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<WarrantyDTO>>> GetWarranties(
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

                var query = _context.Warranties
                    .Include(w => w.Customer)
                    .Include(w => w.Employee)
                    .Include(w => w.Serial)
                        .ThenInclude(s => s.Product)
                    .AsQueryable();

                // Tìm kiếm theo mã bảo hành, tên khách hàng, serial, nhân viên
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(w =>
                        w.WarrantyId.ToLower().Contains(searchTerm) ||
                        (w.Customer != null && w.Customer.CustomerName != null && w.Customer.CustomerName.ToLower().Contains(searchTerm)) ||
                        (w.Serial != null && w.Serial.SerialId != null && w.Serial.SerialId.ToLower().Contains(searchTerm)) ||
                        (w.Employee != null && w.Employee.EmployeeName != null && w.Employee.EmployeeName.ToLower().Contains(searchTerm)) ||
                        (w.Type != null && w.Type.ToLower().Contains(searchTerm)) ||
                        (w.ContentDetail != null && w.ContentDetail.ToLower().Contains(searchTerm)));
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrWhiteSpace(status))
                {
                    query = query.Where(w => w.Status == status);
                }

                // Lọc theo ngày
                if (dateFrom.HasValue)
                {
                    query = query.Where(w => w.Time != null && w.Time.Value.Date >= dateFrom.Value.Date);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(w => w.Time != null && w.Time.Value.Date <= dateTo.Value.Date);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var warranties = await query
                    .OrderByDescending(w => w.Time)
                    .ThenByDescending(w => w.WarrantyId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var warrantyDTOs = warranties.Select(w => new WarrantyDTO
                {
                    WarrantyId = w.WarrantyId,
                    CustomerId = w.CustomerId,
                    CustomerName = w.Customer?.CustomerName,
                    PhoneNumber = w.Customer?.PhoneNumber,
                    SerialId = w.SerialId,
                    ProductName = w.Serial?.Product?.ProductName,
                    ProductModel = w.Serial?.Product?.ProductModel,
                    Specifications = w.Serial?.Specifications,
                    EmployeeId = w.EmployeeId,
                    EmployeeName = w.Employee?.EmployeeName,
                    Type = w.Type,
                    ContentDetail = w.ContentDetail,
                    Status = w.Status,
                    TotalAmount = w.TotalAmount,
                    Time = w.Time
                }).ToList();

                var result = new PagedResult<WarrantyDTO>
                {
                    Items = warrantyDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách bảo hành", error = ex.Message });
            }
        }

        // GET: api/admin/warranties/customers
        // Lấy danh sách khách hàng
        [HttpGet("customers")]
        public async Task<ActionResult<List<CustomerSelectDTO>>> GetCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Where(c => c.Active == null || c.Active == true)
                    .OrderBy(c => c.CustomerName)
                    .Select(c => new CustomerSelectDTO
                    {
                        CustomerId = c.CustomerId,
                        CustomerName = c.CustomerName,
                        PhoneNumber = c.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khách hàng", error = ex.Message });
            }
        }

        // GET: api/admin/warranties/customer/{customerId}/with-serials
        // Lấy thông tin khách hàng kèm serial từ customerId
        [HttpGet("customer/{customerId}/with-serials")]
        public async Task<ActionResult<CustomerWithSerialsDTO>> GetCustomerWithSerials(string customerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "Mã khách hàng không được để trống" });
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId);

                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Lấy các serial của khách hàng
                var serials = await _context.ProductSerials
                    .Where(ps => ps.StockExportDetail != null &&
                                 ps.StockExportDetail.StockExport != null &&
                                 ps.StockExportDetail.StockExport.SaleInvoice != null &&
                                 ps.StockExportDetail.StockExport.SaleInvoice.CustomerId == customer.CustomerId &&
                                 ps.ExportDate != null &&
                                 ps.Status != "in stock")
                    .Include(ps => ps.Product)
                    .OrderBy(ps => ps.SerialId)
                    .Select(ps => new SerialSelectDTO
                    {
                        SerialId = ps.SerialId,
                        ProductName = ps.Product != null ? ps.Product.ProductName : null,
                        ProductModel = ps.Product != null ? ps.Product.ProductModel : null,
                        DisplayName = ps.SerialId + (ps.Product != null ? " - " + ps.Product.ProductName + (ps.Product.ProductModel != null ? " - " + ps.Product.ProductModel : "") : "")
                    })
                    .ToListAsync();

                var result = new CustomerWithSerialsDTO
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    PhoneNumber = customer.PhoneNumber,
                    Serials = serials
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin khách hàng", error = ex.Message });
            }
        }

        // GET: api/admin/warranties/customer-by-phone/{phoneNumber}
        // Tìm khách hàng theo số điện thoại và lấy serial của khách hàng
        [HttpGet("customer-by-phone/{phoneNumber}")]
        public async Task<ActionResult<CustomerWithSerialsDTO>> GetCustomerByPhone(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return BadRequest(new { message = "Số điện thoại không được để trống" });
                }

                // Tìm khách hàng theo số điện thoại
                var customer = await _context.Customers
                    .Where(c => c.PhoneNumber != null && c.PhoneNumber.Trim() == phoneNumber.Trim())
                    .FirstOrDefaultAsync();

                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng với số điện thoại này" });
                }

                // Lấy các serial của khách hàng thông qua:
                // Customer -> SaleInvoice -> StockExport -> StockExportDetail -> ProductSerial
                var serials = await _context.ProductSerials
                    .Where(ps => ps.StockExportDetail != null &&
                                 ps.StockExportDetail.StockExport != null &&
                                 ps.StockExportDetail.StockExport.SaleInvoice != null &&
                                 ps.StockExportDetail.StockExport.SaleInvoice.CustomerId == customer.CustomerId &&
                                 ps.ExportDate != null &&
                                 ps.Status != "in stock")
                    .Include(ps => ps.Product)
                    .OrderBy(ps => ps.SerialId)
                    .Select(ps => new SerialSelectDTO
                    {
                        SerialId = ps.SerialId,
                        ProductName = ps.Product != null ? ps.Product.ProductName : null,
                        ProductModel = ps.Product != null ? ps.Product.ProductModel : null,
                        DisplayName = ps.SerialId + (ps.Product != null ? " - " + ps.Product.ProductName + (ps.Product.ProductModel != null ? " - " + ps.Product.ProductModel : "") : "")
                    })
                    .ToListAsync();

                var result = new CustomerWithSerialsDTO
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    PhoneNumber = customer.PhoneNumber,
                    Serials = serials
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tìm khách hàng theo số điện thoại", error = ex.Message });
            }
        }

        // GET: api/admin/warranties/employees
        // Lấy danh sách nhân viên
        [HttpGet("employees")]
        public async Task<ActionResult<List<EmployeeSelectDTO>>> GetEmployees()
        {
            try
            {
                var employees = await _context.Employees
                    .Where(e => e.Active == null || e.Active == true)
                    .OrderBy(e => e.EmployeeName)
                    .Select(e => new EmployeeSelectDTO
                    {
                        EmployeeId = e.EmployeeId,
                        EmployeeName = e.EmployeeName
                    })
                    .ToListAsync();

                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách nhân viên", error = ex.Message });
            }
        }

        // GET: api/admin/warranties/serials
        // Lấy danh sách serial (chỉ lấy những serial đã được bán - có ExportDate)
        [HttpGet("serials")]
        public async Task<ActionResult<List<SerialSelectDTO>>> GetSerials()
        {
            try
            {
                var serials = await _context.ProductSerials
                    .Where(s => s.ExportDate != null && s.Status != "in stock")
                    .Include(s => s.Product)
                    .OrderBy(s => s.SerialId)
                    .Select(s => new SerialSelectDTO
                    {
                        SerialId = s.SerialId,
                        ProductName = s.Product != null ? s.Product.ProductName : null,
                        ProductModel = s.Product != null ? s.Product.ProductModel : null,
                        DisplayName = s.SerialId + (s.Product != null ? " - " + s.Product.ProductName + (s.Product.ProductModel != null ? " - " + s.Product.ProductModel : "") : "")
                    })
                    .ToListAsync();

                return Ok(serials);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách serial", error = ex.Message });
            }
        }

        // GET: api/admin/warranties/{id}
        // Lấy chi tiết một bảo hành
        [HttpGet("{id}")]
        public async Task<ActionResult<WarrantyDTO>> GetWarranty(string id)
        {
            try
            {
                var warranty = await _context.Warranties
                    .Include(w => w.Customer)
                    .Include(w => w.Employee)
                    .Include(w => w.Serial)
                        .ThenInclude(s => s.Product)
                    .FirstOrDefaultAsync(w => w.WarrantyId == id);

                if (warranty == null)
                {
                    return NotFound(new { message = "Không tìm thấy bảo hành" });
                }

                var warrantyDTO = new WarrantyDTO
                {
                    WarrantyId = warranty.WarrantyId,
                    CustomerId = warranty.CustomerId,
                    CustomerName = warranty.Customer?.CustomerName,
                    PhoneNumber = warranty.Customer?.PhoneNumber,
                    SerialId = warranty.SerialId,
                    ProductName = warranty.Serial?.Product?.ProductName,
                    ProductModel = warranty.Serial?.Product?.ProductModel,
                    Specifications = warranty.Serial?.Specifications,
                    EmployeeId = warranty.EmployeeId,
                    EmployeeName = warranty.Employee?.EmployeeName,
                    Type = warranty.Type,
                    ContentDetail = warranty.ContentDetail,
                    Status = warranty.Status,
                    TotalAmount = warranty.TotalAmount,
                    Time = warranty.Time
                };

                return Ok(warrantyDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin bảo hành", error = ex.Message });
            }
        }

        // POST: api/admin/warranties
        // Tạo mới bảo hành
        [HttpPost]
        public async Task<ActionResult<WarrantyDTO>> CreateWarranty([FromBody] WarrantyCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                // Tạo mã bảo hành nếu chưa có
                string warrantyId = dto.WarrantyId ?? GenerateWarrantyId();

                // Kiểm tra mã đã tồn tại chưa
                var existing = await _context.Warranties
                    .FirstOrDefaultAsync(w => w.WarrantyId == warrantyId);
                if (existing != null)
                {
                    return BadRequest(new { message = "Mã bảo hành đã tồn tại" });
                }

                // Xử lý logic khác nhau cho Bảo hành và Sửa chữa
                string customerId = dto.CustomerId;
                decimal totalAmount = dto.TotalAmount ?? 0;

                if (dto.Type == "Bảo hành")
                {
                    // Bảo hành: Yêu cầu CustomerId và SerialId, TotalAmount = 0
                    if (string.IsNullOrWhiteSpace(customerId))
                    {
                        return BadRequest(new { message = "Bảo hành yêu cầu thông tin khách hàng" });
                    }
                    totalAmount = 0; // Cố định = 0 cho bảo hành
                }
                else if (dto.Type == "Sửa chữa")
                {
                    // Sửa chữa: Có thể tạo khách hàng mới nếu cần, SerialId = null
                    if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(dto.CustomerName))
                    {
                        // Tạo khách hàng mới cho sửa chữa
                        customerId = await CreateNewCustomer(dto.CustomerName, dto.PhoneNumber);
                    }
                }

                var warranty = new Warranty
                {
                    WarrantyId = warrantyId,
                    CustomerId = customerId,
                    SerialId = dto.Type == "Sửa chữa" ? null : dto.SerialId, // SerialId = null cho sửa chữa
                    EmployeeId = dto.EmployeeId,
                    Type = dto.Type,
                    ContentDetail = dto.ContentDetail,
                    Status = dto.Status ?? "Đang xử lý",
                    TotalAmount = totalAmount,
                    Time = DateTime.Now // Thêm thời gian khi tạo mới
                };

                _context.Warranties.Add(warranty);
                await _context.SaveChangesAsync();

                // Log history
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Thêm bảo hành: {warranty.WarrantyId}");
                }

                // Load lại để lấy thông tin đầy đủ
                await _context.Entry(warranty)
                    .Reference(w => w.Customer)
                    .LoadAsync();
                await _context.Entry(warranty)
                    .Reference(w => w.Employee)
                    .LoadAsync();
                await _context.Entry(warranty)
                    .Reference(w => w.Serial)
                    .LoadAsync();
                if (warranty.Serial != null)
                {
                    await _context.Entry(warranty.Serial)
                        .Reference(s => s.Product)
                        .LoadAsync();
                }

                var result = new WarrantyDTO
                {
                    WarrantyId = warranty.WarrantyId,
                    CustomerId = warranty.CustomerId,
                    CustomerName = warranty.Customer?.CustomerName,
                    PhoneNumber = warranty.Customer?.PhoneNumber,
                    SerialId = warranty.SerialId,
                    ProductName = warranty.Serial?.Product?.ProductName,
                    ProductModel = warranty.Serial?.Product?.ProductModel,
                    EmployeeId = warranty.EmployeeId,
                    EmployeeName = warranty.Employee?.EmployeeName,
                    Type = warranty.Type,
                    ContentDetail = warranty.ContentDetail,
                    Status = warranty.Status,
                    TotalAmount = warranty.TotalAmount,
                    Time = warranty.Time
                };

                return CreatedAtAction(nameof(GetWarranty), new { id = warranty.WarrantyId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo bảo hành", error = ex.Message });
            }
        }

        // PUT: api/admin/warranties/{id}
        // Cập nhật bảo hành
        [HttpPut("{id}")]
        public async Task<ActionResult<WarrantyDTO>> UpdateWarranty(string id, [FromBody] WarrantyUpdateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                var warranty = await _context.Warranties
                    .FirstOrDefaultAsync(w => w.WarrantyId == id);

                if (warranty == null)
                {
                    return NotFound(new { message = "Không tìm thấy bảo hành" });
                }

                // Không cho phép cập nhật khi trạng thái là "Hoàn thành" hoặc "Đã hủy"
                if (warranty.Status == "Hoàn thành" || warranty.Status == "Đã hủy")
                {
                    return BadRequest(new { message = "Không thể chỉnh sửa phiếu bảo hành đã hoàn thành hoặc đã hủy" });
                }

                // Xử lý logic cập nhật khác nhau cho Bảo hành và Sửa chữa
                string customerId = dto.CustomerId ?? warranty.CustomerId;
                decimal? totalAmount = dto.TotalAmount ?? warranty.TotalAmount;
                string type = dto.Type ?? warranty.Type;

                if (type == "Bảo hành")
                {
                    // Bảo hành: TotalAmount = 0
                    totalAmount = 0;
                }
                else if (type == "Sửa chữa")
                {
                    // Sửa chữa: Có thể tạo khách hàng mới nếu cần
                    if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(dto.CustomerName))
                    {
                        customerId = await CreateNewCustomer(dto.CustomerName, dto.PhoneNumber);
                    }
                }

                // Cập nhật thông tin
                warranty.CustomerId = customerId;
                warranty.SerialId = type == "Sửa chữa" ? null : (dto.SerialId ?? warranty.SerialId);
                warranty.EmployeeId = dto.EmployeeId ?? warranty.EmployeeId;
                warranty.Type = type;
                warranty.ContentDetail = dto.ContentDetail ?? warranty.ContentDetail;
                
                // Cập nhật thời gian khi trạng thái thay đổi hoặc khi sửa
                if (dto.Status != null && dto.Status != warranty.Status)
                {
                    warranty.Status = dto.Status;
                    warranty.Time = DateTime.Now; // Cập nhật thời gian khi thay đổi trạng thái
                }
                else
                {
                    warranty.Status = dto.Status ?? warranty.Status;
                    // Luôn cập nhật thời gian khi sửa phiếu bảo hành
                    warranty.Time = DateTime.Now;
                }
                
                warranty.TotalAmount = totalAmount;

                await _context.SaveChangesAsync();

                // Log history
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Cập nhật bảo hành: {id}");
                }

                // Load lại để lấy thông tin đầy đủ
                await _context.Entry(warranty)
                    .Reference(w => w.Customer)
                    .LoadAsync();
                await _context.Entry(warranty)
                    .Reference(w => w.Employee)
                    .LoadAsync();
                await _context.Entry(warranty)
                    .Reference(w => w.Serial)
                    .LoadAsync();
                if (warranty.Serial != null)
                {
                    await _context.Entry(warranty.Serial)
                        .Reference(s => s.Product)
                        .LoadAsync();
                }

                var result = new WarrantyDTO
                {
                    WarrantyId = warranty.WarrantyId,
                    CustomerId = warranty.CustomerId,
                    CustomerName = warranty.Customer?.CustomerName,
                    PhoneNumber = warranty.Customer?.PhoneNumber,
                    SerialId = warranty.SerialId,
                    ProductName = warranty.Serial?.Product?.ProductName,
                    ProductModel = warranty.Serial?.Product?.ProductModel,
                    EmployeeId = warranty.EmployeeId,
                    EmployeeName = warranty.Employee?.EmployeeName,
                    Type = warranty.Type,
                    ContentDetail = warranty.ContentDetail,
                    Status = warranty.Status,
                    TotalAmount = warranty.TotalAmount,
                    Time = warranty.Time
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật bảo hành", error = ex.Message });
            }
        }


        // Helper methods
        private string GenerateWarrantyId()
        {
            // Tìm số lớn nhất trong các ID có format WA001, WA002...
            var allIds = _context.Warranties
                .Select(w => w.WarrantyId)
                .Where(id => id != null && id.StartsWith("WA") && id.Length == 5)
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
            return $"WA{(maxNumber + 1):D3}";
        }

        private async Task<string> CreateNewCustomer(string customerName, string phoneNumber)
        {
            // Tạo CustomerId mới
            var allCustomerIds = await _context.Customers
                .Select(c => c.CustomerId)
                .Where(id => id != null && id.StartsWith("C") && id.Length == 4)
                .ToListAsync();

            int maxNumber = 0;
            foreach (var id in allCustomerIds)
            {
                if (id.Length >= 2 && int.TryParse(id.Substring(1), out int num))
                {
                    maxNumber = Math.Max(maxNumber, num);
                }
            }

            string newCustomerId = $"C{(maxNumber + 1):D3}";

            // Tạo khách hàng mới
            var newCustomer = new Customer
            {
                CustomerId = newCustomerId,
                CustomerName = customerName,
                PhoneNumber = phoneNumber,
                Active = true
            };

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            return newCustomerId;
        }
    }
}
