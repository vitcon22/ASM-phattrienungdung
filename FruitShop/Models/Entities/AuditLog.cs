namespace FruitShop.Models.Entities
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public int? UserId { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public string ControllerName { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        
        // Optional: Not mapped directly in DB if using raw query, but useful for JOIN
        public string? UserName { get; set; }
    }
}
