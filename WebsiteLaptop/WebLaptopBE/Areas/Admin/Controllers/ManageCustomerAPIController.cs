using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/admin/customers")]
    [ApiController]
    public class ManageCustomerAPIController : ControllerBase
    {
        private readonly Testlaptop27Context _context;
        private readonly IWebHostEnvironment _environment;

        public ManageCustomerAPIController(Testlaptop27Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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

                var customerDTOs = customers.Select(c => new CustomerDTO
                {
                    CustomerId = c.CustomerId,
                    CustomerName = c.CustomerName,
                    DateOfBirth = c.DateOfBirth,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address,
                    Email = c.Email,
                    Avatar = c.Avatar,
                    Username = c.Username,
                    Active = c.Active,
                    PasswordLength = !string.IsNullOrEmpty(c.Password) ? c.Password.Length : (int?)null
                }).ToList();

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

                var customerDTO = new CustomerDTO
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
                customer.Address = dto.Address;
                customer.Email = dto.Email;
                customer.Username = dto.Username;
                
                // Chỉ cập nhật password nếu có giá trị mới
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    customer.Password = dto.Password; // Lưu mật khẩu dạng plain text (nên hash trong production)
                }

                await _context.SaveChangesAsync();

                // Reload customer để lấy thông tin đầy đủ
                await _context.Entry(customer).ReloadAsync();
                
                var result = new CustomerDTO
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
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật khách hàng", error = ex.Message });
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
    }
}
