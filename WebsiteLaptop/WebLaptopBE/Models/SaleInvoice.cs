using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class SaleInvoice
{
    public string SaleInvoiceId { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public decimal? TotalAmount { get; set; }

    public DateTime? TimeCreate { get; set; }

    public string? Status { get; set; }

    public decimal? DeliveryFee { get; set; }

    public decimal? Discount { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? EmployeeId { get; set; }

    public string? CustomerId { get; set; }

    public string? Phone { get; set; }

    public string? EmployeeShip { get; set; }

    public DateTime? TimeShip { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<SaleInvoiceDetail> SaleInvoiceDetails { get; set; } = new List<SaleInvoiceDetail>();

    public virtual ICollection<StockExport> StockExports { get; set; } = new List<StockExport>();
}
