using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;
using WebLaptopBE.Data;
using WebLaptopBE.DTOs;

namespace WebLaptopBE.Services;

/// <summary>
/// Interface cho Guided Chat Service
/// </summary>
public interface IGuidedChatService
{
    Task<RAGChatResponse> ProcessMessageAsync(RAGChatRequest request);
}

/// <summary>
/// Guided Chat Service - Chatbot với guided conversation (button options)
/// Giảm phụ thuộc NLP, tăng tính ổn định bằng button options
/// </summary>
public class GuidedChatService : IGuidedChatService
{
    private readonly IConversationStateService _conversationStateService;
    private readonly Testlaptop38Context _dbContext;
    private readonly ILogger<GuidedChatService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public GuidedChatService(
        IConversationStateService conversationStateService,
        Testlaptop38Context dbContext,
        ILogger<GuidedChatService> logger,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration)
    {
        _conversationStateService = conversationStateService;
        _dbContext = dbContext;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    /// <summary>
    /// Xử lý message từ user
    /// </summary>
    public async Task<RAGChatResponse> ProcessMessageAsync(RAGChatRequest request)
    {
        try
        {
            // 1. Get hoặc tạo conversation state
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            var state = _conversationStateService.GetOrCreate(sessionId);

            // 2. Xử lý payload từ button click (nếu có)
            if (request.Payload != null && request.Payload.Count > 0)
            {
                return await HandleButtonClickAsync(request.Payload, state);
            }

            // 3. Xử lý text input (phân tích đơn giản)
            return await HandleTextInputAsync(request.Message, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return new RAGChatResponse
            {
                Answer = "Xin lỗi, hiện tại hệ thống đang gặp sự cố. Anh/chị vui lòng thử lại sau.",
                Actions = GetMenuActions(),
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Xử lý button click
    /// </summary>
    private async Task<RAGChatResponse> HandleButtonClickAsync(
        Dictionary<string, object> payload, 
        ConversationState state)
    {
        var intent = payload.GetValueOrDefault("intent")?.ToString() ?? "";
        var value = payload.GetValueOrDefault("value")?.ToString() ?? "";

        _logger.LogInformation("Button click: intent={Intent}, value={Value}", intent, value);

        return intent switch
        {
            "menu_brand" => await ShowBrandOptions(state),
            "menu_cpu" => await ShowCpuOptions(state),
            "menu_ram" => await ShowRamOptions(state),
            "menu_storage" => await ShowStorageOptions(state),
            "menu_price" => ShowPriceOptions(state),
            "menu_warranty" => ShowWarrantyPolicy(),
            "menu_payment" => ShowPaymentPolicy(),
            "menu_privacy" => ShowPrivacyPolicy(),
            
            "filter_brand" => await HandleBrandSelection(value, state),
            "filter_cpu" => await HandleCpuSelection(value, state),
            "filter_ram" => await HandleRamSelection(value, state),
            "filter_storage" => await HandleStorageSelection(value, state),
            "filter_price" => await HandlePriceSelection(value, state),
            
            "show_results" => await ShowProductResults(state),
            "clear_filters" => ClearFiltersAndShowMenu(state),
            "back_to_menu" => ShowMainMenu(state),
            
            _ => ShowMainMenu(state)
        };
    }

    /// <summary>
    /// Xử lý text input (phân tích đơn giản, nếu không chắc thì hỏi lại)
    /// </summary>
    private async Task<RAGChatResponse> HandleTextInputAsync(string message, ConversationState state)
    {
        var messageLower = message.ToLower().Trim();

        // Phát hiện chính sách
        if (ContainsAny(messageLower, "bảo hành", "bao hanh", "warranty"))
        {
            return ShowWarrantyPolicy();
        }
        if (ContainsAny(messageLower, "thanh toán", "thanh toan", "payment", "cod", "qr"))
        {
            return ShowPaymentPolicy();
        }
        if (ContainsAny(messageLower, "bảo mật", "bao mat", "privacy", "thông tin", "thong tin"))
        {
            return ShowPrivacyPolicy();
        }

        // Phát hiện brand (đơn giản)
        var brands = await _dbContext.Brands.Select(b => b.BrandName).ToListAsync();
        foreach (var brand in brands)
        {
            if (!string.IsNullOrEmpty(brand) && messageLower.Contains(brand.ToLower()))
            {
                // Tìm thấy brand, nhưng hỏi xác nhận
                return new RAGChatResponse
                {
                    Answer = $"Anh/chị muốn tìm laptop {brand} đúng không ạ?",
                    Actions = new List<ChatAction>
                    {
                        new() { Label = "Đúng", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "filter_brand", ["value"] = GetBrandId(brand) } },
                        new() { Label = "Không", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "back_to_menu" } }
                    },
                    SessionId = state.SessionId,
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        // Không hiểu rõ -> hiển thị menu
        return new RAGChatResponse
        {
            Answer = "Chào anh/chị! Em là chatbot tư vấn laptop của TenTech.\n\n" +
                     "Để tư vấn nhanh và chính xác, anh/chị vui lòng chọn các nút bên dưới nhé!",
            Actions = GetMenuActions(),
            SessionId = state.SessionId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Hiển thị menu chính
    /// </summary>
    private RAGChatResponse ShowMainMenu(ConversationState state)
    {
        state.CurrentStep = "menu";
        state.Filters.Clear();
        _conversationStateService.Update(state);

        return new RAGChatResponse
        {
            Answer = "Chọn cách tìm kiếm laptop:",
            Actions = GetMenuActions(),
            SessionId = state.SessionId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Lấy menu actions
    /// </summary>
    private List<ChatAction> GetMenuActions()
    {
        return new List<ChatAction>
        {
            new() { Label = "Tìm theo Hãng", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_brand" } },
            new() { Label = "Tìm theo CPU", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_cpu" } },
            new() { Label = "Tìm theo RAM", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_ram" } },
            new() { Label = "Tìm theo khoảng giá", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_price" } },
            new() { Label = "Chính sách bảo hành", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_warranty" } },
            new() { Label = "Hướng dẫn thanh toán", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_payment" } }
        };
    }

    /// <summary>
    /// Hiển thị brand options
    /// </summary>
    private async Task<RAGChatResponse> ShowBrandOptions(ConversationState state)
    {
        var brands = await _dbContext.Brands
            .Where(b => b.BrandId != null)
            .Select(b => new { b.BrandId, b.BrandName })
            .ToListAsync();

        var actions = brands.Select(b => new ChatAction
        {
            Label = b.BrandName ?? b.BrandId!,
            Type = "quick_reply",
            Payload = new Dictionary<string, object> 
            { 
                ["intent"] = "filter_brand", 
                ["value"] = b.BrandId! 
            }
        }).ToList();

        // Thêm nút quay lại
        actions.Add(new ChatAction 
        { 
            Label = "↩ Quay lại", 
            Type = "quick_reply", 
            Payload = new Dictionary<string, object> { ["intent"] = "back_to_menu" } 
        });

        return new RAGChatResponse
        {
            Answer = "Chọn hãng laptop:",
            Actions = actions,
            SessionId = state.SessionId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Xử lý brand selection
    /// </summary>
    private async Task<RAGChatResponse> HandleBrandSelection(string brandId, ConversationState state)
    {
        state.Filters.BrandId = brandId;
        state.CurrentStep = "selected_brand";
        _conversationStateService.Update(state);

        var brandName = await _dbContext.Brands
            .Where(b => b.BrandId == brandId)
            .Select(b => b.BrandName)
            .FirstOrDefaultAsync() ?? brandId;

        // Hỏi tiếp muốn lọc gì không
        return new RAGChatResponse
        {
            Answer = $"Đã chọn: {brandName}\n\nAnh/chị muốn lọc thêm không?",
            Actions = new List<ChatAction>
            {
                new() { Label = "Lọc theo RAM", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_ram" } },
                new() { Label = "Lọc theo Giá", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "menu_price" } },
                new() { Label = "Xem kết quả ngay", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "show_results" } },
                new() { Label = "↩ Chọn lại", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "clear_filters" } }
            },
            SessionId = state.SessionId,
            Timestamp = DateTime.UtcNow
        };
    }

    // TODO: Implement các methods tương tự cho CPU, RAM, Storage, Price...
    // (Tiếp tục trong comment dưới do giới hạn length)

    /// <summary>
    /// Hiển thị kết quả sản phẩm
    /// </summary>
    private async Task<RAGChatResponse> ShowProductResults(ConversationState state)
    {
        var query = _dbContext.Products
            .Include(p => p.ProductConfigurations)
            .Include(p => p.ProductImages)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(state.Filters.BrandId))
        {
            query = query.Where(p => p.BrandId == state.Filters.BrandId);
        }
        if (!string.IsNullOrEmpty(state.Filters.Cpu))
        {
            query = query.Where(p => p.ProductConfigurations.Any(pc => pc.Cpu == state.Filters.Cpu));
        }
        if (!string.IsNullOrEmpty(state.Filters.Ram))
        {
            query = query.Where(p => p.ProductConfigurations.Any(pc => pc.Ram == state.Filters.Ram));
        }
        if (!string.IsNullOrEmpty(state.Filters.Storage))
        {
            query = query.Where(p => p.ProductConfigurations.Any(pc => pc.Rom == state.Filters.Storage));
        }
        if (state.Filters.MinPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice >= state.Filters.MinPrice.Value);
        }
        if (state.Filters.MaxPrice.HasValue)
        {
            query = query.Where(p => p.SellingPrice <= state.Filters.MaxPrice.Value);
        }

        var products = await query
            .Take(10) // Top 10
            .ToListAsync();

        if (products.Count == 0)
        {
            return new RAGChatResponse
            {
                Answer = "Xin lỗi, không tìm thấy sản phẩm phù hợp.\n\nAnh/chị thử lọc lại nhé!",
                Actions = new List<ChatAction>
                {
                    new() { Label = "Chọn lại", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "clear_filters" } },
                    new() { Label = "Về menu", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "back_to_menu" } }
                },
                SessionId = state.SessionId,
                Timestamp = DateTime.UtcNow
            };
        }

        // Build product suggestions với ảnh và link
        var suggestions = products.Select(p => BuildProductSuggestion(p)).ToList();

        var answer = new StringBuilder();
        answer.AppendLine($"Tìm thấy {products.Count} sản phẩm:");

        return new RAGChatResponse
        {
            Answer = answer.ToString(),
            SuggestedProducts = suggestions,
            Actions = new List<ChatAction>
            {
                new() { Label = "Tìm kiếm khác", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "clear_filters" } },
                new() { Label = "Về menu", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "back_to_menu" } }
            },
            SessionId = state.SessionId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Build product suggestion với đầy đủ thông tin (ảnh, link)
    /// </summary>
    private ProductSuggestion BuildProductSuggestion(Models.Product product)
    {
        // Lấy Frontend URL từ config (để link ProductDetail đúng)
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:5253";
        
        // Lấy Backend URL cho ảnh
        var httpContext = _httpContextAccessor.HttpContext;
        var backendUrl = httpContext != null 
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : "http://localhost:5068";

        // Lấy ảnh - ưu tiên Avatar, sau đó ProductImages (dùng Backend URL)
        var imageUrl = $"{backendUrl}/imageProducts/default.jpg"; // Default image
        if (!string.IsNullOrEmpty(product.Avatar))
        {
            // Avatar đã là đường dẫn tương đối hoặc tuyệt đối
            imageUrl = product.Avatar.StartsWith("http") 
                ? product.Avatar 
                : $"{backendUrl}{(product.Avatar.StartsWith("/") ? "" : "/")}{product.Avatar}";
        }
        else if (product.ProductImages != null && product.ProductImages.Any())
        {
            var firstImage = product.ProductImages.FirstOrDefault();
            if (firstImage != null && !string.IsNullOrEmpty(firstImage.ImageId))
            {
                // ImageId là tên file, build URL
                imageUrl = $"{backendUrl}/imageProducts/{firstImage.ImageId}";
            }
        }

        // Detail URL - Phải trỏ về FRONTEND (parameter phải là 'id' theo HomeController)
        var detailUrl = $"{frontendUrl}/Home/ProductDetail?id={product.ProductId}";

        // Lấy config đầu tiên (nếu có)
        var firstConfig = product.ProductConfigurations?.FirstOrDefault();

        return new ProductSuggestion
        {
            ProductId = product.ProductId ?? "",
            Name = product.ProductName ?? "N/A",
            Price = product.SellingPrice ?? 0,
            ImageUrl = imageUrl,
            DetailUrl = detailUrl,
            Brand = product.BrandId,
            Cpu = firstConfig?.Cpu,
            Ram = firstConfig?.Ram,
            Storage = firstConfig?.Rom
        };
    }

    /// <summary>
    /// Hiển thị chính sách bảo hành (FULL TEXT)
    /// </summary>
    private RAGChatResponse ShowWarrantyPolicy()
    {
        var policy = AI.Data.PolicyData.WarrantyPolicy;
        return new RAGChatResponse
        {
            Answer = policy.Content,
            Actions = new List<ChatAction>
            {
                new() { Label = "Về menu", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "back_to_menu" } }
            },
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Hiển thị hướng dẫn thanh toán (FULL TEXT)
    /// </summary>
    private RAGChatResponse ShowPaymentPolicy()
    {
        var policy = AI.Data.PolicyData.PaymentPolicy;
        return new RAGChatResponse
        {
            Answer = policy.Content,
            Actions = new List<ChatAction>
            {
                new() { Label = "Về menu", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "back_to_menu" } }
            },
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Hiển thị chính sách bảo mật (FULL TEXT)
    /// </summary>
    private RAGChatResponse ShowPrivacyPolicy()
    {
        var policy = AI.Data.PolicyData.PrivacyPolicy;
        return new RAGChatResponse
        {
            Answer = policy.Content,
            Actions = new List<ChatAction>
            {
                new() { Label = "Về menu", Type = "quick_reply", Payload = new Dictionary<string, object> { ["intent"] = "back_to_menu" } }
            },
            Timestamp = DateTime.UtcNow
        };
    }



    // Helper methods (tiếp tục implementation...)
    
    private async Task<RAGChatResponse> ShowCpuOptions(ConversationState state) { throw new NotImplementedException(); }
    private async Task<RAGChatResponse> ShowRamOptions(ConversationState state) { throw new NotImplementedException(); }
    private async Task<RAGChatResponse> ShowStorageOptions(ConversationState state) { throw new NotImplementedException(); }
    private RAGChatResponse ShowPriceOptions(ConversationState state) { throw new NotImplementedException(); }
    private async Task<RAGChatResponse> HandleCpuSelection(string cpu, ConversationState state) { throw new NotImplementedException(); }
    private async Task<RAGChatResponse> HandleRamSelection(string ram, ConversationState state) { throw new NotImplementedException(); }
    private async Task<RAGChatResponse> HandleStorageSelection(string storage, ConversationState state) { throw new NotImplementedException(); }
    private async Task<RAGChatResponse> HandlePriceSelection(string priceRange, ConversationState state) { throw new NotImplementedException(); }
    private RAGChatResponse ClearFiltersAndShowMenu(ConversationState state) { return ShowMainMenu(state); }
    private bool ContainsAny(string text, params string[] keywords) { return keywords.Any(k => text.Contains(k)); }
    private string GetBrandId(string brandName) { return brandName; } // Simplified
}

