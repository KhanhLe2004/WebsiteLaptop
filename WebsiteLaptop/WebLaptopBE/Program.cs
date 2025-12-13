using Microsoft.EntityFrameworkCore;
using WebLaptopBE.AI.Orchestrator;
using WebLaptopBE.AI.Plugins;
using WebLaptopBE.AI.SemanticKernel;
using WebLaptopBE.Data;
using WebLaptopBE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Đăng ký DbContext
builder.Services.AddDbContext<Testlaptop38Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=DESKTOP-48K2JPN\\SQLEXPRESS;Initial Catalog=testlaptop38;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Sử dụng camelCase cho JSON (mặc định của ASP.NET Core)
        // BrandId sẽ được serialize thành "brandId" trong JSON
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Không bỏ qua null values - đảm bảo BrandId (ngay cả khi null) vẫn được trả về
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
        // Tránh circular reference khi serialize
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add HttpClientFactory
builder.Services.AddHttpClient();

// Add HttpContextAccessor - Cần thiết cho RAGChatService và GuidedChatService
builder.Services.AddHttpContextAccessor();

// Add Memory Cache for performance optimization (caching embeddings, responses)
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Limit cache size
});

// Add CORS - Improved configuration for development and production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // In development, allow all origins
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Content-Type", "Content-Length");
        }
        else
        {
            // In production, specify allowed origins
            policy.WithOrigins(
                    "https://localhost:5001",
                    "https://localhost:5000",
                    "http://localhost:5001",
                    "http://localhost:5000"
                  )
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials()
                  .WithExposedHeaders("Content-Type", "Content-Length");
        }
    });
});

// ============================================
// ĐĂNG KÝ SERVICES CHO AI CHATBOT
// ============================================

// 1. Product Service - Tìm kiếm sản phẩm từ SQL
builder.Services.AddScoped<IProductService, ProductService>();

// 2. Qdrant Service (cũ - cho policies)
builder.Services.AddScoped<IQdrantService, QdrantService>();

// 3. Qdrant Vector Service (mới - cho products + policies với RAG)
builder.Services.AddScoped<IQdrantVectorService, QdrantVectorService>();

// 4. Semantic Kernel Service - Quản lý LLM
builder.Services.AddSingleton<ISemanticKernelService, SemanticKernelService>();

// 5. RAG Chat Service - RAG pipeline hoàn chỉnh
builder.Services.AddScoped<IRAGChatService, RAGChatService>();

// 6. Indexing Service - Index dữ liệu từ SQL → Qdrant
builder.Services.AddScoped<IIndexingService, IndexingService>();

// 7. Chat Orchestrator Service - Điều phối chatbot (cũ - vẫn giữ để tương thích)
builder.Services.AddScoped<IChatOrchestratorService, ChatOrchestratorService>();

// 8. Plugins - Đăng ký các plugins cho Semantic Kernel
builder.Services.AddScoped<ProductSearchPlugin>();
builder.Services.AddScoped<PolicyRetrievalPlugin>();
builder.Services.AddScoped<IntentDetectionPlugin>();

// 9. Input Validation Service - Validate input từ người dùng
builder.Services.AddScoped<WebLaptopBE.AI.Services.IInputValidationService, WebLaptopBE.AI.Services.InputValidationService>();

// 10. Conversation State Service - Quản lý state của conversation (guided chat)
builder.Services.AddSingleton<IConversationStateService, ConversationStateService>();

// 11. Guided Chat Service - Chatbot với guided conversation (button options)
builder.Services.AddScoped<IGuidedChatService, GuidedChatService>();

builder.Services.AddScoped<IEnhancedProductService, EnhancedProductService>();

var app = builder.Build();

// Đảm bảo thư mục wwwroot/image tồn tại
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
var imagePath = Path.Combine(wwwrootPath, "image");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
    Console.WriteLine($"Created directory: {wwwrootPath}");
}
if (!Directory.Exists(imagePath))
{
    Directory.CreateDirectory(imagePath);
    Console.WriteLine($"Created directory: {imagePath}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use CORS - must be before UseStaticFiles và UseAuthorization
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Enable static files để serve ảnh từ wwwroot/image
// UseStaticFiles() mặc định serve từ wwwroot, nên /image/... sẽ tìm file trong wwwroot/image/...
app.UseStaticFiles();

Console.WriteLine($"Static files serving from: {wwwrootPath}");
Console.WriteLine($"Image files serving from: {imagePath}");
Console.WriteLine($"Image URL pattern: http://localhost:5068/image/{{filename}}");

app.UseAuthorization();

app.MapControllers();

app.Run();
