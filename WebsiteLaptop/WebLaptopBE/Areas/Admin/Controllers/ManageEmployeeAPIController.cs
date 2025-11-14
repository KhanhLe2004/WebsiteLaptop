using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/employees")]
    [ApiController]
    public class ManageEmployeeAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _context;
        private readonly IWebHostEnvironment _environment;

        public ManageEmployeeAPIController(Testlaptop27Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

                var employeeDTOs = employees.Select(e => new EmployeeDTO
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeName = e.EmployeeName,
                    DateOfBirth = e.DateOfBirth,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    Email = e.Email,
                    Avatar = e.Avatar,
                    Username = e.Username,
                    BranchesId = e.BranchesId,
                    RoleId = e.RoleId,
                    Active = e.Active,
                    PasswordLength = !string.IsNullOrEmpty(e.Password) ? e.Password.Length : (int?)null
                }).ToList();

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

                var employeeDTO = new EmployeeDTO
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

                // Tạo mã nhân viên mới
                string newEmployeeId = await GenerateEmployeeIdAsync();

                // Xử lý upload ảnh
                string? avatarFileName = null;
                if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
                {
                    avatarFileName = await SaveImageAsync(dto.AvatarFile);
                }

                // Tạo nhân viên mới
                var employee = new Employee
                {
                    EmployeeId = newEmployeeId,
                    EmployeeName = dto.EmployeeName,
                    DateOfBirth = dto.DateOfBirth,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    Email = dto.Email,
                    Username = dto.Username,
                    Password = dto.Password, // Lưu mật khẩu dạng plain text (nên hash trong production)
                    BranchesId = dto.BranchesId,
                    RoleId = dto.RoleId,
                    Avatar = avatarFileName,
                    Active = true // Mặc định active = true
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Reload employee để lấy thông tin đầy đủ
                await _context.Entry(employee).ReloadAsync();

                var result = new EmployeeDTO
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

                return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo nhân viên", error = ex.Message });
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

                // Cập nhật thông tin
                employee.EmployeeName = dto.EmployeeName;
                employee.DateOfBirth = dto.DateOfBirth;
                employee.PhoneNumber = dto.PhoneNumber;
                employee.Address = dto.Address;
                employee.Email = dto.Email;
                employee.Username = dto.Username;
                employee.BranchesId = dto.BranchesId;
                employee.RoleId = dto.RoleId;
                
                // Chỉ cập nhật password nếu có giá trị mới
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    employee.Password = dto.Password; // Lưu mật khẩu dạng plain text (nên hash trong production)
                }

                await _context.SaveChangesAsync();

                // Reload employee để lấy thông tin đầy đủ
                await _context.Entry(employee).ReloadAsync();
                
                var result = new EmployeeDTO
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
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật nhân viên", error = ex.Message });
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

                return Ok(new { message = "Khôi phục nhân viên thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring employee: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi khôi phục nhân viên", error = ex.Message });
            }
        }

        // Helper method để tạo mã nhân viên mới
        private async Task<string> GenerateEmployeeIdAsync()
        {
            // Lấy mã nhân viên lớn nhất hiện có
            var lastEmployee = await _context.Employees
                .OrderByDescending(e => e.EmployeeId)
                .FirstOrDefaultAsync();

            if (lastEmployee == null)
            {
                return "NV001";
            }

            // Tách số từ mã nhân viên (ví dụ: NV001 -> 001)
            string lastId = lastEmployee.EmployeeId;
            if (lastId.StartsWith("NV") && lastId.Length > 2)
            {
                string numberPart = lastId.Substring(2);
                if (int.TryParse(numberPart, out int number))
                {
                    number++;
                    return $"NV{number:D3}";
                }
            }

            // Nếu không parse được, tạo mã mới dựa trên số lượng
            int count = await _context.Employees.CountAsync();
            return $"NV{(count + 1):D3}";
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
    }
}
