namespace FruitShop.Enums;

/// <summary>
/// Vai trò người dùng - thay thế magic strings "Admin", "Staff", "Customer"
/// </summary>
public static class UserRoleEnum
{
    public const string Admin    = "Admin";
    public const string Staff    = "Staff";
    public const string Customer = "Customer";

    public const int AdminId    = 1;
    public const int StaffId    = 2;
    public const int CustomerId = 3;

    public static string FromString(string value) => value switch
    {
        Admin    => Admin,
        Staff    => Staff,
        Customer => Customer,
        _        => Customer
    };

    public static string ToVietnamese(string role) => role switch
    {
        Admin    => "Quản trị viên",
        Staff    => "Nhân viên",
        Customer => "Khách hàng",
        _        => "Khách hàng"
    };

    public static int ToRoleId(string role) => role switch
    {
        Admin    => AdminId,
        Staff    => StaffId,
        Customer => CustomerId,
        _        => CustomerId
    };

    public static string FromRoleId(int roleId) => roleId switch
    {
        AdminId    => Admin,
        StaffId    => Staff,
        CustomerId => Customer,
        _          => Customer
    };

    public static readonly string[] All = { Admin, Staff, Customer };
}
