# Decision videos: implementation plan

Date: 2026-10-13. Branch `ui/decision-card-centred` from `origin/main` `8cc4776`, worktree `.worktrees/decision-videos`.

Design of record: `reports/Video at decision moments.md` (its recommendation, the "Full plan", chosen by the owner on
2026-10-13). Inputs: the repo audit (`video-integration-proposal.md`, session scratchpad) and
`research_notes/Video at decision moments/unity_webgl_video.md`.

## Owner rulings this plan implements (2026-10-13)

1. Centred, large decision card, up to about 1040 px, with a 16:9 media header.
2. The archival photograph comes first. An "Illustration" switch shows a silent 4–8 s AI loop, labelled
   "AI-generated illustration", on about 12 events.
3. Phase 2: a 3–4 s skippable outcome clip, for the historical option only, played at End Turn.
4. Deport never gets any video.
5. The Ben-Gurion recording on the start screen is a separate PR (PR 4 below). The ISA file has arrived.
6. AI clip rights: "no need to check, just use them". This goes in `docs/oss/DECISIONS.md` (PR 1).

## Constraints, checked against the code

- **The sim never sees video.** The mapping lives in `unity/Assets/UI/EventVideos/event-videos.yaml`, which only
  Unity reads. The clips live in `unity/Assets/StreamingAssets/video/`. Nothing goes under `content/`. The goldens must
  not move: 1950 `71e9904d`, 1952 `62f8ec61`, smoke `d3fdd459`.
- **The smoke run doesn't depend on layout.** It presses `event-options`' first `Button` with a
  `NavigationSubmitEvent` (`GameRoot.cs:376-381`). It doesn't depend on where the card is or which buttons sit in the
  header, as long as the options stay inside `#event-options` and stay first in it.
- **Automated runs skip the start screen** through `StartScreenPresenter.Skipped`, which reads smoke, `golden`, `shot`,
  `vprofile`, `profile`, `units`, `borderproof` and `nostart`. Video-off uses the same automated flags but not
  `nostart`, because that one is manual.
- **#409 `ScreenNav.DisableCovered`** disables the panels that a root class hides (`top-bar`, `right-column`,
  `info-panel`). Today the card sits inside `right-column` (`Game.uxml:80-110`, `Statecraft.uss:166`, 480 px wide).
- **The card's photographs are 9:4 crops at 720×320** (`tools/event_images.py` `ASPECT`, `WIDTH`; 24 rows in
  `event-images.yaml`). A 16:9 header needs new crops from the originals, at up to 960×540.
- **The Hebrew UI is not loaded by the game** (`strings.he.yaml` header; #193). Event text is English content.
- **#401 (HUD PR A)** edits `Game.uxml` (UiFrame `filigree` lines, including the event card's at line 99, and the info
  panel) and `Statecraft.uss` (the info panel). This plan doesn't change any info-panel or gilt-trim rule. The only
  overlap is the event card's UiFrame line, which moves with the card: whichever PR merges second takes the other's
  one-line change.
- **WebGL video** must come from a URL in StreamingAssets, with one VideoPlayer, a RenderTexture and
  `Background.FromRenderTexture`, all unverified in this project until the spike. `com.unity.modules.video` is
  already in `manifest.json`.

## Decisions taken in this plan (rank 10, with the devil's advocate)

