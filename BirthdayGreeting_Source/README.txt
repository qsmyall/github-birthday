Birthday Greeting source package

Files:
- BirthdayGreeting.cs: application source
- Build.cmd: builds BirthdayGreeting.exe with the Windows .NET Framework compiler
- Install.cmd and Uninstall.cmd: per-user installation scripts
- BirthdayGreetingInstaller.sed and BuildInstaller.cmd: rebuild the IExpress installer

Build requirements:
Windows with .NET Framework 4.x compiler available at the standard Framework or Framework64 path.

The installed application saves its local records under:
%LOCALAPPDATA%\BirthdayGreeting\birthday_records.xml