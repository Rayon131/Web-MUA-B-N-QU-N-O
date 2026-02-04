using AppView.Models.Service.VNPay;
using DATNN;
using DATNN.Service;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

// Program.cs
var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialPersonal("DATNN");
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";
        options.AccessDeniedPath = "/Login/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });


builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

builder.Services.AddDbContextFactory<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DATNN")));
builder.Services.AddScoped(p => p.GetRequiredService<IDbContextFactory<MyDbContext>>().CreateDbContext());

builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IVnPayService, VnPayService>();
builder.Services.AddHttpClient<GeocodingService>();
builder.Services.AddTransient<GeocodingService>();

var app = builder.Build();

// ✅ Thêm đoạn này

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
