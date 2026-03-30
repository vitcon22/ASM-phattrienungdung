using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace FruitShop.Controllers
{
    /// <summary>
    /// Controller quản lý người dùng (Admin)
    /// </summary>
    [RequireRole("Admin")]
    public class UserController : Controller
    {
        private readonly UserRepository _userRepo;
        private readonly IConfiguration _config;

        public UserController(UserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config   = config;
        }

        // GET: /User/Index
        public IActionResult Index(string? keyword, string? role, int page = 1)
        {
            int pageSize = _config.GetValue<int>("AppSettings:ItemsPerPage", 15);
            var (items, total) = _userRepo.Search(keyword, role, page, pageSize);
            var paged = PaginationHelper.Create(items, total, page, pageSize);

            ViewBag.Keyword = keyword;
            ViewBag.Role    = role;
            ViewBag.TotalCustomers = _userRepo.CountCustomers();
            return View(paged);
        }

        // POST: /User/ToggleActive/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleActive(int id)
        {
            var user = _userRepo.GetById(id);
            if (user == null) return NotFound();

            // Không cho phép tắt chính mình
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
            return View(user);
        }

        // GET: /User/ExportCsv
        public IActionResult ExportCsv()
        {
            var users = _userRepo.GetAll();
            var sb = new StringBuilder();
            sb.AppendLine("ID,Họ tên,Email,Điện thoại,Vai trò,Trạng thái,Ngày tạo");
            foreach (var u in users)
            {
                sb.AppendLine($"{u.UserId},\"{u.FullName}\",{u.Email},{u.Phone},{u.RoleName},{(u.IsActive ? "Hoạt động" : "Khoá")},{u.CreatedAt:dd/MM/yyyy}");
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"DanhSachNguoiDung_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
