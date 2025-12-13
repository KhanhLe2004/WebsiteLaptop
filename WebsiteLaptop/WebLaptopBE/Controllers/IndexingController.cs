using Microsoft.AspNetCore.Mvc;
using WebLaptopBE.Services;

namespace WebLaptopBE.Controllers;

/// <summary>
/// Controller để quản lý indexing (đưa dữ liệu từ SQL → Qdrant)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IndexingController : ControllerBase
{
    private readonly IIndexingService _indexingService;
    private readonly ILogger<IndexingController> _logger;

    public IndexingController(
        IIndexingService indexingService,
        ILogger<IndexingController> logger)
    {
        _indexingService = indexingService;
        _logger = logger;
    }

    /// <summary>
    /// Index tất cả products vào Qdrant
    /// POST /api/indexing/products
    /// </summary>
    [HttpPost("products")]
    public async Task<IActionResult> IndexProducts()
    {
        try
        {
            _logger.LogInformation("Bắt đầu indexing products...");
            await _indexingService.IndexAllProductsAsync();
            return Ok(new { message = "Indexing products hoàn tất", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi indexing products");
            return StatusCode(500, new { message = "Lỗi khi indexing products", error = ex.Message });
        }
    }

    /// <summary>
    /// Index tất cả policies vào Qdrant
    /// POST /api/indexing/policies
    /// </summary>
    [HttpPost("policies")]
    public async Task<IActionResult> IndexPolicies()
    {
        try
        {
            _logger.LogInformation("Bắt đầu indexing policies...");
            await _indexingService.IndexAllPoliciesAsync();
            return Ok(new { message = "Indexing policies hoàn tất", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi indexing policies");
            return StatusCode(500, new { message = "Lỗi khi indexing policies", error = ex.Message });
        }
    }

    /// <summary>
    /// Index tất cả (products + policies)
    /// POST /api/indexing/all
    /// </summary>
    [HttpPost("all")]
    public async Task<IActionResult> IndexAll()
    {
        try
        {
            _logger.LogInformation("Bắt đầu indexing tất cả...");
            await _indexingService.IndexAllProductsAsync();
            await _indexingService.IndexAllPoliciesAsync();
            return Ok(new { message = "Indexing hoàn tất", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi indexing");
            return StatusCode(500, new { message = "Lỗi khi indexing", error = ex.Message });
        }
    }
}


