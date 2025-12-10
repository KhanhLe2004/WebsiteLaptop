namespace WebLaptopBE.DTOs;

public class ChatDTO
{
    public string ChatId { get; set; } = null!;
    public string? ContentDetail { get; set; }
    public DateTime? Time { get; set; }
    public string? Status { get; set; }
    public string? CustomerId { get; set; }
    public string? EmployeeId { get; set; }
    public string? CustomerName { get; set; }
    public string? EmployeeName { get; set; }
    public string? CustomerAvatar { get; set; }
    public string? EmployeeAvatar { get; set; }
    public string? SenderType { get; set; }
}

public class SendMessageDTO
{
    public string? CustomerId { get; set; }
    public string? EmployeeId { get; set; }
    public string ContentDetail { get; set; } = string.Empty;
    public string SenderType { get; set; } = string.Empty;
}

public class ChatConversationDTO
{
    public string? CustomerId { get; set; }
    public string? EmployeeId { get; set; }
    public string? CustomerName { get; set; }
    public string? EmployeeName { get; set; }
    public string? CustomerAvatar { get; set; }
    public string? EmployeeAvatar { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public string? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}

