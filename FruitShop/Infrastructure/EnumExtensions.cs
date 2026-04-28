using FruitShop.Constants;
using FruitShop.Enums;

namespace FruitShop.Infrastructure;

/// <summary>
/// Extension methods cho string - dùng enum helpers
/// </summary>
public static class EnumExtensions
{
    public static string ToStatusBadge(this string status) => OrderStatusEnum.ToBadge(status);
    public static string ToStatusText(this string status) => OrderStatusEnum.ToVietnamese(status);
    public static string ToRoleText(this string role) => UserRoleEnum.ToVietnamese(role);
    public static string ToPaymentText(this string method) => PaymentMethodEnum.ToVietnamese(method);
}
