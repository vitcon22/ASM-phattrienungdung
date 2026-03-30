namespace FruitShop.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho vai trò người dùng trong hệ thống
    /// </summary>
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty; // Admin, Staff, Customer
    }
}
