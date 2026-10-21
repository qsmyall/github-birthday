using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Media;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

enum ThemeKind { Rose, Galaxy, Forest }
class Program { [STAThread] static void Main(){ Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new BirthdayApp()); } }

class BirthdayApp : Form
{
    const int DesignWidth=1160, DesignHeight=740; const string AutoStartRegistryPath=@"Software\Microsoft\Windows\CurrentVersion\Run",AutoStartValueName="BirthdayGreeting"; ThemeKind selected=ThemeKind.Rose; Panel setup=new Panel(),editor=new Panel(),celebration=new Panel(),top=new Panel(); SetupPreview preview; Dictionary<Control,Rectangle> baseBounds=new Dictionary<Control,Rectangle>(); Dictionary<Control,Font> baseFonts=new Dictionary<Control,Font>(); Dictionary<Control,Font> scaledFonts=new Dictionary<Control,Font>(); float viewportScale=1F; bool layoutReady, fullScreen, switchingFullScreen, updatingAutoStart, updatingLaunchPreference, updatingMusicTracks; FormBorderStyle savedBorder; FormWindowState savedState; Rectangle savedBounds;
    TextBox recipientName=new TextBox(); ComboBox relation=new ComboBox(),musicTrack=new ComboBox(); GradientStylePicker style=new GradientStylePicker(); NumericUpDown age=new NumericUpDown(),wordCount=new NumericUpDown(); CheckBox autoStart=new CheckBox(),launchLastCelebration=new CheckBox(),musicEnabled=new CheckBox(); WishDocument document; LocalDatabase database; GreetingRecord currentRecord;
    PartyCanvas party=new PartyCanvas(); Label title=new Label(),subtitle=new Label(),tag=new Label(),pageTitle=new Label(); Button replay=new Button(),edit=new Button(),view=new Button(),chooseStartupRecord=new Button(),musicToggle=new Button(),openMusicFolder=new Button(),refreshMusicTracks=new Button(); BirthdayMusicPlayer music=new BirthdayMusicPlayer(); Color accent=Color.FromArgb(202,61,91);
    public BirthdayApp()
    {
        database=LocalRecordStore.Load();
        Text="\u751f\u65e5\u795d\u798f";StartPosition=FormStartPosition.CenterScreen;ClientSize=new Size(DesignWidth,DesignHeight);MinimumSize=new Size(920,640);Font=new Font("Microsoft YaHei UI",10F);BackColor=Color.Black;AutoScaleMode=AutoScaleMode.None;KeyPreview=true;
        setup.Bounds=new Rectangle(0,0,DesignWidth,DesignHeight);celebration.Bounds=new Rectangle(0,0,DesignWidth,DesignHeight);
        celebration.VisibleChanged+=(s,e)=>UpdateMusicPlayback();
        BuildSetup();BuildCelebration();Controls.Add(celebration);Controls.Add(setup);CaptureBaseLayout(setup);CaptureBaseLayout(celebration);layoutReady=true;Resize+=(s,e)=>{if(!switchingFullScreen)ApplyViewport();};KeyDown+=(s,e)=>{if(e.KeyCode==Keys.F11&&celebration.Visible){ToggleFullScreen();e.Handled=true;}else if(e.KeyCode==Keys.Escape&&fullScreen){ToggleFullScreen();e.Handled=true;}};FormClosing+=(s,e)=>{music.Stop();SaveLastSession();};Shown+=(s,e)=>OpenLastSessionIfRequested();ApplyViewport();ShowSetup();
    }
    void BuildSetup()
    {
        setup.Dock=DockStyle.None;setup.Paint+=(s,e)=>DrawSetupBackground(e.Graphics,setup.ClientRectangle);
        Label brand=LabelOf("BIRTHDAY ATELIER",10,FontStyle.Bold,Color.FromArgb(146,64,88));brand.Location=new Point(43,26);setup.Controls.Add(brand);
        Label h=LabelOf("\u4e3a\u91cd\u8981\u7684\u4eba\uff0c\u5199\u4e00\u5c01\u751f\u65e5\u4fe1",27,FontStyle.Bold,Color.FromArgb(40,48,63));h.Location=new Point(41,55);setup.Controls.Add(h);
        Label d=LabelOf("\u586b\u5199\u59d3\u540d\u4e0e\u521b\u4f5c\u4fe1\u606f\uff0c\u751f\u6210\u4e00\u4efd\u4e13\u5c5e\u7684\u751f\u65e5\u795d\u798f\u3002",11,FontStyle.Regular,Color.FromArgb(100,111,124));d.Location=new Point(44,101);setup.Controls.Add(d);
        preview=new SetupPreview();preview.Theme=selected;setup.Controls.Add(preview);
        editor.Size=new Size(520,555);editor.BackColor=Color.White;editor.Paint+=DrawEditor;setup.Controls.Add(editor);
        Label et=LabelOf("\u521b\u4f5c\u4fe1\u606f",18,FontStyle.Bold,Color.FromArgb(43,52,67));et.Location=new Point(31,26);editor.Controls.Add(et);
        Button history=new Button{Text="\u5386\u53f2\u8bb0\u5f55",Size=new Size(106,29),Location=new Point(382,25),Font=new Font("Microsoft YaHei UI",9,FontStyle.Bold)};Outline(history);history.Click+=(s,e)=>ShowHistory();editor.Controls.Add(history);
        Label es=LabelOf("\u5173\u7cfb\u53ea\u7528\u4e8e\u8c03\u6574\u795d\u798f\u8bed\u6c14\uff0c\u5e86\u5178\u4f1a\u4e3a\u59d3\u540d\u800c\u70b9\u4eae\u3002",9,FontStyle.Regular,Color.FromArgb(131,141,150));es.Location=new Point(32,58);editor.Controls.Add(es);
        recipientName.Text="\u5bff\u661f";AddField("\u5bf9\u65b9\u59d3\u540d",recipientName,94);
        relation.DropDownStyle=ComboBoxStyle.DropDownList;relation.Items.AddRange(new object[]{"\u957f\u8f88","\u670b\u53cb","\u604b\u4eba","\u540c\u5b66","\u540c\u4e8b"});relation.SelectedIndex=1;AddField("\u5bf9\u65b9\u8eab\u4efd\u5173\u7cfb",relation,32,157,218);
        age.Minimum=1;age.Maximum=120;age.Value=18;AddField("\u5e74\u9f84",age,270,157,218);
        Label sl=LabelOf("\u60f3\u8981\u98ce\u683c",9,FontStyle.Bold,Color.FromArgb(73,82,95));sl.Location=new Point(32,220);editor.Controls.Add(sl);
        style.Location=new Point(32,242);style.Size=new Size(456,47);style.SelectedStyleChanged+=(s,e)=>UpdatePreviewTheme();editor.Controls.Add(style);
        wordCount.Minimum=80;wordCount.Maximum=300;wordCount.Increment=10;wordCount.Value=150;AddField("\u5b8c\u6574\u7248\u76ee\u6807\u5b57\u6570",wordCount,306);
        Label note=LabelOf("\u98ce\u683c\u4ee5\u6e10\u53d8\u8272\u5207\u6362\uff0c\u53ef\u5c06 MP3 \u6216 WAV \u653e\u5165\u97f3\u4e50\u6587\u4ef6\u5939\u3002",9,FontStyle.Regular,Color.FromArgb(125,136,146));note.Location=new Point(32,370);editor.Controls.Add(note);
        autoStart.Text="\u5f00\u673a\u81ea\u52a8\u6253\u5f00";autoStart.AutoSize=false;autoStart.Size=new Size(208,25);autoStart.Location=new Point(276,426);autoStart.Font=new Font("Microsoft YaHei UI",9.3f);autoStart.ForeColor=Color.FromArgb(73,82,95);autoStart.BackColor=Color.White;autoStart.FlatStyle=FlatStyle.Flat;autoStart.Checked=IsAutoStartEnabled();autoStart.CheckedChanged+=(s,e)=>{if(!updatingAutoStart)SaveAutoStartPreference();};editor.Controls.Add(autoStart);
        launchLastCelebration.Text="\u542f\u52a8\u65f6\u76f4\u63a5\u5168\u5c4f\u5e86\u795d\u9009\u4e2d\u8bb0\u5f55";launchLastCelebration.AutoSize=false;launchLastCelebration.Size=new Size(292,25);launchLastCelebration.Location=new Point(32,451);launchLastCelebration.Font=new Font("Microsoft YaHei UI",9.3f);launchLastCelebration.ForeColor=Color.FromArgb(73,82,95);launchLastCelebration.BackColor=Color.White;launchLastCelebration.FlatStyle=FlatStyle.Flat;launchLastCelebration.Checked=database.LaunchLastCelebration;launchLastCelebration.CheckedChanged+=(s,e)=>{if(!updatingLaunchPreference)SaveLaunchPreference();};editor.Controls.Add(launchLastCelebration);
        chooseStartupRecord.Size=new Size(150,30);chooseStartupRecord.Location=new Point(338,448);chooseStartupRecord.Font=new Font("Microsoft YaHei UI",8.5f,FontStyle.Bold);Outline(chooseStartupRecord);chooseStartupRecord.Click+=(s,e)=>ChooseStartupRecord();editor.Controls.Add(chooseStartupRecord);UpdateStartupRecordButton();
        Button create=new Button{Text="\u751f\u6210\u795d\u798f\u5e76\u5f00\u59cb\u5e86\u5178  \u25b6",Size=new Size(456,48),Location=new Point(32,489),Font=new Font("Microsoft YaHei UI",11,FontStyle.Bold)};Fill(create,accent);create.Click+=(s,e)=>ShowCelebration();editor.Controls.Add(create);
        ConfigureMusicOption();
        LayoutSetup();
    }
    void ConfigureMusicOption()
    {
        musicTrack.DropDownStyle=ComboBoxStyle.DropDownList;
        musicTrack.Location=new Point(32,390);
        musicTrack.Size=new Size(246,31);
        musicTrack.Font=new Font("Microsoft YaHei UI",9.3f);
        musicTrack.SelectedIndexChanged+=(s,e)=>SaveMusicTrack();
        editor.Controls.Add(musicTrack);
        refreshMusicTracks.Text="\u5237\u65b0";
        refreshMusicTracks.Size=new Size(62,31);
        refreshMusicTracks.Location=new Point(284,390);
        refreshMusicTracks.Font=new Font("Microsoft YaHei UI",8.5f,FontStyle.Bold);
        Outline(refreshMusicTracks);
        refreshMusicTracks.Click+=(s,e)=>RefreshMusicTracks();
        editor.Controls.Add(refreshMusicTracks);
        openMusicFolder.Text="\u6253\u5f00\u97f3\u4e50\u6587\u4ef6\u5939";
        openMusicFolder.Size=new Size(136,31);
        openMusicFolder.Location=new Point(352,390);
        openMusicFolder.Font=new Font("Microsoft YaHei UI",8.5f,FontStyle.Bold);
        Outline(openMusicFolder);
        openMusicFolder.Click+=(s,e)=>OpenMusicFolder();
        editor.Controls.Add(openMusicFolder);
        musicEnabled.Text="\u5e86\u795d\u65f6\u64ad\u653e\u80cc\u666f\u97f3\u4e50";
        musicEnabled.AutoSize=false;
        musicEnabled.Size=new Size(226,25);
        musicEnabled.Location=new Point(32,426);
        musicEnabled.Font=new Font("Microsoft YaHei UI",9.3f);
        musicEnabled.ForeColor=Color.FromArgb(73,82,95);
        musicEnabled.BackColor=Color.White;
        musicEnabled.FlatStyle=FlatStyle.Flat;
        musicEnabled.Checked=database.MusicEnabled;
        musicEnabled.CheckedChanged+=(s,e)=>SaveMusicPreference();
        editor.Controls.Add(musicEnabled);
        RefreshMusicTracks();
    }
    void RefreshMusicTracks()
    {
        string wanted=database.MusicFileName??"";
        updatingMusicTracks=true;
        try
        {
            musicTrack.BeginUpdate();
            musicTrack.Items.Clear();
            musicTrack.Items.Add(new MusicChoice("","\u5185\u7f6e\u751f\u65e5\u65cb\u5f8b"));
            List<string> tracks=BirthdayMusicPlayer.FindCustomTracks();
            int selectedIndex=0;
            for(int i=0;i<tracks.Count;i++)
            {
                string fileName=Path.GetFileName(tracks[i]);
                musicTrack.Items.Add(new MusicChoice(fileName,fileName));
                if(String.Equals(fileName,wanted,StringComparison.OrdinalIgnoreCase))selectedIndex=i+1;
            }
            musicTrack.SelectedIndex=selectedIndex;
            if(selectedIndex==0&&!String.IsNullOrEmpty(wanted)){database.MusicFileName="";SaveDatabase();}
        }
        finally
        {
            musicTrack.EndUpdate();
            updatingMusicTracks=false;
        }
    }
    void SaveMusicTrack()
    {
        if(updatingMusicTracks)return;
        MusicChoice choice=musicTrack.SelectedItem as MusicChoice;
        database.MusicFileName=choice==null?"":choice.FileName;
        TrackUsage("\u9009\u62e9\u80cc\u666f\u97f3\u4e50",currentRecord,String.IsNullOrEmpty(database.MusicFileName)?"\u5185\u7f6e\u751f\u65e5\u65cb\u5f8b":database.MusicFileName);
        SaveDatabase();
        UpdateMusicPlayback();
    }
    void OpenMusicFolder()
    {
        try
        {
            Directory.CreateDirectory(BirthdayMusicPlayer.MusicFolder);
            Process.Start(new ProcessStartInfo("explorer.exe","\""+BirthdayMusicPlayer.MusicFolder+"\""){UseShellExecute=true});
        }
        catch(Exception error)
        {
            MessageBox.Show(this,"\u65e0\u6cd5\u6253\u5f00\u97f3\u4e50\u6587\u4ef6\u5939\uff1a\n"+error.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }
    void AddField(string text,Control control,int y){AddField(text,control,32,y,456);}
    void AddField(string text,Control control,int x,int y,int width)
    {
        Label l=LabelOf(text,9,FontStyle.Bold,Color.FromArgb(73,82,95));l.Location=new Point(x,y);editor.Controls.Add(l);
        control.Location=new Point(x,y+22);control.Size=new Size(width,32);control.Font=new Font("Microsoft YaHei UI",10);control.BackColor=Color.FromArgb(252,253,254);editor.Controls.Add(control);
    }
    string AutoStartCommand(){return "\""+Application.ExecutablePath+"\"";}
    bool IsAutoStartEnabled()
    {
        try
        {
            using(RegistryKey key=Registry.CurrentUser.OpenSubKey(AutoStartRegistryPath,false))
            {
                object value=key==null?null:key.GetValue(AutoStartValueName);
                return String.Equals(value as string,AutoStartCommand(),StringComparison.OrdinalIgnoreCase);
            }
        }
        catch{return false;}
    }
    void SaveAutoStartPreference()
    {
        try
        {
            using(RegistryKey key=Registry.CurrentUser.CreateSubKey(AutoStartRegistryPath))
            {
                if(autoStart.Checked)key.SetValue(AutoStartValueName,AutoStartCommand(),RegistryValueKind.String);
                else key.DeleteValue(AutoStartValueName,false);
            }
        }
        catch
        {
            updatingAutoStart=true;
            autoStart.Checked=IsAutoStartEnabled();
            updatingAutoStart=false;
            MessageBox.Show(this,"\u65e0\u6cd5\u66f4\u65b0\u5f00\u673a\u542f\u52a8\u8bbe\u7f6e\uff0c\u8bf7\u68c0\u67e5\u5f53\u524d\u7528\u6237\u5bf9 Windows \u542f\u52a8\u9879\u7684\u6743\u9650\u3002",Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }
    void SaveMusicPreference()
    {
        database.MusicEnabled=musicEnabled.Checked;
        database.MusicPreferenceSet=true;
        TrackUsage(database.MusicEnabled?"\u5f00\u542f\u80cc\u666f\u97f3\u4e50":"\u5173\u95ed\u80cc\u666f\u97f3\u4e50",currentRecord,database.MusicEnabled?"\u5e86\u795d\u65f6\u5faa\u73af\u64ad\u653e":"\u5e86\u795d\u65f6\u4e0d\u64ad\u653e");
        SaveDatabase();
        UpdateMusicButton();
        UpdateMusicPlayback();
    }
    void ToggleMusic(){musicEnabled.Checked=!musicEnabled.Checked;}
    void UpdateMusicButton()
    {
        musicToggle.Text=database.MusicEnabled?"\u97f3\u4e50\uff1a\u5f00":"\u97f3\u4e50\uff1a\u5173";
        if(database.MusicEnabled)Fill(musicToggle,accent);else Outline(musicToggle);
    }
    void UpdateMusicPlayback(){UpdateMusicButton();if(database.MusicEnabled&&celebration.Visible)music.Play(database.MusicFileName);else music.Stop();}
    GreetingRecord StartupRecord()
    {
        if(String.IsNullOrEmpty(database.StartupRecordId))return null;for(int i=0;i<database.Generations.Count;i++)if(database.Generations[i].Id==database.StartupRecordId)return database.Generations[i];return null;
    }
    void UpdateStartupRecordButton()
    {
        GreetingRecord record=StartupRecord();if(record==null){chooseStartupRecord.Text="\u9009\u62e9\u542f\u52a8\u8bb0\u5f55";return;}string name=String.IsNullOrEmpty(record.Name)?"\u672a\u547d\u540d":record.Name.Trim();chooseStartupRecord.Text=name.Length>6?"\u5df2\u9009\uff1a"+name.Substring(0,6)+"...":"\u5df2\u9009\uff1a"+name;
    }
    void ChooseStartupRecord()
    {
        if(database.Generations.Count==0){MessageBox.Show(this,"\u8bf7\u5148\u751f\u6210\u6216\u65b0\u5efa\u4e00\u6761\u8bb0\u5f55\u3002",Text,MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
        GreetingRecord selectedRecord=null;using(HistoryForm history=new HistoryForm(database,true))if(history.ShowDialog(this)==DialogResult.OK)selectedRecord=history.SelectedRecord;
        if(selectedRecord==null)return;database.StartupRecordId=selectedRecord.Id;database.LaunchLastCelebration=true;updatingLaunchPreference=true;launchLastCelebration.Checked=true;updatingLaunchPreference=false;TrackUsage("\u9009\u62e9\u542f\u52a8\u5e86\u795d\u8bb0\u5f55",selectedRecord,"\u5c06\u5728\u6bcf\u6b21\u542f\u52a8\u65f6\u5168\u5c4f\u5e86\u795d");SaveDatabase();UpdateStartupRecordButton();
    }
    void SaveLaunchPreference()
    {
        if(launchLastCelebration.Checked&&StartupRecord()==null)
        {
            updatingLaunchPreference=true;launchLastCelebration.Checked=false;updatingLaunchPreference=false;database.LaunchLastCelebration=false;SaveDatabase();MessageBox.Show(this,"\u8bf7\u5148\u70b9\u51fb\u201c\u9009\u62e9\u542f\u52a8\u8bb0\u5f55\u201d\u3002",Text,MessageBoxButtons.OK,MessageBoxIcon.Information);return;
        }
        database.LaunchLastCelebration=launchLastCelebration.Checked;TrackUsage("\u66f4\u65b0\u542f\u52a8\u8bbe\u7f6e",currentRecord,launchLastCelebration.Checked?"\u5df2\u542f\u7528\u542f\u52a8\u5168\u5c4f\u5e86\u795d":"\u5df2\u5173\u95ed\u542f\u52a8\u5168\u5c4f\u5e86\u795d");SaveDatabase();
    }
    void TrackUsage(string action,GreetingRecord record,string detail)
    {
        database.Usage.Insert(0,new UsageRecord{Id=Guid.NewGuid().ToString("N"),OccurredAt=DateTime.Now.ToString("o"),Action=action,RecordId=record==null?"":record.Id,Subject=record==null?"":record.Name,Detail=detail??""});
        if(database.Usage.Count>300)database.Usage.RemoveRange(300,database.Usage.Count-300);
    }
    void SaveDatabase(){LocalRecordStore.Save(database);}
    GreetingRecord LastSavedRecord()
    {
        if(!String.IsNullOrEmpty(database.LastRecordId))for(int i=0;i<database.Generations.Count;i++)if(database.Generations[i].Id==database.LastRecordId)return database.Generations[i];
        return database.Generations.Count==0?null:database.Generations[0];
    }
    void SaveLastSession()
    {
        if(currentRecord!=null)
        {
            database.LastRecordId=currentRecord.Id;
            TrackUsage("\u9000\u51fa\u5e76\u4fdd\u5b58\u8bb0\u5f55",currentRecord,"\u4e0b\u6b21\u53ef\u4ece\u6b64\u8bb0\u5f55\u7ee7\u7eed\u5e86\u795d");
        }
        SaveDatabase();
    }
    void OpenLastSessionIfRequested()
    {
        if(!database.LaunchLastCelebration)return;
        GreetingRecord record=StartupRecord();
        if(record==null){record=LastSavedRecord();if(record!=null)database.StartupRecordId=record.Id;}
        if(record==null){database.LaunchLastCelebration=false;updatingLaunchPreference=true;launchLastCelebration.Checked=false;updatingLaunchPreference=false;SaveDatabase();UpdateStartupRecordButton();return;}
        SaveDatabase();OpenSavedRecord(record,"\u542f\u52a8\u5168\u5c4f\u5e86\u795d");
        EnterFullScreenCelebration();
    }
    void UpdatePreviewTheme(){selected=ThemeFor(style.SelectedStyle);preview.Theme=selected;preview.Invalidate();}
    ThemeKind ThemeFor(string value){if(value=="\u6587\u827a")return ThemeKind.Galaxy;if(value=="\u7b80\u7ea6")return ThemeKind.Forest;return ThemeKind.Rose;}
    void LayoutSetup(){int cardX=Math.Max(570,(DesignWidth+20)/2);editor.Left=Math.Min(cardX,DesignWidth-editor.Width-38);editor.Top=153;preview.Bounds=new Rectangle(40,145,Math.Max(450,editor.Left-88),540);}
    void BuildCelebration()
    {
        celebration.Dock=DockStyle.None;party.Dock=DockStyle.None;party.Bounds=new Rectangle(0,75,DesignWidth,DesignHeight-75);celebration.Controls.Add(party);top.Dock=DockStyle.None;top.Bounds=new Rectangle(0,0,DesignWidth,75);celebration.Controls.Add(top);
        pageTitle=LabelOf("BIRTHDAY ATELIER",10,FontStyle.Bold,Color.White);pageTitle.Location=new Point(31,26);top.Controls.Add(pageTitle);
        Label small=LabelOf("\u4e13\u5c5e\u5e86\u5178\u573a\u666f",10,FontStyle.Regular,Color.FromArgb(235,235,241));small.Location=new Point(180,26);top.Controls.Add(small);
        view=new Button{Text="\u67e5\u770b\u6587\u6848",Size=new Size(104,34),Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(850,20)};Outline(view);view.Click+=(s,e)=>ShowDocument();top.Controls.Add(view);
        edit=new Button{Text="\u2699  \u4fee\u6539\u8bbe\u7f6e",Size=new Size(126,34),Anchor=AnchorStyles.Top|AnchorStyles.Right,Location=new Point(992,20)};Outline(edit);edit.Click+=(s,e)=>ShowSetup();top.Controls.Add(edit);
        tag=LabelOf("",9,FontStyle.Bold,Color.White);tag.AutoSize=false;tag.TextAlign=ContentAlignment.MiddleCenter;tag.Size=new Size(112,25);party.Controls.Add(tag);
        title=new Label{AutoSize=false,TextAlign=ContentAlignment.MiddleCenter,Size=new Size(870,69),Font=new Font("Microsoft YaHei UI",32,FontStyle.Bold),BackColor=Color.Transparent};party.Controls.Add(title);
        subtitle=new Label{AutoSize=false,TextAlign=ContentAlignment.MiddleCenter,Size=new Size(760,55),Font=new Font("Microsoft YaHei UI",13),BackColor=Color.Transparent};party.Controls.Add(subtitle);
        replay=new Button{Text="\u8ba9\u5e78\u798f\u518d\u6b21\u7efd\u653e",Size=new Size(162,43),Font=new Font("Microsoft YaHei UI",10,FontStyle.Bold)};replay.Click+=(s,e)=>party.Celebrate();party.Controls.Add(replay);
        ConfigureMusicButton();
        LayoutParty();
    }
    void ConfigureMusicButton()
    {
        musicToggle.Size=new Size(96,34);
        musicToggle.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        musicToggle.Location=new Point(738,20);
        Outline(musicToggle);
        musicToggle.Click+=(s,e)=>ToggleMusic();
        top.Controls.Add(musicToggle);
        UpdateMusicButton();
    }
    void LayoutParty(){title.Left=(DesignWidth-title.Width)/2;title.Top=74;subtitle.Left=(DesignWidth-subtitle.Width)/2;subtitle.Top=148;tag.Left=(DesignWidth-tag.Width)/2;tag.Top=43;replay.Left=(DesignWidth-replay.Width)/2;replay.Top=DesignHeight-75-59;}
    void CaptureBaseLayout(Control root)
    {
        foreach(Control child in root.Controls){baseBounds[child]=child.Bounds;baseFonts[child]=child.Font;CaptureBaseLayout(child);}
    }
    Rectangle ScaleRect(Rectangle source,float scale){return new Rectangle((int)Math.Round(source.X*scale),(int)Math.Round(source.Y*scale),Math.Max(1,(int)Math.Round(source.Width*scale)),Math.Max(1,(int)Math.Round(source.Height*scale)));}
    void ApplyContentScale(Control root,float scale)
    {
        foreach(Control child in root.Controls)
        {
            Rectangle bounds;if(baseBounds.TryGetValue(child,out bounds))child.Bounds=ScaleRect(bounds,scale);
            Font original;if(baseFonts.TryGetValue(child,out original))SetScaledFont(child,original,scale);
            GradientStylePicker picker=child as GradientStylePicker;if(picker!=null)picker.RenderScale=scale;
            SetupPreview scene=child as SetupPreview;if(scene!=null)scene.RenderScale=scale;
            PartyCanvas canvas=child as PartyCanvas;if(canvas!=null)canvas.RenderScale=scale;
            ApplyContentScale(child,scale);
        }
    }
    void SetScaledFont(Control control,Font source,float scale)
    {
        Font old;Font next=new Font(source.FontFamily,Math.Max(1F,source.SizeInPoints*scale),source.Style,GraphicsUnit.Point);control.Font=next;if(scaledFonts.TryGetValue(control,out old)){scaledFonts.Remove(control);old.Dispose();}scaledFonts[control]=next;
    }
    void ApplyViewport()
    {
        if(!layoutReady||ClientSize.Width<1||ClientSize.Height<1)return;float scale=Math.Min((float)ClientSize.Width/DesignWidth,(float)ClientSize.Height/DesignHeight);viewportScale=scale;int width=(int)Math.Round(DesignWidth*scale),height=(int)Math.Round(DesignHeight*scale),x=(ClientSize.Width-width)/2,y=(ClientSize.Height-height)/2;Rectangle viewport=new Rectangle(x,y,width,height);setup.Bounds=viewport;celebration.Bounds=viewport;ApplyContentScale(setup,scale);ApplyContentScale(celebration,scale);ApplyCelebrationPresentation(scale);setup.Invalidate();celebration.Invalidate();
    }
    void ApplyCelebrationPresentation(float scale)
    {
        bool presentation=fullScreen&&celebration.Visible;
        top.Visible=!presentation;
        replay.Visible=!presentation;
        if(presentation)party.Bounds=ScaleRect(new Rectangle(0,0,DesignWidth,DesignHeight),scale);
    }
    void EnterFullScreenCelebration()
    {
        BeginInvoke((MethodInvoker)delegate{if(!IsDisposed&&!fullScreen&&!switchingFullScreen&&celebration.Visible)ToggleFullScreen();});
    }
    void ToggleFullScreen()
    {
        if(!celebration.Visible||switchingFullScreen)return;
        switchingFullScreen=true;
        if(!fullScreen)
        {
            savedBorder=FormBorderStyle;
            savedState=WindowState;
            savedBounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
            fullScreen=true;
            if(WindowState!=FormWindowState.Normal)WindowState=FormWindowState.Normal;
            FormBorderStyle=FormBorderStyle.None;
            WindowState=FormWindowState.Maximized;
        }
        else
        {
            fullScreen=false;
            WindowState=FormWindowState.Normal;
            FormBorderStyle=savedBorder;
            Bounds=savedBounds;
            if(savedState==FormWindowState.Maximized)WindowState=FormWindowState.Maximized;
        }
        BeginInvoke((MethodInvoker)delegate{if(IsDisposed)return;switchingFullScreen=false;ApplyViewport();});
    }    void ShowSetup(){if(fullScreen)ToggleFullScreen();party.Pause();music.Stop();celebration.Visible=false;setup.Visible=true;setup.BringToFront();}
    void ShowCelebration()
    {
        string name=recipientName.Text.Trim();string r=relation.Text;string s=style.SelectedStyle;if(s=="")s="\u6e29\u67d4\u6cbb\u6108";
        WishDocument created=WishWriter.Create(name,r,(int)age.Value,s,(int)wordCount.Value);
        GreetingRecord record=new GreetingRecord{Id=Guid.NewGuid().ToString("N"),CreatedAt=DateTime.Now.ToString("o"),UpdatedAt=DateTime.Now.ToString("o"),Name=name,Relation=r,Age=(int)age.Value,Style=s,WordCount=(int)wordCount.Value,ShortText=created.ShortText,FullText=created.FullText};
        database.Generations.Insert(0,record);if(database.Generations.Count>200)database.Generations.RemoveRange(200,database.Generations.Count-200);
        database.LastRecordId=record.Id;TrackUsage("\u751f\u6210\u795d\u798f",record,"\u5df2\u4fdd\u5b58\u751f\u6210\u5185\u5bb9");SaveDatabase();PresentRecord(record);EnterFullScreenCelebration();
    }
    void LoadRecordIntoEditor(GreetingRecord record)
    {
        if(record==null)return;
        recipientName.Text=record.Name??"";int relationIndex=relation.Items.IndexOf(record.Relation);if(relationIndex>=0)relation.SelectedIndex=relationIndex;
        age.Value=Math.Max((int)age.Minimum,Math.Min((int)age.Maximum,record.Age));wordCount.Value=Math.Max((int)wordCount.Minimum,Math.Min((int)wordCount.Maximum,record.WordCount));style.SelectStyle(record.Style);UpdatePreviewTheme();
    }
    void PresentRecord(GreetingRecord record)
    {
        if(record==null)return;
        currentRecord=record;document=new WishDocument{ShortText=record.ShortText??"",FullText=record.FullText??""};string s=String.IsNullOrEmpty(record.Style)?"\u6e29\u67d4\u6cbb\u6108":record.Style;selected=ThemeFor(s);preview.Theme=selected;preview.Invalidate();
        accent=selected==ThemeKind.Galaxy?Color.FromArgb(89,137,239):selected==ThemeKind.Forest?Color.FromArgb(43,132,101):s=="\u6d3b\u6cfc"?Color.FromArgb(235,121,61):Color.FromArgb(190,58,90);
        top.BackColor=Color.Black;tag.Text="";tag.BackColor=accent;title.Text="";subtitle.Text=document.ShortText;Fill(replay,accent);Outline(edit);Outline(view);
        party.Theme=selected;party.Accent=accent;party.Person=String.IsNullOrEmpty(record.Name)?"\u751f\u65e5\u5feb\u4e50\uff01":record.Name+"\uff0c\u751f\u65e5\u5feb\u4e50\uff01";party.SetWishes(document.ShortText);title.Visible=false;subtitle.Visible=false;tag.Visible=false;party.Invalidate();setup.Visible=false;celebration.Visible=true;celebration.BringToFront();ApplyViewport();party.Celebrate();
    }
    void OpenSavedRecord(GreetingRecord record,string action)
    {
        if(record==null)return;LoadRecordIntoEditor(record);database.LastRecordId=record.Id;TrackUsage(action,record,"\u5df2\u6253\u5f00\u4fdd\u5b58\u8bb0\u5f55");SaveDatabase();PresentRecord(record);
    }
    bool ContainsRecord(string id)
    {
        if(String.IsNullOrEmpty(id))return false;for(int i=0;i<database.Generations.Count;i++)if(database.Generations[i].Id==id)return true;return false;
    }
    void ShowHistory()
    {
        TrackUsage("\u67e5\u770b\u672c\u5730\u8bb0\u5f55",null,"\u6253\u5f00\u751f\u6210\u4e0e\u4f7f\u7528\u5386\u53f2");SaveDatabase();
        GreetingRecord selectedRecord=null;
        using(HistoryForm history=new HistoryForm(database))if(history.ShowDialog(this)==DialogResult.OK)selectedRecord=history.SelectedRecord;
        if(currentRecord!=null&&!ContainsRecord(currentRecord.Id))currentRecord=null;
        updatingLaunchPreference=true;launchLastCelebration.Checked=database.LaunchLastCelebration;updatingLaunchPreference=false;UpdateStartupRecordButton();SaveDatabase();
        if(selectedRecord!=null)OpenSavedRecord(selectedRecord,"\u6253\u5f00\u751f\u6210\u8bb0\u5f55");
    }
    void ShowDocument(){if(fullScreen)return;if(document==null)document=WishWriter.Create(recipientName.Text.Trim(),relation.Text,(int)age.Value,style.SelectedStyle,(int)wordCount.Value);new WishResultForm(document).ShowDialog(this);}
    Label LabelOf(string t,float sz,FontStyle fs,Color c){return new Label{Text=t,AutoSize=true,Font=new Font("Microsoft YaHei UI",sz,fs),ForeColor=c,BackColor=Color.Transparent};}
    void Fill(Button b,Color c){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderSize=0;b.BackColor=c;b.ForeColor=Color.White;b.Cursor=Cursors.Hand;}
    void Outline(Button b){b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(220,225,232);b.BackColor=Color.FromArgb(250,250,252);b.ForeColor=Color.FromArgb(74,81,99);b.Cursor=Cursors.Hand;}
    void DrawEditor(object s,PaintEventArgs e){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;int dx=Math.Max(1,(int)(6*viewportScale)),dy=Math.Max(1,(int)(8*viewportScale));using(Brush sh=new SolidBrush(Color.FromArgb(18,38,52,70)))e.Graphics.FillRectangle(sh,dx,dy,Math.Max(1,editor.Width-dx-4),Math.Max(1,editor.Height-dy-4));using(Pen p=new Pen(Color.FromArgb(226,231,236)))e.Graphics.DrawRectangle(p,0,0,Math.Max(1,editor.Width-1),Math.Max(1,editor.Height-1));}
    void DrawSetupBackground(Graphics g,Rectangle r){using(LinearGradientBrush b=new LinearGradientBrush(r,Color.FromArgb(247,249,252),Color.FromArgb(251,239,240),35))g.FillRectangle(b,r);int a=(int)(180*viewportScale),bSize=(int)(160*viewportScale);using(Brush bubble=new SolidBrush(Color.FromArgb(104,220,238,231)))g.FillEllipse(bubble,r.Width-(int)(290*viewportScale),(int)(15*viewportScale),a,a);using(Brush bubble=new SolidBrush(Color.FromArgb(95,250,216,190)))g.FillEllipse(bubble,(int)(115*viewportScale),r.Height-(int)(125*viewportScale),bSize,bSize);}
}
public class LocalDatabase
{
    public bool MusicPreferenceSet {get;set;}
    public bool MusicEnabled {get;set;}
    public string MusicFileName {get;set;}
    public bool LaunchLastCelebration {get;set;}
    public string LastRecordId {get;set;}
    public string StartupRecordId {get;set;}
    public List<GreetingRecord> Generations {get;set;}
    public List<UsageRecord> Usage {get;set;}
    public LocalDatabase(){MusicPreferenceSet=true;MusicEnabled=true;Generations=new List<GreetingRecord>();Usage=new List<UsageRecord>();}
    public void Normalize(){if(!MusicPreferenceSet){MusicEnabled=true;MusicPreferenceSet=true;}if(Generations==null)Generations=new List<GreetingRecord>();if(Usage==null)Usage=new List<UsageRecord>();}
}
public class GreetingRecord
{
    public string Id {get;set;} public string CreatedAt {get;set;} public string UpdatedAt {get;set;} public string Name {get;set;} public string Relation {get;set;} public int Age {get;set;} public string Style {get;set;} public int WordCount {get;set;} public string ShortText {get;set;} public string FullText {get;set;}
}
public class UsageRecord
{
    public string Id {get;set;} public string OccurredAt {get;set;} public string Action {get;set;} public string RecordId {get;set;} public string Subject {get;set;} public string Detail {get;set;}
}
static class RecordTools
{
    public static GreetingRecord Clone(GreetingRecord source)
    {
        if(source==null)return null;return new GreetingRecord{Id=source.Id,CreatedAt=source.CreatedAt,UpdatedAt=source.UpdatedAt,Name=source.Name,Relation=source.Relation,Age=source.Age,Style=source.Style,WordCount=source.WordCount,ShortText=source.ShortText,FullText=source.FullText};
    }
    public static void Copy(GreetingRecord target,GreetingRecord source)
    {
        target.Id=source.Id;target.CreatedAt=source.CreatedAt;target.UpdatedAt=source.UpdatedAt;target.Name=source.Name;target.Relation=source.Relation;target.Age=source.Age;target.Style=source.Style;target.WordCount=source.WordCount;target.ShortText=source.ShortText;target.FullText=source.FullText;
    }
}
static class LocalRecordStore
{
    static string Folder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BirthdayGreeting");}}
    static string DatabaseFile {get{return Path.Combine(Folder,"birthday_records.xml");}}
    public static LocalDatabase Load()
    {
        try
        {
            if(!File.Exists(DatabaseFile))return new LocalDatabase();
            using(FileStream stream=File.OpenRead(DatabaseFile))
            {
                LocalDatabase data=(LocalDatabase)new XmlSerializer(typeof(LocalDatabase)).Deserialize(stream);if(data==null)data=new LocalDatabase();data.Normalize();return data;
            }
        }
        catch{return new LocalDatabase();}
    }
    public static void Save(LocalDatabase data)
    {
        try
        {
            data.Normalize();Directory.CreateDirectory(Folder);string temporary=DatabaseFile+".tmp";
            using(FileStream stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))new XmlSerializer(typeof(LocalDatabase)).Serialize(stream,data);
            File.Copy(temporary,DatabaseFile,true);File.Delete(temporary);
        }
        catch{}
    }
}
class BirthdayMusicPlayer
{
    const int SampleRate=22050;
    string builtInMusicFile;
    SoundPlayer builtInPlayer;
    bool mciOpen;
    [DllImport("winmm.dll",CharSet=CharSet.Auto)]
    static extern int mciSendString(string command,StringBuilder returnValue,int returnLength,IntPtr callback);
    public static string MusicFolder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BirthdayGreeting","Music");}}
    public BirthdayMusicPlayer()
    {
        string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BirthdayGreeting");
        builtInMusicFile=Path.Combine(folder,"birthday_bgm.wav");
    }
    public static List<string> FindCustomTracks()
    {
        List<string> tracks=new List<string>();
        try
        {
            Directory.CreateDirectory(MusicFolder);
            tracks.AddRange(Directory.GetFiles(MusicFolder,"*.mp3"));
            tracks.AddRange(Directory.GetFiles(MusicFolder,"*.wav"));
            tracks.Sort(StringComparer.CurrentCultureIgnoreCase);
        }
        catch{}
        return tracks;
    }
    static string ResolveCustomTrack(string fileName)
    {
        if(String.IsNullOrEmpty(fileName))return "";
        string expected=Path.GetFileName(fileName);
        foreach(string track in FindCustomTracks())if(String.Equals(Path.GetFileName(track),expected,StringComparison.OrdinalIgnoreCase))return track;
        return "";
    }
    public void Play(string selectedFileName)
    {
        Stop();
        string selected=ResolveCustomTrack(selectedFileName);
        if(!String.IsNullOrEmpty(selected)&&TryPlayCustom(selected))return;
        try
        {
            EnsureTrack();
            if(builtInPlayer==null)builtInPlayer=new SoundPlayer(builtInMusicFile);
            builtInPlayer.PlayLooping();
        }
        catch{}
    }
    bool TryPlayCustom(string fileName)
    {
        string kind=String.Equals(Path.GetExtension(fileName),".wav",StringComparison.OrdinalIgnoreCase)?"waveaudio":"mpegvideo";
        try
        {
            if(SendMci("open \""+fileName+"\" type "+kind+" alias BirthdayGreetingMusic")!=0)return false;
            mciOpen=true;
            if(SendMci("play BirthdayGreetingMusic repeat")==0)return true;
            SendMci("close BirthdayGreetingMusic");
            mciOpen=false;
        }
        catch{}
        return false;
    }
    static int SendMci(string command){return mciSendString(command,null,0,IntPtr.Zero);}
    public void Stop()
    {
        try{if(builtInPlayer!=null)builtInPlayer.Stop();}catch{}
        try
        {
            if(mciOpen)SendMci("stop BirthdayGreetingMusic");
            SendMci("close BirthdayGreetingMusic");
            mciOpen=false;
        }
        catch{}
    }
    void EnsureTrack()
    {
        if(File.Exists(builtInMusicFile)&&new FileInfo(builtInMusicFile).Length>44)return;
        Directory.CreateDirectory(Path.GetDirectoryName(builtInMusicFile));
        string temporary=builtInMusicFile+".tmp";
        using(FileStream stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
        using(BinaryWriter writer=new BinaryWriter(stream))WriteWave(writer,BuildTrack());
        if(File.Exists(builtInMusicFile))File.Delete(builtInMusicFile);
        File.Move(temporary,builtInMusicFile);
    }
    static byte[] BuildTrack()
    {
        Tone[] notes=new Tone[]{
            new Tone(523.25,.22),new Tone(659.25,.22),new Tone(783.99,.32),new Tone(0,.08),
            new Tone(880.00,.22),new Tone(783.99,.22),new Tone(659.25,.30),new Tone(0,.12),
            new Tone(587.33,.22),new Tone(698.46,.22),new Tone(880.00,.30),new Tone(0,.10),
            new Tone(783.99,.22),new Tone(698.46,.22),new Tone(587.33,.34),new Tone(0,.14),
            new Tone(659.25,.22),new Tone(783.99,.22),new Tone(1046.50,.32),new Tone(0,.08),
            new Tone(987.77,.22),new Tone(783.99,.22),new Tone(659.25,.30),new Tone(0,.12),
            new Tone(698.46,.22),new Tone(880.00,.22),new Tone(1174.66,.32),new Tone(0,.10),
            new Tone(1046.50,.22),new Tone(880.00,.22),new Tone(783.99,.42),new Tone(0,.22)
        };
        int totalSamples=0;
        for(int i=0;i<notes.Length;i++)totalSamples+=Math.Max(1,(int)(notes[i].Seconds*SampleRate));
        byte[] audio=new byte[totalSamples*2];
        int offset=0;
        for(int n=0;n<notes.Length;n++)
        {
            Tone note=notes[n];
            int samples=Math.Max(1,(int)(note.Seconds*SampleRate));
            for(int i=0;i<samples;i++)
            {
                double envelope=Math.Min(1D,Math.Min(i/(SampleRate*.018D),(samples-i)/(SampleRate*.04D)));
                double value=0D;
                if(note.Frequency>0D)
                {
                    double phase=2D*Math.PI*note.Frequency*i/SampleRate;
                    value=(Math.Sin(phase)+.28D*Math.Sin(phase*2D)+.10D*Math.Sin(phase*3D))*.11D*envelope;
                }
                short sample=(short)Math.Max(short.MinValue,Math.Min(short.MaxValue,(int)Math.Round(value*short.MaxValue)));
                audio[offset++]=(byte)(sample&255);
                audio[offset++]=(byte)((sample>>8)&255);
            }
        }
        return audio;
    }
    static void WriteWave(BinaryWriter writer,byte[] audio)
    {
        WriteTag(writer,"RIFF");
        writer.Write(36+audio.Length);
        WriteTag(writer,"WAVE");
        WriteTag(writer,"fmt ");
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate*2);
        writer.Write((short)2);
        writer.Write((short)16);
        WriteTag(writer,"data");
        writer.Write(audio.Length);
        writer.Write(audio);
    }
    static void WriteTag(BinaryWriter writer,string tag){writer.Write(Encoding.ASCII.GetBytes(tag));}
    struct Tone
    {
        public double Frequency;
        public double Seconds;
        public Tone(double frequency,double seconds){Frequency=frequency;Seconds=seconds;}
    }
}
class MusicChoice
{
    public string FileName;
    public string DisplayName;
    public MusicChoice(string fileName,string displayName){FileName=fileName;DisplayName=displayName;}
    public override string ToString(){return DisplayName;}
}
class WishDocument { public string ShortText; public string FullText; }
class HistoryForm:Form
{
    LocalDatabase database; ListView generated=new ListView(),usage=new ListView(); bool choosingStartup; public GreetingRecord SelectedRecord;
    public HistoryForm(LocalDatabase value):this(value,false){}
    public HistoryForm(LocalDatabase value,bool selectStartup)
    {
        database=value;choosingStartup=selectStartup;Text=choosingStartup?"\u9009\u62e9\u542f\u52a8\u5e86\u795d\u8bb0\u5f55":"\u672c\u5730\u8bb0\u5f55";StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(920,570);MinimumSize=new Size(780,470);Font=new Font("Microsoft YaHei UI",9);BackColor=Color.FromArgb(247,248,250);
        Label heading=new Label{Text=choosingStartup?"\u9009\u62e9\u542f\u52a8\u5e86\u795d\u8bb0\u5f55":"\u672c\u5730\u8bb0\u5f55",AutoSize=true,Font=new Font("Microsoft YaHei UI",18,FontStyle.Bold),ForeColor=Color.FromArgb(43,52,67),Location=new Point(25,20)};Controls.Add(heading);
        Label hint=new Label{Text=choosingStartup?"\u9009\u4e2d\u540e\uff0c\u7a0b\u5e8f\u6bcf\u6b21\u542f\u52a8\u90fd\u4f1a\u5168\u5c4f\u5e86\u795d\u8fd9\u6761\u8bb0\u5f55\u3002":"\u751f\u6210\u8bb0\u5f55\u652f\u6301\u65b0\u5efa\u3001\u67e5\u770b\u3001\u7f16\u8f91\u3001\u5220\u9664\u3002",AutoSize=true,Font=new Font("Microsoft YaHei UI",9),ForeColor=Color.FromArgb(111,121,135),Location=new Point(28,55)};Controls.Add(hint);
        TabControl tabs=new TabControl{Location=new Point(24,87),Size=new Size(872,455),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right};Controls.Add(tabs);
        TabPage generatedPage=new TabPage("\u751f\u6210\u8bb0\u5f55");TabPage usagePage=new TabPage("\u4f7f\u7528\u8bb0\u5f55");tabs.TabPages.Add(generatedPage);tabs.TabPages.Add(usagePage);
        ConfigureGeneratedList();Panel recordActions=new Panel{Dock=DockStyle.Bottom,Height=56,BackColor=Color.FromArgb(251,251,252)};
        Button add=ActionButton("\u65b0\u5efa",new Point(12,13),Color.FromArgb(190,58,90),Color.White);add.Click+=(s,e)=>NewRecord();recordActions.Controls.Add(add);
        Button open=ActionButton(choosingStartup?"\u9009\u4e3a\u542f\u52a8":"\u6253\u5f00\u5e86\u795d",new Point(88,13),Color.White,Color.FromArgb(74,81,99));open.Size=choosingStartup?new Size(94,30):new Size(92,30);open.Click+=(s,e)=>OpenSelected();recordActions.Controls.Add(open);
        Button view=ActionButton("\u67e5\u770b",new Point(188,13),Color.White,Color.FromArgb(74,81,99));view.Click+=(s,e)=>ViewSelected();recordActions.Controls.Add(view);
        Button edit=ActionButton("\u7f16\u8f91",new Point(264,13),Color.White,Color.FromArgb(74,81,99));edit.Click+=(s,e)=>EditSelected();recordActions.Controls.Add(edit);
        Button remove=ActionButton("\u5220\u9664",new Point(340,13),Color.FromArgb(255,241,242),Color.FromArgb(175,55,67));remove.Click+=(s,e)=>DeleteSelected();recordActions.Controls.Add(remove);
        Label note=new Label{Text="\u9009\u4e2d\u8bb0\u5f55\u540e\u53ef\u91cd\u65b0\u8fdb\u5165\u5e86\u795d\u3002",AutoSize=true,ForeColor=Color.FromArgb(120,128,140),Location=new Point(435,20)};recordActions.Controls.Add(note);
        generatedPage.Controls.Add(generated);generatedPage.Controls.Add(recordActions);
        ConfigureUsageList();Panel usageActions=new Panel{Dock=DockStyle.Bottom,Height=56,BackColor=Color.FromArgb(251,251,252)};
        Button deleteUsage=ActionButton("\u5220\u9664\u9009\u4e2d",new Point(12,13),Color.White,Color.FromArgb(74,81,99));deleteUsage.Click+=(s,e)=>DeleteSelectedUsage();usageActions.Controls.Add(deleteUsage);
        Button clearUsage=ActionButton("\u6e05\u7a7a\u4f7f\u7528\u8bb0\u5f55",new Point(112,13),Color.FromArgb(255,241,242),Color.FromArgb(175,55,67));clearUsage.Size=new Size(112,30);clearUsage.Click+=(s,e)=>ClearUsage();usageActions.Controls.Add(clearUsage);
        usagePage.Controls.Add(usage);usagePage.Controls.Add(usageActions);ReloadRows(null);
    }
    Button ActionButton(string text,Point location,Color background,Color foreground)
    {
        Button button=new Button{Text=text,Size=new Size(68,30),Location=location,FlatStyle=FlatStyle.Flat,BackColor=background,ForeColor=foreground,Font=new Font("Microsoft YaHei UI",8.5f,FontStyle.Bold)};button.FlatAppearance.BorderColor=Color.FromArgb(218,224,230);button.FlatAppearance.BorderSize=background==Color.FromArgb(190,58,90)?0:1;return button;
    }
    void ConfigureGeneratedList()
    {
        generated.Dock=DockStyle.Fill;generated.View=View.Details;generated.FullRowSelect=true;generated.GridLines=false;generated.HideSelection=false;generated.MultiSelect=false;generated.Columns.Add("\u751f\u6210\u65f6\u95f4",145);generated.Columns.Add("\u59d3\u540d",95);generated.Columns.Add("\u5173\u7cfb",72);generated.Columns.Add("\u98ce\u683c",88);generated.Columns.Add("\u77ed\u53e5\u795d\u798f",420);generated.DoubleClick+=(s,e)=>OpenSelected();
    }
    void ConfigureUsageList()
    {
        usage.Dock=DockStyle.Fill;usage.View=View.Details;usage.FullRowSelect=true;usage.GridLines=false;usage.HideSelection=false;usage.MultiSelect=false;usage.Columns.Add("\u65f6\u95f4",150);usage.Columns.Add("\u64cd\u4f5c",135);usage.Columns.Add("\u5bf9\u8c61",105);usage.Columns.Add("\u8bf4\u660e",440);
    }
    string TimeText(string raw){DateTime time;return DateTime.TryParse(raw,out time)?time.ToString("yyyy-MM-dd HH:mm"):raw??"";}
    string Brief(string text,int length){if(String.IsNullOrEmpty(text))return "";return text.Length<=length?text:text.Substring(0,length)+"...";}
    void ReloadRows(string selectId)
    {
        generated.BeginUpdate();generated.Items.Clear();foreach(GreetingRecord record in database.Generations){ListViewItem item=new ListViewItem(TimeText(record.CreatedAt));item.SubItems.Add(record.Name??"");item.SubItems.Add(record.Relation??"");item.SubItems.Add(record.Style??"");item.SubItems.Add(Brief(record.ShortText,30));item.Tag=record;generated.Items.Add(item);}generated.EndUpdate();
        usage.BeginUpdate();usage.Items.Clear();foreach(UsageRecord record in database.Usage){ListViewItem item=new ListViewItem(TimeText(record.OccurredAt));item.SubItems.Add(record.Action??"");item.SubItems.Add(record.Subject??"");item.SubItems.Add(record.Detail??"");item.Tag=record;usage.Items.Add(item);}usage.EndUpdate();
        if(!String.IsNullOrEmpty(selectId))foreach(ListViewItem item in generated.Items){GreetingRecord record=item.Tag as GreetingRecord;if(record!=null&&record.Id==selectId){item.Selected=true;item.Focused=true;item.EnsureVisible();break;}}
    }
    GreetingRecord CurrentRecord(){return generated.SelectedItems.Count==0?null:generated.SelectedItems[0].Tag as GreetingRecord;}
    UsageRecord CurrentUsage(){return usage.SelectedItems.Count==0?null:usage.SelectedItems[0].Tag as UsageRecord;}
    void AddUsage(string action,GreetingRecord record,string detail)
    {
        database.Usage.Insert(0,new UsageRecord{Id=Guid.NewGuid().ToString("N"),OccurredAt=DateTime.Now.ToString("o"),Action=action,RecordId=record==null?"":record.Id,Subject=record==null?"":record.Name,Detail=detail??""});if(database.Usage.Count>300)database.Usage.RemoveRange(300,database.Usage.Count-300);
    }
    void Persist(){LocalRecordStore.Save(database);}
    void NewRecord()
    {
        using(RecordEditorForm editor=new RecordEditorForm(null))if(editor.ShowDialog(this)==DialogResult.OK){database.Generations.Insert(0,editor.Record);AddUsage("\u65b0\u5efa\u751f\u6210\u8bb0\u5f55",editor.Record,"\u5df2\u65b0\u5efa\u4e00\u6761\u672c\u5730\u8bb0\u5f55");Persist();ReloadRows(editor.Record.Id);}
    }
    void EditSelected()
    {
        GreetingRecord record=CurrentRecord();if(record==null){SelectPrompt();return;}using(RecordEditorForm editor=new RecordEditorForm(record))if(editor.ShowDialog(this)==DialogResult.OK){RecordTools.Copy(record,editor.Record);AddUsage("\u7f16\u8f91\u751f\u6210\u8bb0\u5f55",record,"\u5df2\u66f4\u65b0\u8bb0\u5f55\u5185\u5bb9");Persist();ReloadRows(record.Id);}
    }
    void DeleteSelected()
    {
        GreetingRecord record=CurrentRecord();if(record==null){SelectPrompt();return;}if(MessageBox.Show(this,"\u5220\u9664\u540e\u5c06\u4e00\u5e76\u6e05\u9664\u4e0e\u8be5\u751f\u6210\u8bb0\u5f55\u5173\u8054\u7684\u4f7f\u7528\u8bb0\u5f55\u3002","\u786e\u8ba4\u5220\u9664",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        database.Generations.Remove(record);database.Usage.RemoveAll(item=>item.RecordId==record.Id);if(database.LastRecordId==record.Id)database.LastRecordId=database.Generations.Count==0?"":database.Generations[0].Id;if(database.StartupRecordId==record.Id){database.StartupRecordId="";database.LaunchLastCelebration=false;}AddUsage("\u5220\u9664\u751f\u6210\u8bb0\u5f55",null,(record.Name??"\u672a\u547d\u540d")+"\u7684\u8bb0\u5f55\u5df2\u5220\u9664");Persist();ReloadRows(null);
    }
    void DeleteSelectedUsage()
    {
        UsageRecord record=CurrentUsage();if(record==null){MessageBox.Show(this,"\u8bf7\u5148\u9009\u62e9\u4e00\u6761\u4f7f\u7528\u8bb0\u5f55\u3002",Text,MessageBoxButtons.OK,MessageBoxIcon.Information);return;}database.Usage.Remove(record);Persist();ReloadRows(null);
    }
    void ClearUsage()
    {
        if(database.Usage.Count==0)return;if(MessageBox.Show(this,"\u786e\u8ba4\u6e05\u7a7a\u6240\u6709\u4f7f\u7528\u8bb0\u5f55\uff1f","\u6e05\u7a7a\u4f7f\u7528\u8bb0\u5f55",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;database.Usage.Clear();Persist();ReloadRows(null);
    }
    void SelectPrompt(){MessageBox.Show(this,"\u8bf7\u5148\u9009\u62e9\u4e00\u6761\u751f\u6210\u8bb0\u5f55\u3002",Text,MessageBoxButtons.OK,MessageBoxIcon.Information);}
    void OpenSelected(){GreetingRecord record=CurrentRecord();if(record==null){SelectPrompt();return;}SelectedRecord=record;DialogResult=DialogResult.OK;Close();}
    void ViewSelected(){GreetingRecord record=CurrentRecord();if(record==null){SelectPrompt();return;}using(WishResultForm form=new WishResultForm(new WishDocument{ShortText=record.ShortText??"",FullText=record.FullText??""}))form.ShowDialog(this);}
}

class RecordEditorForm:Form
{
    GreetingRecord draft; TextBox name=new TextBox(),shortText=new TextBox(),fullText=new TextBox(); ComboBox relation=new ComboBox(),style=new ComboBox(); NumericUpDown age=new NumericUpDown(),wordCount=new NumericUpDown(); public GreetingRecord Record {get;private set;}
    public RecordEditorForm(GreetingRecord source)
    {
        draft=source==null?new GreetingRecord{Id=Guid.NewGuid().ToString("N"),CreatedAt=DateTime.Now.ToString("o"),UpdatedAt=DateTime.Now.ToString("o"),Name="\u5bff\u661f",Relation="\u670b\u53cb",Age=18,Style="\u6e29\u67d4\u6cbb\u6108",WordCount=150}:RecordTools.Clone(source);
        Text=source==null?"\u65b0\u5efa\u751f\u6210\u8bb0\u5f55":"\u7f16\u8f91\u751f\u6210\u8bb0\u5f55";StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(720,640);MinimumSize=new Size(660,570);Font=new Font("Microsoft YaHei UI",9.5f);BackColor=Color.FromArgb(251,250,248);
        Field("\u5bf9\u65b9\u59d3\u540d",name,28,24,310);Field("\u8eab\u4efd\u5173\u7cfb",relation,382,24,310);Field("\u5e74\u9f84",age,28,91,150);Field("\u98ce\u683c",style,220,91,472);Field("\u76ee\u6807\u5b57\u6570",wordCount,28,158,180);
        relation.DropDownStyle=ComboBoxStyle.DropDownList;relation.Items.AddRange(new object[]{"\u957f\u8f88","\u670b\u53cb","\u604b\u4eba","\u540c\u5b66","\u540c\u4e8b"});style.DropDownStyle=ComboBoxStyle.DropDownList;style.Items.AddRange(new object[]{"\u6e29\u67d4\u6cbb\u6108","\u6587\u827a","\u7b80\u7ea6","\u6d3b\u6cfc"});age.Minimum=1;age.Maximum=120;wordCount.Minimum=80;wordCount.Maximum=300;wordCount.Increment=10;
        name.Text=draft.Name??"";relation.SelectedItem=draft.Relation??"\u670b\u53cb";if(relation.SelectedIndex<0)relation.SelectedIndex=1;age.Value=Math.Max((int)age.Minimum,Math.Min((int)age.Maximum,draft.Age));style.SelectedItem=draft.Style??"\u6e29\u67d4\u6cbb\u6108";if(style.SelectedIndex<0)style.SelectedIndex=0;wordCount.Value=Math.Max((int)wordCount.Minimum,Math.Min((int)wordCount.Maximum,draft.WordCount));
        Button regenerate=new Button{Text="\u91cd\u65b0\u751f\u6210\u6587\u6848",Size=new Size(120,31),Location=new Point(572,178),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(190,58,90),ForeColor=Color.White};regenerate.FlatAppearance.BorderSize=0;regenerate.Click+=(s,e)=>Regenerate();Controls.Add(regenerate);
        Label shortLabel=LabelOf("\u7b80\u77ed\u4e00\u53e5\u8bdd\u7248",28,227);Controls.Add(shortLabel);shortText.Multiline=true;shortText.ScrollBars=ScrollBars.Vertical;shortText.Location=new Point(28,249);shortText.Size=new Size(664,70);shortText.BackColor=Color.White;shortText.Text=draft.ShortText??"";Controls.Add(shortText);
        Label fullLabel=LabelOf("\u5b8c\u6574\u7248\u957f\u6587\u6848",28,337);Controls.Add(fullLabel);fullText.Multiline=true;fullText.ScrollBars=ScrollBars.Vertical;fullText.Location=new Point(28,359);fullText.Size=new Size(664,200);fullText.BackColor=Color.White;fullText.Text=draft.FullText??"";Controls.Add(fullText);
        Button save=new Button{Text="\u4fdd\u5b58\u8bb0\u5f55",Size=new Size(98,34),Location=new Point(480,584),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(190,58,90),ForeColor=Color.White,Font=new Font("Microsoft YaHei UI",9,FontStyle.Bold)};save.FlatAppearance.BorderSize=0;save.Click+=(s,e)=>SaveRecord();Controls.Add(save);
        Button cancel=new Button{Text="\u53d6\u6d88",Size=new Size(82,34),Location=new Point(590,584),FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Color.FromArgb(74,81,99)};cancel.FlatAppearance.BorderColor=Color.FromArgb(218,224,230);cancel.DialogResult=DialogResult.Cancel;Controls.Add(cancel);CancelButton=cancel;
        if(source==null)Regenerate();
    }
    Label LabelOf(string text,int x,int y){return new Label{Text=text,AutoSize=true,Location=new Point(x,y),Font=new Font("Microsoft YaHei UI",9,FontStyle.Bold),ForeColor=Color.FromArgb(73,82,95)};}
    void Field(string label,Control control,int x,int y,int width){Controls.Add(LabelOf(label,x,y));control.Location=new Point(x,y+22);control.Size=new Size(width,31);control.BackColor=Color.White;Controls.Add(control);}
    void Regenerate(){WishDocument wish=WishWriter.Create(name.Text.Trim(),relation.Text,(int)age.Value,style.Text,(int)wordCount.Value);shortText.Text=wish.ShortText;fullText.Text=wish.FullText;}
    void SaveRecord()
    {
        if(String.IsNullOrWhiteSpace(shortText.Text)||String.IsNullOrWhiteSpace(fullText.Text))Regenerate();string now=DateTime.Now.ToString("o");if(String.IsNullOrEmpty(draft.Id))draft.Id=Guid.NewGuid().ToString("N");if(String.IsNullOrEmpty(draft.CreatedAt))draft.CreatedAt=now;
        draft.UpdatedAt=now;draft.Name=name.Text.Trim();draft.Relation=relation.Text;draft.Age=(int)age.Value;draft.Style=style.Text;draft.WordCount=(int)wordCount.Value;draft.ShortText=shortText.Text.Trim();draft.FullText=fullText.Text.Trim();Record=draft;DialogResult=DialogResult.OK;Close();
    }
}

class WishResultForm:Form
{
    WishDocument doc;
    public WishResultForm(WishDocument value)
    {
        doc=value;Text="\u751f\u65e5\u795d\u798f\u6587\u6848";StartPosition=FormStartPosition.CenterParent;ClientSize=new Size(700,520);MinimumSize=new Size(620,460);Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(251,250,248);
        Label h=new Label{Text="\u751f\u65e5\u795d\u798f",AutoSize=true,Font=new Font("Microsoft YaHei UI",19,FontStyle.Bold),ForeColor=Color.FromArgb(47,51,61),Location=new Point(32,26)};Controls.Add(h);
        AddSection("\u7b80\u77ed\u4e00\u53e5\u8bdd\u7248",doc.ShortText,83,65,true);AddSection("\u5b8c\u6574\u7248\u957f\u6587\u6848",doc.FullText,194,245,false);
    }
    void AddSection(string heading,string content,int y,int h,bool copy)
    {
        Label l=new Label{Text=heading,AutoSize=true,Font=new Font("Microsoft YaHei UI",10,FontStyle.Bold),ForeColor=Color.FromArgb(89,69,80),Location=new Point(33,y)};Controls.Add(l);
        TextBox box=new TextBox{Text=content,ReadOnly=true,Multiline=true,BorderStyle=BorderStyle.FixedSingle,BackColor=Color.White,ForeColor=Color.FromArgb(57,58,64),Location=new Point(32,y+27),Size=new Size(636,h),Font=new Font("Microsoft YaHei UI",10),ScrollBars=ScrollBars.Vertical};Controls.Add(box);
        Button b=new Button{Text="\u590d\u5236",Size=new Size(58,26),Location=new Point(610,y-3),FlatStyle=FlatStyle.Flat,BackColor=Color.White,ForeColor=Color.FromArgb(95,74,84)};b.FlatAppearance.BorderColor=Color.FromArgb(218,207,208);b.Click+=(s,e)=>Clipboard.SetText(content);Controls.Add(b);
    }
}

static class WishWriter
{
    public static WishDocument Create(string name,string relation,int age,string style,int target)
    {
        if(string.IsNullOrEmpty(style))style="\u6e29\u67d4\u6cbb\u6108";if(string.IsNullOrEmpty(relation))relation="\u670b\u53cb";int goal=Math.Max(80,target);string ageText=age+"\u5c81\u7684\u8fd9\u4e00\u5e74";
        string shortText=ShortLine(name,relation,ageText,style);List<string> lines=new List<string>();lines.Add(Open(name,relation,ageText));lines.Add(RelationLine(relation));lines.Add(StyleLine(style));lines.Add("\u613f\u5e73\u5b89\u4e0e\u81ea\u5728\u6210\u4e3a\u4f60\u65e5\u5b50\u91cc\u6700\u5b89\u9759\u7684\u5e95\u8272\uff0c\u4e5f\u613f\u4f60\u5728\u60f3\u8981\u70ed\u70c8\u7684\u65f6\u5019\uff0c\u6562\u4e8e\u5411\u524d\u8d70\u3002");lines.Add("\u4e0d\u5fc5\u6025\u4e8e\u8ffd\u4e0a\u8c01\uff0c\u4e5f\u4e0d\u5fc5\u5c06\u81ea\u5df1\u4ea4\u7ed9\u522b\u4eba\u7684\u5c3a\u5ea6\uff0c\u613f\u4f60\u4e00\u76f4\u6309\u7167\u81ea\u5df1\u7684\u8282\u594f\uff0c\u7b49\u6765\u5fc3\u4e4b\u6240\u5411\u3002");lines.Add("\u65e9\u6668\u6709\u671f\u5f85\uff0c\u508d\u665a\u6709\u6536\u83b7\uff0c\u65e5\u5b50\u6e05\u6de1\u65f6\u4e5f\u4f9d\u7136\u6709\u5149\u3002\u751f\u65e5\u5feb\u4e50\uff0c\u613f\u4f60\u7684\u65b0\u4e00\u5c81\u4e30\u76db\u800c\u4ece\u5bb9\u3002");
        string full="";for(int i=0;i<lines.Count;i++){string next=lines[i];if(full.Length<goal)full+=next;else break;}full=FitToLength(full,goal,relation,style);return new WishDocument{ShortText=shortText,FullText=full};
    }
    static string FitToLength(string text,int goal,string relation,string style)
    {
        string[] additions={"\u613f\u4f60\u4e0d\u7f3a\u7406\u89e3\uff0c\u4e5f\u4e0d\u8f9c\u8d1f\u81ea\u5df1\u3002","\u613f\u4f60\u5728\u6bcf\u4e00\u6b21\u9009\u62e9\u91cc\uff0c\u90fd\u80fd\u4fdd\u7559\u771f\u8bda\u4e0e\u52c7\u6c14\u3002","\u613f\u90a3\u4e9b\u8fd8\u672a\u62b5\u8fbe\u7684\u7f8e\u597d\uff0c\u6b63\u5728\u6162\u6162\u5411\u4f60\u8d70\u6765\u3002","\u613f\u4f60\u6709\u80fd\u91cf\u53bb\u6d3b\u51fa\u81ea\u5df1\u7684\u70ed\u7231\uff0c\u4e5f\u6709\u4f59\u88d5\u53bb\u4eab\u53d7\u5bfb\u5e38\u3002"};
        int i=0;while(text.Length<goal-8&&i<additions.Length*4){text+=additions[i%additions.Length];i++;}return text;
    }    static string ShortLine(string name,string relation,string age,string style)
    {
        string who=string.IsNullOrEmpty(name)?"":name+"\uff0c";if(relation=="\u957f\u8f88")return who+"\u613f\u60a8\u5728"+age+"\uff0c\u5fc3\u5883\u4ece\u5bb9\uff0c\u65e5\u5b50\u81ea\u5728\uff0c\u6240\u5ff5\u7686\u6e29\u67d4\u3002";
        if(relation=="\u604b\u4eba")return who+"\u613f\u4f60\u7684"+age+"\uff0c\u4ecd\u7136\u88ab\u7231\u610f\u70b9\u4eae\uff0c\u4e5f\u88ab\u5fc3\u4e4b\u6240\u5411\u6e29\u67d4\u56de\u5e94\u3002";
        if(relation=="\u540c\u5b66")return who+"\u613f\u4f60\u5728"+age+"\uff0c\u4f9d\u65e7\u6000\u63e3\u70ed\u7231\uff0c\u5411\u7740\u66f4\u5f00\u9614\u7684\u65b9\u5411\u594b\u529b\u5954\u8dd1\u3002";
        if(relation=="\u540c\u4e8b")return who+"\u613f\u4f60\u5728"+age+"\uff0c\u6240\u884c\u7686\u6709\u6536\u83b7\uff0c\u6240\u5fd9\u4ea6\u6709\u56de\u54cd\u3002";
        if(style=="\u6d3b\u6cfc")return who+"\u613f\u4f60\u5728"+age+"\uff0c\u52c7\u6562\u53c8\u6f47\u6d12\uff0c\u628a\u65e5\u5b50\u8fc7\u6210\u81ea\u5df1\u559c\u6b22\u7684\u6837\u5b50\u3002";
        return who+"\u613f\u4f60\u5728"+age+"\uff0c\u6709\u5149\u53ef\u8d76\uff0c\u6709\u7231\u53ef\u4f9d\uff0c\u4e5f\u6709\u5c5e\u4e8e\u81ea\u5df1\u7684\u5b89\u7136\u3002";
    }
    static string Open(string name,string relation,string age){string who=string.IsNullOrEmpty(name)?"":name+"\uff0c";if(relation=="\u957f\u8f88")return who+"\u5728\u60a8\u8fce\u6765"+age+"\u7684\u4eca\u5929\uff0c\u60f3\u628a\u4e00\u4efd\u771f\u631a\u7684\u795d\u798f\u653e\u5728\u60a8\u624b\u8fb9\u3002";if(relation=="\u604b\u4eba")return who+"\u4f60\u7684"+age+"\u5982\u7ea6\u800c\u81f3\uff0c\u800c\u6211\u60f3\u628a\u6240\u6709\u7ec6\u817b\u7684\u795d\u613f\u90fd\u5199\u7ed9\u4f60\u3002";if(relation=="\u540c\u5b66")return who+"\u8d70\u5230"+age+"\uff0c\u8bb0\u5fc6\u91cc\u7684\u4e0a\u8bfe\u94c3\u58f0\u4eff\u4f5b\u8fd8\u5728\uff0c\u4f60\u5374\u5df2\u7ecf\u6709\u4e86\u66f4\u5f00\u9614\u7684\u4e16\u754c\u3002";if(relation=="\u540c\u4e8b")return who+"\u5728\u5fd9\u788c\u4e0e\u534f\u4f5c\u4e4b\u95f4\uff0c\u4eca\u5929\u503c\u5f97\u4e3a\u4f60\u7559\u4e0b\u4e00\u6761\u5f00\u5fc3\u7684\u6ce8\u811a\u3002";return who+"\u53c8\u966a\u4f60\u8d70\u8fc7\u4e86\u4e00\u5c81\uff0c\u4eca\u5929\u60f3\u8ba4\u771f\u5730\u5bf9\u4f60\u8bf4\u4e00\u58f0\u751f\u65e5\u5feb\u4e50\u3002";}
    static string RelationLine(string relation){if(relation=="\u957f\u8f88")return "\u611f\u8c22\u60a8\u4e00\u8def\u4ee5\u6765\u7684\u5b88\u62a4\u4e0e\u4ed8\u51fa\uff0c\u613f\u65f6\u95f4\u5bf9\u60a8\u591a\u4e00\u4e9b\u504f\u7231\uff0c\u5c11\u4e00\u4e9b\u8f9b\u52b3\u3002";if(relation=="\u604b\u4eba")return "\u613f\u6211\u4eec\u4ecd\u7136\u80fd\u628a\u5e73\u51e1\u7684\u65e5\u5b50\u8fc7\u5f97\u6709\u5149\uff0c\u5728\u5f7c\u6b64\u8eab\u8fb9\u5b89\u5fc3\u5730\u505a\u81ea\u5df1\u3002";if(relation=="\u540c\u5b66")return "\u613f\u4f60\u4e0d\u4f1a\u4e22\u6389\u90a3\u4efd\u5c11\u5e74\u6c14\uff0c\u5bf9\u672a\u77e5\u4f9d\u65e7\u6709\u597d\u5947\u5fc3\u548c\u884c\u52a8\u529b\u3002";if(relation=="\u540c\u4e8b")return "\u613f\u4f60\u7684\u4e13\u4e1a\u5f97\u5230\u770b\u89c1\uff0c\u4ed8\u51fa\u5f97\u5230\u73cd\u60dc\uff0c\u5de5\u4f5c\u4e4b\u5916\u4e5f\u62e5\u6709\u81ea\u5df1\u7684\u751f\u6d3b\u3002";return "\u4e0e\u4f60\u76f8\u5904\u7684\u65f6\u95f4\u603b\u662f\u5f88\u8212\u670d\uff0c\u613f\u4f60\u65e0\u8bba\u5728\u54ea\u91cc\uff0c\u90fd\u6709\u4eba\u8bb0\u5f97\u4f60\u7684\u771f\u5fc3\u3002";}
    static string StyleLine(string style){if(style=="\u6587\u827a")return "\u613f\u98ce\u7a7f\u8fc7\u4f60\u7684\u7a97\u524d\u65f6\uff0c\u5e26\u6765\u4e0d\u6b62\u662f\u5b63\u8282\uff0c\u8fd8\u5e26\u6765\u65b0\u7684\u6545\u4e8b\u4e0e\u53ef\u4ee5\u671f\u5f85\u7684\u8fdc\u65b9\u3002";if(style=="\u7b80\u7ea6")return "\u613f\u4f60\u5403\u5f97\u597d\u3001\u7761\u5f97\u9999\u3001\u88ab\u4eba\u5584\u5f85\uff0c\u60f3\u505a\u7684\u4e8b\u4e00\u4ef6\u4e00\u4ef6\u505a\u6210\u3002";if(style=="\u6d3b\u6cfc")return "\u613f\u4f60\u4ecd\u7136\u6562\u7b11\u3001\u6562\u95ef\u3001\u6562\u91cd\u65b0\u5f00\u59cb\uff0c\u628a\u6bcf\u4e00\u4e2a\u5bfb\u5e38\u65e5\u5b50\u8fc7\u5f97\u6709\u58f0\u6709\u8272\u3002";return "\u613f\u4f60\u7684\u751f\u6d3b\u6709\u90a3\u4e48\u4e00\u70b9\u4e0d\u5fc5\u7528\u529b\u7684\u5feb\u4e50\uff0c\u6709\u90a3\u4e48\u4e00\u79cd\u65e0\u987b\u8bc1\u660e\u7684\u5b89\u5fc3\u3002";}
}
class GradientStylePicker:Control
{
    string[] labels={"\u6e29\u67d4\u6cbb\u6108","\u6587\u827a","\u7b80\u7ea6","\u6d3b\u6cfc"};int selected=0;public float RenderScale=1F;public event EventHandler SelectedStyleChanged;
    public string SelectedStyle { get { return labels[selected]; } }
    public void SelectStyle(string value){for(int i=0;i<labels.Length;i++)if(labels[i]==value){selected=i;Invalidate();return;}}
    public GradientStylePicker(){DoubleBuffered=true;Cursor=Cursors.Hand;}
    protected override void OnMouseDown(MouseEventArgs e){int w=Math.Max(1,Width/4);int index=Math.Min(labels.Length-1,Math.Max(0,e.X/w));if(index!=selected){selected=index;Invalidate();if(SelectedStyleChanged!=null)SelectedStyleChanged(this,EventArgs.Empty);}}
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;Color[,] colors={{Color.FromArgb(249,151,170),Color.FromArgb(243,196,131)},{Color.FromArgb(87,108,196),Color.FromArgb(176,112,190)},{Color.FromArgb(84,166,145),Color.FromArgb(180,215,142)},{Color.FromArgb(245,112,88),Color.FromArgb(251,193,71)}};int cell=Math.Max(1,Width/4);
        for(int i=0;i<labels.Length;i++){Rectangle r=new Rectangle(i*cell+2,2,cell-5,Height-4);using(LinearGradientBrush b=new LinearGradientBrush(r,colors[i,0],colors[i,1],LinearGradientMode.Horizontal))Round(e.Graphics,b,r,7);using(Brush text=new SolidBrush(Color.White))using(Font f=new Font("Microsoft YaHei UI",8.3f*RenderScale,FontStyle.Bold)){SizeF s=e.Graphics.MeasureString(labels[i],f);e.Graphics.DrawString(labels[i],f,text,r.Left+(r.Width-s.Width)/2,r.Top+(r.Height-s.Height)/2-1);}if(i==selected)using(Pen p=new Pen(Color.White,Math.Max(1F,2F*RenderScale)))DrawRound(e.Graphics,p,r,Math.Max(4,(int)(7*RenderScale)));}
    }
    void Round(Graphics g,Brush b,Rectangle r,int radius){using(GraphicsPath p=Path(r,radius))g.FillPath(b,p);}void DrawRound(Graphics g,Pen p,Rectangle r,int radius){using(GraphicsPath path=Path(r,radius))g.DrawPath(p,path);}GraphicsPath Path(Rectangle r,int radius){GraphicsPath p=new GraphicsPath();int d=radius*2;p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
class SetupPreview:Panel
{
    public ThemeKind Theme; public float RenderScale=1F;
    int LogicalWidth { get { return Math.Max(1,(int)Math.Round(Width/Math.Max(.01F,RenderScale))); } }
    int LogicalHeight { get { return Math.Max(1,(int)Math.Round(Height/Math.Max(.01F,RenderScale))); } }
    public SetupPreview(){DoubleBuffered=true;}
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;GraphicsState state=g.Save();g.ScaleTransform(RenderScale,RenderScale);int w=LogicalWidth,h=LogicalHeight;Rectangle r=new Rectangle(0,0,w,h);Color bg=Theme==ThemeKind.Rose?Color.FromArgb(253,232,233):Theme==ThemeKind.Galaxy?Color.FromArgb(19,26,64):Color.FromArgb(222,241,229);using(Brush b=new SolidBrush(bg))g.FillRectangle(b,r);if(Theme==ThemeKind.Rose)Rose(g,w,h);if(Theme==ThemeKind.Galaxy)Galaxy(g,w,h);if(Theme==ThemeKind.Forest)Forest(g,w,h);using(Brush veil=new SolidBrush(Color.FromArgb(115,255,255,255)))g.FillRectangle(veil,18,17,155,59);Color txt=Theme==ThemeKind.Galaxy?Color.White:Color.FromArgb(44,57,66);using(Brush b=new SolidBrush(txt)){g.DrawString("\u5f53\u524d\u6a21\u677f",new Font("Microsoft YaHei UI",8,FontStyle.Bold),b,31,28);g.DrawString(ThemeTitle(),new Font("Microsoft YaHei UI",15,FontStyle.Bold),b,30,43);}g.Restore(state);
    }
    string ThemeTitle(){return Theme==ThemeKind.Rose?"\u73ab\u7470\u82b1\u56ed":Theme==ThemeKind.Galaxy?"\u661f\u591c\u94f6\u6cb3":"\u68ee\u6797\u5947\u9047";}
    void Rose(Graphics g,int w,int h){for(int i=0;i<11;i++)Flower(g,25+i*(w-35)/11,h-80-(i%3)*13,i%2==0?Color.FromArgb(213,73,112):Color.FromArgb(245,142,121));Balloon(g,45,118,Color.FromArgb(224,87,121));Balloon(g,w-105,92,Color.FromArgb(246,171,85));Cake(g,w/2,h-150,Color.FromArgb(197,60,91));Gift(g,w/2-173,h-100,102,72,Color.FromArgb(107,141,191));Gift(g,w/2+70,h-100,105,72,Color.FromArgb(234,104,124));}
    void Galaxy(Graphics g,int w,int h){using(Brush glow=new SolidBrush(Color.FromArgb(55,111,128,255)))g.FillEllipse(glow,w/2-165,70,330,300);for(int i=0;i<38;i++){int x=(i*67)%Math.Max(1,w),y=(i*41)%Math.Max(1,h-70);using(Brush b=new SolidBrush(i%5==0?Color.FromArgb(255,220,118):Color.FromArgb(218,228,255)))g.FillEllipse(b,x,y,i%5==0?5:2,i%5==0?5:2);}using(Brush moon=new SolidBrush(Color.FromArgb(250,229,151)))g.FillEllipse(moon,w-125,65,65,65);using(Brush cut=new SolidBrush(Color.FromArgb(19,26,64)))g.FillEllipse(cut,w-102,54,65,65);Cake(g,w/2,h-148,Color.FromArgb(80,126,229));}
    void Forest(Graphics g,int w,int h){for(int i=0;i<6;i++)Tree(g,15+i*(w-20)/6,h-196-(i%2)*17,Color.FromArgb(48+i*5,124+i*7,88+i*3));using(Brush hill=new SolidBrush(Color.FromArgb(136,194,143)))g.FillEllipse(hill,-60,h-155,w+120,165);for(int i=0;i<14;i++)Leaf(g,20+i*(w-30)/14,70+(i%4)*48,Color.FromArgb(56,137,103));Cake(g,w/2,h-151,Color.FromArgb(47,132,101));}
    void Balloon(Graphics g,int x,int y,Color c){using(Brush b=new SolidBrush(c))g.FillEllipse(b,x,y,67,83);using(Brush b=new SolidBrush(Color.FromArgb(75,255,255,255)))g.FillEllipse(b,x+14,y+12,15,22);using(Pen p=new Pen(Color.FromArgb(120,100,90)))g.DrawLine(p,x+34,y+83,x+42,y+140);}
    void Gift(Graphics g,int x,int y,int w,int h,Color c){using(Brush b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);using(Brush r=new SolidBrush(Color.FromArgb(255,224,150)))g.FillRectangle(r,x+w/2-7,y,14,h);using(Brush r=new SolidBrush(Color.FromArgb(255,224,150)))g.FillRectangle(r,x,y+17,w,10);}
    void Flower(Graphics g,int x,int y,Color c){using(Brush b=new SolidBrush(c)){for(int i=0;i<5;i++){double a=i*1.256;g.FillEllipse(b,x+(int)(Math.Cos(a)*7)-7,y+(int)(Math.Sin(a)*7)-7,14,14);}using(Brush core=new SolidBrush(Color.FromArgb(247,194,78)))g.FillEllipse(core,x-5,y-5,10,10);}}
    void Tree(Graphics g,int x,int y,Color c){using(Brush b=new SolidBrush(c)){g.FillEllipse(b,x,y,76,88);g.FillEllipse(b,x-18,y+40,108,85);}using(Brush trunk=new SolidBrush(Color.FromArgb(102,81,62)))g.FillRectangle(trunk,x+32,y+86,12,70);}
    void Leaf(Graphics g,int x,int y,Color c){using(Brush b=new SolidBrush(c))g.FillEllipse(b,x,y,17,9);}
    void Cake(Graphics g,int c,int y,Color baseColor){using(Brush sh=new SolidBrush(Color.FromArgb(45,43,53,63)))g.FillEllipse(sh,c-145,y+101,290,25);Round(g,baseColor,new Rectangle(c-115,y+40,230,66),17);Round(g,Color.FromArgb(255,226,216),new Rectangle(c-121,y+30,242,22),10);Round(g,Color.FromArgb(246,170,102),new Rectangle(c-86,y-15,172,50),14);Round(g,Color.FromArgb(255,240,221),new Rectangle(c-92,y-24,184,18),9);for(int i=-1;i<=1;i++){int x=c+i*45;using(Brush b=new SolidBrush(i==0?baseColor:Color.FromArgb(101,156,147)))g.FillRectangle(b,x-5,y-62,10,40);using(Brush b=new SolidBrush(Color.FromArgb(255,186,66)))g.FillEllipse(b,x-8,y-83,16,23);}}
    void Round(Graphics g,Color c,Rectangle r,int z){using(GraphicsPath p=new GraphicsPath()){int d=z*2;p.AddArc(r.Left,r.Top,d,d,180,90);p.AddArc(r.Right-d,r.Top,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.Left,r.Bottom-d,d,d,90,90);p.CloseFigure();using(Brush b=new SolidBrush(c))g.FillPath(b,p);}}
}
class PartyCanvas:Panel
{
    public ThemeKind Theme; public Color Accent; public string Person=""; public string CustomWish=""; public float RenderScale=1F;
    int LW { get { return Math.Max(1,(int)Math.Round(Width/Math.Max(.01F,RenderScale))); } } int LH { get { return Math.Max(1,(int)Math.Round(Height/Math.Max(.01F,RenderScale))); } }
    Timer timer=new Timer{Interval=20};Random random=new Random();List<Bit> bits=new List<Bit>();List<Wish> wishes=new List<Wish>();int frame;
    Color[] rose={Color.FromArgb(239,78,85),Color.FromArgb(255,178,74),Color.FromArgb(246,224,206),Color.FromArgb(190,66,91)};
    Color[] galaxy={Color.FromArgb(104,167,255),Color.FromArgb(220,188,255),Color.FromArgb(255,224,116),Color.FromArgb(238,241,255)};
    Color[] forest={Color.FromArgb(77,182,130),Color.FromArgb(251,190,83),Color.FromArgb(229,244,218),Color.FromArgb(238,119,87)};
    public PartyCanvas(){DoubleBuffered=true;timer.Tick+=(s,e)=>MoveScene();}
    public void Pause(){timer.Stop();}
    public void SetWishes(string custom){CustomWish=custom;wishes.Clear();string[] lines={custom,"\u751f\u65e5\u5feb\u4e50\uff0c\u613f\u671b\u6210\u771f\u3002","\u613f\u6240\u6709\u7684\u7f8e\u597d\u90fd\u5411\u4f60\u5954\u6765\u3002","\u4eca\u5929\u662f\u72ec\u4e00\u65e0\u4e8c\u7684\u5e78\u798f\u65e5\u3002","\u613f\u4f60\u773c\u91cc\u6709\u661f\u5149\uff0c\u5fc3\u4e2d\u6709\u70ed\u7231\u3002","\u613f\u4f60\u6bcf\u4e00\u6b65\uff0c\u90fd\u8e0f\u7740\u81ea\u5df1\u7684\u5149\u3002"};for(int i=0;i<lines.Length;i++)wishes.Add(NewWish(lines[i],i));}
    public void Celebrate(){frame=0;bits.Clear();for(int i=0;i<175;i++)bits.Add(NewBit(true));if(wishes.Count==0)SetWishes("\u751f\u65e5\u5feb\u4e50\uff01");timer.Start();Invalidate();}
    protected override void OnPaintBackground(PaintEventArgs e){}
    protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAlias;g.Clear(Color.Black);GraphicsState state=g.Save();g.ScaleTransform(RenderScale,RenderScale);DrawPaperGrain(g);DrawCode(g);DrawDoodles(g);DrawHeading(g);DrawCake(g);foreach(Wish w in wishes)DrawWish(g,w);foreach(Bit b in bits)DrawBit(g,b);g.Restore(state);}
    Color[] Palette(){return Theme==ThemeKind.Galaxy?galaxy:Theme==ThemeKind.Forest?forest:rose;}
    void DrawPaperGrain(Graphics g){Color[] p=Palette();for(int i=0;i<38;i++){int x=(i*97+frame/4)%Math.Max(1,LW);int y=(i*53+31)%Math.Max(1,LH);using(Brush b=new SolidBrush(Color.FromArgb(25,p[i%p.Length])))g.FillEllipse(b,x,y,2+(i%3),2+(i%3));}}
    void DrawCode(Graphics g){Color chalk=Color.FromArgb(150,205,205,205);using(Brush b=new SolidBrush(chalk))using(Font f=new Font("Consolas",7.2f)){g.DrawString("le(x, y, Beta=1.15);",f,b,27,29);g.DrawString("ratio): 1 \u4e2a\u661f\u671f",f,b,37,48);g.DrawString("g x  .  g y",f,b,46,106);g.DrawString("focus: CANVAS_CENTER",f,b,31,126);g.DrawString("period . . .",f,b,47,154);g.DrawString("wish = true;",f,b,LW-165,119);g.DrawString("const joy = 100%;",f,b,LW-178,139);}}
    void DrawDoodles(Graphics g){Color[] p=Palette();int wobble=(int)(Math.Sin(frame*.06)*3);Star(g,105,84,15,p[0],true);Star(g,LW-102,130,19,p[Theme==ThemeKind.Galaxy?1:0],true);Star(g,LW-77,255,14,p[1],true);Heart(g,58,207,p[0]);Heart(g,LW-152,76,p[Theme==ThemeKind.Forest?3:1]);for(int i=0;i<11;i++){int x=(i*97+frame/3)%Math.Max(1,LW);int y=190+(i*57)%(Math.Max(1,LH-315));using(Brush b=new SolidBrush(Color.FromArgb(180,p[i%p.Length])))g.FillEllipse(b,x,y,3+(i%3),3+(i%3));}using(Pen pen=new Pen(Color.FromArgb(220,242,234,213),2)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawArc(pen,LW-294,222+wobble,71,44,183,133);g.DrawLine(pen,LW-250,250+wobble,LW-240,277+wobble);g.DrawLine(pen,LW-264,271+wobble,LW-240,277+wobble);g.DrawLine(pen,LW-240,277+wobble,LW-227,256+wobble);}}
    void DrawHeading(Graphics g){using(Brush white=new SolidBrush(Color.FromArgb(247,244,235)))using(Font english=new Font("Segoe Print",28,FontStyle.Bold))g.DrawString("Happy Birthday",english,white,LW/2-158,14);string line=Person==""?"\u751f\u65e5\u5feb\u4e50\uff01":Person;using(Brush b=new SolidBrush(Color.FromArgb(244,239,224)))using(Font cn=new Font("KaiTi",24,FontStyle.Bold))g.DrawString(line,cn,b,LW-316,Math.Max(253,LH/2-48));using(Pen p=new Pen(Color.FromArgb(218,242,235,218),1.5f)){p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;g.DrawLine(p,LW/2-153,58,LW/2+139,56);g.DrawArc(p,LW/2-90,48,180,25,185,170);}}
    void DrawCake(Graphics g){int c=LW/2,y=LH-198;Color[] p=Palette();Color chalk=Color.FromArgb(238,238,228,204);Color tint=p[1];using(Pen basePen=new Pen(chalk,2.2f)){basePen.StartCap=LineCap.Round;basePen.EndCap=LineCap.Round;Tier(g,basePen,c-155,y+81,310,62);Tier(g,basePen,c-118,y+30,236,58);Tier(g,basePen,c-78,y-16,156,50);Frost(g,basePen,c-155,y+84,310);Frost(g,basePen,c-118,y+33,236);Frost(g,basePen,c-78,y-13,156);g.DrawArc(basePen,c-151,y+126,302,25,0,180);g.DrawLine(basePen,c-156,y+92,c-151,y+139);g.DrawLine(basePen,c+156,y+92,c+151,y+139);g.DrawLine(basePen,c,y-56,c,y-18);g.DrawLine(basePen,c-8,y-56,c+8,y-56);g.DrawEllipse(basePen,c-8,y-78,16,24);}using(Brush b=new SolidBrush(tint)){for(int i=0;i<13;i++)g.FillEllipse(b,c-129+i*21,y+103+(i%2)*10,5,5);g.FillEllipse(b,c-3,y-74+(int)(Math.Sin(frame*.25)*3),6,10);}using(Font f=new Font("Segoe Print",14,FontStyle.Bold))using(Brush b=new SolidBrush(chalk))g.DrawString("HAPPY BIRTHDAY",f,b,c-95,y+87);}
    void Tier(Graphics g,Pen p,int x,int y,int w,int h){g.DrawArc(p,x,y,w,18,180,180);g.DrawLine(p,x,y+9,x+8,y+h);g.DrawLine(p,x+w,y+9,x+w-8,y+h);g.DrawArc(p,x+8,y+h-18,w-16,18,0,180);}void Frost(Graphics g,Pen p,int x,int y,int w){for(int i=0;i<6;i++){int px=x+15+i*(w-30)/6;g.DrawArc(p,px,y-3,24,17,0,180);}}
    void DrawWish(Graphics g,Wish w){Color c=w.C;using(Brush b=new SolidBrush(Color.FromArgb(238,c)))using(Font f=new Font("KaiTi",w.Size,FontStyle.Bold))g.DrawString(w.Text,f,b,w.X,w.Y);using(Pen underline=new Pen(Color.FromArgb(150,c),1.4f)){underline.StartCap=LineCap.Round;underline.EndCap=LineCap.Round;g.DrawArc(underline,w.X+6,w.Y+w.Size+3,Math.Max(80,w.Width-14),7,190,160);}}
    void Star(Graphics g,int x,int y,int r,Color c,bool fill){PointF[] pts=new PointF[10];for(int i=0;i<10;i++){double a=-Math.PI/2+i*Math.PI/5;float rr=i%2==0?r:r*.45f;pts[i]=new PointF(x+(float)Math.Cos(a)*rr,y+(float)Math.Sin(a)*rr);}using(Brush b=new SolidBrush(c))g.FillPolygon(b,pts);}void Heart(Graphics g,int x,int y,Color c){using(Brush b=new SolidBrush(c)){g.FillEllipse(b,x,y,12,12);g.FillEllipse(b,x+11,y,12,12);Point[] q={new Point(x,y+7),new Point(x+23,y+7),new Point(x+12,y+25)};g.FillPolygon(b,q);}}
    Bit NewBit(bool burst){Color[] p=Palette();return new Bit{X=burst?LW/2+random.Next(-170,171):random.Next(Math.Max(1,LW)),Y=burst?LH*.56f+random.Next(-45,46):-12,VX=burst?random.Next(-58,59)/10f:random.Next(-7,8)/10f,VY=burst?random.Next(-100,-32)/10f:random.Next(12,37)/10f,A=random.Next(360),S=random.Next(-10,11),C=p[random.Next(p.Length)],Type=random.Next(4)};}
    Wish NewWish(string text,int i){return new Wish{Text=text,X=LW+random.Next(0,Math.Max(220,LW)),Y=210+(i%5)*48,Speed=.62f+random.Next(35,100)/100f,Width=Math.Max(150,text.Length*19+30),Size=i%3==0?18:15,C=Palette()[i%Palette().Length]};}
    void MoveScene(){frame++;for(int i=bits.Count-1;i>=0;i--){Bit b=bits[i];b.X+=b.VX;b.Y+=b.VY;b.VY+=.095f;b.A+=b.S;if(b.Y>LH+35)bits.RemoveAt(i);}if(frame%5==0)bits.Add(NewBit(false));if(frame%330==0)for(int i=0;i<105;i++)bits.Add(NewBit(true));if(bits.Count>460)bits.RemoveRange(0,bits.Count-460);for(int i=0;i<wishes.Count;i++){Wish w=wishes[i];w.X-=w.Speed;if(w.X+w.Width<0){w.X=LW+random.Next(80,340);w.Y=210+random.Next(0,5)*48;w.Speed=.62f+random.Next(35,100)/100f;w.Size=random.Next(0,3)==0?18:15;}}Invalidate();}
    void DrawBit(Graphics g,Bit b){GraphicsState s=g.Save();g.TranslateTransform(b.X,b.Y);g.RotateTransform(b.A);using(Brush p=new SolidBrush(b.C)){if(b.Type==0)g.FillEllipse(p,-3,-3,6,6);else if(b.Type==1)Star(g,0,0,6,b.C,true);else if(b.Type==2)Heart(g,-6,-6,b.C);else g.FillRectangle(p,-3,-7,6,14);}g.Restore(s);}
}class Bit{public float X,Y,VX,VY,A,S;public Color C;public int Type;}class Wish{public float X,Y,Speed,Size;public int Width;public string Text;public Color C;}
