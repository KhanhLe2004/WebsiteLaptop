using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class ProductReview
{
    public string? ContentDetail { get; set; }

    public int? Rate { get; set; }

    public string Username { get; set; } = null!;

    public DateTime? Time { get; set; }

    public string ProductId { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual Account UsernameNavigation { get; set; } = null!;
}
