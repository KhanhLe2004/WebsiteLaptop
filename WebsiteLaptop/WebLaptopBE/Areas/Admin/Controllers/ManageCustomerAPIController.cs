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

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/customers")]
    [ApiController]
    public class ManageCustomerAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _context;
        private readonly IWebHostEnvironment _environment;
        private readonly HttpClient _httpClient;
        private readonly HistoryService _historyService;
        private const string ADDRESS_API_BASE_URL = "https://production.cas.so/address-kit/2025-07-01";

        public ManageCustomerAPIController(Testlaptop36Context context, IWebHostEnvironment environment, IHttpClientFactory httpClientFactory, HistoryService historyService)
        {
            _context = context;
            _environment = environment;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _historyService = historyService;
        }

        // Helper method để lấy EmployeeId từ header
        private string? GetEmployeeId()
        {
            return Request.Headers["X-Employee-Id"].FirstOrDefault();
        }

        // GET: api/admin/customers
        // Lấy danh sách khách hàng có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<CustomerDTO>>> GetCustomers(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] bool? active = null)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100; // Giới hạn tối đa

                var query = _context.Customers.AsQueryable();

                // Tìm kiếm theo tên, email, số điện thoại hoặc mã khách hàng
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(c => 
                        (c.CustomerName != null && c.CustomerName.ToLower().Contains(searchTerm)) || 
                        (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                        (c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(searchTerm)) ||
                        c.CustomerId.ToLower().Contains(searchTerm));
                }

                // Lọc theo trạng thái active
                if (active.HasValue)
                {
                    query = query.Where(c => c.Active == active.Value);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var customers = await query
                    .OrderByDescending(c => c.CustomerId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var customerDTOs = customers.Select(c => ParseCustomerAddress(c)).ToList();

                var result = new PagedResult<CustomerDTO>
                {
                    Items = customerDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách khách hàng", error = ex.Message });
            }
        }

        // GET: api/admin/customers/{id}
        // Lấy chi tiết một khách hàng
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDTO>> GetCustomer(string id)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == id);

                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                var customerDTO = ParseCustomerAddress(customer);

                return Ok(customerDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin khách hàng", error = ex.Message });
            }
        }

        // PUT: api/admin/customers/{id}
        // Cập nhật khách hàng
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(string id, [FromForm] CustomerUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Kiểm tra username đã tồn tại (nếu có và khác username hiện tại)
                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != customer.Username)
                {
                    if (await _context.Customers.AnyAsync(c => c.Username == dto.Username))
                    {
                        return Conflict(new { message = "Tên đăng nhập đã tồn tại" });
                    }
                }

                // Kiểm tra email đã tồn tại (nếu có và khác email hiện tại)
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != customer.Email)
                {
                    if (await _context.Customers.AnyAsync(c => c.Email == dto.Email))
                    {
                        return Conflict(new { message = "Email đã tồn tại" });
                    }
                }

                // Xử lý upload ảnh mới
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(customer.Avatar))
                    {
                        DeleteImage(customer.Avatar);
                    }

                    customer.Avatar = await SaveImageAsync(dto.AvatarFile);
                }
                else if (dto.AvatarToDelete == true)
                {
                    // Xóa avatar nếu được yêu cầu
                    if (!string.IsNullOrEmpty(customer.Avatar))
                    {
                        DeleteImage(customer.Avatar);
                    }
                    customer.Avatar = null;
                }

                // Cập nhật thông tin
                customer.CustomerName = dto.CustomerName;
                customer.DateOfBirth = dto.DateOfBirth;
                customer.PhoneNumber = dto.PhoneNumber;
                customer.Email = dto.Email;
                customer.Username = dto.Username;
                
                // Chỉ cập nhật password nếu có giá trị mới
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    customer.Password = dto.Password; // Lưu mật khẩu dạng plain text (nên hash trong production)
                }

                // Xử lý địa chỉ mới
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
                        // Log warning nhưng không block update
                        Console.WriteLine($"Warning: Could not fetch province name from API: {apiEx.Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.CommuneCode) && !string.IsNullOrWhiteSpace(dto.ProvinceCode))
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                        var response = await _httpClient.GetAsync($"{ADDRESS_API_BASE_URL}/provinces/{dto.ProvinceCode}/communes", cts.Token);
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonString = await response.Content.ReadAsStringAsync(cts.Token);
                            var jsonDoc = JsonDocument.Parse(jsonString);
                            if (jsonDoc.RootElement.TryGetProperty("communes", out var communesElement))
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
                    catch (Exception ex)
                    {
                        // Log warning nhưng không block update
                        Console.WriteLine($"Warning: Không thể lấy tên phường/xã: {ex.Message}");
                    }
                }

                // Xử lý địa chỉ: ghép từ các thành phần
                string? fullAddress = BuildFullAddress(dto.AddressDetail, communeName, provinceName);
                if (string.IsNullOrWhiteSpace(fullAddress) && !string.IsNullOrWhiteSpace(dto.Address))
                {
                    fullAddress = dto.Address; // Giữ nguyên Address cũ nếu không có thông tin mới
                }

                // Lưu thông tin địa chỉ chi tiết vào Address dưới dạng JSON
                // Nhưng nếu JSON quá dài (> 200 ký tự), chỉ lưu FullAddress
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
                        // Nếu có lỗi khi serialize JSON, chỉ lưu FullAddress
                        Console.WriteLine($"Error serializing address JSON: {jsonEx.Message}");
                        finalAddress = fullAddress;
                    }
                }

                customer.Address = finalAddress;

                await _context.SaveChangesAsync();

                // Reload customer để lấy thông tin đầy đủ
                await _context.Entry(customer).ReloadAsync();
                
                var result = ParseCustomerAddress(customer);
                
                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Sửa khách hàng: {id} - {customer.CustomerName}");
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Log inner exception để debug
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                
                // Log stack trace để debug
                Console.WriteLine($"Error updating customer: {errorMessage}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { message = "Lỗi khi cập nhật khách hàng", error = errorMessage });
            }
        }

        // DELETE: api/admin/customers/{id}
        // Ẩn khách hàng (set active = false) thay vì xóa thực sự
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(string id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Set active = false thay vì xóa
                customer.Active = false;
                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Xóa khách hàng: {id} - {customer.CustomerName}");
                }

                return Ok(new { message = "Đã ẩn khách hàng thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding customer: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi ẩn khách hàng", error = ex.Message });
            }
        }

        // POST: api/admin/customers/{id}/restore
        // Khôi phục khách hàng (set active = true)
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreCustomer(string id)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(id);
                if (customer == null)
                {
                    return NotFound(new { message = "Không tìm thấy khách hàng" });
                }

                // Set active = true
                customer.Active = true;
                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Khôi phục khách hàng: {id} - {customer.CustomerName}");
                }

                return Ok(new { message = "Khôi phục khách hàng thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring customer: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi khôi phục khách hàng", error = ex.Message });
            }
        }

        // Helper method để load customer với đầy đủ thông tin
        private async Task<CustomerDTO> GetCustomerByIdAsync(string customerId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return null!;
            }

            return new CustomerDTO
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.CustomerName,
                DateOfBirth = customer.DateOfBirth,
                PhoneNumber = customer.PhoneNumber,
                Address = customer.Address,
                Email = customer.Email,
                Avatar = customer.Avatar,
                Username = customer.Username,
                Active = customer.Active
            };
        }

        // Hàm hỗ trợ lưu ảnh
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty");
                }

                // Validate file type
                if (string.IsNullOrEmpty(file.ContentType) || !file.ContentType.StartsWith("image/"))
                {
                    throw new ArgumentException($"Invalid file type: {file.ContentType}. Only image files are allowed.");
                }

                // Validate file size (max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    throw new ArgumentException($"File size ({file.Length} bytes) exceeds maximum allowed size ({maxFileSize} bytes)");
                }

                // Tạo tên file unique
                var fileExtension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    // Nếu không có extension, thử lấy từ content type
                    fileExtension = file.ContentType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/gif" => ".gif",
                        "image/webp" => ".webp",
                        _ => ".jpg" // Default
                    };
                }

                // Tính toán độ dài tối đa cho phần baseName để tổng độ dài (base + ext) <= 20
                const int maxTotalLength = 20;
                var extLength = fileExtension.Length;
                var baseNameMaxLength = Math.Max(1, maxTotalLength - extLength);

                // Tạo baseName ngẫu nhiên có độ dài phù hợp
                string GenerateRandomBaseName(int length)
                {
                    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    var rnd = new Random();
                    var buffer = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        buffer[i] = chars[rnd.Next(chars.Length)];
                    }
                    return new string(buffer);
                }

                var baseName = GenerateRandomBaseName(baseNameMaxLength);
                var fileName = $"{baseName}{fileExtension}";
                
                // Đường dẫn thư mục lưu ảnh
                var webRootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                var uploadsFolder = Path.Combine(webRootPath, "imageCustomers");
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
                // Lưu file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lưu ảnh: {ex.Message}", ex);
            }
        }

        // Hàm hỗ trợ xóa ảnh
        private void DeleteImage(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "imageCustomers", fileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch
            {
                // Không throw exception nếu xóa file thất bại
            }
        }

        // GET: api/admin/customers/provinces
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

        // GET: api/admin/customers/provinces/{provinceCode}/communes
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

        // Helper method để parse địa chỉ từ Customer và tạo CustomerDTO
        private CustomerDTO ParseCustomerAddress(Customer customer)
        {
            try
            {
                var dto = new CustomerDTO
                {
                    CustomerId = customer.CustomerId,
                    CustomerName = customer.CustomerName,
                    DateOfBirth = customer.DateOfBirth,
                    PhoneNumber = customer.PhoneNumber,
                    Address = customer.Address,
                    Email = customer.Email,
                    Avatar = customer.Avatar,
                    Username = customer.Username,
                    Active = customer.Active,
                    PasswordLength = !string.IsNullOrEmpty(customer.Password) ? customer.Password.Length : (int?)null
                };

                // Thử parse JSON từ Address (hỗ trợ cả format cũ và mới dạng key ngắn)
                if (!string.IsNullOrWhiteSpace(customer.Address))
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(customer.Address);
                        var root = jsonDoc.RootElement;

                        dto.ProvinceCode = root.TryGetProperty("ProvinceCode", out var provinceCode) ? provinceCode.GetString()
                            : root.TryGetProperty("p", out var pShort) ? pShort.GetString() : dto.ProvinceCode;

                        dto.CommuneCode = root.TryGetProperty("CommuneCode", out var communeCode) ? communeCode.GetString()
                            : root.TryGetProperty("c", out var cShort) ? cShort.GetString() : dto.CommuneCode;

                        dto.AddressDetail = root.TryGetProperty("AddressDetail", out var addressDetail) ? addressDetail.GetString()
                            : root.TryGetProperty("ad", out var adShort) ? adShort.GetString() : dto.AddressDetail;

                        if (root.TryGetProperty("FullAddress", out var fullAddress))
                        {
                            dto.Address = fullAddress.GetString();
                        }
                        else if (root.TryGetProperty("fa", out var faShort))
                        {
                            dto.Address = faShort.GetString();
                        }
                    }
                    catch
                    {
                        // Nếu không phải JSON, giữ nguyên Address và thử parse format legacy
                        try
                        {
                            ParseLegacyAddress(customer.Address, dto);
                        }
                        catch
                        {
                            // Nếu parse legacy cũng lỗi, giữ nguyên Address
                        }
                    }
                }
                else
                {
                    try
                    {
                        ParseLegacyAddress(customer.Address, dto);
                    }
                    catch
                    {
                        // Nếu parse legacy lỗi, giữ nguyên Address
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
                // Nếu có lỗi nghiêm trọng, trả về DTO cơ bản
                return new CustomerDTO
                {
                    CustomerId = customer?.CustomerId ?? "",
                    CustomerName = customer?.CustomerName,
                    DateOfBirth = customer?.DateOfBirth,
                    PhoneNumber = customer?.PhoneNumber,
                    Address = customer?.Address,
                    Email = customer?.Email,
                    Avatar = customer?.Avatar,
                    Username = customer?.Username,
                    Active = customer?.Active,
                    PasswordLength = !string.IsNullOrEmpty(customer?.Password) ? customer.Password.Length : (int?)null
                };
            }
        }

        private void ParseLegacyAddress(string? address, CustomerDTO dto)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            dto.Address ??= address;

            // Ưu tiên tìm mã từ format có nhãn [Phường/Xã: code], [Tỉnh/Thành: code]
            string detailPart = address;
            bool foundProvinceCode = false;
            bool foundCommuneCode = false;

            int communeIndex = address.IndexOf("[Phường/Xã:", StringComparison.OrdinalIgnoreCase);
            if (communeIndex >= 0)
            {
                int end = address.IndexOf(']', communeIndex);
                if (end > communeIndex)
                {
                    var communeSegment = address.Substring(communeIndex, end - communeIndex);
                    var code = communeSegment.Split(':').LastOrDefault()?.Trim();
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        dto.CommuneCode ??= code;
                        foundCommuneCode = true;
                    }
                }
                detailPart = address.Substring(0, communeIndex).Trim().TrimEnd(',');
            }

            int provinceIndex = address.IndexOf("[Tỉnh/Thành:", StringComparison.OrdinalIgnoreCase);
            if (provinceIndex >= 0)
            {
                int end = address.IndexOf(']', provinceIndex);
                if (end > provinceIndex)
                {
                    var provinceSegment = address.Substring(provinceIndex, end - provinceIndex);
                    var code = provinceSegment.Split(':').LastOrDefault()?.Trim();
                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        dto.ProvinceCode ??= code;
                        foundProvinceCode = true;
                    }
                }
                if (provinceIndex < detailPart.Length)
                {
                    detailPart = detailPart.Substring(0, provinceIndex).Trim().TrimEnd(',');
                }
            }

            // Nếu chưa tìm được mã từ format có nhãn, thử parse theo dấu phẩy
            // Nhưng chỉ lấy địa chỉ cụ thể, không gán tên vào mã
            if (!foundProvinceCode || !foundCommuneCode)
            {
                var parts = address
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();

                if (parts.Count >= 1)
                {
                    // Chỉ lấy địa chỉ cụ thể từ phần đầu
                    // Không gán tên vào ProvinceCode/CommuneCode vì đó là tên, không phải mã
                    if (string.IsNullOrWhiteSpace(dto.AddressDetail))
                    {
                        dto.AddressDetail = parts[0];
                    }
                }
            }
            else
            {
                // Nếu đã tìm được mã từ format có nhãn, lấy địa chỉ cụ thể từ detailPart
                dto.AddressDetail ??= detailPart;
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
    }
}
