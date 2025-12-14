using WebLaptopBE.DTOs;

namespace WebLaptopBE.Services;

public interface IRAGChatService
{

    Task<RAGChatResponse> ProcessUserMessageAsync(string userMessage, string? customerId = null);
}



