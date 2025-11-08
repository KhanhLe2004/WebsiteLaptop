using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Supplier
{
    public string SupplierId { get; set; } = null!;

    public string? SupplierName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<StockImport> StockImports { get; set; } = new List<StockImport>();
}
