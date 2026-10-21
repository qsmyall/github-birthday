using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
#if UNINSTALLER
        BirthdayGreetingSetup.Uninstall();
#else
        if(args.Length>0&&String.Equals(args[0],"--uninstall",StringComparison.OrdinalIgnoreCase))BirthdayGreetingSetup.Uninstall();
        else BirthdayGreetingSetup.Install();
#endif
    }
}

static class BirthdayGreetingSetup
{
    const string ProductFolder="BirthdayGreeting";
    const string ProductName="\u751f\u65e5\u795d\u798f";
    const string PayloadName="BirthdayGreetingPayload.exe";
    const string UninstallerPayloadName="BirthdayGreetingUninstallerPayload.exe";
    static string InstallFolder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs",ProductFolder);}}
    static string ApplicationPath {get{return Path.Combine(InstallFolder,"BirthdayGreeting.exe");}}
    static string UninstallerPath {get{return Path.Combine(InstallFolder,"BirthdayGreeting_Uninstall.exe");}}
    static string StartMenuFolder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs));}}
    static string ApplicationShortcut {get{return Path.Combine(StartMenuFolder,"BirthdayGreeting.lnk");}}
    static string UninstallShortcut {get{return Path.Combine(StartMenuFolder,"Uninstall BirthdayGreeting.lnk");}}

    public static void Install()
    {
        if(MessageBox.Show("\u662f\u5426\u4e3a\u5f53\u524d Windows \u7528\u6237\u5b89\u88c5\u751f\u65e5\u795d\u798f\uff1f","\u5b89\u88c5\u751f\u65e5\u795d\u798f",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        try
        {
            Directory.CreateDirectory(InstallFolder);
            WritePayload(ApplicationPath,PayloadName);
            WritePayload(UninstallerPath,UninstallerPayloadName);
            CreateShortcut(ApplicationShortcut,ApplicationPath,"",ProductName);
            CreateShortcut(UninstallShortcut,UninstallerPath,"","\u5378\u8f7d\u751f\u65e5\u795d\u798f");
            MessageBox.Show("\u5b89\u88c5\u5b8c\u6210\u3002\n\n\u5df2\u521b\u5efa\u5f00\u59cb\u83dc\u5355\u5feb\u6377\u65b9\u5f0f\u3002","\u751f\u65e5\u795d\u798f",MessageBoxButtons.OK,MessageBoxIcon.Information);
            Process.Start(ApplicationPath);
        }
        catch(Exception error)
        {
            MessageBox.Show("\u5b89\u88c5\u5931\u8d25\uff1a\n"+error.Message,"\u751f\u65e5\u795d\u798f",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
    }

    public static void Uninstall()
    {
        if(MessageBox.Show("\u5378\u8f7d\u5c06\u79fb\u9664\u7a0b\u5e8f\u4e0e\u5feb\u6377\u65b9\u5f0f\uff0c\u4fdd\u7559\u672c\u5730\u751f\u6210\u8bb0\u5f55\u3002\n\n\u662f\u5426\u7ee7\u7eed\uff1f","\u5378\u8f7d\u751f\u65e5\u795d\u798f",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        try
        {
            DeleteIfPresent(ApplicationShortcut);DeleteIfPresent(UninstallShortcut);DeleteIfPresent(ApplicationPath);
            MessageBox.Show("\u5378\u8f7d\u5df2\u5b8c\u6210\u3002\u672c\u5730\u751f\u6210\u8bb0\u5f55\u672a\u88ab\u5220\u9664\u3002","\u751f\u65e5\u795d\u798f",MessageBoxButtons.OK,MessageBoxIcon.Information);
            ScheduleFolderRemoval(InstallFolder);
        }
        catch(Exception error)
        {
            MessageBox.Show("\u5378\u8f7d\u5931\u8d25\uff1a\n"+error.Message,"\u751f\u65e5\u795d\u798f",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
    }

    static void WritePayload(string outputPath,string resourceName)
    {
        using(Stream input=Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
        {
            if(input==null)throw new InvalidOperationException("\u672a\u627e\u5230\u5b89\u88c5\u8d44\u6e90\u3002");
            using(FileStream output=new FileStream(outputPath,FileMode.Create,FileAccess.Write,FileShare.None))input.CopyTo(output);
        }
    }
    static void CreateShortcut(string shortcutPath,string targetPath,string arguments,string description)
    {
        Type shellType=Type.GetTypeFromProgID("WScript.Shell");if(shellType==null)return;object shell=Activator.CreateInstance(shellType);
        object shortcut=shellType.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod|BindingFlags.Public|BindingFlags.Instance,null,shell,new object[]{shortcutPath});Type shortcutType=shortcut.GetType();
        shortcutType.InvokeMember("TargetPath",BindingFlags.SetProperty|BindingFlags.Public|BindingFlags.Instance,null,shortcut,new object[]{targetPath});
        shortcutType.InvokeMember("WorkingDirectory",BindingFlags.SetProperty|BindingFlags.Public|BindingFlags.Instance,null,shortcut,new object[]{Path.GetDirectoryName(targetPath)});
        shortcutType.InvokeMember("Arguments",BindingFlags.SetProperty|BindingFlags.Public|BindingFlags.Instance,null,shortcut,new object[]{arguments});
        shortcutType.InvokeMember("Description",BindingFlags.SetProperty|BindingFlags.Public|BindingFlags.Instance,null,shortcut,new object[]{description});
        shortcutType.InvokeMember("Save",BindingFlags.InvokeMethod|BindingFlags.Public|BindingFlags.Instance,null,shortcut,null);
    }
    static void DeleteIfPresent(string path){if(File.Exists(path))File.Delete(path);}
    static void ScheduleFolderRemoval(string folder)
    {
        string command="/c timeout /t 2 /nobreak >nul & rd /s /q \""+folder+"\"";
        Process.Start(new ProcessStartInfo{FileName=Environment.GetEnvironmentVariable("ComSpec")??"cmd.exe",Arguments=command,UseShellExecute=false,CreateNoWindow=true});
    }
}