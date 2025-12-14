using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class StockImportDetail
{
    public string StockImportDetailId { get; set; } = null!;

    public string? StockImportId { get; set; }

    public string? ProductId { get; set; }

    public string? Specifications { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public virtual Product? Product { get; set; }

    public virtual StockImport? StockImport { get; set; }
}
