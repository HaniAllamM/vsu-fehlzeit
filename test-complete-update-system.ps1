# Complete Update System Test
Write-Host "=== FehlzeitApp Complete Update System Test ===" -ForegroundColor Green
Write-Host ""

# Step 1: Check if app is installed
Write-Host "1. Checking if FehlzeitApp is installed..." -ForegroundColor Yellow
$installedApp = "C:\Users\Hani.Allam\AppData\Local\FehlzeitApp\current\FehlzeitApp.exe"
if (Test-Path $installedApp) {
    $installedVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($installedApp).FileVersion
    Write-Host "   ✅ App is installed - Version: $installedVersion" -ForegroundColor Green
} else {
    Write-Host "   ❌ App not found at: $installedApp" -ForegroundColor Red
    Write-Host "   Please install the app first using the setup.exe" -ForegroundColor Yellow
    exit 1
}

# Step 2: Check update directories
Write-Host ""
Write-Host "2. Checking update directories..." -ForegroundColor Yellow

# Check desktop updates directory
$desktopUpdatesDir = "C:\Users\Hani.Allam\Desktop\Updates\FehlZeitApp"
if (Test-Path $desktopUpdatesDir) {
    Write-Host "   ✅ Desktop updates directory exists" -ForegroundColor Green
} else {
    Write-Host "   ❌ Desktop updates directory not found: $desktopUpdatesDir" -ForegroundColor Red
    Write-Host "   Creating directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $desktopUpdatesDir -Force | Out-Null
}

# Check app updates directory
$appUpdatesDir = "C:\Users\Hani.Allam\AppData\Local\FehlzeitApp\updates"
if (Test-Path $appUpdatesDir) {
    Write-Host "   ✅ App updates directory exists" -ForegroundColor Green
} else {
    Write-Host "   ❌ App updates directory not found: $appUpdatesDir" -ForegroundColor Red
    Write-Host "   Creating directory..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $appUpdatesDir -Force | Out-Null
}

# Step 3: Check for available updates
Write-Host ""
Write-Host "3. Checking for available updates..." -ForegroundColor Yellow

# Check desktop RELEASES file
$desktopReleasesFile = Join-Path $desktopUpdatesDir "RELEASES"
$appReleasesFile = Join-Path $appUpdatesDir "RELEASES"

$hasDesktopUpdates = $false
$hasAppUpdates = $false

if (Test-Path $desktopReleasesFile) {
    Write-Host "   ✅ Desktop RELEASES file exists" -ForegroundColor Green
    $hasDesktopUpdates = $true
    
    Write-Host "   Available versions on desktop:" -ForegroundColor Cyan
    Get-Content $desktopReleasesFile | ForEach-Object {
        if ($_ -match "FehlzeitApp-(\d+\.\d+\.\d+)-full\.nupkg") {
            Write-Host "     - Version $($matches[1])" -ForegroundColor White
        }
    }
} else {
    Write-Host "   ❌ Desktop RELEASES file not found" -ForegroundColor Red
}

if (Test-Path $appReleasesFile) {
    Write-Host "   ✅ App RELEASES file exists" -ForegroundColor Green
    $hasAppUpdates = $true
    
    Write-Host "   Available versions in app directory:" -ForegroundColor Cyan
    Get-Content $appReleasesFile | ForEach-Object {
        if ($_ -match "FehlzeitApp-(\d+\.\d+\.\d+)-full\.nupkg") {
            Write-Host "     - Version $($matches[1])" -ForegroundColor White
        }
    }
} else {
    Write-Host "   ❌ App RELEASES file not found" -ForegroundColor Red
}

# Step 4: Copy updates if needed
if ($hasDesktopUpdates -and -not $hasAppUpdates) {
    Write-Host ""
    Write-Host "4. Copying updates from desktop to app directory..." -ForegroundColor Yellow
    
    # Copy RELEASES file
    Copy-Item $desktopReleasesFile $appReleasesFile -Force
    Write-Host "   ✅ Copied RELEASES file" -ForegroundColor Green
    
    # Copy all .nupkg files
    $nupkgFiles = Get-ChildItem $desktopUpdatesDir -Filter "*.nupkg"
    foreach ($file in $nupkgFiles) {
        Copy-Item $file.FullName $appUpdatesDir -Force
        Write-Host "   ✅ Copied $($file.Name)" -ForegroundColor Green
    }
}

# Step 5: Version comparison
Write-Host ""
Write-Host "5. Comparing versions..." -ForegroundColor Yellow

if ($hasAppUpdates) {
    $availableVersions = @()
    Get-Content $appReleasesFile | ForEach-Object {
        if ($_ -match "FehlzeitApp-(\d+\.\d+\.\d+)-full\.nupkg") {
            $availableVersions += $matches[1]
        }
    }
    
    if ($availableVersions.Count -gt 0) {
        $latestVersion = ($availableVersions | Sort-Object {[Version]$_} | Select-Object -Last 1)
        Write-Host "   Latest available: $latestVersion" -ForegroundColor Green
        Write-Host "   Installed version: $installedVersion" -ForegroundColor Cyan
        
        if ([Version]$latestVersion -gt [Version]$installedVersion) {
            Write-Host "   ✅ UPDATE AVAILABLE! The app should detect this update." -ForegroundColor Green
        } else {
            Write-Host "   ℹ️  No update needed - installed version is current or newer" -ForegroundColor Yellow
        }
    }
}

# Step 6: Test update detection
Write-Host ""
Write-Host "6. Testing update detection..." -ForegroundColor Yellow
Write-Host "   Starting the app to test update detection..." -ForegroundColor White
Write-Host "   Watch for the update dialog!" -ForegroundColor Magenta
Write-Host ""

# Start the app
$updateExe = "C:\Users\Hani.Allam\AppData\Local\FehlzeitApp\Update.exe"
if (Test-Path $updateExe) {
    Start-Process $updateExe
    Write-Host "   ✅ App started via Update.exe" -ForegroundColor Green
} else {
    Start-Process $installedApp
    Write-Host "   ✅ App started directly" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Green
Write-Host "✅ App installation: $($installedVersion)" -ForegroundColor White
Write-Host "✅ Update directories: Configured" -ForegroundColor White
$updateStatus = if ($hasAppUpdates) { 'Available' } else { 'Not found' }
Write-Host "✅ Update files: $updateStatus" -ForegroundColor White
Write-Host ""
Write-Host "If you see an update dialog, the system is working correctly!" -ForegroundColor Green
Write-Host "If no dialog appears, check the update.log file for errors." -ForegroundColor Yellow
