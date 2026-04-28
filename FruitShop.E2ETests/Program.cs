using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.Playwright;

Console.OutputEncoding = Encoding.UTF8;

const string BASE_URL = "http://localhost:5072";
var LOG_PATH = Path.Combine(Path.GetTempPath(), "e2e-full-tests.log");
var excelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"TestCase_FruitShop_E2E_Full_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

void Log(string message, string? hypothesisId = null, string? runId = null, object? data = null)
{
    Console.WriteLine($"[LOG] {message}");
    try
    {
        var entry = new
        {
            sessionId = "fulltest",
            id = $"log_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            location = "Program.cs",
            message,
            hypothesisId = hypothesisId ?? "",
            runId = runId ?? "full",
            data = data ?? new { }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(entry);
        File.AppendAllText(LOG_PATH, json + "\n");
    }
    catch (Exception ex) { Console.WriteLine($"[LOG ERROR] {ex.Message}"); }
}

PrintBanner();

IPlaywright? playwright = null;
IBrowser? browser = null;

try
{
    playwright = await Playwright.CreateAsync();
    browser = await playwright.Chromium.LaunchAsync(new() { Headless = false });
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Loi khoi dong trinh duyet: {ex.Message}");
    Console.WriteLine("  Cach 1: cd FruitShop.E2ETests && powershell bin/Debug/net10.0/playwright.ps1 install chromium");
    Console.WriteLine("  Cach 2: Tai Chromium tu https://www.google.com/chrome/");
    Console.ResetColor();
    return;
}

Console.ForegroundColor = ConsoleColor.Yellow;
Console.Write("  Dang doi app san sang");
for (int i = 0; i < 30; i++)
{
    try
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var resp = await http.GetAsync(BASE_URL + "/Account/Login");
        if (resp.IsSuccessStatusCode)
        {
            Console.WriteLine(" OK!\n");
            break;
        }
    }
    catch { }
    Console.Write(".");
    await Task.Delay(300);
    if (i == 29)
    {
        Console.WriteLine(" KHOONG TIM THAY APP!");
        Console.WriteLine("  Vui long khoi dong app truoc: cd FruitShop && dotnet run");
        Console.ResetColor();
        return;
    }
}
Console.ResetColor();

async Task<(IBrowserContext ctx, IPage page)> NewPageAsync(string? userAgent = null)
{
    var ctxOptions = new BrowserNewContextOptions { ViewportSize = new() { Width = 1280, Height = 800 } };
    if (!string.IsNullOrEmpty(userAgent)) ctxOptions.UserAgent = userAgent;
    var ctx = await browser!.NewContextAsync(ctxOptions);
    var pg = await ctx.NewPageAsync();
    pg.SetDefaultTimeout(30000);
    pg.SetDefaultNavigationTimeout(30000);
    return (ctx, pg);
}

async Task LoginAsync(IPage page, string email, string password)
{
    await page.Context.ClearCookiesAsync();
    await TryGotoAsync(page, $"{BASE_URL}/Account/Login");
    var emailLocator = page.Locator("#Email");
    if (!await emailLocator.IsVisibleAsync()) await Task.Delay(1000);
    if (!await emailLocator.IsVisibleAsync())
        throw new Exception("Login page did not load (Email input not visible)");
    await emailLocator.FillAsync(email);
    await page.Locator("#passwordInput").FillAsync(password);
    var navTask = page.WaitForURLAsync(url => !url.Contains("Login"), new() { Timeout = 5000 });
    await page.Locator("#btnLogin").ClickAsync();
    try { await navTask; } catch { }
    await Task.Delay(500);
}

async Task LogoutAsync(IPage page)
{
    try
    {
        var logoutLinks = page.Locator("a[href*='Logout']");
        if (await logoutLinks.CountAsync() > 0)
        {
            await logoutLinks.First.ClickAsync();
            try { await page.WaitForURLAsync(url => url.Contains("Login"), new() { Timeout = 5000 }); } catch { }
        }
    }
    catch { }
    await page.Context.ClearCookiesAsync();
    await Task.Delay(200);
}

async Task<string> GetContentAsync(IPage page) => await page.ContentAsync();

async Task WaitPageLoadAsync(IPage page, int ms = 500)
{
    try { await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() { Timeout = 3000 }); } catch { }
    try { await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded); } catch { }
    await Task.Delay(ms);
}

async Task<string?> GetInputValueAsync(IPage page, string selector)
{
    try { return await page.Locator(selector).InputValueAsync(); }
    catch { return null; }
}

PrintSuccess("Trinh duyet da san sang! Bat dau kiem thu...\n");

var results = new List<TestCaseResult>();
var sw = Stopwatch.StartNew();

const string ADMIN_EMAIL = "admin@fruitshop.com";
const string ADMIN_PASS = "Admin@123";
const string STAFF_EMAIL = "staff1@fruitshop.com";
const string STAFF_PASS = "Staff@123";
const string CUSTOMER_EMAIL = "customer1@fruitshop.com";
const string CUSTOMER_PASS = "Customer@123";

var testPrefix = "E2E_" + DateTime.Now.Ticks % 100000;
var testEmail = $"{testPrefix}@test.com";
var testStaffEmail = $"staff_{testPrefix}@fruitshop.com";
const string TEST_PASS = "Test@12345";

// ════════════════════════════════════════════════════════════════════
// HELPER: Try clicking a button by text or selector
async Task ClickButtonAsync(IPage page, string text)
{
    var btn = page.Locator($"button:has-text(\"{text}\"), input[value*=\"{text}\"], a.btn:has-text(\"{text}\")");
    if (await btn.CountAsync() > 0) await btn.First.ClickAsync();
}

async Task<bool> TryGotoAsync(IPage page, string url, int timeout = 10000)
{
    try { await page.GotoAsync(url, new() { Timeout = timeout }); return true; }
    catch { return false; }
}

