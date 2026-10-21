@echo off
setlocal
set "APP_DIR=%~dp0"
if "%APP_DIR:~-1%"=="\" set "APP_DIR=%APP_DIR:~0,-1%"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Remove-Item -LiteralPath (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\BirthdayGreeting.lnk') -Force -ErrorAction SilentlyContinue"
start "" /b cmd.exe /c "timeout /t 2 /nobreak >nul & rd /s /q ""%APP_DIR%"""
endlocal
exit /b 0