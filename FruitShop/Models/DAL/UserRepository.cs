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
        public virtual User? GetByEmail(string email)
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
        public virtual User? GetById(int userId)
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
        public virtual IEnumerable<User> GetAll()
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
        /// Đếm số lượng khách hàng đang hoạt động
        /// </summary>
        public virtual int CountCustomers()
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Users WHERE RoleId = 3 AND IsActive = 1";
            return conn.ExecuteScalar<int>(sql);
        }

        /// <summary>
        /// Đếm khách hàng mới đăng ký trong tháng hiện tại (RQ86)
        /// </summary>
        public virtual int CountNewCustomersThisMonth()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT COUNT(*) FROM Users
                WHERE RoleId = 3
                  AND MONTH(CreatedAt) = MONTH(GETDATE())
                  AND YEAR(CreatedAt) = YEAR(GETDATE())";
            return conn.ExecuteScalar<int>(sql);
        }

        public virtual int CountNewUsersToday()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT COUNT(*) FROM Users
                WHERE RoleId = 3
                  AND CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)";
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
        public virtual int Insert(User user)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Users (FullName, Email, Password, Phone, Address, RoleId, IsActive, Points, Tier, EmailConfirmed, VerificationToken)
                VALUES (@FullName, @Email, @Password, @Phone, @Address, @RoleId, @IsActive, @Points, @Tier, @EmailConfirmed, @VerificationToken);
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
        public virtual void UpdateProfile(User user) => Update(user);

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public virtual void UpdatePassword(int userId, string hashedPassword)
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
        public virtual void ToggleActive(int userId, bool isActive)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Users SET IsActive = @IsActive WHERE UserId = @UserId";
            conn.Execute(sql, new { IsActive = isActive, UserId = userId });
        }

        /// <summary>Tìm kiếm user có phân trang</summary>
        public virtual (IEnumerable<User> Items, int TotalCount) Search(string? keyword, string? role, int page, int pageSize)
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

        // --- Phase 2: Points & Tiers ---
        public void AddPoints(int userId, int pointsToAdd)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Users SET Points = Points + @PointsToAdd WHERE UserId = @UserId;
                UPDATE Users SET Tier =
                    CASE
                        WHEN Points >= 1000 THEN 'Platinum'
                        WHEN Points >= 500 THEN 'Gold'
                        WHEN Points >= 200 THEN 'Silver'
                        ELSE 'Standard'
                    END
                WHERE UserId = @UserId;";
            conn.Execute(sql, new { UserId = userId, PointsToAdd = pointsToAdd });
        }

        // RQ35: Trừ điểm khi khách dùng để đổi giảm giá
        public void RedeemPoints(int userId, int pointsToRedeem)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Users SET Points = Points - @PointsToRedeem
                WHERE UserId = @UserId AND Points >= @PointsToRedeem";
            conn.Execute(sql, new { UserId = userId, PointsToRedeem = pointsToRedeem });
        }

        // --- Phase 2: Tokens ---
        public User? GetByVerificationToken(string token)
        {
            using var conn = _context.CreateConnection();
            return conn.QueryFirstOrDefault<User>("SELECT * FROM Users WHERE VerificationToken = @Token AND IsActive = 1", new { Token = token });
        }

        public void ConfirmEmail(int userId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("UPDATE Users SET EmailConfirmed = 1, VerificationToken = NULL WHERE UserId = @UserId", new { UserId = userId });
        }

        public User? GetByResetToken(string token)
        {
            using var conn = _context.CreateConnection();
            return conn.QueryFirstOrDefault<User>("SELECT * FROM Users WHERE ResetToken = @Token AND ResetTokenExpiry > GETDATE() AND IsActive = 1", new { Token = token });
        }

        public void UpdateResetToken(int userId, string token, DateTime expiry)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("UPDATE Users SET ResetToken = @Token, ResetTokenExpiry = @Expiry WHERE UserId = @UserId", new { UserId = userId, Token = token, Expiry = expiry });
        }

        public void ClearResetToken(int userId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("UPDATE Users SET ResetToken = NULL, ResetTokenExpiry = NULL WHERE UserId = @UserId", new { UserId = userId });
        }

        // RQ14: Admin tạo Staff/Admin account
        public virtual int CreateByAdmin(User user)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Users (FullName, Email, Password, Phone, Address, RoleId, IsActive, Points, Tier, EmailConfirmed, VerificationToken)
                VALUES (@FullName, @Email, @Password, @Phone, @Address, @RoleId, @IsActive, 0, 'Standard', 1, NULL);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return conn.ExecuteScalar<int>(sql, new
            {
                user.FullName,
                user.Email,
                user.Password,
                user.Phone,
                user.Address,
                user.RoleId,
                user.IsActive
            });
        }

        // RQ14: Admin cập nhật Staff/Admin account
        public virtual void UpdateByAdmin(User user)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Users SET
                    FullName = @FullName,
                    Phone    = @Phone,
                    Address  = @Address,
                    RoleId   = @RoleId,
                    IsActive = @IsActive
                WHERE UserId = @UserId";
            conn.Execute(sql, user);
        }

        // RQ14: Lấy danh sách vai trò cho form tạo/sửa
        public virtual IEnumerable<(int RoleId, string RoleName)> GetRoles()
        {
            using var conn = _context.CreateConnection();
            return conn.Query<(int, string)>("SELECT RoleId, RoleName FROM Roles WHERE RoleId IN (1,2)");
        }

        // Export users to Excel (RQ101)
        public virtual IEnumerable<User> GetAllForExport()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT u.*, r.RoleName
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                ORDER BY r.RoleName, u.CreatedAt DESC";
            return conn.Query<User>(sql);
        }
    }
}
