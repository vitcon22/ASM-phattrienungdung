using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class OperatingCostRepository
    {
        private readonly FruitShopContext _context;

        public OperatingCostRepository(FruitShopContext context)
        {
            _context = context;
        }

        public IEnumerable<OperatingCost> GetByMonthYear(int month, int year)
        {
            using var conn = _context.CreateConnection();
            return conn.Query<OperatingCost>(
                "SELECT * FROM OperatingCosts WHERE Month = @Month AND Year = @Year ORDER BY CostType",
                new { Month = month, Year = year });
        }

        public IEnumerable<OperatingCost> GetAll()
        {
            using var conn = _context.CreateConnection();
            return conn.Query<OperatingCost>(
                "SELECT * FROM OperatingCosts ORDER BY Year DESC, Month DESC, CostType");
        }

        public decimal GetTotalByMonthYear(int month, int year)
        {
            using var conn = _context.CreateConnection();
            return conn.ExecuteScalar<decimal>(
                "SELECT ISNULL(SUM(Amount), 0) FROM OperatingCosts WHERE Month = @Month AND Year = @Year",
                new { Month = month, Year = year });
        }

        public int Insert(OperatingCost cost)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO OperatingCosts (Month, Year, CostType, Amount, Note)
                VALUES (@Month, @Year, @CostType, @Amount, @Note);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return conn.ExecuteScalar<int>(sql, cost);
        }

        public void Update(OperatingCost cost)
        {
            using var conn = _context.CreateConnection();
            conn.Execute(@"
                UPDATE OperatingCosts SET
                    Month = @Month, Year = @Year, CostType = @CostType,
                    Amount = @Amount, Note = @Note
                WHERE CostId = @CostId", cost);
        }

        public void Delete(int costId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("DELETE FROM OperatingCosts WHERE CostId = @CostId", new { CostId = costId });
        }

        public OperatingCost? GetById(int costId)
        {
            using var conn = _context.CreateConnection();
            return conn.QueryFirstOrDefault<OperatingCost>(
                "SELECT * FROM OperatingCosts WHERE CostId = @CostId", new { CostId = costId });
        }

        /// <summary>Lấy tổng giá vốn (COGS) của đơn Delivered trong tháng (RQ32)</summary>
        public decimal GetCOGSByMonth(int month, int year)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT ISNULL(SUM(od.Quantity * b.BuyPrice), 0)
                FROM OrderDetails od
                INNER JOIN Orders o ON od.OrderId = o.OrderId
                INNER JOIN Fruits f ON od.FruitId = f.FruitId
                INNER JOIN Batches b ON f.FruitId = b.FruitId
                WHERE MONTH(o.OrderDate) = @Month
                  AND YEAR(o.OrderDate) = @Year
                  AND o.Status = 'Delivered'
                  AND b.BatchId IN (
                      SELECT TOP 1 b2.BatchId
                      FROM Batches b2
                      WHERE b2.FruitId = f.FruitId
                      ORDER BY b2.ImportDate DESC
                  )";
            return conn.ExecuteScalar<decimal>(sql, new { Month = month, Year = year });
        }

        /// <summary>Lấy COGS đơn giản: sum(quantity * fruit.Price) từ Batches nhập gần nhất (RQ32)</summary>
        public decimal GetCOGSSimple(int month, int year)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT ISNULL(SUM(od.Quantity * (
                    SELECT TOP 1 b3.BuyPrice
                    FROM Batches b3
                    WHERE b3.FruitId = od.FruitId
                    ORDER BY b3.ImportDate DESC
                )), 0)
                FROM OrderDetails od
                INNER JOIN Orders o ON od.OrderId = o.OrderId
                WHERE MONTH(o.OrderDate) = @Month
                  AND YEAR(o.OrderDate) = @Year
                  AND o.Status = 'Delivered'";
            return conn.ExecuteScalar<decimal>(sql, new { Month = month, Year = year });
        }
    }
}
