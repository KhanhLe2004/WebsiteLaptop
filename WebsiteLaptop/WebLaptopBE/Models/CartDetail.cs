using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class CartDetail
{
    public string CartDetailId { get; set; } = null!;

    public int? Quantity { get; set; }

    public string? Specifications { get; set; }

    public string? CartId { get; set; }

    public string? ProductId { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual Product? Product { get; set; }
}
