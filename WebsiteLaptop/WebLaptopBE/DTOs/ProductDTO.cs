using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho ProductConfiguration
    public class ProductConfigurationDTO
    {
        public string ConfigurationId { get; set; } = null!;
        public string? Specifications { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public string? ProductId { get; set; }
    }

    // DTO cho ProductImage
    public class ProductImageDTO
    {
        public string ImageId { get; set; } = null!;
        public string? ProductId { get; set; }
        public string? ImageUrl { get; set; } // Đường dẫn ảnh (sử dụng ImageId làm tên file)
    }

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
        public List<ProductConfigurationDTO> Configurations { get; set; } = new List<ProductConfigurationDTO>();
        public List<ProductImageDTO> Images { get; set; } = new List<ProductImageDTO>();
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

        // Danh sách cấu hình sản phẩm (gửi dưới dạng JSON string)
        public string? ConfigurationsJson { get; set; }

        // Danh sách ảnh sản phẩm (sẽ được xử lý từ form files)
        // Note: Sẽ lấy từ Request.Form.Files với name "ImageFiles"
    }

    // DTO cho tạo ProductConfiguration
    public class ProductConfigurationCreateDTO
    {
        public string? Specifications { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
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

        // Danh sách cấu hình sản phẩm (gửi dưới dạng JSON string)
        public string? ConfigurationsJson { get; set; }

        // Danh sách ảnh cần xóa (gửi dưới dạng JSON string - array of ImageId)
        public string? ImagesToDeleteJson { get; set; }

        // Flag để xóa avatar (true nếu muốn xóa avatar hiện có)
        public bool? AvatarToDelete { get; set; }

        // Danh sách ảnh sản phẩm mới (sẽ được xử lý từ form files)
        // Note: Sẽ lấy từ Request.Form.Files với name "ImageFiles"
    }

    // DTO cho cập nhật ProductConfiguration
    public class ProductConfigurationUpdateDTO
    {
        public string? ConfigurationId { get; set; } // Nếu có thì update, nếu null thì tạo mới
        public string? Specifications { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
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

