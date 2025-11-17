using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị khách hàng
    public class CustomerDTO
    {
        public string CustomerId { get; set; } = null!;
        public string? CustomerName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; }
        public string? Username { get; set; }
        public bool? Active { get; set; }
        public int? PasswordLength { get; set; } // Độ dài mật khẩu (không trả về mật khẩu thực)
        public string? ProvinceCode { get; set; }
        public string? CommuneCode { get; set; }
        public string? AddressDetail { get; set; }
    }

    // DTO cho cập nhật khách hàng
    public class CustomerUpdateDTO
    {
        [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên khách hàng không được quá 100 ký tự")]
        public string CustomerName { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được quá 200 ký tự")]
        public string? Address { get; set; }

        public string? ProvinceCode { get; set; }

        public string? CommuneCode { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ cụ thể không được quá 200 ký tự")]
        public string? AddressDetail { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        public string? Email { get; set; }

        [StringLength(50, ErrorMessage = "Tên đăng nhập không được quá 50 ký tự")]
        public string? Username { get; set; }

        [StringLength(100, ErrorMessage = "Mật khẩu không được quá 100 ký tự")]
        public string? Password { get; set; }

        public IFormFile? AvatarFile { get; set; }

        // Flag để xóa avatar (true nếu muốn xóa avatar hiện có)
        public bool? AvatarToDelete { get; set; }
    }
}

