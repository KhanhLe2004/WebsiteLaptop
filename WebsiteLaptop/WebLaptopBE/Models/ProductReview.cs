using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class ProductReview
{
    public string ProductReviewId { get; set; } = null!;

    public string? ContentDetail { get; set; }

    public int? Rate { get; set; }

    public string? CustomerId { get; set; }

    public DateTime? Time { get; set; }

    public string? ProductId { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Product? Product { get; set; }
}
