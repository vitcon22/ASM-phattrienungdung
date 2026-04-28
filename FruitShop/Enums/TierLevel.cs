namespace FruitShop.Enums;

/// <summary>
/// Hạng thành viên dựa trên điểm tích luỹ - thay thế magic strings
/// </summary>
public static class TierLevelEnum
{
    public const string Standard  = "Standard";
    public const string Silver    = "Silver";
    public const string Gold      = "Gold";
    public const string Platinum  = "Platinum";

    public static string FromPoints(int points) => points switch
    {
        >= 1000 => Platinum,
        >= 500  => Gold,
        >= 200  => Silver,
        _       => Standard
    };

    public static string FromString(string value) => value switch
    {
        Standard  => Standard,
        Silver    => Silver,
        Gold      => Gold,
        Platinum  => Platinum,
        _         => Standard
    };

    public static string ToVietnamese(string tier) => tier switch
    {
        Standard  => "Thành viên",
        Silver    => "Bạc",
        Gold      => "Vàng",
        Platinum  => "Bạch Kim",
        _ => tier
    };

    public static string ToColor(string tier) => tier switch
    {
        Standard  => "#6c757d",
        Silver    => "#adb5bd",
        Gold      => "#ffc107",
        Platinum  => "#e0e0e0",
        _ => "#6c757d"
    };
}
