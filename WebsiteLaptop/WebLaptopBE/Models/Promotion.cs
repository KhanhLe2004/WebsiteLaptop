using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Promotion
{
    public string PromotionId { get; set; } = null!;

    public string? ProductId { get; set; }

    public string? Type { get; set; }

    public string? ContentDetail { get; set; }

    public virtual Product? Product { get; set; }
}
