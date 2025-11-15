using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị nhân viên
    public class EmployeeDTO
    {
        public string EmployeeId { get; set; } = null!;
        public string? EmployeeName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; }
        public string? Username { get; set; }
        public string? BranchesId { get; set; }
        public string? RoleId { get; set; }
        public bool? Active { get; set; }
        public int? PasswordLength { get; set; } // Độ dài mật khẩu (không trả về mật khẩu thực)
    }

    // DTO cho tạo mới nhân viên
    public class EmployeeCreateDTO
    {
        // EmployeeId có thể được gửi từ frontend (đã được generate tự động) hoặc để trống để tự động tạo
        public string? EmployeeId { get; set; }

        [Required(ErrorMessage = "Tên nhân viên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên nhân viên không được quá 100 ký tự")]
        public string EmployeeName { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        public string? Address { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        public string? Email { get; set; }

        [StringLength(50, ErrorMessage = "Tên đăng nhập không được quá 50 ký tự")]
        public string? Username { get; set; }

        [StringLength(100, ErrorMessage = "Mật khẩu không được quá 100 ký tự")]
        public string? Password { get; set; }

        public string? BranchesId { get; set; }

        public string? RoleId { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    // DTO cho cập nhật nhân viên
    public class EmployeeUpdateDTO
    {
        [Required(ErrorMessage = "Tên nhân viên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên nhân viên không được quá 100 ký tự")]
        public string EmployeeName { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        public string? Address { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        public string? Email { get; set; }

        [StringLength(50, ErrorMessage = "Tên đăng nhập không được quá 50 ký tự")]
        public string? Username { get; set; }

        [StringLength(100, ErrorMessage = "Mật khẩu không được quá 100 ký tự")]
        public string? Password { get; set; }

        public string? BranchesId { get; set; }

        public string? RoleId { get; set; }

        public IFormFile? AvatarFile { get; set; }

        // Flag để xóa avatar (true nếu muốn xóa avatar hiện có)
        public bool? AvatarToDelete { get; set; }
    }

    // DTO cho chi nhánh (dùng cho combobox)
    public class BranchDTO
    {
        public string BranchesId { get; set; } = null!;
        public string? BranchesName { get; set; }
    }

    // DTO cho vai trò (dùng cho combobox)
    public class RoleDTO
    {
        public string RoleId { get; set; } = null!;
        public string? RoleName { get; set; }
    }
}

