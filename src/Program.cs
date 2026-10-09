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

[assembly: AssemblyTitle("GAMMA Easy")]
[assembly: AssemblyDescription("Independent Anomaly and GAMMA installer and launcher")]
[assembly: AssemblyVersion("0.1.0.0")]

static class Program
{
    internal const string BackendHash = "bfe9f5390604d5a962d5a156a18e48526a56b3a51d92d815042d649a0f2f7105";
    internal const string Marker = ".gamma-easy-owned";
    internal const string Owner = "GAMMA Easy 0.1 - managed game folder";
    internal static string Home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GammaEasy");
    internal static string StateFile { get { return Path.Combine(Home, "install.xml"); } }
    internal static string LogFile { get { return Path.Combine(Home, "installation.log"); } }
    internal static readonly object LogLock = new object();
    internal static string Backend { get { return Path.Combine(Home, "backend", "stalker-gamma.exe"); } }

    [STAThread]
    static int Main(string[] args)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        try
        {
            if (args.Length == 2 && args[0] == "--self-test")
            {
                try { return SelfTests.Run(args[1]); }
                catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "test-failure.txt"), error.ToString()); return 1; }
            }
            if (args.Length == 2 && args[0] == "--render")
            {
                Home = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1])), "render-state");
                using (var form = new MainForm(null, true))
                using (var bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.Show(); form.WindowState = FormWindowState.Normal;
                    foreach (Control child in form.Controls) { child.Visible = true; child.CreateControl(); }
                    form.PerformLayout(); Application.DoEvents();
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(args[1]); form.Close();
                }
                return 0;
            }
            if (args.Length == 2 && args[0] == "--worker")
            {
                // UAC may run under a different administrator account; keep the request and logs
                // beside this installed launcher rather than using that account's profile folder.
                Home = Path.GetDirectoryName(Application.ExecutablePath);
                return Worker.Run(args[1]);
            }
            if (Path.GetFileName(Application.ExecutablePath).Equals("GammaSetup.exe", StringComparison.OrdinalIgnoreCase))
            {
                Application.Run(new SetupForm()); return 0;
            }
            bool acquired;
            using (var mutex = new Mutex(true, "Local\\GammaEasyUI-" + WindowsIdentity.GetCurrent().User.Value, out acquired))
            {
                if (!acquired) { MessageBox.Show("GAMMA Easy is already open.", "GAMMA Easy"); return 0; }
                Directory.CreateDirectory(Home);
                Application.Run(new MainForm(LoadState(), false));
            }
            return 0;
        }
        catch (Exception e) { MessageBox.Show(e.Message, "GAMMA Easy", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1; }
    }

    internal static string Hash(Stream stream)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }
    internal static void SaveState(InstallState state)
    {
        Directory.CreateDirectory(Home);
        var temporary = StateFile + ".tmp";
        using (var fs = File.Create(temporary)) new XmlSerializer(typeof(InstallState)).Serialize(fs, state);
        if (File.Exists(StateFile)) File.Replace(temporary, StateFile, null);
        else File.Move(temporary, StateFile);
    }
    internal static InstallState LoadState()
    {
        if (!File.Exists(StateFile)) return new InstallState();
        using (var fs = File.OpenRead(StateFile)) return (InstallState)new XmlSerializer(typeof(InstallState)).Deserialize(fs);
    }
    internal static void Log(string value)
    {
        value = Regex.Replace(value ?? "", @"\x1B\[[0-?]*[ -/]*[@-~]", "");
        lock (LogLock) File.AppendAllText(LogFile, value + Environment.NewLine, Encoding.UTF8);
    }
    internal static void ExtractBackend(string destination)
    {
        using (var embedded = Assembly.GetExecutingAssembly().GetManifestResourceStream("backend.zip"))
        {
            if (embedded == null || Hash(embedded) != BackendHash) throw new InvalidDataException("The bundled installer failed its SHA256 check.");
            embedded.Position = 0;
            using (var zip = new ZipArchive(embedded, ZipArchiveMode.Read))
            {
                Directory.CreateDirectory(destination);
                foreach (var entry in zip.Entries)
                {
                    var path = Path.GetFullPath(Path.Combine(destination, entry.FullName));
                    if (!path.StartsWith(Path.GetFullPath(destination).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Archive contains an unsafe path.");
                    if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(path); continue; }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    // Reject junctions/symlinks in an existing extraction location.
                    Safety.RejectReparseParents(path);
                    using (var input = entry.Open()) using (var output = File.Create(path)) input.CopyTo(output);
                }
            }
        }
    }
    internal static bool IsAdmin()
    {
        return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }
    [DllImport("kernel32.dll")] static extern ulong GetTickCount64();
    internal static bool RestartPending(InstallState state)
    {
        DateTime since;
        return DateTime.TryParse(state.RestartRequiredSince, null, System.Globalization.DateTimeStyles.RoundtripKind, out since) &&
            DateTime.UtcNow.AddMilliseconds(-(double)GetTickCount64()) <= since.ToUniversalTime();
    }
    internal static string Quote(string argument)
    {
        // Windows CommandLineToArgvW quoting, including trailing backslashes.
        var b = new StringBuilder("\""); int slashes = 0;
        foreach (char c in argument)
        {
            if (c == '\\') { slashes++; continue; }
            if (c == '"') b.Append('\\', slashes * 2 + 1).Append(c);
            else b.Append('\\', slashes).Append(c);
            slashes = 0;
        }
        return b.Append('\\', slashes * 2).Append('"').ToString();
    }
    internal static void Shortcut(string filename, string target)
    {
        dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        dynamic link = shell.CreateShortcut(filename);
        link.TargetPath = target; link.WorkingDirectory = Path.GetDirectoryName(target);
        link.Description = "Install and play STALKER GAMMA"; link.Save();
        Marshal.FinalReleaseComObject(link); Marshal.FinalReleaseComObject(shell);
    }
}
