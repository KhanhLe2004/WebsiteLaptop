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
    /// <returns>RAGChatResponse với answer và suggestedProducts</returns>
    Task<RAGChatResponse> ProcessUserMessageAsync(string userMessage, string? customerId = null);
}

// NOTE: RAGChatResponse đã được định nghĩa trong WebLaptopBE.DTOs.RAGChatDTO.cs
// Không định nghĩa lại ở đây để tránh conflict


