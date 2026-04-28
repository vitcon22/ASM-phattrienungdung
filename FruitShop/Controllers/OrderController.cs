using FruitShop.Constants;
using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;
using FruitShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers;

/// <summary>
/// Controller quản lý đơn hàng - đã refactor dùng Service Layer
/// Customer: giỏ hàng, đặt hàng, lịch sử, huỷ
/// Staff/Admin: xem tất cả, cập nhật trạng thái, xuất file
/// </summary>
public class OrderController : Controller
{
    private readonly OrderRepository     _orderRepo;
    private readonly FruitRepository  _fruitRepo;
    private readonly ValidationHelper  _validationHelper;
    private readonly ICartService    _cartService;
    private readonly CouponRepository _couponRepo;
    private readonly UserRepository  _userRepo;
    private readonly IExportService _exportService;
    private readonly EmailTemplateService _emailService;
    private readonly IConfiguration _config;

    public OrderController(
        OrderRepository orderRepo,
        FruitRepository fruitRepo,
        ValidationHelper validationHelper,
        ICartService cartService,
        CouponRepository couponRepo,
        UserRepository userRepo,
        IExportService exportService,
        EmailTemplateService emailService,
        IConfiguration config)
    {
        _orderRepo      = orderRepo;
        _fruitRepo      = fruitRepo;
        _validationHelper = validationHelper;
        _cartService    = cartService;
        _couponRepo     = couponRepo;
        _userRepo       = userRepo;
        _exportService = exportService;
        _emailService  = emailService;
        _config        = config;
    }

    // ============================================================
    // CUSTOMER - GIỎ HÀNG
    // ============================================================

