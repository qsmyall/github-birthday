@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:winexe /out:BirthdayGreeting.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Xml.dll BirthdayGreeting.cs
endlocal