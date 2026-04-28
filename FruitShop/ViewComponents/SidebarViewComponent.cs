using Microsoft.AspNetCore.Mvc;

namespace FruitShop.ViewComponents;

/// <summary>
/// ViewComponent hiển thị sidebar Admin - tách khỏi _Layout.cshtml
/// </summary>
public class SidebarViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var role = HttpContext.Session.GetString("UserRole") ?? "";
        var userName = HttpContext.Session.GetString("UserName") ?? "Người dùng";
        ViewBag.Role = role;
        ViewBag.UserName = userName;
        return View();
    }
}
