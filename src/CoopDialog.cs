// Copyright (C) 2026. GPL-3.0-or-later.
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

class CoopDialog : Form
{
    internal string Action, Archive;
    internal CoopDialog(string root)
    {
        Text = "Optional xrRazom co-op"; ClientSize = new Size(600, 415); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(19,24,25); ForeColor = Color.White;
        Controls.Add(new Label { Text = "xrRazom 1.4 · optional co-op", Font = new Font("Segoe UI", 20, FontStyle.Bold), Bounds = new Rectangle(24,20,550,40) });
        Controls.Add(new Label { Text = "Use the official Slim ZIP or Full Bundle (core files only).\nSetup creates a separate MO2 profile and engine backups.\nIt stays in single-player until you select Co-op.\n\nLAN: close Steam first. Steam: everyone needs Call of Pripyat.\nSet your player name and Host/Join options inside xrRazom.\nUse distinct co-op saves; profiles may share the saves folder.\n\nTwo-player gameplay is still untested in this launcher.", Bounds = new Rectangle(24,75,550,200) });
        var download = new Button { Text = "Download and set up", Bounds = new Rectangle(24,285,175,40) };
        download.Click += (s,e) => { Action="install"; Archive=null; DialogResult=DialogResult.OK; }; Controls.Add(download);
        var web = new LinkLabel { Text="Open official download page if automatic download is blocked", LinkColor=Color.LightGreen, Bounds=new Rectangle(24,385,550,24) };
        web.LinkClicked += (s,e) => Process.Start(new ProcessStartInfo(CoopManager.DownloadPage) { UseShellExecute=true }); Controls.Add(web);
        var install = new Button { Text = "Install from ZIP", Bounds = new Rectangle(212,285,175,40) };
        install.Click += (s,e) => { using (var files = new OpenFileDialog { Filter = "Official xrRazom Slim ZIP|*.zip", CheckFileExists = true }) if (files.ShowDialog(this) == DialogResult.OK) { Action = "install"; Archive = files.FileName; DialogResult = DialogResult.OK; } }; Controls.Add(install);
        var recover = new Button { Text = "Recover switch", Bounds = new Rectangle(400,285,175,40) };
        recover.Click += (s,e) => { Action = "recover"; DialogResult = DialogResult.OK; }; Controls.Add(recover);
        var export = new Button { Text="Export my compatibility", Bounds=new Rectangle(24,340,270,38) }; Controls.Add(export);
        export.Click += async (s,e) =>
        {
            using(var save=new SaveFileDialog { Filter="Co-op manifest|*.xml", FileName="gamma-easy-coop.xml" })
            if(save.ShowDialog(this)==DialogResult.OK)
            {
                export.Enabled=false;
                try { var data=await Task.Run(()=>CoopFingerprint.Scan(root)); CoopFingerprint.Save(data,save.FileName); MessageBox.Show("Manifest saved. Share it yourself with your friend. It contains mod names, relative file paths and hashes; personal MCM settings are excluded."); }
                catch(Exception error) { MessageBox.Show(error.Message,"Compatibility scan"); }
                finally { if(!IsDisposed) export.Enabled=true; }
            }
        };
        var compare = new Button { Text="Compare friend's manifest", Bounds=new Rectangle(307,340,268,38) }; Controls.Add(compare);
        compare.Click += async (s,e) =>
        {
            using(var open=new OpenFileDialog { Filter="Co-op manifest|*.xml" })
            if(open.ShowDialog(this)==DialogResult.OK)
            {
                compare.Enabled=false;
                try { var friend=CoopFingerprint.Read(open.FileName); var mine=await Task.Run(()=>CoopFingerprint.Scan(root)); var differences=CoopFingerprint.Compare(mine,friend); string report=System.IO.Path.Combine(Program.Home,"coop-comparison.txt"); File.WriteAllLines(report,differences.Count==0 ? new[]{"Compared config/script files, engines and mod order match. This does not certify every world/asset file."} : differences.ToArray()); Process.Start("notepad.exe",Program.Quote(report)); }
                catch(Exception error) { MessageBox.Show(error.Message,"Compatibility comparison"); }
                finally { if(!IsDisposed) compare.Enabled=true; }
            }
        };
    }
}
