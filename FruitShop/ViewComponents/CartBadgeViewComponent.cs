using Microsoft.AspNetCore.Mvc;

namespace FruitShop.ViewComponents;

/// <summary>
/// ViewComponent hiển thị số badge giỏ hàng - thay hardcoded trong layout
/// </summary>
public class CartBadgeViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var cartCount = HttpContext.Session.GetString("Cart");
        int count = 0;
        if (!string.IsNullOrEmpty(cartCount))
        {
            try
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(cartCount);
                if (items != null)
                    count = items.Sum(x => (int?)x?.GetValue("Quantity") ?? 0);
            }
            catch { count = 0; }
        }
        return View(count);
    }
}
