using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class StockExportDetail
{
    public string StockExportDetailId { get; set; } = null!;

    public string? StockExportId { get; set; }

    public string? ProductId { get; set; }

    public string? Specifications { get; set; }

    public int? Quantity { get; set; }

    public virtual ICollection<ProductSerial> ProductSerials { get; set; } = new List<ProductSerial>();

    public virtual StockExport? StockExport { get; set; }
}
