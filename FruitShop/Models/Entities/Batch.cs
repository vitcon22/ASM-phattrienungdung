namespace FruitShop.Models.Entities
{
    public class Batch
    {
        public int BatchId { get; set; }
        public int FruitId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public DateTime ImportDate { get; set; } = DateTime.Now;
        public DateTime? ManufactureDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal BuyPrice { get; set; }
        public int Quantity { get; set; }
        public int RemainingQty { get; set; }

        // Navigation properties (không map DB)
        public string? FruitName { get; set; }
        public string? SupplierName { get; set; }
    }
}
