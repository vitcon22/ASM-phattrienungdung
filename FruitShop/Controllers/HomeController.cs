using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    /// <summary>
    /// Controller trang chủ dành cho Customer
    /// </summary>
    public class HomeController : Controller
    {
        private readonly FruitRepository _fruitRepo;
        private readonly CategoryRepository _categoryRepo;

        public HomeController(FruitRepository fruitRepo, CategoryRepository categoryRepo)
        {
            _fruitRepo    = fruitRepo;
            _categoryRepo = categoryRepo;
        }

        // GET: / or /Home
        public IActionResult Index(string? keyword, int? categoryId)
        {
            var fruits     = _fruitRepo.GetAllActive();
            var categories = _categoryRepo.GetAll();

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrWhiteSpace(keyword))
                fruits = fruits.Where(f =>
                    f.FruitName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    (f.Description ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase));

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId > 0)
                fruits = fruits.Where(f => f.CategoryId == categoryId);

            ViewBag.Categories  = categories;
            ViewBag.Keyword     = keyword;
            ViewBag.CategoryId  = categoryId;

            // Số lượng giỏ hàng
            var cart = SessionHelper.GetObject<List<CartItemViewModel>>(HttpContext.Session, SessionHelper.CartKey);
            ViewBag.CartCount = cart?.Sum(x => x.Quantity) ?? 0;

            return View(fruits.ToList());
        }

        // GET: /Home/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: /Home/Error
        public IActionResult Error()
        {
            return View();
        }
    }
}
