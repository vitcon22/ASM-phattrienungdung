using FruitShop.Filters;
using FruitShop.Models.DAL;
using FruitShop.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace FruitShop.Controllers
{
    [RequireRole("Admin")]
    public class OperatingCostController : Controller
    {
        private readonly OperatingCostRepository _costRepo;
        private readonly OrderRepository _orderRepo;

        public OperatingCostController(OperatingCostRepository costRepo, OrderRepository orderRepo)
        {
            _costRepo  = costRepo;
            _orderRepo = orderRepo;
        }

        // GET: /OperatingCost?month=4&year=2026
        public IActionResult Index(int? month, int? year)
        {
            month ??= DateTime.Now.Month;
            year  ??= DateTime.Now.Year;

            var costs    = _costRepo.GetByMonthYear(month.Value, year.Value).ToList();
            var revenue  = _orderRepo.GetRevenue("month", month.Value, year.Value);
            decimal totalCost = costs.Sum(c => c.Amount);
            decimal cogs    = _costRepo.GetCOGSSimple(month.Value, year.Value); // RQ32: Giá vốn
            decimal grossProfit = revenue - cogs; // Lợi nhuận gộp
            decimal netProfit = grossProfit - totalCost; // Lợi nhuận ròng

            ViewBag.Month   = month.Value;
            ViewBag.Year    = year.Value;
            ViewBag.Revenue = revenue;
            ViewBag.TotalCost = totalCost;
            ViewBag.COGS    = cogs;
            ViewBag.GrossProfit = grossProfit;
            ViewBag.NetProfit = netProfit;
            ViewBag.CostTypes = new[] { "Điện", "Nước", "Thuê mặt bằng", "Lương nhân viên", "Vận chuyển", "Marketing", "Khác" };
            return View(costs);
        }

        // POST: /OperatingCost/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OperatingCost model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction(nameof(Index), new { month = model.Month, year = model.Year });
            }
            try
            {
                _costRepo.Insert(model);
                TempData["Success"] = "Đã thêm chi phí thành công!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { month = model.Month, year = model.Year });
        }

        // POST: /OperatingCost/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, int month, int year)
        {
            try
            {
                _costRepo.Delete(id);
                TempData["Success"] = "Đã xóa chi phí!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi: {ex.Message}";
            }
            return RedirectToAction(nameof(Index), new { month, year });
        }

        // GET: /OperatingCost/ExportExcel?month=4&year=2026
        public IActionResult ExportExcel(int? month, int? year)
        {
            month ??= DateTime.Now.Month;
            year  ??= DateTime.Now.Year;

            var costs     = _costRepo.GetByMonthYear(month.Value, year.Value).ToList();
            var revenue   = _orderRepo.GetRevenue("month");
            var totalCost = costs.Sum(c => c.Amount);
            var cogs     = _costRepo.GetCOGSSimple(month.Value, year.Value);
            var grossProfit = revenue - cogs;
            var netProfit   = grossProfit - totalCost;

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Chi phí vận hành");

            // Header
            ws.Cells["A1"].Value = $"CHI PHÍ VẬN HÀNH THÁNG {month}/{year}";
            ws.Cells["A1:F1"].Merge = true;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
            ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

            // Summary row
            ws.Cells["A3"].Value = "Doanh thu tháng";
            ws.Cells["B3"].Value = (double)revenue;
            ws.Cells["B3"].Style.Numberformat.Format = "#,##0";
            ws.Cells["A4"].Value = "Giá vốn (COGS)";
            ws.Cells["B4"].Value = (double)cogs;
            ws.Cells["B4"].Style.Numberformat.Format = "#,##0";
            ws.Cells["A5"].Value = "Lợi nhuận gộp";
            ws.Cells["B5"].Value = (double)grossProfit;
            ws.Cells["B5"].Style.Numberformat.Format = "#,##0";
            ws.Cells["A6"].Value = "Chi phí vận hành";
            ws.Cells["B6"].Value = (double)totalCost;
            ws.Cells["B6"].Style.Numberformat.Format = "#,##0";
            ws.Cells["A7"].Value = "Lợi nhuận ròng";
            ws.Cells["B7"].Value = (double)netProfit;
            ws.Cells["B7"].Style.Numberformat.Format = "#,##0";
            ws.Cells["B7"].Style.Font.Bold = true;
            ws.Cells["B7"].Style.Font.Color.SetColor(netProfit >= 0 ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68));

            for (int r = 3; r <= 7; r++)
                ws.Cells[$"A{r}"].Style.Font.Bold = true;

            // Table header
            string[] headers = { "STT", "Loại chi phí", "Số tiền (đ)", "Ghi chú", "Ngày nhập" };
            int row = 10;
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cells[row, c + 1].Value = headers[c];
                ws.Cells[row, c + 1].Style.Font.Bold = true;
                ws.Cells[row, c + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, c + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(68, 114, 196));
                ws.Cells[row, c + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[row, c + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            row = 11;
            int no = 1;
            foreach (var c in costs)
            {
                ws.Cells[row, 1].Value = no++;
                ws.Cells[row, 2].Value = c.CostType;
                ws.Cells[row, 3].Value = (double)c.Amount;
                ws.Cells[row, 3].Style.Numberformat.Format = "#,##0";
                ws.Cells[row, 4].Value = c.Note;
                ws.Cells[row, 5].Value = c.CreatedAt.ToString("dd/MM/yyyy");
                row++;
            }

            ws.Cells[row + 1, 2].Value = "TỔNG CỘNG";
            ws.Cells[row + 1, 2].Style.Font.Bold = true;
            ws.Cells[row + 1, 3].Value = (double)totalCost;
            ws.Cells[row + 1, 3].Style.Numberformat.Format = "#,##0";
            ws.Cells[row + 1, 3].Style.Font.Bold = true;
            ws.Cells[row + 1, 3].Style.Font.Color.SetColor(Color.Red);

            ws.Columns.AutoFit();
            var filename = $"ChiPhiVanHanh_{month}_{year}.xlsx";
            return File(package.GetAsByteArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
    }
}
