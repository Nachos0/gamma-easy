// GAMMA Easy 0.1.0. Copyright (C) 2026. GPL-3.0-or-later.
// Independent community launcher. Not an official GAMMA or GSC product.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

class MainForm : Form
{
    ComboBox drives;
    Label location, status, detail;
    Button install, play, stop, repair;
    CheckBox exclusions, runtimes;
    ProgressBar progress;
    TextBox logs;
    InstallState state;
    Process worker;
    System.Windows.Forms.Timer timer;
    bool busy;
    string request;
    int logLength;
    internal MainForm(InstallState saved, bool preview)
    {
        state = saved ?? new InstallState();
        Text = "GAMMA Easy · 0.1"; ClientSize = new Size(850, 680); MinimumSize = Size;
        BackColor = Color.FromArgb(19, 24, 25); ForeColor = Color.FromArgb(232, 235, 226);
        Font = new Font("Segoe UI", 10); StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(new Label { Text = "GAMMA EASY", Font = new Font("Segoe UI", 28, FontStyle.Bold), Bounds = new Rectangle(32, 23, 600, 57) });
        Controls.Add(new Label { Text = "Your way into the Zone.", ForeColor = Color.FromArgb(157, 174, 166), Bounds = new Rectangle(36, 82, 700, 27) });
        Controls.Add(new Label { Text = "INSTALL LOCATION", Font = new Font("Segoe UI", 10, FontStyle.Bold), Bounds = new Rectangle(36, 129, 730, 25) });
        drives = new ComboBox { Bounds = new Rectangle(36, 158, 778, 34), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 12), BackColor = Color.FromArgb(36, 44, 44), ForeColor = ForeColor };
        Controls.Add(drives);
        location = new Label { Bounds = new Rectangle(36, 205, 778, 44), ForeColor = Color.FromArgb(166, 183, 176) }; Controls.Add(location);
        exclusions = new CheckBox { Text = "Add only the game folders to Windows Defender exclusions", Bounds = new Rectangle(36, 251, 778, 26), Checked = state.Exclusions }; Controls.Add(exclusions);
        runtimes = new CheckBox { Text = "Install required Microsoft Visual C++ and DirectX components", Bounds = new Rectangle(36, 281, 778, 26), Checked = state.Runtimes }; Controls.Add(runtimes);
        install = MakeButton("Install", 36, 329, 250, true); install.Click += StartInstall;
        play = MakeButton("Play", 303, 329, 250, false); play.Click += Play;
        stop = MakeButton("Stop", 570, 329, 244, false); stop.Enabled = false; stop.Click += Cancel;
        status = new Label { Text = "Choose a drive to begin", Font = new Font("Segoe UI", 14, FontStyle.Bold), Bounds = new Rectangle(36, 393, 778, 34) }; Controls.Add(status);
        detail = new Label { Text = "200 GB free recommended · internal SSD preferred", ForeColor = Color.FromArgb(166, 183, 176), Bounds = new Rectangle(36, 430, 778, 40) }; Controls.Add(detail);
        progress = new ProgressBar { Bounds = new Rectangle(36, 473, 778, 9), Style = ProgressBarStyle.Continuous }; Controls.Add(progress);
        logs = new TextBox { Bounds = new Rectangle(36, 496, 778, 112), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(12, 16, 17), ForeColor = Color.FromArgb(174, 192, 174), Font = new Font("Consolas", 9), BorderStyle = BorderStyle.FixedSingle }; Controls.Add(logs);
        repair = MakeButton("Repair / update", 36, 623, 180, false); repair.Height = 32; repair.Click += StartInstall;
        var openLogs = MakeButton("Open log", 230, 623, 140, false); openLogs.Height = 32; openLogs.Click += (s, e) => { if (File.Exists(Program.LogFile)) Process.Start("notepad.exe", Program.Quote(Program.LogFile)); };
        var openMods = MakeButton("Mod Organizer", 384, 623, 170, false); openMods.Height = 32; openMods.Click += (s, e) => { if (!busy && !string.IsNullOrEmpty(state.Root)) OpenMo2(false); };
        var license = MakeButton("About", 568, 623, 246, false); license.Height = 32; license.Click += (s, e) => MessageBox.Show("GAMMA Easy 0.1.0\nIndependent community app. Not affiliated with GAMMA, GSC or FaithBeam.\n\nInstaller engine: FaithBeam/stalker-gamma-cli 1.37.1\nhttps://github.com/FaithBeam/stalker-gamma-cli\n\nGPL-3.0-or-later. Source and third-party notices accompany this build.\nNo game files are bundled. Antivirus is never disabled.\n\nThis first build has not completed a full game installation test.", "About GAMMA Easy");
        drives.SelectedIndexChanged += (s, e) => RefreshReady();
        timer = new System.Windows.Forms.Timer { Interval = 500 }; timer.Tick += Poll;
        FormClosing += (s, e) => { if (busy) { Cancel(s, EventArgs.Empty); e.Cancel = true; } };
        if (preview)
        {
            drives.Items.Add(new DriveChoice { Root = "D:\\", Text = "D:  ·  Games  ·  488 GB free" }); drives.SelectedIndex = 0; return;
        }
        Shown += async (s, e) =>
        {
            status.Text = "Checking available drives…";
            var choices = await Task.Run(() => GetDrives());
            foreach (var choice in choices) drives.Items.Add(choice);
            int chosen = choices.FindIndex(x => x.Root == state.Root);
            if (choices.Count > 0) drives.SelectedIndex = chosen >= 0 ? chosen : 0;
            else { status.Text = "No supported internal drive found"; detail.Text = "An internal NTFS drive is required. Drive identification must be available."; install.Enabled = false; play.Enabled = false; repair.Enabled = false; }
            if (File.Exists(Program.LogFile)) { string text = File.ReadAllText(Program.LogFile); logs.Text = text.Length > 12000 ? text.Substring(text.Length - 12000) : text; }
        };
        play.Enabled = false; repair.Enabled = false;
    }
    Button MakeButton(string title, int x, int y, int width, bool accent)
    {
        var button = new Button { Text = title, Bounds = new Rectangle(x, y, width, 46), FlatStyle = FlatStyle.Flat, BackColor = accent ? Color.FromArgb(143, 174, 86) : Color.FromArgb(36, 44, 44), ForeColor = accent ? Color.FromArgb(18, 25, 17) : ForeColor, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
        button.FlatAppearance.BorderColor = Color.FromArgb(69, 83, 74); Controls.Add(button); return button;
    }
    static List<DriveChoice> GetDrives()
    {
        var result = new List<DriveChoice>();
        foreach (var d in DriveInfo.GetDrives())
        {
            try { if (d.IsReady && d.DriveType == DriveType.Fixed && d.DriveFormat == "NTFS" && Safety.IsInternal(d.Name)) result.Add(new DriveChoice { Root = d.Name, Text = d.Name.Substring(0, 2) + "  ·  " + d.VolumeLabel + "  ·  " + (d.AvailableFreeSpace / 1000000000L) + " GB free" }); }
            catch { }
        }
        return result.OrderByDescending(x => new DriveInfo(x.Root).AvailableFreeSpace).ToList();
    }
    string SelectedRoot { get { return drives.SelectedItem == null ? "" : ((DriveChoice)drives.SelectedItem).Root; } }
    void RefreshReady()
    {
        if (busy) return;
        location.Text = "Game folders: " + SelectedRoot + "Anomaly   and   " + SelectedRoot + "GAMMA\nDownloads stay on the same drive.";
        bool ready = false;
        if (state.Phase == "Ready" && state.Root == SelectedRoot)
        {
            try { Safety.VerifyGame(SelectedRoot + "Anomaly", SelectedRoot + "GAMMA"); ready = true; }
            catch (Exception e) { detail.Text = e.Message; }
        }
        bool restart = ready && Program.RestartPending(state);
        play.Enabled = ready && !restart; install.Enabled = SelectedRoot.Length > 0; repair.Enabled = state.Root == SelectedRoot && state.Phase != "New";
        install.Text = ready ? "Installed" : (state.Root == SelectedRoot && state.Phase != "New" ? "Retry installation" : "Install"); install.Enabled &= !ready;
        status.Text = ready ? "Ready to play" : (state.Phase == "Failed" && state.Root == SelectedRoot ? "Installation needs attention" : "Choose your drive, then install");
        if (ready) { detail.Text = restart ? "Microsoft components require a Windows restart before playing." : "GAMMA profile checked. Play starts DX11 through Mod Organizer."; progress.Value = 100; if (restart) status.Text = "Installed · restart Windows to play"; }
        else if (state.Phase == "Failed" && state.Root == SelectedRoot) detail.Text = state.Error;
        else detail.Text = "Fresh installation needs 200 GB free. Windows will request permission to install.";
    }
    async void StartInstall(object sender, EventArgs e)
    {
        if (busy || SelectedRoot.Length == 0) return;
        string root = SelectedRoot;
        try
        {
            if (sender == repair && state.Phase == "Ready" && MessageBox.Show("Repair / update reapplies the official GAMMA profile. Custom mod selections and launcher entries may be reset. Continue?", "Repair / update", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
            install.Enabled = false; repair.Enabled = false; status.Text = "Checking installation location…";
            await Task.Run(() => Safety.ValidateDrive(root, true));
            if (Process.GetProcessesByName("ModOrganizer").Length > 0 || Process.GetProcessesByName("AnomalyDX11").Length > 0 || Process.GetProcessesByName("AnomalyDX11AVX").Length > 0)
                throw new IOException("Close Mod Organizer and the game before installing or updating.");
            state.Root = root; state.Exclusions = exclusions.Checked; state.Runtimes = runtimes.Checked; state.Phase = "Installing"; state.Error = "";
            Program.SaveState(state);
            request = Guid.NewGuid().ToString("N");
            var start = new ProcessStartInfo(Application.ExecutablePath, "--worker " + request) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = Program.Home };
            worker = Process.Start(start);
            busy = true; drives.Enabled = exclusions.Enabled = runtimes.Enabled = false; play.Enabled = repair.Enabled = install.Enabled = false; stop.Enabled = true;
            progress.Style = ProgressBarStyle.Marquee; status.Text = "Installing Anomaly and GAMMA";
            detail.Text = "Keep the app open. Stop keeps downloaded archives for your next attempt.";
            logs.Clear(); logLength = 0; timer.Start();
        }
        catch (Exception error) { status.Text = "Cannot start installation"; detail.Text = error is Win32Exception && ((Win32Exception)error).NativeErrorCode == 1223 ? "Windows permission was cancelled. Click Install to try again." : error.Message; install.Enabled = true; repair.Enabled = true; }
    }
    void Cancel(object sender, EventArgs e)
    {
        if (!busy) return;
        File.WriteAllText(Path.Combine(Program.Home, "cancel-" + request), "cancel");
        stop.Enabled = false; status.Text = "Stopping safely…";
    }
    void Poll(object sender, EventArgs e)
    {
        try
        {
            if (File.Exists(Program.LogFile))
            {
                using (var stream = new FileStream(Program.LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int length = (int)Math.Min(int.MaxValue, stream.Length);
                    if (length != logLength)
                    {
                        logLength = length; stream.Seek(Math.Max(0, stream.Length - 32000), SeekOrigin.Begin);
                        using (var reader = new StreamReader(stream)) logs.Text = reader.ReadToEnd();
                        logs.SelectionStart = logs.TextLength; logs.ScrollToCaret();
                    }
                }
            }
            if (worker == null || !worker.HasExited) return;
            timer.Stop(); busy = false; drives.Enabled = exclusions.Enabled = runtimes.Enabled = true; stop.Enabled = false; progress.Style = ProgressBarStyle.Continuous; progress.Value = 0;
            var loaded = Program.LoadState();
            if (loaded.Phase == "Installing") { loaded.Phase = "Failed"; loaded.Error = "Installer stopped unexpectedly. Open the log, then retry."; Program.SaveState(loaded); }
            state = loaded; worker.Dispose(); worker = null; RefreshReady();
        }
        catch (IOException) { }
    }
    void Play(object sender, EventArgs e)
    {
        try { if (busy) return; if (Program.RestartPending(state)) throw new IOException("Restart Windows after installing Microsoft components, then try Play again."); Safety.VerifyGame(state.Root + "Anomaly", state.Root + "GAMMA"); OpenMo2(true); }
        catch (Exception error) { status.Text = "Game could not start"; detail.Text = error.Message; play.Enabled = false; repair.Enabled = true; }
    }
    void OpenMo2(bool run)
    {
        try
        {
            string gamma = state.Root + "GAMMA";
            Process.Start(new ProcessStartInfo(Path.Combine(gamma, "ModOrganizer.exe"), run ? "-p " + Program.Quote("G.A.M.M.A") + " run -e " + Program.Quote("Anomaly (DX11)") : "") { WorkingDirectory = gamma, UseShellExecute = true });
            if (run) { status.Text = "Starting GAMMA…"; detail.Text = "Mod Organizer mounts the mods and starts the game."; }
        }
        catch (Exception error) { MessageBox.Show(error.Message, "Launch failed"); }
    }
}
