using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;

namespace WebLaptopBE.Controllers;

/// <summary>
/// Controller để lấy options (brands, CPU, RAM, Storage) từ SQL
/// Phục vụ guided conversation với button options
/// </summary>
[ApiController]
[Route("api/chat/options")]
public class ChatOptionsController : ControllerBase
{
    private readonly WebLaptopTenTechContext _dbContext;
    private readonly ILogger<ChatOptionsController> _logger;

    public ChatOptionsController(
        WebLaptopTenTechContext dbContext,
        ILogger<ChatOptionsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách brands từ DB
    /// GET /api/chat/options/brands
    /// </summary>
    [HttpGet("brands")]
    public async Task<ActionResult<List<OptionItem>>> GetBrands()
    {
        try
        {
            var brands = await _dbContext.Brands
                .Where(b => b.BrandId != null)
                .Select(b => new OptionItem
                {
                    Value = b.BrandId!,
                    Label = b.BrandName ?? b.BrandId!,
                    Count = _dbContext.Products.Count(p => p.BrandId == b.BrandId)
                })
                .Where(b => b.Count > 0) // Chỉ lấy brand có sản phẩm
                .OrderByDescending(b => b.Count)
                .ToListAsync();

            _logger.LogInformation("Found {Count} brands", brands.Count);
            return Ok(brands);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands");
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách hãng" });
        }
    }

    /// <summary>
    /// Lấy danh sách CPU từ DB (distinct)
    /// GET /api/chat/options/cpu
    /// </summary>
    [HttpGet("cpu")]
    public async Task<ActionResult<List<OptionItem>>> GetCpuOptions([FromQuery] string? brandId = null)
    {
        try
        {
            var query = _dbContext.ProductConfigurations.AsQueryable();
            
            // Filter theo brand nếu có
            if (!string.IsNullOrEmpty(brandId))
            {
                query = query.Where(pc => pc.Product != null && pc.Product.BrandId == brandId);
            }

            var cpuOptions = await query
                .Where(pc => !string.IsNullOrEmpty(pc.Cpu))
                .GroupBy(pc => pc.Cpu)
                .Select(g => new OptionItem
                {
                    Value = g.Key!,
                    Label = g.Key!,
                    Count = g.Count()
                })
                .OrderByDescending(o => o.Count)
                .Take(20) // Giới hạn 20 options
                .ToListAsync();

            _logger.LogInformation("Found {Count} CPU options (brandId={BrandId})", cpuOptions.Count, brandId);
            return Ok(cpuOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CPU options");
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách CPU" });
        }
    }

    /// <summary>
    /// Lấy danh sách RAM từ DB (distinct)
    /// GET /api/chat/options/ram
    /// </summary>
    [HttpGet("ram")]
    public async Task<ActionResult<List<OptionItem>>> GetRamOptions([FromQuery] string? brandId = null)
    {
        try
        {
            var query = _dbContext.ProductConfigurations.AsQueryable();
            
            if (!string.IsNullOrEmpty(brandId))
            {
                query = query.Where(pc => pc.Product != null && pc.Product.BrandId == brandId);
            }

            var ramOptions = await query
                .Where(pc => !string.IsNullOrEmpty(pc.Ram))
                .GroupBy(pc => pc.Ram)
                .Select(g => new OptionItem
                {
                    Value = g.Key!,
                    Label = g.Key!,
                    Count = g.Count()
                })
                .OrderBy(o => ParseRamSize(o.Value)) // Sort theo dung lượng
                .ToListAsync();

            _logger.LogInformation("Found {Count} RAM options (brandId={BrandId})", ramOptions.Count, brandId);
            return Ok(ramOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting RAM options");
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách RAM" });
        }
    }

    /// <summary>
    /// Lấy danh sách Storage từ DB (distinct)
    /// GET /api/chat/options/storage
    /// </summary>
    [HttpGet("storage")]
    public async Task<ActionResult<List<OptionItem>>> GetStorageOptions([FromQuery] string? brandId = null)
    {
        try
        {
            var query = _dbContext.ProductConfigurations.AsQueryable();
            
            if (!string.IsNullOrEmpty(brandId))
            {
                query = query.Where(pc => pc.Product != null && pc.Product.BrandId == brandId);
            }

            var storageOptions = await query
                .Where(pc => !string.IsNullOrEmpty(pc.Rom))
                .GroupBy(pc => pc.Rom)
                .Select(g => new OptionItem
                {
                    Value = g.Key!,
                    Label = g.Key!,
                    Count = g.Count()
                })
                .OrderBy(o => ParseStorageSize(o.Value)) // Sort theo dung lượng
                .ToListAsync();

            _logger.LogInformation("Found {Count} storage options (brandId={BrandId})", storageOptions.Count, brandId);
            return Ok(storageOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage options");
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách ổ cứng" });
        }
    }

    /// <summary>
    /// Lấy danh sách price ranges (fixed ranges)
    /// GET /api/chat/options/price-ranges
    /// </summary>
    [HttpGet("price-ranges")]
    public ActionResult<List<OptionItem>> GetPriceRanges()
    {
        try
        {
            var priceRanges = new List<OptionItem>
            {
                new() { Value = "0-10", Label = "Dưới 10 triệu", Count = 0 },
                new() { Value = "10-15", Label = "10 - 15 triệu", Count = 0 },
                new() { Value = "15-20", Label = "15 - 20 triệu", Count = 0 },
                new() { Value = "20-25", Label = "20 - 25 triệu", Count = 0 },
                new() { Value = "25-30", Label = "25 - 30 triệu", Count = 0 },
                new() { Value = "30-40", Label = "30 - 40 triệu", Count = 0 },
                new() { Value = "40-50", Label = "40 - 50 triệu", Count = 0 },
                new() { Value = "50-100", Label = "Trên 50 triệu", Count = 0 }
            };

            return Ok(priceRanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price ranges");
            return StatusCode(500, new { message = "Lỗi khi lấy khoảng giá" });
        }
    }

    /// <summary>
    /// Parse RAM size từ string (ví dụ: "16GB" -> 16)
    /// </summary>
    private int ParseRamSize(string ram)
    {
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(ram, @"(\d+)");
            return match.Success ? int.Parse(match.Value) : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Parse Storage size từ string (ví dụ: "512GB SSD" -> 512)
    /// </summary>
    private int ParseStorageSize(string storage)
    {
        try
        {
            var match = System.Text.RegularExpressions.Regex.Match(storage, @"(\d+)");
            return match.Success ? int.Parse(match.Value) : 0;
        }
        catch
        {
            return 0;
        }
    }
}

