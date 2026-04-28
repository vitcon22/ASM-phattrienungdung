namespace FruitShop.Enums;

/// <summary>
/// Tình trạng tồn kho - thay thế magic strings "in", "low", "out"
/// </summary>
public static class StockStatusEnum
{
    public const string In  = "in";
    public const string Low = "low";
    public const string Out = "out";

    public static string FromString(string? value) => value switch
    {
        In  => In,
        Low => Low,
        Out => Out,
        _    => ""
    };

    public static string ToVietnamese(string? status) => status switch
    {
        In  => "Còn hàng",
        Low => "Sắp hết",
        Out => "Hết hàng",
        _    => "Tất cả"
    };

    public static readonly string[] All = { In, Low, Out };
}
