using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;

namespace FruitShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly ValidationHelper _validationHelper;
        private readonly WishlistRepository _wishlistRepo;

        public AccountController(UserRepository userRepo, ValidationHelper validationHelper, WishlistRepository wishlistRepo)
        {
            _userRepo         = userRepo;
            _validationHelper = validationHelper;
            _wishlistRepo     = wishlistRepo;
        }

        // GET: /Account/Login
        public IActionResult Login(string? returnUrl)
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToHome();
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                var user = _userRepo.GetByEmail(model.Email);
                if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                    return View(model);
                }
                HttpContext.Session.SetInt32(SessionHelper.UserIdKey, user.UserId);
                HttpContext.Session.SetString(SessionHelper.UserNameKey, user.FullName);
                HttpContext.Session.SetString(SessionHelper.UserRoleKey, user.RoleName ?? "Customer");
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToHome();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session)) return RedirectToHome();
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                if (_validationHelper.IsEmailDuplicate(model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View(model);
                }
                var user = new User
                {
                    FullName = model.FullName,
                    Email    = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Phone    = model.Phone,
                    Address  = model.Address,
                    RoleId   = 3,
                    IsActive = true
                };
                _userRepo.Insert(user);
                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            SessionHelper.Clear(HttpContext.Session);
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Profile
        [RequireRole("Admin", "Staff", "Customer")]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
            var user   = _userRepo.GetById(userId);
            if (user == null) return RedirectToAction(nameof(Login));
            var role = HttpContext.Session.GetString(SessionHelper.UserRoleKey) ?? "Customer";
            ViewBag.UserLayout = role is "Admin" or "Staff" ? "_Layout" : "_LayoutUser";
            
            ViewBag.Wishlists = _wishlistRepo.GetByUser(userId).ToList();
            
            return View(user);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin", "Staff", "Customer")]
        public IActionResult Profile(User model)
        {
            var userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
            try
            {
                model.UserId = userId;
                _userRepo.UpdateProfile(model);
                HttpContext.Session.SetString(SessionHelper.UserNameKey, model.FullName);
                TempData["Success"] = "Cập nhật thông tin thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction(nameof(Profile));
        }

        // GET: /Account/ChangePassword
        [RequireRole("Admin", "Staff", "Customer")]
        public IActionResult ChangePassword()
        {
            var role = HttpContext.Session.GetString(SessionHelper.UserRoleKey) ?? "Customer";
            ViewBag.UserLayout = role is "Admin" or "Staff" ? "_Layout" : "_LayoutUser";
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin", "Staff", "Customer")]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và xác nhận không khớp.";
                return RedirectToAction(nameof(ChangePassword));
            }
            if (newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction(nameof(ChangePassword));
            }
            var userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
            var user   = _userRepo.GetById(userId);
            if (user == null || !BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                TempData["Error"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction(nameof(ChangePassword));
            }
            _userRepo.UpdatePassword(userId, BCrypt.Net.BCrypt.HashPassword(newPassword));
            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(Profile));
        }

        private IActionResult RedirectToHome()
        {
            var role = SessionHelper.GetUserRole(HttpContext.Session);
            return role switch
            {
                "Admin" => RedirectToAction("Index", "Dashboard"),
                "Staff" => RedirectToAction("Index", "Fruit"),
                _       => RedirectToAction("Index", "Home")
            };
        }
    }
}
