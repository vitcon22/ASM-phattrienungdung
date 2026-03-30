using FruitShop.Models.DAL;

namespace FruitShop.Helpers
{
    /// <summary>
    /// Helper kiểm tra validation bổ sung (server-side logic phức tạp)
    /// </summary>
    public class ValidationHelper
    {
        private readonly FruitRepository _fruitRepo;
        private readonly CategoryRepository _categoryRepo;
        private readonly UserRepository _userRepo;

        public ValidationHelper(
            FruitRepository fruitRepo,
            CategoryRepository categoryRepo,
            UserRepository userRepo)
        {
            _fruitRepo    = fruitRepo;
            _categoryRepo = categoryRepo;
            _userRepo     = userRepo;
        }

        /// <summary>
        /// Kiểm tra tên trái cây có bị trùng trong cùng danh mục không
        /// </summary>
        public bool IsFruitNameDuplicate(string name, int categoryId, int excludeId = 0)
        {
            return _fruitRepo.NameExistsInCategory(name, categoryId, excludeId);
        }

        /// <summary>
        /// Kiểm tra email đăng ký có bị trùng không
        /// </summary>
        public bool IsEmailDuplicate(string email, int excludeUserId = 0)
        {
            return _userRepo.EmailExists(email, excludeUserId);
        }

        /// <summary>
        /// Kiểm tra số lượng đặt có vượt tồn kho không
        /// </summary>
        public bool IsQuantityExceedStock(int fruitId, int requestedQuantity)
        {
            var fruit = _fruitRepo.GetById(fruitId);
            return fruit == null || requestedQuantity > fruit.StockQuantity;
        }

        /// <summary>
        /// Kiểm tra danh mục có thể xóa không (không có sản phẩm active)
        /// </summary>
        public bool CanDeleteCategory(int categoryId)
        {
            return !_categoryRepo.HasFruits(categoryId);
        }

        /// <summary>
        /// Kiểm tra trái cây có thể xóa không (không có trong đơn hàng nào)
        /// </summary>
        public bool CanDeleteFruit(int fruitId)
        {
            return !_fruitRepo.IsInOrder(fruitId);
        }

        /// <summary>
        /// Kiểm tra file ảnh upload có hợp lệ không
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateImageFile(IFormFile? file)
        {
            if (file == null) return (true, string.Empty); // Không bắt buộc

            // Kiểm tra kích thước (tối đa 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return (false, "Ảnh không được vượt quá 5MB");

            // Kiểm tra định dạng
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return (false, "Chỉ chấp nhận ảnh .jpg, .jpeg, .png, .gif, .webp");

            return (true, string.Empty);
        }
    }
}
