# FehlzeitApp - Publish New Version Script
# This script automates the process of publishing a new version

param(
    [Parameter(Mandatory=$true)]
    [string]$NewVersion
)

Write-Host "=== Publishing FehlzeitApp Version $NewVersion ===" -ForegroundColor Green
Write-Host ""

# Step 1: Update version in .csproj
Write-Host "1. Updating version to $NewVersion..." -ForegroundColor Yellow
$csprojPath = "FehlzeitApp.csproj"
$content = Get-Content $csprojPath -Raw
$content = $content -replace '<AssemblyVersion>\d+\.\d+\.\d+\.\d+</AssemblyVersion>', "<AssemblyVersion>$NewVersion</AssemblyVersion>"
$content = $content -replace '<FileVersion>\d+\.\d+\.\d+\.\d+</FileVersion>', "<FileVersion>$NewVersion</FileVersion>"
Set-Content $csprojPath $content
Write-Host "   ✅ Version updated in .csproj" -ForegroundColor Green

# Step 2: Build in Release mode
Write-Host "2. Building in Release mode..." -ForegroundColor Yellow
dotnet build -c Release -nologo
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "   ❌ Build failed" -ForegroundColor Red
    exit 1
}

# Step 3: Pack the update with correct URL
Write-Host "3. Creating update package..." -ForegroundColor Yellow
$vpkPath = "$env:USERPROFILE\.dotnet\tools\vpk.exe"
$outputDir = "C:\Users\Hani.Allam\Desktop\Updates\FehlzeitApp"

# Use the correct vpk pack command
& $vpkPath pack --packId "FehlzeitApp" --packVersion $NewVersion --packDir "bin\Release\net9.0-windows" --outputDir $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ Update package created" -ForegroundColor Green
} else {
    Write-Host "   ❌ Packaging failed" -ForegroundColor Red
    exit 1
}

# Step 4: Show results
Write-Host ""
Write-Host "🎉 Version $NewVersion published successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Files created:" -ForegroundColor Cyan
Write-Host "   - FehlzeitApp-$NewVersion-full.nupkg" -ForegroundColor White
Write-Host "   - FehlzeitApp-win-Setup.exe" -ForegroundColor White
Write-Host "   - Updated RELEASES file" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "   1. Test the update by running the installed app" -ForegroundColor White
Write-Host "   2. The app should detect the new version and offer to update" -ForegroundColor White
Write-Host ""
Write-Host "To test: Run the installed app and check for update dialog" -ForegroundColor Magenta
