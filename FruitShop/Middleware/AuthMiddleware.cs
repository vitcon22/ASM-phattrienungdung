using FruitShop.Helpers;

namespace FruitShop.Middleware
{
    /// <summary>
    /// Middleware kiểm tra xác thực session cho mỗi request
    /// Các đường dẫn không cần đăng nhập: /Account/Login, /Account/Register, /Home/AccessDenied
    /// </summary>
    public class AuthMiddleware
    {
        private readonly RequestDelegate _next;

        // Các đường dẫn công khai không cần đăng nhập
        private static readonly string[] PublicPaths = new[]
        {
            "/account/login",
            "/account/register",
            "/account/logout",
            "/home/accessdenied",
            "/home/index",
            "/home",
            "/"
        };

        public AuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";

            // Cho phép truy cập file tĩnh (wwwroot)
            if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images") || path.StartsWith("/lib"))
            {
                await _next(context);
                return;
            }

            // Cho phép các đường dẫn công khai
            bool isPublic = PublicPaths.Any(p => path == p || path.StartsWith(p + "/"));
            if (isPublic)
            {
                await _next(context);
                return;
            }

            // Kiểm tra session
            if (!SessionHelper.IsLoggedIn(context.Session))
            {
                context.Response.Redirect("/Account/Login");
                return;
            }

            await _next(context);
        }
    }
}
