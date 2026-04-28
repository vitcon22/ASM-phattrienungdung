using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using FruitShop.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using System.Security.Claims;

namespace FruitShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly ValidationHelper _validationHelper;
        private readonly WishlistRepository _wishlistRepo;
        private readonly IEmailService _emailService;

        public AccountController(UserRepository userRepo, ValidationHelper validationHelper, WishlistRepository wishlistRepo, IEmailService emailService)
        {
            _userRepo         = userRepo;
            _validationHelper = validationHelper;
            _wishlistRepo     = wishlistRepo;
            _emailService     = emailService;
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
        public async Task<IActionResult> Register(RegisterViewModel model)
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
                    IsActive = true,
                    EmailConfirmed = false,
                    VerificationToken = Guid.NewGuid().ToString()
                };
                _userRepo.Insert(user);

                // Gửi mail xác nhận
                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { token = user.VerificationToken }, protocol: Request.Scheme);
                var confirmBody = "<div style='font-family:Arial,sans-serif;max-width:500px;margin:0 auto;'>" +
                    "<div style='background:linear-gradient(135deg,#10B981,#06B6D4);padding:30px 40px;text-align:center;border-radius:12px 12px 0 0;'>" +
                    "<h1 style='color:#fff;margin:0;font-size:24px;'>FruitShop</h1>" +
                    "<p style='color:rgba(255,255,255,0.85);margin:8px 0 0;'>Xác nhận tài khoản</p></div>" +
                    "<div style='background:#fff;padding:30px 40px;border-radius:0 0 12px 12px;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>" +
                    "<p style='font-size:15px;'>Chào <strong>" + user.FullName + "</strong>,</p>" +
                    "<p style='font-size:14px;color:#374151;'>Cảm ơn bạn đã đăng ký FruitShop! Vui lòng nhấn nút bên dưới để kích hoạt tài khoản.</p>" +
                    "<div style='text-align:center;margin:30px 0;'>" +
                    "<a href='" + callbackUrl + "' style='display:inline-block;background:linear-gradient(135deg,#10B981,#06B6D4);color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px;'>Kích hoạt tài khoản</a></div>" +
                    "<p style='font-size:13px;color:#9CA3AF;text-align:center;margin-top:24px;'>— FruitShop Team —</p></div></div>";
                await _emailService.SendEmailAsync(user.Email, "Xác nhận tài khoản FruitShop", confirmBody);

                TempData["Success"] = "Đăng ký thành công! Vui lòng kiểm tra email để kích hoạt tài khoản.";
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi hệ thống: {ex.Message}");
                return View(model);
            }
        }

        // GET: /Account/ConfirmEmail
        public IActionResult ConfirmEmail(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Home");
            var user = _userRepo.GetByVerificationToken(token);
            if (user == null)
            {
                TempData["Error"] = "Mã xác nhận không hợp lệ.";
                return RedirectToAction(nameof(Login));
            }
            _userRepo.ConfirmEmail(user.UserId);
            TempData["Success"] = "Xác nhận email thành công! Bạn có thể đăng nhập ngay.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword() => View();

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = _userRepo.GetByEmail(model.Email);
            if (user == null)
            {
                // Không báo lỗi để tránh dò tìm email
                TempData["Success"] = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi link đặt lại mật khẩu.";
                return RedirectToAction(nameof(Login));
            }

            var token = Guid.NewGuid().ToString();
            _userRepo.UpdateResetToken(user.UserId, token, DateTime.Now.AddHours(2));

            var callbackUrl = Url.Action("ResetPassword", "Account", new { token }, protocol: Request.Scheme);
            var body = "<div style='font-family:Arial,sans-serif;max-width:500px;margin:0 auto;'>" +
                "<div style='background:linear-gradient(135deg,#10B981,#06B6D4);padding:30px 40px;text-align:center;border-radius:12px 12px 0 0;'>" +
                "<h1 style='color:#fff;margin:0;font-size:24px;'>FruitShop</h1>" +
                "<p style='color:rgba(255,255,255,0.85);margin:8px 0 0;'>Đặt lại mật khẩu</p></div>" +
                "<div style='background:#fff;padding:30px 40px;border-radius:0 0 12px 12px;box-shadow:0 4px 20px rgba(0,0,0,0.08);'>" +
                "<p style='font-size:15px;'>Xin chào <strong>" + user.FullName + "</strong>,</p>" +
                "<p style='font-size:14px;color:#374151;'>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản FruitShop của bạn.</p>" +
                "<p style='font-size:14px;color:#374151;'>Nhấn vào nút bên dưới để đặt lại mật khẩu (link có hiệu lực trong <strong>2 giờ</strong>):</p>" +
                "<div style='text-align:center;margin:30px 0;'>" +
                "<a href='" + callbackUrl + "' style='display:inline-block;background:linear-gradient(135deg,#10B981,#06B6D4);color:#fff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:bold;font-size:15px;'>Đặt lại mật khẩu</a></div>" +
                "<p style='font-size:13px;color:#6B7280;'>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Tài khoản của bạn vẫn an toàn.</p>" +
                "<p style='font-size:13px;color:#9CA3AF;text-align:center;margin-top:24px;'>— FruitShop Team —</p></div></div>";
            await _emailService.SendEmailAsync(user.Email, "[FruitShop] Đặt lại mật khẩu", body);

            TempData["Success"] = "Vui lòng kiểm tra email để đặt lại mật khẩu.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Index", "Home");
            return View(new ResetPasswordViewModel { Token = token });
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = _userRepo.GetByResetToken(model.Token);
            if (user == null)
            {
                TempData["Error"] = "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            _userRepo.UpdatePassword(user.UserId, BCrypt.Net.BCrypt.HashPassword(model.Password));
            _userRepo.ClearResetToken(user.UserId);

            TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("FruitShopCookie");
            SessionHelper.Clear(HttpContext.Session);
            return RedirectToAction(nameof(Login));
        }

        // --- EXTERNAL LOGIN (GOOGLE/FACEBOOK) ---

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            if (remoteError != null)
            {
                TempData["Error"] = $"Lỗi từ nhà cung cấp: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var result = await HttpContext.AuthenticateAsync("FruitShopCookie");
            if (!result.Succeeded) return RedirectToAction(nameof(Login));

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name  = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? "User";

            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Không lấy được email từ tài khoản mạng xã hội.";
                return RedirectToAction(nameof(Login));
            }

            var user = _userRepo.GetByEmail(email);
            if (user == null)
            {
                // Tự động đăng ký khách hàng mới
                user = new User
                {
                    FullName = name,
                    Email    = email,
                    Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random pass
                    RoleId   = 3, // Customer
                    IsActive = true,
                    EmailConfirmed = true // Đã xác thực qua mạng xã hội
                };
                _userRepo.Insert(user);
                user = _userRepo.GetByEmail(email); // Lấy lại để có UserId
            }

            // Set Session
            HttpContext.Session.SetInt32(SessionHelper.UserIdKey, user!.UserId);
            HttpContext.Session.SetString(SessionHelper.UserNameKey, user.FullName);
            HttpContext.Session.SetString(SessionHelper.UserRoleKey, user.RoleName ?? "Customer");

            return RedirectToHome();
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

        // GET: /Account/ChangePassword → chuyển hướng sang tab đổi mật khẩu trong Profile
        [RequireRole("Admin", "Staff", "Customer")]
        public IActionResult ChangePassword()
        {
            return RedirectToAction(nameof(Profile));
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
