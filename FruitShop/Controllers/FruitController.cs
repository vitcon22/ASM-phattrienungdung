using FruitShop.Constants;
using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;
using FruitShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;

namespace FruitShop.Controllers;

/// <summary>
/// Controller quản lý trái cây - đã refactor dùng Service Layer
/// Staff/Admin: CRUD; Customer: chỉ xem
/// </summary>
public class FruitController : Controller
{
    private readonly FruitRepository     _fruitRepo;
    private readonly CategoryRepository  _categoryRepo;
    private readonly SupplierRepository _supplierRepo;
    private readonly ReviewRepository   _reviewRepo;
    private readonly WishlistRepository _wishlistRepo;
    private readonly ValidationHelper   _validationHelper;
    private readonly IFileStorageService _fileStorage;
    private readonly IExportService    _exportService;
    private readonly IConfiguration   _config;
    private readonly IWebHostEnvironment _env;

    public FruitController(
        FruitRepository fruitRepo,
        CategoryRepository categoryRepo,
        SupplierRepository supplierRepo,
        ReviewRepository reviewRepo,
        WishlistRepository wishlistRepo,
        ValidationHelper validationHelper,
        IFileStorageService fileStorage,
        IExportService exportService,
        IConfiguration config,
        IWebHostEnvironment env)
    {
        _fruitRepo        = fruitRepo;
        _categoryRepo     = categoryRepo;
        _supplierRepo     = supplierRepo;
        _reviewRepo       = reviewRepo;
        _wishlistRepo     = wishlistRepo;
        _validationHelper = validationHelper;
        _fileStorage      = fileStorage;
        _exportService   = exportService;
        _config          = config;
        _env             = env;
    }

    // GET: /Fruit
    public IActionResult Index(
        string? keyword, int? categoryId, string? origin,
        decimal? minPrice, decimal? maxPrice, string? stockStatus,
        int page = 1)
    {
        int pageSize = _config.GetValue("AppSettings:ItemsPerPage", AppConstants.Pagination.DefaultPageSize);
        var (items, total) = _fruitRepo.Search(keyword, categoryId, origin, minPrice, maxPrice, stockStatus, page, pageSize);
        var paged = PaginationHelper.Create(items, total, page, pageSize);

        ViewBag.Categories  = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName", categoryId);
        ViewBag.Keyword     = keyword;
        ViewBag.CategoryId  = categoryId;
        ViewBag.Origin      = origin;
        ViewBag.MinPrice    = minPrice;
        ViewBag.MaxPrice    = maxPrice;
        ViewBag.StockStatus = stockStatus;
        ViewBag.UserRole    = SessionHelper.GetUserRole(HttpContext.Session);
        return View(paged);
    }

    // GET: /Fruit/Details/5
    public IActionResult Details(int id)
    {
        var fruit = _fruitRepo.GetById(id);
        if (fruit == null) return NotFound();

        int? userId = HttpContext.Session.GetInt32(SessionHelper.UserIdKey);

        ViewBag.UserRole       = SessionHelper.GetUserRole(HttpContext.Session);
        ViewBag.IsLoggedIn    = userId.HasValue;
        ViewBag.Reviews       = _reviewRepo.GetByFruitId(id).ToList();
        ViewBag.AvgRating     = _reviewRepo.GetAverageRating(id);
        ViewBag.ReviewCount   = _reviewRepo.GetReviewCount(id);
        ViewBag.IsInWishlist  = userId.HasValue && _wishlistRepo.IsInWishlist(userId.Value, id);

        ViewData["Title"]           = fruit.FruitName;
        ViewData["MetaDescription"] = $"{fruit.FruitName} tươi ngon, xuất xứ {fruit.Origin}.";
        ViewData["MetaKeywords"]    = $"{fruit.FruitName}, {fruit.CategoryName}, trái cây tươi";

        ViewBag.RelatedProducts = _fruitRepo.GetRelated(id, fruit.CategoryId, 4).ToList();
        if (fruit.StockQuantity == 0)
            ViewBag.Alternatives = _fruitRepo.GetAlternatives(id, fruit.CategoryId, 4).ToList();

        return View(fruit);
    }

