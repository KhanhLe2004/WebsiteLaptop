using System;
using System.Collections.Generic;

namespace WebLaptopBE.Models;

public partial class Branch
{
    public string BranchesId { get; set; } = null!;

    public string? BranchesName { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
