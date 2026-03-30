using System.ComponentModel.DataAnnotations;

namespace FruitShop.Models.Entities
{
    public class Coupon
    {
        public int CouponId { get; set; }
        [Required]
        public string Code { get; set; } = string.Empty;
        [Range(1, 100)]
        public int DiscountPercent { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
