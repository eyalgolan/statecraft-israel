# M3 release: the browser build

**Status:** M3 ships as the local browser build. The itch.io upload and the cross-upload save test are deferred to the
start of M4, as a private Draft (owner ruling, 2026-10-10); their sections below are kept, marked "Deferred to M4".

## What ships
- **Folder and zip:** `unity/Builds/Web-brotli/` uses Brotli compression with no decompression fallback. It is zipped as `unity/Builds/statecraft-m3-web.zip`, with `index.html` at the zip root. Both are build output and are not committed.
- **Built by:** `"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression brotli -logFile "$PWD/unity-build-web-br.log"`, then `python3 tools/package_release.py` (it zips the folder, `index.html` at the root, and refuses while any sensitive event is not sensitivity-reviewed). Never zip by hand.
- **After building:** drop the build's rewrites of the settings with
  `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset` (as in `dev-setup.md`), so no
  build-cache change gets committed.
- **Verified locally by:** `uv run tools/web_check.py unity/Builds/Web-brotli --query "golden&smoke=5"` with the three
  full `--expect` values pinned in `docs/determinism.md` (both goldens and the smoke line). This runs both golden hashes
  in the WebGL IL2CPP player, then plays five turns through the UI's own buttons (answering two events through the card:
  the IDF founding order on turn 2, the first truce on turn 4).
  - **Result at the merge candidate (2026-10-11, commit a099655, Brotli build):** `GOLDEN OK 1950 71e9904d…`,
    `GOLDEN OK 1952 73590e32…`, `SMOKE OK 5 turns 1948-06-19 d3fdd459…` (all three matched in full), then PASS.
  - History: 2026-10-10 at d450566 (Brotli, before the 1948 war-period events): `GOLDEN OK 1950 ee5d0826…`,
    `GOLDEN OK 1952 91bb6fb5…`, `SMOKE OK 5 turns 1948-06-19 7004c29a…`, PASS; folder 23 MB in 36 files, zip 20,862,385
    bytes. After the 1948 events (playtest 1 E, Disabled build): the current hashes, PASS.
- **Size (merge candidate, a099655):**
  - The folder is 23 MB (`du -sh`) in 51 files. itch.io allows 1,000 files.
  - The zip is 20 MB (20,941,435 bytes).
  - `StreamingAssets/map/heightmap.r16` (8 MB) was served as is, outside Unity's compression. Issue #44 packed it (below).
