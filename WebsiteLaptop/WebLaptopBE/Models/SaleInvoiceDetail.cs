using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class SaleInvoiceDetail
{
    public string SaleInvoiceDetailId { get; set; } = null!;

    public string? SaleInvoiceId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public string? ProductId { get; set; }

    public string? Specifications { get; set; }

    public virtual Product? Product { get; set; }

    public virtual SaleInvoice? SaleInvoice { get; set; }
}
