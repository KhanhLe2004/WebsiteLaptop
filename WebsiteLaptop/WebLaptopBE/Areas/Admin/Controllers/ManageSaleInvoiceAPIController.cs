using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Threading;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;
using WebLaptopBE.Services;
using OfficeOpenXml;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/sale-invoices")]
    [ApiController]
    public class ManageSaleInvoiceAPIController : ControllerBase
    {
        private readonly Testlaptop38Context _context;
        private readonly HistoryService _historyService;

        private readonly HttpClient _httpClient;
        private const string ADDRESS_API_BASE_URL = "https://production.cas.so/address-kit/2025-07-01";

        public ManageSaleInvoiceAPIController(Testlaptop38Context context, HistoryService historyService, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _historyService = historyService;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
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
            [FromQuery] string? status = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
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

                // Lọc theo ngày
                if (dateFrom.HasValue)
                {
                    query = query.Where(si => si.TimeCreate.HasValue && si.TimeCreate.Value.Date >= dateFrom.Value.Date);
                }
                if (dateTo.HasValue)
                {
                    query = query.Where(si => si.TimeCreate.HasValue && si.TimeCreate.Value.Date <= dateTo.Value.Date);
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
                    CustomerName = si.Customer?.CustomerName,
                    CustomerPhone = si.Phone ?? si.Customer?.PhoneNumber // Ưu tiên lấy từ SaleInvoice.Phone
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
                // Log chi tiết lỗi để debug
                System.Diagnostics.Debug.WriteLine($"Error in GetSaleInvoices: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách hóa đơn", error = ex.Message, details = ex.InnerException?.Message });
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

                // Load EmployeeShip nếu có
                Employee? employeeShip = null;
                if (!string.IsNullOrEmpty(saleInvoice.EmployeeShip))
                {
                    employeeShip = await _context.Employees
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
                    EmployeeShipName = employeeShip?.EmployeeName,
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
                        Specifications = detail.Specifications,
                        WarrantyPeriod = detail.Product?.WarrantyPeriod
                    }).ToList()
                };

                return Ok(saleInvoiceDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin hóa đơn", error = ex.Message });
            }
        }

        // GET: api/admin/sale-invoices/customer-by-phone/{phoneNumber}
        // Tìm khách hàng theo số điện thoại
        [HttpGet("customer-by-phone/{phoneNumber}")]
        public async Task<ActionResult<CustomerSelectDTO>> GetCustomerByPhone(string phoneNumber)
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

                var result = new CustomerSelectDTO
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    PhoneNumber = customer.PhoneNumber
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tìm khách hàng theo số điện thoại", error = ex.Message });
            }
        }

        // POST: api/admin/sale-invoices
        // Tạo mới hóa đơn
        [HttpPost]
        public async Task<ActionResult<SaleInvoiceDTO>> CreateSaleInvoice([FromBody] SaleInvoiceCreateDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                // Tạo mã hóa đơn nếu chưa có
                string saleInvoiceId = dto.SaleInvoiceId ?? GenerateSaleInvoiceId();

                // Kiểm tra mã đã tồn tại chưa
                var existing = await _context.SaleInvoices
                    .FirstOrDefaultAsync(si => si.SaleInvoiceId == saleInvoiceId);
                if (existing != null)
                {
                    return BadRequest(new { message = "Mã hóa đơn đã tồn tại" });
                }

                // Xử lý khách hàng: nếu không có customerId nhưng có customerName và phoneNumber, tạo khách hàng mới
                string customerId = dto.CustomerId;
                if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(dto.CustomerName) && !string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    // Tạo khách hàng mới
                    customerId = await CreateNewCustomer(dto.CustomerName, dto.PhoneNumber);
                }

                if (string.IsNullOrWhiteSpace(customerId))
                {
                    return BadRequest(new { message = "Không thể xác định khách hàng. Vui lòng nhập số điện thoại hoặc tên khách hàng" });
                }

                // Xử lý địa chỉ: lấy tên tỉnh/thành và phường/xã từ API
                string? provinceName = null;
                string? communeName = null;
                
                if (!string.IsNullOrWhiteSpace(dto.ProvinceCode))
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var provinceResponse = await _httpClient.GetAsync($"{ADDRESS_API_BASE_URL}/provinces", cts.Token);
                        if (provinceResponse.IsSuccessStatusCode)
                        {
                            var provinceJson = await provinceResponse.Content.ReadAsStringAsync(cts.Token);
                            var provinceDoc = JsonDocument.Parse(provinceJson);
                            if (provinceDoc.RootElement.TryGetProperty("provinces", out var provincesElement))
                            {
                                foreach (var province in provincesElement.EnumerateArray())
                                {
                                    if (province.TryGetProperty("code", out var code) && code.GetString() == dto.ProvinceCode)
                                    {
                                        if (province.TryGetProperty("name", out var name))
                                        {
                                            provinceName = name.GetString();
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception apiEx)
                    {
                        Console.WriteLine($"Warning: Could not fetch province name from API: {apiEx.Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.ProvinceCode) && !string.IsNullOrWhiteSpace(dto.CommuneCode))
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var communeResponse = await _httpClient.GetAsync($"{ADDRESS_API_BASE_URL}/provinces/{dto.ProvinceCode}/communes", cts.Token);
                        if (communeResponse.IsSuccessStatusCode)
                        {
                            var communeJson = await communeResponse.Content.ReadAsStringAsync(cts.Token);
                            var communeDoc = JsonDocument.Parse(communeJson);
                            if (communeDoc.RootElement.TryGetProperty("communes", out var communesElement))
                            {
                                foreach (var commune in communesElement.EnumerateArray())
                                {
                                    if (commune.TryGetProperty("code", out var code) && code.GetString() == dto.CommuneCode)
                                    {
                                        if (commune.TryGetProperty("name", out var name))
                                        {
                                            communeName = name.GetString();
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception apiEx)
                    {
                        Console.WriteLine($"Warning: Could not fetch commune name from API: {apiEx.Message}");
                    }
                }

                // Xử lý địa chỉ: ghép từ các thành phần
                string? fullAddress = BuildFullAddress(dto.AddressDetail, communeName, provinceName);
                if (string.IsNullOrWhiteSpace(fullAddress) && !string.IsNullOrWhiteSpace(dto.DeliveryAddress))
                {
                    fullAddress = dto.DeliveryAddress; // Giữ nguyên DeliveryAddress cũ nếu không có thông tin mới
                }

                // Lưu thông tin địa chỉ chi tiết vào DeliveryAddress dưới dạng JSON (giới hạn độ dài)
                string? finalAddress = fullAddress;
                if (!string.IsNullOrWhiteSpace(dto.ProvinceCode) || !string.IsNullOrWhiteSpace(dto.CommuneCode) || !string.IsNullOrWhiteSpace(dto.AddressDetail))
                {
                    try
                    {
                        finalAddress = SerializeAddress(dto.ProvinceCode, provinceName, dto.CommuneCode, communeName, dto.AddressDetail, fullAddress);
                        if (string.IsNullOrWhiteSpace(finalAddress))
                        {
                            finalAddress = fullAddress;
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"Error serializing address JSON: {jsonEx.Message}");
                        finalAddress = fullAddress;
                    }
                }

                // Tính tổng tiền từ chi tiết nếu có
                decimal subtotal = 0;
                if (dto.Details != null && dto.Details.Any())
                {
                    subtotal = dto.Details.Sum(d => (d.Quantity ?? 0) * (d.UnitPrice ?? 0));
                }
                
                // Áp dụng khuyến mại nếu có
                decimal discount = dto.Discount ?? 0;
                decimal shippingDiscount = dto.ShippingDiscount ?? 0;
                decimal discountedSubtotal = subtotal - discount;
                decimal finalDeliveryFee = Math.Max(0, (dto.DeliveryFee ?? 0) - shippingDiscount);
                decimal totalAmount = discountedSubtotal + finalDeliveryFee;

                // Xử lý TimeCreate: Parse time từ client (có thể là string hoặc DateTime)
                DateTime? invoiceTime = dto.TimeCreate;
                if (invoiceTime.HasValue)
                {
                    // Nếu là UTC, convert về local time
                    if (invoiceTime.Value.Kind == DateTimeKind.Utc)
                    {
                        invoiceTime = invoiceTime.Value.ToLocalTime();
                    }
                    // Nếu là Unspecified (từ string parse), giữ nguyên (đã là local time)
                    else if (invoiceTime.Value.Kind == DateTimeKind.Unspecified)
                    {
                        // Giữ nguyên, coi như local time
                        invoiceTime = DateTime.SpecifyKind(invoiceTime.Value, DateTimeKind.Local);
                    }
                }
                else
                {
                    invoiceTime = DateTime.Now;
                }

                // Tạo hóa đơn mới
                var saleInvoice = new SaleInvoice
                {
                    SaleInvoiceId = saleInvoiceId,
                    PaymentMethod = dto.PaymentMethod,
                    TotalAmount = totalAmount,
                    TimeCreate = invoiceTime.Value,
                    Status = dto.Status ?? "Chờ xử lý",
                    DeliveryFee = finalDeliveryFee,
                    Discount = discount,
                    DeliveryAddress = finalAddress,
                    EmployeeId = dto.EmployeeId,
                    CustomerId = customerId,
                    Phone = dto.PhoneNumber // Lưu số điện thoại vào SaleInvoice.Phone
                };

                _context.SaleInvoices.Add(saleInvoice);

                // Tạo chi tiết hóa đơn nếu có
                if (dto.Details != null && dto.Details.Any())
                {
                    int detailIndex = 1;
                    foreach (var detailDto in dto.Details)
                    {
                        string detailId = $"SID{detailIndex:D4}";
                        
                        // Kiểm tra và tạo ID duy nhất
                        while (await _context.SaleInvoiceDetails.AnyAsync(d => d.SaleInvoiceDetailId == detailId))
                        {
                            detailIndex++;
                            detailId = $"SID{detailIndex:D4}";
                        }

                        var detail = new SaleInvoiceDetail
                        {
                            SaleInvoiceDetailId = detailId,
                            SaleInvoiceId = saleInvoice.SaleInvoiceId,
                            ProductId = detailDto.ProductId,
                            Quantity = detailDto.Quantity ?? 1,
                            UnitPrice = detailDto.UnitPrice ?? 0,
                            Specifications = detailDto.Specifications
                        };

                        _context.SaleInvoiceDetails.Add(detail);
                        detailIndex++;
                    }
                }

                await _context.SaveChangesAsync();

                // Log history
                var employeeId = GetEmployeeId() ?? dto.EmployeeId;
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Thêm hóa đơn: {saleInvoice.SaleInvoiceId}");
                }

                // Load lại để lấy thông tin đầy đủ
                await _context.Entry(saleInvoice)
                    .Reference(si => si.Customer)
                    .LoadAsync();
                await _context.Entry(saleInvoice)
                    .Reference(si => si.Employee)
                    .LoadAsync();
                await _context.Entry(saleInvoice)
                    .Collection(si => si.SaleInvoiceDetails)
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
                    CustomerName = saleInvoice.Customer?.CustomerName,
                    CustomerPhone = saleInvoice.Phone ?? saleInvoice.Customer?.PhoneNumber
                };

                return CreatedAtAction(nameof(GetSaleInvoice), new { id = saleInvoice.SaleInvoiceId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo hóa đơn", error = ex.Message });
            }
        }

        // Helper method để tạo khách hàng mới
        private async Task<string> CreateNewCustomer(string customerName, string phoneNumber)
        {
            // Tạo mã khách hàng mới
            var allCustomerIds = await _context.Customers
                .Where(c => c.CustomerId.StartsWith("C") && c.CustomerId.Length == 4)
                .Select(c => c.CustomerId)
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

        // GET: api/admin/sale-invoices/next-id
        // Lấy mã hóa đơn tiếp theo
        [HttpGet("next-id")]
        public ActionResult<object> GetNextSaleInvoiceId()
        {
            try
            {
                string nextId = GenerateSaleInvoiceId();
                return Ok(new { saleInvoiceId = nextId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo mã hóa đơn", error = ex.Message });
            }
        }

        // Helper method để tạo mã hóa đơn
        private string GenerateSaleInvoiceId()
        {
            try
            {
                var allIds = _context.SaleInvoices
                    .Where(si => si.SaleInvoiceId != null && 
                                 si.SaleInvoiceId.StartsWith("SI") && 
                                 si.SaleInvoiceId.Length == 5)
                    .Select(si => si.SaleInvoiceId)
                    .ToList();

                int maxNumber = 0;
                foreach (var id in allIds)
                {
                    if (id.Length >= 3 && int.TryParse(id.Substring(2), out int num))
                    {
                        maxNumber = Math.Max(maxNumber, num);
                    }
                }

                return $"SI{(maxNumber + 1):D3}";
            }
            catch
            {
                return "SI001";
            }
        }

        // GET: api/admin/sale-invoices/provinces
        // Lấy danh sách tỉnh/thành
        [HttpGet("provinces")]
        public async Task<ActionResult<List<ProvinceDTO>>> GetProvinces()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ADDRESS_API_BASE_URL}/provinces");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonString);
                
                var provinces = new List<ProvinceDTO>();
                if (jsonDoc.RootElement.TryGetProperty("provinces", out var provincesElement))
                {
                    foreach (var province in provincesElement.EnumerateArray())
                    {
                        provinces.Add(new ProvinceDTO
                        {
                            Code = province.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "",
                            Name = province.TryGetProperty("name", out var name) ? name.GetString() : null,
                            EnglishName = province.TryGetProperty("englishName", out var englishName) ? englishName.GetString() : null,
                            AdministrativeLevel = province.TryGetProperty("administrativeLevel", out var level) ? level.GetString() : null
                        });
                    }
                }

                return Ok(provinces.OrderBy(p => p.Name).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách tỉnh/thành", error = ex.Message });
            }
        }

        // GET: api/admin/sale-invoices/product-configurations/{productId}
        // Lấy danh sách cấu hình sản phẩm
        [HttpGet("product-configurations/{productId}")]
        public async Task<ActionResult<List<object>>> GetProductConfigurations(string productId)
        {
            try
            {
                var configurations = await _context.ProductConfigurations
                    .Where(pc => pc.ProductId == productId)
                    .OrderBy(pc => pc.ConfigurationId)
                    .Select(pc => new
                    {
                        configurationId = pc.ConfigurationId,
                        productId = pc.ProductId,
                        cpu = pc.Cpu,
                        ram = pc.Ram,
                        rom = pc.Rom,
                        card = pc.Card,
                        quantity = pc.Quantity,
                        price = pc.Price
                    })
                    .ToListAsync();

                return Ok(configurations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách cấu hình sản phẩm", error = ex.Message });
            }
        }

        // GET: api/admin/sale-invoices/promotions
        // Lấy danh sách khuyến mại dựa trên danh sách sản phẩm
        [HttpGet("promotions")]
        public async Task<ActionResult<object>> GetPromotions([FromQuery] string[] productIds)
        {
            try
            {
                if (productIds == null || productIds.Length == 0)
                {
                    return Ok(new
                    {
                        discountPromotions = new List<object>(),
                        freeshipPromotions = new List<object>()
                    });
                }

                // Lấy tất cả khuyến mại cho các sản phẩm đã chọn
                var promotions = await _context.Promotions
                    .Include(p => p.Product)
                    .Where(p => productIds.Contains(p.ProductId) && !string.IsNullOrEmpty(p.Type))
                    .ToListAsync();

                // Phân loại khuyến mại
                var discountPromotions = promotions
                    .Where(p => p.Type != null && (p.Type.ToLower().Contains("giảm") || p.Type.Contains("%")))
                    .Select(p => {
                        var discountPercent = ExtractDiscountPercent(p.ContentDetail);
                        var displayText = discountPercent > 0 
                            ? $"Giảm giá {discountPercent}% - {p.Product?.ProductName}"
                            : $"{p.Type} - {p.Product?.ProductName}";
                        
                        return new {
                            promotionId = p.PromotionId,
                            productId = p.ProductId,
                            productName = p.Product?.ProductName,
                            productModel = p.Product?.ProductModel,
                            type = p.Type,
                            contentDetail = p.ContentDetail,
                            displayText = displayText
                        };
                    })
                    .ToList();

                var freeshipPromotions = promotions
                    .Where(p => p.Type != null && p.Type.ToLower().Contains("freeship"))
                    .Select(p => new {
                        promotionId = p.PromotionId,
                        productId = p.ProductId,
                        productName = p.Product?.ProductName,
                        productModel = p.Product?.ProductModel,
                        type = p.Type,
                        contentDetail = p.ContentDetail,
                        displayText = "Freeship"
                    })
                    .ToList();

                return Ok(new
                {
                    discountPromotions = discountPromotions,
                    freeshipPromotions = freeshipPromotions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khuyến mại", error = ex.Message });
            }
        }

        // POST: api/admin/sale-invoices/apply-promotion
        // Áp dụng khuyến mại cho hóa đơn
        [HttpPost("apply-promotion")]
        public async Task<ActionResult<object>> ApplyPromotion([FromBody] ApplyPromotionRequest request)
        {
            try
            {
                if (request == null || (request.SelectedDiscountPromotions == null || !request.SelectedDiscountPromotions.Any()) &&
                    (request.SelectedFreeshipPromotions == null || !request.SelectedFreeshipPromotions.Any()))
                {
                    return BadRequest(new { message = "Vui lòng chọn ít nhất một khuyến mại" });
                }

                if (request.InvoiceDetails == null || !request.InvoiceDetails.Any())
                {
                    return BadRequest(new { message = "Vui lòng thêm sản phẩm vào hóa đơn" });
                }

                // Lấy tất cả khuyến mại được chọn
                var allSelectedPromotionIds = new List<string>();
                if (request.SelectedDiscountPromotions != null)
                    allSelectedPromotionIds.AddRange(request.SelectedDiscountPromotions);
                if (request.SelectedFreeshipPromotions != null)
                    allSelectedPromotionIds.AddRange(request.SelectedFreeshipPromotions);

                var promotions = await _context.Promotions
                    .Include(p => p.Product)
                    .Where(p => allSelectedPromotionIds.Contains(p.PromotionId))
                    .ToListAsync();

                if (!promotions.Any())
                {
                    return BadRequest(new { message = "Không tìm thấy khuyến mại được chọn" });
                }

                // Tính toán khuyến mại
                decimal originalSubtotal = 0;
                decimal discountedSubtotal = 0;
                decimal deliveryFee = request.DeliveryFee ?? 0;
                decimal finalDeliveryFee = deliveryFee;
                var promotionDetails = new List<object>();
                var hasFreeship = false;

                foreach (var item in request.InvoiceDetails)
                {
                    if (item == null) continue;
                    var price = item.UnitPrice ?? 0;
                    var quantity = item.Quantity ?? 0;
                    if (quantity > 0 && price > 0)
                    {
                        var itemTotal = price * quantity;
                        originalSubtotal += itemTotal;

                        // Tìm khuyến mại giảm giá cho sản phẩm này
                        var discountPromotion = promotions.FirstOrDefault(p =>
                            p.ProductId == item.ProductId &&
                            request.SelectedDiscountPromotions != null &&
                            request.SelectedDiscountPromotions.Contains(p.PromotionId) &&
                            p.Type != null && (p.Type.ToLower().Contains("giảm") || p.Type.Contains("%")));

                        if (discountPromotion != null)
                        {
                            // Lấy phần trăm từ ContentDetail
                            var discountPercent = ExtractDiscountPercent(discountPromotion.ContentDetail);
                            if (discountPercent > 0)
                            {
                                var discountedPrice = itemTotal * (1 - discountPercent / 100m);
                                var discountAmount = itemTotal - discountedPrice;
                                discountedSubtotal += discountedPrice;

                                promotionDetails.Add(new
                                {
                                    type = "discount",
                                    productName = discountPromotion.Product?.ProductName,
                                    discountPercent = discountPercent,
                                    discountAmount = discountAmount,
                                    displayText = $"Giảm giá {discountPercent}% - {discountPromotion.Product?.ProductName}"
                                });
                            }
                            else
                            {
                                discountedSubtotal += itemTotal;
                            }
                        }
                        else
                        {
                            discountedSubtotal += itemTotal;
                        }

                        // Kiểm tra freeship cho sản phẩm này
                        var freeshipPromotion = promotions.FirstOrDefault(p =>
                            p.ProductId == item.ProductId &&
                            request.SelectedFreeshipPromotions != null &&
                            request.SelectedFreeshipPromotions.Contains(p.PromotionId) &&
                            p.Type != null && p.Type.ToLower().Contains("freeship"));

                        if (freeshipPromotion != null && !hasFreeship)
                        {
                            hasFreeship = true;
                            finalDeliveryFee = 0;
                        }
                    }
                }

                var discount = originalSubtotal - discountedSubtotal;
                var shippingDiscount = deliveryFee - finalDeliveryFee;
                var totalDiscount = discount + shippingDiscount;
                var finalTotal = discountedSubtotal + finalDeliveryFee;

                // Thêm thông báo freeship nếu có
                if (hasFreeship)
                {
                    promotionDetails.Add(new
                    {
                        type = "freeship",
                        discountAmount = shippingDiscount,
                        displayText = "Freeship cho toàn bộ đơn hàng"
                    });
                }

                return Ok(new
                {
                    message = "Áp dụng khuyến mại thành công",
                    promotionDetails = promotionDetails,
                    originalSubtotal = originalSubtotal,
                    discountedSubtotal = discountedSubtotal,
                    discount = discount,
                    originalDeliveryFee = deliveryFee,
                    finalDeliveryFee = finalDeliveryFee,
                    shippingDiscount = shippingDiscount,
                    totalDiscount = totalDiscount,
                    finalTotal = finalTotal,
                    selectedDiscountPromotions = request.SelectedDiscountPromotions,
                    selectedFreeshipPromotions = request.SelectedFreeshipPromotions,
                    hasFreeship = hasFreeship
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi áp dụng khuyến mại", error = ex.Message });
            }
        }

        // Helper method để lấy phần trăm giảm giá từ ContentDetail
        private decimal ExtractDiscountPercent(string? contentDetail)
        {
            if (string.IsNullOrWhiteSpace(contentDetail))
                return 0;

            try
            {
                // Tìm số phần trăm trong chuỗi (ví dụ: "Giảm 15% giá bán" -> 15)
                var match = System.Text.RegularExpressions.Regex.Match(contentDetail, @"(\d+)%");
                if (match.Success && decimal.TryParse(match.Groups[1].Value, out decimal percent))
                {
                    return percent;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return 0;
        }

        // GET: api/admin/sale-invoices/provinces/{provinceCode}/communes
        // Lấy danh sách phường/xã theo tỉnh/thành
        [HttpGet("provinces/{provinceCode}/communes")]
        public async Task<ActionResult<List<CommuneDTO>>> GetCommunes(string provinceCode)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ADDRESS_API_BASE_URL}/provinces/{provinceCode}/communes");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(jsonString);
                
                var communes = new List<CommuneDTO>();
                if (jsonDoc.RootElement.TryGetProperty("communes", out var communesElement))
                {
                    foreach (var commune in communesElement.EnumerateArray())
                    {
                        communes.Add(new CommuneDTO
                        {
                            Code = commune.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "",
                            Name = commune.TryGetProperty("name", out var name) ? name.GetString() : null,
                            EnglishName = commune.TryGetProperty("englishName", out var englishName) ? englishName.GetString() : null,
                            AdministrativeLevel = commune.TryGetProperty("administrativeLevel", out var level) ? level.GetString() : null,
                            ProvinceCode = commune.TryGetProperty("provinceCode", out var provCode) ? provCode.GetString() : null,
                            ProvinceName = commune.TryGetProperty("provinceName", out var provName) ? provName.GetString() : null
                        });
                    }
                }

                return Ok(communes.OrderBy(c => c.Name).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách phường/xã", error = ex.Message });
            }
        }

        // Helper method để ghép địa chỉ đầy đủ từ các thành phần
        private string? BuildFullAddress(string? addressDetail, string? communeName, string? provinceName)
        {
            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(addressDetail))
            {
                parts.Add(addressDetail.Trim());
            }
            
            if (!string.IsNullOrWhiteSpace(communeName))
            {
                parts.Add(communeName);
            }
            
            if (!string.IsNullOrWhiteSpace(provinceName))
            {
                parts.Add(provinceName);
            }
            
            return parts.Count > 0 ? string.Join(", ", parts) : null;
        }

        // Helper method để serialize địa chỉ thành JSON
        private string? SerializeAddress(string? provinceCode, string? provinceName, string? communeCode, string? communeName, string? addressDetail, string? fullAddress)
        {
            try
            {
                var addressObj = new Dictionary<string, string?>();
                
                if (!string.IsNullOrWhiteSpace(provinceCode))
                    addressObj["p"] = provinceCode;
                if (!string.IsNullOrWhiteSpace(provinceName))
                    addressObj["pn"] = provinceName;
                if (!string.IsNullOrWhiteSpace(communeCode))
                    addressObj["c"] = communeCode;
                if (!string.IsNullOrWhiteSpace(communeName))
                    addressObj["cn"] = communeName;
                if (!string.IsNullOrWhiteSpace(addressDetail))
                    addressObj["ad"] = addressDetail;
                if (!string.IsNullOrWhiteSpace(fullAddress))
                    addressObj["fa"] = fullAddress;

                if (addressObj.Count == 0)
                    return null;

                var jsonString = JsonSerializer.Serialize(addressObj);
                
                // Kiểm tra độ dài (giới hạn 200 ký tự)
                if (jsonString.Length <= 200)
                {
                    return jsonString;
                }
                else
                {
                    // Nếu quá dài, chỉ lưu fullAddress
                    return fullAddress;
                }
            }
            catch
            {
                // Nếu serialize lỗi, trả về fullAddress
                return fullAddress;
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

                // Tạo phiếu xuất hàng khi trạng thái chuyển từ "Chờ xử lý" sang "Đang xử lý"
                if (oldStatus == "Chờ xử lý" && dto.Status == "Đang xử lý")
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
                    CustomerName = saleInvoice.Customer?.CustomerName,
                    CustomerPhone = saleInvoice.Phone ?? saleInvoice.Customer?.PhoneNumber // Ưu tiên lấy từ SaleInvoice.Phone
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

        // GET: api/admin/sale-invoices/{id}/export-excel
        // Xuất hóa đơn ra Excel
        [HttpGet("{id}/export-excel")]
        public async Task<IActionResult> ExportToExcel(string id)
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

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Hóa đơn");

                // Header TenTech
                worksheet.Cells[1, 1].Value = "TenTech";
                worksheet.Cells[1, 1, 1, 5].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 20;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(129, 196, 8)); // #81C408
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                worksheet.Row(1).Height = 30;

                // Thông tin đơn hàng
                int row = 3;
                worksheet.Cells[row, 1].Value = "HÓA ĐƠN";
                worksheet.Cells[row, 1, row, 5].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Size = 14;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                row++;

                worksheet.Cells[row, 1].Value = $"SỐ {saleInvoice.SaleInvoiceId}";
                worksheet.Cells[row, 1, row, 5].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Size = 16;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(220, 53, 69)); // #dc3545
                worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                row++;

                worksheet.Cells[row, 1].Value = $"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}";
                worksheet.Cells[row, 1, row, 5].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                row++;
                row++;

                // Thông tin khách hàng
                worksheet.Cells[row, 1].Value = "1. Thông tin người đặt hàng";
                worksheet.Cells[row, 1, row, 5].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;
                worksheet.Cells[row, 1].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                row++;

                worksheet.Cells[row, 1].Value = "Họ tên:";
                worksheet.Cells[row, 2].Value = saleInvoice.Customer?.CustomerName ?? "-";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                worksheet.Cells[row, 1].Value = "Điện thoại:";
                worksheet.Cells[row, 2].Value = saleInvoice.Phone ?? saleInvoice.Customer?.PhoneNumber ?? "-";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                worksheet.Cells[row, 1].Value = "Địa chỉ:";
                worksheet.Cells[row, 2].Value = saleInvoice.DeliveryAddress ?? "-";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                row++;

                worksheet.Cells[row, 1].Value = "Phương thức thanh toán:";
                worksheet.Cells[row, 2].Value = saleInvoice.PaymentMethod ?? "-";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;
                worksheet.Cells[row, 1].Value = "Nhân viên:";
                worksheet.Cells[row, 2].Value = saleInvoice.Employee?.EmployeeName ?? "-";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                row++;

                // Chi tiết sản phẩm
                worksheet.Cells[row, 1].Value = "2. Sản phẩm đặt hàng";
                worksheet.Cells[row, 1, row, 5].Merge = true;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                worksheet.Cells[row, 1].Style.Font.Size = 12;
                worksheet.Cells[row, 1].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                worksheet.Cells[row, 1].Style.Border.Bottom.Color.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                row++;

                worksheet.Cells[row, 1].Value = "#";
                worksheet.Cells[row, 2].Value = "Tên sản phẩm";
                worksheet.Cells[row, 3].Value = "SL";
                worksheet.Cells[row, 4].Value = "Giá tiền";
                worksheet.Cells[row, 5].Value = "Tổng (SLxG)";
                worksheet.Cells[row, 1, row, 5].Style.Font.Bold = true;
                worksheet.Cells[row, 1, row, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[row, 1, row, 5].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(248, 249, 250)); // #f8f9fa
                worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                row++;

                int stt = 1;
                decimal subtotal = 0;
                foreach (var detail in saleInvoice.SaleInvoiceDetails ?? new List<SaleInvoiceDetail>())
                {
                    worksheet.Cells[row, 1].Value = stt;
                    worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 2].Value = detail.Product?.ProductName ?? "-";
                    if (!string.IsNullOrEmpty(detail.Specifications))
                    {
                        worksheet.Cells[row, 2].Value = $"{detail.Product?.ProductName ?? "-"}\n{detail.Specifications}";
                    }
                    worksheet.Cells[row, 3].Value = detail.Quantity ?? 0;
                    worksheet.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                    worksheet.Cells[row, 4].Value = detail.UnitPrice ?? 0;
                    worksheet.Cells[row, 4].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    var itemTotal = (detail.Quantity ?? 0) * (detail.UnitPrice ?? 0);
                    worksheet.Cells[row, 5].Value = itemTotal;
                    worksheet.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    subtotal += itemTotal;
                    row++;
                    stt++;
                }

                row++;
                row++;

                // Tổng tiền
                worksheet.Cells[row, 3].Value = "Tạm tính:";
                worksheet.Cells[row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 5].Value = subtotal;
                worksheet.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                row++;

                worksheet.Cells[row, 3].Value = "Phí giao hàng:";
                worksheet.Cells[row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 5].Value = saleInvoice.DeliveryFee ?? 0;
                worksheet.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                row++;

                if (saleInvoice.Discount > 0)
                {
                    worksheet.Cells[row, 3].Value = "Giảm giá:";
                    worksheet.Cells[row, 3].Style.Font.Bold = true;
                    worksheet.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    worksheet.Cells[row, 5].Value = -saleInvoice.Discount;
                    worksheet.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                    worksheet.Cells[row, 5].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(220, 53, 69)); // #dc3545
                    row++;
                }

                // Border top cho tổng tiền
                worksheet.Cells[row, 3, row, 5].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thick;
                worksheet.Cells[row, 3, row, 5].Style.Border.Top.Color.SetColor(System.Drawing.Color.FromArgb(129, 196, 8));
                worksheet.Cells[row, 3].Value = "Tổng tiền thanh toán:";
                worksheet.Cells[row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 3].Style.Font.Size = 14;
                worksheet.Cells[row, 3].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 5].Value = saleInvoice.TotalAmount ?? 0;
                worksheet.Cells[row, 5].Style.Font.Bold = true;
                worksheet.Cells[row, 5].Style.Font.Size = 14;
                worksheet.Cells[row, 5].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(220, 53, 69)); // #dc3545
                worksheet.Cells[row, 5].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

                // Format số tiền
                worksheet.Cells[3, 5, row, 5].Style.Numberformat.Format = "#,##0";

                // Auto fit columns
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"HoaDon_{saleInvoice.SaleInvoiceId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất hóa đơn ra Excel", error = ex.Message });
            }
        }

        // GET: api/admin/sale-invoices/{id}/export-pdf
        // Xuất hóa đơn ra PDF
        [HttpGet("{id}/export-pdf")]
        public async Task<IActionResult> ExportToPdf(string id)
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

                QuestPDF.Settings.License = LicenseType.Community;

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Content()
                            .Column(column =>
                            {
                                column.Spacing(0.5f, Unit.Centimetre);

                                // TenTech header trong content
                                column.Item()
                                    .Background(Colors.Green.Lighten1)
                                    .Padding(15)
                                    .AlignCenter()
                                    .Text("TenTech")
                                    .FontSize(20)
                                    .Bold()
                                    .FontColor(Colors.White);

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Thông tin đơn hàng
                                column.Item().AlignCenter().Text("HÓA ĐƠN").FontSize(14).Bold();
                                column.Item().AlignCenter().Text($"SỐ {saleInvoice.SaleInvoiceId ?? "-"}").FontSize(16).Bold().FontColor(Colors.Red.Darken1);
                                column.Item().AlignCenter().Text($"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Thông tin khách hàng
                                column.Item().Text("1. Thông tin người đặt hàng").Bold().FontSize(12);
                                column.Item().LineHorizontal(1).LineColor(Colors.Green.Lighten1);
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Họ tên:").Bold();
                                    row.RelativeItem(2).Text(saleInvoice.Customer?.CustomerName ?? "-");
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Điện thoại:").Bold();
                                    row.RelativeItem(2).Text(saleInvoice.Phone ?? saleInvoice.Customer?.PhoneNumber ?? "-");
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Địa chỉ:").Bold();
                                    row.RelativeItem(2).Text(saleInvoice.DeliveryAddress ?? "-");
                                });

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Phương thức thanh toán và nhân viên
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Phương thức thanh toán:").Bold();
                                    row.RelativeItem(2).Text(saleInvoice.PaymentMethod ?? "-");
                                });
                                column.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("Nhân viên:").Bold();
                                    row.RelativeItem(2).Text(saleInvoice.Employee?.EmployeeName ?? "-");
                                });

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Chi tiết sản phẩm
                                column.Item().Text("2. Sản phẩm đặt hàng").Bold().FontSize(12);
                                column.Item().LineHorizontal(1).LineColor(Colors.Green.Lighten1);
                                
                                // Tính subtotal trước
                                decimal subtotal = 0;
                                foreach (var detail in saleInvoice.SaleInvoiceDetails ?? new List<SaleInvoiceDetail>())
                                {
                                    var itemTotal = (detail.Quantity ?? 0) * (detail.UnitPrice ?? 0);
                                    subtotal += itemTotal;
                                }

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.5f);
                                        columns.RelativeColumn(2f);
                                        columns.RelativeColumn(0.8f);
                                        columns.RelativeColumn(1.2f);
                                        columns.RelativeColumn(1.2f);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellStyleHeader).Text("#").Bold();
                                        header.Cell().Element(CellStyleHeader).Text("Tên sản phẩm").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignCenter().Text("SL").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Giá tiền").Bold();
                                        header.Cell().Element(CellStyleHeader).AlignRight().Text("Tổng (SLxG)").Bold();
                                    });

                                    int stt = 1;
                                    foreach (var detail in saleInvoice.SaleInvoiceDetails ?? new List<SaleInvoiceDetail>())
                                    {
                                        var itemTotal = (detail.Quantity ?? 0) * (detail.UnitPrice ?? 0);
                                        var productName = detail.Product?.ProductName ?? "-";
                                        if (!string.IsNullOrEmpty(detail.Specifications))
                                        {
                                            productName = $"{productName}\n{detail.Specifications}";
                                        }

                                        table.Cell().Element(CellStyle).Text(stt.ToString());
                                        table.Cell().Element(CellStyle).Text(productName);
                                        table.Cell().Element(CellStyle).AlignCenter().Text((detail.Quantity ?? 0).ToString());
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(detail.UnitPrice ?? 0));
                                        table.Cell().Element(CellStyle).AlignRight().Text(FormatCurrency(itemTotal));
                                        stt++;
                                    }
                                });

                                column.Item().PaddingTop(0.5f, Unit.Centimetre);

                                // Tổng tiền
                                decimal total = saleInvoice.TotalAmount ?? 0;
                                decimal deliveryFee = saleInvoice.DeliveryFee ?? 0;
                                decimal discount = saleInvoice.Discount ?? 0;

                                // Tổng tiền - giống Excel, căn phải, mở rộng, không xuống dòng
                                column.Item().AlignRight().Row(row =>
                                {
                                    row.RelativeItem(1);
                                    row.ConstantItem(7, Unit.Centimetre).Column(col =>
                                    {
                                        col.Item().Row(r =>
                                        {
                                            r.RelativeItem(3).Text("Tạm tính:").Bold();
                                            r.ConstantItem(4, Unit.Centimetre).AlignRight().Text(FormatCurrency(subtotal));
                                        });
                                        col.Item().Row(r =>
                                        {
                                            r.RelativeItem(3).Text("Phí giao hàng:").Bold();
                                            r.ConstantItem(4, Unit.Centimetre).AlignRight().Text(FormatCurrency(deliveryFee));
                                        });
                                        if (discount > 0)
                                        {
                                            col.Item().Row(r =>
                                            {
                                                r.RelativeItem(3).Text("Giảm giá:").Bold();
                                                r.ConstantItem(4, Unit.Centimetre).AlignRight().Text("-" + FormatCurrency(discount)).FontColor(Colors.Red.Darken1);
                                            });
                                        }
                                        col.Item().PaddingTop(0.3f, Unit.Centimetre);
                                        col.Item().LineHorizontal(2).LineColor(Colors.Green.Lighten1);
                                        col.Item().Row(r =>
                                        {
                                            r.RelativeItem(3).Text("Tổng tiền thanh toán:").Bold().FontSize(14);
                                            r.ConstantItem(4, Unit.Centimetre).AlignRight().Text(FormatCurrency(total)).Bold().FontSize(14).FontColor(Colors.Red.Darken1);
                                        });
                                    });
                                });
                            });

                        
                    });
                });

                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                var fileName = $"HoaDon_{saleInvoice.SaleInvoiceId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(stream.ToArray(), "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xuất hóa đơn ra PDF", error = ex.Message });
            }
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .Padding(5)
                .Background(Colors.White);
        }

        private static IContainer CellStyleHeader(IContainer container)
        {
            return container
                .Border(1)
                .Padding(5)
                .Background(Colors.White);
        }

        private static string FormatCurrency(decimal amount)
        {
            return amount.ToString("N0") + " đ";
        }

    }
}