| # | Decision | Devil's advocate | Why it stands |
|---|---|---|---|
| A | The card leaves `right-column` for a new full-height layer, `event-layer`, under the bar. The minimap keeps the column. | Moving the element conflicts with #401's one-line UiFrame change. | Centring a child of a 480 px right-hand column needs negative offsets that depend on the window size. A sibling layer is the plain way to do it, and the conflict is one line. |
| B | While a screen (Cabinet, Budget, Defence, Why) is open, the card is hidden and disabled. It comes back when the screen closes. | Today a player can read the Cabinet beside an open event. | A centred card and a centre screen overlap at every window size. This is #210's existing two-row rule, extended to every tier, and it goes in `DisableCovered`, so nothing re-enables a covered panel. |
| C | Video is opt-in per card: the photograph shows first, and the switch starts the loop. There is no autoplay, so there is no separate pause button and no reduced-motion jslib. | WCAG 2.2.2 asks for pause, stop or hide on motion longer than 5 s. | 2.2.2 covers content that starts automatically. Here the player's own switch starts the loop and the same switch stops it. |
| D | The mapping is a YAML TextAsset that `GameRoot` takes as a serialized field, the same way it takes `strings.en.yaml`, parsed at start by `EventVideoMapping` with YamlDotNet. There is no editor importer and no ScriptableObject. | The photos use an importer and a ScriptableObject (`EventImageImport`). | The photos need the importer to reference textures. Clips are URLs, so they don't, and the importer would be an abstraction nobody asked for. An EditMode test checks the rows instead: each event exists, each file exists, and no row names a Deport option. |
| E | Encoding is pinned in `tools/encode_event_videos.py`: H.264 High, yuv420p, 960×540, 24 fps, CRF 28, `-g 48`, `-an`, `+faststart`. It refuses any output that has an audio stream. | 720p is sharper. | The header is about 960 px wide at most at the 1080p reference. 540p is about 40% smaller (audit §4.3). |
| F | Video is forced off by `EventVideo.ForcedOff(url, args, webgl)`: smoke > 0, `golden`, `shot`, `vprofile`, `profile`, `units`, `borderproof`. When it is off, the switch is not shown at all, so captures and `?smoke` see the same card as PR 1. | The switch could be shown greyed out instead. | Not shown is the deterministic choice: no element to focus, nothing to measure. |
| G | The fallback: if a clip is missing, fails (`errorReceived`) or isn't prepared within 8 s, the header goes back to the photograph and the switch hides for that event. | It could retry. | A silent retry hides errors. The photograph is the source of truth. |

## Questions for you before code (each ranked below 10, so I won't act on them alone)

1. **Modal or not (9/10).** At 1920×1080 a 1040 px card with a full 16:9 header reaches from the bar nearly to the
   bottom of the window, and it overlaps the info panel (x 440–1480 against x 16–536). I propose:
   - while the card is open, a dim scrim under the bar blocks map clicks, and the info panel and the right-hand column
     are disabled through `DisableCovered`;
   - a "Hide" button on the card collapses it to a small "Decision waiting" button at the top right under the bar, which
     reopens it;
   - the card reopens by itself when the shown event changes.

   Without the collapse, a player couldn't look at the map or inspect a hex while any decision is pending, and that is a
   regression. The alternative is no scrim, and the card overlapping the map and the info panel.
2. **The illustration switch persists for the session (8/10).** I propose that once the player picks Illustration, the
   following cards open on their clip (when they have one) until the player switches back. It isn't saved, and every
   launch starts on the photograph. The alternative resets to the photograph on every card.
3. **A burned-in bug on the clips (8/10).** The report's table asks for a small burned-in bug as well as the UI label,
   because StreamingAssets files can be downloaded and passed around. This machine's ffmpeg has no `drawtext` and
   Pillow has no raqm. So I'd render "AI-generated · נוצר בבינה מלאכותית" once, as a transparent PNG, in headless
   Chrome (proper bidi, Frank Ruhl Libre and EB Garamond, both OFL), commit it with an ASSETS row, and overlay it with
   ffmpeg. The alternatives are English only, or no bug.
4. **ASSETS status for the clips (9/10).** You said "status per the repo's convention, citing that ruling". The
   convention has two statuses: `owner-verify` (waiting for the owner) and `verified` (the owner signed off). Since the
   ruling *is* the owner's sign-off, I'd use `verified`, with the note "Owner ruling 2026-10-13 (DECISIONS.md D-V):
   terms not checked". That stops the clips blocking `--release`. Consequence: the Dreamina clips (A07, A09) would pass
   the release gate for a paid build. `owner-verify` would keep them blocking. Which one?
5. **Sensitive events (9/10).** `ten_days` (J09) and `operation_yoav` (W04a) are `war-casualties`. The audit
   recommended a per-clip sensitivity review. I propose `sensitive: true, sensitivity_reviewed: false` rows that a
   `-shareable` build refuses, mirroring the photo gate in `Build.cs`, and they show normally in dev builds. The
   alternative is to leave those two out until you review them.
6. **The contact sheet tiers and Hebrew (9/10).**
   - The WebGL player is the Mobile tier, and its captures come from headed Chrome.
   - Editor renders show no UI (`MapCapture.Promo`), and a macOS player can't be driven by CDP. So the PC tier would be
     an *uncommitted* editor capture that renders the MapCapture scene at quality level 1 and composites the UI document
     through `PanelSettings.targetTexture`. It is deleted after use.
   - "HE" can only mean the card rendered with `strings.he.yaml`, so that just the new labels are Hebrew. The full RTL
     card waits for #193.

   Is that what you want?
