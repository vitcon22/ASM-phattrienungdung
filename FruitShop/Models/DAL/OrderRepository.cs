using Dapper;
using FruitShop.Models.Entities;
using FruitShop.Models.ViewModels;

namespace FruitShop.Models.DAL
{
    /// <summary>
    /// Repository xử lý các thao tác liên quan đến Orders và OrderDetails
    /// </summary>
    public class OrderRepository
    {
        private readonly FruitShopContext _context;

        public OrderRepository(FruitShopContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy đơn hàng với phân trang và lọc (Staff/Admin)
        /// </summary>
        public (IEnumerable<Order> Items, int TotalCount) Search(
            string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            using var conn = _context.CreateConnection();

            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(status)) conditions.Add("o.Status = @Status");
            if (fromDate.HasValue) conditions.Add("o.OrderDate >= @FromDate");
            if (toDate.HasValue)  conditions.Add("o.OrderDate < DATEADD(day,1,@ToDate)");

            var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            var countSql = $"SELECT COUNT(*) FROM Orders o {where}";
            var dataSql = $@"
                SELECT o.*, u.FullName AS CustomerName
                FROM Orders o
                INNER JOIN Users u ON o.UserId = u.UserId
                {where}
                ORDER BY o.OrderDate DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var param = new
            {
                Status   = status,
                FromDate = fromDate,
                ToDate   = toDate,
                Offset   = (page - 1) * pageSize,
                PageSize = pageSize
            };

            var total = conn.ExecuteScalar<int>(countSql, param);
            var items = conn.Query<Order>(dataSql, param);
            return (items, total);
        }

        /// <summary>
        /// Lấy đơn hàng của một customer cụ thể
        /// </summary>
        public IEnumerable<Order> GetByCustomer(int userId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT o.*
                FROM Orders o
                WHERE o.UserId = @UserId
                ORDER BY o.OrderDate DESC";
            return conn.Query<Order>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Lấy chi tiết đơn hàng theo ID
        /// </summary>
        public Order? GetById(int orderId)
        {
            using var conn = _context.CreateConnection();
            const string orderSql = @"
                SELECT o.*, u.FullName AS CustomerName
                FROM Orders o
                INNER JOIN Users u ON o.UserId = u.UserId
                WHERE o.OrderId = @OrderId";

            const string detailSql = @"
                SELECT od.*, f.FruitName, f.ImageUrl, f.Unit
                FROM OrderDetails od
                INNER JOIN Fruits f ON od.FruitId = f.FruitId
                WHERE od.OrderId = @OrderId";

            var order = conn.QueryFirstOrDefault<Order>(orderSql, new { OrderId = orderId });
            if (order != null)
            {
                order.OrderDetails = conn.Query<OrderDetail>(detailSql, new { OrderId = orderId }).ToList();
            }
            return order;
        }

        /// <summary>
        /// Đếm số đơn hàng theo trạng thái
        /// </summary>
        public Dictionary<string, int> CountByStatus()
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT Status, COUNT(*) AS Cnt FROM Orders GROUP BY Status";
            var result = conn.Query(sql).ToDictionary(r => (string)r.Status, r => (int)r.Cnt);
            return result;
        }

        /// <summary>
        /// Tổng số đơn hàng
        /// </summary>
        public int CountAll()
        {
            using var conn = _context.CreateConnection();
            return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Orders");
        }

        /// <summary>
        /// Tính doanh thu hôm nay / tháng / năm (không tính Pending và Cancelled)
        /// </summary>
        public decimal GetRevenue(string period)
        {
            using var conn = _context.CreateConnection();
            var sql = period switch
            {
                "today" => @"
                    SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders
                    WHERE CAST(OrderDate AS DATE) = CAST(GETDATE() AS DATE)
                      AND Status NOT IN ('Cancelled', 'Pending')",
                "month" => @"
                    SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders
                    WHERE MONTH(OrderDate) = MONTH(GETDATE()) AND YEAR(OrderDate) = YEAR(GETDATE())
                      AND Status NOT IN ('Cancelled', 'Pending')",
                "year" => @"
                    SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders
                    WHERE YEAR(OrderDate) = YEAR(GETDATE())
                      AND Status NOT IN ('Cancelled', 'Pending')",
                _ => "SELECT 0"
            };
            return conn.ExecuteScalar<decimal>(sql);
        }

