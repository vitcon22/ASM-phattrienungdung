using Dapper;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;

namespace FruitShop.Models.DAL
{
    /// <summary>
    /// Repository xử lý các thao tác liên quan đến Fruits (Trái cây)
    /// </summary>
    public class FruitRepository
    {
        private readonly FruitShopContext _context;

        public FruitRepository(FruitShopContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả trái cây có phân trang, lọc và tìm kiếm nâng cao (RQ10, RQ20).
        /// stockStatus: "in" = còn hàng, "low" = sắp hết (&lt;10), "out" = hết hàng
        /// </summary>
        public virtual (IEnumerable<Fruit> Items, int TotalCount) Search(
            string? keyword, int? categoryId,
            string? origin, decimal? minPrice, decimal? maxPrice, string? stockStatus,
            int page, int pageSize)
        {
            using var conn = _context.CreateConnection();

            var conditions = new List<string> { "f.IsActive = 1" };
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("(f.FruitName LIKE @Keyword OR f.Origin LIKE @Keyword)");
            if (categoryId.HasValue && categoryId > 0)
                conditions.Add("f.CategoryId = @CategoryId");
            if (!string.IsNullOrWhiteSpace(origin))
                conditions.Add("f.Origin LIKE @Origin");
            if (minPrice.HasValue)
                conditions.Add("f.Price >= @MinPrice");
            if (maxPrice.HasValue)
                conditions.Add("f.Price <= @MaxPrice");
            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                if (stockStatus == "in")   conditions.Add("f.StockQuantity >= f.MinStock");
                if (stockStatus == "low")  conditions.Add("f.StockQuantity > 0 AND f.StockQuantity < f.MinStock");
                if (stockStatus == "out")  conditions.Add("f.StockQuantity = 0");
            }

            var where = "WHERE " + string.Join(" AND ", conditions);

            var countSql = $"SELECT COUNT(*) FROM Fruits f {where}";
            var dataSql = $@"
                SELECT f.*, c.CategoryName, s.SupplierName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                LEFT JOIN Suppliers s ON f.SupplierId = s.SupplierId
                {where}
                ORDER BY f.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var param = new
            {
                Keyword    = $"%{keyword}%",
                CategoryId = categoryId ?? 0,
                Origin     = $"%{origin}%",
                MinPrice   = minPrice,
                MaxPrice   = maxPrice,
                Offset     = (page - 1) * pageSize,
                PageSize   = pageSize
            };

            var total = conn.ExecuteScalar<int>(countSql, param);
            var items = conn.Query<Fruit>(dataSql, param);
            return (items, total);
        }

        /// <summary>Tổng giá trị tồn kho hiện tại (RQ40)</summary>
        public virtual decimal GetTotalStockValue()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT ISNULL(SUM(CAST(StockQuantity AS DECIMAL(18,2)) * Price), 0)
                FROM Fruits WHERE IsActive = 1";
            return conn.ExecuteScalar<decimal>(sql);
        }

        /// <summary>Tổng số lượng tồn kho</summary>
        public virtual int GetTotalStockQuantity()
        {
            using var conn = _context.CreateConnection();
            return conn.ExecuteScalar<int>("SELECT ISNULL(SUM(StockQuantity), 0) FROM Fruits WHERE IsActive = 1");
        }

