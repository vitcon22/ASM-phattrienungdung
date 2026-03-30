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
        /// Lấy tất cả trái cây có phân trang, lọc và tìm kiếm
        /// </summary>
        public (IEnumerable<Fruit> Items, int TotalCount) Search(
            string? keyword, int? categoryId, int page, int pageSize)
        {
            using var conn = _context.CreateConnection();

            var conditions = new List<string> { "f.IsActive = 1" };
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("f.FruitName LIKE @Keyword");
            if (categoryId.HasValue && categoryId > 0)
                conditions.Add("f.CategoryId = @CategoryId");

            var where = "WHERE " + string.Join(" AND ", conditions);

            var countSql = $"SELECT COUNT(*) FROM Fruits f {where}";
            var dataSql = $@"
                SELECT f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                {where}
                ORDER BY f.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var param = new
            {
                Keyword    = $"%{keyword}%",
                CategoryId = categoryId ?? 0,
                Offset     = (page - 1) * pageSize,
                PageSize   = pageSize
            };

            var total = conn.ExecuteScalar<int>(countSql, param);
            var items = conn.Query<Fruit>(dataSql, param);
            return (items, total);
        }

        /// <summary>
        /// Lấy tất cả trái cây (cho Admin Tồn Kho v.v)
        /// </summary>
        public IEnumerable<Fruit> GetAll()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                ORDER BY f.FruitName";
            return conn.Query<Fruit>(sql);
        }

        /// <summary>
        /// Lấy tất cả trái cây đang hoạt động (dùng cho trang customer)
        /// </summary>
        public IEnumerable<Fruit> GetAllActive()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                WHERE f.IsActive = 1
                ORDER BY f.FruitName";
            return conn.Query<Fruit>(sql);
        }

        /// <summary>
        /// Lấy trái cây theo ID
        /// </summary>
        public Fruit? GetById(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT f.*, c.CategoryName
                FROM Fruits f
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                WHERE f.FruitId = @FruitId";
            return conn.QueryFirstOrDefault<Fruit>(sql, new { FruitId = fruitId });
        }

        /// <summary>
        /// Lấy trái cây theo danh mục (cho trang customer)
        /// </summary>
        public IEnumerable<Fruit> GetByCategory(int categoryId)
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
        public IEnumerable<string> AutoComplete(string keyword)
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
        public bool NameExistsInCategory(string name, int categoryId, int excludeId = 0)
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
        public bool IsInOrder(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM OrderDetails WHERE FruitId = @FruitId";
            return conn.ExecuteScalar<int>(sql, new { FruitId = fruitId }) > 0;
        }

        /// <summary>
        /// Đếm tổng số trái cây đang hoạt động
        /// </summary>
        public int CountActive()
        {
            using var conn = _context.CreateConnection();
            return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Fruits WHERE IsActive = 1");
        }

        /// <summary>
        /// Lấy danh sách trái cây tồn kho thấp (< threshold)
        /// </summary>
        public IEnumerable<Fruit> GetLowStock(int threshold = 10)
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
        public int Insert(Fruit fruit)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Fruits (FruitName, CategoryId, Price, StockQuantity, Unit, Origin, Description, ImageUrl, IsActive)
                VALUES (@FruitName, @CategoryId, @Price, @StockQuantity, @Unit, @Origin, @Description, @ImageUrl, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return conn.ExecuteScalar<int>(sql, fruit);
        }

        /// <summary>
        /// Cập nhật thông tin trái cây
        /// </summary>
        public void Update(Fruit fruit)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Fruits SET
                    FruitName     = @FruitName,
                    CategoryId    = @CategoryId,
                    Price         = @Price,
                    StockQuantity = @StockQuantity,
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
        public void DeductStock(int fruitId, int quantity)
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
        public void SoftDelete(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Fruits SET IsActive = 0 WHERE FruitId = @FruitId";
            conn.Execute(sql, new { FruitId = fruitId });
        }

        /// <summary>
        /// Top 5 trái cây bán chạy (dùng cho Dashboard)
        /// </summary>
        public IEnumerable<TopFruitItem> GetTopSelling(int top = 5)
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
    }
}