    // GET: /Fruit/Create
    [RequireRole("Admin", "Staff")]
    public IActionResult Create()
    {
        SetupSelectLists();
        return View(new FruitViewModel());
    }

    // POST: /Fruit/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole("Admin", "Staff")]
    public async Task<IActionResult> Create(FruitViewModel model)
    {
        if (!ModelState.IsValid) { SetupSelectLists(); return View(model); }

        var (imgValid, imgError) = _fileStorage.ValidateImage(model.ImageFile);
        if (!imgValid)
        {
            ModelState.AddModelError("ImageFile", imgError);
            SetupSelectLists(); return View(model);
        }

        if (_validationHelper.IsFruitNameDuplicate(model.FruitName, model.CategoryId))
        {
            ModelState.AddModelError("FruitName", "Tên trái cây đã tồn tại trong danh mục này");
            SetupSelectLists(); return View(model);
        }

        var imageUrl = await _fileStorage.SaveImageAsync(
            model.ImageFile, AppConstants.ImageUpload.FruitFolder, _env);

        _fruitRepo.Insert(new Fruit
        {
            FruitName = model.FruitName, CategoryId = model.CategoryId,
            SupplierId = model.SupplierId, Price = model.Price,
            StockQuantity = model.StockQuantity, MinStock = model.MinStock,
            Unit = model.Unit, Origin = model.Origin,
            Description = model.Description, ImageUrl = imageUrl,
            IsActive = model.IsActive
        });

        TempData["Success"] = "Thêm trái cây thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Fruit/Edit/5
    [RequireRole("Admin", "Staff")]
    public IActionResult Edit(int id)
    {
        var fruit = _fruitRepo.GetById(id);
        if (fruit == null) return NotFound();

        var model = MapToViewModel(fruit);
        SetupSelectLists(fruit.CategoryId, fruit.SupplierId);
        return View(model);
    }

    // POST: /Fruit/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole("Admin", "Staff")]
    public async Task<IActionResult> Edit(FruitViewModel model)
    {
        if (!ModelState.IsValid) { SetupSelectLists(); return View(model); }

        if (_validationHelper.IsFruitNameDuplicate(model.FruitName, model.CategoryId, model.FruitId))
        {
            ModelState.AddModelError("FruitName", "Tên trái cây đã tồn tại trong danh mục này");
            SetupSelectLists(); return View(model);
        }

        string imageUrl = model.CurrentImageUrl ?? AppConstants.ImageUpload.DefaultImage;
        if (model.ImageFile != null)
        {
            var (valid, err) = _fileStorage.ValidateImage(model.ImageFile);
            if (!valid) { ModelState.AddModelError("ImageFile", err); SetupSelectLists(); return View(model); }
            _fileStorage.DeleteImage(model.CurrentImageUrl, AppConstants.ImageUpload.FruitFolder, _env);
            imageUrl = await _fileStorage.SaveImageAsync(model.ImageFile, AppConstants.ImageUpload.FruitFolder, _env);
        }

        _fruitRepo.Update(new Fruit
        {
            FruitId = model.FruitId, FruitName = model.FruitName,
            CategoryId = model.CategoryId, SupplierId = model.SupplierId,
            Price = model.Price, StockQuantity = model.StockQuantity,
            MinStock = model.MinStock, Unit = model.Unit,
            Origin = model.Origin, Description = model.Description,
            ImageUrl = imageUrl, IsActive = model.IsActive
        });

        TempData["Success"] = "Cập nhật trái cây thành công!";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Fruit/Delete/5
    [RequireRole("Admin", "Staff")]
    public IActionResult Delete(int id)
    {
        var fruit = _fruitRepo.GetById(id);
        if (fruit == null) return NotFound();
        return View(fruit);
    }

    // POST: /Fruit/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [RequireRole("Admin", "Staff")]
    public IActionResult DeleteConfirmed(int id)
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

    // GET: /Fruit/AutoComplete?keyword=xoai
    public IActionResult AutoComplete(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return Json(Array.Empty<string>());
        return Json(_fruitRepo.AutoComplete(keyword));
    }

    // GET: /Fruit/ImportExcel
    [RequireRole("Admin", "Staff")]
    public IActionResult ImportExcel() => View();

    // POST: /Fruit/ImportExcel
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireRole("Admin", "Staff")]
    public IActionResult ImportExcel(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
            { TempData["Error"] = "Vui lòng chọn file Excel."; return RedirectToAction(nameof(Index)); }
        if (!excelFile.FileName.EndsWith(".xlsx"))
            { TempData["Error"] = "Chỉ hỗ trợ file .xlsx."; return RedirectToAction(nameof(Index)); }

        try
        {
            using var stream = new MemoryStream();
            excelFile.CopyTo(stream);
            using var package = new ExcelPackage(stream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
                { TempData["Error"] = "File Excel không có dữ liệu."; return RedirectToAction(nameof(Index)); }

            int rowCount = worksheet.Dimension.Rows;
            int imported = 0;

            for (int row = 2; row <= rowCount; row++)
            {
                var fruitName    = worksheet.Cells[row, 1].Text?.Trim();
                var catIdStr     = worksheet.Cells[row, 2].Text?.Trim();
                var priceStr     = worksheet.Cells[row, 3].Text?.Trim();
                var stockStr     = worksheet.Cells[row, 4].Text?.Trim();
                var unit         = worksheet.Cells[row, 5].Text?.Trim();

                if (string.IsNullOrEmpty(fruitName) || string.IsNullOrEmpty(catIdStr) || string.IsNullOrEmpty(priceStr))
                    continue;

                if (int.TryParse(catIdStr, out int catId) && decimal.TryParse(priceStr, out decimal price))
                {
                    int.TryParse(worksheet.Cells[row, 6].Text?.Trim(), out int minStock);
                    if (minStock == 0) minStock = 10;

                    _fruitRepo.Insert(new Fruit
                    {
                        FruitName     = fruitName,
                        CategoryId   = catId,
                        Price        = price,
                        StockQuantity = int.TryParse(stockStr, out int qty) ? qty : 0,
                        MinStock     = minStock,
                        Unit         = string.IsNullOrEmpty(unit) ? "kg" : unit,
                        Origin       = worksheet.Cells[row, 7].Text?.Trim(),
                        Description  = worksheet.Cells[row, 8].Text?.Trim(),
                        ImageUrl     = AppConstants.ImageUpload.DefaultImage,
                        IsActive     = true
                    });
                    imported++;
                }
            }
            TempData["Success"] = $"Đã nhập thành công {imported} sản phẩm từ file Excel.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Lỗi khi đọc file Excel: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: /Fruit/ExportCsv
    [RequireRole("Admin", "Staff")]
    public IActionResult ExportCsv()
    {
        var fruits = _fruitRepo.GetAll();
        var bytes  = _exportService.ExportFruitsToCsv(fruits);
        return File(bytes, "text/csv", string.Format(AppConstants.ExportFiles.FruitsCsv, DateTime.Now.ToString("yyyyMMdd")));
    }

    // === Private Helpers ===
    private void SetupSelectLists(int? selectedCat = null, int? selectedSup = null)
    {
        ViewBag.Categories = new SelectList(_categoryRepo.GetAll(), "CategoryId", "CategoryName", selectedCat);
        ViewBag.Suppliers  = new SelectList(_supplierRepo.GetAllActive(), "SupplierId", "SupplierName", selectedSup);
    }

    private static FruitViewModel MapToViewModel(Fruit fruit) => new()
    {
        FruitId         = fruit.FruitId,
        FruitName       = fruit.FruitName,
        CategoryId      = fruit.CategoryId,
        SupplierId      = fruit.SupplierId,
        Price           = fruit.Price,
        StockQuantity   = fruit.StockQuantity,
        MinStock        = fruit.MinStock,
        Unit            = fruit.Unit,
        Origin          = fruit.Origin,
        Description     = fruit.Description,
        CurrentImageUrl = fruit.ImageUrl,
        IsActive       = fruit.IsActive
    };
}
