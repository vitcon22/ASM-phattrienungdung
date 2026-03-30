using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class WishlistRepository
    {
        private readonly FruitShopContext _context;

        public WishlistRepository(FruitShopContext context)
        {
            _context = context;
        }

        public IEnumerable<Wishlist> GetByUser(int userId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT w.*, f.FruitName, f.Price, f.ImageUrl, f.Unit,
                       CAST(CASE WHEN f.StockQuantity > 0 AND f.IsActive = 1 THEN 1 ELSE 0 END AS BIT) AS InStock
                FROM Wishlists w
                INNER JOIN Fruits f ON w.FruitId = f.FruitId
                WHERE w.UserId = @UserId
                ORDER BY w.AddedAt DESC";
            return conn.Query<Wishlist>(sql, new { UserId = userId });
        }

        public bool IsInWishlist(int userId, int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Wishlists WHERE UserId = @UserId AND FruitId = @FruitId";
            return conn.ExecuteScalar<int>(sql, new { UserId = userId, FruitId = fruitId }) > 0;
        }

        public void Toggle(int userId, int fruitId)
        {
            using var conn = _context.CreateConnection();
            if (IsInWishlist(userId, fruitId))
            {
                conn.Execute("DELETE FROM Wishlists WHERE UserId = @UserId AND FruitId = @FruitId", 
                             new { UserId = userId, FruitId = fruitId });
            }
            else
            {
                conn.Execute("INSERT INTO Wishlists (UserId, FruitId) VALUES (@UserId, @FruitId)", 
                             new { UserId = userId, FruitId = fruitId });
            }
        }
    }
}
