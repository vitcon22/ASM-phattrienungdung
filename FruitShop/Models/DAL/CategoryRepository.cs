using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    /// <summary>
    /// Repository xử lý các thao tác liên quan đến Categories (Danh mục trái cây)
    /// </summary>
    public class CategoryRepository
    {
        private readonly FruitShopContext _context;

        public CategoryRepository(FruitShopContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả danh mục đang hoạt động (có kèm số lượng sản phẩm)
        /// </summary>
        public IEnumerable<Category> GetAll(bool includeInactive = false)
        {
            using var conn = _context.CreateConnection();
            var sql = @"
                SELECT c.*,
                       (SELECT COUNT(*) FROM Fruits f WHERE f.CategoryId = c.CategoryId AND f.IsActive = 1) AS FruitCount
                FROM Categories c
                WHERE (@IncludeInactive = 1 OR c.IsActive = 1)
                ORDER BY c.CategoryName";
            return conn.Query<Category>(sql, new { IncludeInactive = includeInactive ? 1 : 0 });
        }

        /// <summary>
        /// Tìm kiếm danh mục theo tên với phân trang
        /// </summary>
        public (IEnumerable<Category> Items, int TotalCount) Search(string? keyword, int page, int pageSize)
        {
            using var conn = _context.CreateConnection();
            var whereClause = string.IsNullOrWhiteSpace(keyword)
                ? "WHERE c.IsActive = 1"
                : "WHERE c.IsActive = 1 AND c.CategoryName LIKE @Keyword";

            var countSql = $"SELECT COUNT(*) FROM Categories c {whereClause}";
            var dataSql = $@"
                SELECT c.*,
                       (SELECT COUNT(*) FROM Fruits f WHERE f.CategoryId = c.CategoryId AND f.IsActive = 1) AS FruitCount
                FROM Categories c
                {whereClause}
                ORDER BY c.CategoryId
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var param = new { Keyword = $"%{keyword}%", Offset = (page - 1) * pageSize, PageSize = pageSize };
            var total = conn.ExecuteScalar<int>(countSql, param);
            var items = conn.Query<Category>(dataSql, param);
            return (items, total);
        }

        /// <summary>
        /// Lấy danh mục theo ID
        /// </summary>
        public Category? GetById(int categoryId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT c.*,
                       (SELECT COUNT(*) FROM Fruits f WHERE f.CategoryId = c.CategoryId AND f.IsActive = 1) AS FruitCount
                FROM Categories c
                WHERE c.CategoryId = @CategoryId";
            return conn.QueryFirstOrDefault<Category>(sql, new { CategoryId = categoryId });
        }

        /// <summary>
        /// Kiểm tra tên danh mục đã tồn tại chưa
        /// </summary>
        public bool NameExists(string name, int excludeId = 0)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Categories WHERE CategoryName = @Name AND CategoryId != @ExcludeId AND IsActive = 1";
            return conn.ExecuteScalar<int>(sql, new { Name = name, ExcludeId = excludeId }) > 0;
        }

        /// <summary>
        /// Kiểm tra danh mục còn sản phẩm không (trước khi xóa)
        /// </summary>
        public bool HasFruits(int categoryId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Fruits WHERE CategoryId = @CategoryId AND IsActive = 1";
            return conn.ExecuteScalar<int>(sql, new { CategoryId = categoryId }) > 0;
        }

        /// <summary>
        /// Thêm danh mục mới
        /// </summary>
        public void Insert(Category category)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Categories (CategoryName, Description, IsActive)
                VALUES (@CategoryName, @Description, @IsActive)";
            conn.Execute(sql, category);
        }

        /// <summary>
        /// Cập nhật danh mục
        /// </summary>
        public void Update(Category category)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Categories SET
                    CategoryName = @CategoryName,
                    Description  = @Description,
                    IsActive     = @IsActive
                WHERE CategoryId = @CategoryId";
            conn.Execute(sql, category);
        }

        /// <summary>
        /// Xóa mềm danh mục (IsActive = false)
        /// </summary>
        public void SoftDelete(int categoryId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Categories SET IsActive = 0 WHERE CategoryId = @CategoryId";
            conn.Execute(sql, new { CategoryId = categoryId });
        }
    }
}
