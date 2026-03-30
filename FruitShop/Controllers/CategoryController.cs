using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FruitShop.Controllers
{
    /// <summary>
    /// Controller quản lý danh mục trái cây (Staff/Admin)
    /// </summary>
    [RequireRole("Admin", "Staff")]
    public class CategoryController : Controller
    {
        private readonly CategoryRepository _categoryRepo;
        private readonly ValidationHelper _validationHelper;
        private readonly IConfiguration _config;

        public CategoryController(
            CategoryRepository categoryRepo,
            ValidationHelper validationHelper,
            IConfiguration config)
        {
            _categoryRepo     = categoryRepo;
            _validationHelper = validationHelper;
            _config           = config;
        }

        // GET: /Category
        public IActionResult Index(string? keyword, int page = 1)
        {
            int pageSize = _config.GetValue<int>("AppSettings:ItemsPerPage", 10);
            var (items, total) = _categoryRepo.Search(keyword, page, pageSize);
            var paged = PaginationHelper.Create(items, total, page, pageSize);

            ViewBag.Keyword = keyword;
            return View(paged);
        }

        // GET: /Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            try
            {
                // Kiểm tra tên trùng
                if (_categoryRepo.NameExists(category.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại");
                    return View(category);
                }

                _categoryRepo.Insert(category);
                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return View(category);
            }
        }

        // GET: /Category/Edit/5
        public IActionResult Edit(int id)
        {
            var category = _categoryRepo.GetById(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        // POST: /Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category category)
        {
            if (!ModelState.IsValid)
                return View(category);

            try
            {
                // Kiểm tra tên trùng (trừ chính nó)
                if (_categoryRepo.NameExists(category.CategoryName, category.CategoryId))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại");
                    return View(category);
                }

                _categoryRepo.Update(category);
                TempData["Success"] = "Cập nhật danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                return View(category);
            }
        }

        // GET: /Category/Delete/5
        public IActionResult Delete(int id)
        {
            var category = _categoryRepo.GetById(id);
            if (category == null)
                return NotFound();
            return View(category);
        }

        // POST: /Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                // Kiểm tra còn sản phẩm không
                if (!_validationHelper.CanDeleteCategory(id))
                {
                    TempData["Error"] = "Không thể xóa danh mục này vì còn trái cây đang hoạt động!";
                    return RedirectToAction(nameof(Index));
                }

                _categoryRepo.SoftDelete(id);
                TempData["Success"] = "Đã xóa danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
