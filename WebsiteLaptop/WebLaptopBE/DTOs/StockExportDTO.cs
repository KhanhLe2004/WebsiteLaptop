using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị phiếu xuất hàng
    public class StockExportDTO
    {
        public string StockExportId { get; set; } = null!;
        public string? SaleInvoiceId { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime? Time { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? TotalQuantity { get; set; }
        public string? Status { get; set; }
        public List<StockExportDetailDTO>? Details { get; set; }
    }

    // DTO cho chi tiết phiếu xuất hàng
    public class StockExportDetailDTO
    {
        public string StockExportDetailId { get; set; } = null!;
        public string? StockExportId { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductModel { get; set; }
        public string? Specifications { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }

    // DTO cho tạo mới phiếu xuất hàng
    public class StockExportCreateDTO
    {
        public string? StockExportId { get; set; }
        public string? SaleInvoiceId { get; set; }
        public string? EmployeeId { get; set; }
        public DateTime? Time { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; }
        public List<StockExportDetailCreateDTO>? Details { get; set; }
    }

    // DTO cho tạo mới chi tiết phiếu xuất hàng
    public class StockExportDetailCreateDTO
    {
        public string? ProductId { get; set; }
        [StringLength(100, ErrorMessage = "Thông số kỹ thuật không được quá 100 ký tự")]
        public string? Specifications { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }

    // DTO cho cập nhật phiếu xuất hàng
    public class StockExportUpdateDTO
    {
        public string? SaleInvoiceId { get; set; }
        public string? EmployeeId { get; set; }
        public DateTime? Time { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; }
        public List<StockExportDetailCreateDTO>? Details { get; set; }
    }

    // DTO cho hóa đơn bán (select)
    public class SaleInvoiceSelectDTO
    {
        public string SaleInvoiceId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }

    // DTO cho sản phẩm tồn kho thấp
    public class LowStockProductDTO
    {
        public string ProductId { get; set; } = null!;
        public string? ProductName { get; set; }
        public string? ProductModel { get; set; }
        public string ConfigurationId { get; set; } = null!;
        public string? Cpu { get; set; }
        public string? Ram { get; set; }
        public string? Rom { get; set; }
        public string? Card { get; set; }
        public int Quantity { get; set; }
        public decimal? Price { get; set; }
    }
}
