using FruitShop.Constants;
using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers;

/// <summary>
/// Controller quản lý người dùng (Admin)
/// Đã tách ExportService dùng AppConstants
/// </summary>
[RequireRole("Admin")]
public class UserController : Controller
{
    private readonly UserRepository _userRepo;
    private readonly OrderRepository _orderRepo;
    private readonly IConfiguration _config;
    private readonly IExportService _exportService;

    public UserController(UserRepository userRepo, OrderRepository orderRepo,
        IConfiguration config, IExportService exportService)
    {
        _userRepo      = userRepo;
        _orderRepo     = orderRepo;
        _config        = config;
        _exportService = exportService;
    }

    // GET: /User/Index
    public IActionResult Index(string? keyword, string? role, int page = 1)
    {
        int pageSize = _config.GetValue("AppSettings:ItemsPerPage", 15);
        var (items, total) = _userRepo.Search(keyword, role, page, pageSize);
        var paged = PaginationHelper.Create(items, total, page, pageSize);

        ViewBag.Keyword = keyword;
        ViewBag.Role    = role;
        ViewBag.TotalCustomers = _userRepo.CountCustomers();
        return View(paged);
    }

    // GET: /User/Create  (RQ14: Admin tạo Staff/Admin)
    public IActionResult Create()
    {
        var roles = _userRepo.GetRoles().ToList();
        ViewBag.Roles = roles;
        return View(new User { IsActive = true, RoleId = 2 }); // default: Staff
    }

    // POST: /User/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(User model, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            ModelState.AddModelError("Password", "Mật khẩu phải có ít nhất 6 ký tự.");
        }
        if (_userRepo.EmailExists(model.Email))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại trong hệ thống.");
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _userRepo.GetRoles().ToList();
            return View(model);
        }

        model.Password = BCrypt.Net.BCrypt.HashPassword(password);
        model.IsActive = true;
        int id = _userRepo.CreateByAdmin(model);
        TempData["Success"] = $"Đã tạo tài khoản '{model.FullName}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: /User/Edit/5  (RQ14: Admin sửa Staff/Admin)
    public IActionResult Edit(int id)
    {
        var user = _userRepo.GetById(id);
        if (user == null) return NotFound();
        if (user.RoleName == "Customer")
        {
            TempData["Error"] = "Không thể chỉnh sửa tài khoản khách hàng từ đây.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Roles = _userRepo.GetRoles().ToList();
        return View(user);
    }

    // POST: /User/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(User model, string? newPassword)
    {
        var existing = _userRepo.GetById(model.UserId);
        if (existing == null) return NotFound();
        if (existing.RoleName == "Customer")
        {
            TempData["Error"] = "Không thể chỉnh sửa tài khoản khách hàng từ đây.";
            return RedirectToAction(nameof(Index));
        }
        if (_userRepo.EmailExists(model.Email, model.UserId))
        {
            ModelState.AddModelError("Email", "Email đã tồn tại trong hệ thống.");
        }
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = _userRepo.GetRoles().ToList();
            return View(model);
        }

        // Cập nhật thông tin (không đổi mật khẩu ở đây)
        existing.FullName = model.FullName;
        existing.Phone   = model.Phone;
        existing.Address = model.Address;
        existing.RoleId  = model.RoleId;
        existing.IsActive = model.IsActive;
        _userRepo.UpdateByAdmin(existing);

        if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length >= 6)
        {
            _userRepo.UpdatePassword(model.UserId, BCrypt.Net.BCrypt.HashPassword(newPassword));
            TempData["Success"] = $"Đã cập nhật tài khoản '{model.FullName}' (mật khẩu đã được đổi)!";
        }
        else
        {
            TempData["Success"] = $"Đã cập nhật tài khoản '{model.FullName}'!";
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /User/Delete/5  (RQ14: Admin xóa Staff/Admin)
    public IActionResult Delete(int id)
    {
        var user = _userRepo.GetById(id);
        if (user == null) return NotFound();
        if (user.RoleName == "Customer")
        {
            TempData["Error"] = "Không thể xóa tài khoản khách hàng. Vui lòng sử dụng Khoá/Mở khoá.";
            return RedirectToAction(nameof(Index));
        }
        // Không cho xóa chính mình
        int myId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
        if (id == myId)
        {
            TempData["Error"] = "Không thể xóa tài khoản của chính mình!";
            return RedirectToAction(nameof(Index));
        }
        return View(user);
    }

    // POST: /User/DeleteConfirmed/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        int myId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
        if (id == myId)
        {
            TempData["Error"] = "Không thể xóa tài khoản của chính mình!";
            return RedirectToAction(nameof(Index));
        }
        var user = _userRepo.GetById(id);
        if (user == null) return NotFound();
        if (user.RoleName == "Customer")
        {
            TempData["Error"] = "Không thể xóa tài khoản khách hàng.";
            return RedirectToAction(nameof(Index));
        }
        _userRepo.SoftDelete(id);
        TempData["Success"] = $"Đã xóa tài khoản '{user.FullName}'!";
        return RedirectToAction(nameof(Index));
    }

    // POST: /User/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleActive(int id)
    {
        var user = _userRepo.GetById(id);
        if (user == null) return NotFound();

        int myId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
        if (id == myId)
        {
            TempData["Error"] = "Không thể tắt tài khoản của chính mình!";
            return RedirectToAction(nameof(Index));
        }

        _userRepo.ToggleActive(id, !user.IsActive);
        TempData["Success"] = user.IsActive
            ? $"Đã khoá tài khoản {user.FullName}"
            : $"Đã mở khoá tài khoản {user.FullName}";
        return RedirectToAction(nameof(Index));
    }

    // GET: /User/Details/5
    public IActionResult Details(int id)
    {
        var user = _userRepo.GetById(id);
        if (user == null) return NotFound();

        if (user.RoleName == "Customer")
        {
            var orders = _orderRepo.GetByCustomer(id).ToList();
            ViewBag.Orders      = orders;
            ViewBag.TotalOrders = orders.Count;
            ViewBag.TotalSpent  = _orderRepo.GetCustomerTotalSpent(id);
            ViewBag.LastOrder   = orders.OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate;
        }
        return View(user);
    }

    // GET: /User/ExportCsv
    public IActionResult ExportCsv()
    {
        var users = _userRepo.GetAll();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("ID,Họ tên,Email,Điện thoại,Vai trò,Trạng thái,Ngày tạo");
        foreach (var u in users)
        {
            sb.AppendLine($"{u.UserId},\"{u.FullName}\",{u.Email},{u.Phone},{u.RoleName},{(u.IsActive ? "Hoạt động" : "Khoá")},{u.CreatedAt:dd/MM/yyyy}");
        }
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"DanhSachNguoiDung_{DateTime.Now:yyyyMMdd}.csv");
    }

    // GET: /User/ExportExcel  (RQ101)
    public IActionResult ExportExcel()
    {
        var users = _userRepo.GetAllForExport().ToList();
        var bytes = _exportService.ExportUsersToExcel(users);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"DanhSachNguoiDung_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // GET: /User/Customers — alias cho AdminCustomerController (tránh duplicate)
    public IActionResult Customers(string? keyword, int page = 1)
    {
        return Index(keyword, "Customer", page);
    }
}
