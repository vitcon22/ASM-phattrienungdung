using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FruitShop.Controllers
{
    /// <summary>
    /// Controller quản lý trái cây: CRUD + ảnh upload + phân trang + lọc theo danh mục
    /// Staff/Admin: CRUD; Customer: chỉ xem
    /// </summary>
    public class FruitController : Controller
    {
        private readonly FruitRepository _fruitRepo;
        private readonly CategoryRepository _categoryRepo;
        private readonly ReviewRepository _reviewRepo;
        private readonly WishlistRepository _wishlistRepo;
        private readonly ValidationHelper _validationHelper;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        // Đường dẫn lưu ảnh
        private const string ImageFolder = "images/fruits";

        public FruitController(
            FruitRepository fruitRepo,
            CategoryRepository categoryRepo,
            ReviewRepository reviewRepo,
            WishlistRepository wishlistRepo,
            ValidationHelper validationHelper,
            IConfiguration config,
            IWebHostEnvironment env)
        {
            _fruitRepo        = fruitRepo;
            _categoryRepo     = categoryRepo;
            _reviewRepo       = reviewRepo;
            _wishlistRepo     = wishlistRepo;
            _validationHelper = validationHelper;
            _config           = config;
            _env              = env;
        }

        // GET: /Fruit
        public IActionResult Index(string? keyword, int? categoryId, int page = 1)
        {
            int pageSize = _config.GetValue<int>("AppSettings:ItemsPerPage", 10);
            var (items, total) = _fruitRepo.Search(keyword, categoryId, page, pageSize);
            var paged = PaginationHelper.Create(items, total, page, pageSize);

            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName", categoryId);
            ViewBag.Keyword    = keyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.UserRole   = SessionHelper.GetUserRole(HttpContext.Session);
            return View(paged);
        }

        // GET: /Fruit/Details/5
        public IActionResult Details(int id)
        {
            var fruit = _fruitRepo.GetById(id);
            if (fruit == null) return NotFound();

            int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);

            ViewBag.UserRole  = SessionHelper.GetUserRole(HttpContext.Session);
            ViewBag.IsLoggedIn = userId.HasValue;
            
            // Reviews & Wishlist
            ViewBag.Reviews = _reviewRepo.GetByFruitId(id).ToList();
            ViewBag.AvgRating = _reviewRepo.GetAverageRating(id);
            ViewBag.ReviewCount = _reviewRepo.GetReviewCount(id);
            ViewBag.IsInWishlist = userId.HasValue && _wishlistRepo.IsInWishlist(userId.Value, id);

            return View(fruit);
        }

        // GET: /Fruit/Create  [Admin/Staff]
        [RequireRole("Admin", "Staff")]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
            return View(new FruitViewModel());
        }

        // POST: /Fruit/Create  [Admin/Staff]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin", "Staff")]
        public async Task<IActionResult> Create(FruitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                return View(model);
            }

            try
            {
                // Validate file ảnh
                var (imgValid, imgError) = _validationHelper.ValidateImageFile(model.ImageFile);
                if (!imgValid)
                {
                    ModelState.AddModelError("ImageFile", imgError);
                    ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                    return View(model);
                }

                // Kiểm tra tên trùng trong danh mục
                if (_validationHelper.IsFruitNameDuplicate(model.FruitName, model.CategoryId))
                {
                    ModelState.AddModelError("FruitName", "Tên trái cây đã tồn tại trong danh mục này");
                    ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                    return View(model);
                }

                // Upload ảnh
                string imageUrl = await SaveImageAsync(model.ImageFile);

                var fruit = new Fruit
                {
                    FruitName     = model.FruitName,
                    CategoryId    = model.CategoryId,
                    Price         = model.Price,
                    StockQuantity = model.StockQuantity,
                    Unit          = model.Unit,
                    Origin        = model.Origin,
                    Description   = model.Description,
                    ImageUrl      = imageUrl,
                    IsActive      = model.IsActive
                };

                _fruitRepo.Insert(fruit);
                TempData["Success"] = "Thêm trái cây thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                return View(model);
            }
        }

        // GET: /Fruit/Edit/5  [Admin/Staff]
        [RequireRole("Admin", "Staff")]
        public IActionResult Edit(int id)
        {
            var fruit = _fruitRepo.GetById(id);
            if (fruit == null) return NotFound();

            var model = new FruitViewModel
            {
                FruitId          = fruit.FruitId,
                FruitName        = fruit.FruitName,
                CategoryId       = fruit.CategoryId,
                Price            = fruit.Price,
                StockQuantity    = fruit.StockQuantity,
                Unit             = fruit.Unit,
                Origin           = fruit.Origin,
                Description      = fruit.Description,
                CurrentImageUrl  = fruit.ImageUrl,
                IsActive         = fruit.IsActive
            };

            ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName", fruit.CategoryId);
            return View(model);
        }

        // POST: /Fruit/Edit/5  [Admin/Staff]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin", "Staff")]
        public async Task<IActionResult> Edit(FruitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                return View(model);
            }

            try
            {
                // Kiểm tra tên trùng (trừ chính nó)
                if (_validationHelper.IsFruitNameDuplicate(model.FruitName, model.CategoryId, model.FruitId))
                {
                    ModelState.AddModelError("FruitName", "Tên trái cây đã tồn tại trong danh mục này");
                    ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                    return View(model);
                }

                // Xử lý ảnh mới
                string imageUrl = model.CurrentImageUrl ?? "default.jpg";
                if (model.ImageFile != null)
                {
                    var (valid, err) = _validationHelper.ValidateImageFile(model.ImageFile);
                    if (!valid)
                    {
                        ModelState.AddModelError("ImageFile", err);
                        ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                        return View(model);
                    }
                    // Xóa ảnh cũ nếu khác default
                    DeleteOldImage(model.CurrentImageUrl);
                    imageUrl = await SaveImageAsync(model.ImageFile);
                }

                var fruit = new Fruit
                {
                    FruitId       = model.FruitId,
                    FruitName     = model.FruitName,
                    CategoryId    = model.CategoryId,
                    Price         = model.Price,
                    StockQuantity = model.StockQuantity,
                    Unit          = model.Unit,
                    Origin        = model.Origin,
                    Description   = model.Description,
                    ImageUrl      = imageUrl,
                    IsActive      = model.IsActive
                };

                _fruitRepo.Update(fruit);
                TempData["Success"] = "Cập nhật trái cây thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName");
                return View(model);
            }
        }

        // GET: /Fruit/Delete/5  [Admin/Staff]
        [RequireRole("Admin", "Staff")]
        public IActionResult Delete(int id)
        {
            var fruit = _fruitRepo.GetById(id);
            if (fruit == null) return NotFound();
            return View(fruit);
        }

        // POST: /Fruit/Delete/5  [Admin/Staff]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [RequireRole("Admin", "Staff")]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                if (!_validationHelper.CanDeleteFruit(id))
                {
                    TempData["Error"] = "Không thể xóa trái cây này vì đã có trong đơn hàng!";
                    return RedirectToAction(nameof(Index));
                }
                _fruitRepo.SoftDelete(id);
                TempData["Success"] = "Đã xóa trái cây thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Fruit/AutoComplete?keyword=xoai  [AJAX - Tính năng sáng tạo]
        public IActionResult AutoComplete(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(Array.Empty<string>());
            var suggestions = _fruitRepo.AutoComplete(keyword);
            return Json(suggestions);
        }

        // ============================================================
        // PRIVATE HELPERS - xử lý file ảnh
        // ============================================================

        /// <summary>
        /// Lưu ảnh upload vào wwwroot/images/fruits/, trả về tên file
        /// </summary>
        private async Task<string> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "default.jpg";

            var uploadDir = Path.Combine(_env.WebRootPath, ImageFolder);
            Directory.CreateDirectory(uploadDir); // Tạo thư mục nếu chưa có

            // Tên file unique để tránh trùng
            var ext      = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        /// <summary>
        /// Xóa ảnh cũ khỏi wwwroot/images/fruits/ (không xóa ảnh default)
        /// </summary>
        private void DeleteOldImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || imageUrl == "default.jpg") return;

            var filePath = Path.Combine(_env.WebRootPath, ImageFolder, imageUrl);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }
    }
}
