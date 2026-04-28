# ============================================================
# RunSeleniumTests.ps1
# Chạy web app + Selenium tests tự động
# ============================================================

$ErrorActionPreference = "Stop"

$FruitShopPath = "d:\ASM phattrienungdung\FruitShop"
$TestPath = "d:\ASM phattrienungdung\FruitShop.SeleniumTests"
$AppUrl = "http://localhost:5072"
$StartUrl = "$AppUrl/Account/Login"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Selenium Test Runner for FruitShop" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# --- Bước 1: Khởi động web app ---
Write-Host "[1/4] Killing any existing FruitShop processes..." -ForegroundColor Yellow
Get-Process -Name "FruitShop" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep 2

Write-Host "[2/4] Starting FruitShop web app on $AppUrl..." -ForegroundColor Yellow
$webApp = Start-Process -FilePath "dotnet" -ArgumentList "run --urls", $AppUrl -WorkingDirectory $FruitShopPath -PassThru -WindowStyle Normal

# --- Bước 2: Đợi web app sẵn sàng ---
Write-Host "[3/4] Waiting for web app to start..." -ForegroundColor Yellow
$maxWait = 30
$waited = 0
$ready = $false

while ($waited -lt $maxWait) {
    Start-Sleep 1
    try {
        $response = Invoke-WebRequest -Uri $StartUrl -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 302) {
            $ready = $true
            Write-Host "       Web app is ready!" -ForegroundColor Green
            break
        }
    }
    catch { }
    $waited++
    if ($waited % 5 -eq 0) {
        Write-Host "       Still waiting... ($waited/$maxWait sec)" -ForegroundColor Gray
    }
}

if (-not $ready) {
    Write-Host "[ERROR] Web app did not start within $maxWait seconds." -ForegroundColor Red
    Write-Host "        Is port 5072 already in use?" -ForegroundColor Red
    Stop-Process -Id $webApp.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

# --- Bước 3: Chạy Selenium tests ---
Write-Host "[4/4] Running Selenium tests (Chrome will open)...`n" -ForegroundColor Yellow

# Disable headless in TestBase temporarily
$testBasePath = Join-Path $TestPath "TestBase.cs"
$content = Get-Content $testBasePath -Raw
if ($content -match 'options\.AddArgument\("--headless"\)') {
    Write-Host "       Note: Headless mode detected. Tests will still run with visible Chrome." -ForegroundColor Gray
}

dotnet test $TestPath --no-build --logger "console;verbosity=detailed" 2>&1
$testExitCode = $LASTEXITCODE

# --- Bước 4: Dọn dẹp ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Cleanup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Stopping web app (PID: $($webApp.Id))..." -ForegroundColor Yellow
Stop-Process -Id $webApp.Id -Force -ErrorAction SilentlyContinue

if ($testExitCode -eq 0) {
    Write-Host "All tests passed!" -ForegroundColor Green
} else {
    Write-Host "Some tests failed (exit code: $testExitCode)" -ForegroundColor Red
}

exit $testExitCode
