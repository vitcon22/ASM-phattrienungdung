using FruitShop.Helpers;
using FruitShop.Models.DAL;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    public class WishlistController : Controller
    {
        private readonly WishlistRepository _wishlistRepo;

        public WishlistController(WishlistRepository wishlistRepo)
        {
            _wishlistRepo = wishlistRepo;
        }

        // GET: /Wishlist — Trang danh sách yêu thích (RQ67)
        public IActionResult Index()
        {
            int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var items = _wishlistRepo.GetByUser(userId.Value).ToList();
            return View(items);
        }

        // POST: /Wishlist/Toggle
        [HttpPost]
        public IActionResult Toggle(int fruitId)
        {
            int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
            if (userId == null)
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập để lưu sản phẩm yêu thích." });
            }

            _wishlistRepo.Toggle(userId.Value, fruitId);
            bool isInWishlist = _wishlistRepo.IsInWishlist(userId.Value, fruitId);

            return Json(new { success = true, isInWishlist = isInWishlist, message = isInWishlist ? "Đã thêm vào yêu thích" : "Đã bỏ yêu thích" });
        }
    }
}
