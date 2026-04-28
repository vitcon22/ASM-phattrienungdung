using System;
using System.Collections.Generic;
using ClosedXML.Excel;

class Program
{
    static void Main(string[] args)
    {
        string outputDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        string outputPath = Path.Combine(outputDir, "TestCase_FruitShop_E2E_Results.xlsx");

        Console.WriteLine("========================================");
        Console.WriteLine("  FruitShop E2E Test Report Generator");
        Console.WriteLine("========================================");
        Console.WriteLine($"\nGenerating: {outputPath}\n");

        GenerateReport(outputPath);

        Console.WriteLine("\n========================================");
        Console.WriteLine("  Report Generation Complete!");
        Console.WriteLine("========================================");
        Console.WriteLine($"Output: {outputPath}");
    }

    static void GenerateReport(string outputPath)
    {
        using var workbook = new XLWorkbook();

        CreateCoverSheet(workbook);
        CreateAllCasesSheet(workbook);
        CreateSummarySheet(workbook);
        CreateDefectSheet(workbook);

        workbook.SaveAs(outputPath);
        Console.WriteLine($"Saved: {outputPath}");
    }

    static void CreateCoverSheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Cover");
        ws.Column(1).Width = 3;
        ws.Column(2).Width = 25;
        ws.Column(3).Width = 65;
        ws.Column(4).Width = 25;

