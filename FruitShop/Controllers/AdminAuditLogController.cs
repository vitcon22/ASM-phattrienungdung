using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FruitShop.Controllers
{
    [RequireRole("Admin")]
    public class AdminAuditLogController : Controller
    {
        private readonly AuditLogRepository _auditRepo;
        private readonly IConfiguration _config;

        public AdminAuditLogController(AuditLogRepository auditRepo, IConfiguration config)
        {
            _auditRepo = auditRepo;
            _config    = config;
        }

        public IActionResult Index(
            string? keyword, string? controller,
            DateTime? fromDate, DateTime? toDate,
            int page = 1)
        {
            int pageSize = _config.GetValue<int>("AppSettings:ItemsPerPage", 20);
            var items = _auditRepo.Search(keyword, controller, fromDate, toDate, page, pageSize).ToList();
            int total  = _auditRepo.Count(keyword, controller, fromDate, toDate);
            var paged  = PaginationHelper.Create(items, total, page, pageSize);

            var controllers = _auditRepo.GetDistinctControllers().ToList();

            ViewBag.Keyword     = keyword;
            ViewBag.Controller  = controller;
            ViewBag.FromDate    = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate      = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Controllers  = new SelectList(controllers, controller);
            return View(paged);
        }
    }
}
