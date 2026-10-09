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

static class SelfTests
{
    internal static int Run(string folder)
    {
        string reportFolder = Path.GetFullPath(folder); Directory.CreateDirectory(reportFolder);
        folder = Path.Combine(reportFolder, "run-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
        Program.Home = Path.Combine(folder, "state"); Directory.CreateDirectory(Program.Home);
        var checks = new List<string>();
        Action<bool, string> check = (ok, title) => { if (!ok) throw new Exception("FAILED: " + title); checks.Add("PASS: " + title); };
        check(Safety.NormalizeRoot("d:/") == "D:\\", "drive root normalization");
        check(DriveInfo.GetDrives().Any(d => d.IsReady && d.DriveType == DriveType.Fixed && Safety.IsInternal(d.Name)), "live internal drive identification");
        foreach (string input in new[] { "D:\\Games", "\\\\server\\share", "C:\\Users", "D:\\..", "bad" })
        {
            bool refused = false; try { Safety.NormalizeRoot(input); } catch { refused = true; } check(refused, "reject unsafe root: " + input);
        }
        string occupied = Path.Combine(folder, "occupied"); Directory.CreateDirectory(occupied); File.WriteAllText(Path.Combine(occupied, "keep.txt"), "keep");
        bool rejected = false; try { Safety.CheckFolder(occupied); } catch { rejected = true; }
        check(rejected && File.ReadAllText(Path.Combine(occupied, "keep.txt")) == "keep", "unmanaged existing files are preserved");
        check(!Safety.Successful("Install finished\n", 1), "nonzero exit cannot enable Play");
        check(!Safety.Successful("Install failed! broken\n", 0), "failure cannot enable Play even with exit zero");
        check(!Safety.Successful("Application terminated unexpectedly\nInstall finished\n", 0), "fatal output cannot enable Play");
        check(!Safety.Successful("nothing\n", 0), "missing completion cannot enable Play");
        check(Safety.Successful("progress\nInstall finished\n", 0), "explicit completion accepted");
        check(!Program.RestartPending(new InstallState()), "no unnecessary restart requirement");
        check(Program.RestartPending(new InstallState { RestartRequiredSince = DateTime.UtcNow.ToString("o") }), "runtime restart requirement honoured");
        var state = new InstallState { Root = "D:\\", Phase = "Failed", Error = "test" }; Program.SaveState(state);
        check(Program.LoadState().Error == "test", "state survives restart");
        state.Phase = "Ready"; Program.SaveState(state); check(Program.LoadState().Phase == "Ready", "atomic state replacement");
        using (var embedded = Assembly.GetExecutingAssembly().GetManifestResourceStream("backend.zip")) check(Program.Hash(embedded) == Program.BackendHash, "bundled backend SHA256");
        Program.ExtractBackend(Path.Combine(Program.Home, "backend"));
        check(File.Exists(Program.Backend), "backend extraction");
        using (var p = new Process())
        using (var job = new ProcessJob())
        {
            p.StartInfo = new ProcessStartInfo(Program.Backend, "full-install --help") { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            p.Start(); job.Attach(p); string output = p.StandardOutput.ReadToEnd(); p.WaitForExit();
            check(p.ExitCode == 0 && output.Contains("--preserve-user-settings"), "real backend starts and exposes required commands");
        }
        // Integration check: closing a job must stop both the worker and its child.
        using (var parent = new Process())
        {
            string shell = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");
            string script = "Start-Sleep -Milliseconds 500; $probe=Start-Process -FilePath '" + shell.Replace("'", "''") + "' -ArgumentList '-NoProfile -Command Start-Sleep -Seconds 30' -WindowStyle Hidden -PassThru; Write-Output $probe.Id; Start-Sleep -Seconds 30";
            parent.StartInfo = new ProcessStartInfo(shell, "-NoProfile -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script))) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
            using (var job = new ProcessJob())
            {
                parent.Start(); job.Attach(parent);
                int pid = int.Parse(parent.StandardOutput.ReadLine());
                using (var child = Process.GetProcessById(pid))
                {
                    job.Dispose(); check(parent.WaitForExit(5000) && child.WaitForExit(5000), "cancellation stops backend child processes");
                }
            }
        }
        string anomaly = Path.Combine(folder, "Anomaly"), gamma = Path.Combine(folder, "GAMMA"); Directory.CreateDirectory(anomaly); Directory.CreateDirectory(gamma);
        foreach (var file in new[] { "AnomalyLauncher.exe", "bin\\AnomalyDX11.exe", "fsgame.ltx", "appdata\\user.ltx" }) { Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(anomaly, file))); File.WriteAllText(Path.Combine(anomaly, file), "fixture"); }
        string profile = Path.Combine(gamma, "profiles", "G.A.M.M.A"); Directory.CreateDirectory(profile);
        File.WriteAllText(Path.Combine(gamma, "ModOrganizer.exe"), "fixture");
        File.WriteAllText(Path.Combine(gamma, "ModOrganizer.ini"), "selected_profile=@ByteArray(G.A.M.M.A)\ngamePath=@ByteArray(" + anomaly.Replace('\\', '/') + ")\n3\\title=Anomaly (DX11)");
        File.WriteAllText(Path.Combine(profile, "modlist.txt"), "+TestMod\n"); File.WriteAllText(Path.Combine(profile, "modpack_maker_list.txt"), "fixture");
        rejected = false; try { Safety.VerifyGame(anomaly, gamma); } catch { rejected = true; } check(rejected, "missing enabled mod blocks Play");
        string mod = Path.Combine(gamma, "mods", "TestMod"); Directory.CreateDirectory(mod); File.WriteAllText(Path.Combine(mod, "meta.ini"), "fixture");
        rejected = false; try { Safety.VerifyGame(anomaly, gamma); } catch { rejected = true; } check(rejected, "metadata-only mod blocks Play");
        File.WriteAllText(Path.Combine(mod, "test.ltx"), "fixture"); check(Safety.VerifyGame(anomaly, gamma) == 1, "complete fixture verifies");
        string iniPath = Path.Combine(gamma, "ModOrganizer.ini");
        string originalIni = File.ReadAllText(iniPath);
        File.WriteAllText(iniPath, originalIni.Replace(anomaly.Replace('\\', '/'), anomaly.Replace(@"\", @"\\")));
        check(Safety.VerifyGame(anomaly, gamma) == 1, "MO2 escaped Windows path verifies");
        check(!Safety.SameGamePath("@ByteArray(D:\\\\Other)", "D:\\Anomaly"), "different game path still refused");
        string canonical = Path.Combine(gamma, "mods", InstallationRecovery.CorrectName); Directory.CreateDirectory(canonical); File.WriteAllText(Path.Combine(canonical, "payload.ltx"), "fixture");
        File.WriteAllLines(Path.Combine(profile, "modlist.txt"), new[] { "-" + InstallationRecovery.CorrectName, "+" + InstallationRecovery.BrokenName, "+TestMod" });
        check(InstallationRecovery.RepairKnownAlias(gamma), "known upstream alias is repaired");
        var repaired = File.ReadAllLines(Path.Combine(profile, "modlist.txt"));
        check(repaired.Length == 2 && repaired[0] == "+" + InstallationRecovery.CorrectName, "alias preserves enabled entry priority without duplicate");
        check(Directory.GetFiles(profile, "*.bak").Length == 1, "profile backed up before repair");
        check(!InstallationRecovery.RepairKnownAlias(gamma), "alias repair is idempotent");
        File.WriteAllLines(Path.Combine(profile, "modlist.txt"), new[] { "-" + InstallationRecovery.CorrectName, "+TestMod" });
        check(!InstallationRecovery.RepairKnownAlias(gamma) && File.ReadAllLines(Path.Combine(profile, "modlist.txt"))[0].StartsWith("-"), "intentional disabled canonical mod is preserved");
        string cache = Path.Combine(gamma, "downloads"); Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "sample.zip"), "abc", new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(profile, "modpack_maker_list.txt"), "url\t0\tpatch\tname\tpage\tsample.zip\t900150983cd24fb0d6963f7d28e17f72\n");
        var audit = CacheAudit.Run(gamma, CancellationToken.None, null);
        check(audit.Checked == 1 && audit.Corrupt.Count == 0, "optional cached MD5 audit accepts correct bytes");
        File.WriteAllText(Path.Combine(cache, "sample.zip"), "changed");
        check(CacheAudit.Run(gamma, CancellationToken.None, null).Corrupt.Count == 1, "cached MD5 mismatch detected without deleting files");
        var cancelled = new CancellationTokenSource(); cancelled.Cancel(); rejected = false;
        try { CacheAudit.Run(gamma, cancelled.Token, null); } catch (OperationCanceledException) { rejected = true; }
        check(rejected, "MD5 audit can be cancelled");
        File.WriteAllText(Path.Combine(profile, "modlist.txt"), "+UnknownMod\n"); rejected = false; try { Safety.VerifyGame(anomaly, gamma); } catch { rejected = true; } check(rejected, "unknown missing mod is still refused");
        File.WriteAllText(Path.Combine(profile, "modlist.txt"), "+..\n"); rejected = false; try { Safety.VerifyGame(anomaly, gamma); } catch { rejected = true; } check(rejected, "mod path traversal refused");
        CoopTests.Run(Path.Combine(folder,"coop"),check);
        File.WriteAllLines(Path.Combine(reportFolder, "test-results.txt"), checks); return 0;
    }
}
