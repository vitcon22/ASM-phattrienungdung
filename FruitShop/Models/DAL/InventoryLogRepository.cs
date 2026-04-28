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

        public virtual IEnumerable<InventoryLog> GetLogs(int? fruitId = null)
        {
            using var conn = _context.CreateConnection();
            var conditions = new List<string> { "1=1" };
            var param = new DynamicParameters();
            if (fruitId.HasValue && fruitId.Value > 0)
            {
                conditions.Add("l.FruitId = @FruitId");
                param.Add("FruitId", fruitId.Value);
            }

            var where = "WHERE " + string.Join(" AND ", conditions);
            var sql = $@"
                SELECT l.*, f.FruitName, u.FullName AS StaffName,
                       f.StockQuantity AS NewStockQuantity
                FROM InventoryLogs l
                INNER JOIN Fruits f ON l.FruitId = f.FruitId
                INNER JOIN Users u ON l.StaffId = u.UserId
                {where}
                ORDER BY l.CreatedAt DESC
                OFFSET 0 ROWS FETCH NEXT 200 ROWS ONLY";
            return conn.Query<InventoryLog>(sql, param);
        }

        public virtual void AddLogAndAdjustStock(InventoryLog log)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                const string logSql = @"
                    INSERT INTO InventoryLogs (FruitId, StaffId, QuantityChange, Reason)
                    VALUES (@FruitId, @StaffId, @QuantityChange, @Reason)";
                conn.Execute(logSql, log, tran);

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
