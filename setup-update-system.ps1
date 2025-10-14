# Setup Update System
Write-Host "=== FehlzeitApp Update System Setup ===" -ForegroundColor Green
Write-Host ""

# Step 1: Create necessary directories
Write-Host "1. Creating update directories..." -ForegroundColor Yellow

$desktopUpdatesDir = "C:\Users\Hani.Allam\Desktop\Updates\FehlzeitApp"
$appUpdatesDir = "C:\Users\Hani.Allam\AppData\Local\FehlzeitApp\updates"

# Create desktop updates directory
if (-not (Test-Path $desktopUpdatesDir)) {
    New-Item -ItemType Directory -Path $desktopUpdatesDir -Force | Out-Null
    Write-Host "   ✅ Created desktop updates directory: $desktopUpdatesDir" -ForegroundColor Green
} else {
    Write-Host "   ✅ Desktop updates directory already exists" -ForegroundColor Green
}

# Create app updates directory
if (-not (Test-Path $appUpdatesDir)) {
    New-Item -ItemType Directory -Path $appUpdatesDir -Force | Out-Null
    Write-Host "   ✅ Created app updates directory: $appUpdatesDir" -ForegroundColor Green
} else {
    Write-Host "   ✅ App updates directory already exists" -ForegroundColor Green
}

# Step 2: Check if vpk tool is installed
Write-Host ""
Write-Host "2. Checking for vpk tool..." -ForegroundColor Yellow
$vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
if (Test-Path $vpkPath) {
    Write-Host "   ✅ vpk tool is installed" -ForegroundColor Green
} else {
    Write-Host "   ❌ vpk tool not found" -ForegroundColor Red
    Write-Host "   Installing vpk tool..." -ForegroundColor Yellow
    dotnet tool install --global vpk
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ vpk tool installed successfully" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Failed to install vpk tool" -ForegroundColor Red
        Write-Host "   Please install manually: dotnet tool install --global vpk" -ForegroundColor Yellow
    }
}

# Step 3: Verify project configuration
Write-Host ""
Write-Host "3. Checking project configuration..." -ForegroundColor Yellow

# Check if Velopack package is referenced
$csprojContent = Get-Content "FehlzeitApp.csproj" -Raw
if ($csprojContent -match "Velopack") {
    Write-Host "   ✅ Velopack package is referenced" -ForegroundColor Green
} else {
    Write-Host "   ❌ Velopack package not found in project" -ForegroundColor Red
    Write-Host "   Adding Velopack package..." -ForegroundColor Yellow
    dotnet add package Velopack
}

# Check current version
if ($csprojContent -match '<AssemblyVersion>(\d+\.\d+\.\d+\.\d+)</AssemblyVersion>') {
    $currentVersion = $matches[1]
    Write-Host "   ✅ Current version: $currentVersion" -ForegroundColor Green
} else {
    Write-Host "   ❌ Could not determine current version" -ForegroundColor Red
}

# Step 4: Test build
Write-Host ""
Write-Host "4. Testing build..." -ForegroundColor Yellow
dotnet build -c Release -nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed" -ForegroundColor Red
    Write-Host "   Please fix build errors before proceeding" -ForegroundColor Yellow
    exit 1
}

# Step 5: Create sample update (if no updates exist)
Write-Host ""
Write-Host "5. Checking for existing updates..." -ForegroundColor Yellow

$releasesFile = Join-Path $desktopUpdatesDir "RELEASES"
if (-not (Test-Path $releasesFile)) {
    Write-Host "   No existing updates found" -ForegroundColor Yellow
    Write-Host "   To create a test update, run: .\publish-auto-version.ps1" -ForegroundColor Cyan
} else {
    Write-Host "   ✅ Updates already exist" -ForegroundColor Green
    Write-Host "   Available versions:" -ForegroundColor Cyan
    Get-Content $releasesFile | ForEach-Object {
        if ($_ -match "FehlzeitApp-(\d+\.\d+\.\d+)-full\.nupkg") {
            Write-Host "     - Version $($matches[1])" -ForegroundColor White
        }
    }
}

# Step 6: Summary
Write-Host ""
Write-Host "=== Setup Complete ===" -ForegroundColor Green
Write-Host "✅ Update directories: Created" -ForegroundColor White
$vpkStatus = if (Test-Path $vpkPath) { 'Installed' } else { 'Not found' }
$configStatus = if ($csprojContent -match 'Velopack') { 'Ready' } else { 'Needs Velopack' }
Write-Host "✅ vpk tool: $vpkStatus" -ForegroundColor White
Write-Host "✅ Project configuration: $configStatus" -ForegroundColor White
Write-Host "✅ Build: Successful" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run .\publish-auto-version.ps1 to create a test update" -ForegroundColor White
Write-Host "2. Run .\test-complete-update-system.ps1 to test the update system" -ForegroundColor White
Write-Host "3. Start the app to see if updates are detected" -ForegroundColor White
