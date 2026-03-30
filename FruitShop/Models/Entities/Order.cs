namespace FruitShop.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho đơn hàng
    /// </summary>
    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        // Các trạng thái: Pending, Confirmed, Shipping, Delivered, Cancelled
        public string? ShippingAddress { get; set; }
        public string? Note { get; set; }
        public int? CreatedBy { get; set; } // UserId của nhân viên xử lý

        // Coupon
        public int? CouponId { get; set; }
        public decimal DiscountAmount { get; set; }

        // Navigation properties
        public string? CustomerName { get; set; }
        public string? StaffName { get; set; }
        public List<OrderDetail> OrderDetails { get; set; } = new();

        /// <summary>
        /// Lấy màu badge Bootstrap theo trạng thái
        /// </summary>
        public string GetStatusBadge() => Status switch
        {
            "Pending"   => "warning",
            "Confirmed" => "primary",
            "Shipping"  => "info",
            "Delivered" => "success",
            "Cancelled" => "danger",
            _ => "secondary"
        };

        /// <summary>
        /// Lấy text tiếng Việt theo trạng thái
        /// </summary>
        public string GetStatusText() => Status switch
        {
            "Pending"   => "Chờ xác nhận",
            "Confirmed" => "Đã xác nhận",
            "Shipping"  => "Đang giao",
            "Delivered" => "Đã giao",
            "Cancelled" => "Đã huỷ",
            _ => Status
        };
    }
}
