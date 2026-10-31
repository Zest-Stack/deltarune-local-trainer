using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows.Forms;

static class Program
{
    [STAThread] static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        string save = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DELTARUNE");
        if (args.Length == 2 && args[0] == "--save-dir") save = Path.GetFullPath(args[1]);
        bool fresh;
        using (var mutex = new System.Threading.Mutex(true, "Local\\DeltaruneLocalTrainerV1", out fresh))
        {
            if (!fresh) { MessageBox.Show("修改器已在运行，请切换到已有窗口。", "DELTARUNE"); return; }
            Application.Run(new Trainer(save));
        }
    }
}

sealed class Trainer : Form
{
    readonly string save;
    readonly string root = @"D:\Steam\steamapps\common\DELTARUNE";
    readonly Timer timer = new Timer();
    readonly Label state = new Label(), live = new Label(), notice = new Label();
    readonly CheckBox hpLock = new CheckBox(), god = new CheckBox(), tpLock = new CheckBox();
    readonly NumericUpDown hp = new NumericUpDown(), df = new NumericUpDown(), gold = new NumericUpDown(), tp = new NumericUpDown();
    readonly ComboBox party = new ComboBox();
    readonly List<Button> actions = new List<Button>();
    readonly Color ink = Color.FromArgb(228,235,245), muted = Color.FromArgb(159,176,197), accent = Color.FromArgb(110,231,183);
    long seq = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    int op, target; decimal val;
    bool ready, battle, closing;
    int chapter;
    DateTime sent;
    Dictionary<string,string> status = new Dictionary<string,string>();

