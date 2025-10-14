# FehlzeitApp - Framework-Dependent Publishing
# This script publishes the app without .NET runtime (requires .NET 9.0 on target machine)

Write-Host "=== FehlzeitApp Framework-Dependent Publishing ===" -ForegroundColor Green
Write-Host ""

# Step 1: Get current version from .csproj
Write-Host "1. Reading current version..." -ForegroundColor Yellow
$csprojPath = "FehlzeitApp.csproj"
$content = Get-Content $csprojPath -Raw

# Extract current version
if ($content -match '<AssemblyVersion>(\d+)\.(\d+)\.(\d+)\.(\d+)</AssemblyVersion>') {
    $major = [int]$matches[1]
    $minor = [int]$matches[2]
    $patch = [int]$matches[3]
    $build = [int]$matches[4]
    
    $currentVersion = "$major.$minor.$patch"
    $currentVersionFull = "$major.$minor.$patch.$build"
    
    Write-Host "   Current version: $currentVersionFull" -ForegroundColor Cyan
} else {
    Write-Host "   ❌ Could not read current version" -ForegroundColor Red
    exit 1
}

# Step 2: Publish as framework-dependent
Write-Host "2. Publishing as framework-dependent..." -ForegroundColor Yellow
Write-Host "   This will NOT include .NET runtime (~20MB)" -ForegroundColor Gray
Write-Host "   Target machine must have .NET 9.0 installed" -ForegroundColor Gray

# Create output directory
$outputDir = "bin\Release\net9.0-windows\publish"
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null

# Publish without .NET runtime
dotnet publish -c Release -r win-x64 --self-contained false -o $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Framework-dependent application published" -ForegroundColor Green
} else {
    Write-Host "   ❌ Publishing failed" -ForegroundColor Red
    exit 1
}

# Step 3: Create Velopack package
Write-Host "3. Creating Velopack package..." -ForegroundColor Yellow
$vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
$velopackOutputDir = "C:\Users\Hani.Allam\Desktop\Updates\FehlzeitApp"

# Use the correct vpk pack command
& $vpkPath pack --packId "FehlzeitApp" --packVersion $currentVersion --packDir $outputDir --outputDir $velopackOutputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Velopack package created" -ForegroundColor Green
} else {
    Write-Host "   ❌ Velopack packaging failed" -ForegroundColor Red
    exit 1
}

# Step 4: Copy updates to app directory
Write-Host "4. Copying updates to app directory..." -ForegroundColor Yellow
$sourceDir = "C:\Users\Hani.Allam\Desktop\Updates\FehlzeitApp"
$targetDir = "C:\Users\Hani.Allam\AppData\Local\FehlzeitApp\updates"

# Create target directory if it doesn't exist
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

# Copy RELEASES file
$releasesFile = Join-Path $sourceDir "RELEASES"
if (Test-Path $releasesFile) {
    Copy-Item $releasesFile $targetDir -Force
    Write-Host "   ✅ Copied RELEASES file" -ForegroundColor Green
}

# Copy all .nupkg files
$nupkgFiles = Get-ChildItem $sourceDir -Filter "*.nupkg"
foreach ($file in $nupkgFiles) {
    Copy-Item $file.FullName $targetDir -Force
    Write-Host "   ✅ Copied $($file.Name)" -ForegroundColor Green
}

# Step 5: Show results
Write-Host ""
Write-Host "🎉 FehlzeitApp Framework-Dependent published successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Files created:" -ForegroundColor Cyan
Write-Host "   - FehlzeitApp-$currentVersion-full.nupkg (framework-dependent)" -ForegroundColor White
Write-Host "   - FehlzeitApp-win-Setup.exe (installer)" -ForegroundColor White
Write-Host "   - Updated RELEASES file" -ForegroundColor White
Write-Host "   - Updates copied to app directory" -ForegroundColor White
Write-Host ""
Write-Host "Package size: ~20MB (requires .NET 9.0 on target machine)" -ForegroundColor Yellow
Write-Host "Target: Windows x64 (framework-dependent)" -ForegroundColor Yellow
Write-Host ""
Write-Host "⚠️  IMPORTANT: Target machines must have .NET 9.0 installed!" -ForegroundColor Red
Write-Host "   Download from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "   1. Ensure target machines have .NET 9.0 installed" -ForegroundColor White
Write-Host "   2. Run the setup.exe to install the app" -ForegroundColor White
Write-Host "   3. Updates will be detected automatically" -ForegroundColor White
Write-Host ""
Write-Host "To test: Run the setup.exe and install the app" -ForegroundColor Magenta
