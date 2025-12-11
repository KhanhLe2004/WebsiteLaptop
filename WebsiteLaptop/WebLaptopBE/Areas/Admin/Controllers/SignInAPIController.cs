using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;
using WebLaptopBE.Services;

namespace WebLaptopBE.Areas.Admin.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignInAPIController : ControllerBase
    {
        private readonly Testlaptop36Context _context;
        private readonly HistoryService _historyService;

        public SignInAPIController(Testlaptop36Context context, HistoryService historyService)
        {
            _context = context;
            _historyService = historyService;
        }

        // POST: api/SignInAPI
        [HttpPost]
        public async Task<ActionResult<SignInResponseDTO>> SignIn([FromBody] SignInRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new SignInResponseDTO
                    {
                        Success = false,
                        Message = "Dữ liệu không hợp lệ"
                    });
                }

                // Tìm nhân viên theo username hoặc email
                var employee = await _context.Employees
                    .Include(e => e.Role)
                    .FirstOrDefaultAsync(e => 
                        (e.Username != null && e.Username == request.UsernameOrEmail) ||
                        (e.Email != null && e.Email == request.UsernameOrEmail));

                // Kiểm tra nhân viên có tồn tại không
                if (employee == null)
                {
                    return Unauthorized(new SignInResponseDTO
                    {
                        Success = false,
                        Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                    });
                }

                // Kiểm tra mật khẩu (so sánh plain text - trong production nên hash)
                if (employee.Password != request.Password)
                {
                    return Unauthorized(new SignInResponseDTO
                    {
                        Success = false,
                        Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                    });
                }

                // Kiểm tra tài khoản có đang active không
                if (employee.Active != true)
                {
                    return Unauthorized(new SignInResponseDTO
                    {
                        Success = false,
                        Message = "Tài khoản của bạn đã bị vô hiệu hóa"
                    });
                }

                // Tạo response với thông tin nhân viên
                var response = new SignInResponseDTO
                {
                    Success = true,
                    Message = "Đăng nhập thành công",
                    Employee = new EmployeeSignInDTO
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = employee.EmployeeName,
                        Email = employee.Email,
                        Username = employee.Username,
                        Avatar = employee.Avatar,
                        RoleId = employee.RoleId,
                        RoleName = employee.Role?.RoleName,
                        Active = employee.Active
                    }
                };

                // Log history cho đăng nhập thành công
                await _historyService.LogHistoryAsync(employee.EmployeeId, $"Đăng nhập hệ thống");

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new SignInResponseDTO
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi khi đăng nhập: " + ex.Message
                });
            }
        }
    }
}
