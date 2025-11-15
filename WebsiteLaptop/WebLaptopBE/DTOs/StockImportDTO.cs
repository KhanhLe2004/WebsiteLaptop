using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị phiếu nhập hàng
    public class StockImportDTO
    {
        public string StockImportId { get; set; } = null!;
        public string? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime? Time { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<StockImportDetailDTO>? Details { get; set; }
    }

    // DTO cho chi tiết phiếu nhập hàng
    public class StockImportDetailDTO
    {
        public string StockImportDetailId { get; set; } = null!;
        public string? StockImportId { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Specifications { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }

    // DTO cho tạo mới phiếu nhập hàng
    public class StockImportCreateDTO
    {
        public string? StockImportId { get; set; }
        public string? SupplierId { get; set; }
        public string? EmployeeId { get; set; }
        public DateTime? Time { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<StockImportDetailCreateDTO>? Details { get; set; }
    }

    // DTO cho tạo mới chi tiết phiếu nhập hàng
    public class StockImportDetailCreateDTO
    {
        public string? ProductId { get; set; }
        [StringLength(100, ErrorMessage = "Thông số kỹ thuật không được quá 100 ký tự")]
        public string? Specifications { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }

    // DTO cho cập nhật phiếu nhập hàng
    public class StockImportUpdateDTO
    {
        public string? SupplierId { get; set; }
        public string? EmployeeId { get; set; }
        public DateTime? Time { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<StockImportDetailCreateDTO>? Details { get; set; }
    }
}

