# Changelog

## 0.2.0 — Optional co-op alpha

- Verified xrRazom 1.4 Slim/Full import; Full installs core files only.
- Automatic download with official-browser/import fallback.
- Separate MO2 profile, reversible engines and persistent recovery journals.
- Engine-baseline refresh during GAMMA repair/update.
- Read-only friend comparison for configs, scripts, engines and mod order.
- Supported in-game Host/Join guidance. Two-player gameplay is unvalidated.

52 isolated checks and an authenticated real-package fixture passed.

## Unreleased

- Documented the planned optional xrRazom co-op integration and reversible mode
  switch. This is a research/roadmap update; no co-op functionality is shipped.

## 0.1.1 — Installation verification recovery

- Correct the specific upstream DarkasleQif/Darkasleif mod-name mismatch, with a
  profile backup and preserved priority/enabled state.
- Accept the escaped Windows game-path format written by Mod Organizer.
- Add Verify to recheck a completed backend installation without downloads or extraction.
- Offer an optional, cancellable cached-archive MD5 audit, disabled by default.
- Continue rejecting unrelated missing mods and paths pointing at another game folder.

## 0.1.0 — Early alpha

- Windows Forms setup, drive selection, and launcher.
- Anomaly and GAMMA installation through a pinned FaithBeam backend.
- Drive-root layout, internal-drive/NTFS/space checks, and existing-file protection.
- Optional Defender folder exclusions and signed Microsoft runtime downloads.
- Stop/retry with cached archives and backend child-process cleanup.
- Profile/file checks and DX11 launch through Mod Organizer 2.
- Build, tests, dependency locks, corresponding-source packaging, and Windows CI.

**Validation:** isolated launcher checks passed locally. Full game installation,
system prerequisite installation, UAC handoff, and in-game launch remain untested.