7. **The photographs grow (9/10).** New 16:9 crops at up to 960×540 cost about 4× the pixels of 720×320 in `.data`, and
   their GPU memory goes from about 2.9 MB to about 6.5 MB (DXT1 crunched). I'll measure the `.data.br` change in PR 1 and
   report it. A 16:9 crop shows more of each picture than the 9:4 one, so the five sensitivity-reviewed photographs
   (altalena, ten_days, operation_yoav, operation_hiram, bernadotte_assassination) are flagged on the contact sheet for
   a look.

## Answers (coordinator, 2026-10-13; plan approved)

1. Modal: yes. A dim backdrop blocks map clicks, and the info panel and the minimap are disabled while the card is open.
   Hide collapses the card to "Decision waiting" (EN and HE), which reopens it, and a new event reopens it by itself.
   Hide and reopen go in the focus tests.
2. The Illustration switch is remembered for the session, and is not saved.
3. Burned-in bug: yes, the bilingual PNG from headless Chrome, overlaid with ffmpeg. It is small, in a corner, inside the
   safe area.
4. Clip rows: `verified`, citing D-V. D-V says that it covers commercial and paid builds and that the owner accepts the
   Dreamina restriction.
5. J09 and W04a: sensitive and not reviewed, so the gate refuses them until the owner reviews them.
6. Contact sheet as planned, with the HE limitation (#193) labelled on the sheet.
7. Bigger photographs: fine. Report the `.data` change, and flag the sensitivity-reviewed photographs whose new crop shows
   more.
8. The WAV goes through Unity's AudioClip import with no processing (no normalising, no force to mono). The ISA licence
   allows technical format changes. The sha256 test on the source WAV stays.

## PR 1: the centred card, photographs only, plus the DECISIONS ruling

Branch `ui/decision-card-centred`. The issue to claim: open "Decision cards: centred, large, 16:9 media header (video
phase 0)" (`Refs #106, #41`) and comment the branch on it.

**Files**
- `unity/Assets/UI/Game.uxml`:
  - move `event-card` out of `right-column` into a new `event-layer` (after `info-panel`, before the screens), with
    the card's existing inner block unchanged, except that `event-photo` becomes `event-media` (16:9);
  - add the `event-hide` button and the `event-reopen` button (question 1).
- `unity/Assets/UI/Statecraft.uss`:
  - `.event-layer`: absolute; left, right and bottom 16; `top` per HUD tier (112, 140, 232, 328, the same as
    `right-column`);
  - `.event-card`: `width: 1040px; max-width: 100%`, centred, `max-height: 100%`;
  - `.event-media`: a 16:9 height, set by the presenter from the card's width, since USS has no aspect-ratio property.
    It is still the first to shrink on short windows, as now;
  - the tier, `screen-open` and `start-open` rules for the layer;
  - `.event-open .toast` at the top centre over the media header.

  No rule for `.info-panel`, `UiFrame` or the gilt trim is changed.
- `EventCardPresenter.cs`:
  - the media header's height (width × 9/16, capped by the room left);
  - the collapse and reopen;
  - card focus when a new event opens (the card itself, not an option, so no choice looks recommended; Tab reaches the options);
  - `RoomForDroppedPhoto` reads the layer instead of `right-gap`.

  The drop and restore logic (`MinPhotoHeight`, the CC BY credit never cut) stays as it is.
- `ScreenNav.cs` (`DisableCovered`): `event-layer` is disabled under `start-open` and `screen-open`. While the card is
  open (`event-open`), `right-column` and `info-panel` are disabled.
- `HudPresenter.cs`: nothing, because the tier classes already exist.
- `tools/event_images.py`: `ASPECT = 16/9`, `WIDTH = 960`, and `MAX_BYTES` re-checked.
- `event-images.yaml`: the `crop` values reviewed for 16:9 and the comment updated.
- The 24 JPGs remade from the originals, fetched into an empty folder outside the repo with a project User-Agent, no
  owner identifiers, and their SHA-1 checked by the tool.
