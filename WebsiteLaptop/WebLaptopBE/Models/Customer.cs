using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Customer
{
    public string CustomerId { get; set; } = null!;

    public string? CustomerName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Avatar { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool? Active { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<SaleInvoice> SaleInvoices { get; set; } = new List<SaleInvoice>();

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
