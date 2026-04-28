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

        public virtual IEnumerable<Review> GetByFruitId(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string reviewsSql = @"
                SELECT r.*, u.FullName AS CustomerName
                FROM Reviews r
                INNER JOIN Users u ON r.UserId = u.UserId
                WHERE r.FruitId = @FruitId AND r.IsApproved = 1
                ORDER BY r.CreatedAt DESC";

            const string imagesSql = @"
                SELECT ri.* FROM ReviewImages ri
                INNER JOIN Reviews r ON ri.ReviewId = r.ReviewId
                WHERE r.FruitId = @FruitId";

            var reviews = conn.Query<Review>(reviewsSql, new { FruitId = fruitId }).ToList();
            if (reviews.Count == 0) return reviews;

            var images = conn.Query<ReviewImage>(imagesSql, new { FruitId = fruitId })
                .GroupBy(img => img.ReviewId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var r in reviews)
                r.Images = images.GetValueOrDefault(r.ReviewId) ?? new List<ReviewImage>();

            return reviews;
        }

        public virtual int Insert(Review review)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                INSERT INTO Reviews (FruitId, UserId, Rating, Comment)
                VALUES (@FruitId, @UserId, @Rating, @Comment);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return conn.ExecuteScalar<int>(sql, review);
        }

        public virtual void InsertImage(int reviewId, string imageUrl)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("INSERT INTO ReviewImages (ReviewId, ImageUrl) VALUES (@ReviewId, @ImageUrl)", 
                new { ReviewId = reviewId, ImageUrl = imageUrl });
        }

        public virtual double GetAverageRating(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT ISNULL(AVG(CAST(Rating AS FLOAT)), 0) FROM Reviews WHERE FruitId = @FruitId";
            return conn.ExecuteScalar<double>(sql, new { FruitId = fruitId });
        }
        
        public virtual int GetReviewCount(int fruitId)
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT COUNT(*) FROM Reviews WHERE FruitId = @FruitId";
            return conn.ExecuteScalar<int>(sql, new { FruitId = fruitId });
        }

        public virtual Review? GetById(int reviewId)
        {
            using var conn = _context.CreateConnection();
            return conn.QueryFirstOrDefault<Review>(
                "SELECT r.*, u.FullName AS CustomerName FROM Reviews r INNER JOIN Users u ON r.UserId = u.UserId WHERE r.ReviewId = @ReviewId",
                new { ReviewId = reviewId });
        }

        public virtual void Update(Review review)
        {
            using var conn = _context.CreateConnection();
            conn.Execute(@"
                UPDATE Reviews SET Rating = @Rating, Comment = @Comment, UpdatedAt = GETDATE()
                WHERE ReviewId = @ReviewId",
                review);
        }

        public virtual void Delete(int reviewId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("DELETE FROM ReviewImages WHERE ReviewId = @ReviewId", new { ReviewId = reviewId });
            conn.Execute("DELETE FROM Reviews WHERE ReviewId = @ReviewId", new { ReviewId = reviewId });
        }

        public virtual void DeleteImages(int reviewId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("DELETE FROM ReviewImages WHERE ReviewId = @ReviewId", new { ReviewId = reviewId });
        }

        // RQ64 (admin): Lấy tất cả reviews cho duyệt
        public virtual IEnumerable<Review> GetAll(int page, int pageSize, string? keyword, string? status)
        {
            using var conn = _context.CreateConnection();
            var cond = new List<string> { "1=1" };
            if (!string.IsNullOrWhiteSpace(keyword))
                cond.Add("(f.FruitName LIKE @Keyword OR u.FullName LIKE @Keyword)");
            if (status == "approved")
                cond.Add("r.IsApproved = 1");
            else if (status == "pending")
                cond.Add("r.IsApproved = 0");

            var where = "WHERE " + string.Join(" AND ", cond);
            var sql = $@"
                SELECT r.*, u.FullName AS CustomerName, f.FruitName
                FROM Reviews r
                INNER JOIN Users u ON r.UserId = u.UserId
                INNER JOIN Fruits f ON r.FruitId = f.FruitId
                {where}
                ORDER BY r.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var param = new { Keyword = $"%{keyword}%", Offset = (page - 1) * pageSize, PageSize = pageSize };
            return conn.Query<Review>(sql, param);
        }

        public virtual int CountAll(string? keyword, string? status)
        {
            using var conn = _context.CreateConnection();
            var cond = new List<string> { "1=1" };
            if (!string.IsNullOrWhiteSpace(keyword))
                cond.Add("(f.FruitName LIKE @Keyword OR u.FullName LIKE @Keyword)");
            if (status == "approved")
                cond.Add("r.IsApproved = 1");
            else if (status == "pending")
                cond.Add("r.IsApproved = 0");

            var where = "WHERE " + string.Join(" AND ", cond);
            var sql = $@"
                SELECT COUNT(*) FROM Reviews r
                INNER JOIN Users u ON r.UserId = u.UserId
                INNER JOIN Fruits f ON r.FruitId = f.FruitId
                {where}";
            return conn.ExecuteScalar<int>(sql, new { Keyword = $"%{keyword}%" });
        }

        public virtual void Approve(int reviewId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("UPDATE Reviews SET IsApproved = 1 WHERE ReviewId = @ReviewId", new { ReviewId = reviewId });
        }

        public virtual void Reject(int reviewId)
        {
            using var conn = _context.CreateConnection();
            conn.Execute("UPDATE Reviews SET IsApproved = 0 WHERE ReviewId = @ReviewId", new { ReviewId = reviewId });
        }
    }
}
