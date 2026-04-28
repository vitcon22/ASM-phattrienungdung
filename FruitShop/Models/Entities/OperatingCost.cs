using System.ComponentModel.DataAnnotations;

namespace FruitShop.Models.Entities
{
    public class OperatingCost
    {
        public int CostId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tháng/năm")]
        [Display(Name = "Tháng/Năm")]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập loại chi phí")]
        [StringLength(100)]
        [Display(Name = "Loại chi phí")]
        public string CostType { get; set; } = string.Empty; // Điện, Nước, Thuê mặt bằng, Lương, Khác

        [Required(ErrorMessage = "Vui lòng nhập số tiền")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền không được âm")]
        [Display(Name = "Số tiền (đ)")]
        public decimal Amount { get; set; }

        [StringLength(500)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Computed
        public string MonthYear => $"Tháng {Month:D2}/{Year}";
    }
}
