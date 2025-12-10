using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Models;

namespace WebLaptopBE.Data;

public partial class Testlaptop36Context : DbContext
{
    public Testlaptop36Context()
    {
    }

    public Testlaptop36Context(DbContextOptions<Testlaptop36Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Brand> Brands { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartDetail> CartDetails { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<History> Histories { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductConfiguration> ProductConfigurations { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductReview> ProductReviews { get; set; }

    public virtual DbSet<ProductSerial> ProductSerials { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SaleInvoice> SaleInvoices { get; set; }

    public virtual DbSet<SaleInvoiceDetail> SaleInvoiceDetails { get; set; }

    public virtual DbSet<StockExport> StockExports { get; set; }

    public virtual DbSet<StockExportDetail> StockExportDetails { get; set; }

    public virtual DbSet<StockImport> StockImports { get; set; }

    public virtual DbSet<StockImportDetail> StockImportDetails { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Warranty> Warranties { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-GDN4V8P;Initial Catalog=testlaptop36;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.BranchesId);

            entity.Property(e => e.BranchesId)
                .HasMaxLength(20)
                .HasColumnName("branches_id");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.BranchesName)
                .HasMaxLength(100)
                .HasColumnName("branches_name");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
        });

        modelBuilder.Entity<Brand>(entity =>
        {
            entity.Property(e => e.BrandId)
                .HasMaxLength(20)
                .HasColumnName("brand_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.BrandName)
                .HasMaxLength(50)
                .HasColumnName("brand_name");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Cart");

            entity.Property(e => e.CartId)
                .HasMaxLength(20)
                .HasColumnName("cart_id");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .HasColumnName("customer_id");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Customer).WithMany(p => p.Carts)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Cart_Customer");
        });

        modelBuilder.Entity<CartDetail>(entity =>
        {
            entity.ToTable("CartDetail");

            entity.Property(e => e.CartDetailId)
                .HasMaxLength(20)
                .HasColumnName("cartDetail_id");
            entity.Property(e => e.CartId)
                .HasMaxLength(20)
                .HasColumnName("cart_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Specifications)
                .HasMaxLength(100)
                .HasColumnName("specifications");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartDetails)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartDetail_Cart");

            entity.HasOne(d => d.Product).WithMany(p => p.CartDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_CartDetail_Product");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("Chat");

            entity.Property(e => e.ChatId)
                .HasMaxLength(20)
                .HasColumnName("chat_id");
            entity.Property(e => e.ContentDetail).HasColumnName("content_detail");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .HasColumnName("customer_id");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .HasColumnName("employee_id");
            entity.Property(e => e.SenderType)
                .HasMaxLength(20)
                .HasColumnName("sender_type");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Time)
                .HasColumnType("datetime")
                .HasColumnName("time");

            entity.HasOne(d => d.Customer).WithMany(p => p.Chats)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Chat_Customer");

            entity.HasOne(d => d.Employee).WithMany(p => p.Chats)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Chat_Employee");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");

            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .HasColumnName("customer_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.Avatar)
                .HasMaxLength(300)
                .HasColumnName("avatar");
            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasColumnName("customer_name");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(20)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee");

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .HasColumnName("employee_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.Avatar)
                .HasMaxLength(100)
                .HasColumnName("avatar");
            entity.Property(e => e.BranchesId)
                .HasMaxLength(20)
                .HasColumnName("branches_id");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.EmployeeName)
                .HasMaxLength(50)
                .HasColumnName("employee_name");
            entity.Property(e => e.Password)
                .HasMaxLength(20)
                .HasColumnName("password");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.RoleId)
                .HasMaxLength(20)
                .HasColumnName("role_id");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasOne(d => d.Branches).WithMany(p => p.Employees)
                .HasForeignKey(d => d.BranchesId)
                .HasConstraintName("FK_Employee_Branches");

            entity.HasOne(d => d.Role).WithMany(p => p.Employees)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK_Employee_Role");
        });

        modelBuilder.Entity<History>(entity =>
        {
            entity.ToTable("History");

            entity.Property(e => e.HistoryId)
                .HasMaxLength(20)
                .HasColumnName("history_id");
            entity.Property(e => e.ActivityType)
                .HasMaxLength(200)
                .HasColumnName("activity_type");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .HasColumnName("employee_id");
            entity.Property(e => e.Time)
                .HasColumnType("datetime")
                .HasColumnName("time");

            entity.HasOne(d => d.Employee).WithMany(p => p.Histories)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_History_Employee");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");

            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Avatar)
                .HasMaxLength(100)
                .HasColumnName("avatar");
            entity.Property(e => e.BrandId)
                .HasMaxLength(20)
                .HasColumnName("brand_id");
            entity.Property(e => e.Camera)
                .HasMaxLength(50)
                .HasColumnName("camera");
            entity.Property(e => e.Connect)
                .HasMaxLength(200)
                .HasColumnName("connect");
            entity.Property(e => e.OriginalSellingPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("original_selling_price");
            entity.Property(e => e.Pin)
                .HasMaxLength(50)
                .HasColumnName("pin");
            entity.Property(e => e.ProductModel)
                .HasMaxLength(100)
                .HasColumnName("product_model");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .HasColumnName("product_name");
            entity.Property(e => e.Screen)
                .HasMaxLength(50)
                .HasColumnName("screen");
            entity.Property(e => e.SellingPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("selling_price");
            entity.Property(e => e.WarrantyPeriod).HasColumnName("warranty_period");
            entity.Property(e => e.Weight)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("weight");

            entity.HasOne(d => d.Brand).WithMany(p => p.Products)
                .HasForeignKey(d => d.BrandId)
                .HasConstraintName("FK_Product_Brands");
        });

        modelBuilder.Entity<ProductConfiguration>(entity =>
        {
            entity.HasKey(e => e.ConfigurationId);

            entity.ToTable("ProductConfiguration");

            entity.Property(e => e.ConfigurationId)
                .HasMaxLength(20)
                .HasColumnName("configuration_id");
            entity.Property(e => e.Card)
                .HasMaxLength(50)
                .HasColumnName("card");
            entity.Property(e => e.Cpu)
                .HasMaxLength(50)
                .HasColumnName("cpu");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Ram)
                .HasMaxLength(50)
                .HasColumnName("ram");
            entity.Property(e => e.Rom)
                .HasMaxLength(50)
                .HasColumnName("rom");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductConfigurations)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductConfiguration_Product");
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId);

            entity.ToTable("ProductImage");

            entity.Property(e => e.ImageId)
                .HasMaxLength(20)
                .HasColumnName("image_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductImage_Product");
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("ProductReview");

            entity.Property(e => e.ProductReviewId)
                .HasMaxLength(20)
                .HasColumnName("productReview_id");
            entity.Property(e => e.ContentDetail).HasColumnName("content_detail");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .HasColumnName("customer_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Rate).HasColumnName("rate");
            entity.Property(e => e.Time)
                .HasColumnType("datetime")
                .HasColumnName("time");

            entity.HasOne(d => d.Customer).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_ProductReview_Customer");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductReview_Product");
        });

