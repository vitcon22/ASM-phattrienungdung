using Microsoft.AspNetCore.Mvc;

namespace FruitShop.ViewComponents;

/// <summary>
/// ViewComponent hiển thị thông báo Alert (Success/Error) - tách khỏi mỗi View
/// </summary>
public class AlertViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View(new AlertModel
        {
            Success = TempData["Success"]?.ToString(),
            Error   = TempData["Error"]?.ToString()
        });
    }
}

public class AlertModel
{
    public string? Success { get; set; }
    public string? Error { get; set; }
    public bool HasSuccess => !string.IsNullOrEmpty(Success);
    public bool HasError   => !string.IsNullOrEmpty(Error);
}
