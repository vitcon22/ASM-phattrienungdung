using FruitShop.Constants;
using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FruitShop.Controllers
{
    [RequireRole("Admin", "Staff")]
    public class BatchController : Controller
    {
        private readonly BatchRepository _batchRepo;
        private readonly FruitRepository _fruitRepo;
        private readonly InventoryLogRepository _inventoryRepo;
        private readonly IConfiguration _config;

        public BatchController(BatchRepository batchRepo, FruitRepository fruitRepo,
            InventoryLogRepository inventoryRepo, IConfiguration config)
        {
            _batchRepo     = batchRepo;
            _fruitRepo     = fruitRepo;
            _inventoryRepo = inventoryRepo;
            _config        = config;
        }

        // GET: /Batch
        public IActionResult Index(int? fruitId, string? keyword, int page = 1)
        {
            int pageSize = _config.GetValue<int>("AppSettings:ItemsPerPage", 10);
            var items = _batchRepo.Search(fruitId, keyword, page, pageSize).ToList();
            int total = _batchRepo.Count(fruitId, keyword);
            var paged = PaginationHelper.Create(items, total, page, pageSize);

            var expiringSoon = _batchRepo.GetExpiringSoon(3).ToList();

            ViewBag.Fruits    = new SelectList(_fruitRepo.GetAll(), "FruitId", "FruitName", fruitId);
            ViewBag.FruitId   = fruitId;
            ViewBag.Keyword   = keyword;
            ViewBag.ExpiringSoon = expiringSoon;
            return View(paged);
        }

        // GET: /Batch/Create
        public IActionResult Create()
        {
            ViewBag.Fruits = new SelectList(_fruitRepo.GetAll(), "FruitId", "FruitName");
            var model = new Batch
            {
                ImportDate  = DateTime.Today,
                ExpiryDate  = DateTime.Today.AddDays(AppConstants.BatchDefaults.ExpiryDays),
                BatchCode   = $"BATCH-{DateTime.Now:yyyyMMdd}-{new Random().Next(10, 99)}"
            };
            return View(model);
        }

        // POST: /Batch/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Batch model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Fruits = new SelectList(_fruitRepo.GetAll(), "FruitId", "FruitName", model.FruitId);
                return View(model);
            }

            try
            {
                model.RemainingQty = model.Quantity;
                _batchRepo.Insert(model);

                // Cộng vào tồn kho và ghi log
                int staffId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
                var log = new InventoryLog
                {
                    FruitId        = model.FruitId,
                    StaffId        = staffId,
                    QuantityChange = model.Quantity,
                    Reason         = $"[NHẬP LÔ] {model.BatchCode} — Giá nhập: {model.BuyPrice:N0}đ/đơn vị"
                };
                _inventoryRepo.AddLogAndAdjustStock(log);

                TempData["Success"] = $"Nhập lô hàng {model.BatchCode} thành công! Tồn kho tăng +{model.Quantity}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                ViewBag.Fruits = new SelectList(_fruitRepo.GetAll(), "FruitId", "FruitName", model.FruitId);
                return View(model);
            }
        }

        // GET: /Batch/Details/5
        public IActionResult Details(int id)
        {
            var batch = _batchRepo.GetById(id);
            if (batch == null) return NotFound();
            return View(batch);
        }

        // GET: /Batch/PrintReceipt/5  — Phiếu nhập kho (RQ102)
        public IActionResult PrintReceipt(int id)
        {
            var batch = _batchRepo.GetById(id);
            if (batch == null) return NotFound();
            var staffName = HttpContext.Session.GetString(SessionHelper.UserNameKey) ?? "Nhân viên";
            ViewBag.StaffName = staffName;
            ViewBag.ShopName  = _config.GetValue<string>("AppSettings:ShopName") ?? "FruitShop";
            return View(batch);
        }

        // GET: /Batch/ExpiryWarning  — Cảnh báo hàng sắp hết hạn (RQ29)
        public IActionResult ExpiryWarning(int days = 7)
        {
            var batches = _batchRepo.GetExpiringSoon(days).ToList();
            ViewBag.Days = days;
            return View(batches);
        }

        // POST: /Batch/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin")]
        public IActionResult Delete(int id)
        {
            try
            {
                _batchRepo.Delete(id);
                TempData["Success"] = "Đã xóa lô hàng thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
