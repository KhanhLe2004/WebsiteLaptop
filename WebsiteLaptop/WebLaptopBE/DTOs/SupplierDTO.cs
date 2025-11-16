using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị nhà cung cấp
    public class SupplierDTO
    {
        public string SupplierId { get; set; } = null!;
        public string? SupplierName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public bool? Active { get; set; }
        public int StockImportCount { get; set; }
    }

    // DTO cho tạo mới nhà cung cấp
    public class SupplierCreateDTO
    {
        public string? SupplierId { get; set; }
        
        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [StringLength(200, ErrorMessage = "Tên nhà cung cấp không được quá 200 ký tự")]
        public string? SupplierName { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
    }

    // DTO cho cập nhật nhà cung cấp
    public class SupplierUpdateDTO
    {
        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [StringLength(200, ErrorMessage = "Tên nhà cung cấp không được quá 200 ký tự")]
        public string? SupplierName { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "Email không được quá 100 ký tự")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
    }
}

