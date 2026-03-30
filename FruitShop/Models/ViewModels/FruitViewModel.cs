using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FruitShop.Models.ViewModels
{
    /// <summary>
    /// ViewModel dùng cho form tạo/sửa trái cây
    /// </summary>
    public class FruitViewModel
    {
        public int FruitId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Tên trái cây")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Tên phải từ 2 đến 100 ký tự")]
        [Display(Name = "Tên trái cây")]
        public string FruitName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn Danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Giá")]
        [Range(1000, 1000000000, ErrorMessage = "Giá phải từ 1.000đ đến 1.000.000.000đ")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm")]
        [Display(Name = "Tồn kho")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Đơn vị")]
        [Display(Name = "Đơn vị tính")]
        public string Unit { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Xuất xứ tối đa 100 ký tự")]
        [Display(Name = "Xuất xứ")]
        public string? Origin { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        // File upload ảnh
        [Display(Name = "Ảnh sản phẩm")]
        public IFormFile? ImageFile { get; set; }

        // Ảnh hiện tại (dùng khi Edit)
        public string? CurrentImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
