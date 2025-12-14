using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Employee
{
    public string EmployeeId { get; set; } = null!;

    public string? EmployeeName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? RoleId { get; set; }

    public string? Avatar { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool? Active { get; set; }

    public virtual ICollection<Chat> Chats { get; set; } = new List<Chat>();

    public virtual ICollection<History> Histories { get; set; } = new List<History>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<SaleInvoice> SaleInvoices { get; set; } = new List<SaleInvoice>();

    public virtual ICollection<StockExport> StockExports { get; set; } = new List<StockExport>();

    public virtual ICollection<StockImport> StockImports { get; set; } = new List<StockImport>();

    public virtual ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}