        /// <summary>
        /// Lấy tất cả trái cây (cho Admin Tồn Kho v.v)
        /// </summary>
        public virtual IEnumerable<Fruit> GetAll()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName, s.SupplierName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                LEFT JOIN Suppliers s ON f.SupplierId = s.SupplierId
                ORDER BY f.FruitName";
            return conn.Query<Fruit>(sql);
        }

        /// <summary>
        /// Lấy tất cả trái cây đang hoạt động (dùng cho trang customer)
        /// </summary>
        public virtual IEnumerable<Fruit> GetAllActive()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName, s.SupplierName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                LEFT JOIN Suppliers s ON f.SupplierId = s.SupplierId
                WHERE f.IsActive = 1
                ORDER BY f.FruitName";
            return conn.Query<Fruit>(sql);
        }

        /// <summary>
        /// Lấy trái cây theo ID
        /// </summary>
        public virtual Fruit? GetById(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName, s.SupplierName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                LEFT JOIN Suppliers s ON f.SupplierId = s.SupplierId
                WHERE f.FruitId = @FruitId";
            return conn.QueryFirstOrDefault<Fruit>(sql, new { FruitId = fruitId });
        }

        /// <summary>
        /// Lấy trái cây theo danh mục (cho trang customer)
        /// </summary>
        public virtual IEnumerable<Fruit> GetByCategory(int categoryId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                WHERE f.CategoryId = @CategoryId AND f.IsActive = 1
                ORDER BY f.FruitName";
            return conn.Query<Fruit>(sql, new { CategoryId = categoryId });
        }

        /// <summary>
        /// Autocomplete: tìm tên trái cây theo từ khóa (tối đa 10 kết quả)
        /// </summary>
        public virtual IEnumerable<string> AutoComplete(string keyword)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT TOP 10 FruitName
                FROM Fruits
                WHERE FruitName LIKE @Keyword AND IsActive = 1
                ORDER BY FruitName";
            return conn.Query<string>(sql, new { Keyword = $"%{keyword}%" });
        }

        /// <summary>
        /// Kiểm tra tên trái cây đã tồn tại trong danh mục chưa
        /// </summary>
        public virtual bool NameExistsInCategory(string name, int categoryId, int excludeId = 0)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT COUNT(*) FROM Fruits
                WHERE FruitName = @Name AND CategoryId = @CategoryId
                  AND FruitId != @ExcludeId AND IsActive = 1";
            return conn.ExecuteScalar<int>(sql, new { Name = name, CategoryId = categoryId, ExcludeId = excludeId }) > 0;
        }

        /// <summary>
        /// Kiểm tra trái cây có trong đơn hàng không (trước khi xóa)
        /// </summary>
        public virtual bool IsInOrder(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM OrderDetails WHERE FruitId = @FruitId";
            return conn.ExecuteScalar<int>(sql, new { FruitId = fruitId }) > 0;
        }

        /// <summary>
        /// Đếm tổng số trái cây đang hoạt động
        /// </summary>
        public virtual int CountActive()
        {
            using var conn = _context.CreateConnection();
            return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Fruits WHERE IsActive = 1");
        }

        /// <summary>
        /// Lấy danh sách trái cây tồn kho thấp (< threshold)
        /// </summary>
        public virtual IEnumerable<Fruit> GetLowStock(int threshold = 10)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                WHERE f.IsActive = 1 AND f.StockQuantity < @Threshold
                ORDER BY f.StockQuantity ASC";
            return conn.Query<Fruit>(sql, new { Threshold = threshold });
        }

        /// <summary>
        /// Thêm trái cây mới
        /// </summary>
        public virtual int Insert(Fruit fruit)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Fruits (FruitName, CategoryId, SupplierId, Price, StockQuantity, MinStock, Unit, Origin, Description, ImageUrl, IsActive)
                VALUES (@FruitName, @CategoryId, @SupplierId, @Price, @StockQuantity, @MinStock, @Unit, @Origin, @Description, @ImageUrl, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return conn.ExecuteScalar<int>(sql, fruit);
        }

        /// <summary>
        /// Cập nhật thông tin trái cây
        /// </summary>
        public virtual void Update(Fruit fruit)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Fruits SET
                    FruitName     = @FruitName,
                    CategoryId    = @CategoryId,
                    SupplierId    = @SupplierId,
                    Price         = @Price,
                    StockQuantity = @StockQuantity,
                    MinStock      = @MinStock,
                    Unit          = @Unit,
                    Origin        = @Origin,
                    Description   = @Description,
                    ImageUrl      = @ImageUrl,
                    IsActive      = @IsActive
                WHERE FruitId = @FruitId";
            conn.Execute(sql, fruit);
        }

        /// <summary>
        /// Giảm tồn kho khi xác nhận đơn hàng
        /// </summary>
        public virtual void DeductStock(int fruitId, int quantity)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Fruits SET StockQuantity = StockQuantity - @Quantity
                WHERE FruitId = @FruitId AND StockQuantity >= @Quantity";
            conn.Execute(sql, new { FruitId = fruitId, Quantity = quantity });
        }

        /// <summary>
        /// Xóa mềm trái cây
        /// </summary>
        public virtual void SoftDelete(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Fruits SET IsActive = 0 WHERE FruitId = @FruitId";
            conn.Execute(sql, new { FruitId = fruitId });
        }

        /// <summary>
        /// Top 5 trái cây bán chạy (dùng cho Dashboard)
        /// </summary>
        public virtual IEnumerable<TopFruitItem> GetTopSelling(int top = 5)
        {
            using var conn = _context.CreateConnection();
            var sql = $@"
                SELECT TOP {top}
                    f.FruitName,
                    SUM(od.Quantity) AS TotalQuantity,
                    SUM(od.Quantity * od.UnitPrice) AS TotalRevenue
                FROM OrderDetails od
                INNER JOIN Fruits f ON od.FruitId = f.FruitId
                INNER JOIN Orders o ON od.OrderId = o.OrderId
                WHERE o.Status NOT IN ('Cancelled', 'Pending')
                GROUP BY f.FruitId, f.FruitName
                ORDER BY TotalQuantity DESC";
            return conn.Query<TopFruitItem>(sql);
        }

        public virtual IEnumerable<Fruit> GetRelated(int fruitId, int categoryId, int count = 4)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT TOP (@Count) f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                WHERE f.CategoryId = @CategoryId AND f.FruitId != @FruitId AND f.IsActive = 1
                ORDER BY NEWID()";
            return conn.Query<Fruit>(sql, new { FruitId = fruitId, CategoryId = categoryId, Count = count });
        }

        public virtual IEnumerable<Fruit> GetAlternatives(int fruitId, int categoryId, int count = 4)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT TOP (@Count) f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                WHERE f.CategoryId = @CategoryId AND f.FruitId != @FruitId AND f.IsActive = 1 AND f.StockQuantity > 0
                ORDER BY f.StockQuantity DESC";
            return conn.Query<Fruit>(sql, new { FruitId = fruitId, CategoryId = categoryId, Count = count });
        }

        // Phase 3: Category Inventory Report (RQ107)
        public virtual IEnumerable<CategoryInventoryReportItem> GetCategoryInventoryReport(int? categoryId = null)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT
                    c.CategoryId,
                    c.CategoryName,
                    COUNT(f.FruitId)                                                                          AS TotalProducts,
                    ISNULL(SUM(f.StockQuantity), 0)                                                           AS TotalQuantity,
                    ISNULL(SUM(CAST(f.StockQuantity AS DECIMAL(18,2)) * f.Price), 0)                          AS TotalValue,
                    SUM(CASE WHEN f.StockQuantity > 0 AND f.StockQuantity < f.MinStock THEN 1 ELSE 0 END) AS LowStockCount,
                    SUM(CASE WHEN f.StockQuantity = 0 THEN 1 ELSE 0 END)                                       AS OutOfStockCount
                FROM Categories c
                LEFT JOIN Fruits f ON c.CategoryId = f.CategoryId AND f.IsActive = 1
                WHERE (@CategoryId IS NULL OR c.CategoryId = @CategoryId)
                GROUP BY c.CategoryId, c.CategoryName
                ORDER BY TotalValue DESC";
            return conn.Query<CategoryInventoryReportItem>(sql, new { CategoryId = categoryId });
        }
    }
}
