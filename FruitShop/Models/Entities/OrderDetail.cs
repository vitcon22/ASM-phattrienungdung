namespace FruitShop.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho chi tiết đơn hàng
    /// </summary>
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int FruitId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal => Quantity * UnitPrice; // Tính toán phía C# (khớp với computed column DB)

        // Navigation properties
        public string? FruitName { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
    }
}
