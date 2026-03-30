namespace FruitShop.Models.Entities
{
    public class InventoryLog
    {
        public int LogId { get; set; }
        public int FruitId { get; set; }
        public int StaffId { get; set; }
        public int QuantityChange { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }

        // Cho view hiển thị
        public string FruitName { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
    }
}
