using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

string outputPath = Path.Combine("D:\\ASM phattrienungdung", "KichBan_TrinhBay_FruitShop_Updated.docx");

using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
var mainPart = doc.AddMainDocumentPart();
mainPart.Document = new Document();
var body = mainPart.Document.AppendChild(new Body());

var sectPr = new SectionProperties(
    new PageSize { Width = 12240, Height = 15840 },
    new PageMargin { Top = 1134, Bottom = 1134, Left = 1134, Right = 1134 }
);

void AddTitle(string text)
{
    body.AppendChild(new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "200" }
        ),
        new Run(
            new RunProperties(
                new Bold(), new FontSize { Val = "36" },
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new Color { Val = "1F4E79" }
            ),
            new Text(text)
        )
    ));
}

void AddHeading1(string text)
{
    body.AppendChild(new Paragraph(
        new ParagraphProperties(new SpacingBetweenLines { Before = "360", After = "120" }),
        new Run(
            new RunProperties(
                new Bold(), new FontSize { Val = "28" },
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new Color { Val = "1F4E79" }
            ),
            new Text(text)
        )
    ));
}

void AddHeading2(string text)
{
    body.AppendChild(new Paragraph(
        new ParagraphProperties(new SpacingBetweenLines { Before = "240", After = "80" }),
        new Run(
            new RunProperties(
                new Bold(), new FontSize { Val = "24" },
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new Color { Val = "2E75B6" }
            ),
            new Text(text)
        )
    ));
}

void AddParagraph(string text, bool bold = false, bool italic = false, string fontSize = "22", string? color = null)
{
    var lines = text.Split('\n');
    var p = new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "80" }));
    for (int i = 0; i < lines.Length; i++)
    {
        var rp = new RunProperties(
            new FontSize { Val = fontSize },
            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
        );
        if (bold) rp.AppendChild(new Bold());
        if (italic) rp.AppendChild(new Italic());
        if (color != null) rp.AppendChild(new Color { Val = color });

        var run = new Run(rp, new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
        p.AppendChild(run);
        if (i < lines.Length - 1) p.AppendChild(new Run(new Break()));
    }
    body.AppendChild(p);
}

void AddQuote(string text)
{
    var p = new Paragraph(
        new ParagraphProperties(
            new Indentation { Left = "720" },
            new SpacingBetweenLines { After = "80" },
            new ParagraphBorders(
                new LeftBorder { Val = BorderValues.Single, Size = 12, Color = "2E75B6", Space = 8 }
            )
        ),
        new Run(
            new RunProperties(
                new Italic(),
                new FontSize { Val = "22" },
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new Color { Val = "404040" }
            ),
            new Text(text) { Space = SpaceProcessingModeValues.Preserve }
        )
    );
    body.AppendChild(p);
}

void AddCode(string text)
{
    var lines = text.Split('\n');
    var p = new Paragraph(new ParagraphProperties(
        new Shading { Val = ShadingPatternValues.Clear, Fill = "F2F2F2" },
        new Indentation { Left = "360" },
        new SpacingBetweenLines { After = "120" }
    ));
    for (int i = 0; i < lines.Length; i++)
    {
        p.AppendChild(new Run(
            new RunProperties(
                new FontSize { Val = "18" },
                new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" }
            ),
            new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve }
        ));
        if (i < lines.Length - 1) p.AppendChild(new Run(new Break()));
    }
    body.AppendChild(p);
}

void AddBullet(string text, bool bold = false)
{
    var rp = new RunProperties(
        new FontSize { Val = "22" },
        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
    );
    if (bold) rp.AppendChild(new Bold());

    body.AppendChild(new Paragraph(
        new ParagraphProperties(
            new Indentation { Left = "720", Hanging = "360" },
            new SpacingBetweenLines { After = "40" }
        ),
        new Run(
            new RunProperties(
                new FontSize { Val = "22" },
                new RunFonts { Ascii = "Symbol", HighAnsi = "Symbol" }
            ),
            new Text("· ") { Space = SpaceProcessingModeValues.Preserve }
        ),
        new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve })
    ));
}