    public Trainer(string path)
    {
        save = path;
        Text = "DELTARUNE · 本地修改器";
        ClientSize = new Size(780,675); MinimumSize = Size; MaximumSize = Size;
        StartPosition = FormStartPosition.CenterScreen; MaximizeBox = false;
        BackColor = Color.FromArgb(15,22,33); ForeColor = ink;
        Font = new Font("Microsoft YaHei UI",10F); AutoScaleMode = AutoScaleMode.Dpi;
        Label title = LabelAt("DELTARUNE",24,18,500,36); title.Font = new Font("Segoe UI",23F,FontStyle.Bold);
        LabelAt("本地控制台  /  免费 · 无需联网",26,62,600,26).ForeColor = muted;
        state.SetBounds(26,101,725,30); state.ForeColor = accent; Controls.Add(state);
        live.SetBounds(26,137,725,78); live.Font = new Font("Microsoft YaHei UI",10F); Controls.Add(live);
        LabelAt("持续效果",26,230,150,25).ForeColor = muted;
        Check(hpLock,"全队锁血（保持满血）",26,265);
        Check(god,"无敌（拦截常规伤害）",290,265);
        Check(tpLock,"锁定 TP",560,265);
        LabelAt("修改目标",26,318,85,28);
        party.SetBounds(116,315,236,30); party.DropDownStyle = ComboBoxStyle.DropDownList;
        party.Items.AddRange(new object[]{"当前队伍全部角色","Kris","Susie","Ralsei","Noelle"}); party.SelectedIndex=0; Controls.Add(party);
        LabelAt("仅修改当前在队角色",370,318,350,28).ForeColor=muted;
        Row("生命",hp,99999,999,363);
        ButtonAt("设置当前生命",300,360,143,34,delegate { Command(1,hp.Value); });
        ButtonAt("设为生命上限并回满",455,360,221,34,delegate { Command(2,hp.Value); });
        Row("基础防御",df,9999,99,410);
        ButtonAt("设置防御",300,407,143,34,delegate { Command(3,df.Value); });
        ButtonAt("全队回满生命",455,407,221,34,delegate { Command(6,0); });
        Row("暗世界金钱",gold,9999999,99999,457);
        ButtonAt("设置金钱",300,454,143,34,delegate { Command(4,gold.Value); });
        Row("TP 百分比",tp,100,100,504);
        ButtonAt("立即设置 TP",300,501,143,34,delegate { Command(5,tp.Value); });
        tp.ValueChanged += delegate { WriteControl(false); };
        LabelAt("TP 只在战斗中生效；锁定值跟随此输入框。",455,504,305,40).ForeColor=muted;
        notice.SetBounds(26,550,728,42); notice.ForeColor=muted; Controls.Add(notice);
        ButtonAt("启动游戏",26,609,140,36,delegate { Launch(); },false);
        ButtonAt("关闭全部效果",182,609,160,36,delegate { Disable(); },false);
        ButtonAt("恢复原游戏文件",358,609,175,36,delegate { Restore(); },false);
        ButtonAt("使用说明",549,609,127,36,delegate { Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"使用说明.txt")){UseShellExecute=true}); },false);
        Label author = LabelAt("By Zest-Stack",26,650,150,22); author.ForeColor = muted;
        LinkLabel github = new LinkLabel { Text = "GitHub: github.com/Zest-Stack", AutoSize = true, LinkColor = accent, BackColor = BackColor };
        github.SetBounds(170,650,300,22); github.Click += delegate { Process.Start(new ProcessStartInfo("https://github.com/Zest-Stack") { UseShellExecute = true }); }; Controls.Add(github);
        FormClosing += delegate { closing=true; timer.Stop(); WriteControl(true); };
        timer.Interval=500; timer.Tick+=delegate { Poll(); }; timer.Start();
        notice.Text="先启动游戏并进入章节。数值修改不会自动保存进度。";
        Poll();
    }
    Label LabelAt(string text,int x,int y,int w,int h) { var l=new Label{Text=text}; l.SetBounds(x,y,w,h);Controls.Add(l);return l; }
    void Check(CheckBox box,string text,int x,int y) { box.Text=text; box.SetBounds(x,y,260,30); Controls.Add(box);box.CheckedChanged+=delegate{WriteControl(false);}; }
    void Row(string text,NumericUpDown n,decimal max,decimal value,int y)
    {
        LabelAt(text,26,y,130,28); n.SetBounds(166,y-3,120,32); n.Maximum=max;n.Minimum=text=="生命"?1:0;n.Value=value;n.ThousandsSeparator=true;
        n.BackColor=Color.FromArgb(30,42,58);n.ForeColor=ink;Controls.Add(n);
    }
    void ButtonAt(string text,int x,int y,int w,int h,Action action,bool command=true)
    {
        var b=new Button{Text=text,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(30,44,62),ForeColor=ink};
        b.FlatAppearance.BorderColor=Color.FromArgb(66,86,110);b.SetBounds(x,y,w,h);b.Click+=delegate{try{action();}catch(Exception e){notice.Text=e.Message;}};Controls.Add(b); if(command)actions.Add(b);
    }
    static Dictionary<string,string> Parse(string path)
    {
        var d=new Dictionary<string,string>();
        using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
        using(var r=new StreamReader(f)) { string line;while((line=r.ReadLine())!=null){int n=line.IndexOf('=');if(n>0)d[line.Substring(0,n)]=line.Substring(n+1);} }
        return d;
    }
    string Get(string key,string fallback="—") { string s; return status.TryGetValue(key,out s)?s:fallback; }
    void Poll()
    {
        try
        {
            Directory.CreateDirectory(save);
            FileInfo newest=null;
            foreach(string f in Directory.GetFiles(save,"codex_trainer_status_*.txt")){var fi=new FileInfo(f);if(newest==null||fi.LastWriteTimeUtc>newest.LastWriteTimeUtc)newest=fi;}
            bool connected=newest!=null&&(DateTime.UtcNow-newest.LastWriteTimeUtc).TotalSeconds<3;
            if(connected)
            {
                var next=Parse(newest.FullName);
                if(!next.ContainsKey("complete")){WriteControl(false);return;}
                status=next;
                int nextChapter;int.TryParse(Get("chapter","0"),out nextChapter);
                if(chapter!=nextChapter){chapter=nextChapter;op=0;seq++;}
                ready=Get("ready")=="1";battle=Get("battle")=="1";
                state.Text="● 已连接第 "+chapter+" 章  ·  "+(ready?(battle?"战斗中":"探索中"):"请进入可操作场景");
                string team="";string[] names={"", "Kris", "Susie", "Ralsei", "Noelle"};
                for(int i=0;i<3;i++) { string p=Get("party"+i,"");if(p.Length==0)continue;string[] a=p.Split(',');int id;if(a.Length==4&&int.TryParse(a[0],out id))team+=(id<names.Length?names[id]:"角色"+id)+"  "+a[1]+" / "+a[2]+" HP · 防御 "+a[3]+"    "; }
                live.Text="金钱  "+Get("gold")+"     TP  "+Get("tp")+"%\n"+team+"\n"+Get("room","");
                if(op!=0 && Get("ack")==seq.ToString(CultureInfo.InvariantCulture)){op=0;notice.Text="已生效；上方显示游戏回读的实际数值。";}
                else if(op!=0 && (DateTime.UtcNow-sent).TotalSeconds>4){op=0;seq++;notice.Text="本次修改未收到确认，已取消。请返回游戏后重试。";}
            }
            else
            {
                ready=false;battle=false;op=0;
                state.Text="○ 等待游戏连接 · 请从 Steam 启动并进入章节";
                live.Text="支持本机已适配的第 1～5 章。游戏暂停或失去响应时回读也会暂停。";
            }
            foreach(var b in actions)b.Enabled=ready;
            actions[5].Enabled=ready&&battle;
            WriteControl(false);
        }
        catch(Exception e){ready=false;state.Text="连接暂不可用";notice.Text=e.Message;foreach(var b in actions)b.Enabled=false;}
    }
    void Command(int code,decimal value)
    {
        if(!ready || (code==5&&!battle)) {notice.Text="请先进入对应的游戏场景。";return;}
        if(op!=0){notice.Text="上一条修改正在等待游戏确认。";return;}
        op=code;val=value;target=code==6?0:party.SelectedIndex;seq++;sent=DateTime.UtcNow;
        if(code==1&&hpLock.Checked)notice.Text="锁血开启时生命会立即回满；要设置当前生命，请先关闭锁血。";
        else notice.Text="正在等待游戏确认…";
        WriteControl(false);
    }
    void WriteControl(bool off)
    {
        try
        {
            if(closing&&!off)return;
            Directory.CreateDirectory(save);
            string text="[control]\nheartbeat="+DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()+"\nenabled="+(off?0:1)+"\nlockhp="+(!off&&hpLock.Checked?1:0)+"\ngod="+(!off&&god.Checked?1:0)+"\nlocktp="+(!off&&tpLock.Checked?1:0)+"\ntp="+tp.Value.ToString(CultureInfo.InvariantCulture)+"\nseq="+seq+"\nop="+(off?0:op)+"\nvalue="+val.ToString(CultureInfo.InvariantCulture)+"\ntarget="+target+"\n";
            string path=Path.Combine(save,"codex_trainer_control.ini"),tmp=path+".tmp";
            File.WriteAllText(tmp,text,new UTF8Encoding(false));
            if(File.Exists(path))File.Replace(tmp,path,null);else File.Move(tmp,path);
        }
        catch(Exception e){notice.Text="控制文件写入失败："+e.Message;}
    }
    void Disable(){hpLock.Checked=false;god.Checked=false;tpLock.Checked=false;op=0;seq++;WriteControl(false);notice.Text="持续效果已关闭。已修改的数值不会自动恢复，可重新读档撤销未保存的修改。";}
    void Launch(){Process.Start(new ProcessStartInfo("steam://rungameid/1671210"){UseShellExecute=true});}
    static string Hash(string file){using(var s=File.OpenRead(file))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(s)).Replace("-","");}
    void Restore()
    {
        foreach(var p in Process.GetProcessesByName("DELTARUNE")) { if(!p.HasExited){MessageBox.Show("请先退出游戏，再恢复文件。",Text);return;} }
        string manifest=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"installed.tsv");
        if(!File.Exists(manifest))throw new IOException("未找到安装记录，无法自动恢复。");
        var lines=File.ReadAllLines(manifest);
        foreach(string line in lines)
        {
            string[] a=line.Split('\t');if(a.Length!=4)throw new IOException("安装记录格式错误。");
            string current=Path.GetFullPath(Path.Combine(root,a[0])),backup=Path.GetFullPath(Path.Combine(root,a[1]));
            if(!current.StartsWith(root+"\\",StringComparison.OrdinalIgnoreCase)||!backup.StartsWith(root+"\\",StringComparison.OrdinalIgnoreCase))throw new IOException("安装路径校验失败。");
            if(Hash(backup)!=a[2])throw new IOException("原文件备份校验失败，已停止恢复。");
            string hash=Hash(current);if(hash!=a[3]&&hash!=a[2])throw new IOException("游戏文件已被更新或再次打补丁，自动恢复已停止，以免覆盖新版。");
        }
        if(MessageBox.Show("恢复安装修改器前的五个章节文件（保留当时的汉化）？存档不受影响。",Text,MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
        Disable();
        foreach(string line in lines){var a=line.Split('\t');string dst=Path.Combine(root,a[0]),tmp=dst+".cdt-restore";File.Copy(Path.Combine(root,a[1]),tmp,true);File.Replace(tmp,dst,null);}
        notice.Text="原游戏文件已恢复。要再次使用需重新安装控制补丁。";
    }
}
