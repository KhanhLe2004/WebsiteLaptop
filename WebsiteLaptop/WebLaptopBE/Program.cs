using Microsoft.EntityFrameworkCore;
using WebLaptopBE.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Đăng ký DbContext
builder.Services.AddDbContext<Testlaptop33Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=DESKTOP-GDN4V8P;Initial Catalog=testlaptop20;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False"));

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

// Đăng ký NotificationService như singleton
builder.Services.AddSingleton<WebLaptopBE.Services.NotificationService>();

// Đăng ký EmailService như singleton
builder.Services.AddSingleton<WebLaptopBE.Services.EmailService>();

// Đăng ký VnPayService
builder.Services.AddScoped<WebLaptopBE.Services.IVnPayService, WebLaptopBE.Services.VnPayService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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
