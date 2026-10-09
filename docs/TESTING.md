# Validation status

The initial build passed **25 checks** locally on Windows. CI is configured to rerun
these from the repository. Passing them demonstrates launcher behavior; it does not demonstrate a
working installed game.

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

- A complete fresh Anomaly + GAMMA installation from the current upstream files.
- UAC handoff under a standard Windows user, including alternate administrator credentials.
- Real system runtime installation and restart behavior.
- Actual MO2/DX11 startup and an in-game session.
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
