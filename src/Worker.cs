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

static class Worker
{
    static string cancel;
    static CancellationTokenSource stopSource;
    static InstallState activeState;
    static void CheckCancel() { if (File.Exists(cancel)) throw new OperationCanceledException("Stopped. Downloaded archives are kept; click Retry installation to continue."); }
    internal static int Run(string request)
    {
        if (!Regex.IsMatch(request, "^[a-f0-9]{32}$") || !Program.IsAdmin()) return 1;
        bool acquired;
        using (var mutex = new Mutex(true, "Global\\GammaEasyInstall", out acquired))
        {
            if (!acquired) return 1;
            var state = Program.LoadState();
            activeState = state;
            cancel = Path.Combine(Program.Home, "cancel-" + request);
            stopSource = new CancellationTokenSource();
            using (var cancelTimer = new System.Threading.Timer(s => { if (File.Exists(cancel)) stopSource.Cancel(); }, null, 0, 250))
            {
            try
            {
                File.WriteAllText(Program.LogFile, "GAMMA Easy 0.2.0 | FaithBeam backend release 1.37.1\r\n");
                Program.Log("Checking drive and existing folders…");
                Safety.ValidateDrive(state.Root, true);
                Program.ExtractBackend(Path.Combine(Program.Home, "backend"));
                var folders = Safety.GameFolders(state.Root);
                var coop = CoopManager.ForRoot(state.Root);
                coop.BeginMaintenance();
                foreach (string folder in folders) { Directory.CreateDirectory(folder); File.WriteAllText(Path.Combine(folder, Program.Marker), Program.Owner); }
                string cache = Path.Combine(folders[1], "downloads"); Directory.CreateDirectory(cache); Safety.RejectReparseParents(cache);
                // Use GAMMA/downloads itself as cache: the backend therefore does not create a symlink.
                CheckCancel();
                if (state.Runtimes) InstallRuntimes();
                RunCommand(Program.Backend, "config create --name gamma-easy --anomaly " + Program.Quote(folders[0]) + " --gamma " + Program.Quote(folders[1]) + " --cache " + Program.Quote(cache) + " --download-threads 2", "-> gamma-easy");
                string command = "full-install --enable-long-paths --preserve-user-settings --preserve-mcm-settings";
                if (state.Exclusions) command += " --add-folders-to-win-defender-exclusion";
                string output = RunCommand(Program.Backend, command, "Install finished");
                if (!Safety.Successful(output, 0)) throw new IOException("The installer did not confirm completion. Read the log and retry.");
                CheckCancel();
                Program.Log("Checking DX11, first-run defaults, Mod Organizer and enabled mods…");
                if (InstallationRecovery.RepairKnownAlias(folders[1])) Program.Log("Corrected a known upstream GAMMA profile naming mismatch.");
                int count = Safety.VerifyGame(folders[0], folders[1]);
                coop.FinishMaintenance();
                Program.Log("Verified " + count + " enabled mods. Ready to play.");
                state.Phase = "Ready"; state.Error = ""; Program.SaveState(state); return 0;
            }
            catch (Exception error)
            {
                Program.Log("INSTALLATION STOPPED: " + error.Message);
                state.Phase = "Failed"; state.Error = error.Message; Program.SaveState(state); return 1;
            }
            finally { if (File.Exists(cancel)) File.Delete(cancel); }
            }
        }
    }
    internal static string RunCommand(string executable, string args, string completion)
    {
        CheckCancel(); Program.Log("Running " + Path.GetFileName(executable) + " " + args);
        var output = new StringBuilder(); var gate = new object();
        using (var job = new ProcessJob())
        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo(executable, args) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = Path.GetDirectoryName(executable), StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
            DataReceivedEventHandler receive = (s, e) => { if (e.Data == null) return; string line = Regex.Replace(e.Data, @"\x1B\[[0-?]*[ -/]*[@-~]", ""); lock (gate) output.AppendLine(line); Program.Log(line); };
            process.OutputDataReceived += receive; process.ErrorDataReceived += receive;
            process.Start();
            try { job.Attach(process); }
            catch { process.Kill(); throw; }
            process.BeginOutputReadLine(); process.BeginErrorReadLine();
            while (!process.WaitForExit(250)) CheckCancel();
            process.WaitForExit();
            string result = output.ToString();
            if (process.ExitCode != 0) throw new IOException(Path.GetFileName(executable) + " exited with code " + process.ExitCode + ". See the log for details.");
            if (completion != null && !result.Split('\n').Any(x => x.Trim() == completion))
                throw new IOException("The installer exited without confirming success. See the log for details.");
            return result;
        }
    }
    static void InstallRuntimes()
    {
        string dir = Path.Combine(Program.Home, "runtimes"); Directory.CreateDirectory(dir);
        string vc = Path.Combine(dir, "vc_redist.x64.exe");
        string dx = Path.Combine(dir, "directx_Jun2010_redist.exe");
        Program.Log("Downloading Microsoft game components…");
        Download("https://aka.ms/vc14/vc_redist.x64.exe", vc);
        VerifyMicrosoftDownload("https://aka.ms/vc14/vc_redist.x64.exe", vc);
        // VC++ returns 1638 when a newer version is already installed, and 3010 for reboot required.
        RunInstaller(vc, "/install /quiet /norestart", new[] { 0, 1638, 3010 });
        Download("https://download.microsoft.com/download/8/4/a/84a35bf1-dafe-4ae8-82af-ad2ae20b6b14/directx_Jun2010_redist.exe", dx);
        VerifyMicrosoftDownload("https://download.microsoft.com/download/8/4/a/84a35bf1-dafe-4ae8-82af-ad2ae20b6b14/directx_Jun2010_redist.exe", dx);
        string expanded = Path.Combine(dir, "DirectX"); Directory.CreateDirectory(expanded);
        RunCommand(Path.Combine(Program.Home, "backend", "resources", "7zz.exe"), "x " + Program.Quote(dx) + " -o" + Program.Quote(expanded) + " -y", null);
        string setup = Path.Combine(expanded, "DXSETUP.exe"); VerifyMicrosoftSignature(setup);
        RunInstaller(setup, "/silent", new[] { 0, 3010 });
    }
    static void VerifyMicrosoftSignature(string file)
    {
        // Verify Authenticode (including its timestamp) through Windows before executing downloaded code.
        string literal = "'" + file.Replace("'", "''") + "'";
        string script = "$s=Get-AuthenticodeSignature -LiteralPath " + literal + "; if ($s.Status -ne 'Valid' -or $s.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { Write-Error 'Microsoft signature verification failed'; exit 1 }";
        string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        RunCommand(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"), "-NoProfile -NonInteractive -EncodedCommand " + encoded, null);
    }
    static void VerifyMicrosoftDownload(string url, string file)
    {
        try { VerifyMicrosoftSignature(file); }
        catch (OperationCanceledException) { throw; }
        catch
        {
            Program.Log("Cached Microsoft download could not be verified. Downloading a fresh copy…");
            File.Delete(file); Download(url, file); VerifyMicrosoftSignature(file);
        }
    }
    static void Download(string url, string file)
    {
        CheckCancel();
        if (File.Exists(file)) return;
        string partial = file + ".partial";
        Exception last = null;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(10);
                    using (var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, stopSource.Token).GetAwaiter().GetResult())
                    {
                        response.EnsureSuccessStatusCode();
                        using (var input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        using (var output = File.Create(partial))
                        {
                            var buffer = new byte[131072]; int length;
                            while ((length = input.ReadAsync(buffer, 0, buffer.Length, stopSource.Token).GetAwaiter().GetResult()) > 0) { CheckCancel(); output.Write(buffer, 0, length); }
                        }
                    }
                }
                File.Move(partial, file); Program.Log("Downloaded " + Path.GetFileName(file)); return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception error) { last = error; Program.Log("Download attempt " + attempt + " failed: " + error.Message); if (attempt < 3) Thread.Sleep(1000); }
        }
        throw new IOException("Could not download " + Path.GetFileName(file), last);
    }
    static void RunInstaller(string exe, string arguments, int[] allowed)
    {
        // Do not kill an MSI/DirectX installation midway. Stop is honoured immediately afterwards.
        CheckCancel(); Program.Log("Installing " + Path.GetFileName(exe));
        using (var p = Process.Start(new ProcessStartInfo(exe, arguments) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(exe) }))
        {
            p.WaitForExit();
            if (!allowed.Contains(p.ExitCode)) throw new IOException("Microsoft installer returned " + p.ExitCode + ".");
            if (p.ExitCode == 3010) { Program.Log("A Windows restart is required before playing."); activeState.RestartRequiredSince = DateTime.UtcNow.ToString("o"); }
        }
        CheckCancel();
    }
}

// The job object closes all backend child processes on cancellation or an app crash.
