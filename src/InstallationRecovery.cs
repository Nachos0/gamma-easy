// Copyright (C) 2026. GPL-3.0-or-later.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

static class InstallationRecovery
{
    internal const string BrokenName = "DarkasleQif's and iTheon's Parts Condition Color Thresholds Changes";
    internal const string CorrectName = "Darkasleif's and iTheon's Parts Condition Color Thresholds Changes";

    internal static bool RepairKnownAlias(string gamma)
    {
        string profile = Path.Combine(gamma, "profiles", "G.A.M.M.A", "modlist.txt");
        string actual = Path.Combine(gamma, "mods", CorrectName);
        var lines = File.ReadAllLines(profile);
        if (!lines.Contains("+" + BrokenName) || !Directory.Exists(actual) ||
            !Directory.EnumerateFileSystemEntries(actual).Any(x => Path.GetFileName(x) != "meta.ini")) return false;
        if (Process.GetProcessesByName("ModOrganizer").Length > 0) throw new IOException("Close Mod Organizer before repairing the profile.");
        Safety.RejectReparseParents(profile);
        File.Copy(profile, profile + ".before-alias-" + Guid.NewGuid().ToString("N") + ".bak");
        // Retain the upstream entry's position and enabled state; remove a duplicate
        // canonical entry only when this exact upstream typo was present and enabled.
        var repaired = lines.Where(x => x != "+" + CorrectName && x != "-" + CorrectName)
            .Select(x => x == "+" + BrokenName ? "+" + CorrectName : x).ToArray();
        File.WriteAllLines(profile, repaired, new UTF8Encoding(false));
        return true;
    }

    internal static int VerifyFinished()
    {
        if (Process.GetProcessesByName("stalker-gamma").Length > 0 || Process.GetProcessesByName("ModOrganizer").Length > 0)
            throw new IOException("Close Mod Organizer and let any active installation finish before verifying.");
        var state = Program.LoadState();
        var folders = Safety.GameFolders(state.Root);
        if (!File.Exists(Program.LogFile)) throw new IOException("No completed installation log was found.");
        string log = File.ReadAllText(Program.LogFile);
        if (!Safety.Successful(log, 0) || !log.Contains("--gamma " + Program.Quote(folders[1])))
            throw new IOException("The log does not confirm a completed backend installation for this drive. Use Retry installation.");
        Safety.ValidateDrive(state.Root, false);
        if (RepairKnownAlias(folders[1])) Program.Log("Corrected the known GAMMA profile name: " + CorrectName);
        int count = Safety.VerifyGame(folders[0], folders[1]);
        state.Phase = "Ready"; state.Error = ""; Program.SaveState(state);
        Program.Log("Verification passed: " + count + " enabled mods. No downloads or extraction were performed.");
        return count;
    }
}
