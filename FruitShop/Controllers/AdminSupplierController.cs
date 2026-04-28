using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    [RequireRole("Admin", "Staff")]
    public class AdminSupplierController : Controller
    {
        private readonly SupplierRepository _supplierRepo;
        private readonly BatchRepository    _batchRepo;
        private readonly IConfiguration _config;

        public AdminSupplierController(SupplierRepository supplierRepo, BatchRepository batchRepo, IConfiguration config)
        {
            _supplierRepo = supplierRepo;
            _batchRepo    = batchRepo;
            _config      = config;
        }

        public IActionResult Index(string? keyword, int page = 1)
        {
            int pageSize = _config.GetValue("AppSettings:ItemsPerPage", 15);
            var (items, total) = _supplierRepo.Search(keyword, page, pageSize);
            var paged = PaginationHelper.Create(items, total, page, pageSize);
            ViewBag.Keyword = keyword;
            return View(paged);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Supplier model)
        {
            if (ModelState.IsValid)
            {
                _supplierRepo.Insert(model);
                TempData["SuccessMessage"] = "Thêm nhà cung cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public IActionResult Edit(int id)
        {
            var supplier = _supplierRepo.GetById(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Supplier model)
        {
            if (id != model.SupplierId) return BadRequest();

            if (ModelState.IsValid)
            {
                _supplierRepo.Update(model);
                TempData["SuccessMessage"] = "Cập nhật nhà cung cấp thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (_supplierRepo.IsInUse(id))
            {
                TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp này vì đang có trái cây liên kết.";
                return RedirectToAction(nameof(Index));
            }

            _supplierRepo.Delete(id);
            TempData["SuccessMessage"] = "Xóa nhà cung cấp thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminSupplier/History/5 — Lịch sử nhập hàng theo nhà cung cấp (RQ36)
        public IActionResult History(int id)
        {
            var supplier = _supplierRepo.GetById(id);
            if (supplier == null) return NotFound();

            var batches = _batchRepo.GetBySupplier(id).ToList();

            ViewBag.Supplier      = supplier;
            ViewBag.TotalBatches  = batches.Count;
            ViewBag.TotalQty      = batches.Sum(b => b.Quantity);
            ViewBag.TotalValue    = batches.Sum(b => b.BuyPrice * b.Quantity);
            ViewBag.LastImport    = batches.OrderByDescending(b => b.ImportDate).FirstOrDefault()?.ImportDate;
            return View(batches);
        }

        // GET: /AdminSupplier/PriceComparison — So sánh giá nhập giữa các nhà cung cấp (RQ37)
        public IActionResult PriceComparison()
        {
            var rows = _batchRepo.GetPriceComparisonBySupplier().ToList();
            return View(rows);
        }
    }
}
