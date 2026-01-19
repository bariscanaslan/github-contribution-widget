Write-Host "GitHub Widget Installer oluþturuluyor..." -ForegroundColor Green

# 1. Publish
Write-Host "`n[1/3] Uygulama publish ediliyor..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Publish hatasý!" -ForegroundColor Red
    exit 1
}

# 2. Dosyalarý kontrol et
Write-Host "`n[2/3] Dosyalar kontrol ediliyor..." -ForegroundColor Yellow
$publishPath = "bin\Release\net8.0-windows\win-x64\publish"
if (-not (Test-Path "$publishPath\GithubContributionWidget.exe")) {
    Write-Host "EXE dosyasý bulunamadý!" -ForegroundColor Red
    exit 1
}

# appsettings.json ve icon.ico'yu kopyala
Copy-Item "appsettings.json" -Destination $publishPath -Force
Copy-Item "icon.ico" -Destination $publishPath -Force -ErrorAction SilentlyContinue
Write-Host "Config dosyalarý kopyalandý." -ForegroundColor Gray

# 3. Inno Setup ile installer oluþtur
Write-Host "`n[3/3] Installer oluþturuluyor..." -ForegroundColor Yellow

# Inno Setup'ýn yolunu bul (tüm olasý konumlar)
$innoSetupPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
)

$innoSetupPath = $null
foreach ($path in $innoSetupPaths) {
    if (Test-Path $path) {
        $innoSetupPath = $path
        Write-Host "Inno Setup bulundu: $innoSetupPath" -ForegroundColor Gray
        break
    }
}

# ISCC.exe bulunamazsa, Compil32.exe'yi kontrol et
if (-not $innoSetupPath) {
    $compil32Path = "$env:LOCALAPPDATA\Programs\Inno Setup 6\Compil32.exe"
    if (Test-Path $compil32Path) {
        # ISCC.exe ayný klasörde olmalý
        $isccPath = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        if (Test-Path $isccPath) {
            $innoSetupPath = $isccPath
            Write-Host "Inno Setup bulundu: $innoSetupPath" -ForegroundColor Gray
        } else {
            Write-Host "ISCC.exe bulunamadý ama Compil32.exe var!" -ForegroundColor Yellow
            Write-Host "Lütfen Inno Setup'ý yeniden kurun ve 'Command-line compiler' seçeneðini iþaretleyin." -ForegroundColor Yellow
            Write-Host "Veya ISCC.exe'yi manuel olarak kullanýn: $compil32Path" -ForegroundColor Gray
            exit 1
        }
    }
}

if (-not $innoSetupPath) {
    Write-Host "Inno Setup bulunamadý!" -ForegroundColor Red
    Write-Host "Arama yapýlan konumlar:" -ForegroundColor Yellow
    foreach ($path in $innoSetupPaths) {
        Write-Host "  - $path" -ForegroundColor Gray
    }
    Write-Host "`nLütfen Inno Setup'ý yükleyin: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

# Installer'ý derle
& $innoSetupPath "installer.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n Installer baþarýyla oluþturuldu!" -ForegroundColor Green
    Write-Host "Konum: installer_output\GitHubWidget_Setup_v1.0.0.exe" -ForegroundColor Cyan
    
    # Installer klasörünü aç
    Start-Process "explorer.exe" "installer_output"
} else {
    Write-Host "`n Installer oluþturulamadý!" -ForegroundColor Red
    exit 1
}