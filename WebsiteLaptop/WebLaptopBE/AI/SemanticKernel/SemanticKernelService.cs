using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace WebLaptopBE.AI.SemanticKernel;

/// <summary>
/// Service để setup và quản lý Semantic Kernel
/// Semantic Kernel là framework của Microsoft để làm việc với LLM (Large Language Models)
/// </summary>
public interface ISemanticKernelService
{
    /// <summary>
    /// Lấy Kernel instance (dùng để gọi LLM)
    /// </summary>
    Kernel GetKernel();
    
    /// <summary>
    /// Tạo response từ LLM dựa trên prompt
    /// </summary>
    Task<string> GenerateResponseAsync(string prompt, KernelArguments? arguments = null);
}

/// <summary>
/// Implementation của SemanticKernelService
/// </summary>
public class SemanticKernelService : ISemanticKernelService
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelService> _logger;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor - Setup Semantic Kernel với OpenAI
    /// </summary>
    public SemanticKernelService(IConfiguration configuration, ILogger<SemanticKernelService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Lấy OpenAI API Key từ config
        var openAiApiKey = _configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI:ApiKey không được cấu hình trong appsettings.json");
        
        // Lấy model name từ config (mặc định: gpt-4o-mini - rẻ hơn gpt-4)
        var modelId = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        
        // Tạo Kernel builder
        var builder = Kernel.CreateBuilder();
        
        // Thêm OpenAI Chat Completion service
        // Chat Completion = service để chat với LLM
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: openAiApiKey
        );
        
        // Build kernel
        _kernel = builder.Build();
        
        _logger.LogInformation("Semantic Kernel đã được khởi tạo với model: {ModelId}", modelId);
    }

    /// <summary>
    /// Lấy Kernel instance
    /// </summary>
    public Kernel GetKernel() => _kernel;

    /// <summary>
    /// Tạo response từ LLM
    /// </summary>
    /// <param name="prompt">Câu prompt (hướng dẫn) cho LLM</param>
    /// <param name="arguments">Các biến để thay thế trong prompt (optional)</param>
    /// <returns>Response từ LLM</returns>
    public async Task<string> GenerateResponseAsync(string prompt, KernelArguments? arguments = null)
    {
        try
        {
            // Gọi LLM với prompt
            var result = await _kernel.InvokePromptAsync(prompt, arguments ?? new KernelArguments());
            
            // Trả về response
            return result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi generate response với Semantic Kernel");
            throw;
        }
    }
}




