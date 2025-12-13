using System.Text.Json.Serialization;

namespace WebLaptopBE.DTOs;

/// <summary>
/// Request từ frontend khi user gửi message
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// Câu hỏi/message của user
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Session ID để theo dõi cuộc hội thoại (tự động tạo nếu không có)
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// ID khách hàng (nếu đã đăng nhập)
    /// </summary>
    public string? CustomerId { get; set; }
    
    /// <summary>
    /// Context bổ sung (optional)
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Response trả về cho frontend
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// Câu trả lời từ AI
    /// </summary>
    public string Response { get; set; } = string.Empty;
    
    /// <summary>
    /// Intent đã phát hiện (ví dụ: "product_inquiry", "policy_inquiry")
    /// </summary>
    public string Intent { get; set; } = string.Empty;
    
    /// <summary>
    /// Độ tin cậy của intent detection (0-1)
    /// </summary>
    public float Confidence { get; set; }
    
    /// <summary>
    /// Session ID của cuộc hội thoại
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Danh sách sản phẩm (nếu intent là product_inquiry)
    /// </summary>
    public List<ProductDTO>? Products { get; set; }
    
    /// <summary>
    /// Danh sách policy documents (nếu intent là policy_inquiry)
    /// </summary>
    public List<PolicySearchResultDTO>? Policies { get; set; }
    
    /// <summary>
    /// Có cần làm rõ câu hỏi không (nếu confidence thấp)
    /// </summary>
    public bool RequiresClarification { get; set; }
    
    /// <summary>
    /// Thời gian tạo response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO cho policy search result (để trả về cho frontend)
/// </summary>
public class PolicySearchResultDTO
{
    public string Content { get; set; } = string.Empty;
    public float Score { get; set; }
    public string? PolicyType { get; set; }
}

/// <summary>
/// Item trong lịch sử chat
/// </summary>
public class ChatHistoryItem
{
    public string ChatId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime Time { get; set; }
    public string? Intent { get; set; }
}

/// <summary>
/// Intent detection result (dùng trong nội bộ)
/// </summary>
public class IntentResult
{
    public string Intent { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public Dictionary<string, object> ExtractedInfo { get; set; } = new();
}

/// <summary>
/// Request cho RAG Chat API - Mở rộng để hỗ trợ guided conversation
/// </summary>
public class RAGChatRequest
{
    /// <summary>
    /// Câu hỏi của user
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// ID khách hàng (optional)
    /// </summary>
    public string? CustomerId { get; set; }
    
    /// <summary>
    /// Session ID để theo dõi conversation state
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Payload từ button click (nếu có)
    /// Ví dụ: { "intent": "filter_brand", "value": "Dell", "step": "select_cpu" }
    /// </summary>
    public Dictionary<string, object>? Payload { get; set; }
}



