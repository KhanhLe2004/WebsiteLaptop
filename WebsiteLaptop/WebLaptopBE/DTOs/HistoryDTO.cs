namespace WebLaptopBE.DTOs;

public class HistoryDTO
{
    public string HistoryId { get; set; } = null!;
    public string? ActivityType { get; set; }
    public string? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateTime? Time { get; set; }
}

