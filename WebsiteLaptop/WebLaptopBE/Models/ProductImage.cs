using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class ProductImage
{
    public string ImageId { get; set; } = null!;

    public string? ProductId { get; set; }

    public virtual Product? Product { get; set; }
}
