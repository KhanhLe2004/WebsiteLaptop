using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho request đổi mật khẩu
    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Mã nhân viên là bắt buộc")]
        public string EmployeeId { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; } = null!;
    }
}

