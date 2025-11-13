using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Product
{
    public string ProductId { get; set; } = null!;

    public string? ProductName { get; set; }

    public string? ProductModel { get; set; }

    public int? WarrantyPeriod { get; set; }

    public decimal? OriginalSellingPrice { get; set; }

    public decimal? SellingPrice { get; set; }

    public string? Screen { get; set; }

    public string? Camera { get; set; }

    public string? Connect { get; set; }

    public decimal? Weight { get; set; }

    public string? Pin { get; set; }

    public string? BrandId { get; set; }

    public string? Avatar { get; set; }

    public bool? Active { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();

    public virtual ICollection<ProductConfiguration> ProductConfigurations { get; set; } = new List<ProductConfiguration>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual ICollection<ProductSerial> ProductSerials { get; set; } = new List<ProductSerial>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual ICollection<SaleInvoiceDetail> SaleInvoiceDetails { get; set; } = new List<SaleInvoiceDetail>();

    public virtual ICollection<StockImportDetail> StockImportDetails { get; set; } = new List<StockImportDetail>();
}
