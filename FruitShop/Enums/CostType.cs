namespace FruitShop.Enums;

/// <summary>
/// Loại chi phí vận hành - thay thế magic strings
/// </summary>
public static class CostTypeEnum
{
    public const string Electricity = "Điện";
    public const string Water       = "Nước";
    public const string Rent        = "Thuê mặt bằng";
    public const string Salary      = "Lương nhân viên";
    public const string Transport   = "Vận chuyển";
    public const string Marketing  = "Marketing";
    public const string Other      = "Khác";

    public static string FromString(string value) => value switch
    {
        Electricity => Electricity,
        Water       => Water,
        Rent        => Rent,
        Salary      => Salary,
        Transport   => Transport,
        Marketing  => Marketing,
        Other      => Other,
        _          => Other
    };

    public static readonly string[] All = new[]
    {
        Electricity, Water, Rent, Salary, Transport, Marketing, Other
    };
}
