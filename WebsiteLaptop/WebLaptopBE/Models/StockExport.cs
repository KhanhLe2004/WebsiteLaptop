using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class StockExport
{
    public string StockExportId { get; set; } = null!;

    public string? EmployeeId { get; set; }

    public string? SaleInvoiceId { get; set; }

    public string? Status { get; set; }

    public DateTime? Time { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual SaleInvoice? SaleInvoice { get; set; }

    public virtual ICollection<StockExportDetail> StockExportDetails { get; set; } = new List<StockExportDetail>();
}
