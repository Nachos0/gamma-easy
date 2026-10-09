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

static class Safety
{
    internal static string NormalizeRoot(string root)
    {
        if (!Regex.IsMatch(root ?? "", @"^[A-Za-z]:[\\/]?$")) throw new InvalidOperationException("Choose a local drive, such as D:\\.");
        return root.Substring(0, 1).ToUpperInvariant() + ":\\";
    }
    internal static void RejectReparseParents(string path)
    {
        var current = Path.GetFullPath(path);
        while (!string.IsNullOrEmpty(current))
        {
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("A symbolic link or junction is not allowed here: " + current);
            current = Path.GetDirectoryName(current);
        }
    }
    internal static bool IsInternal(string root)
    {
        try
        {
            string logical = NormalizeRoot(root).Substring(0, 2);
            using (var partitions = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_LogicalDisk.DeviceID='" + logical + "'} WHERE AssocClass=Win32_LogicalDiskToPartition"))
            using (var items = partitions.Get())
            {
                int found = 0;
                foreach (ManagementObject partition in items)
                using (var disks = new ManagementObjectSearcher("ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partition["DeviceID"] + "'} WHERE AssocClass=Win32_DiskDriveToDiskPartition"))
                using (var devices = disks.Get())
                foreach (ManagementObject disk in devices)
                {
                    found++;
                    string type = Convert.ToString(disk["InterfaceType"]);
                    string media = Convert.ToString(disk["MediaType"]);
                    string model = Convert.ToString(disk["Model"]);
                    if (type.Equals("USB", StringComparison.OrdinalIgnoreCase) || media.IndexOf("removable", StringComparison.OrdinalIgnoreCase) >= 0 || model.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0) return false;
                }
                return found > 0;
            }
        }
        catch { return false; }
    }
    internal static string[] GameFolders(string root)
    {
        root = NormalizeRoot(root);
        return new[] { Path.Combine(root, "Anomaly"), Path.Combine(root, "GAMMA") };
    }
    internal static void CheckFolder(string folder)
    {
        RejectReparseParents(folder);
        if (File.Exists(folder)) throw new IOException("A file already exists at " + folder);
        if (!Directory.Exists(folder) || !Directory.EnumerateFileSystemEntries(folder).Any()) return;
        var marker = Path.Combine(folder, Program.Marker);
        if (!File.Exists(marker) || File.ReadAllText(marker) != Program.Owner)
            throw new IOException(folder + " already contains files. Choose another drive or move that existing installation yourself. GAMMA Easy will not overwrite it.");
    }
    internal static void ValidateDrive(string root, bool checkSpace)
    {
        root = NormalizeRoot(root);
        var drive = new DriveInfo(root);
        if (!drive.IsReady || drive.DriveType != DriveType.Fixed || !IsInternal(root))
            throw new IOException("Choose an internal hard drive or SSD. USB, network and removable drives are unsupported.");
        if (!drive.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase)) throw new IOException("Choose an NTFS drive.");
        foreach (var folder in GameFolders(root)) CheckFolder(folder);
        if (checkSpace)
        {
            long alreadyUsed = GameFolders(root).Sum(x => Directory.Exists(x) ? SizeWithoutLinks(x) : 0);
            long required = Math.Max(20000000000L, 200000000000L - alreadyUsed);
            if (drive.AvailableFreeSpace < required)
                throw new IOException("This installation needs about " + (required / 1000000000L) + " GB free on " + root + ". Currently available: " + (drive.AvailableFreeSpace / 1000000000L) + " GB.");
        }
    }
    static long SizeWithoutLinks(string folder)
    {
        long bytes = 0;
        foreach (var entry in new DirectoryInfo(folder).EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue;
            if (entry is FileInfo) bytes += ((FileInfo)entry).Length;
            else bytes += SizeWithoutLinks(entry.FullName);
        }
        return bytes;
    }
    internal static int VerifyGame(string anomaly, string gamma)
    {
        foreach (string file in new[] { "AnomalyLauncher.exe", "bin\\AnomalyDX11.exe", "fsgame.ltx", "appdata\\user.ltx" })
            if (!File.Exists(Path.Combine(anomaly, file))) throw new IOException("Missing Anomaly file: " + file);
        foreach (string file in new[] { "ModOrganizer.exe", "ModOrganizer.ini", "profiles\\G.A.M.M.A\\modlist.txt", "profiles\\G.A.M.M.A\\modpack_maker_list.txt" })
            if (!File.Exists(Path.Combine(gamma, file))) throw new IOException("Missing GAMMA file: " + file);
        string ini = File.ReadAllText(Path.Combine(gamma, "ModOrganizer.ini"));
        if (!ini.Contains("selected_profile=@ByteArray(G.A.M.M.A)")) throw new IOException("The GAMMA profile is not configured.");
        var gamePath = Regex.Match(ini, @"(?m)^gamePath=(.*)$");
        if (!gamePath.Success || !SameGamePath(gamePath.Groups[1].Value.Trim(), anomaly)) throw new IOException("Mod Organizer points to the wrong Anomaly folder.");
        if (!ini.Contains("title=Anomaly (DX11)")) throw new IOException("The DX11 launch entry is missing.");
        int count = 0;
        foreach (string raw in File.ReadLines(Path.Combine(gamma, "profiles", "G.A.M.M.A", "modlist.txt")))
        {
            string line = raw.Trim();
            if (!line.StartsWith("+")) continue;
            string name = line.Substring(1);
            if (name.EndsWith("_separator", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Length == 0 || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new IOException("Invalid mod name in the profile.");
            string dir = Path.Combine(gamma, "mods", name);
            if (!Directory.Exists(dir) || !Directory.EnumerateFileSystemEntries(dir).Any(x => !Path.GetFileName(x).Equals("meta.ini", StringComparison.OrdinalIgnoreCase)))
                throw new IOException("Missing or empty enabled mod: " + name);
            count++;
        }
        if (count == 0) throw new IOException("No enabled mods were found.");
        return count;
    }
    internal static bool SameGamePath(string encoded, string expected)
    {
        if (encoded.StartsWith("@ByteArray(") && encoded.EndsWith(")")) encoded = encoded.Substring(11, encoded.Length - 12);
        encoded = encoded.Replace(@"\\", @"\").Replace('/', '\\');
        try { return Path.IsPathRooted(encoded) && Path.GetFullPath(encoded).TrimEnd('\\').Equals(Path.GetFullPath(expected).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }
    internal static bool Successful(string output, int exitCode)
    {
        return exitCode == 0 && output.Split('\n').Any(x => x.Trim().Equals("Install finished", StringComparison.Ordinal)) &&
            output.IndexOf("Install failed!", StringComparison.OrdinalIgnoreCase) < 0 &&
            output.IndexOf("Application terminated unexpectedly", StringComparison.OrdinalIgnoreCase) < 0;
    }
}
