using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị hãng
    public class BrandDTO
    {
        public string BrandId { get; set; } = null!;
        public string? BrandName { get; set; }
        public int ProductCount { get; set; }
    }

    // DTO cho tạo mới hãng
    public class BrandCreateDTO
    {
        public string? BrandId { get; set; }
        
        [Required(ErrorMessage = "Tên hãng không được để trống")]
        [StringLength(100, ErrorMessage = "Tên hãng không được quá 100 ký tự")]
        public string? BrandName { get; set; }
    }

    // DTO cho cập nhật hãng
    public class BrandUpdateDTO
    {
        [Required(ErrorMessage = "Tên hãng không được để trống")]
        [StringLength(100, ErrorMessage = "Tên hãng không được quá 100 ký tự")]
        public string? BrandName { get; set; }
    }
}

