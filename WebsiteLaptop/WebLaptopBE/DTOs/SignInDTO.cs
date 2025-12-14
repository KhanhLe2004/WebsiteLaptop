using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho request đăng nhập
    public class SignInRequestDTO
    {
        [Required(ErrorMessage = "Email hoặc tên đăng nhập là bắt buộc")]
        public string UsernameOrEmail { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = null!;

        public bool RememberMe { get; set; } = false;
    }

    // DTO cho response đăng nhập
    public class SignInResponseDTO
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public EmployeeSignInDTO? Employee { get; set; }
    }

    // DTO cho thông tin nhân viên sau khi đăng nhập
    public class EmployeeSignInDTO
    {
        public string EmployeeId { get; set; } = null!;
        public string? EmployeeName { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Avatar { get; set; }
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public bool? Active { get; set; }
    }
}

