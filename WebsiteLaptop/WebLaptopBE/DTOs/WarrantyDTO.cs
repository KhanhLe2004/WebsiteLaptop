using System.ComponentModel.DataAnnotations;

namespace WebLaptopBE.DTOs
{
    // DTO cho hiển thị bảo hành
    public class WarrantyDTO
    {
        public string WarrantyId { get; set; } = null!;
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? SerialId { get; set; }
        public string? ProductName { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? Type { get; set; }
        public string? ContentDetail { get; set; }
        public string? Status { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    // DTO cho tạo mới bảo hành
    public class WarrantyCreateDTO
    {
        public string? WarrantyId { get; set; }
        public string? CustomerId { get; set; }
        public string? SerialId { get; set; }
        public string? EmployeeId { get; set; }
        
        [StringLength(100, ErrorMessage = "Loại bảo hành không được quá 100 ký tự")]
        public string? Type { get; set; }
        
        [StringLength(500, ErrorMessage = "Nội dung chi tiết không được quá 500 ký tự")]
        public string? ContentDetail { get; set; }
        
        [StringLength(50, ErrorMessage = "Trạng thái không được quá 50 ký tự")]
        public string? Status { get; set; }
        
        public decimal? TotalAmount { get; set; }
    }

    // DTO cho cập nhật bảo hành
    public class WarrantyUpdateDTO
    {
        public string? CustomerId { get; set; }
        public string? SerialId { get; set; }
        public string? EmployeeId { get; set; }
        
        [StringLength(100, ErrorMessage = "Loại bảo hành không được quá 100 ký tự")]
        public string? Type { get; set; }
        
        [StringLength(500, ErrorMessage = "Nội dung chi tiết không được quá 500 ký tự")]
        public string? ContentDetail { get; set; }
        
        [StringLength(50, ErrorMessage = "Trạng thái không được quá 50 ký tự")]
        public string? Status { get; set; }
        
        public decimal? TotalAmount { get; set; }
    }

    // DTO cho khách hàng (select)
    public class CustomerSelectDTO
    {
        public string CustomerId { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    // DTO cho nhân viên (select)
    public class EmployeeSelectDTO
    {
        public string EmployeeId { get; set; } = null!;
        public string? EmployeeName { get; set; }
    }

    // DTO cho serial (select)
    public class SerialSelectDTO
    {
        public string SerialId { get; set; } = null!;
        public string? ProductName { get; set; }
        public string DisplayName { get; set; } = null!;
    }

    // DTO cho khách hàng kèm danh sách serial
    public class CustomerWithSerialsDTO
    {
        public string CustomerId { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? PhoneNumber { get; set; }
        public List<SerialSelectDTO> Serials { get; set; } = new List<SerialSelectDTO>();
    }
}