- **Smaller download: the packed heightmap (issue #44, 2026-10-10).** StreamingAssets files are served as they are, so
  the raw 16-bit grid cost 8,396,802 bytes on every load. It is now `heightmap.r16z` (4,062,705 bytes): the grid's row
  deltas split into a low-byte and a high-byte plane, in one zlib stream (`tools/heightmap_codec.py` writes it,
  `HeightmapCodec.cs` decodes it with `System.IO.Compression.DeflateStream`, which IL2CPP backs with its bundled zlib on
  WebGL too, then checks the size and the Adler-32). It decodes to the old file byte for byte: `MapTests` and
  `test_heightmap_codec.py` pin the SHA-256 of the raw file (`b6882a77…`), and the terrain tests, goldens and smoke are
  unchanged.
  - **Options weighed (same grid, bytes):** raw 8,396,802; zlib of the raw grid 6,284,586; Brotli 11 of the raw grid
    (what moving it into the `.data` file would give: Unity's Brotli build matches quality 11, 10,988,872 against
    10,953,925 for the same `.data`) 4,982,506; row deltas in byte planes + zlib (chosen) 4,062,705; the same planes
    under Brotli 11 3,822,573. Moving it into `.data` saves less (3.4 MB on the wire against 4.3 MB; 1.3 MB of zip
    against 2.2 MB), puts the bytes in the file the engine must load before it starts (the StreamingAssets fetch runs
    beside the content fetches), grows the uncompressed build used for local and CI checks by 8 MB, and moves the file
    every map tool reads. The Brotli-planes variant would save 0.24 MB more but needs the same decoder plus all of that.
  - **Size, fb31ce1 -> issue #44 (Brotli build, measured on the folders; the zip is `tools/package_release.py`'s
    deflate zip of the same files, made without the release gate):** folder 28,310,337 -> 23,954,695 bytes (-4.36 MB,
    -15%; this is what a browser downloads), zip 25,782,074 -> 23,535,021 bytes (-2.25 MB, -9%; the old zip already
    deflated the raw grid to about 6.3 MB), 51 files both. The wasm grows by 14 KB (`.wasm.br` 8,622,716 ->
    8,636,749: DeflateStream). The zip has grown since a099655 (20.9 MB) with visual v1's baked textures.
  - **Boot (`uv run tools/headed.py boot --runs 3`, Disabled build, fb31ce1 -> #44; load average 6-12 from other
    sessions):** `--cpu 1`: `BOOT map_build_ms median=432` -> `480`, `BOOT first_frame_ms median=3580` -> `3646`;
    `--cpu 4`: `map_build_ms median=1897` -> `2035`, `first_frame_ms median=7320` -> `7401`. The decode is the "heightmap
    decoded" phase: 7 -> 41 ms at `--cpu 1`, 34 -> 184 ms at `--cpu 4`. Every span stays within the visual v1 boot
    budget (Task 1 baselines 443 / 3547 ms at `--cpu 1` and 1813 / 6963 ms at `--cpu 4`, plus max(300 ms, 50%)); the
    tool exits 0 on each. Served from localhost the download is free, so these runs show only the decode's cost; on a
    real connection 4.3 MB less to fetch is worth far more (3.5 s at 10 Mbit/s).
  - **Goldens:** `uv run tools/web_check.py unity/Builds/Web-disabled --query "golden&smoke=5"` with the three `--expect`
    values from `docs/determinism.md`: PASS on both builds (the Brotli build too).
- **Play it locally:** serve the folder with web_check's handler, which sends `Content-Encoding: br` for `*.br`. Plain `python3 -m http.server` does not, so the Brotli build would not load. Then open http://localhost:8000/.
  ```
  python3 -c "import sys,functools,http.server; sys.path.insert(0,'tools'); import web_check as w; http.server.ThreadingHTTPServer(('127.0.0.1',8000), functools.partial(w.Handler, directory='unity/Builds/Web-brotli')).serve_forever()"
  ```
  The uncompressed build also works with `python3 -m http.server 8000 --directory unity/Builds/Web-disabled`.
- **Console lines a browser check can rely on:**
  - `GAME OK <date> turn <n> hash <state hash>` at start (a new game, or the autosave continued).
  - `TURN <n> <date> hash <state hash>` after every End Turn.
  - `CAMERA distance=<m> yaw=<deg>` with `?camlog`.
  - `PROFILE ...` with `?profile`.
  - `QUALITY level=<n> scale=<s> tiltshift=<on|off> p95_ms=<ms>` whenever the runtime quality fallback steps (WebGL only;
    off with `?nofallback`; docs/visual-v1.md "Runtime quality fallback").
  - `BOOT phase=<name> at=<ms> ms dt=<ms> ms` for each boot phase with `?bootlog` (playtest 2, P2-1).
  - `NAV f=<frame> ...` with `?navlog`: every raw mouse event, UI pointer and submit event, screen toggle and map click,
    with its frame (playtest 2, P2-4).
- **Player-facing wording:** the Budget screen gives each ministry's multiplier as "spending effect 150%" (the share's
  effect on that ministry's effects, on the 0-100 scale the rest of the game uses), never "x1.50" (playtest 2, P2-6).

## WebGL terrain profile
Results from `?profile`: 300 frames per setting after 60 warm-up frames, from the diorama camera over central Israel. All runs were in Chrome for Testing 1228 with `--disable-frame-rate-limit --disable-gpu-vsync`.