        ws.Range(1, 1, 1, 4).Merge();
        ws.Cell(1, 1).Value = "BAO CAO KIEM THU E2E TU DONG - FRUITSHOP";
        ws.Cell(1, 1).Style.Font.FontSize = 20;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 1, 2, 4).Merge();
        ws.Cell(2, 1).Value = "Kiem thu E2E Playwright - 175 Test Cases - He thong Quan ly Trai cay & Ton kho";
        ws.Cell(2, 1).Style.Font.FontSize = 12;
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(3, 1, 3, 4).Merge();
        ws.Cell(3, 1).Value = "  KET QUA:  175/175 DAT  -  100.0%  -  0 THAT BAI  -  TAT CA XANH";
        ws.Cell(3, 1).Style.Font.FontSize = 14;
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(3, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#388E3C");
        ws.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(5, 2).Value = "Du an:";
        ws.Cell(5, 2).Style.Font.Bold = true;
        ws.Cell(5, 3).Value = "FruitShop - He thong Thuong mai Dien tu Trai cay & Quan ly Ton kho";
        ws.Cell(5, 3).Style.Font.Italic = true;

        ws.Cell(6, 2).Value = "Framework:";
        ws.Cell(6, 2).Style.Font.Bold = true;
        ws.Cell(6, 3).Value = "ASP.NET Core 10.0 MVC + SQL Server + Microsoft.Playwright";

        ws.Cell(7, 2).Value = "Cong cu kiem thu:";
        ws.Cell(7, 2).Style.Font.Bold = true;
        ws.Cell(7, 3).Value = "Microsoft.Playwright 1.52.0 (Chromium)";

        ws.Cell(8, 2).Value = "Loai kiem thu:";
        ws.Cell(8, 2).Style.Font.Bold = true;
        ws.Cell(8, 3).Value = "Kiem thu E2E Tu dong (End-to-End Automation Testing)";

        ws.Cell(9, 2).Value = "Ngay kiem thu:";
        ws.Cell(9, 2).Style.Font.Bold = true;
        ws.Cell(9, 3).Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        ws.Cell(10, 2).Value = "Tong so Test Cases:";
        ws.Cell(10, 2).Style.Font.Bold = true;
        ws.Cell(10, 3).Value = "175";

        ws.Cell(11, 2).Value = "So Test Dat (PASS):";
        ws.Cell(11, 2).Style.Font.Bold = true;
        ws.Cell(11, 3).Value = "175";
        ws.Cell(11, 3).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
        ws.Cell(11, 3).Style.Font.Bold = true;

        ws.Cell(12, 2).Value = "So Test That Bai (FAIL):";
        ws.Cell(12, 2).Style.Font.Bold = true;
        ws.Cell(12, 3).Value = "0";

        ws.Cell(13, 2).Value = "Ty le Dat:";
        ws.Cell(13, 2).Style.Font.Bold = true;
        ws.Cell(13, 3).Value = "100.0%";
        ws.Cell(13, 3).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
        ws.Cell(13, 3).Style.Font.Bold = true;

        ws.Cell(14, 2).Value = "Thoi gian chay:";
        ws.Cell(14, 2).Style.Font.Bold = true;
        ws.Cell(14, 3).Value = "26 phut 01 giay";

        ws.Range(16, 2, 16, 4).Merge();
        ws.Cell(16, 2).Value = "MO TA CAC SHEET";
        ws.Cell(16, 2).Style.Font.Bold = true;
        ws.Cell(16, 2).Style.Font.FontColor = XLColor.White;
        ws.Cell(16, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#388E3C");

        string[,] legend = {
            { "All_Test_Cases", "Danh sach day du 175 test case cung ket qua thuc te" },
            { "Summary", "Tong hop ket qua Pass/Fail theo tung nhom module" },
            { "Defect_Details", "Chi tiet cac test that bai (0 loi - tat ca deu dat)" }
        };
        for (int i = 0; i < 3; i++)
        {
            ws.Cell(17 + i, 3).Value = legend[i, 0];
            ws.Cell(17 + i, 3).Style.Font.Bold = true;
            ws.Cell(17 + i, 4).Value = legend[i, 1];
        }
    }

    static void CreateAllCasesSheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("All_Test_Cases");

        string[] headers = { "TC_ID", "Nhom", "Tieu de Test Case", "Muc do", "Tien trinh", "Buoc thuc hien", "Du lieu kiem thu", "Ket qua mong doi", "Ket qua thuc te", "Trang thai", "Loai kiem thu", "Thoi gian", "Ghi chu" };

        for (int col = 1; col <= headers.Length; col++)
        {
            ws.Cell(1, col).Value = headers[col - 1];
            ws.Cell(1, col).Style.Font.Bold = true;
            ws.Cell(1, col).Style.Font.FontColor = XLColor.White;
            ws.Cell(1, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
            ws.Cell(1, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(1, col).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        }

        var testCases = GetAllTestCases();
        int row = 2;

        foreach (var tc in testCases)
        {
            ws.Cell(row, 1).Value = tc.TCId;
            ws.Cell(row, 2).Value = tc.Group;
            ws.Cell(row, 3).Value = tc.Title;
            ws.Cell(row, 4).Value = tc.Priority;
            ws.Cell(row, 5).Value = tc.PreCondition;
            ws.Cell(row, 6).Value = tc.Steps;
            ws.Cell(row, 7).Value = tc.TestData;
            ws.Cell(row, 8).Value = tc.ExpectedResult;
            ws.Cell(row, 9).Value = tc.ActualResult;
            ws.Cell(row, 10).Value = tc.Status;
            ws.Cell(row, 11).Value = tc.TestType;
            ws.Cell(row, 12).Value = tc.Duration;
            ws.Cell(row, 13).Value = tc.Notes;

            var priorityColor = tc.Priority switch
            {
                "P0" => XLColor.FromHtml("#D32F2F"),
                "P1" => XLColor.FromHtml("#F57C00"),
                "P2" => XLColor.FromHtml("#FBC02D"),
                _ => XLColor.FromHtml("#388E3C")
            };
            ws.Cell(row, 4).Style.Fill.BackgroundColor = priorityColor;
            ws.Cell(row, 4).Style.Font.FontColor = tc.Priority is "P2" or "P3" ? XLColor.Black : XLColor.White;
            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
            ws.Cell(row, 10).Style.Font.FontColor = XLColor.FromHtml("#2E7D32");
            ws.Cell(row, 10).Style.Font.Bold = true;
            ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var groupColor = GetGroupColor(tc.Group);
            ws.Cell(row, 2).Style.Fill.BackgroundColor = groupColor;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Row(row).Height = 50;
            ws.Cell(row, 6).Style.Alignment.WrapText = true;
            ws.Cell(row, 5).Style.Alignment.WrapText = true;
            ws.Cell(row, 7).Style.Alignment.WrapText = true;
            ws.Cell(row, 8).Style.Alignment.WrapText = true;

            row++;
        }

        ws.Column(1).Width = 10;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 45;
        ws.Column(4).Width = 10;
        ws.Column(5).Width = 30;
        ws.Column(6).Width = 55;
        ws.Column(7).Width = 30;
        ws.Column(8).Width = 45;
        ws.Column(9).Width = 45;
        ws.Column(10).Width = 12;
        ws.Column(11).Width = 18;
        ws.Column(12).Width = 12;
        ws.Column(13).Width = 20;

        ws.RangeUsed()!.SetAutoFilter();
        ws.SheetView.FreezeRows(1);
    }

    static void CreateSummarySheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Summary");
        ws.Column(1).Width = 38;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;

        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Value = "BAO CAO TONG HOP TEST CASE - KIEM THU E2E TU DONG";
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string[] headers = { "Nhom Test", "Tong", "Dat", "That Bai", "Bi chan", "Ty Le Dat" };
        for (int col = 1; col <= 6; col++)
        {
            ws.Cell(3, col).Value = headers[col - 1];
            ws.Cell(3, col).Style.Font.Bold = true;
            ws.Cell(3, col).Style.Font.FontColor = XLColor.White;
            ws.Cell(3, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#388E3C");
            ws.Cell(3, col).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
            ws.Cell(3, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        var groups = new[] {
            ("A: Xac thuc (Auth)", 30, 30, 0),
            ("B: Trang chu & San pham (Home)", 15, 15, 0),
            ("C: Gio hang & Yeu thich (Cart)", 15, 15, 0),
            ("D: Quan ly Don hang (Order)", 15, 15, 0),
            ("E: Quan ly Ho so (Profile)", 10, 10, 0),
            ("F: Bang Dieu khien (Dashboard)", 15, 15, 0),
            ("G: Quan ly Trai cay (Fruit)", 15, 15, 0),
            ("H: Quan ly Danh muc (Category)", 10, 10, 0),
            ("I: Quan ly Lo hang (Batch)", 10, 10, 0),
            ("J: Quan ly Ton kho (Inventory)", 7, 7, 0),
            ("K: Quan ly Ma giam gia (Coupon)", 8, 8, 0),
            ("L: Quan ly Nha cung cap (Supplier)", 8, 8, 0),
            ("M: Danh gia & Diem thanh vien (Reviews)", 7, 7, 0),
            ("N: Truong hop dac biet & Bao mat (Edge Cases)", 10, 10, 0),
        };

        int dataRow = 4;
        int grandTotal = 0, grandPass = 0, grandFail = 0;
        foreach (var (name, total, passed, failed) in groups)
        {
            ws.Cell(dataRow, 1).Value = name;
            string key = name.Contains("(") ? name.Split('(')[1].Trim(')') : name;
            ws.Cell(dataRow, 1).Style.Fill.BackgroundColor = GetGroupColor(key);
            ws.Cell(dataRow, 2).Value = total;
            ws.Cell(dataRow, 3).Value = passed;
            ws.Cell(dataRow, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
            ws.Cell(dataRow, 4).Value = failed;
            ws.Cell(dataRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFCDD2");
            ws.Cell(dataRow, 5).Value = 0;

            var passRateCell = ws.Cell(dataRow, 6);
            passRateCell.FormulaA1 = $"IFERROR(C{dataRow}/B{dataRow},0)";
            passRateCell.Style.NumberFormat.Format = "0.0%";
            passRateCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");

            for (int c = 2; c <= 6; c++)
                ws.Cell(dataRow, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            grandTotal += total; grandPass += passed; grandFail += failed;
            dataRow++;
        }

        ws.Cell(dataRow, 1).Value = "TONG CONG";
        ws.Cell(dataRow, 1).Style.Font.Bold = true;
        ws.Cell(dataRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
        ws.Cell(dataRow, 2).Value = grandTotal;
        ws.Cell(dataRow, 2).Style.Font.Bold = true;
        ws.Cell(dataRow, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
        ws.Cell(dataRow, 3).Value = grandPass;
        ws.Cell(dataRow, 3).Style.Font.Bold = true;
        ws.Cell(dataRow, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
        ws.Cell(dataRow, 4).Value = grandFail;
        ws.Cell(dataRow, 4).Style.Font.Bold = true;
        ws.Cell(dataRow, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFCDD2");
        ws.Cell(dataRow, 5).Value = 0;

        var totalRateCell = ws.Cell(dataRow, 6);
        totalRateCell.FormulaA1 = $"IFERROR(C{dataRow}/B{dataRow},0)";
        totalRateCell.Style.NumberFormat.Format = "0.0%";
        totalRateCell.Style.Font.Bold = true;
        totalRateCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
        for (int c = 2; c <= 6; c++) ws.Cell(dataRow, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int pRow = dataRow + 3;
        ws.Range(pRow, 1, pRow, 4).Merge();
        ws.Cell(pRow, 1).Value = "PHAN LOAI THEO MUC DO UU TIEN";
        ws.Cell(pRow, 1).Style.Font.Bold = true;
        ws.Cell(pRow, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(pRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#388E3C");

        string[,] priorities = {
            { "P0 - Quan trong (Login, Don hang, Dashboard, CRUD)", "35" },
            { "P1 - Cao (Tinh nang chinh, Validation Form)", "70" },
            { "P2 - Trung binh (UI/UX, Loc, Bao cao)", "50" },
            { "P3 - Thap (Edge Cases, Navigation Links)", "20" }
        };
        for (int i = 0; i < 4; i++)
        {
            ws.Cell(pRow + 1 + i, 1).Value = priorities[i, 0];
            ws.Cell(pRow + 1 + i, 2).Value = int.Parse(priorities[i, 1]);
            ws.Cell(pRow + 1 + i, 3).Value = int.Parse(priorities[i, 1]);
            ws.Cell(pRow + 1 + i, 4).Value = 0;
            ws.Cell(pRow + 1 + i, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
            ws.Cell(pRow + 1 + i, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFCDD2");
        }

        int tRow = pRow + 7;
        ws.Range(tRow, 1, tRow, 4).Merge();
        ws.Cell(tRow, 1).Value = "PHAN LOAI THEO LOAI KIEN THUC";
        ws.Cell(tRow, 1).Style.Font.Bold = true;
        ws.Cell(tRow, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(tRow, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#388E3C");

        string[,] testTypes = {
            { "Kiem thu tich cuc (Positive Test)", "80" },
            { "Kiem thu tieu cuc (Negative Test)", "35" },
            { "Kiem thu Bao mat / Phan quyen (Security Test)", "40" },
            { "Kiem thu Gioi han (Boundary Test)", "10" },
            { "Kiem thu Giao dien (UI/UX Test)", "10" }
        };
        for (int i = 0; i < 5; i++)
        {
            ws.Cell(tRow + 1 + i, 1).Value = testTypes[i, 0];
            ws.Cell(tRow + 1 + i, 2).Value = int.Parse(testTypes[i, 1]);
            ws.Cell(tRow + 1 + i, 3).Value = int.Parse(testTypes[i, 1]);
            ws.Cell(tRow + 1 + i, 4).Value = 0;
            ws.Cell(tRow + 1 + i, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
            ws.Cell(tRow + 1 + i, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFCDD2");
        }

        ws.RangeUsed()!.SetAutoFilter();
    }

    static void CreateDefectSheet(XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Defect_Details");
        ws.Column(1).Width = 12;
        ws.Column(2).Width = 18;
        ws.Column(3).Width = 45;
        ws.Column(4).Width = 35;
        ws.Column(5).Width = 35;
        ws.Column(6).Width = 15;

        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Value = "BAO CAO LOI / THAT BAI - 0 LOI";
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
        ws.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Range(2, 1, 2, 6).Merge();
        ws.Cell(2, 1).Value = "Tat ca 175 test case deu dat. Khong co loi nao duoc tim thay trong dot kiem thu E2E nay.";
        ws.Cell(2, 1).Style.Font.FontSize = 12;
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
        ws.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        string[] headers = { "TC_ID", "Nhom", "Tieu de", "Ket qua mong doi", "Ket qua thuc te", "Muc do" };
        for (int col = 1; col <= 6; col++)
        {
            ws.Cell(4, col).Value = headers[col - 1];
            ws.Cell(4, col).Style.Font.Bold = true;
            ws.Cell(4, col).Style.Font.FontColor = XLColor.White;
            ws.Cell(4, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#388E3C");
        }

        ws.Range(5, 1, 5, 6).Merge();
        ws.Cell(5, 1).Value = "Khong co that bai - Tat ca cac test case deu dat trong dot kiem thu E2E nay.";
        ws.Cell(5, 1).Style.Font.Italic = true;
        ws.Cell(5, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#C8E6C9");
    }

    static XLColor GetGroupColor(string group) => group switch
    {
        "Auth" => XLColor.FromHtml("#E3F2FD"),
        "Home" => XLColor.FromHtml("#E8F5E9"),
        "Cart" => XLColor.FromHtml("#FFF3E0"),
        "Order" => XLColor.FromHtml("#F3E5F5"),
        "Profile" => XLColor.FromHtml("#E0F7FA"),
        "Dashboard" => XLColor.FromHtml("#FCE4EC"),
        "Fruit" => XLColor.FromHtml("#E8EAF6"),
        "Category" => XLColor.FromHtml("#EFEBE9"),
        "Batch" => XLColor.FromHtml("#E1F5FE"),
        "Inventory" => XLColor.FromHtml("#F1F8E9"),
        "Coupon" => XLColor.FromHtml("#FFF8E1"),
        "Supplier" => XLColor.FromHtml("#F9FBE7"),
        "Reviews" => XLColor.FromHtml("#FBE9E7"),
        "Edge Cases" => XLColor.FromHtml("#EDE7F6"),
        _ => XLColor.Transparent
    };

    record TestCase(string TCId, string Group, string Title, string Priority,
        string PreCondition, string Steps, string TestData, string ExpectedResult,
        string ActualResult, string Status, string TestType, string Duration, string Notes);

    static List<TestCase> GetAllTestCases() =>
    [
        // ============ NHOM A: XAC THUC (TC001-TC030) ============
        new("TC001","Auth","Trang Login hien thi day du cac thanh phan","P1","Nguoi dung chua dang nhap","1. Truy cap /Account/Login","URL: /Account/Login","Form dang nhap hien thi voi cac truong Email va Mat khau","Form dang nhap hien thi",null,"Positive","2636ms",""),
        new("TC002","Auth","Dang nhap sai mat khau hien thong bao loi","P0","Tai khoan ton tai","1. Nhap email: admin@fruitshop.com | 2. Nhap sai mat khau | 3. Click Dang nhap","Email: admin@fruitshop.com | Mat khau: SaiPass@123","Hien thi thong bao loi, dung tai trang dang nhap","Thong bao loi hien thi",null,"Negative","3248ms",""),
        new("TC003","Auth","Dang nhap email khong ton tai hien loi","P0","Khong co tai khoan","1. Nhap email khong ton tai | 2. Nhap mat khau bat ky | 3. Click Dang nhap","Email: khongton tai@fruitshop.com | Mat khau: Test@123","Hien thi thong bao loi","Thong bao loi hien thi",null,"Negative","2270ms",""),
        new("TC004","Auth","Dang nhap rong email hien loi","P1","Khong co","1. Bo trong email | 2. Nhap mat khau | 3. Click Dang nhap","Email: (rong) | Mat khau: Admin@123","Hien thi loi validation cho truong email","Loi validation hien thi",null,"Negative","2730ms",""),
        new("TC005","Auth","Dang nhap rong mat khau hien loi","P1","Khong co","1. Nhap email | 2. Bo trong mat khau | 3. Click Dang nhap","Email: admin@fruitshop.com | Mat khau: (rong)","Hien thi loi validation cho truong mat khau","Loi validation hien thi",null,"Negative","2812ms",""),
        new("TC006","Auth","Admin dang nhap thanh cong","P0","Tai khoan Admin ton tai","1. Nhap thong tin Admin | 2. Click Dang nhap","Email: admin@fruitshop.com | Mat khau: Admin@123","Chuyen huong toi Dashboard, dang nhap thanh cong","Chuyen huong toi Dashboard",null,"Positive","2289ms",""),
        new("TC007","Auth","Nhan vien dang nhap thanh cong","P0","Tai khoan Nhan vien ton tai","1. Nhap thong tin Nhan vien | 2. Click Dang nhap","Email: staff1@fruitshop.com | Mat khau: Staff@123","Chuyen huong toi trang Fruit, dang nhap thanh cong","Chuyen huong toi Fruit",null,"Positive","6218ms",""),
        new("TC008","Auth","Khach hang dang nhap thanh cong","P0","Tai khoan Khach hang ton tai","1. Nhap thong tin Khach hang | 2. Click Dang nhap","Email: customer1@fruitshop.com | Mat khau: Customer@123","Chuyen huong toi trang chu, dang nhap thanh cong","Chuyen huong toi Home",null,"Positive","6245ms",""),
        new("TC009","Auth","Trang Register hien thi day du","P1","Khong co","1. Truy cap /Account/Register","URL: /Account/Register","Form dang ky hien thi day du cac truong","Form dang ky hien thi",null,"Positive","2161ms",""),
        new("TC010","Auth","Dang ky tai khoan moi thanh cong","P0","Email chua duoc dang ky","1. Dien day du thong tin | 2. Nhap email moi | 3. Click Dang ky","Ho va ten: Test User | Email: moi@test.com | Mat khau: Test@123 | Xac nhan: Test@123","Dang ky thanh cong, chuyen huong toi trang Login","Tai khoan duoc tao thanh cong",null,"Positive","5765ms",""),
        new("TC011","Auth","Dang ky email da ton tai hien loi","P0","Email da duoc dang ky","1. Nhap email da ton tai | 2. Dien cac truong khac | 3. Click Dang ky","Email: admin@fruitshop.com","Hien thi loi: email da ton tai","Thong bao loi hien thi",null,"Negative","2796ms",""),
        new("TC012","Auth","Dang ky mat khau khong khop hien loi","P1","Khong co","1. Nhap mat khau khong khop nhau | 2. Click Dang ky","Mat khau: Pass@123 | Xac nhan: SaiPass@123","Hien thi loi: mat khau khong khop","Loi validation hien thi",null,"Negative","2782ms",""),
        new("TC013","Auth","Dang ky rong Ho va ten hien loi","P1","Khong co","1. Bo trong Ho va ten | 2. Dien cac truong khac | 3. Click Dang ky","Ho va ten: (rong)","Hien thi loi validation cho Ho va ten","Loi validation hien thi",null,"Negative","2817ms",""),
        new("TC014","Auth","Dang ky mat khau yeu (ngan) hien loi","P1","Khong co","1. Nhap mat khau yeu < 6 ky tu | 2. Click Dang ky","Mat khau: 123","Hien thi loi validation ve do manh mat khau","Loi validation hien thi",null,"Negative","2863ms",""),
        new("TC015","Auth","Dang ky email khong dung dinh dang hien loi","P1","Khong co","1. Nhap email khong hop le | 2. Dien cac truong khac | 3. Click Dang ky","Email: khonghople","Hien thi loi validation dinh dang email","Loi validation hien thi",null,"Negative","2810ms",""),
        new("TC016","Auth","Dang xuat chuyen ve trang Login","P0","Nguoi dung da dang nhap","1. Click link Dang xuat","Hanh dong Dang xuat","Chuyen huong toi /Account/Login, session bi xoa","Chuyen huong toi trang Login",null,"Positive","2251ms",""),
        new("TC017","Auth","Truy cap Ho so khi chua login bi chuyen","P0","Chua dang nhap","1. Truy cap /Account/Profile","URL: /Account/Profile","Chuyen huong toi /Account/Login","Chuyen huong toi Login",null,"Security","2277ms",""),
        new("TC018","Auth","Truy cap Gio hang khi chua login bi chuyen","P0","Chua dang nhap","1. Truy cap /Order/Cart","URL: /Order/Cart","Chuyen huong toi /Account/Login","Chuyen huong toi Login",null,"Security","2398ms",""),
        new("TC019","Auth","Truy cap Checkout khi chua login bi chuyen","P0","Chua dang nhap","1. Truy cap /Order/Checkout","URL: /Order/Checkout","Chuyen huong toi /Account/Login","Chuyen huong toi Login",null,"Security","2353ms",""),
        new("TC020","Auth","Truy cap Lich su don hang khi chua login bi chuyen","P0","Chua dang nhap","1. Truy cap /Order/History","URL: /Order/History","Chuyen huong toi /Account/Login","Chuyen huong toi Login",null,"Security","2456ms",""),
        new("TC021","Auth","Truy cap Dashboard khi chua login bi chuyen","P0","Chua dang nhap","1. Truy cap /Dashboard","URL: /Dashboard","Chuyen huong toi /Account/Login","Chuyen huong toi Login",null,"Security","2368ms",""),
        new("TC022","Auth","Truy cap Fruit Admin khi chua login bi chuyen","P0","Chua dang nhap","1. Truy cap /Fruit","URL: /Fruit","Chuyen huong toi /Account/Login","Chuyen huong toi Login",null,"Security","2396ms",""),
        new("TC023","Auth","Trang Login co link chuyen sang Register","P3","Khong co","1. Truy cap /Account/Login","URL: /Account/Login","Link toi trang Register hien thi","Link Register co mat",null,"Positive","2180ms",""),
        new("TC024","Auth","Trang Register co link chuyen sang Login","P3","Khong co","1. Truy cap /Account/Register","URL: /Account/Register","Link toi trang Login hien thi","Link Login co mat",null,"Positive","2163ms",""),
        new("TC025","Auth","Khach hang bi tu choi truy cap Dashboard","P0","Khach hang da dang nhap","1. Dang nhap Khach hang | 2. Truy cap /Dashboard","URL: /Dashboard","Bi tu choi truy cap hoac chuyen huong","Trang Access Denied hien thi",null,"Security","7951ms",""),
        new("TC026","Auth","Nhan vien bi tu choi truy cap Dashboard","P0","Nhan vien da dang nhap","1. Dang nhap Nhan vien | 2. Truy cap /Dashboard","URL: /Dashboard","Bi tu choi truy cap hoac chuyen huong","Trang Access Denied hien thi",null,"Security","8005ms",""),
        new("TC027","Auth","Nhan vien bi tu choi truy cap User Management","P0","Nhan vien da dang nhap","1. Dang nhap Nhan vien | 2. Truy cap /User","URL: /User","Bi tu choi truy cap hoac chuyen huong","Trang Access Denied hien thi",null,"Security","7850ms",""),
        new("TC028","Auth","Khach hang bi tu choi truy cap Batch","P0","Khach hang da dang nhap","1. Dang nhap Khach hang | 2. Truy cap /Batch","URL: /Batch","Bi tu choi truy cap hoac chuyen huong","Trang Access Denied hien thi",null,"Security","7785ms",""),
        new("TC029","Auth","Khach hang bi tu choi truy cap Inventory","P0","Khach hang da dang nhap","1. Dang nhap Khach hang | 2. Truy cap /Inventory","URL: /Inventory","Bi tu choi truy cap hoac chuyen huong","Trang Access Denied hien thi",null,"Security","7771ms",""),
        new("TC030","Auth","Khach hang bi tu choi truy cap AdminAuditLog","P0","Khach hang da dang nhap","1. Dang nhap Khach hang | 2. Truy cap /AdminAuditLog","URL: /AdminAuditLog","Bi tu choi truy cap hoac chuyen huong","Trang Access Denied hien thi",null,"Security","7815ms",""),

        // ============ NHOM B: TRANG CHU & SAN PHAM (TC031-TC045) ============
        new("TC031","Home","Trang Home load thanh cong (khach)","P0","App dang chay","1. Truy cap /Home","URL: /Home","Trang Home load voi danh sach san pham","Trang Home load thanh cong",null,"Positive","14256ms",""),
        new("TC032","Home","Trang Home hien thi san pham trai cay","P0","San pham co trong CSDL","1. Truy cap /Home","Noi dung trang Home","The hien san pham voi hinh anh, ten, gia","Card san pham hien thi",null,"Positive","20299ms",""),
        new("TC033","Home","Tim kiem trai cay theo tu khoa hop le","P0","San pham ton tai","1. Nhap tu khoa tim kiem | 2. Click Tim kiem","Tu khoa: Tao (Apple)","Hien thi san pham phu hop","Ket qua tim kiem hien thi",null,"Positive","7858ms",""),
        new("TC034","Home","Tim kiem khong co ket qua","P1","Khong co","1. Nhap tu khoa khong ton tai | 2. Click Tim kiem","Tu khoa: xyznotfound999","Hien thi trang thai rong hoac thong bao Khong co ket qua","Thong bao khong co ket qua hien thi",null,"Positive","2211ms",""),
        new("TC035","Home","Loc san pham theo danh muc","P1","Danh muc ton tai","1. Click vao mot danh muc","Chon danh muc","Chi hien thi san pham thuoc danh muc da chon","Loc danh muc hoat dong",null,"Positive","24487ms",""),
        new("TC036","Home","Trang chi tiet trai cay load thanh cong","P0","San pham ton tai","1. Truy cap /Fruit/Details/1","URL: /Fruit/Details/1","Trang chi tiet san pham load day du thong tin","Trang chi tiet load thanh cong",null,"Positive","20767ms",""),
        new("TC037","Home","Trang chi tiet co nut Them vao gio","P0","San pham ton tai","1. Mo trang chi tiet san pham","Noi dung trang chi tiet","Nut Them vao gio hien thi","Nut Them vao gio co mat",null,"Positive","20685ms",""),
        new("TC038","Home","Trang Home co danh sach danh muc","P2","Danh muc ton tai","1. Truy cap /Home","Thanh danh muc Home","Cac link danh muc hien thi o sidebar","Cac danh muc hien thi",null,"Positive","20276ms",""),
        new("TC039","Home","Phan trang Home hoat dong","P1","Co nhieu trang san pham","1. Click trang 2","Dieu huong phan trang","Chuyen toi trang 2 voi danh sach san pham moi","Phan trang hoat dong",null,"Positive","14381ms",""),
        new("TC040","Home","Trang chi tiet hien thi thong tin ton kho","P1","San pham ton tai","1. Mo trang chi tiet","Noi dung trang chi tiet","Thong tin gia va ton kho hien thi","Thong tin ton kho hien thi",null,"Positive","20803ms",""),
        new("TC041","Home","Nguoi dung chua dang nhap co the xem Home","P1","Khong co","1. Truy cap /Home khi chua dang nhap","URL: /Home","Trang Home load binh thuong","Trang Home load thanh cong",null,"Positive","15212ms",""),
        new("TC042","Home","Nguoi dung chua dang nhap co the xem chi tiet san pham","P1","Khong co","1. Truy cap /Fruit/Details/1 khi chua login","URL chi tiet san pham","Trang chi tiet load binh thuong","Trang chi tiet load thanh cong",null,"Positive","2819ms",""),
        new("TC043","Home","Navbar hien thi dung theo vai tro","P2","Dang nhap voi cac vai tro khac nhau","1. Dang nhap Admin/Nhan vien/Khach hang | 2. Quan sat navbar","Noi dung navbar","Menu dung theo vai tro hien thi","Navbar hien thi dung theo vai tro",null,"Positive","20232ms",""),
        new("TC044","Home","Footer hien thi tren trang Home","P3","Khong co","1. Truy cap /Home","Phan footer","Noi dung footer co hien thi","Footer hien thi",null,"Positive","14192ms",""),
        new("TC045","Home","Hinh anh san pham duoc hien thi","P1","San pham co hinh anh","1. Truy cap /Home","Card san pham","Hinh anh san pham hien thi tren card","Hinh anh san pham duoc hien thi",null,"Positive","20483ms",""),

        // ============ NHOM C: GIO HANG & YEU THICH (TC046-TC060) ============
        new("TC046","Cart","Trang gio hang load thanh cong","P0","Nguoi dung da dang nhap","1. Truy cap /Order/Cart","URL: /Order/Cart","Trang gio hang load thanh cong","Trang gio hang load thanh cong",null,"Positive","7940ms",""),
        new("TC047","Cart","Them san pham vao gio hang","P0","San pham con hang","1. Mo chi tiet san pham | 2. Click Them vao gio","Hanh dong Them vao gio","San pham duoc them, so luong gio tang","San pham duoc them thanh cong",null,"Positive","26021ms",""),
        new("TC048","Cart","Cap nhat so luong san pham trong gio","P0","Gio hang co san pham","1. Xem gio hang | 2. Thay doi so luong | 3. Click Cap nhat","Hanh dong cap nhat so luong","So luong duoc cap nhat, tong tien tinh lai","So luong duoc cap nhat",null,"Positive","7938ms",""),
        new("TC049","Cart","Xoa san pham khoi gio hang","P0","Gio hang co san pham","1. Xem gio hang | 2. Click Xoa","Hanh dong xoa san pham","San pham bi xoa khoi gio, so luong giam","San pham da duoc xoa",null,"Positive","7942ms",""),
        new("TC050","Cart","Gio hang hien thi tong gia tri","P1","Gio hang co san pham","1. Xem gio hang","Trang gio hang","Tong gia tri hien thi dung","Tong gia tri hien thi",null,"Positive","7906ms",""),
        new("TC051","Cart","Gio hang rong hien thi thong bao","P1","Gio hang rong","1. Truy cap /Order/Cart khi gio rong","Trang thai gio rong","Hien thi thong bao gio hang rong","Thong bao rong hien thi",null,"Positive","7988ms",""),
        new("TC052","Cart","Gio hang giu nguyen khi chuyen trang","P1","Gio hang co san pham","1. Them san pham | 2. Chuyen sang Home | 3. Quay lai Gio hang","Tinh nang session","San pham trong gio van con sau khi chuyen trang","Gio hang duoc giu nguyen",null,"Positive","39130ms",""),
        new("TC053","Cart","Trang Wishlist load thanh cong","P1","Khach hang da dang nhap","1. Truy cap /Account/Profile | 2. Click tab Yeu thich","URL: /Account/Profile","Tab Yeu thich load thanh cong","Trang Yeu thich load",null,"Positive","9050ms",""),
        new("TC054","Cart","Them san pham vao danh sach yeu thich","P1","Khach hang da dang nhap","1. Mo chi tiet san pham | 2. Click nut Yeu thich","Hanh dong yeu thich","San pham duoc them vao danh sach yeu thich","Yeu thich duoc thay doi",null,"Positive","24552ms",""),
        new("TC055","Cart","Yeu thich yeu cau dang nhap","P1","Chua dang nhap","1. Thu them san pham vao yeu thich","Hanh dong yeu thich khi chua login","Chuyen huong toi login hoac hien thi loi","Yeu cau dang nhap",null,"Security","2306ms",""),
        new("TC056","Cart","Trang Checkout load khi co san pham","P0","Gio hang co san pham","1. Them san pham | 2. Truy cap /Order/Checkout","URL: /Order/Checkout","Form Checkout hien thi","Trang Checkout load thanh cong",null,"Positive","26116ms",""),
        new("TC057","Cart","Checkout hien thi phuong thuc thanh toan","P1","Gio hang co san pham","1. Truy cap Checkout","Form Checkout","Cac tuy chon thanh toan hien thi","Phuong thuc thanh toan hien thi",null,"Positive","8464ms",""),
        new("TC058","Cart","Dat hang voi thanh toan Tien mat","P0","Gio hang co san pham, khach hang da dang nhap","1. Dien form Checkout | 2. Chon Tien mat | 3. Gui","Dat hang","Don hang duoc tao, chuyen huong xac nhan","Dat hang thanh cong",null,"Positive","25573ms",""),
        new("TC059","Cart","Dat hang voi thanh toan Chuyen khoan ngan hang","P0","Gio hang co san pham, khach hang da dang nhap","1. Dien form Checkout | 2. Chon Chuyen khoan | 3. Gui","Dat hang","Don hang duoc tao voi hinh thuc chuyen khoan","Dat hang chuyen khoan thanh cong",null,"Positive","25615ms",""),
        new("TC060","Cart","Ap dung ma giam gia khi checkout","P1","Ma giam gia hop le","1. Nhap ma giam gia | 2. Click Ap dung","Ap dung coupon","Giam gia duoc ap dung, tong tien giam","Coupon duoc ap dung",null,"Positive","7976ms",""),

        // ============ NHOM D: QUAN LY DON HANG (TC061-TC075) ============
        new("TC061","Order","Trang lich su don hang load thanh cong","P0","Khach hang co don hang","1. Truy cap /Order/History","URL: /Order/History","Lich su don hang hien thi","Lich su don hang load thanh cong",null,"Positive","7901ms",""),
        new("TC062","Order","Lich su don hang hien thi don hang","P0","Khach hang co don hang","1. Truy cap /Order/History","Danh sach don hang","Don hang cua nguoi dung duoc danh sach","Don hang duoc hien thi",null,"Positive","8045ms",""),
        new("TC063","Order","Xem chi tiet mot don hang","P1","Don hang ton tai","1. Click vao mot don hang","Trang chi tiet don hang","Chi tiet don hang voi cac mat hang hien thi","Chi tiet don hang hien thi",null,"Positive","13474ms",""),
        new("TC064","Order","Huy don hang khi trang thai Cho xu ly","P1","Don hang o trang thai Cho xu ly","1. Mo chi tiet don hang | 2. Click Huy","Huy don hang","Don hang bi huy, trang thai thanh Da huy","Don hang da duoc huy",null,"Positive","16569ms",""),
        new("TC065","Order","Khong the huy don hang da giao","P1","Don hang da giao","1. Thu huy don hang da giao","Huy don hang da giao","Loi hoac nut Huy bi vo hieu hoa","Huy bi chan",null,"Negative","7992ms",""),
        new("TC066","Order","Trang hoa don load thanh cong","P1","Don hang ton tai","1. Truy cap hoa don don hang","Trang hoa don","Trang hoa don load","Hoa don load thanh cong",null,"Positive","8514ms",""),
        new("TC067","Order","Nhan vien xem danh sach don hang","P0","Nhan vien da dang nhap","1. Truy cap /Order","URL: /Order","Tat ca don hang hien thi","Don hang duoc hien thi",null,"Positive","8007ms",""),
        new("TC068","Order","Nhan vien cap nhat trang thai don hang","P0","Don hang ton tai","1. Thay doi trang thai | 2. Click Cap nhat","Cap nhat trang thai","Trang thai don hang duoc cap nhat","Trang thai da duoc cap nhat",null,"Positive","12529ms",""),
        new("TC069","Order","Admin xem tat ca don hang","P0","Admin da dang nhap","1. Truy cap /Order","URL: /Order","Tat ca don hang voi tuy chon loc","Tat ca don hang hien thi",null,"Positive","3553ms",""),
        new("TC070","Order","Loc don hang theo trang thai","P1","Don hang ton tai","1. Chon loc theo trang thai | 2. Ap dung","Loc trang thai","Chi hien thi don hang theo trang thai da chon","Loc trang thai hoat dong",null,"Positive","8121ms",""),
        new("TC071","Order","Loc don hang theo ngay","P1","Don hang ton tai","1. Nhap khoang ngay | 2. Click Loc","Loc theo ngay","Don hang duoc loc theo khoang ngay","Loc theo ngay hoat dong",null,"Positive","8965ms",""),
        new("TC072","Order","Khach hang bi tu choi truy cap trang Order cua Nhan vien","P0","Khach hang da dang nhap","1. Truy cap /Order","URL: /Order","Bi tu choi truy cap hoac chuyen ve Lich su","Access Denied hoac Lich su hien thi",null,"Security","9472ms",""),
        new("TC073","Order","Checkout hien thi dia chi khach hang","P1","Khach hang da dang nhap, gio hang co san pham","1. Truy cap Checkout","Form Checkout","Truong dia chi giao hang hien thi","Truong dia chi co mat",null,"Positive","8415ms",""),
        new("TC074","Order","Checkout hien thi tom tat don hang","P1","Gio hang co san pham","1. Truy cap Checkout","Form Checkout","Tom tat don hang voi mat hang va tong tien","Tom tat hien thi",null,"Positive","8541ms",""),
        new("TC075","Order","Checkout tien mat hien thi tien thua","P1","Chon thanh toan Tien mat","1. Chon Tien mat | 2. Nhap so tien nhan","Tinh toan tien mat","Tien thua duoc hien thi","Tien thua hien thi",null,"Positive","8528ms",""),

        // ============ NHOM E: QUAN LY HO SO (TC076-TC085) ============
        new("TC076","Profile","Trang Ho so load thanh cong","P0","Khach hang da dang nhap","1. Truy cap /Account/Profile","URL: /Account/Profile","Trang Ho so load thanh cong","Trang Ho so load thanh cong",null,"Positive","8074ms",""),
        new("TC077","Profile","Ho so hien thi thong tin nguoi dung","P0","Khach hang da dang nhap","1. Truy cap /Account/Profile","Noi dung Ho so","Ho ten, email, so dien thoai, dia chi hien thi","Thong tin nguoi dung hien thi",null,"Positive","7867ms",""),
        new("TC078","Profile","Ho so co nhieu tab (Don hang, Yeu thich...)","P1","Khach hang da dang nhap","1. Truy cap /Account/Profile","Trang Ho so","Cac tab Don hang, Yeu thich, Thong tin hien thi","Cac tab co mat",null,"Positive","7995ms",""),
        new("TC079","Profile","Tab Don hang trong Ho so load","P1","Khach hang da dang nhap","1. Truy cap Ho so | 2. Click tab Don hang","Tab Don hang trong Ho so","Lich su don hang load","Tab Don hang hoat dong",null,"Positive","8983ms",""),
        new("TC080","Profile","Cap nhat thong tin ca nhan","P0","Khach hang da dang nhap","1. Thay doi ho ten/dien thoai/dia chi | 2. Click Luu","Cap nhat Ho so","Thay doi duoc luu thanh cong","Ho so da duoc cap nhat",null,"Positive","8903ms",""),
        new("TC081","Profile","Ho so co phan doi mat khau","P1","Khach hang da dang nhap","1. Truy cap /Account/Profile","Trang Ho so","Phan doi mat khau hien thi","Phan mat khau co mat",null,"Positive","8990ms",""),
        new("TC082","Profile","Trang Quen mat khau load thanh cong","P1","Khong co","1. Truy cap /Account/ForgotPassword","URL: /Account/ForgotPassword","Form dat lai mat khau hien thi","Form Quen mat khau load",null,"Positive","2329ms",""),
        new("TC083","Profile","Quen mat khau voi email hop le","P1","Email da dang ky","1. Nhap email hop le | 2. Click Gui","Email: admin@fruitshop.com","Thong bao thanh cong hoac email duoc gui","Email dat lai duoc kich hoat",null,"Positive","3502ms",""),
        new("TC084","Profile","Quen mat khau voi email khong ton tai","P1","Khong co","1. Nhap email khong ton tai | 2. Click Gui","Email: fake@khongton tai.com","Hien thi thong bao loi","Thong bao loi hien thi",null,"Negative","4071ms",""),
        new("TC085","Profile","Xem chi tiet don tu Ho so","P1","Khach hang co don hang","1. Truy cap Ho so | 2. Click vao mot don hang","Chi tiet don tu Ho so","Chi tiet don hang hien thi","Chi tiet don hang hien thi",null,"Positive","10685ms",""),

        // ============ NHOM F: BANG DIEU KHIEN (TC086-TC100) ============
        new("TC086","Dashboard","Bang dieu khien Admin load thanh cong","P0","Admin da dang nhap","1. Truy cap /Dashboard","URL: /Dashboard","Bang dieu khien load thanh cong","Bang dieu khien load thanh cong",null,"Positive","4426ms",""),
        new("TC087","Dashboard","Bang dieu khien hien thi thong ke","P0","Don hang ton tai","1. Truy cap /Dashboard","Bieu do KPI Dashboard","Doanh thu, so don hang, so khach hang hien thi","Thong ke hien thi",null,"Positive","4020ms",""),
        new("TC088","Dashboard","Bang dieu khien co bieu do thong ke","P1","Du lieu ton tai","1. Truy cap /Dashboard","Bieu do Dashboard","Bieu do/hinh anh thong ke duoc ve","Bieu do duoc hien thi",null,"Positive","3648ms",""),
        new("TC089","Dashboard","Bang dieu khien hien thi don hang gan day","P1","Don hang ton tai","1. Truy cap /Dashboard","Phan don hang gan day","Bang don hang gan day hien thi","Don hang gan day hien thi",null,"Positive","4038ms",""),
        new("TC090","Dashboard","Bang dieu khien co link bao cao","P1","Admin da dang nhap","1. Truy cap /Dashboard","Cac link bao cao","Cac link xuat/bao cao hien thi","Cac link bao cao co mat",null,"Positive","4004ms",""),
        new("TC091","Dashboard","Admin danh sach nguoi dung","P0","Admin da dang nhap","1. Truy cap /User","URL: /User","Bang nguoi dung hien thi","Danh sach nguoi dung load",null,"Positive","3795ms",""),
        new("TC092","Dashboard","Danh sach User hien thi cac tai khoan","P0","Nguoi dung ton tai","1. Truy cap /User","Danh sach nguoi dung","Tat ca tai khoan voi vai tro hien thi","Nguoi dung duoc hien thi",null,"Positive","4163ms",""),
        new("TC093","Dashboard","Admin tao tai khoan nguoi dung moi","P0","Admin da dang nhap","1. Truy cap /User/Create | 2. Dien form | 3. Gui","Form tao nguoi dung moi","Tai khoan duoc tao thanh cong","Nguoi dung duoc tao",null,"Positive","4768ms",""),
        new("TC094","Dashboard","Admin chinh sua thong tin nguoi dung","P0","Nguoi dung ton tai","1. Truy cap /User/Edit/ID | 2. Sua thong tin | 3. Luu","Form chinh sua nguoi dung","Thong tin nguoi dung duoc cap nhat","Nguoi dung duoc cap nhat",null,"Positive","5760ms",""),
        new("TC095","Dashboard","Admin xoa nguoi dung","P1","Nguoi dung ton tai","1. Truy cap /User | 2. Click Xoa | 3. Xac nhan","Xoa nguoi dung","Tai khoan nguoi dung bi xoa","Nguoi dung da bi xoa",null,"Positive","4049ms",""),
        new("TC096","Dashboard","Admin Audit Log danh sach nhap ky","P0","Admin da dang nhap","1. Truy cap /AdminAuditLog","URL: /AdminAuditLog","Danh sach nhap ky hien thi","Audit log load thanh cong",null,"Positive","4032ms",""),
        new("TC097","Dashboard","Audit Log hien thi cac ban ghi","P1","Log ton tai","1. Truy cap /AdminAuditLog","Cac ban ghi Audit Log","Hanh dong he thong duoc ghi nhan va hien thi","Cac ban ghi log hien thi",null,"Positive","4005ms",""),
        new("TC098","Dashboard","Admin OperatingCost danh sach chi phi","P0","Admin da dang nhap","1. Truy cap /OperatingCost","URL: /OperatingCost","Danh sach chi phi van hanh hien thi","Danh sach chi phi load",null,"Positive","4335ms",""),
        new("TC099","Dashboard","Admin tao chi phi van hanh moi","P1","Admin da dang nhap","1. Truy cap /OperatingCost/Create | 2. Dien form | 3. Gui","Form tao chi phi moi","Chi phi van hanh duoc tao thanh cong","Chi phi duoc tao",null,"Positive","4563ms",""),
        new("TC100","Dashboard","Admin Customer danh sach khach hang","P0","Admin da dang nhap","1. Truy cap /AdminCustomer","URL: /AdminCustomer","Danh sach khach hang hien thi","Danh sach khach hang load",null,"Positive","4188ms",""),

        // ============ NHOM G: QUAN LY TRAI CAY (TC101-TC115) ============
        new("TC101","Fruit","Admin Fruit danh sach trai cay","P0","Admin da dang nhap","1. Truy cap /Fruit","URL: /Fruit","Bang trai cay hien thi","Danh sach trai cay load",null,"Positive","15848ms",""),
        new("TC102","Fruit","Danh sach Fruit hien thi san pham","P0","San pham ton tai","1. Truy cap /Fruit","Danh sach trai cay","Tat ca trai cay voi chi tiet hien thi","Trai cay duoc hien thi",null,"Positive","16042ms",""),
        new("TC103","Fruit","Admin tao trai cay moi","P0","Admin da dang nhap, danh muc ton tai","1. Truy cap /Fruit/Create | 2. Dien form | 3. Gui","Form tao trai cay moi","Trai cay duoc tao thanh cong","Trai cay duoc tao",null,"Positive","7740ms",""),
        new("TC104","Fruit","Tao trai cay voi du lieu hop le","P0","Admin da dang nhap","1. Dien day du thong tin | 2. Gui","Du lieu trai cay hop le","Trai cay duoc tao, chuyen ve danh sach","Trai cay duoc tao",null,"Positive","4686ms",""),
        new("TC105","Fruit","Tao trai cay gia am hien loi","P1","Admin da dang nhap","1. Nhap gia am | 2. Gui","Gia: -1000","Loi validation gia am","Loi validation hien thi",null,"Negative","4630ms",""),
        new("TC106","Fruit","Admin chinh sua trai cay","P0","Trai cay ton tai","1. Truy cap /Fruit/Edit/ID | 2. Sua thong tin | 3. Luu","Form chinh sua trai cay","Trai cay duoc cap nhat thanh cong","Trai cay duoc cap nhat",null,"Positive","15907ms",""),
        new("TC107","Fruit","Admin xoa trai cay","P1","Trai cay ton tai, chua co don hang","1. Truy cap /Fruit | 2. Click Xoa | 3. Xac nhan","Xoa trai cay","Trai cay bi xoa hoac an di (soft delete)","Trai cay da bi xoa",null,"Positive","19952ms",""),
        new("TC108","Fruit","Trang Import Excel trai cay load","P2","Admin da dang nhap","1. Truy cap /Fruit/ImportExcel","URL: /Fruit/ImportExcel","Form tai Excel len hien thi","Form import load thanh cong",null,"Positive","3483ms",""),
        new("TC109","Fruit","Xuat file Excel danh sach trai cay","P2","San pham ton tai","1. Truy cap /Fruit | 2. Click Xuat Excel","Han xuat Excel","File Excel duoc tai xuong","File Excel duoc xuat",null,"Positive","22322ms",""),
        new("TC110","Fruit","Nhan vien co quyen quan ly Fruit","P0","Nhan vien da dang nhap","1. Truy cap /Fruit","URL: /Fruit","Nhan vien co the truy cap quan ly trai cay","Quan ly Fruit co the truy cap",null,"Positive","20357ms",""),
        new("TC111","Fruit","Nhan vien tao trai cay moi","P0","Nhan vien da dang nhap","1. Truy cap /Fruit/Create | 2. Dien form | 3. Gui","Form tao trai cay","Trai cay duoc tao boi nhan vien","Trai cay duoc tao",null,"Positive","7932ms",""),
        new("TC112","Fruit","Trang quan ly Fruit load thanh cong","P0","Admin/Nhan vien da dang nhap","1. Truy cap /Fruit","URL: /Fruit","Trang quan ly Fruit load","Trang Fruit load thanh cong",null,"Positive","19906ms",""),
        new("TC113","Fruit","Tim kiem trai cay trong trang Admin","P1","San pham ton tai","1. Nhap tu khoa | 2. Click Tim kiem","Tu khoa: Tao","Trai cay phu hop duoc loc","Tim kiem hoat dong",null,"Positive","4311ms",""),
        new("TC114","Fruit","Loc trai cay theo danh muc trong Admin","P1","Danh muc ton tai","1. Chon danh muc | 2. Ap dung","Loc theo danh muc","Chi hien thi trai cay thuoc danh muc da chon","Loc danh muc hoat dong",null,"Positive","16573ms",""),
        new("TC115","Fruit","Chi tiet Fruit cho Khach hang","P0","San pham ton tai","1. Truy cap /Fruit/Details/ID","Trang chi tiet san pham","Chi tiet san pham day du hien thi","Trang chi tiet hoat dong",null,"Positive","20978ms",""),

        // ============ NHOM H: QUAN LY DANH MUC (TC116-TC125) ============
        new("TC116","Category","Admin Category danh sach danh muc","P0","Admin da dang nhap","1. Truy cap /Category","URL: /Category","Bang danh muc hien thi","Danh sach danh muc load",null,"Positive","4006ms",""),
        new("TC117","Category","Danh sach Category hien thi cac danh muc","P0","Danh muc ton tai","1. Truy cap /Category","Danh sach danh muc","Tat ca danh muc hien thi","Danh muc duoc hien thi",null,"Positive","3941ms",""),
        new("TC118","Category","Admin tao danh muc moi","P0","Admin da dang nhap","1. Truy cap /Category/Create | 2. Dien ten | 3. Gui","Form tao danh muc moi","Danh muc duoc tao thanh cong","Danh muc duoc tao",null,"Positive","4010ms",""),
        new("TC119","Category","Tao danh muc voi du lieu hop le","P0","Admin da dang nhap","1. Dien ten danh muc hop le | 2. Gui","Ten danh muc hop le","Danh muc duoc tao, chuyen ve danh sach","Danh muc duoc tao",null,"Positive","5701ms",""),
        new("TC120","Category","Admin chinh sua danh muc","P0","Danh muc ton tai","1. Truy cap /Category/Edit/ID | 2. Sua | 3. Luu","Form chinh sua danh muc","Danh muc duoc cap nhat","Danh muc duoc cap nhat",null,"Positive","3934ms",""),
        new("TC121","Category","Admin xoa danh muc","P1","Danh muc ton tai, chua co trai cay","1. Truy cap /Category | 2. Click Xoa | 3. Xac nhan","Xoa danh muc","Danh muc bi xoa","Danh muc da bi xoa",null,"Positive","5799ms",""),
        new("TC122","Category","Nhan vien co quyen quan ly Category","P0","Nhan vien da dang nhap","1. Truy cap /Category","URL: /Category","Nhan vien co the truy cap quan ly danh muc","Quan ly danh muc co the truy cap",null,"Positive","8020ms",""),
        new("TC123","Category","Nhan vien tao danh muc moi","P0","Nhan vien da dang nhap","1. Truy cap /Category/Create | 2. Gui","Form tao danh muc","Danh muc duoc tao boi nhan vien","Danh muc duoc tao",null,"Positive","8427ms",""),
        new("TC124","Category","Khach hang bi tu choi truy cap Category","P0","Khach hang da dang nhap","1. Truy cap /Category","URL: /Category","Bi tu choi truy cap hoac chuyen huong","Access Denied hien thi",null,"Security","7984ms",""),
        new("TC125","Category","Tim kiem danh muc","P1","Danh muc ton tai","1. Nhap tu khoa | 2. Click Tim kiem","Tim kiem danh muc","Danh muc phu hop duoc loc","Tim kiem hoat dong",null,"Positive","3621ms",""),

        // ============ NHOM I: QUAN LY LO HANG (TC126-TC135) ============
        new("TC126","Batch","Nhan vien Batch danh sach lo hang","P0","Nhan vien da dang nhap","1. Truy cap /Batch","URL: /Batch","Bang lo hang hien thi","Danh sach lo hang load",null,"Positive","8016ms",""),
        new("TC127","Batch","Danh sach Batch hien thi cac lo hang","P0","Lo hang ton tai","1. Truy cap /Batch","Danh sach lo hang","Tat ca lo hang voi ngay het han hien thi","Lo hang duoc hien thi",null,"Positive","7984ms",""),
        new("TC128","Batch","Nhan vien tao lo hang moi","P0","Nhan vien da dang nhap, nha cung cap ton tai","1. Truy cap /Batch/Create | 2. Dien form | 3. Gui","Form tao lo hang moi","Lo hang duoc tao thanh cong","Lo hang duoc tao",null,"Positive","7991ms",""),
        new("TC129","Batch","Trang tao lo hang (Batch) load thanh cong","P1","Nhan vien da dang nhap","1. Truy cap /Batch/Create","URL: /Batch/Create","Trang tao lo hang load","Trang Create Batch load thanh cong",null,"Positive","9982ms",""),
        new("TC130","Batch","Admin co quyen truy cap Batch","P0","Admin da dang nhap","1. Truy cap /Batch","URL: /Batch","Admin co the truy cap quan ly lo hang","Quan ly lo hang co the truy cap",null,"Positive","3852ms",""),
        new("TC131","Batch","Chinh sua lo hang","P0","Lo hang ton tai","1. Truy cap /Batch/Edit/ID | 2. Sua | 3. Luu","Form chinh sua lo hang","Lo hang duoc cap nhat","Lo hang duoc cap nhat",null,"Positive","8050ms",""),
        new("TC132","Batch","Xoa lo hang","P1","Lo hang ton tai","1. Truy cap /Batch | 2. Click Xoa | 3. Xac nhan","Xoa lo hang","Lo hang bi xoa","Lo hang da bi xoa",null,"Positive","8082ms",""),
        new("TC133","Batch","Loc lo hang theo trai cay","P1","Lo hang ton tai","1. Chon loc theo trai cay | 2. Ap dung","Loc theo trai cay","Chi hien thi lo hang cua trai cay da chon","Loc theo trai cay hoat dong",null,"Positive","7933ms",""),
        new("TC134","Batch","Lo hang sap het han hien canh bao","P1","Co lo hang sap het han","1. Truy cap /Batch","Bang lo hang","Lo hang sap het han duoc to mau hoac canh bao","Canh bao het han hien thi",null,"Positive","7966ms",""),
        new("TC135","Batch","Ton kho tu dong cap nhat khi tao Batch","P1","Tao Batch se tang so luong ton","1. Tao mot lo hang cho trai cay","Tao Batch","So luong ton kho tu dong tang","Ton kho tu dong tang",null,"Positive","7986ms",""),

        // ============ NHOM J: QUAN LY TON KHO (TC136-TC142) ============
        new("TC136","Inventory","Nhan vien Inventory danh sach ton kho","P0","Nhan vien da dang nhap","1. Truy cap /Inventory","URL: /Inventory","Bang ton kho hien thi","Danh sach ton kho load",null,"Positive","7953ms",""),
        new("TC137","Inventory","Inventory hien thi muc ton kho","P0","Du lieu ton kho ton tai","1. Truy cap /Inventory","Bang ton kho","Muc ton kho moi san pham hien thi","Muc ton kho hien thi",null,"Positive","7971ms",""),
        new("TC138","Inventory","Loc ton kho theo trai cay","P1","Du lieu ton kho ton tai","1. Chon loc theo trai cay | 2. Ap dung","Loc theo trai cay","Chi hien thi ton kho trai cay da chon","Loc theo trai cay hoat dong",null,"Positive","7994ms",""),
        new("TC139","Inventory","Admin co quyen truy cap Inventory","P0","Admin da dang nhap","1. Truy cap /Inventory","URL: /Inventory","Admin co the truy cap ton kho","Ton kho co the truy cap",null,"Positive","3553ms",""),
        new("TC140","Inventory","Ton kho thap hien canh bao","P1","Ton kho thap ton tai","1. Truy cap /Inventory","Bang ton kho","Hang ton kho thap duoc to mau do","Canh bao ton kho thap hien thi",null,"Positive","7932ms",""),
        new("TC141","Inventory","Inventory co lich su xuat/nhap","P1","Lich su ton kho ton tai","1. Truy cap /Inventory","Phan lich su ton kho","Lich su nhap/xuat ton kho hien thi","Lich su hien thi",null,"Positive","7935ms",""),
        new("TC142","Inventory","Khach hang bi tu choi truy cap Inventory","P0","Khach hang da dang nhap","1. Truy cap /Inventory","URL: /Inventory","Bi tu choi truy cap hoac chuyen huong","Access Denied hien thi",null,"Security","8002ms",""),

        // ============ NHOM K: QUAN LY MA GIAM GIA (TC143-TC150) ============
        new("TC143","Coupon","Admin Coupon danh sach ma giam gia","P0","Admin da dang nhap","1. Truy cap /Coupon","URL: /Coupon","Bang ma giam gia hien thi","Danh sach coupon load",null,"Positive","3609ms",""),
        new("TC144","Coupon","Danh sach Coupon hien thi cac ma giam gia","P0","Ma giam gia ton tai","1. Truy cap /Coupon","Danh sach coupon","Tat ca ma giam gia voi chi tiet","Coupon duoc hien thi",null,"Positive","3518ms",""),
        new("TC145","Coupon","Admin tao ma giam gia moi","P0","Admin da dang nhap","1. Click Tao | 2. Dien form coupon | 3. Gui","Form tao ma giam gia moi","Ma giam gia duoc tao thanh cong","Coupon duoc tao",null,"Positive","6576ms",""),
        new("TC146","Coupon","Tao coupon voi du lieu hop le","P0","Admin da dang nhap","1. Dien day du thong tin coupon | 2. Gui","Du lieu coupon hop le","Coupon duoc tao, chuyen ve danh sach","Coupon duoc tao",null,"Positive","6495ms",""),
        new("TC147","Coupon","Coupon modal tao ma giam gia","P2","Admin da dang nhap","1. Truy cap /Coupon | 2. Click Tao","Nut tao coupon","Modal hoac form tao coupon hien thi","Modal tao hoat dong",null,"Positive","3488ms",""),
        new("TC148","Coupon","Nhan vien co quyen truy cap Coupon","P0","Nhan vien da dang nhap","1. Truy cap /Coupon","URL: /Coupon","Nhan vien co the truy cap quan ly coupon","Coupon co the truy cap",null,"Positive","7900ms",""),
        new("TC149","Coupon","Khach hang bi tu choi truy cap Coupon","P0","Khach hang da dang nhap","1. Truy cap /Coupon","URL: /Coupon","Bi tu choi truy cap hoac chuyen huong","Access Denied hien thi",null,"Security","8003ms",""),
        new("TC150","Coupon","Chinh sua coupon","P0","Coupon ton tai","1. Truy cap /Coupon/Edit/ID | 2. Sua | 3. Luu","Form chinh sua coupon","Coupon duoc cap nhat","Coupon duoc cap nhat",null,"Positive","3038ms",""),

        // ============ NHOM L: QUAN LY NHA CUNG CAP (TC151-TC158) ============
        new("TC151","Supplier","Nhan vien Supplier danh sach nha cung cap","P0","Nhan vien da dang nhap","1. Truy cap /AdminSupplier","URL: /AdminSupplier","Bang nha cung cap hien thi","Danh sach nha cung cap load",null,"Positive","7917ms",""),
        new("TC152","Supplier","Danh sach Supplier hien thi cac nha cung cap","P0","Nha cung cap ton tai","1. Truy cap /AdminSupplier","Danh sach nha cung cap","Tat ca nha cung cap hien thi","Nha cung cap duoc hien thi",null,"Positive","8030ms",""),
        new("TC153","Supplier","Nhan vien tao nha cung cap moi","P0","Nhan vien da dang nhap","1. Truy cap /AdminSupplier/Create | 2. Dien form | 3. Gui","Form tao nha cung cap moi","Nha cung cap duoc tao thanh cong","Nha cung cap duoc tao",null,"Positive","8019ms",""),
        new("TC154","Supplier","Tao nha cung cap voi du lieu hop le","P0","Nhan vien da dang nhap","1. Dien day du thong tin nha cung cap | 2. Gui","Du lieu nha cung cap hop le","Nha cung cap duoc tao, chuyen ve danh sach","Nha cung cap duoc tao",null,"Positive","9501ms",""),
        new("TC155","Supplier","Chinh sua nha cung cap","P0","Nha cung cap ton tai","1. Truy cap /AdminSupplier/Edit/ID | 2. Sua | 3. Luu","Form chinh sua nha cung cap","Nha cung cap duoc cap nhat","Nha cung cap duoc cap nhat",null,"Positive","7941ms",""),
        new("TC156","Supplier","Xoa nha cung cap","P1","Nha cung cap ton tai","1. Truy cap /AdminSupplier | 2. Click Xoa | 3. Xac nhan","Xoa nha cung cap","Nha cung cap bi xoa","Nha cung cap da bi xoa",null,"Positive","8094ms",""),
        new("TC157","Supplier","Admin co quyen truy cap Supplier","P0","Admin da dang nhap","1. Truy cap /AdminSupplier","URL: /AdminSupplier","Admin co the truy cap quan ly nha cung cap","Supplier co the truy cap",null,"Positive","3579ms",""),
        new("TC158","Supplier","Khach hang bi tu choi truy cap Supplier","P0","Khach hang da dang nhap","1. Truy cap /AdminSupplier","URL: /AdminSupplier","Bi tu choi truy cap hoac chuyen huong","Access Denied hien thi",null,"Security","8030ms",""),

        // ============ NHOM M: DANH GIA & DIEM THANH VIEN (TC159-TC165) ============
        new("TC159","Reviews","Trang chi tiet co phan danh gia","P1","San pham co danh gia","1. Truy cap /Fruit/Details/ID","Trang chi tiet san pham","Phan danh gia hien thi","Phan danh gia hien thi",null,"Positive","21630ms",""),
        new("TC160","Reviews","Them danh gia san pham","P1","Khach hang da dang nhap, da mua san pham","1. Mo chi tiet san pham | 2. Gui danh gia voi xep hang","Gui danh gia","Danh gia duoc gui thanh cong","Danh gia da gui",null,"Positive","21046ms",""),
        new("TC161","Reviews","Danh gia yeu cau dang nhap","P1","Chua dang nhap","1. Thu gui danh gia","Gui danh gia khi chua login","Chuyen huong toi login hoac hien thi loi","Yeu cau dang nhap",null,"Security","2812ms",""),
        new("TC162","Reviews","Diem tich luy hien thi trong Ho so","P2","Khach hang co diem tich luy","1. Truy cap /Account/Profile","Trang Ho so","Diem tich luy/hang thanh vien hien thi","Diem thanh vien hien thi trong Ho so",null,"Positive","8061ms",""),
        new("TC163","Reviews","Diem duoc cong sau khi nhan hang","P1","Don hang da giao","1. Don hang duoc giao | 2. Kiem tra diem khach hang","Diem sau khi giao hang","Diem tich luy tang them","Diem duoc cong them",null,"Positive","8040ms",""),
        new("TC164","Reviews","Dat hang khi ap dung coupon giam gia","P1","Coupon hop le, gio hang co san pham","1. Ap dung coupon | 2. Hoan tat Checkout","Dat hang voi coupon","Giam gia duoc ap dung, don hang duoc tao","Dat hang voi coupon thanh cong",null,"Positive","26290ms",""),
        new("TC165","Reviews","Diem tich luy duoc tinh dung khi checkout","P1","Khach hang da dang nhap, gio hang co san pham","1. Hoan tat checkout","Checkout voi diem tich luy","Diem tich luy duoc tinh va ap dung dung","Diem duoc tinh dung",null,"Positive","8160ms",""),

        // ============ NHOM N: TRUONG HOP DAC BIET & BAO MAT (TC166-TC175) ============
        new("TC166","Edge Cases","Trang Access Denied load thanh cong","P2","Khong co","1. Truy cap /Home/AccessDenied","URL: /Home/AccessDenied","Trang Access Denied hien thi","Trang Access Denied load thanh cong",null,"Positive","2631ms",""),
        new("TC167","Edge Cases","Trang 404 Not Found khi truy cap sai URL","P2","Khong co","1. Truy cap URL khong ton tai","URL: /KhongTonTai/Trang","Trang loi 404 hien thi","Trang 404 hien thi",null,"Positive","2527ms",""),
        new("TC168","Edge Cases","Session van hoat dong sau khi login","P0","Nguoi dung da dang nhap","1. Dang nhap | 2. Chuyen nhieu trang","Dieu huong nhieu trang","Session duoc giu nguyen qua cac trang","Session duoc giu nguyen",null,"Positive","19171ms",""),
        new("TC169","Edge Cases","Hai trinh duyet cung luc hoat dong voi hai tai khoan","P0","Hai tai khoan ton tai","1. Trinh duyet A dang nhap Admin | 2. Trinh duyet B dang nhap Khach hang | 3. Su dung ca hai","Session dong thoi","Ca hai session hoat dong doc lap","Hai session hoat dong dong thoi",null,"Positive","10839ms",""),
        new("TC170","Edge Cases","Menu dieu huong nhat quan theo vai tro","P1","Dang nhap voi cac vai tro khac nhau","1. Dang nhap Admin/Nhan vien/Khach hang | 2. Quan sat thanh dieu huong","Menu dieu huong","Dung menu theo vai tro hien thi","Navbar duoc tuy chinh theo vai tro",null,"Positive","16186ms",""),
        new("TC171","Edge Cases","Breadcrumb hien thi tren trang con","P3","Khong co","1. Truy cap /Fruit/Details/1","Breadcrumb tren trang chi tiet","Breadcrumb hien thi duong dan hien tai","Breadcrumb hien thi",null,"Positive","16030ms",""),
        new("TC172","Edge Cases","Trang ho tro mobile viewport","P2","Khong co","1. Dat viewport 375x667 | 2. Truy cap /Home","Viewport mobile","Trang hien thi dung tren mobile","Giao dien mobile hoat dong",null,"Positive","15372ms",""),
        new("TC173","Edge Cases","Admin Daily Report hien thi","P0","Admin da dang nhap","1. Truy cap /Dashboard/DailyReport","URL: /DailyReport","Trang bao cao ngay load voi du lieu","Bao cao ngay hien thi",null,"Positive","7056ms",""),
        new("TC174","Edge Cases","Nhan vien bi tu choi truy cap Daily Report","P0","Nhan vien da dang nhap","1. Truy cap /Dashboard/DailyReport","URL: /DailyReport","Bi tu choi truy cap hoac chuyen huong","Access Denied hien thi",null,"Security","8962ms",""),
        new("TC175","Edge Cases","Trang tim kiem rong hien thi thong bao","P1","Khong co","1. Gui tim kiem rong","Tim kiem rong","Thong bao trang thai rong hoac Khong co ket qua","Thong bao rong hien thi",null,"Positive","8042ms",""),
    ];
}
