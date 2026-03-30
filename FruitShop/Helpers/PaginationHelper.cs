namespace FruitShop.Helpers
{
    /// <summary>
    /// Model phân trang - chứa dữ liệu trang và metadata
    /// </summary>
    public class PagedList<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage     => CurrentPage < TotalPages;

        public PagedList(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
        {
            Items       = items;
            TotalCount  = totalCount;
            CurrentPage = currentPage;
            PageSize    = pageSize;
            TotalPages  = (int)Math.Ceiling(totalCount / (double)pageSize);
        }
    }

    /// <summary>
    /// Helper tạo PagedList từ dữ liệu
    /// </summary>
    public static class PaginationHelper
    {
        /// <summary>
        /// Tạo đối tượng phân trang
        /// </summary>
        public static PagedList<T> Create<T>(
            IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
        {
            // Đảm bảo page hợp lệ
            if (currentPage < 1) currentPage = 1;
            return new PagedList<T>(items, totalCount, currentPage, pageSize);
        }

        /// <summary>
        /// Tính offset cho SQL query
        /// </summary>
        public static int GetOffset(int page, int pageSize)
        {
            return (page - 1) * pageSize;
        }
    }
}