        modelBuilder.Entity<ProductSerial>(entity =>
        {
            entity.HasKey(e => e.SerialId);

            entity.ToTable("ProductSerial");

            entity.Property(e => e.SerialId)
                .HasMaxLength(20)
                .HasColumnName("serial_id");
            entity.Property(e => e.ExportDate)
                .HasColumnType("datetime")
                .HasColumnName("export_date");
            entity.Property(e => e.ImportDate)
                .HasColumnType("datetime")
                .HasColumnName("import_date");
            entity.Property(e => e.Note)
                .HasMaxLength(200)
                .HasColumnName("note");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Specifications)
                .HasMaxLength(100)
                .HasColumnName("specifications");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.StockExportDetailId)
                .HasMaxLength(20)
                .HasColumnName("stockExportDetail_id");
            entity.Property(e => e.WarrantyEndDate)
                .HasColumnType("datetime")
                .HasColumnName("warranty_end_date");
            entity.Property(e => e.WarrantyStartDate)
                .HasColumnType("datetime")
                .HasColumnName("warranty_start_date");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductSerials)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_ProductSerial_Product");

            entity.HasOne(d => d.StockExportDetail).WithMany(p => p.ProductSerials)
                .HasForeignKey(d => d.StockExportDetailId)
                .HasConstraintName("FK_ProductSerial_StockExportDetail");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("Promotion");

            entity.Property(e => e.PromotionId)
                .HasMaxLength(20)
                .HasColumnName("promotion_id");
            entity.Property(e => e.ContentDetail)
                .HasMaxLength(200)
                .HasColumnName("content_detail");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");

            entity.HasOne(d => d.Product).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_Promotion_Product");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.Property(e => e.RoleId)
                .HasMaxLength(20)
                .HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<SaleInvoice>(entity =>
        {
            entity.ToTable("SaleInvoice");

            entity.Property(e => e.SaleInvoiceId)
                .HasMaxLength(20)
                .HasColumnName("saleInvoice_id");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .HasColumnName("customer_id");
            entity.Property(e => e.DeliveryAddress)
                .HasMaxLength(100)
                .HasColumnName("delivery_address");
            entity.Property(e => e.DeliveryFee)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("delivery_fee");
            entity.Property(e => e.Discount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("discount");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .HasColumnName("employee_id");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TimeCreate)
                .HasColumnType("datetime")
                .HasColumnName("time_create");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Customer).WithMany(p => p.SaleInvoices)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_SaleInvoice_Customer");

            entity.HasOne(d => d.Employee).WithMany(p => p.SaleInvoices)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_SaleInvoice_Employee");
        });

        modelBuilder.Entity<SaleInvoiceDetail>(entity =>
        {
            entity.ToTable("SaleInvoiceDetail");

            entity.Property(e => e.SaleInvoiceDetailId)
                .HasMaxLength(20)
                .HasColumnName("saleInvoiceDetail_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SaleInvoiceId)
                .HasMaxLength(20)
                .HasColumnName("saleInvoice_id");
            entity.Property(e => e.Specifications)
                .HasMaxLength(100)
                .HasColumnName("specifications");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleInvoiceDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_SaleInvoiceDetail_Product");

            entity.HasOne(d => d.SaleInvoice).WithMany(p => p.SaleInvoiceDetails)
                .HasForeignKey(d => d.SaleInvoiceId)
                .HasConstraintName("FK_SaleInvoiceDetail_SaleInvoice");
        });

        modelBuilder.Entity<StockExport>(entity =>
        {
            entity.ToTable("StockExport");

            entity.Property(e => e.StockExportId)
                .HasMaxLength(20)
                .HasColumnName("stockExport_id");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .HasColumnName("employee_id");
            entity.Property(e => e.SaleInvoiceId)
                .HasMaxLength(20)
                .HasColumnName("saleInvoice_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasColumnName("status");
            entity.Property(e => e.Time)
                .HasColumnType("datetime")
                .HasColumnName("time");

            entity.HasOne(d => d.Employee).WithMany(p => p.StockExports)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_StockExport_Employee");

            entity.HasOne(d => d.SaleInvoice).WithMany(p => p.StockExports)
                .HasForeignKey(d => d.SaleInvoiceId)
                .HasConstraintName("FK_StockExport_SaleInvoice");
        });

        modelBuilder.Entity<StockExportDetail>(entity =>
        {
            entity.ToTable("StockExportDetail");

            entity.Property(e => e.StockExportDetailId)
                .HasMaxLength(20)
                .HasColumnName("stockExportDetail_id");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Specifications)
                .HasMaxLength(100)
                .HasColumnName("specifications");
            entity.Property(e => e.StockExportId)
                .HasMaxLength(20)
                .HasColumnName("stockExport_id");

            entity.HasOne(d => d.StockExport).WithMany(p => p.StockExportDetails)
                .HasForeignKey(d => d.StockExportId)
                .HasConstraintName("FK_StockExportDetail_StockExport");
        });

        modelBuilder.Entity<StockImport>(entity =>
        {
            entity.ToTable("StockImport");

            entity.Property(e => e.StockImportId)
                .HasMaxLength(20)
                .HasColumnName("stockImport_id");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .HasColumnName("employee_id");
            entity.Property(e => e.SupplierId)
                .HasMaxLength(20)
                .HasColumnName("supplier_id");
            entity.Property(e => e.Time)
                .HasColumnType("datetime")
                .HasColumnName("time");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Employee).WithMany(p => p.StockImports)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_StockImport_Employee");

            entity.HasOne(d => d.Supplier).WithMany(p => p.StockImports)
                .HasForeignKey(d => d.SupplierId)
                .HasConstraintName("FK_StockImport_Supplier");
        });

        modelBuilder.Entity<StockImportDetail>(entity =>
        {
            entity.ToTable("StockImportDetail");

            entity.Property(e => e.StockImportDetailId)
                .HasMaxLength(20)
                .HasColumnName("stockImportDetail_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductId)
                .HasMaxLength(20)
                .HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Specifications)
                .HasMaxLength(100)
                .HasColumnName("specifications");
            entity.Property(e => e.StockImportId)
                .HasMaxLength(20)
                .HasColumnName("stockImport_id");

            entity.HasOne(d => d.Product).WithMany(p => p.StockImportDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_StockImportDetail_Product");

            entity.HasOne(d => d.StockImport).WithMany(p => p.StockImportDetails)
                .HasForeignKey(d => d.StockImportId)
                .HasConstraintName("FK_StockImportDetail_StockImport");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Supplier");

            entity.Property(e => e.SupplierId)
                .HasMaxLength(20)
                .HasColumnName("supplier_id");
            entity.Property(e => e.Active).HasColumnName("active");
            entity.Property(e => e.Address)
                .HasMaxLength(100)
                .HasColumnName("address");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(50)
                .HasColumnName("supplier_name");
        });

        modelBuilder.Entity<Warranty>(entity =>
        {
            entity.ToTable("Warranty");

            entity.Property(e => e.WarrantyId)
                .HasMaxLength(20)
                .HasColumnName("warranty_id");
            entity.Property(e => e.ContentDetail)
                .HasMaxLength(200)
                .HasColumnName("content_detail");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .HasColumnName("customer_id");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .HasColumnName("employee_id");
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("phone_number");
            entity.Property(e => e.SerialId)
                .HasMaxLength(20)
                .HasColumnName("serial_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasColumnName("type");

            entity.HasOne(d => d.Customer).WithMany(p => p.Warranties)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Warranty_Customer");

            entity.HasOne(d => d.Employee).WithMany(p => p.Warranties)
                .HasForeignKey(d => d.EmployeeId)
                .HasConstraintName("FK_Warranty_Employee");

            entity.HasOne(d => d.Serial).WithMany(p => p.Warranties)
                .HasForeignKey(d => d.SerialId)
                .HasConstraintName("FK_Warranty_ProductSerial");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
