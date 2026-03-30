namespace FruitShop.Models.Entities
{
    public class Wishlist
    {
        public int UserId { get; set; }
        public int FruitId { get; set; }
        public DateTime AddedAt { get; set; }

        // Cho view
        public string FruitName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public bool InStock { get; set; }
    }
}
