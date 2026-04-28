using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class AuditLogRepository
    {
        private readonly FruitShopContext _context;

        public AuditLogRepository(FruitShopContext context)
        {
            _context = context;
        }

        public virtual void Insert(AuditLog log)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO AuditLogs (UserId, ActionName, ControllerName, Details, IpAddress, Timestamp)
                VALUES (@UserId, @ActionName, @ControllerName, @Details, @IpAddress, GETDATE())";
            conn.Execute(sql, log);
        }

        public virtual IEnumerable<AuditLog> GetAll()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT a.*, u.FullName as UserName
                FROM AuditLogs a
                LEFT JOIN Users u ON a.UserId = u.UserId
                ORDER BY a.Timestamp DESC";
            return conn.Query<AuditLog>(sql);
        }
        
        public virtual IEnumerable<AuditLog> GetRecent(int count)
        {
            using var conn = _context.CreateConnection();
            var sql = $@"
                SELECT TOP {count} a.*, u.FullName as UserName
                FROM AuditLogs a
                LEFT JOIN Users u ON a.UserId = u.UserId
                ORDER BY a.Timestamp DESC";
            return conn.Query<AuditLog>(sql);
        }

        public virtual IEnumerable<AuditLog> Search(string? keyword, string? controller, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            using var conn = _context.CreateConnection();
            var conditions = new List<string> { "1=1" };
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("(u.FullName LIKE @Keyword OR a.ActionName LIKE @Keyword OR a.Details LIKE @Keyword)");
            if (!string.IsNullOrWhiteSpace(controller))
                conditions.Add("a.ControllerName = @Controller");
            if (fromDate.HasValue)
                conditions.Add("a.Timestamp >= @FromDate");
            if (toDate.HasValue)
                conditions.Add("a.Timestamp < DATEADD(day, 1, @ToDate)");

            var where = "WHERE " + string.Join(" AND ", conditions);
            var sql = $@"
                SELECT a.*, u.FullName as UserName
                FROM AuditLogs a
                LEFT JOIN Users u ON a.UserId = u.UserId
                {where}
                ORDER BY a.Timestamp DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            return conn.Query<AuditLog>(sql, new
            {
                Keyword = $"%{keyword}%",
                Controller = controller,
                FromDate = fromDate,
                ToDate = toDate,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            });
        }

        public virtual int Count(string? keyword, string? controller, DateTime? fromDate, DateTime? toDate)
        {
            using var conn = _context.CreateConnection();
            var conditions = new List<string> { "1=1" };
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("(u.FullName LIKE @Keyword OR a.ActionName LIKE @Keyword OR a.Details LIKE @Keyword)");
            if (!string.IsNullOrWhiteSpace(controller))
                conditions.Add("a.ControllerName = @Controller");
            if (fromDate.HasValue)
                conditions.Add("a.Timestamp >= @FromDate");
            if (toDate.HasValue)
                conditions.Add("a.Timestamp < DATEADD(day, 1, @ToDate)");

            var where = "WHERE " + string.Join(" AND ", conditions);
            var sql = $@"
                SELECT COUNT(*) FROM AuditLogs a
                LEFT JOIN Users u ON a.UserId = u.UserId
                {where}";
            return conn.ExecuteScalar<int>(sql, new
            {
                Keyword = $"%{keyword}%",
                Controller = controller,
                FromDate = fromDate,
                ToDate = toDate
            });
        }

        public virtual IEnumerable<string> GetDistinctControllers()
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT DISTINCT ControllerName FROM AuditLogs ORDER BY ControllerName";
            return conn.Query<string>(sql);
        }
    }
}
