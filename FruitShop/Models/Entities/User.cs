namespace FruitShop.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho người dùng trong hệ thống
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // BCrypt hash
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Phase 2: Points & Tiers
        public int Points { get; set; } = 0;
        public string Tier { get; set; } = "Standard";

        // Phase 2: Auth Tokens
        public bool EmailConfirmed { get; set; } = false;
        public string? VerificationToken { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation property
        public string? RoleName { get; set; }
    }
}
