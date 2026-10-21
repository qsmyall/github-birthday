Birthday Greeting source package

Files:
- BirthdayGreeting.cs: application source
- BirthdayGreetingInstaller.cs: standalone installer and one-click uninstaller source
- Build.cmd: builds BirthdayGreeting.exe
- BuildInstaller.cmd: builds BirthdayGreeting_Setup.exe and BirthdayGreeting_Uninstall.exe

Build requirements:
Windows with the .NET Framework 4.x compiler available at the standard Framework or Framework64 path.

The installer is per-user and creates Start menu entries for the application and one-click uninstaller.
The settings screen can open the custom music folder. Place MP3 or WAV files in the folder, refresh the list, and select a track for the celebration loop. The internally generated melody remains available as a fallback.
Custom music folder:
%LOCALAPPDATA%\BirthdayGreeting\Music
Installed application records:
%LOCALAPPDATA%\BirthdayGreeting\birthday_records.xml
