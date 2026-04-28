using FruitShop.Models.Entities;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text;

namespace FruitShop.Services;

/// <summary>
/// Service xuất dữ liệu ra CSV, Excel, PDF - tách khỏi Controller
/// </summary>
public interface IExportService
{
    byte[] ExportOrdersToCsv(IEnumerable<Order> orders);
    byte[] ExportFruitsToCsv(IEnumerable<Fruit> fruits);
    byte[] ExportUsersToCsv(IEnumerable<User> users);
    byte[] ExportOrdersToExcel(IEnumerable<Order> orders);
    byte[] ExportUsersToExcel(IEnumerable<User> users);
    byte[] ExportRevenueToExcel(IEnumerable<Order> orders, IEnumerable<dynamic> byCategory, DateTime from, DateTime to);
    byte[] ExportInvoiceToPdf(Order order);
}

public class ExportService : IExportService
{
    public byte[] ExportOrdersToCsv(IEnumerable<Order> orders)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Mã ĐH,Khách hàng,Ngày đặt,Tổng tiền,Trạng thái,Địa chỉ,Ghi chú");
        foreach (var o in orders)
        {
            sb.AppendLine($"{o.OrderId},\"{o.CustomerName}\",{o.OrderDate:dd/MM/yyyy HH:mm},{o.TotalAmount:N0},{o.GetStatusText()},\"{o.ShippingAddress}\",\"{o.Note}\"");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public byte[] ExportFruitsToCsv(IEnumerable<Fruit> fruits)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ID,Tên sản phẩm,Danh mục,Nhà cung cấp,Giá bán,Tồn kho,Ngưỡng tối thiểu,Đơn vị,Xuất xứ,Trạng thái");
        foreach (var f in fruits)
        {
            sb.AppendLine($"{f.FruitId},\"{f.FruitName}\",\"{f.CategoryName}\",\"{f.SupplierName}\",{f.Price},{f.StockQuantity},{f.MinStock},{f.Unit},\"{f.Origin}\",{(f.IsActive ? "Đang bán" : "Ngưng bán")}");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public byte[] ExportUsersToCsv(IEnumerable<User> users)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ID,Họ tên,Email,Điện thoại,Vai trò,Trạng thái,Ngày tạo");
        foreach (var u in users)
        {
            sb.AppendLine($"{u.UserId},\"{u.FullName}\",{u.Email},{u.Phone},{u.RoleName},{(u.IsActive ? "Hoạt động" : "Khoá")},{u.CreatedAt:dd/MM/yyyy}");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public byte[] ExportUsersToExcel(IEnumerable<User> users)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Người dùng");

        ws.Cells["A1"].Value = "DANH SÁCH NGƯỜI DÙNG";
        ws.Cells["A1:H1"].Merge = true;
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 14;
        ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
        ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

        string[] headers = { "ID", "Họ tên", "Email", "Điện thoại", "Vai trò", "Trạng thái", "Điểm tích lũy", "Ngày tạo" };
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
        foreach (var u in users)
        {
            ws.Cells[row, 1].Value = u.UserId;
            ws.Cells[row, 2].Value = u.FullName;
            ws.Cells[row, 3].Value = u.Email;
            ws.Cells[row, 4].Value = u.Phone ?? "—";
            ws.Cells[row, 5].Value = u.RoleName;
            ws.Cells[row, 6].Value = u.IsActive ? "Hoạt động" : "Khoá";
            ws.Cells[row, 7].Value = u.Points;
            ws.Cells[row, 8].Value = u.CreatedAt.ToString("dd/MM/yyyy");

            if (!u.IsActive)
            {
                for (int c = 1; c <= 8; c++)
                    ws.Cells[row, c].Style.Font.Color.SetColor(Color.Gray);
            }
            row++;
        }

        ws.Columns.AutoFit();
        return package.GetAsByteArray();
    }

    public byte[] ExportOrdersToExcel(IEnumerable<Order> orders)
    {
        using var package = new ExcelPackage();
        var ws = package.Workbook.Worksheets.Add("Orders");
        ws.Cells[1, 1].Value = "Mã ĐH";
        ws.Cells[1, 2].Value = "Ngày đặt";
        ws.Cells[1, 3].Value = "Khách hàng";
        ws.Cells[1, 4].Value = "Tổng tiền";
        ws.Cells[1, 5].Value = "Trạng thái";

        using (var range = ws.Cells[1, 1, 1, 5])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        }

        int row = 2;
        foreach (var o in orders)
        {
            ws.Cells[row, 1].Value = o.OrderId;
            ws.Cells[row, 2].Value = o.OrderDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cells[row, 3].Value = o.CustomerName;
            ws.Cells[row, 4].Value = (double)o.TotalAmount;
            ws.Cells[row, 5].Value = o.Status;
            row++;
        }
        ws.Cells.AutoFitColumns();
        return package.GetAsByteArray();
    }

    public byte[] ExportRevenueToExcel(IEnumerable<Order> orders, IEnumerable<dynamic> byCategory, DateTime from, DateTime to)
    {
        using var package = new ExcelPackage();

        // Sheet 1: Đơn hàng
        var ws = package.Workbook.Worksheets.Add("Báo cáo doanh thu");
        ws.Cells["A1"].Value = $"BÁO CÁO DOANH THU — {from:dd/MM/yyyy} đến {to:dd/MM/yyyy}";
        ws.Cells["A1:G1"].Merge = true;
        ws.Cells["A1"].Style.Font.Bold = true;
        ws.Cells["A1"].Style.Font.Size = 14;
        ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(31, 78, 121));
        ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

        string[] headers = { "Mã đơn", "Khách hàng", "Ngày đặt", "Tổng tiền", "Giảm giá", "Thực thu", "Trạng thái" };
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
        var validOrders = orders.Where(o => o.Status != "Cancelled").ToList();
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
            if (o.Status == "Cancelled")
                ws.Cells[row, 1, row, 7].Style.Font.Color.SetColor(Color.Gray);
            row++;
        }

