namespace FruitShop.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho danh mục trái cây
    /// </summary>
    public class Category
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        // Số lượng sản phẩm trong danh mục (dùng trong view)
        public int FruitCount { get; set; }
    }
}
