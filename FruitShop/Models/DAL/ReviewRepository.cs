using Dapper;
using FruitShop.Models.Entities;

namespace FruitShop.Models.DAL
{
    public class ReviewRepository
    {
        private readonly FruitShopContext _context;

        public ReviewRepository(FruitShopContext context)
        {
            _context = context;
        }

        public IEnumerable<Review> GetByFruitId(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT r.*, u.FullName AS CustomerName
                FROM Reviews r
                INNER JOIN Users u ON r.UserId = u.UserId
                WHERE r.FruitId = @FruitId
                ORDER BY r.CreatedAt DESC";
            return conn.Query<Review>(sql, new { FruitId = fruitId });
        }

        public void Insert(Review review)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Reviews (FruitId, UserId, Rating, Comment)
                VALUES (@FruitId, @UserId, @Rating, @Comment)";
            conn.Execute(sql, review);
        }

        public double GetAverageRating(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT ISNULL(AVG(CAST(Rating AS FLOAT)), 0) FROM Reviews WHERE FruitId = @FruitId";
            return conn.ExecuteScalar<double>(sql, new { FruitId = fruitId });
        }
        
        public int GetReviewCount(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Reviews WHERE FruitId = @FruitId";
            return conn.ExecuteScalar<int>(sql, new { FruitId = fruitId });
        }
    }
}
