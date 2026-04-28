using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class BatchRepository
    {
        private readonly FruitShopContext _context;

        public BatchRepository(FruitShopContext context)
        {
            _context = context;
        }

        public virtual IEnumerable<Batch> GetAll()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT b.*, f.FruitName
                FROM Batches b
                INNER JOIN Fruits f ON b.FruitId = f.FruitId
                ORDER BY b.ImportDate DESC";
            return conn.Query<Batch>(sql);
        }

        public virtual IEnumerable<Batch> GetByFruitId(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT b.*, f.FruitName
                FROM Batches b
                INNER JOIN Fruits f ON b.FruitId = f.FruitId
                WHERE b.FruitId = @FruitId
                ORDER BY b.ExpiryDate ASC";
            return conn.Query<Batch>(sql, new { FruitId = fruitId });
        }

        public virtual Batch? GetById(int batchId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT b.*, f.FruitName
                FROM Batches b
                INNER JOIN Fruits f ON b.FruitId = f.FruitId
                WHERE b.BatchId = @Id";
            return conn.QueryFirstOrDefault<Batch>(sql, new { Id = batchId });
        }

        public virtual int Insert(Batch batch)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Batches (FruitId, BatchCode, ImportDate, ManufactureDate, ExpiryDate, BuyPrice, Quantity, RemainingQty)
                VALUES (@FruitId, @BatchCode, @ImportDate, @ManufactureDate, @ExpiryDate, @BuyPrice, @Quantity, @RemainingQty);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return conn.ExecuteScalar<int>(sql, batch);
        }

        public virtual void Update(Batch batch)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Batches SET
                    FruitId = @FruitId,
                    BatchCode = @BatchCode,
                    ImportDate = @ImportDate,
                    ManufactureDate = @ManufactureDate,
                    ExpiryDate = @ExpiryDate,
                    BuyPrice = @BuyPrice,
                    Quantity = @Quantity,
                    RemainingQty = @RemainingQty
                WHERE BatchId = @BatchId";
            conn.Execute(sql, batch);
        }

        public virtual void Delete(int batchId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("DELETE FROM Batches WHERE BatchId = @BatchId", new { BatchId = batchId });
        }

        public virtual IEnumerable<Batch> GetBySupplier(int supplierId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT b.*, f.FruitName, s.SupplierName
                FROM Batches b
                INNER JOIN Fruits f ON b.FruitId = f.FruitId
                LEFT JOIN Suppliers s ON f.SupplierId = s.SupplierId
                WHERE f.SupplierId = @SupplierId
                ORDER BY b.ImportDate DESC";
            return conn.Query<Batch>(sql, new { SupplierId = supplierId });
        }

        public virtual IEnumerable<dynamic> GetPriceComparisonBySupplier()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT
                    s.SupplierId, s.SupplierName,
                    f.FruitId, f.FruitName, f.Unit,
                    COUNT(b.BatchId)            AS BatchCount,
                    SUM(b.Quantity)             AS TotalQtyImported,
                    MIN(b.BuyPrice)             AS MinBuyPrice,
                    MAX(b.BuyPrice)             AS MaxBuyPrice,
                    AVG(b.BuyPrice)             AS AvgBuyPrice,
                    MAX(b.ImportDate)           AS LastImportDate
                FROM Batches b
                INNER JOIN Fruits f ON b.FruitId = f.FruitId
                INNER JOIN Suppliers s ON f.SupplierId = s.SupplierId
                GROUP BY s.SupplierId, s.SupplierName, f.FruitId, f.FruitName, f.Unit
                ORDER BY s.SupplierName, f.FruitName";
            return conn.Query<dynamic>(sql);
        }

        public virtual IEnumerable<Batch> GetExpiringSoon(int days = 3)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT b.*, f.FruitName
                FROM Batches b
                INNER JOIN Fruits f ON b.FruitId = f.FruitId
                WHERE b.ExpiryDate BETWEEN GETDATE() AND DATEADD(day, @Days, GETDATE())
                  AND b.RemainingQty > 0
                ORDER BY b.ExpiryDate ASC";
            return conn.Query<Batch>(sql, new { Days = days });
        }

        public virtual IEnumerable<Batch> Search(int? fruitId, string? keyword, int page, int pageSize)
        {
            using var conn = _context.CreateConnection();
            var conditions = new List<string> { "1=1" };
            if (fruitId.HasValue && fruitId > 0)
                conditions.Add("b.FruitId = @FruitId");
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("(b.BatchCode LIKE @Keyword OR f.FruitName LIKE @Keyword)");
            var where = "WHERE " + string.Join(" AND ", conditions);
            var sql = $@"
                SELECT b.*, f.FruitName
                FROM Batches b
                INNER JOIN Fruits f ON b.FruitId = f.FruitId
                {where}
                ORDER BY b.ImportDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            return conn.Query<Batch>(sql, new { FruitId = fruitId, Keyword = $"%{keyword}%", Offset = (page - 1) * pageSize, PageSize = pageSize });
        }

        public virtual int Count(int? fruitId, string? keyword)
        {
            using var conn = _context.CreateConnection();
            var conditions = new List<string> { "1=1" };
            if (fruitId.HasValue && fruitId > 0)
                conditions.Add("b.FruitId = @FruitId");
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("(b.BatchCode LIKE @Keyword OR f.FruitName LIKE @Keyword)");
            var where = "WHERE " + string.Join(" AND ", conditions);
            var sql = $"SELECT COUNT(*) FROM Batches b INNER JOIN Fruits f ON b.FruitId = f.FruitId {where}";
            return conn.ExecuteScalar<int>(sql, new { FruitId = fruitId, Keyword = $"%{keyword}%" });
        }
    }
}