| `heightmapPixelError` | headless (`web_check.py`) avg / p95 ms | headed, Metal (Apple M5 Pro, 1728x858) avg / p95 ms |
|---|---|---|
| 1 (shipped; keeps the hex overlay draped) | 4.92 / 7.00 | 5.38 / 7.00 |
| 5 (Unity's default) | 1.51 / 2.00 | 2.19 / 4.00 |

**Decision: keep 1.** The p95 is 7 ms, far under the 33 ms of 30 fps (M2 ruling K2: above 1, the hex overlay sinks into valleys).

## Checks in a real browser (headed Chrome for Testing, ANGLE Metal), 2026-10-10
These were run with a small Chrome DevTools Protocol driver: `uv run --with websockets==15.0.1`, with a temporary profile and a fixed origin, `http://localhost:8765`.

### Rendering
- The terrain, hex overlay, settlement markers and labels, all UI Toolkit panels and the minimap render. Nothing is magenta.
- WebGL reports `ANGLE (Apple, ANGLE Metal Renderer: Apple M5 Pro)`.
- Three hidden URP utility shaders (`Hidden/CoreSRP/CoreCopy`, `Hidden/Universal Render Pipeline/StencilDitherMaskSeed`, `Hidden/Universal/HDRDebugView`) need shader target 4.5, which WebGL 2 (ES 3.0) lacks, so the browser reported them "not supported on this GPU". URP loads them up front (Blitter, UniversalRenderer, DebugHandler), but this build never draws with them: the render-graph copy needs framebuffer-fetch emulation, which Core RP disables on WebGL; stencil LOD cross-fade is off (Blue Noise, no LOD groups); the HDR debug view belongs to the Rendering Debugger. `Assets/Editor/WebShaderStripping.cs` strips them from WebGL builds only (playtest 1, item 12). The console no longer shows the three errors, and the goldens and the 5-turn smoke give the same hashes as before.
- The only 404 is `/favicon.ico`.

### Wheel zoom
- One wheel notch (Chrome sends 100 px) zooms by 10%: 120000 m to 108000 m to 97200 m.
- A 300 px burst zooms by three steps.
- The wheel over a panel does not zoom.
- Before this release a notch moved the camera only 0.08%. The Input System's default scroll behaviour already normalises a notch to 1, and the controller divided by 120 again; that division is now removed.

### Saves
Each step below passed:

| Step | Result |
|---|---|
| (a) Two turns, then a hard reload | Continued on the same turn and hash (turn 2, 1948-05-29). |
| (b) Tab closed immediately after End Turn's `TURN` line, then a new tab | Continued on turn 3. |
| (b) Whole browser quit immediately after a turn, then relaunched on the same profile | Continued on turn 4. |
| (c, local) Same origin, a second upload directory (`/upload-b/`, as an itch.io re-upload does) | Continued on turn 4. |
| (d) IndexedDB | Database `/idbfs`, store `FILE_DATA`, keys `/idbfs/statecraft-saves` and `/idbfs/statecraft-saves/autosave.txt`. No `JS_FileSystem_Sync` deprecation message. |
| (e) A private browser context (`Target.createBrowserContext`) | The game starts (a new game) and plays a turn. A second private context starts a new game again; no persistence is expected there. |

Firefox is not installed on the development Mac, so the Firefox check is open.

## OWNER STEP: sensitivity review before any shared build
Spec section 5.5 requires a recorded sensitivity review for events with sensitive tags (holocaust, terror, massacre,
war-casualties). The owner's sign-off for all seven events below is recorded in commit
[25d2ef6](https://github.com/eyalgolan/statecraft-israel/commit/25d2ef6004436668b42e289aff3dce06af128e09)
(2026-10-10; decision D11 in [docs/oss/DECISIONS.md](oss/DECISIONS.md)): each event's YAML `status:` is now
`sensitivity-reviewed`. For any new sensitive event, the owner records the sign-off **only** by moving its YAML
`status:` to `sensitivity-reviewed` and committing it (the loader accepts that value). Nothing else counts as a sign-off.

- **Gate:** `Build.Web ... -shareable` and `Build.Mac ... -shareable` refuse to build while any sensitive event is not
  `sensitivity-reviewed`, and name them (`Statecraft.Sim.ReleaseGate.SensitiveUnreviewed`). `tools/package_release.py`
  makes the shareable zip and runs the same check (`Statecraft.Cli --release-gate`) on the content inside the build, so
  it prints `PACKAGE REFUSED ...` and exits 1 while any sensitive event is not sensitivity-reviewed. Use `-shareable` and the script for every build
  that leaves this machine, including the M4 private Draft once it is shared with anyone. Local dev builds (no flag)
  are not gated, because web_check and the golden checks need them.
- **Pinned set:** `FinalWaveTests.TheSensitiveEventsArePinned` lists the seven ids, and
  `TheOwnerTableListsEverySensitiveEvent` fails if a sensitive event is missing from this table, so a new one cannot
  skip the review.

| Event | Tags | What to review |
|---|---|---|
| `altalena` | security, war-casualties | Jews killed by Jews: 16 Irgun and 3 IDF dead (in the YAML comment, not the card) |
| `ten_days` | security, war-casualties | Lydda and Ramle are shown as a military operation only (Lydda airport, the Jerusalem road); leaving out the population's flight and expulsion is an editorial choice the notes flag for explicit approval |
| `operation_yoav` | security, war-casualties | the Hebron-hills decision; military text only |
| `operation_hiram` | security, war-casualties | military text only; the contested population figures and the 26 Sep cabinet quote are left out |
| `operation_horev` | security, war-casualties | military text only |
| `bernadotte_assassination` | security, terror | the `terror` tag follows the Israeli government's own Prevention of Terrorism Ordinance |
| `reparations_claim` | holocaust | the claim's framing (the cost of absorbing survivors: 500,000 at $3,000 each); the "refuse" option |

## Deferred to M4: upload to itch.io (OWNER STEP)
Deferred to the start of M4, as a private Draft (owner ruling, 2026-10-10). The steps stay correct for then. This needs the
owner's itch.io account and is never automated, and the build must pass the sensitivity gate above (`-shareable`).
1. Sign in at https://itch.io. Go to Dashboard, then **Create new project**.
2. Set the title to "Statecraft: Israel (M3 preview)", Kind of project to **HTML**, and Classification to Games.
3. Under Uploads, upload the zip made by `bash scripts/oss/package_web.sh unity/Builds/Web-brotli unity/Builds/statecraft-m3-web.zip` (it puts the licence and attribution files inside, then runs `tools/package_release.py`; see `docs/RELEASING.md`) and tick **This file will be played in the browser**.
4. Embed options:
   - Viewport **1280 × 720**. It fits a 1366×768 or 1440×900 laptop page without scrolling, and under the UI's
     ScaleWithScreenSize (reference 1920×1080) it lays out exactly like 1920×1080; `UiLayoutTests` covers 1280×720.
   - Tick **Fullscreen button**. ("Click to launch in fullscreen" is the alternative launch mode if the embed feels small.)
   - Leave **SharedArrayBuffer support** off; the build uses no threads.
5. Visibility & access:
   - Choose **Draft** first.
   - Open the page and play a turn. The browser console (DevTools) must show `GAME OK` and then `TURN 1`.
   - Then choose **Restricted** (password) or **Public**.
6. Record the page URL below under "Published at".

itch.io serves `.br` files with `Content-Encoding: br`, because it detects the extension, so no server settings are needed.

Published at: (owner fills in)

## Saves on itch.io: the fix, and its test (the test is deferred to M4)
- **Problem:** on WebGL, `Application.persistentDataPath` is `/idbfs/<MD5 of the page's directory URL>`. itch.io serves every upload from a new URL, so a new upload would hide the saves made with the previous one.
- **Fix (Task 21):** the autosave lives at the fixed path `/idbfs/statecraft-saves/autosave.txt`.
  - The template sets `autoSyncPersistentDataPath: true`, so closing the file persists it to IndexedDB.
  - IndexedDB is per origin, so saves survive re-uploads to the same project and stay separate per browser profile.
  - The local checks above passed: reload, tab close, browser restart, and a second directory on the same origin.
- **Test plan (OWNER STEP, deferred to M4 with the upload; after the first two uploads):**
  1. Use one browser profile throughout, without clearing site data or using private mode. Record browser/version, OS,
     project page URL and the actual game-frame URL/origin (not only the outer itch.io page). Upload A as above. Play two
     turns and copy the last `TURN` line, including date, turn and full state hash. Record the upload/build ID, the git
     commit the build was made from and the zip's SHA-256. The game does not print its content hash; an identical zip
     guarantees identical content.
  2. Reload A. Expected: `GAME OK` gives the same date, turn and state hash as that last `TURN` line. Wait for the game to
     finish loading before reading it. Do not press New Game or End Turn after taking the baseline.
  3. Upload B to the same project, replacing A, with unchanged game content and save schema for this persistence control.
     Re-uploading the same build is enough to isolate the changed upload URL. Record both upload/build IDs. Confirm B has
     a different upload URL but the same actual game-frame origin. If its origin differs, record that separately: this
     fixed-folder test cannot prove persistence between origins, because IndexedDB is origin-scoped.
  4. Open B with a hard reload. **Pass** only if date, turn and full state hash match the baseline. Copy the complete
     `GAME OK` line, the old `TURN` line and any load warning/error. A date/turn match alone is not a pass. Reload B once
     more and check the same tuple, so a one-time replay is not mistaken for a durable save.
  5. If B starts again on 15 May 1948, or the state hash differs with unchanged content, record fail with both build IDs,
     browser/OS and frame origins. Do not reset or overwrite the save before capturing the evidence. Follow #43's fallback:
     if the save does not survive re-upload, save export/import becomes the next issue. No unrelated account uploads.
  6. Record the result below. This plan is not a completed itch.io test or a Firefox/Safari/Windows runtime check.

**Separate changed-content replay check:** after the unchanged-content control, a build with intentional content changes
may show "This save was made with different game content; the replayed game may differ." Record that warning and both
builds' git commits. Matching date/turn in this case is not evidence of exact state preservation, and it does not
replace the unchanged-content persistence control above. A changed state hash may be a replay effect rather than a storage failure.

Result: not run on itch.io (owner step).

| Evidence | Owner-run result |
| --- | --- |
| Browser/version | pending |
| OS | pending |
| Project page URL | pending |
| Upload A/build ID and game-frame URL | pending |
| Upload B/build ID and game-frame URL | pending |
| Game-frame origin A / B | pending |
| Git commit and zip SHA-256 A / B | pending |
| Date / turn before and after | pending |
| State hash before | pending |
| State hash after | pending |
| Full TURN / GAME OK lines and warnings | pending |
| Reload B result | pending |
| Pass / fail and fallback issue if needed | pending |

Review basis: `unity/Assets/Scripts/SaveStore.cs` fixes the WebGL directory; `sim/Statecraft.Sim/SaveGame.cs` records the
content hash and command log; `unity/Assets/Scripts/UI/GameRoot.cs` logs the current state hash; `SaveUiTests.cs` covers
changed page-directory paths and changed-content warnings. Those code/tests support the plan, not an itch.io runtime pass.

## Known limits of this build
- Placeholder settlement markers; per-era art is M4.
- No Diplomacy screen or legacy report (M4).
- English only.
- The headless smoke plays five turns; a full-slice smoke is M4.
- The wheel-zoom step (one notch = 10%) was measured in Chrome on macOS only. Firefox reports wheel deltas in lines and
  Windows at 125% scaling reports 125 px per notch, so re-check the step in the owner's M4 itch.io check.
- Not uploaded to itch.io: the upload and the cross-upload save test are deferred to the start of M4 (owner ruling,
  2026-10-10). The seven sensitive events are `sensitivity-reviewed`, with the owner's sign-off recorded in commit
  [25d2ef6](https://github.com/eyalgolan/statecraft-israel/commit/25d2ef6004436668b42e289aff3dce06af128e09) (see above).
