using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị khuyến mại
    public class PromotionDTO
    {
        public string PromotionId { get; set; } = null!;
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductModel { get; set; }
        public string? Type { get; set; }
        public string? ContentDetail { get; set; }
    }

    // DTO cho tạo mới khuyến mại
    public class PromotionCreateDTO
    {
        public string? PromotionId { get; set; }
        
        [Required(ErrorMessage = "Sản phẩm không được để trống")]
        public string? ProductId { get; set; }
        
        [Required(ErrorMessage = "Loại khuyến mại không được để trống")]
        [StringLength(50, ErrorMessage = "Loại khuyến mại không được quá 50 ký tự")]
        public string? Type { get; set; }
        
        [StringLength(500, ErrorMessage = "Nội dung chi tiết không được quá 500 ký tự")]
        public string? ContentDetail { get; set; }
    }

    // DTO cho cập nhật khuyến mại
    public class PromotionUpdateDTO
    {
        [Required(ErrorMessage = "Sản phẩm không được để trống")]
        public string? ProductId { get; set; }
        
        [Required(ErrorMessage = "Loại khuyến mại không được để trống")]
        [StringLength(50, ErrorMessage = "Loại khuyến mại không được quá 50 ký tự")]
        public string? Type { get; set; }
        
        [StringLength(500, ErrorMessage = "Nội dung chi tiết không được quá 500 ký tự")]
        public string? ContentDetail { get; set; }
    }

    // DTO cho tạo khuyến mại hàng loạt cho nhiều sản phẩm
    public class PromotionBatchCreateDTO
    {
        [Required(ErrorMessage = "Danh sách sản phẩm không được để trống")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất một sản phẩm")]
        public List<string> ProductIds { get; set; } = new List<string>();
        
        [Required(ErrorMessage = "Loại khuyến mại không được để trống")]
        [StringLength(50, ErrorMessage = "Loại khuyến mại không được quá 50 ký tự")]
        public string? Type { get; set; }
        
        [StringLength(500, ErrorMessage = "Nội dung chi tiết không được quá 500 ký tự")]
        public string? ContentDetail { get; set; }
    }
}

