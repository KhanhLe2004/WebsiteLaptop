using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class StockImport
{
    public string StockImportId { get; set; } = null!;

    public string? SupplierId { get; set; }

    public string? EmployeeId { get; set; }

    public DateTime? Time { get; set; }

    public decimal? TotalAmount { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ICollection<StockImportDetail> StockImportDetails { get; set; } = new List<StockImportDetail>();

    public virtual Supplier? Supplier { get; set; }
}
