namespace WebLaptopFE.Areas.Admin.Models
{
    public class SignInResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public EmployeeInfo? Employee { get; set; }
    }

    public class EmployeeInfo
    {
        public string EmployeeId { get; set; } = null!;
        public string? EmployeeName { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Avatar { get; set; }
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? BranchesId { get; set; }
        public string? BranchesName { get; set; }
        public bool? Active { get; set; }
    }
}

