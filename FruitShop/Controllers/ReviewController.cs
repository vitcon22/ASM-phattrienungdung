using FruitShop.Constants;
using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ReviewRepository _reviewRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ReviewController(ReviewRepository reviewRepo, IWebHostEnvironment env, IConfiguration config)
        {
            _reviewRepo = reviewRepo;
            _env        = env;
            _config     = config;
        }

        // POST: /Review/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int fruitId, int rating, string? comment, IFormFileCollection? images)
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
                FruitId    = fruitId,
                UserId     = userId.Value,
                Rating     = rating,
                Comment    = comment,
                IsApproved = true // mặc định auto-approve
            };

            int reviewId = _reviewRepo.Insert(review);

            if (images != null && images.Count > 0)
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "images/reviews");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                foreach (var file in images.Take(3))
                {
                    if (file.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        string filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        _reviewRepo.InsertImage(reviewId, fileName);
                    }
                }
            }

            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Details", "Fruit", new { id = fruitId });
        }

        // GET: /Review/Edit/5 — Sửa đánh giá của chính mình
        [RequireRole("Customer", "Admin", "Staff")]
        public IActionResult Edit(int id)
        {
            var review = _reviewRepo.GetById(id);
            if (review == null) return NotFound();

            int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
            string? role = HttpContext.Session.GetString(SessionHelper.UserRoleKey);
            if (review.UserId != userId && role != "Admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View(review);
        }

        // POST: /Review/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Customer", "Admin", "Staff")]
        public async Task<IActionResult> Edit(int id, int rating, string? comment, IFormFileCollection? images)
        {
            var review = _reviewRepo.GetById(id);
            if (review == null) return NotFound();

            int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
            string? role = HttpContext.Session.GetString(SessionHelper.UserRoleKey);
            if (review.UserId != userId && role != "Admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (rating < 1 || rating > 5) rating = 5;

            review.Rating = rating;
            review.Comment = comment;
            _reviewRepo.Update(review);

            if (images != null && images.Count > 0)
            {
                _reviewRepo.DeleteImages(id);
                string uploadDir = Path.Combine(_env.WebRootPath, "images/reviews");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                foreach (var file in images.Take(3))
                {
                    if (file.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        string filePath = Path.Combine(uploadDir, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        _reviewRepo.InsertImage(id, fileName);
                    }
                }
            }

            TempData["Success"] = "Đã cập nhật đánh giá!";
            return RedirectToAction("Details", "Fruit", new { id = review.FruitId });
        }

        // POST: /Review/Delete/5 — Xóa đánh giá của chính mình
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Customer", "Admin", "Staff")]
        public IActionResult Delete(int id)
        {
            var review = _reviewRepo.GetById(id);
            if (review == null) return NotFound();

            int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);
            string? role = HttpContext.Session.GetString(SessionHelper.UserRoleKey);
            if (review.UserId != userId && role != "Admin")
            {
                TempData["Error"] = "Bạn không có quyền xóa đánh giá này.";
                return RedirectToAction("AccessDenied", "Home");
            }

            _reviewRepo.Delete(id);
            TempData["Success"] = "Đã xóa đánh giá!";
            return RedirectToAction("Details", "Fruit", new { id = review.FruitId });
        }

        // ============================================================
        // ADMIN: Quản lý đánh giá (RQ64 - duyệt/reject)
        // ============================================================

        [RequireRole("Admin")]
        public IActionResult AdminIndex(string? keyword, string? status, int page = 1)
        {
            int pageSize = _config.GetValue("AppSettings:ItemsPerPage", 15);
            var items = _reviewRepo.GetAll(page, pageSize, keyword, status).ToList();
            int total = _reviewRepo.CountAll(keyword, status);
            var paged = PaginationHelper.Create(items, total, page, pageSize);

            ViewBag.Keyword = keyword;
            ViewBag.Status  = status;
            return View(paged);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        public IActionResult Approve(int id)
        {
            _reviewRepo.Approve(id);
            TempData["Success"] = "Đã duyệt đánh giá!";
            return RedirectToAction(nameof(AdminIndex));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        public IActionResult Reject(int id)
        {
            _reviewRepo.Reject(id);
            TempData["Success"] = "Đã từ chối đánh giá!";
            return RedirectToAction(nameof(AdminIndex));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        public IActionResult DeleteAdmin(int id)
        {
            var review = _reviewRepo.GetById(id);
            if (review == null) return NotFound();
            _reviewRepo.Delete(id);
            TempData["Success"] = "Đã xóa đánh giá!";
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
