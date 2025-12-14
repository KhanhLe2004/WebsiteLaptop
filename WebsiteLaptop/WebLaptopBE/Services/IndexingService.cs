using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebLaptopBE.AI.SemanticKernel;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;
using WebLaptopBE.Models;

namespace WebLaptopBE.Services;

/// <summary>
/// Indexing Service - Đưa dữ liệu từ SQL → Qdrant
/// </summary>
public class IndexingService : IIndexingService
{
    private readonly WebLaptopTenTechContext _dbContext;
    private readonly IQdrantVectorService _qdrantVectorService;
    private readonly ISemanticKernelService _semanticKernelService;
    private readonly ILogger<IndexingService> _logger;
    private readonly IConfiguration _configuration;

    public IndexingService(
        WebLaptopTenTechContext dbContext,
        IQdrantVectorService qdrantVectorService,
        ISemanticKernelService semanticKernelService,
        ILogger<IndexingService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _qdrantVectorService = qdrantVectorService;
        _semanticKernelService = semanticKernelService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task IndexAllProductsAsync()
    {
        try
        {
            _logger.LogInformation("Bắt đầu indexing products...");

            // Đảm bảo collection tồn tại
            var laptopsCollection = _configuration["Qdrant:LaptopsCollection"] ?? "laptops_collection";
            if (!await _qdrantVectorService.CollectionExistsAsync(laptopsCollection))
            {
                await _qdrantVectorService.CreateCollectionAsync(laptopsCollection);
            }

            // Lấy tất cả products từ database
            var products = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .Where(p => p.Active == true)
                .ToListAsync();

            _logger.LogInformation("Tìm thấy {Count} products để index", products.Count);

            int successCount = 0;
            int failCount = 0;

            foreach (var product in products)
            {
                try
                {
                    await IndexProductAsync(product.ProductId);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi index product: {ProductId}", product.ProductId);
                    failCount++;
                }
            }

            _logger.LogInformation("Indexing hoàn tất: Thành công {Success}, Thất bại {Fail}", successCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi indexing products");
            throw;
        }
    }

    public async Task IndexProductAsync(string productId)
    {
        try
        {
            var product = await _dbContext.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductConfigurations)
                .FirstOrDefaultAsync(p => p.ProductId == productId && p.Active == true);

            if (product == null)
            {
                _logger.LogWarning("Product không tồn tại: {ProductId}", productId);
                return;
            }

            // Build description text từ product data
            var description = BuildProductDescription(product);

            // Tạo embedding
            var embedding = await GenerateEmbeddingAsync(description);

            // Build metadata
            var metadata = new Dictionary<string, object>
            {
                ["productId"] = product.ProductId,
                ["name"] = product.ProductName ?? string.Empty,
                ["brand"] = product.Brand?.BrandName ?? string.Empty,
                ["price"] = product.SellingPrice ?? 0,
                ["warrantyPeriod"] = product.WarrantyPeriod ?? 0
            };

            // Thêm thông tin cấu hình nếu có
            if (product.ProductConfigurations.Any())
            {
                var config = product.ProductConfigurations.First();
                metadata["cpu"] = config.Cpu ?? string.Empty;
                metadata["ram"] = config.Ram ?? string.Empty;
                metadata["rom"] = config.Rom ?? string.Empty;
                metadata["card"] = config.Card ?? string.Empty;
            }

            // Upsert vào Qdrant
            var productEmbedding = new ProductEmbedding
            {
                ProductId = product.ProductId,
                Name = product.ProductName ?? string.Empty,
                Description = description,
                Embedding = embedding,
                Metadata = metadata
            };

            await _qdrantVectorService.UpsertProductAsync(productEmbedding);
            
            _logger.LogDebug("Đã index product: {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi index product: {ProductId}", productId);
            throw;
        }
    }

    public async Task IndexAllPoliciesAsync()
    {
        try
        {
            _logger.LogInformation("Bắt đầu indexing policies...");

            // Đảm bảo collection tồn tại
            var policiesCollection = _configuration["Qdrant:PoliciesCollection"] ?? "policies_collection";
            if (!await _qdrantVectorService.CollectionExistsAsync(policiesCollection))
            {
                await _qdrantVectorService.CreateCollectionAsync(policiesCollection);
            }

            // Đọc policies từ file hoặc database
            // Tạm thời dùng hard-coded policies, có thể mở rộng đọc từ DB/file sau
            var policies = GetDefaultPolicies();

            int successCount = 0;
            int failCount = 0;

            foreach (var policy in policies)
            {
                try
                {
                    // Tạo embedding
                    var embedding = await GenerateEmbeddingAsync(policy.Content);

                    var policyEmbedding = new PolicyEmbedding
                    {
                        PolicyId = policy.PolicyId,
                        Content = policy.Content,
                        Embedding = embedding,
                        Metadata = policy.Metadata
                    };

                    await _qdrantVectorService.UpsertPolicyAsync(policyEmbedding);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi index policy: {PolicyId}", policy.PolicyId);
                    failCount++;
                }
            }

            _logger.LogInformation("Indexing policies hoàn tất: Thành công {Success}, Thất bại {Fail}", successCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi indexing policies");
            throw;
        }
    }

    /// <summary>
    /// Build product description text từ product entity - Cải thiện với nhiều context hơn
    /// </summary>
    private string BuildProductDescription(Product product)
    {
        var desc = new System.Text.StringBuilder();
        
        // Tên và thương hiệu
        desc.AppendLine($"Laptop {product.ProductName ?? product.ProductModel ?? "N/A"}");
        
        if (product.Brand != null)
        {
            desc.AppendLine($"Thương hiệu: {product.Brand.BrandName}");
        }
        
        // Cấu hình chi tiết với mô tả
        if (product.ProductConfigurations.Any())
        {
            var config = product.ProductConfigurations.First();
            var cpu = config.Cpu ?? "N/A";
            var ram = config.Ram ?? "N/A";
            var rom = config.Rom ?? "N/A";
            var card = config.Card ?? "";
            
            desc.AppendLine($"CPU: {cpu} {GetCpuDescription(cpu)}");
            desc.AppendLine($"RAM: {ram} {GetRamDescription(ram)}");
            desc.AppendLine($"Ổ cứng: {rom} {GetStorageDescription(rom)}");
            if (!string.IsNullOrEmpty(card))
            {
                desc.AppendLine($"Card đồ họa: {card} {GetGpuDescription(card)}");
            }
        }
        
        // Giá và phân khúc
        if (product.SellingPrice.HasValue)
        {
            var price = product.SellingPrice.Value;
            desc.AppendLine($"Giá: {price:N0} VND");
            desc.AppendLine($"Phân khúc: {GetPriceSegment(price)}");
        }
        
        // Thông tin khác
        if (product.WarrantyPeriod.HasValue)
        {
            desc.AppendLine($"Bảo hành: {product.WarrantyPeriod.Value} tháng");
        }
        
        if (!string.IsNullOrEmpty(product.Screen))
        {
            desc.AppendLine($"Màn hình: {product.Screen}");
        }
        
        if (product.Weight.HasValue)
        {
            desc.AppendLine($"Trọng lượng: {product.Weight.Value} kg {GetWeightCategory(product.Weight.Value)}");
        }
        
        // Use case recommendations
        var useCases = GetUseCaseRecommendations(product);
        if (!string.IsNullOrEmpty(useCases))
        {
            desc.AppendLine($"Phù hợp cho: {useCases}");
        }
        
        // Điểm nổi bật
        var highlights = GetHighlightFeatures(product);
        if (!string.IsNullOrEmpty(highlights))
        {
            desc.AppendLine($"Điểm nổi bật: {highlights}");
        }

        return desc.ToString();
    }
    
    /// <summary>
    /// Mô tả CPU dựa trên model
    /// </summary>
    private string GetCpuDescription(string? cpu)
    {
        if (string.IsNullOrEmpty(cpu)) return "";
        var cpuLower = cpu.ToLower();
        
        if (cpuLower.Contains("i9") || cpuLower.Contains("ryzen 9"))
            return "- Hiệu năng cực mạnh, phù hợp gaming cao cấp và đồ họa chuyên nghiệp";
        if (cpuLower.Contains("i7") || cpuLower.Contains("ryzen 7"))
            return "- Hiệu năng cao, phù hợp gaming và đồ họa";
        if (cpuLower.Contains("i5") || cpuLower.Contains("ryzen 5"))
            return "- Cân bằng hiệu năng và giá cả, phù hợp đa mục đích";
        if (cpuLower.Contains("i3") || cpuLower.Contains("ryzen 3"))
            return "- Đủ dùng cho văn phòng và học tập";
        
        return "";
    }
    
    /// <summary>
    /// Mô tả RAM dựa trên dung lượng
    /// </summary>
    private string GetRamDescription(string? ram)
    {
        if (string.IsNullOrEmpty(ram)) return "";
        var ramLower = ram.ToLower();
        
        if (ramLower.Contains("32") || ramLower.Contains("64"))
            return "- RAM lớn, đa nhiệm cực tốt, phù hợp công việc nặng";
        if (ramLower.Contains("16"))
            return "- RAM đủ lớn, đa nhiệm tốt, phù hợp gaming và đồ họa";
        if (ramLower.Contains("8"))
            return "- RAM đủ dùng cho văn phòng và học tập";
        
        return "";
    }
    
    /// <summary>
    /// Mô tả ổ cứng dựa trên loại và dung lượng
    /// </summary>
    private string GetStorageDescription(string? rom)
    {
        if (string.IsNullOrEmpty(rom)) return "";
        var romLower = rom.ToLower();
        
        if (romLower.Contains("ssd") || romLower.Contains("nvme"))
            return "- SSD nhanh, khởi động và load ứng dụng nhanh";
        if (romLower.Contains("hdd"))
            return "- HDD dung lượng lớn, giá tốt";
        if (romLower.Contains("512") || romLower.Contains("1tb"))
            return "- Dung lượng lớn, lưu trữ nhiều";
        
        return "";
    }
    
    /// <summary>
    /// Mô tả GPU dựa trên model
    /// </summary>
    private string GetGpuDescription(string? card)
    {
        if (string.IsNullOrEmpty(card)) return "";
        var cardLower = card.ToLower();
        
        if (cardLower.Contains("rtx 40") || cardLower.Contains("rtx 30"))
            return "- Card đồ họa rời mạnh, gaming 4K và đồ họa chuyên nghiệp";
        if (cardLower.Contains("rtx") || cardLower.Contains("gtx"))
            return "- Card đồ họa rời, gaming tốt";
        if (cardLower.Contains("radeon"))
            return "- Card đồ họa AMD, gaming và render tốt";
        if (cardLower.Contains("integrated") || cardLower.Contains("onboard"))
            return "- Card đồ họa tích hợp, đủ dùng văn phòng";
        
        return "";
    }
    
    /// <summary>
    /// Xác định phân khúc giá
    /// </summary>
    private string GetPriceSegment(decimal price)
    {
        if (price < 10000000)
            return "Tầm trung, phù hợp học sinh/sinh viên";
        if (price < 20000000)
            return "Tầm trung cao, phù hợp văn phòng và học tập";
        if (price < 30000000)
            return "Cao cấp, phù hợp gaming và đồ họa";
        return "Flagship, hiệu năng tối đa";
    }
    
    /// <summary>
    /// Xác định phân loại trọng lượng
    /// </summary>
    private string GetWeightCategory(decimal weight)
    {
        if (weight < 1.5m)
            return "- Siêu nhẹ, dễ mang theo";
        if (weight < 2.0m)
            return "- Nhẹ, tiện di chuyển";
        if (weight < 2.5m)
            return "- Trọng lượng vừa phải";
        return "- Nặng hơn, thường là laptop gaming";
    }
    
    /// <summary>
    /// Đề xuất use case dựa trên cấu hình
    /// </summary>
    private string GetUseCaseRecommendations(Product product)
    {
        var recommendations = new List<string>();
        
        if (product.ProductConfigurations.Any())
        {
            var config = product.ProductConfigurations.First();
            var cpu = config.Cpu?.ToLower() ?? "";
            var ram = config.Ram?.ToLower() ?? "";
            var card = config.Card?.ToLower() ?? "";
            var price = product.SellingPrice ?? 0;
            
            // Gaming
            if ((cpu.Contains("i7") || cpu.Contains("i9") || cpu.Contains("ryzen 7") || cpu.Contains("ryzen 9")) &&
                !string.IsNullOrEmpty(card) && (card.Contains("rtx") || card.Contains("gtx") || card.Contains("radeon")))
            {
                recommendations.Add("Gaming");
                recommendations.Add("Đồ họa");
            }
            
            // Văn phòng và học tập
            if (ram.Contains("8") || ram.Contains("16"))
            {
                if (price < 20000000)
                {
                    recommendations.Add("Văn phòng");
                    recommendations.Add("Học tập");
                }
            }
            
            // Đồ họa chuyên nghiệp
            if ((cpu.Contains("i7") || cpu.Contains("i9")) && ram.Contains("16") && 
                (card.Contains("rtx") || card.Contains("quadro")))
            {
                recommendations.Add("Đồ họa chuyên nghiệp");
                recommendations.Add("Render video");
            }
            
            // Lập trình
            if (ram.Contains("16") || ram.Contains("32"))
            {
                recommendations.Add("Lập trình");
            }
        }
        
        return recommendations.Any() ? string.Join(", ", recommendations.Distinct()) : "Đa mục đích";
    }
    
    /// <summary>
    /// Tạo điểm nổi bật của sản phẩm
    /// </summary>
    private string GetHighlightFeatures(Product product)
    {
        var highlights = new List<string>();
        
        if (product.ProductConfigurations.Any())
        {
            var config = product.ProductConfigurations.First();
            var cpu = config.Cpu?.ToLower() ?? "";
            var ram = config.Ram?.ToLower() ?? "";
            var rom = config.Rom?.ToLower() ?? "";
            var card = config.Card?.ToLower() ?? "";
            
            if (cpu.Contains("i9") || cpu.Contains("ryzen 9"))
                highlights.Add("CPU flagship");
            else if (cpu.Contains("i7") || cpu.Contains("ryzen 7"))
                highlights.Add("CPU mạnh");
            
            if (ram.Contains("32") || ram.Contains("64"))
                highlights.Add("RAM cực lớn");
            else if (ram.Contains("16"))
                highlights.Add("RAM lớn");
            
            if (rom.Contains("ssd") || rom.Contains("nvme"))
                highlights.Add("SSD nhanh");
            
            if (card.Contains("rtx 40") || card.Contains("rtx 30"))
                highlights.Add("Card đồ họa flagship");
            else if (card.Contains("rtx") || card.Contains("gtx"))
                highlights.Add("Card đồ họa rời");
        }
        
        if (product.SellingPrice.HasValue && product.SellingPrice.Value < 15000000)
        {
            highlights.Add("Giá tốt");
        }
        
        if (product.Weight.HasValue && product.Weight.Value < 1.5m)
        {
            highlights.Add("Siêu nhẹ");
        }
        
        return highlights.Any() ? string.Join(", ", highlights) : "";
    }

    /// <summary>
    /// Generate embedding từ text
    /// </summary>
    private async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        // Sử dụng QdrantVectorService để generate embedding
        // Tạm thời tạo một HttpClient riêng để gọi OpenAI
        var httpClient = new HttpClient();
        var apiKey = _configuration["OpenAI:ApiKey"] 
            ?? throw new InvalidOperationException("OpenAI:ApiKey không được cấu hình");

        var request = new
        {
            input = text,
            model = "text-embedding-ada-002"
        };

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        var response = await httpClient.PostAsJsonAsync("https://api.openai.com/v1/embeddings", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>();
        
        if (result?.Data == null || result.Data.Count == 0)
        {
            throw new Exception("Không nhận được embedding từ OpenAI");
        }

        return result.Data[0].Embedding;
    }

    /// <summary>
    /// Get default policies (có thể mở rộng đọc từ DB/file)
    /// </summary>
    private List<PolicyData> GetDefaultPolicies()
    {
        return new List<PolicyData>
        {
            new PolicyData
            {
                PolicyId = "policy_warranty_001",
                Content = @"Chính sách bảo hành: Tất cả sản phẩm laptop được bảo hành chính hãng từ 12 đến 24 tháng tùy theo sản phẩm. 
Bảo hành bao gồm lỗi phần cứng và phần mềm do nhà sản xuất. 
Khách hàng cần giữ hóa đơn và tem bảo hành. 
Thời gian xử lý bảo hành từ 3-7 ngày làm việc.",
                Metadata = new Dictionary<string, object>
                {
                    ["policy_type"] = "warranty",
                    ["title"] = "Chính sách bảo hành"
                }
            },
            new PolicyData
            {
                PolicyId = "policy_return_001",
                Content = @"Chính sách đổi trả: Khách hàng có thể đổi trả sản phẩm trong vòng 7 ngày kể từ ngày mua nếu sản phẩm còn nguyên seal, chưa sử dụng, và có lỗi do nhà sản xuất. 
Sản phẩm đổi trả phải kèm theo hóa đơn và đầy đủ phụ kiện. 
Phí vận chuyển đổi trả do khách hàng chịu trừ trường hợp lỗi do nhà sản xuất.",
                Metadata = new Dictionary<string, object>
                {
                    ["policy_type"] = "return",
                    ["title"] = "Chính sách đổi trả"
                }
            },
            new PolicyData
            {
                PolicyId = "policy_refund_001",
                Content = @"Chính sách hoàn tiền: Hoàn tiền 100% trong vòng 3 ngày đầu nếu sản phẩm chưa sử dụng, còn nguyên seal, và có lỗi do nhà sản xuất. 
Sau 3 ngày, chỉ áp dụng đổi sản phẩm khác. 
Hoàn tiền sẽ được thực hiện qua phương thức thanh toán ban đầu trong vòng 5-7 ngày làm việc.",
                Metadata = new Dictionary<string, object>
                {
                    ["policy_type"] = "refund",
                    ["title"] = "Chính sách hoàn tiền"
                }
            }
        };
    }

    private class PolicyData
    {
        public string PolicyId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    private class OpenAIEmbeddingResponse
    {
        public List<EmbeddingData> Data { get; set; } = new();
    }

    private class EmbeddingData
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}


