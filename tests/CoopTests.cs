// Copyright (C) 2026. GPL-3.0-or-later.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

static class CoopTests
{
    internal static void Run(string root, Action<bool,string> check, string realArchive = null)
    {
        string a = Path.Combine(root,"game"), g = Path.Combine(root,"gamma"), h = Path.Combine(root,"backups");
        Directory.CreateDirectory(a); Directory.CreateDirectory(g);
        foreach (string name in new[] { "AnomalyLauncher.exe", "bin/AnomalyDX11.exe", "fsgame.ltx", "appdata/user.ltx" })
        { string p=Path.Combine(a,name); Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllText(p,"single-player fixture"); }
        string profile=Path.Combine(g,"profiles","G.A.M.M.A"); Directory.CreateDirectory(profile);
        File.WriteAllText(Path.Combine(g,"ModOrganizer.exe"),"fixture");
        File.WriteAllText(Path.Combine(g,"ModOrganizer.ini"),"[General]\nselected_profile=@ByteArray(G.A.M.M.A)\ngamePath=@ByteArray("+a.Replace('\\','/')+")\n3\\title=Anomaly (DX11)\n");
        File.WriteAllText(Path.Combine(profile,"modlist.txt"),"+BaseMod\n"); File.WriteAllText(Path.Combine(profile,"modpack_maker_list.txt"),"fixture");
        string baseMod=Path.Combine(g,"mods","BaseMod"); Directory.CreateDirectory(baseMod); File.WriteAllText(Path.Combine(baseMod,"data.ltx"),"fixture");
        var manager=new CoopManager(a,g,h);
        if (realArchive == null)
        {
            using(var memory=new MemoryStream())
            {
                using(var zip=new ZipArchive(memory,ZipArchiveMode.Create,true))
                foreach(string name in new[] { "Anomaly/bin/AnomalyDX11.exe","Anomaly/bin/new-network.dll","Anomaly/gamedata/configs/coop.ltx","gamedata/scripts/coop.script" })
                using(var writer=new StreamWriter(zip.CreateEntry(name).Open())) writer.Write("co-op fixture");
                memory.Position=0; using(var zip=new ZipArchive(memory,ZipArchiveMode.Read)) manager.InstallEntries(zip,CancellationToken.None);
            }
        }
        else manager.Install(realArchive,CancellationToken.None);
        check(manager.State().Installed && !manager.State().Active,"co-op preparation does not activate engine");
        check(File.ReadAllText(Path.Combine(a,"bin","AnomalyDX11.exe"))=="single-player fixture","preparation preserves single-player engine");
        check(File.ReadAllText(Path.Combine(profile,"modlist.txt"))=="+BaseMod\n","single-player profile preserved");
        bool refused=false; try { manager.Resolve("game/bin/../../appdata/saves/delete"); } catch { refused=true; } check(refused,"co-op paths cannot traverse outside payload");
        if(realArchive == null)
        {
            manager.TestFault=i=>{ if(i==2) throw new IOException("simulated interrupted copy"); };
            refused=false; try { manager.Switch(true); } catch { refused=true; } manager.TestFault=null;
            check(refused && !manager.Pending && !manager.State().Active,"failed mode switch rolls back journal");
            check(File.ReadAllText(Path.Combine(a,"bin","AnomalyDX11.exe"))=="single-player fixture" && !File.Exists(Path.Combine(a,"bin","new-network.dll")),"rollback restores files and removes only managed additions");
        }
        manager.Switch(true);
        check(manager.State().Active && manager.VerifyMode()==CoopManager.Profile,"co-op engine and profile activate together");
        check(Safety.VerifyGame(a,g,CoopManager.Profile)==2,"co-op profile passes normal game file checks");
        manager.Switch(false);
        check(!manager.State().Active && manager.VerifyMode()=="G.A.M.M.A","single-player mode restored");
        check(File.ReadAllText(Path.Combine(a,"bin","AnomalyDX11.exe"))=="single-player fixture","original engine bytes restored exactly");
        string engine=Path.Combine(a,"bin","AnomalyDX11.exe"); File.WriteAllText(engine,"outside change");
        refused=false; try { manager.Switch(true); } catch { refused=true; }
        check(refused && File.ReadAllText(engine)=="outside change","external engine changes are not overwritten");
        File.WriteAllText(engine,"single-player fixture");
        manager.BeginMaintenance(); check(manager.State().Maintenance,"maintenance blocks co-op launch");
        refused=false; try { manager.VerifyMode(); } catch { refused=true; } check(refused,"incomplete maintenance blocks Play");
        File.WriteAllText(engine,"updated single-player fixture"); manager.FinishMaintenance();
        manager.Switch(true); manager.Switch(false);
        check(File.ReadAllText(engine)=="updated single-player fixture","GAMMA update refreshes single-player backup");
        var mine=new CoopFingerprintData { Mods=new List<string>{"A"}, Files=new List<CoopFingerprintFile>{new CoopFingerprintFile{Path="gamedata/configs/a.ltx",Hash="a"}} };
        var friend=new CoopFingerprintData { Mods=new List<string>{"A"}, Files=new List<CoopFingerprintFile>{new CoopFingerprintFile{Path="gamedata/configs/a.ltx",Hash="a"}} };
        check(CoopFingerprint.Compare(mine,friend).Count==0,"identical friend manifests compare cleanly");
        friend.Files[0].Hash="changed"; friend.Mods.Add("B"); check(CoopFingerprint.Compare(mine,friend).Count==2,"friend file and mod-order differences reported");
    }
}
