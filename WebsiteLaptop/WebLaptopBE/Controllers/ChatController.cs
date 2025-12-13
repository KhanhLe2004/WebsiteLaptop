using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using WebLaptopBE.AI.Orchestrator;
using WebLaptopBE.AI.SemanticKernel;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Services;

namespace WebLaptopBE.Controllers;

/// <summary>
/// Controller để xử lý các request từ chat widget
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatOrchestratorService _orchestrator;
    private readonly IRAGChatService _ragChatService;
    private readonly ILogger<ChatController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ChatController(
        IChatOrchestratorService orchestrator,
        IRAGChatService ragChatService,
        ILogger<ChatController> logger,
        IWebHostEnvironment environment)
    {
        _orchestrator = orchestrator;
        _ragChatService = ragChatService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Endpoint chính để chat với AI (RAG-based)
    /// POST /api/chat/ai
    /// </summary>
    [HttpPost("ai")]
    public async Task<ActionResult<RAGChatResponse>> ChatAI([FromBody] RAGChatRequest request)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                return BadRequest(new { message = "Request body không được để trống" });
            }

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message không được để trống" });
            }

            // Validate message length
            if (request.Message.Length > 1000)
            {
                return BadRequest(new { message = "Message quá dài. Vui lòng giới hạn trong 1000 ký tự." });
            }

            _logger.LogInformation("Received chat request: Message length = {Length}, CustomerId = {CustomerId}", 
                request.Message.Length, request.CustomerId ?? "null");

            // Xử lý message bằng RAG
            var response = await _ragChatService.ProcessUserMessageAsync(request.Message, request.CustomerId);
            
            if (response == null)
            {
                _logger.LogWarning("RAG service returned null response");
                return StatusCode(500, new { 
                    message = "Xin lỗi, hệ thống không thể tạo phản hồi. Vui lòng thử lại sau."
                });
            }

            _logger.LogInformation("Successfully processed chat request, response length = {Length}", 
                response.Answer?.Length ?? 0);
            
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument in chat request");
            return BadRequest(new { 
                message = "Yêu cầu không hợp lệ: " + ex.Message,
                error = ex.Message 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing RAG chat message: {ErrorType} - {ErrorMessage}", 
                ex.GetType().Name, ex.Message);
            
            // Don't expose internal error details to client
            return StatusCode(500, new { 
                message = "Xin lỗi, hiện tại hệ thống đang gặp sự cố. Anh/chị vui lòng thử lại sau.",
                // Only include error details in development
                error = _environment.IsDevelopment() ? ex.Message : null
            });
        }
    }

    /// <summary>
    /// Endpoint cũ để chat với AI (dùng Orchestrator)
    /// POST /api/chat
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { message = "Message không được để trống" });
            }

            // Xử lý message
            var response = await _orchestrator.ProcessMessageAsync(request);
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { message = "Lỗi server, vui lòng thử lại sau", error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy lịch sử chat theo session ID
    /// GET /api/chat/history/{sessionId}
    /// </summary>
    [HttpGet("history/{sessionId}")]
    public Task<ActionResult<List<ChatHistoryItem>>> GetChatHistory(string sessionId)
    {
        try
        {
            // TODO: Implement lấy chat history từ database
            // Hiện tại trả về empty list
            return Task.FromResult<ActionResult<List<ChatHistoryItem>>>(Ok(new List<ChatHistoryItem>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat history");
            return Task.FromResult<ActionResult<List<ChatHistoryItem>>>(StatusCode(500, new { message = "Lỗi server", error = ex.Message }));
        }
    }

    /// <summary>
    /// Health check endpoint - Basic
    /// GET /api/chat/health
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Comprehensive health check endpoint - Checks all dependencies
    /// GET /api/chat/health/detailed
    /// </summary>
    [HttpGet("health/detailed")]
    public async Task<IActionResult> HealthDetailed()
    {
        var components = new Dictionary<string, object>();
        string overallStatus = "healthy";

        // Check Qdrant
        try
        {
            var qdrantService = HttpContext.RequestServices.GetRequiredService<IQdrantVectorService>();
            var laptopsExists = await qdrantService.CollectionExistsAsync("laptops_collection");
            var policiesExists = await qdrantService.CollectionExistsAsync("policies_collection");
            
            var qdrantStatus = laptopsExists && policiesExists ? "healthy" : "degraded";
            if (qdrantStatus == "degraded" && overallStatus == "healthy")
            {
                overallStatus = "degraded";
            }
            
            components["qdrant"] = new
            {
                status = qdrantStatus,
                laptopsCollection = laptopsExists,
                policiesCollection = policiesExists
            };
        }
        catch (Exception ex)
        {
            components["qdrant"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
            if (overallStatus == "healthy")
            {
                overallStatus = "degraded";
            }
        }

        // Check OpenAI/Semantic Kernel
        try
        {
            var semanticKernelService = HttpContext.RequestServices.GetRequiredService<ISemanticKernelService>();
            var kernel = semanticKernelService.GetKernel();
            var openAiStatus = kernel != null ? "healthy" : "unhealthy";
            
            if (openAiStatus == "unhealthy" && overallStatus == "healthy")
            {
                overallStatus = "degraded";
            }
            
            components["openai"] = new
            {
                status = openAiStatus
            };
        }
        catch (Exception ex)
        {
            components["openai"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
            if (overallStatus == "healthy")
            {
                overallStatus = "degraded";
            }
        }

        // Check Database
        try
        {
            var dbContext = HttpContext.RequestServices.GetRequiredService<Testlaptop38Context>();
            var productCount = await dbContext.Products.CountAsync();
            components["database"] = new
            {
                status = "healthy",
                productCount = productCount
            };
        }
        catch (Exception ex)
        {
            components["database"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
            overallStatus = "unhealthy";
        }

        // Check RAG Service
        try
        {
            var ragService = HttpContext.RequestServices.GetRequiredService<IRAGChatService>();
            var ragStatus = ragService != null ? "healthy" : "unhealthy";
            
            if (ragStatus == "unhealthy" && overallStatus == "healthy")
            {
                overallStatus = "degraded";
            }
            
            components["ragService"] = new
            {
                status = ragStatus
            };
        }
        catch (Exception ex)
        {
            components["ragService"] = new
            {
                status = "unhealthy",
                error = ex.Message
            };
            if (overallStatus == "healthy")
            {
                overallStatus = "degraded";
            }
        }

        // Create final response object
        var healthStatus = new
        {
            timestamp = DateTime.UtcNow,
            status = overallStatus,
            components = components
        };

        var statusCode = overallStatus == "healthy" ? 200 : 
                        overallStatus == "degraded" ? 200 : 503;
        
        return StatusCode(statusCode, healthStatus);
    }
}



