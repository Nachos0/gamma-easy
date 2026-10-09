# Third-party components

GAMMA Easy is an independent GPL-3.0-or-later Windows wrapper. It integrates the
following projects, which retain their original licenses and copyrights.

| Component | Pinned version / revision | License / source |
| --- | --- | --- |
| FaithBeam stalker-gamma-cli | 1.37.1 / `fad25e08a273368ca65c5faa6a65a6515efc7895` | [GPL-3.0](https://github.com/FaithBeam/stalker-gamma-cli) |
| 7-Zip | 26.03 | LGPL, BSD parts, unRAR restriction; [source](https://github.com/ip7z/7zip/tree/26.03) |
| curl-impersonate | v2.2.2 | Project license and underlying curl/dependency notices; [source](https://github.com/lexiforest/curl-impersonate/tree/v2.2.2) |
| LibGit2Sharp | 0.32.0 | MIT; [source](https://github.com/libgit2/libgit2sharp/tree/0.32.0) |
| LibGit2Sharp.NativeBinaries | 2.0.324 | Wrapper and native component notices; [source](https://github.com/libgit2/libgit2sharp.nativebinaries/tree/2.0.324) |
| libgit2 | `5853918c4c6a7b12f8becf4bd11ff4362ebb9020` | GPLv2 with linking exception; [source](https://github.com/libgit2/libgit2/tree/5853918c4c6a7b12f8becf4bd11ff4362ebb9020) |

Original license texts are in `licenses/`. Release source bundles include the app,
original backend archive, corresponding backend source, and matching native project
sources. Upstream build scripts describe additional native/managed dependencies and
their sources. The source URLs and SHA256 values are locked in
[`dependencies.lock.json`](../dependencies.lock.json).

GAMMA mod definitions and patches come from their upstream providers during
installation. STALKER/Anomaly/game archives, modpack files, and Microsoft runtimes
are not redistributed inside this setup. This project claims no endorsement by
the upstream authors, GAMMA, or GSC Game World.
