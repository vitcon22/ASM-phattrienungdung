namespace FruitShop.Constants;

/// <summary>
/// Các hằng số ứng dụng - thay thế magic strings và keys rải rác trong code
/// </summary>
public static class AppConstants
{
    // === Session Keys ===
    public static class SessionKeys
    {
        public const string UserId    = "UserId";
        public const string UserName  = "UserName";
        public const string UserRole  = "UserRole";
        public const string Cart      = "Cart";
    }

    // === Role Names ===
    public static class Roles
    {
        public const string Admin     = "Admin";
        public const string Staff     = "Staff";
        public const string Customer  = "Customer";
    }

    // === Order Status ===
    public static class OrderStatuses
    {
        public const string Pending   = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Shipping  = "Shipping";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
    }

    // === Tier ===
    public static class Tiers
    {
        public const string Standard  = "Standard";
        public const string Silver   = "Silver";
        public const string Gold     = "Gold";
        public const string Platinum = "Platinum";
    }

    // === Pagination ===
    public static class Pagination
    {
        public const int DefaultPageSize   = 10;
        public const int AdminPageSize      = 15;
        public const int AuditLogPageSize   = 20;
    }

    // === Image Upload ===
    public static class ImageUpload
    {
        public const string FruitFolder   = "images/fruits";
        public const string ReviewFolder  = "images/reviews";
        public const string DefaultImage  = "default.jpg";
        public const long   MaxFileSize   = 5 * 1024 * 1024; // 5MB

        public static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };
    }

    // === Points ===
    public static class Points
    {
        public const decimal PointsPer10K = 1m; // 10,000đ = 1 điểm
        public const int SilverMinPoints  = 200;
        public const int GoldMinPoints    = 500;
        public const int PlatinumMinPoints = 1000;
    }

    // === Cache Keys ===
    public static class CacheKeys
    {
        public const string Categories   = "Categories_All";
        public const string Suppliers    = "Suppliers_Active";
        public const string Dashboard    = "Dashboard_{0}";
    }

    // === File Export ===
    public static class ExportFiles
    {
        public const string OrdersCsv      = "DanhSachDonHang_{0}.csv";
        public const string FruitsCsv      = "DanhSachSanPham_{0}.csv";
        public const string UsersCsv       = "DanhSachNguoiDung_{0}.csv";
        public const string RevenueExcel   = "BaoCaoDoanhThu_{0}_{1}.xlsx";
    }

    // === Cookie / Auth ===
    public static class Auth
    {
        public const string CookieScheme = "FruitShopCookie";
        public const string LoginPath    = "/Account/Login";
        public const string LogoutPath   = "/Account/Logout";
    }

    // === Public Paths ===
    public static class PublicPaths
    {
        public static readonly string[] AllowList = new[]
        {
            "/account/login",
            "/account/register",
            "/account/logout",
            "/account/forgotpassword",
            "/account/resetpassword",
            "/account/confirmemail",
            "/home/accessdenied",
            "/home/index",
            "/home",
            "/",
            "/fruit/details"
        };
    }

    // === Batch ===
    public static class BatchDefaults
    {
        public const int ExpiryDays    = 30;
        public const int ExpiryWarning = 7;
    }
}
