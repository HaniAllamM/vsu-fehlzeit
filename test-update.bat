@echo off
echo Testing FehlzeitApp Update System...
echo.

echo Current installed version: 1.0.7 (or earlier)
echo Latest available version: 1.0.8
echo.

echo Running the installer to check for updates...
echo You should see an update dialog if a newer version is available.
echo.

Start-Process -FilePath ".\dist\com.haniallam.fehlzeitapp-win-Setup.exe" -Wait

echo.
echo Update test completed!
echo.
pause