        ws.Cells[row, 1].Value = "TỔNG DOANH THU (Delivered)";
        ws.Cells[row, 1, row, 5].Merge = true;
        ws.Cells[row, 1].Style.Font.Bold = true;
        ws.Cells[row, 6].Value = (double)validOrders.Sum(o => o.TotalAmount - o.DiscountAmount);
        ws.Cells[row, 6].Style.Font.Bold = true;
        ws.Cells[row, 6].Style.Numberformat.Format = "#,##0";
        ws.Columns.AutoFit();

        // Sheet 2: Theo danh mục
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

        return package.GetAsByteArray();
    }

    public byte[] ExportInvoiceToPdf(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<table border='1' cellpadding='6' cellspacing='0'>");
        sb.AppendLine($"<tr><td><b>Mã ĐH:</b></td><td>#{order.OrderId}</td></tr>");
        sb.AppendLine($"<tr><td><b>Ngày lập:</b></td><td>{order.OrderDate:dd/MM/yyyy HH:mm}</td></tr>");
        sb.AppendLine($"<tr><td><b>Khách hàng:</b></td><td>{order.CustomerName}</td></tr>");
        sb.AppendLine($"<tr><td><b>Địa chỉ:</b></td><td>{order.ShippingAddress}</td></tr>");
        sb.AppendLine("</table>");

        var detailHtml = new StringBuilder();
        detailHtml.AppendLine("<table border='1' cellpadding='6' cellspacing='0' style='width:100%;margin-top:20px'>");
        detailHtml.AppendLine("<thead><tr><th>Sản phẩm</th><th>ĐVT</th><th>Số lượng</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>");
        foreach (var d in order.OrderDetails)
        {
            detailHtml.AppendLine($"<tr><td>{d.FruitName}</td><td>{d.Unit}</td><td>{d.Quantity}</td><td>{d.UnitPrice:N0}đ</td><td>{d.Quantity * d.UnitPrice:N0}đ</td></tr>");
        }
        detailHtml.AppendLine("</tbody></table>");

        var html = $@"
        <html><head><style>body{{font-family:Arial,sans-serif}}table{{border-collapse:collapse;width:100%}}th,td{{border:1px solid #ddd;padding:8px}}th{{background:#f2f2f2}}</style></head><body>
        <h1>Hoá Đơn Bán Hàng - FruitShop</h1>
        {sb}
        {detailHtml}
        <h3 style='text-align:right'>Tổng cộng: {order.TotalAmount:N0}đ</h3>
        <p><b>Ghi chú:</b> {order.Note}</p>
        </body></html>";

        using var stream = new MemoryStream();
        iText.Html2pdf.HtmlConverter.ConvertToPdf(html, stream, new iText.Html2pdf.ConverterProperties());
        return stream.ToArray();
    }
}
