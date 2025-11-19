using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManageProfileAPIController : ControllerBase
    {
        private readonly Testlaptop33Context _context;
        private readonly IWebHostEnvironment _environment;
        private readonly HttpClient _httpClient;
        private const string ADDRESS_API_BASE_URL = "https://production.cas.so/address-kit/2025-07-01";

        public ManageProfileAPIController(Testlaptop33Context context, IWebHostEnvironment environment, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _environment = environment;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // GET: api/ManageProfileAPI/{employeeId}
        // Lấy thông tin profile của nhân viên
        [HttpGet("{employeeId}")]
        public async Task<ActionResult<EmployeeDTO>> GetProfile(string employeeId)
        {
            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

                if (employee == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhân viên" });
                }

                var employeeDTO = ParseEmployeeAddress(employee);

                return Ok(employeeDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin profile", error = ex.Message });
            }
        }

        // PUT: api/ManageProfileAPI/{employeeId}
        // Cập nhật thông tin profile của nhân viên (chỉ cho phép cập nhật thông tin cá nhân)
        [HttpPut("{employeeId}")]
        public async Task<IActionResult> UpdateProfile(string employeeId, [FromForm] EmployeeProfileUpdateDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var employee = await _context.Employees.FindAsync(employeeId);
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

                // Cập nhật thông tin (KHÔNG cho phép thay đổi RoleId, BranchesId, EmployeeId)
                employee.EmployeeName = dto.EmployeeName;
                employee.DateOfBirth = dto.DateOfBirth;
                employee.PhoneNumber = dto.PhoneNumber;
                employee.Address = finalAddress;
                employee.Email = dto.Email;
                employee.Username = dto.Username;
                
                // Chỉ cập nhật password nếu có giá trị mới
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    employee.Password = dto.Password; // Lưu mật khẩu dạng plain text (nên hash trong production)
                }

                await _context.SaveChangesAsync();

                // Reload employee để lấy thông tin đầy đủ
                await _context.Entry(employee).ReloadAsync();
                
                var result = ParseEmployeeAddress(employee);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }
                Console.WriteLine($"Error updating profile {employeeId}: {errorMessage}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Lỗi khi cập nhật thông tin profile", error = errorMessage });
            }
        }

        // GET: api/ManageProfileAPI/provinces
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

        // GET: api/ManageProfileAPI/provinces/{provinceCode}/communes
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

        // PUT: api/ManageProfileAPI/change-password
        // Đổi mật khẩu của nhân viên
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Dữ liệu không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.EmployeeId))
                {
                    return BadRequest(new { message = "Mã nhân viên là bắt buộc" });
                }

                if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
                {
                    return BadRequest(new { message = "Mật khẩu hiện tại là bắt buộc" });
                }

                if (string.IsNullOrWhiteSpace(dto.NewPassword))
                {
                    return BadRequest(new { message = "Mật khẩu mới là bắt buộc" });
                }

                if (dto.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
                }

                if (dto.NewPassword != dto.ConfirmPassword)
                {
                    return BadRequest(new { message = "Mật khẩu xác nhận không khớp" });
                }

                var employee = await _context.Employees.FindAsync(dto.EmployeeId);
                if (employee == null)
                {
                    return NotFound(new { message = "Không tìm thấy nhân viên" });
                }

                // Kiểm tra mật khẩu hiện tại (so sánh plain text - trong production nên hash)
                if (employee.Password != dto.CurrentPassword)
                {
                    return Unauthorized(new { message = "Mật khẩu hiện tại không đúng" });
                }

                // Cập nhật mật khẩu mới (trong production nên hash mật khẩu)
                employee.Password = dto.NewPassword;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi đổi mật khẩu", error = ex.Message });
            }
        }

        // Helper methods (tương tự như ManageEmployeeAPIController)
        private async Task<string> SaveImageAsync(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new ArgumentException("File is null or empty");
                }

                if (string.IsNullOrEmpty(file.ContentType) || !file.ContentType.StartsWith("image/"))
                {
                    throw new ArgumentException($"Invalid file type: {file.ContentType}. Only image files are allowed.");
                }

                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (file.Length > maxFileSize)
                {
                    throw new ArgumentException($"File size ({file.Length} bytes) exceeds maximum allowed size ({maxFileSize} bytes)");
                }

                var fileExtension = Path.GetExtension(file.FileName);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    fileExtension = file.ContentType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/gif" => ".gif",
                        "image/webp" => ".webp",
                        _ => ".jpg"
                    };
                }

                const int maxTotalLength = 20;
                var extLength = fileExtension.Length;
                var baseNameMaxLength = Math.Max(1, maxTotalLength - extLength);

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
                
                var webRootPath = _environment.WebRootPath;
                if (string.IsNullOrEmpty(webRootPath))
                {
                    webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                }
                var uploadsFolder = Path.Combine(webRootPath, "imageEmployees");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                
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
                    BranchesId = employee.BranchesId,
                    RoleId = employee.RoleId,
                    Active = employee.Active,
                    PasswordLength = !string.IsNullOrEmpty(employee.Password) ? employee.Password.Length : (int?)null
                };

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
                        // Nếu không phải JSON, giữ nguyên Address
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
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
                    BranchesId = employee?.BranchesId,
                    RoleId = employee?.RoleId,
                    Active = employee?.Active,
                    PasswordLength = !string.IsNullOrEmpty(employee?.Password) ? employee.Password.Length : (int?)null
                };
            }
        }
    }
}
