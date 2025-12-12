using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị hóa đơn bán
    public class SaleInvoiceDTO
    {
        public string SaleInvoiceId { get; set; } = null!;
        public string? PaymentMethod { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? TimeCreate { get; set; }
        public string? Status { get; set; }
        public decimal? DeliveryFee { get; set; }
        public string? DeliveryAddress { get; set; }
        public decimal? Discount { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? EmployeeShip { get; set; }
        public string? EmployeeShipName { get; set; }
        public DateTime? TimeShip { get; set; }
        public List<SaleInvoiceDetailDTO>? Details { get; set; }
    }

    // DTO cho chi tiết hóa đơn bán
    public class SaleInvoiceDetailDTO
    {
        public string SaleInvoiceDetailId { get; set; } = null!;
        public string? SaleInvoiceId { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductModel { get; set; }
        public string? Specifications { get; set; }
    }

    // DTO cho tạo mới hóa đơn bán
    public class SaleInvoiceCreateDTO
    {
        public string? SaleInvoiceId { get; set; }
        
        [StringLength(50, ErrorMessage = "Phương thức thanh toán không được quá 50 ký tự")]
        public string? PaymentMethod { get; set; }
        
        public decimal? TotalAmount { get; set; }
        
        public string? Status { get; set; }
        
        public decimal? DeliveryFee { get; set; }
        
        [StringLength(200, ErrorMessage = "Địa chỉ giao hàng không được quá 200 ký tự")]
        public string? DeliveryAddress { get; set; }
        
        public string? EmployeeId { get; set; }
        
        public string? CustomerId { get; set; }
        
        public List<SaleInvoiceDetailCreateDTO>? Details { get; set; }
    }

    // DTO cho tạo mới chi tiết hóa đơn
    public class SaleInvoiceDetailCreateDTO
    {
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? ProductId { get; set; }
        [StringLength(100, ErrorMessage = "Thông số kỹ thuật không được quá 100 ký tự")]
        public string? Specifications { get; set; }
    }

    // DTO cho cập nhật hóa đơn bán
    public class SaleInvoiceUpdateDTO
    {
        [StringLength(50, ErrorMessage = "Phương thức thanh toán không được quá 50 ký tự")]
        public string? PaymentMethod { get; set; }
        
        public decimal? TotalAmount { get; set; }
        
        public string? Status { get; set; }
        
        public decimal? DeliveryFee { get; set; }
        
        [StringLength(200, ErrorMessage = "Địa chỉ giao hàng không được quá 200 ký tự")]
        public string? DeliveryAddress { get; set; }
        
        public string? EmployeeId { get; set; }
        
        public string? CustomerId { get; set; }
        
        public List<SaleInvoiceDetailCreateDTO>? Details { get; set; }
    }
}

