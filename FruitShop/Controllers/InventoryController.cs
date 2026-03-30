using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FruitShop.Controllers
{
    [RequireRole("Admin", "Staff")]
    public class InventoryController : Controller
    {
        private readonly InventoryLogRepository _inventoryRepo;
        private readonly FruitRepository _fruitRepo;

        public InventoryController(InventoryLogRepository inventoryRepo, FruitRepository fruitRepo)
        {
            _inventoryRepo = inventoryRepo;
            _fruitRepo     = fruitRepo;
        }

        public IActionResult Index()
        {
            var logs = _inventoryRepo.GetLogs();
            ViewBag.Fruits = new SelectList(_fruitRepo.GetAll(), "FruitId", "FruitName");
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdjustStock(int fruitId, int quantityChange, string reason)
        {
            try
            {
                if (quantityChange == 0)
                {
                    TempData["Error"] = "Số lượng thay đổi phải khác 0.";
                    return RedirectToAction(nameof(Index));
                }
                if (string.IsNullOrWhiteSpace(reason))
                {
                    TempData["Error"] = "Vui lòng nhập lý do.";
                    return RedirectToAction(nameof(Index));
                }

                var staffId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey) ?? 0;
                var log = new InventoryLog
                {
                    FruitId        = fruitId,
                    StaffId        = staffId,
                    QuantityChange = quantityChange,
                    Reason         = reason
                };

                _inventoryRepo.AddLogAndAdjustStock(log);

                TempData["Success"] = "Điều chỉnh tồn kho thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
