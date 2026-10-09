# Install GAMMA Easy

This is an **unsigned early-alpha build**. The launcher checks have passed, but the
complete game installation and launch have not yet been validated end to end.

## Before starting

- Use Windows 10/11 **x64** with .NET Framework 4.8.
- Pick an internal **NTFS** drive. An SSD is preferred. USB and network drives are
  not supported.
- Allow about **200 GB free** for a fresh installation, including cached archives.
- The selected root must have room for separate `Anomaly` and `GAMMA` folders.
  Existing nonempty folders are refused unless GAMMA Easy previously created them.

You do not need to download RC3, install 7-Zip, or configure MO2 manually for this
installer route. GAMMA Easy embeds a pinned third-party installer engine and its
extraction tool. Its compatibility with the complete current modpack remains an
end-to-end test item.

## Install and play

1. Download the Windows ZIP from
   [Releases](https://github.com/Nachos0/gamma-easy/releases).
2. Extract the ZIP and run `GammaSetup.exe`.
3. Click **Install launcher**. It creates a Start menu shortcut.
4. In the launcher, select the drive. For example, selecting `D:` uses
   `D:\Anomaly` and `D:\GAMMA`.
5. Leave **Install required Microsoft components** checked unless you already have
   the required Visual C++ and legacy DirectX runtimes.
6. Optionally choose the clearly labeled **Windows Defender folder exclusions**.
   Antivirus is never switched off by the app.
7. Click **Install** and accept the Windows administrator prompt.
8. Wait for the downloads, extraction, and checks. If a Windows restart is
   requested by a runtime installer, restart before playing.
9. Click **Play**. MO2 loads the `G.A.M.M.A` profile and starts Anomaly in DX11.

Game files come from their upstream sources. The installer does not bundle or
rehost the game or modpack. A download host may throttle, fail, or challenge a request.

## Stop, retry, and update

After a backend install finishes, **Verify** rechecks the files/profile without
downloading or extracting again. Leave the optional cached-download MD5 checkbox
off for this fast check. Enabling it scans cached archives against the saved
published digests; Stop cancels that read-only scan. Missing cached archives do not
mean installed game files are missing. Full install/retry still performs the
third-party backend's own required integrity checks.

**Stop** cancels the backend and its child processes. If a Microsoft runtime
installer is running, it is allowed to finish first. Complete cached archives
remain available; an incomplete individual archive may need a fresh download.

**Retry installation** reruns the installer against folders this app owns.
**Repair / update** restores the official GAMMA profile and can reset custom mod
selections and executable entries. The backend is asked to preserve game `user.ltx`
and MCM settings. Close MO2 and the game first.

## Files and Windows changes

| Location or setting | Purpose |
| --- | --- |
| `%LOCALAPPDATA%\GammaEasy` | Launcher, embedded backend, state, runtime cache, installation log |
| Start menu `GAMMA Easy` | Launcher shortcut |
| Selected drive `\Anomaly` | Game files and game settings/saves |
| Selected drive `\GAMMA` | Mods and portable Mod Organizer configuration |
| `GAMMA\downloads` | Cached archives |
| `%APPDATA%\stalker-gamma` | Backend profile settings and detailed logs, under the installing administrator account |
| Windows `LongPathsEnabled` | Enabled by the backend during installation |
| Defender folder exclusions | Added only if selected |
| Microsoft game runtimes | Installed only if selected |

## Remove the launcher

Close the launcher, remove `%LOCALAPPDATA%\GammaEasy`, and remove its Start menu
shortcut. The game folders are separate. Remove them only if you intend to delete
the game and its saves. Exclusions, Windows settings, and Microsoft runtimes are
not removed automatically.

For the upstream manual process, see the
[official GAMMA guide](https://github.com/Grokitach/Stalker_GAMMA/wiki/Installing-GAMMA).
