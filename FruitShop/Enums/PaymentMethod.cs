namespace FruitShop.Enums;

/// <summary>
/// Phương thức thanh toán - thay thế magic strings
/// </summary>
public static class PaymentMethodEnum
{
    public const string Cash     = "Cash";
    public const string Transfer = "Transfer";
    public const string QR       = "QR";

    public static string FromString(string value) => value switch
    {
        Cash     => Cash,
        Transfer => Transfer,
        QR       => QR,
        _         => Cash
    };

    public static string ToVietnamese(string method) => method switch
    {
        Cash     => "Tiền mặt",
        Transfer => "Chuyển khoản",
        QR       => "QR Code",
        _         => method
    };

    public static string ToIcon(string method) => method switch
    {
        Cash     => "fa-money-bill-wave",
        Transfer => "fa-university",
        QR       => "fa-qrcode",
        _        => "fa-wallet"
    };

    public static readonly string[] All = { Cash, Transfer, QR };
}