- `EventImageImport.MaxEdge` stays 1024.
- `ASSETS.yaml`: the crop text in each photo row's note (licences unchanged).
- `docs/event-images.md`.
- `EventImageCatalog.cs`: the memory comment.
- `strings.en.yaml` and `strings.he.yaml`: `event.hide`, `event.reopen`.
- `docs/oss/DECISIONS.md`, a new ticked line (moved to its own docs PR, #449, after review):
  > - [x] D-V AI video clips in the game: Veo 3.1 (through ElevenLabs) and Seedance 2.0 (through Dreamina).
  > Decided (owner, 2026-10-13): "no need to check, just use them". D-D found that Dreamina's terms restrict commercial
  > use, and D5 covered images only; the owner accepts this.
- `CHANGELOG.md`, Unreleased.

**Tests (EditMode, written first)**
- `UiLayoutTests`, extending the existing tests, plus `DecisionCardLayoutTests`. At every `PromoHudTests` shape
  (1920×1080, 1280×720, 1366×768, 1760×1000, 1920×1200, 1440×900, 1024×768, 1280×1024, 1024×1024, 960×1080, 800×1000),
  plus 390×844, 960×600 and 2560×1080, for every real event:
  - the card is centred (|left gap − right gap| ≤ 1 px), at most 1040 logical px wide, inside the layer, and below the
    bar at every tier, the narrow two-row and three-row tiers included;
  - the media header is 16:9 within 1 px when the window has room, and the photograph is shown whole with its credit or
    dropped, never cut (the current rules, re-pointed);
  - the options are inside the card and their text fits;
  - no window shape at least as tall as 1366×657 scrolls the body.
- **Focus and #409:**
  - with the card open, `info-panel` and `right-column` buttons are disabled and Tab can't reach them;
  - collapsing the card enables them again;
  - with a screen open (every tier), the card is hidden and disabled, and closing the screen restores it;
  - under the start screen the layer is disabled;
  - a new event puts the focus on the card (no option lit), and an event that is already open does not steal the focus from
    the top bar.
- **Collapse:** Hide, then reopen; a new shown event reopens the card.
- **The existing tests still pass:** `EventCardTests`, `TheTwoRowHudKeepsWhatAScreenCoversOutOfReach`,
  `AWideScreenHidesTheInfoPanelItCovers`, `PromoHudTests.TheToastIsOpaqueAndClearOfTheScreens` (updated for the toast
  over the card), and `SimSmokeTests`.
- `tools/tests/test_event_images.py`: the 16:9 box, and the size cap.

**Verification**
- `dotnet test sim/Statecraft.sln`, the tools pytest, `scripts/oss` pytest and `check_repo.py`.
- The full EditMode suite under `machine_lock --shared --wait-unity`, with 0 `warning CS`.
- A WebGL build, then `web_check.py --query "golden&smoke=5"` with the three values.
- The `.data.br` and `.wasm.br` sizes against main, built on the same machine.
- `headed.py boot --cpu 4 --runs 5` against main: the map build doesn't change, and the first frame is reported.
- Then revert the `ProjectSettings`, `Assets/Settings` and `UNITY_MCP` churn.
- Contact sheet to you **before the push**:
  - EN and HE label sets;
  - Mobile (WebGL, headed Chrome) and PC (editor composite);
  - 1920×1080, 1440×900, 1024×1024 (two-row), 390×844 (three-row), and 2560×1080;
  - a photograph event, a portrait-only event, and a long event;
  - the collapse state;
  - the 24 new 16:9 crops on a separate sheet, the sensitive ones marked.

## PR 2: the WebGL spike, then the clips and the Illustration switch

Branch `ui/decision-card-video`, from main after PR 1 merges, or stacked on it if you say so.

**Step 1, the spike (measure before building the rest).** One clip (L14), a minimal `CardVideo`, and one event.
- `.wasm.br`, `.data.br` and `framework.js` sizes against PR 1's build. Is the video module stripped today? The spike
  shows that.
- `headed.py boot --cpu 4 --runs 5`, interleaved with the PR 1 build: map-build median and range, first frame. The
  #321 margin is about 100 ms over 2620 ms against the 2720 ms budget, and a regression beyond noise is a stop. No
  clip is requested before the map is built: the player is created lazily, on the first switch press.
- Playback in headed Chrome (ANGLE Metal):
  - after pressing the switch, two screenshots 1 s apart differ inside the header, and nowhere else;
  - the console has no errors;
  - `Background.FromRenderTexture` updates without `MarkDirtyRepaint` (if it doesn't, repaint on `frameReady`);
  - a renamed clip falls back to the photograph within 8 s;
  - a frame-time sample with the loop running and with it off (`?vprofile` isn't usable here, so CDP
    `Performance.getMetrics`).
- Firefox and Safari aren't on this machine's automation path. I'll note them as untested unless you want a manual
  check.

The numbers go to you; then step 2.

**Step 2, files**
- `unity/Assets/UI/EventVideos/event-videos.yaml`. One row per event: `event`, `file`, `source` (the promo shot and
  its sha256), `generator`, `generated` (date), `alt`, `sensitive`, `sensitivity_reviewed`.
- `unity/Assets/StreamingAssets/video/<clip>.mp4`, 12 clips:

  | Event | Shot | Source |
  |---|---|---|
  | latrun_proposal | L14 | demo2 v2, Veo 3.1 |
  | first_truce | K03a | demo3, Veo 3.1 |
  | jerusalem_ceasefire | K03b | demo3, Veo 3.1 |
  | operation_yoav | W04a | demo3, Veo 3.1; sensitive |
  | maabarot_policy | W05a | demo3, Veo 3.1 |
  | frumkin_commission | W05b | demo3, Veo 3.1 |
  | law_of_return | K04a | demo3, Veo 3.1 |
  | hula_drainage | W06 | demo3, Veo 3.1 |
  | austerity_1949 | A09 | demo2 v1, Seedance 2.0 / Dreamina; audio stripped |
  | iraqi_exodus | A07 | demo2 v1, Seedance 2.0 / Dreamina; audio stripped |
  | ten_days | J09 | demo2 v2, Veo 3.1; sensitive |
  | jerusalem_military_government | JQ18 | demo2 v2, Veo 3.1; generic alt, because it shows May |

  - Not used: H01, A27 (Altalena), any demo 1 file, any finished cut, any audio.
  - Each source is copied out of `promo/` (read-only) and encoded by the script. Every clip gets a frame contact sheet
    checked for recognisable faces before it is committed.
  - About 8–9 MB in all (audit §4.3).
- `tools/encode_event_videos.py`:
  - pinned arguments;
  - an `ffprobe` check of each output: H.264, 960×540, no audio stream, `moov` before `mdat`;
  - an optional bug overlay (question 3).
- `EventVideoMapping.cs` (parse and `Problems`), and `EventVideo.cs` (`ForcedOff`, URL).
- `CardVideo.cs`: one lazy `VideoPlayer` (URL source, RenderTexture target, `audioOutputMode None`, looping), one
  reused 960×540 RenderTexture released when the card closes or collapses, and `errorReceived` and the timeout leading
  to the fallback.
- `EventCardPresenter.cs`: the Photograph | Illustration switch in the header's corner, and the "AI-generated
  illustration" strip in place of the credit while the clip shows. Focus order: options, then Hide, then the switch.
- `GameRoot.cs`: the serialized TextAsset, and `ForcedOff` passed in.
- `Build.cs`: `-shareable` refuses sensitive unreviewed clip rows (question 5).
- `strings.{en,he}.yaml`: `event.photo`, `event.illustration`, `event.ai_label`.
- `ASSETS.yaml`: one row per clip, with `ai_generated: true`, the generator, the generation date, the source shot and
  its sha256, the encode recipe, and the status per question 4, citing D-V. Plus a row for the bug PNG if question 3
  goes ahead.
- `docs/ai-disclosure.md` regenerated, the README "AI use" line, `docs/event-videos.md`, `CHANGELOG.md`.

**Tests (EditMode)**
- Mapping:
  - every row's event exists in `content/` and its file exists in StreamingAssets;
  - no duplicate events;
  - no row names an event with an option whose id is `deport` (the Deport guard, ahead of GDD Phase 6);
  - `ai_generated` is required.
- `ForcedOff` is true for each automated flag (WebGL URL and desktop args) and false for none or `nostart`. With video
  forced off, the switch isn't in the tree and the card matches PR 1's.
- The switch appears only for events with a clip. Pressing it shows the label and hides the credit. Pressing it again
  restores the photograph and the credit.
- Fallback: a missing file, or a simulated `errorReceived`, puts the photograph back, hides the switch for that event
  and logs once.
- Focus order: options, then Hide, then the switch.
- Content hash and goldens: `SimSmokeTests` and the WebGL `golden&smoke=5` are unchanged, and a test asserts that
  `event-videos.yaml` isn't under `Paths.Content`.
- Layout: every PR 1 layout test, with the switch present.

**Verification:** as in PR 1, plus the spike numbers repeated on the final build, and an itch-style relative-path check
(`web_check` serves the build from a subdirectory). Then a contact sheet and the measurements to you before the push:
- the 12 clips' mid frames;
- the switch states, in EN and HE;
- the fallback;
- Mobile and PC.

## PR 3 (later, after your review of PR 2): outcome clips at End Turn

Only for the historical option (`historical: true`) of a mapped event, and only when that option was the one queued.
Played at End Turn as "this turn's orders": 3–4 s, skippable by click, Esc, Enter or Space, with video forced off in
automated runs. Deport never gets a clip, and the mapping test refuses it. Candidates from the audit:

| Event | Option | Shot |
|---|---|---|
| first_truce | accept | A02 |
| operation_horev | launch | A05 |
| maabarot_policy | maabarot | A08 |
| hula_drainage | continue_works | W08 |

There is no A27. The detailed plan comes after PR 2.

## PR 4 (separate, start screen): Ben-Gurion's Declaration recording

The input is `promo/demo2/audio/archival/BG-1948-ORIGINAL_isa-00113hj.wav`:
- 9.29 s, sha256 `2b949e12f1d33d6f…`;
- Israel State Archives item 00113hj (טס-169/21), rights field «גנזך המדינה»;
- provenance and the licence quotes are in the session scratchpad, `bg-audio/isa/*.info.json`.

Branch `ui/start-screen-declaration-audio`, which plays independently of the video PRs. I'll plan it in full when it
starts. The outline:

- **The file is not altered.** No trim, fade, denoise or loudness change.
  - Copy it byte for byte to `unity/Assets/Audio/declaration-1948-isa-00113hj.wav`.
  - *Superseded (review of #465):* that WAV was itself an ffmpeg decode, so "byte for byte" could not be claimed. What
    ships is a stream-copy cut of the archive's upload, decoded to PCM WAV with no other processing, fetched from
    StreamingAssets; the chain is in `docs/declaration-recording.md`.
  - Import it as an AudioClip with **PCM** compression, so Unity doesn't transcode it. The cost of PCM is about 9.29 s ×
    rate × channels × 2 bytes; I'll measure it.
  - Check: the imported clip's sample count and rate match the WAV.
  - I'll ask you whether Unity's container conversion counts as "alteration". If it does, the fallback is the raw file
    in StreamingAssets played through a jslib `<audio>` element.
- **It plays only after a user gesture.** On the start screen, a "Play the recording" button (a click, Enter or a tap)
  starts it. It never autoplays and never plays on Continue or New Game by surprise. The button switches to "Stop".
  `ForcedOff` (PR 2) applies: automated runs skip the start screen anyway. It is muted under the existing skip flags.
- **EN and HE captions in UI Toolkit:**
  - Hebrew in Frank Ruhl Libre through the advanced text generator, `language-direction="RTL"`, as `start-he` already
    does;
  - English is the official translation, the same text as the panel;
  - the cue times come from listening, and live in a small Unity-side `declaration-captions.yaml`;
  - `StartScreenTests` renders both and checks the RTL order.
- **The credit line on screen:** "Recording: Israel State Archives", beside the existing "Painting: AI-generated." It
  plays over the still painting only, never with H01 or any moving image.
- **Provenance:**
  - an `ASSETS.yaml` row with the licence "Israel State Archives terms of use". That value isn't in
    `scripts/oss/oss_policy.py` `ALLOWED_LICENCES`, so the PR adds it, as its own reviewed line;
  - status `owner-verify`;
  - the note records the recordist's 1980s claim (ע"א 474/86): moot in Israel since 1998, US status uncertain;
  - `NOTICE` carries the credit.
- **Tests:**
  - the button is the only thing that starts playback;
  - the credit is present;
  - the caption text matches `docs/sources/declaration-1948`;
  - the sha256 of the shipped WAV equals the source's (the no-alteration guard);
  - the goldens and smoke stay unchanged.
- **Audio-module check:** this is the game's first `AudioSource`. I'll measure the `.wasm` change and boot at `--cpu 4`,
  as in the PR 2 spike.

## Machine and repo rules

- Every Unity run goes through `uv run tools/machine_lock.py --shared|--exclusive --wait-unity --timeout 3000`, without
  `-nographics`, and with `-CI false -GITHUB_ACTIONS false -TF_BUILD false`. Afterwards I revert `ProjectSettings`,
  `Assets/Settings` and the `UNITY_MCP` defines.
- No AppleScript, `pkill` or `killall`, and any stop is by PID only.
- `promo/` is read only, and `promo/demo/` is never touched.
- One PR per phase. I claim the issue and never merge.
- No owner email or identifiers in any web request: the Commons original downloads use the project User-Agent only.
