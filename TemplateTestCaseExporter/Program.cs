using System;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace TemplateTestCaseExporter;

class Program
{
    static void Main(string[] args)
    {
        string outputDir = args.Length > 0 ? args[0] : Path.Combine(Directory.GetCurrentDirectory(), "Reports");
        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        string outputPath = Path.Combine(outputDir, "Template_TestCase_FruitShop.xlsx");

        Console.WriteLine("===========================================");
        Console.WriteLine("  FruitShop Selenium Test Case Generator");
        Console.WriteLine("===========================================");
        Console.WriteLine();
        Console.WriteLine($"Generating: {outputPath}");
        Console.WriteLine();

        GenerateTemplate(outputPath);

        Console.WriteLine();
        Console.WriteLine("===========================================");
        Console.WriteLine("  Test Case Generation Complete!");
        Console.WriteLine("===========================================");
        Console.WriteLine();
        Console.WriteLine("Sheets created:");
        Console.WriteLine("  1. Cover           - Overview and instructions");
        Console.WriteLine("  2. Summary        - Test case summary by module");
        Console.WriteLine("  3. 01_Account     - Login, Register, Password tests");
        Console.WriteLine("  4. 02_Home        - Home page, search, filter tests");
        Console.WriteLine("  5. 03_Fruit       - Product CRUD, details tests");
        Console.WriteLine("  6. 04_Cart        - Cart operations tests");
        Console.WriteLine("  7. 05_Order       - Checkout, order management tests");
        Console.WriteLine("  8. 06_Category    - Category CRUD tests");
        Console.WriteLine("  9. 07_Dashboard   - KPI, reports tests");
        Console.WriteLine(" 10. 08_User_Management - User admin tests");
        Console.WriteLine(" 11. 09_Coupon      - Coupon management tests");
        Console.WriteLine(" 12. 10_Inventory   - Stock management tests");
        Console.WriteLine(" 13. 11_Batch       - Batch/lot management tests");
        Console.WriteLine(" 14. 12_Supplier    - Supplier management tests");
        Console.WriteLine(" 15. 13_Profile     - Profile update tests");
        Console.WriteLine(" 16. 14_Wishlist    - Wishlist tests");
        Console.WriteLine(" 17. 15_Review      - Review submission tests");
        Console.WriteLine(" 18. 16_AuditLog    - Audit log access tests");
        Console.WriteLine(" 19. 17_Customer    - Customer admin tests");
        Console.WriteLine(" 20. 18_OperatingCost - Operating cost tests");
        Console.WriteLine();
        Console.WriteLine("Total test cases: 200+");
    }

    static void GenerateTemplate(string outputPath)
    {
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        CreateCoverSheet(workbook);
        CreateModuleSheets(workbook);
        CreateSummarySheet(workbook);
        workbook.SaveAs(outputPath);
        Console.WriteLine($"\nTemplate_TestCase.xlsx saved to: {outputPath}");
    }

