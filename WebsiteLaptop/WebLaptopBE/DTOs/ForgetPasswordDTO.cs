using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho request quên mật khẩu
    public class ForgetPasswordRequestDTO
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;
    }

    // DTO cho response quên mật khẩu
    public class ForgetPasswordResponseDTO
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}

