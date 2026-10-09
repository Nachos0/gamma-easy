// Copyright (C) 2026. GPL-3.0-or-later. Read-only comparison, no automatic sharing.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

public class CoopFingerprintFile { public string Path, Hash; }
public class CoopFingerprintData
{
    public string Format = "GammaEasy-Coop-1", Provider = "xrRazom-1.4";
    public List<string> Mods = new List<string>();
    public List<CoopFingerprintFile> Files = new List<CoopFingerprintFile>();
}
static class CoopFingerprint
{
    static void AddLayer(string root, Dictionary<string,string> files)
    {
        foreach (string kind in new[] { "configs", "scripts" })
        {
            string folder = System.IO.Path.Combine(root,"gamedata",kind);
            if (!Directory.Exists(folder)) continue;
            Safety.RejectReparseParents(folder);
            foreach (string path in Walk(folder))
            {
                if (System.IO.Path.GetFileName(path).Equals("axr_options.ltx",StringComparison.OrdinalIgnoreCase)) continue;
                string relative = path.Substring(root.TrimEnd('\\').Length + 1).Replace('\\','/').ToLowerInvariant();
                files[relative] = CoopManager.FileHash(path);
            }
        }
    }
    static IEnumerable<string> Walk(string root)
    {
        foreach(string path in Directory.GetFileSystemEntries(root))
        {
            Safety.RejectReparseParents(path);
            if(Directory.Exists(path)) { foreach(string child in Walk(path)) yield return child; }
            else yield return path;
        }
    }
    internal static CoopFingerprintData Scan(string root)
    {
        var manager=CoopManager.ForRoot(root);
        if(manager.VerifyMode()!=CoopManager.Profile) throw new IOException("Activate co-op before scanning its effective files.");
        var folders=Safety.GameFolders(root); var data=new CoopFingerprintData();
        foreach(string row in File.ReadLines(System.IO.Path.Combine(folders[1],"profiles",CoopManager.Profile,"modlist.txt")))
        {
            string line=row.Trim(); if(line.StartsWith("+") && !line.EndsWith("_separator")) data.Mods.Add(line.Substring(1));
        }
        var files=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        AddLayer(folders[0],files);
        foreach(string mod in data.Mods.AsEnumerable().Reverse())
        {
            if(mod!=System.IO.Path.GetFileName(mod) || mod=="." || mod=="..") throw new IOException("Unsafe mod name.");
            AddLayer(System.IO.Path.Combine(folders[1],"mods",mod),files);
        }
        AddLayer(System.IO.Path.Combine(folders[1],"overwrite"),files);
        foreach(var entry in manager.State().Files.Where(x=>x.Key.StartsWith("game/bin/"))) files["engine/"+entry.Key.Substring(9)]=entry.Payload;
        data.Files=files.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new CoopFingerprintFile { Path=x.Key, Hash=x.Value }).ToList();
        return data;
    }
    internal static void Save(CoopFingerprintData data,string file) { using(var stream=File.Create(file)) new XmlSerializer(typeof(CoopFingerprintData)).Serialize(stream,data); }
    internal static CoopFingerprintData Read(string file)
    {
        using(var stream=File.OpenRead(file))
        using(var reader=XmlReader.Create(stream,new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null, MaxCharactersInDocument=20000000 }))
        {
            var data=(CoopFingerprintData)new XmlSerializer(typeof(CoopFingerprintData)).Deserialize(reader);
            if(data.Format!="GammaEasy-Coop-1" || data.Files.Count>100000) throw new IOException("Not a supported co-op manifest.");
            return data;
        }
    }
    internal static List<string> Compare(CoopFingerprintData mine,CoopFingerprintData friend)
    {
        var messages=new List<string>();
        if(mine.Provider!=friend.Provider) messages.Add("Co-op providers/versions differ.");
        if(!mine.Mods.SequenceEqual(friend.Mods)) messages.Add("Enabled mods or priority order differ.");
        var theirs=friend.Files.ToDictionary(x=>x.Path,x=>x.Hash,StringComparer.OrdinalIgnoreCase);
        var ours=mine.Files.ToDictionary(x=>x.Path,x=>x.Hash,StringComparer.OrdinalIgnoreCase);
        foreach(string path in ours.Keys.Union(theirs.Keys,StringComparer.OrdinalIgnoreCase))
        {
            string a,b; ours.TryGetValue(path,out a); theirs.TryGetValue(path,out b);
            if(a!=b) messages.Add("Different effective file: "+path);
        }
        return messages;
    }
}
