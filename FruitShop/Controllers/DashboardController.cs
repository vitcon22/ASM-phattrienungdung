using FruitShop.Constants;
using FruitShop.Filters;
using FruitShop.Models.DAL;
using FruitShop.Models.ViewModels;
using FruitShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text;

namespace FruitShop.Controllers;

[RequireRole("Admin")]
public class DashboardController : Controller
{
    private readonly OrderRepository _orderRepo;
    private readonly FruitRepository _fruitRepo;
    private readonly UserRepository _userRepo;
    private readonly CategoryRepository _categoryRepo;
    private readonly IExportService _exportService;
    private readonly IConfiguration _config;

    public DashboardController(
        OrderRepository orderRepo,
        FruitRepository fruitRepo,
        UserRepository userRepo,
        CategoryRepository categoryRepo,
        IExportService exportService,
        IConfiguration config)
    {
        _orderRepo     = orderRepo;
        _fruitRepo    = fruitRepo;
        _userRepo     = userRepo;
        _categoryRepo = categoryRepo;
        _exportService = exportService;
        _config       = config;
    }

    public IActionResult Index()
    {
        try
        {
            int lowStockThreshold = _config.GetValue("AppSettings:LowStockThreshold", 10);
            var statusCounts  = _orderRepo.CountByStatus();
            var last7Days    = _orderRepo.GetLast7DaysRevenue().ToList();
            var recentOrders = _orderRepo.GetRecent(8).ToList();

            var model = new DashboardViewModel
            {
                RevenueToday     = _orderRepo.GetRevenue("today"),
                RevenueThisMonth = _orderRepo.GetRevenue("month"),
                RevenueThisYear = _orderRepo.GetRevenue("year"),
                TotalCustomers  = _userRepo.CountCustomers(),
                TotalFruits     = _fruitRepo.CountActive(),
                TotalOrders     = _orderRepo.CountAll(),

                PendingOrders   = statusCounts.GetValueOrDefault(AppConstants.OrderStatuses.Pending,   0),
                ConfirmedOrders = statusCounts.GetValueOrDefault(AppConstants.OrderStatuses.Confirmed, 0),
                ShippingOrders  = statusCounts.GetValueOrDefault(AppConstants.OrderStatuses.Shipping,  0),
                DeliveredOrders = statusCounts.GetValueOrDefault(AppConstants.OrderStatuses.Delivered, 0),
                CancelledOrders = statusCounts.GetValueOrDefault(AppConstants.OrderStatuses.Cancelled, 0),

                TopFruits           = _fruitRepo.GetTopSelling(5).ToList(),
                Last7DaysRevenue    = last7Days,
                LowStockFruits     = _fruitRepo.GetLowStock(lowStockThreshold).ToList(),
                RecentOrders       = recentOrders,
                TotalStockValue    = _fruitRepo.GetTotalStockValue(),
                TotalStockQuantity = _fruitRepo.GetTotalStockQuantity(),
                RevenueByCategories    = _orderRepo.GetRevenueByCategory().ToList(),
                NewCustomersThisMonth = _userRepo.CountNewCustomersThisMonth(),
                NewUsersToday = _userRepo.CountNewUsersToday()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Lỗi khi tải Dashboard: " + ex.Message;
            return View(new DashboardViewModel());
        }
    }

    public IActionResult DailyReport(DateTime? date)
    {
        var reportDate = date ?? DateTime.Today;
        var from = reportDate.Date;
        var to   = reportDate.Date.AddDays(1).AddSeconds(-1);

        var orders      = _orderRepo.GetAllForExport(null, from, to).ToList();
        var validOrders = orders.Where(o => o.Status != AppConstants.OrderStatuses.Cancelled).ToList();

        ViewBag.ReportDate        = reportDate;
        ViewBag.Orders          = orders;
        ViewBag.TotalOrders     = orders.Count;
        ViewBag.ValidOrders     = validOrders.Count;
        ViewBag.CancelledOrders = orders.Count(o => o.Status == AppConstants.OrderStatuses.Cancelled);
        ViewBag.TotalRevenue    = validOrders.Sum(o => o.TotalAmount);
        ViewBag.PendingOrders   = orders.Count(o => o.Status == AppConstants.OrderStatuses.Pending);
        ViewBag.DeliveredOrders = orders.Count(o => o.Status == AppConstants.OrderStatuses.Delivered);

        int lowStockThreshold = _config.GetValue("AppSettings:LowStockThreshold", 10);
        ViewBag.LowStockFruits  = _fruitRepo.GetLowStock(lowStockThreshold).ToList();
        ViewBag.TotalStockValue = _fruitRepo.GetTotalStockValue();

        return View();
    }

    public IActionResult ExportOrders(string? status, DateTime? fromDate, DateTime? toDate)
    {
        var orders = _orderRepo.GetAllForExport(status, fromDate, toDate);
        var sb = new StringBuilder();
        sb.AppendLine("Mã DH,Khách hàng,Ngày đặt,Tổng tiền,Trạng thái,Địa chỉ,Ghi chú");
        foreach (var o in orders)
        {
            var line = string.Format(
                "{0},\"{1}\",{2:dd/MM/yyyy HH:mm},{3:N0},{4},\"{5}\",\"{6}\"",
                o.OrderId, o.CustomerName ?? "", o.OrderDate, o.TotalAmount,
                o.GetStatusText(), o.ShippingAddress ?? "", o.Note ?? "");
            sb.AppendLine(line);
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var filename = string.Format(AppConstants.ExportFiles.OrdersCsv, DateTime.Now.ToString("yyyyMMdd"));
        return File(bytes, "text/csv", filename);
    }

    public IActionResult ExportRevenueExcel(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate ?? DateTime.Today.AddMonths(-1);
        var to   = toDate   ?? DateTime.Today;

        var orders     = _orderRepo.GetAllForExport(null, from, to).ToList();
        var byCategory = _orderRepo.GetRevenueByCategory().ToList();

        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Báo cáo doanh thu");

        ws.Cells["A1"].Value = "BÁO CÁO DOANH THU — " + from.ToString("dd/MM/yyyy") + " đến " + to.ToString("dd/MM/yyyy");
        ws.Cells["A1:G1"].Merge = true;
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 14;
        ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
        ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

        int row = 3;
        string[] headers = { "Mã đơn", "Khách hàng", "Ngày đặt", "Tổng tiền", "Giảm giá", "Thực thu", "Trạng thái" };
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[row, c + 1].Value = headers[c];
            ws.Cells[row, c + 1].Style.Font.Bold = true;
            ws.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
            ws.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[row, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        row = 4;
        foreach (var o in orders)
        {
            ws.Cells[row, 1].Value = o.OrderId;
            ws.Cells[row, 2].Value = o.CustomerName;
            ws.Cells[row, 3].Value = o.OrderDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cells[row, 4].Value = (double)o.TotalAmount;
            ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";
            ws.Cells[row, 5].Value = (double)o.DiscountAmount;
            ws.Cells[row, 5].Style.Numberformat.Format = "#,##0";
            ws.Cells[row, 6].Value = (double)(o.TotalAmount - o.DiscountAmount);
            ws.Cells[row, 6].Style.Numberformat.Format = "#,##0";
            ws.Cells[row, 7].Value = o.GetStatusText();
            if (o.Status == AppConstants.OrderStatuses.Cancelled)
                ws.Cells[row, 1, row, 7].Style.Font.Color.SetColor(Color.Gray);
            row++;
        }
        ws.Columns.AutoFit();

        var ws2 = package.Workbook.Worksheets.Add("Theo danh mục");
        ws2.Cells["A1"].Value = "Danh mục";
        ws2.Cells["B1"].Value = "Doanh thu";
        ws2.Cells["A1:B1"].Style.Font.Bold = true;
        int r2 = 2;
        foreach (var cat in byCategory)
        {
            ws2.Cells[r2, 1].Value = cat.CategoryName;
            ws2.Cells[r2, 2].Value = (double)cat.Revenue;
            ws2.Cells[r2, 2].Style.Numberformat.Format = "#,##0";
            r2++;
        }
        ws2.Columns.AutoFit();

        var filename = string.Format(AppConstants.ExportFiles.RevenueExcel,
            from.ToString("yyyyMMdd"), to.ToString("yyyyMMdd"));
        return File(package.GetAsByteArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }

    public IActionResult CategoryReport(int? categoryId)
    {
        var categories = _categoryRepo.GetAll().ToList();
        var reportData = _fruitRepo.GetCategoryInventoryReport(categoryId).ToList();
        ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", categoryId);
        ViewBag.SelectedCategoryId = categoryId;
        return View(reportData);
    }

    // RQ107: Xuất báo cáo tồn kho theo danh mục ra Excel
    public IActionResult ExportCategoryReportExcel(int? categoryId)
    {
        var categories = _categoryRepo.GetAll().ToList();
        var reportData = _fruitRepo.GetCategoryInventoryReport(categoryId).ToList();
        using var package = new ExcelPackage();

        // Sheet 1: Tổng quan
        var ws = package.Workbook.Worksheets.Add("Tổng quan");
        ws.Cells["A1"].Value = "BÁO CÁO TỒN KHO THEO DANH MỤC";
        ws.Cells["A1:F1"].Merge = true;
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 14;
        ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
        ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

        string[] headers = { "Danh mục", "Sản phẩm", "Tổng tồn kho", "Giá trị (đ)", "Sắp hết", "Hết hàng" };
        int row = 3;
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cells[row, c + 1].Value = headers[c];
            ws.Cells[row, c + 1].Style.Font.Bold = true;
            ws.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
            ws.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
            ws.Cells[row, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        row = 4;
        foreach (var item in reportData)
        {
            ws.Cells[row, 1].Value = item.CategoryName;
            ws.Cells[row, 2].Value = item.TotalProducts;
            ws.Cells[row, 3].Value = item.TotalQuantity;
            ws.Cells[row, 4].Value = (double)item.TotalValue;
            ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";
            ws.Cells[row, 5].Value = item.LowStockCount;
            ws.Cells[row, 6].Value = item.OutOfStockCount;
            row++;
        }

        // Tổng cộng
        ws.Cells[row, 1].Value = "TỔNG CỘNG";
        ws.Cells[row, 1].Style.Font.Bold = true;
        ws.Cells[row, 2].Value = reportData.Sum(x => x.TotalProducts);
        ws.Cells[row, 3].Value = reportData.Sum(x => x.TotalQuantity);
        ws.Cells[row, 4].Value = (double)reportData.Sum(x => x.TotalValue);
        ws.Cells[row, 4].Style.Numberformat.Format = "#,##0";
        ws.Cells[row, 4].Style.Font.Bold = true;
        ws.Cells[row, 5].Value = reportData.Sum(x => x.LowStockCount);
        ws.Cells[row, 6].Value = reportData.Sum(x => x.OutOfStockCount);
        ws.Columns.AutoFit();

        // Sheet 2: Chi tiết sản phẩm
        var ws2 = package.Workbook.Worksheets.Add("Chi tiết sản phẩm");
        ws2.Cells["A1"].Value = "CHI TIẾT SẢN PHẨM THEO DANH MỤC";
        ws2.Cells["A1:H1"].Merge = true;
        ws2.Cells["A1"].Style.Font.Bold = true;
        ws2.Cells["A1"].Style.Font.Size = 12;
        ws2.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws2.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws2.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
        ws2.Cells["A1"].Style.Font.Color.SetColor(Color.White);

        string[] detailHeaders = { "Danh mục", "Tên sản phẩm", "Giá bán", "Tồn kho", "Ngưỡng", "Giá trị (đ)", "Trạng thái", "Nhà cung cấp" };
        row = 3;
        for (int c = 0; c < detailHeaders.Length; c++)
        {
            ws2.Cells[row, c + 1].Value = detailHeaders[c];
            ws2.Cells[row, c + 1].Style.Font.Bold = true;
            ws2.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws2.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
            ws2.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
        }

        var fruits = _fruitRepo.GetAll()
            .Where(f => categoryId == null || f.CategoryId == categoryId)
            .ToList();
        row = 4;
        foreach (var f in fruits)
        {
            var cat = categories.FirstOrDefault(c => c.CategoryId == f.CategoryId);
            ws2.Cells[row, 1].Value = cat?.CategoryName ?? "";
            ws2.Cells[row, 2].Value = f.FruitName;
            ws2.Cells[row, 3].Value = (double)f.Price;
            ws2.Cells[row, 3].Style.Numberformat.Format = "#,##0";
            ws2.Cells[row, 4].Value = f.StockQuantity;
            ws2.Cells[row, 5].Value = f.MinStock;
            ws2.Cells[row, 6].Value = (double)(f.Price * f.StockQuantity);
            ws2.Cells[row, 6].Style.Numberformat.Format = "#,##0";
            ws2.Cells[row, 7].Value = f.StockQuantity <= 0 ? "Hết hàng" : f.StockQuantity < f.MinStock ? "Sắp hết" : "Còn hàng";
            ws2.Cells[row, 8].Value = "";
            row++;
        }
        ws2.Columns.AutoFit();

        var filename = $"BaoCaoTonKho_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(package.GetAsByteArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }
}
