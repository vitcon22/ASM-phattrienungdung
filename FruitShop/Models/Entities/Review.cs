namespace FruitShop.Models.Entities
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int FruitId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Mở rộng hiển thị
        public string CustomerName { get; set; } = string.Empty;
    }
}