        /// <summary>
        /// Doanh thu 7 ngày gần nhất (cho Line chart Dashboard)
        /// </summary>
        public IEnumerable<RevenueByDay> GetLast7DaysRevenue()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT
                    FORMAT(CAST(OrderDate AS DATE), 'dd/MM') AS DayLabel,
                    ISNULL(SUM(TotalAmount), 0) AS Revenue
                FROM Orders
                WHERE OrderDate >= DATEADD(day, -6, CAST(GETDATE() AS DATE))
                  AND Status NOT IN ('Cancelled', 'Pending')
                GROUP BY CAST(OrderDate AS DATE)
                ORDER BY CAST(OrderDate AS DATE)";
            return conn.Query<RevenueByDay>(sql);
        }

        /// <summary>
        /// Tạo đơn hàng mới (transaction: insert Order + OrderDetails + trừ tồn kho)
        /// </summary>
        public int CreateOrder(Order order, List<CartItemViewModel> cartItems)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                // Tính tổng tiền sau khi giảm giá
                order.TotalAmount = cartItems.Sum(x => x.UnitPrice * x.Quantity) - order.DiscountAmount;
                if (order.TotalAmount < 0) order.TotalAmount = 0;

                // Thêm đơn hàng
                const string orderSql = @"
                    INSERT INTO Orders (UserId, TotalAmount, Status, ShippingAddress, Note, CreatedBy, CouponId, DiscountAmount)
                    VALUES (@UserId, @TotalAmount, @Status, @ShippingAddress, @Note, @CreatedBy, @CouponId, @DiscountAmount);
                    SELECT CAST(SCOPE_IDENTITY() AS INT)";
                int orderId = conn.ExecuteScalar<int>(orderSql, order, tran);

                // Thêm chi tiết đơn hàng và trừ tồn kho
                foreach (var item in cartItems)
                {
                    const string detailSql = @"
                        INSERT INTO OrderDetails (OrderId, FruitId, Quantity, UnitPrice)
                        VALUES (@OrderId, @FruitId, @Quantity, @UnitPrice)";
                    conn.Execute(detailSql, new
                    {
                        OrderId  = orderId,
                        item.FruitId,
                        item.Quantity,
                        UnitPrice = item.UnitPrice
                    }, tran);

                    // Trừ tồn kho
                    const string stockSql = @"
                        UPDATE Fruits SET StockQuantity = StockQuantity - @Quantity
                        WHERE FruitId = @FruitId";
                    conn.Execute(stockSql, new { item.FruitId, item.Quantity }, tran);
                }

                tran.Commit();
                return orderId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng (Staff/Admin)
        /// </summary>
        public void UpdateStatus(int orderId, string status, int staffId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Orders SET Status = @Status, CreatedBy = @StaffId
                WHERE OrderId = @OrderId";
            conn.Execute(sql, new { OrderId = orderId, Status = status, StaffId = staffId });
        }

        /// <summary>Customer huỷ đơn hàng khi còn Pending</summary>
        public bool CancelOrder(int orderId, int userId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Orders SET Status = 'Cancelled'
                WHERE OrderId = @OrderId AND UserId = @UserId AND Status = 'Pending'";
            return conn.Execute(sql, new { OrderId = orderId, UserId = userId }) > 0;
        }

        /// <summary>Lấy N đơn hàng gần nhất (cho Dashboard)</summary>
        public IEnumerable<Order> GetRecent(int count = 10)
        {
            using var conn = _context.CreateConnection();
            var sql = $@"
                SELECT TOP {count} o.*, u.FullName AS CustomerName
                FROM Orders o
                INNER JOIN Users u ON o.UserId = u.UserId
                ORDER BY o.OrderDate DESC";
            return conn.Query<Order>(sql);
        }

        /// <summary>Lấy tất cả đơn hàng để export CSV</summary>
        public IEnumerable<Order> GetAllForExport(string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var conn = _context.CreateConnection();
            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(status))  conditions.Add("o.Status = @Status");
            if (fromDate.HasValue)               conditions.Add("o.OrderDate >= @FromDate");
            if (toDate.HasValue)                 conditions.Add("o.OrderDate < DATEADD(day,1,@ToDate)");
            var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            var sql = $@"
                SELECT o.*, u.FullName AS CustomerName
                FROM Orders o
                INNER JOIN Users u ON o.UserId = u.UserId
                {where}
                ORDER BY o.OrderDate DESC";
            return conn.Query<Order>(sql, new { Status = status, FromDate = fromDate, ToDate = toDate });
        }
    }
}
