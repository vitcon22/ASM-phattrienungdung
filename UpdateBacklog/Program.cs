using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

string path = @"D:\ASM phattrienungdung\Product_Backlog_v2111_new.xlsx";
string outPath = @"D:\ASM phattrienungdung\Product_Backlog_v2111_updated.xlsx";

using var package = new ExcelPackage(new FileInfo(path));
var ws = package.Workbook.Worksheets[0];

var updates = new Dictionary<int, (string note, string status)>
{
    // RQ05 - Supplier management (AdminSupplierController full CRUD)
    { 5, ("Da co trang AdminSupplier voi CRUD day du: Index, Create, Edit, Delete, History, PriceComparison.", "Da trien khai") },

    // RQ14 - Admin tao/sua Staff/Admin accounts
    { 14, ("Da bo sung: Admin tao tai khoan Staff/Admin (UserController Create/Edit/Delete + UserRepository CreateByAdmin/UpdateByAdmin).", "Da trien khai") },

    // RQ17 - Ghi nhan hang hong/hao hu
    { 17, ("Da co: Inventory/ReturnSpoiled action cho phep ghi nhan hang hong/hao hu voi ly do, tu dong tru ton kho.", "Da trien khai") },

    // RQ28 - Gan lo hang + han su dung
    { 28, ("Da co Batch.Create voi BatchCode tu dong, ImportDate, ManufactureDate, ExpiryDate. BatchController/Details/PrintReceipt.", "Da trien khai") },

    // RQ30 - Xuat bao cao doanh thu Excel
    { 30, ("Da co Dashboard/ExportRevenueExcel voi EPPlus (2 sheets: Don hang + Theo danh muc).", "Da trien khai") },

    // RQ38 - Nhat ky hoat dong nguoi dung
    { 38, ("Da co AuditLogRepository voi AdminAuditLogController. Loc theo keyword, controller, ngay. Ghi tu dong POST/DELETE/PUT qua AuditLogFilter.", "Da trien khai") },

    // RQ46 - Nhap nhieu san pham hang loat tu Excel
    { 46, ("Da co FruitController.ImportExcel voi EPPlus. Upload file .xlsx, parse tung dong, Insert vao Fruits.", "Da trien khai") },

    // RQ47 - Ghi nhan chi phi vanh hanh
    { 47, ("Da co OperatingCostController voi Index (theo thang), Create, Delete. Hien thi Revenue/COGS/Loi nhuan.", "Da trien khai") },

    // RQ64 - Danh gia san pham
    { 64, ("Da co ReviewController voi Submit, Edit, Delete (khach hang). AdminIndex voi Approve/Reject. IsApproved flag.", "Da trien khai") },

    // RQ66 - Them san pham yeu thich
    { 66, ("Da co WishlistController voi Toggle, Index. Nut heart tren Fruit/Details.", "Da trien khai") },

    // RQ67 - Xem danh sach Wishlist
    { 67, ("Da co trang Wishlist/Index rieng. Hien thi tat ca san pham yeu thich voi nut Mua ngay va Xoa.", "Da trien khai") },

    // RQ68 - Xoa san pham khoi Wishlist
    { 68, ("Da co trong Wishlist/Index va WishlistController.Toggle (POST). Khach co the xoa bat ky luc nao.", "Da trien khai") },

    // RQ80 - Xoa/vô hieu hoa coupon
    { 80, ("Da co CouponController.ToggleActive cho phep bat/tat coupon. Khong co xoa vinh vien (an toan du lieu).", "Da trien khai") },

    // RQ101 - Xuat danh sach san pham ra file Excel
    { 101, ("Da co User/ExportExcel (EPPlus, 8 cot: ID, Ho ten, Email, DT, Vai tro, Trang thai, Diem, Ngay tao).", "Da trien khai") },

    // RQ102 - In phieu nhap kho
    { 102, ("Da co Batch/PrintReceipt voi Layout=null, in duoc truc tiep tu trinh duyet. Co ky ten 3 ben.", "Da trien khai") },

    // RQ107 - Xem bao cao ton kho theo danh muc
    { 107, ("Da co Dashboard/CategoryReport voi CategoryInventoryReportItem. Chart.js (bar+doughnut), Export Excel, filter theo danh muc. Link trong sidebar.", "Da trien khai") },

    // RQ109 - Cau hinh nguong ton kho thap
    { 109, ("Da co Fruit.MinStock. Dashboard hien thi LowStockFruits khi StockQuantity < MinStock. Batch/ExpiryWarning.", "Da trien khai") },

    // RQ103 - Email xac nhan khi dat hang
    { 103, ("Da goi _emailService.SendOrderConfirmationAsync trong OrderController.Checkout. EmailTemplateService co template HTML day du voi chi tiet don hang (san pham, so luong, don gia, giam gia, diem tich luy).", "Da trien khai") },

    // RQ104 - Email khi trang thai don hang thay doi
    { 104, ("Da goi _emailService.SendStatusUpdateAsync trong OrderController.UpdateStatus. Gui email khi don chuyen sang Confirmed/Shipping/Delivered/Cancelled.", "Da trien khai") },

    // RQ105 - Quen mat khau dat lai qua email
    { 105, ("Da co AccountController.ForgotPassword voi HTML template email dep, link dat lai co han trong 2h, AccountController.ResetPassword xu ly.", "Da trien khai") },
};

int maxRow = ws.Dimension.Rows;
int updated = 0;
for (int r = 2; r <= maxRow; r++)
{
    var cellA = ws.Cells[r, 1].Text?.Trim();
    if (string.IsNullOrEmpty(cellA)) continue;

    foreach (var kvp in updates)
    {
        string expected = "RQ" + kvp.Key.ToString("D2");
        if (cellA.StartsWith(expected))
        {
            ws.Cells[r, 7].Value = kvp.Value.note;
            ws.Cells[r, 8].Value = kvp.Value.status;
            ws.Cells[r, 8].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[r, 8].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(34, 197, 94));
            ws.Cells[r, 8].Style.Font.Color.SetColor(Color.White);
            ws.Cells[r, 8].Style.Font.Bold = true;
            Console.WriteLine($"Updated RQ{kvp.Key}: {kvp.Value.status}");
            updated++;
            break;
        }
    }
}

await package.SaveAsAsync(new FileInfo(outPath));
Console.WriteLine($"Saved to: {outPath}");Console.WriteLine($"Done! Updated {updated} rows.");