void AddTable(string[][] data, bool hasHeader = true)
{
    var table = new Table(new TableProperties(
        new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
        new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
            new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
            new RightBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" }
        )
    ));

    for (int i = 0; i < data.Length; i++)
    {
        var row = new TableRow();
        foreach (var cellText in data[i])
        {
            var rp = new RunProperties(
                new FontSize { Val = "20" },
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
            );
            if (i == 0 && hasHeader)
            {
                rp.AppendChild(new Bold());
                rp.AppendChild(new Color { Val = "FFFFFF" });
            }

            var cellProps = new TableCellProperties(
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            );
            if (i == 0 && hasHeader)
                cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "1F4E79" });
            else if (i % 2 == 0)
                cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "F2F7FC" });

            var cell = new TableCell(
                cellProps,
                new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { After = "0" }),
                    new Run(rp, new Text(cellText) { Space = SpaceProcessingModeValues.Preserve })
                )
            );
            row.AppendChild(cell);
        }
        table.AppendChild(row);
    }
    body.AppendChild(table);
    body.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "120" })));
}

void AddSeparator()
{
    body.AppendChild(new Paragraph(
        new ParagraphProperties(
            new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "2E75B6", Space = 1 }),
            new SpacingBetweenLines { Before = "240", After = "240" }
        )
    ));
}

void AddPageBreak()
{
    body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
}

// ==========================================
// CONTENT
// ==========================================

// === TRANG BÌA ===
body.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "600" })));
AddTitle("KICH BAN THUYET TRINH");
AddTitle("KIEM THU HE THONG FRUITSHOP");
body.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "200" })));

AddParagraph("Du an: FruitShop - He thong quan ly ban trai cay truc tuyen", bold: true, fontSize: "24");
AddParagraph("Cong nghe: ASP.NET Core MVC + xUnit + Moq", fontSize: "22");
AddParagraph("Tong test case: 225 (11 module) - 100% Pass", fontSize: "22");
AddParagraph($"Ngay: {DateTime.Now:dd/MM/yyyy}", fontSize: "22");

AddPageBreak();

// === PHAN 1: GIOI THIEU (2 phut) ===
AddHeading1("PHAN 1: GIOI THIEU (2 phut)");
AddSeparator();

AddQuote("\"FruitShop la he thong quan ly ban hang trai cay truc tuyen, xay dung tren ASP.NET Core MVC voi Dapper ORM va SQL Server. He thong co 11 module chinh, 225 test case da duoc tu dong hoa hoan toan voi xUnit va Moq.\"");

AddParagraph("Mo man bang con so:", bold: true);
AddBullet("11 Controller (module)");
AddBullet("225 test case tu dong");
AddBullet("225/225 Pass - 100% dat");
AddBullet("Ty le tu dong hoa: 100%");
AddBullet("Thoi gian chay toan bo: ~2 giay");

AddQuote("\"Day la so lieu ma em muon mo man trinh bay hom nay.\"");

// === PHAN 2: MO HINH Kiem THU (3 phut) ===
AddHeading1("PHAN 2: MO HINH KIEM THU (3 phut)");
AddSeparator();

AddHeading2("a) Test Pyramid - Cac lop kiem thu");
AddBullet("Lop 1: E2E Test (it nhat, cham nhat) - Selenium WebDriver");
AddBullet("Lop 2: Integration Test - ket noi DB that");
AddBullet("Lop 3: Unit Test (nhieu nhat, nhanh nhat) - xUnit + Moq <- PHU HOP VOI DU AN");

AddHeading2("b) xUnit - Framework Unit Test");
AddBullet("Framework unit test chinh thuc cho .NET (Microsoft khuyen nghi)");
AddBullet("Moi test method danh dau [Fact], chay doc lap, khong phu thuoc nhau");
AddBullet("Dung Assert de kiem tra: Assert.Equal(), Assert.IsType<>(), Assert.NotNull()...");

AddHeading2("c) Moq - Thu vien Mock (gia lap du lieu)");
AddBullet("Gia lap Repository - gia lap tac dong database, KHONG can SQL Server that");
AddBullet("Khi test KHONG can ket noi SQL Server, khong can du lieu that");
AddBullet("Co the kiem soat du lieu tra ve, gia lap loi DB, kiem tra method duoc goi khong");

