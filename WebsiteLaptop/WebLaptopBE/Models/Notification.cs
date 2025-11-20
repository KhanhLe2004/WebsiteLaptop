using System;

namespace WebLaptopBE.Models;

public partial class Notification
{
    public string NotificationId { get; set; } = null!;

    public string? SaleInvoiceId { get; set; }

    public string? StockExportId { get; set; }

    public string? Message { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Type { get; set; }

    public virtual SaleInvoice? SaleInvoice { get; set; }

    public virtual StockExport? StockExport { get; set; }
}

