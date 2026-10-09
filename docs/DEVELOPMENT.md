# Development

## Layout

```text
src/                 Windows Forms UI, setup, installer worker, safety checks
tests/SelfTests.cs   Isolated fixtures and process cleanup checks
scripts/            Dependency fetching, compilation, tests, release packaging
docs/               User guides, validation, roadmap, release notes
third-party/        Dependency notices and licenses
dependencies.lock.json
.github/            Windows CI, release workflow, issue and PR templates
```

The wrapper targets .NET Framework 4.8 and uses C# syntax supported by its compiler.
The pinned backend is a self-contained native Windows x64 executable; end users
do not need the .NET SDK used to build that upstream project.

## Build and test

```powershell
.\scripts\build.ps1
.\scripts\test.ps1
.\scripts\package.ps1
```

These scripts write only to `.cache`, `build`, `test-results`, and `dist` within the
repository. `build.ps1` downloads the backend only when its cached SHA256 does not
match the lock. Packaging downloads the matching upstream source archives and
checks every source archive against its recorded SHA256.
The downloadable source bundle includes these dependency archives in
`upstream-sources`; the scripts can use those verified copies without fetching them.

No test should download the game, modify game folders at a drive root, install
system runtimes, change Defender, or request elevation. The current real-backend
smoke test runs only its help command. That backend may create its own diagnostic
log under the current user's application-data folder.

## Architecture

The setup copies the launcher and verified backend to the per-user app directory.
The regular UI runs without administrator permission. Clicking Install starts an
elevated worker, which validates the drive, refuses unmanaged existing folders,
installs requested prerequisites, creates the backend profile, and runs full install.

The worker writes a log and atomic XML state. The UI reads progress and sends Stop
through a request-specific cancellation file. Backend children are attached to a
Windows job object so closing the job stops extraction/download helpers. Microsoft
runtime installers are allowed to finish rather than being killed midway.

Play requires the worker's confirmed success and local file/profile checks. It uses
MO2's CLI to launch the configured DX11 entry, preserving the virtual mod filesystem.

## Updating the backend

1. Choose a specific upstream release and inspect its matching source.
2. Verify the downloaded binary digest against the release's published digest.
3. Update `dependencies.lock.json`, including corresponding source archives.
4. Update `BackendHash` in `src/Program.cs` and the release version strings shown in
   `src/Worker.cs`, `src/MainForm.cs`, and the dependency notices.
5. Rebuild, run isolated tests, and perform a complete game installation test.

The build fails if the source hash and manifest disagree. Do not update only the
version number or select a floating `latest` binary.

## Releases

Push a version tag such as `v0.1.0`. The workflow builds and tests on Windows,
packages the installer ZIP and source ZIP, produces `SHA256SUMS.txt`, and publishes
an **early-alpha prerelease** after the build succeeds.

Both the binary and corresponding source belong in the release. Do not commit
generated installers, dependency caches, credentials, or game files into Git.
Current source bundles include the app, original backend binary, matching backend
source, native dependency sources, and license notices.

The executable is not signed yet. Current packages are buildable from a dependency
lock, but byte-for-byte deterministic C# compilation is not claimed.