AddQuote("\"Tai sao dung Mock ma khong test truc tiep DB? Vi Unit Test can chay nhanh, doc lap, lap lai duoc. Neu phu thuoc DB that thi test se cham, du lieu thay doi se lam test fail sai.\"");

AddHeading2("Vi du minh hoa (mo code cho coi):");
AddCode(@"// 1. Tao mock - gia lap UserRepository, KHONG cham DB
var userRepo = new Mock<UserRepository>(...);

// 2. Setup - khi goi GetByEmail thi tra ve user gia
userRepo.Setup(r => r.GetByEmail(""test@test.com"")).Returns(fakeUser);

// 3. Goi Controller that
var result = controller.Login(model, null);

// 4. Kiem tra ket qua
Assert.IsType<RedirectToActionResult>(result);

// 5. Verify - dam bao method da duoc goi dung
userRepo.Verify(r => r.GetByEmail(""test@test.com""), Times.Once);");

// === PHAN 3: CHIEN LUOC KIEM THU (3 phut) ===
AddPageBreak();
AddHeading1("PHAN 3: CHIEN LUOC KIEM THU (3 phut)");
AddSeparator();

AddQuote("\"Em kiem thu theo 4 nhom truong hop cho moi Controller:\"");

AddTable(new[] {
    new[] { "Nhom", "Mo ta", "Vi du" },
    new[] { "Happy path", "Luong dung, du lieu hop le", "Dang nhap dung email + password -> redirect Home" },
    new[] { "Validation", "Du lieu khong hop le", "Email rong, mat khau qua ngan, ten trung" },
    new[] { "Authorization", "Phan quyen, bao mat", "Chua dang nhap -> redirect Login, Customer khong xem don nguoi khac" },
    new[] { "Exception", "DB loi, du lieu null", "Repository throw Exception -> bat loi, hien thi thong bao" },
});

AddHeading2("Cau truc Test Project:");
AddCode(@"FruitShop.Tests/
├── TestBase.cs                  <- Class cha chung, chua helper
├── FakeSession.cs               <- Gia lap Session (thay HttpContext that)
├── AccountControllerTests.cs       (24 tests)
├── HomeControllerTests.cs          (20 tests)
├── CategoryControllerTests.cs      (20 tests)
├── FruitControllerTests.cs         (21 tests)
├── OrderControllerTests.cs         (20 tests)
├── UserControllerTests.cs          (20 tests)
├── DashboardControllerTests.cs     (20 tests)
├── CouponControllerTests.cs        (20 tests)
├── InventoryControllerTests.cs     (20 tests)
├── ReviewControllerTests.cs        (20 tests)
└── WishlistControllerTests.cs      (20 tests)");

// === PHAN 4: DEMO CHAY TEST (3 phut) ===
AddHeading1("PHAN 4: DEMO CHAY TEST (3 phut)");
AddSeparator();

AddQuote("\"Em se demo chay test truc tiep tren may.\"");

AddParagraph("Buoc 1: Mo Terminal, chay lenh:", bold: true);
AddCode("dotnet test");

AddParagraph("Buoc 2: Cho ket qua (khoang 2-3 giay):", bold: true);
AddCode("Passed! - Failed: 0, Passed: 225, Skipped: 0, Total: 225, Duration: 2s");

AddQuote("\"Toan bo 225 test chay trong khoang 2 giay vi khong ket noi DB that. Day la uu diem cua Unit Test voi Mock.\"");

AddParagraph("Buoc 3: Mo 1 file test cu the de giai thich (AccountControllerTests.cs):", bold: true);

AddQuote("\"Vi du AccountController co 24 test case, chia thanh cac nhom:\"");
AddBullet("Login GET: 4 test (chua dang nhap, da dang nhap Customer/Admin, ReturnUrl)");
AddBullet("Login POST: 6 test (ModelState invalid, email sai, password sai, dung, ReturnUrl, DB loi)");
AddBullet("Register: 4 test (GET chua/da login, POST email trung, POST hop le + BCrypt hash)");
AddBullet("Logout: 1 test (xoa session, redirect Login)");
AddBullet("Profile: 3 test (GET user ton tai/khong, POST cap nhat)");
AddBullet("ChangePassword: 4 test (confirm khong khop, mat khau ngan, mat khau cu sai, hop le)");

// === PHAN 5: CAC DIEM NOI BAT (2 phut) ===
AddPageBreak();
AddHeading1("PHAN 5: CAC DIEM NOI BAT (2 phut)");
AddSeparator();

AddQuote("\"Mot so diem dang chu y trong qua trinh kiem thu:\"");

AddHeading2("1. Bao mat - BCrypt Hash mat khau");
AddParagraph("Test dam bao mat khau duoc hash, khong bao gio luu plaintext vao database:");
AddCode(@"// Kiem tra mat khau KHONG phai plaintext
Assert.NotEqual(""plainpwd"", inserted.Password);

// Kiem tra mat khau duoc hash dung bang BCrypt
Assert.True(BCrypt.Net.BCrypt.Verify(""plainpwd"", inserted.Password));");

AddHeading2("2. Session & Phan quyen");
AddParagraph("Test dam bao Customer khong duoc xem don hang cua nguoi khac:");
AddCode(@"// Dang nhap voi UserId = 5
LoginAs(5, ""Customer"");

// Don hang #1 thuoc UserId = 99 (nguoi khac)
orderRepo.Setup(r => r.GetById(1))
         .Returns(new Order { UserId = 99 });

// Khi Customer xem don nguoi khac -> AccessDenied
var result = controller.Details(1);
Assert.Equal(""AccessDenied"", result.ActionName);");

AddHeading2("3. Xu ly bien (Edge case) - Rating");
AddParagraph("Test dam bao rating ngoai khoang 1-5 duoc xu ly dung:");
AddCode(@"// Rating = 0 (duoi min) -> clamp ve 5
controller.Submit(1, rating: 0, null);
// -> Rating duoc luu = 5

// Rating = 10 (tren max) -> clamp ve 5
controller.Submit(1, rating: 10, null);
// -> Rating duoc luu = 5");

AddHeading2("4. Exception Handling - Xu ly loi DB");
AddParagraph("Test dam bao khi database loi, controller bat exception va hien thi thong bao than thien:");
AddCode(@"// Gia lap DB throw exception
repo.Setup(r => r.Insert(It.IsAny<Coupon>()))
    .Throws(new Exception(""DB loi""));

// Goi controller
controller.Create(""CODE"", 20, DateTime.Now.AddDays(30));

// Kiem tra: redirect Index + hien thi loi (khong crash)
Assert.Equal(""Index"", result.ActionName);
Assert.NotNull(controller.TempData[""Error""]);");

AddHeading2("5. Gio hang - Ton kho");
AddParagraph("Test dam bao khong the them so luong vuot ton kho:");
AddCode(@"// Ton kho chi con 2
fruitRepo.Setup(r => r.GetById(1))
         .Returns(new Fruit { StockQuantity = 2 });

// Them 5 san pham -> bi tu choi
var result = controller.AddToCart(1, qty: 5);
Assert.Equal(""Details"", result.ActionName); // redirect + bao loi");

// === PHAN 6: KET QUA & KET LUAN (2 phut) ===
AddPageBreak();
AddHeading1("PHAN 6: KET QUA & KET LUAN (2 phut)");
AddSeparator();

AddQuote("\"Tong ket lai ket qua kiem thu:\"");

AddTable(new[] {
    new[] { "Chi so", "Gia tri" },
    new[] { "Tong test case", "225" },
    new[] { "Pass", "225 (100%)" },
    new[] { "Fail", "0" },
    new[] { "Tong module", "11 Controller" },
    new[] { "Thoi gian chay", "~2 giay" },
    new[] { "Tu dong hoa", "100%" },
});

AddParagraph("Chi tiet theo module:", bold: true);
AddTable(new[] {
    new[] { "STT", "Module", "So TC", "Pass", "Fail" },
    new[] { "1", "AccountController", "24", "24", "0" },
    new[] { "2", "HomeController", "20", "20", "0" },
    new[] { "3", "CategoryController", "20", "20", "0" },
    new[] { "4", "FruitController", "21", "21", "0" },
    new[] { "5", "OrderController", "20", "20", "0" },
    new[] { "6", "UserController", "20", "20", "0" },
    new[] { "7", "DashboardController", "20", "20", "0" },
    new[] { "8", "CouponController", "20", "20", "0" },
    new[] { "9", "InventoryController", "20", "20", "0" },
    new[] { "10", "ReviewController", "20", "20", "0" },
    new[] { "11", "WishlistController", "20", "20", "0" },
    new[] { "", "TONG CONG", "225", "225", "0" },
});

AddQuote("\"Toan bo test case da duoc xuat ra file Excel theo template chuan (TestCase_FruitShop.xlsx), bao gom Cover page, chi tiet tung module, va Test Report tong hop.\"");

AddQuote("\"Qua qua trinh kiem thu, em dam bao he thong xu ly dung cac truong hop: du lieu hop le, du lieu loi, ngoai le tu database, phan quyen va bao mat mat khau.\"");

// === PHAN 7: CAU HOI DU DOAN & CACH TRA LOI ===
AddPageBreak();
AddHeading1("PHAN 7: CAU HOI DU DOAN & CACH TRA LOI");
AddSeparator();

AddHeading2("Q1: Tai sao dung xUnit ma khong dung NUnit hay MSTest?");
AddQuote("\"xUnit la framework moi nhat, duoc Microsoft khuyen nghi cho .NET Core/5+. Thiet ke hien dai hon, moi test chay doc lap tren instance rieng, tranh chia se state giua cac test. NUnit va MSTest la framework cu hon, thiet ke cho .NET Framework.\"");

AddHeading2("Q2: Mock co dam bao test dung khi chay DB that khong?");
AddQuote("\"Unit Test voi Mock kiem tra logic cua Controller - dam bao Controller xu ly dung khi nhan du lieu tu Repository. De dam bao query SQL dung, can them Integration Test ket noi DB that. Trong du an nay, cac SQL query dung Dapper voi Parameterized Query da duoc kiem tra rieng.\"");

AddHeading2("Q3: Tai sao Repository method phai danh dau virtual?");
AddQuote("\"Moq tao proxy class ke thua tu class goc, no can override method de gia lap hanh vi. Neu khong co virtual, Moq khong the override duoc va se goi method that -> dan den can ket noi DB that. Danh dau virtual cho phep Moq 'chan' method va tra ve du lieu gia.\"");

AddHeading2("Q4: 225 test case da du chua?");
AddQuote("\"225 test case phu het cac chuc nang chinh bao gom: happy path, validation, phan quyen va xu ly exception. De tang coverage co the bo sung them: Integration Test (test DB that), Performance Test (test hieu nang), va UI Test voi Selenium WebDriver cho cac luong nghiep vu quan trong.\"");

AddHeading2("Q5: FakeSession la gi? Tai sao can no?");
AddQuote("\"ASP.NET Core quan ly Session qua interface ISession. Trong moi truong test khong co HttpContext that (khong co web server), nen em tu viet FakeSession implement ISession. FakeSession luu data trong Dictionary<string, byte[]> thay vi luu tren server, giup test chay nhanh va doc lap.\"");

AddHeading2("Q6: Tai sao test chay nhanh vay (2 giay cho 225 test)?");
AddQuote("\"Vi toan bo test deu dung Mock - khong ket noi database, khong goi network, khong doc file. Tat ca du lieu deu duoc gia lap trong memory. Day la dac diem quan trong nhat cua Unit Test: nhanh, doc lap, lap lai duoc.\"");

AddHeading2("Q7: Lam sao biet test du coverage?");
AddQuote("\"Moi Controller action deu co it nhat 1 test happy path va 1 test error case. Cac method quan trong (Login, Register, Checkout, UpdateStatus) co nhieu test hon cho boundary case. Co the dung cong cu nhu coverlet/dotnet-coverage de do line coverage chinh xac.\"");

// === FOOTER ===
body.AppendChild(new Paragraph(new ParagraphProperties(new SpacingBetweenLines { Before = "480" })));
AddSeparator();
AddParagraph("Thoi luong du kien: ~15 phut (bao gom demo)", italic: true, fontSize: "22", color: "666666");
AddParagraph($"Tai lieu lap ngay: {DateTime.Now:dd/MM/yyyy}", italic: true, fontSize: "22", color: "666666");

body.AppendChild(sectPr);

Console.WriteLine($"Done! File saved: {outputPath}");
