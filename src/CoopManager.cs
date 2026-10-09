// Copyright (C) 2026. GPL-3.0-or-later. Original implementation.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;

public class CoopFile
{
    public string Key;
    public string Original;
    public string Payload;
}
public class CoopState
{
    public string Anomaly, Gamma, Transaction;
    public bool Installed, Active, Maintenance;
    public List<CoopFile> Files = new List<CoopFile>();
}
public class CoopChange
{
    public string Key, Before, After;
}
public class CoopJournal
{
    public string Id;
    public List<CoopChange> Changes = new List<CoopChange>();
}

class CoopManager
{
    internal const string Profile = "G.A.M.M.A. Co-op";
    internal const string Mod = "GAMMA Easy xrRazom 1.4";
    internal const string PublisherMd5 = "3e33cc23314123b8a8cbf79457fb2403";
    internal const string FullMd5 = "ede2ce10bd15962030f696bc76ba7f29";
    internal const string FullSha256 = "8bd015490d20af95e4a2d28eb78a79e365a0b19795f29be6a77a902f05b7c9a7";
    internal const string DownloadPage = "https://www.moddb.com/mods/stalker-anomaly/addons/xrrazom-stalker-anomaly-co-op-slim";
    readonly string anomaly, gamma, home;
    internal Action<int> TestFault;
    internal CoopManager(string anomaly, string gamma, string home)
    {
        this.anomaly = Path.GetFullPath(anomaly); this.gamma = Path.GetFullPath(gamma); this.home = Path.GetFullPath(home);
        Safety.RejectReparseParents(this.home);
    }
    internal static CoopManager ForRoot(string root)
    {
        var folders = Safety.GameFolders(root);
        return new CoopManager(folders[0], folders[1], Path.Combine(Program.Home, "coop", root.Substring(0, 1).ToUpperInvariant()));
    }
    internal bool Pending { get { return File.Exists(Path.Combine(home, "journal.xml")); } }
    internal CoopState State()
    {
        string file = Path.Combine(home, "state.xml");
        if (!File.Exists(file)) return new CoopState { Anomaly = anomaly, Gamma = gamma };
        Safety.RejectReparseParents(file);
        var state = ReadXml<CoopState>(file);
        if (!string.Equals(state.Anomaly, anomaly, StringComparison.OrdinalIgnoreCase) || !string.Equals(state.Gamma, gamma, StringComparison.OrdinalIgnoreCase)) throw new IOException("Co-op backups belong to another installation.");
        return state;
    }
    internal static void RequireClosed()
    {
        foreach (var process in Process.GetProcesses())
        using (process)
            if (process.ProcessName == "ModOrganizer" || process.ProcessName == "stalker-gamma" || process.ProcessName.StartsWith("AnomalyDX", StringComparison.OrdinalIgnoreCase))
                throw new IOException("Close Anomaly, Mod Organizer and the installer before changing co-op mode.");
    }
    FileStream Lock(bool recovery)
    {
        RequireClosed(); Directory.CreateDirectory(home); Safety.RejectReparseParents(home);
        var stream = new FileStream(Path.Combine(home, "operation.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try { State(); if (!recovery && Pending) throw new IOException("An interrupted co-op switch needs recovery. Open Co-op setup and choose Recover."); return stream; }
        catch { stream.Dispose(); throw; }
    }
    internal string Resolve(string key)
    {
        if (!(key.StartsWith("game/bin/") || key.StartsWith("game/gamedata/") || key.StartsWith("gamma/mods/" + Mod + "/") || key == "gamma/mods/" + Mod || key.StartsWith("gamma/profiles/" + Profile + "/") || key == "gamma/profiles/" + Profile || key == "gamma/ModOrganizer.ini" || key.StartsWith("control/"))) throw new IOException("File is outside the managed co-op payload: " + key);
        string prefix = key.StartsWith("game/") ? "game/" : key.StartsWith("gamma/") ? "gamma/" : key.StartsWith("control/") ? "control/" : null;
        if (prefix == null) throw new IOException("Invalid co-op file key.");
        string root = prefix == "game/" ? anomaly : prefix == "gamma/" ? gamma : home;
        string relative = key.Substring(prefix.Length).Replace('/', '\\');
        if (Path.IsPathRooted(relative) || relative.Split('\\').Any(p => p.Length == 0 || p == "." || p == ".." || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)) throw new IOException("Unsafe co-op path: " + key);
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new IOException("Co-op path leaves its installation.");
        Safety.RejectReparseParents(path); return path;
    }
    internal static string FileHash(string path)
    {
        if (!File.Exists(path)) return null;
        using (var stream = File.OpenRead(path)) return Program.Hash(stream);
    }
    string Store(byte[] data)
    {
        using (var stream = new MemoryStream(data))
        {
            string hash = Program.Hash(stream), file = Resolve("control/objects/" + hash);
            if (FileHash(file) != hash) Put(file, data);
            return hash;
        }
    }
    byte[] Content(string hash)
    {
        if (hash == null) return null;
        if (!Regex.IsMatch(hash, "^[a-f0-9]{64}$")) throw new IOException("Invalid co-op backup hash.");
        string path = Resolve("control/objects/" + hash);
        if (FileHash(path) != hash) throw new IOException("Co-op backup is missing or damaged. No file changes were made.");
        return File.ReadAllBytes(path);
    }
    static void Put(string path, byte[] data)
    {
        Safety.RejectReparseParents(path);
        if (Directory.Exists(path)) throw new IOException("Expected a file: " + path);
        if (data == null) { if (File.Exists(path)) File.Delete(path); return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + ".gamma-easy-" + Guid.NewGuid().ToString("N");
        using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write)) { file.Write(data, 0, data.Length); file.Flush(true); }
        if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path);
    }
    static byte[] XmlBytes<T>(T value)
    {
        using (var stream = new MemoryStream()) { new XmlSerializer(typeof(T)).Serialize(stream, value); return stream.ToArray(); }
    }
    static T ReadXml<T>(string path)
    {
        using (var file = File.OpenRead(path))
        using (var reader = XmlReader.Create(file, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20000000 })) return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
    }
    void Transaction(Dictionary<string, string> desired, CoopState next)
    {
        var journal = new CoopJournal { Id = Guid.NewGuid().ToString("N") }; next.Transaction = journal.Id;
        foreach (var item in desired)
        {
            string path = Resolve(item.Key); Content(item.Value);
            journal.Changes.Add(new CoopChange { Key = item.Key, Before = File.Exists(path) ? Store(File.ReadAllBytes(path)) : null, After = item.Value });
        }
        string oldState = Resolve("control/state.xml");
        journal.Changes.Add(new CoopChange { Key = "control/state.xml", Before = File.Exists(oldState) ? Store(File.ReadAllBytes(oldState)) : null, After = Store(XmlBytes(next)) });
        Put(Resolve("control/journal.xml"), XmlBytes(journal));
        try
        {
            int i = 0;
            foreach (var change in journal.Changes) { Put(Resolve(change.Key), Content(change.After)); if (TestFault != null) TestFault(++i); }
            File.Delete(Resolve("control/journal.xml"));
        }
        catch { RecoverInternal(); throw; }
    }
    void RecoverInternal()
    {
        if (!Pending) return;
        var journal = ReadXml<CoopJournal>(Resolve("control/journal.xml"));
        bool committed = State().Transaction == journal.Id;
        foreach (var change in journal.Changes)
        {
            string current = FileHash(Resolve(change.Key));
            if (current != change.Before && current != change.After) throw new IOException("A co-op file changed outside the app. Backups were retained; recovery needs attention: " + change.Key);
            Content(committed ? change.After : change.Before);
            if (committed && current != change.After) throw new IOException("Committed co-op files changed. Recovery needs attention.");
        }
        if (!committed) foreach (var change in journal.Changes.AsEnumerable().Reverse()) Put(Resolve(change.Key), Content(change.Before));
        File.Delete(Resolve("control/journal.xml"));
    }
    internal void Recover() { using (Lock(true)) RecoverInternal(); }
    internal static string SetProfile(string ini, string profile)
    {
        if (!Regex.IsMatch(ini, @"(?m)^selected_profile=.*$")) throw new IOException("MO2 selected profile is missing.");
        return Regex.Replace(ini, @"(?m)^selected_profile=.*$", "selected_profile=@ByteArray(" + profile + ")");
    }
    internal void Install(string archive, CancellationToken cancel)
    {
        using (Lock(false))
        {
            var old = State();
            if (old.Installed) throw new IOException("Co-op is already set up. Use its mode switch or Recover.");
            Safety.VerifyGame(anomaly, gamma);
            using (var file = File.OpenRead(archive))
            {
                string md5;
                using (var hash = MD5.Create()) md5 = BitConverter.ToString(hash.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
                if (md5 != PublisherMd5 && md5 != FullMd5) throw new IOException("Choose an official xrRazom 1.4 Slim or Full Bundle ZIP. Its published MD5 does not match.");
                if(md5==FullMd5) { file.Position=0; if(Program.Hash(file)!=FullSha256) throw new IOException("Full Bundle SHA256 does not match the verified package."); }
                file.Position = 0; cancel.ThrowIfCancellationRequested();
                using (var zip = new ZipArchive(file, ZipArchiveMode.Read)) InstallEntries(zip, cancel);
            }
        }
    }
    // Called by isolated fixture tests after a synthetic ZIP is constructed.
    internal void InstallEntries(ZipArchive zip, CancellationToken cancel)
    {
        var next = State();
        if (next.Installed || Directory.Exists(Resolve("gamma/mods/" + Mod)) || Directory.Exists(Resolve("gamma/profiles/" + Profile))) throw new IOException("The target co-op profile or mod already exists. Manual installs are not adopted.");
        var changes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var payloadKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int bin = 0, engineData = 0, modData = 0;
        foreach (var entry in zip.Entries)
        {
            cancel.ThrowIfCancellationRequested();
            string name = entry.FullName.Replace('\\', '/');
            if (name.EndsWith("/")) continue;
            if (name.Split('/').Any(p => p == "." || p == ".." || p.Contains(":"))) throw new IOException("Unsafe path in co-op ZIP.");
            string key = null;
            if (name.StartsWith("Anomaly/bin/", StringComparison.OrdinalIgnoreCase)) { key = "game/bin/" + name.Substring(12); bin++; }
            else if (name.StartsWith("Anomaly/gamedata/", StringComparison.OrdinalIgnoreCase)) { key = "game/gamedata/" + name.Substring(17); engineData++; }
            else if (name.StartsWith("gamedata/", StringComparison.OrdinalIgnoreCase)) { key = "gamma/mods/" + Mod + "/gamedata/" + name.Substring(9); modData++; }
            if (key == null) continue;
            if (!payloadKeys.Add(key)) throw new IOException("Duplicate path in co-op package.");
            string target = Resolve(key);
            if (entry.Length > 134217728) throw new IOException("Unexpected oversized co-op payload file.");
            using (var source = entry.Open()) using (var memory = new MemoryStream())
            {
                source.CopyTo(memory); string payload = Store(memory.ToArray());
                string original = File.Exists(target) ? Store(File.ReadAllBytes(target)) : null;
                if (key.StartsWith("game/bin/") && key.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && original == payload) throw new IOException("Co-op engines appear to be installed already. A clean single-player backup is required.");
                next.Files.Add(new CoopFile { Key = key, Original = original, Payload = payload });
                if (key.StartsWith("gamma/")) changes[key] = payload;
            }
        }
        if (bin == 0 || engineData == 0 || modData == 0) throw new IOException("Slim archive layout does not match the supported recipe.");
        string baseProfile = Path.Combine(gamma, "profiles", "G.A.M.M.A");
        foreach (string source in Directory.GetFiles(baseProfile))
        {
            string name = Path.GetFileName(source);
            if (name.EndsWith(".bak") || name.StartsWith("modlist.txt.")) continue;
            changes["gamma/profiles/" + Profile + "/" + name] = Store(File.ReadAllBytes(source));
        }
        string modlist = File.ReadAllText(Path.Combine(baseProfile, "modlist.txt"));
        changes["gamma/profiles/" + Profile + "/modlist.txt"] = Store(Encoding.UTF8.GetBytes("+" + Mod + "\r\n" + modlist));
        next.Installed = true; next.Active = false;
        cancel.ThrowIfCancellationRequested(); Transaction(changes, next);
    }
    internal void Switch(bool active)
    {
        using (Lock(false))
        {
            var state = State();
            if (!state.Installed || state.Maintenance) throw new IOException("Co-op is not installed, or GAMMA maintenance needs to finish.");
            var changes = new Dictionary<string, string>();
            foreach (var item in state.Files.Where(x => x.Key.StartsWith("game/")))
            {
                string current = FileHash(Resolve(item.Key)), expected = state.Active ? item.Payload : item.Original;
                if (current != expected) throw new IOException("Engine changed outside GAMMA Easy. Restore or repair the active mode before switching: " + item.Key);
                changes[item.Key] = active ? item.Payload : item.Original;
            }
            if (active) VerifyMod(state);
            string iniPath = Resolve("gamma/ModOrganizer.ini");
            changes["gamma/ModOrganizer.ini"] = Store(Encoding.UTF8.GetBytes(SetProfile(File.ReadAllText(iniPath), active ? Profile : "G.A.M.M.A")));
            state.Active = active; Transaction(changes, state);
        }
    }
    void VerifyMod(CoopState state)
    {
        foreach (var item in state.Files.Where(x => x.Key.StartsWith("gamma/")))
            if (FileHash(Resolve(item.Key)) != item.Payload) throw new IOException("Managed co-op mod changed or is missing: " + item.Key);
        var list = File.ReadAllLines(Resolve("gamma/profiles/" + Profile + "/modlist.txt")).Where(x => x.Length > 0 && !x.StartsWith("#")).ToArray();
        if (list.Length == 0 || list[0] != "+" + Mod) throw new IOException("Co-op must be enabled at highest priority in its profile.");
    }
    internal string VerifyMode()
    {
        if (Pending) throw new IOException("Recover the interrupted co-op operation before playing.");
        var state = State();
        if (!state.Installed) return "G.A.M.M.A";
        if (state.Maintenance) throw new IOException("GAMMA maintenance is incomplete. Retry installation before playing.");
        foreach (var item in state.Files.Where(x => x.Key.StartsWith("game/")))
            if (FileHash(Resolve(item.Key)) != (state.Active ? item.Payload : item.Original)) throw new IOException("Active engine files do not match the saved mode. Open Co-op setup for recovery.");
        if (state.Active) VerifyMod(state);
        return state.Active ? Profile : "G.A.M.M.A";
    }
    internal void BeginMaintenance()
    {
        if (!State().Installed) return;
        if (State().Active) Switch(false);
        using (Lock(false)) { var state = State(); state.Maintenance = true; Transaction(new Dictionary<string, string>(), state); }
    }
    internal void FinishMaintenance()
    {
        if (!State().Installed) return;
        using (Lock(false))
        {
            var state = State();
            foreach (var item in state.Files.Where(x => x.Key.StartsWith("game/"))) { string path = Resolve(item.Key); item.Original = File.Exists(path) ? Store(File.ReadAllBytes(path)) : null; }
            var changes = new Dictionary<string, string>();
            foreach (var item in state.Files.Where(x => x.Key.StartsWith("gamma/"))) changes[item.Key] = item.Payload;
            string source = Path.Combine(gamma,"profiles","G.A.M.M.A","modlist.txt"); Safety.RejectReparseParents(source);
            changes["gamma/profiles/" + Profile + "/modlist.txt"] = Store(Encoding.UTF8.GetBytes("+" + Mod + "\r\n" + File.ReadAllText(source)));
            state.Maintenance = false; state.Active = false; Transaction(changes, state);
        }
    }
}
