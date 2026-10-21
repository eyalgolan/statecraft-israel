# M3 release: the browser build

**Status:** M3 ships as the local browser build. The itch.io upload and the cross-upload save test are deferred to the
start of M4, as a private Draft (owner ruling, 2026-10-10); their sections below are kept, marked "Deferred to M4".

## What ships
- **Folder and zip:** `unity/Builds/Web-gzip/` uses gzip compression with no decompression fallback (it was Brotli until the owner's decision on #879, 17 October 2026: "gzip for both"; `docs/RELEASING.md`, step 5, says why). It is zipped as `unity/Builds/statecraft-m3-web.zip`, with `index.html` at the zip root. Both are build output and are not committed.
- **File names:** the files under `Build/` are named by their content hash (issue #437), for example
  `Build/814d5f9032ce4b5847cf75e492700bbb.loader.js`, so a browser that cached an earlier build cannot mix its files into
  this one. The build's `index.html` names them; nothing else may assume a name. `index.html` keeps its fixed name and
  a browser may show a cached copy of it (no host this project uses is known to forbid that), so the page asks the
  server for its current copy (`fetch` with `cache: "reload"`, which also stores that copy for the next visit) and loads the files that copy names. The StreamingAssets
  files (`content/`, `map/`, `video/` and the start screen's recording) keep their names; the player adds the build's
  id to every URL it fetches them by (`?b=<Application.buildGUID>`,
  `GameInputs.WithBuildId`), so a browser cannot serve one build's cached content to another.
- **Built by:** `"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression gzip -shareable -logFile "$PWD/unity-build-web-gz.log"`, then `python3 tools/package_release.py` (it zips the folder, `index.html` at the root, and refuses while any sensitive event is not sensitivity-reviewed, and when the build's `event-photos.json` is missing, says the build was not `-shareable`, or lists an unreviewed sensitive event photograph). Never zip by hand.
- **After building:** drop the build's rewrites of the settings with
  `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset` (as in `dev-setup.md`), so no
  build-cache change gets committed.
- **Verified locally by:** `uv run tools/web_check.py unity/Builds/Web-gzip --query "golden&smoke=5"` with the three
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
- **Play it locally:** serve the folder with web_check's handler, which sends `Content-Encoding: gzip` for `*.gz` (and `br` for `*.br`). Plain `python3 -m http.server` does not, so a compressed build would not load. Then open http://localhost:8000/.
  ```
  uv run --python 3.12 python -c "import sys,functools,http.server; sys.path.insert(0,'tools'); import web_check as w; http.server.ThreadingHTTPServer(('127.0.0.1',8000), functools.partial(w.Handler, directory='unity/Builds/Web-gzip')).serve_forever()"
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

Firefox is not installed on the development Mac. A Firefox result from a contributor, and the WebKit check, are in the
next section.

## Other browsers (#43), 2026-10-14
Build: main `04bf7cb`, WebGL, `-compression disabled`, served with web_check's handler on `http://localhost`. Mac: Apple
M5 Pro, macOS 27.0.1. Chromium ran through `tools/web_check.py` as it is. WebKit and Firefox are Playwright's builds
(Playwright 1.64.0, from `~/Library/Caches/ms-playwright`): Playwright's WebKit is the engine Safari uses, not Safari
itself, and its Firefox is a patched Firefox. Chromium in the headed run is the same Chrome for Testing, driven by
Playwright. The headed run is scripted: it starts a new game, zooms with the wheel, orbits with Q, plays four
turns answering each event with its first option, opens Cabinet, Budget, Defence and Why? and closes each with Escape,
turns on the First Truce card's Illustration, then reloads the page and restarts the browser on the same profile.

| | Chrome for Testing 149.0.7827.55 | WebKit 27.2 (Playwright) | Firefox 157.0 (Playwright) |
|---|---|---|---|
| `golden&smoke=5` | PASS: both goldens and the smoke value from `docs/determinism.md`. 11 s for the whole `web_check` run; in a Playwright run of the same check the goldens arrive 5.5 s after the page opens, the smoke line at 9.3 s | PASS, headless. Goldens at 10.3 s, smoke at 19.3 s | Not run: the browser does not start on macOS 27 (below) |
| WebGL renderer | ANGLE Metal, Apple M5 Pro | "Apple GPU" | - |
| Start screen, map, HUD, panels, fonts | Correct | Same as Chromium; the screenshots match apart from the small difference in camera angle left by the Q orbit | - |
| Hebrew lines on the start screen | Right to left, correct | Same (Unity draws the text, so the browser does not lay it out) | - |
| Wheel, one notch (100 px) | 120000 m to 108000 m (10%) | Same | - |
| Wheel over a panel | No zoom | No zoom | - |
| Q held 0.6 s | Orbits to yaw 323.5 | Orbits to yaw 323.7 | - |
| Mouse clicks: New Game, End Turn, options, the four screens, Continue | All work | All work | - |
| Event card video (`first_truce.mp4`) | `VIDEO playing`, the clip shows | `VIDEO playing`, the clip shows | - |
| Audio | The game has no sound (the card clips are silent), so there is nothing to play. Chrome warns "The AudioContext was not allowed to start" until the first click | No warning | - |
| Save: four turns, then reload, then Continue | Same date, turn and hash (turn 4, 1948-06-12) | Same | - |
| Save: browser closed and restarted on the same profile | Same date, turn and hash | Same | - |
| IndexedDB databases | `/idbfs`, `UnityCache` | `/idbfs`, `UnityCache` | - |
| Console warnings | Six "WebGL: INVALID_ENUM: getInternalformatParameter" per load; the AudioContext warning above | One "WebGL: non-portable extension enabled: WEBGL_polygon_mode" per load | - |
| Console errors | None | None | - |

Contributor result (Chaiky3, in #43): **Firefox 157.0, Windows 11 at 125% scale, main `fa10633`: pass.** It loads and
renders, the start screen's Hebrew shows, a touchpad zoom step is 10%, and Continue after a reload returns to the same
turn. Without a hard refresh Firefox mixed cached files from an older build ("too much recursion"); that is #437.

Differences and notes:
- **Firefox does not start here.** Playwright's Firefox 157.0 build exits with "Could not find profile folder" headless,
  and fails to start headed, on macOS 27.0.1, so this Mac has no Firefox result. The Windows result above is the Firefox
  evidence so far.
- **Wheel in lines: no bug (T41).** The first run had three lines (`deltaMode` 1, sent to the canvas by script) zoom
  0.16% in Chrome and 0.32% in WebKit instead of 10%. That came from the order of the test, not from the line mode.
  Unity's web player hands the browser's raw `deltaY` and `deltaMode` to its engine code (the wheel handler in
  `*.framework.js` copies both), and the engine counts the smallest wheel delta it has seen since the page loaded as one
  notch, whatever the mode. C# sees only the result in `Mouse.scroll`; the Input System package only turns the engine's
  scaling on or off (`scrollDeltaBehavior`, left at `UniformAcrossAllPlatforms`). Measured on the same build, each case
  from a fresh page, events sent to the canvas by script, the same in Chrome and WebKit:

  | Wheel events, in order | Zoom from each, in notches (one notch is 10%) |
  |---|---|
  | 3 lines | 1 |
  | 1 line, or 1 page, or 3 px | 1 |
  | 3 lines, 3 lines (also 10 s apart) | 1, 1 |
  | 100 px, 3 lines | 1, 0.03 |
  | 3 lines, 100 px | 1, 3 (33, clamped) |
  | 100 px, 50 px, 100 px | 1, 0.5, 2 |
  | 100 px, 200 px, 100 px | 1, 2, 1 |
  | 0.5 px, 4 px, 100 px | 1, 3, 3 (8 and 200, clamped) |

  So a mouse that always sends three lines a notch zooms 10% a notch, as one that sends 100 px does. The earlier run
  sent a 100 px notch first (in Chrome, Playwright's 100 px notch counts as 200: a 100 px event after it zooms half a
  notch), so the three lines after it counted as 3/100 (3/200 in Chrome). Nothing in the game's code was changed for
  this.

  What the measurements did show is a different risk: one small delta sets the notch for the rest of the session.
  **Fixed (T45).** A scripted swipe of 31 events, 0.5 to 8 px about 16 ms apart (108 px in all, a trackpad's ramp and
  momentum), zoomed 95% (28 notches, three a frame, stopped by the 6 km limit) in Chrome and WebKit, on its own or after a
  mouse notch; after it, one 100 px notch out zoomed three notches. The camera now counts the browser's wheel events
  itself (`WheelInput.jslib` queues each event's `deltaY` and `deltaMode`, and `CameraController.WheelEventNotches`
  scales them):
  - a pixel event of 50 px or more is one notch, whatever its size. A mouse sends one event a notch, but its size
    varies: 100 px in Chrome and Edge on Windows, more with display scaling (125 or 150 px at 125% or 150%, if Chrome
    scales it; not measured), less at a page zoom above 100% (80 px at 125%), and 200 px from Chrome's synthetic
    wheel (Playwright and CDP send 100 and the page sees 200, at `devicePixelRatio` 1). The threshold is half of
    Chrome's 100 px notch, so a page zoom up to 200% still gives a notch;
  - a whole number of macOS mouse ticks (4.000244140625 px, what Chrome and Safari send on a Mac; an accelerated tick
    is several ticks' worth) is one notch;
  - three lines, or one page, is one notch;
  - smaller pixel events, as touchpads send, count 100 px to the notch. The measured swipe's events are 0.5 to 8 px.
    There is no documented ceiling for a real trackpad's events, and a fast flick can send events of tens of pixels or
    more; one of 50 px or more counts as one notch, so an event between 50 and 100 px zooms up to twice what its length
    would, and a bigger one less. Each frame is still clamped to three notches.

  The same swipe now zooms 1.08 notches (10.8%) over 30 or 31 frames, never more than 0.15 in a frame, and a notch of
  100, 150 or 200 px or three lines, before or after it, zooms 10%. `uv run tools/headed.py wheel` repeats these cases
  in Chrome, and `pytest tools/tests/test_headed.py --headed` checks them, including Chrome's own 100 px notch.

  Firefox 88 and later report a mouse notch in lines only to a script that reads `deltaMode` before `deltaY`, and in
  pixels (three line heights) otherwise; Unity's own handler reads `deltaY` first. So the listener sits on `window` in
  the capture phase, ahead of Unity's handler, and reads the mode first; Firefox then keeps the notch in lines for
  both. That listener is what handles Firefox; the 50 px rule is only a backstop in case a notch still arrives in
  pixels. Not measured: Playwright's Firefox does not start on this Mac (headless: "Failed to launch the browser
  process"; headed and with a persistent profile: no window within 60 s). Were the notch to arrive in pixels after
  all, three line heights of 50 px or more would still be one notch. With a default font of 14 px or less, three line
  heights come to about 48 px, so such a notch would zoom about 0.48 of a step: too little, never too much.

  Known limits: a mouse notch below 50 px that is not a whole macOS tick counts in proportion, for instance a Mac mouse
  tick at 110% page zoom (3.64 px, 3.6%), a notch at a page zoom of 200% or more, or a Firefox notch in pixels with a
  default font of 14 px or less (about 48 px, 0.48 of a step). Still to be tried by hand: a Mac
  trackpad (including fast flicks) and a Mac mouse in Chrome and Safari, a mouse in Firefox (Windows, Linux, Mac),
  Chrome or Edge on Windows at 125% and 150% display scale (one notch), and a mouse on Linux.
- **Aborted requests in Chrome.** At each start Chrome lists about 30 of the 51 content files (and the map files) as
  `net::ERR_ABORTED` in its network log. The files arrive whole: both browsers log the same `BOOT OK content` hash and
  the goldens pass. WebKit lists none. Not a console error, so it is left as it is.
- **Known warnings, nothing to fix.** On every load Chrome logs six "WebGL: INVALID_ENUM: getInternalformatParameter"
  warnings and, until the first click, "The AudioContext was not allowed to start"; WebKit logs one "WebGL: non-portable
  extension enabled: WEBGL_polygon_mode". With the aborted requests above, these are the expected console noise of this
  build: none is an error, and the game runs the same with them.
- Input in these runs is synthetic (Playwright's mouse, wheel and keys), not a physical mouse or touchpad.

Still open: Safari itself (only its engine was run here), Firefox on a Mac, Chrome and Edge on Windows, a real mouse wheel
in Firefox, a real touchpad and a Mac mouse (above), and the two-upload itch.io save test below. Each needs the owner's
machines or the project's itch.io account.

## OWNER STEP: sensitivity review before any shared build
Spec section 5.5 requires a recorded sensitivity review for events with sensitive tags (holocaust, terror, massacre,
war-casualties). The owner's sign-off for all seven events below is recorded in commit
[25d2ef6](https://github.com/eyalgolan/statecraft-israel/commit/25d2ef6004436668b42e289aff3dce06af128e09)
(2026-10-10; decision D11 in [docs/oss/DECISIONS.md](oss/DECISIONS.md)): each event's YAML `status:` is now
`sensitivity-reviewed`. For any new sensitive event, the owner records the sign-off **only** by moving its YAML
`status:` to `sensitivity-reviewed` and committing it (the loader accepts that value). Nothing else counts as a sign-off.

**Re-review, 2026-10-15.** The event re-timing for monthly turns (#577) changed the text of `altalena`, `ten_days`,
`operation_hiram` and `bernadotte_assassination`; they went back to `fact-checked` until the owner re-reviewed them
("Approve all 4", reviewer: the owner, recorded on #600 and in D11), and they are `sensitivity-reviewed` again.

- **Gate:** `Build.Web ... -shareable` and `Build.Mac ... -shareable` refuse to build while any sensitive event is not
  `sensitivity-reviewed`, and name them (`Statecraft.Sim.ReleaseGate.SensitiveUnreviewed`). `tools/package_release.py`
  makes the shareable zip and runs the same check (`Statecraft.Cli --release-gate`) on the content inside the build, so
  it prints `PACKAGE REFUSED ...` and exits 1 while any sensitive event is not sensitivity-reviewed. It also refuses a
  build folder whose `event-photos.json` (written by every build) is missing, not `-shareable`, or lists an unreviewed
  sensitive event photograph (`docs/event-images.md`). Use `-shareable` and the script for every build
  that leaves this machine, including the M4 private Draft once it is shared with anyone. Local dev builds (no flag)
  are not gated, because web_check and the golden checks need them.
- **Alternate-timeline content (R-ALT-011, Q-ALT-9):** `Statecraft.Cli --release-gate`, and so
  `tools/package_release.py` and `scripts/oss/release_gate.sh`, also refuse while any consequence card (`kind:
  consequence`) or any event with `variants` has no `reviewed_by`, naming them on a `RELEASE GATE REFUSED` line
  (`Statecraft.Sim.ReleaseGate.AlternateUnsigned`; the CLI also prints each as `unsigned alternate card: <id>`). The
  sign-off is a person's `reviewed_by`, written only by that person. The Unity `-shareable` build flag checks the sensitive events only.
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

**Map lines (display only; not gated by the release gate).** The 1949 armistice lines and zones
(`content/map/armistice_lines_1949.geojson`, decision D-A) need the same review before a shared build shows them. The owner
records the sign-off by moving each feature's `status` to `sensitivity-reviewed` (after `fact-checked`). The 1948
sub-districts (D-B) are reviewed with them.

| Line or zone | Front, from | What to review |
|---|---|---|
| `egypt_adl_gaza` | Egypt, 24 Feb 1949 | the Gaza-Rafah line, Wadi Hasi to Gaza only (the stretch to the frontier is a gap, shown as nothing); its name |
| `egypt_el_auja_dmz` | Egypt, 24 Feb 1949 | the El Auja Demilitarized Zone and its name |
| `lebanon_adl` | Lebanon, 23 Mar 1949 | the line shown as the Mandate frontier it follows; "the international boundary" in its name |
| `syria_adl` | Syria, 20 Jul 1949 | the line and its name |
| `syria_dmz_north`, `syria_dmz_central`, `syria_dmz_south` | Syria, 20 Jul 1949 | the three parts of the Demilitarized Zone, their names (place names chosen by us: Banias, Tell el Qadi, Dardara, Mishmar HaYarden, Ein Gev, El Hamma; a mix of Arabic forms, Tell el Qadi rather than Dan and El Hamma rather than Hamat Gader, and Hebrew ones) |
| `egypt_mv1950_demarcation`, `egypt_mv1950_zone_lines` | Egypt, 22 Feb 1950 | the line as drawn on the signed modus vivendi map and the zone A and C lines; that the label and panel name the modus vivendi apart from the 1949 line |
| `jordan_adl_iraqi_*` (four stages), `jordan_adl_a_latrun`, `jordan_adl_bc_south`, `jordan_adl_d_arava` | Jordan, 3 Apr 1949 (line A by its deadlines: 8 May, 22 May, 17 Jul 1949) | low accuracy, traced from a low-resolution scan, and labelled so; the Art. VI.3 stage areas as read; Latrun traced along the outer loop; Jerusalem, the no-man's-lands and Mount Scopus left out (gaps) |
| On-screen strings | from 1949 | the map label ("Mandate sub-districts, 1947 · 1949 armistice lines: Egypt (and the 1950 modus vivendi), Lebanon, Jordan (low accuracy), Syria"), the info panel lines ("On the ... · low accuracy, from a low-resolution scan"), the agreement titles in the "Egypt-Israel" style (the agreements' own: "Egyptian-Israeli", "Lebanese-Israeli", "Hashemite Jordan Kingdom-Israel", "Israeli-Syrian"), and the seven Hebrew strings in `strings.he.yaml` (Hebrew is off until reviewed) |

## Deferred to M4: upload to itch.io (OWNER STEP)
Deferred to the start of M4, as a private Draft (owner ruling, 2026-10-10). The steps stay correct for then. This needs the
owner's itch.io account and is never automated, and the build must pass the sensitivity gate above (`-shareable`).
1. Sign in at https://itch.io. Go to Dashboard, then **Create new project**.
2. Set the title to "Statecraft: Israel (M3 preview)", Kind of project to **HTML**, and Classification to Games.
3. Under Uploads, upload the zip made by `bash scripts/oss/package_web.sh unity/Builds/Web-gzip unity/Builds/statecraft-m3-web.zip` (it puts the licence and attribution files inside, then runs `tools/package_release.py`; see `docs/RELEASING.md`) and tick **This file will be played in the browser**.
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

No server settings are needed. itch.io detects gzip from the bytes and sends it with `Content-Encoding: gzip`; it also
detects `.br`, but its CDN decodes Brotli and re-encodes by content type, so a Brotli `.data` reached players
uncompressed (checked with curl on two public Unity games, 17 October 2026). That is why the release build is gzip
(#879); see https://itch.io/docs/creators/html5, "Compression".

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
  4. First open B with a normal load, not a hard reload (issue #437: a returning player's browser has A cached). It must
     start and print `GAME OK`, and DevTools' Network panel must show the `Build/` file names that B's `index.html`
     names, not A's. Record the result, and any `BOOT FAIL` line, as a separate row. Then open B with a hard reload.
     **Pass** only if date, turn and full state hash match the baseline. Copy the complete
     `GAME OK` line, the old `TURN` line and any load warning/error. A date/turn match alone is not a pass. Reload B once
     more and check the same tuple, so a one-time replay is not mistaken for a durable save.
  5. If B starts again on 15 May 1948, or the state hash differs with unchanged content, record fail with both build IDs,
     browser/OS and frame origins. Do not reset or overwrite the save before capturing the evidence. Follow #43's fallback:
     if the save does not survive re-upload, save export/import becomes the next issue. No unrelated account uploads.
  6. Record the result below. This plan is not a completed itch.io test or a Firefox/Safari/Windows runtime check.

**Separate changed-content replay check:** after the unchanged-content control, a build with intentional content changes
may show "This save is from an earlier version of the game, so events may unfold a little differently."
Record that warning and both builds' git commits. Matching date/turn in this case is not evidence of exact state preservation, and it does not
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
| B opened without a hard reload: `GAME OK`, and B's `Build/` names (issue #437) | pending |
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
