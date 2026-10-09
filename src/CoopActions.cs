// Copyright (C) 2026. GPL-3.0-or-later.
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;

public class CoopRequest
{
    public string Root, Action, Archive;
}
static class CoopActions
{
    internal static string RequestFile(string id) { return System.IO.Path.Combine(Program.Home, "coop-request-" + id + ".xml"); }
    internal static void Save(CoopRequest request, string id)
    {
        using (var file = File.Create(RequestFile(id))) new XmlSerializer(typeof(CoopRequest)).Serialize(file, request);
    }
    internal static int Run(string id)
    {
        if (!Regex.IsMatch(id, "^[a-f0-9]{32}$") || !Program.IsAdmin()) return 1;
        string result = RequestFile(id) + ".result";
        try
        {
            CoopRequest request;
            using (var file = File.OpenRead(RequestFile(id))) request = (CoopRequest)new XmlSerializer(typeof(CoopRequest)).Deserialize(file);
            var install = Program.LoadState();
            if (install.Root != Safety.NormalizeRoot(request.Root)) throw new IOException("Co-op request does not match this installation.");
            Safety.ValidateDrive(request.Root, false);
            var manager = CoopManager.ForRoot(request.Root);
            if (request.Action == "recover") manager.Recover();
            else if (request.Action == "install") { if (install.Phase != "Ready") throw new IOException("Finish the base game installation first."); if(string.IsNullOrEmpty(request.Archive)) request.Archive=CoopDownload.Fetch().GetAwaiter().GetResult(); manager.Install(request.Archive, CancellationToken.None); }
            else if (request.Action == "coop") manager.Switch(true);
            else if (request.Action == "single") manager.Switch(false);
            else throw new IOException("Unknown co-op action.");
            File.WriteAllText(result, "OK"); Program.Log("Co-op action finished: " + request.Action); return 0;
        }
        catch (Exception error) { File.WriteAllText(result, error.Message); Program.Log("Co-op action needs attention: " + error.Message); return 1; }
    }
}
