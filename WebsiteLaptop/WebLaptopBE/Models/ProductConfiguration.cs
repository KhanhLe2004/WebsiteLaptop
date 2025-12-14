using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class ProductConfiguration
{
    public string ConfigurationId { get; set; } = null!;

    public string? Cpu { get; set; }

    public string? Ram { get; set; }

    public string? Rom { get; set; }

    public string? Card { get; set; }

    public decimal? Price { get; set; }

    public string? ProductId { get; set; }

    public int? Quantity { get; set; }

    public virtual Product? Product { get; set; }
}
