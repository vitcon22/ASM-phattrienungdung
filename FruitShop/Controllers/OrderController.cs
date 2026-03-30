using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    /// <summary>
    /// Controller quản lý đơn hàng:
    /// - Customer: giỏ hàng (Session), đặt hàng, lịch sử, huỷ
    /// - Staff/Admin: xem tất cả, cập nhật trạng thái, xem chi tiết
    /// </summary>
    public class OrderController : Controller
    {
        private readonly OrderRepository _orderRepo;
        private readonly FruitRepository _fruitRepo;
        private readonly ValidationHelper _validationHelper;
        private readonly IConfiguration _config;
        private readonly CouponRepository _couponRepo;

        public OrderController(
            OrderRepository orderRepo,
            FruitRepository fruitRepo,
            ValidationHelper validationHelper,
            IConfiguration config,
            CouponRepository couponRepo)
        {
            _orderRepo        = orderRepo;
            _fruitRepo        = fruitRepo;
            _validationHelper = validationHelper;
            _config           = config;
            _couponRepo       = couponRepo;
        }

        // ============================================================
        // CUSTOMER - GIỎ HÀNG
        // ============================================================

        // GET: /Order/Cart
        public IActionResult Cart()
        {
            var cart = GetCart();
            ViewBag.CartCount = cart.Sum(x => x.Quantity);
            return View(cart);
        }

        // POST: /Order/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int fruitId, int quantity = 1)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account",
                    new { returnUrl = Url.Action("Details", "Fruit", new { id = fruitId }) });

            var fruit = _fruitRepo.GetById(fruitId);
            if (fruit == null || !fruit.IsActive)
            {
                TempData["Error"] = "Sản phẩm không tồn tại!";
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra tồn kho
            if (quantity < 1) quantity = 1;
            if (quantity > fruit.StockQuantity)
            {
                TempData["Error"] = $"Chỉ còn {fruit.StockQuantity} {fruit.Unit} trong kho!";
                return RedirectToAction("Details", "Fruit", new { id = fruitId });
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.FruitId == fruitId);
            if (existingItem != null)
            {
                // Đã có trong giỏ → tăng số lượng
                int newQty = existingItem.Quantity + quantity;
                if (newQty > fruit.StockQuantity) newQty = fruit.StockQuantity;
                existingItem.Quantity      = newQty;
                existingItem.StockQuantity = fruit.StockQuantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    FruitId       = fruit.FruitId,
                    FruitName     = fruit.FruitName,
                    UnitPrice     = fruit.Price,
                    Quantity      = quantity,
                    ImageUrl      = fruit.GetImageUrl(),
                    Unit          = fruit.Unit,
                    StockQuantity = fruit.StockQuantity
                });
            }

            SaveCart(cart);
            TempData["Success"] = $"Đã thêm {fruit.FruitName} vào giỏ hàng!";
            return RedirectToAction(nameof(Cart));
        }

        // POST: /Order/UpdateCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCart(int fruitId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.FruitId == fruitId);
            if (item != null)
            {
                if (quantity <= 0)
                    cart.Remove(item);
                else
                {
                    // Validate tồn kho
                    if (quantity > item.StockQuantity)
                        quantity = item.StockQuantity;
                    item.Quantity = quantity;
                }
            }
            SaveCart(cart);
            return RedirectToAction(nameof(Cart));
        }

        // POST: /Order/RemoveFromCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int fruitId)
        {
            var cart = GetCart();
            cart.RemoveAll(x => x.FruitId == fruitId);
            SaveCart(cart);
            TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
            return RedirectToAction(nameof(Cart));
        }

        // POST: /Order/ApplyCoupon
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyCoupon(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Json(new { success = false, message = "Vui lòng nhập mã." });
            var coupon = _couponRepo.GetByCode(code.Trim().ToUpper());
            if (coupon == null) 
                return Json(new { success = false, message = "Mã không hợp lệ hoặc đã hết hạn." });

            var cart = GetCart();
            decimal total = cart.Sum(x => x.Subtotal);
            decimal discount = total * coupon.DiscountPercent / 100;
            return Json(new { 
                success = true, 
                code = coupon.Code,
                percent = coupon.DiscountPercent,
                discountAmount = discount,
                finalAmount = total - discount
            });
        }

        // GET: /Order/Checkout
        [RequireRole("Customer")]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction(nameof(Cart));
            }

            // Pre-fill địa chỉ của user
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var model = new OrderViewModel
            {
                CartItems = cart
            };
            return View(model);
        }

        // POST: /Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Customer")]
        public IActionResult Checkout(OrderViewModel model)
        {
            var cart = GetCart();
            model.CartItems = cart;

            if (!ModelState.IsValid)
                return View(model);

            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống!";
                return RedirectToAction(nameof(Cart));
            }

            try
            {
                // Validate tồn kho lần cuối
                foreach (var item in cart)
                {
                    if (_validationHelper.IsQuantityExceedStock(item.FruitId, item.Quantity))
                    {
                        TempData["Error"] = $"Sản phẩm '{item.FruitName}' không đủ tồn kho!";
                        return RedirectToAction(nameof(Cart));
                    }
                }

                var userId = SessionHelper.GetUserId(HttpContext.Session);
                var order = new Order
                {
                    UserId          = userId,
                    Status          = "Pending",
                    ShippingAddress = model.ShippingAddress,
                    Note            = model.Note
                };

                // Tính toán Coupon
                if (!string.IsNullOrEmpty(model.CouponCode))
                {
                    var coupon = _couponRepo.GetByCode(model.CouponCode);
                    if (coupon != null && coupon.IsActive && coupon.ExpiryDate >= DateTime.Now)
                    {
                        order.CouponId = coupon.CouponId;
                        order.DiscountAmount = cart.Sum(x => x.Subtotal) * coupon.DiscountPercent / 100;
                    }
                }

                int orderId = _orderRepo.CreateOrder(order, cart);

                // Xóa giỏ hàng sau khi đặt thành công
                SaveCart(new List<CartItemViewModel>());
                TempData["Success"] = $"Đặt hàng thành công! Mã đơn #{orderId}";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi khi đặt hàng: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Order/History  (Customer xem đơn của mình)
        [RequireRole("Customer")]
        public IActionResult History()
        {
            int userId = SessionHelper.GetUserId(HttpContext.Session);
            var orders = _orderRepo.GetByCustomer(userId);
            return View(orders);
        }

        // POST: /Order/Cancel/5  (Customer huỷ đơn Pending)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Customer")]
        public IActionResult Cancel(int id)
        {
            int userId = SessionHelper.GetUserId(HttpContext.Session);
            bool success = _orderRepo.CancelOrder(id, userId);
            TempData[success ? "Success" : "Error"] = success
                ? "Đã huỷ đơn hàng thành công!"
                : "Không thể huỷ đơn này (chỉ huỷ được khi đơn đang chờ xác nhận)";
            return RedirectToAction(nameof(History));
        }

        // ============================================================
        // STAFF / ADMIN - QUẢN LÝ ĐƠN HÀNG
        // ============================================================

        // GET: /Order/Index  (Staff/Admin)
        [RequireRole("Admin", "Staff")]
        public IActionResult Index(string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int pageSize = _config.GetValue<int>("AppSettings:ItemsPerPage", 10);
            var (items, total) = _orderRepo.Search(status, fromDate, toDate, page, pageSize);
            var paged = PaginationHelper.Create(items, total, page, pageSize);

            ViewBag.Status   = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate   = toDate?.ToString("yyyy-MM-dd");
            return View(paged);
        }

        // GET: /Order/Details/5
        public IActionResult Details(int id)
        {
            var order = _orderRepo.GetById(id);
            if (order == null) return NotFound();

            // Customer chỉ được xem đơn của chính mình
            var role   = SessionHelper.GetUserRole(HttpContext.Session);
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (role == "Customer" && order.UserId != userId)
                return RedirectToAction("AccessDenied", "Home");

            // Tạo QR Code data (JSON thông tin đơn hàng)
            ViewBag.QrData = $"FruitShop|OrderId:{order.OrderId}|Total:{order.TotalAmount:N0}|Status:{order.Status}";
            ViewBag.UserRole = role;
            return View(order);
        }

        // POST: /Order/UpdateStatus  (Staff/Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin", "Staff")]
        public IActionResult UpdateStatus(int id, string status)
        {
            try
            {
                int staffId = SessionHelper.GetUserId(HttpContext.Session);
                _orderRepo.UpdateStatus(id, status, staffId);
                TempData["Success"] = $"Đã cập nhật trạng thái đơn #{id} thành '{status}'!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // ============================================================
        // PRIVATE HELPERS - Giỏ hàng
        // ============================================================

        private List<CartItemViewModel> GetCart()
        {
            return SessionHelper.GetObject<List<CartItemViewModel>>(
                HttpContext.Session, SessionHelper.CartKey)
                ?? new List<CartItemViewModel>();
        }

        private void SaveCart(List<CartItemViewModel> cart)
        {
            SessionHelper.SetObject(HttpContext.Session, SessionHelper.CartKey, cart);
        }
    }
}
