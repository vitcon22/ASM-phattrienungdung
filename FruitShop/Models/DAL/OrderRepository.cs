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
        public virtual (IEnumerable<Order> Items, int TotalCount) Search(
            string? status, DateTime? fromDate, DateTime? toDate, string? keyword,
            int page, int pageSize)
        {
            using var conn = _context.CreateConnection();

            var conditions = new List<string>();
            if (!string.IsNullOrEmpty(status)) conditions.Add("o.Status = @Status");
            if (fromDate.HasValue) conditions.Add("o.OrderDate >= @FromDate");
            if (toDate.HasValue)  conditions.Add("o.OrderDate < DATEADD(day,1,@ToDate)");
            if (!string.IsNullOrWhiteSpace(keyword))
                conditions.Add("(CAST(o.OrderId AS NVARCHAR) LIKE @Keyword OR u.FullName LIKE @Keyword OR u.Phone LIKE @Keyword)");

            var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            var countSql = $@"
                SELECT COUNT(*) FROM Orders o
                INNER JOIN Users u ON o.UserId = u.UserId
                {where}";
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
                Keyword  = $"%{keyword}%",
                Offset   = (page - 1) * pageSize,
                PageSize = pageSize
            };

            var total = conn.ExecuteScalar<int>(countSql, param);
            var items = conn.Query<Order>(dataSql, param);
            return (items, total);
        }

        /// <summary>Tổng doanh thu của 1 khách (đơn Delivered)</summary>
        public virtual decimal GetCustomerTotalSpent(int userId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders
                WHERE UserId = @UserId AND Status = 'Delivered'";
            return conn.ExecuteScalar<decimal>(sql, new { UserId = userId });
        }

        /// <summary>
        /// Lấy đơn hàng của một customer cụ thể
        /// </summary>
        public virtual IEnumerable<Order> GetByCustomer(int userId)
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
        public virtual Order? GetById(int orderId)
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
        public virtual Dictionary<string, int> CountByStatus()
        {
            using var conn = _context.CreateConnection();
            const string sql = "SELECT Status, COUNT(*) AS Cnt FROM Orders GROUP BY Status";
            var result = conn.Query(sql).ToDictionary(r => (string)r.Status, r => (int)r.Cnt);
            return result;
        }

        /// <summary>
        /// Tổng số đơn hàng
        /// </summary>
        public virtual int CountAll()
        {
            using var conn = _context.CreateConnection();
            return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Orders");
        }

        /// <summary>
        /// Tính doanh thu hôm nay / tháng / năm (không tính Pending và Cancelled)
        /// </summary>
        public virtual decimal GetRevenue(string period, int? month = null, int? year = null)
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
                    WHERE MONTH(OrderDate) = @Month AND YEAR(OrderDate) = @Year
                      AND Status NOT IN ('Cancelled', 'Pending')",
                "year" => @"
                    SELECT ISNULL(SUM(TotalAmount), 0) FROM Orders
                    WHERE YEAR(OrderDate) = @Year
                      AND Status NOT IN ('Cancelled', 'Pending')",
                _ => "SELECT 0"
            };
            if (period == "today" || period == "_")
                return conn.ExecuteScalar<decimal>(sql);
            var m = month ?? DateTime.Now.Month;
            var y = year  ?? DateTime.Now.Year;
            return conn.ExecuteScalar<decimal>(sql, new { Month = m, Year = y });
        }

        /// <summary>
        /// Doanh thu 7 ngày gần nhất (cho Line chart Dashboard)
        /// </summary>
        public virtual IEnumerable<RevenueByDay> GetLast7DaysRevenue()
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
        public virtual int CreateOrder(Order order, List<CartItemViewModel> cartItems, int pointsRedeemed = 0, decimal pointsDiscount = 0)
        {
            using var conn = _context.CreateConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();
            try
            {
                // Tính tổng tiền sau khi giảm giá (coupon + points)
                decimal subtotal = cartItems.Sum(x => x.UnitPrice * x.Quantity);
                order.TotalAmount = subtotal - order.DiscountAmount - pointsDiscount;
                if (order.TotalAmount < 0) order.TotalAmount = 0;

                // RQ35: lưu points đã dùng
                order.PointsRedeemed = pointsRedeemed;
                order.PointsDiscount = pointsDiscount;

                // Thêm đơn hàng
                const string orderSql = @"
                    INSERT INTO Orders (UserId, TotalAmount, Status, ShippingAddress, Note, CreatedBy, CouponId, DiscountAmount, PaymentMethod, AmountReceived, PointsRedeemed, PointsDiscount)
                    VALUES (@UserId, @TotalAmount, @Status, @ShippingAddress, @Note, @CreatedBy, @CouponId, @DiscountAmount, @PaymentMethod, @AmountReceived, @PointsRedeemed, @PointsDiscount);
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
        public virtual void UpdateStatus(int orderId, string status, int staffId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Orders SET Status = @Status, CreatedBy = @StaffId
                WHERE OrderId = @OrderId";
            conn.Execute(sql, new { OrderId = orderId, Status = status, StaffId = staffId });
        }

        /// <summary>Customer huỷ đơn hàng khi còn Pending</summary>
        public virtual bool CancelOrder(int orderId, int userId)
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                UPDATE Orders SET Status = 'Cancelled'
                WHERE OrderId = @OrderId AND UserId = @UserId AND Status = 'Pending'";
            return conn.Execute(sql, new { OrderId = orderId, UserId = userId }) > 0;
        }

        /// <summary>Lấy N đơn hàng gần nhất (cho Dashboard)</summary>
        public virtual IEnumerable<Order> GetRecent(int count = 10)
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
        public virtual IEnumerable<Order> GetAllForExport(string? status = null, DateTime? fromDate = null, DateTime? toDate = null)
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

        // --- Phase 3: Advanced Charts ---
        public virtual IEnumerable<RevenueByCategory> GetRevenueByCategory()
        {
            using var conn = _context.CreateConnection();
            const string sql = @"
                SELECT c.CategoryName, ISNULL(SUM(od.Quantity * od.UnitPrice), 0) AS Revenue
                FROM OrderDetails od
                INNER JOIN Fruits f ON od.FruitId = f.FruitId
                INNER JOIN Categories c ON f.CategoryId = c.CategoryId
                INNER JOIN Orders o ON od.OrderId = o.OrderId
                WHERE o.Status NOT IN ('Cancelled', 'Pending')
                GROUP BY c.CategoryName
                ORDER BY Revenue DESC";
            return conn.Query<RevenueByCategory>(sql);
        }
    }
}
