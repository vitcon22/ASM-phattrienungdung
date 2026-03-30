using FruitShop.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FruitShop.Filters
{
    /// <summary>
    /// Custom Action Filter: Kiểm tra role của user
    /// Dùng: [RequireRole("Admin")] hoặc [RequireRole("Admin", "Staff")]
    /// </summary>
    public class RequireRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public RequireRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            // Chưa đăng nhập
            if (!SessionHelper.IsLoggedIn(session))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Không đủ quyền
            if (!SessionHelper.HasAnyRole(session, _roles))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
