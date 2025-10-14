# FehlzeitApp - Auto Version Publishing Script
# This script automatically increments the version and publishes

Write-Host "=== FehlzeitApp Auto Version Publishing ===" -ForegroundColor Green
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
    
    # Auto-increment patch version
    $patch++
    $newVersion = "$major.$minor.$patch"
    $newVersionFull = "$major.$minor.$patch.$build"
    
    Write-Host "   Current version: $major.$minor.$([int]$matches[3]).$build" -ForegroundColor Cyan
    Write-Host "   New version: $newVersion" -ForegroundColor Green
} else {
    Write-Host "   ❌ Could not read current version" -ForegroundColor Red
    exit 1
}

# Step 2: Update version in .csproj
Write-Host "2. Updating version to $newVersion..." -ForegroundColor Yellow
$content = $content -replace '<AssemblyVersion>\d+\.\d+\.\d+\.\d+</AssemblyVersion>', "<AssemblyVersion>$newVersionFull</AssemblyVersion>"
$content = $content -replace '<FileVersion>\d+\.\d+\.\d+\.\d+</FileVersion>', "<FileVersion>$newVersionFull</FileVersion>"
Set-Content $csprojPath $content
Write-Host "   ✅ Version updated in .csproj" -ForegroundColor Green

# Step 3: Build in Release mode
Write-Host "3. Building in Release mode..." -ForegroundColor Yellow
dotnet build -c Release -nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed" -ForegroundColor Red
    exit 1
}

# Step 4: Pack the update with correct URL
Write-Host "4. Creating update package..." -ForegroundColor Yellow
$vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
$outputDir = "C:\Users\Hani.Allam\Desktop\Updates\FehlzeitApp"

# Use the correct vpk pack command
& $vpkPath pack --packId "FehlzeitApp" --packVersion $newVersion --packDir "bin\Release\net9.0-windows" --outputDir $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Update package created" -ForegroundColor Green
} else {
    Write-Host "   ❌ Packaging failed" -ForegroundColor Red
    exit 1
}

# Step 5: Show results
Write-Host ""
Write-Host "🎉 Version $newVersion published successfully!" -ForegroundColor Green
Write-Host ""

# Step 6: Copy updates to app directory
Write-Host "5. Copying updates to app directory..." -ForegroundColor Yellow
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

Write-Host ""
Write-Host "Files created:" -ForegroundColor Cyan
Write-Host "   - FehlzeitApp-$newVersion-full.nupkg" -ForegroundColor White
Write-Host "   - FehlzeitApp-win-Setup.exe" -ForegroundColor White
Write-Host "   - Updated RELEASES file" -ForegroundColor White
Write-Host "   - Updates copied to app directory" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "   1. Run the installed app - it should detect version $newVersion" -ForegroundColor White
Write-Host "   2. You should see an update dialog!" -ForegroundColor White
Write-Host ""
Write-Host "To test: Start the installed app and look for update dialog" -ForegroundColor Magenta
