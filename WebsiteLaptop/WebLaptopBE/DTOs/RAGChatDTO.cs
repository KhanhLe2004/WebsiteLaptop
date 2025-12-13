namespace WebLaptopBE.DTOs;

/// <summary>
/// Response cho RAG Chat API - Mở rộng với actions (button options)
/// </summary>
public class RAGChatResponse
{
    /// <summary>
    /// Câu trả lời từ AI
    /// </summary>
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// Danh sách actions (button options) để người dùng chọn
    /// </summary>
    public List<ChatAction>? Actions { get; set; }
    
    /// <summary>
    /// Danh sách sản phẩm gợi ý (với đầy đủ thông tin: ảnh, link)
    /// </summary>
    public List<ProductSuggestion>? SuggestedProducts { get; set; }
    
    /// <summary>
    /// Thời gian tạo response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Session ID
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Chat Action - Đại diện cho một button option
/// </summary>
public class ChatAction
{
    /// <summary>
    /// Label hiển thị trên button
    /// </summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>
    /// Loại action: "quick_reply", "url", "menu"
    /// </summary>
    public string Type { get; set; } = "quick_reply";
    
    /// <summary>
    /// Payload gửi về server khi click button
    /// Ví dụ: { "intent": "filter_brand", "value": "Dell" }
    /// </summary>
    public Dictionary<string, object>? Payload { get; set; }
    
    /// <summary>
    /// URL (nếu type = "url")
    /// </summary>
    public string? Url { get; set; }
}

/// <summary>
/// Product Suggestion - Sản phẩm gợi ý với đầy đủ thông tin
/// </summary>
public class ProductSuggestion
{
    public string ProductId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? DetailUrl { get; set; }
    public string? Brand { get; set; }
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Storage { get; set; }
}

/// <summary>
/// Conversation State - Lưu trạng thái hội thoại
/// </summary>
public class ConversationState
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Bước hiện tại trong flow
    /// Ví dụ: "menu", "select_brand", "select_cpu", "show_results"
    /// </summary>
    public string CurrentStep { get; set; } = "menu";
    
    /// <summary>
    /// Filters đã chọn
    /// </summary>
    public ProductFilter Filters { get; set; } = new();
    
    /// <summary>
    /// Lịch sử hội thoại
    /// </summary>
    public List<string> MessageHistory { get; set; } = new();
}

/// <summary>
/// Product Filter - Filter sản phẩm
/// </summary>
public class ProductFilter
{
    public string? BrandId { get; set; }
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Storage { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    
    public bool HasAnyFilter()
    {
        return !string.IsNullOrEmpty(BrandId) 
            || !string.IsNullOrEmpty(Cpu) 
            || !string.IsNullOrEmpty(Ram) 
            || !string.IsNullOrEmpty(Storage) 
            || MinPrice.HasValue 
            || MaxPrice.HasValue;
    }
    
    public void Clear()
    {
        BrandId = null;
        Cpu = null;
        Ram = null;
        Storage = null;
        MinPrice = null;
        MaxPrice = null;
    }
}

/// <summary>
/// Option Item - Một lựa chọn trong danh sách options
/// </summary>
public class OptionItem
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; } // Số lượng sản phẩm
}

