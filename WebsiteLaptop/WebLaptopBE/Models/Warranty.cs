using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Warranty
{
    public string WarrantyId { get; set; } = null!;

    public string? CustomerId { get; set; }

    public string? SerialId { get; set; }

    public string? EmployeeId { get; set; }

    public string? Type { get; set; }

    public string? ContentDetail { get; set; }

    public string? Status { get; set; }

    public decimal? TotalAmount { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual ProductSerial? Serial { get; set; }
}
