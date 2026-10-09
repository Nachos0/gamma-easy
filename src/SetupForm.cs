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

class SetupForm : Form
{
    Label status;
    Button install;
    internal SetupForm()
    {
        Text = "GAMMA Easy Setup"; ClientSize = new Size(560, 280); FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(20, 25, 26); ForeColor = Color.White; Font = new Font("Segoe UI", 10);
        Controls.Add(new Label { Text = "GAMMA EASY", Font = new Font("Segoe UI", 25, FontStyle.Bold), Bounds = new Rectangle(30, 25, 500, 50) });
        status = new Label { Text = "Install the launcher, choose a drive, then download GAMMA.\nWindows 10/11 x64 · about 200 GB free for the game\nIndependent community app · game files downloaded separately", Bounds = new Rectangle(32, 95, 500, 85) };
        Controls.Add(status);
        install = new Button { Text = "Install launcher", Bounds = new Rectangle(32, 200, 200, 44), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(143, 174, 86), ForeColor = Color.Black };
        Controls.Add(install); install.Click += Install;
    }
    async void Install(object sender, EventArgs e)
    {
        install.Enabled = false; status.Text = "Installing launcher and verifying the bundled installer…";
        try
        {
            string target = Path.Combine(Program.Home, "GammaEasy.exe");
            await Task.Run(() =>
            {
                if (!Environment.Is64BitOperatingSystem) throw new IOException("Windows x64 is required.");
                Directory.CreateDirectory(Program.Home); Safety.RejectReparseParents(target);
                File.Copy(Application.ExecutablePath, target, true);
                File.WriteAllText(target + ".config", "<?xml version=\"1.0\"?><configuration><startup><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.8\" /></startup><runtime><AppContextSwitchOverrides value=\"Switch.System.IO.UseLegacyPathHandling=false;Switch.System.IO.BlockLongPaths=false\" /></runtime></configuration>");
                Program.ExtractBackend(Path.Combine(Program.Home, "backend"));
                using (var license = Assembly.GetExecutingAssembly().GetManifestResourceStream("LICENSE"))
                using (var output = File.Create(Path.Combine(Program.Home, "LICENSE.txt"))) license.CopyTo(output);
                Program.Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "GAMMA Easy.lnk"), target);
            });
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); Close();
        }
        catch (Exception error) { status.Text = "Could not install. Close any running GAMMA Easy window and try again.\n" + error.Message; install.Enabled = true; }
    }
}
