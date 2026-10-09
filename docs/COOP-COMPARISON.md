# Co-op candidates and app improvements

Research date: 2026-10-09. This is a documentation review, not a multiplayer test.
The base installation has now reached the game world, confirmed by the user.
No co-op provider has been installed or validated by GAMMA Easy.

## Candidate shortlist

| Candidate | Primary-source evidence | Position for this app |
| --- | --- | --- |
| xrRazom 1.4 | Author documents base GAMMA support with bugs, plus an MO2 installer and separate engine payload. | First candidate to test. |
| Anomaly Together 2.8.11F | Linked release targets Anomaly 1.5.3, uses AOEngine, includes addon patches, and links GAMMA tutorials. | Alternative candidate, evaluated independently. |
| xrMPE | Author explicitly states that this Call of Pripyat co-op mod does not work with Anomaly. | Outside this app's GAMMA integration scope. |
| Zone | Project describes a dedicated Go server and separate injected client for Anomaly. | Different architecture; research only, with no suitability established. |

Sources: [xrRazom](https://www.moddb.com/mods/xrrazom-stalker-anomaly-co-op),
[Anomaly Together release](https://www.moddb.com/downloads/anomaly-together-2-coop),
[xrMPE](https://www.moddb.com/mods/x-ray-multiplayer-extension), and
[Zone](https://github.com/AnarchadiaMC/Zone).

## Recommendation: test xrRazom first, retain Anomaly Together as an alternative

This is an engineering judgment from the available documentation, not a finding
that one engine is faster, more stable, or bug-free. xrRazom's GAMMA/MO2 route and
documented integration in another Windows launcher provide a clearer starting
point for the reversible mode switch we want. A two-player trial on our actual
installed GAMMA configuration must decide whether that starting point works.

Anomaly Together's author describes addon compatibility as requiring adjustments.
Its guide uses LAN/IP or Steam and specifies UDP port 44139. The guide also asks
clients to prepare matching faction/location settings. That deserves its own
provider-specific setup and test recipe rather than borrowing xrRazom assumptions.
[Source](https://www.moddb.com/downloads/anomaly-together-2-coop).

Do not mix both providers' engines or patches. Provider selection should be a
choice among separately tested configurations. Initially, only the provider that
passes our checks should become available in the app.

## Useful existing work

[STALKER-GAMMA-COMMANDER-Windows](https://github.com/brodrigz/STALKER-GAMMA-COMMANDER-Windows)
documents a pinned xrRazom Slim 1.4 package, separate MO2 profile, reversible engine
activation, maintenance recovery, and retained backups. It also notes that MO2
profiles share Anomaly saves, and that some session/friend-comparison controls are
hidden pending validation. This is an implementation reference to inspect, not
evidence that our integration has been tested. Review its license before reusing code.

[stalker-sync](https://github.com/Andrew1431/stalker-sync) compares the effective
files after MO2 priorities and Overwrite, rather than only mod names. Its documented
workflow uses manifests, difference reports, and backed-up patches. That suggests
a useful friend-compatibility feature, subject to code review and our own tests.

The `tdclmf/Anomaly-Together-2.0` repository exposes scripts and a release, but
its connection to the exact ModDB package supplied by the user has not been
established. Use the supplied author/upload page as the download reference until
package provenance and version identity are confirmed. Other surfaced GAMMA
multiplayer prototypes are not selected merely because their README mentions GAMMA.

## How GAMMA Easy can improve the experience

These are proposed features; none are shipped co-op behavior.

1. **Optional setup after a verified base install.** Download a pinned provider
   package and apply a recorded GAMMA compatibility recipe.
2. **A true Single-player / Co-op switch.** Change the profile and all required
   engine files together. Track original, replaced, and newly added files so the
   original mode can be restored. Refuse switching while the game or MO2 is open.
3. **Recover interrupted changes.** Use a transaction record and verified backups;
   block launch after an incomplete switch until recovery succeeds.
4. **Check friends' actual configuration.** Compare provider/protocol versions,
   active mod order, and the files the game will actually load. Offer a read-only
   difference report before proposing any repair.
5. **Keep co-op compatibility separate from the optional archive MD5 checkbox.**
   Healthy downloaded ZIPs alone cannot prove two active mod configurations match.
   Use cached scans where appropriate, with an explicit full rescan when needed.
6. **Guide Host / Join and LAN / Steam honestly.** Use the selected provider's
   supported workflow. Do not silently change networking, replace player identity,
   or pretend the launcher can bypass the provider's account requirements.
7. **Protect save intent.** Start with clearly named co-op saves and documented
   host ownership. Separate MO2 profiles alone are not proof of save isolation.
8. **Coordinate GAMMA repair/update.** Preserve or refresh the single-player
   baseline, then reapply only a compatible co-op recipe. Identify altered engine
   files before launch rather than allowing the wrong mode to start.

## The test that should choose the provider

Use two computers with the same recorded GAMMA configuration. Test one provider
at a time: installation, host/join, trading and loot, healing/revive, a quest,
level transitions, host save/load, client reconnect, and switching back to the
original single-player game. Repeat after a GAMMA repair/update.

Record versions, enabled mods, patches, networking route, and observed failures.
Choose the provider based on those results. No comparative performance or
stability result is claimed by this research.
