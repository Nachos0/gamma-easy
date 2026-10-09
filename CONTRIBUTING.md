# Contributing

Small, reviewable changes and detailed installation reports are welcome.

1. Open an issue for a substantial behavior change.
2. Fork the repository and create a branch.
3. Build with `scripts/build.ps1` and run `scripts/test.ps1` on Windows x64.
4. Explain the problem, changed behavior, and validation in your pull request.

Keep drive/path validation, existing-file protection, cancellation, explicit
security choices, and confirmed-success checks intact. Do not disable antivirus,
silently add exclusions, skip download verification, or report success after a
failed backend command.

Tests must avoid downloading the game or changing system/game state. A real
installation test is a separate, deliberate activity; document its environment and
results. Never commit game archives, generated binaries, dependency caches,
credentials, or personal logs.

Use the bug form for launcher defects. Upstream game/mod problems may need the
GAMMA or backend project. Give those authors credit when changing their integration.

Contributions to this repository are provided under GPL-3.0-or-later. Third-party
files retain their own notices and licenses.
