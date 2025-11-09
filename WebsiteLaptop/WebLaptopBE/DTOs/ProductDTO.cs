using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị sản phẩm
    public class ProductDTO
    {
        public string ProductId { get; set; } = null!;
        public string? ProductName { get; set; }
        public string? ProductModel { get; set; }
        public int? WarrantyPeriod { get; set; }
        public decimal? OriginalSellingPrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public string? Screen { get; set; }
        public string? Camera { get; set; }
        public string? Connect { get; set; }
        public decimal? Weight { get; set; }
        public string? Pin { get; set; }
        public string? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? Avatar { get; set; }
    }

    // DTO cho tạo mới sản phẩm
    public class ProductCreateDTO
    {
        [Required(ErrorMessage = "Mã sản phẩm là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã sản phẩm không được quá 20 ký tự")]
        public string ProductId { get; set; } = null!;

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được quá 100 ký tự")]
        public string ProductName { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Model không được quá 100 ký tự")]
        public string? ProductModel { get; set; }

        [Range(0, 120, ErrorMessage = "Thời gian bảo hành phải từ 0 đến 120 tháng")]
        public int? WarrantyPeriod { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải lớn hơn hoặc bằng 0")]
        public decimal? OriginalSellingPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal? SellingPrice { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin màn hình không được quá 50 ký tự")]
        public string? Screen { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin camera không được quá 50 ký tự")]
        public string? Camera { get; set; }

        [StringLength(200, ErrorMessage = "Thông tin kết nối không được quá 200 ký tự")]
        public string? Connect { get; set; }

        [Range(0, 10, ErrorMessage = "Trọng lượng phải từ 0 đến 10 kg")]
        public decimal? Weight { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin pin không được quá 50 ký tự")]
        public string? Pin { get; set; }

        [StringLength(20, ErrorMessage = "Mã thương hiệu không được quá 20 ký tự")]
        public string? BrandId { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    // DTO cho cập nhật sản phẩm
    public class ProductUpdateDTO
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được quá 100 ký tự")]
        public string ProductName { get; set; } = null!;

        [StringLength(100, ErrorMessage = "Model không được quá 100 ký tự")]
        public string? ProductModel { get; set; }

        [Range(0, 120, ErrorMessage = "Thời gian bảo hành phải từ 0 đến 120 tháng")]
        public int? WarrantyPeriod { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải lớn hơn hoặc bằng 0")]
        public decimal? OriginalSellingPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        public decimal? SellingPrice { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin màn hình không được quá 50 ký tự")]
        public string? Screen { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin camera không được quá 50 ký tự")]
        public string? Camera { get; set; }

        [StringLength(200, ErrorMessage = "Thông tin kết nối không được quá 200 ký tự")]
        public string? Connect { get; set; }

        [Range(0, 10, ErrorMessage = "Trọng lượng phải từ 0 đến 10 kg")]
        public decimal? Weight { get; set; }

        [StringLength(50, ErrorMessage = "Thông tin pin không được quá 50 ký tự")]
        public string? Pin { get; set; }

        [StringLength(20, ErrorMessage = "Mã thương hiệu không được quá 20 ký tự")]
        public string? BrandId { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    // DTO cho phân trang
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}

