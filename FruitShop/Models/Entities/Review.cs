namespace FruitShop.Models.Entities
{
    public class Review
    {
        public int ReviewId { get; set; }
        public int FruitId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; } = true; // mặc định approve
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation / display
        public string CustomerName { get; set; } = string.Empty;
        public string FruitName { get; set; } = string.Empty;
        public List<ReviewImage> Images { get; set; } = new();
    }
}
