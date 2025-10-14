# FehlzeitApp - Publishing Options
# This script provides different publishing options for your app

Write-Host "=== FehlzeitApp Publishing Options ===" -ForegroundColor Green
Write-Host ""

Write-Host "Choose your publishing option:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Self-contained with .NET (Recommended for distribution)" -ForegroundColor Cyan
Write-Host "   - Size: ~100MB" -ForegroundColor Gray
Write-Host "   - Includes .NET runtime" -ForegroundColor Gray
Write-Host "   - Works on any Windows machine" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Framework-dependent (Smaller size)" -ForegroundColor Cyan
Write-Host "   - Size: ~20MB" -ForegroundColor Gray
Write-Host "   - Requires .NET 9.0 installed on target machine" -ForegroundColor Gray
Write-Host "   - Faster startup" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Single file (Portable)" -ForegroundColor Cyan
Write-Host "   - Size: ~80MB" -ForegroundColor Gray
Write-Host "   - Single executable file" -ForegroundColor Gray
Write-Host "   - Includes .NET runtime" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Trimmed (Smallest with .NET)" -ForegroundColor Cyan
Write-Host "   - Size: ~60MB" -ForegroundColor Gray
Write-Host "   - Includes .NET runtime (trimmed)" -ForegroundColor Gray
Write-Host "   - May have compatibility issues" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Enter your choice (1-4)"

switch ($choice) {
    "1" {
        Write-Host "Publishing as self-contained with .NET..." -ForegroundColor Green
        .\publish-with-dotnet.ps1
    }
    "2" {
        Write-Host "Publishing as framework-dependent..." -ForegroundColor Green
        .\publish-framework-dependent.ps1
    }
    "3" {
        Write-Host "Publishing as single file..." -ForegroundColor Green
        .\publish-single-file.ps1
    }
    "4" {
        Write-Host "Publishing as trimmed..." -ForegroundColor Green
        .\publish-trimmed.ps1
    }
    default {
        Write-Host "Invalid choice. Please run the script again." -ForegroundColor Red
        exit 1
    }
}