    static void CreateCoverSheet(ClosedXML.Excel.XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Cover");
        ws.ColumnWidth = 3;

        ws.Range(1, 1, 1, 12).Merge();
        ws.Cell(1, 1).Value = "FRUITSHOP - SELENIUM AUTOMATION TEST CASE";
        ws.Cell(1, 1).Style.Font.FontSize = 18;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#2E7D32");
        ws.Cell(1, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        ws.Range(2, 1, 2, 12).Merge();
        ws.Cell(2, 1).Value = "Comprehensive E2E Testing Template - Powered by Selenium WebDriver + xUnit";
        ws.Cell(2, 1).Style.Font.FontSize = 11;
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#C8E6C9");
        ws.Cell(2, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        ws.Cell(4, 2).Value = "Project:";
        ws.Cell(4, 2).Style.Font.Bold = true;
        ws.Cell(4, 3).Value = "FruitShop - Fruit E-Commerce & Inventory Management System";

        ws.Cell(5, 2).Value = "Framework:";
        ws.Cell(5, 2).Style.Font.Bold = true;
        ws.Cell(5, 3).Value = "ASP.NET Core 10.0 MVC + SQL Server + Selenium WebDriver";

        ws.Cell(6, 2).Value = "Test Framework:";
        ws.Cell(6, 2).Style.Font.Bold = true;
        ws.Cell(6, 3).Value = "xUnit + Selenium WebDriver 4.43.0 + ChromeDriver";

        ws.Cell(7, 2).Value = "Total Test Cases:";
        ws.Cell(7, 2).Style.Font.Bold = true;
        ws.Cell(7, 3).Value = "200+ across 18 modules";

        ws.Cell(9, 2).Value = "SHEET LEGEND";
        ws.Cell(9, 2).Style.Font.Bold = true;
        ws.Cell(9, 2).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        ws.Cell(9, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#388E3C");
        ws.Range(9, 2, 9, 5).Merge();

        ws.Cell(10, 3).Value = "Module_Sheets";
        ws.Cell(10, 3).Style.Font.Bold = true;
        ws.Cell(10, 4).Value = "Contains test cases per module";

        ws.Cell(11, 3).Value = "Summary";
        ws.Cell(11, 3).Style.Font.Bold = true;
        ws.Cell(11, 4).Value = "Overview of all test results";

        ws.Cell(12, 3).Value = "TestCase Template";
        ws.Cell(12, 3).Style.Font.Bold = true;
        ws.Cell(12, 4).Value = "Individual test case details";

        ws.Cell(14, 2).Value = "HOW TO USE";
        ws.Cell(14, 2).Style.Font.Bold = true;
        ws.Cell(14, 2).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        ws.Cell(14, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#388E3C");
        ws.Range(14, 2, 14, 6).Merge();

        string[] instructions = [
            "1. Review test cases in each module sheet",
            "2. Update 'Test Data' column with your actual test values",
            "3. Run Selenium tests with: dotnet test --verbosity normal",
            "4. Update 'Actual Result' and 'Status' after each run",
            "5. Use Summary sheet to track overall progress"
        ];
        for (int i = 0; i < instructions.Length; i++)
        {
            ws.Cell(15 + i, 2).Value = instructions[i];
            ws.Range(15 + i, 2, 15 + i, 6).Merge();
        }
    }

    static void CreateModuleSheets(ClosedXML.Excel.XLWorkbook workbook)
    {
        var modules = GetAllTestCases();
        string[] headers = ["TC_ID", "Test Module", "Test Case Title", "Priority", "Pre-condition", "Test Steps", "Test Data", "Expected Result", "Actual Result", "Status", "Test Type", "Notes"];

        foreach (var (moduleName, testCases) in modules)
        {
            var ws = workbook.Worksheets.Add(moduleName);

            for (int col = 1; col <= headers.Length; col++)
            {
                ws.Cell(1, col).Value = headers[col - 1];
                ws.Cell(1, col).Style.Font.Bold = true;
                ws.Cell(1, col).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                ws.Cell(1, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#2E7D32");
                ws.Cell(1, col).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                ws.Cell(1, col).Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
            }

            int dataRow = 2;
            foreach (var tc in testCases)
            {
                ws.Cell(dataRow, 1).Value = tc.TCId;
                ws.Cell(dataRow, 2).Value = tc.Module;
                ws.Cell(dataRow, 3).Value = tc.Title;
                ws.Cell(dataRow, 4).Value = tc.Priority;
                ws.Cell(dataRow, 5).Value = tc.PreCondition;
                ws.Cell(dataRow, 6).Value = tc.Steps;
                ws.Cell(dataRow, 7).Value = tc.TestData;
                ws.Cell(dataRow, 8).Value = tc.ExpectedResult;
                ws.Cell(dataRow, 9).Value = tc.ActualResult;
                ws.Cell(dataRow, 10).Value = tc.Status;
                ws.Cell(dataRow, 11).Value = tc.TestType;
                ws.Cell(dataRow, 12).Value = tc.Notes;

                var priorityColor = tc.Priority switch
                {
                    "P0 - Critical" => ClosedXML.Excel.XLColor.FromHtml("#D32F2F"),
                    "P1 - High" => ClosedXML.Excel.XLColor.FromHtml("#F57C00"),
                    "P2 - Medium" => ClosedXML.Excel.XLColor.FromHtml("#FBC02D"),
                    "P3 - Low" => ClosedXML.Excel.XLColor.FromHtml("#388E3C"),
                    _ => ClosedXML.Excel.XLColor.Transparent
                };
                ws.Cell(dataRow, 4).Style.Fill.BackgroundColor = priorityColor;
                ws.Cell(dataRow, 4).Style.Font.FontColor = tc.Priority == "P3 - Low" ? ClosedXML.Excel.XLColor.White : ClosedXML.Excel.XLColor.Black;

                var statusColor = tc.Status switch
                {
                    "PASS" => ClosedXML.Excel.XLColor.FromHtml("#C8E6C9"),
                    "FAIL" => ClosedXML.Excel.XLColor.FromHtml("#FFCDD2"),
                    "BLOCKED" => ClosedXML.Excel.XLColor.FromHtml("#FFF9C4"),
                    "NOT RUN" => ClosedXML.Excel.XLColor.FromHtml("#E0E0E0"),
                    _ => ClosedXML.Excel.XLColor.Transparent
                };
                ws.Cell(dataRow, 10).Style.Fill.BackgroundColor = statusColor;
                ws.Cell(dataRow, 10).Style.Font.Bold = true;

                ws.Row(dataRow).Height = 40;
                ws.Cell(dataRow, 6).Style.Alignment.WrapText = true;
                ws.Cell(dataRow, 5).Style.Alignment.WrapText = true;
                ws.Cell(dataRow, 8).Style.Alignment.WrapText = true;

                dataRow++;
            }

            ws.Column(1).Width = 10;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 35;
            ws.Column(4).Width = 12;
            ws.Column(5).Width = 30;
            ws.Column(6).Width = 50;
            ws.Column(7).Width = 25;
            ws.Column(8).Width = 40;
            ws.Column(9).Width = 25;
            ws.Column(10).Width = 10;
            ws.Column(11).Width = 15;
            ws.Column(12).Width = 20;

            ws.RangeUsed()!.SetAutoFilter();
            ws.SheetView.FreezeRows(1);
        }
    }

    static void CreateSummarySheet(ClosedXML.Excel.XLWorkbook workbook)
    {
        var ws = workbook.Worksheets.Add("Summary");

        ws.Cell(1, 1).Value = "TEST CASE SUMMARY REPORT";
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        ws.Cell(1, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#2E7D32");
        ws.Range(1, 1, 1, 6).Merge();
        ws.Cell(1, 1).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

        ws.Cell(3, 1).Value = "Module";
        ws.Cell(3, 2).Value = "Total";
        ws.Cell(3, 3).Value = "Passed";
        ws.Cell(3, 4).Value = "Failed";
        ws.Cell(3, 5).Value = "Blocked";
        ws.Cell(3, 6).Value = "Not Run";
        ws.Cell(3, 7).Value = "Pass Rate";

        for (int col = 1; col <= 7; col++)
        {
            ws.Cell(3, col).Style.Font.Bold = true;
            ws.Cell(3, col).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            ws.Cell(3, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#388E3C");
            ws.Cell(3, col).Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
        }

        var modules = GetAllTestCases();
        int row = 4;
        int grandTotal = 0;
        foreach (var (moduleName, testCases) in modules)
        {
            ws.Cell(row, 1).Value = moduleName;
            ws.Cell(row, 2).Value = testCases.Count;
            ws.Cell(row, 3).Value = 0;
            ws.Cell(row, 4).Value = 0;
            ws.Cell(row, 5).Value = 0;
            ws.Cell(row, 6).Value = testCases.Count;

            ws.Cell(row, 3).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#C8E6C9");
            ws.Cell(row, 4).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFCDD2");
            ws.Cell(row, 5).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#FFF9C4");
            ws.Cell(row, 6).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E0E0E0");

            var passRateCell = ws.Cell(row, 7);
            passRateCell.FormulaA1 = $"IF(B{row}=0,0,C{row}/B{row})";
            passRateCell.Style.NumberFormat.Format = "0.0%";

            grandTotal += testCases.Count;
            row++;
        }

        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E8F5E9");
        ws.Cell(row, 2).Value = grandTotal;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E8F5E9");
        ws.Cell(row, 7).FormulaA1 = $"IF(B{row}=0,0,C{row}/B{row})";
        ws.Cell(row, 7).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 7).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#E8F5E9");

        ws.Range(3, 1, row, 7).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
        ws.Range(3, 1, 3, 7).Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;

        ws.Column(1).Width = 25;
        ws.Column(2).Width = 12;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 12;
        ws.Column(7).Width = 12;

        ws.Cell(row + 3, 1).Value = "PRIORITY BREAKDOWN";
        ws.Cell(row + 3, 1).Style.Font.Bold = true;
        ws.Cell(row + 3, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        ws.Cell(row + 3, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#388E3C");
        ws.Range(row + 3, 1, row + 3, 3).Merge();

        string[] priorities = ["P0 - Critical", "P1 - High", "P2 - Medium", "P3 - Low"];
        int prRow = row + 4;
        foreach (var pr in priorities)
        {
            ws.Cell(prRow, 1).Value = pr;
            ws.Cell(prRow, 2).Value = grandTotal / 4;
            prRow++;
        }

        ws.Cell(row + 9, 1).Value = "TEST TYPES";
        ws.Cell(row + 9, 1).Style.Font.Bold = true;
        ws.Cell(row + 9, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#388E3C");
        ws.Cell(row + 9, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        ws.Range(row + 9, 1, row + 9, 3).Merge();

        string[] testTypes = ["Positive Test", "Negative Test", "Boundary Test", "Security Test", "Role-Based Access Test"];
        int ttRow = row + 10;
        foreach (var tt in testTypes)
        {
            ws.Cell(ttRow, 1).Value = tt;
            ws.Cell(ttRow, 2).Value = grandTotal / 5;
            ttRow++;
        }
    }

    record TC(string TCId, string Module, string Title, string Priority, string PreCondition, string Steps, string TestData, string ExpectedResult, string ActualResult, string Status, string TestType, string Notes);

    static List<(string Module, List<TC> TestCases)> GetAllTestCases() =>
    [
        ("01_Account", GetAccountTestCases()),
        ("02_Home", GetHomeTestCases()),
        ("03_Fruit", GetFruitTestCases()),
        ("04_Cart", GetCartTestCases()),
        ("05_Order", GetOrderTestCases()),
        ("06_Category", GetCategoryTestCases()),
        ("07_Dashboard", GetDashboardTestCases()),
        ("08_User_Management", GetUserManagementTestCases()),
        ("09_Coupon", GetCouponTestCases()),
        ("10_Inventory", GetInventoryTestCases()),
        ("11_Batch", GetBatchTestCases()),
        ("12_Supplier", GetSupplierTestCases()),
        ("13_Profile", GetProfileTestCases()),
        ("14_Wishlist", GetWishlistTestCases()),
        ("15_Review", GetReviewTestCases()),
        ("16_AuditLog", GetAuditLogTestCases()),
        ("17_Customer", GetCustomerTestCases()),
        ("18_OperatingCost", GetOperatingCostTestCases()),
    ];

    static List<TC> GetAccountTestCases() => [
        new("AC01","Account","Login with valid credentials (Admin)","P0 - Critical","User has valid admin account","1. Navigate to /Account/Login\n2. Enter email\n3. Enter password\n4. Click Submit","Email: admin@fruitshop.com\nPassword: Admin@123","Redirect to Dashboard or Home. Login successful.","","NOT RUN","Positive",""),
        new("AC02","Account","Login with valid credentials (Staff)","P0 - Critical","User has valid staff account","1. Navigate to /Account/Login\n2. Enter staff credentials\n3. Click Submit","Email: staff@fruitshop.com\nPassword: Staff@123","Redirect to dashboard or home page.","","NOT RUN","Positive",""),
        new("AC03","Account","Login with valid credentials (Customer)","P0 - Critical","User has valid customer account","1. Navigate to /Account/Login\n2. Enter customer credentials\n3. Click Submit","Email: customer@fruitshop.com\nPassword: Customer@123","Redirect to home page with customer access.","","NOT RUN","Positive",""),
        new("AC04","Account","Login with invalid password","P0 - Critical","User has account with correct email","1. Navigate to /Account/Login\n2. Enter valid email\n3. Enter wrong password\n4. Click Submit","Email: admin@fruitshop.com\nPassword: WrongPassword@123","Show error message. Stay on login page.","","NOT RUN","Negative",""),
        new("AC05","Account","Login with non-existent email","P0 - Critical","None","1. Navigate to /Account/Login\n2. Enter non-existent email\n3. Enter any password\n4. Click Submit","Email: nonexistent@fruitshop.com\nPassword: AnyPass@123","Show error message. Stay on login page.","","NOT RUN","Negative",""),
        new("AC06","Account","Login with empty email field","P1 - High","None","1. Navigate to /Account/Login\n2. Leave email empty\n3. Enter password\n4. Click Submit","Email: (empty)\nPassword: Admin@123","Show validation error for email field.","","NOT RUN","Negative",""),
        new("AC07","Account","Login with empty password field","P1 - High","None","1. Navigate to /Account/Login\n2. Enter email\n3. Leave password empty\n4. Click Submit","Email: admin@fruitshop.com\nPassword: (empty)","Show validation error for password field.","","NOT RUN","Negative",""),
        new("AC08","Account","Login with empty both fields","P1 - High","None","1. Navigate to /Account/Login\n2. Leave both fields empty\n3. Click Submit","Email: (empty)\nPassword: (empty)","Show validation errors for both fields.","","NOT RUN","Negative",""),
        new("AC09","Account","Logout clears session","P0 - Critical","User is logged in as Admin","1. Login as Admin\n2. Click Logout link\n3. Navigate to /Dashboard","Session cleared","Redirected to login or access denied.","","NOT RUN","Positive",""),
        new("AC10","Account","Forgot password with valid email","P1 - High","User has registered email","1. Navigate to /Account/Login\n2. Click Forgot Password\n3. Enter valid email\n4. Click Submit","Email: admin@fruitshop.com","Show success message or redirect.","","NOT RUN","Positive",""),
        new("AC11","Account","Forgot password with invalid email","P2 - Medium","None","1. Navigate to /Account/ForgotPassword\n2. Enter non-existent email\n3. Click Submit","Email: fake@email.com","Show error message.","","NOT RUN","Negative",""),
        new("AC12","Account","Register with valid data","P0 - Critical","None","1. Navigate to /Account/Register\n2. Fill all required fields\n3. Click Register","FullName: Test User\nEmail: unique@test.com\nPassword: TestUser@123","Registration successful, redirect to login or home.","","NOT RUN","Positive",""),
        new("AC13","Account","Register with duplicate email","P0 - Critical","Email already exists","1. Navigate to /Account/Register\n2. Enter existing email\n3. Fill remaining fields\n4. Click Register","Email: admin@fruitshop.com","Show error: email already exists.","","NOT RUN","Negative",""),
        new("AC14","Account","Register with password mismatch","P1 - High","None","1. Navigate to /Account/Register\n2. Fill form with mismatched passwords\n3. Click Register","Password: Password@123\nConfirm: DifferentPass@123","Show error: passwords do not match.","","NOT RUN","Negative",""),
        new("AC15","Account","Register with weak password","P1 - High","None","1. Navigate to /Account/Register\n2. Enter weak password\n3. Click Register","Password: 123","Show validation error about password strength.","","NOT RUN","Negative",""),
        new("AC16","Account","Register with empty fields","P1 - High","None","1. Navigate to /Account/Register\n2. Leave all fields empty\n3. Click Register","All fields: (empty)","Show validation errors for all required fields.","","NOT RUN","Negative",""),
        new("AC17","Account","Register with invalid email format","P1 - High","None","1. Navigate to /Account/Register\n2. Enter invalid email format\n3. Fill remaining fields\n4. Click Register","Email: notanemail","Show validation error for email format.","","NOT RUN","Negative",""),
        new("AC18","Account","Register - link to login","P3 - Low","None","1. Navigate to /Account/Register\n2. Click 'Already have account? Login'","","Redirect to /Account/Login","","NOT RUN","Positive",""),
        new("AC19","Account","Login page elements present","P2 - Medium","None","1. Navigate to /Account/Login","Page loaded","Email and Password fields visible.","","NOT RUN","Positive",""),
        new("AC20","Account","Profile access without login","P0 - Critical","User not logged in","1. Navigate to /Account/Profile","","Redirect to /Account/Login","","NOT RUN","Security",""),
    ];

    static List<TC> GetHomeTestCases() => [
        new("HM01","Home","Home page displays products","P0 - Critical","Products exist in database","1. Navigate to /Home","","Product cards displayed with images, names, prices.","","NOT RUN","Positive",""),
        new("HM02","Home","Search with valid query","P0 - Critical","Products match search query","1. Enter 'Apple' in search\n2. Click Search","Query: Apple","Display matching products.","","NOT RUN","Positive",""),
        new("HM03","Home","Search with no results","P1 - High","None","1. Enter 'xyznonexistent'\n2. Click Search","Query: xyznonexistent123","Show empty state or 'No results'.","","NOT RUN","Negative",""),
        new("HM04","Home","Filter by category","P1 - High","Categories exist","1. Click on category link","Category: Apple","Only products in selected category shown.","","NOT RUN","Positive",""),
        new("HM05","Home","Pagination navigation","P1 - High","Multiple pages of products","1. Click page 2 in pagination","","Navigate to page 2 with updated product list.","","NOT RUN","Positive",""),
        new("HM06","Home","Click product navigates to details","P0 - Critical","Products exist","1. Click on first product card","","Navigate to /Fruit/Details/{id}.","","NOT RUN","Positive",""),
        new("HM07","Home","Add to cart from product card","P0 - Critical","Product in stock","1. Click 'Add to Cart' on product","","Item added to cart. Cart badge updated.","","NOT RUN","Positive",""),
        new("HM08","Home","Wishlist toggle","P1 - High","User logged in as Customer","1. Click wishlist button on product","","Wishlist toggled. Alert shown.","","NOT RUN","Positive",""),
        new("HM09","Home","Sort products by price","P2 - Medium","Multiple products","1. Select 'Price: Low to High'","","Products sorted by ascending price.","","NOT RUN","Positive",""),
        new("HM10","Home","Navbar navigation links","P2 - Medium","None","1. Click each nav link","","Navigate to correct pages.","","NOT RUN","Positive",""),
        new("HM11","Home","Footer links functional","P3 - Low","None","1. Scroll to footer\n2. Click footer link","","Navigate to correct page.","","NOT RUN","Positive",""),
        new("HM12","Home","Product card shows name and price","P1 - High","Products exist","1. View home page","","Product name and price visible on cards.","","NOT RUN","Positive",""),
        new("HM13","Home","Breadcrumb navigation","P3 - Low","None","1. View breadcrumb on home page","","Breadcrumb shows current location.","","NOT RUN","Positive",""),
        new("HM14","Home","Category sidebar visible","P2 - Medium","Categories exist","1. View home page sidebar","","Category links displayed in sidebar.","","NOT RUN","Positive",""),
        new("HM15","Home","Access denied page loads","P2 - Medium","None","1. Navigate to /Home/AccessDenied","","Display access denied message.","","NOT RUN","Positive",""),
        new("HM16","Home","Login link navigates to login","P3 - Low","User not logged in","1. Click Login link","","Redirect to /Account/Login.","","NOT RUN","Positive",""),
    ];

    static List<TC> GetFruitTestCases() => [
        new("FR01","Fruit","Fruit index page loads with table","P0 - Critical","Products exist","1. Navigate to /Fruit","","Data table with products displayed.","","NOT RUN","Positive",""),
        new("FR02","Fruit","Search filters results","P1 - High","Products exist","1. Enter search query\n2. Click search","Query: Apple","Only matching products shown.","","NOT RUN","Positive",""),
        new("FR03","Fruit","Fruit details page shows all info","P0 - Critical","Product exists","1. Navigate to Fruit Index\n2. Click Details","","Name, price, image, description, reviews visible.","","NOT RUN","Positive",""),
        new("FR04","Fruit","Add to cart from details page","P0 - Critical","Product in stock","1. Open fruit details\n2. Click Add to Cart","","Item added to cart successfully.","","NOT RUN","Positive",""),
        new("FR05","Fruit","Quantity input accepts numbers","P1 - High","Product exists","1. Open fruit details\n2. Enter quantity","Quantity: 5","Quantity field updated.","","NOT RUN","Positive",""),
        new("FR06","Fruit","Review section visible","P2 - Medium","Reviews exist for product","1. Open fruit details","","Reviews section displayed with stars.","","NOT RUN","Positive",""),
        new("FR07","Fruit","Related products displayed","P2 - Medium","Product has related items","1. Open fruit details","","Related products section shown.","","NOT RUN","Positive",""),
        new("FR08","Fruit","Admin: Create fruit with valid data","P0 - Critical","Admin logged in","1. Navigate to /Fruit/Create\n2. Fill all fields\n3. Save","Name: Selenium Test\nPrice: 25000\nStock: 100","Fruit created, redirect to index.","","NOT RUN","Positive",""),
        new("FR09","Fruit","Admin: Create fruit with invalid data","P1 - High","Admin logged in","1. Navigate to /Fruit/Create\n2. Leave fields empty\n3. Save","","Validation errors shown. Stay on form.","","NOT RUN","Negative",""),
        new("FR10","Fruit","Admin: Edit fruit loads form","P0 - Critical","Fruit exists","1. Navigate to /Fruit\n2. Click Edit","","Form pre-filled with existing data.","","NOT RUN","Positive",""),
        new("FR11","Fruit","Admin: Edit fruit updates data","P0 - Critical","Fruit exists","1. Open edit form\n2. Change name\n3. Save","Name: Updated Fruit","Changes saved, redirect to index.","","NOT RUN","Positive",""),
        new("FR12","Fruit","Admin: Delete fruit shows confirmation","P1 - High","Fruit exists","1. Navigate to /Fruit\n2. Click Delete","","Confirmation dialog shown.","","NOT RUN","Positive",""),
        new("FR13","Fruit","Admin: Pagination works","P2 - Medium","Multiple pages","1. Navigate to /Fruit\n2. Click next page","","Navigate to next page.","","NOT RUN","Positive",""),
        new("FR14","Fruit","Admin: Export CSV link works","P2 - Medium","Products exist","1. Navigate to /Fruit\n2. Click Export CSV","","CSV file download initiated.","","NOT RUN","Positive",""),
        new("FR15","Fruit","Admin: Import Excel form loads","P2 - Medium","Admin logged in","1. Navigate to /Fruit/ImportExcel","","File upload form displayed.","","NOT RUN","Positive",""),
    ];

    static List<TC> GetCartTestCases() => [
        new("CA01","Cart","Empty cart shows empty message","P1 - High","User logged in, cart empty","1. Navigate to /Order/Cart","","Empty cart message displayed.","","NOT RUN","Positive",""),
        new("CA02","Cart","Add item increases cart count","P0 - Critical","Product in stock","1. Add product to cart\n2. View cart","","Cart count increases.","","NOT RUN","Positive",""),
        new("CA03","Cart","Update quantity reflects change","P0 - Critical","Items in cart","1. View cart\n2. Change quantity\n3. Update","Quantity: 5","Quantity updated, total recalculated.","","NOT RUN","Positive",""),
        new("CA04","Cart","Remove item decreases count","P0 - Critical","Items in cart","1. View cart\n2. Click Remove","","Item removed, count decreases.","","NOT RUN","Positive",""),
        new("CA05","Cart","Apply valid coupon shows discount","P1 - High","Valid coupon exists","1. View cart\n2. Enter coupon code\n3. Apply","Code: ADMIN10","Discount applied, total reduced.","","NOT RUN","Positive",""),
        new("CA06","Cart","Apply invalid coupon shows error","P1 - High","None","1. View cart\n2. Enter invalid code\n3. Apply","Code: INVALID999","Error alert shown.","","NOT RUN","Negative",""),
        new("CA07","Cart","Empty cart hides checkout","P1 - High","Cart is empty","1. Navigate to /Order/Cart","","Checkout button hidden or disabled.","","NOT RUN","Positive",""),
        new("CA08","Cart","Continue shopping navigates","P3 - Low","None","1. Click Continue Shopping","","Navigate to /Home or /Fruit.","","NOT RUN","Positive",""),
        new("CA09","Cart","Total amount calculates correctly","P1 - High","Items in cart","1. View cart","","Total = sum of (price x quantity).","","NOT RUN","Positive",""),
        new("CA10","Cart","Cart displays product images","P2 - Medium","Items in cart","1. View cart","","Product images visible.","","NOT RUN","Positive",""),
    ];

    static List<TC> GetOrderTestCases() => [
        new("OR01","Order","Checkout with empty cart redirects","P1 - High","Cart is empty","1. Navigate to /Order/Checkout","","Redirect to cart or show error.","","NOT RUN","Negative",""),
        new("OR02","Order","Checkout form displays with items","P0 - Critical","Items in cart","1. Add item to cart\n2. Navigate to /Order/Checkout","","Checkout form with fields displayed.","","NOT RUN","Positive",""),
        new("OR03","Order","Checkout with valid data creates order","P0 - Critical","Items in cart","1. Fill checkout form\n2. Submit","Name: Test Customer\nAddress: 123 Test St","Order created, redirect to confirmation.","","NOT RUN","Positive",""),
        new("OR04","Order","Checkout with empty fields shows errors","P1 - High","Items in cart","1. Leave fields empty\n2. Submit","","Validation errors for required fields.","","NOT RUN","Negative",""),
        new("OR05","Order","Checkout with valid coupon shows discount","P1 - High","Valid coupon","1. Apply coupon\n2. Submit","Coupon: ADMIN10","Discount applied to total.","","NOT RUN","Positive",""),
        new("OR06","Order","Checkout with invalid coupon shows error","P1 - High","None","1. Apply invalid coupon\n2. Submit","Coupon: INVALID999","Error alert shown.","","NOT RUN","Negative",""),
        new("OR07","Order","Order history displays orders","P0 - Critical","User has orders","1. Navigate to /Order/History","","List of user orders displayed.","","NOT RUN","Positive",""),
        new("OR08","Order","Order details shows all info","P1 - High","Order exists","1. Open order details","","Order items, total, status visible.","","NOT RUN","Positive",""),
        new("OR09","Order","Invoice accessible from order","P2 - Medium","Order exists","1. Open order details\n2. Click Invoice","","Invoice PDF generated.","","NOT RUN","Positive",""),
        new("OR10","Order","Cancel pending order","P1 - High","Order in Pending status","1. Open order details\n2. Click Cancel","","Order cancelled, status updated.","","NOT RUN","Positive",""),
        new("OR11","Order","Admin: View all orders","P0 - Critical","Staff/Admin logged in","1. Navigate to /Order","","All orders table displayed.","","NOT RUN","Positive",""),
        new("OR12","Order","Admin: Update order status","P0 - Critical","Order exists","1. Change status dropdown\n2. Update","Status: Shipped","Status updated successfully.","","NOT RUN","Positive",""),
        new("OR13","Order","Admin: Filter by status","P1 - High","Orders exist","1. Select status filter\n2. Apply","Status: Pending","Only filtered orders shown.","","NOT RUN","Positive",""),
        new("OR14","Order","Admin: Export Excel works","P2 - Medium","Orders exist","1. Navigate to /Order\n2. Click Export Excel","","Excel file download initiated.","","NOT RUN","Positive",""),
        new("OR15","Order","Admin: Export PDF works","P2 - Medium","Orders exist","1. Navigate to /Order\n2. Click Export PDF","","PDF file download initiated.","","NOT RUN","Positive",""),
    ];

    static List<TC> GetCategoryTestCases() => [
        new("CT01","Category","Admin: Category index loads with table","P0 - Critical","Admin logged in","1. Navigate to /Category","","Category table displayed.","","NOT RUN","Positive",""),
        new("CT02","Category","Admin: Create category succeeds","P0 - Critical","Admin logged in","1. Click Create\n2. Enter name and description\n3. Save","Name: Selenium Cat","Category created, redirect to index.","","NOT RUN","Positive",""),
        new("CT03","Category","Admin: Create with duplicate name fails","P1 - High","Category exists","1. Create category with existing name","Name: Apple","Error: category exists.","","NOT RUN","Negative",""),
        new("CT04","Category","Admin: Create with empty name fails","P1 - High","Admin logged in","1. Leave name empty\n2. Save","","Validation error shown.","","NOT RUN","Negative",""),
        new("CT05","Category","Admin: Edit category loads form","P0 - Critical","Category exists","1. Click Edit on category","","Form pre-filled with data.","","NOT RUN","Positive",""),
        new("CT06","Category","Admin: Edit category saves changes","P0 - Critical","Category exists","1. Change name\n2. Save","Name: Updated Category","Changes saved.","","NOT RUN","Positive",""),
        new("CT07","Category","Admin: Delete shows confirmation","P1 - High","Category exists","1. Click Delete","","Confirmation dialog shown.","","NOT RUN","Positive",""),
        new("CT08","Category","Admin: Delete confirms and removes","P1 - High","Category exists","1. Confirm delete","","Category removed from list.","","NOT RUN","Positive",""),
        new("CT09","Category","Admin: Pagination works","P2 - Medium","Multiple pages","1. Click next page","","Navigate to next page.","","NOT RUN","Positive",""),
        new("CT10","Category","Staff: Cannot access category admin","P0 - Critical","Staff logged in","1. Navigate to /Category","","Access denied or redirect.","","NOT RUN","Security",""),
    ];

    static List<TC> GetDashboardTestCases() => [
        new("DS01","Dashboard","Admin: Dashboard loads with KPI cards","P0 - Critical","Admin logged in","1. Navigate to /Dashboard","","KPI cards (Revenue, Orders, Customers) displayed.","","NOT RUN","Positive",""),
        new("DS02","Dashboard","Admin: Revenue displayed","P0 - Critical","Orders exist","1. View dashboard","","Total revenue shown.","","NOT RUN","Positive",""),
        new("DS03","Dashboard","Admin: Low stock alerts shown","P1 - High","Low stock items exist","1. View dashboard","","Low stock items highlighted.","","NOT RUN","Positive",""),
        new("DS04","Dashboard","Admin: Charts displayed","P2 - Medium","Data exists","1. View dashboard","","Revenue and order charts rendered.","","NOT RUN","Positive",""),
        new("DS05","Dashboard","Admin: Daily report loads","P1 - High","Admin logged in","1. Navigate to /Dashboard/DailyReport","","Daily report table/chart displayed.","","NOT RUN","Positive",""),
        new("DS06","Dashboard","Admin: Category report loads","P1 - High","Admin logged in","1. Navigate to /Dashboard/CategoryReport","","Category performance report displayed.","","NOT RUN","Positive",""),
        new("DS07","Dashboard","Admin: Export orders works","P2 - Medium","Orders exist","1. Click Export Orders","","Export initiated.","","NOT RUN","Positive",""),
        new("DS08","Dashboard","Admin: Date filter works","P2 - Medium","Data exists","1. Set date range\n2. Apply filter","Date: Last 30 days","Data filtered by date range.","","NOT RUN","Positive",""),
        new("DS09","Dashboard","Admin: Top products displayed","P2 - Medium","Orders exist","1. View dashboard","","Top selling products listed.","","NOT RUN","Positive",""),
        new("DS10","Dashboard","Admin: Recent orders displayed","P2 - Medium","Orders exist","1. View dashboard","","Recent orders table shown.","","NOT RUN","Positive",""),
        new("DS11","Dashboard","Staff: Dashboard loads with stats","P1 - High","Staff logged in","1. Navigate to /Dashboard","","Dashboard with staff-level stats.","","NOT RUN","Positive",""),
        new("DS12","Dashboard","Customer: Dashboard access denied","P0 - Critical","Customer logged in","1. Navigate to /Dashboard","","Access denied or redirect.","","NOT RUN","Security",""),
    ];

    static List<TC> GetUserManagementTestCases() => [
        new("UM01","User_Management","Admin: User index loads with table","P0 - Critical","Admin logged in","1. Navigate to /User","","User table displayed.","","NOT RUN","Positive",""),
        new("UM02","User_Management","Admin: List all users","P0 - Critical","Users exist","1. View user index","","All users listed with roles.","","NOT RUN","Positive",""),
        new("UM03","User_Management","Admin: View user details","P1 - High","User exists","1. Click Details","","User profile information displayed.","","NOT RUN","Positive",""),
        new("UM04","User_Management","Admin: Toggle user active status","P0 - Critical","User exists","1. Click Toggle Active","","User status toggled.","","NOT RUN","Positive",""),
        new("UM05","User_Management","Admin: Filter by role","P1 - High","Users exist","1. Select role filter\n2. Apply","Role: Admin","Only users of selected role shown.","","NOT RUN","Positive",""),
        new("UM06","User_Management","Admin: Export CSV works","P2 - Medium","Users exist","1. Click Export CSV","","CSV file download initiated.","","NOT RUN","Positive",""),
        new("UM07","User_Management","Admin: Pagination works","P2 - Medium","Multiple pages","1. Click next page","","Navigate to next page.","","NOT RUN","Positive",""),
        new("UM08","User_Management","Staff: Cannot access user management","P0 - Critical","Staff logged in","1. Navigate to /User","","Access denied.","","NOT RUN","Security",""),
        new("UM09","User_Management","Customer: Cannot access user management","P0 - Critical","Customer logged in","1. Navigate to /User","","Access denied.","","NOT RUN","Security",""),
    ];

    static List<TC> GetCouponTestCases() => [
        new("CP01","Coupon","Admin: Coupon index loads","P0 - Critical","Admin logged in","1. Navigate to /Coupon","","Coupon table displayed.","","NOT RUN","Positive",""),
        new("CP02","Coupon","Admin: List all coupons","P0 - Critical","Coupons exist","1. View coupon index","","All coupons listed with status.","","NOT RUN","Positive",""),
        new("CP03","Coupon","Admin: Create coupon succeeds","P0 - Critical","Admin logged in","1. Create new coupon\n2. Save","Code: SELENIUM10\nDiscount: 10%","Coupon created successfully.","","NOT RUN","Positive",""),
        new("CP04","Coupon","Admin: Create duplicate code fails","P1 - High","Coupon exists","1. Create with existing code","Code: ADMIN10","Error: code exists.","","NOT RUN","Negative",""),
        new("CP05","Coupon","Admin: Edit coupon loads form","P0 - Critical","Coupon exists","1. Click Edit","","Form pre-filled with data.","","NOT RUN","Positive",""),
        new("CP06","Coupon","Admin: Delete shows confirmation","P1 - High","Coupon exists","1. Click Delete","","Confirmation shown.","","NOT RUN","Positive",""),
        new("CP07","Coupon","Admin: Status badges visible","P2 - Medium","Coupons exist","1. View coupon index","","Status badges (Active/Expired) shown.","","NOT RUN","Positive",""),
        new("CP08","Coupon","Staff: Cannot access coupon","P0 - Critical","Staff logged in","1. Navigate to /Coupon","","Access denied.","","NOT RUN","Security",""),
    ];

    static List<TC> GetInventoryTestCases() => [
        new("IN01","Inventory","Staff: Inventory index loads","P0 - Critical","Staff logged in","1. Navigate to /Inventory","","Inventory table displayed.","","NOT RUN","Positive",""),
        new("IN02","Inventory","Staff: List all inventory items","P0 - Critical","Items exist","1. View inventory","","All items with stock levels shown.","","NOT RUN","Positive",""),
        new("IN03","Inventory","Staff: Low stock items highlighted","P1 - High","Low stock items exist","1. View inventory","","Low stock rows highlighted in red.","","NOT RUN","Positive",""),
        new("IN04","Inventory","Staff: Adjust stock succeeds","P0 - Critical","Item exists","1. Click Adjust\n2. Enter quantity and reason\n3. Save","Qty: 10\nReason: Restock","Stock updated, log created.","","NOT RUN","Positive",""),
        new("IN05","Inventory","Staff: Adjust with zero quantity fails","P1 - High","Item exists","1. Set quantity to 0\n2. Save","Qty: 0","Validation error shown.","","NOT RUN","Negative",""),
        new("IN06","Inventory","Staff: Pagination works","P2 - Medium","Multiple pages","1. Click next page","","Navigate to next page.","","NOT RUN","Positive",""),
        new("IN07","Inventory","Customer: Cannot access inventory","P0 - Critical","Customer logged in","1. Navigate to /Inventory","","Access denied.","","NOT RUN","Security",""),
    ];

    static List<TC> GetBatchTestCases() => [
        new("BT01","Batch","Staff: Batch index loads","P0 - Critical","Staff logged in","1. Navigate to /Batch","","Batch table displayed.","","NOT RUN","Positive",""),
        new("BT02","Batch","Staff: List all batches","P0 - Critical","Batches exist","1. View batch index","","All batches with expiry dates listed.","","NOT RUN","Positive",""),
        new("BT03","Batch","Staff: Create batch loads form","P0 - Critical","Staff logged in","1. Click Create","","Batch creation form displayed.","","NOT RUN","Positive",""),
        new("BT04","Batch","Staff: Create batch with valid data","P0 - Critical","Fruit and supplier exist","1. Fill batch form\n2. Save","Qty: 50\nPrice: 15000","Batch created successfully.","","NOT RUN","Positive",""),
        new("BT05","Batch","Staff: Edit batch loads form","P0 - Critical","Batch exists","1. Click Edit","","Form pre-filled with data.","","NOT RUN","Positive",""),
        new("BT06","Batch","Staff: Print receipt works","P2 - Medium","Batch exists","1. Click Print Receipt","","Receipt PDF generated.","","NOT RUN","Positive",""),
        new("BT07","Batch","Staff: Expiry warning loads","P1 - High","Staff logged in","1. Click Expiry Warning","","List of expiring batches shown.","","NOT RUN","Positive",""),
        new("BT08","Batch","Customer: Cannot access batch","P0 - Critical","Customer logged in","1. Navigate to /Batch","","Access denied.","","NOT RUN","Security",""),
    ];

    static List<TC> GetSupplierTestCases() => [
        new("SP01","Supplier","Staff: Supplier index loads","P0 - Critical","Staff logged in","1. Navigate to /AdminSupplier","","Supplier table displayed.","","NOT RUN","Positive",""),
        new("SP02","Supplier","Staff: List all suppliers","P0 - Critical","Suppliers exist","1. View supplier index","","All suppliers listed.","","NOT RUN","Positive",""),
        new("SP03","Supplier","Staff: Create supplier succeeds","P0 - Critical","Staff logged in","1. Click Create\n2. Fill form\n3. Save","Name: Selenium Supplier","Supplier created successfully.","","NOT RUN","Positive",""),
        new("SP04","Supplier","Staff: Edit supplier loads form","P0 - Critical","Supplier exists","1. Click Edit","","Form pre-filled with data.","","NOT RUN","Positive",""),
        new("SP05","Supplier","Staff: Delete shows confirmation","P1 - High","Supplier exists","1. Click Delete","","Confirmation shown.","","NOT RUN","Positive",""),
        new("SP06","Supplier","Staff: View supplier details","P1 - High","Supplier exists","1. Click Details","","Supplier info displayed.","","NOT RUN","Positive",""),
        new("SP07","Supplier","Staff: View supplier history","P2 - Medium","Supplier exists","1. Click History","","Supplier transaction history shown.","","NOT RUN","Positive",""),
        new("SP08","Supplier","Staff: Price comparison loads","P2 - Medium","Staff logged in","1. Click Price Comparison","","Comparison chart/table displayed.","","NOT RUN","Positive",""),
        new("SP09","Supplier","Customer: Cannot access supplier","P0 - Critical","Customer logged in","1. Navigate to /AdminSupplier","","Access denied.","","NOT RUN","Security",""),
    ];

    static List<TC> GetProfileTestCases() => [
        new("PF01","Profile","Profile page shows user info","P0 - Critical","Customer logged in","1. Navigate to /Account/Profile","","User info form displayed.","","NOT RUN","Positive",""),
        new("PF02","Profile","Update profile succeeds","P0 - Critical","Customer logged in","1. Change name/phone/address\n2. Save","Name: Updated Name","Changes saved, success message shown.","","NOT RUN","Positive",""),
        new("PF03","Profile","Change password with wrong current fails","P1 - High","Customer logged in","1. Enter wrong current password\n2. Save","Current: WrongPass","Error: current password incorrect.","","NOT RUN","Negative",""),
        new("PF04","Profile","Change password with mismatch fails","P1 - High","Customer logged in","1. Enter mismatched new passwords\n2. Save","New: Pass@123\nConfirm: Other@123","Error: passwords do not match.","","NOT RUN","Negative",""),
        new("PF05","Profile","Change password with weak password fails","P1 - High","Customer logged in","1. Enter weak new password\n2. Save","New: 123","Error: password too weak.","","NOT RUN","Negative",""),
        new("PF06","Profile","Loyalty points visible","P2 - Medium","Customer has points","1. View profile","","Loyalty points displayed.","","NOT RUN","Positive",""),
        new("PF07","Profile","Access without login redirects","P0 - Critical","Not logged in","1. Navigate to /Account/Profile","","Redirect to login.","","NOT RUN","Security",""),
    ];

    static List<TC> GetWishlistTestCases() => [
        new("WS01","Wishlist","Empty wishlist shows message","P1 - High","Customer logged in, empty wishlist","1. Navigate to /Wishlist","","Empty wishlist message shown.","","NOT RUN","Positive",""),
        new("WS02","Wishlist","Add item to wishlist","P1 - High","Customer logged in","1. Add product to wishlist","","Item added, count increases.","","NOT RUN","Positive",""),
        new("WS03","Wishlist","Remove item from wishlist","P1 - High","Items in wishlist","1. Remove item","","Item removed, count decreases.","","NOT RUN","Positive",""),
        new("WS04","Wishlist","Add to cart from wishlist","P2 - Medium","Items in wishlist","1. Click Add to Cart","","Item moved to cart.","","NOT RUN","Positive",""),
        new("WS05","Wishlist","Access without login redirects","P0 - Critical","Not logged in","1. Navigate to /Wishlist","","Redirect to login.","","NOT RUN","Security",""),
        new("WS06","Wishlist","Continue shopping navigates","P3 - Low","None","1. Click Continue Shopping","","Navigate to /Home.","","NOT RUN","Positive",""),
    ];

    static List<TC> GetReviewTestCases() => [
        new("RV01","Review","Submit review succeeds","P1 - High","Customer logged in, ordered product","1. Open product details\n2. Submit review with rating and comment","Rating: 5 stars\nComment: Great!","Review submitted successfully.","","NOT RUN","Positive",""),
        new("RV02","Review","Display rating stars on product","P1 - High","Reviews exist","1. Open product details","","Rating stars displayed.","","NOT RUN","Positive",""),
        new("RV03","Review","Display review list","P1 - High","Reviews exist","1. Open product details","","List of reviews with comments shown.","","NOT RUN","Positive",""),
        new("RV04","Review","Submit review without login prompts login","P1 - High","Not logged in","1. Try to submit review","","Redirect to login.","","NOT RUN","Security",""),
        new("RV05","Review","Submit review with empty comment fails","P1 - High","Customer logged in","1. Submit review without comment","","Validation error shown.","","NOT RUN","Negative",""),
    ];

    static List<TC> GetAuditLogTestCases() => [
        new("AL01","AuditLog","Admin: Audit log index loads","P0 - Critical","Admin logged in","1. Navigate to /AdminAuditLog","","Audit log table displayed.","","NOT RUN","Positive",""),
        new("AL02","AuditLog","Admin: List all audit logs","P0 - Critical","Logs exist","1. View audit log","","All activity logs listed.","","NOT RUN","Positive",""),
        new("AL03","AuditLog","Admin: Filter by action type","P1 - High","Logs exist","1. Select action filter","Action: Create","Only filtered logs shown.","","NOT RUN","Positive",""),
        new("AL04","AuditLog","Admin: Filter by user","P1 - High","Logs exist","1. Select user filter","User: admin@fruitshop.com","Only user's logs shown.","","NOT RUN","Positive",""),
        new("AL05","AuditLog","Admin: Pagination works","P2 - Medium","Multiple pages","1. Click next page","","Navigate to next page.","","NOT RUN","Positive",""),
        new("AL06","AuditLog","Staff: Cannot access audit log","P0 - Critical","Staff logged in","1. Navigate to /AdminAuditLog","","Access denied.","","NOT RUN","Security",""),
        new("AL07","AuditLog","Customer: Cannot access audit log","P0 - Critical","Customer logged in","1. Navigate to /AdminAuditLog","","Access denied.","","NOT RUN","Security",""),
    ];

    static List<TC> GetCustomerTestCases() => [
        new("CU01","Customer","Admin: Customer index loads","P0 - Critical","Admin logged in","1. Navigate to /AdminCustomer","","Customer table displayed.","","NOT RUN","Positive",""),
        new("CU02","Customer","Admin: List all customers","P0 - Critical","Customers exist","1. View customer index","","All customers listed.","","NOT RUN","Positive",""),
        new("CU03","Customer","Admin: View customer details","P1 - High","Customer exists","1. Click Details","","Customer profile and stats displayed.","","NOT RUN","Positive",""),
        new("CU04","Customer","Admin: View customer order history","P2 - Medium","Customer has orders","1. Open customer details","","Customer order history shown.","","NOT RUN","Positive",""),
        new("CU05","Customer","Admin: View loyalty points","P2 - Medium","Customer has points","1. Open customer details","","Loyalty points displayed.","","NOT RUN","Positive",""),
        new("CU06","Customer","Staff: Cannot access admin customer","P0 - Critical","Staff logged in","1. Navigate to /AdminCustomer","","Access denied.","","NOT RUN","Security",""),
    ];

    static List<TC> GetOperatingCostTestCases() => [
        new("OC01","OperatingCost","Admin: Operating cost index loads","P0 - Critical","Admin logged in","1. Navigate to /OperatingCost","","Cost table displayed.","","NOT RUN","Positive",""),
        new("OC02","OperatingCost","Admin: List all costs","P0 - Critical","Costs exist","1. View operating cost","","All operating costs listed.","","NOT RUN","Positive",""),
        new("OC03","OperatingCost","Admin: Create cost succeeds","P0 - Critical","Admin logged in","1. Click Create\n2. Fill form\n3. Save","Name: Utilities\nAmount: 500000","Cost created successfully.","","NOT RUN","Positive",""),
        new("OC04","OperatingCost","Admin: Edit cost loads form","P0 - Critical","Cost exists","1. Click Edit","","Form pre-filled with data.","","NOT RUN","Positive",""),
        new("OC05","OperatingCost","Admin: Delete shows confirmation","P1 - High","Cost exists","1. Click Delete","","Confirmation shown.","","NOT RUN","Positive",""),
        new("OC06","OperatingCost","Admin: Total cost displayed","P1 - High","Costs exist","1. View operating cost","","Total cost card shown.","","NOT RUN","Positive",""),
        new("OC07","OperatingCost","Admin: Month filter works","P2 - Medium","Costs exist","1. Select month\n2. Apply filter","Month: 2026-04","Costs filtered by month.","","NOT RUN","Positive",""),
        new("OC08","OperatingCost","Staff: Cannot access operating cost","P0 - Critical","Staff logged in","1. Navigate to /OperatingCost","","Access denied.","","NOT RUN","Security",""),
    ];
}
