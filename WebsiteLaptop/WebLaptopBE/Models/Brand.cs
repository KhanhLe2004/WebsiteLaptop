using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Brand
{
    public string BrandId { get; set; } = null!;

    public string? BrandName { get; set; }

    public bool? Active { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
