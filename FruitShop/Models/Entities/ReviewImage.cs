using System;

namespace FruitShop.Models.Entities
{
    public class ReviewImage
    {
        public int ReviewImageId { get; set; }
        public int ReviewId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
