using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class SupplierRepository
    {
        private readonly FruitShopContext _context;

        public SupplierRepository(FruitShopContext context)
        {
            _context = context;
        }

        public virtual IEnumerable<Supplier> GetAll()
        {
            using var conn = _context.CreateConnection();
            return conn.Query<Supplier>("SELECT * FROM Suppliers ORDER BY SupplierName");
        }

        public virtual IEnumerable<Supplier> GetAllActive()
        {
            using var conn = _context.CreateConnection();
            return conn.Query<Supplier>("SELECT * FROM Suppliers WHERE IsActive = 1 ORDER BY SupplierName");
        }

        public virtual Supplier? GetById(int supplierId)
        {
            using var conn = _context.CreateConnection();
            return conn.QueryFirstOrDefault<Supplier>("SELECT * FROM Suppliers WHERE SupplierId = @Id", new { Id = supplierId });
        }

        public virtual int Insert(Supplier supplier)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Suppliers (SupplierName, ContactName, Phone, Email, Address, IsActive)
                VALUES (@SupplierName, @ContactName, @Phone, @Email, @Address, @IsActive);
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            return conn.ExecuteScalar<int>(sql, supplier);
        }

        public virtual void Update(Supplier supplier)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Suppliers SET
                    SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Phone = @Phone,
                    Email = @Email,
                    Address = @Address,
                    IsActive = @IsActive
                WHERE SupplierId = @SupplierId";
            conn.Execute(sql, supplier);
        }

        public virtual bool IsInUse(int supplierId)
        {
            using var conn = _context.CreateConnection();
            return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Fruits WHERE SupplierId = @SupplierId") > 0;
        }

        public virtual void Delete(int supplierId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("DELETE FROM Suppliers WHERE SupplierId = @SupplierId", new { SupplierId = supplierId });
        }

        // Phân trang cho AdminSupplier/Index
        public virtual (IEnumerable<Supplier> Items, int TotalCount) Search(string? keyword, int page, int pageSize)
        {
            using var conn = _context.CreateConnection();
            var where = string.IsNullOrWhiteSpace(keyword)
                ? ""
                : "WHERE s.SupplierName LIKE @Keyword OR s.ContactName LIKE @Keyword OR s.Phone LIKE @Keyword";
            var countSql = $"SELECT COUNT(*) FROM Suppliers s {where}";
            var dataSql = $@"
                SELECT s.* FROM Suppliers s
                {where}
                ORDER BY s.SupplierName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var p = new { Keyword = $"%{keyword}%", Offset = (page - 1) * pageSize, PageSize = pageSize };
            return (conn.Query<Supplier>(dataSql, p), conn.ExecuteScalar<int>(countSql, p));
        }
    }
}
