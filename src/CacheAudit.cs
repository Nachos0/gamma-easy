// Copyright (C) 2026. GPL-3.0-or-later.
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

class CacheAuditResult
{
    internal int Checked, Unavailable, Unsupported;
    internal readonly List<string> Corrupt = new List<string>();
    internal string Summary { get { return "MD5 cache check: " + Checked + " checked, " + Corrupt.Count + " mismatched, " + Unavailable + " unavailable, " + Unsupported + " without usable published MD5."; } }
}

static class CacheAudit
{
    internal static CacheAuditResult Run(string gamma, CancellationToken cancel, Action<string> progress)
    {
        var result = new CacheAuditResult();
        var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string manifest = Path.Combine(gamma, "profiles", "G.A.M.M.A", "modpack_maker_list.txt");
        foreach (string row in File.ReadLines(manifest))
        {
            cancel.ThrowIfCancellationRequested();
            var fields = row.Split('\t');
            if (fields.Length < 7) continue;
            string name = fields[5].Trim(), md5 = fields[6].Trim().ToLowerInvariant();
            if (name.Length == 0 || name != Path.GetFileName(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !Regex.IsMatch(md5, "^[a-f0-9]{32}$")) { result.Unsupported++; continue; }
            string previous;
            if (expected.TryGetValue(name, out previous) && previous != md5) throw new InvalidDataException("Conflicting MD5 values in the saved manifest: " + name);
            expected[name] = md5;
        }
        // Base archive has a published digest in the pinned backend source.
        expected["Anomaly-1.5.3-Full.2.7z"] = "d6bce51a4e6d98f9610ef0aa967ba964";
        foreach (var entry in expected)
        {
            cancel.ThrowIfCancellationRequested();
            string path = Path.Combine(gamma, "downloads", entry.Key);
            if (!File.Exists(path)) { result.Unavailable++; continue; }
            Safety.RejectReparseParents(path);
            if (progress != null) progress("Checking MD5: " + entry.Key);
            string actual = HashFile(path, cancel);
            result.Checked++;
            if (actual != entry.Value) { result.Corrupt.Add(entry.Key); if (progress != null) progress("MD5 mismatch: " + entry.Key); }
        }
        return result;
    }
    internal static string HashFile(string path, CancellationToken cancel)
    {
        using (var md5 = MD5.Create())
        using (var file = File.OpenRead(path))
        {
            var buffer = new byte[131072]; int count;
            while ((count = file.Read(buffer, 0, buffer.Length)) > 0) { cancel.ThrowIfCancellationRequested(); md5.TransformBlock(buffer, 0, count, buffer, 0); }
            cancel.ThrowIfCancellationRequested(); md5.TransformFinalBlock(new byte[0], 0, 0);
            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
