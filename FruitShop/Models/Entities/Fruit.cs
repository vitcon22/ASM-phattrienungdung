namespace FruitShop.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho sản phẩm trái cây
    /// </summary>
    public class Fruit
    {
        public int FruitId { get; set; }
        public string FruitName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public int MinStock { get; set; } = 10;
        public string Unit { get; set; } = string.Empty; // kg, hộp, trái
        public string? Origin { get; set; }              // Xuất xứ
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties (join data)
        public string? CategoryName { get; set; }
        public string? SupplierName { get; set; }

        /// <summary>
        /// Kiểm tra còn hàng không
        /// </summary>
        public bool InStock => StockQuantity > 0;

        /// <summary>
        /// Lấy đường dẫn ảnh, dùng ảnh mặc định nếu chưa có
        /// </summary>
        public string GetImageUrl() => string.IsNullOrEmpty(ImageUrl) ? "default.jpg" : ImageUrl;
    }
}
