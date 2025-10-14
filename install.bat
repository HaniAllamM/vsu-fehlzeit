@echo off
echo Installing FehlzeitApp v1.0.1...
echo.

REM Create installation directory
set "INSTALL_DIR=%USERPROFILE%\FehlzeitApp"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

REM Extract files
echo Extracting files to %INSTALL_DIR%...
powershell -command "Expand-Archive -Path '%~dp0FehlzeitApp-v1.0.1.zip' -DestinationPath '%INSTALL_DIR%' -Force"

REM Create desktop shortcut
echo Creating desktop shortcut...
set "DESKTOP=%USERPROFILE%\Desktop"
set "SHORTCUT=%DESKTOP%\FehlzeitApp.lnk"
powershell -command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT%'); $Shortcut.TargetPath = '%INSTALL_DIR%\FehlzeitApp.exe'; $Shortcut.WorkingDirectory = '%INSTALL_DIR%'; $Shortcut.Description = 'FehlzeitApp - Professional Edition'; $Shortcut.Save()"

echo.
echo Installation completed!
echo FehlzeitApp has been installed to: %INSTALL_DIR%
echo A desktop shortcut has been created.
echo.
echo You can now run FehlzeitApp from the desktop shortcut or by double-clicking FehlzeitApp.exe in the installation folder.
echo.
pause
