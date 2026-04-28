using FruitShop.Constants;
using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Infrastructure;
using FruitShop.Middleware;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Services;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

/* =====================================================
   ĐĂNG KÝ SERVICES
   ===================================================== */

// MVC với Razor Views và Global Filters
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AuditLogFilterAttribute>();
    options.Filters.Add<ExceptionHandlerFilter>();
});

// Session (dùng cho giỏ hàng + xác thực)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite    = SameSiteMode.Strict;
});

// IHttpContextAccessor
builder.Services.AddHttpContextAccessor();

// ===== DATABASE CONTEXT =====
builder.Services.AddSingleton<FruitShopContext>();

// ===== REPOSITORIES (Scoped - 1 instance / request) =====
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<FruitRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<ReviewRepository>();
builder.Services.AddScoped<CouponRepository>();
builder.Services.AddScoped<WishlistRepository>();
builder.Services.AddScoped<InventoryLogRepository>();
builder.Services.AddScoped<SupplierRepository>();
builder.Services.AddScoped<BatchRepository>();
builder.Services.AddScoped<AuditLogRepository>();
builder.Services.AddScoped<OperatingCostRepository>();

// ===== SERVICE LAYER (mới - clean architecture) =====
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<EmailTemplateService>();

// ===== VALIDATION HELPER =====
builder.Services.AddScoped<ValidationHelper>();

// ===== AUTHENTICATION (OAuth) =====
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = AppConstants.Auth.CookieScheme;
})
.AddCookie(AppConstants.Auth.CookieScheme, options =>
{
    options.LoginPath    = AppConstants.Auth.LoginPath;
    options.LogoutPath   = AppConstants.Auth.LogoutPath;
})
.AddGoogle(options =>
{
    options.ClientId     = builder.Configuration["Authentication:Google:ClientId"] ?? "PLACEHOLDER";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "PLACEHOLDER";
})
.AddFacebook(options =>
{
    options.AppId     = builder.Configuration["Authentication:Facebook:AppId"] ?? "PLACEHOLDER";
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "PLACEHOLDER";
});

/* =====================================================
   BUILD APP
   ===================================================== */
var app = builder.Build();

// EPPlus license
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// Session TRƯỚC AuthMiddleware
app.UseSession();

// Global Exception Handler - bắt tất cả exception
app.UseMiddleware<GlobalExceptionMiddleware>();

// Custom AuthMiddleware - kiểm tra session
app.UseMiddleware<AuthMiddleware>();

app.UseAuthorization();

/* =====================================================
   ROUTING
   ===================================================== */
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
