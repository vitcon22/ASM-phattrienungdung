using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ReviewRepository _reviewRepo;

        public ReviewController(ReviewRepository reviewRepo)
        {
            _reviewRepo = reviewRepo;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Submit(int fruitId, int rating, string? comment)
        {
            int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
            if (userId == null)
            {
                TempData["Error"] = "Bạn cần đăng nhập để đánh giá sản phẩm.";
                return RedirectToAction("Login", "Account");
            }

            if (rating < 1 || rating > 5) rating = 5;

            var review = new Review
            {
                FruitId = fruitId,
                UserId = userId.Value,
                Rating = rating,
                Comment = comment
            };

            _reviewRepo.Insert(review);

            // Bỏ cache session tạm thời nếu cần, view sẽ tự load lại từ DB
            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Details", "Fruit", new { id = fruitId });
        }
    }
}
