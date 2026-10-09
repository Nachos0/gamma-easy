# Changelog

## Unreleased

- Documented the planned optional xrRazom co-op integration and reversible mode
  switch. This is a research/roadmap update; no co-op functionality is shipped.

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
