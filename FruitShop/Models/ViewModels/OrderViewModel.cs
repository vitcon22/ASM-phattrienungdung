using System.ComponentModel.DataAnnotations;
using FruitShop.Models.Entities;

namespace FruitShop.Models.ViewModels
{
    /// <summary>
    /// ViewModel dùng khi Customer xác nhận đơn hàng (Checkout)
    /// </summary>
    public class OrderViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ giao hàng")]
        [StringLength(255, ErrorMessage = "Địa chỉ tối đa 255 ký tự")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Danh sách sản phẩm trong giỏ hàng (dùng để hiển thị trên trang checkout)
        public List<CartItemViewModel> CartItems { get; set; } = new();

        // Tổng tiền
        public decimal TotalAmount => CartItems.Sum(x => x.Subtotal);

        // Coupon
        public string? CouponCode { get; set; }
        public int? CouponId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount => TotalAmount - DiscountAmount;
    }

    /// <summary>
    /// ViewModel cho item trong giỏ hàng (lưu trong Session)
    /// </summary>
    public class CartItemViewModel
    {
        public int FruitId { get; set; }
        public string FruitName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public int StockQuantity { get; set; } // Để validate khi thay đổi SL

        public decimal Subtotal => UnitPrice * Quantity;
    }

    /// <summary>
    /// ViewModel cho Dashboard thống kê (Admin)
    /// </summary>
    public class DashboardViewModel
    {
        // Doanh thu
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }

        // Số lượng tổng quát
        public int TotalCustomers { get; set; }
        public int TotalFruits { get; set; }
        public int TotalOrders { get; set; }

        // Đơn hàng theo trạng thái
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Top 5 trái cây bán chạy
        public List<TopFruitItem> TopFruits { get; set; } = new();

        // Doanh thu 7 ngày gần nhất
        public List<RevenueByDay> Last7DaysRevenue { get; set; } = new();

        // Trái cây tồn kho thấp
        public List<Fruit> LowStockFruits { get; set; } = new();

        // 10 đơn hàng gần nhất
        public List<Order> RecentOrders { get; set; } = new();

        // Người dùng mới hôm nay
        public int NewUsersToday { get; set; }
    }

    public class TopFruitItem
    {
        public string FruitName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RevenueByDay
    {
        public string DayLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}
