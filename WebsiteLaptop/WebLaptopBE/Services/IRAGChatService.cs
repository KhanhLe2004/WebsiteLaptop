using WebLaptopBE.DTOs;

namespace WebLaptopBE.Services;

/// <summary>
/// Interface cho RAG Chat Service
/// RAG = Retrieval-Augmented Generation
/// Service này thực hiện: User question → Vector search → LLM generate response
/// </summary>
public interface IRAGChatService
{
    /// <summary>
    /// Xử lý câu hỏi của user và trả về câu trả lời bằng RAG
    /// </summary>
    /// <param name="userMessage">Câu hỏi của user</param>
    /// <param name="customerId">ID khách hàng (optional)</param>
    /// <returns>ChatResponse với answer và suggestedProducts</returns>
    Task<RAGChatResponse> ProcessUserMessageAsync(string userMessage, string? customerId = null);
}

/// <summary>
/// Response từ RAG Chat Service
/// </summary>
public class RAGChatResponse
{
    /// <summary>
    /// Câu trả lời từ AI (tiếng Việt)
    /// </summary>
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// Danh sách sản phẩm đề xuất (nếu có)
    /// </summary>
    public List<ProductDTO>? SuggestedProducts { get; set; }
    
    /// <summary>
    /// Thời gian tạo response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}


