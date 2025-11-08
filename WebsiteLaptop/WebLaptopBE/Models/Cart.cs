using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Cart
{
    public string CartId { get; set; } = null!;

    public decimal? TotalAmount { get; set; }

    public string? Username { get; set; }

    public virtual ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();

    public virtual Account? UsernameNavigation { get; set; }
}
