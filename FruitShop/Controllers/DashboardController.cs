using FruitShop.Filters;
using FruitShop.Helpers;
using FruitShop.Models.DAL;
using FruitShop.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace FruitShop.Controllers
{
    [RequireRole("Admin")]
    public class DashboardController : Controller
    {
        private readonly OrderRepository _orderRepo;
        private readonly FruitRepository _fruitRepo;
        private readonly UserRepository  _userRepo;
        private readonly IConfiguration  _config;

        public DashboardController(
            OrderRepository orderRepo,
            FruitRepository fruitRepo,
            UserRepository  userRepo,
            IConfiguration  config)
        {
            _orderRepo = orderRepo;
            _fruitRepo = fruitRepo;
            _userRepo  = userRepo;
            _config    = config;
        }

        // GET: /Dashboard
        public IActionResult Index()
        {
            try
            {
                int lowStockThreshold = _config.GetValue<int>("AppSettings:LowStockThreshold", 10);
                var statusCounts = _orderRepo.CountByStatus();
                var last7Days    = _orderRepo.GetLast7DaysRevenue().ToList();
                var recentOrders = _orderRepo.GetRecent(8).ToList();

                var model = new DashboardViewModel
                {
                    RevenueToday     = _orderRepo.GetRevenue("today"),
                    RevenueThisMonth = _orderRepo.GetRevenue("month"),
                    RevenueThisYear  = _orderRepo.GetRevenue("year"),

                    TotalCustomers   = _userRepo.CountCustomers(),
                    TotalFruits      = _fruitRepo.CountActive(),
                    TotalOrders      = _orderRepo.CountAll(),

                    PendingOrders   = statusCounts.GetValueOrDefault("Pending",   0),
                    ConfirmedOrders = statusCounts.GetValueOrDefault("Confirmed", 0),
                    ShippingOrders  = statusCounts.GetValueOrDefault("Shipping",  0),
                    DeliveredOrders = statusCounts.GetValueOrDefault("Delivered", 0),
                    CancelledOrders = statusCounts.GetValueOrDefault("Cancelled", 0),

                    TopFruits        = _fruitRepo.GetTopSelling(5).ToList(),
                    Last7DaysRevenue = last7Days,
                    LowStockFruits   = _fruitRepo.GetLowStock(lowStockThreshold).ToList(),
                    RecentOrders     = recentOrders
                };

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi khi tải Dashboard: {ex.Message}";
                return View(new DashboardViewModel());
            }
        }

        // GET: /Dashboard/ExportOrders  — xuất CSV đơn hàng
        public IActionResult ExportOrders(string? status, DateTime? fromDate, DateTime? toDate)
        {
            var orders = _orderRepo.GetAllForExport(status, fromDate, toDate);
            var sb = new StringBuilder();
            sb.AppendLine("Mã ĐH,Khách hàng,Ngày đặt,Tổng tiền,Trạng thái,Địa chỉ,Ghi chú");
            foreach (var o in orders)
            {
                sb.AppendLine($"{o.OrderId},\"{o.CustomerName}\",{o.OrderDate:dd/MM/yyyy HH:mm},{o.TotalAmount:N0},{o.GetStatusText()},\"{o.ShippingAddress}\",\"{o.Note}\"");
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var filename = $"DanhSachDonHang_{DateTime.Now:yyyyMMdd}.csv";
            return File(bytes, "text/csv", filename);
        }
    }
}
