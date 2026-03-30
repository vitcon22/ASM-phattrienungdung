using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Middleware;
using FruitShop.Models.DAL;

var builder = WebApplication.CreateBuilder(args);

/* =====================================================
   ĐĂNG KÝ SERVICES
   ===================================================== */

// MVC với Razor Views
builder.Services.AddControllersWithViews();

// Session (dùng cho giỏ hàng + xác thực)
builder.Services.AddDistributedMemoryCache(); // Cache trong bộ nhớ
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(2);  // Session tồn tại 2 giờ
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite    = SameSiteMode.Strict;
});

// IHttpContextAccessor (cần trong Razor views để truy cập Session)
builder.Services.AddHttpContextAccessor();

// ===== ĐĂNG KÝ DATABASE CONTEXT =====
builder.Services.AddSingleton<FruitShopContext>();

// ===== ĐĂNG KÝ REPOSITORIES (Scoped - 1 instance / request) =====
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<FruitRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<ReviewRepository>();
builder.Services.AddScoped<CouponRepository>();
builder.Services.AddScoped<WishlistRepository>();
builder.Services.AddScoped<InventoryLogRepository>();

// ===== ĐĂNG KÝ HELPERS =====
builder.Services.AddScoped<ValidationHelper>();

/* =====================================================
   BUILD APP
   ===================================================== */
var app = builder.Build();

// Middleware mặc định
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Phục vụ wwwroot (ảnh, CSS, JS)

app.UseRouting();

// Session TRƯỚC AuthMiddleware
app.UseSession();

// Custom AuthMiddleware - kiểm tra session trước các route
app.UseMiddleware<AuthMiddleware>();

app.UseAuthorization();

/* =====================================================
   ROUTING
   ===================================================== */
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
