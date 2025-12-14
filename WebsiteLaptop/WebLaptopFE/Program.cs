var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add HttpClient
builder.Services.AddHttpClient();

// Add Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Use Session - phải đặt trước UseAuthorization
app.UseSession();

app.UseAuthorization();

// Route riêng cho SignIn với action mặc định là Index
//app.MapControllerRoute(
//    name: "adminSignIn",
//    pattern: "Admin/SignIn/{action=Index}/{id?}",
//    defaults: new { area = "Admin", controller = "SignIn", action = "Index" });

// Route cho Area Admin (phải đặt trước default route)
// Khi truy cập /Admin sẽ tự động đi đến Home/Index, sau đó redirect đến SignIn nếu chưa đăng nhập
app.MapControllerRoute(
    name: "areas",
     pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
