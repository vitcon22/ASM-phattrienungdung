using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    /// <summary>
    /// Repository xử lý các thao tác liên quan đến User
    /// </summary>
    public class UserRepository
    {
        private readonly FruitShopContext _context;

        public UserRepository(FruitShopContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy user theo email để đăng nhập
        /// </summary>
        public User? GetByEmail(string email)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT u.*, r.RoleName
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                WHERE u.Email = @Email AND u.IsActive = 1";
            return conn.QueryFirstOrDefault<User>(sql, new { Email = email });
        }

        /// <summary>
        /// Lấy user theo ID
        /// </summary>
        public User? GetById(int userId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT u.*, r.RoleName
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                WHERE u.UserId = @UserId";
            return conn.QueryFirstOrDefault<User>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Lấy tất cả users
        /// </summary>
        public IEnumerable<User> GetAll()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT u.*, r.RoleName
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                ORDER BY u.CreatedAt DESC";
            return conn.Query<User>(sql);
        }

        /// <summary>
        /// Đếm số lượng khách hàng
        /// </summary>
        public int CountCustomers()
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Users WHERE RoleId = 3 AND IsActive = 1";
            return conn.ExecuteScalar<int>(sql);
        }

        /// <summary>
        /// Kiểm tra email đã tồn tại chưa
        /// </summary>
        public bool EmailExists(string email, int excludeUserId = 0)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Users WHERE Email = @Email AND UserId != @ExcludeUserId";
            return conn.ExecuteScalar<int>(sql, new { Email = email, ExcludeUserId = excludeUserId }) > 0;
        }

        /// <summary>
        /// Thêm user mới (đăng ký)
        /// </summary>
        public int Insert(User user)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Users (FullName, Email, Password, Phone, Address, RoleId, IsActive)
                VALUES (@FullName, @Email, @Password, @Phone, @Address, @RoleId, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return conn.ExecuteScalar<int>(sql, user);
        }

        /// <summary>
        /// Cập nhật thông tin cá nhân user
        /// </summary>
        public void Update(User user)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Users SET
                    FullName = @FullName,
                    Phone    = @Phone,
                    Address  = @Address
                WHERE UserId = @UserId";
            conn.Execute(sql, user);
        }

        /// <summary>Alias for Update - used by Profile page</summary>
        public void UpdateProfile(User user) => Update(user);

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public void UpdatePassword(int userId, string hashedPassword)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Users SET Password = @Password WHERE UserId = @UserId";
            conn.Execute(sql, new { Password = hashedPassword, UserId = userId });
        }

        /// <summary>Xóa mềm user (IsActive = false)</summary>
        public void SoftDelete(int userId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Users SET IsActive = 0 WHERE UserId = @UserId";
            conn.Execute(sql, new { UserId = userId });
        }

        /// <summary>Bật/tắt trạng thái user</summary>
        public void ToggleActive(int userId, bool isActive)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Users SET IsActive = @IsActive WHERE UserId = @UserId";
            conn.Execute(sql, new { IsActive = isActive, UserId = userId });
        }

        /// <summary>Tìm kiếm user có phân trang</summary>
        public (IEnumerable<User> Items, int TotalCount) Search(string? keyword, string? role, int page, int pageSize)
        {
            using var conn = _context.CreateConnection();
            var conditions = new List<string> { "1=1" };
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("(u.FullName LIKE @Keyword OR u.Email LIKE @Keyword OR u.Phone LIKE @Keyword)");
            if (!string.IsNullOrWhiteSpace(role))
                conditions.Add("r.RoleName = @Role");

            var where = "WHERE " + string.Join(" AND ", conditions);
            var countSql = $"SELECT COUNT(*) FROM Users u INNER JOIN Roles r ON u.RoleId=r.RoleId {where}";
            var dataSql  = $@"
                SELECT u.*, r.RoleName FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                {where}
                ORDER BY u.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var param = new { Keyword = $"%{keyword}%", Role = role, Offset = (page - 1) * pageSize, PageSize = pageSize };
            return (conn.Query<User>(dataSql, param), conn.ExecuteScalar<int>(countSql, param));
        }
    }
}
