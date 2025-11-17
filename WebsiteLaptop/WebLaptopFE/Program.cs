using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Thêm HttpClient để gọi API ngoài (ví dụ tinhthanhpho)
builder.Services.AddHttpClient("TinhThanhPhoClient", client =>
{
    client.BaseAddress = new Uri("https://www.tinhthanhpho.com/");
    // Nếu cần header mặc định nào, set ở đây
});

// Thêm CORS — cho phép frontend (có thể chỉnh domain theo thực tế)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
          .WithOrigins("http://localhost:3000", "https://your-frontend-domain.com") // sửa lại origin frontend
          .AllowAnyHeader()
          .AllowAnyMethod();
    });
});

// Thêm controller + views (MVC)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Cấu hình pipeline middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Áp dụng CORS
app.UseCors("AllowFrontend");

app.UseRouting();

app.UseAuthorization();

// Map route controller MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Ví dụ: tạo endpoint API nội bộ để lấy danh sách tỉnh mới từ tinhthanhpho
app.MapGet("/api/new-provinces", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("TinhThanhPhoClient");

    // Gọi API tinhthanhpho — nếu cần thêm header như Authorization thì cấu hình thêm
    var response = await client.GetAsync("api/v1/new-provinces");

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem($"Error calling external API: {response.StatusCode}");
    }

    var content = await response.Content.ReadAsStringAsync();
    return Results.Content(content, "application/json");
});

app.Run();