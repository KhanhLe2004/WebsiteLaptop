using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;
using WebLaptopBE.Services;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/employees")]
    [ApiController]
    public class ManageEmployeeAPIController : ControllerBase
    {
        private readonly Testlaptop38Context _context;
        private readonly IWebHostEnvironment _environment;
        private readonly HttpClient _httpClient;
        private readonly HistoryService _historyService;
        private const string ADDRESS_API_BASE_URL = "https://production.cas.so/address-kit/2025-07-01";

        public ManageEmployeeAPIController(Testlaptop38Context context, IWebHostEnvironment environment, IHttpClientFactory httpClientFactory, HistoryService historyService)
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

        // GET: api/admin/employees
        // Lấy danh sách nhân viên có phân trang và tìm kiếm
        [HttpGet]
        public async Task<ActionResult<PagedResult<EmployeeDTO>>> GetEmployees(
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

                var query = _context.Employees.AsQueryable();

                // Tìm kiếm theo tên, email, số điện thoại hoặc mã nhân viên
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(e => 
                        (e.EmployeeName != null && e.EmployeeName.ToLower().Contains(searchTerm)) || 
                        (e.Email != null && e.Email.ToLower().Contains(searchTerm)) ||
                        (e.PhoneNumber != null && e.PhoneNumber.ToLower().Contains(searchTerm)) ||
                        e.EmployeeId.ToLower().Contains(searchTerm));
                }

                // Lọc theo trạng thái active
                if (active.HasValue)
                {
                    query = query.Where(e => e.Active == active.Value);
                }

                // Đếm tổng số
                var totalItems = await query.CountAsync();

                // Lấy dữ liệu theo trang
                var employees = await query
                    .OrderByDescending(e => e.EmployeeId)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var employeeDTOs = employees.Select(e => ParseEmployeeAddress(e)).ToList();

                var result = new PagedResult<EmployeeDTO>
                {
                    Items = employeeDTOs,
                    TotalItems = totalItems,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách nhân viên", error = ex.Message });
            }
        }

        // GET: api/admin/employees/{id}
        // Lấy chi tiết một nhân viên
        [HttpGet("{id}")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployee(string id)
        {
            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhân viên" });
                }

                var employeeDTO = ParseEmployeeAddress(employee);

                return Ok(employeeDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin nhân viên", error = ex.Message });
            }
        }

        // POST: api/admin/employees
        // Tạo mới nhân viên
        [HttpPost]
        public async Task<ActionResult<EmployeeDTO>> CreateEmployee([FromForm] EmployeeCreateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Kiểm tra username đã tồn tại
                if (!string.IsNullOrWhiteSpace(dto.Username))
                {
                    if (await _context.Employees.AnyAsync(e => e.Username == dto.Username))
                    {
                        return Conflict(new { message = "Tên đăng nhập đã tồn tại" });
                    }
                }

                // Kiểm tra email đã tồn tại
                if (!string.IsNullOrWhiteSpace(dto.Email))
                {
                    if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
                    {
                        return Conflict(new { message = "Email đã tồn tại" });
                    }
                }

                // Tạo mã nhân viên mới (tự động generate nếu chưa có)
                string newEmployeeId;
                if (!string.IsNullOrWhiteSpace(dto.EmployeeId))
                {
                    // Sử dụng EmployeeId từ frontend (đã được generate tự động)
                    newEmployeeId = dto.EmployeeId.Trim();
                    
                    // Kiểm tra mã đã tồn tại chưa
                    if (await _context.Employees.AnyAsync(e => e.EmployeeId == newEmployeeId))
                    {
                        // Nếu đã tồn tại, tạo mã mới
                        newEmployeeId = await GenerateEmployeeIdAsync();
                    }
                }
                else
                {
                    // Tự động tạo mã mới
                    newEmployeeId = await GenerateEmployeeIdAsync();
                }

                // Xử lý upload ảnh
                string? avatarFileName = null;
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    avatarFileName = await SaveImageAsync(dto.AvatarFile);
                }

                // Lấy tên tỉnh/thành và phường/xã từ API (không bắt buộc, nếu lỗi thì bỏ qua)
                // Sử dụng Task để không block quá trình tạo nhân viên
                string? provinceName = null;
                string? communeName = null;
                
                // Chỉ lấy tên nếu có mã, nhưng không block nếu API lỗi
                if (!string.IsNullOrWhiteSpace(dto.ProvinceCode))
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Timeout 3 giây
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
                        // Log nhưng không throw - chỉ lưu mã, không lưu tên
                        Console.WriteLine($"Warning: Could not fetch province name from API: {apiEx.Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(dto.ProvinceCode) && !string.IsNullOrWhiteSpace(dto.CommuneCode))
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Timeout 3 giây
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
                        // Log nhưng không throw - chỉ lưu mã, không lưu tên
                        Console.WriteLine($"Warning: Could not fetch commune name from API: {apiEx.Message}");
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
                        var addressInfo = new
                        {
                            ProvinceCode = dto.ProvinceCode,
                            ProvinceName = provinceName,
                            CommuneCode = dto.CommuneCode,
                            CommuneName = communeName,
                            AddressDetail = dto.AddressDetail,
                            FullAddress = fullAddress
                        };
                        var addressJson = System.Text.Json.JsonSerializer.Serialize(addressInfo);
                        
                        // Kiểm tra độ dài (giới hạn 200 ký tự cho database)
                        if (addressJson != null && addressJson.Length <= 200)
                        {
                            finalAddress = addressJson;
                        }
                        // Nếu quá dài, chỉ lưu FullAddress
                    }
                    catch (Exception jsonEx)
                    {
                        // Nếu có lỗi khi serialize JSON, chỉ lưu FullAddress
                        Console.WriteLine($"Error serializing address JSON: {jsonEx.Message}");
                    }
                }

                // Kiểm tra RoleId có tồn tại trong database không (nếu có giá trị)
                if (!string.IsNullOrWhiteSpace(dto.RoleId))
                {
                    var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId);
                    if (!roleExists)
                    {
                        return BadRequest(new { message = "Vai trò không tồn tại" });
                    }
                }

                // Tạo nhân viên mới
                var employee = new Employee
                {
                    EmployeeId = newEmployeeId,
                    EmployeeName = dto.EmployeeName,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber,
                    Address = finalAddress,
                    Email = dto.Email,
                    Username = dto.Username,
                    Password = dto.Password, // Lưu mật khẩu dạng plain text (nên hash trong production)
                    RoleId = string.IsNullOrWhiteSpace(dto.RoleId) ? null : dto.RoleId,
                    Avatar = avatarFileName,
                    Active = true // Mặc định active = true
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                var result = ParseEmployeeAddress(employee);

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Thêm nhân viên: {employee.EmployeeId} - {employee.EmployeeName}");
                }

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, result);
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
                Console.WriteLine($"Error creating employee: {errorMessage}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                return StatusCode(500, new { message = "Lỗi khi tạo nhân viên", error = errorMessage });
            }
        }

        // PUT: api/admin/employees/{id}
        // Cập nhật nhân viên
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEmployee(string id, [FromForm] EmployeeUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhân viên" });
                }

                // Kiểm tra username đã tồn tại (nếu có và khác username hiện tại)
                if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != employee.Username)
                {
                    if (await _context.Employees.AnyAsync(e => e.Username == dto.Username))
                    {
                        return Conflict(new { message = "Tên đăng nhập đã tồn tại" });
                    }
                }

                // Kiểm tra email đã tồn tại (nếu có và khác email hiện tại)
                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != employee.Email)
                {
                    if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
                    {
                        return Conflict(new { message = "Email đã tồn tại" });
                    }
                }

                // Xử lý upload ảnh mới
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(employee.Avatar))
                    {
                        DeleteImage(employee.Avatar);
                    }

                    employee.Avatar = await SaveImageAsync(dto.AvatarFile);
                }
                else if (dto.AvatarToDelete == true)
                {
                    // Xóa avatar nếu được yêu cầu
                    if (!string.IsNullOrEmpty(employee.Avatar))
                    {
                        DeleteImage(employee.Avatar);
                    }
                    employee.Avatar = null;
                }

                // Lấy tên tỉnh/thành và phường/xã từ API (không bắt buộc, nếu lỗi thì bỏ qua)
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
                if (string.IsNullOrWhiteSpace(fullAddress) && !string.IsNullOrWhiteSpace(dto.Address))
                {
                    fullAddress = dto.Address; // Giữ nguyên Address cũ nếu không có thông tin mới
                }

                // Lưu thông tin địa chỉ chi tiết vào Address dưới dạng JSON (giới hạn độ dài)
                string? finalAddress = fullAddress;
                if (!string.IsNullOrWhiteSpace(dto.ProvinceCode) || !string.IsNullOrWhiteSpace(dto.CommuneCode) || !string.IsNullOrWhiteSpace(dto.AddressDetail))
                {
                    try
                    {
                        var addressInfo = new
                        {
                            ProvinceCode = dto.ProvinceCode,
                            ProvinceName = provinceName,
                            CommuneCode = dto.CommuneCode,
                            CommuneName = communeName,
                            AddressDetail = dto.AddressDetail,
                            FullAddress = fullAddress
                        };
                        var addressJson = System.Text.Json.JsonSerializer.Serialize(addressInfo);
                        if (addressJson != null && addressJson.Length <= 200)
                        {
                            finalAddress = addressJson;
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Console.WriteLine($"Error serializing address JSON (update): {jsonEx.Message}");
                    }
                }

                // Kiểm tra RoleId
                if (!string.IsNullOrWhiteSpace(dto.RoleId))
                {
                    var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId);
                    if (!roleExists)
                    {
                        return BadRequest(new { message = "Vai trò không tồn tại" });
                    }
                }

                // Cập nhật thông tin
                employee.EmployeeName = dto.EmployeeName;
                employee.DateOfBirth = dto.DateOfBirth;
                employee.PhoneNumber = dto.PhoneNumber;
                employee.Address = finalAddress;
                employee.Email = dto.Email;
                employee.Username = dto.Username;
                employee.RoleId = string.IsNullOrWhiteSpace(dto.RoleId) ? null : dto.RoleId;
                
                // Chỉ cập nhật password nếu có giá trị mới
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    employee.Password = dto.Password; // Lưu mật khẩu dạng plain text (nên hash trong production)
                }

                await _context.SaveChangesAsync();

                // Reload employee để lấy thông tin đầy đủ
                await _context.Entry(employee).ReloadAsync();
                
                var result = ParseEmployeeAddress(employee);
                
                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Sửa nhân viên: {id} - {employee.EmployeeName}");
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                Console.WriteLine($"Error updating employee {id}: {errorMessage}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Lỗi khi cập nhật nhân viên", error = errorMessage });
            }
        }

        // DELETE: api/admin/employees/{id}
        // Ẩn nhân viên (set active = false) thay vì xóa thực sự
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhân viên" });
                }

                // Set active = false thay vì xóa
                employee.Active = false;
                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Xóa nhân viên: {id} - {employee.EmployeeName}");
                }

                return Ok(new { message = "Đã ẩn nhân viên thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding employee: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi ẩn nhân viên", error = ex.Message });
            }
        }

        // POST: api/admin/employees/{id}/restore
        // Khôi phục nhân viên (set active = true)
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreEmployee(string id)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhân viên" });
                }

                // Set active = true
                employee.Active = true;
                await _context.SaveChangesAsync();

                // Ghi log lịch sử
                var employeeId = GetEmployeeId();
                if (!string.IsNullOrEmpty(employeeId))
                {
                    await _historyService.LogHistoryAsync(employeeId, $"Khôi phục nhân viên: {id} - {employee.EmployeeName}");
                }

                return Ok(new { message = "Khôi phục nhân viên thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring employee: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi khôi phục nhân viên", error = ex.Message });
            }
        }

        // GET: api/admin/employees/next-id
        // Lấy EmployeeId tiếp theo (tự động generate)
        [HttpGet("next-id")]
        public async Task<ActionResult> GetNextEmployeeId()
        {
            try
            {
                var nextId = await GenerateEmployeeIdAsync();
                return Ok(new { employeeId = nextId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo EmployeeId", error = ex.Message });
            }
        }

        // GET: api/admin/employees/roles
        // Lấy danh sách vai trò
        [HttpGet("roles")]
        public async Task<ActionResult<List<RoleDTO>>> GetRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .OrderBy(r => r.RoleName)
                    .Select(r => new RoleDTO
                    {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách vai trò", error = ex.Message });
            }
        }

        // GET: api/admin/employees/provinces
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

        // GET: api/admin/employees/provinces/{provinceCode}/communes
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

        // Helper method để tạo mã nhân viên mới
        private async Task<string> GenerateEmployeeIdAsync()
        {
            // Lấy mã nhân viên lớn nhất hiện có theo format E
            var lastEmployee = await _context.Employees
                .Where(e => e.EmployeeId.StartsWith("E") && e.EmployeeId.Length == 4)
                .OrderByDescending(e => e.EmployeeId)
                .FirstOrDefaultAsync();

            if (lastEmployee == null)
            {
                return "E001";
            }

            // Tách số từ mã nhân viên (ví dụ: E001 -> 001)
            string lastId = lastEmployee.EmployeeId;
            if (lastId.StartsWith("E") && lastId.Length > 1)
            {
                string numberPart = lastId.Substring(1);
                if (int.TryParse(numberPart, out int number))
                {
                    number++;
                    return $"E{number:D3}";
                }
            }

            // Nếu không parse được, tạo mã mới dựa trên số lượng nhân viên có format E
            int count = await _context.Employees
                .CountAsync(e => e.EmployeeId.StartsWith("E") && e.EmployeeId.Length == 4);
            return $"E{(count + 1):D3}";
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
                var uploadsFolder = Path.Combine(webRootPath, "imageEmployees");
                
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
                var filePath = Path.Combine(_environment.WebRootPath ?? "wwwroot", "imageEmployees", fileName);
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

        // Helper method để parse địa chỉ từ Employee và tạo EmployeeDTO
        private EmployeeDTO ParseEmployeeAddress(Employee employee)
        {
            try
            {
                var dto = new EmployeeDTO
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = employee.EmployeeName,
                    DateOfBirth = employee.DateOfBirth,
                    PhoneNumber = employee.PhoneNumber,
                    Address = employee.Address,
                    Email = employee.Email,
                    Avatar = employee.Avatar,
                    Username = employee.Username,
                    RoleId = employee.RoleId,
                    Active = employee.Active,
                    PasswordLength = !string.IsNullOrEmpty(employee.Password) ? employee.Password.Length : (int?)null
                };

                // Thử parse JSON từ Address (hỗ trợ cả format cũ và mới dạng key ngắn)
                if (!string.IsNullOrWhiteSpace(employee.Address))
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(employee.Address);
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
                            ParseLegacyAddress(employee.Address, dto);
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
                        ParseLegacyAddress(employee.Address, dto);
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
                return new EmployeeDTO
                {
                    EmployeeId = employee?.EmployeeId ?? "",
                    EmployeeName = employee?.EmployeeName,
                    DateOfBirth = employee?.DateOfBirth,
                    PhoneNumber = employee?.PhoneNumber,
                    Address = employee?.Address,
                    Email = employee?.Email,
                    Avatar = employee?.Avatar,
                    Username = employee?.Username,
                    RoleId = employee?.RoleId,
                    Active = employee?.Active,
                    PasswordLength = !string.IsNullOrEmpty(employee?.Password) ? employee.Password.Length : (int?)null
                };
            }
        }

        private void ParseLegacyAddress(string? address, EmployeeDTO dto)
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
    }
}
