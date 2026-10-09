# Planned co-op integration: xrRazom

**Status: research and planning only.** Co-op is not included in the current app
or release. First validate the ordinary GAMMA installation and game launch, then
work out and test the integration. The desired result is an optional co-op mode
that the user can enable after the base installation is ready.

## Upstream findings

Checked on 2026-10-09. These are the author's claims, not results from our testing.

- xrRazom targets Anomaly 1.5.3. The author reports that the base GAMMA profile is
  playable, with bugs and incomplete compatibility across its mods.
- Version 1.4 provides an MO2 installer. Installation also requires applying its
  engine payload to Anomaly outside MO2; the co-op mod takes highest priority.
- A GAMMA reinstall can restore the original engine and reset the mod list.
- Session participants need matching mods. Steam sessions require Call of Pripyat
  on every participant's account; LAN does not require that Steam ownership.

Sources:
[mod overview](https://www.moddb.com/mods/xrrazom-stalker-anomaly-co-op) and
[author's installation guide](https://www.moddb.com/mods/xrrazom-stalker-anomaly-co-op/downloads/xrrazom-stalker-anomaly-co-op-10).

## Intended app flow

1. Finish the ordinary GAMMA installation and verify it works.
2. Offer **Install co-op** as an optional post-install action.
3. Download and verify a specific supported xrRazom release from its upstream source.
4. Apply the tested GAMMA configuration, patches, and engine handling.
5. Offer **Single-player / Co-op** mode selection, then **Play**.

A toggle must switch the required engine payload and MO2 configuration together.
Merely unchecking a mod entry is insufficient when files outside MO2 were replaced.
Do not display a functional toggle until both directions have been validated.

## Decisions for the implementation phase

- Inspect the selected archive and its FOMOD selections before writing an automated
  install recipe. Determine which bundled components duplicate GAMMA components.
- Identify the exact tweaks required by the chosen GAMMA and xrRazom versions;
  record them in a versioned compatibility recipe rather than relying on guesses.
- Choose engine isolation or verified backup/restore after testing the actual
  payload. Account for every affected file, including newly added files.
- Keep distinct MO2 profiles and define how to preserve single-player saves and
  co-op host data. Do not assume the saves are interchangeable.
- Record installed versions and relevant hashes so mismatched peers can receive
  useful guidance. Keep upstream mod matching enabled.
- Coordinate repair/update with co-op state: detect invalidated engine files and
  require a verified reapplication or return to single-player before Play.
- Decide how the UI guides LAN versus Steam hosting/joining using supported
  upstream behavior. Network configuration remains a later implementation decision.
- Review upstream distribution terms before bundling any co-op files. Prefer a
  client-side download from the author rather than adding mod binaries to Git.

## Acceptance checks before shipping

- A clean GAMMA install launches successfully before adding co-op.
- Two players can host/join using a matching, recorded configuration.
- Test save/load, reconnect, level transitions, and representative GAMMA mods.
- Switching back launches the original single-player configuration successfully.
- Cancellation, failed installation, and partial switching preserve recoverability.
- GAMMA repair/update cannot silently leave co-op selected with the wrong engine.

No co-op archive has been downloaded, no game files have been modified, and no
compatibility recipe is claimed as validated by this planning change.
