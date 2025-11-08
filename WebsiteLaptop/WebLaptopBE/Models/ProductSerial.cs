using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class ProductSerial
{
    public string SerialId { get; set; } = null!;

    public string? ProductId { get; set; }

    public string? Specifications { get; set; }

    public string? StockExportDetailId { get; set; }

    public string? Status { get; set; }

    public DateTime? ImportDate { get; set; }

    public DateTime? ExportDate { get; set; }

    public DateTime? WarrantyStartDate { get; set; }

    public DateTime? WarrantyEndDate { get; set; }

    public string? Note { get; set; }

    public virtual Product? Product { get; set; }

    public virtual StockExportDetail? StockExportDetail { get; set; }

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