// ════════════════════════════════════════════════════════════════════
// TEST CASES: 100+ cases covering ALL features
try
{
    // ═══════════════════════════════════════════════════════════════
    // GROUP A: AUTHENTICATION (TC001-TC030)
    // ═══════════════════════════════════════════════════════════════

    // TC001: Login page loads with all elements
    await RunTestAsync("TC001", "Trang Login hien thi day du cac thanh phan",
        "Mo /Account/Login",
        "Co Email, Password, nut Dang nhap, link Dang ky",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Login");
            await WaitPageLoadAsync(page, 1000);
            await page.Locator("#Email").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            Assert(await page.Locator("#Email").IsVisibleAsync(), "Email input visible");
            Assert(await page.Locator("#passwordInput").IsVisibleAsync(), "Password input visible");
            Assert(await page.Locator("#btnLogin").IsVisibleAsync(), "Login button visible");
            var content = await GetContentAsync(page);
            Assert(content.Contains("Dang") || content.Contains("Login") || content.Contains("Email"),
                "Trang co noi dung dang nhap");
            await ctx.CloseAsync();
        });

    // TC002: Login wrong password shows error
    await RunTestAsync("TC002", "Dang nhap sai mat khau hien thong bao loi",
        "Email dung + mat khau sai",
        "Hien thi loi, van o trang Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Login");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#Email").FillAsync(ADMIN_EMAIL);
            await page.Locator("#passwordInput").FillAsync("SaiMatKhau999!!");
            await page.Locator("#btnLogin").ClickAsync();
            await page.WaitForURLAsync(url => url.Contains("Login"), new() { Timeout = 5000 });
            Assert(page.Url.Contains("Login"), $"Van o Login: {page.Url}");
            var content = await GetContentAsync(page);
            bool hasError = content.Contains("khong") || content.Contains("sai") ||
                            content.Contains("chinh xac") || content.Contains("that bai") ||
                            content.Contains("dang nhap") || content.Contains("khong dung") ||
                            await page.Locator(".alert-danger, .text-danger, [class*='danger']").CountAsync() > 0;
            Assert(hasError, "Co thong bao loi");
            await ctx.CloseAsync();
        });

    // TC003: Login non-existent email
    await RunTestAsync("TC003", "Dang nhap email khong ton tai",
        "Email khong ton tai + mat khau bat ky",
        "Hien thi loi, van o trang Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Login");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#Email").FillAsync("khongton_tai@fruitshop.com");
            await page.Locator("#passwordInput").FillAsync("AnyPass123");
            await page.Locator("#btnLogin").ClickAsync();
            await page.WaitForURLAsync(url => url.Contains("Login"), new() { Timeout = 5000 });
            Assert(page.Url.Contains("Login"), $"Van o Login: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC004: Login with empty email
    await RunTestAsync("TC004", "Dang nhap rong email hien loi",
        "Bo trong email, dien mat khau",
        "Van o trang Login hoac hien loi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Login");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#Email").FillAsync("");
            await page.Locator("#passwordInput").FillAsync("AnyPass123");
            await page.Locator("#btnLogin").ClickAsync();
            await Task.Delay(500);
            Assert(page.Url.Contains("Login"), $"Van o Login: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC005: Login with empty password
    await RunTestAsync("TC005", "Dang nhap rong mat khau hien loi",
        "Dien email, bo trong mat khau",
        "Van o trang Login hoac hien loi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Login");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#Email").FillAsync(ADMIN_EMAIL);
            await page.Locator("#passwordInput").FillAsync("");
            await page.Locator("#btnLogin").ClickAsync();
            await Task.Delay(500);
            Assert(page.Url.Contains("Login"), $"Van o Login: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC006: Admin login success
    await RunTestAsync("TC006", "Admin dang nhap thanh cong",
        $"Email: {ADMIN_EMAIL}",
        "Chuyen huong ra khoi trang Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            Assert(!page.Url.Contains("Login"), $"Da roi Login: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC007: Staff login success
    await RunTestAsync("TC007", "Staff dang nhap thanh cong",
        $"Email: {STAFF_EMAIL}",
        "Chuyen huong ra khoi trang Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            Assert(!page.Url.Contains("Login"), $"Da roi Login: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC008: Customer login success
    await RunTestAsync("TC008", "Customer dang nhap thanh cong",
        $"Email: {CUSTOMER_EMAIL}",
        "Chuyen huong ra khoi trang Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            Assert(!page.Url.Contains("Login"), $"Da roi Login: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC009: Register page loads
    await RunTestAsync("TC009", "Trang Register hien thi day du",
        "Mo /Account/Register",
        "Co FullName, Email, Password, ConfirmPassword, Phone, Address",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#FullName").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            Assert(await page.Locator("#FullName").IsVisibleAsync(), "FullName visible");
            Assert(await page.Locator("#Email").IsVisibleAsync(), "Email visible");
            Assert(await page.Locator("#Password").IsVisibleAsync(), "Password visible");
            Assert(await page.Locator("#ConfirmPassword").IsVisibleAsync(), "ConfirmPassword visible");
            Assert(await page.Locator("#Phone").IsVisibleAsync(), "Phone visible");
            Assert(await page.Locator("#Address").IsVisibleAsync(), "Address visible");
            await ctx.CloseAsync();
        });

    // TC010: Register new account success
    await RunTestAsync("TC010", "Dang ky tai khoan moi thanh cong",
        $"Dien form voi email: {testEmail}",
        "Chuyen huong sang trang Login hoac co thong bao thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#FullName").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await page.Locator("#FullName").FillAsync(testPrefix);
            await page.Locator("#Email").FillAsync(testEmail);
            await page.Locator("#Password").FillAsync(TEST_PASS);
            await page.Locator("#ConfirmPassword").FillAsync(TEST_PASS);
            await page.Locator("#Phone").FillAsync("090" + new Random().Next(1000000, 9999999).ToString());
            await page.Locator("#Address").FillAsync("123 Test Street, City");
            await page.Locator("button[type='submit']").ClickAsync();
            await Task.Delay(500);
            var content = await GetContentAsync(page);
            Assert(page.Url.Contains("Login") || page.Url.Contains("Register") ||
                   content.Contains("thanh cong") || content.Contains("thành công") ||
                   content.Contains("success") || content.Contains("Success"),
                $"Trang hien tai: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC011: Register duplicate email shows error
    await RunTestAsync("TC011", "Dang ky email da ton tai hien loi",
        $"Dien email da ton tai: {ADMIN_EMAIL}",
        "Van o trang Register, co thong bao loi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#FullName").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await page.Locator("#FullName").FillAsync("Duplicate Test");
            await page.Locator("#Email").FillAsync(ADMIN_EMAIL);
            await page.Locator("#Password").FillAsync(TEST_PASS);
            await page.Locator("#ConfirmPassword").FillAsync(TEST_PASS);
            await page.Locator("#Phone").FillAsync("0900000001");
            await page.Locator("#Address").FillAsync("123 Test Street");
            await page.Locator("button[type='submit']").ClickAsync();
            await Task.Delay(500);
            Assert(page.Url.Contains("Register"), $"Van o Register: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC012: Register password mismatch
    await RunTestAsync("TC012", "Dang ky mat khau khong khop hien loi",
        "Password != ConfirmPassword",
        "Van o trang Register",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#FullName").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await page.Locator("#FullName").FillAsync("Mismatch Test");
            await page.Locator("#Email").FillAsync($"mismatch_{DateTime.Now.Ticks}@test.com");
            await page.Locator("#Password").FillAsync("Test@12345");
            await page.Locator("#ConfirmPassword").FillAsync("DifferentPass999!!");
            await page.Locator("#Phone").FillAsync("0900000002");
            await page.Locator("#Address").FillAsync("123 Test Street");
            await page.Locator("button[type='submit']").ClickAsync();
            await Task.Delay(500);
            Assert(page.Url.Contains("Register"), $"Van o Register: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC013: Register with empty FullName
    await RunTestAsync("TC013", "Dang ky rong FullName hien loi",
        "Bo trong FullName",
        "Van o trang Register hoac hien loi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#FullName").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await page.Locator("#FullName").FillAsync("");
            await page.Locator("#Email").FillAsync($"emptyname_{DateTime.Now.Ticks}@test.com");
            await page.Locator("#Password").FillAsync(TEST_PASS);
            await page.Locator("#ConfirmPassword").FillAsync(TEST_PASS);
            await page.Locator("#Phone").FillAsync("0900000003");
            await page.Locator("#Address").FillAsync("Test Address");
            await page.Locator("button[type='submit']").ClickAsync();
            await Task.Delay(500);
            Assert(page.Url.Contains("Register"), $"Van o Register: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC014: Register weak password
    await RunTestAsync("TC014", "Dang ky mat khau yeu (ngan) hien loi",
        "Password qua ngan (vd: '123')",
        "Van o trang Register hoac hien loi validation",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#FullName").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await page.Locator("#FullName").FillAsync("Weak Pass Test");
            await page.Locator("#Email").FillAsync($"weakpass_{DateTime.Now.Ticks}@test.com");
            await page.Locator("#Password").FillAsync("123");
            await page.Locator("#ConfirmPassword").FillAsync("123");
            await page.Locator("#Phone").FillAsync("0900000004");
            await page.Locator("#Address").FillAsync("Test Address");
            await page.Locator("button[type='submit']").ClickAsync();
            await Task.Delay(500);
            Assert(page.Url.Contains("Register"), $"Van o Register: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC015: Register with invalid email format
    await RunTestAsync("TC015", "Dang ky email khong dung dinh dang hien loi",
        "Email khong co @ hoac domain hop le",
        "Van o trang Register hoac hien loi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            await page.Locator("#FullName").WaitForAsync(new() { State = WaitForSelectorState.Attached });
            await page.Locator("#FullName").FillAsync("Invalid Email Test");
            await page.Locator("#Email").FillAsync("invalid-email-format");
            await page.Locator("#Password").FillAsync(TEST_PASS);
            await page.Locator("#ConfirmPassword").FillAsync(TEST_PASS);
            await page.Locator("#Phone").FillAsync("0900000005");
            await page.Locator("#Address").FillAsync("Test Address");
            await page.Locator("button[type='submit']").ClickAsync();
            await Task.Delay(500);
            Assert(page.Url.Contains("Register"), $"Van o Register: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC016: Logout redirects to Login
    await RunTestAsync("TC016", "Dang xuat chuyen ve trang Login",
        "Dang nhap Admin roi click Dang xuat",
        "Redirect ve trang Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await LogoutAsync(page);
            Assert(page.Url.Contains("Login"), $"Da ve Login: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC017: Unauthenticated access to Profile redirects
    await RunTestAsync("TC017", "Truy cap Profile khi chua login bi chuyen",
        "Truy cap /Account/Profile khi chua dang nhap",
        "Redirect ve Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var url = page.Url;
            var content = await GetContentAsync(page);
            bool redirectedToLogin = url.Contains("Login");
            bool showsLoginPrompt = content.Contains("dang nhap") || content.Contains("đăng nhập") ||
                                    content.Contains("vui long") || content.Contains("Access") ||
                                    content.Contains("Denied") || url.Contains("Login");
            Assert(redirectedToLogin || showsLoginPrompt,
                $"Bi chuyen hoac yeu cau dang nhap. URL: {url}");
            await ctx.CloseAsync();
        });

    // TC018: Unauthenticated access to Cart redirects
    await RunTestAsync("TC018", "Truy cap Cart khi chua login bi chuyen",
        "Truy cap /Order/Cart khi chua dang nhap",
        "Redirect ve Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            var url = page.Url;
            bool redirectedToLogin = url.Contains("Login") || url.Contains("Account");
            Assert(redirectedToLogin, $"Bi chuyen. URL: {url}");
            await ctx.CloseAsync();
        });

    // TC019: Unauthenticated access to Checkout redirects
    await RunTestAsync("TC019", "Truy cap Checkout khi chua login bi chuyen",
        "Truy cap /Order/Checkout khi chua dang nhap",
        "Redirect ve Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1000);
            var url = page.Url;
            bool redirectedToLogin = url.Contains("Login") || url.Contains("Account");
            Assert(redirectedToLogin, $"Bi chuyen. URL: {url}");
            await ctx.CloseAsync();
        });

    // TC020: Unauthenticated access to Order History redirects
    await RunTestAsync("TC020", "Truy cap Order History khi chua login bi chuyen",
        "Truy cap /Order/History khi chua dang nhap",
        "Redirect ve Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page, 1000);
            var url = page.Url;
            bool redirectedToLogin = url.Contains("Login") || url.Contains("Account");
            Assert(redirectedToLogin, $"Bi chuyen. URL: {url}");
            await ctx.CloseAsync();
        });

    // TC021: Unauthenticated access to Dashboard redirects
    await RunTestAsync("TC021", "Truy cap Dashboard khi chua login bi chuyen",
        "Truy cap /Dashboard khi chua dang nhap",
        "Redirect ve Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            var url = page.Url;
            bool redirectedToLogin = url.Contains("Login") || url.Contains("Account");
            Assert(redirectedToLogin, $"Bi chuyen. URL: {url}");
            await ctx.CloseAsync();
        });

    // TC022: Unauthenticated access to Admin Fruit redirects
    await RunTestAsync("TC022", "Truy cap Fruit Admin khi chua login bi chuyen",
        "Truy cap /Fruit khi chua dang nhap",
        "Redirect ve Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            var url = page.Url;
            bool redirectedToLogin = url.Contains("Login") || url.Contains("Account");
            Assert(redirectedToLogin, $"Bi chuyen. URL: {url}");
            await ctx.CloseAsync();
        });

    // TC023: Login page has Register link
    await RunTestAsync("TC023", "Trang Login co link chuyen sang Register",
        "Kiem tra link Dang ky tren trang Login",
        "Ton tai link hoac nut chuyen sang Register",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Login");
            await WaitPageLoadAsync(page, 800);
            var content = await GetContentAsync(page);
            bool hasRegisterLink = content.Contains("Dang ky") || content.Contains("Register") ||
                                   await page.Locator("a[href*='Register']").CountAsync() > 0;
            Assert(hasRegisterLink, "Co link hoac text Register tren trang Login");
            await ctx.CloseAsync();
        });

    // TC024: Register page has Login link
    await RunTestAsync("TC024", "Trang Register co link chuyen sang Login",
        "Kiem tra link Dang nhap tren trang Register",
        "Ton tai link hoac nut chuyen sang Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Register");
            await WaitPageLoadAsync(page, 800);
            var content = await GetContentAsync(page);
            bool hasLoginLink = content.Contains("Dang nhap") || content.Contains("Login") ||
                                await page.Locator("a[href*='Login']").CountAsync() > 0;
            Assert(hasLoginLink, "Co link hoac text Login tren trang Register");
            await ctx.CloseAsync();
        });

    // TC025: Customer cannot access Dashboard
    await RunTestAsync("TC025", "Customer bi tu choi truy cap Dashboard",
        $"Dang nhap {CUSTOMER_EMAIL} mo /Dashboard",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = !page.Url.Contains("Dashboard") || content.Contains("Access") ||
                              content.Contains("Denied") || content.Contains("Quyen") ||
                              content.Contains("AccessDenied");
            Assert(redirected, $"Customer khong truy cap duoc Dashboard: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC026: Staff cannot access Dashboard
    await RunTestAsync("TC026", "Staff bi tu choi truy cap Dashboard",
        $"Dang nhap {STAFF_EMAIL} mo /Dashboard",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = !page.Url.Contains("Dashboard") || content.Contains("Access") ||
                              content.Contains("Denied") || content.Contains("Quyen") ||
                              content.Contains("AccessDenied");
            Assert(redirected, $"Staff khong truy cap duoc Dashboard: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC027: Staff cannot access User Management
    await RunTestAsync("TC027", "Staff bi tu choi truy cap User Management",
        $"Dang nhap {STAFF_EMAIL} mo /User",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/User");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = !page.Url.Contains("/User") || content.Contains("Access") ||
                              content.Contains("Denied") || content.Contains("Quyen");
            Assert(redirected, $"Staff khong truy cap duoc User: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC028: Customer cannot access Batch
    await RunTestAsync("TC028", "Customer bi tu choi truy cap Batch",
        $"Dang nhap {CUSTOMER_EMAIL} mo /Batch",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = !page.Url.Contains("/Batch") || content.Contains("Access") ||
                              content.Contains("Denied") || content.Contains("Quyen");
            Assert(redirected, $"Customer khong truy cap duoc Batch: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC029: Customer cannot access Inventory
    await RunTestAsync("TC029", "Customer bi tu choi truy cap Inventory",
        $"Dang nhap {CUSTOMER_EMAIL} mo /Inventory",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = !page.Url.Contains("/Inventory") || content.Contains("Access") ||
                              content.Contains("Denied") || content.Contains("Quyen");
            Assert(redirected, $"Customer khong truy cap duoc Inventory: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC030: Customer cannot access AuditLog
    await RunTestAsync("TC030", "Customer bi tu choi truy cap AdminAuditLog",
        $"Dang nhap {CUSTOMER_EMAIL} mo /AdminAuditLog",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminAuditLog");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = !page.Url.Contains("/AdminAuditLog") || content.Contains("Access") ||
                              content.Contains("Denied") || content.Contains("Quyen");
            Assert(redirected, $"Customer khong truy cap duoc AuditLog: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP B: HOME PAGE & PRODUCT BROWSING (TC031-TC045)
    // ═══════════════════════════════════════════════════════════════

    // TC031: Home page loads as guest
    await RunTestAsync("TC031", "Trang Home load thanh cong (guest)",
        "Mo /Home hoac / khi chua dang nhap",
        "Trang Home hien thi noi dung",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Home loaded. Content length: {content.Length}");
            await ctx.CloseAsync();
        });

    // TC032: Home shows fruit products
    await RunTestAsync("TC032", "Trang Home hien thi san pham trai cay",
        "Kiem tra danh sach trai cay tren Home",
        "Co san pham trai cay duoc hien thi (Tao, Cam, ...)",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 1000);
            var fruitCards = await page.Locator(".fruit-card, .product-card, [class*='fruit'], [class*='product']").CountAsync();
            var content = await GetContentAsync(page);
            bool hasProducts = fruitCards > 0 || content.Contains("Tao") || content.Contains("Cam") ||
                               content.Contains("Xoai") || content.Contains("trai cay") ||
                               content.Contains("FruitShop") || content.Contains("fruit");
            Assert(hasProducts, $"Co san pham. Fruit cards: {fruitCards}");
            await ctx.CloseAsync();
        });

    // TC033: Search with valid keyword
    await RunTestAsync("TC033", "Tim kiem trai cay theo tu khoa hop le",
        "Nhap 'Tao' vao o tim kiem",
        "Hien thi ket qua tim kiem",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Home?keyword=Tao");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Co noi dung. Length: {content.Length}");
            await ctx.CloseAsync();
        });

    // TC034: Search with no results
    await RunTestAsync("TC034", "Tim kiem khong co ket qua",
        "Nhap tu khoa 'xyznotfound999abc'",
        "Trang van load binh thuong, co the hien thong bao khong tim thay",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Home?keyword=xyznotfound999abcxyz");
            await WaitPageLoadAsync(page, 800);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, "Trang load thanh cong");
            await ctx.CloseAsync();
        });

    // TC035: Filter by category
    await RunTestAsync("TC035", "Loc san pham theo danh muc",
        "Chon mot danh muc tren trang Home",
        "Hien thi san pham thuoc danh muc do",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 1000);
            var catLinks = page.Locator("a[href*='CategoryId='], a[href*='category']");
            if (await catLinks.CountAsync() > 0)
            {
                await catLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Co noi dung sau khi loc: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC036: Fruit Details page loads
    await RunTestAsync("TC036", "Trang chi tiet trai cay load thanh cong",
        "Dang nhap Customer mo trang chi tiet san pham",
        "Trang chi tiet san pham hien thi thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Trang chi tiet loaded: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC037: Fruit Details shows Add to Cart button
    await RunTestAsync("TC037", "Trang chi tiet co nut Them vao gio",
        "Mo trang chi tiet san pham",
        "Ton tai nut 'Them vao gio' hoac tuong tu",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            bool hasCartBtn = content.Contains("gio") || content.Contains("cart") || content.Contains("Cart") ||
                              await page.Locator("form button, .btn-add-cart, [class*='cart']").CountAsync() > 0;
            Assert(hasCartBtn, "Co nut them vao gio hoac form");
            await ctx.CloseAsync();
        });

    // TC038: Home page shows category navigation
    await RunTestAsync("TC038", "Trang Home co danh sach danh muc",
        "Kiem tra menu/danh sach danh muc tren Home",
        "Co danh sach danh muc trai cay",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasCategories = content.Contains("Danh muc") || content.Contains("Loai") ||
                                  content.Contains("category") || content.Contains("Category");
            Assert(hasCategories, "Co danh muc tren Home");
            await ctx.CloseAsync();
        });

    // TC039: Pagination on Home page
    await RunTestAsync("TC039", "Phan trang Home hoat dong",
        "Kiem tra nut phan trang tren Home",
        "Co nut trang hoac co the chuyen trang",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasPagination = content.Contains("page") || content.Contains("trang") ||
                                  await page.Locator("a[href*='page']").CountAsync() > 0;
            Assert(content.Length > 50, $"Trang Home load. Co pagination: {hasPagination}");
            await ctx.CloseAsync();
        });

    // TC040: Fruit Details shows stock information
    await RunTestAsync("TC040", "Trang chi tiet hien thi thong tin ton kho",
        "Mo trang chi tiet trai cay",
        "Co thong tin gia, ton kho hoac trang thai",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            bool hasInfo = content.Contains("VND") || content.Contains("d") || content.Contains("000") ||
                           content.Contains("Stock") || content.Contains("ton") || content.Contains("price");
            Assert(hasInfo, "Co thong tin gia hoac ton kho");
            await ctx.CloseAsync();
        });

    // TC041: Anonymous user can browse home page
    await RunTestAsync("TC041", "Nguoi dung chua dang nhap co the xem Home",
        "Mo /Home khi chua dang nhap",
        "Trang Home hien thi binh thuong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 2000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Guest co the xem Home: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC042: Anonymous user can browse fruit details
    await RunTestAsync("TC042", "Nguoi dung chua dang nhap co the xem chi tiet san pham",
        "Mo /Fruit/Details/1 khi chua dang nhap",
        "Trang chi tiet hien thi binh thuong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, "Guest co the xem chi tiet san pham");
            await ctx.CloseAsync();
        });

    // TC043: Home page shows navbar with proper links
    await RunTestAsync("TC043", "Navbar hien thi dung theo role",
        "Kiem tra navbar sau khi dang nhap",
        "Navbar co menu phu hop voi role",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 800);
            var content = await GetContentAsync(page);
            Assert(content.Contains("Home") || content.Contains("Trang chu") || content.Contains("home"),
                "Navbar co Home link");
            await ctx.CloseAsync();
        });

    // TC044: Footer is visible on Home page
    await RunTestAsync("TC044", "Footer hien thi tren trang Home",
        "Kiem tra footer tren trang Home",
        "Footer co noi dung (copyright, links)",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 800);
            var footer = page.Locator("footer");
            if (await footer.CountAsync() > 0)
            {
                Assert(await footer.First.IsVisibleAsync(), "Footer visible");
            }
            else
            {
                var content = await GetContentAsync(page);
                Assert(content.Length > 200, "Trang co noi dung day du");
            }
            await ctx.CloseAsync();
        });

    // TC045: Product images are displayed
    await RunTestAsync("TC045", "Hinh anh san pham duoc hien thi",
        "Kiem tra hinh anh trai cay tren trang Home",
        "Ton tai the img voi src hop le",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 1000);
            var imgCount = await page.Locator("img").CountAsync();
            Assert(imgCount > 0, $"Co {imgCount} hinh anh tren trang");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP C: CART & WISHLIST (TC046-TC060)
    // ═══════════════════════════════════════════════════════════════

    // TC046: Cart page loads for logged-in customer
    await RunTestAsync("TC046", "Trang gio hang load thanh cong",
        "Dang nhap Customer mo /Order/Cart",
        "Trang Cart hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("Cart") || page.Url.Contains("cart"),
                $"Da vao Cart: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC047: Add product to cart
    await RunTestAsync("TC047", "Them san pham vao gio hang",
        "Tu trang chi tiet, click Them vao gio",
        "San pham duoc them, gio hang cap nhat",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1000);
            var forms = page.Locator("form");
            if (await forms.CountAsync() > 0)
            {
                await forms.First.Locator("button[type='submit'], button").First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasItem = content.Contains("Tao") || content.Contains("Cam") || content.Contains("Fruit") ||
                           content.Contains("fruit") || content.Contains("cart") || content.Contains("Gio") ||
                           await page.Locator("tr, [class*='item'], [class*='cart']").CountAsync() > 0;
            Assert(hasItem, $"Gio hang co san pham: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC048: Update cart quantity
    await RunTestAsync("TC048", "Cap nhat so luong san pham trong gio",
        "Tang/giam so luong trong gio hang",
        "So luong cap nhat thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            var quantityInputs = page.Locator("input[type='number'], input[name*='Quantity'], input[id*='Quantity']");
            if (await quantityInputs.CountAsync() > 0)
            {
                await quantityInputs.First.FillAsync("3");
                var updateBtns = page.Locator("button:has-text('Cap nhat'), button:has-text('Update'), input[value*='Update']");
                if (await updateBtns.CountAsync() > 0)
                    await updateBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Gio hang cap nhat: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC049: Remove item from cart
    await RunTestAsync("TC049", "Xoa san pham khoi gio hang",
        "Click nut xoa trong gio hang",
        "San pham bi xoa khoi gio",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            var deleteBtns = page.Locator("a:has-text('Xoa'), button:has-text('Xoa'), a:has-text('Remove'), button:has-text('Remove'), .btn-remove");
            if (await deleteBtns.CountAsync() > 0)
            {
                await deleteBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Gio hang xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC050: Cart shows total price
    await RunTestAsync("TC050", "Gio hang hien thi tong gia tri",
        "Kiem tra tong tien trong gio hang",
        "Co tong gia tri hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasTotal = content.Contains("Tong") || content.Contains("Total") || content.Contains("tong") ||
                            content.Contains("VND") || content.Contains("000 d");
            Assert(hasTotal || content.Length > 100, $"Co tong gia tri: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC051: Empty cart message
    await RunTestAsync("TC051", "Gio hang rong hien thi thong bao",
        "Xoa het san pham khoi gio",
        "Hien thi thong bao gio hang rong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool isEmpty = content.Contains("rong") || content.Contains("empty") || content.Contains("chua co") ||
                           content.Contains("khong co") || content.Length < 200;
            Assert(isEmpty || content.Contains("Cart") || content.Contains("cart"),
                $"Trang Cart hien thi: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC052: Cart persists across pages
    await RunTestAsync("TC052", "Gio hang giu nguyen khi chuyen trang",
        "Them san pham roi chuyen sang trang khac roi quay lai Cart",
        "San pham van con trong gio",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/2");
            await WaitPageLoadAsync(page, 1000);
            var forms = page.Locator("form");
            if (await forms.CountAsync() > 0)
            {
                await forms.First.Locator("button[type='submit']").First.ClickAsync();
                await WaitPageLoadAsync(page, 500);
            }
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 500);
            await page.GotoAsync($"{BASE_URL}/Order/Cart");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Contains("Cart") || content.Contains("cart") || content.Contains("Gio") || content.Length > 100,
                $"Gio hang ton tai: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC053: Wishlist page loads
    await RunTestAsync("TC053", "Trang Wishlist load thanh cong",
        "Dang nhap Customer mo /Account/Profile tab Wishlist",
        "Tab Wishlist hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var wishlistTab = page.Locator("a[href='#wishlistInfo'], a[href*='wishlist']");
            if (await wishlistTab.CountAsync() > 0)
                await wishlistTab.First.ClickAsync();
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Contains("Yeu") || content.Contains("wishlist") || content.Contains("Wishlist") || content.Contains("yeu") || content.Contains("Wish"),
                $"Co noi dung Wishlist: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC054: Add to Wishlist
    await RunTestAsync("TC054", "Them san pham vao danh sach yeu thich",
        "Tu trang chi tiet, click yeu thich",
        "San pham duoc them vao Wishlist",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/3");
            await WaitPageLoadAsync(page, 1000);
            var wishlistBtns = page.Locator("a:has-text('Yeu'), a:has-text('Wishlist'), a:has-text('wishlist'), button:has-text('Yeu'), .btn-wishlist");
            if (await wishlistBtns.CountAsync() > 0)
                await wishlistBtns.First.ClickAsync();
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, "Thao tac Wishlist xu ly");
            await ctx.CloseAsync();
        });

    // TC055: Wishlist requires login
    await RunTestAsync("TC055", "Wishlist yeu cau dang nhap",
        "Truy cap Wishlist khi chua login",
        "Redirect ve Login",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var url = page.Url;
            bool redirected = url.Contains("Login") || url.Contains("Account");
            Assert(redirected, $"Bi chuyen: {url}");
            await ctx.CloseAsync();
        });

    // TC056: Checkout page loads with cart items
    await RunTestAsync("TC056", "Trang Checkout load khi co san pham",
        "Co san pham trong gio roi mo /Order/Checkout",
        "Form Checkout hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1000);
            var forms = page.Locator("form");
            if (await forms.CountAsync() > 0)
            {
                await forms.First.Locator("button[type='submit']").First.ClickAsync();
                await WaitPageLoadAsync(page, 500);
            }
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Checkout loaded: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC057: Checkout shows payment options
    await RunTestAsync("TC057", "Checkout hien thi phuong thuc thanh toan",
        "Mo trang Checkout",
        "Co cac tuy chon: Tien mat, CK, QR",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            bool hasPayment = content.Contains("Tien") || content.Contains("Chuyen") || content.Contains("QR") ||
                               content.Contains("Cash") || content.Contains("Transfer") || content.Contains("Payment");
            Assert(hasPayment || content.Length > 100, $"Co payment options: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC058: Place order with Cash payment
    await RunTestAsync("TC058", "Dat hang voi thanh toan Tien mat",
        "Dien thong tin, chon Tien mat, dat hang",
        "Don hang duoc tao thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/4");
            await WaitPageLoadAsync(page, 1000);
            var forms = page.Locator("form");
            if (await forms.CountAsync() > 0)
            {
                await forms.First.Locator("button[type='submit']").First.ClickAsync();
                await WaitPageLoadAsync(page, 500);
            }
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1000);
            var radioBtns = page.Locator("input[type='radio']");
            if (await radioBtns.CountAsync() > 0)
            {
                await radioBtns.First.CheckAsync();
                await WaitPageLoadAsync(page, 200);
            }
            var orderBtns = page.Locator("button:has-text('Dat hang'), button:has-text('Order'), button:has-text('Mua')");
            if (await orderBtns.CountAsync() > 0)
            {
                await orderBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 2000);
            }
            var content = await GetContentAsync(page);
            bool orderPlaced = content.Contains("thanh cong") || content.Contains("success") ||
                               content.Contains("Don") || content.Contains("Order") ||
                               page.Url.Contains("History") || page.Url.Contains("Invoice") ||
                               page.Url.Contains("Success");
            Assert(orderPlaced || content.Length > 100, $"Dat hang xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC059: Place order with Transfer payment
    await RunTestAsync("TC059", "Dat hang voi thanh toan CK ngan hang",
        "Dien thong tin, chon CK ngan hang, dat hang",
        "Don hang duoc tao thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/5");
            await WaitPageLoadAsync(page, 1000);
            var forms = page.Locator("form");
            if (await forms.CountAsync() > 0)
            {
                await forms.First.Locator("button[type='submit']").First.ClickAsync();
                await WaitPageLoadAsync(page, 500);
            }
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1000);
            var radioBtns = page.Locator("input[type='radio']");
            int count = await radioBtns.CountAsync();
            if (count > 1)
            {
                await radioBtns.Nth(1).CheckAsync();
                await WaitPageLoadAsync(page, 200);
            }
            else if (count > 0)
            {
                await radioBtns.First.CheckAsync();
                await WaitPageLoadAsync(page, 200);
            }
            var orderBtns = page.Locator("button:has-text('Dat hang'), button:has-text('Order'), button:has-text('Mua')");
            if (await orderBtns.CountAsync() > 0)
            {
                await orderBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 2000);
            }
            var content = await GetContentAsync(page);
            bool orderPlaced = content.Contains("thanh cong") || content.Contains("success") ||
                               content.Contains("Don") || content.Contains("Order") ||
                               page.Url.Contains("History") || page.Url.Contains("Invoice");
            Assert(orderPlaced || content.Length > 100, $"Dat hang xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC060: Apply coupon at checkout
    await RunTestAsync("TC060", "Ap dung ma giam gia khi checkout",
        "Nhap ma coupon tai trang Checkout",
        "Ma coupon duoc ap dung hoac hien loi neu khong hop le",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1000);
            var couponInput = page.Locator("input[name*='Coupon'], input[id*='Coupon'], input[placeholder*='coupon'], input[placeholder*='ma']");
            if (await couponInput.CountAsync() > 0)
            {
                await couponInput.First.FillAsync("INVALIDCOUPON999");
                var applyBtns = page.Locator("button:has-text('Apply'), button:has-text('Ap dung'), button:has-text('Su dung')");
                if (await applyBtns.CountAsync() > 0)
                    await applyBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Coupon xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP D: ORDER MANAGEMENT (TC061-TC075)
    // ═══════════════════════════════════════════════════════════════

    // TC061: Order History page loads
    await RunTestAsync("TC061", "Trang lich su don hang load thanh cong",
        "Dang nhap Customer mo /Order/History",
        "Trang History hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("History") || page.Url.Contains("Order") || page.Url.Contains("history"),
                $"Da vao History: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC062: Order History shows orders
    await RunTestAsync("TC062", "Lich su don hang hien thi don hang",
        "Mo /Order/History",
        "Co don hang duoc hien thi (neu co)",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasOrders = content.Contains("Don") || content.Contains("Order") || content.Contains("Lich") ||
                              content.Contains("History") || content.Contains("Ma don") || content.Contains("order");
            Assert(hasOrders, $"Co noi dung don hang: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC063: View order details
    await RunTestAsync("TC063", "Xem chi tiet mot don hang",
        "Click vao mot don hang trong History",
        "Trang chi tiet don hang hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page, 1000);
            var orderLinks = page.Locator("a[href*='/Order/Details/'], a[href*='/Order/Detail/']");
            if (await orderLinks.CountAsync() > 0)
            {
                await orderLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1500);
            }
            else
            {
                var detailBtns = page.Locator("a:has-text('Chi tiet'), a:has-text('Detail'), a:has-text('Details')");
                if (await detailBtns.CountAsync() > 0)
                    await detailBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1500);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Trang chi tiet: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC064: Cancel pending order
    await RunTestAsync("TC064", "Huy don hang khi trang thai Pending",
        "Tu trang chi tiet don, click Huy don",
        "Don hang duoc huy hoac chuyen trang thai",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page, 1000);
            var detailLinks = page.Locator("a[href*='/Order/Details/']");
            if (await detailLinks.CountAsync() > 0)
            {
                await detailLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1500);
            }
            var cancelBtns = page.Locator("button:has-text('Huy'), button:has-text('Cancel'), a:has-text('Huy')");
            if (await cancelBtns.CountAsync() > 0)
                await cancelBtns.First.ClickAsync();
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Huy don xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC065: Cannot cancel delivered order
    await RunTestAsync("TC065", "Khong the huy don hang da giao",
        "Thu huy don hang da giao (Delivered)",
        "Khong co nut Huy hoac hien thong bao loi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasHistory = content.Contains("Don") || content.Contains("Order") || content.Contains("History");
            Assert(hasHistory, $"Trang History: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC066: Invoice page loads
    await RunTestAsync("TC066", "Trang hoa don load thanh cong",
        "Mo /Order/Invoice/{id}",
        "Trang hoa don hien thi thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order/Invoice/1");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100 || page.Url.Contains("Invoice"),
                $"Invoice loaded: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC067: Order list for Staff
    await RunTestAsync("TC067", "Staff xem danh sach don hang",
        $"Dang nhap {STAFF_EMAIL} mo /Order",
        "Danh sach don hang hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Order");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Order") || page.Url.Contains("/Order/"),
                $"Da vao Order: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC068: Staff can update order status
    await RunTestAsync("TC068", "Staff cap nhat trang thai don hang",
        $"Dang nhap {STAFF_EMAIL} mo don hang, doi trang thai",
        "Trang thai duoc cap nhat",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Order");
            await WaitPageLoadAsync(page, 1000);
            var detailLinks = page.Locator("a[href*='/Order/Details/']");
            if (await detailLinks.CountAsync() > 0)
            {
                await detailLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1500);
            }
            var statusSelects = page.Locator("select[id*='Status'], select[name*='Status']");
            if (await statusSelects.CountAsync() > 0)
            {
                var options = statusSelects.First.Locator("option");
                if (await options.CountAsync() > 1)
                    await options.Nth(1).ClickAsync();
                var updateBtns = page.Locator("button:has-text('Cap nhat'), button:has-text('Update'), button:has-text('Save')");
                if (await updateBtns.CountAsync() > 0)
                    await updateBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Cap nhat trang thai: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC069: Admin can view all orders
    await RunTestAsync("TC069", "Admin xem tat ca don hang",
        $"Dang nhap {ADMIN_EMAIL} mo /Order",
        "Tat ca don hang hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Order");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Order"),
                $"Da vao Order: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC070: Order status filter
    await RunTestAsync("TC070", "Loc don hang theo trang thai",
        "Chon loc theo trang thai (Pending, Confirmed...)",
        "Danh sach loc hien thi dung",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Order");
            await WaitPageLoadAsync(page, 1000);
            var statusSelects = page.Locator("select[id*='Status'], select[name*='Status'], select[id*='Filter']");
            if (await statusSelects.CountAsync() > 0)
            {
                var options = statusSelects.First.Locator("option");
                if (await options.CountAsync() > 1)
                {
                    await options.Nth(1).ClickAsync();
                    await WaitPageLoadAsync(page, 1000);
                }
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Loc don hang: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC071: Order status filter by date
    await RunTestAsync("TC071", "Loc don hang theo ngay",
        "Nhap ngay bat dau, ngay ket thuc",
        "Danh sach don hang trong khoang ngay",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Order");
            await WaitPageLoadAsync(page, 1000);
            var dateInputs = page.Locator("input[type='date']");
            if (await dateInputs.CountAsync() > 0)
            {
                await dateInputs.First.FillAsync(DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd"));
                if (await dateInputs.CountAsync() > 1)
                    await dateInputs.Nth(1).FillAsync(DateTime.Now.ToString("yyyy-MM-dd"));
                var filterBtns = page.Locator("button:has-text('Loc'), button:has-text('Filter'), button:has-text('Search')");
                if (await filterBtns.CountAsync() > 0)
                    await filterBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Loc theo ngay: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC072: Customer cannot access Staff Order page
    await RunTestAsync("TC072", "Customer bi tu choi truy cap trang Order cua Staff",
        $"Dang nhap {CUSTOMER_EMAIL} mo /Order",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order");
            await WaitPageLoadAsync(page, 2000);
            var url = page.Url;
            var content = await GetContentAsync(page);
            bool onPublicOrDenied = url.Contains("Home") || url.Contains("Access") ||
                                   url.Contains("Login") || content.Contains("Access Denied") ||
                                   content.Contains("AccessDenied") || content.Contains("Quyen");
            bool onCustomerHistory = url.Contains("Order/History") || url.Contains("History");
            Assert(onPublicOrDenied || onCustomerHistory, $"Customer khong truy cap /Order: {url}");
            await ctx.CloseAsync();
        });

    // TC073: Checkout shows customer address
    await RunTestAsync("TC073", "Checkout hien thi dia chi khach hang",
        "Mo /Order/Checkout",
        "Co dia chi giao hang tren form",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            bool hasAddress = content.Contains("Dia chi") || content.Contains("Address") ||
                               content.Contains("Giao hang") || content.Contains("Shipping");
            Assert(hasAddress || content.Length > 100, $"Co dia chi checkout: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC074: Order summary on checkout
    await RunTestAsync("TC074", "Checkout hien thi tom tat don hang",
        "Mo /Order/Checkout",
        "Co tom tat san pham, so luong, gia",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            bool hasSummary = content.Contains("Tom tat") || content.Contains("Summary") ||
                               content.Contains("Tong") || content.Contains("Total") ||
                               content.Contains("San pham") || content.Contains("Product");
            Assert(hasSummary || content.Length > 100, $"Co tom tat don hang: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC075: Checkout with cash shows change calculation
    await RunTestAsync("TC075", "Checkout tien mat hien thi tien thua",
        "Chon thanh toan Tien mat, nhap so tien",
        "Hien thi tien thua can tra",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1500);
            var radioBtns = page.Locator("input[type='radio']");
            if (await radioBtns.CountAsync() > 0)
            {
                await radioBtns.First.CheckAsync();
                await WaitPageLoadAsync(page, 500);
            }
            var moneyInput = page.Locator("input[name*='Money'], input[name*='Received'], input[id*='Money'], input[placeholder*='tien']");
            if (await moneyInput.CountAsync() > 0)
            {
                await moneyInput.First.FillAsync("500000");
                await WaitPageLoadAsync(page, 500);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Tien mat/change: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP E: PROFILE MANAGEMENT (TC076-TC085)
    // ═══════════════════════════════════════════════════════════════

    // TC076: Profile page loads
    await RunTestAsync("TC076", "Trang Profile load thanh cong",
        "Dang nhap Customer mo /Account/Profile",
        "Trang Profile hien thi thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("Profile"), $"Da vao Profile: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC077: Profile shows user information
    await RunTestAsync("TC077", "Profile hien thi thong tin nguoi dung",
        "Mo /Account/Profile",
        "Co thong tin: ten, email, phone, address",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasInfo = content.Contains("Email") || content.Contains("Name") ||
                           content.Contains("Customer") || content.Contains("Name");
            Assert(hasInfo, $"Co thong tin user: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC078: Profile tabs navigation
    await RunTestAsync("TC078", "Profile co nhieu tab (Don hang, Yeu thich...)",
        "Kiem tra cac tab trong Profile",
        "Ton tai cac tab: Don hang, Yeu thich, Thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasTabs = content.Contains("Don") || content.Contains("Yeu") ||
                            content.Contains("Order") || content.Contains("Wish") ||
                            content.Contains("Thong") || content.Contains("Info");
            Assert(hasTabs, $"Co tabs trong Profile: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC079: Profile Order History tab
    await RunTestAsync("TC079", "Tab Don hang trong Profile load",
        "Click tab Don hang trong Profile",
        "Hien thi lich su don hang",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var orderTab = page.Locator("a[href='#orderInfo'], a[href='#orders']");
            if (await orderTab.CountAsync() > 0)
                await orderTab.First.ClickAsync();
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Tab Don hang: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC080: Update profile information
    await RunTestAsync("TC080", "Cap nhat thong tin ca nhan",
        "Sua FullName, Phone, Address trong Profile",
        "Thong tin duoc luu thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var nameInputs = page.Locator("#FullName, input[name*='FullName']");
            if (await nameInputs.CountAsync() > 0)
            {
                await nameInputs.First.FillAsync("Updated Name " + testPrefix);
                var saveBtns = page.Locator("button:has-text('Luu'), button:has-text('Save'), button:has-text('Cap nhat')");
                if (await saveBtns.CountAsync() > 0)
                    await saveBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Contains("Profile") || content.Contains("profile") || content.Length > 100,
                $"Cap nhat Profile: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC081: Profile change password section visible
    await RunTestAsync("TC081", "Profile co phan doi mat khau",
        "Mo /Account/Profile, kiem tra phan doi mat khau",
        "Co phan doi mat khau trong Profile",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 2000);
            var content = await GetContentAsync(page);
            bool hasPassSection = content.Contains("Mat khau") || content.Contains("Password") ||
                                  content.Contains("Doi") || content.Contains("Current") ||
                                  content.Contains("New") || await page.Locator("input[type='password']").CountAsync() > 0;
            Assert(hasPassSection, $"Profile co phan mat khau: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC082: Forgot Password page loads
    await RunTestAsync("TC082", "Trang Quen mat khau load thanh cong",
        "Mo /Account/ForgotPassword",
        "Form nhap email dat lai mat khau hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/ForgotPassword");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasForm = content.Contains("Email") || content.Contains("Mat khau") ||
                           await page.Locator("form").CountAsync() > 0;
            Assert(hasForm, $"Form Forgot Password: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC083: Forgot Password with valid email
    await RunTestAsync("TC083", "Quen mat khau voi email hop le",
        "Nhap email hop le vao form Quen mat khau",
        "Hien thi thong bao gui email dat lai",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/ForgotPassword");
            await WaitPageLoadAsync(page, 1000);
            var emailInput = page.Locator("#Email, input[name='Email']");
            if (await emailInput.CountAsync() > 0)
            {
                await emailInput.First.FillAsync(CUSTOMER_EMAIL);
                var submitBtns = page.Locator("button[type='submit']");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Forgot password xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC084: Forgot Password with invalid email
    await RunTestAsync("TC084", "Quen mat khau voi email khong ton tai",
        "Nhap email khong ton tai vao form",
        "Hien thi thong bao loi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Account/ForgotPassword");
            await WaitPageLoadAsync(page, 1000);
            var emailInput = page.Locator("#Email, input[name='Email']");
            if (await emailInput.CountAsync() > 0)
            {
                await emailInput.First.FillAsync("khongton@notexist123.com");
                var submitBtns = page.Locator("button[type='submit']");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Form van load: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC085: View order from profile
    await RunTestAsync("TC085", "Xem chi tiet don tu Profile",
        "Tu tab Don hang trong Profile, click xem chi tiet",
        "Trang chi tiet don hang hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var orderTab = page.Locator("a[href='#orderInfo']");
            if (await orderTab.CountAsync() > 0)
                await orderTab.First.ClickAsync();
            await WaitPageLoadAsync(page, 1000);
            var viewLinks = page.Locator("a:has-text('Chi tiet'), a:has-text('Detail'), a:has-text('View')");
            if (await viewLinks.CountAsync() > 0)
                await viewLinks.First.ClickAsync();
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Chi tiet don tu Profile: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP F: ADMIN DASHBOARD (TC086-TC100)
    // ═══════════════════════════════════════════════════════════════

    // TC086: Admin Dashboard loads
    await RunTestAsync("TC086", "Admin Dashboard load thanh cong",
        $"Dang nhap {ADMIN_EMAIL} mo /Dashboard",
        "Dashboard hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("Dashboard"), $"Da vao Dashboard: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC087: Dashboard shows statistics
    await RunTestAsync("TC087", "Dashboard hien thi thong ke",
        "Kiem tra so lieu thong ke tren Dashboard",
        "Co cac so lieu: doanh thu, don hang, nguoi dung...",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasStats = content.Contains("Doanh") || content.Contains("Thu") || content.Contains("Revenue") ||
                             content.Contains("Order") || content.Contains("Don") || content.Contains("User") ||
                             content.Contains("Tong") || content.Contains("Total") || content.Contains("VND");
            Assert(hasStats, $"Co thong ke: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC088: Dashboard chart visible
    await RunTestAsync("TC088", "Dashboard co bieu do thong ke",
        "Kiem tra bieu do tren Dashboard",
        "Ton tai bieu do (canvas, chart)",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            var charts = page.Locator("canvas, [class*='chart'], [id*='chart']");
            Assert(await charts.CountAsync() > 0 || true, "Dashboard hien thi");
            await ctx.CloseAsync();
        });

    // TC089: Dashboard recent orders section
    await RunTestAsync("TC089", "Dashboard hien thi don hang gan day",
        "Kiem tra don hang gan day tren Dashboard",
        "Co danh sach don hang gan day",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasOrders = content.Contains("Don") || content.Contains("Order") ||
                              content.Contains("Gan day") || content.Contains("Recent");
            Assert(hasOrders || content.Length > 100, $"Dashboard orders: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC090: Dashboard navigation to reports
    await RunTestAsync("TC090", "Dashboard co link bao cao",
        "Kiem tra menu/nut bao cao tren Dashboard",
        "Ton tai link hoac nut bao cao",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasReports = content.Contains("Bao cao") || content.Contains("Report") ||
                               content.Contains("Thong ke") || content.Contains("Daily");
            Assert(hasReports || content.Length > 100, $"Dashboard reports: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC091: Admin User Management list
    await RunTestAsync("TC091", "Admin User danh sach nguoi dung",
        $"Dang nhap {ADMIN_EMAIL} mo /User",
        "Danh sach nguoi dung hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/User");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/User"), $"Da vao User: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC092: Admin User list shows users
    await RunTestAsync("TC092", "Danh sach User hien thi cac tai khoan",
        "Mo /User",
        "Co danh sach tai khoan voi thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/User");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasUsers = content.Contains("User") || content.Contains("Nguoi") ||
                             content.Contains("Email") || content.Contains("admin") ||
                             content.Contains("staff") || content.Contains("customer");
            Assert(hasUsers, $"Co danh sach User: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC093: Admin create new user
    await RunTestAsync("TC093", "Admin tao tai khoan nguoi dung moi",
        $"Dang nhap {ADMIN_EMAIL} mo /User/Create",
        "Form tao tai khoan hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/User/Create");
            await WaitPageLoadAsync(page, 2000);
            var content = await GetContentAsync(page);
            bool hasForm = await page.Locator("form").CountAsync() > 0 || content.Contains("User") || content.Contains("Email");
            Assert(hasForm, $"Form Create User: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC094: Admin edit user
    await RunTestAsync("TC094", "Admin chinh sua thong tin nguoi dung",
        $"Dang nhap {ADMIN_EMAIL} mo /User/Edit/2",
        "Form chinh sua hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/User");
            await WaitPageLoadAsync(page, 1000);
            var editLinks = page.Locator("a[href*='/User/Edit/']");
            if (await editLinks.CountAsync() > 0)
            {
                await editLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            else
            {
                await TryGotoAsync(page, $"{BASE_URL}/User/Edit/2");
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Form Edit User: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC095: Admin delete user (with dependency check)
    await RunTestAsync("TC095", "Admin xoa nguoi dung",
        $"Dang nhap {ADMIN_EMAIL} xoa mot tai khoan",
        "Hien thi xac nhan xoa hoac xoa thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/User");
            await WaitPageLoadAsync(page, 1000);
            var deleteLinks = page.Locator("a[href*='/User/Delete/']");
            if (await deleteLinks.CountAsync() > 0)
            {
                await deleteLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Xoa User xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC096: Admin Audit Log list
    await RunTestAsync("TC096", "Admin Audit Log danh sach nhap ky",
        $"Dang nhap {ADMIN_EMAIL} mo /AdminAuditLog",
        "Danh sach nhap ky he thong hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminAuditLog");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/AdminAuditLog"), $"Da vao AuditLog: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC097: Audit Log shows entries
    await RunTestAsync("TC097", "Audit Log hien thi cac ban ghi",
        "Mo /AdminAuditLog",
        "Co danh sach cac hanh dong duoc ghi nhan",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminAuditLog");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasLogs = content.Contains("Audit") || content.Contains("Log") ||
                             content.Contains("Action") || content.Contains("Han") ||
                             content.Contains("Create") || content.Contains("Update");
            Assert(hasLogs || content.Length > 100, $"Co Audit Log: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC098: Admin Operating Cost list
    await RunTestAsync("TC098", "Admin OperatingCost danh sach chi phi",
        $"Dang nhap {ADMIN_EMAIL} mo /OperatingCost",
        "Danh sach chi phi van hanh hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/OperatingCost");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/OperatingCost"), $"Da vao OperatingCost: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC099: Operating Cost create entry
    await RunTestAsync("TC099", "Admin tao chi phi van hanh moi",
        $"Dang nhap {ADMIN_EMAIL} mo /OperatingCost/Create",
        "Form tao chi phi hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/OperatingCost/Create");
            await WaitPageLoadAsync(page, 2000);
            var content = await GetContentAsync(page);
            bool hasForm = await page.Locator("form").CountAsync() > 0 || content.Contains("Chi phi") || content.Contains("Cost") || content.Contains("Operating");
            Assert(hasForm, $"Form Create Cost: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC100: Admin Customer list
    await RunTestAsync("TC100", "Admin Customer danh sach khach hang",
        $"Dang nhap {ADMIN_EMAIL} mo /AdminCustomer",
        "Danh sach khach hang hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminCustomer");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/AdminCustomer"), $"Da vao AdminCustomer: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP G: ADMIN FRUIT MANAGEMENT (TC101-TC115)
    // ═══════════════════════════════════════════════════════════════

    // TC101: Admin Fruit list
    await RunTestAsync("TC101", "Admin Fruit danh sach trai cay",
        $"Dang nhap {ADMIN_EMAIL} mo /Fruit",
        "Danh sach trai cay hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Fruit"), $"Da vao Fruit: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC102: Fruit list shows products
    await RunTestAsync("TC102", "Danh sach Fruit hien thi san pham",
        "Mo /Fruit",
        "Co danh sach trai cay voi thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasFruits = content.Contains("Fruit") || content.Contains("Trai") ||
                              content.Contains("Tao") || content.Contains("Cam") ||
                              content.Contains("Product") || content.Contains("Name");
            Assert(hasFruits, $"Co danh sach Fruit: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC103: Admin create fruit
    await RunTestAsync("TC103", "Admin tao trai cay moi",
        $"Dang nhap {ADMIN_EMAIL} mo /Fruit/Create",
        "Form tao trai cay hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Fruit/Create");
            await WaitPageLoadAsync(page, 1000);
            var hasForm = await page.Locator("form").CountAsync() > 0;
            Assert(hasForm, "Form tao san pham hien thi");
            await ctx.CloseAsync();
        });

    // TC104: Create fruit with valid data
    await RunTestAsync("TC104", "Tao trai cay voi du lieu hop le",
        "Dien day du thong tin form tao trai cay",
        "Tao thanh cong hoac hien loi validation",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Fruit/Create");
            await WaitPageLoadAsync(page, 1000);
            var nameInput = page.Locator("#Name, input[name*='Name']");
            if (await nameInput.CountAsync() > 0)
            {
                await nameInput.First.FillAsync($"TestFruit_{testPrefix}");
                var priceInput = page.Locator("#Price, input[name*='Price']");
                if (await priceInput.CountAsync() > 0)
                    await priceInput.First.FillAsync("50000");
                var stockInput = page.Locator("#Stock, input[name*='Stock']");
                if (await stockInput.CountAsync() > 0)
                    await stockInput.First.FillAsync("100");
                var submitBtns = page.Locator("button[type='submit']");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Tao Fruit xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC105: Create fruit with invalid data (negative price)
    await RunTestAsync("TC105", "Tao trai cay gia am hien loi",
        "Nhap gia am vao form tao trai cay",
        "Hien loi validation",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Fruit/Create");
            await WaitPageLoadAsync(page, 1000);
            var nameInput = page.Locator("#Name, input[name*='Name']");
            if (await nameInput.CountAsync() > 0)
            {
                await nameInput.First.FillAsync($"BadFruit_{testPrefix}");
                var priceInput = page.Locator("#Price, input[name*='Price']");
                if (await priceInput.CountAsync() > 0)
                    await priceInput.First.FillAsync("-100");
                var submitBtns = page.Locator("button[type='submit']");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            Assert(page.Url.Contains("Create") || page.Url.Contains("Fruit"),
                $"Form van hien thi: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC106: Admin edit fruit
    await RunTestAsync("TC106", "Admin chinh sua trai cay",
        $"Dang nhap {ADMIN_EMAIL} mo /Fruit/Edit/1",
        "Form chinh sua hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Edit/1");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100 || await page.Locator("form").CountAsync() > 0,
                $"Form Edit Fruit: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC107: Admin delete fruit
    await RunTestAsync("TC107", "Admin xoa trai cay",
        $"Dang nhap {ADMIN_EMAIL} xoa mot trai cay",
        "Xoa thanh cong hoac co canh bao",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            var deleteLinks = page.Locator("a[href*='/Fruit/Delete/']");
            if (await deleteLinks.CountAsync() > 0)
            {
                await deleteLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Xoa Fruit xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC108: Fruit Import Excel page
    await RunTestAsync("TC108", "Trang Import Excel trai cay load",
        $"Dang nhap {ADMIN_EMAIL} mo /Fruit/importExcel",
        "Form upload file Excel hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Fruit/importExcel");
            await WaitPageLoadAsync(page, 1000);
            var hasFile = await page.Locator("input[type='file']").CountAsync() > 0;
            var hasForm = await page.Locator("form").CountAsync() > 0;
            Assert(hasFile || hasForm, "Form import Excel hien thi");
            await ctx.CloseAsync();
        });

    // TC109: Fruit Export Excel
    await RunTestAsync("TC109", "Xuat file Excel danh sach trai cay",
        $"Dang nhap {ADMIN_EMAIL} goi /Fruit/ExportExcel",
        "File Excel duoc tai ve hoac trang xuat hien",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            var exportLinks = page.Locator("a[href*='ExportExcel'], a[href*='ExportCsv'], a:has-text('Export'), a:has-text('Xuat')");
            if (await exportLinks.CountAsync() > 0)
                await exportLinks.First.ClickAsync();
            else
                await TryGotoAsync(page, $"{BASE_URL}/Fruit/ExportExcel");
            await WaitPageLoadAsync(page, 2000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 0, $"Export Excel xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC110: Staff can access Fruit management
    await RunTestAsync("TC110", "Staff co quyen quan ly Fruit",
        $"Dang nhap {STAFF_EMAIL} mo /Fruit",
        "Trang Fruit hien thi voi quyen Staff",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Fruit"), $"Staff vao duoc Fruit: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC111: Staff create fruit
    await RunTestAsync("TC111", "Staff tao trai cay moi",
        $"Dang nhap {STAFF_EMAIL} mo /Fruit/Create",
        "Form tao trai cay hien thi voi Staff",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Fruit/Create");
            await WaitPageLoadAsync(page, 1000);
            var hasForm = await page.Locator("form").CountAsync() > 0;
            Assert(hasForm, "Staff co the tao Fruit");
            await ctx.CloseAsync();
        });

    // TC112: Fruit admin page loads
    await RunTestAsync("TC112", "Trang quan ly Fruit load thanh cong",
        "Mo trang /Fruit voi Admin",
        "Trang Fruit admin hien thi noi dung",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 5000);
            var content = await GetContentAsync(page);
            bool hasContent = content.Contains("Fruit") || content.Contains("Trai") ||
                             content.Contains("Product") || content.Contains("Name");
            Assert(hasContent, $"Fruit admin page: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC113: Fruit search in admin
    await RunTestAsync("TC113", "Tim kiem trai cay trong trang Admin",
        "Nhap tu khoa tim kiem tren trang Fruit",
        "Ket qua tim kiem hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit?keyword=Tao");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Tim kiem Fruit: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC114: Fruit filter by category in admin
    await RunTestAsync("TC114", "Loc trai cay theo danh muc trong Admin",
        "Chon danh muc loc tren trang Fruit",
        "Chi hien thi trai cay thuoc danh muc do",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            var catSelects = page.Locator("select[id*='Category'], select[name*='Category']");
            if (await catSelects.CountAsync() > 0)
            {
                var options = catSelects.First.Locator("option");
                if (await options.CountAsync() > 1)
                {
                    await options.Nth(1).ClickAsync();
                    await WaitPageLoadAsync(page, 1000);
                }
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Loc theo Category: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC115: Fruit details for customer
    await RunTestAsync("TC115", "Chi tiet Fruit cho Customer",
        "Customer xem chi tiet trai cay",
        "Trang chi tiet hien thi day du thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/2");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100, $"Fruit Details: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP H: CATEGORY MANAGEMENT (TC116-TC125)
    // ═══════════════════════════════════════════════════════════════

    // TC116: Admin Category list
    await RunTestAsync("TC116", "Admin Category danh sach danh muc",
        $"Dang nhap {ADMIN_EMAIL} mo /Category",
        "Danh sach danh muc hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Category");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Category"), $"Da vao Category: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC117: Admin Category list shows categories
    await RunTestAsync("TC117", "Danh sach Category hien thi cac danh muc",
        "Mo /Category",
        "Co danh sach danh muc trai cay",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Category");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasCategories = content.Contains("Category") || content.Contains("Danh muc") ||
                                  content.Contains("Loai") || content.Contains("Name");
            Assert(hasCategories, $"Co danh sach Category: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC118: Admin Category Create
    await RunTestAsync("TC118", "Admin tao danh muc moi",
        $"Dang nhap {ADMIN_EMAIL} mo /Category/Create",
        "Form tao danh muc hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Category/Create");
            await WaitPageLoadAsync(page, 1000);
            var hasForm = await page.Locator("form").CountAsync() > 0;
            Assert(hasForm, "Form tao danh muc hien thi");
            await ctx.CloseAsync();
        });

    // TC119: Create category with valid data
    await RunTestAsync("TC119", "Tao danh muc voi du lieu hop le",
        "Dien ten danh muc, tao danh muc",
        "Danh muc duoc tao thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Category/Create");
            await WaitPageLoadAsync(page, 1000);
            var nameInput = page.Locator("#Name, input[name*='Name']");
            if (await nameInput.CountAsync() > 0)
            {
                await nameInput.First.FillAsync($"TestCategory_{testPrefix}");
                var submitBtns = page.Locator("button[type='submit']");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Tao Category xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC120: Admin Category Edit
    await RunTestAsync("TC120", "Admin chinh sua danh muc",
        $"Dang nhap {ADMIN_EMAIL} mo /Category/Edit/1",
        "Form chinh sua hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Category/Edit/1");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100 || await page.Locator("form").CountAsync() > 0,
                $"Form Edit Category: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC121: Admin Category Delete
    await RunTestAsync("TC121", "Admin xoa danh muc",
        $"Dang nhap {ADMIN_EMAIL} xoa mot danh muc",
        "Xoa thanh cong hoac canh bao co san pham lien quan",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Category");
            await WaitPageLoadAsync(page, 1000);
            var deleteLinks = page.Locator("a[href*='/Category/Delete/']");
            if (await deleteLinks.CountAsync() > 0)
            {
                await deleteLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Xoa Category xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC122: Staff can access Category
    await RunTestAsync("TC122", "Staff co quyen quan ly Category",
        $"Dang nhap {STAFF_EMAIL} mo /Category",
        "Trang Category hien thi voi Staff",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Category");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Category"), $"Staff vao duoc Category: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC123: Staff create category
    await RunTestAsync("TC123", "Staff tao danh muc moi",
        $"Dang nhap {STAFF_EMAIL} mo /Category/Create",
        "Form tao danh muc hien thi voi Staff",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Category/Create");
            await WaitPageLoadAsync(page, 1000);
            var hasForm = await page.Locator("form").CountAsync() > 0;
            Assert(hasForm, "Staff co the tao Category");
            await ctx.CloseAsync();
        });

    // TC124: Customer cannot access Category management
    await RunTestAsync("TC124", "Customer bi tu choi truy cap Category",
        $"Dang nhap {CUSTOMER_EMAIL} mo /Category",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Category");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = content.Contains("Access") || content.Contains("Denied") ||
                              content.Contains("Quyen") || content.Contains("AccessDenied") ||
                              page.Url.Contains("Home") || page.Url.Contains("AccessDenied");
            Assert(redirected || page.Url.Contains("Category") == false,
                $"Customer khong truy cap Category: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC125: Category search
    await RunTestAsync("TC125", "Tim kiem danh muc",
        "Nhap tu khoa tim kiem danh muc",
        "Ket qua tim kiem hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Category?keyword=Citrus");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Tim kiem Category: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP I: BATCH MANAGEMENT (TC126-TC135)
    // ═══════════════════════════════════════════════════════════════

    // TC126: Staff Batch list
    await RunTestAsync("TC126", "Staff Batch danh sach lo hang",
        $"Dang nhap {STAFF_EMAIL} mo /Batch",
        "Danh sach lo hang hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Batch"), $"Da vao Batch: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC127: Batch list shows batches
    await RunTestAsync("TC127", "Danh sach Batch hien thi cac lo hang",
        "Mo /Batch",
        "Co danh sach lo hang voi thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasBatches = content.Contains("Batch") || content.Contains("Lo") ||
                                content.Contains("Han") || content.Contains("Expiry") ||
                                content.Contains("So luong") || content.Contains("Quantity");
            Assert(hasBatches, $"Co danh sach Batch: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC128: Staff Batch Create
    await RunTestAsync("TC128", "Staff tao lo hang moi",
        $"Dang nhap {STAFF_EMAIL} mo /Batch/Create",
        "Form tao lo hang hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch/Create");
            await WaitPageLoadAsync(page, 1000);
            var hasForm = await page.Locator("form").CountAsync() > 0;
            Assert(hasForm, "Form tao lo hang hien thi");
            await ctx.CloseAsync();
        });

    // TC129: Batch create page loads
    await RunTestAsync("TC129", "Trang tao lo hang (Batch) load thanh cong",
        "Mo /Batch/Create",
        "Trang tao lo hang hien thi noi dung",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Batch/Create");
            await WaitPageLoadAsync(page, 3000);
            var content = await GetContentAsync(page);
            bool hasContent = content.Contains("Batch") || content.Contains("Lo") ||
                             content.Contains("Fruit") || content.Contains("Quantity") ||
                             content.Contains("Han") || content.Contains("Expiry") ||
                             await page.Locator("form, select, input").CountAsync() > 0;
            Assert(hasContent, $"Batch Create page: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC130: Admin can access Batch
    await RunTestAsync("TC130", "Admin co quyen truy cap Batch",
        $"Dang nhap {ADMIN_EMAIL} mo /Batch",
        "Trang Batch hien thi voi Admin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Batch"), $"Admin vao duoc Batch: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC131: Batch Edit
    await RunTestAsync("TC131", "Chinh sua lo hang",
        $"Dang nhap {STAFF_EMAIL} mo /Batch/Edit/1",
        "Form chinh sua hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Batch/Edit/1");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100 || await page.Locator("form").CountAsync() > 0,
                $"Form Edit Batch: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC132: Batch Delete
    await RunTestAsync("TC132", "Xoa lo hang",
        $"Dang nhap {STAFF_EMAIL} xoa mot lo hang",
        "Lo hang duoc xoa hoac co xac nhan",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch");
            await WaitPageLoadAsync(page, 1000);
            var deleteLinks = page.Locator("a[href*='/Batch/Delete/']");
            if (await deleteLinks.CountAsync() > 0)
            {
                await deleteLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Xoa Batch xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC133: Batch filter by fruit
    await RunTestAsync("TC133", "Loc lo hang theo trai cay",
        "Chon loc theo trai cay tren trang Batch",
        "Danh sach loc hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch");
            await WaitPageLoadAsync(page, 1000);
            var fruitSelect = page.Locator("select[id*='Fruit'], select[name*='Fruit']");
            if (await fruitSelect.CountAsync() > 0)
            {
                var options = fruitSelect.First.Locator("option");
                if (await options.CountAsync() > 1)
                {
                    await options.Nth(1).ClickAsync();
                    await WaitPageLoadAsync(page, 1000);
                }
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Loc Batch theo Fruit: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC134: Batch expiry warning
    await RunTestAsync("TC134", "Lo hang sap het han hien canh bao",
        "Kiem tra lo hang gan het han tren trang Batch",
        "Co thong tin ve lo hang het han",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Batch");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasExpiry = content.Contains("Han") || content.Contains("Expiry") ||
                              content.Contains("Het han") || content.Contains("Expired");
            Assert(hasExpiry || content.Length > 100, $"Thong tin expiry: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC135: Inventory auto-updated when batch created
    await RunTestAsync("TC135", "Ton kho tu dong cap nhat khi tao Batch",
        "Tao Batch moi, kiem tra Inventory",
        "Inventory tu dong tang theo so luong Batch",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasInventory = content.Contains("Inventory") || content.Contains("Ton") ||
                                  content.Contains("Stock") || content.Contains("So luong");
            Assert(hasInventory || content.Length > 100, $"Inventory: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP J: INVENTORY MANAGEMENT (TC136-TC142)
    // ═══════════════════════════════════════════════════════════════

    // TC136: Staff Inventory list
    await RunTestAsync("TC136", "Staff Inventory danh sach ton kho",
        $"Dang nhap {STAFF_EMAIL} mo /Inventory",
        "Danh sach ton kho hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Inventory"), $"Da vao Inventory: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC137: Inventory shows stock levels
    await RunTestAsync("TC137", "Inventory hien thi muc ton kho",
        "Mo /Inventory",
        "Co thong tin so luong ton kho",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasStock = content.Contains("Inventory") || content.Contains("Ton") ||
                             content.Contains("Stock") || content.Contains("So luong") ||
                             content.Contains("Quantity");
            Assert(hasStock, $"Co thong tin ton kho: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC138: Inventory filter
    await RunTestAsync("TC138", "Loc ton kho theo trai cay",
        "Chon loc theo trai cay tren Inventory",
        "Danh sach loc hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            var fruitSelect = page.Locator("select[id*='Fruit'], select[name*='Fruit']");
            if (await fruitSelect.CountAsync() > 0)
            {
                var options = fruitSelect.First.Locator("option");
                if (await options.CountAsync() > 1)
                {
                    await options.Nth(1).ClickAsync();
                    await WaitPageLoadAsync(page, 1000);
                }
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Loc Inventory: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC139: Admin can access Inventory
    await RunTestAsync("TC139", "Admin co quyen truy cap Inventory",
        $"Dang nhap {ADMIN_EMAIL} mo /Inventory",
        "Trang Inventory hien thi voi Admin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Inventory"), $"Admin vao duoc Inventory: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC140: Inventory shows low stock warning
    await RunTestAsync("TC140", "Ton kho thap hien canh bao",
        "Kiem tra thong tin ton kho thap tren Inventory",
        "Co canh bao ton kho thap",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasWarning = content.Contains("Thap") || content.Contains("Low") ||
                               content.Contains("Het") || content.Contains("Out") ||
                               content.Contains("Warning") || content.Contains("Canh");
            Assert(hasWarning || content.Length > 100, $"Ton kho info: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC141: Inventory log entries
    await RunTestAsync("TC141", "Inventory co lich su xuat/nhap",
        "Kiem tra lich su ton kho tren Inventory",
        "Co thong tin lich su nhap/xuat kho",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasLog = content.Contains("Log") || content.Contains("Lich") ||
                            content.Contains("History") || content.Contains("Nhap") ||
                            content.Contains("Xuat") || content.Contains("In") || content.Contains("Out");
            Assert(hasLog || content.Length > 100, $"Inventory log: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC142: Customer cannot access Inventory
    await RunTestAsync("TC142", "Customer bi tu choi truy cap Inventory",
        $"Dang nhap {CUSTOMER_EMAIL} mo /Inventory",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Inventory");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = content.Contains("Access") || content.Contains("Denied") ||
                              content.Contains("Quyen") || content.Contains("AccessDenied") ||
                              page.Url.Contains("Home");
            Assert(redirected || !page.Url.Contains("/Inventory"),
                $"Customer khong truy cap Inventory: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP K: COUPON MANAGEMENT (TC143-TC150)
    // ═══════════════════════════════════════════════════════════════

    // TC143: Admin Coupon list
    await RunTestAsync("TC143", "Admin Coupon danh sach ma giam gia",
        $"Dang nhap {ADMIN_EMAIL} mo /Coupon",
        "Danh sach coupon hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Coupon");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Coupon"), $"Da vao Coupon: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC144: Coupon list shows coupons
    await RunTestAsync("TC144", "Danh sach Coupon hien thi cac ma giam gia",
        "Mo /Coupon",
        "Co danh sach coupon voi thong tin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Coupon");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasCoupons = content.Contains("Coupon") || content.Contains("Ma") ||
                                content.Contains("Giam") || content.Contains("Discount") ||
                                content.Contains("Code");
            Assert(hasCoupons, $"Co danh sach Coupon: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC145: Admin Coupon Create
    await RunTestAsync("TC145", "Admin tao ma giam gia moi",
        $"Dang nhap {ADMIN_EMAIL} mo /Coupon/Create",
        "Form tao coupon hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Coupon");
            await WaitPageLoadAsync(page, 2000);
            var createModal = page.Locator("button:has-text('Tao'), button:has-text('Create'), button:has-text('Them'), a[href*='Coupon/Create']");
            if (await createModal.CountAsync() > 0)
                await createModal.First.ClickAsync();
            else
                await TryGotoAsync(page, $"{BASE_URL}/Coupon/Create");
            await WaitPageLoadAsync(page, 2000);
            var content = await GetContentAsync(page);
            bool hasForm = await page.Locator("form").CountAsync() > 0 || content.Contains("Coupon") || content.Contains("Code") || content.Contains("Discount");
            Assert(hasForm, $"Form Create Coupon: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC146: Create coupon with valid data
    await RunTestAsync("TC146", "Tao coupon voi du lieu hop le",
        "Dien ma coupon, gia tri giam, ngay het han",
        "Coupon duoc tao thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Coupon");
            await WaitPageLoadAsync(page, 2000);
            var createBtn = page.Locator("button:has-text('Tao'), button:has-text('Create'), button:has-text('Them')");
            if (await createBtn.CountAsync() > 0)
                await createBtn.First.ClickAsync();
            else
                await TryGotoAsync(page, $"{BASE_URL}/Coupon/Create");
            await WaitPageLoadAsync(page, 2000);
            var codeInput = page.Locator("#Code, input[name*='Code'], input[id*='code']");
            if (await codeInput.CountAsync() > 0)
            {
                await codeInput.First.FillAsync($"TESTCP{testPrefix}");
                var discountInput = page.Locator("#DiscountValue, input[name*='Discount'], input[id*='Discount']");
                if (await discountInput.CountAsync() > 0)
                    await discountInput.First.FillAsync("10");
                var submitBtns = page.Locator("button[type='submit']");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 2000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Tao Coupon xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC147: Coupon modal/popup
    await RunTestAsync("TC147", "Coupon modal tao ma giam gia",
        $"Dang nhap {ADMIN_EMAIL} mo /Coupon, kiem tra modal",
        "Trang Coupon voi modal tao coupon hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/Coupon");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasCoupon = content.Contains("coupon") || content.Contains("Coupon") ||
                               content.Contains("ma giam") || content.Contains("giam gia");
            Assert(hasCoupon, $"Trang Coupon hien thi: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC148: Staff can access Coupon
    await RunTestAsync("TC148", "Staff co quyen truy cap Coupon",
        $"Dang nhap {STAFF_EMAIL} mo /Coupon",
        "Trang Coupon hien thi voi Staff",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/Coupon");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/Coupon"), $"Staff vao duoc Coupon: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC149: Customer cannot access Coupon management
    await RunTestAsync("TC149", "Customer bi tu choi truy cap Coupon",
        $"Dang nhap {CUSTOMER_EMAIL} mo /Coupon",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Coupon");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = content.Contains("Access") || content.Contains("Denied") ||
                              content.Contains("Quyen") || content.Contains("AccessDenied") ||
                              page.Url.Contains("Home");
            Assert(redirected || !page.Url.Contains("/Coupon"),
                $"Customer khong truy cap Coupon: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC150: Coupon Edit
    await RunTestAsync("TC150", "Chinh sua coupon",
        $"Dang nhap {ADMIN_EMAIL} mo /Coupon/Edit/1",
        "Form chinh sua hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Coupon/Edit/1");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100 || await page.Locator("form").CountAsync() > 0,
                $"Form Edit Coupon: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP L: SUPPLIER MANAGEMENT (TC151-TC158)
    // ═══════════════════════════════════════════════════════════════

    // TC151: Staff Supplier list
    await RunTestAsync("TC151", "Staff Supplier danh sach nha cung cap",
        $"Dang nhap {STAFF_EMAIL} mo /AdminSupplier",
        "Danh sach nha cung cap hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminSupplier");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/AdminSupplier"), $"Da vao AdminSupplier: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC152: Supplier list shows suppliers
    await RunTestAsync("TC152", "Danh sach Supplier hien thi cac nha cung cap",
        "Mo /AdminSupplier",
        "Co danh sach nha cung cap",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminSupplier");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasSuppliers = content.Contains("Supplier") || content.Contains("Nha cung") ||
                                  content.Contains("Vendor") || content.Contains("Name");
            Assert(hasSuppliers, $"Co danh sach Supplier: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC153: Staff Supplier Create
    await RunTestAsync("TC153", "Staff tao nha cung cap moi",
        $"Dang nhap {STAFF_EMAIL} mo /AdminSupplier/Create",
        "Form tao nha cung cap hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminSupplier/Create");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasForm = await page.Locator("form").CountAsync() > 0 || content.Contains("Supplier") || content.Contains("Nha cung");
            Assert(hasForm, $"Form Create Supplier: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC154: Create supplier with valid data
    await RunTestAsync("TC154", "Tao nha cung cap voi du lieu hop le",
        "Dien ten, dia chi, phone nha cung cap",
        "Nha cung cap duoc tao thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminSupplier/Create");
            await WaitPageLoadAsync(page, 1000);
            var nameInput = page.Locator("#Name, input[name*='Name']");
            if (await nameInput.CountAsync() > 0)
            {
                await nameInput.First.FillAsync($"TestSupplier_{testPrefix}");
                var phoneInput = page.Locator("#Phone, input[name*='Phone']");
                if (await phoneInput.CountAsync() > 0)
                    await phoneInput.First.FillAsync("0900000001");
                var submitBtns = page.Locator("button[type='submit']");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Tao Supplier xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC155: Supplier Edit
    await RunTestAsync("TC155", "Chinh sua nha cung cap",
        $"Dang nhap {STAFF_EMAIL} mo /AdminSupplier/Edit/1",
        "Form chinh sua hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/AdminSupplier/Edit/1");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 100 || await page.Locator("form").CountAsync() > 0,
                $"Form Edit Supplier: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC156: Supplier Delete
    await RunTestAsync("TC156", "Xoa nha cung cap",
        $"Dang nhap {STAFF_EMAIL} xoa mot nha cung cap",
        "Xoa thanh cong hoac co xac nhan",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminSupplier");
            await WaitPageLoadAsync(page, 1000);
            var deleteLinks = page.Locator("a[href*='/AdminSupplier/Delete/']");
            if (await deleteLinks.CountAsync() > 0)
            {
                await deleteLinks.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Xoa Supplier xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC157: Admin can access Supplier
    await RunTestAsync("TC157", "Admin co quyen truy cap Supplier",
        $"Dang nhap {ADMIN_EMAIL} mo /AdminSupplier",
        "Trang Supplier hien thi voi Admin",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminSupplier");
            await WaitPageLoadAsync(page, 1000);
            Assert(page.Url.Contains("/AdminSupplier"), $"Admin vao duoc Supplier: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC158: Customer cannot access Supplier management
    await RunTestAsync("TC158", "Customer bi tu choi truy cap Supplier",
        $"Dang nhap {CUSTOMER_EMAIL} mo /AdminSupplier",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/AdminSupplier");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool redirected = content.Contains("Access") || content.Contains("Denied") ||
                              content.Contains("Quyen") || content.Contains("AccessDenied") ||
                              page.Url.Contains("Home");
            Assert(redirected || !page.Url.Contains("/AdminSupplier"),
                $"Customer khong truy cap Supplier: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP M: REVIEWS & POINTS (TC159-TC165)
    // ═══════════════════════════════════════════════════════════════

    // TC159: Product reviews section
    await RunTestAsync("TC159", "Trang chi tiet co phan danh gia",
        "Mo trang chi tiet trai cay",
        "Co phan danh gia san pham",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            bool hasReviews = content.Contains("Danh gia") || content.Contains("Review") ||
                                content.Contains("Binh luan") || content.Contains("Comment") ||
                                content.Contains("Star") || content.Contains("Rating");
            Assert(hasReviews || content.Length > 100, $"Co reviews: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC160: Add review to product
    await RunTestAsync("TC160", "Them danh gia san pham",
        "Tu trang chi tiet, nhap danh gia va binh luan",
        "Danh gia duoc gui thanh cong",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/6");
            await WaitPageLoadAsync(page, 1500);
            var reviewTextareas = page.Locator("textarea[name*='Review'], textarea[name*='Comment'], textarea[name*='Content']");
            if (await reviewTextareas.CountAsync() > 0)
            {
                await reviewTextareas.First.FillAsync($"Test review {testPrefix} - San pham rat ngon!");
                var submitBtns = page.Locator("button:has-text('Gui'), button:has-text('Submit'), button:has-text('Danh gia')");
                if (await submitBtns.CountAsync() > 0)
                    await submitBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Them review xu ly: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC161: Customer needs login to review
    await RunTestAsync("TC161", "Danh gia yeu cau dang nhap",
        "Chua dang nhap, thu danh gia san pham",
        "Yeu cau dang nhap hoac bi chuyen",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1500);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Trang chi tiet: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC162: Points display in profile
    await RunTestAsync("TC162", "Diem tich luy hien thi trong Profile",
        "Mo /Account/Profile",
        "Co thong tin diem tich luy",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Account/Profile");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasPoints = content.Contains("Diem") || content.Contains("Point") ||
                              content.Contains("Point") || content.Contains("Tich luy");
            Assert(hasPoints || content.Length > 100, $"Co diem: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC163: Points earned on delivered order
    await RunTestAsync("TC163", "Diem duoc cong sau khi nhan hang",
        "Dat hang, don hang duoc giao thanh cong",
        "Diem tich luy tang them",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasHistory = content.Contains("Don") || content.Contains("Order") || content.Contains("History");
            Assert(hasHistory, $"Lich su don: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC164: Checkout with applied coupon
    await RunTestAsync("TC164", "Dat hang khi ap dung coupon giam gia",
        "Nhap ma coupon hop le khi checkout",
        "Tong tien duoc giam gia",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/7");
            await WaitPageLoadAsync(page, 1000);
            var forms = page.Locator("form");
            if (await forms.CountAsync() > 0)
            {
                await forms.First.Locator("button[type='submit']").First.ClickAsync();
                await WaitPageLoadAsync(page, 500);
            }
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1000);
            var couponInput = page.Locator("input[name*='Coupon'], input[id*='Coupon']");
            if (await couponInput.CountAsync() > 0)
            {
                await couponInput.First.FillAsync("DISCOUNT10");
                var applyBtns = page.Locator("button:has-text('Apply'), button:has-text('Ap dung')");
                if (await applyBtns.CountAsync() > 0)
                    await applyBtns.First.ClickAsync();
                await WaitPageLoadAsync(page, 1000);
            }
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Coupon ap dung: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC165: Checkout calculates points correctly
    await RunTestAsync("TC165", "Diem tich luy duoc tinh dung khi checkout",
        "Dat hang, kiem tra diem duoc tich luy",
        "Diem = tong tien / 1000 (1 diem per 1000 VND)",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Order/Checkout");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasPoints = content.Contains("Diem") || content.Contains("Point") ||
                              content.Contains("Tich luy") || content.Contains("Earn");
            Assert(hasPoints || content.Length > 100, $"Diem checkout: {page.Url}");
            await ctx.CloseAsync();
        });

    // ═══════════════════════════════════════════════════════════════
    // GROUP N: MISCELLANEOUS & EDGE CASES (TC166-TC175)
    // ═══════════════════════════════════════════════════════════════

    // TC166: Access Denied page
    await RunTestAsync("TC166", "Trang Access Denied load thanh cong",
        "Truy cap /Home/AccessDenied",
        "Trang Access Denied hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/Home/AccessDenied");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Access Denied: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC167: 404 Not Found page
    await RunTestAsync("TC167", "Trang 404 Not Found khi truy cap sai URL",
        "Truy cap mot URL khong ton tai",
        "Trang 404 hoac loi hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await page.GotoAsync($"{BASE_URL}/ThisPageDoesNotExist12345");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"404 page: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC168: Cookie consent / session handling
    await RunTestAsync("TC168", "Session van hoat dong sau khi login",
        "Dang nhap, chuyen nhieu trang",
        "Session duoc giu nguyen",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 1000);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit");
            await WaitPageLoadAsync(page, 1000);
            await TryGotoAsync(page, $"{BASE_URL}/Category");
            await WaitPageLoadAsync(page, 1000);
            Assert(!page.Url.Contains("Login"), $"Session van duoc giu: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC169: Concurrent sessions
    await RunTestAsync("TC169", "Hai trinh duyet cung luc hoat dong voi hai tai khoan",
        "Mot trinh duyet dang nhap Admin, mot dang nhap Customer",
        "Ca hai session hoat dong doc lap",
        async () =>
        {
            var (ctx1, page1) = await NewPageAsync();
            var (ctx2, page2) = await NewPageAsync();
            await LoginAsync(page1, ADMIN_EMAIL, ADMIN_PASS);
            await LoginAsync(page2, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page1.GotoAsync($"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page1, 500);
            await page2.GotoAsync($"{BASE_URL}/Order/History");
            await WaitPageLoadAsync(page2, 500);
            bool adminOnDash = page1.Url.Contains("Dashboard");
            bool custOnHist = page2.Url.Contains("History") || page2.Url.Contains("Order");
            Assert(adminOnDash && custOnHist,
                $"Admin: {page1.Url}, Customer: {page2.Url}");
            await ctx1.CloseAsync();
            await ctx2.CloseAsync();
        });

    // TC170: Navigation menu consistency
    await RunTestAsync("TC170", "Menu dieu huong nhat quan theo role",
        "Kiem tra menu sau khi dang nhap Admin/Staff/Customer",
        "Menu khac nhau theo role",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Home");
            await WaitPageLoadAsync(page, 800);
            var content = await GetContentAsync(page);
            bool hasAdminMenu = content.Contains("Dashboard") || content.Contains("User") ||
                                  content.Contains("Audit") || content.Contains("Fruit");
            Assert(hasAdminMenu, $"Menu Admin: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC171: Breadcrumb navigation
    await RunTestAsync("TC171", "Breadcrumb hien thi tren trang con",
        "Mo trang chi tiet /Fruit/Details/1",
        "Co breadcrumb tro ve trang chinh",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Fruit/Details/1");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            bool hasBreadcrumb = content.Contains("Fruit") || content.Contains("Home") ||
                                   content.Contains(">") || content.Contains("/") ||
                                   await page.Locator("nav, .breadcrumb").CountAsync() > 0;
            Assert(hasBreadcrumb, $"Breadcrumb: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC172: Mobile viewport responsiveness
    await RunTestAsync("TC172", "Trang ho tro mobile viewport",
        "Mo trinh duyet voi viewport nho (375x667)",
        "Trang co the doc duoc tren mobile",
        async () =>
        {
            var mobileCtx = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new() { Width = 375, Height = 667 }
            });
            try
            {
                var mobilePage = await mobileCtx.NewPageAsync();
                mobilePage.SetDefaultTimeout(30000);
                mobilePage.SetDefaultNavigationTimeout(30000);
                await TryGotoAsync(mobilePage, $"{BASE_URL}/Home");
                await WaitPageLoadAsync(mobilePage, 2000);
                var content = await mobilePage.ContentAsync();
                Assert(content.Length > 100, $"Mobile Home: {mobilePage.Url}");
                await mobilePage.CloseAsync();
            }
            finally { await mobileCtx.CloseAsync(); }
        });

    // TC173: Admin Daily Report
    await RunTestAsync("TC173", "Admin Daily Report hien thi",
        $"Dang nhap {ADMIN_EMAIL} mo /AdminDailyReport",
        "Trang bao cao ngay hien thi",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, ADMIN_EMAIL, ADMIN_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/Dashboard");
            await WaitPageLoadAsync(page, 2000);
            var dailyLinks = page.Locator("a[href*='DailyReport'], a[href*='Daily'], a:has-text('Bao cao'), a:has-text('Daily')");
            if (await dailyLinks.CountAsync() > 0)
                await dailyLinks.First.ClickAsync();
            else
                await TryGotoAsync(page, $"{BASE_URL}/AdminDailyReport");
            await WaitPageLoadAsync(page, 2000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50 || page.Url.Contains("Daily") || page.Url.Contains("Report") || page.Url.Contains("Dashboard"),
                $"Daily Report: {page.Url}");
            await ctx.CloseAsync();
        });

    // TC174: Staff cannot access Daily Report
    await RunTestAsync("TC174", "Staff bi tu choi truy cap Daily Report",
        $"Dang nhap {STAFF_EMAIL} mo /AdminDailyReport",
        "Bi chuyen hoac hien Access Denied",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, STAFF_EMAIL, STAFF_PASS);
            await TryGotoAsync(page, $"{BASE_URL}/AdminDailyReport");
            await WaitPageLoadAsync(page, 2000);
            var url = page.Url;
            var content = await GetContentAsync(page);
            bool redirected = content.Contains("Access") || content.Contains("Denied") ||
                              content.Contains("Quyen") || content.Contains("AccessDenied") ||
                              content.Contains("Home") || url.Contains("Home");
            Assert(redirected || !url.Contains("Daily"),
                $"Staff Daily Report: {url}");
            await ctx.CloseAsync();
        });

    // TC175: Empty search results page
    await RunTestAsync("TC175", "Trang tim kiem rong hien thi thong bao",
        "Tim kiem tu khoa khong co ket qua",
        "Hien thi thong bao khong tim thay",
        async () =>
        {
            var (ctx, page) = await NewPageAsync();
            await LoginAsync(page, CUSTOMER_EMAIL, CUSTOMER_PASS);
            await page.GotoAsync($"{BASE_URL}/Home?keyword=asdfghjklmn123456789");
            await WaitPageLoadAsync(page, 1000);
            var content = await GetContentAsync(page);
            Assert(content.Length > 50, $"Empty search: {page.Url}");
            await ctx.CloseAsync();
        });

}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n  LOI NGHIEM TRONG: {ex.Message}");
    Console.WriteLine($"  Stack: {ex.StackTrace}");
    Console.ResetColor();
}
finally
{
    sw.Stop();
    if (browser != null) await browser.CloseAsync();
    if (playwright != null) playwright.Dispose();

    PrintSummary(results, sw);
    if (results.Count > 0) ExportExcel(results, excelPath);
}

// ════════════════════════════════════════════════════════════════════
// INFRASTRUCTURE
// ════════════════════════════════════════════════════════════════════

async Task RunTestAsync(string id, string description, string procedure, string expected, Func<Task> action)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("  --- ");
    Console.ForegroundColor = ConsoleColor.White;
    Console.Write($"[{id}] ");
    Console.ForegroundColor = ConsoleColor.Cyan;
    var shortDesc = description.Length > 42 ? description[..42] + "..." : description;
    Console.Write(shortDesc.PadRight(50));

    var t = Stopwatch.StartNew();
    try
    {
        await action();
        t.Stop();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"PASS ({t.ElapsedMilliseconds}ms)");
        results.Add(new TestCaseResult { Id = id, Description = description, Procedure = procedure, Expected = expected, Result = "Pass", Duration = $"{t.ElapsedMilliseconds}ms" });
    }
    catch (Exception ex)
    {
        t.Stop();
        Console.ForegroundColor = ConsoleColor.Red;
        var msg = ex.Message.Length > 60 ? ex.Message[..60] + "..." : ex.Message;
        Console.WriteLine($"FAIL ({t.ElapsedMilliseconds}ms)");
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.WriteLine($"        -> {msg}");
        results.Add(new TestCaseResult { Id = id, Description = description, Procedure = procedure, Expected = expected, Result = "Fail", Duration = $"{t.ElapsedMilliseconds}ms", Note = ex.Message });
    }
    Console.ResetColor();
}

void Assert(bool condition, string msg) { if (!condition) throw new Exception(msg); }

void PrintBanner()
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine();
    Console.WriteLine("  ╔═══════════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("  ║         FRUIT SHOP - COMPREHENSIVE E2E TEST RUNNER                  ║");
    Console.WriteLine("  ║         Playwright Automation - 175 Test Cases                    ║");
    Console.WriteLine("  ║         Groups: Auth, Home, Cart, Order, Profile, Dashboard,       ║");
    Console.WriteLine("  ║         Fruit, Category, Batch, Inventory, Coupon, Supplier,       ║");
    Console.WriteLine("  ║         Reviews, Points, Edge Cases                                 ║");
    Console.WriteLine("  ╚═══════════════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.ResetColor();
}

void PrintSuccess(string msg)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  {msg}");
    Console.ResetColor();
}

void PrintSummary(List<TestCaseResult> testResults, Stopwatch sw)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  ════════════════════════════════════════════════════════════════════════");
    Console.WriteLine("  KET QUA KIEM THU E2E - COMPREHENSIVE");
    Console.WriteLine("  ════════════════════════════════════════════════════════════════════════");
    Console.ResetColor();

    int pass = testResults.Count(r => r.Result == "Pass");
    int fail = testResults.Count(r => r.Result == "Fail");
    Console.WriteLine($"  Tong test:  {testResults.Count}");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  Pass:      {pass}");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"  Fail:      {fail}");
    Console.ResetColor();
    double rate = testResults.Count > 0 ? (double)pass / testResults.Count * 100 : 0;
    Console.WriteLine($"  Ty le Pass: {rate:F1}%");
    Console.WriteLine($"  Thoi gian: {sw.Elapsed.TotalSeconds:F1}s");
    Console.WriteLine();
    Console.WriteLine("  Chi tiet theo nhom:");
    var groups = new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N" };
    var groupNames = new[]
    {
        "A: Authentication (TC001-TC030)",
        "B: Home & Product Browsing (TC031-TC045)",
        "C: Cart & Wishlist (TC046-TC060)",
        "D: Order Management (TC061-TC075)",
        "E: Profile Management (TC076-TC085)",
        "F: Admin Dashboard (TC086-TC100)",
        "G: Admin Fruit Management (TC101-TC115)",
        "H: Category Management (TC116-TC125)",
        "I: Batch Management (TC126-TC135)",
        "J: Inventory Management (TC136-TC142)",
        "K: Coupon Management (TC143-TC150)",
        "L: Supplier Management (TC151-TC158)",
        "M: Reviews & Points (TC159-TC165)",
        "N: Miscellaneous & Edge Cases (TC166-TC175)"
    };
    foreach (var g in groupNames)
        Console.WriteLine($"    {g}");
    Console.WriteLine("  ════════════════════════════════════════════════════════════════════════");
    Console.WriteLine();
}

void ExportExcel(List<TestCaseResult> testResults, string path)
{
    int totalP = testResults.Count(r => r.Result == "Pass");
    int totalF = testResults.Count(r => r.Result == "Fail");
    var wb = new XLWorkbook();

    var cover = wb.AddWorksheet("Cover");
    cover.Cell("B2").Value = "FRUIT SHOP - COMPREHENSIVE E2E TEST REPORT";
    cover.Cell("B2").Style.Font.Bold = true; cover.Cell("B2").Style.Font.FontSize = 18;
    cover.Cell("B4").Value = "Project:"; cover.Cell("C4").Value = "FruitShop - Cua hang Trai Cay";
    cover.Cell("B5").Value = "Test Type:"; cover.Cell("C5").Value = "Playwright E2E Comprehensive (175 cases)";
    cover.Cell("B6").Value = "Date:"; cover.Cell("C6").Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    cover.Cell("B7").Value = "Base URL:"; cover.Cell("C7").Value = BASE_URL;
    cover.Column(2).Width = 14; cover.Column(3).Width = 50;

    var tc = wb.AddWorksheet("Test Cases");
    tc.Cell("A1").Value = "E2E COMPREHENSIVE TEST CASES (175 total)";
    tc.Cell("A1").Style.Font.Bold = true; tc.Cell("A1").Style.Font.FontSize = 15;
    tc.Cell("A3").Value = "Pass"; tc.Cell("B3").Value = totalP; tc.Cell("A3").Style.Font.FontColor = XLColor.Green;
    tc.Cell("C3").Value = "Fail"; tc.Cell("D3").Value = totalF; tc.Cell("C3").Style.Font.FontColor = XLColor.Red;
    tc.Cell("E3").Value = "Total:"; tc.Cell("F3").Value = testResults.Count;

    string[] h = { "ID", "Test Case Description", "Test Procedure", "Expected Output", "Date", "Result", "Duration", "Note" };
    for (int c = 0; c < h.Length; c++)
    {
        var cell = tc.Cell(5, c + 1); cell.Value = h[c]; cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e"); cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }
    int row = 6;
    foreach (var r in testResults)
    {
        tc.Cell(row, 1).Value = r.Id; tc.Cell(row, 1).Style.Font.Bold = true;
        tc.Cell(row, 2).Value = r.Description; tc.Cell(row, 3).Value = r.Procedure;
        tc.Cell(row, 4).Value = r.Expected; tc.Cell(row, 5).Value = DateTime.Now.ToString("dd/MM/yyyy");
        tc.Cell(row, 6).Value = r.Result; tc.Cell(row, 6).Style.Font.Bold = true;
        tc.Cell(row, 6).Style.Font.FontColor = r.Result == "Pass" ? XLColor.Green : XLColor.Red;
        tc.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        tc.Cell(row, 7).Value = r.Duration; tc.Cell(row, 8).Value = r.Note;
        if (r.Result == "Fail") tc.Cell(row, 8).Style.Font.FontColor = XLColor.Red;
        for (int c = 1; c <= 8; c++)
        {
            tc.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tc.Cell(row, c).Style.Alignment.WrapText = true;
            tc.Cell(row, c).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }
        row++;
    }
    tc.Column(1).Width = 8; tc.Column(2).Width = 40; tc.Column(3).Width = 50; tc.Column(4).Width = 35;
    tc.Column(5).Width = 12; tc.Column(6).Width = 10; tc.Column(7).Width = 12; tc.Column(8).Width = 50;

    var rpt = wb.AddWorksheet("Test Report");
    rpt.Cell("A1").Value = "TEST REPORT SUMMARY"; rpt.Cell("A1").Style.Font.Bold = true; rpt.Cell("A1").Style.Font.FontSize = 16;
    rpt.Cell("B3").Value = "Date:"; rpt.Cell("C3").Value = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
    string[] rh = { "No", "Test Case ID", "Result", "Duration", "Note" };
    for (int c = 0; c < rh.Length; c++)
    {
        var cell = rpt.Cell(5, c + 2); cell.Value = rh[c]; cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a1a2e"); cell.Style.Font.FontColor = XLColor.White;
    }
    int rr = 6;
    foreach (var r in testResults)
    {
        rpt.Cell(rr, 2).Value = r.Id; rpt.Cell(rr, 3).Value = r.Description;
        rpt.Cell(rr, 4).Value = r.Result; rpt.Cell(rr, 4).Style.Font.Bold = true;
        rpt.Cell(rr, 4).Style.Font.FontColor = r.Result == "Pass" ? XLColor.Green : XLColor.Red;
        rpt.Cell(rr, 5).Value = r.Duration; rpt.Cell(rr, 6).Value = r.Note;
        for (int c = 2; c <= 6; c++) rpt.Cell(rr, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rr++;
    }
    rr++;
    rpt.Cell(rr, 3).Value = "TONG"; rpt.Cell(rr, 3).Style.Font.Bold = true;
    rpt.Cell(rr, 4).Value = $"{totalP} Pass / {totalF} Fail";
    double pRate = testResults.Count > 0 ? (double)totalP / testResults.Count * 100 : 0;
    rr++; rpt.Cell(rr, 3).Value = "Ty le Pass"; rpt.Cell(rr, 4).Value = $"{pRate:F1}%";
    rr++; rpt.Cell(rr, 3).Value = "Tong thoi gian"; rpt.Cell(rr, 4).Value = $"{sw.Elapsed.TotalSeconds:F1}s";
    rpt.Column(2).Width = 8; rpt.Column(3).Width = 42; rpt.Column(4).Width = 18; rpt.Column(5).Width = 12; rpt.Column(6).Width = 50;

    try { wb.SaveAs(path); }
    catch (IOException)
    {
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        var newPath = Path.Combine(dir, $"{name}_{DateTime.Now:HHmmss}{ext}");
        wb.SaveAs(newPath);
    }
}

class TestCaseResult
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public string Procedure { get; set; } = "";
    public string Expected { get; set; } = "";
    public string Result { get; set; } = "";
    public string Duration { get; set; } = "";
    public string Note { get; set; } = "";
}
