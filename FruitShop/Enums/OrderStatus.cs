namespace FruitShop.Enums;

/// <summary>
/// Trạng thái đơn hàng - thay thế magic strings "Pending", "Confirmed"...
/// </summary>
public static class OrderStatusEnum
{
    public const string Pending   = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Shipping  = "Shipping";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";

    public static string FromString(string value) => value switch
    {
        Pending   => Pending,
        Confirmed => Confirmed,
        Shipping  => Shipping,
        Delivered => Delivered,
        Cancelled => Cancelled,
        _ => Pending
    };

    public static string ToVietnamese(string status) => status switch
    {
        Pending   => "Chờ xác nhận",
        Confirmed => "Đã xác nhận",
        Shipping  => "Đang giao",
        Delivered => "Đã giao",
        Cancelled => "Đã huỷ",
        _ => status
    };

    public static string ToBadge(string status) => status switch
    {
        Pending   => "warning",
        Confirmed => "primary",
        Shipping  => "info",
        Delivered => "success",
        Cancelled => "danger",
        _ => "secondary"
    };

    public static readonly string[] All = { Pending, Confirmed, Shipping, Delivered, Cancelled };
}
