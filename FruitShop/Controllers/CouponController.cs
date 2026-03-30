using FruitShop.Filters;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    [RequireRole("Admin", "Staff")]
    public class CouponController : Controller
    {
        private readonly CouponRepository _couponRepo;

        public CouponController(CouponRepository couponRepo)
        {
            _couponRepo = couponRepo;
        }

        public IActionResult Index()
        {
            var coupons = _couponRepo.GetAll();
            return View(coupons);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(string code, int discountPercent, DateTime expiryDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code) || discountPercent < 1 || discountPercent > 100)
                {
                    TempData["Error"] = "Dữ liệu không hợp lệ!";
                    return RedirectToAction(nameof(Index));
                }

                var coupon = new Coupon
                {
                    Code = code.Trim().ToUpper(),
                    DiscountPercent = discountPercent,
                    ExpiryDate = expiryDate,
                    IsActive = true
                };

                _couponRepo.Insert(coupon);
                TempData["Success"] = "Thêm mã giảm giá thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id, bool isActive)
        {
            _couponRepo.ToggleActive(id, isActive);
            TempData["Success"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
