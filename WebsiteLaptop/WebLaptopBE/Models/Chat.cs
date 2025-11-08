using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Chat
{
    public string ChatId { get; set; } = null!;

    public string? ContentDetail { get; set; }

    public DateTime? Time { get; set; }

    public string? Status { get; set; }

    public string? CustomerId { get; set; }

    public string? EmployeeId { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Employee? Employee { get; set; }
}
