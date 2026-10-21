@echo off
setlocal
set "APP_DIR=%LOCALAPPDATA%\Programs\BirthdayGreeting"
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
if not exist "%APP_DIR%" mkdir "%APP_DIR%"
copy /y "%~dp0BirthdayGreeting.exe" "%APP_DIR%\BirthdayGreeting.exe" >nul
copy /y "%~dp0Uninstall.cmd" "%APP_DIR%\Uninstall.cmd" >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$shell=New-Object -ComObject WScript.Shell; $link=Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\BirthdayGreeting.lnk'; $shortcut=$shell.CreateShortcut($link); $shortcut.TargetPath=Join-Path $env:LOCALAPPDATA 'Programs\BirthdayGreeting\BirthdayGreeting.exe'; $shortcut.WorkingDirectory=Split-Path $shortcut.TargetPath; $shortcut.Save()"
if not exist "%APP_DIR%\BirthdayGreeting.exe" exit /b 1
start "" "%APP_DIR%\BirthdayGreeting.exe"
endlocal
exit /b 0