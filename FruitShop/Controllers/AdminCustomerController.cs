using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers;

/// <summary>
/// Redirect tất cả request sang UserController (tránh duplicate code)
/// </summary>
public class AdminCustomerController : Controller
{
    public IActionResult Index(string? keyword, int page = 1)
        => RedirectToAction("Customers", "User", new { keyword, page });

    public IActionResult Details(int id)
        => RedirectToAction("Details", "User", new { id });

    [HttpPost]
    public IActionResult ToggleActive(int id)
        => RedirectToAction("ToggleActive", "User", new { id });
}
