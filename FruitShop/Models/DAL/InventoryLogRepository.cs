using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class InventoryLogRepository
    {
        private readonly FruitShopContext _context;

        public InventoryLogRepository(FruitShopContext context)
        {
            _context = context;
        }

        public IEnumerable<InventoryLog> GetLogs(int limit = 50)
        {
            using var conn = _context.CreateConnection();
            var sql = $@"
                SELECT TOP {limit} l.*, f.FruitName, u.FullName AS StaffName
                FROM InventoryLogs l
                INNER JOIN Fruits f ON l.FruitId = f.FruitId
                INNER JOIN Users u ON l.StaffId = u.UserId
                ORDER BY l.CreatedAt DESC";
            return conn.Query<InventoryLog>(sql);
        }

        public void AddLogAndAdjustStock(InventoryLog log)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                // Thêm log
                const string logSql = @"
                    INSERT INTO InventoryLogs (FruitId, StaffId, QuantityChange, Reason)
                    VALUES (@FruitId, @StaffId, @QuantityChange, @Reason)";
                conn.Execute(logSql, log, tran);

                // Cập nhật tồn kho
                const string updateSql = @"
                    UPDATE Fruits SET StockQuantity = StockQuantity + @QuantityChange
                    WHERE FruitId = @FruitId";
                conn.Execute(updateSql, new { log.QuantityChange, log.FruitId }, tran);

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }
    }
}
