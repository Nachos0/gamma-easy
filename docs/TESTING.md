# Validation status

The initial build passed **25 checks** locally on Windows. CI is configured to rerun
these from the repository. Passing them demonstrates launcher behavior; it does not demonstrate a
working installed game.

Version 0.1.1 passes **36 isolated checks**, including MO2 Windows-path parsing,
the known upstream mod-name alias, profile backup/priority preservation, optional
cached MD5 matching, mismatch reporting, and audit cancellation. A real completed
installation passed file/profile verification with 578 enabled mods after the
specific naming repair.

## First real installation and in-world launch — 2026-10-09

The user reported that the game started normally through the updated app and
successfully loaded into the world. This followed a real installation, a transient
GitHub clone failure/retry, and the 0.1.1 profile/path correction. The installed
profile passed local checks with 578 enabled mods.

Evidence is a successful user-reported in-world launch plus the inspected installer
log and file/profile checks. It is not an independently observed extended gameplay
test or evidence for every supported Windows configuration. Co-op, prolonged
stability, and save/load behavior remain unvalidated.

| Area | Checked |
| --- | --- |
| Drive paths | Root normalization, nested/network/path traversal rejection, live internal-drive identification |
| Existing folders | Unmanaged files refused and preserved |
| Success reporting | Nonzero exit, failure text, fatal text, and missing completion cannot be treated as success |
| Runtime restarts | Restart requirement is recognized |
| Persisted state | State round trip and atomic replacement |
| Bundled installer | SHA256, extraction, real backend help command |
| Cancellation | Windows job closes both a backend-style parent and child process |
| Installed profile | Missing/empty enabled mods and unsafe mod names are refused; complete fixture accepted |

Additional local checks found live GAMMA definition endpoints responding, verified
Microsoft signatures on the Visual C++ and DirectX downloads, unpacked DirectX with
the bundled 7-Zip, verified DXSETUP's signature, and visually inspected the launcher.

## Still needed before a stable release

- Repeat a fresh installation on another supported Windows configuration.
- UAC handoff under a standard Windows user, including alternate administrator credentials.
- Real system runtime installation and restart behavior.
- Extended gameplay, save/load, and repeated MO2/DX11 startup.
- Stop/retry during real large downloads and extraction.
- Repair/update against a working installation, including saved-game/settings preservation.
- Mirror failures, antivirus blocking, and low-space failures during installation.

The app checks files, profile configuration, and enabled mod folder contents. It
does **not** reproduce MO2's full game-data validation or prove every installed
file's integrity. Upstream installer integrity checks are separate.

## Reporting a real test

Use the installation-test issue form and include the app/backend version, Windows
version, drive type, phase, and relevant log excerpt. Remove usernames, personal
paths, access tokens, and unrelated information. Describe what happened rather
than just marking the install as passed.