    public IActionResult Cart()
    {
        var cart = _cartService.GetCart(HttpContext.Session);
        ViewBag.CartCount = cart.Sum(x => x.Quantity);
        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddToCart(int fruitId, int quantity = 1)
    {
        if (!SessionHelper.IsLoggedIn(HttpContext.Session))
            return RedirectToAction("Login", "Account",
                new { returnUrl = Url.Action("Details", "Fruit", new { id = fruitId }) });

        var fruit = _fruitRepo.GetById(fruitId);
        if (fruit == null || !fruit.IsActive)
            { TempData["Error"] = "Sản phẩm không tồn tại!"; return RedirectToAction("Index", "Home"); }

        if (quantity < 1) quantity = 1;
        if (quantity > fruit.StockQuantity)
            { TempData["Error"] = $"Chỉ còn {fruit.StockQuantity} {fruit.Unit} trong kho!"; return RedirectToAction("Details", "Fruit", new { id = fruitId }); }

        _cartService.AddToCart(HttpContext.Session, fruit, quantity);
        TempData["Success"] = $"Đã thêm {fruit.FruitName} vào giỏ hàng!";
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCart(int fruitId, int quantity)
    {
        _cartService.UpdateQuantity(HttpContext.Session, fruitId, quantity);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(int fruitId)
    {
        _cartService.RemoveFromCart(HttpContext.Session, fruitId);
        TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplyCoupon(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Json(new { success = false, message = "Vui lòng nhập mã." });

        var coupon = _couponRepo.GetByCode(code.Trim().ToUpper());
        if (coupon == null)
            return Json(new { success = false, message = "Mã không hợp lệ hoặc đã hết hạn." });

        var cart = _cartService.GetCart(HttpContext.Session);
        decimal total    = cart.Sum(x => x.Subtotal);
        decimal discount = total * coupon.DiscountPercent / 100;

        return Json(new {
            success = true,
            code = coupon.Code,
            percent = coupon.DiscountPercent,
            discountAmount = discount,
            finalAmount = total - discount
        });
    }

    // ============================================================
    // CUSTOMER - CHECKOUT & ORDERS
    // ============================================================

        [RequireRole("Customer")]
        public IActionResult Checkout()
        {
            var cart = _cartService.GetCart(HttpContext.Session);
            if (!cart.Any())
                { TempData["Error"] = "Giỏ hàng trống!"; return RedirectToAction(nameof(Cart)); }

            int userId = SessionHelper.GetUserId(HttpContext.Session);
            var user = _userRepo.GetById(userId);
            ViewBag.CurrentPoints = user?.Points ?? 0;
            ViewBag.CurrentTier   = user?.Tier ?? "Standard";
            return View(new OrderViewModel { CartItems = cart });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Customer")]
        public IActionResult Checkout(OrderViewModel model)
        {
            var cart = _cartService.GetCart(HttpContext.Session);
            model.CartItems = cart;

            if (!ModelState.IsValid || !cart.Any())
                { TempData["Error"] = "Giỏ hàng trống!"; return RedirectToAction(nameof(Cart)); }

            foreach (var item in cart)
            {
                if (_validationHelper.IsQuantityExceedStock(item.FruitId, item.Quantity))
                {
                    TempData["Error"] = $"Sản phẩm '{item.FruitName}' không đủ tồn kho!";
                    return RedirectToAction(nameof(Cart));
                }
            }

            int userId = SessionHelper.GetUserId(HttpContext.Session);
            var user   = _userRepo.GetById(userId);

            // RQ35: Xử lý điểm tích lũy đổi giảm giá
            int pointsToRedeem = 0;
            decimal pointsDiscount = 0;
            if (model.PointsRedeemed > 0 && user != null)
            {
                // Mỗi 100 điểm = giảm 1,000đ, tối đa dùng 50% giá trị đơn hàng
                decimal subtotal = cart.Sum(x => x.Subtotal);
                int maxRedeemable = Math.Min(model.PointsRedeemed, user.Points);
                decimal maxDiscount = subtotal * 50 / 100;
                pointsDiscount = Math.Min(maxRedeemable * 10m, maxDiscount); // 100 pts = 1000đ
                pointsToRedeem = model.PointsRedeemed;
            }

            var order  = new Order
            {
                UserId          = userId,
                Status          = AppConstants.OrderStatuses.Pending,
                ShippingAddress = model.ShippingAddress,
                Note            = model.Note,
                PaymentMethod   = string.IsNullOrEmpty(model.PaymentMethod) ? "Cash" : model.PaymentMethod,
                AmountReceived  = model.AmountReceived
            };

            if (!string.IsNullOrEmpty(model.CouponCode))
            {
                var coupon = _couponRepo.GetByCode(model.CouponCode);
                if (coupon != null && coupon.IsActive && coupon.ExpiryDate >= DateTime.Now)
                {
                    order.CouponId      = coupon.CouponId;
                    order.DiscountAmount = cart.Sum(x => x.Subtotal) * coupon.DiscountPercent / 100;
                }
            }

            int orderId = _orderRepo.CreateOrder(order, cart, pointsToRedeem, pointsDiscount);

            // Trừ điểm sau khi đặt thành công
            if (pointsToRedeem > 0)
                _userRepo.RedeemPoints(userId, pointsToRedeem);

            // Cộng điểm sau khi giao hàng (xử lý ở UpdateStatus)
            _cartService.ClearCart(HttpContext.Session);

            _ = _emailService.SendOrderConfirmationAsync(orderId, userId);

            TempData["Success"] = $"Đặt hàng thành công! Mã đơn #{orderId}";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

    [RequireRole("Customer")]
    public IActionResult History()
    {
        int userId = SessionHelper.GetUserId(HttpContext.Session);
        return View(_orderRepo.GetByCustomer(userId));
    }

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

    [RequireRole("Admin", "Staff")]
    public IActionResult Index(string? status, DateTime? fromDate, DateTime? toDate, string? keyword, int page = 1)
    {
        int pageSize = _config.GetValue("AppSettings:ItemsPerPage", AppConstants.Pagination.DefaultPageSize);
        var (items, total) = _orderRepo.Search(status, fromDate, toDate, keyword, page, pageSize);
        var paged = PaginationHelper.Create(items, total, page, pageSize);

        ViewBag.Status   = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate   = toDate?.ToString("yyyy-MM-dd");
        ViewBag.Keyword  = keyword;
        return View(paged);
    }

    // GET: /Order/Invoice/5
    public IActionResult Invoice(int id)
    {
        var order = _orderRepo.GetById(id);
        if (order == null) return NotFound();

        var role   = SessionHelper.GetUserRole(HttpContext.Session);
        var userId = SessionHelper.GetUserId(HttpContext.Session);
        if (role == "Customer" && order.UserId != userId)
            return RedirectToAction("AccessDenied", "Home");

        return View(order);
    }

    // GET: /Order/Details/5
    public IActionResult Details(int id)
    {
        var order = _orderRepo.GetById(id);
        if (order == null) return NotFound();

        var role   = SessionHelper.GetUserRole(HttpContext.Session);
        var userId = SessionHelper.GetUserId(HttpContext.Session);
        if (role == "Customer" && order.UserId != userId)
            return RedirectToAction("AccessDenied", "Home");

        ViewBag.QrData = $"FruitShop|OrderId:{order.OrderId}|Total:{order.TotalAmount:N0}|Status:{order.Status}";
        ViewBag.UserRole = role;
        return View(order);
    }

    // POST: /Order/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole("Admin", "Staff")]
    public IActionResult UpdateStatus(int id, string status)
    {
        int staffId = SessionHelper.GetUserId(HttpContext.Session);
        _orderRepo.UpdateStatus(id, status, staffId);

        if (status == AppConstants.OrderStatuses.Delivered)
        {
            var order = _orderRepo.GetById(id);
            if (order != null && order.TotalAmount > 0)
            {
                int points = (int)(order.TotalAmount / 10000);
                if (points > 0) _userRepo.AddPoints(order.UserId, points);
            }
        }

        _ = _emailService.SendStatusUpdateAsync(id, status);

        TempData["Success"] = $"Đã cập nhật trạng thái đơn #{id} thành '{status}'!";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // EXPORT FILES
    // ============================================================

    [RequireRole("Admin", "Staff")]
    public IActionResult ExportExcel(string? status, DateTime? fromDate, DateTime? toDate, string? keyword)
    {
        var (orders, _) = _orderRepo.Search(status, fromDate, toDate, keyword, 1, 10000);
        var bytes = _exportService.ExportOrdersToExcel(orders);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Orders-{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [RequireRole("Admin", "Staff")]
    public IActionResult ExportPdf(int id)
    {
        var order = _orderRepo.GetById(id);
        if (order == null) return NotFound();

        var bytes = _exportService.ExportInvoiceToPdf(order);
        return File(bytes, "application/pdf", $"Invoice_{order.OrderId}.pdf");
    }
}
