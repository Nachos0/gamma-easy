# Optional xrRazom co-op — 0.2.0 alpha

The app can prepare xrRazom 1.4, create a separate MO2 profile, activate its engine,
and restore the original single-player engine. Two-player gameplay is unvalidated.
Backups and switching were tested with fixtures and the authenticated package.

## Setup

1. Finish and verify the ordinary GAMMA installation.
2. Close Anomaly and MO2. Open Co-op setup in GAMMA Easy.
3. Choose Download and set up, or Install from ZIP with official xrRazom 1.4 Slim
   or Full Bundle. If automatic downloading is blocked, use its official browser
   page and import the ZIP.
4. Preparation leaves the game in Single-player mode.
5. Choose Co-op · xrRazom, accept the Windows prompt, and click Play.

Full Bundle installs core files only, omitting extra MCM, FDDA and language options
as specified by its GAMMA installer selection. The mod is highest priority in
G.A.M.M.A. Co-op; the original single-player mod selection is preserved.

Package checks are mandatory, independent of optional cache MD5 scanning:
Slim MD5 3e33cc23314123b8a8cbf79457fb2403; Full MD5 ede2ce10bd15962030f696bc76ba7f29.
The inspected Full Bundle is also SHA256-pinned to
8bd015490d20af95e4a2d28eb78a79e365a0b19795f29be6a77a902f05b7c9a7.
Co-op payloads are imported/downloaded client-side, not redistributed in releases.

## Host and join

Use Settings → xrRazom Co-op inside the game. The app does not rewrite undocumented
session settings or silently change your firewall/router.

- LAN: everyone closes Steam before launch. Enable hosting and start/load the world
  on the host. The client chooses Join Session and enters the host IP.
- Steam: everyone starts Steam and has Call of Pripyat on their account. Use the
  provider's Steam invitation/join flow.
- Set each player's name and keep it consistent. Keep matching mods and the
  provider's compatibility check enabled.

[Author's guide](https://www.moddb.com/mods/stalker-anomaly/addons/xrrazom-stalker-anomaly-co-op-slim).

## Returning to single-player and recovery

Close Anomaly and MO2, then select Single-player. Original engine bytes are restored
and only managed additions are removed. Unexpected external engine changes cause
refusal rather than overwrite.

Use distinct co-op save names. MO2 profiles may share Anomaly saves. The app does
not convert saves or isolate host/client persistence; follow the provider's rules.

Interrupted operations block Play. Use Co-op setup → Recover switch. If files changed
externally, backups are retained and recovery needs attention.

GAMMA Easy restores single-player before its repair/update, refreshes backups after
success and leaves co-op inactive. Select Co-op again when ready. Failed maintenance
blocks launch until successful retry. External updates/manual co-op are not adopted.

Backups/journals: %LOCALAPPDATA%\GammaEasy\coop\<drive-letter>. Switch to Single-player
before removing the launcher or deleting those backups.

## Friend comparison

With co-op active, Export my compatibility and share the XML yourself. It contains
mod order and relative paths/hashes, excluding personal MCM settings. Compare
friend's manifest produces a read-only difference report.

The scan accounts for base data, mod priority and MO2 Overwrite. It compares configs,
scripts and managed engines, not every world/spawn or asset file. Matching the report
is not a replacement for xrRazom's own connection checks. No friend patch is applied.

[Candidate comparison](COOP-COMPARISON.md).
