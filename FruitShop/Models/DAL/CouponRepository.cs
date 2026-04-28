using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class CouponRepository
    {
        private readonly FruitShopContext _context;

        public CouponRepository(FruitShopContext context)
        {
            _context = context;
        }

        public virtual IEnumerable<Coupon> GetAll()
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT * FROM Coupons ORDER BY ExpiryDate DESC";
            return conn.Query<Coupon>(sql);
        }

        public virtual Coupon? GetByCode(string code)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT * FROM Coupons WHERE Code = @Code AND IsActive = 1 AND ExpiryDate >= GETDATE()";
            return conn.QueryFirstOrDefault<Coupon>(sql, new { Code = code });
        }

        public virtual void Insert(Coupon coupon)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Coupons (Code, DiscountPercent, ExpiryDate, IsActive)
                VALUES (@Code, @DiscountPercent, @ExpiryDate, @IsActive)";
            conn.Execute(sql, coupon);
        }

        public virtual void ToggleActive(int couponId, bool isActive)
        {
            using var conn = _context.CreateConnection();
            const string sql = "UPDATE Coupons SET IsActive = @IsActive WHERE CouponId = @CouponId";
            conn.Execute(sql, new { IsActive = isActive, CouponId = couponId });
        }
    }
}
