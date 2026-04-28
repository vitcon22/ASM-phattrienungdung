using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;

namespace FruitShop.Models.DAL;

public interface IUserRepository
{
    User? GetById(int id);
    User? GetByEmail(string email);
    User? GetByVerificationToken(string token);
    User? GetByResetToken(string token);
    void Insert(User user);
    void UpdateProfile(User user);
    void UpdatePassword(int userId, string hashedPassword);
    void UpdateResetToken(int userId, string token, DateTime expiry);
    void ClearResetToken(int userId);
    void ConfirmEmail(int userId);
    bool IsEmailDuplicate(string email);
    (IEnumerable<User> Items, int Total) Search(string? keyword, string? role, int page, int pageSize);
    IEnumerable<User> GetAll();
    int CountCustomers();
    int CountNewCustomersThisMonth();
    void ToggleActive(int userId, bool isActive);
    void AddPoints(int userId, int points);
}

public interface IWishlistRepository
{
    IEnumerable<Wishlist> GetByUser(int userId);
    bool IsInWishlist(int userId, int fruitId);
    void Toggle(int userId, int fruitId);
}

public interface ICategoryRepository
{
    IEnumerable<Category> GetAll();
    Category? GetById(int id);
    (IEnumerable<Category> Items, int Total) Search(string? keyword, int page, int pageSize);
    bool NameExists(string name, int? excludeId = null);
    void Insert(Category category);
    void Update(Category category);
    void SoftDelete(int id);
}

public interface IFruitRepository
{
    IEnumerable<Fruit> GetAll();
    IEnumerable<Fruit> GetAllActive();
    Fruit? GetById(int id);
    (IEnumerable<Fruit> Items, int Total) Search(string? keyword, int? categoryId, string? origin, decimal? minPrice, decimal? maxPrice, string? stockStatus, int page, int pageSize);
    bool IsFruitNameDuplicate(string name, int categoryId, int? excludeId = null);
    void Insert(Fruit fruit);
    void Update(Fruit fruit);
    void SoftDelete(int id);
    IEnumerable<string> AutoComplete(string keyword);
    int CountActive();
    IEnumerable<Fruit> GetTopSelling(int top);
    IEnumerable<Fruit> GetLowStock(int threshold);
    IEnumerable<Fruit> GetRelated(int fruitId, int categoryId, int top);
    IEnumerable<Fruit> GetAlternatives(int fruitId, int categoryId, int top);
    decimal GetTotalStockValue();
    int GetTotalStockQuantity();
    IEnumerable<CategoryInventoryReportItem> GetCategoryInventoryReport(int? categoryId);
    IEnumerable<RevenueByCategoryItem> GetRevenueByCategory();
}

public interface IOrderRepository
{
    Order? GetById(int id);
    IEnumerable<Order> GetByCustomer(int userId);
    (IEnumerable<Order> Items, int Total) Search(string? status, DateTime? fromDate, DateTime? toDate, string? keyword, int page, int pageSize);
    IEnumerable<Order> GetAllForExport(string? status, DateTime? fromDate, DateTime? toDate);
    IEnumerable<Order> GetRecent(int count);
    int CreateOrder(Order order, IEnumerable<CartItemViewModel> cartItems);
    bool CancelOrder(int orderId, int userId);
    void UpdateStatus(int orderId, string status, int staffId);
    decimal GetRevenue(string period);
    Dictionary<string, int> CountByStatus();
    IEnumerable<RevenueByCategoryItem> GetRevenueByCategory();
    IEnumerable<DailyRevenueItem> GetLast7DaysRevenue();
    int CountAll();
    decimal GetCustomerTotalSpent(int userId);
}

public interface IBatchRepository
{
    IEnumerable<Batch> Search(int? fruitId, string? keyword, int page, int pageSize);
    int Count(int? fruitId, string? keyword);
    Batch? GetById(int id);
    IEnumerable<Batch> GetExpiringSoon(int days);
    IEnumerable<Batch> GetBySupplier(int supplierId);
    IEnumerable<PriceComparisonItem> GetPriceComparisonBySupplier();
    void Insert(Batch batch);
    void Delete(int id);
}

public interface IInventoryLogRepository
{
    IEnumerable<InventoryLog> GetLogs();
    void AddLogAndAdjustStock(InventoryLog log);
}

public interface ICouponRepository
{
    IEnumerable<Coupon> GetAll();
    Coupon? GetByCode(string code);
    void Insert(Coupon coupon);
    void ToggleActive(int id, bool isActive);
}

public interface IReviewRepository
{
    IEnumerable<Review> GetByFruitId(int fruitId);
    decimal GetAverageRating(int fruitId);
    int GetReviewCount(int fruitId);
    int Insert(Review review);
    void InsertImage(int reviewId, string fileName);
}

public interface ISupplierRepository
{
    IEnumerable<Supplier> GetAll();
    IEnumerable<Supplier> GetAllActive();
    Supplier? GetById(int id);
    void Insert(Supplier supplier);
    void Update(Supplier supplier);
    void Delete(int id);
    bool IsInUse(int id);
}

public interface IAuditLogRepository
{
    IEnumerable<AuditLog> Search(string? keyword, string? controller, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    int Count(string? keyword, string? controller, DateTime? fromDate, DateTime? toDate);
    IEnumerable<string> GetDistinctControllers();
}

public interface IOperatingCostRepository
{
    IEnumerable<OperatingCost> GetByMonthYear(int month, int year);
    void Insert(OperatingCost cost);
    void Delete(int id);
}

// Extension interfaces for composite repositories
public interface IValidationHelper
{
    bool IsEmailDuplicate(string email);
    bool IsFruitNameDuplicate(string name, int categoryId, int? excludeId = null);
    bool CanDeleteFruit(int fruitId);
    bool CanDeleteCategory(int categoryId);
    (bool IsValid, string Error) ValidateImageFile(IFormFile? file);
    bool IsQuantityExceedStock(int fruitId, int quantity);
}

// CategoryInventoryReportItem is defined in Models/ViewModels/CategoryInventoryReportItem.cs

public class RevenueByCategoryItem
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Revenue { get; set; }
}

public class DailyRevenueItem
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}

public class PriceComparisonItem
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = "";
    public int FruitId { get; set; }
    public string FruitName { get; set; } = "";
    public decimal? AvgBuyPrice { get; set; }
    public int TotalQuantity { get; set; }
}
