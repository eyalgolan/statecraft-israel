# Statecraft: Israel: visual v1 implementation plan (the quick wins)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task by task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **Historical plan.** The code is authoritative; `docs/visual-v1.md` records what shipped. Interfaces lines below that name values later changed carry a "(shipped: ...)" note.

**Goal:** Close the cheapest, largest gaps between the M3 map and the promo art bar (`promo/final/03-gameplay-1936.png`, `02-map-diorama.png`) without touching the simulation: map-scale shadows from a low warm sun, a real two-band tilt-shift, a stylised Terrain Lit Shader Graph fed by baked AO/curvature/sky-view, a coast-graded sea with foam, a colour grade with vignette and aerial/height haze, an anti-aliased shader hex grid, EB Garamond map labels, and a measured WebGL/desktop quality-tier decision.

**Architecture:**
- **Tools (`tools/`):** `terrain_bake.py` turns the committed heightmap into two textures (AO + curvature + sky-view; signed distance to the coast) under `unity/Assets/Textures/Map/`; `terrain_graph.py` writes the terrain Shader Graph from Unity's own "Terrain Simple" template plus one Custom Function node; `art_compare.py` and `art_checkpoint.sh` produce the owner's side-by-side screenshots; `web_check.py` gains `--shots` (headless, at DPR 1); `machine_lock.py` serialises frame-time runs against every other Unity job on the machine (an open editor included); M3's `headed.py` gains `profile` (headed `?vprofile`) and a boot budget (`boot --runs --baseline`).
- **Shaders (`unity/Assets/Shaders/`):** one shared include (`StatecraftMap.hlsl`: globals, hex distance field, water shading, haze) used by the terrain graph's Custom Function (`StatecraftTerrain.hlsl`) and by the sea (`Sea.shader`); a three-pass `TiltShift.shader` driven by a URP render-graph renderer feature.
- **Unity runtime (`unity/Assets/Scripts/Map/`):** small static units with one job each (`MapLighting`, `ShadowFit`, `TiltShift`, `MapGrade`, `MapShaderGlobals`, `MapPalette`, `PromoPose`, `VisualToggles`, `FrameProfile`, `VisualShots`); `CameraRig`, `MapBuilder` and `MapScene` only call them. Labels move to UI Toolkit (`Scripts/UI/MapLabelsPresenter.cs`). No rules live in Unity and nothing here reads or writes sim state.

**Tech stack:** Unity 6000.3.25f1 (URP 17.3.0 with render graph, Shader Graph 17.3 "Terrain Lit" sub-target, UI Toolkit, Input System 1.20.0, test-framework 1.6.0, IL2CPP for WebGL 2 and macOS); Python through uv 0.12.5 (numpy 2.5.3, pillow 12.3.0, pytest 9.1.1); Chrome for Testing from the Playwright cache (`~/Library/Caches/ms-playwright/chromium-1228`).

**Spec:** `docs/superpowers/specs/2026-10-09-statecraft-israel-design.md` (§6 Presentation: "overlays the hex grid and blends biomes", "`CameraRig`: diorama pitch, zoom levels and a tilt-shift look", "Visual targets are the approved concept images in `promo/final/`"; §9). **Research input:** `research_notes/M4 visual pipeline/notes.md` (§0 findings, §1 gap analysis, §2 rendering, §2.3 tiers). **Code base:** `main` at `554ca2c` (M3 merged at `4061a8b`, then open-sourced: MIT code, CC BY 4.0 assets, public at github.com/eyalgolan/statecraft-israel). It contains m3-fix's border skirt and the playtest-2 work (`DiagLog` with `?bootlog`/`?navlog`, Input System event merging off with a per-press map queue, the loading state, the event-card restyle in `Statecraft.uss`, `people.yaml` `focus_y`, the flat-array `BiomeBlender`) and M3's last round (`tools/headed.py`, `tools/package_release.py`, the boot-failure line). Read files with `git -C <repo> show 554ca2c:<path>`. **Visual v1 branches from `main` at or after `554ca2c`, in the worktree `.worktrees/visual-v1`** (Phase 0 precondition: `git merge-base --is-ancestor 554ca2c HEAD` succeeds); the Phase 1 track worktrees branch from `visual-v1`, and every task lands on `main` by PR (or merge) after its review: Phase 0 first, then each track's tasks in order, the merge-point and Task 10 last.

**Owner rulings (binding, 2026-10-11):**
1. Scale is Civ-style symbolic (settlements sprawl over hexes by population; buildings and trees about 10–25× real, superseded on 2026-10-11 by about 60× for city blocks and 100× for houses and trees, see `docs/m3-rulings.md`; real geography). That is the next art track; this plan must not block it: the camera gains a closer hero zoom (Task 2) and nothing here bakes object scale into terrain or shaders.
2. Where the promo and era accuracy conflict, the promo wins.
3. Order: these quick wins first, then the settlement/vegetation art track. Scope is only the items in the Goal.
4. Every visual task ends with a scripted, **non-blocking** owner art checkpoint (side-by-side with the promo), plus objective tests where possible: render-sample tests, GLES3 shader compile, the frame-time profile on headless and headed WebGL, and unchanged golden hashes.

**Controller input (binding, 2026-10-11):** the m3 base includes m3-fix `7573092`. The round-1 sea plane is gone; an edge skirt (`MapBuilder.BuildBorderSkirt`, `MapScene.BorderSkirtName`) lies entirely outside the terrain: flat sea at sea level where the edge is sea (submesh 0, `Sea.mat`), muted land falling to sea level over about 20 km where the edge is land (submesh 1, `BorderLand.mat`). Known gap: at the Jordanian plateau edge (about 900 m) the land skirt drops off softly instead of continuing the relief. **Task 7 owns closing that gap.**

## Controller questions

Each has a recommended resolution; the plan proceeds with it.

1. **How the Terrain Lit Shader Graph is authored headlessly.** Agents cannot drive the Shader Graph editor. *Used:* `tools/terrain_graph.py` copies Unity's own template (`com.unity.shadergraph/GraphTemplates/Cross Pipeline/Terrain Simple.shadergraph`, present in `unity/Library/PackageCache`), keeps its targets and block nodes, and wires UV, Position, Normal and two Texture2D properties into **one Custom Function node** in **File** mode (`m_SourceType: 0`; Shader Graph's `HlslSourceType` is `{ File, String }`) whose source is `StatecraftTerrain.hlsl`. All shading logic lives in HLSL, so later art work edits HLSL, not graph JSON. Task 6 has a hard gate with a negative half (no import error) and a positive half (every generated forward pass's source includes `StatecraftTerrain.hlsl`, and a land pixel rendered with a magenta biome texture comes out magenta, not black). If either half fails the implementer stops and reports BLOCKED with the evidence (cost if wrong: about one day to wire the same HLSL into a hand-written fork of URP's `TerrainLit.shader`).
2. **Labels: world-space EB Garamond `TextMesh`, or UI Toolkit screen-space labels.** World-space labels would be blurred by the new tilt-shift in the top and bottom bands, and the promo's labels are crisp pills. *Used:* UI Toolkit labels (`MapLabelsPresenter`), positioned in `LateUpdate` (after the camera has moved) through an inline pixel `translate` (no layout pass), decluttered by priority so pills never overlap, styled in `Statecraft.uss` with the shipped EB Garamond SemiBold. Editor captures (`MapCapture.Promo`) do not show them; the WebGL screenshots (`?shot`) do.
3. **Fog.** URP's built-in fog keywords are stripped from builds when the boot scene has fog off (Automatic fog stripping); turning fog on at runtime would then do nothing in players. *Used:* the haze is an analytic term in our own terrain and sea shaders (`ScHazeAmount` in `StatecraftMap.hlsl`: distance aerial perspective plus extra haze low in the rift), needing no keywords and no depth texture. It is applied **after lighting** on both land and water (land: `BaseColor × (1 − h)` plus `Emission = haze × h`; water: a final-colour lerp), with a cooler, weaker haze over water so the sea stays the promo's saturated blue. Settlement markers (URP Lit placeholders) get no haze until the art track.
4. **Where the art checkpoint images come from.** *Used:* the WebGL player itself (`?shot` logs PNG chunks to the console; `web_check.py --shots` reassembles them), so the checkpoint shows exactly the web tier with its UI and labels. `MapCapture.Promo` adds editor renders of both quality tiers for desktop comparison.

## Global Constraints

Copied from the M3 plan (`docs/superpowers/plans/2026-10-10-statecraft-m3.md`, Global Constraints) and still binding, plus the visual-track rules at the end.

- **Unity:** editor `6000.3.25f1`, `$UNITY` as set in `docs/dev-setup.md`; run `tools/build_sim_for_unity.sh` before any Unity command after a pull. New Unity C# files start with `#nullable enable`.
- **M0–M3 Unity conventions:** materials are editor-made assets (no runtime `Shader.Find`; `Shader.Find` only in `Assets/Editor` and tests); runtime code never touches `Renderer.material`/`.materials` (no instancing): it uses `sharedMaterial`, global shader properties, or a `MaterialPropertyBlock`; runtime-created objects are owned by `MapRuntimeAssets` or destroyed by their owner; the **new Input System only**; edit-mode tests in batch mode; capture tools never rewrite `Map.unity` (only `MapCapture.MakeScene` does, and this plan does not run it).
- **No game rules in Unity.** Nothing in this plan reads or writes `GameState`, `Content` or anything under `sim/` or `content/`. Every task ends with `git diff --stat <task-base>..HEAD -- sim content` printing nothing.
- **Determinism:** golden hashes live in `GoldenRun.cs`, `DeterminismTests.cs` and `docs/determinism.md` (`Hash1950 71e9904d…`, `Hash1952 73590e32…` on `main` at `554ca2c`; the 5-turn smoke line is `SMOKE OK 5 turns 1948-06-19 d3fdd459…`; re-check in "Re-verify after M3 merge"). This plan never changes them. Every task that builds a player runs the golden check in that player.
- **0 warnings:** no new compiler warnings from our code: `grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log` prints nothing. Python tools: no new pytest warnings.
- **Edit-mode runs** always pass `-buildTarget OSXUniversal`, so a WebGL build earlier in the worktree does not leave the editor on the WebGL target (and so on quality level 0) for the tests, and (from Task 1 Step 7 on, when the tool exists) run under the shared machine lock: `uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log`. Every other Unity batch command (builds, imports, `-executeMethod` scripts) is wrapped the same way.
- **Project settings hygiene:** tests and `MapCapture.Promo` call `QualitySettings.SetQualityLevel`, which can make Unity write `m_CurrentQuality`. Before every commit `git status --short unity/ProjectSettings` must print nothing unless the task's Files list the setting; otherwise `git checkout -- unity/ProjectSettings/QualitySettings.asset`.
- **UI tests:** a `UIDocument` gives the tree a real panel; buttons are pressed with `NavigationSubmitEvent`, never `ClickEvent`. Shared harness: `unity/Assets/Tests/EditMode/UiTestKit.cs`.
- **Strings:** UI text lives outside code (labels show content names from `settlements_1948.yaml`, which is content).
- **Look (M3 ruling 2):** dark navy panels, thin gold borders, EB Garamond serif.
- **Commits:** stage explicit paths (never `git add -A`) and include the `.meta` files Unity creates; logs are `unity-*.log`, test results `unity-*.xml` (both git-ignored); end every commit message with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Builds and settings (M3 amendment R1):** a player build rewrites some URP-asset and player-setting fields as build caches; afterwards run `git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset`. That restores from the index, so a task that changes real settings **stages them first**. `tools/art_checkpoint.sh` refuses to build while `unity/Assets/Settings` or `ProjectSettings.asset` has unstaged changes.
- **Web:** compression Disabled for local checks; every browser check goes through `tools/web_check.py` (Chrome for Testing, `--headless=new`, `--disable-frame-rate-limit --disable-gpu-vsync`, served on `http://localhost`).
- **Visual track rules (new):**
  - WebGL 2 has no compute shaders: nothing here uses compute, the GPU Resident Drawer or indirect draws.
  - The web tier stays on the Forward path with **no depth texture and no opaque texture**; every new effect works without them.
  - Every new shader compiles for GLES3 (`ShaderChecks.Gles3Errors` is empty) and a WebGL build log shows no `Shader error` for it.
  - Art checkpoints never block a task: the implementer runs `tools/art_checkpoint.sh <task>`, attaches the `*-compare.png` and `*-deuteranopia.png`/`*-protanopia.png` paths to the task report, and moves on; the owner reviews asynchronously.
  - Every visual feature registers a `VisualToggles` switch so `?vprofile` can price it: `shadows`, `tiltshift`, `grade`, `haze`, `water`, `grid`, `labels`.
  - **Measurement discipline (review I1):** every frame-time run goes through `tools/machine_lock.py --exclusive --wait-unity`, which holds the machine lock and waits until no other batch-mode Unity process (edit-mode run, build, import, in any worktree) is running; Unity builds and captures inside `art_checkpoint.sh` take the lock shared. Chrome runs at device pixel ratio 1 (`--force-device-scale-factor=1`) unless a step asks for the DPR-2 stress proxy, and every `PROFILE` line states its resolution and render scale. The page's viewport is fixed to exactly 1920x1080 CSS pixels at the DPR over CDP (`Emulation.setDeviceMetricsOverride`, before navigation), and `web_check.py` and `headed.py profile` FAIL on any `res=` other than 1920x1080 × DPR, or on 120 s without console output (`--stall`). The WebGL frame clock is quantised to about 1 ms, so p95 values are whole milliseconds. Each configuration runs 3 times in interleaved order; a feature's cost is the difference of the medians, and it is over budget only when that difference exceeds the budget **plus** the larger of the two spreads (max − min of the three runs). The controller dispatches at most one profiling step at a time across tracks.
  - **Boot budget (playtest 2, P2-1; controller ruling 3):** `?bootlog` phase lines are recorded in Task 1 (baseline) and after Tasks 6 and 8. Two spans, each the median of three cold loads in a kept-awake headed window with a 1920x1080 viewport (`tools/headed.py boot --runs 3 --baseline <ms> --baseline-first-frame <ms>`): `map_build_ms` (heightmap decoded to map built) and `first_frame_ms` (navigation start to the second animation frame after `GAME OK`: scene load, texture uploads, map build and the first draw with its shader compiles). Each may grow by at most max(300 ms, 50% of its baseline), the tool's default. Task 1 baselines: `--cpu 1` 443 / 3547 ms; `--cpu 4` 1813 / 6963 ms.

## Review Focus

Inputs and conditions the spec and rulings imply that no feature test would otherwise exercise, most likely first. Each is pinned by a named test in its owning task.

1. **The camera orbited to any yaw.** Expected: the sea outside the map matches the sea inside it (grid included), and shadows still fall from the fixed world sun. *Tests: Tasks 7 and 8 `TheSeaBeyondTheEdgeMatchesTheTerrainSea` (yaw 0, 90, 180 and 270, patch averages, at hex centres, grid on and off); Task 2 `TheSunIsLowWarmAndFromTheWestNorthWest` (sun fixed in world space, independent of the rig).*
2. **Zoom extremes: the new 6 km hero zoom and the 250 km maximum.** Expected: shadows cover the whole view, hex lines stay one to two pixels wide and anti-aliased (never vanish, never alias into moiré), the tilt-shift weakens at maximum zoom-out so the overview stays readable. *Tests: Task 2 `TheShadowDistanceCoversTheWholeView` (6, 45, 120, 250 km; since Task 2 fix round 1 it asserts the straight-line distance to all four viewport-corner ground hits, which URP's shadow fade uses, divided by (1 − cascadeBorder), over 16:9, 21:9 and 4:3 and insets 0–0.5); Task 8 `GridLinesStayThinAtBothZoomExtremes`; Task 3 `TheBlurIsResolutionIndependentAndFollowsTheTopBar`.*
3. **Window size, aspect, render scale and the top bar's inset (21:9, a 4K window, the web tier's 0.8).** Expected: the blur is the same share of the picture at any resolution or render scale, the sharp band follows the map area below the top bar, shadows still cover the top corners, labels follow a resize. *Tests: Task 3 `TheBlurIsResolutionIndependentAndFollowsTheTopBar` and `AnOffCentreFocusFollowsTheTopBar` (TopInset 0.3); Task 2 `TheShadowDistanceCoversTheWholeView` (16:9 and 21:9 at the largest inset).*
4. **The map rebuilt (New Game, a second Build, destroying the map).** Expected: one set of labels, the URP asset's shadow distance restored, globals re-applied, no leaked textures or volume components. *Tests: Task 2 `DestroyingTheMapRestoresTheShadowDistance`; Task 9 `RebuildingKeepsOneLabelPerSettlement`; Task 6 `TheBiomeTextureIsOwnedByTheMapAndSetForTheShaders`; Task 3 `RebuildingDoesNotLeakVolumeComponents`.*
5. **A weak or software WebGL 2 GPU, and colour-blind players.** Expected: every new shader has a subshader that compiles for GLES3 (base and main-light-shadow variants), the R8 coast texture and half-resolution blur are supported, a SwiftShader run renders the map with no shader errors and both golden hashes, a DPR-2 fill-rate stress run is recorded; the coast, the selected hex and the grid stay distinguishable under simulated deuteranopia and protanopia. *Tests: `ShaderChecks.Gles3Errors` in Tasks 3, 6, 7, 8; Task 10 Steps 1 and 5; Task 4 `ColourBlindPlayersStillTellCoastAndSelectionApart`.*

## Parallel tracks

Phase 0 runs first on `visual-v1` (worktree `.worktrees/visual-v1`), branched from `main` at or after `554ca2c` (M3, m3-fix, playtest 2 and the open-source release are all in it); each task's PR (or merge) goes to `main`. Phase 1 runs three tracks in parallel worktrees (`superpowers:using-git-worktrees`), each branched from the Phase 0 commit; their file sets are disjoint except where marked "shared, non-overlapping hunks". Unity locks a project while it runs, so each worktree is its own project and their Unity runs can overlap.

| Phase | Track | Tasks | Worktree / branch | Files it owns |
|---|---|---|---|---|
| 0 | **Harness** | 1 | `visual-v1` | `Scripts/Map/{PromoPose,VisualToggles,FrameStats,FrameProfile,VisualShots}.cs`, `Scripts/Map/{MapRuntimeAssets,PixelErrorProfile}.cs`, `Scripts/Map/MapScene.cs` (teardown hand-off only), `Scripts/Map/CameraRig.cs` (teardown list only), `Scripts/UI/GameRoot.cs` (two flag lines), `Editor/MapCapture.cs` (`Promo`), `Tests/EditMode/{RenderSample,ShaderChecks,VisualHarnessTests}.cs`, `tools/{web_check,art_compare,machine_lock,headed}.py`, `tools/art_checkpoint.sh`, their tests, `tools/README.md`, `docs/visual-v1.md`, `docs/dev-setup.md` |
| 1 | **A: light and lens** | 2, 3, 4 in order | `vis-light` | `Scripts/Map/{MapLighting,ShadowFit,TiltShift,TiltShiftFeature,MapGrade,CameraRig,CameraController}.cs`, `Shaders/TiltShift.shader`, `Editor/PostFxAssets.cs`, `Materials/TiltShift.mat`, `Settings/{Mobile,PC}_RPAsset.asset`, `Settings/{Mobile,PC}_Renderer.asset`, `Tests/EditMode/{LightingTests,TiltShiftTests,GradeTests}.cs`; `Tests/EditMode/SettlementTests.cs` (shared, non-overlapping hunk: `RebuildingDoesNotLeak…` only) |
| 1 | **B: ground and water** | 5, 6, 7, 8 in order | `vis-ground` | `tools/{terrain_bake,terrain_graph}.py` + tests, `Textures/Map/**`, `Shaders/{StatecraftMap.hlsl,StatecraftTerrain.hlsl,StatecraftTerrain.shadergraph,Sea.shader}`, `Scripts/Map/{MapBuilder,MapShaderGlobals,MapPalette,HexGrid}.cs`, `Scripts/Map/MapScene.cs` (Configure/Build/Select, shared, non-overlapping with Track C's one property), `Editor/{MapMaterials,MapTextures,MapCapture}.cs`, `Materials/{Terrain,Sea,HexOverlay}.mat`, `Map.unity` (one line), `Tests/EditMode/{MapTests,TerrainShaderTests,SeaTests,HexGridTests,MapActionUiTests,GameSceneTests}.cs`; `Tests/EditMode/SettlementTests.cs` (shared, non-overlapping hunks: `Configure` calls, the seam test, the rift test, the selection test, the backdrop test's comment) |
| 1 | **C: labels** | 9 | `vis-labels` | `Scripts/UI/MapLabelsPresenter.cs`, `Scripts/UI/GameRoot.cs` (labels lines), `Scripts/Map/SettlementPlacer.cs`, `Scripts/Map/MapScene.cs` (shared: the `Settlements` property and one assignment), `UI/{Game.uxml,Statecraft.uss}`, `Tests/EditMode/MapLabelTests.cs`; `Tests/EditMode/SettlementTests.cs` (shared, non-overlapping hunk: the two label tests and their helpers) |
| — | **Merge point** | merge A, B, C into `visual-v1` | `visual-v1` | Expected: Unity edit mode `failed="0"` with total = B1 + 23 (Track A: 11 + 6 + 6) + 19 (Track B: 9 + 6 + 4) + 5 (Track C) = **B1 + 47** (B1 = B0 + 21 = 338, the total after Task 1 and its fix round); tools = **T1 + 15** (T1 = T0 + 34 = 143); sim unchanged |
| 2 | **D: tiers** | 10 | `visual-v1` | `Editor/QualityTiers.cs`, `Scripts/Map/QualityTierValues.cs`, `Settings/**`, `ProjectSettings/QualitySettings.asset`, `Tests/EditMode/QualityTierTests.cs`, `docs/visual-v1.md`, `docs/dev-setup.md` |

Notes for the controller:
- **Critical path is Track B** (Tasks 5 → 6 → 7 → 8, roughly 28–41 agent-hours). Track A (about 15–21 h) and Track C (about 7–9 h) finish before it; their implementers can review Track B tasks.
- **Shared files:** `MapScene.cs` (B edits `Configure`/`Build`'s material checks and the builder call; C adds a property and one assignment after `SettlementPlacer.Parse`); `GameRoot.cs` (Phase 0 adds two flag lines; C adds the labels presenter); `SettlementTests.cs` (A, B, C each edit different tests). Merge B first, then A, then C; conflicts, if any, are adjacent-line conflicts with an obvious union.
- **Render tests across tracks:** Track A's render tests run before Track B's terrain shader exists, Track B's before Track A's grade. All of them run again at the merge point; thresholds in this plan were chosen with that headroom (each threshold names its reasoning).
- **Settings and the art checkpoint:** a track that changes `unity/Assets/Settings` stages the change before running `tools/art_checkpoint.sh` (the script refuses otherwise).
- **One machine (review I1):** three worktrees share one Mac. Profiling steps (`?vprofile`, `?bootlog` budgets) run under `tools/machine_lock.py --exclusive --wait-unity`; dispatch at most one at a time. Builds, imports and edit-mode runs of the other tracks may continue; the profile waits for them (machine_lock recognises Unity by its executable, so queued wrappers never count; macOS flock does not queue fairly, so a stream of shared jobs can delay an exclusive profile).
- **Hidden shared state at the merge:** Track A's lighting and grade shift every colour Track B's render tests read; Track B's thresholds below are derived with Track A's sun (intensity 1.6, warm) already in the arithmetic, and all render tests re-run at the merge point.

## Effort (honest)

Agent-hours include Unity batch runs (an edit-mode run is about 4–6 minutes; a WebGL build about 2–3 minutes), review fixes and the checkpoint script, plus about 30 minutes per new worktree for its cold `Library` import (three worktrees). Profiling steps now run 3 interleaved repeats and wait for the machine lock, which adds about 20–40 minutes per profiled task. They exclude owner review latency.

| Task | Size | Estimate | Main risk |
|---|---|---|---|
| 1 Harness | M | 8–10 h | console size limits for `SHOT` chunks (mitigated: 8,000-character chunks, count and SHA-256 checks) |
| 2 Sun, ambient, map-scale shadows, hero zoom | S | 4–6 h | shadow acne at 90 m texels (bias tuning) |
| 3 Tilt-shift | M | 6–8 h | render-graph API details in URP 17.3 |
| 4 Grade, tonemapping, vignette, readability checks | M | 5–7 h | taste; the owner checkpoint carries it |
| 5 Terrain bakes (tools) | M | 5–7 h | numpy run time on 2049² (42 s measured) |
| 6 Terrain Lit Shader Graph | L | 10–16 h | generated graph import (hard gate, controller question 1) |
| 7 Sea, foam, shadows on water, skirt plateau | M | 7–10 h | seam tolerance with animated water (mitigated: patch averages) |
| 8 Shader hex grid on land and sea, line mesh removed | M | 6–8 h | test churn from the removed overlay parameter |
| 9 EB Garamond labels with declutter | M | 7–9 h | layout-based tests need frames (UnityTest) |
| 10 WebGL tier review | M | 6–8 h | needs headed runs on the owner's machine for the final numbers |
| **Total** | | **65–90 h** (plus 1.5 h of worktree imports) | Wall clock with three tracks: about 8–12 working days, plus owner review |

---
## Re-verify after M3 merge

Every code assumption below was read at m3 `8978566` (which contains m3-fix `7573092` and the playtest-2 work) and re-checked on `main` at `554ca2c` (after the M3 merge and open-sourcing): R1–R9, R11–R15, R19–R22, R24 and R25 are unchanged there (no file under `unity/Assets/Scripts/Map`, `Editor/Map*`, `Settings`, `ProjectSettings`, `Map.unity`, `content/map` or `StreamingAssets` changed); R10, R16, R17, R18, R23 are updated below and R26–R27 are new. Before Task 1 starts, the controller runs each check on `main` after M3 has merged and, where one fails, amends the named tasks before dispatching them. Paths are relative to the repo root.

| # | Assumption (at `8978566`; re-checked at `554ca2c`) | Check | Tasks that depend on it |
|---|---|---|---|
| R1 | `MapScene.Configure(terrain, overlay, marker, selection, sea, borderLand)`: six materials, in that order; `Build` throws `InvalidOperationException` naming each missing field | `grep -n "public void Configure" unity/Assets/Scripts/Map/MapScene.cs` | 8 (removes `overlay`), 9 |
| R2 | `MapBuilder.Build(HeightmapMeta, float[,], HexFile, Material terrain, Material overlay)` and `Build(string mapDir, string hexesJson, Material, Material)`; `ApplyBiomeColours` makes 7 one-pixel `TerrainLayer`s and 1024² alphamaps and is followed by `Boot.DiagLog.BootPhase("biome colours blended");`; `BuildHexOverlay` makes the `HexOverlay` child, followed by `Boot.DiagLog.BootPhase("hex overlay draped");` | `grep -n "public static GameObject Build\|ApplyBiomeColours\|BuildHexOverlay\|AlphamapResolution\|BootPhase" unity/Assets/Scripts/Map/MapBuilder.cs` | 6, 8 |
| R3 | The border skirt: `MapBuilder.BuildBorderSkirt(TerrainData, float elevMinM, MapRuntimeAssets)`, `SkirtRings = {0, 4000, 20000, 60000, 600000}`, private `SkirtFall = {0, 0.4, 0.85, 1, 1}`, one spoke per heightmap vertex along each edge (`SkirtSamplesPerEdge` is gone), each spoke's ring vertices consecutive in the vertex list; submesh 0 sea (`Sea.mat`), submesh 1 land (`BorderLand.mat`), entirely outside the terrain; sea spokes at `seaY = (0 - elev_min_m) × VerticalExaggeration` | `grep -n "SkirtRings\|SkirtFall\|BuildBorderSkirt\|seaY\|int samples" unity/Assets/Scripts/Map/MapBuilder.cs` | 7 |
| R4 | `Sea.mat` and `BorderLand.mat` are URP Lit, matte, made in `MapMaterials.Create` (`SeaPath`, `BorderLandPath`); `MapBuilder.BiomeColours["sea"] = (0.16, 0.42, 0.55)`, `BorderLandColour = (0.60, 0.56, 0.46)` | `grep -n "SeaPath\|BorderLandPath\|Make(" unity/Assets/Editor/MapMaterials.cs` | 6, 7 |
| R5 | Tests this plan edits, by name: `MapSceneTests.TheBorderSkirtContinuesEachEdgeOutsideTheMap` (four-edge sliver check), `TheBackdropIsLandWhereTheNeighboursAreLand`, `TheRiftBelowSeaLevelIsVisible` (samples a hex corner raised by `OverlayClearanceM`, darkest pixel), `TheSeaBeyondTheEdgeMatchesTheTerrainSea` (single-pixel compare, tolerance 0.012, yaw 0), `SelectingAHexOutlinesItOnTheTerrain` (asserts the selection queue is above `HexOverlay.mat`'s, one `UnityPerMaterial` CBUFFER per pass, and the band's middle above `SampleHeight + MapBuilder.OverlayClearanceM - 1`), `BuildWithoutMaterialsNamesTheMissingField`, `RebuildByAFreshComponentStillClearsTheOldMap`, `RebuildingDoesNotLeakDepthOfFieldComponents`, `LabelsAreLightWithADarkShadowAboveTheMarkerFacingTheRig`, `EveryTelAvivClusterLabelIsUnambiguouslyClosestToItsOwnMarker` (all in `SettlementTests.cs`); `MapTests.BuildUsesThePassedTerrainAndOverlayMaterials`, `BiomeLayersAreMatte`, `EveryOverlayVertexLiesAboveTheTerrainAndInsideItsBounds`, `EveryPointAlongEveryOverlaySegmentLiesAboveTheTerrain`, `TerrainUsesFineLodSoTheCoarseSurfaceDoesNotCoverTheOverlay`, `DestroyingTheMapDestroysItsRuntimeAssets`, `BuildFromReadInputsMatchesBuildFromPaths`, and the two bounds/biome rejection tests; `GameSceneTests.MapUnityIsTheOnlyBuildSceneAndIsFullyWired` (asserts `overlayMaterial`); `MapActionUiTests` (two six-material `Configure` calls) | `grep -n "public .*void \(TheBorderSkirt\|TheBackdrop\|TheRift\|TheSeaBeyond\|SelectingAHex\|BuildWithoutMaterials\|RebuildBy\|RebuildingDoesNotLeak\|LabelsAreLight\|EveryTelAviv\)" unity/Assets/Tests/EditMode/SettlementTests.cs`; `grep -n "OverlayMatPath\|overlayMat\|OverlayClearanceM" unity/Assets/Tests/EditMode/*.cs` | 3, 6, 7, 8, 9 |
| R6 | `CameraRig`: `Pitch = 50`, `Fov = 35`, `MaxTopInset = 0.5`, `Init()` creates the global `Volume` with a runtime `VolumeProfile` and a Gaussian `DepthOfField` (`_dof`), `Apply()` sets the transform, the DoF range and `ApplyProjection()`; the file has no `#nullable enable` | `grep -n "_dof\|MaxTopInset\|public void Init\|public void Apply" unity/Assets/Scripts/Map/CameraRig.cs` | 1, 2, 3, 4 |
| R7 | `CameraController.MinDistance = 20000f`, `MaxDistance = 250000f`; `CameraInputTests.ZoomIsClampedAndOrbitTurnsTheView` clamps to `MinDistance` by name | `grep -n "MinDistance\|MaxDistance" unity/Assets/Scripts/Map/CameraController.cs unity/Assets/Tests/EditMode/CameraInputTests.cs` | 2 |
| R8 | `MapScene.Build` logs `DiagLog.BootPhase` after the heightmap, the hexes, the skirt and the settlements, ends with `rig.Init()` then owns `rig.Volume.profile` and its components through the terrain's `MapRuntimeAssets`; `MapRuntimeAssets` is `[ExecuteAlways]` with `Own<T>` and `OnDestroy`; `MapScene.SelectionLiftM = 20f` and `Select` uses `MapBuilder.OverlayClearanceM + SelectionLiftM` | `sed -n '/rig.Init()/,/assets.Own(rig.Volume.profile)/p' unity/Assets/Scripts/Map/MapScene.cs`; `grep -n "BootPhase\|SelectionLiftM" unity/Assets/Scripts/Map/MapScene.cs` | 1, 8 |
| R9 | `SettlementPlacer.Place(list, frame, terrain, markerMaterial)` makes a cube per settlement named by id plus `TextMesh` labels `<id>_label` with a `shadow` child; `Marker` table `(colour, h, cap)` by type; `LabelAnchor` and `LabelOffsetEastM` tables | `grep -n "TextMesh\|LabelAnchor\|LabelOffsetEastM\|static readonly Dictionary<string, (Color" unity/Assets/Scripts/Map/SettlementPlacer.cs` | 9 |
| R10 | `GameRoot.Begin` has `var camRig = rig = cameraController.GetComponent<CameraRig>();`, `var mini = minimap = Own(new MinimapPresenter(root, map.Hexes));`, removes the `loading` class and logs `BootPhase("screens ready")`, and `if (Application.isPlaying && PixelErrorProfile.Requested()) StartCoroutine(PixelErrorProfile.Run(map.Terrain));`; `GameRoot.Update()` starts with `SyncMinimap();`; there is no `LateUpdate`; `Shutdown()` has `minimap = null; rig = null; drawn = null; logCamera = false;` and restores the pixel error | `grep -n "camRig = rig\|var mini = \|loading\|screens ready\|PixelErrorProfile.Requested\|void Update\|LateUpdate\|SyncMinimap();\|minimap = null" unity/Assets/Scripts/UI/GameRoot.cs` | 1, 9 |
| R11 | `PixelErrorProfile.Line(float, List<float>)` throws `ArgumentNullException` for null and `ArgumentException("no frames were measured", "frameMs")` for an empty list; format `PROFILE pixelError=1 frames=100 avg_ms=50.50 p95_ms=95.00` | `grep -n "throw\|string.Format" unity/Assets/Scripts/Map/PixelErrorProfile.cs` | 1 |
| R12 | `tools/web_check.py` has `scan`, `drain`, `watch(inbox, poll, join_reader, expect, fail, timeout, now, sleep)` and `main` with `--query/--expect/--fail/--timeout/--swiftshader`; `tools/tests/test_web_check.py` tests `scan` and `watch` | `grep -n "^def \|add_argument" tools/web_check.py` | 1 |
| R13 | URP assets: `Mobile_RPAsset` (WebGL tier) Forward, render scale 0.8, HDR on (`m_SupportsHDR: 1`), LDR grading (`m_ColorGradingMode: 0`), depth and opaque textures off, 1 cascade, 1024 shadow map, soft shadows off, biases 1/1, `m_ShadowDistance: 50`; `PC_RPAsset` Forward+, 4 cascades, 2048, soft, SSAO feature on `PC_Renderer`; `Mobile_Renderer` has no features; `QualitySettings` maps WebGL to level 0 (Mobile) and Standalone to 1 (PC) | `grep -nE "m_RenderScale|m_SupportsHDR|m_ColorGradingMode|m_RequireDepthTexture|m_RequireOpaqueTexture|m_ShadowDistance|m_ShadowCascadeCount|m_MainLightShadowmapResolution|m_SoftShadowsSupported" unity/Assets/Settings/*_RPAsset.asset`; `grep -n "m_RendererFeatures\|m_RenderingMode" unity/Assets/Settings/*_Renderer.asset`; `grep -n -A3 "m_PerPlatformDefaultQuality" unity/ProjectSettings/QualitySettings.asset` | 2, 3, 4, 10 |
| R14 | `Map.unity`: a default Directional Light (soft shadows, colour (1, 0.957, 0.839)), `RenderSettings` fog off, `MapScene` serialises `overlayMaterial` and `borderLandMaterial` | `grep -n "overlayMaterial\|borderLandMaterial\|m_Fog:" unity/Assets/Map.unity` | 2, 8 |
| R15 | `heightmap.json` is 2049² over lon 34.20–35.90, lat 29.45–33.35, `elev_min_m -427.04`, `elev_max_m 2367.42`; `hexgrid.py` puts axial (0, 0) at (lon0, lat1) with y growing south, pointy-top, `HEX_SIZE_KM = 5` | `cat unity/Assets/StreamingAssets/map/heightmap.json`; `grep -n "def hex_center_km\|def lonlat_to_km\|HEX_SIZE_KM =" tools/hexgrid.py` | 5, 8 |
| R16 | Test totals are recorded, not asserted: the tools count (`tools/README.md`'s `Expected:` line: `109 passed, 1 skipped` on `main` at `554ca2c`, with `test_package_release.py`) as T0 and the Unity edit-mode total as B0, both in Task 1 Step 1; `tools/tests/conftest.py` defines the `--headed` pytest option and marker | `uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q \| tail -1` | all |
| R17 | Golden hashes `Hash1950 = 71e9904d…`, `Hash1952 = 73590e32…`; WebGL lines `GOLDEN OK 1950 71e9904d…` / `GOLDEN OK 1952 73590e32…`; smoke `SMOKE OK 5 turns 1948-06-19 d3fdd459…` (`docs/m3-release.md`) | `grep -n "Hash1950\|Hash1952" sim/Statecraft.Sim/GoldenRun.cs`; `grep -n "d3fdd459" docs/m3-release.md` | all |
| R18 | `ProjectSetup.Apply()` is called by `Build.Web` and sets `StackTraceLogType.None` for `LogType.Log`; `Build.Web` takes `-compression` and `-out` and writes `unity/Builds/Web-<compression>/` | `grep -n "Builds/Web-\|ProjectSetup.Apply\|\"-out\"" unity/Assets/Editor/Build.cs`; `grep -n "StackTraceLogType" unity/Assets/Editor/ProjectSetup.cs` | 1, 10 |
| R19 | `Statecraft.Game.asmdef` references `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime`, `Unity.InputSystem` (enough for render-graph passes and UI Toolkit) | `cat unity/Assets/Scripts/Statecraft.Game.asmdef` | 3, 9 |
| R20 | `HexSelection.shader` has a `DepthOnly` pass, `Offset -1, -4`, queue `Geometry+10`, and the same `UnityPerMaterial` CBUFFER in every pass | `grep -n "Queue\|Offset\|DepthOnly\|CBUFFER_START" unity/Assets/Shaders/HexSelection.shader` | 7, 8 |
| R21 | `DiagLog` (`Statecraft.Game.Boot`) logs `BOOT phase=<name> at=<ms> ms dt=<ms> ms` under `?bootlog`; phases in order include `player started`, `heightmap decoded`, `hexes parsed`, `terrain heights set`, `biome colours blended`, `hex overlay draped`, `border skirt`, `settlements placed`, `map built`, `session started (...)`, `screens ready` | `grep -rn "BootPhase(" unity/Assets/Scripts`; `grep -n "DiagLog" unity/Assets/Scripts/Map/MapBuilder.cs unity/Assets/Scripts/Map/MapScene.cs` | 1, 6, 8 |
| R22 | `Game.uxml`: the root is `<ui:VisualElement name="root" class="root loading" picking-mode="Ignore">` whose first child is `<ui:Label name="loading" text="Loading the map of 1948…" class="loading-line" picking-mode="Ignore" />` | `sed -n 1,8p unity/Assets/UI/Game.uxml` | 9 |
| R23 | `Statecraft.uss` ends with the loading block (`.loading-line`, `.loading .loading-line`, `.root.loading { background-color: var(--navy-solid); }`, `.loading .date-box, … { visibility: hidden; }`); the event-card restyle (`.event-head`, `.event-image` 96×128, `.right-column` 480 px) is above it; the file defines `--gold` | `tail -5 unity/Assets/UI/Statecraft.uss`; `grep -n "\-\-gold:\|event-head\|right-column" unity/Assets/UI/Statecraft.uss` | 9 |
| R24 | The WebGL template does not set `config.devicePixelRatio`, so Unity uses the browser's DPR (2 on a Retina display) | `grep -n "devicePixelRatio\|var config" unity/Assets/WebGLTemplates/Statecraft/index.html` | 1, 10 |
| R26 | `docs/dev-setup.md`'s "Page flags" bullet ends `(every UI pointer and submit event, screen toggle and map click, with its frame). Desktop players take \`-golden\`,` / `\`-smoke N\`, ... \`-bootlog\` and \`-navlog\`. The console also shows ...`; `tools/README.md`'s `headed.py` entry ends `and \`loadshot\` (screenshots during the load). Its browser test runs only with \`pytest --headed\`.` and its `Expected:` line reads `109 passed, 1 skipped (...)` | `grep -n "with its frame). Desktop players" docs/dev-setup.md`; `grep -n "loadshot\|Expected:" tools/README.md` | 1 |
| R27 | `docs/determinism.md` pins the full browser expectations: `GOLDEN OK 1950 71e9904d0ee4…5b07`, `GOLDEN OK 1952 73590e324f19…9767`, `SMOKE OK 5 turns 1948-06-19 d3fdd459ddb8…a5b6`; `GameBoot` raises `Failed` and logs `BOOT FAIL` (already in `web_check`'s default `--fail`) | `grep -n "GOLDEN OK\|SMOKE OK" docs/determinism.md` | 1, 10 |
| R25 | `BiomeBlender(IReadOnlyList<Vector2> centres, IReadOnlyList<int> layers, float hexSizeM, int layerCount)` and `Weights(Vector2, float[])` keep their signatures after the flat-array refactor (`BiomeBlenderSpeedTests` pins bit-identical weights) | `grep -n "public BiomeBlender\|public void Weights" unity/Assets/Scripts/Map/BiomeBlender.cs` | 6 |


## File structure (new files)

```
tools/                         terrain_bake.py  terrain_graph.py  art_compare.py  art_checkpoint.sh  machine_lock.py
                               (headed.py and web_check.py are extended, not created)
tools/tests/                   test_terrain_bake.py  test_terrain_graph.py  test_art_compare.py  test_web_check_shots.py
                               test_machine_lock.py  (test_headed.py is extended)
unity/Assets/Textures/Map/     terrain_bake.png  coast.png                       (generated by terrain_bake.py)
unity/Assets/Shaders/          StatecraftMap.hlsl  StatecraftTerrain.hlsl  StatecraftTerrain.shadergraph (generated)
                               Sea.shader  TiltShift.shader
unity/Assets/Materials/        TiltShift.mat                                     (HexOverlay.mat is deleted in Task 8)
unity/Assets/Scripts/Map/      PromoPose  VisualToggles  FrameStats  FrameProfile  VisualShots          (Task 1)
                               MapLighting  ShadowFit  TiltShift  TiltShiftFeature  MapGrade            (Track A)
                               MapPalette  MapShaderGlobals  HexGrid                                     (Track B)
                               QualityTierValues                                                         (Task 10)
unity/Assets/Scripts/UI/       MapLabelsPresenter.cs                                                     (Track C)
unity/Assets/Editor/           PostFxAssets.cs  MapTextures.cs  QualityTiers.cs
unity/Assets/Tests/EditMode/   RenderSample  ShaderChecks  VisualHarnessTests  LightingTests  TiltShiftTests  GradeTests
                               TerrainShaderTests  SeaTests  HexGridTests  MapLabelTests  QualityTierTests
docs/                          visual-v1.md
```

---

## Phase 0: the harness (Task 1)

### Task 1: Promo poses, toggles, the visual profile, WebGL screenshots and the art checkpoint

*Phase 0, branch `visual-v1` (from `main` after M3 merged). Effort 8–10 h.* Every later task needs the same instruments: a camera pose that frames the promo's ground, a switch per visual feature, a frame-time profile that prices each switch on WebGL (`?vprofile`, after M3's `PixelErrorProfile`) with repeats, interleaving and the resolution in every line, a machine lock so profiles never share the Mac with another Unity job, a boot-time report from playtest 2's `?bootlog`, screenshots from the WebGL player itself (`?shot`) checked for completeness, and a script that puts them next to the promo (with colour-blind simulations). It also adds the colour metrics (`DeltaE`, CVD simulation) and a GLES3 compile check that understands subshaders built for other platforms. This task records the "before" numbers and images.

**Files:**
- Create: `unity/Assets/Scripts/Map/PromoPose.cs`, `unity/Assets/Scripts/Map/VisualToggles.cs`, `unity/Assets/Scripts/Map/FrameStats.cs`, `unity/Assets/Scripts/Map/FrameProfile.cs`, `unity/Assets/Scripts/Map/VisualShots.cs`
- Create: `unity/Assets/Tests/EditMode/RenderSample.cs`, `unity/Assets/Tests/EditMode/ShaderChecks.cs`, `unity/Assets/Tests/EditMode/VisualHarnessTests.cs`
- Create: `tools/art_compare.py`, `tools/art_checkpoint.sh`, `tools/machine_lock.py`, `tools/tests/test_art_compare.py`, `tools/tests/test_web_check_shots.py`, `tools/tests/test_machine_lock.py`, `docs/visual-v1.md`
- Modify: `unity/Assets/Scripts/Map/MapRuntimeAssets.cs`, `unity/Assets/Scripts/Map/PixelErrorProfile.cs`, `unity/Assets/Scripts/Map/CameraRig.cs`, `unity/Assets/Scripts/Map/MapScene.cs`, `unity/Assets/Scripts/UI/GameRoot.cs`, `unity/Assets/Editor/MapCapture.cs`, `tools/web_check.py`, `tools/headed.py` (M3's headed-browser driver: boot budgets and headed profiles go through it, review N8), `tools/tests/test_headed.py`, `tools/README.md`, `docs/dev-setup.md`
- Unity creates a `.meta` for every new file under `unity/Assets`; stage each with its file.

**Interfaces:**
- Consumes: `UrlFlags.Has(string?, string)`, `CameraRig.{LookAt, Distance, Yaw, Apply()}`, `GeoFrame.ToWorld`, `MapRuntimeAssets.Own`, `GameRoot.Begin` (R10), `DiagLog` `BOOT phase=` lines (R21).
- Produces (later tasks rely on these exact names):
  - `PromoPose { string Name; double Lon, Lat; float Distance, Yaw; static PromoPose Gameplay03, Diorama02; static IReadOnlyList<PromoPose> All; void Apply(CameraRig rig, GeoFrame frame); }`
  - `VisualToggles { static Action Register(string name, Action<bool> set) /* returns a removal token: removes the switch only while it is still the registered one */; static void Unregister(string name); static IReadOnlyList<string> Names; static void Set(string name, bool on); static void SetAll(bool on); static void Clear(); }`
  - `FrameStats.Summary(IReadOnlyList<float> frameMs) -> (float avg, float p95)`
  - `FrameProfile { const int WarmupFrames = 60, MeasuredFrames = 300, Repeats = 3; static bool Requested(); static bool Requested(string? url, string[] args, bool webgl); static string Line(string label, string pose, string resolution, float renderScale, int rep, IReadOnlyList<float> frameMs); static string Summary(string label, string pose, IReadOnlyList<(float avg, float p95)> runs); static IReadOnlyList<int> Order(int count, int rep); static IEnumerator Run(CameraRig, GeoFrame, Action<string> log, bool quitWhenDone); }` logging `PROFILE visual=<all|-name|none> pose=<pose> res=<w>x<h> scale=<s> rep=<r> frames=300 avg_ms=<a> p95_ms=<p>`, then per configuration `PROFILE summary visual=<label> pose=<pose> runs=3 median_avg_ms=<m> spread_ms=<s> median_p95_ms=<p>`, and `PROFILE visual done`
  - `VisualShots { const int SettleFrames = 30, ChunkChars = 8000; static bool Requested(); static bool Requested(string? url, string[] args, bool webgl); static IEnumerable<string> Lines(string name, byte[] png); static IEnumerator Run(CameraRig, GeoFrame, Action<string> log); }` logging `SHOT <pose> begin <n> <sha256>`, `SHOT <pose> <i>/<n> <base64>`, `SHOT done`
  - `MapRuntimeAssets.OnTeardown(Action)` and `MapRuntimeAssets.RunTeardown()` (idempotent, newest first, a throwing action is logged and the rest still run; `MapScene.Clear` calls it before destroying the map, `OnDestroy` calls it too); `CameraRig.TeardownActions` (a `List<Action>` that `MapScene.Build` hands to the map's `MapRuntimeAssets` after `rig.Init()`; `Init` runs and clears any left behind); a switch is always owned by a teardown: `TeardownActions.Add(VisualToggles.Register(...))` in `CameraRig.Init`, `assets.OnTeardown(VisualToggles.Register(...))` elsewhere
  - Test kit: `RenderSample.Render(Camera, int w, int h)`, `RenderSample.Render(Camera, int w, int h, Vector3[] world, Vector2[] screenOut)`, `.At(x, y)`, `.Mean(cx, cy, radius)`, `.GradientEnergy(float y0, float y1)`, `.MeanSaturation()`, `.LumaStd()`, `.MeanLuma(float x0, float y0, float x1, float y1)`, `.FractionClipped(float level)`, `static Luma(Color)`, `static FractionDarker(RenderSample a, RenderSample b, float by)`, `static Vector3 Lab(Color srgb)`, `static float DeltaE(Color a, Color b)` (CIE76), `enum Cvd { Deuteranopia, Protanopia }`, `static Color SimulateCvd(Color srgb, Cvd kind)` (Machado 2009, severity 1); `ShaderChecks.Gles3Errors(Shader) -> List<string>`, `ShaderChecks.ExcludesGles3(string passSource) -> bool`
  - Tools: `web_check.py --shots DIR` (headless, DPR 1); `web_check.ShotCollector(out_dir).feed(text) -> bool` (raises on `SHOT done` with an unfinished shot); `web_check.chrome_flags(profile, swiftshader, dpr=1) -> list[str]`; `web_check.py` fixes the viewport to 1920x1080 over CDP and fails on another `res=` or shot size, and on `--stall S` seconds without console output; `web_check.profile_errors(lines, expected_res)`, `web_check.expected_res(w, h, dpr)`; `tools/machine_lock.py (--shared|--exclusive) [--wait-unity] -- <command>` (`lockfile_holders`, `unity_pids`: batch-mode Unity and any editor holding a worktree's `unity/Temp/UnityLockfile`); `tools/headed.py boot --runs N [--baseline MS] [--baseline-first-frame MS] [--budget MS] [--cpu R]` printing `BOOT map_build_ms median=<m> runs=<n> cpu=<r>` and `BOOT first_frame_ms median=<m> runs=<n> cpu=<r>` (exit 1 when either exceeds its baseline + `--budget`, default max(300 ms, 50%)) and `tools/headed.py profile --dpr N` (headed `?vprofile` at 1920x1080, uncapped), with `headed.boot_phases`, `headed.map_build_ms`, `headed.over_budget`, `headed.browser_flags`; `tools/art_compare.py <shots-dir> <name> [--log PATH]`; `tools/art_checkpoint.sh [--no-build] <name>`

- [ ] **Step 1: Record the baseline**

Run (repo root, on `visual-v1`; `machine_lock.py` does not exist yet, so these two Unity-free and Unity commands run plainly):
```bash
git merge-base --is-ancestor 554ca2c HEAD && echo "base includes main 554ca2c"
tools/build_sim_for_unity.sh
"$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q | tail -1
git rev-parse HEAD
```
Expected: `base includes main 554ca2c`; `exit=0`, `failed="0"`; tools `109 passed, 1 skipped` at `554ca2c` (R16; recorded, not asserted: if `main` has moved on, the count may differ, and the recorded value is what later steps add to). Write the Unity total down as **B0**, the tools passed count as **T0** (and the skipped count, which stays as it is) and the commit as **task-base**; every later expected total is stated relative to them.

- [ ] **Step 2: Write the failing Unity tests**

Create `unity/Assets/Tests/EditMode/RenderSample.cs`:

```csharp
#nullable enable
using UnityEngine;

/// <summary>Edit-mode render sampling for the visual tests: renders a camera into a texture and reads the pixels back.
/// Row 0 is the bottom of the image (viewport y = 0). Luma is Rec. 709 over the stored (display-referred) values.</summary>
public sealed class RenderSample
{
    public readonly int Width, Height;
    public readonly Color[] Pixels;

    RenderSample(int width, int height, Color[] pixels) { Width = width; Height = height; Pixels = pixels; }

    public static RenderSample Render(Camera cam, int width, int height) => Render(cam, width, height, System.Array.Empty<Vector3>(), System.Array.Empty<Vector2>());

    /// <summary>Renders, and projects each world point to this image's pixels while the camera still targets it.</summary>
    public static RenderSample Render(Camera cam, int width, int height, Vector3[] world, Vector2[] screenOut)
    {
        var rt = new RenderTexture(width, height, 24);
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        var oldTarget = cam.targetTexture;
        try
        {
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0); tex.Apply();
            for (int i = 0; i < world.Length; i++) { var p = cam.WorldToScreenPoint(world[i]); screenOut[i] = new Vector2(p.x, p.y); }
            return new RenderSample(width, height, tex.GetPixels());
        }
        finally
        {
            RenderTexture.active = null; cam.targetTexture = oldTarget;
            rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
    }

    public Color At(int x, int y) => Pixels[Mathf.Clamp(y, 0, Height - 1) * Width + Mathf.Clamp(x, 0, Width - 1)];

    /// <summary>The mean colour of the (2 radius + 1)² patch around (cx, cy).</summary>
    public Color Mean(int cx, int cy, int radius)
    {
        Color sum = Color.clear; int n = 0;
        for (int y = cy - radius; y <= cy + radius; y++)
            for (int x = cx - radius; x <= cx + radius; x++) { sum += At(x, y); n++; }
        return sum / n;
    }

    public static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

    /// <summary>Mean |dL/dx| + |dL/dy| over the rows from y0 to y1 (fractions of the height, 0 = bottom): local contrast,
    /// which a blur lowers.</summary>
    public float GradientEnergy(float y0, float y1)
    {
        int r0 = Mathf.Clamp(Mathf.FloorToInt(y0 * Height), 0, Height - 2), r1 = Mathf.Clamp(Mathf.CeilToInt(y1 * Height), r0 + 1, Height - 1);
        double sum = 0; int n = 0;
        for (int y = r0; y < r1; y++)
            for (int x = 0; x < Width - 1; x++)
            {
                float l = Luma(At(x, y));
                sum += Mathf.Abs(Luma(At(x + 1, y)) - l) + Mathf.Abs(Luma(At(x, y + 1)) - l); n++;
            }
        return (float)(sum / n);
    }

    /// <summary>Mean luma over a rectangle given as fractions of the image (0 = left / bottom).</summary>
    public float MeanLuma(float x0, float y0, float x1, float y1)
    {
        double sum = 0; int n = 0;
        for (int y = Mathf.FloorToInt(y0 * Height); y < Mathf.CeilToInt(y1 * Height); y++)
            for (int x = Mathf.FloorToInt(x0 * Width); x < Mathf.CeilToInt(x1 * Width); x++) { sum += Luma(At(x, y)); n++; }
        return (float)(sum / n);
    }

    public float MeanSaturation()
    {
        double sum = 0;
        foreach (var c in Pixels) { Color.RGBToHSV(c, out _, out var s, out _); sum += s; }
        return (float)(sum / Pixels.Length);
    }

    public float LumaStd()
    {
        double sum = 0, sq = 0;
        foreach (var c in Pixels) { double l = Luma(c); sum += l; sq += l * l; }
        double mean = sum / Pixels.Length;
        return (float)System.Math.Sqrt(System.Math.Max(0, sq / Pixels.Length - mean * mean));
    }

    /// <summary>Share of pixels whose luma in <paramref name="a"/> is lower than in <paramref name="b"/> by more than <paramref name="by"/>.</summary>
    public static float FractionDarker(RenderSample a, RenderSample b, float by)
    {
        int n = 0;
        for (int i = 0; i < a.Pixels.Length; i++) if (Luma(a.Pixels[i]) < Luma(b.Pixels[i]) - by) n++;
        return (float)n / a.Pixels.Length;
    }

    /// <summary>Share of pixels with any channel at or above <paramref name="level"/> (clipping: 0.995 is the top 8-bit step).</summary>
    public float FractionClipped(float level)
    {
        int n = 0;
        foreach (var c in Pixels) if (c.r >= level || c.g >= level || c.b >= level) n++;
        return (float)n / Pixels.Length;
    }

    static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
    static float ToSrgb(float c) => c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;

    /// <summary>CIELAB (D65) of a display (sRGB) colour.</summary>
    public static Vector3 Lab(Color srgb)
    {
        float r = ToLinear(srgb.r), g = ToLinear(srgb.g), b = ToLinear(srgb.b);
        float x = (0.4124f * r + 0.3576f * g + 0.1805f * b) / 0.95047f;
        float y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
        float z = (0.0193f * r + 0.1192f * g + 0.9505f * b) / 1.08883f;
        float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;
        float fx = F(x), fy = F(y), fz = F(z);
        return new Vector3(116f * fy - 16f, 500f * (fx - fy), 200f * (fy - fz));
    }

    /// <summary>CIE76 colour difference: about 2.3 is just noticeable; 20 or more reads as a different colour at a glance.</summary>
    public static float DeltaE(Color a, Color b) => Vector3.Distance(Lab(a), Lab(b));

    public enum Cvd { Deuteranopia, Protanopia }

    // Machado, Oliveira and Fernandes (2009), severity 1.0, applied to linear RGB.
    static readonly float[,] Deutan = { { 0.367322f, 0.860646f, -0.227968f }, { 0.280085f, 0.672501f, 0.047413f }, { -0.011820f, 0.042940f, 0.968881f } };
    static readonly float[,] Protan = { { 0.152286f, 1.052583f, -0.204868f }, { 0.114503f, 0.786281f, 0.099216f }, { -0.003882f, -0.048116f, 1.051998f } };

    /// <summary>How a display colour looks to a player with full deuteranopia or protanopia.</summary>
    public static Color SimulateCvd(Color srgb, Cvd kind)
    {
        var m = kind == Cvd.Deuteranopia ? Deutan : Protan;
        float r = ToLinear(srgb.r), g = ToLinear(srgb.g), b = ToLinear(srgb.b);
        float Row(int i) => Mathf.Clamp01(m[i, 0] * r + m[i, 1] * g + m[i, 2] * b);
        return new Color(ToSrgb(Row(0)), ToSrgb(Row(1)), ToSrgb(Row(2)), srgb.a);
    }
}
```

Create `unity/Assets/Tests/EditMode/ShaderChecks.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Compiles a shader for GLES3 (WebGL 2) and returns the errors. Each pass is compiled for vertex and fragment, in
/// its base variant and with the main-light-shadow keyword (the variant the web tier draws the map with). A pass whose
/// source excludes GLES3 (`#pragma exclude_renderers ... gles3 ...`, or `#pragma only_renderers` without gles3) is skipped:
/// Shader Graph's Terrain Lit target emits DOTS subshaders at target 4.5 that are built for other APIs. The check cannot
/// pass vacuously: at least one subshader must have passes that are built for GLES3 and all compile.</summary>
public static class ShaderChecks
{
    static readonly string[][] Variants = { Array.Empty<string>(), new[] { "_MAIN_LIGHT_SHADOWS" } };
    static readonly Regex Exclude = new Regex(@"#pragma\s+exclude_renderers\s+([^\r\n]*)");
    static readonly Regex Only = new Regex(@"#pragma\s+only_renderers\s+([^\r\n]*)");

    public static bool ExcludesGles3(string passSource)
    {
        foreach (Match m in Exclude.Matches(passSource ?? "")) if (Regex.IsMatch(m.Groups[1].Value, @"\bgles3\b")) return true;
        foreach (Match m in Only.Matches(passSource ?? "")) if (!Regex.IsMatch(m.Groups[1].Value, @"\bgles3\b")) return true;
        return false;
    }

    public static List<string> Gles3Errors(Shader shader)
    {
        if (shader == null) throw new ArgumentNullException(nameof(shader));
        var errors = new List<string>();
        int usable = 0;
        var data = ShaderUtil.GetShaderData(shader);
        for (int s = 0; s < data.SubshaderCount; s++)
        {
            var sub = data.GetSerializedSubshader(s);
            var subErrors = new List<string>();
            int built = 0;
            for (int p = 0; p < sub.PassCount; p++)
            {
                var pass = sub.GetPass(p);
                if (ExcludesGles3(pass.SourceCode)) continue;
                built++;
                foreach (var keywords in Variants)
                    foreach (var stage in new[] { ShaderType.Vertex, ShaderType.Fragment })
                    {
                        var info = pass.CompileVariant(stage, keywords, ShaderCompilerPlatform.GLES3x, BuildTarget.WebGL);
                        var where = $"{shader.name} subshader {s} pass '{pass.Name}' {stage} [{string.Join(" ", keywords)}]";
                        foreach (var m in info.Messages)
                            if (m.severity == ShaderCompilerMessageSeverity.Error) subErrors.Add($"{where}: {m.message} (line {m.line})");
                        if (!info.Success && info.Messages.Length == 0) subErrors.Add($"{where}: failed without a message");
                    }
            }
            if (built > 0 && subErrors.Count == 0) usable++;
            errors.AddRange(subErrors);
        }
        if (usable == 0) errors.Add($"{shader.name}: no subshader whose passes are built for GLES3 and all compile");
        return errors;
    }
}
```

Create `unity/Assets/Tests/EditMode/VisualHarnessTests.cs`:

```csharp
#nullable enable
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

/// <summary>The visual track's instruments (plan Task 1): profile lines, flags, toggles, screenshot chunks, poses,
/// teardown hand-off, the colour metrics and the GLES3 compile check.</summary>
public class VisualHarnessTests
{
    [TearDown] public void ClearToggles() => VisualToggles.Clear();

    [Test]
    public void ProfileLinesShareTheAverageAndP95AndStateTheResolution()
    {
        var frames = Enumerable.Range(1, 100).Select(i => (float)i).ToList();
        Assert.AreEqual("PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=2 frames=100 avg_ms=50.50 p95_ms=95.00",
            FrameProfile.Line("all", "gameplay03", "1920x1080", 0.8f, 2, frames));
        Assert.AreEqual("PROFILE pixelError=1 frames=100 avg_ms=50.50 p95_ms=95.00", PixelErrorProfile.Line(1f, frames));
        Assert.Throws<System.ArgumentException>(() => FrameStats.Summary(new List<float>()));
    }

    [Test]
    public void TheSummaryReportsTheMedianAndTheSpreadOfTheRepeats()
    {
        Assert.AreEqual("PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=6.00 spread_ms=2.00 median_p95_ms=9.00",
            FrameProfile.Summary("-grid", "diorama02", new[] { (5f, 8f), (7f, 10f), (6f, 9f) }));
    }

    [Test]
    public void RepeatsRunTheConfigurationsInInterleavedOrders()
    {
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, FrameProfile.Order(4, 0).ToArray());
        CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, FrameProfile.Order(4, 1).ToArray());
        CollectionAssert.AreEqual(new[] { 2, 3, 0, 1 }, FrameProfile.Order(4, 2).ToArray());
        for (int rep = 0; rep < 6; rep++)
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 7), FrameProfile.Order(7, rep), "each repeat runs every configuration once");
    }

    [TestCase("http://localhost:8000/index.html?vprofile", true)]
    [TestCase("http://localhost:8000/index.html?profile", false)]
    [TestCase("https://html.itch.zone/html/1/vprofile-x/index.html", false)]
    public void TheVisualProfileFlagIsAWholeQueryParameter(string url, bool expected) =>
        Assert.AreEqual(expected, FrameProfile.Requested(url, new string[0], true));

    [Test]
    public void DesktopPlayersTakeDashFlags()
    {
        Assert.IsTrue(FrameProfile.Requested(null, new[] { "Statecraft", "-vprofile" }, false));
        Assert.IsTrue(VisualShots.Requested(null, new[] { "Statecraft", "-shot" }, false));
        Assert.IsFalse(VisualShots.Requested("http://localhost/index.html?shots", new string[0], true));
        Assert.IsTrue(VisualShots.Requested("http://localhost/index.html?golden&shot", new string[0], true));
    }

    [Test]
    public void TogglesRunInNameOrderAndUnknownNamesThrow()
    {
        var log = new List<string>();
        VisualToggles.Register("tiltshift", on => log.Add("tiltshift " + on));
        VisualToggles.Register("grade", on => log.Add("grade " + on));
        CollectionAssert.AreEqual(new[] { "grade", "tiltshift" }, VisualToggles.Names.ToArray());
        VisualToggles.SetAll(false);
        CollectionAssert.AreEqual(new[] { "grade False", "tiltshift False" }, log);
        Assert.Throws<KeyNotFoundException>(() => VisualToggles.Set("fog", true));
        VisualToggles.Register("grade", on => log.Add("grade again " + on));   // re-registering replaces
        VisualToggles.Set("grade", true);
        Assert.AreEqual("grade again True", log.Last());
    }

    [Test]
    public void ShotsSplitIntoNumberedChunksAfterAHashedHeader()
    {
        var png = new byte[20000]; for (int i = 0; i < png.Length; i++) png[i] = (byte)(i * 7);
        var lines = VisualShots.Lines("gameplay03", png).ToList();
        var head = lines[0].Split(' ');
        Assert.AreEqual(new[] { "SHOT", "gameplay03", "begin" }, head.Take(3).ToArray());
        int n = int.Parse(head[3]);
        Assert.AreEqual(64, head[4].Length, "SHA-256 in hex");
        Assert.AreEqual(n + 1, lines.Count);
        StringAssert.StartsWith($"SHOT gameplay03 1/{n} ", lines[1]);
        Assert.IsTrue(lines.Skip(1).All(l => l.Split(' ')[3].Length <= VisualShots.ChunkChars));
        CollectionAssert.AreEqual(png, System.Convert.FromBase64String(string.Concat(lines.Skip(1).Select(l => l.Split(' ')[3]))));
    }

    [Test]
    public void PromoPosesAreInsideTheMapAndTheZoomRange()
    {
        Assert.AreEqual(new[] { "gameplay03", "diorama02" }, PromoPose.All.Select(p => p.Name).ToArray());
        foreach (var p in PromoPose.All)
        {
            Assert.That(p.Distance, Is.InRange(CameraController.MinDistance, CameraController.MaxDistance), p.Name);
            Assert.That(p.Lon, Is.InRange(34.2, 35.9), p.Name); Assert.That(p.Lat, Is.InRange(29.45, 33.35), p.Name);
        }
    }

    [Test]
    public void TheMapRunsItsTeardownActionsNewestFirstWhenDestroyed()
    {
        var go = new GameObject("assets"); var assets = go.AddComponent<MapRuntimeAssets>();
        var order = new List<int>();
        assets.OnTeardown(() => order.Add(1)); assets.OnTeardown(() => order.Add(2));
        Object.DestroyImmediate(go);
        CollectionAssert.AreEqual(new[] { 2, 1 }, order);
    }

    [Test]
    public void ColourMetricsBehave()
    {
        Assert.AreEqual(0f, RenderSample.DeltaE(Color.grey, Color.grey), 1e-4f);
        Assert.AreEqual(100f, RenderSample.Lab(Color.white).x, 0.1f, "white is L* 100");
        Assert.Greater(RenderSample.DeltaE(Color.red, Color.green), 80f);
        var grey = new Color(0.5f, 0.5f, 0.5f);
        foreach (var kind in new[] { RenderSample.Cvd.Deuteranopia, RenderSample.Cvd.Protanopia })
            Assert.Less(RenderSample.DeltaE(grey, RenderSample.SimulateCvd(grey, kind)), 1f, $"{kind} keeps greys");
        var red = RenderSample.SimulateCvd(new Color(0.8f, 0.1f, 0.1f), RenderSample.Cvd.Deuteranopia);
        var green = RenderSample.SimulateCvd(new Color(0.1f, 0.6f, 0.1f), RenderSample.Cvd.Deuteranopia);
        Assert.Less(RenderSample.DeltaE(red, green), RenderSample.DeltaE(new Color(0.8f, 0.1f, 0.1f), new Color(0.1f, 0.6f, 0.1f)) * 0.5f,
            "red and green move together for a deuteranope");
    }

    static Shader Make(string source) => ShaderUtil.CreateShaderAsset(source, false);
    const string Good = "Pass { HLSLPROGRAM\n#pragma vertex v\n#pragma fragment f\nfloat4 v(float4 p : POSITION) : SV_POSITION { return p; }\nfloat4 f() : SV_Target { return 1; }\nENDHLSL }";
    const string Broken = "Pass { HLSLPROGRAM\n#pragma vertex v\n#pragma fragment f\nfloat4 v(float4 p : POSITION) : SV_POSITION { return p; }\nfloat4 f() : SV_Target { return undefined_symbol; }\nENDHLSL }";
    const string ExcludedBroken = "Pass { HLSLPROGRAM\n#pragma exclude_renderers gles3 glcore\n#pragma vertex v\n#pragma fragment f\nfloat4 v(float4 p : POSITION) : SV_POSITION { return p; }\nfloat4 f() : SV_Target { return undefined_symbol; }\nENDHLSL }";

    [Test]
    public void TheGles3CheckPassesAGoodShaderAndCatchesABrokenOne()
    {
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/HexSelection")));
        var broken = Make("Shader \"Test/Broken\" { SubShader { " + Broken + " } }");
        try { CollectionAssert.IsNotEmpty(ShaderChecks.Gles3Errors(broken)); }
        finally { Object.DestroyImmediate(broken); }
    }

    /// <summary>Negative controls (review C2): a subshader built for other APIs is not this check's business, but a shader
    /// with nothing built for GLES3 fails.</summary>
    [Test]
    public void SubshadersThatExcludeGles3AreSkippedButOneMustCompile()
    {
        Assert.IsTrue(ShaderChecks.ExcludesGles3("#pragma target 4.5\n#pragma exclude_renderers gles3 glcore\n"));
        Assert.IsTrue(ShaderChecks.ExcludesGles3("#pragma only_renderers metal vulkan d3d11\n"));
        Assert.IsFalse(ShaderChecks.ExcludesGles3("#pragma only_renderers gles3 metal\n#pragma exclude_renderers d3d11\n"));
        var mixed = Make("Shader \"Test/Mixed\" { SubShader { " + ExcludedBroken + " } SubShader { " + Good + " } }");
        var none = Make("Shader \"Test/None\" { SubShader { " + ExcludedBroken + " } }");
        try
        {
            CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(mixed), "the excluded subshader is skipped, the other compiles");
            var errors = ShaderChecks.Gles3Errors(none);
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            StringAssert.Contains("no subshader", errors[0]);
        }
        finally { Object.DestroyImmediate(mixed); Object.DestroyImmediate(none); }
    }
}
```

- [ ] **Step 3: Run them and confirm they fail**

Run:
```bash
"$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -m3 "error CS" unity-tests.log
```
Expected: `exit=1`; `error CS0103: The name 'FrameProfile' does not exist in the current context` (and the same for `VisualToggles`, `VisualShots`, `PromoPose`, `FrameStats`, `OnTeardown`, `RenderSample.DeltaE`, `ShaderChecks.ExcludesGles3`).

- [ ] **Step 4: Implement the runtime pieces**

Create `unity/Assets/Scripts/Map/FrameStats.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.Map
{
    /// <summary>Average and 95th-percentile frame time (ms), shared by the terrain and visual profiles.</summary>
    public static class FrameStats
    {
        public static (float avg, float p95) Summary(IReadOnlyList<float> frameMs)
        {
            if (frameMs == null) throw new ArgumentNullException(nameof(frameMs));
            if (frameMs.Count == 0) throw new ArgumentException("no frames were measured", nameof(frameMs));
            var sorted = frameMs.OrderBy(x => x).ToList();
            var p95 = sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(sorted.Count * 0.95) - 1)];
            return (sorted.Average(), p95);
        }
    }
}
```

In `unity/Assets/Scripts/Map/PixelErrorProfile.cs`, replace the body of `Line`

```csharp
            if (frameMs == null) throw new ArgumentNullException(nameof(frameMs));
            if (frameMs.Count == 0) throw new ArgumentException("no frames were measured", nameof(frameMs));
            var sorted = frameMs.OrderBy(x => x).ToList();
            var p95 = sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(sorted.Count * 0.95) - 1)];
            return string.Format(CultureInfo.InvariantCulture, "PROFILE pixelError={0:0} frames={1} avg_ms={2:0.00} p95_ms={3:0.00}",
                pixelError, sorted.Count, sorted.Average(), p95);
```

with

```csharp
            var (avg, p95) = FrameStats.Summary(frameMs);
            return string.Format(CultureInfo.InvariantCulture, "PROFILE pixelError={0:0} frames={1} avg_ms={2:0.00} p95_ms={3:0.00}",
                pixelError, frameMs.Count, avg, p95);
```

(`FrameStats.Summary` throws the same exceptions with the same parameter name, so M3's `PixelErrorProfileTests` keep passing.)

Create `unity/Assets/Scripts/Map/PromoPose.cs`:

```csharp
#nullable enable
using System.Collections.Generic;

namespace Statecraft.Game.Map
{
    /// <summary>Fixed camera poses that frame the same ground as the approved promo images, so every art checkpoint compares
    /// like with like. Yaw 0 looks north (the sea on the left), as both promo images do.</summary>
    public readonly struct PromoPose
    {
        public readonly string Name; public readonly double Lon, Lat; public readonly float Distance, Yaw;

        public PromoPose(string name, double lon, double lat, float distance, float yaw)
        { Name = name; Lon = lon; Lat = lat; Distance = distance; Yaw = yaw; }

        /// <summary>promo/final/03-gameplay-1936.png: Jaffa and Tel Aviv at the left, Lydda and Ramla to the right, the sea
        /// top-left, Qalqilya top-right. Matched on hex count (about nine across, so about 60 km), not on geography: the
        /// promo's Jaffa-to-Ramla spread (about 17 km over 60% of the frame) would need a 25-30 km view, but under the
        /// owner's symbolic-scale ruling (2026-10-11) settlements will sprawl over neighbouring hexes, so the hex count is
        /// the scale the art track will match. The 6 km hero zoom stays available for close looks.</summary>
        public static readonly PromoPose Gameplay03 = new PromoPose("gameplay03", 34.86, 32.00, 60000f, 10f);

        /// <summary>promo/final/02-map-diorama.png: the country from the Negev northwards, the sea on the left, the Dead Sea
        /// right of centre; the camera's farthest zoom.</summary>
        public static readonly PromoPose Diorama02 = new PromoPose("diorama02", 35.05, 31.55, CameraController.MaxDistance, 0f);

        public static IReadOnlyList<PromoPose> All { get; } = new[] { Gameplay03, Diorama02 };

        public void Apply(CameraRig rig, GeoFrame frame)
        {
            rig.LookAt = frame.ToWorld(Lon, Lat, 0); rig.Distance = Distance; rig.Yaw = Yaw; rig.Apply();
        }
    }
}
```

Create `unity/Assets/Scripts/Map/VisualToggles.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Game.Map
{
    /// <summary>One on/off switch per visual feature (shadows, tilt-shift, grade, haze, grid...), registered by the feature
    /// itself, so the visual profile can price each one and tests can isolate it. Names run in ordinal order; registering
    /// a name again replaces its switch (a rebuilt map re-registers).</summary>
    public static class VisualToggles
    {
        static readonly SortedDictionary<string, Action<bool>> Switches = new SortedDictionary<string, Action<bool>>(StringComparer.Ordinal);

        public static void Register(string name, Action<bool> set)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("a toggle needs a name", nameof(name));
            Switches[name] = set ?? throw new ArgumentNullException(nameof(set));
        }

        public static void Unregister(string name) => Switches.Remove(name);
        public static IReadOnlyList<string> Names => Switches.Keys.ToList();

        public static void Set(string name, bool on)
        {
            if (!Switches.TryGetValue(name, out var set)) throw new KeyNotFoundException($"no visual toggle named '{name}' (known: {string.Join(", ", Switches.Keys)})");
            set(on);
        }

        public static void SetAll(bool on) { foreach (var set in Switches.Values.ToList()) set(on); }
        public static void Clear() => Switches.Clear();
    }
}
```

Create `unity/Assets/Scripts/Map/FrameProfile.cs`:

```csharp
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Boot;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Prices each visual feature on the real player. At every promo pose it runs every configuration (everything on:
    /// "all"; each toggle off alone: "-name"; everything off: "none") Repeats times, in a different interleaved order each
    /// repeat (Order), 300 frames after 60 warm-up frames each, and logs one line per run with the resolution and the URP
    /// render scale, then one summary line per configuration: the median of the run averages, their spread (max - min) and
    /// the median p95. A feature costs median(all) - median(-name); it is over a budget only when that exceeds the budget
    /// plus the larger spread (docs/visual-v1.md). Asked for by "vprofile" in the WebGL page URL (a whole query parameter)
    /// or -vprofile on desktop, where the player quits when done. Run it under tools/machine_lock.py --exclusive.</summary>
    public static class FrameProfile
    {
        public const int WarmupFrames = 60, MeasuredFrames = 300, Repeats = 3;

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool Requested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "vprofile") : args.Contains("-vprofile");

        public static string Line(string label, string pose, string resolution, float renderScale, int rep, IReadOnlyList<float> frameMs)
        {
            var (avg, p95) = FrameStats.Summary(frameMs);
            return string.Format(CultureInfo.InvariantCulture,
                "PROFILE visual={0} pose={1} res={2} scale={3:0.00} rep={4} frames={5} avg_ms={6:0.00} p95_ms={7:0.00}",
                label, pose, resolution, renderScale, rep, frameMs.Count, avg, p95);
        }

        public static string Summary(string label, string pose, IReadOnlyList<(float avg, float p95)> runs)
        {
            if (runs == null || runs.Count == 0) throw new ArgumentException("no runs", nameof(runs));
            float Median(IEnumerable<float> xs) { var s = xs.OrderBy(x => x).ToList(); return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2f; }
            var avgs = runs.Select(r => r.avg).ToList();
            return string.Format(CultureInfo.InvariantCulture,
                "PROFILE summary visual={0} pose={1} runs={2} median_avg_ms={3:0.00} spread_ms={4:0.00} median_p95_ms={5:0.00}",
                label, pose, runs.Count, Median(avgs), avgs.Max() - avgs.Min(), Median(runs.Select(r => r.p95)));
        }

        /// <summary>Run order of <paramref name="count"/> configurations in repeat <paramref name="rep"/>: forward, reversed,
        /// then forward from the middle, so no configuration always runs first (warm) or last (hot).</summary>
        public static IReadOnlyList<int> Order(int count, int rep)
        {
            var forward = Enumerable.Range(0, count).ToList();
            switch (rep % 3)
            {
                case 0: return forward;
                case 1: forward.Reverse(); return forward;
                default: return forward.Skip(count / 2).Concat(forward.Take(count / 2)).ToList();
            }
        }

        public static IEnumerator Run(CameraRig rig, GeoFrame frame, Action<string> log, bool quitWhenDone)
        {
            var configs = new List<(string label, string? off)> { ("all", null) };
            foreach (var name in VisualToggles.Names) configs.Add(("-" + name, name));
            configs.Add(("none", "*"));
            foreach (var pose in PromoPose.All)
            {
                pose.Apply(rig, frame);
                var results = configs.ToDictionary(c => c.label, c => new List<(float avg, float p95)>());
                for (int rep = 0; rep < Repeats; rep++)
                    foreach (var i in Order(configs.Count, rep))
                    {
                        var (label, off) = configs[i];
                        VisualToggles.SetAll(off != "*");
                        if (off != null && off != "*") VisualToggles.Set(off, false);
                        for (int f = 0; f < WarmupFrames; f++) yield return null;
                        var times = new List<float>(MeasuredFrames);
                        for (int f = 0; f < MeasuredFrames; f++) { yield return null; times.Add(Time.unscaledDeltaTime * 1000f); }
                        var scale = UniversalRenderPipeline.asset != null ? UniversalRenderPipeline.asset.renderScale : 1f;
                        log(Line(label, pose.Name, $"{Screen.width}x{Screen.height}", scale, rep, times));
                        results[label].Add(FrameStats.Summary(times));
                    }
                foreach (var (label, _) in configs) log(Summary(label, pose.Name, results[label]));
            }
            VisualToggles.SetAll(true);
            log("PROFILE visual done");
            if (quitWhenDone) Application.Quit(0);
        }
    }
}
```

Create `unity/Assets/Scripts/Map/VisualShots.cs`:

```csharp
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Statecraft.Game.Boot;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>Screenshots of the running player at each promo pose, written to the console as base64 chunks, because a
    /// WebGL page cannot write files: "SHOT &lt;pose&gt; begin &lt;n&gt; &lt;sha256&gt;", then n lines "SHOT &lt;pose&gt; &lt;i&gt;/&lt;n&gt; &lt;data&gt;",
    /// and "SHOT done" at the end. tools/web_check.py --shots reassembles and checks them. Asked for by "shot" in the page
    /// URL or -shot on desktop.</summary>
    public static class VisualShots
    {
        public const int SettleFrames = 30;
        public const int ChunkChars = 8000;   // well under any console line limit; a cut line fails the SHA-256 check

        public static bool Requested() =>
            Requested(Application.absoluteURL, Environment.GetCommandLineArgs(), Application.platform == RuntimePlatform.WebGLPlayer);

        public static bool Requested(string? url, string[] args, bool webgl) =>
            webgl ? UrlFlags.Has(url, "shot") : args.Contains("-shot");

        public static IEnumerable<string> Lines(string name, byte[] png)
        {
            var b64 = Convert.ToBase64String(png);
            int n = Math.Max(1, (b64.Length + ChunkChars - 1) / ChunkChars);
            string hash;
            using (var sha = SHA256.Create()) hash = string.Concat(sha.ComputeHash(png).Select(b => b.ToString("x2")));
            yield return $"SHOT {name} begin {n} {hash}";
            for (int i = 0; i < n; i++)
                yield return $"SHOT {name} {i + 1}/{n} {b64.Substring(i * ChunkChars, Math.Min(ChunkChars, b64.Length - i * ChunkChars))}";
        }

        public static IEnumerator Run(CameraRig rig, GeoFrame frame, Action<string> log)
        {
            foreach (var pose in PromoPose.All)
            {
                pose.Apply(rig, frame);
                for (int i = 0; i < SettleFrames; i++) yield return null;
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                try { foreach (var line in Lines(pose.Name, tex.EncodeToPNG())) log(line); }
                finally { UnityEngine.Object.Destroy(tex); }
            }
            log("SHOT done");
        }
    }
}
```

In `unity/Assets/Scripts/Map/MapRuntimeAssets.cs`, replace

```csharp
        readonly List<Object> owned = new List<Object>();

        public T Own<T>(T o) where T : Object { owned.Add(o); return o; }

        void OnDestroy()
        {
```

with

```csharp
        readonly List<Object> owned = new List<Object>();
        readonly List<System.Action> teardown = new List<System.Action>();

        public T Own<T>(T o) where T : Object { owned.Add(o); return o; }

        /// <summary>Runs <paramref name="action"/> when the map is destroyed (newest first), for state the map changes outside
        /// itself: the URP asset's shadow distance, global shader values.</summary>
        public void OnTeardown(System.Action action) => teardown.Add(action ?? throw new System.ArgumentNullException(nameof(action)));

        void OnDestroy()
        {
            for (int i = teardown.Count - 1; i >= 0; i--) teardown[i]();
            teardown.Clear();
```

In `unity/Assets/Scripts/Map/CameraRig.cs`:
1. Replace `using UnityEngine;` (the first line) with `using System;\nusing System.Collections.Generic;\nusing UnityEngine;`.
2. Replace

```csharp
        DepthOfField _dof;
```

   with

```csharp
        DepthOfField _dof;

        /// <summary>Undo actions for state Init changes outside the rig (the URP asset's shadow distance...). MapScene.Build hands
        /// them to the map's MapRuntimeAssets right after Init, so destroying the map undoes them.</summary>
        public readonly List<Action> TeardownActions = new List<Action>();
```

In `unity/Assets/Scripts/Map/MapScene.cs`, replace

```csharp
            assets.Own(rig.Volume.profile);
        }
```

with

```csharp
            assets.Own(rig.Volume.profile);
            foreach (var undo in rig.TeardownActions) assets.OnTeardown(undo);
            rig.TeardownActions.Clear();
        }
```

In `unity/Assets/Scripts/UI/GameRoot.cs`, replace

```csharp
            if (Application.isPlaying && PixelErrorProfile.Requested()) StartCoroutine(PixelErrorProfile.Run(map.Terrain));
```

with

```csharp
            if (Application.isPlaying && PixelErrorProfile.Requested()) StartCoroutine(PixelErrorProfile.Run(map.Terrain));
            // The visual track's instruments (docs/visual-v1.md): both move the camera, so one per run.
            if (Application.isPlaying && FrameProfile.Requested())
                StartCoroutine(FrameProfile.Run(camRig, map.Frame, Debug.Log, Application.platform != RuntimePlatform.WebGLPlayer));
            else if (Application.isPlaying && VisualShots.Requested()) StartCoroutine(VisualShots.Run(camRig, map.Frame, Debug.Log));
```

In `unity/Assets/Editor/MapCapture.cs`, add after the `Flythrough` method:

```csharp
    /// <summary>Renders each promo pose (PromoPose.All) at 1920x1080 once per quality tier (0 = Mobile, the WebGL tier;
    /// 1 = PC) into &lt;-captureDir&gt; (default captures/visual/editor) as &lt;tier&gt;-&lt;pose&gt;.png. Editor renders show no UI
    /// Toolkit labels; tools/art_checkpoint.sh pairs them with the WebGL screenshots, which do.</summary>
    public static void Promo()
    {
        var args = System.Environment.GetCommandLineArgs();
        int at = System.Array.IndexOf(args, "-captureDir");
        var dir = at >= 0 && at + 1 < args.Length ? args[at + 1] : Path.Combine(CaptureDir, "visual", "editor");
        var map = OpenAndBuild();
        var rig = Camera.main.GetComponent<CameraRig>();
        int saved = QualitySettings.GetQualityLevel();
        try
        {
            for (int level = 0; level < QualitySettings.names.Length; level++)
            {
                QualitySettings.SetQualityLevel(level, true);
                foreach (var pose in PromoPose.All)
                {
                    pose.Apply(rig, map.Frame);
                    Save(Camera.main, 1920, 1080, Path.Combine(dir, $"{QualitySettings.names[level]}-{pose.Name}.png"));
                }
            }
        }
        finally { QualitySettings.SetQualityLevel(saved, true); }
    }
```

- [ ] **Step 5: Run the Unity tests and confirm they pass**

Run:
```bash
"$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=0`; total = **B0 + 14** at first delivery (11 tests plus the 3 cases of the flag test; **B0 + 21** after Task 1 fix round 1), `failed="0"`; no warning lines. Write the total down as **B1**.

- [ ] **Step 6: Write the failing tools tests**

Create `tools/tests/test_web_check_shots.py`:

```python
import base64
import hashlib

import pytest

import web_check as wc


def shot_lines(name: str, png: bytes, chunk: int = 10) -> list[str]:
    b64 = base64.b64encode(png).decode()
    parts = [b64[i:i + chunk] for i in range(0, len(b64), chunk)]
    return [f"SHOT {name} begin {len(parts)} {hashlib.sha256(png).hexdigest()}"] + \
           [f"SHOT {name} {i + 1}/{len(parts)} {p}" for i, p in enumerate(parts)]


def test_a_shot_is_reassembled_and_written(tmp_path):
    png = bytes(range(256)) * 3
    c = wc.ShotCollector(tmp_path)
    assert all(c.feed(line) for line in shot_lines("gameplay03", png))
    assert (tmp_path / "gameplay03.png").read_bytes() == png
    assert c.written == [tmp_path / "gameplay03.png"]
    assert c.feed("SHOT done") is False          # printed and matched by --expect, not consumed
    assert c.feed("GAME OK 1948-05-15") is False


def test_parts_may_arrive_out_of_order(tmp_path):
    png = b"\x89PNG" + bytes(200)
    lines = shot_lines("diorama02", png)
    c = wc.ShotCollector(tmp_path)
    for line in [lines[0]] + list(reversed(lines[1:])):
        c.feed(line)
    assert (tmp_path / "diorama02.png").read_bytes() == png


def test_a_cut_line_fails_the_hash(tmp_path):
    lines = shot_lines("gameplay03", bytes(300))
    lines[2] = lines[2][:-3]
    c = wc.ShotCollector(tmp_path)
    with pytest.raises(ValueError, match="SHA-256"):
        for line in lines:
            c.feed(line)


def test_a_part_before_its_header_is_an_error(tmp_path):
    with pytest.raises(ValueError, match="before its begin line"):
        wc.ShotCollector(tmp_path).feed("SHOT gameplay03 1/2 AAAA")


def test_a_lost_part_fails_loudly_at_shot_done(tmp_path):
    lines = shot_lines("gameplay03", bytes(300))
    c = wc.ShotCollector(tmp_path)
    for line in lines[:2] + lines[3:]:           # one part line never arrives
        c.feed(line)
    with pytest.raises(ValueError, match=r"gameplay03: \d+ of \d+ parts"):
        c.feed("SHOT done")


def test_headless_chrome_runs_uncapped_at_dpr_1():
    flags = wc.chrome_flags("/tmp/p", swiftshader=False)
    for flag in ("--headless=new", "--disable-frame-rate-limit", "--disable-gpu-vsync", "--window-size=1920,1080",
                 "--user-data-dir=/tmp/p", "--force-device-scale-factor=1"):
        assert flag in flags
    assert "--use-angle=swiftshader" in wc.chrome_flags("/tmp/p", swiftshader=True)
```

Create `tools/tests/test_art_compare.py`:

```python
import numpy as np
from PIL import Image

import art_compare as ac


def solid(rgb, size=(64, 36)):
    return Image.new("RGB", size, rgb)


def noisy(size=(192, 108), seed=1):
    return Image.fromarray(np.random.default_rng(seed).integers(0, 255, (size[1], size[0], 3), dtype=np.uint8), "RGB")


def test_metrics_read_saturation_contrast_and_warmth():
    grey = ac.metrics(solid((128, 128, 128)))
    assert grey["saturation"] == 0 and grey["contrast"] == 0 and grey["warmth"] == 0
    red = ac.metrics(solid((255, 0, 0)))
    assert red["saturation"] > 0.99 and red["warmth"] > 0.99


def test_palette_distance_is_zero_for_the_same_image_and_one_for_disjoint_palettes():
    a = solid((200, 150, 60))
    assert ac.histogram_distance(a, a) == 0
    assert ac.histogram_distance(solid((0, 0, 0)), solid((255, 255, 255))) == 1


def test_compose_puts_both_images_at_the_same_height_under_a_caption():
    out = ac.compose(solid((10, 20, 30), (1920, 1080)), solid((200, 100, 50), (2752, 1536)), "t")
    assert out.height == ac.CAPTION_PX + ac.HEIGHT_PX
    assert out.width == round(1920 * ac.HEIGHT_PX / 1080) + ac.GAP_PX + round(2752 * ac.HEIGHT_PX / 1536)


def test_colour_blind_simulation_keeps_greys_and_merges_red_with_green():
    grey = np.asarray(ac.simulate_cvd(solid((128, 128, 128)), "deuteranopia"), dtype=int)
    assert np.abs(grey - 128).max() <= 1
    red = np.asarray(ac.simulate_cvd(solid((200, 30, 30)), "protanopia"), dtype=int)[0, 0]
    green = np.asarray(ac.simulate_cvd(solid((30, 160, 30)), "protanopia"), dtype=int)[0, 0]
    assert abs(int(red[0]) - int(green[0])) < 100      # 170 apart before simulation


def test_main_pairs_each_pose_with_its_promo_logs_a_row_and_writes_cvd_copies(tmp_path):
    promo = tmp_path / "promo.png"; noisy((275, 153), 2).save(promo)
    shots = tmp_path / "shots"; (shots / "editor").mkdir(parents=True)
    noisy().save(shots / "gameplay03.png")
    noisy(seed=3).save(shots / "editor" / "PC-diorama02.png")
    noisy(seed=4).save(shots / "unrelated.png")
    log = tmp_path / "elsewhere" / "art-log.tsv"
    code = ac.main(["art_compare.py", str(shots), "task-x", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo})
    assert code == 0
    for stem in ("gameplay03", "editor/PC-diorama02"):
        for suffix in ("-compare.png", "-compare-deuteranopia.png", "-compare-protanopia.png"):
            assert (shots / (stem + suffix)).exists(), stem + suffix
    rows = log.read_text().splitlines()
    assert len(rows) == 2 and all(r.startswith("task-x\t") for r in rows)
    assert ac.main(["art_compare.py", str(shots), "again", "--log", str(log)], root=tmp_path,
                   promos={"gameplay03": promo, "diorama02": promo}) == 0
    assert not (shots / "gameplay03-compare-compare.png").exists()


def test_a_uniform_screenshot_fails(tmp_path):
    promo = tmp_path / "promo.png"; noisy().save(promo)
    shots = tmp_path / "shots"; shots.mkdir()
    solid((0, 0, 0), (192, 108)).save(shots / "gameplay03.png")       # a black capture (the WebGL readback failed)
    assert ac.main(["art_compare.py", str(shots), "t"], root=tmp_path, promos={"gameplay03": promo, "diorama02": promo}) == 2
```

Create `tools/tests/test_machine_lock.py`:

```python
import fcntl
import os
import subprocess
import sys
import time
from pathlib import Path

import pytest

import machine_lock as ml


def test_the_command_follows_the_double_dash_and_a_mode_is_required():
    a = ml.parse(["--exclusive", "--wait-unity", "--", "uv", "run", "tools/web_check.py", "--query", "vprofile"])
    assert a.exclusive and a.wait_unity and a.command == ["uv", "run", "tools/web_check.py", "--query", "vprofile"]
    assert ml.parse(["--shared", "--", "echo", "x"]).command == ["echo", "x"]
    with pytest.raises(SystemExit):
        ml.parse(["--", "echo"])
    with pytest.raises(SystemExit):
        ml.parse(["--shared"])


def test_waiting_ends_when_no_unity_process_is_left():
    seen = iter([[101, 102], [102], []])
    clock = iter(range(100))
    assert ml.wait_for_unity(600, pids=lambda: next(seen), sleep=lambda s: None, now=lambda: next(clock)) is True


def test_waiting_gives_up_at_the_timeout():
    clock = iter(range(0, 1000, 100))
    assert ml.wait_for_unity(250, pids=lambda: [7], sleep=lambda s: None, now=lambda: next(clock)) is False


def test_a_shared_holder_really_blocks_an_exclusive_lock(tmp_path):
    lock = tmp_path / "machine.lock"
    env = {**os.environ, "STATECRAFT_MACHINE_LOCK": str(lock)}
    child = subprocess.Popen([sys.executable, ml.__file__, "--shared", "--", sys.executable, "-c", "import time; time.sleep(5)"],
                             env=env, stdout=subprocess.PIPE, text=True)
    try:
        assert "taking the machine lock (shared)" in child.stdout.readline()
        blocked, deadline = False, time.time() + 4
        while not blocked and time.time() < deadline:
            with open(lock, "a+") as f:
                try:
                    fcntl.flock(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
                    fcntl.flock(f, fcntl.LOCK_UN)
                    time.sleep(0.05)
                except BlockingIOError:
                    blocked = True
        assert blocked, "an exclusive lock was granted while the shared holder ran"
    finally:
        child.terminate()
        child.wait(10)


def test_an_editor_holding_a_worktree_lockfile_counts_as_unity(tmp_path):
    temp = tmp_path / "a" / "unity" / "Temp"
    temp.mkdir(parents=True)
    (temp / "UnityLockfile").write_text("")
    (tmp_path / "b").mkdir()
    asked = []
    pids = ml.lockfile_holders([tmp_path / "a", tmp_path / "b"], lsof=lambda files: (asked.extend(files), "4242\n4242\n")[1])
    assert pids == [4242]
    assert asked == [str(temp / "UnityLockfile")]
    assert ml.lockfile_holders([tmp_path / "b"], lsof=lambda files: "1\n") == []
```

Append to `tools/tests/test_headed.py`:

```python


def test_boot_phases_and_the_map_build_span():
    lines = ["BOOT phase=player started at=900 ms dt=900 ms", "BOOT phase=heightmap decoded at=5200 ms dt=1300 ms",
             "GAME OK 1948-05-15 turn 0 hash 00", "BOOT phase=map built at=9400 ms dt=300 ms"]
    phases = headed.boot_phases(lines)
    assert phases[0] == ("player started", 900, 900)
    assert headed.map_build_ms(phases) == 9400 - 5200 + 1300
    with pytest.raises(ValueError, match="map built"):
        headed.map_build_ms(phases[:2])


def test_the_boot_budget_compares_the_median_with_the_baseline():
    assert headed.over_budget([5500, 5700, 5900], 4800, 1000) == (5700, False)   # 5700 <= 5800
    assert headed.over_budget([5500, 5700, 5900], 4600, 1000) == (5700, True)    # 5700 > 5600
    assert headed.over_budget([5500], None, 1000) == (5500, False)


def test_profile_windows_are_uncapped_at_the_requested_dpr():
    plain = headed.browser_flags("/tmp/p", 1568, 752)
    assert "--use-angle=metal" in plain and "--window-size=1568,752" in plain and plain[-1] == "about:blank"
    assert not any("scale-factor" in f or "frame-rate" in f for f in plain), "navpairs and boot keep their old flags"
    prof = headed.browser_flags("/tmp/p", 1920, 1080, dpr=2, uncapped=True)
    for flag in ("--force-device-scale-factor=2", "--disable-frame-rate-limit", "--disable-gpu-vsync", "--window-size=1920,1080"):
        assert flag in prof
    assert prof[-1] == "about:blank"
```

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests/test_web_check_shots.py tools/tests/test_art_compare.py tools/tests/test_machine_lock.py tools/tests/test_headed.py -q 2>&1 | tail -3
```
Expected: FAIL: `AttributeError: module 'web_check' has no attribute 'ShotCollector'`, `ModuleNotFoundError` for `art_compare` and `machine_lock`, and `AttributeError: module 'headed' has no attribute 'boot_phases'`.

- [ ] **Step 7: Implement the tools**

Replace `tools/web_check.py` with:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Serve a Unity WebGL build on http://localhost and open it in Chrome; pass when the console shows every expected line,
fail on a failure line or a timeout. Uses Chrome for Testing from the Playwright cache (macOS only, no npm needed) in
--headless=new mode (the cached chrome-headless-shell has no WebGL for Unity): console messages reach stderr with
--enable-logging=stderr --v=1 as INFO:CONSOLE... "text", source: ...

Usage (from the repo root):
  uv run tools/web_check.py unity/Builds/Web-disabled --query "golden" --expect "GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07" --expect "GOLDEN OK 1952 73590e324f19e9959a455a4da79e93d7089dfcf283c13aac02af6b0a624b9767"
Options: --query Q (page URL query, without '?'), --expect TEXT (repeatable), --fail REGEX (default: GOLDEN FAIL|SMOKE FAIL|BOOT FAIL),
--timeout SECONDS (default 300), --swiftshader (software WebGL when there is no GPU), --shots DIR (reassemble the player's
"SHOT" console lines, from ?shot, into DIR/<pose>.png; a "SHOT done" with an unfinished shot fails the run). Chrome runs at
device pixel ratio 1. Headed runs (a visible window, another DPR) go through tools/headed.py.
Pre-compressed builds: *.br and *.gz files are served with Content-Encoding, *.wasm(.br/.gz) as application/wasm.
Brotli needs a secure context; http://localhost is one."""
import argparse
import base64
import functools
import hashlib
import http.server
import os
import re
import subprocess
import sys
import queue
import shutil
import tempfile
import threading
import time
from pathlib import Path

# A console message's first line; warnings and errors carry a stack trace on later lines (ProjectSetup sets none for
# Debug.Log), so the closing quote and ", source:" may be far below. Only the first line is matched.
CONSOLE = re.compile(r'INFO:CONSOLE[^\]]*\] "(.*?)(?:", source: .*)?$')
CHROME_GLOB = "ms-playwright/chromium-*/chrome-mac*/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing"
SHOT_BEGIN = re.compile(r"^SHOT (\S+) begin (\d+) ([0-9a-f]{64})$")
SHOT_PART = re.compile(r"^SHOT (\S+) (\d+)/(\d+) (\S+)$")


class Handler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        path = self.path.split("?")[0]
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def guess_type(self, path):
        base = re.sub(r"\.(br|gz)$", "", str(path))
        if base.endswith(".wasm"):
            return "application/wasm"
        if base.endswith(".js"):
            return "application/javascript"
        if base.endswith((".data", ".r16")):
            return "application/octet-stream"
        return super().guess_type(base)

    def log_message(self, fmt, *args):
        if os.environ.get("WEB_CHECK_VERBOSE"):
            print("http:", fmt % args)


class ShotCollector:
    """Reassembles the PNGs Statecraft.Game.Map.VisualShots logs: "SHOT <pose> begin <n> <sha256>" then n lines
    "SHOT <pose> <i>/<n> <base64>" (any order). A finished shot is checked against its SHA-256 and written to
    <out_dir>/<pose>.png (out_dir None: checked, not written)."""

    def __init__(self, out_dir: Path | None):
        self.out_dir = out_dir
        self.expect: dict[str, tuple[int, str]] = {}
        self.parts: dict[str, dict[int, str]] = {}
        self.written: list[Path] = []

    def feed(self, text: str) -> bool:
        """True when text was a begin or part line (consumed: too long to print); False for anything else. "SHOT done"
        while a shot is unfinished (a part line was lost) raises ValueError naming it."""
        if text == "SHOT done" and self.expect:
            unfinished = ", ".join(f"{n}: {len(self.parts.get(n, {}))} of {k} parts" for n, (k, _) in self.expect.items())
            raise ValueError(f"SHOT done with unfinished shots ({unfinished})")
        m = SHOT_BEGIN.match(text)
        if m:
            self.expect[m.group(1)] = (int(m.group(2)), m.group(3))
            self.parts[m.group(1)] = {}
            return True
        m = SHOT_PART.match(text)
        if not m:
            return False
        name, i = m.group(1), int(m.group(2))
        if name not in self.expect:
            raise ValueError(f"SHOT {name}: part {i}/{m.group(3)} before its begin line")
        self.parts[name][i] = m.group(4)
        if len(self.parts[name]) == self.expect[name][0]:
            self._finish(name)
        return True

    def _finish(self, name: str) -> None:
        n, digest = self.expect.pop(name)
        parts = self.parts.pop(name)
        try:
            png = base64.b64decode("".join(parts[i] for i in range(1, n + 1)), validate=True)
        except ValueError:                       # binascii.Error: a cut line leaves invalid base64
            png = b""
        if hashlib.sha256(png).hexdigest() != digest:
            raise ValueError(f"SHOT {name}: SHA-256 mismatch (a console line was cut or lost)")
        if self.out_dir is not None:
            self.out_dir.mkdir(parents=True, exist_ok=True)
            path = self.out_dir / f"{name}.png"
            path.write_bytes(png)
            self.written.append(path)
            print(f"shot: wrote {path}")


def find_chrome() -> Path:
    """The newest Chrome for Testing in the Playwright cache (macOS, arm64 or x64), by numeric revision."""
    found = list(Path.home().joinpath("Library", "Caches").glob(CHROME_GLOB))
    if not found:
        sys.exit("Chrome for Testing not found under ~/Library/Caches/ms-playwright/chromium-<rev>/chrome-mac*"
                 " (macOS only; install with: npx playwright install chromium)")

    def revision(path: Path) -> int:
        m = re.search(r"chromium-(\d+)", str(path))
        return int(m.group(1)) if m else -1
    return max(found, key=revision)


def chrome_flags(profile: str, swiftshader: bool) -> list[str]:
    """Headless Chrome at device pixel ratio 1, frame rate uncapped (so frame times are not the display's)."""
    flags = ["--headless=new", "--enable-logging=stderr", "--v=1", "--no-first-run", "--no-default-browser-check",
             "--autoplay-policy=no-user-gesture-required", "--window-size=1920,1080", "--force-device-scale-factor=1",
             "--disable-frame-rate-limit", "--disable-gpu-vsync", f"--user-data-dir={profile}"]
    if swiftshader:
        flags += ["--use-angle=swiftshader", "--enable-unsafe-swiftshader"]
    return flags


def serve(root: Path) -> http.server.ThreadingHTTPServer:
    server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), functools.partial(Handler, directory=str(root)))
    threading.Thread(target=server.serve_forever, daemon=True).start()
    return server


def scan(lines, expect, fail):
    """Reads stderr lines: returns (status, pending, texts). status is "fail" at the first failure message, "pass" when
    every expected text has been seen, else "pending". texts are the console messages found (first lines only)."""
    pending, texts = list(expect), []
    for raw in lines:
        m = CONSOLE.search(raw)
        if not m:
            continue
        text = m.group(1)
        texts.append(text)
        if fail.search(text):
            return "fail", pending, texts
        pending = [p for p in pending if p not in text]
    return ("pass" if not pending else "pending"), pending, texts


def drain(inbox: queue.Queue) -> list:
    batch = []
    while True:       # lines are dropped once scanned: only the batch is held
        try:
            batch.append(inbox.get_nowait())
        except queue.Empty:
            return batch


def watch(inbox, poll, join_reader, expect, fail, timeout, now=time.time, sleep=time.sleep, on_text=None) -> int:
    """The check loop: 0 on PASS, 1 on a failure line, on BROWSER EXITED or on TIMEOUT. poll() is the browser's
    returncode (None while running); join_reader() waits briefly for the stderr reader thread to hit EOF, so output
    written just before the exit is still scanned. on_text(text) -> True consumes a message (it is not printed)."""
    pending, deadline = list(expect), now() + timeout

    def look():
        nonlocal pending
        status, pending, texts = scan(drain(inbox), pending, fail)
        for t in texts:
            if on_text is not None and on_text(t):
                continue
            print("console:", t)
        if status == "fail":
            print(f"FAIL: {texts[-1]}")
            return 1
        if status == "pass":
            print("PASS: saw " + ", ".join(expect))
            return 0
        return None

    while now() < deadline:
        result = look()
        if result is not None:
            return result
        code = poll()
        if code is not None:
            join_reader()
            result = look()
            if result is not None:
                return result
            print(f"BROWSER EXITED code {code}; never saw " + ", ".join(pending))
            return 1
        sleep(0.2)
    print("TIMEOUT: never saw " + ", ".join(pending))
    return 1


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("build", type=Path)
    ap.add_argument("--query", default="")
    ap.add_argument("--expect", action="append", default=[])
    ap.add_argument("--fail", default=r"GOLDEN FAIL|SMOKE FAIL|BOOT FAIL")
    ap.add_argument("--timeout", type=float, default=300)
    ap.add_argument("--swiftshader", action="store_true")
    ap.add_argument("--shots", type=Path)
    a = ap.parse_args()
    if not (a.build / "index.html").exists():
        sys.exit(f"{a.build}/index.html not found")
    server, proc, profile = None, None, tempfile.mkdtemp(prefix="web-check-")
    shots = ShotCollector(a.shots)
    try:
        chrome = find_chrome()
        server = serve(a.build)
        url = f"http://localhost:{server.server_address[1]}/index.html" + (f"?{a.query}" if a.query else "")
        proc = subprocess.Popen([str(chrome), *chrome_flags(profile, a.swiftshader), url],
                                stderr=subprocess.PIPE, stdout=subprocess.DEVNULL, text=True, errors="replace")
        inbox: queue.Queue = queue.Queue()
        reader = threading.Thread(target=lambda: [inbox.put(l) for l in iter(proc.stderr.readline, "")], daemon=True)
        reader.start()
        try:
            return watch(inbox, proc.poll, lambda: reader.join(2), list(a.expect), re.compile(a.fail), a.timeout, on_text=shots.feed)
        except ValueError as e:
            print(f"FAIL: {e}")
            return 1
    finally:
        if proc is not None:
            proc.terminate()
            try:
                proc.wait(5)
            except subprocess.TimeoutExpired:
                proc.kill(); proc.wait()
        if server is not None:
            server.shutdown(); server.server_close()
        shutil.rmtree(profile, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
```

Create `tools/art_compare.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0"]
# ///
"""Owner art checkpoint: puts each screenshot next to the promo image its pose copies, prints rough numbers, and writes
colour-blind simulations of each comparison.

Usage: uv run tools/art_compare.py <shots-dir> <checkpoint-name> [--log PATH]
For every PNG under <shots-dir> whose name ends in a pose (gameplay03, diorama02), writes <name>-compare.png beside it
(screenshot left, promo right, a caption with the numbers), <name>-compare-deuteranopia.png and
<name>-compare-protanopia.png (Machado, Oliveira and Fernandes 2009, severity 1), and appends a row to the art log
(default captures/visual/art-log.tsv; art_checkpoint.sh points every worktree at the main checkout's): checkpoint, image,
saturation, contrast (luma std), warmth (mean R - B), and the palette distance to the promo (total variation between
8x8x8 RGB histograms; 0 = same palette, 1 = disjoint). The numbers inform the owner's review. Exit code 2 when a
screenshot is near-uniform (luma std < 0.01: a black or blank WebGL capture), 1 when no screenshot matched a pose."""
import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
PROMOS = {"gameplay03": ROOT / "promo" / "final" / "03-gameplay-1936.png",
          "diorama02": ROOT / "promo" / "final" / "02-map-diorama.png"}
CAPTION_PX, GAP_PX, HEIGHT_PX = 44, 16, 720
UNIFORM_STD = 0.01
MACHADO = {
    "deuteranopia": np.array([[0.367322, 0.860646, -0.227968], [0.280085, 0.672501, 0.047413], [-0.011820, 0.042940, 0.968881]]),
    "protanopia": np.array([[0.152286, 1.052583, -0.204868], [0.114503, 0.786281, 0.099216], [-0.003882, -0.048116, 1.051998]]),
}


def metrics(img: Image.Image) -> dict[str, float]:
    rgb = img.convert("RGB")
    a = np.asarray(rgb, dtype=np.float32) / 255.0
    s = np.asarray(rgb.convert("HSV"), dtype=np.float32)[..., 1] / 255.0
    luma = 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]
    return {"saturation": float(s.mean()), "contrast": float(luma.std()), "warmth": float((a[..., 0] - a[..., 2]).mean())}


def histogram_distance(a: Image.Image, b: Image.Image, bins: int = 8) -> float:
    def hist(img: Image.Image) -> np.ndarray:
        x = (np.asarray(img.convert("RGB").resize((256, 256)), dtype=np.int64) // (256 // bins)).reshape(-1, 3)
        h = np.bincount(x[:, 0] * bins * bins + x[:, 1] * bins + x[:, 2], minlength=bins ** 3).astype(np.float64)
        return h / h.sum()
    return float(0.5 * np.abs(hist(a) - hist(b)).sum())


def simulate_cvd(img: Image.Image, kind: str) -> Image.Image:
    a = np.asarray(img.convert("RGB"), dtype=np.float64) / 255.0
    lin = np.where(a <= 0.04045, a / 12.92, ((a + 0.055) / 1.055) ** 2.4)
    out = np.clip(lin @ MACHADO[kind].T, 0.0, 1.0)
    srgb = np.where(out <= 0.0031308, out * 12.92, 1.055 * out ** (1 / 2.4) - 0.055)
    return Image.fromarray(np.round(srgb * 255).astype(np.uint8), "RGB")


def compose(shot: Image.Image, promo: Image.Image, title: str) -> Image.Image:
    def fit(img: Image.Image) -> Image.Image:
        return img.convert("RGB").resize((round(img.width * HEIGHT_PX / img.height), HEIGHT_PX), Image.LANCZOS)
    left, right = fit(shot), fit(promo)
    out = Image.new("RGB", (left.width + GAP_PX + right.width, CAPTION_PX + HEIGHT_PX), (14, 22, 38))
    out.paste(left, (0, CAPTION_PX))
    out.paste(right, (left.width + GAP_PX, CAPTION_PX))
    ImageDraw.Draw(out).text((12, 14), title, fill=(230, 214, 170))
    return out


def main(argv: list[str], root: Path = ROOT, promos: dict[str, Path] = PROMOS) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("shots", type=Path)
    ap.add_argument("name")
    ap.add_argument("--log", type=Path)
    a = ap.parse_args(argv[1:])
    log = a.log or root / "captures" / "visual" / "art-log.tsv"
    log.parent.mkdir(parents=True, exist_ok=True)
    rows, uniform = 0, []
    for path in sorted(a.shots.rglob("*.png")):
        if "-compare" in path.stem:
            continue
        pose = next((p for p in promos if path.stem.endswith(p)), None)
        if pose is None:
            continue
        shot, promo = Image.open(path), Image.open(promos[pose])
        m, p, d = metrics(shot), metrics(promo), histogram_distance(shot, promo)
        if m["contrast"] < UNIFORM_STD:
            uniform.append(path)
            print(f"FAIL: {path} is near-uniform (luma std {m['contrast']:.4f}): the capture is blank")
            continue
        title = (f"{a.name} | {path.stem} (left) vs {promos[pose].name} (right) | saturation {m['saturation']:.2f}/{p['saturation']:.2f}"
                 f"  contrast {m['contrast']:.3f}/{p['contrast']:.3f}  warmth {m['warmth']:.3f}/{p['warmth']:.3f}  palette distance {d:.2f}")
        pair = compose(shot, promo, title)
        pair.save(path.with_name(path.stem + "-compare.png"))
        for kind in MACHADO:
            simulate_cvd(pair, kind).save(path.with_name(f"{path.stem}-compare-{kind}.png"))
        with log.open("a") as f:
            f.write("\t".join([a.name, str(path), f"{m['saturation']:.4f}", f"{m['contrast']:.4f}", f"{m['warmth']:.4f}", f"{d:.4f}"]) + "\n")
        print("ART " + title)
        rows += 1
    if uniform:
        return 2
    if rows == 0:
        print(f"no screenshot named after a pose ({', '.join(promos)}) under {a.shots}")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
```

Create `tools/machine_lock.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""One Mac, several worktrees: frame-time runs must not share the machine with Unity imports, builds, test runs or an open
Unity editor.

Usage: uv run tools/machine_lock.py (--shared | --exclusive) [--wait-unity] [--timeout SECONDS] -- <command> [args...]
--shared takes the machine lock shared (Unity builds, captures and edit-mode runs); --exclusive takes it alone (frame-time
profiles and boot budgets) and, with --wait-unity, then waits until no Unity is left running: no batch-mode Unity process,
and no process (an interactive editor included) holding any worktree's unity/Temp/UnityLockfile. The lock file is
~/.cache/statecraft/machine.lock (fcntl.flock; STATECRAFT_MACHINE_LOCK overrides it), shared by every worktree of this
user. Exit code: the command's, or 3 when --wait-unity times out."""
import argparse
import fcntl
import os
import subprocess
import sys
import time
from pathlib import Path

LOCK = Path(os.environ.get("STATECRAFT_MACHINE_LOCK", str(Path.home() / ".cache" / "statecraft" / "machine.lock")))
UNITY_BATCH = "Unity.app/Contents/MacOS/Unity .*-batchmode"


def parse(argv: list[str]) -> argparse.Namespace:
    ap = argparse.ArgumentParser()
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--shared", action="store_true")
    mode.add_argument("--exclusive", action="store_true")
    ap.add_argument("--wait-unity", action="store_true")
    ap.add_argument("--timeout", type=float, default=3600)
    ap.add_argument("command", nargs=argparse.REMAINDER)
    a = ap.parse_args(argv)
    if a.command and a.command[0] == "--":
        a.command = a.command[1:]
    if not a.command:
        ap.error("no command after --")
    return a


def unity_batch_pids() -> list[int]:
    r = subprocess.run(["pgrep", "-f", UNITY_BATCH], capture_output=True, text=True)
    return [int(p) for p in r.stdout.split() if p.isdigit() and int(p) != os.getpid()]


def worktrees() -> list[Path]:
    """Every worktree of this repository, so an editor open in any of them is seen."""
    r = subprocess.run(["git", "worktree", "list", "--porcelain"], capture_output=True, text=True)
    return [Path(line[len("worktree "):]) for line in r.stdout.splitlines() if line.startswith("worktree ")]


def lockfile_holders(roots: list[Path], lsof=None) -> list[int]:
    """Pids holding any root's unity/Temp/UnityLockfile: every Unity editor with that project open, interactive or batch."""
    files = [str(p / "unity" / "Temp" / "UnityLockfile") for p in roots if (p / "unity" / "Temp" / "UnityLockfile").exists()]
    if not files:
        return []
    run = lsof or (lambda fs: subprocess.run(["lsof", "-t", *fs], capture_output=True, text=True).stdout)
    return sorted({int(p) for p in run(files).split() if p.isdigit()})


def unity_pids() -> list[int]:
    return sorted(set(unity_batch_pids()) | set(lockfile_holders(worktrees())))


def wait_for_unity(timeout: float, pids=unity_pids, sleep=time.sleep, now=time.monotonic) -> bool:
    deadline = now() + timeout
    while True:
        busy = pids()
        if not busy:
            return True
        if now() >= deadline:
            return False
        print(f"machine_lock: waiting for Unity processes {busy} (batch runs, or an editor with a worktree open)", flush=True)
        sleep(10)


def main(argv: list[str] | None = None) -> int:
    a = parse(sys.argv[1:] if argv is None else argv)
    LOCK.parent.mkdir(parents=True, exist_ok=True)
    with open(LOCK, "a+") as f:
        print(f"machine_lock: taking the machine lock ({'exclusive' if a.exclusive else 'shared'})", flush=True)
        fcntl.flock(f, fcntl.LOCK_EX if a.exclusive else fcntl.LOCK_SH)
        if a.wait_unity and not wait_for_unity(a.timeout):
            print("machine_lock: timed out waiting for Unity")
            return 3
        return subprocess.call(a.command)


if __name__ == "__main__":
    sys.exit(main())
```

Modify the committed `tools/headed.py` (review N8 and the controller's round-2 ruling: the plan's headed runs and boot timings go through it, so there is one headed-browser driver):

1. Replace

```python
import argparse
import asyncio
import base64
```

   with

```python
import argparse
import asyncio
import base64
import statistics
```

2. Replace

```python
class Browser:
    def __init__(self, build: Path, port=8766, width=WINDOW[0], height=WINDOW[1]):
        self.build, self.port, self.size = Path(build), port, (width, height)
```

   with

```python
BOOT_PHASE = re.compile(r"BOOT phase=(.+?) at=(\d+) ms dt=(\d+) ms")


def browser_flags(profile: str, width: int, height: int, dpr: float | None = None, uncapped: bool = False) -> list[str]:
    """Chrome flags for a headed run. dpr forces the device pixel ratio (1: the same pixels as a headless run; 2: the
    visual v1 fill-rate stress proxy); uncapped lifts the display's frame-rate cap for frame-time profiles."""
    flags = ["--use-angle=metal", "--remote-debugging-port=0", f"--user-data-dir={profile}", "--no-first-run",
             "--no-default-browser-check", f"--window-size={width},{height}"]
    if dpr is not None:
        flags.append(f"--force-device-scale-factor={dpr:g}")
    if uncapped:
        flags += ["--disable-frame-rate-limit", "--disable-gpu-vsync"]
    return flags + ["about:blank"]


def boot_phases(lines) -> list[tuple[str, int, int]]:
    """(name, at ms, dt ms) of every ?bootlog phase among console lines."""
    return [(m.group(1), int(m.group(2)), int(m.group(3))) for m in map(BOOT_PHASE.search, lines) if m]


def map_build_ms(phases) -> int:
    """The map-build span (visual v1 boot budget): at(map built) - at(heightmap decoded) + dt(heightmap decoded), from the
    end of the content fetch to the map's last phase."""
    first: dict[str, tuple[int, int]] = {}
    for name, at, dt in phases:
        first.setdefault(name, (at, dt))
    for need in ("heightmap decoded", "map built"):
        if need not in first:
            raise ValueError(f"no '{need}' phase (was ?bootlog in the query?)")
    return first["map built"][0] - first["heightmap decoded"][0] + first["heightmap decoded"][1]


def over_budget(spans, baseline, budget) -> tuple[float, bool]:
    """(median span, whether it exceeds baseline + budget); never over without a baseline."""
    median = statistics.median(spans)
    return median, baseline is not None and median > baseline + budget


class Browser:
    def __init__(self, build: Path, port=8766, width=WINDOW[0], height=WINDOW[1], dpr=None, uncapped=False):
        self.build, self.port, self.size = Path(build), port, (width, height)
        self.dpr, self.uncapped = dpr, uncapped
```

3. Replace

```python
        flags = ["--use-angle=metal", "--remote-debugging-port=0", f"--user-data-dir={self.profile}", "--no-first-run",
                 "--no-default-browser-check", f"--window-size={w},{h}", "about:blank"]
```

   with

```python
        flags = browser_flags(self.profile, w, h, self.dpr, self.uncapped)
```

4. Replace the whole `boot` function with:

```python
async def boot(args):
    """?bootlog timings, args.runs times (each in a fresh profile: a cold load), then the median map-build span; 1 when it
    exceeds --baseline + --budget."""
    spans = []
    for run in range(args.runs):
        async with Browser(args.build) as b:
            sid = await b.open("bootlog", args.cpu)
            await b.wait_for(sid, r"^GAME OK")
            origin = (await b.send("Runtime.evaluate", {"expression": "performance.timeOrigin", "returnByValue": True}, session=sid))["result"]["value"]
            print(f"CPU {args.cpu}x, run {run + 1} of {args.runs}")
            for s, text, ts in b.console:
                if s == sid and (text.startswith("BOOT") or text.startswith("GAME OK")):
                    print(f"{ts - origin:8.0f} ms  {text[:140]}")
            span = map_build_ms(boot_phases(b.lines(sid)))
            print(f"BOOT map build {span} ms")
            spans.append(span)
    median, over = over_budget(spans, args.baseline, args.budget)
    print(f"BOOT map_build_ms median={median:.0f} runs={len(spans)}")
    if over:
        print(f"OVER BUDGET: {median:.0f} ms > baseline {args.baseline:.0f} + {args.budget:.0f} ms")
    return 1 if over else 0


async def profile(args):
    """?vprofile (visual v1) in a headed 1920x1080 window at --dpr, frame rate uncapped; prints the PROFILE lines."""
    async with Browser(args.build, width=1920, height=1080, dpr=args.dpr, uncapped=True) as b:
        sid = await b.open("vprofile", args.cpu)
        await b.wait_for(sid, r"^PROFILE visual done", timeout=args.timeout)
        for line in b.lines(sid):
            if line.startswith("PROFILE"):
                print(line)
    return 0
```

5. Replace

```python
    p.add_argument("command", choices=["boot", "navpairs", "loadshot"])
```

   with

```python
    p.add_argument("command", choices=["boot", "navpairs", "loadshot", "profile"])
```

   and

```python
    p.add_argument("--out", default="headed", help="screenshot path prefix (loadshot)")
    args = p.parse_args(argv)
    asyncio.run({"boot": boot, "navpairs": navpairs, "loadshot": loadshot}[args.command](args))


if __name__ == "__main__":
    main()
```

   with

```python
    p.add_argument("--out", default="headed", help="screenshot path prefix (loadshot)")
    p.add_argument("--runs", type=int, default=1, help="boot: cold loads to take the median of")
    p.add_argument("--baseline", type=float, help="boot: exit 1 when the median map-build span exceeds this + --budget (ms)")
    p.add_argument("--budget", type=float, default=1000, help="boot: allowed growth over --baseline (ms)")
    p.add_argument("--dpr", type=float, default=1.0, help="profile: device pixel ratio (2 = the fill-rate stress proxy)")
    p.add_argument("--timeout", type=float, default=1800, help="profile: seconds to wait for PROFILE visual done")
    args = p.parse_args(argv)
    result = asyncio.run({"boot": boot, "navpairs": navpairs, "loadshot": loadshot, "profile": profile}[args.command](args))
    return result if isinstance(result, int) else 0


if __name__ == "__main__":
    sys.exit(main())
```

6. In its docstring, replace `  loadshot  screenshots during the load and once ready (the loading line, or the boot-failure line)` with:

```
  loadshot  screenshots during the load and once ready (the loading line, or the boot-failure line)
  profile   visual v1's ?vprofile in a headed 1920x1080 window at --dpr (frame rate uncapped)
```

   and add `boot --runs N [--baseline MS] [--budget MS]` to its examples line: `  uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 --baseline 5400`.

Create `tools/art_checkpoint.sh` (then `chmod +x tools/art_checkpoint.sh`):

```bash
#!/usr/bin/env bash
# Owner art checkpoint (non-blocking): a WebGL build (skipped with --no-build when the task has just built), screenshots at
# the promo poses from the player itself (?shot: the web tier with its UI and labels), editor renders of both quality
# tiers, and side-by-side images against the promo with colour-blind simulations. Re-checks both golden hashes in the same
# player run and fails if a screenshot is missing or blank. The art log lives in the main checkout's captures/ so every
# worktree appends to one history. Usage: tools/art_checkpoint.sh [--no-build] <checkpoint-name>
set -euo pipefail
build=1
if [[ "${1:-}" == "--no-build" ]]; then build=0; shift; fi
name="${1:?usage: tools/art_checkpoint.sh [--no-build] <checkpoint-name>}"
root="$(git rev-parse --show-toplevel)"
cd "$root"
main_root="$(cd "$(git rev-parse --path-format=absolute --git-common-dir)/.." && pwd)"
: "${UNITY:?set UNITY as in docs/dev-setup.md}"
shared() { uv run tools/machine_lock.py --shared -- "$@"; }
if [[ $build == 1 ]]; then
  if ! git diff --quiet -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset; then
    echo "art_checkpoint: stage your changes under unity/Assets/Settings and ProjectSettings.asset first (the build rewrites them and this script restores them from the index)" >&2
    exit 2
  fi
  shared "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$root/unity-build-web-art.log"
  git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
  if grep -n "Shader error" "$root/unity-build-web-art.log"; then echo "art_checkpoint: shader errors in the WebGL build" >&2; exit 1; fi
fi
[[ -f unity/Builds/Web-disabled/index.html ]] || { echo "art_checkpoint: no build at unity/Builds/Web-disabled (run without --no-build)" >&2; exit 2; }
out="captures/visual/$name"
rm -rf "$out"; mkdir -p "$out"
uv run tools/web_check.py unity/Builds/Web-disabled --query "golden&shot" --expect "GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07" --expect "GOLDEN OK 1952 73590e324f19e9959a455a4da79e93d7089dfcf283c13aac02af6b0a624b9767" --expect "SHOT done" --shots "$out" \
  --fail 'GOLDEN FAIL|SMOKE FAIL|BOOT FAIL|Statecraft/.*(not supported|Unsupported)|(not supported|Unsupported).*Statecraft/'
for pose in gameplay03 diorama02; do
  [[ -s "$out/$pose.png" ]] || { echo "art_checkpoint: $out/$pose.png is missing" >&2; exit 1; }
done
shared "$UNITY" -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod MapCapture.Promo -captureDir "$root/$out/editor" -logFile "$root/unity-capture-promo.log"
git checkout -- unity/ProjectSettings/QualitySettings.asset   # SetQualityLevel may have written m_CurrentQuality
uv run tools/art_compare.py "$out" "$name" --log "$main_root/captures/visual/art-log.tsv"
echo "ART CHECKPOINT $name: $(ls "$out"/*-compare*.png "$out"/editor/*-compare*.png | tr '\n' ' ')"
```

- [ ] **Step 8: Run the tools tests and confirm they pass**

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q | tail -1
```
Expected: **T0 + 20** passed at first delivery (**T0 + 34** after Task 1 fix round 1), the skipped count unchanged (the M3 `test_web_check.py` tests still pass: `scan` and `watch` keep their behaviour, `on_text` defaults to `None`; M3's headed browser test still skips without `--headed`). Write the passed total down as **T1**.

- [ ] **Step 9: Record the baseline profile, boot time and the "before" checkpoint**

Run (from here on, every Unity command and every measurement goes through `tools/machine_lock.py`):
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | tee "$PWD/unity-vprofile-t01-headless.log" | grep "PROFILE"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py profile --build unity/Builds/Web-disabled --dpr 1 | tee "$PWD/unity-vprofile-t01-headed.log" | grep "PROFILE"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 | tee "$PWD/unity-boot-t01.log" | grep "BOOT map"
tools/art_checkpoint.sh --no-build t01-before
```
Expected:
- Each profile prints, per pose, `PROFILE visual=all ... res=1920x1080 scale=0.80 rep=0 ...` and `visual=none`, three repeats each (no toggles exist yet), then two `PROFILE summary` lines per pose and `PROFILE visual done`. `res=1920x1080` in **both** runs: `headed.py profile` opens a 1920x1080 window at DPR 1 (`--force-device-scale-factor=1`). If the headed line shows a smaller size, the laptop screen clamped the window: note the size beside the headed numbers in `docs/visual-v1.md`.
- `BOOT map_build_ms median=<m> runs=3`: write `<m>` down as the **boot baseline** (Tasks 6 and 8 compare against it).
- The checkpoint prints `PASS: saw GOLDEN OK 1950, GOLDEN OK 1952, SHOT done`, writes `captures/visual/t01-before/{gameplay03,diorama02}.png` and four `editor/{Mobile,PC}-<pose>.png`, and lists six `-compare.png` files with their two colour-blind copies each. If a `SHOT` part fails its hash or is missing (a console line was cut or lost), halve `VisualShots.ChunkChars`, re-run Step 5 and this step, and note the new value in `docs/visual-v1.md`.

- [ ] **Step 10: Write the visual log and the developer notes**

Create `docs/visual-v1.md` (fill the tables with the lines Step 9 printed, verbatim):

```markdown
# Visual v1: measurements, budgets and checkpoints

Plan: `docs/superpowers/plans/2026-10-11-statecraft-visual-v1.md`. Promo targets: `promo/final/03-gameplay-1936.png`,
`promo/final/02-map-diorama.png`. Poses: `PromoPose.Gameplay03` (34.86 E, 32.00 N, 60 km, yaw 10) and
`PromoPose.Diorama02` (35.05 E, 31.55 N, 250 km, yaw 0).

## How to measure
- Frame time: `?vprofile` (WebGL) or `-vprofile` (desktop player, quits when done), always under
  `uv run tools/machine_lock.py --exclusive --wait-unity -- ...` (no other Unity job on the machine). Per pose, every
  configuration (`all`, each toggle off alone as `-<name>`, `none`) runs 3 times in interleaved orders, 300 frames after
  60 warm-up frames; Chrome runs with `--disable-frame-rate-limit --disable-gpu-vsync --force-device-scale-factor=1` (headless through
  `web_check.py`, headed through `headed.py profile`), so times are not capped at the display rate and headed and headless
  runs draw the same pixels. Every line states `res=` and
  the render `scale=`. Read the `PROFILE summary` lines: a feature costs `median(all) - median(-name)`.
- Boot: `uv run tools/headed.py boot --runs 3 [--baseline MS]` (three cold loads in a headed window; the map-build span is
  heightmap decoded to map built; `--cpu 4` adds CPU throttling as a cheap low-end proxy).
- Headed frame time: `uv run tools/headed.py profile --dpr 1` (`--dpr 2` is the fill-rate stress proxy). `--wait-unity`
  also waits for an interactive Unity editor that has any worktree open (it holds `unity/Temp/UnityLockfile`): close it,
  or the profile waits.
- Screenshots: `tools/art_checkpoint.sh [--no-build] <name>` (WebGL `?shot` + editor renders of both tiers + side-by-side
  images and their deuteranopia/protanopia copies under `captures/visual/<name>/`, git-ignored; rows in the main
  checkout's `captures/visual/art-log.tsv`).

## Budgets (proposed; ratified in Task 10)
Measured on the development Mac (Apple silicon, integrated GPU), 1920x1080 at DPR 1, Chrome for Testing:
- headless, all features on: median p95 <= 16.7 ms at both poses, and median(all) - median(none) <= 8 ms;
- each feature: median(all) - median(-feature) <= 2.5 ms (tilt-shift <= 1.5 ms, labels <= 1 ms); a cost counts as over only
  when it exceeds the budget plus the larger of the two spreads;
- headed numbers are recorded, plus a DPR-2 headed run in Task 10 as a fill-rate stress proxy for weaker GPUs; the 30 fps
  floor for an integrated-GPU laptop (research notes 2.3) is 33 ms p95;
- boot: each boot span stays within baseline + max(300 ms, 50%) of the Task 1 baseline (median of 3; Task 1 fix round 1).

## Baseline (Task 1, before any visual change)
| Run | Lines |
|---|---|
| headless | (paste the four PROFILE summary lines) |
| headed (DPR 1) | (paste the four PROFILE summary lines and the res= of one run line) |
| boot | (paste the BOOT map_build_ms line) |

## Checkpoints
| Task | Name | Images | Owner verdict |
|---|---|---|---|
| 1 | t01-before | captures/visual/t01-before/*-compare*.png | (owner) |
```

In `docs/dev-setup.md`, replace

```markdown
  (every UI pointer and submit event, screen toggle and map click, with its frame). Desktop players take `-golden`,
  `-smoke N`, `-profile`, `-camlog`, `-bootlog` and `-navlog`. The console also shows `GAME OK <date> turn <n> hash <h>` at start and `TURN <n> <date> hash <h>` per turn.
```

(the end of the "Page flags" bullet) with

```markdown
  (every UI pointer and submit event, screen toggle and map click, with its frame), `?vprofile` (the visual frame-time
  profile per feature) and `?shot` (screenshots at the promo poses as `SHOT` console lines). Desktop players take `-golden`,
  `-smoke N`, `-profile`, `-camlog`, `-bootlog`, `-navlog`, `-vprofile` and `-shot`. The console also shows `GAME OK <date> turn <n> hash <h>` at start and `TURN <n> <date> hash <h>` per turn.
- Visual checks (docs/visual-v1.md): measure only under the machine lock, which also waits for any open Unity editor:
  `uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py <build> --query vprofile --expect "PROFILE visual done" --timeout 1800`
  (headless), `... -- uv run tools/headed.py profile --build <build> --dpr 1` (headed; `--dpr 2` is the fill-rate stress
  proxy) and `... -- uv run tools/headed.py boot --build <build> --runs 3 --baseline <ms>` (the boot budget); wrap Unity
  batch commands in `uv run tools/machine_lock.py --shared -- ...`; `tools/art_checkpoint.sh [--no-build] <name>` for the
  owner's side-by-side images.
```

(The anchor is the bullet's text on `main` at `554ca2c`, R26.)

In `tools/README.md`, replace

```markdown
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
```

with

```markdown
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
- `art_compare.py` puts screenshots next to the promo image of their pose, writes deuteranopia and protanopia copies and
  logs rough colour numbers; `art_checkpoint.sh [--no-build] <name>` builds WebGL, takes the screenshots
  (`web_check.py --shots`) and runs it (docs/visual-v1.md).
- `machine_lock.py` serialises frame-time measurements against every other Unity job on the machine (all worktrees,
  batch runs and open editors).
```

In the `headed.py` entry, replace `and \`loadshot\` (screenshots during the load).` with `, \`loadshot\` (screenshots during the load) and \`profile\` (visual v1's \`?vprofile\` in a headed window at a chosen DPR); \`boot --runs N --baseline MS\` checks the boot budget.`

In its "Running the tests" section, replace the passed count in the `Expected:` line with T1 (Step 8), keeping its skipped count and its note on the headed browser check.

- [ ] **Step 11: Check nothing outside the presentation layer moved, then commit**

Run:
```bash
git diff --stat <task-base> -- sim content    # prints nothing
git status --short unity/ProjectSettings       # prints nothing
git add tools/web_check.py tools/headed.py tools/art_compare.py tools/art_checkpoint.sh tools/machine_lock.py tools/README.md \
  tools/tests/test_web_check_shots.py tools/tests/test_art_compare.py tools/tests/test_machine_lock.py tools/tests/test_headed.py \
  docs/visual-v1.md docs/dev-setup.md \
  unity/Assets/Scripts/Map/PromoPose.cs unity/Assets/Scripts/Map/PromoPose.cs.meta \
  unity/Assets/Scripts/Map/VisualToggles.cs unity/Assets/Scripts/Map/VisualToggles.cs.meta \
  unity/Assets/Scripts/Map/FrameStats.cs unity/Assets/Scripts/Map/FrameStats.cs.meta \
  unity/Assets/Scripts/Map/FrameProfile.cs unity/Assets/Scripts/Map/FrameProfile.cs.meta \
  unity/Assets/Scripts/Map/VisualShots.cs unity/Assets/Scripts/Map/VisualShots.cs.meta \
  unity/Assets/Scripts/Map/MapRuntimeAssets.cs unity/Assets/Scripts/Map/PixelErrorProfile.cs \
  unity/Assets/Scripts/Map/CameraRig.cs unity/Assets/Scripts/Map/MapScene.cs unity/Assets/Scripts/UI/GameRoot.cs \
  unity/Assets/Editor/MapCapture.cs \
  unity/Assets/Tests/EditMode/RenderSample.cs unity/Assets/Tests/EditMode/RenderSample.cs.meta \
  unity/Assets/Tests/EditMode/ShaderChecks.cs unity/Assets/Tests/EditMode/ShaderChecks.cs.meta \
  unity/Assets/Tests/EditMode/VisualHarnessTests.cs unity/Assets/Tests/EditMode/VisualHarnessTests.cs.meta
git status --short   # nothing else staged; captures/, builds and unity-*.log are git-ignored
git commit -m "feat(visual): promo poses, feature toggles, ?vprofile with repeats and ?shot, the machine lock, headed profile and boot budget, art checkpoint

Baseline frame times, boot time and the t01-before images are recorded in docs/visual-v1.md.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Phase 1, Track A: light and lens (Tasks 2–4)

### Task 2: A low warm sun, a cool fill, shadows fitted to the camera, and a closer hero zoom

*Track A, worktree `vis-light`. Effort 4–6 h.* Research §0.2: both URP assets keep `m_ShadowDistance: 50` (metres) while the camera sits 20–250 km away, so the map never casts a shadow. This task fits the shadow distance to the camera distance every frame, lowers and warms the sun (late afternoon from the west-north-west, as the promo's long shadows fall to the right), gives the scene a cool sky fill, doubles the web tier's shadow map, and lets the camera come in to 6 km (owner ruling 1: a closer hero zoom must be possible).

Why `ShadowFit.CoverFactor = 1.75`: at pitch 50° and FOV 35°, with the top bar's inset at its maximum (0.5), the top screen row meets the ground at a view depth of 1.66 × the rig distance (`TheShadowDistanceCoversTheWholeView` checks it on the camera's own rays); 1.75 adds 5% margin. At 60 km a 2048 shadow map then spans about 210 km, about 100 m per texel: coarse for buildings, right for 3×-exaggerated relief, and the symbolic-scale buildings of the art track (300–700 m) still cast readable shadows.

**Files:**
- Create: `unity/Assets/Scripts/Map/MapLighting.cs`, `unity/Assets/Scripts/Map/ShadowFit.cs`, `unity/Assets/Tests/EditMode/LightingTests.cs`
- Modify: `unity/Assets/Scripts/Map/CameraRig.cs`, `unity/Assets/Scripts/Map/CameraController.cs`, `unity/Assets/Settings/Mobile_RPAsset.asset`, `docs/visual-v1.md`

**Interfaces:**
- Consumes: `CameraRig.TeardownActions`, `VisualToggles.Register`, `PromoPose`, `RenderSample` (Task 1).
- Produces: `MapLighting { const float SunElevation = 30f, SunAzimuth = 285f, SunIntensity = 1.6f; static readonly Color SunColour, AmbientSky, AmbientEquator, AmbientGround; static Light? FindSun(); static void Apply(Light? sun); }` (Task 4 removed `AmbientProbe()`: Trilight ambient replaces any assigned probe, so it never rendered); `ShadowFit { const float CoverFactor = 1.75f, MinDistanceM = 3000f, MaxDistanceM = 450000f /* shipped: corner fit and MaxDistanceM 1,000,000; see docs/visual-v1.md */; static float Distance(float cameraDistance); static void Apply(float cameraDistance); static void Restore(); }`; toggle `"shadows"`; `CameraController.MinDistance = 6000f`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/LightingTests.cs`:

```csharp
#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Plan Task 2: the sun, the ambient fill, shadows at map scale and the hero zoom.</summary>
public class LightingTests
{
    [TearDown]
    public void Restore()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);   // destroys any map: its teardown restores
        ShadowFit.Restore();
        VisualToggles.Clear();
    }

    [Test]
    public void TheSunIsLowWarmAndFromTheWestNorthWest()
    {
        var go = new GameObject("Sun"); var sun = go.AddComponent<Light>(); sun.type = LightType.Directional;
        try
        {
            MapLighting.Apply(sun);
            var travel = sun.transform.forward;
            Assert.AreEqual(-Mathf.Sin(MapLighting.SunElevation * Mathf.Deg2Rad), travel.y, 1e-4f, "elevation");
            Assert.That(MapLighting.SunElevation, Is.InRange(25f, 35f), "late afternoon: long shadows");
            Assert.Greater(travel.x, 0f, "the light travels east: the sun is in the west");
            Assert.Less(travel.z, 0f, "and a little south: the sun is north of west");
            Assert.Greater(sun.color.r, sun.color.b + 0.2f, "warm");
            Assert.AreEqual(LightShadows.Soft, sun.shadows);
            Assert.AreSame(sun, RenderSettings.sun);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void TheAmbientFillIsCoolFromTheSkyAndDarkFromTheGround()
    {
        MapLighting.Apply(null);
        var c = new Color[2];
        RenderSettings.ambientProbe.Evaluate(new[] { Vector3.up, Vector3.down }, c);
        Assert.Greater(c[0].b, c[0].r, "sky fill is cool");
        Assert.Greater(c[0].grayscale, c[1].grayscale + 0.05f, "brighter from above");
        Assert.AreEqual(AmbientMode.Trilight, RenderSettings.ambientMode);
    }

    [TestCase(6000f)]
    [TestCase(45000f)]
    [TestCase(120000f)]
    [TestCase(250000f)]
    public void TheShadowDistanceCoversTheWholeView(float distance)
    {
        var go = new GameObject("Cam"); var cam = go.AddComponent<Camera>(); var rig = go.AddComponent<CameraRig>();
        try
        {
            cam.fieldOfView = CameraRig.Fov;
            foreach (var aspect in new[] { 16f / 9f, 21f / 9f })
            {
                cam.aspect = aspect;
                rig.LookAt = Vector3.zero; rig.Distance = distance; rig.TopInset = CameraRig.MaxTopInset; rig.Apply();
                foreach (var v in new[] { new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 1f) })
                {
                    var ray = cam.ViewportPointToRay(new Vector3(v.x, v.y, 0));
                    var hit = ray.origin + ray.direction * (-ray.origin.y / ray.direction.y);
                    var depth = Vector3.Dot(hit - cam.transform.position, cam.transform.forward);
                    Assert.LessOrEqual(depth, ShadowFit.Distance(distance), $"{distance} m, aspect {aspect:0.00}, viewport {v}");
                }
            }
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void FittingFollowsTheCameraAndRestoringPutsTheAssetBack()
    {
        var asset = UniversalRenderPipeline.asset; Assert.IsNotNull(asset);
        var before = asset!.shadowDistance;
        ShadowFit.Apply(60000f); Assert.AreEqual(ShadowFit.Distance(60000f), asset.shadowDistance, 1e-3f);
        ShadowFit.Apply(10000f); Assert.AreEqual(ShadowFit.Distance(10000f), asset.shadowDistance, 1e-3f);
        Assert.AreEqual(ShadowFit.MinDistanceM, ShadowFit.Distance(100f));
        ShadowFit.Restore(); Assert.AreEqual(before, asset.shadowDistance);
        ShadowFit.Restore(); Assert.AreEqual(before, asset.shadowDistance, "restoring twice is harmless");
    }

    [Test]
    public void DestroyingTheMapRestoresTheShadowDistance()
    {
        var asset = UniversalRenderPipeline.asset!; var before = asset.shadowDistance;
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build();
        Assert.AreNotEqual(before, asset.shadowDistance, "fitted to the camera by Build");
        CollectionAssert.Contains(VisualToggles.Names, "shadows");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.AreEqual(before, asset.shadowDistance, "restored with the map");
    }

    [Test]
    public void TheHeroZoomComesInToSixKilometres()
    {
        Assert.AreEqual(6000f, CameraController.MinDistance);
        Assert.AreEqual(250000f, CameraController.MaxDistance);
    }

    [Test]
    public void TheWebTierShadowMapIs2048()
    {
        var web = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        Assert.AreEqual(2048, web.mainLightShadowmapResolution);
    }

    /// <summary>Rendered: over the Dead Sea escarpment (the steepest relief on the map) the sun now casts shadows. Without
    /// the fit (the asset's 50 m) nothing would. Much of the escarpment's shadow at 30 degrees falls east onto the Dead Sea;
    /// in Track A alone that water is URP-lit terrain and darkens; after the merge the water is ScWaterShade, which samples
    /// the main light's shadow attenuation (Task 7, review I6), so the shadow still lands on it and the 2% floor holds.</summary>
    [UnityTest]
    public IEnumerator ReliefCastsShadowsAtMapScale()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>(); var sun = MapLighting.FindSun();
        Assert.IsNotNull(sun, "Map.unity has a directional light");
        rig.LookAt = map.Frame.ToWorld(35.38, 31.45, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var lit = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("shadows", false);
        var flat = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("shadows", true);
        float shadowed = RenderSample.FractionDarker(lit, flat, 0.04f);
        TestContext.WriteLine($"shadowed share {shadowed:P1}");
        Assert.Greater(shadowed, 0.02f, "at least 2% of the view is in a cast shadow");
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -m2 "error CS" unity-tests.log
```
Expected: `exit=1`; `error CS0103: The name 'MapLighting' does not exist in the current context`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/Map/MapLighting.cs`:

```csharp
#nullable enable
using UnityEngine;
using UnityEngine.Rendering;

namespace Statecraft.Game.Map
{
    /// <summary>The map's light (research notes §1 "Lighting", promo 03): a low, warm late-afternoon sun from the
    /// west-north-west, whose long shadows fall to the right of the default view, and a cool sky fill that keeps the shadow
    /// side readable. The sun is fixed in the world, so orbiting the camera moves the light across the screen.</summary>
    public static class MapLighting
    {
        public const float SunElevation = 30f;   // degrees above the horizon
        public const float SunAzimuth = 285f;    // degrees clockwise from north: west-north-west
        public const float SunIntensity = 1.6f;
        public static readonly Color SunColour = new Color(1.00f, 0.84f, 0.64f);
        public static readonly Color AmbientSky = new Color(0.52f, 0.60f, 0.72f);
        public static readonly Color AmbientEquator = new Color(0.48f, 0.46f, 0.42f);
        public static readonly Color AmbientGround = new Color(0.26f, 0.22f, 0.17f);

        /// <summary>The scene's first directional light, or null.</summary>
        public static Light? FindSun()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
                if (l.type == LightType.Directional) return l;
            return null;
        }

        /// <summary>Points, colours and enables shadows on <paramref name="sun"/> (when there is one) and sets the trilight
        /// ambient and its probe.</summary>
        public static void Apply(Light? sun)
        {
            if (sun != null)
            {
                // A light shines along its forward axis: towards the azimuth opposite the sun, tilted down by the elevation.
                sun.transform.rotation = Quaternion.Euler(SunElevation, SunAzimuth - 180f, 0f);
                sun.color = SunColour; sun.intensity = SunIntensity;
                sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.85f;
                RenderSettings.sun = sun;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky; RenderSettings.ambientEquatorColor = AmbientEquator; RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.ambientProbe = AmbientProbe();
        }

        /// <summary>The trilight ambient as spherical harmonics (set explicitly: a runtime change of the trilight colours does
        /// not rebuild the probe the shaders read).</summary>
        public static SphericalHarmonicsL2 AmbientProbe()
        {
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(AmbientEquator);
            sh.AddDirectionalLight(Vector3.up, AmbientSky - AmbientEquator, 1f);
            sh.AddDirectionalLight(Vector3.down, AmbientGround - AmbientEquator, 1f);
            return sh;
        }
    }
}
```

Create `unity/Assets/Scripts/Map/ShadowFit.cs`:

```csharp
#nullable enable
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>Shadows at map scale (research notes §0.2): URP has no per-camera shadow distance, so the active URP asset's
    /// shadowDistance follows the rig's distance, and Restore puts the asset's own value back (the map registers Restore as a
    /// teardown action, so destroying the map, a rebuild or the end of a test leaves the asset as it was).</summary>
    public static class ShadowFit
    {
        /// <summary>View depth of the top screen row is 1.66 x the rig distance at the largest top inset (LightingTests).</summary>
        public const float CoverFactor = 1.75f;
        public const float MinDistanceM = 3000f, MaxDistanceM = 450000f;

        static UniversalRenderPipelineAsset? touched;
        static float original;

        public static float Distance(float cameraDistance) => Mathf.Clamp(cameraDistance * CoverFactor, MinDistanceM, MaxDistanceM);

        public static void Apply(float cameraDistance)
        {
            var asset = UniversalRenderPipeline.asset;
            if (asset == null) return;
            if (touched != asset) { Restore(); touched = asset; original = asset.shadowDistance; }   // a quality-level switch
            asset.shadowDistance = Distance(cameraDistance);
        }

        public static void Restore()
        {
            if (touched != null) touched.shadowDistance = original;
            touched = null;
        }
    }
}
```

In `unity/Assets/Scripts/Map/CameraRig.cs`:
1. Replace

```csharp
        public readonly List<Action> TeardownActions = new List<Action>();
```

   with

```csharp
        public readonly List<Action> TeardownActions = new List<Action>();
        bool initialised;   // only a rig the map set up fits the shadow distance (bare rigs in tests must not touch the URP asset)
```

2. Replace

```csharp
            Volume = vol;
            Apply();
```

   with

```csharp
            Volume = vol;
            MapLighting.Apply(MapLighting.FindSun());
            TeardownActions.Add(VisualToggles.Register("shadows", on => { var sun = MapLighting.FindSun(); if (sun != null) sun.shadows = on ? LightShadows.Soft : LightShadows.None; }));
            TeardownActions.Add(ShadowFit.Restore);
            initialised = true;
            Apply();
```

3. Replace

```csharp
            ApplyProjection();
        }
```

   (the end of `Apply()`) with

```csharp
            if (initialised) ShadowFit.Apply(Distance);
            ApplyProjection();
        }
```

In `unity/Assets/Scripts/Map/CameraController.cs`, replace

```csharp
        public const float MinDistance = 20000f, MaxDistance = 250000f;
```

with

```csharp
        /// <summary>6 km is the hero zoom (owner ruling 2026-10-11: symbolic-scale settlements need a closer view); 250 km
        /// shows the country.</summary>
        public const float MinDistance = 6000f, MaxDistance = 250000f;
```

In `unity/Assets/Settings/Mobile_RPAsset.asset`, replace the line `  m_MainLightShadowmapResolution: 1024` with `  m_MainLightShadowmapResolution: 2048`.

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep "shadowed share" unity-tests.log
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
git status --short unity/Assets/Settings   # only Mobile_RPAsset.asset: no test saved a fitted shadow distance
```
Expected: `exit=0`; total = **B1 + 11**, `failed="0"`; a `shadowed share` line above 2%; no warnings; `git status` lists only ` M unity/Assets/Settings/Mobile_RPAsset.asset`. If acne (speckled self-shadow on lit slopes) shows in Step 5's images, raise `m_ShadowNormalBias` on `Mobile_RPAsset` from 1 to 2 and re-run this step.

- [ ] **Step 5: Profile and run the art checkpoint (non-blocking)**

Run:
```bash
git add unity/Assets/Settings/Mobile_RPAsset.asset        # staged before any build (Global Constraints)
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t02-light
```
Expected: the summary now includes `PROFILE summary visual=-shadows ...` lines for both poses; the checkpoint prints `PASS: saw GOLDEN OK 1950, GOLDEN OK 1952, SHOT done` and the compare paths. Add the `all`/`-shadows` summary lines to `docs/visual-v1.md` under a new heading `## Task 2: sun and shadows`, and a row `| 2 | t02-light | captures/visual/t02-light/*-compare*.png | (owner) |` to the Checkpoints table. Budget check: `median(all) - median(-shadows) <= 2.5 ms + the larger spread` at both poses; if it is over, note it under the heading for Task 10 (do not block). Note for the owner in the checkpoint row: at the new 6 km hero zoom the view is filled by the 600–1,800 m placeholder marker cubes, so judge the zoom range, not the close-up art, until the settlement art track replaces them.

- [ ] **Step 6: Commit**

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add unity/Assets/Scripts/Map/MapLighting.cs unity/Assets/Scripts/Map/MapLighting.cs.meta \
  unity/Assets/Scripts/Map/ShadowFit.cs unity/Assets/Scripts/Map/ShadowFit.cs.meta \
  unity/Assets/Scripts/Map/CameraRig.cs unity/Assets/Scripts/Map/CameraController.cs \
  unity/Assets/Settings/Mobile_RPAsset.asset docs/visual-v1.md \
  unity/Assets/Tests/EditMode/LightingTests.cs unity/Assets/Tests/EditMode/LightingTests.cs.meta
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(visual): a low warm sun, a cool fill, shadows fitted to the camera, a 6 km hero zoom

The URP shadow distance (50 m) now follows the rig (1.75 x its distance) and is restored with the map.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 3: A real tilt-shift: both bands blurred by a screen-space pass

*Track A. Effort 6–8 h.* Research §0.3: URP's Gaussian depth of field "only does far-field blurring", so today only the top of the frame softens. The promo blurs the bottom ~15% and the top ~20% with a sharp band between. This task retires the Gaussian DoF and adds a custom URP render-graph pass: the frame is blurred at half resolution with a separable 9-tap Gaussian whose radius grows with the distance from a horizontal focus band (no depth texture, so it suits the web tier), then composited back with the band's weight as alpha. The band follows the map area below the top bar, the radius is a share of the picture's height (UV units, review I4: the same blur at any resolution, at the web tier's 0.8 render scale and in batch-mode captures, with no pixel-height source to get wrong), and it weakens at maximum zoom-out so the overview stays readable (research risk 8). Note: edit-mode tests render into a `targetTexture`, which URP draws at render scale 1; the UV-unit radius makes that difference irrelevant to the blur.

**Files:**
- Create: `unity/Assets/Shaders/TiltShift.shader`, `unity/Assets/Scripts/Map/TiltShift.cs`, `unity/Assets/Scripts/Map/TiltShiftFeature.cs`, `unity/Assets/Editor/PostFxAssets.cs`, `unity/Assets/Tests/EditMode/TiltShiftTests.cs`
- Create (by `PostFxAssets.Create`): `unity/Assets/Materials/TiltShift.mat`
- Modify: `unity/Assets/Scripts/Map/CameraRig.cs`, `unity/Assets/Settings/Mobile_Renderer.asset`, `unity/Assets/Settings/PC_Renderer.asset` (by `PostFxAssets.Create`), `unity/Assets/Tests/EditMode/SettlementTests.cs` (one test), `docs/visual-v1.md`

**Interfaces:**
- Consumes: `CameraRig.{TopInset, Distance, Apply}`, `CameraController.MaxDistance`, `VisualToggles`, `RenderSample`, `ShaderChecks`, `PromoPose`.
- Produces: `TiltShift { static readonly int ParamsId /* _ScTiltShift */; const float HalfWidth = 0.17f, Feather = 0.18f, MaxRadiusUv = 10f / 1080f, FullStrengthDistanceM = 60000f, MinStrength = 0.35f; static bool Enabled; static float Weight(float y, Vector4 p); static float FocusCentre(float topInset); static float Strength(float distance); static Vector4 Params(float topInset, float distance); static void Apply(float topInset, float distance); }`; `PostFxPaths.TiltShiftMaterial`; `TiltShiftFeature : ScriptableRendererFeature { Material? Material; int Downsample; }`; `PostFxAssets.Create()`; toggle `"tiltshift"`; global `float4 _ScTiltShift` (x focus centre in viewport y, y sharp half-width, z feather, w max radius as a share of the picture height).

- [ ] **Step 0: Fix the GLES3 `require` policy and judge the real shaders before any Task 3 shader (Task 1 re-review F1)**

`ShaderChecks.GlslErrors` fails any `#extension X : require`. That is the right default, but Unity's GLES3 output for URP Lit, Terrain Lit and Shader Graph shaders may legitimately `require` an extension that WebGL 2 (ANGLE) accepts, and a false failure would stall Task 3, 6, 7 or 8 in the middle of its red/green step. Policy: default-deny, with an explicit `AllowedRequires` set. An entry is added only with a comment saying it was verified to work in WebGL 2 in headless Chrome (the compile in sub-step 3), never because the check complained. Tasks 3, 6, 7 and 8 report any new `require` they meet and extend the set by this same rule; they never weaken `GlslErrors`.

1. In `unity/Assets/Tests/EditMode/ShaderChecks.cs`, add `using System.Linq;` to the usings, then replace the `GlslErrors` method with the following two methods (the `Require` regex and everything else stay):

```csharp
    /// <summary>The `#extension X : require` lines the check accepts. Default-deny: an entry needs a comment saying it was
    /// verified to work in WebGL 2 in headless Chrome (a `compileShader` of a shader carrying that line, see plan Task 3
    /// Step 0). An extension is never added because the check complained, only because the compile succeeded.</summary>
    static readonly HashSet<string> AllowedRequires = new HashSet<string>
    {
        // (empty: add `"GL_..."` with a "verified to work in WebGL 2 in headless Chrome, <date>, <Chrome version>" comment)
    };

    public static List<string> GlslErrors(string glsl) => GlslErrors(glsl, AllowedRequires);

    /// <summary>What in generated GLES3 GLSL WebGL 2 cannot run: no GLSL, a version other than "300 es", or a required
    /// extension that is not in <paramref name="allowedRequires"/>.</summary>
    public static List<string> GlslErrors(string glsl, ISet<string> allowedRequires)
    {
        var errors = new List<string>();
        var versions = Version.Matches(glsl ?? "");
        if (versions.Count == 0) errors.Add("no GLSL was generated");
        foreach (Match v in versions)
            if (v.Groups[1].Value.Trim() != "300 es") errors.Add($"#version {v.Groups[1].Value.Trim()}: WebGL 2 runs GLSL ES 3.00 only");
        foreach (Match r in Require.Matches(glsl ?? ""))
            if (!allowedRequires.Contains(r.Groups[1].Value))
                errors.Add($"requires {r.Groups[1].Value}, which is not in ShaderChecks.AllowedRequires: if WebGL 2 supports it, " +
                           "verify it in headless Chrome (plan Task 3 Step 0) and add it there with a comment saying so");
        return errors;
    }
```

2. In `unity/Assets/Tests/EditMode/VisualHarnessTests.cs`, add these tests after `AShaderThatCompilesForMetalButNotForWebGL2Fails`:

```csharp
    [Test]
    public void AnAllowedRequireIsAcceptedAndAnUnknownOneFailsWithHowToAddIt()
    {
        const string glsl = "#version 300 es\n#extension GL_EXT_demo_allowed : require\nvoid main() {}";
        Assert.IsEmpty(ShaderChecks.GlslErrors(glsl, new HashSet<string> { "GL_EXT_demo_allowed" }), "an allowed require passes");
        var errors = ShaderChecks.GlslErrors(glsl, new HashSet<string>());
        Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
        StringAssert.Contains("GL_EXT_demo_allowed", errors[0]);
        StringAssert.Contains("AllowedRequires", errors[0], "the message says where to add it");
        Assert.IsNotEmpty(ShaderChecks.GlslErrors("#version 300 es\n#extension GL_EXT_other : require\nvoid main() {}",
            new HashSet<string> { "GL_EXT_demo_allowed" }), "allowing one extension allows no other");
        Assert.IsEmpty(ShaderChecks.GlslErrors("#version 300 es\n#extension GL_EXT_demo_allowed : enable\nvoid main() {}", new HashSet<string>()),
            "enable and warn never fail");
    }
```

3. Create `unity/Assets/Tests/EditMode/Gles3Report.cs`, a read-only report (it changes no asset) that prints `Gles3Errors` for the shaders this track depends on:

```csharp
#nullable enable
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Plan Task 3 Step 0: prints ShaderChecks.Gles3Errors for URP Lit, URP Terrain Lit and every Statecraft shader, so
/// a `require` is judged once, up front, instead of in the middle of a task. Run with -executeMethod Gles3Report.Run.</summary>
public static class Gles3Report
{
    static readonly string[] Urp = { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Terrain/Lit" };

    public static void Run()
    {
        var shaders = Urp.Select(Shader.Find).ToList();
        shaders.AddRange(AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Shaders" })
            .Select(g => AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(g))));
        foreach (var shader in shaders.Where(s => s != null).Distinct())
        {
            var errors = ShaderChecks.Gles3Errors(shader);
            Debug.Log($"GLES3REPORT {shader.name}: {errors.Count} error(s)");
            foreach (var e in errors) Debug.Log($"GLES3REPORT   {e}");
        }
        foreach (var name in Urp)
            if (Shader.Find(name) == null) Debug.LogError($"GLES3REPORT {name}: shader not found");
        Debug.Log("GLES3REPORT done");
    }
}
```

4. Run the report and the new test, and read the output:

```bash
"$UNITY" -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Gles3Report.Run -logFile "$PWD/unity-gles3-report.log"; echo exit=$?
grep "GLES3REPORT" unity-gles3-report.log
"$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testFilter AnAllowedRequireIsAcceptedAndAnUnknownOneFailsWithHowToAddIt -testResults "$PWD/unity-editmode-gles3.xml" -logFile unity-tests.log; echo exit=$?
```

Expected: the test passes; the report ends with `GLES3REPORT done`; every shader line reads `0 error(s)`. For each `requires GL_...` line, decide once, here:

- Verify the extension in WebGL 2 in headless Chrome. This page compiles a fragment shader carrying exactly that line; set `EXT` to the extension name:

```bash
CHROME="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
EXT=GL_EXT_shader_non_constant_global_initializers
cat > "$PWD/gles3-require-probe.html" <<HTML
<script>
const gl = document.createElement('canvas').getContext('webgl2');
let out = 'NO WEBGL2';
if (gl) {
  const sh = gl.createShader(gl.FRAGMENT_SHADER);
  gl.shaderSource(sh, '#version 300 es\n#extension $EXT : require\nprecision highp float;\nout vec4 o;\nvoid main() { o = vec4(1.0); }');
  gl.compileShader(sh);
  out = gl.getShaderParameter(sh, gl.COMPILE_STATUS) ? 'REQUIRE OK' : 'REQUIRE FAILS: ' + gl.getShaderInfoLog(sh);
}
document.title = out; document.body.textContent = out;
</script>
HTML
"$CHROME" --headless=new --use-angle=swiftshader --enable-unsafe-swiftshader --virtual-time-budget=3000 --dump-dom "file://$PWD/gles3-require-probe.html" | grep -o "REQUIRE [A-Z]*.*\|NO WEBGL2"
rm -f "$PWD/gles3-require-probe.html"
```

- `REQUIRE OK`: add the extension to `AllowedRequires` with the comment `// <name>: verified to work in WebGL 2 in headless Chrome (<Chrome version>, <date>)`, re-run the report, and record the entry in `docs/visual-v1.md` under the GLES3 check.
- `REQUIRE FAILS` or `NO WEBGL2`: the shader genuinely cannot run on the web tier. Record which shader and which extension, and fix or avoid that shader before relying on it (for Unity's own URP shaders, report it to the controller instead of editing them).
- A shader still unverified is not allowed by default: it stays a failure.

Do not write any Task 3 shader until every `GLES3REPORT` line has been judged this way. If the allowlist changed, commit it on its own:

```bash
git add unity/Assets/Tests/EditMode/ShaderChecks.cs unity/Assets/Tests/EditMode/VisualHarnessTests.cs unity/Assets/Tests/EditMode/Gles3Report.cs docs/visual-v1.md
git commit -m "test: GLES3 require allowlist (default-deny, verified in WebGL 2) and the shader report" -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/TiltShiftTests.cs`:

```csharp
#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Plan Task 3: the two-band tilt-shift.</summary>
public class TiltShiftTests
{
    static readonly Vector4 P = new Vector4(0.5f, 0.17f, 0.18f, 10f / 1080f);

    [TearDown]
    public void Reset()
    {
        TiltShift.Enabled = true;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        VisualToggles.Clear();
    }

    [Test]
    public void TheMiddleBandIsSharpAndBothEdgesBlur()
    {
        Assert.AreEqual(0f, TiltShift.Weight(0.5f, P));
        Assert.AreEqual(0f, TiltShift.Weight(0.34f, P), 1e-6f, "inside the sharp half-width");
        Assert.AreEqual(1f, TiltShift.Weight(0.0f, P)); Assert.AreEqual(1f, TiltShift.Weight(1.0f, P));
        Assert.AreEqual(TiltShift.Weight(0.2f, P), TiltShift.Weight(0.8f, P), 1e-6f, "symmetric about the focus");
        Assert.Greater(TiltShift.Weight(0.15f, P), TiltShift.Weight(0.25f, P), "stronger towards the edge");
    }

    [Test]
    public void TheBlurIsResolutionIndependentAndFollowsTheTopBar()
    {
        Assert.AreEqual(0.5f, TiltShift.FocusCentre(0f));
        Assert.AreEqual(0.45f, TiltShift.FocusCentre(0.1f), 1e-6f, "the middle of the map area below a 10% bar");
        Assert.AreEqual(0.25f, TiltShift.FocusCentre(5f), 1e-6f, "clamped to CameraRig.MaxTopInset");
        Assert.AreEqual(0.45f, TiltShift.Params(0.1f, 30000f).x, 1e-6f);
        Assert.AreEqual(10f / 1080f, TiltShift.MaxRadiusUv, 1e-7f, "10 px of a 1080-line picture, whatever the window or render scale");
        Assert.AreEqual(TiltShift.MaxRadiusUv, TiltShift.Params(0f, 30000f).w, 1e-7f, "full strength when close");
        Assert.AreEqual(TiltShift.MaxRadiusUv * TiltShift.MinStrength, TiltShift.Params(0f, CameraController.MaxDistance).w, 1e-7f, "weakest at the overview");
    }

    [Test]
    public void BothTiersRunTheTiltShiftAndTheFarBlurIsGone()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(PostFxPaths.TiltShiftMaterial);
        Assert.IsNotNull(mat); Assert.AreEqual("Statecraft/TiltShift", mat.shader.name);
        foreach (var path in new[] { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" })
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            var f = data.rendererFeatures.OfType<TiltShiftFeature>().SingleOrDefault();
            Assert.IsNotNull(f, path); Assert.IsTrue(f!.isActive, path); Assert.AreSame(mat, f.Material, path);
        }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        Object.FindFirstObjectByType<MapScene>().Build();
        Assert.IsFalse(Camera.main.GetComponent<CameraRig>().Volume.profile.Has<DepthOfField>(), "the far-only Gaussian DoF is retired");
        CollectionAssert.Contains(VisualToggles.Names, "tiltshift");
    }

    [Test]
    public void TheTiltShiftShaderCompilesForWebGL2() =>
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/TiltShift")));

    /// <summary>Rendered on both tiers at the gameplay pose: local contrast in the top and bottom bands falls by at least
    /// 40%, the middle band keeps at least 95% of it. (Thresholds leave room for the terrain and grade tracks: a blur
    /// lowers gradient energy whatever the texture.)</summary>
    [UnityTest]
    public IEnumerator TheTopAndBottomBlurAndTheMiddleStaysSharpOnBothTiers()
    {
        int saved = QualitySettings.GetQualityLevel();
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        try
        {
            foreach (var level in new[] { 0, 1 })
            {
                QualitySettings.SetQualityLevel(level, true);
                TiltShift.Apply(0f, rig.Distance);
                TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
                TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
                float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
                float top = Ratio(0.90f, 0.98f), bottom = Ratio(0.02f, 0.10f), middle = Ratio(0.46f, 0.54f);
                TestContext.WriteLine($"{QualitySettings.names[level]}: top {top:0.00}, bottom {bottom:0.00}, middle {middle:0.00}");
                Assert.Less(top, 0.6f, "top band blurred"); Assert.Less(bottom, 0.6f, "bottom band blurred");
                Assert.Greater(middle, 0.95f, "middle band sharp");
                // Review N6: the same blur at twice the resolution (the radius is a share of the picture's height).
                TiltShift.Enabled = false; var sharpBig = RenderSample.Render(cam, 1920, 1080);
                TiltShift.Enabled = true; var shiftedBig = RenderSample.Render(cam, 1920, 1080);
                float topBig = shiftedBig.GradientEnergy(0.90f, 0.98f) / sharpBig.GradientEnergy(0.90f, 0.98f);
                TestContext.WriteLine($"{QualitySettings.names[level]}: top at 1920x1080 {topBig:0.00}");
                Assert.AreEqual(top, topBig, 0.1f, "the top band loses the same share of its detail at 1080 lines as at 540");
            }
        }
        finally { QualitySettings.SetQualityLevel(saved, true); }
    }

    /// <summary>Review I4: an asymmetric case, so a vertically flipped composite or a focus that ignores the top bar fails.
    /// With TopInset 0.3 the focus is at 0.35: the band around 0.35 stays sharp, and 0.70-0.78 (sharp when the focus is at
    /// 0.5) is blurred.</summary>
    [UnityTest]
    public IEnumerator AnOffCentreFocusFollowsTheTopBar()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0.3f; rig.Apply();
        Assert.AreEqual(0.35f, Shader.GetGlobalVector(TiltShift.ParamsId).x, 1e-5f, "CameraRig.Apply passes the inset");
        TiltShift.Enabled = false; var sharp = RenderSample.Render(cam, 960, 540);
        TiltShift.Enabled = true; var shifted = RenderSample.Render(cam, 960, 540);
        float Ratio(float y0, float y1) => shifted.GradientEnergy(y0, y1) / sharp.GradientEnergy(y0, y1);
        float focus = Ratio(0.33f, 0.37f), upper = Ratio(0.70f, 0.78f);
        TestContext.WriteLine($"focus band {focus:0.00}, upper band {upper:0.00}");
        Assert.Greater(focus, 0.95f, "sharp at the inset's focus (0.35)");
        Assert.Less(upper, 0.8f, "blurred above it");
        rig.TopInset = 0f; rig.Apply();
    }
}
```

`PostFxPaths` is the runtime-assembly copy of the material path that tests (which cannot see `Assets/Editor`) use, as `UiPaths` does for the UI; it lives in `TiltShift.cs`.

In `unity/Assets/Tests/EditMode/SettlementTests.cs`, replace

```csharp
    public void RebuildingDoesNotLeakDepthOfFieldComponents()
    {
        var map = NewMap(); map.Build();
        int after1 = Resources.FindObjectsOfTypeAll<DepthOfField>().Length;
        map.Build(); map.Build();
        Assert.AreEqual(after1, Resources.FindObjectsOfTypeAll<DepthOfField>().Length);
    }
```

with

```csharp
    public void RebuildingDoesNotLeakVolumeComponents()
    {
        var map = NewMap(); map.Build();
        int after1 = Resources.FindObjectsOfTypeAll<VolumeComponent>().Length;
        map.Build(); map.Build();
        Assert.AreEqual(after1, Resources.FindObjectsOfTypeAll<VolumeComponent>().Length);
    }
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -m2 "error CS" unity-tests.log
```
Expected: `exit=1`; `error CS0103: The name 'TiltShift' does not exist in the current context`.

- [ ] **Step 3: Implement the shader, the pass and the settings**

Create `unity/Assets/Shaders/TiltShift.shader`:

```hlsl
// Two-band tilt-shift (plan Task 3). Pass 0 blurs horizontally from the camera colour into a half-resolution target,
// pass 1 blurs that vertically, pass 2 composites it over the camera colour with the band weight as alpha. The radius at a
// pixel is TiltWeight(y) * _ScTiltShift.w pixels of the camera target, so the focus band stays sharp and the blur grows
// smoothly towards the top and bottom. The radius is a share of the picture's height (UV units). No depth texture: it runs on
// the web tier.
Shader "Statecraft/TiltShift"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _ScTiltShift;   // x focus centre (viewport y, 0 = bottom), y sharp half-width, z feather, w max radius (share of height)

        float TiltWeight(float y)
        {
            return smoothstep(_ScTiltShift.y, _ScTiltShift.y + _ScTiltShift.z, abs(y - _ScTiltShift.x));
        }

        half3 Sample(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb; }

        // 9-tap Gaussian (sigma = 2 taps) whose tap spacing is a quarter of the local radius.
        half3 Blur(float2 uv, float2 dir)
        {
            float r = TiltWeight(uv.y) * _ScTiltShift.w;
            // r is a share of the picture's height; the horizontal step converts it with the aspect ratio, which the render
            // scale does not change (review I4).
            float2 s = dir * (r * 0.25) * float2(_ScreenParams.y / _ScreenParams.x, 1.0);
            half3 c = Sample(uv) * 0.2270270;
            c += (Sample(uv + s) + Sample(uv - s)) * 0.1945946;
            c += (Sample(uv + 2.0 * s) + Sample(uv - 2.0 * s)) * 0.1216216;
            c += (Sample(uv + 3.0 * s) + Sample(uv - 3.0 * s)) * 0.0540541;
            c += (Sample(uv + 4.0 * s) + Sample(uv - 4.0 * s)) * 0.0162162;
            return c;
        }
        ENDHLSL

        Pass
        {
            Name "TiltShiftHorizontal"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return half4(Blur(i.texcoord, float2(1, 0)), 1); }
            ENDHLSL
        }
        Pass
        {
            Name "TiltShiftVertical"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return half4(Blur(i.texcoord, float2(0, 1)), 1); }
            ENDHLSL
        }
        Pass
        {
            Name "TiltShiftComposite"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target { return half4(Sample(i.texcoord), TiltWeight(i.texcoord.y)); }
            ENDHLSL
        }
    }
}
```

Create `unity/Assets/Scripts/Map/TiltShift.cs`:

```csharp
#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The tilt-shift band (plan Task 3; promo 03 blurs about the bottom 15% and top 20%). CameraRig.Apply calls
    /// Apply every frame with the top bar's inset and the rig distance; TiltShiftFeature draws the blur while Enabled. The
    /// radius is a share of the picture's height, so no pixel size is needed. Weight mirrors TiltWeight in TiltShift.shader.</summary>
    public static class TiltShift
    {
        public static readonly int ParamsId = Shader.PropertyToID("_ScTiltShift");
        public const float HalfWidth = 0.17f, Feather = 0.18f;
        /// <summary>The largest blur radius as a share of the picture's height: 10 px of a 1080-line picture.</summary>
        public const float MaxRadiusUv = 10f / 1080f;
        public const float FullStrengthDistanceM = 60000f, MinStrength = 0.35f;
        public static bool Enabled = true;

        /// <summary>HLSL smoothstep(p.y, p.y + p.z, |y - p.x|).</summary>
        public static float Weight(float y, Vector4 p)
        {
            float t = Mathf.Clamp01((Mathf.Abs(y - p.x) - p.y) / p.z);
            return t * t * (3f - 2f * t);
        }

        /// <summary>The middle of the map area below a top bar covering <paramref name="topInset"/> of the screen.</summary>
        public static float FocusCentre(float topInset) => (1f - Mathf.Clamp(topInset, 0f, CameraRig.MaxTopInset)) * 0.5f;

        public static float Strength(float distance) =>
            Mathf.Lerp(1f, MinStrength, Mathf.InverseLerp(FullStrengthDistanceM, CameraController.MaxDistance, distance));

        public static Vector4 Params(float topInset, float distance) =>
            new Vector4(FocusCentre(topInset), HalfWidth, Feather, MaxRadiusUv * Strength(distance));

        public static void Apply(float topInset, float distance) => Shader.SetGlobalVector(ParamsId, Params(topInset, distance));
    }

    /// <summary>Asset paths the tests share with the editor scripts (tests cannot reference Assets/Editor).</summary>
    public static class PostFxPaths
    {
        public const string TiltShiftMaterial = "Assets/Materials/TiltShift.mat";
    }
}
```

Create `unity/Assets/Scripts/Map/TiltShiftFeature.cs`:

```csharp
#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The tilt-shift renderer feature (on both URP renderers, PostFxAssets.Create). Before URP's own post-processing
    /// (so the grade and vignette apply on top): blur the camera colour horizontally into a 1/Downsample target, vertically
    /// into a second one, then composite back with the band weight as alpha. Game cameras only.</summary>
    public sealed class TiltShiftFeature : ScriptableRendererFeature
    {
        [SerializeField] Material? material;
        [SerializeField, Range(1, 4)] int downsample = 2;
        TiltShiftPass? pass;

        public Material? Material { get => material; set => material = value; }
        public int Downsample { get => downsample; set => downsample = Mathf.Clamp(value, 1, 4); }

        public override void Create() =>
            pass = new TiltShiftPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing, requiresIntermediateTexture = true };

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material == null || pass == null || !TiltShift.Enabled) return;
            pass.Setup(material, downsample);
            renderer.EnqueuePass(pass);
        }
    }

    sealed class TiltShiftPass : ScriptableRenderPass
    {
        Material material = null!;
        int downsample = 2;

        sealed class PassData { public TextureHandle source; public Material material = null!; public int pass; }

        public void Setup(Material m, int d) { material = m; downsample = d; }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var camera = frameData.Get<UniversalCameraData>();
            var resources = frameData.Get<UniversalResourceData>();
            if (camera.cameraType != CameraType.Game || resources.isActiveTargetBackBuffer) return;
            var colour = resources.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(colour);
            desc.sizeMode = TextureSizeMode.Explicit;
            desc.width = Mathf.Max(1, desc.width / downsample); desc.height = Mathf.Max(1, desc.height / downsample);
            desc.msaaSamples = MSAASamples.None; desc.clearBuffer = false;
            desc.filterMode = FilterMode.Bilinear; desc.wrapMode = TextureWrapMode.Clamp;
            desc.name = "TiltShiftA"; var a = renderGraph.CreateTexture(desc);
            desc.name = "TiltShiftB"; var b = renderGraph.CreateTexture(desc);
            Blit(renderGraph, "TiltShift Horizontal", colour, a, 0, AccessFlags.Write);
            Blit(renderGraph, "TiltShift Vertical", a, b, 1, AccessFlags.Write);
            Blit(renderGraph, "TiltShift Composite", b, colour, 2, AccessFlags.ReadWrite);   // blends over the sharp frame
        }

        void Blit(RenderGraph graph, string name, TextureHandle source, TextureHandle target, int shaderPass, AccessFlags targetAccess)
        {
            using var builder = graph.AddRasterRenderPass<PassData>(name, out var data);
            data.source = source; data.material = material; data.pass = shaderPass;
            builder.UseTexture(source, AccessFlags.Read);
            builder.SetRenderAttachment(target, 0, targetAccess);
            builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1, 1, 0, 0), d.material, d.pass));
        }
    }
}
```

Create `unity/Assets/Editor/PostFxAssets.cs`:

```csharp
#nullable enable
using System.Linq;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Creates TiltShift.mat and adds one active TiltShiftFeature using it to both URP renderers (web: Mobile, desktop:
    /// PC). Idempotent. Adding a feature needs the renderer's feature list and its local-file-id map kept in step, as URP's own
    /// renderer inspector does. Run headless:
    /// Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.PostFxAssets.Create</summary>
    public static class PostFxAssets
    {
        public static readonly string[] Renderers = { "Assets/Settings/Mobile_Renderer.asset", "Assets/Settings/PC_Renderer.asset" };

        public static void Create()
        {
            var shader = Shader.Find("Statecraft/TiltShift") ?? throw new System.InvalidOperationException("shader not found: Statecraft/TiltShift");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(PostFxPaths.TiltShiftMaterial);
            if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, PostFxPaths.TiltShiftMaterial); }
            else mat.shader = shader;
            EditorUtility.SetDirty(mat);
            foreach (var path in Renderers)
                AddFeature(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path) ?? throw new System.IO.FileNotFoundException("renderer missing", path), mat);
            AssetDatabase.SaveAssets();
        }

        static void AddFeature(ScriptableRendererData data, Material mat)
        {
            var feature = data.rendererFeatures.OfType<TiltShiftFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<TiltShiftFeature>();
                feature.name = "TiltShift";
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.SaveAssetIfDirty(data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
                var so = new SerializedObject(data);
                var features = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                features.arraySize++; features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++; map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            feature.Material = mat;
            feature.SetActive(true);
            EditorUtility.SetDirty(feature); EditorUtility.SetDirty(data);
        }
    }
}
```

In `unity/Assets/Scripts/Map/CameraRig.cs`:
1. Replace the class summary's first sentence `/// <summary>Diorama camera: fixed 50-degree pitch, 35-degree FOV, Gaussian far blur beyond the look-at point (tilt-shift look).` with `/// <summary>Diorama camera: fixed 50-degree pitch, 35-degree FOV, a two-band tilt-shift (TiltShiftFeature) driven from Apply.` and delete the summary line `/// Gaussian, not Bokeh: physical Bokeh DoF keeps everything sharp at 100 km focus distances.</summary>`, ending the preceding line with `</summary>`.
2. Delete the line `        DepthOfField _dof;`.
3. Replace

```csharp
            _dof = vol.profile.Add<DepthOfField>(true); _dof.mode.Override(DepthOfFieldMode.Gaussian);
            _dof.gaussianMaxRadius.Override(1.5f);
            Volume = vol;
```

   with

```csharp
            Volume = vol;
            TeardownActions.Add(VisualToggles.Register("tiltshift", on => TiltShift.Enabled = on));
```

4. Replace

```csharp
            if (_dof != null) { _dof.gaussianStart.Override(Distance * 1.05f); _dof.gaussianEnd.Override(Distance * 1.8f); }
```

   with

```csharp
            TiltShift.Apply(TopInset, Distance);
```

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.PostFxAssets.Create -logFile "$PWD/unity-postfx.log"; echo exit=$?
git status --short unity/Assets
```
Expected: `exit=0`; new `unity/Assets/Materials/TiltShift.mat` (+ `.meta`); ` M unity/Assets/Settings/Mobile_Renderer.asset` and ` M unity/Assets/Settings/PC_Renderer.asset` (each gains one `TiltShiftFeature` sub-asset, its `m_RendererFeatures` entry and its `m_RendererFeatureMap` id).

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "^(Mobile|PC): top" unity-tests.log
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=0`; total = **(Task 2 total) + 6**, `failed="0"`; two lines like `Mobile: top 0.3x, bottom 0.3x, middle 1.00` and one `focus band 0.9x, upper band 0.xx`; no warnings. If `AnOffCentreFocusFollowsTheTopBar` fails with the focus band blurred and the band at 0.62–0.68 sharp, the composite is flipped vertically: check that `Blitter.BlitTexture` is given `new Vector4(1, 1, 0, 0)` and not the RTHandle scale, then re-run.

- [ ] **Step 5: Profile and run the art checkpoint (non-blocking)**

Run:
```bash
git add unity/Assets/Settings/Mobile_Renderer.asset unity/Assets/Settings/PC_Renderer.asset
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py profile --build unity/Builds/Web-disabled --dpr 1 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t03-tiltshift
```
Expected: `PROFILE summary visual=-tiltshift` lines for both poses, headless and headed; budget `median(all) - median(-tiltshift) <= 1.5 ms + the larger spread` (record, do not block); the checkpoint passes golden and shots. Record the summary lines under `## Task 3: tilt-shift` in `docs/visual-v1.md` and add the checkpoint row.

- [ ] **Step 6: Commit**

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add unity/Assets/Shaders/TiltShift.shader unity/Assets/Shaders/TiltShift.shader.meta \
  unity/Assets/Scripts/Map/TiltShift.cs unity/Assets/Scripts/Map/TiltShift.cs.meta \
  unity/Assets/Scripts/Map/TiltShiftFeature.cs unity/Assets/Scripts/Map/TiltShiftFeature.cs.meta \
  unity/Assets/Editor/PostFxAssets.cs unity/Assets/Editor/PostFxAssets.cs.meta \
  unity/Assets/Materials/TiltShift.mat unity/Assets/Materials/TiltShift.mat.meta \
  unity/Assets/Settings/Mobile_Renderer.asset unity/Assets/Settings/PC_Renderer.asset \
  unity/Assets/Scripts/Map/CameraRig.cs unity/Assets/Tests/EditMode/SettlementTests.cs docs/visual-v1.md \
  unity/Assets/Tests/EditMode/TiltShiftTests.cs unity/Assets/Tests/EditMode/TiltShiftTests.cs.meta
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(visual): a two-band tilt-shift as a half-resolution render-graph pass; the far-only Gaussian DoF is retired

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 4: Colour grade, tonemapping and vignette, checked for readability

*Track A. Effort 5–7 h.* Research §1 "Colour grade": the build is ungraded, with muddy midtones; the promo pairs a teal sea with warm amber land, lifted saturation, crisp contrast and a gentle vignette. Both tiers grade in LDR (`m_ColorGradingMode: 0`), where URP bakes colour adjustments, white balance and split toning into one LUT pass, the post effects Unity lists as mobile-friendly; the vignette is one more full-screen term. Haze is in the shaders (controller question 3, Tasks 6–7).

**Tonemapper (review I9).** Both tiers render HDR (`m_SupportsHDR: 1`) with no tonemapper today, so the sunlit desert at the new sun intensity 1.6 plus +0.15 exposure clips to flat white. *Used:* **Neutral**. In LDR grading URP applies the tonemapper in the uber post pass just before the grading LUT (`Common.hlsl` `ApplyColorGrading`: "LDR Grading: Apply tonemapping (result is clamped) ... Apply internal linear LUT"), a few ALU operations per pixel. Neutral only rolls off the highlights and leaves mid-tone hue and contrast near identity, so the grade below keeps meaning what it says; ACES's filmic curve and its gamut conversion darken the mid-tones and shift saturated hues (the sea's blue and the amber land are exactly the colours the grade is built on), so it would fight the grade. `TheSunlitDesertDoesNotClip` shows the result.

**Readability (review I9).** The warm balance, amber highlights and +16 saturation push the land towards the selection band's gold (1, 0.84, 0.38). `TheSelectedHexStillStandsOutOnGradedLand` measures it at both promo poses, graded and ungraded (CIE76 ΔE ≥ 20 between the band and the land 1 km inside the hex), and `ColourBlindPlayersStillTellCoastAndSelectionApart` simulates deuteranopia and protanopia (Machado 2009) and requires ΔE ≥ 15 for sea against land, the selection against land and the selection against sea. Borders are not drawn in visual v1; the art track's border task adds its colours to that test (Deferred). The checkpoint images also get deuteranopia and protanopia copies for the owner (Task 1's `art_compare.py`).

**Files:**
- Create: `unity/Assets/Scripts/Map/MapGrade.cs`, `unity/Assets/Tests/EditMode/GradeTests.cs`
- Modify: `unity/Assets/Scripts/Map/CameraRig.cs`, `docs/visual-v1.md`

**Interfaces:**
- Consumes: `CameraRig.Init` (the runtime `VolumeProfile`), `VisualToggles`, `RenderSample`, `PromoPose`.
- Produces: `MapGrade { const TonemappingMode Tonemapper = TonemappingMode.Neutral; const float PostExposure = 0.15f, Contrast = 14f, Saturation = 16f /* shipped: 0, set at the merge point */, Temperature = 5f, VignetteIntensity = 0.26f; static readonly Color SplitShadows, SplitHighlights, VignetteColour; static void Apply(VolumeProfile profile); static void SetActive(VolumeProfile profile, bool on); }`; toggle `"grade"` (grade, tonemapper and vignette together).

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/GradeTests.cs`:

```csharp
#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

/// <summary>Plan Task 4: the grade, the tonemapper, the vignette, and what they do to readability.</summary>
public class GradeTests
{
    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static (MapScene map, CameraRig rig, VolumeProfile profile) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var rig = Camera.main.GetComponent<CameraRig>();
        return (map, rig, rig.Volume.profile);
    }

    [Test]
    public void TheGradeIsWarmLandTealShadowsAndAGentleVignette()
    {
        var (_, _, profile) = Open();
        Assert.IsTrue(profile.TryGet<ColorAdjustments>(out var ca));
        Assert.AreEqual(MapGrade.Saturation, ca.saturation.value); Assert.Greater(ca.contrast.value, 0f);
        Assert.IsTrue(profile.TryGet<WhiteBalance>(out var wb)); Assert.Greater(wb.temperature.value, 0f, "warm");
        Assert.IsTrue(profile.TryGet<SplitToning>(out var st));
        Assert.Greater(st.shadows.value.b, st.shadows.value.r, "teal shadows"); Assert.Greater(st.highlights.value.r, st.highlights.value.b, "amber highlights");
        Assert.IsTrue(profile.TryGet<Vignette>(out var v)); Assert.That(v.intensity.value, Is.InRange(0.2f, 0.35f));
        Assert.IsTrue(profile.TryGet<Tonemapping>(out var tm)); Assert.AreEqual(TonemappingMode.Neutral, tm.mode.value);
        CollectionAssert.Contains(VisualToggles.Names, "grade");
    }

    static HexJson NearestLandHex(MapScene map, double lon, double lat)
    {
        HexJson? best = null;
        foreach (var h in map.Hexes.hexes)
            if (h.land && h.biome != "lake" && (best == null || (h.lon - lon) * (h.lon - lon) + (h.lat - lat) * (h.lat - lat) < (best.lon - lon) * (best.lon - lon) + (best.lat - lat) * (best.lat - lat))) best = h;
        return best!;
    }

    /// <summary>Selects the land hex nearest (lon, lat) and returns world points on the band (the middle of its second edge,
    /// read from the band's own mesh, so it holds whatever height the band rides at) and on the land 1 km inside it.</summary>
    static (Vector3 band, Vector3 land) SelectNear(MapScene map, double lon, double lat)
    {
        var hex = NearestLandHex(map, lon, lat);
        map.Select(hex.q, hex.r);
        var sel = map.Terrain.transform.Find(MapScene.SelectionName);
        var v = sel.GetComponent<MeshFilter>().sharedMesh.vertices;
        int n = MapBuilder.RingVertices, j = MapBuilder.OverlaySegmentsPerEdge + MapBuilder.OverlaySegmentsPerEdge / 2;
        var band = sel.TransformPoint((v[j] + v[n + j]) / 2f);
        var centre = map.Frame.ToWorld(hex.lon, hex.lat, 0);
        var inward = new Vector3(centre.x - band.x, 0, centre.z - band.z).normalized;
        var land = new Vector3(band.x, 0, band.z) + inward * 1000f; land.y = map.Terrain.SampleHeight(land);
        return (band, land);
    }

    /// <summary>Review I9: the selected hex still reads on graded land, at both promo poses, graded and ungraded.</summary>
    [UnityTest]
    public IEnumerator TheSelectedHexStillStandsOutOnGradedLand()
    {
        var (map, rig, _) = Open();
        yield return null;
        var cam = Camera.main;
        foreach (var pose in PromoPose.All)
        {
            pose.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
            var (band, land) = SelectNear(map, pose.Lon, pose.Lat);
            foreach (var graded in new[] { true, false })
            {
                VisualToggles.Set("grade", graded);
                var px = new Vector2[2];
                var s = RenderSample.Render(cam, 960, 540, new[] { band, land }, px);
                Color b = s.At((int)px[0].x, (int)px[0].y), l = s.Mean((int)px[1].x, (int)px[1].y, 1);
                float de = RenderSample.DeltaE(b, l);
                TestContext.WriteLine($"{pose.Name} graded={graded}: band {b}, land {l}, deltaE {de:0.0}");
                Assert.GreaterOrEqual(de, 20f, $"{pose.Name} graded={graded}");
            }
            VisualToggles.Set("grade", true);
        }
        map.Select(-9999, -9999);
    }

    /// <summary>Review I9: colour-blind players. Under simulated deuteranopia and protanopia (Machado 2009, severity 1), at
    /// the gameplay pose: sea against land, the selection against land and the selection against sea stay at deltaE76 >= 15.
    /// (Borders are not drawn in visual v1; the art track's border task adds its colours here.)</summary>
    [UnityTest]
    public IEnumerator ColourBlindPlayersStillTellCoastAndSelectionApart()
    {
        var (map, rig, _) = Open();
        yield return null;
        var cam = Camera.main;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var (band, land) = SelectNear(map, 34.86, 32.00);
        var sea = map.Frame.ToWorld(34.66, 32.00, 0); sea.y = map.Terrain.SampleHeight(sea);
        var px = new Vector2[3];
        var s = RenderSample.Render(cam, 960, 540, new[] { band, land, sea }, px);
        Color b = s.At((int)px[0].x, (int)px[0].y), l = s.Mean((int)px[1].x, (int)px[1].y, 1), w = s.Mean((int)px[2].x, (int)px[2].y, 2);
        foreach (var kind in new[] { RenderSample.Cvd.Deuteranopia, RenderSample.Cvd.Protanopia })
        {
            Color B = RenderSample.SimulateCvd(b, kind), L = RenderSample.SimulateCvd(l, kind), W = RenderSample.SimulateCvd(w, kind);
            float seaLand = RenderSample.DeltaE(W, L), selLand = RenderSample.DeltaE(B, L), selSea = RenderSample.DeltaE(B, W);
            TestContext.WriteLine($"{kind}: sea-land {seaLand:0.0}, selection-land {selLand:0.0}, selection-sea {selSea:0.0}");
            Assert.GreaterOrEqual(seaLand, 15f, $"{kind}: the coast");
            Assert.GreaterOrEqual(selLand, 15f, $"{kind}: the selection on land");
            Assert.GreaterOrEqual(selSea, 15f, $"{kind}: the selection against the sea");
        }
        map.Select(-9999, -9999);
    }

    /// <summary>Review I9: with the Neutral tonemapper the sunlit desert does not clip: in the Negev (34.9 E, 30.8 N, 60 km)
    /// and at the gameplay pose, under 1% of pixels have any channel at the top 8-bit step.</summary>
    [UnityTest]
    public IEnumerator TheSunlitDesertDoesNotClip()
    {
        var (map, rig, _) = Open();
        yield return null;
        var cam = Camera.main;
        foreach (var (name, lon, lat, distance) in new[] { ("Negev", 34.9, 30.8, 60000f), ("gameplay03", PromoPose.Gameplay03.Lon, PromoPose.Gameplay03.Lat, PromoPose.Gameplay03.Distance) })
        {
            rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var s = RenderSample.Render(cam, 480, 270);
            float clipped = s.FractionClipped(0.995f);
            TestContext.WriteLine($"{name}: clipped {clipped:P2}");
            Assert.Less(clipped, 0.01f, name);
        }
    }

    /// <summary>Rendered at the gameplay pose: the grade lifts mean saturation by at least 5%.</summary>
    [UnityTest]
    public IEnumerator TheGradeLiftsSaturation()
    {
        var (map, rig, _) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var cam = Camera.main;
        VisualToggles.Set("grade", false); var flat = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("grade", true); var graded = RenderSample.Render(cam, 480, 270);
        TestContext.WriteLine($"saturation {flat.MeanSaturation():0.000} -> {graded.MeanSaturation():0.000}");
        Assert.Greater(graded.MeanSaturation(), flat.MeanSaturation() * 1.05f);
    }

    /// <summary>Rendered: the vignette darkens the corners relative to the centre.</summary>
    [UnityTest]
    public IEnumerator TheVignetteDarkensTheCorners()
    {
        var (map, rig, profile) = Open();
        yield return null;
        PromoPose.Gameplay03.Apply(rig, map.Frame); rig.TopInset = 0f; rig.Apply();
        var cam = Camera.main;
        profile.TryGet<Vignette>(out var v);
        v.active = false; var plain = RenderSample.Render(cam, 480, 270);
        v.active = true; var vignetted = RenderSample.Render(cam, 480, 270);
        float Ratio(RenderSample s) => s.MeanLuma(0f, 0f, 0.1f, 0.1f) / s.MeanLuma(0.45f, 0.45f, 0.55f, 0.55f);
        TestContext.WriteLine($"corner/centre {Ratio(plain):0.000} -> {Ratio(vignetted):0.000}");
        Assert.Less(Ratio(vignetted), Ratio(plain) * 0.9f);
    }
}
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=1`; `error CS0103: The name 'MapGrade' does not exist in the current context`.

- [ ] **Step 3: Implement**

Create `unity/Assets/Scripts/Map/MapGrade.cs`:

```csharp
#nullable enable
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.Map
{
    /// <summary>The map's colour grade (research notes §1, §2.2 "Colour grading"): a Neutral tonemapper (the HDR sun no longer
    /// clips the desert), slightly brighter, more contrast and saturation, a warm white balance, teal shadows against amber
    /// highlights (the promo's complementary sea and land), and a gentle dark-navy vignette. LDR grading on both tiers: the
    /// tonemapper and one LUT in the uber pass.</summary>
    public static class MapGrade
    {
        /// <summary>Neutral, not ACES: it only rolls off highlights, so the sea's blue and the amber land keep their hue (plan Task 4).</summary>
        public const TonemappingMode Tonemapper = TonemappingMode.Neutral;
        public const float PostExposure = 0.15f, Contrast = 14f, Saturation = 16f, Temperature = 5f, Tint = 2f;   // 9 -> 5 in Task 4 so the merged diorama warmth lands below the promo's 0.145
        public const float SplitBalance = -15f, VignetteIntensity = 0.26f, VignetteSmoothness = 0.45f;
        public static readonly Color SplitShadows = new Color(0.36f, 0.52f, 0.60f);
        public static readonly Color SplitHighlights = new Color(0.96f, 0.78f, 0.55f);
        public static readonly Color VignetteColour = new Color(0.05f, 0.06f, 0.10f);

        public static void Apply(VolumeProfile profile)
        {
            var tm = profile.Add<Tonemapping>(true); tm.mode.Override(Tonemapper);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(PostExposure); ca.contrast.Override(Contrast); ca.saturation.Override(Saturation);
            ca.hueShift.Override(0f); ca.colorFilter.Override(Color.white);
            var wb = profile.Add<WhiteBalance>(true); wb.temperature.Override(Temperature); wb.tint.Override(Tint);
            var st = profile.Add<SplitToning>(true);
            st.shadows.Override(SplitShadows); st.highlights.Override(SplitHighlights); st.balance.Override(SplitBalance);
            var v = profile.Add<Vignette>(true);
            v.intensity.Override(VignetteIntensity); v.smoothness.Override(VignetteSmoothness); v.color.Override(VignetteColour);
            v.center.Override(new Vector2(0.5f, 0.5f)); v.rounded.Override(false);
        }

        public static void SetActive(VolumeProfile profile, bool on)
        {
            foreach (var c in profile.components)
                if (c is Tonemapping || c is ColorAdjustments || c is WhiteBalance || c is SplitToning || c is Vignette) c.active = on;
        }
    }
}
```

In `unity/Assets/Scripts/Map/CameraRig.cs`, replace

```csharp
            Volume = vol;
            TeardownActions.Add(VisualToggles.Register("tiltshift", on => TiltShift.Enabled = on));
```

with

```csharp
            Volume = vol;
            MapGrade.Apply(vol.profile);
            TeardownActions.Add(VisualToggles.Register("tiltshift", on => TiltShift.Enabled = on));
            TeardownActions.Add(VisualToggles.Register("grade", on => MapGrade.SetActive(vol.profile, on)));
```

(`MapScene.Build` already owns every component of the profile, so the four new components are destroyed with the map; `RebuildingDoesNotLeakVolumeComponents` checks it.)

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=0`; total = **(Task 3 total) + 6**, `failed="0"`; the `saturation`, `corner/centre`, `clipped`, `deltaE` and colour-blind lines in `unity-tests.log`; no warnings. If the selection test fails only graded, lower `MapGrade.Saturation` or `SplitHighlights`' amber before touching the selection colour (the gold is the UI's), and re-run.

- [ ] **Step 5: Profile and run the art checkpoint (non-blocking)**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t04-grade
```
Expected: `PROFILE summary visual=-grade` lines (budget 2.5 ms plus the larger spread); golden and shots pass. Record under `## Task 4: grade` in `docs/visual-v1.md`, with the art-log row's saturation and warmth next to the promo's (the caption prints both), and look at the `*-deuteranopia.png`/`*-protanopia.png` copies for anything the test points do not cover.

- [ ] **Step 6: Commit**

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add unity/Assets/Scripts/Map/MapGrade.cs unity/Assets/Scripts/Map/MapGrade.cs.meta unity/Assets/Scripts/Map/CameraRig.cs docs/visual-v1.md \
  unity/Assets/Tests/EditMode/GradeTests.cs unity/Assets/Tests/EditMode/GradeTests.cs.meta
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(visual): colour grade (warm balance, teal/amber split toning, saturation, contrast) and a gentle vignette

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Phase 1, Track B: ground and water (Tasks 5–8)

### Task 5: Baked AO, curvature, sky-view and coast distance from the heightmap

*Track B, worktree `vis-ground`. Effort 5–7 h.* Research §2.2 "Baked terrain lighting": the carved-diorama look (dark gullies, bright ridges, shaded valley floors) comes from offline terrain analysis, which costs nothing at run time and is the best value on WebGL. The sea needs a distance-to-coast field for its colour ramp and foam without a depth texture. Both are baked here, in the tools pipeline, from the **committed** heightmap (no network: the 30 m DEM tiles are a cache that is not in the repository), with heights exaggerated exactly as the game draws them (`MapBuilder.VerticalExaggeration = 3`), so the shading matches the visible relief.

Outputs (2048 × 2048, row 0 = north, texel (j, i) = the mean of heightmap samples j..j+1, i..i+1, so texel centres sit at the terrain UVs (j + 0.5) / 2048):
- `terrain_bake.png` (RGB8): **R** ambient occlusion (horizon search to 2 km; 1 = open), **G** curvature (0.5 flat, > 0.5 convex ridge, < 0.5 hollow), **B** sky-view factor (horizon search to 12 km).
- `coast.png` (L8): signed distance to the nearest shoreline, `0.5 + d / (2 × 12 km)` clamped to [0, 1], negative in water. Water is open sea (|elevation| ≤ 0.5 m, connected to the map's border, as `hexgrid.py` rule 1 defines sea) and the lakes (flat water below −150 m, as rule 2: the Sea of Galilee, the Dead Sea and its evaporation ponds).

**Files:**
- Create: `tools/terrain_bake.py`, `tools/tests/test_terrain_bake.py`, `unity/Assets/Textures/Map/terrain_bake.png`, `unity/Assets/Textures/Map/coast.png` (both generated), their `.meta` files (Unity import)
- Modify: `tools/README.md`

**Interfaces:**
- Consumes: `unity/Assets/StreamingAssets/map/heightmap.{r16,json}` (R15).
- Produces: the two PNGs at the paths above; constants other tasks mirror: `COAST_RANGE_M = 12000.0` (HLSL `ScCoastRangeM`, Task 7), `VERTICAL_EXAGGERATION = 3.0` (`MapBuilder.VerticalExaggeration`); functions `load_heightmap`, `spacing_m`, `openness`, `curvature`, `water_mask`, `distance_to`, `to_texels`, `encode_coast`, `bake`, `main(map_dir, out_dir)`.

- [ ] **Step 1: Write the failing tests**

Create `tools/tests/test_terrain_bake.py`:

```python
import json
from pathlib import Path

import numpy as np
import pytest
from PIL import Image

import terrain_bake as tb

ROOT = Path(__file__).resolve().parents[2]


def meta_for(h: int, w: int, dx: float, dy: float) -> dict:
    return {"width": w, "height": h, "width_km": (w - 1) * dx / 1000, "length_km": (h - 1) * dy / 1000,
            "lon0": 34.2, "lon1": 35.9, "lat0": 29.45, "lat1": 33.35}


def test_flat_land_is_open_and_has_no_curvature():
    z = np.full((41, 41), 300.0, dtype=np.float32)
    assert np.allclose(tb.openness(z, 100, 100, 2000), 1.0)
    assert np.allclose(tb.curvature(z, 100, 100), 0.5)


def test_a_pit_sees_less_sky_than_its_rim():
    y, x = np.mgrid[0:65, 0:65]
    z = (100 + 2.0 * ((x - 32) ** 2 + (y - 32) ** 2)).astype(np.float32)   # a bowl: 0 at the centre, ~2 km at the corners
    sky = tb.openness(z, 100, 100, 2000)
    assert sky[32, 32] < 0.9
    assert sky[32, 32] < sky[0, 0]


def test_ridges_are_convex_and_valleys_concave():
    x = np.arange(65)
    z = np.tile(500 + 300 * np.cos(2 * np.pi * x / 32), (65, 1)).astype(np.float32)   # crests at x = 0, 32, 64
    c = tb.curvature(z, 100, 100)
    assert c[32, 32] > 0.55 and c[32, 16] < 0.45


def test_sea_is_border_connected_zero_and_an_inland_zero_pocket_is_land():
    elev = np.full((40, 40), 50.0, dtype=np.float32)
    elev[:, :10] = 0.0                      # the sea along the west border
    elev[19:22, 29:32] = 0.0                # an enclosed 0 m pocket
    water = tb.water_mask(elev)
    assert water[:, :10].all()
    assert not water[19:22, 29:32].any()
    assert not water[:, 10:].any()


def test_flat_water_below_minus_150_is_a_lake_and_a_slope_is_not():
    elev = np.full((40, 40), 300.0, dtype=np.float32)
    elev[5:17, 5:17] = -420.0                                                 # the Dead Sea's surface: flat
    elev[22:34, 5:17] = (-200.0 - 10.0 * np.arange(12))[None, :]              # the rift floor: sloping
    water = tb.water_mask(elev)
    assert water[8:14, 8:14].all()
    assert not water[22:34, 5:17].any()


def test_coast_distance_is_signed_metres_across_a_straight_coast():
    elev = np.full((30, 60), 50.0, dtype=np.float32)
    elev[:, :20] = 0.0                                                        # sea columns 0..19
    water = tb.water_mask(elev)
    to_water = tb.distance_to(water, 100, 100, 12000)
    to_land = tb.distance_to(~water, 100, 100, 12000)
    assert to_water[15, 30] == pytest.approx(1100)                           # 11 columns east of the last sea column
    assert to_land[15, 9] == pytest.approx(1100)                             # 11 columns west of the first land column
    assert to_water[15, 59] == pytest.approx(4000)


def test_distance_uses_each_axis_own_spacing():
    elev = np.full((40, 30), 50.0, dtype=np.float32)
    elev[:20, :] = 0.0                                                        # sea in the north rows (row 0 = north)
    d = tb.distance_to(tb.water_mask(elev), 79.0, 211.0, 12000)
    assert d[25, 15] == pytest.approx(6 * 211.0)


def test_texels_average_four_samples_and_coast_encoding_is_centred():
    a = np.arange(9, dtype=np.float32).reshape(3, 3)
    assert np.array_equal(tb.to_texels(a), np.array([[2, 3], [5, 6]], dtype=np.float32))
    enc = tb.encode_coast(np.array([0.0, -12000.0, 12000.0, -50000.0, 120.0], dtype=np.float32))
    assert enc.tolist() == [128, 0, 255, 0, 129]


def test_main_writes_both_textures_one_texel_smaller_and_deterministically(tmp_path):
    n = 33
    y, x = np.mgrid[0:n, 0:n]
    elev = np.where(x < 8, 0.0, 40.0 + 30.0 * np.sin(y / 3.0) + 20.0 * x).astype(np.float32)
    lo, hi = float(elev.min()), float(elev.max())
    map_dir = tmp_path / "map"; map_dir.mkdir()
    np.round((elev - lo) / (hi - lo) * 65535).astype("<u2").tofile(map_dir / "heightmap.r16")
    (map_dir / "heightmap.json").write_text(json.dumps({**meta_for(n, n, 79.0, 211.0), "elev_min_m": lo, "elev_max_m": hi}))
    tb.main(map_dir, tmp_path / "a"); tb.main(map_dir, tmp_path / "b")
    bake = Image.open(tmp_path / "a" / "terrain_bake.png"); coast = Image.open(tmp_path / "a" / "coast.png")
    assert (bake.mode, bake.size) == ("RGB", (n - 1, n - 1))
    assert (coast.mode, coast.size) == ("L", (n - 1, n - 1))
    for name in ("terrain_bake.png", "coast.png"):
        assert (tmp_path / "a" / name).read_bytes() == (tmp_path / "b" / name).read_bytes()
    c = np.asarray(coast)
    assert c[:, :5].max() < 128 and c[:, 12:].min() > 128                   # water west, land east


def texel(lon: float, lat: float) -> tuple[int, int]:
    meta = json.loads((ROOT / "unity/Assets/StreamingAssets/map/heightmap.json").read_text())
    col = int((lon - meta["lon0"]) / (meta["lon1"] - meta["lon0"]) * 2048)
    row = int((meta["lat1"] - lat) / (meta["lat1"] - meta["lat0"]) * 2048)
    return row, col


def test_the_committed_coast_texture_marks_the_seas_and_lakes():
    c = np.asarray(Image.open(ROOT / "unity/Assets/Textures/Map/coast.png"))
    assert c.shape == (2048, 2048)
    assert c[texel(34.40, 32.00)] < 64, "the Mediterranean off Tel Aviv, well offshore"
    assert c[texel(35.59, 32.82)] < 128, "the Sea of Galilee"
    assert c[texel(35.50, 31.50)] < 128, "the Dead Sea"
    assert c[texel(34.95, 29.50)] < 128, "the Gulf of Aqaba off Eilat"
    assert c[texel(35.21, 31.78)] > 192, "Jerusalem, far inland"
    b = np.asarray(Image.open(ROOT / "unity/Assets/Textures/Map/terrain_bake.png"))
    assert b.shape == (2048, 2048, 3)
```

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests/test_terrain_bake.py -q 2>&1 | tail -2
```
Expected: FAIL, `ModuleNotFoundError: No module named 'terrain_bake'`.

- [ ] **Step 2: Implement**

Create `tools/terrain_bake.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = ["numpy==2.5.3", "pillow==12.3.0"]
# ///
"""Heightmap -> the baked shading textures of the terrain and sea shaders (no network: reads only the committed heightmap).

Usage: uv run tools/terrain_bake.py   (about 45 seconds)
Reads unity/Assets/StreamingAssets/map/heightmap.r16 + heightmap.json; writes, 2048 x 2048, row 0 = north:
  unity/Assets/Textures/Map/terrain_bake.png  RGB8: R ambient occlusion (horizon search to AO_RADIUS_M; 1 = open),
      G curvature (0.5 flat, > 0.5 convex), B sky-view factor (horizon search to SKY_RADIUS_M)
  unity/Assets/Textures/Map/coast.png         L8: 0.5 + d / (2 COAST_RANGE_M), clamped; d = signed distance (m) to the
      nearest shoreline, < 0 in water (open sea: |elevation| <= SEA_EPS_M and connected to the map's border; lakes: flat
      water below LAKE_BELOW_M), the rules tools/hexgrid.py uses for its sea and lake biomes
Heights are multiplied by VERTICAL_EXAGGERATION (MapBuilder.VerticalExaggeration) so the shading matches the relief the
game draws. Texel (j, i) is the mean of heightmap samples j..j+1, i..i+1, so its centre sits at terrain UV (j + 0.5) / 2048.
Distances use each axis' own sample spacing (about 79 m east-west, 211 m north-south)."""
import json
import math
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
MAP_DIR = ROOT / "unity" / "Assets" / "StreamingAssets" / "map"
OUT_DIR = ROOT / "unity" / "Assets" / "Textures" / "Map"
VERTICAL_EXAGGERATION = 3.0
AO_RADIUS_M = 2000.0
SKY_RADIUS_M = 12000.0
DIRECTIONS = 16
STEPS = 16
CURVATURE_RADIUS_M = 600.0
CURVATURE_SCALE_M = 150.0      # exaggerated metres above or below the local mean that reach 1 or 0
SEA_EPS_M = 0.5
LAKE_BELOW_M = -150.0
LAKE_FLAT_M = 0.25             # largest height range in a 3x3 window that is still a water surface
COAST_RANGE_M = 12000.0        # StatecraftMap.hlsl ScCoastRangeM


def load_heightmap(map_dir: Path) -> tuple[np.ndarray, dict]:
    meta = json.loads((map_dir / "heightmap.json").read_text())
    raw = np.fromfile(map_dir / "heightmap.r16", dtype="<u2").reshape(meta["height"], meta["width"])
    elev = meta["elev_min_m"] + raw.astype(np.float64) / 65535.0 * (meta["elev_max_m"] - meta["elev_min_m"])
    return elev.astype(np.float32), meta


def spacing_m(meta: dict) -> tuple[float, float]:
    return meta["width_km"] * 1000 / (meta["width"] - 1), meta["length_km"] * 1000 / (meta["height"] - 1)


def shifted(a: np.ndarray, oy: int, ox: int) -> np.ndarray:
    """out[y, x] = a[y + oy, x + ox], the border's values repeated beyond it."""
    h, w = a.shape
    ys = np.clip(np.arange(h) + oy, 0, h - 1)
    xs = np.clip(np.arange(w) + ox, 0, w - 1)
    return a[ys[:, None], xs[None, :]]


def openness(z: np.ndarray, dx: float, dy: float, radius_m: float, directions: int = DIRECTIONS, steps: int = STEPS) -> np.ndarray:
    """Sky-view factor: 1 - the mean over directions of sin(horizon elevation), the horizon searched out to radius_m and
    never below 0 (a peak sees the whole upper hemisphere: 1)."""
    acc = np.zeros(z.shape, dtype=np.float32)
    for k in range(directions):
        theta = 2 * math.pi * k / directions
        best = np.zeros(z.shape, dtype=np.float32)      # tan of the highest horizon so far
        seen: set[tuple[int, int]] = set()
        for s in range(1, steps + 1):
            r = radius_m * (s / steps) ** 1.5             # denser near the point
            ox, oy = int(round(r * math.cos(theta) / dx)), int(round(-r * math.sin(theta) / dy))
            if (ox, oy) == (0, 0) or (ox, oy) in seen:
                continue
            seen.add((ox, oy))
            np.maximum(best, (shifted(z, oy, ox) - z) / math.hypot(ox * dx, oy * dy), out=best)
        acc += np.sin(np.arctan(best))
    return (1.0 - acc / directions).astype(np.float32)


def box_mean(a: np.ndarray, ry: int, rx: int) -> np.ndarray:
    """Mean over the (2 ry + 1) x (2 rx + 1) window, the border repeated (integral image)."""
    p = np.pad(a.astype(np.float64), ((ry, ry), (rx, rx)), mode="edge")
    c = np.zeros((p.shape[0] + 1, p.shape[1] + 1))
    c[1:, 1:] = p.cumsum(0).cumsum(1)
    h, w = a.shape
    ky, kx = 2 * ry + 1, 2 * rx + 1
    s = c[ky:ky + h, kx:kx + w] - c[0:h, kx:kx + w] - c[ky:ky + h, 0:w] + c[0:h, 0:w]
    return (s / (ky * kx)).astype(np.float32)


def curvature(z: np.ndarray, dx: float, dy: float) -> np.ndarray:
    """0.5 + (z - local mean) / (2 CURVATURE_SCALE_M), clamped to [0, 1]; the local mean is two box passes (a tent)."""
    rx, ry = max(1, round(CURVATURE_RADIUS_M / dx)), max(1, round(CURVATURE_RADIUS_M / dy))
    mean = box_mean(box_mean(z, ry, rx), ry, rx)
    return np.clip(0.5 + (z - mean) / (2 * CURVATURE_SCALE_M), 0.0, 1.0).astype(np.float32)


def grow(mask: np.ndarray) -> np.ndarray:
    out = mask.copy()
    out[1:, :] |= mask[:-1, :]; out[:-1, :] |= mask[1:, :]
    out[:, 1:] |= mask[:, :-1]; out[:, :-1] |= mask[:, 1:]
    return out


def shrink(mask: np.ndarray) -> np.ndarray:
    return ~grow(~mask)


def border_connected(candidate: np.ndarray) -> np.ndarray:
    """The pixels of candidate connected (4-neighbour) to the map's border through candidate pixels."""
    reached = np.zeros_like(candidate)
    for edge in (np.s_[0, :], np.s_[-1, :], np.s_[:, 0], np.s_[:, -1]):
        reached[edge] = candidate[edge]
    while True:
        before = reached
        for _ in range(16):                          # 16 growth steps per convergence check
            reached = grow(reached) & candidate
        if np.array_equal(before, reached):
            return reached


def lake_mask(elev: np.ndarray) -> np.ndarray:
    hi, lo = elev.copy(), elev.copy()
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            s = shifted(elev, oy, ox)
            np.maximum(hi, s, out=hi); np.minimum(lo, s, out=lo)
    flat = (elev < LAKE_BELOW_M) & (hi - lo <= LAKE_FLAT_M)
    return grow(grow(shrink(shrink(flat)))) & flat      # an opening: specks of flat rift floor drop out


def water_mask(elev: np.ndarray) -> np.ndarray:
    return border_connected(np.abs(elev) <= SEA_EPS_M) | lake_mask(elev)


def distance_to(target: np.ndarray, dx: float, dy: float, cap: float) -> np.ndarray:
    """Chamfer distance (m, 8 neighbours, each axis its own spacing) to the nearest target pixel, capped at cap."""
    d = np.where(target, 0.0, cap).astype(np.float32)
    diag = math.hypot(dx, dy)
    moves = [(0, 1, dx), (0, -1, dx), (1, 0, dy), (-1, 0, dy), (1, 1, diag), (1, -1, diag), (-1, 1, diag), (-1, -1, diag)]
    while True:
        new = d.copy()
        for oy, ox, step in moves:
            np.minimum(new, shifted(d, oy, ox) + np.float32(step), out=new)
        if np.array_equal(new, d):
            return d
        d = new


def to_texels(a: np.ndarray) -> np.ndarray:
    return (0.25 * (a[:-1, :-1] + a[1:, :-1] + a[:-1, 1:] + a[1:, 1:])).astype(np.float32)


def to_u8(a: np.ndarray) -> np.ndarray:
    return np.clip(np.round(a * 255), 0, 255).astype(np.uint8)


def encode_coast(signed_m: np.ndarray, range_m: float = COAST_RANGE_M) -> np.ndarray:
    return to_u8(0.5 + signed_m / (2 * range_m))


def bake(elev: np.ndarray, meta: dict) -> tuple[np.ndarray, np.ndarray]:
    dx, dy = spacing_m(meta)
    z = elev * np.float32(VERTICAL_EXAGGERATION)
    ao, sky, curv = openness(z, dx, dy, AO_RADIUS_M), openness(z, dx, dy, SKY_RADIUS_M), curvature(z, dx, dy)
    water = water_mask(elev)
    signed = np.where(water, -distance_to(~water, dx, dy, COAST_RANGE_M), distance_to(water, dx, dy, COAST_RANGE_M))
    rgb = np.stack([to_u8(to_texels(ao)), to_u8(to_texels(curv)), to_u8(to_texels(sky))], axis=-1)
    return rgb, encode_coast(to_texels(signed.astype(np.float32)))


def main(map_dir: Path = MAP_DIR, out_dir: Path = OUT_DIR) -> None:
    elev, meta = load_heightmap(map_dir)
    rgb, coast = bake(elev, meta)
    out_dir.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgb, "RGB").save(out_dir / "terrain_bake.png")
    Image.fromarray(coast, "L").save(out_dir / "coast.png")
    print(f"wrote {out_dir / 'terrain_bake.png'} and {out_dir / 'coast.png'} ({coast.shape[1]} x {coast.shape[0]}): "
          f"water {np.mean(coast < 128):.1%} of texels; AO mean {rgb[..., 0].mean() / 255:.3f}, sky-view mean {rgb[..., 2].mean() / 255:.3f}")


if __name__ == "__main__":
    main()
```

- [ ] **Step 3: Generate the textures and import them**

Run:
```bash
time uv run tools/terrain_bake.py
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -logFile "$PWD/unity-import.log"; echo exit=$?
ls -la unity/Assets/Textures/Map/
```
If `test_the_committed_coast_texture_marks_the_seas_and_lakes` later fails only for a lake, the DEM's lake surface is noisier than `LAKE_FLAT_M`: raise it to 1.5 (the tolerance `hexgrid.py` uses for its lake rule), re-run this step and say so in the task report.

Expected: the `wrote ...` line with `water 20.2% of texels; AO mean 0.882, sky-view mean 0.890` (measured on the development Mac against the m3 `8978566` heightmap while this plan was written, in 42 s; the 216 sea and 16 lake hexes of 1,102 are 21%); `exit=0`; `terrain_bake.png`, `coast.png` and their `.meta` files. Open both PNGs once by eye: the coast outline, the Dead Sea and the Sea of Galilee are visible in `coast.png`; the Judean desert's gullies show dark in the red channel of `terrain_bake.png`.

- [ ] **Step 4: Run the tools tests and confirm they pass**

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q | tail -1
```
Expected: **T1 + 10** passed.

- [ ] **Step 5: Document and commit**

In `tools/README.md`, replace

```markdown
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
```

with

```markdown
- `preview_map.py` renders the geography check image `tools/.cache/map-preview.png`.
- `terrain_bake.py` writes `unity/Assets/Textures/Map/terrain_bake.png` (AO, curvature, sky-view) and `coast.png`
  (signed distance to the shoreline) from the committed heightmap (no network). Re-run it whenever the heightmap changes.
```

and update the expected total in "Running the tests" to the Step 4 count.

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add tools/terrain_bake.py tools/tests/test_terrain_bake.py tools/README.md \
  unity/Assets/Textures.meta unity/Assets/Textures/Map.meta \
  unity/Assets/Textures/Map/terrain_bake.png unity/Assets/Textures/Map/terrain_bake.png.meta \
  unity/Assets/Textures/Map/coast.png unity/Assets/Textures/Map/coast.png.meta
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(tools): bake terrain AO, curvature, sky-view and the signed coast distance from the heightmap

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 6: The stylised terrain: a Terrain Lit Shader Graph fed by biome colour, slope rock, height and the bakes

*Track B. Effort 10–16 h (the largest task; it carries controller question 1's gate).* Research §0.4 and §2.2: today 7 one-pixel splat layers draw every terrain pixel twice and give flat colour with relief from N·L alone. This task replaces them with one Terrain Lit Shader Graph whose Custom Function (`StatecraftTerrain.hlsl`) shades the ground from: a biome colour texture baked on the CPU from the existing `BiomeBlender` (a warmer, promo-matched palette), warmer stone with height and a dusty rift floor below −150 m, rock on steep slopes (the 3× relief makes the escarpments steep), two octaves of value noise to break the flat colour, curvature (ridges light, hollows dark) and the baked AO and sky-view, plus an analytic haze that thickens with distance and low in the rift (controller question 3). Water inside the map still shades as the flat, lit sea colour, exactly as `Sea.mat` lights the skirt's sea, so the seam stays invisible until Task 7 changes both together. The terrain also stops falling back to its low-resolution basemap: `Terrain.basemapDistance` defaults to 1 km, and the camera is always farther than that, so today's far terrain is drawn from a baked basemap, not the full shader.

**Files:**
- Create: `unity/Assets/Shaders/StatecraftMap.hlsl`, `unity/Assets/Shaders/StatecraftTerrain.hlsl`, `tools/terrain_graph.py`, `tools/tests/test_terrain_graph.py`, `unity/Assets/Shaders/StatecraftTerrain.shadergraph` (generated), `unity/Assets/Scripts/Map/MapPalette.cs`, `unity/Assets/Scripts/Map/MapShaderGlobals.cs`, `unity/Assets/Editor/MapTextures.cs`, `unity/Assets/Tests/EditMode/TerrainShaderTests.cs`
- Modify: `unity/Assets/Scripts/Map/MapBuilder.cs`, `unity/Assets/Editor/MapMaterials.cs`, `unity/Assets/Materials/Terrain.mat` (by `MapMaterials.Create`), `unity/Assets/Textures/Map/*.png.meta` (by `MapTextures.Configure`), `unity/Assets/Tests/EditMode/MapTests.cs`, `tools/README.md`, `docs/visual-v1.md`

**Interfaces:**
- Consumes: Task 5's `terrain_bake.png` and `coast.png`; `BiomeBlender`, `GeoFrame`, `HeightmapMeta`, `MapRuntimeAssets.Own`, `MapBuilder.BiomeColours` (its keys), `VisualToggles`, `RenderSample`, `ShaderChecks`, `PromoPose`.
- Produces:
  - HLSL (`StatecraftMap.hlsl`): globals `float4 _ScMap` (x width m, y length m, z sea-level world y, w vertical exaggeration), `float4 _ScMapOrigin` (xyz terrain origin, w hex circumradius m), `float4 _ScHaze` (rgb linear land haze colour, a strongest land haze), `float4 _ScHazeWater` (rgb linear water haze colour, a strongest water haze), `float4 _ScHazeRange` (x rift haze top world y, y rift haze full world y, z aerial distance scale m), `TEXTURE2D(_ScBiomeTex)`; functions `ScHash`, `ScNoise`, `ScHazeAmount(float3 positionWS)` (0..1, applied after lighting), `ScHazeWater(float3 colour, float3 positionWS)`, `ScWater(float coast)`, `ScGround(float2 uv, float2 mapM, float3 n, float3 positionWS, float4 bake)`. Task 7 adds `ScWaterShade`, Task 8 `ScHexEdgeDistance`/`ScGridLine`.
  - Custom Function `StatecraftTerrain_float(float2 UV, float3 PositionWS, float3 NormalWS, UnityTexture2D Bake, UnityTexture2D Coast, out float3 BaseColor, out float3 NormalOut, out float Occlusion, out float Smoothness, out float Metallic, out float3 Emission)`.
  - Shader `Statecraft/StatecraftTerrain` with material textures `_ScBakeTex`, `_ScCoastTex`.
  - C#: `MapPalette.Ground` (biome name → sRGB `Color`); `MapShaderGlobals { static readonly int Map, MapOrigin, Biome, Haze, HazeWater, HazeRange; static readonly Color HazeColour, WaterHazeColour; const float HazeMax, WaterHazeMax, HazeDistanceM, RiftHazeTopM, RiftHazeFullM; static void Apply(Terrain, HeightmapMeta, float hexSizeM, Texture2D biome, MapRuntimeAssets assets); }`; `MapTexturePaths { const string Bake, Coast; }`; `MapBuilder.BiomeTextureResolution = 1024`, `MapBuilder.NoBasemapDistanceM = 1e7f`; `MapTextures.Configure()`; toggle `"haze"`.

- [ ] **Step 1: Write the generator's failing tests**

Create `tools/tests/test_terrain_graph.py`:

```python
import json

import pytest

import terrain_graph as tg

SG = "UnityEditor.ShaderGraph."


def obj(t, oid, **kw):
    return {"m_SGVersion": 0, "m_Type": t, "m_ObjectId": oid, **kw}


def slot(kind, oid, sid, name, direction):
    o = obj(SG + kind + "MaterialSlot", oid, m_Id=sid, m_DisplayName=name, m_SlotType=direction, m_Hidden=False,
            m_ShaderOutputName=name, m_StageCapability=3)
    if kind.startswith("Vector"):
        n = int(kind[6]) if kind != "Vector1" else 1
        o["m_Value"] = 0.0 if n == 1 else {c: 0.0 for c in "xyzw"[:n]}
        o["m_DefaultValue"] = o["m_Value"]
    return o


def fixture() -> str:
    """A small stand-in for Terrain Simple.shadergraph: the same object kinds and links, fewer of them."""
    descriptors = ["VertexDescription.Position", "SurfaceDescription.BaseColor", "SurfaceDescription.NormalWS",
                   "SurfaceDescription.Metallic", "SurfaceDescription.Smoothness", "SurfaceDescription.Occlusion",
                   "SurfaceDescription.Emission"]
    objs, blocks = [], []
    for i, d in enumerate(descriptors):
        s = slot("Vector3" if i < 3 or i == 6 else "Vector1", f"bs{i}", i, d, 0)
        b = obj(SG + "BlockNode", f"b{i}", m_Name=d, m_Slots=[{"m_Id": s["m_ObjectId"]}], m_SerializedDescriptor=d)
        objs += [b, s]; blocks.append(b)
    extra_slots = [slot("Vector2", "x1", 0, "In", 0), slot("Vector4", "x2", 1, "Out", 1),
                   slot("Texture2D", "x3", 2, "Tex", 1), slot("Texture2DInput", "x4", 3, "TexIn", 0)]
    add = obj(SG + "AddNode", "add", m_Name="Add", m_Slots=[{"m_Id": s["m_ObjectId"]} for s in extra_slots])
    sticky = obj(SG + "StickyNoteData", "note", m_Title="sample")
    target = obj("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget", "urp", m_ActiveSubTarget={"m_Id": "terrain"},
                 m_CastShadows=False, m_ReceiveShadows=True)
    sub = obj("UnityEditor.Rendering.Universal.ShaderGraph.UniversalTerrainLitSubTarget", "terrain")
    cat = obj(SG + "CategoryData", "cat", m_Name="", m_ChildObjectList=[])
    graph = obj(SG + "GraphData", "graph", m_Properties=[], m_Keywords=[], m_Dropdowns=[], m_CategoryData=[{"m_Id": "cat"}],
                m_Nodes=[{"m_Id": b["m_ObjectId"]} for b in blocks] + [{"m_Id": "add"}], m_GroupDatas=[],
                m_StickyNoteDatas=[{"m_Id": "note"}],
                m_Edges=[{"m_OutputSlot": {"m_Node": {"m_Id": "add"}, "m_SlotId": 1}, "m_InputSlot": {"m_Node": {"m_Id": "b1"}, "m_SlotId": 1}}],
                m_VertexContext={"m_Position": {"x": 0, "y": 0}, "m_Blocks": [{"m_Id": "b0"}]},
                m_FragmentContext={"m_Position": {"x": 0, "y": 100}, "m_Blocks": [{"m_Id": f"b{i}"} for i in range(1, 7)]},
                m_Path="Terrain", m_ActiveTargets=[{"m_Id": "urp"}])
    return "\n\n".join(json.dumps(o, indent=4) for o in [graph, cat, target, sub, add, *extra_slots, sticky, *objs]) + "\n"


GUID = "0123456789abcdef0123456789abcdef"


def built():
    objs = tg.split(tg.build(fixture(), GUID))
    return objs, {o["m_ObjectId"]: o for o in objs}


def test_sample_nodes_and_notes_go_and_the_blocks_stay():
    objs, by_id = built()
    graph = objs[0]
    kinds = [tg.kind(by_id[n["m_Id"]]) for n in graph["m_Nodes"]]
    assert kinds.count("BlockNode") == 7 and "AddNode" not in kinds
    assert sorted(set(kinds) - {"BlockNode"}) == ["CustomFunctionNode", "NormalVectorNode", "PositionNode", "PropertyNode", "UVNode"]
    assert graph["m_StickyNoteDatas"] == [] and "note" not in by_id and "add" not in by_id
    assert graph["m_Path"] == "Statecraft"


def test_every_fragment_block_is_fed_by_the_custom_function():
    objs, by_id = built()
    graph = objs[0]
    cf = next(o for o in objs if tg.kind(o) == "CustomFunctionNode")
    fed = {e["m_InputSlot"]["m_Node"]["m_Id"]: e for e in graph["m_Edges"] if e["m_OutputSlot"]["m_Node"]["m_Id"] == cf["m_ObjectId"]}
    for sid, _, _, descriptor in tg.OUTPUTS:
        block = next(o for o in objs if o.get("m_SerializedDescriptor") == descriptor)
        e = fed[block["m_ObjectId"]]
        assert e["m_OutputSlot"]["m_SlotId"] == sid
        assert e["m_InputSlot"]["m_SlotId"] == by_id[block["m_Slots"][0]["m_Id"]]["m_Id"]


def test_the_function_is_the_hlsl_file_by_guid_and_name_with_single_precision():
    objs, _ = built()
    cf = next(o for o in objs if tg.kind(o) == "CustomFunctionNode")
    # m_SourceType 0 is File (Shader Graph's HlslSourceType is { File, String }); String with an empty body would compile
    # to zero outputs without an error (review C1).
    assert (cf["m_SourceType"], cf["m_FunctionName"], cf["m_FunctionSource"], cf["m_Precision"]) == (0, "StatecraftTerrain", GUID, 1)
    assert tg.FILE_MODE == 0
    assert next(o for o in objs if tg.kind(o) == "PositionNode")["m_Precision"] == 1


def test_the_textures_are_blackboard_properties_with_the_shaders_names():
    objs, by_id = built()
    graph = objs[0]
    props = [by_id[p["m_Id"]] for p in graph["m_Properties"]]
    assert [p["m_DefaultReferenceName"] for p in props] == ["_ScBakeTex", "_ScCoastTex"]
    cat = next(o for o in objs if tg.kind(o) == "CategoryData")
    assert [c["m_Id"] for c in cat["m_ChildObjectList"]] == [p["m_ObjectId"] for p in props]
    assert next(o for o in objs if tg.kind(o) == "UniversalTarget")["m_CastShadows"] is True


def test_every_edge_is_wired_to_existing_slots_and_the_output_is_deterministic():
    text = tg.build(fixture(), GUID)
    assert text == tg.build(fixture(), GUID)
    objs = tg.split(text)
    by_id = {o["m_ObjectId"]: o for o in objs}
    assert len(by_id) == len(objs), "object ids are unique"
    for e in objs[0]["m_Edges"]:
        for end in ("m_OutputSlot", "m_InputSlot"):
            node = by_id[e[end]["m_Node"]["m_Id"]]
            assert e[end]["m_SlotId"] in [by_id[s["m_Id"]]["m_Id"] for s in node["m_Slots"]]
    with pytest.raises(ValueError, match="slot prototypes"):
        tg.build(fixture().replace("Texture2DInputMaterialSlot", "SomethingElse"), GUID)
```

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests/test_terrain_graph.py -q 2>&1 | tail -2
```
Expected: FAIL, `ModuleNotFoundError: No module named 'terrain_graph'`.

- [ ] **Step 2: Implement the generator**

Create `tools/terrain_graph.py`:

```python
# /// script
# requires-python = ">=3.11"
# dependencies = []
# ///
"""Writes unity/Assets/Shaders/StatecraftTerrain.shadergraph (shader "Statecraft/StatecraftTerrain"): Unity's own
"Terrain Simple" Terrain Lit template with its sample nodes, groups and notes removed, and one Custom Function node
(File mode, m_SourceType 0: StatecraftTerrain.hlsl, function StatecraftTerrain, single precision) that takes the terrain UV, the absolute
world position and normal and two blackboard textures (_ScBakeTex, _ScCoastTex) and feeds every fragment block. All
shading lives in the HLSL file; re-run this only when the node's inputs or outputs change. Slot objects are copied from
the template's own slots, so they match the installed Shader Graph version. Object ids are UUID5s: the output is
deterministic.

Usage: uv run tools/terrain_graph.py
Needs unity/Library/PackageCache (open the project in Unity once) and unity/Assets/Shaders/StatecraftTerrain.hlsl.meta."""
import json
import re
import sys
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TEMPLATE_GLOB = "unity/Library/PackageCache/com.unity.shadergraph@*/GraphTemplates/Cross Pipeline/Terrain Simple.shadergraph"
HLSL_META = ROOT / "unity" / "Assets" / "Shaders" / "StatecraftTerrain.hlsl.meta"
OUT = ROOT / "unity" / "Assets" / "Shaders" / "StatecraftTerrain.shadergraph"
GRAPH_PATH = "Statecraft"
FUNCTION = "StatecraftTerrain"
NS = uuid.UUID("5b0c1f7e-2f61-4c3a-9a57-7a1d1e0c6f42")
SG = "UnityEditor.ShaderGraph."
SINGLE = 1      # Precision.Single
FILE_MODE = 0   # HlslSourceType.File (Shader Graph: enum HlslSourceType { File, String })
TEXTURES = [("ScBakeTex", "_ScBakeTex", 3), ("ScCoastTex", "_ScCoastTex", 4)]   # display name, reference, function slot
INPUTS = [(0, "UV", "Vector2"), (1, "PositionWS", "Vector3"), (2, "NormalWS", "Vector3"),
          (3, "Bake", "Texture2DInput"), (4, "Coast", "Texture2DInput")]
OUTPUTS = [(5, "BaseColor", "Vector3", "SurfaceDescription.BaseColor"), (6, "NormalOut", "Vector3", "SurfaceDescription.NormalWS"),
           (7, "Occlusion", "Vector1", "SurfaceDescription.Occlusion"), (8, "Smoothness", "Vector1", "SurfaceDescription.Smoothness"),
           (9, "Metallic", "Vector1", "SurfaceDescription.Metallic"), (10, "Emission", "Vector3", "SurfaceDescription.Emission")]
SLOT_KINDS = {"Vector1MaterialSlot", "Vector2MaterialSlot", "Vector3MaterialSlot", "Vector4MaterialSlot",
              "Texture2DMaterialSlot", "Texture2DInputMaterialSlot"}


def oid(name: str) -> str:
    return uuid.uuid5(NS, name).hex


def split(text: str) -> list[dict]:
    return [json.loads(o) for o in re.split(r"\n\n(?=\{)", text.strip())]


def join(objs: list[dict]) -> str:
    return "\n\n".join(json.dumps(o, indent=4) for o in objs) + "\n"


def kind(o: dict) -> str:
    return o["m_Type"].rsplit(".", 1)[-1]


def make_slot(protos: dict, k: str, sid: int, name: str, direction: int, owner: str) -> dict:
    o = json.loads(json.dumps(protos[k + "MaterialSlot"]))
    o.update({"m_ObjectId": oid(f"slot:{owner}:{sid}"), "m_Id": sid, "m_DisplayName": name, "m_SlotType": direction,
              "m_ShaderOutputName": name, "m_Hidden": False})
    for key in ("m_Value", "m_DefaultValue"):
        if isinstance(o.get(key), dict):
            o[key] = {c: 0.0 for c in o[key]}
        elif isinstance(o.get(key), (int, float)):
            o[key] = 0.0
    return o


def make_node(type_name: str, name: str, slots: list[dict], extra: dict, x: float, y: float, version: int = 0, precision: int = 0) -> dict:
    return {"m_SGVersion": version, "m_Type": SG + type_name, "m_ObjectId": oid("node:" + name), "m_Group": {"m_Id": ""},
            "m_Name": name, "m_DrawState": {"m_Expanded": True, "m_Position": {"serializedVersion": "2", "x": x, "y": y,
                                                                                  "width": 220.0, "height": 130.0}},
            "m_Slots": [{"m_Id": s["m_ObjectId"]} for s in slots], "synonyms": [], "m_Precision": precision,
            "m_PreviewExpanded": False, "m_DismissedVersion": 0, "m_PreviewMode": 0,
            "m_CustomColors": {"m_SerializableColors": []}, **extra}


def edge(a: dict, a_slot: int, b: dict, b_slot: int) -> dict:
    return {"m_OutputSlot": {"m_Node": {"m_Id": a["m_ObjectId"]}, "m_SlotId": a_slot},
            "m_InputSlot": {"m_Node": {"m_Id": b["m_ObjectId"]}, "m_SlotId": b_slot}}


def build(template_text: str, hlsl_guid: str) -> str:
    objs = split(template_text)
    by_id = {o["m_ObjectId"]: o for o in objs}
    graph = objs[0]
    if kind(graph) != "GraphData":
        raise ValueError("the template's first object is not its GraphData")
    protos: dict[str, dict] = {}
    for o in objs:
        if kind(o) in SLOT_KINDS:
            protos.setdefault(kind(o), o)
    if SLOT_KINDS - protos.keys():
        raise ValueError(f"the template lacks slot prototypes: {sorted(SLOT_KINDS - protos.keys())}")
    block_ids = [b["m_Id"] for ctx in ("m_VertexContext", "m_FragmentContext") for b in graph[ctx]["m_Blocks"]]
    blocks = {by_id[i]["m_SerializedDescriptor"]: by_id[i] for i in block_ids}

    keep = [graph] + [o for o in objs[1:] if ".Rendering." in o["m_Type"] or kind(o) == "CategoryData"]
    for i in block_ids:
        keep.append(by_id[i])
        keep += [by_id[s["m_Id"]] for s in by_id[i]["m_Slots"]]
    for o in keep:
        if kind(o) == "UniversalTarget":
            o["m_CastShadows"] = True       # the relief casts the map-scale shadows (plan Task 2)
            o["m_ReceiveShadows"] = True

    uv_out = make_slot(protos, "Vector4", 0, "Out", 1, "uv")
    uv = make_node("UVNode", "UV", [uv_out], {"m_OutputChannel": 0}, -900, 0)
    pos_out = make_slot(protos, "Vector3", 0, "Out", 1, "position")
    pos = make_node("PositionNode", "Position", [pos_out], {"m_Space": 4, "m_PositionSource": 0}, -900, 150, version=1, precision=SINGLE)
    nrm_out = make_slot(protos, "Vector3", 0, "Out", 1, "normal")
    nrm = make_node("NormalVectorNode", "Normal Vector", [nrm_out], {"m_Space": 2}, -900, 300, precision=SINGLE)
    added = [uv, uv_out, pos, pos_out, nrm, nrm_out]
    props, prop_nodes = [], []
    for display, ref, _ in TEXTURES:
        p = {"m_SGVersion": 0, "m_Type": SG + "Internal.Texture2DShaderProperty", "m_ObjectId": oid("prop:" + ref),
             "m_Guid": {"m_GuidSerialized": str(uuid.uuid5(NS, "guid:" + ref))}, "m_Name": display,
             "m_DefaultRefNameVersion": 1, "m_RefNameGeneratedByDisplayName": display, "m_DefaultReferenceName": ref,
             "m_OverrideReferenceName": "", "m_GeneratePropertyBlock": True, "m_UseCustomSlotLabel": False,
             "m_CustomSlotLabel": "", "m_DismissedVersion": 0, "m_Precision": 0, "overrideHLSLDeclaration": False,
             "hlslDeclarationOverride": 0, "m_Hidden": False, "m_PerRendererData": False, "m_customAttributes": [],
             "m_Value": {"m_SerializedTexture": "{\"texture\":{\"instanceID\":0}}", "m_Guid": ""},
             "isMainTexture": False, "useTilingAndOffset": False, "useTexelSize": False, "m_Modifiable": True, "m_DefaultType": 0}
        out = make_slot(protos, "Texture2D", 0, "Out", 1, "prop:" + ref)
        pn = make_node("PropertyNode", display, [out], {"m_Property": {"m_Id": p["m_ObjectId"]}}, -900, 450 + 120 * len(props))
        props.append(p)
        prop_nodes.append(pn)
        added += [p, pn, out]
    cf_slots = [make_slot(protos, k, sid, name, 0, "cf") for sid, name, k in INPUTS] + \
               [make_slot(protos, k, sid, name, 1, "cf") for sid, name, k, _ in OUTPUTS]
    cf = make_node("CustomFunctionNode", "StatecraftTerrain (Custom Function)", cf_slots,
                   {"m_SourceType": FILE_MODE, "m_FunctionName": FUNCTION, "m_FunctionSource": hlsl_guid,
                    "m_FunctionSourceUsePragmas": True, "m_FunctionBody": ""}, -450, 100, version=1, precision=SINGLE)
    added += [cf, *cf_slots]

    edges = [edge(uv, 0, cf, 0), edge(pos, 0, cf, 1), edge(nrm, 0, cf, 2)]
    edges += [edge(pn, 0, cf, sid) for pn, (_, _, sid) in zip(prop_nodes, TEXTURES)]
    for sid, _, _, descriptor in OUTPUTS:
        if descriptor not in blocks:
            raise ValueError(f"the template has no {descriptor} block")
        block = blocks[descriptor]
        edges.append(edge(cf, sid, block, by_id[block["m_Slots"][0]["m_Id"]]["m_Id"]))

    graph.update(m_Path=GRAPH_PATH, m_Properties=[{"m_Id": p["m_ObjectId"]} for p in props], m_Keywords=[], m_Dropdowns=[],
                 m_GroupDatas=[], m_StickyNoteDatas=[], m_Edges=edges,
                 m_Nodes=[{"m_Id": i} for i in block_ids] + [{"m_Id": n["m_ObjectId"]} for n in (uv, pos, nrm, *prop_nodes, cf)])
    for o in keep:
        if kind(o) == "CategoryData":
            o["m_ChildObjectList"] = [{"m_Id": p["m_ObjectId"]} for p in props]
    return join(keep + added)


def main() -> int:
    templates = sorted(ROOT.glob(TEMPLATE_GLOB))
    if not templates:
        print(f"no template at {TEMPLATE_GLOB}: open the project in Unity once so the package cache exists")
        return 1
    m = re.search(r"^guid: ([0-9a-f]{32})$", HLSL_META.read_text(), re.M) if HLSL_META.exists() else None
    if m is None:
        print(f"{HLSL_META} missing: import StatecraftTerrain.hlsl in Unity first")
        return 1
    OUT.write_text(build(templates[-1].read_text(), m.group(1)))
    print(f"wrote {OUT} from {templates[-1].relative_to(ROOT)} (function {FUNCTION} in {m.group(1)})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
```

Run:
```bash
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests/test_terrain_graph.py -q 2>&1 | tail -2
```
Expected: `5 passed`.

- [ ] **Step 3: Write the Unity failing tests**

Create `unity/Assets/Tests/EditMode/TerrainShaderTests.cs`:

```csharp
#nullable enable
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 6: the Statecraft terrain graph, its baked inputs and what they do on screen.</summary>
public class TerrainShaderTests
{
    const string TerrainMatPath = "Assets/Materials/Terrain.mat";

    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    /// <summary>Review C1, the positive half of the Task 6 gate: the generated passes include StatecraftTerrain.hlsl (the Custom
    /// Function is in File mode), and the HLSL really paints the terrain: with a magenta biome texture a flat coastal-plain
    /// pixel comes out magenta, not black or the sea colour.</summary>
    [UnityTest]
    public IEnumerator TheCustomFunctionDrivesTheTerrain()
    {
        var shader = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath).shader;
        var data = ShaderUtil.GetShaderData(shader);
        int including = 0;
        for (int s = 0; s < data.SubshaderCount; s++)
        {
            var sub = data.GetSerializedSubshader(s);
            for (int p = 0; p < sub.PassCount; p++) if (sub.GetPass(p).SourceCode.Contains("StatecraftTerrain.hlsl")) including++;
        }
        Assert.Greater(including, 0, "no generated pass includes StatecraftTerrain.hlsl: the Custom Function is not in File mode");
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        // A flat coastal-plain point (review N7): no rock blend and no stone tint can stand between the texture and the screen.
        var plain = map.Frame.ToWorld(34.90, 31.95, 0); plain.y = map.Terrain.SampleHeight(plain);
        rig.LookAt = plain; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var magenta = new Texture2D(1, 1, TextureFormat.RGBA32, false); magenta.SetPixel(0, 0, Color.magenta); magenta.Apply();
        var real = Shader.GetGlobalTexture(MapShaderGlobals.Biome);
        try
        {
            Shader.SetGlobalTexture(MapShaderGlobals.Biome, magenta);
            var px = new Vector2[1];
            var s = RenderSample.Render(cam, 480, 270, new[] { plain }, px);
            var c = s.Mean((int)px[0].x, (int)px[0].y, 2);
            TestContext.WriteLine($"land with a magenta biome texture: {c}");
            Assert.Greater(c.r + c.g + c.b, 0.15f, "not black");
            Assert.Greater(Mathf.Min(c.r, c.b), c.g + 0.1f, "magenta: the biome texture reaches the screen through the HLSL");
        }
        finally { Shader.SetGlobalTexture(MapShaderGlobals.Biome, real); Object.DestroyImmediate(magenta); }
    }

    [Test]
    public void TheTerrainUsesTheStatecraftGraphWithItsBakedTextures()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        Assert.AreEqual("Statecraft/StatecraftTerrain", mat.shader.name);
        Assert.IsFalse(ShaderUtil.ShaderHasError(mat.shader), string.Join("\n", ShaderUtil.GetShaderMessages(mat.shader).Select(m => m.message)));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Bake), mat.GetTexture("_ScBakeTex"));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast), mat.GetTexture("_ScCoastTex"));
        CollectionAssert.AreEquivalent(MapBuilder.BiomeColours.Keys, MapPalette.Ground.Keys, "a ground colour for every biome");
    }

    [Test]
    public void TheTerrainShaderCompilesForWebGL2() =>
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath).shader));

    [Test]
    public void TheBakedTexturesImportAsLinearClampedData()
    {
        var bake = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Bake);
        var coast = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast);
        foreach (var t in new[] { bake, coast })
        {
            Assert.AreEqual(2048, t.width, t.name); Assert.AreEqual(2048, t.height, t.name);
            Assert.IsFalse(t.isDataSRGB, t.name + " is data, not colour");
            Assert.AreEqual(TextureWrapMode.Clamp, t.wrapMode, t.name);
            Assert.Greater(t.mipmapCount, 1, t.name);
        }
        Assert.AreEqual(TextureFormat.R8, coast.format, "one uncompressed channel: compression blocks would show in the foam band");
    }

    [Test]
    public void TheTerrainDrawsInOnePassAndNeverFallsBackToItsBasemap()
    {
        var terrain = OpenMap().Terrain;
        Assert.AreEqual(1, terrain.terrainData.terrainLayers.Length, "one layer: one pass (7 layers drew every pixel twice)");
        Assert.GreaterOrEqual(terrain.basemapDistance, 1e6f, "the camera is always farther than the 1 km default");
    }

    [Test]
    public void TheBiomeTextureIsOwnedByTheMapAndSetForTheShaders()
    {
        var map = OpenMap();
        var biome = Shader.GetGlobalTexture(MapShaderGlobals.Biome);
        Assert.IsNotNull(biome); Assert.AreEqual(MapBuilder.BiomeTextureResolution, biome.width);
        var m = Shader.GetGlobalVector(MapShaderGlobals.Map);
        Assert.AreEqual(map.Terrain.terrainData.size.x, m.x, 1f); Assert.AreEqual(map.Terrain.terrainData.size.z, m.y, 1f);
        Assert.AreEqual(map.Hexes.hex_size_km * 1000f, Shader.GetGlobalVector(MapShaderGlobals.MapOrigin).w, 1e-3f);
        CollectionAssert.Contains(VisualToggles.Names, "haze");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Assert.IsTrue(biome == null, "destroyed with the map");
    }

    /// <summary>Rendered by Tel Aviv: 10 km offshore is the sea colour, 10 km inland the ground colour.</summary>
    [UnityTest]
    public IEnumerator TheCoastSeparatesLandFromSea()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = map.Frame.ToWorld(34.78, 32.08, 0); rig.Distance = 60000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var sea = map.Frame.ToWorld(34.66, 32.08, 0); var land = map.Frame.ToWorld(34.89, 32.08, 0);
        sea.y = map.Terrain.SampleHeight(sea); land.y = map.Terrain.SampleHeight(land);
        var px = new Vector2[2];
        var s = RenderSample.Render(cam, 960, 540, new[] { sea, land }, px);
        Color a = s.Mean((int)px[0].x, (int)px[0].y, 3), b = s.Mean((int)px[1].x, (int)px[1].y, 3);
        TestContext.WriteLine($"sea {a}, land {b}");
        Assert.Greater(a.b, a.r + 0.15f, "sea"); Assert.Greater(b.r, b.b + 0.05f, "land");
    }

    /// <summary>Rendered over the Judean hills: the bakes (AO, curvature, sky-view) raise the relief's contrast by at least
    /// 8% against a neutral bake (AO 1, curvature 0.5, sky 1). Terrain.mat is changed in memory only and restored.</summary>
    [UnityTest]
    public IEnumerator TheBakesCarveTheRelief()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = map.Frame.ToWorld(35.25, 31.70, 0); rig.Distance = 45000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        var real = mat.GetTexture("_ScBakeTex");
        var neutral = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        neutral.SetPixel(0, 0, new Color(1f, 0.5f, 1f, 1f)); neutral.Apply();
        try
        {
            var carved = RenderSample.Render(cam, 480, 270);
            mat.SetTexture("_ScBakeTex", neutral);
            var plain = RenderSample.Render(cam, 480, 270);
            TestContext.WriteLine($"luma std {plain.LumaStd():0.0000} -> {carved.LumaStd():0.0000}");
            Assert.Greater(carved.LumaStd(), plain.LumaStd() * 1.08f);
        }
        finally { mat.SetTexture("_ScBakeTex", real); Object.DestroyImmediate(neutral); }
    }

    /// <summary>Rendered at the country-wide pose: the haze moves the far (top) band more than the near (bottom) band.</summary>
    [UnityTest]
    public IEnumerator TheHazeThickensWithDistance()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        VisualToggles.Set("haze", false); var clear = RenderSample.Render(cam, 480, 270);
        VisualToggles.Set("haze", true); var hazy = RenderSample.Render(cam, 480, 270);
        float far = Mathf.Abs(hazy.MeanLuma(0f, 0.8f, 1f, 0.95f) - clear.MeanLuma(0f, 0.8f, 1f, 0.95f));
        float near = Mathf.Abs(hazy.MeanLuma(0f, 0.05f, 1f, 0.2f) - clear.MeanLuma(0f, 0.05f, 1f, 0.2f));
        TestContext.WriteLine($"haze shift far {far:0.000}, near {near:0.000}");
        Assert.Greater(far, near + 0.005f);
    }
}
```

In `unity/Assets/Tests/EditMode/MapTests.cs`:
1. Replace `        Assert.AreEqual("Universal Render Pipeline/Terrain/Lit", terrainMat.shader.name);` with `        Assert.AreEqual("Statecraft/StatecraftTerrain", terrainMat.shader.name);`.
2. Replace `            Assert.AreEqual(MapBuilder.AlphamapResolution, terrain.terrainData.alphamapResolution);` with `            Assert.AreEqual(1, terrain.terrainData.terrainLayers.Length);`.
3. In `BiomeLayersAreMatte`, delete the comment line `            // URP Terrain/Lit has no specular-highlight or environment-reflection toggle; its per-layer defaults are zeroed.` and the `for (int i = 0; i < 4; i++) { ... }` block after it (the graph has no `_SmoothnessN` properties; the one layer stays matte and alpha-free, which the rest of the test still checks).

- [ ] **Step 4: Run them and confirm they fail**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -m2 "error CS" unity-tests.log
```
Expected: `exit=1`; `error CS0103: The name 'MapTexturePaths' does not exist in the current context` (and `MapPalette`, `MapShaderGlobals`).

- [ ] **Step 5: Implement the HLSL**

Create `unity/Assets/Shaders/StatecraftMap.hlsl`:

```hlsl
#ifndef STATECRAFT_MAP_INCLUDED
#define STATECRAFT_MAP_INCLUDED
// Shared by the terrain graph's Custom Function (StatecraftTerrain.hlsl) and the sea (Sea.shader). The globals are set by
// MapShaderGlobals.Apply (C#) whenever the map is built. Colours are linear; SRGBToLinear converts the authored sRGB values.

float4 _ScMap;        // x width (m, east), y length (m, north), z sea level (world y), w vertical exaggeration
float4 _ScMapOrigin;  // xyz terrain origin (world), w hex circumradius (m)
float4 _ScHaze;       // rgb land haze colour (linear), a strongest land haze (0..1)
float4 _ScHazeWater;  // rgb water haze colour (linear, cooler), a strongest water haze (weaker: the sea keeps its blue)
float4 _ScHazeRange;  // x rift haze begins (world y), y rift haze is full (world y), z aerial distance scale (m)
TEXTURE2D(_ScBiomeTex); SAMPLER(sampler_ScBiomeTex);

float ScHash(float2 p)
{
    p = frac(p * float2(0.1031, 0.1030));
    p += dot(p, p.yx + 33.33);
    return frac((p.x + p.y) * p.x);
}

float ScNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(ScHash(i), ScHash(i + float2(1, 0)), u.x), lerp(ScHash(i + float2(0, 1)), ScHash(i + float2(1, 1)), u.x), u.y);
}

// Aerial perspective, 0..1 before each surface's strength: grows with distance (1 - exp(-d / scale)) and up to 2.5x low in
// the rift. It is applied after lighting on both surfaces (review I2): land outputs BaseColor x (1 - h) plus Emission =
// haze colour x h, so Lit's result is lerp(lit ground, haze, h); water lerps its final colour (ScHazeWater).
float ScHazeAmount(float3 positionWS)
{
    float dist = length(_WorldSpaceCameraPos - positionWS);
    float aerial = 1.0 - exp(-dist / max(_ScHazeRange.z, 1.0));
    float low = saturate((_ScHazeRange.x - positionWS.y) / max(_ScHazeRange.x - _ScHazeRange.y, 1.0));
    return saturate(aerial * (1.0 + 1.5 * low));
}

float3 ScHazeWater(float3 colour, float3 positionWS)
{
    return lerp(colour, _ScHazeWater.rgb, _ScHazeWater.a * ScHazeAmount(positionWS));
}

// 1 in water, 0 on land; coast is the signed distance texture (0.5 at the shore, tools/terrain_bake.py).
float ScWater(float coast) { return 1.0 - smoothstep(0.497, 0.503, coast); }

// The painted ground: biome colour, warmer stone high up, a dusty rift floor, rock on steep slopes, two octaves of
// breakup, curvature (ridges light, hollows dark) and the baked occlusion (bake: r AO, g curvature, b sky-view).
float3 ScGround(float2 uv, float2 mapM, float3 n, float3 positionWS, float4 bake)
{
    float elevM = (positionWS.y - _ScMap.z) / _ScMap.w;            // real metres above sea level
    float slope = 1.0 - saturate(n.y);                             // 0 flat, 1 vertical
    float3 g = SAMPLE_TEXTURE2D(_ScBiomeTex, sampler_ScBiomeTex, uv).rgb;
    g = lerp(g, g * float3(1.08, 1.00, 0.86), smoothstep(500.0, 1100.0, elevM));
    g = lerp(g, g * float3(1.10, 1.04, 0.92), 1.0 - smoothstep(-350.0, -150.0, elevM));
    float rock = smoothstep(0.22, 0.45, slope + (ScNoise(mapM / 900.0) - 0.5) * 0.12);
    g = lerp(g, SRGBToLinear(float3(0.56, 0.49, 0.41)), rock);
    g *= 0.93 + 0.14 * (ScNoise(mapM / 2600.0) * 0.6 + ScNoise(mapM / 520.0) * 0.4);
    g *= 1.0 + 0.30 * (bake.g - 0.5) * 2.0;
    g *= lerp(1.0, bake.r * lerp(0.6, 1.0, bake.b), 0.45);
    return g;
}

#endif
```

Create `unity/Assets/Shaders/StatecraftTerrain.hlsl`:

```hlsl
#ifndef STATECRAFT_TERRAIN_INCLUDED
#define STATECRAFT_TERRAIN_INCLUDED
#include "Assets/Shaders/StatecraftMap.hlsl"

// The Custom Function of StatecraftTerrain.shadergraph (written by tools/terrain_graph.py; keep this signature in step with
// its INPUTS and OUTPUTS). UV is the terrain UV (0..1 over the map); PositionWS and NormalWS are absolute world space;
// Bake is terrain_bake.png (r AO, g curvature, b sky-view); Coast is coast.png (0.5 = shore, lower = water).
void StatecraftTerrain_float(float2 UV, float3 PositionWS, float3 NormalWS, UnityTexture2D Bake, UnityTexture2D Coast,
    out float3 BaseColor, out float3 NormalOut, out float Occlusion, out float Smoothness, out float Metallic, out float3 Emission)
{
    float3 n = normalize(NormalWS);
    float4 bake = SAMPLE_TEXTURE2D(Bake.tex, Bake.samplerstate, UV);
    float coast = SAMPLE_TEXTURE2D(Coast.tex, Coast.samplerstate, UV).r;
    float2 mapM = UV * _ScMap.xy;
    float3 ground = ScGround(UV, mapM, n, PositionWS, bake);
    float h = _ScHaze.a * ScHazeAmount(PositionWS);                 // land haze, applied after lighting via Emission
    float ao = bake.r * lerp(0.6, 1.0, bake.b);
    float water = ScWater(coast);
    // Water: the flat sea colour, lit like the skirt's URP Lit Sea.mat so the map's edge shows no seam (Task 7 replaces
    // both sides with ScWaterShade together). No haze on water yet: the skirt's URP Lit sea has none either.
    BaseColor = lerp(ground * (1.0 - h), SRGBToLinear(float3(0.16, 0.42, 0.55)), water);
    NormalOut = normalize(lerp(n, float3(0, 1, 0), water));
    Occlusion = lerp(lerp(1.0, ao, 0.8), 1.0, water);
    Smoothness = 0.0;
    Metallic = 0.0;
    Emission = _ScHaze.rgb * h * (1.0 - water);
}

#endif
```

Run Unity once so it imports the two files and writes `StatecraftTerrain.hlsl.meta`, then generate and import the graph:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -logFile "$PWD/unity-import.log"; echo exit=$?
uv run tools/terrain_graph.py
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -logFile "$PWD/unity-import-graph.log"; echo exit=$?
grep -niE "StatecraftTerrain.*(error|exception)|(error|exception).*StatecraftTerrain" unity-import-graph.log
```
Expected: `exit=0` twice; `wrote unity/Assets/Shaders/StatecraftTerrain.shadergraph from unity/Library/PackageCache/com.unity.shadergraph@.../Terrain Simple.shadergraph (function StatecraftTerrain in <guid>)`; the grep prints nothing.

**Gate, negative half (controller question 1):** if the grep prints anything, or Unity rewrites the graph into something with no Custom Function node (`grep -c CustomFunctionNode unity/Assets/Shaders/StatecraftTerrain.shadergraph` prints 0 after the import), or the node is not in File mode (`grep -c '"m_SourceType": 0' unity/Assets/Shaders/StatecraftTerrain.shadergraph` prints 0), stop: report BLOCKED with the matching log lines and the graph's first 200 lines. Do not hand-edit the graph. The positive half (the HLSL really reaches the screen) runs first in Step 7, once the material and the globals exist.

- [ ] **Step 6: Implement the C# side**

Create `unity/Assets/Scripts/Map/MapPalette.cs`:

```csharp
#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The ground colours the terrain shader paints per biome (sRGB; MapBuilder bakes them, blended between hexes,
    /// into the biome texture). Warmer and more saturated than MapBuilder.BiomeColours, which the minimap keeps: dry-grass
    /// gold on the coastal plain, olive-ochre hills, sand-amber desert (research notes §1, promo 03). "sea" and "lake" are
    /// only seen where a lake or sea hex's blend reaches the shore; the water itself is shaded from the coast texture.</summary>
    public static class MapPalette
    {
        public static IReadOnlyDictionary<string, Color> Ground { get; } = new Dictionary<string, Color>
        {
            ["sea"] = new Color(0.62f, 0.60f, 0.42f), ["lake"] = new Color(0.60f, 0.58f, 0.42f),
            ["coastal_plain"] = new Color(0.64f, 0.62f, 0.36f), ["hills"] = new Color(0.66f, 0.58f, 0.38f),
            ["mountains"] = new Color(0.62f, 0.53f, 0.40f), ["valley"] = new Color(0.58f, 0.58f, 0.37f),
            ["desert"] = new Color(0.84f, 0.70f, 0.48f),
        };
    }
}
```

Create `unity/Assets/Scripts/Map/MapShaderGlobals.cs`:

```csharp
#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The global shader values StatecraftMap.hlsl reads, set when the map is built (MapBuilder.Build): the map's
    /// size, sea level and exaggeration, its origin and hex size, the biome colour texture and the haze. Globals, not
    /// material values, so the shared editor-made materials are never changed or instanced at run time.</summary>
    public static class MapShaderGlobals
    {
        public static readonly int Map = Shader.PropertyToID("_ScMap"), MapOrigin = Shader.PropertyToID("_ScMapOrigin"),
            Biome = Shader.PropertyToID("_ScBiomeTex"), Haze = Shader.PropertyToID("_ScHaze"), HazeWater = Shader.PropertyToID("_ScHazeWater"),
            HazeRange = Shader.PropertyToID("_ScHazeRange");

        /// <summary>A warm, dusty haze over land (sRGB).</summary>
        public static readonly Color HazeColour = new Color(0.80f, 0.76f, 0.68f);
        public const float HazeMax = 0.6f;
        /// <summary>A cooler, weaker haze over water (review I2), so the sea stays the promo's saturated blue towards the
        /// horizon: 0.4 of the land's strength.</summary>
        public static readonly Color WaterHazeColour = new Color(0.62f, 0.74f, 0.86f);
        public const float WaterHazeMax = HazeMax * 0.4f;
        /// <summary>Distance scale of the aerial haze: 1 - exp(-d / scale); at the gameplay pose's far edge (about 85 km) 19%.</summary>
        public const float HazeDistanceM = 400000f;
        /// <summary>Real elevations between which the haze thickens, low in the Jordan rift.</summary>
        public const float RiftHazeTopM = -100f, RiftHazeFullM = -400f;

        static Vector4 haze, hazeWater;

        public static void Apply(Terrain terrain, HeightmapMeta meta, float hexSizeM, Texture2D biome, MapRuntimeAssets assets)
        {
            var size = terrain.terrainData.size; var o = terrain.transform.position;
            float Y(float elevM) => o.y + (elevM - meta.elev_min_m) * MapBuilder.VerticalExaggeration;
            Shader.SetGlobalVector(Map, new Vector4(size.x, size.z, Y(0f), MapBuilder.VerticalExaggeration));
            Shader.SetGlobalVector(MapOrigin, new Vector4(o.x, o.y, o.z, hexSizeM));
            Shader.SetGlobalTexture(Biome, biome);
            var c = HazeColour.linear;
            haze = new Vector4(c.r, c.g, c.b, HazeMax);
            var w = WaterHazeColour.linear;
            hazeWater = new Vector4(w.r, w.g, w.b, WaterHazeMax);
            Shader.SetGlobalVector(Haze, haze);
            Shader.SetGlobalVector(HazeWater, hazeWater);
            Shader.SetGlobalVector(HazeRange, new Vector4(Y(RiftHazeTopM), Y(RiftHazeFullM), HazeDistanceM, 0f));
            assets.OnTeardown(VisualToggles.Register("haze", on =>
            {
                Shader.SetGlobalVector(Haze, on ? haze : new Vector4(haze.x, haze.y, haze.z, 0f));
                Shader.SetGlobalVector(HazeWater, on ? hazeWater : new Vector4(hazeWater.x, hazeWater.y, hazeWater.z, 0f));
            }));
        }
    }

    /// <summary>The baked map textures (tools/terrain_bake.py), shared by the editor scripts and the tests.</summary>
    public static class MapTexturePaths
    {
        public const string Bake = "Assets/Textures/Map/terrain_bake.png";
        public const string Coast = "Assets/Textures/Map/coast.png";
    }
}
```

In `unity/Assets/Scripts/Map/MapBuilder.cs`:
1. Replace

```csharp
        public const int AlphamapResolution = 1024;   // ~158 m (E-W) x ~421 m (N-S) per texel
```

   with

```csharp
        public const int BiomeTextureResolution = 1024;   // ~158 m (E-W) x ~421 m (N-S) per texel
        /// <summary>Terrain.basemapDistance: farther than the camera ever is, so the terrain is always drawn by the Statecraft
        /// graph, never by its low-resolution baked basemap (Unity's default switches at 1 km).</summary>
        public const float NoBasemapDistanceM = 1e7f;
```

2. Replace

```csharp
            ApplyBiomeColours(data, frame, hexFile, assets);
            Boot.DiagLog.BootPhase("biome colours blended");
```

   (the phase line came with playtest 2, R21) with

```csharp
            terrain.basemapDistance = NoBasemapDistanceM;
            data.terrainLayers = new[] { NeutralLayer(assets) };   // one layer, one pass: the graph paints from the biome texture
            data.alphamapResolution = 16;
            MapShaderGlobals.Apply(terrain, meta, hexFile.hex_size_km * 1000f, BakeBiomeTexture(frame, hexFile, assets), assets);
            Boot.DiagLog.BootPhase("biome texture baked");
```

3. Replace the whole `ApplyBiomeColours` method (from its line `/// <summary>One terrain layer per biome; each alphamap texel blends the biomes of nearby hex centres (BiomeBlender).</summary>` to its closing brace) with:

```csharp
        /// <summary>The single terrain layer the Terrain Lit target needs; the graph ignores its colour.</summary>
        static TerrainLayer NeutralLayer(MapRuntimeAssets assets)
        {
            var tex = assets.Own(new Texture2D(1, 1, TextureFormat.RGB24, false) { name = "TerrainLayerNeutral" }); // no alpha: no smoothness
            tex.SetPixel(0, 0, Color.grey); tex.Apply();
            return assets.Own(new TerrainLayer
            {
                name = "neutral", diffuseTexture = tex, tileSize = new Vector2(BiomeLayerTileSizeM, BiomeLayerTileSizeM),
                smoothness = 0f, metallic = 0f,
            });
        }

        /// <summary>The ground colour of every point, blended from the biomes of nearby hex centres (BiomeBlender) in the
        /// MapPalette.Ground colours, baked once into an sRGB texture over the whole map (texel row 0 = south) that the
        /// terrain shader samples as _ScBiomeTex. Owned by the map.</summary>
        static Texture2D BakeBiomeTexture(GeoFrame frame, HexFile hexes, MapRuntimeAssets assets)
        {
            var names = new List<string>(Biome.Keys);
            var colours = new Color[names.Count];
            for (int i = 0; i < names.Count; i++) colours[i] = MapPalette.Ground[names[i]];
            var centres = new List<Vector2>(hexes.hexes.Length); var layerOf = new List<int>(hexes.hexes.Length);
            foreach (var h in hexes.hexes)
            {
                var w = frame.ToWorld(h.lon, h.lat, 0); centres.Add(new Vector2(w.x, w.z)); layerOf.Add(names.IndexOf(h.biome));
            }
            var blender = new BiomeBlender(centres, layerOf, hexes.hex_size_km * 1000f, names.Count);
            int res = BiomeTextureResolution;
            var tex = assets.Own(new Texture2D(res, res, TextureFormat.RGBA32, true, false)
                { name = "BiomeColour", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear });
            var px = new Color32[res * res]; var wts = new float[names.Count];
            for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                {
                    blender.Weights(new Vector2((x + 0.5f) / res * frame.WidthM, (y + 0.5f) / res * frame.LengthM), wts);
                    var c = Color.clear;
                    for (int l = 0; l < wts.Length; l++) c += colours[l] * wts[l];
                    c.a = 1f;
                    px[y * res + x] = c;
                }
            tex.SetPixels32(px); tex.Apply(true, false);
            return tex;
        }
```

Create `unity/Assets/Editor/MapTextures.cs`:

```csharp
#nullable enable
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Import settings of the baked map textures: linear data (not sRGB), clamped, mipmapped, 2048. The shading bake
    /// is block-compressed (smooth fields compress well); the coast distance is one uncompressed 8-bit channel (R8), since
    /// compression blocks would show in the 350 m foam band. Idempotent; MapMaterials.Create calls it.</summary>
    public static class MapTextures
    {
        public static void Configure()
        {
            Set(MapTexturePaths.Bake, singleChannel: false);
            Set(MapTexturePaths.Coast, singleChannel: true);
        }

        static void Set(string path, bool singleChannel)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter ?? throw new System.IO.FileNotFoundException("run tools/terrain_bake.py", path);
            imp.textureType = singleChannel ? TextureImporterType.SingleChannel : TextureImporterType.Default;
            if (singleChannel)
            {
                var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
                s.singleChannelComponent = TextureImporterSingleChannelComponent.Red;
                imp.SetTextureSettings(s);
            }
            imp.sRGBTexture = false; imp.mipmapEnabled = true; imp.isReadable = false;
            imp.wrapMode = TextureWrapMode.Clamp; imp.filterMode = FilterMode.Bilinear; imp.maxTextureSize = 2048;
            imp.textureCompression = singleChannel ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }
    }
}
```

In `unity/Assets/Editor/MapMaterials.cs`, replace

```csharp
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Materials");
            var terrain = Make(TerrainPath, "Universal Render Pipeline/Terrain/Lit", null);
            // Matte defaults. The terrain overrides these from each TerrainLayer (MapBuilder sets 0 there too); URP Terrain/Lit
            // exposes no specular-highlight or environment-reflection toggle.
            for (int i = 0; i < 4; i++) { terrain.SetFloat("_Smoothness" + i, 0f); terrain.SetFloat("_Metallic" + i, 0f); }
            EditorUtility.SetDirty(terrain);
```

with

```csharp
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Materials");
            MapTextures.Configure();
            // The Statecraft terrain graph (tools/terrain_graph.py + StatecraftTerrain.hlsl) with the baked textures.
            var terrain = Make(TerrainPath, "Statecraft/StatecraftTerrain", null);
            terrain.SetTexture("_ScBakeTex", Tex(Statecraft.Game.Map.MapTexturePaths.Bake));
            terrain.SetTexture("_ScCoastTex", Tex(Statecraft.Game.Map.MapTexturePaths.Coast));
            EditorUtility.SetDirty(terrain);
```

and add, after the `Make` method:

```csharp
        static Texture2D Tex(string path) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>(path) ?? throw new System.IO.FileNotFoundException("texture missing (tools/terrain_bake.py)", path);
```

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.MapMaterials.Create -logFile "$PWD/unity-materials.log"; echo exit=$?
git status --short unity/Assets/Materials unity/Assets/Textures
```
Expected: `exit=0`; ` M unity/Assets/Materials/Terrain.mat` and the two texture `.meta` files modified (the other materials may show no change).

- [ ] **Step 7: Run the tests and confirm they pass**

First the positive half of the gate, alone:
```bash
tools/build_sim_for_unity.sh
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testFilter "TerrainShaderTests.TheCustomFunctionDrivesTheTerrain" -testResults "$PWD/unity-editmode-gate.xml" -logFile unity-tests-gate.log; echo exit=$?
grep "magenta biome texture" unity-tests-gate.log
```
Expected: `exit=0` and a magenta colour (red and blue well above green). **If it fails, stop and report BLOCKED** (controller question 1) with the line and `grep -c StatecraftTerrain.hlsl` of the pass sources it reports; do not tune thresholds.

Then everything:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "^(sea |luma std|haze shift|seam at|land with)" unity-tests.log
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=0`; total = **B1 + 9**, `failed="0"` (Track B starts at B1; the M3 seam, backdrop and rift tests still pass because water is still the lit sea colour and carries no haze); the evidence lines; no warnings.

- [ ] **Step 8: Profile and run the art checkpoint (non-blocking)**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
grep "BUILD " unity-build-web.log
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t06-terrain
```
Expected: `BUILD Succeeded WebGL ...` with the size about 3–6 MB above Task 1's (the two textures); `PROFILE summary visual=all` no slower than Task 1's baseline by more than 2.5 ms (median, plus the larger spread) at either pose (one pass instead of two should make it faster); the checkpoint passes golden and shots.

Then the boot budget (the biome bake still runs on the main thread, now into a texture, and the graph adds shader variants):
```bash
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 --baseline 443 --baseline-first-frame 3547 > "$PWD/unity-boot-t06.log"; echo exit=$?; grep "BOOT .*median\|OVER BUDGET" unity-boot-t06.log
```
Expected: `exit=0`. If it is over budget, record the per-phase `dt` of `biome texture baked` against Task 1's `biome colours blended` and raise it in the task report (the remedy is to bake the biome colour texture offline in `terrain_bake.py`, since hexes.json and the palette are static; that is a follow-up, not part of this task). Record the build size, the summary lines and the boot line under `## Task 6: terrain` in `docs/visual-v1.md`, plus the checkpoint row.

- [ ] **Step 9: Commit**

In `tools/README.md`, after the `terrain_bake.py` entry add:

```markdown
- `terrain_graph.py` writes `unity/Assets/Shaders/StatecraftTerrain.shadergraph` from Unity's "Terrain Simple" template
  with one Custom Function node (`StatecraftTerrain.hlsl`); re-run it only when that function's inputs or outputs change.
```

and update the expected tools total to the current count (T1 + 15).

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add tools/terrain_graph.py tools/tests/test_terrain_graph.py tools/README.md docs/visual-v1.md \
  unity/Assets/Shaders/StatecraftMap.hlsl unity/Assets/Shaders/StatecraftMap.hlsl.meta \
  unity/Assets/Shaders/StatecraftTerrain.hlsl unity/Assets/Shaders/StatecraftTerrain.hlsl.meta \
  unity/Assets/Shaders/StatecraftTerrain.shadergraph unity/Assets/Shaders/StatecraftTerrain.shadergraph.meta \
  unity/Assets/Scripts/Map/MapPalette.cs unity/Assets/Scripts/Map/MapPalette.cs.meta \
  unity/Assets/Scripts/Map/MapShaderGlobals.cs unity/Assets/Scripts/Map/MapShaderGlobals.cs.meta \
  unity/Assets/Scripts/Map/MapBuilder.cs unity/Assets/Editor/MapTextures.cs unity/Assets/Editor/MapTextures.cs.meta \
  unity/Assets/Editor/MapMaterials.cs unity/Assets/Materials/Terrain.mat \
  unity/Assets/Textures/Map/terrain_bake.png.meta unity/Assets/Textures/Map/coast.png.meta \
  unity/Assets/Tests/EditMode/TerrainShaderTests.cs unity/Assets/Tests/EditMode/TerrainShaderTests.cs.meta \
  unity/Assets/Tests/EditMode/MapTests.cs
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(visual): the Statecraft Terrain Lit Shader Graph: biome colour, height, slope rock, breakup, curvature, baked AO/sky-view, haze

One terrain layer (one pass) instead of seven; the terrain no longer falls back to its basemap beyond 1 km.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 7: The sea past the map: coast-graded colour, foam, salt rims, shadows on water, and the plateau skirt

*Track B. Effort 7–10 h.* Research §1 "Water": the sea is one flat colour; the promo grades it from turquoise shallows through teal to navy, lines the shore with white foam, and (02) gives the Dead Sea a pale turquoise with white salt rims. m3-fix's border skirt (`7573092`, in the m3 base `8978566`, controller input) already carries the sea past the map as flat geometry at sea level, outside the terrain only, with `Sea.mat` on its sea submesh. This task gives both sides of the edge the same water: one function, `ScWaterShade` in `StatecraftMap.hlsl`, driven by Task 5's signed coast distance (no depth texture). Inside the map the terrain graph outputs it as emission on a black, fully metallic, fully occluded surface, so Lit adds nothing to it; past the edge `Sea.shader` returns it directly. Because emission ignores URP's lighting, `ScWaterShade` samples the main light's shadow attenuation itself (review I6), so the escarpments' shadows still fall on the Dead Sea and Task 2's `ReliefCastsShadowsAtMapScale` keeps its meaning. Both sides take the same cooler, weaker water haze (`ScHazeWater`, review I2), so the sea stays the promo's saturated blue towards the horizon; the expected colours are worked out below. A `water` toggle (review I10) falls back to the flat sea colour on both sides so the profile can price the water. The foam band animates with time; the seam test therefore compares 7×7 patch means instead of single pixels, at four camera yaws. It also closes the controller's known gap: the land skirt now holds the edge's height for 20 km and eases to sea level only by the last ring, so the Jordanian plateau (about 900 m) continues past the edge instead of sagging at once.

**Files:**
- Create: `unity/Assets/Shaders/Sea.shader`, `unity/Assets/Tests/EditMode/SeaTests.cs`
- Modify: `unity/Assets/Shaders/StatecraftMap.hlsl`, `unity/Assets/Shaders/StatecraftTerrain.hlsl`, `unity/Assets/Scripts/Map/MapShaderGlobals.cs` (the `water` toggle), `unity/Assets/Editor/MapMaterials.cs`, `unity/Assets/Materials/Sea.mat` (by `MapMaterials.Create`), `unity/Assets/Scripts/Map/MapBuilder.cs` (`SkirtFall` only), `unity/Assets/Tests/EditMode/SettlementTests.cs` (the seam test), `docs/visual-v1.md`

**Interfaces:**
- Consumes: Task 6's `StatecraftMap.hlsl` globals and `ScHazeAmount`/`ScHazeWater`/`ScWater`/`ScNoise`, `MapShaderGlobals`, `MapTexturePaths.Coast`, `MapMaterials.Tex`; m3-fix's `MapBuilder.BuildBorderSkirt`, `SkirtRings`, `MapScene.BorderSkirtName` (R3).
- Produces: HLSL `float _ScWaterOn`, `static const float ScCoastRangeM = 12000.0`, `float3 ScWaterShade(float coast, float elevM, float3 positionWS, float2 mapM)` (lit, shadowed, unhazed); shader `Statecraft/Sea` (`_ScCoastTex`; passes `Sea`, `DepthOnly`, `DepthNormals`); `MapShaderGlobals.WaterOn`; toggle `"water"`; `MapBuilder.SkirtFall = {0, 0.05, 0.15, 0.45, 1}`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/SeaTests.cs`:

```csharp
#nullable enable
using System.Collections;
using System.IO;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 7: the water, inside the map and past its edge, and the plateau skirt.</summary>
public class SeaTests
{
    const string SeaMatPath = "Assets/Materials/Sea.mat";

    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    static MapScene OpenMap()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        return map;
    }

    [Test]
    public void TheSkirtSeaUsesTheWaterShaderWithTheCoastTexture()
    {
        var sea = AssetDatabase.LoadAssetAtPath<Material>(SeaMatPath);
        Assert.AreEqual("Statecraft/Sea", sea.shader.name);
        Assert.IsFalse(ShaderUtil.ShaderHasError(sea.shader));
        Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePaths.Coast), sea.GetTexture("_ScCoastTex"));
        Assert.GreaterOrEqual(sea.FindPass("DepthOnly"), 0, "in the depth prepass, like the selection band (m3-fix N6)");
        Assert.GreaterOrEqual(sea.FindPass("DepthNormals"), 0);
    }

    [Test]
    public void TheWaterShadersCompileForWebGL2()
    {
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(Shader.Find("Statecraft/Sea")));
        CollectionAssert.IsEmpty(ShaderChecks.Gles3Errors(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain.mat").shader));
    }

    /// <summary>The controller's known gap: at the Jordanian plateau the land skirt holds the edge's height for 20 km (at
    /// least 80% of its height above the sea) and reaches sea level only at the last ring.</summary>
    [Test]
    public void TheLandSkirtHoldsThePlateauThenEasesToSeaLevel()
    {
        var map = OpenMap();
        var size = map.Terrain.terrainData.size;
        var verts = map.Terrain.transform.Find(MapScene.BorderSkirtName).GetComponent<MeshFilter>().sharedMesh.vertices;
        var meta = HeightmapLoader.Load(Statecraft.Game.Paths.StreamingMap).meta;
        float seaY = (0f - meta.elev_min_m) * MapBuilder.VerticalExaggeration;
        float zAmman = map.Frame.ToWorld(35.9, 31.95, 0).z;
        int best = -1;
        for (int i = 0; i < verts.Length; i++)
            if (Mathf.Abs(verts[i].x - size.x) < 0.5f && (best < 0 || Mathf.Abs(verts[i].z - zAmman) < Mathf.Abs(verts[best].z - zAmman))) best = i;
        Assert.GreaterOrEqual(best, 0, "an east-edge spoke");
        float edge = verts[best].y - seaY;
        Assert.Greater(edge, 1500f, "the plateau stands well above the sea (about 900 m, exaggerated 3x)");
        Assert.AreEqual(size.x + MapBuilder.SkirtRings[2], verts[best + 2].x, 1f, "ring 2 of the same spoke");
        Assert.GreaterOrEqual(verts[best + 2].y - seaY, 0.8f * edge, "held 20 km out");
        Assert.AreEqual(seaY, verts[best + MapBuilder.SkirtRings.Length - 1].y, 1f, "at sea level at the last ring");
    }

    static float Luma(Color c) => RenderSample.Luma(c);

    /// <summary>Rendered west of Tel Aviv: just offshore (inside the 350 m foam band) is brighter than 1.2 km out, which is
    /// lighter and greener than the open sea 24 km out. The shore is found in coast.png itself.</summary>
    [UnityTest]
    public IEnumerator ShallowsAreLighterThanTheOpenSeaAndFoamLinesTheShore()
    {
        var map = OpenMap();
        yield return null;
        var coast = new Texture2D(2, 2);
        coast.LoadImage(File.ReadAllBytes(MapTexturePaths.Coast));   // readable copy; row 0 = south
        int row = Mathf.FloorToInt((32.08f - 29.45f) / (33.35f - 29.45f) * coast.height);
        int col = Mathf.FloorToInt((34.95f - 34.2f) / (35.9f - 34.2f) * coast.width);
        while (col > 0 && coast.GetPixel(col, row).r >= 0.5f) col--;          // walk west to the first water texel
        Object.DestroyImmediate(coast);
        Vector3 At(int c)
        {
            double lon = 34.2 + (c + 0.5) / 2048.0 * (35.9 - 34.2), lat = 29.45 + (row + 0.5) / 2048.0 * (33.35 - 29.45);
            var w = map.Frame.ToWorld(lon, lat, 0); w.y = map.Terrain.SampleHeight(w); return w;
        }
        Vector3 foam = At(col - 2), shallow = At(col - 15), deep = At(col - 300);
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        rig.LookAt = shallow; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[2];
        var near = RenderSample.Render(cam, 960, 540, new[] { foam, shallow }, px);
        Color f = near.Mean((int)px[0].x, (int)px[0].y, 1), s = near.Mean((int)px[1].x, (int)px[1].y, 1);
        rig.LookAt = deep; rig.Apply();
        var far = RenderSample.Render(cam, 960, 540, new[] { deep }, px);
        Color d = far.Mean((int)px[0].x, (int)px[0].y, 2);
        TestContext.WriteLine($"foam {f}, shallow {s}, deep {d}");
        Assert.Greater(Luma(f), Luma(s) + 0.02f, "foam at the shore");
        Assert.Greater(Luma(s), Luma(d) + 0.05f, "shallows lighter");
        Assert.Greater(s.g / s.b, d.g / d.b, "shallows greener");
    }

    /// <summary>Rendered: the Dead Sea is pale turquoise, much lighter than the open Mediterranean.</summary>
    [UnityTest]
    public IEnumerator TheDeadSeaIsPalerThanTheOpenSea()
    {
        var map = OpenMap();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        Color Sample(double lon, double lat)
        {
            var w = map.Frame.ToWorld(lon, lat, 0); w.y = map.Terrain.SampleHeight(w);
            rig.LookAt = w; rig.Distance = 40000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var px = new Vector2[1];
            var r = RenderSample.Render(cam, 480, 270, new[] { w }, px);
            return r.Mean((int)px[0].x, (int)px[0].y, 2);
        }
        Color dead = Sample(35.50, 31.50), med = Sample(34.40, 32.00);
        TestContext.WriteLine($"Dead Sea {dead}, Mediterranean {med}");
        Assert.Greater(Luma(dead), Luma(med) + 0.1f);
    }

    /// <summary>Review I2: at the country-wide pose (about 300 km from the camera) the open sea stays saturated blue under the
    /// water haze: b > r + 0.15 (worked out in Step 4: about 0.21 before the grade). And the water toggle exists.</summary>
    [UnityTest]
    public IEnumerator TheOpenSeaStaysBlueUnderTheHaze()
    {
        var map = OpenMap();
        yield return null;
        CollectionAssert.Contains(VisualToggles.Names, "water");
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        PromoPose.Diorama02.Apply(rig, map.Frame); rig.TopInset = 0; rig.Apply();
        var sea = map.Frame.ToWorld(34.40, 31.80, 0); sea.y = map.Terrain.SampleHeight(sea);
        var px = new Vector2[1];
        var s = RenderSample.Render(cam, 960, 540, new[] { sea }, px);
        var c = s.Mean((int)px[0].x, (int)px[0].y, 2);
        TestContext.WriteLine($"open sea at the diorama pose {c}, b - r = {c.b - c.r:0.000}");
        Assert.Greater(c.b, c.r + 0.15f);
    }
}
```

In `unity/Assets/Tests/EditMode/SettlementTests.cs`, replace the whole `TheSeaBeyondTheEdgeMatchesTheTerrainSea` method (from its `[UnityTest]` line to its closing brace, R5) with:

```csharp
    [UnityTest]
    public System.Collections.IEnumerator TheSeaBeyondTheEdgeMatchesTheTerrainSea()
    {
        // Visual v1 Task 7: both sides now shade with StatecraftMap.hlsl ScWaterShade, whose waves vary over a few hundred
        // metres, so 7x7-pixel patch means are compared (still 0.012), at four yaws (Review Focus 1).
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var size = map.Terrain.terrainData.size;
        foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
            foreach (var (look, inside, beyond) in new[] {
                (new Vector3(3000, 0, size.z * 0.55f), new Vector3(2500, 0, size.z * 0.55f), new Vector3(-2500, 0, size.z * 0.55f)),
                (new Vector3(3000, 0, size.z - 3000), new Vector3(2500, 0, size.z - 2500), new Vector3(-2500, 0, size.z + 2500)) })
            {
                rig.LookAt = look; rig.Distance = 120000f; rig.Yaw = yaw; rig.TopInset = 0; rig.Apply();
                var y = map.Terrain.SampleHeight(inside);
                var px = new Vector2[2];
                var s = RenderSample.Render(cam, 1600, 900, new[] { new Vector3(inside.x, y, inside.z), new Vector3(beyond.x, y, beyond.z) }, px);
                Color a = s.Mean((int)px[0].x, (int)px[0].y, 3), b = s.Mean((int)px[1].x, (int)px[1].y, 3);
                var diff = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
                TestContext.WriteLine($"seam at {look}, yaw {yaw}: terrain sea {a}, beyond {b}, difference {diff:0.000}");
                Assert.Less(diff, 0.012f, $"seam at {look}, yaw {yaw}: terrain sea {a}, beyond {b}");
            }
    }
```

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=1` with failures, no compile errors: `TheSkirtSeaUsesTheWaterShaderWithTheCoastTexture` (`Expected: "Statecraft/Sea" But was: "Universal Render Pipeline/Lit"`), `TheWaterShadersCompileForWebGL2` (`ArgumentNullException`: no such shader yet), `TheLandSkirtHoldsThePlateauThenEasesToSeaLevel` (held height below 80%), `TheDeadSeaIsPalerThanTheOpenSea` (both the flat sea colour). The patch-mean seam test passes already (both sides are still the flat colour).

- [ ] **Step 3: Implement**

In `unity/Assets/Shaders/StatecraftMap.hlsl`, replace

```hlsl
// 1 in water, 0 on land; coast is the signed distance texture (0.5 at the shore, tools/terrain_bake.py).
float ScWater(float coast) { return 1.0 - smoothstep(0.497, 0.503, coast); }
```

with

```hlsl
// 1 in water, 0 on land; coast is the signed distance texture (0.5 at the shore, tools/terrain_bake.py).
float ScWater(float coast) { return 1.0 - smoothstep(0.497, 0.503, coast); }

static const float ScCoastRangeM = 12000.0;   // tools/terrain_bake.py COAST_RANGE_M
float _ScWaterOn;                              // 1 = shaded water, 0 = the flat sea colour (the "water" toggle)

// The water's lit colour before haze (callers apply ScHazeWater and output it as is): a turquoise -> teal -> navy ramp with
// distance from the shore; the Dead Sea (below -300 m) pale turquoise with a white salt rim, the Sea of Galilee a fresher
// teal; two scrolling noise layers tilt a wave normal for Lambert from the sun (with its shadows: cliffs shade the water)
// plus sky fill and a small glint; animated foam bands within 350 m of the shore. elevM is the real elevation of the water
// surface; mapM metres east and north of the map's south-west corner.
float3 ScWaterShade(float coast, float elevM, float3 positionWS, float2 mapM)
{
    if (_ScWaterOn < 0.5) return SRGBToLinear(float3(0.16, 0.42, 0.55)) * 0.8;
    float offshoreM = saturate(0.5 - coast) * 2.0 * ScCoastRangeM;
    float t = pow(saturate(offshoreM / ScCoastRangeM), 0.55);
    float3 shallow = SRGBToLinear(float3(0.38, 0.74, 0.72));
    float3 mid = SRGBToLinear(float3(0.13, 0.47, 0.58));
    float3 deep = SRGBToLinear(float3(0.06, 0.24, 0.40));
    float3 c = t < 0.5 ? lerp(shallow, mid, t * 2.0) : lerp(mid, deep, t * 2.0 - 1.0);
    float salt = step(elevM, -300.0);
    float fresh = step(elevM, -100.0) * (1.0 - salt);
    c = lerp(c, SRGBToLinear(float3(0.45, 0.78, 0.78)), salt * 0.8);
    c = lerp(c, SRGBToLinear(float3(0.15, 0.48, 0.60)), fresh * 0.7);

    float time = _Time.y;
    float2 p = mapM / 700.0;
    float2 g = float2(ScNoise(p + float2(time * 0.05, 0.0)) - ScNoise(p + float2(0.37 + time * 0.05, 0.0)),
                      ScNoise(p.yx + float2(0.0, time * 0.04)) - ScNoise(p.yx + float2(0.0, 0.41 + time * 0.04)));
    float3 n = normalize(float3(g.x * 0.15, 1.0, g.y * 0.15));
#if defined(UNIVERSAL_REALTIME_LIGHTS_INCLUDED) && !defined(SHADERGRAPH_PREVIEW)
    Light sun = GetMainLight(TransformWorldToShadowCoord(positionWS));   // review I6: the water receives the sun's shadows
    float3 l = sun.direction, lc = sun.color;
    float shadow = sun.shadowAttenuation;
#else
    float3 l = normalize(float3(-0.83, 0.5, 0.22)), lc = float3(1.0, 0.84, 0.64);   // passes without lights (depth, preview)
    float shadow = 1.0;
#endif
    float3 v = normalize(_WorldSpaceCameraPos - positionWS);
    float3 lit = c * (0.55 + 0.45 * saturate(dot(n, l)) * lc * shadow);
    lit += pow(saturate(dot(reflect(-l, n), v)), 80.0) * 0.25 * lc * shadow;

    float band = 1.0 - smoothstep(0.0, 350.0, offshoreM);
    float waves = 0.7 + 0.3 * sin(offshoreM / 90.0 - time * 1.3);
    float rim = salt * (1.0 - smoothstep(0.0, 900.0, offshoreM));
    float foam = saturate(band * waves * (1.0 - salt) + rim);
    return lerp(lit, SRGBToLinear(float3(0.93, 0.95, 0.94)), foam * 0.85);
}
```

In `unity/Assets/Shaders/StatecraftTerrain.hlsl`, replace

```hlsl
    // Water: the flat sea colour, lit like the skirt's URP Lit Sea.mat so the map's edge shows no seam (Task 7 replaces
    // both sides with ScWaterShade together). No haze on water yet: the skirt's URP Lit sea has none either.
    BaseColor = lerp(ground * (1.0 - h), SRGBToLinear(float3(0.16, 0.42, 0.55)), water);
    NormalOut = normalize(lerp(n, float3(0, 1, 0), water));
    Occlusion = lerp(lerp(1.0, ao, 0.8), 1.0, water);
    Smoothness = 0.0;
    Metallic = 0.0;
    Emission = _ScHaze.rgb * h * (1.0 - water);
```

with

```hlsl
    // Water: ScWaterShade's colour (sunlit with its shadows, then the cool water haze) as emission on a black, fully metallic,
    // fully occluded surface, so Lit adds nothing (no diffuse, specular colour 0, indirect light times occlusion 0): exactly
    // what Sea.shader returns past the edge. Land keeps its after-lighting haze through the same Emission.
    float elevM = (PositionWS.y - _ScMap.z) / _ScMap.w;
    float3 waterColour = ScHazeWater(ScWaterShade(coast, elevM, PositionWS, mapM), PositionWS);
    BaseColor = ground * (1.0 - h) * (1.0 - water);
    NormalOut = normalize(lerp(n, float3(0, 1, 0), water));
    Occlusion = lerp(lerp(1.0, ao, 0.8), 0.0, water);
    Smoothness = 0.0;
    Metallic = water;
    Emission = lerp(_ScHaze.rgb * h, waterColour, water);
```

In `unity/Assets/Scripts/Map/MapShaderGlobals.cs`:
1. Replace `            HazeRange = Shader.PropertyToID("_ScHazeRange");` with `            HazeRange = Shader.PropertyToID("_ScHazeRange"), WaterOn = Shader.PropertyToID("_ScWaterOn");`.
2. Replace

```csharp
            assets.OnTeardown(VisualToggles.Register("haze", on =>
```

   with

```csharp
            Shader.SetGlobalFloat(WaterOn, 1f);
            assets.OnTeardown(VisualToggles.Register("water", on => Shader.SetGlobalFloat(WaterOn, on ? 1f : 0f)));
            assets.OnTeardown(VisualToggles.Register("haze", on =>
```

Create `unity/Assets/Shaders/Sea.shader`:

```hlsl
// The border skirt's sea past the map's edge (plan Task 7): the same water as the terrain's own sea, from the same function
// (StatecraftMap.hlsl ScWaterShade) and coast texture (clamped at the edge), with the same water haze, so no seam shows. Unlit:
// the function lights itself, as the terrain graph's water (emission only) does. DepthOnly and DepthNormals passes keep it
// in the depth prepass on tiers that make one.
Shader "Statecraft/Sea"
{
    Properties
    {
        [NoScaleOffset] _ScCoastTex ("Coast distance (R)", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float _ScSeaUnused;   // the SRP Batcher needs a UnityPerMaterial buffer; the sea has no per-material numbers
        CBUFFER_END
        struct Attributes { float4 positionOS : POSITION; };
        ENDHLSL

        Pass
        {
            Name "Sea"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // The main light's shadows, as the terrain graph's forward pass has them (ScWaterShade samples them).
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"
            TEXTURE2D(_ScCoastTex); SAMPLER(sampler_ScCoastTex);
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 mapM = input.positionWS.xz - _ScMapOrigin.xz;
                float coast = SAMPLE_TEXTURE2D(_ScCoastTex, sampler_ScCoastTex, saturate(mapM / _ScMap.xy)).r;
                return half4(ScHazeWater(ScWaterShade(coast, 0.0, input.positionWS, mapM), input.positionWS), 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Vert(Attributes input) : SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float4 Vert(Attributes input) : SV_POSITION { return TransformObjectToHClip(input.positionOS.xyz); }
            half4 Frag() : SV_Target { return half4(0.0, 1.0, 0.0, 0.0); }   // the sea faces up
            ENDHLSL
        }
    }
}
```

In `unity/Assets/Editor/MapMaterials.cs`, replace the block from the comment line beginning `// The sea past the map's edge` through its `EditorUtility.SetDirty(sea);` with:

```csharp
            // The skirt's sea past the map's edge: the terrain's own water shading (StatecraftMap.hlsl ScWaterShade), so no seam.
            var sea = Make(SeaPath, "Statecraft/Sea", null);
            sea.SetTexture("_ScCoastTex", Tex(Statecraft.Game.Map.MapTexturePaths.Coast));
            EditorUtility.SetDirty(sea);
```

In `unity/Assets/Scripts/Map/MapBuilder.cs`, replace

```csharp
        /// <summary>Share of the fall to sea level reached at each ring (land samples only).</summary>
        static readonly float[] SkirtFall = { 0f, 0.4f, 0.85f, 1f, 1f };
```

with

```csharp
        /// <summary>Share of the fall to sea level reached at each ring (land samples only). Visual v1 Task 7: the edge's height
        /// is held for 20 km (the Jordanian plateau, about 900 m, continues past the edge instead of sagging at once) and
        /// eases to sea level by the last ring, beyond anything the camera sees.</summary>
        public static readonly float[] SkirtFall = { 0f, 0.05f, 0.15f, 0.45f, 1f };
```

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.MapMaterials.Create -logFile "$PWD/unity-materials.log"; echo exit=$?
git status --short unity/Assets/Materials
```
Expected: `exit=0`; ` M unity/Assets/Materials/Sea.mat`.

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=0`; total = **(Task 6 total) + 6**, `failed="0"`, including m3-fix's `TheBorderSkirtContinuesEachEdgeOutsideTheMap` (its inner edge still meets the terrain: ring 0 is the edge height), the reworked seam test (eight `seam at` lines, every difference under 0.012) and these colour tests, whose margins are worked out here (review I2) with the deep-water colour sRGB (0.06, 0.24, 0.40) = linear (0.0052, 0.0467, 0.133), Task 2's sun (linear colour (1.0, 0.674, 0.367) × 1.6, 30° up, so N·L ≈ 0.5 and the Lambert factor 0.55 + 0.225 × colour = (0.91, 0.79, 0.68)), lit deep water linear (0.0047, 0.0370, 0.0907), and the water haze linear (0.342, 0.508, 0.708) at strength 0.24 × (1 − e^(−d / 400 km)):
  - `TheBackdropIsLandWhereTheNeighboursAreLand`, west sample at about 90 km: haze 0.24 × 0.20 = 0.048, linear (0.0209, 0.0596, 0.120), sRGB (0.155, 0.27, 0.381): b − r ≈ 0.23 > 0.15.
  - `TheOpenSeaStaysBlueUnderTheHaze`, about 300 km: haze 0.24 × 0.53 = 0.127, linear (0.0475, 0.0969, 0.169), sRGB (0.24, 0.35, 0.45): b − r ≈ 0.21 > 0.15.
  - `TheCoastSeparatesLandFromSea` (Task 6), 10 km offshore at 60 km: the ramp gives linear (0.0072, 0.074, 0.164), lit (0.0066, 0.059, 0.112), haze 0.24 × 0.14 = 0.033: sRGB r ≈ 0.14, b ≈ 0.40: b − r ≈ 0.26 > 0.15.
  - In Track B alone the sun is still `Map.unity`'s default (50° up, intensity 1), whose Lambert factors are within 0.05 of these. At the merge, Track A's warm white balance and amber highlights take an estimated 0.02–0.04 off b − r and the +16 saturation adds a similar amount back, leaving every margin at 0.04 or more; the merge point re-runs all three.
No warnings. If a seam line fails only at one yaw, the glint differs between the two sides because one of them is outside the light's `GetMainLight` path: check that `Sea.shader` includes `Lighting.hlsl` before `StatecraftMap.hlsl`.

- [ ] **Step 5: Profile and run the art checkpoint (non-blocking)**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t07-sea
```
Expected: `PROFILE summary visual=all` no more than 2.5 ms (median, plus the larger spread) slower than Task 6's at either pose, and a `visual=-water` line pricing the water; checkpoint passes. Record under `## Task 7: sea` in `docs/visual-v1.md` with the checkpoint row, and one sentence on the plateau edge as seen in `diorama02` (the owner's known gap).

- [ ] **Step 6: Commit**

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add unity/Assets/Shaders/Sea.shader unity/Assets/Shaders/Sea.shader.meta \
  unity/Assets/Shaders/StatecraftMap.hlsl unity/Assets/Shaders/StatecraftTerrain.hlsl \
  unity/Assets/Editor/MapMaterials.cs unity/Assets/Materials/Sea.mat unity/Assets/Scripts/Map/MapBuilder.cs unity/Assets/Scripts/Map/MapShaderGlobals.cs \
  unity/Assets/Tests/EditMode/SeaTests.cs unity/Assets/Tests/EditMode/SeaTests.cs.meta \
  unity/Assets/Tests/EditMode/SettlementTests.cs docs/visual-v1.md
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(visual): coast-graded water with foam and salt rims on both sides of the map's edge; the plateau skirt holds its height

Closes the m3-fix known gap: the land skirt keeps the edge's height for 20 km before easing to sea level.

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 8: The hex grid drawn by the shaders on land and sea, anti-aliased; the 1-pixel line mesh removed

*Track B. Effort 6–8 h.* Research §0.5 and §1 "Hex grid": the grid is a `MeshTopology.Lines` mesh lifted 120 m above the terrain: always one pixel, aliased, never dimmed, slightly detached at oblique angles, and it competes with the terrain. The promo's grid is light and faint over land and light over the sea, and it runs on over the open sea. This task draws the grid in the terrain shader **and in the skirt's `Sea.shader`** (review I3: the lattice continues past the map's edge, so no step shows there) from a hex distance field (the same layout as `tools/hexgrid.py`: pointy-top axial, (0, 0) at the map's north-west corner, y south), anti-aliased with `fwidth`, 1.5 pixels wide at any zoom, light on both land (0.25 opacity; the promo wins over M2's dark lines, ruling 2) and water (0.35), thinning towards maximum zoom-out; and deletes the line mesh, its material, its `overlayMaterial` parameter, its `OverlayClearanceM` and its boot phase. With no line mesh to cover, the selected-hex band drops from 140 m to 30 m above the ground (review minor 1: at the 6 km hero zoom it visibly floated); its depth offset still keeps it from z-fighting.

**Files:**
- Create: `unity/Assets/Scripts/Map/HexGrid.cs`, `unity/Assets/Tests/EditMode/HexGridTests.cs`
- Modify: `unity/Assets/Shaders/StatecraftMap.hlsl`, `unity/Assets/Shaders/StatecraftTerrain.hlsl`, `unity/Assets/Shaders/Sea.shader`, `unity/Assets/Scripts/Map/MapShaderGlobals.cs`, `unity/Assets/Scripts/Map/MapBuilder.cs`, `unity/Assets/Scripts/Map/MapScene.cs`, `unity/Assets/Editor/MapMaterials.cs`, `unity/Assets/Editor/MapCapture.cs`, `unity/Assets/Map.unity` (one line), `unity/Assets/Tests/EditMode/{MapTests,SettlementTests,MapActionUiTests,GameSceneTests}.cs`, `docs/visual-v1.md`
- Delete: `unity/Assets/Materials/HexOverlay.mat`, `unity/Assets/Materials/HexOverlay.mat.meta`

**Interfaces:**
- Consumes: `_ScMap`, `_ScMapOrigin.w` (hex size) from Task 6; `ScWaterShade`, `ScHazeWater` and `Sea.shader` from Task 7; `hexes.json` layout (R15).
- Produces: HLSL `float4 _ScGrid` (x land opacity, y water opacity, z width px, w on), `ScGridLandSrgb`, `ScGridWaterSrgb`, `float ScHexEdgeDistance(float2 p, float size)`, `float ScGridLine(float2 mapM, float widthPx)`, `float ScGridAmount(float2 mapM, float3 positionWS)`; C# `HexGrid { static Vector2 Centre(int q, int r, float size); static Vector2 NearestCentre(Vector2 p, float size); static Vector2 FromWorld(Vector3 world, Vector3 origin, float lengthM); static Vector3 ToWorld(Vector2 p, Vector3 origin, float lengthM); static float EdgeDistance(Vector2 p, float size); }`; `MapShaderGlobals.Grid`, `GridLandOpacity = 0.25f`, `GridWaterOpacity = 0.35f` (shipped: 0.35 / 0.10 plus a footprint fade, Task 8 fix round 1 and merge point), `GridWidthPx = 1.5f`; toggle `"grid"`; `MapScene.SelectionClearanceM = 30f` (replacing `SelectionLiftM` and `MapBuilder.OverlayClearanceM`); `MapBuilder.Build(HeightmapMeta, float[,], HexFile, Material terrain)` and `Build(string mapDir, string hexesJson, Material terrain)`; `MapScene.Configure(Material terrain, Material marker, Material selection, Material sea, Material borderLand)`.

- [ ] **Step 1: Write the failing tests and remove the overlay from the existing ones**

Create `unity/Assets/Tests/EditMode/HexGridTests.cs`:

```csharp
#nullable enable
using System.Collections;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Plan Task 8: the shader-drawn hex grid.</summary>
public class HexGridTests
{
    const float Size = 5000f;

    [TearDown]
    public void Reset() { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    [Test]
    public void CentresAreTheInradiusFromEveryEdgeAndCornersAndEdgesAreZero()
    {
        var c = HexGrid.Centre(3, -2, Size);
        float inradius = Mathf.Sqrt(3f) / 2f * Size;
        Assert.AreEqual(inradius, HexGrid.EdgeDistance(c, Size), 1e-2f);
        Assert.AreEqual(0f, HexGrid.EdgeDistance(c + new Vector2(inradius, 0f), Size), 1e-2f, "middle of the east edge");
        Assert.AreEqual(0f, HexGrid.EdgeDistance(c + new Vector2(inradius, -Size / 2f), Size), 1e-2f, "a corner");
        Assert.AreEqual(inradius / 2f, HexGrid.EdgeDistance(c + new Vector2(inradius / 2f, 0f), Size), 1e-2f);
    }

    [Test]
    public void NearestCentreAndToWorldInvertTheGridMapping()
    {
        var c = HexGrid.Centre(-2, 5, Size);
        Assert.AreEqual(c, HexGrid.NearestCentre(c + new Vector2(1000f, -800f), Size), "a point inside a hex rounds to its centre");
        var origin = new Vector3(10f, 5f, 20f); const float length = 431000f;
        var world = HexGrid.ToWorld(c, origin, length);
        Assert.AreEqual(c.x, HexGrid.FromWorld(world, origin, length).x, 1e-2f);
        Assert.AreEqual(c.y, HexGrid.FromWorld(world, origin, length).y, 1e-2f);
        Assert.Less(HexGrid.ToWorld(new Vector2(-4330f, 100f), origin, length).x, origin.x, "west of the map stays west");
    }

    /// <summary>The shader's layout is tools/hexgrid.py's: every hex centre in hexes.json lies on a grid centre.</summary>
    [Test]
    public void ItAgreesWithEveryHexCentreInHexesJson()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        float size = map.Hexes.hex_size_km * 1000f, inradius = Mathf.Sqrt(3f) / 2f * size;
        foreach (var h in map.Hexes.hexes)
        {
            var p = HexGrid.FromWorld(map.Frame.ToWorld(h.lon, h.lat, 0), map.Terrain.transform.position, map.Frame.LengthM);
            Assert.AreEqual(0f, (p - HexGrid.Centre(h.q, h.r, size)).magnitude, 50f, $"hex ({h.q}, {h.r}) centre");
            Assert.AreEqual(inradius, HexGrid.EdgeDistance(p, size), 50f, $"hex ({h.q}, {h.r})");
        }
    }

    static (MapScene map, Camera cam, CameraRig rig) Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        var cam = Camera.main;
        return (map, cam, cam.GetComponent<CameraRig>());
    }

    /// <summary>Rendered: the middle of a land hex's east edge changes with the grid on (the promo's land lines are light).</summary>
    [UnityTest]
    public IEnumerator GridLinesShowOnHexEdges()
    {
        var (map, cam, rig) = Open();
        yield return null;
        var hex = System.Array.Find(map.Hexes.hexes, h => h.land && h.biome == "hills");
        var centre = map.Frame.ToWorld(hex.lon, hex.lat, 0);
        var edge = centre + new Vector3(Mathf.Sqrt(3f) / 2f * map.Hexes.hex_size_km * 1000f, 0f, 0f);
        edge.y = map.Terrain.SampleHeight(edge);
        rig.LookAt = centre; rig.Distance = 30000f; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
        var px = new Vector2[1];
        var on = RenderSample.Render(cam, 960, 540, new[] { edge }, px);
        VisualToggles.Set("grid", false);
        var off = RenderSample.Render(cam, 960, 540, new[] { edge }, px);
        VisualToggles.Set("grid", true);
        float change = 0f;
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                change = Mathf.Max(change, Mathf.Abs(RenderSample.Luma(on.At((int)px[0].x + dx, (int)px[0].y + dy)) - RenderSample.Luma(off.At((int)px[0].x + dx, (int)px[0].y + dy))));
        TestContext.WriteLine($"edge luma change {change:0.000}");
        Assert.Greater(change, 0.03f);
    }

    /// <summary>Rendered at the hero zoom (6 km) and the overview (250 km): lines are visible but cover little of the view
    /// (no moiré wash at the overview, still drawn up close).</summary>
    [UnityTest]
    public IEnumerator GridLinesStayThinAtBothZoomExtremes()
    {
        var (map, cam, rig) = Open();
        yield return null;
        // Look at the middle of a hex edge (review minor 2: the 6 km view spans about 7 km, less than a hex, so an arbitrary
        // point might show no line at all).
        float size = map.Hexes.hex_size_km * 1000f;
        var origin = map.Terrain.transform.position;
        var centre = HexGrid.NearestCentre(HexGrid.FromWorld(map.Frame.ToWorld(35.0, 31.6, 0), origin, map.Frame.LengthM), size);
        var edgeMid = HexGrid.ToWorld(centre + new Vector2(Mathf.Sqrt(3f) / 2f * size, 0f), origin, map.Frame.LengthM);
        edgeMid.y = map.Terrain.SampleHeight(edgeMid);
        foreach (var distance in new[] { CameraController.MinDistance, CameraController.MaxDistance })
        {
            rig.LookAt = edgeMid; rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
            var on = RenderSample.Render(cam, 960, 540);
            VisualToggles.Set("grid", false);
            var off = RenderSample.Render(cam, 960, 540);
            VisualToggles.Set("grid", true);
            float covered = RenderSample.FractionDarker(on, off, 0.02f) + RenderSample.FractionDarker(off, on, 0.02f);
            TestContext.WriteLine($"{distance} m: grid covers {covered:P2}");
            Assert.That(covered, Is.InRange(0.0005f, 0.25f), $"{distance} m");
        }
    }

    [Test]
    public void TheLineMeshAndItsMaterialAreGone()
    {
        Assert.IsNull(AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/HexOverlay.mat"));
        var map = new GameObject("m").AddComponent<MapScene>();
        try { Assert.IsNull(new SerializedObject(map).FindProperty("overlayMaterial")); }
        finally { Object.DestroyImmediate(map.gameObject); }
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var built = Object.FindFirstObjectByType<MapScene>(); built.Build();
        Assert.IsNull(built.Terrain.transform.Find("HexOverlay"));
        CollectionAssert.Contains(VisualToggles.Names, "grid");
    }
}
```

Remove the overlay from the existing tests (mechanical edits first, then the named ones):

```bash
sed -i '' 's/var (terrainMat, overlayMat) = Materials();/var terrainMat = TerrainMaterial();/; s/, terrainMat, overlayMat)/, terrainMat)/g' unity/Assets/Tests/EditMode/MapTests.cs
sed -i '' 's/Load(OverlayMatPath), //g' unity/Assets/Tests/EditMode/SettlementTests.cs
sed -i '' '/Assets\/Materials\/HexOverlay.mat"),$/d' unity/Assets/Tests/EditMode/MapActionUiTests.cs
sed -i '' '/map.FindProperty("overlayMaterial")/d' unity/Assets/Tests/EditMode/GameSceneTests.cs
```

Then, in `unity/Assets/Tests/EditMode/MapTests.cs`:
1. Replace `    const string TerrainMatPath = "Assets/Materials/Terrain.mat", OverlayMatPath = "Assets/Materials/HexOverlay.mat";` with `    const string TerrainMatPath = "Assets/Materials/Terrain.mat";`.
2. Replace the whole `Materials()` helper (from `static (Material terrain, Material overlay) Materials()` to its closing brace) with:

```csharp
    static Material TerrainMaterial()
    {
        var terrainMat = AssetDatabase.LoadAssetAtPath<Material>(TerrainMatPath);
        Assert.IsNotNull(terrainMat, TerrainMatPath);
        return terrainMat;
    }
```

3. Replace the whole `BuildUsesThePassedTerrainAndOverlayMaterials` method with:

```csharp
    [Test]
    public void BuildUsesThePassedTerrainMaterialAndDrawsNoLineMesh()
    {
        var terrainMat = TerrainMaterial();
        Assert.AreEqual("Statecraft/StatecraftTerrain", terrainMat.shader.name);
        var go = MapBuilder.Build(Paths.StreamingMap, HexesJson, terrainMat);
        try
        {
            var terrain = go.GetComponent<Terrain>();
            Assert.AreSame(terrainMat, terrain.materialTemplate);
            Assert.IsNull(go.transform.Find("HexOverlay"), "the grid is drawn by the terrain shader (visual v1 Task 8)");
            Assert.AreEqual(2049, terrain.terrainData.heightmapResolution);
            Assert.AreEqual(1, terrain.terrainData.terrainLayers.Length);
        }
        finally { Object.DestroyImmediate(go); }
    }
```

4. In `BuildRejectsMissingMaterials`, replace `MapBuilder.Build(Paths.StreamingMap, HexesJson, null, null)` with `MapBuilder.Build(Paths.StreamingMap, HexesJson, null)`.
5. Delete the comment `    // --- Overlay drape (M1, L6) ---` and the two tests under it, `EveryOverlayVertexLiesAboveTheTerrainAndInsideItsBounds` and `EveryPointAlongEveryOverlaySegmentLiesAboveTheTerrain` (their geometry is gone; `DrapedRing`, which the selection band still uses, stays covered by `SelectingAHexOutlinesItOnTheTerrain`).
6. In `DestroyingTheMapDestroysItsRuntimeAssets`, replace `var owned = new System.Collections.Generic.List<Object> { data, go.transform.Find("HexOverlay").GetComponent<MeshFilter>().sharedMesh };` with `var owned = new System.Collections.Generic.List<Object> { data };` and `Assert.IsTrue(terrainMat != null && overlayMat != null, "material assets must not be destroyed");` with `Assert.IsTrue(terrainMat != null, "the material asset must not be destroyed");`.
7. In `BuildFromReadInputsMatchesBuildFromPaths`, delete the two lines comparing `a.transform.Find("HexOverlay")...vertices` with `b.transform.Find("HexOverlay")...vertices`.

In `unity/Assets/Tests/EditMode/SettlementTests.cs`:
1. In the `const string TerrainMatPath = ...` declaration, delete `OverlayMatPath = "Assets/Materials/HexOverlay.mat",` (the declaration then starts `const string TerrainMatPath = "Assets/Materials/Terrain.mat",` and continues with `MarkerMatPath`).
2. Replace `        Assert.Greater(selection.renderQueue, Load(OverlayMatPath).renderQueue, "drawn after the overlay lines");` with `        Assert.Greater(selection.renderQueue, 2000, "drawn after the opaque terrain (queue Geometry+10)");`.
3. In `TheRiftBelowSeaLevelIsVisible`, replace

```csharp
            var corner = new Vector3(corner2.x, 0, corner2.y); corner.y = map.Terrain.SampleHeight(corner) + MapBuilder.OverlayClearanceM;
            var colours = new Color[1]; var lines = new Color[1];
            yield return RenderAt(centre, 40000f, new[] { centre }, colours);
            yield return RenderAt(centre, 40000f, new[] { corner }, lines, radius: 2, darkest: true);
            TestContext.WriteLine($"{name} ({low.q}, {low.r}) {low.elev_m:0} m: centre {colours[0]}, line {lines[0]}");
            Assert.IsTrue(LandColour(colours[0]), $"{name}: the {low.biome} colour, rendered {colours[0]}");
            Assert.Less(lines[0].r + lines[0].g + lines[0].b, 0.75f * (colours[0].r + colours[0].g + colours[0].b), $"{name}: the hex line shows, rendered {lines[0]}");
```

   with

```csharp
            var corner = new Vector3(corner2.x, 0, corner2.y); corner.y = map.Terrain.SampleHeight(corner);
            var colours = new Color[1];
            yield return RenderAt(centre, 40000f, new[] { centre }, colours);
            // Visual v1 Task 8: the terrain shader draws the grid, light and faint as in the promo; the line shows if the
            // corner's neighbourhood changes when the grid is switched off.
            var cam = Camera.main; var px = new Vector2[1];
            var on = RenderSample.Render(cam, 1600, 900, new[] { corner }, px);
            VisualToggles.Set("grid", false);
            var off = RenderSample.Render(cam, 1600, 900, new[] { corner }, px);
            VisualToggles.Set("grid", true);
            float change = 0f;
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                    change = Mathf.Max(change, Mathf.Abs(RenderSample.Luma(on.At((int)px[0].x + dx, (int)px[0].y + dy)) - RenderSample.Luma(off.At((int)px[0].x + dx, (int)px[0].y + dy))));
            TestContext.WriteLine($"{name} ({low.q}, {low.r}) {low.elev_m:0} m: centre {colours[0]}, line change {change:0.000}");
            Assert.IsTrue(LandColour(colours[0]), $"{name}: the {low.biome} colour, rendered {colours[0]}");
            Assert.Greater(change, 0.03f, $"{name}: the hex line shows");
```

4. In `SelectingAHexOutlinesItOnTheTerrain`, replace `Assert.Greater(mid.y, map.Terrain.SampleHeight(mid) + MapBuilder.OverlayClearanceM - 1f,` with `Assert.Greater(mid.y, map.Terrain.SampleHeight(mid) + MapScene.SelectionClearanceM - 1f,`.
5. Replace the whole `TheSeaBeyondTheEdgeMatchesTheTerrainSea` method (Task 7's version) with this one (review N1: one hex row, ±4330 m, haze off for the seam itself, plus a haze-on check with a derived tolerance):

```csharp
    [UnityTest]
    public System.Collections.IEnumerator TheSeaBeyondTheEdgeMatchesTheTerrainSea()
    {
        // Visual v1 Tasks 7-8 (reviews I3, N1): both sides shade with ScWaterShade and draw the same hex grid (the lattice runs
        // on past the edge). Each pair is two hex centres on one odd hex row, at x = +4330 m (on the map) and -4330 m (on the
        // skirt), 4.3 km from any line, so they differ only by being on either side of the edge. 7x7-pixel patch means are
        // compared with the haze off (both sides call the same ScHazeWater, so haze continuity holds by construction, but haze
        // grows with camera distance and the two points need not be equidistant), with the grid on and off, at four yaws:
        // tolerance 0.012, as in M3.
        // One haze-on check per pair keeps the haze honest, at yaw 0 only: the pair is symmetric about the look-at point, so
        // both points are equally far from the camera and the 0.012 tolerance holds. At yaw 90 the camera distances differ by
        // up to 5.6 km; with the correct sRGB slopes at deep water (about 10.0, 3.0, 1.8) haze alone adds about 0.015, which
        // would leave too little margin under any fixed limit (plan re-review 2, R2-1), so no yaw-90 haze-on check is made.
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        var map = Object.FindFirstObjectByType<MapScene>();
        map.Build();
        yield return null;
        var cam = Camera.main; var rig = cam.GetComponent<CameraRig>();
        var size = map.Terrain.terrainData.size; var origin = map.Terrain.transform.position;
        float hex = map.Hexes.hex_size_km * 1000f;
        // On odd row r, centre x = 8660 q + 4330 r: q = -(r - 1) / 2 gives +4330, q = -(r + 1) / 2 gives -4330.
        (Vector3 inside, Vector3 beyond) Pair(int r) =>
            (HexGrid.ToWorld(HexGrid.Centre(-(r - 1) / 2, r, hex), origin, size.z), HexGrid.ToWorld(HexGrid.Centre(-(r + 1) / 2, r, hex), origin, size.z));
        int middleRow = 2 * Mathf.RoundToInt((1f - 0.55f) * size.z / (1.5f * hex) / 2f) + 1;   // an odd row near 55% of the length
        var pairs = new[] { Pair(middleRow), Pair(1) };                                      // the west coast and the north-west corner
        float Diff(Vector3 inside, Vector3 beyond, float yaw, string what)
        {
            var y = map.Terrain.SampleHeight(inside);
            var a3 = new Vector3(inside.x, y, inside.z); var b3 = new Vector3(beyond.x, y, beyond.z);
            rig.LookAt = (a3 + b3) / 2; rig.Distance = 120000f; rig.Yaw = yaw; rig.TopInset = 0; rig.Apply();
            var px = new Vector2[2];
            var s = RenderSample.Render(cam, 1600, 900, new[] { a3, b3 }, px);
            Color a = s.Mean((int)px[0].x, (int)px[0].y, 3), b = s.Mean((int)px[1].x, (int)px[1].y, 3);
            var diff = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
            TestContext.WriteLine($"seam {what} yaw {yaw} at {inside}: terrain sea {a}, beyond {b}, difference {diff:0.000}");
            return diff;
        }
        try
        {
            foreach (var (inside, beyond) in pairs)
            {
                Assert.AreEqual(4330f, inside.x - origin.x, 1f); Assert.AreEqual(-4330f, beyond.x - origin.x, 1f);
                Assert.AreEqual(inside.z, beyond.z, 1e-3f, "one hex row");
            }
            VisualToggles.Set("haze", false);
            foreach (var grid in new[] { true, false })
            {
                VisualToggles.Set("grid", grid);
                foreach (var yaw in new[] { 0f, 90f, 180f, 270f })
                    foreach (var (inside, beyond) in pairs)
                        Assert.Less(Diff(inside, beyond, yaw, $"grid={grid} haze=off"), 0.012f, $"grid={grid} yaw {yaw}");
            }
            VisualToggles.Set("haze", true); VisualToggles.Set("grid", true);
            foreach (var (inside, beyond) in pairs)
            {
                Assert.Less(Diff(inside, beyond, 0f, "haze=on"), 0.012f, "haze on, yaw 0: equidistant");
            }
        }
        finally { VisualToggles.Set("grid", true); VisualToggles.Set("haze", true); }
    }
```

Run:
```bash
grep -rn "overlayMat\|OverlayMatPath\|HexOverlay" unity/Assets --include=*.cs
```
Expected: only the `HexOverlay` strings inside `HexGridTests.cs` and `MapTests.BuildUsesThePassedTerrainMaterialAndDrawsNoLineMesh` (asserting absence).

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=1`; `error CS1501: No overload for method 'Build' takes 3 arguments` and `error CS0103: The name 'HexGrid' does not exist in the current context`.

- [ ] **Step 3: Implement the grid**

Create `unity/Assets/Scripts/Map/HexGrid.cs`:

```csharp
#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map
{
    /// <summary>The hex grid's distance field, the C# twin of ScHexEdgeDistance (StatecraftMap.hlsl), on tools/hexgrid.py's
    /// layout: pointy-top axial hexes of circumradius size; (q, r) = (0, 0) is centred on the map's north-west corner
    /// (lon0, lat1); x grows east and y south, in metres.</summary>
    public static class HexGrid
    {
        const float Sqrt3 = 1.7320508f, HalfSqrt3 = 0.8660254f;

        public static Vector2 Centre(int q, int r, float size) => new Vector2(size * (Sqrt3 * q + HalfSqrt3 * r), size * 1.5f * r);

        /// <summary>The centre of the hex containing p (cube rounding, Red Blob Games).</summary>
        public static Vector2 NearestCentre(Vector2 p, float size)
        {
            float qf = (Sqrt3 / 3f * p.x - p.y / 3f) / size, rf = 2f / 3f * p.y / size, sf = -qf - rf;
            float q = Mathf.Round(qf), r = Mathf.Round(rf), s = Mathf.Round(sf);
            float dq = Mathf.Abs(q - qf), dr = Mathf.Abs(r - rf), ds = Mathf.Abs(s - sf);
            if (dq > dr && dq > ds) q = -r - s; else if (dr > ds) r = -q - s;
            return Centre((int)q, (int)r, size);
        }

        /// <summary>The grid point (x east, y south of the north-west corner) of a world point on a map whose terrain origin
        /// (south-west corner) is <paramref name="origin"/>.</summary>
        public static Vector2 FromWorld(Vector3 world, Vector3 origin, float lengthM) => new Vector2(world.x - origin.x, lengthM - (world.z - origin.z));

        /// <summary>The inverse of FromWorld, at the origin's height.</summary>
        public static Vector3 ToWorld(Vector2 p, Vector3 origin, float lengthM) => new Vector3(origin.x + p.x, origin.y, origin.z + lengthM - p.y);

        /// <summary>Distance (m) from p to the nearest edge of the hex containing it: the inradius at a centre, 0 on an edge.</summary>
        public static float EdgeDistance(Vector2 p, float size)
        {
            var v = p - NearestCentre(p, size);
            float m = Mathf.Max(Mathf.Abs(v.x), Mathf.Max(Mathf.Abs(0.5f * v.x + HalfSqrt3 * v.y), Mathf.Abs(0.5f * v.x - HalfSqrt3 * v.y)));
            return HalfSqrt3 * size - m;
        }
    }
}
```

In `unity/Assets/Shaders/StatecraftMap.hlsl`, replace

```hlsl
float4 _ScHazeRange;  // x rift haze begins (world y), y rift haze is full (world y), z aerial distance scale (m)
```

with

```hlsl
float4 _ScHazeRange;  // x rift haze begins (world y), y rift haze is full (world y), z aerial distance scale (m)
float4 _ScGrid;       // x land line opacity, y water line opacity, z line width (pixels), w 1 = on
```

and add before the final `#endif`:

```hlsl
// Distance (m) from p to the nearest edge of its hex: tools/hexgrid.py's pointy-top axial layout, (0, 0) at the north-west
// corner, p.x east and p.y south (HexGrid.EdgeDistance is the C# twin).
float ScHexEdgeDistance(float2 p, float size)
{
    float qf = (0.57735027 * p.x - p.y / 3.0) / size;
    float rf = 0.66666667 * p.y / size;
    float sf = -qf - rf;
    float q = round(qf), r = round(rf), s = round(sf);
    float dq = abs(q - qf), dr = abs(r - rf), ds = abs(s - sf);
    if (dq > dr && dq > ds) q = -r - s; else if (dr > ds) r = -q - s;
    float2 v = p - float2(size * (1.7320508 * q + 0.8660254 * r), size * 1.5 * r);
    float m = max(abs(v.x), max(abs(0.5 * v.x + 0.8660254 * v.y), abs(0.5 * v.x - 0.8660254 * v.y)));
    return 0.8660254 * size - m;
}

// Anti-aliased coverage (0..1) of a hex line widthPx pixels wide at mapM (metres east and north of the south-west corner):
// fwidth turns the metre distance into pixels, so the line keeps its width at every zoom.
float ScGridLine(float2 mapM, float widthPx)
{
    float d = ScHexEdgeDistance(float2(mapM.x, _ScMap.y - mapM.y), _ScMapOrigin.w);
    float aa = max(fwidth(d), 1e-3);
    return 1.0 - smoothstep(0.5 * widthPx * aa, (0.5 * widthPx + 1.0) * aa, d);
}

// The grid as drawn on both the terrain and the skirt's sea: on/off, thinner towards the overview. Light lines on land and
// water, as in the promo (ruling 2: the promo wins over M2's dark lines).
static const float3 ScGridLandSrgb = float3(0.93, 0.90, 0.80);
static const float3 ScGridWaterSrgb = float3(0.82, 0.90, 0.95);
float ScGridAmount(float2 mapM, float3 positionWS)
{
    float fade = lerp(1.0, 0.6, saturate((length(_WorldSpaceCameraPos - positionWS) - 40000.0) / 200000.0));
    return ScGridLine(mapM, _ScGrid.z) * _ScGrid.w * fade;
}
```

In `unity/Assets/Shaders/StatecraftTerrain.hlsl`, replace

```hlsl
    float elevM = (PositionWS.y - _ScMap.z) / _ScMap.w;
    float3 waterColour = ScHazeWater(ScWaterShade(coast, elevM, PositionWS, mapM), PositionWS);
```

with

```hlsl
    float elevM = (PositionWS.y - _ScMap.z) / _ScMap.w;
    float3 waterColour = ScHazeWater(ScWaterShade(coast, elevM, PositionWS, mapM), PositionWS);
    // The hex grid, light and faint on land and water, thinner towards the overview (the same as Sea.shader past the edge).
    float gridLine = ScGridAmount(mapM, PositionWS);
    ground = lerp(ground, SRGBToLinear(ScGridLandSrgb), gridLine * _ScGrid.x);
    waterColour = lerp(waterColour, SRGBToLinear(ScGridWaterSrgb), gridLine * _ScGrid.y);
```

In `unity/Assets/Shaders/Sea.shader`, replace

```hlsl
                return half4(ScHazeWater(ScWaterShade(coast, 0.0, input.positionWS, mapM), input.positionWS), 1.0);
```

with

```hlsl
                float3 c = ScHazeWater(ScWaterShade(coast, 0.0, input.positionWS, mapM), input.positionWS);
                // The hex lattice runs on past the map's edge, exactly as on the terrain's water (review I3).
                c = lerp(c, SRGBToLinear(ScGridWaterSrgb), ScGridAmount(mapM, input.positionWS) * _ScGrid.y);
                return half4(c, 1.0);
```

In `unity/Assets/Scripts/Map/MapShaderGlobals.cs`:
1. Replace `HazeRange = Shader.PropertyToID("_ScHazeRange"), WaterOn = Shader.PropertyToID("_ScWaterOn");` with `HazeRange = Shader.PropertyToID("_ScHazeRange"), WaterOn = Shader.PropertyToID("_ScWaterOn"), Grid = Shader.PropertyToID("_ScGrid");`.
2. Replace `        static Vector4 haze, hazeWater;` with:

```csharp
        /// <summary>Hex line opacity on land and water, and width in pixels (anti-aliased, the same at every zoom). Light,
        /// faint lines on land as in the promo.</summary>
        public const float GridLandOpacity = 0.25f, GridWaterOpacity = 0.35f, GridWidthPx = 1.5f;

        static Vector4 haze, hazeWater;
```

3. Replace `            Shader.SetGlobalFloat(WaterOn, 1f);` with:

```csharp
            Shader.SetGlobalVector(Grid, new Vector4(GridLandOpacity, GridWaterOpacity, GridWidthPx, 1f));
            assets.OnTeardown(VisualToggles.Register("grid", on => Shader.SetGlobalVector(Grid, new Vector4(GridLandOpacity, GridWaterOpacity, GridWidthPx, on ? 1f : 0f))));
            Shader.SetGlobalFloat(WaterOn, 1f);
```

- [ ] **Step 4: Remove the line mesh**

In `unity/Assets/Scripts/Map/MapBuilder.cs`:
1. In both `Build` overloads, delete the parameter `, Material overlayMaterial`, the line `if (overlayMaterial == null) throw new ArgumentNullException(nameof(overlayMaterial));`, and `, overlayMaterial` in the path overload's call to the other.
2. In the path overload's summary, replace `Builds the terrain (with a "HexOverlay" child) from` with `Builds the terrain from` and `(Assets/Materials/Terrain.mat,\n        /// HexOverlay.mat) passed in by the caller` with `(Assets/Materials/Terrain.mat) passed in by\n        /// the caller` (the material sentence now names one asset).
3. Delete

```csharp
            var overlay = BuildHexOverlay(frame, hexFile, data, overlayMaterial, assets);
            overlay.transform.SetParent(terrainGo.transform, false);
            Boot.DiagLog.BootPhase("hex overlay draped");
```

   (the phase line came with playtest 2, R21; there is nothing left to time).
4. Delete the whole `BuildHexOverlay` method (from its summary line `/// <summary>Line mesh of every land hex outline.` to its closing brace) and the constant `OverlayClearanceM` with its line comment. Keep `OverlaySegmentsPerEdge`, `OverlaySamplesPerSegment`, `RingVertices` and `DrapedRing`: the selection band uses them (its clearance is now `MapScene.SelectionClearanceM`).

In `unity/Assets/Scripts/Map/MapScene.cs`:
1. Replace `        [SerializeField] Material terrainMaterial, overlayMaterial, markerMaterial, selectionMaterial, seaMaterial, borderLandMaterial;` with `        [SerializeField] Material terrainMaterial, markerMaterial, selectionMaterial, seaMaterial, borderLandMaterial;`.
1a. Replace

```csharp
        /// <summary>Height of the band above the hex overlay's lines, which it covers (its material also has a depth offset).</summary>
        public const float SelectionLiftM = 20f;
```

   with

```csharp
        /// <summary>Height of the band above the highest ground under it. 30 m since visual v1 Task 8 (it was the line mesh's
        /// 120 m plus 20 m, to cover the lines; at the 6 km hero zoom that floated); the material's depth offset keeps it
        /// from z-fighting the terrain.</summary>
        public const float SelectionClearanceM = 30f;
```

   and `clearance = MapBuilder.OverlayClearanceM + SelectionLiftM;` with `clearance = SelectionClearanceM;`.
2. Replace the `Configure` method with:

```csharp
        public void Configure(Material terrain, Material marker, Material selection, Material sea, Material borderLand)
        {
            terrainMaterial = terrain; markerMaterial = marker; selectionMaterial = selection;
            seaMaterial = sea; borderLandMaterial = borderLand;
        }
```

3. Delete the line `            if (overlayMaterial == null) throw new InvalidOperationException("MapScene: overlayMaterial is not assigned");`.
4. Replace `            var terrainGo = MapBuilder.Build(meta, heights, Hexes, terrainMaterial, overlayMaterial);` with `            var terrainGo = MapBuilder.Build(meta, heights, Hexes, terrainMaterial);`.

In `unity/Assets/Editor/MapMaterials.cs`, delete `        public const string OverlayPath = Folder + "/HexOverlay.mat";` and the line `            Make(OverlayPath, "Universal Render Pipeline/Unlit", new Color(0.15f, 0.12f, 0.08f, 1f));`.

In `unity/Assets/Editor/MapCapture.cs`, replace `map.Configure(Load(MapMaterials.TerrainPath), Load(MapMaterials.OverlayPath), Load(MapMaterials.MarkerPath),` with `map.Configure(Load(MapMaterials.TerrainPath), Load(MapMaterials.MarkerPath),`.

Run:
```bash
sed -i '' '/^  overlayMaterial: /d' unity/Assets/Map.unity
git rm -q unity/Assets/Materials/HexOverlay.mat unity/Assets/Materials/HexOverlay.mat.meta
grep -rn "overlayMaterial\|OverlayPath\|OverlayClearanceM\|SelectionLiftM\|hex overlay draped" unity/Assets --include=*.cs; grep -c "overlayMaterial" unity/Assets/Map.unity
```
Expected: no `.cs` matches except `HexGridTests` (`FindProperty("overlayMaterial")`, asserting absence); `0` for `Map.unity`.

- [ ] **Step 5: Run the tests and confirm they pass**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=0`; total = **(Task 7 total) + 4** (6 new, the 2 drape tests deleted), `failed="0"`; `edge luma change` above 0.03; two `grid covers` lines inside 0.05%–25%; sixteen haze-off `seam` lines under 0.012 and two haze-on lines (yaw 0, under 0.012; no yaw-90 haze-on check, re-review 2 R2-1); `TheRiftBelowSeaLevelIsVisible` sees its hex lines (`line change` above 0.03); the selection test passes at the 30 m clearance; no warnings. If `TheTerrainShaderCompilesForWebGL2` or `TheWaterShadersCompileForWebGL2` now reports a derivative (`fwidth`/`ddx`) error in the terrain graph's GLES subshader (Shader Graph builds it at `#pragma target 2.0`; review minor 10), add `#pragma require derivatives` as the first line after the include guard of `StatecraftMap.hlsl` (the Custom Function's pragmas are used, `m_FunctionSourceUsePragmas`), re-import (`uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -logFile "$PWD/unity-import.log"`) and re-run.

- [ ] **Step 6: Profile and run the art checkpoint (non-blocking)**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t08-grid
```
Expected: `PROFILE summary visual=-grid` lines (budget 2.5 ms plus the larger spread; the line mesh's draw call is gone); checkpoint passes. Then the boot budget (the overlay's draping phase is gone, so this should improve):
```bash
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 --baseline 443 --baseline-first-frame 3547 > "$PWD/unity-boot-t08.log"; echo exit=$?; grep "BOOT .*median\|OVER BUDGET" unity-boot-t08.log
```
Expected: `exit=0`. Record the summary lines and the boot line under `## Task 8: grid` in `docs/visual-v1.md` with the checkpoint row.

- [ ] **Step 7: Commit**

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add unity/Assets/Scripts/Map/HexGrid.cs unity/Assets/Scripts/Map/HexGrid.cs.meta \
  unity/Assets/Shaders/StatecraftMap.hlsl unity/Assets/Shaders/StatecraftTerrain.hlsl unity/Assets/Shaders/Sea.shader \
  unity/Assets/Scripts/Map/MapShaderGlobals.cs unity/Assets/Scripts/Map/MapBuilder.cs unity/Assets/Scripts/Map/MapScene.cs \
  unity/Assets/Editor/MapMaterials.cs unity/Assets/Editor/MapCapture.cs unity/Assets/Map.unity \
  unity/Assets/Tests/EditMode/HexGridTests.cs unity/Assets/Tests/EditMode/HexGridTests.cs.meta \
  unity/Assets/Tests/EditMode/MapTests.cs unity/Assets/Tests/EditMode/SettlementTests.cs \
  unity/Assets/Tests/EditMode/MapActionUiTests.cs unity/Assets/Tests/EditMode/GameSceneTests.cs docs/visual-v1.md
git status --short   # also shows the staged deletion of HexOverlay.mat and its .meta
git commit -m "feat(visual): the hex grid is drawn by the terrain and sea shaders, light and anti-aliased at 1.5 px; the lifted line mesh is gone

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Phase 1, Track C: labels (Task 9)

### Task 9: Map labels in EB Garamond, crisp above the tilt-shift, never overlapping

*Track C, worktree `vis-labels`. Effort 7–9 h.* Research §0.7 and §1 "Labels": settlement names are Arial-like `TextMesh` objects in the 3D scene. They move to UI Toolkit (controller question 2): one `Label` per settlement in a full-screen, click-through layer under the panels, styled as the promo's dark pill with a thin gold edge in the shipped EB Garamond SemiBold. Screen-space labels sit above the post-processing, so the tilt-shift never blurs them. The dense Tel Aviv–Lod cluster keeps M2's anchors (labels extend away from their neighbours) and Ramla's westward nudge.

Review I5 shapes the presenter:
- **No lag:** `GameRoot.LateUpdate` refreshes the labels, after `CameraController.Update` has moved the camera, so pills never trail it by a frame.
- **No layout pass per frame:** each pill sits in a zero-size absolute slot whose inline `translate` (pixels) carries it to its marker; the anchor class's percentage `translate` stays on the pill. Changing a translate does not dirty layout. `Refresh` also returns early when the camera's view-projection, the panel size and the zoom have not changed.
- **Declutter:** pills are placed in priority order (city, then town and Arab town, then the rest, each in file order); a pill that would overlap an already placed one (its last laid-out size plus a 4 px margin) is hidden, and villages, kibbutzim and moshavim are hidden beyond 150 km. Hiding uses `visibility`, not `display`, so a hidden pill keeps its size and the choice never flickers.
- **Tests on layout bounds:** M2's cluster test comes back as a `[UnityTest]` on `worldBound` at 90 km and at the overview, and the position test checks the pill against an independent projection (the marker box's own top through `Camera.WorldToScreenPoint` and the panel's scale), not against the presenter's own call.

The playtest-2 loading state (`Game.uxml` root `class="root loading"`, R22) also hides the layer until the game is ready. The new USS rules are appended after the loading block (R23) and use only new selectors (`.map-labels`, `.map-label-slot`, `.map-label*`, `.anchor-*`), so they cannot collide with the event-card restyle. Known limit (review minor 13): the presenter reads the marker anchors when it is created; a later `MapScene.Build` would leave them stale. Only the boot builds the map today, and `GameRoot` recreates the presenter on every `Begin`.

**Files:**
- Create: `unity/Assets/Scripts/UI/MapLabelsPresenter.cs`, `unity/Assets/Tests/EditMode/MapLabelTests.cs`
- Modify: `unity/Assets/Scripts/Map/SettlementPlacer.cs`, `unity/Assets/Scripts/Map/MapScene.cs` (one property, one line), `unity/Assets/Scripts/UI/GameRoot.cs`, `unity/Assets/UI/Game.uxml`, `unity/Assets/UI/Statecraft.uss`, `unity/Assets/Tests/EditMode/SettlementTests.cs` (two tests out), `docs/visual-v1.md`

**Interfaces:**
- Consumes: `MapScene.{Frame, Terrain}`, `CameraRig.Distance`, `VisualToggles`, `UiTestKit`, `UiPaths`, `GameRoot.Begin`/`Shutdown` (R10).
- Produces: `MapScene.Settlements` (`IReadOnlyList<Settlement>`); `SettlementPlacer.MarkerHeightM(string type)`; `MapLabelsPresenter : IDisposable { const string ContainerName = "map-labels", SlotClass = "map-label-slot", LabelClass = "map-label"; const float LiftPx = 6f, MarginPx = 4f, DeclutterDistanceM = 150000f; MapLabelsPresenter(VisualElement root, MapScene map, Camera camera, Func<float>? zoomDistance = null); IReadOnlyList<Label> Labels; IReadOnlyList<VisualElement> Slots; void Refresh(bool force = false); static Vector3 Anchor(MapScene map, Settlement s); static int Priority(string type); static bool HiddenByZoom(string type, float distance); static Rect PillRect(Vector2 at, Vector2 size, string anchorClass); }`; `GameRoot.LateUpdate`; toggle `"labels"`; USS classes `map-labels`, `map-label-slot`, `map-label`, `map-label-<type>`, `anchor-lower-centre|left|right`.

- [ ] **Step 1: Write the failing tests**

Create `unity/Assets/Tests/EditMode/MapLabelTests.cs`:

```csharp
#nullable enable
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using Statecraft.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/// <summary>Plan Task 9: settlement labels as UI Toolkit pills in EB Garamond, decluttered and on their markers.</summary>
public class MapLabelTests
{
    MapScene map = null!;
    Camera cam = null!;
    CameraRig rig = null!;

    [SetUp]
    public void Open()
    {
        EditorSceneManager.OpenScene("Assets/Map.unity", OpenSceneMode.Single);
        map = Object.FindFirstObjectByType<MapScene>(); map.Build();
        cam = Camera.main; rig = cam.GetComponent<CameraRig>();
        Pose(34.85, 32.0, 90000f);
    }

    [TearDown]
    public void Close() { cam.targetTexture = null; EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); VisualToggles.Clear(); }

    void Pose(double lon, double lat, float distance)
    {
        rig.LookAt = map.Frame.ToWorld(lon, lat, 0); rig.Distance = distance; rig.Yaw = 0; rig.TopInset = 0; rig.Apply();
    }

    static bool Shown(VisualElement slot) => slot.style.visibility.value == Visibility.Visible;

    [Test]
    public void EverySettlementHasOneGaramondPillThatIgnoresThePointer()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        var names = map.Settlements.Select(s => s.Name).ToArray();
        CollectionAssert.AreEqual(names, labels.Labels.Select(l => l.text).ToArray());
        var container = ui.Q<VisualElement>(MapLabelsPresenter.ContainerName);
        Assert.AreEqual(names.Length, container.childCount);
        foreach (var slot in labels.Slots) Assert.AreEqual(PickingMode.Ignore, slot.pickingMode);
        foreach (var l in labels.Labels)
        {
            Assert.IsTrue(l.ClassListContains(MapLabelsPresenter.LabelClass), l.text);
            Assert.AreEqual(PickingMode.Ignore, l.pickingMode, l.text + " must not catch map clicks");
        }
        Assert.AreEqual(PickingMode.Ignore, container.pickingMode);
        VisualToggles.Set("labels", false);
        Assert.AreEqual(DisplayStyle.None, container.style.display.value, "the labels toggle hides the layer");
        VisualToggles.Set("labels", true);
        Assert.AreEqual(DisplayStyle.Flex, container.style.display.value);
    }

    [Test]
    public void LabelsOffScreenAreHiddenAndComeBack()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        var tlv = labels.Slots.Single(s => s.name == "slot-tel_aviv");
        Pose(35.5, 33.2, CameraController.MinDistance);   // the far north, close up
        labels.Refresh(force: true);
        Assert.IsFalse(Shown(tlv), "Tel Aviv is off screen");
        Pose(34.85, 32.0, 90000f);
        labels.Refresh(force: true);
        Assert.IsTrue(Shown(tlv));
    }

    [Test]
    public void TheDenseClusterKeepsItsAnchorsAndMinorPlacesGiveWayAtTheOverview()
    {
        using var ui = new UiTestKit();
        using var labels = new MapLabelsPresenter(ui.Root, map, cam);
        string AnchorOf(string id) => labels.Labels.Single(l => l.name == "label-" + id).GetClasses().Single(c => c.StartsWith("anchor-"));
        Assert.AreEqual("anchor-lower-right", AnchorOf("jaffa")); Assert.AreEqual("anchor-lower-right", AnchorOf("rishon_lezion"));
        Assert.AreEqual("anchor-lower-left", AnchorOf("lod")); Assert.AreEqual("anchor-lower-left", AnchorOf("petah_tikva"));
        Assert.AreEqual("anchor-lower-centre", AnchorOf("tel_aviv"));
        var ramla = map.Settlements.Single(s => s.Id == "ramla");
        Assert.AreEqual(map.Frame.ToWorld(ramla.Lon, ramla.Lat, 0).x - 1500f, MapLabelsPresenter.Anchor(map, ramla).x, 1f, "Ramla's label nudged west");
        Assert.Less(MapLabelsPresenter.Priority("city"), MapLabelsPresenter.Priority("town"));
        Assert.AreEqual(MapLabelsPresenter.Priority("town"), MapLabelsPresenter.Priority("arab_town"));
        Assert.Less(MapLabelsPresenter.Priority("arab_town"), MapLabelsPresenter.Priority("kibbutz"));
        foreach (var minor in new[] { "village", "kibbutz", "moshav" })
        {
            Assert.IsFalse(MapLabelsPresenter.HiddenByZoom(minor, 90000f), minor + " at 90 km");
            Assert.IsTrue(MapLabelsPresenter.HiddenByZoom(minor, CameraController.MaxDistance), minor + " at the overview");
        }
        Assert.IsFalse(MapLabelsPresenter.HiddenByZoom("town", CameraController.MaxDistance));
        Assert.AreEqual(new Rect(90, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-centre"));
        Assert.AreEqual(new Rect(100, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-left"));
        Assert.AreEqual(new Rect(80, 70, 20, 10), MapLabelsPresenter.PillRect(new Vector2(100, 80), new Vector2(20, 10), "anchor-lower-right"));
    }

    [Test]
    public void RebuildingKeepsOneLabelPerSettlement()
    {
        using var ui = new UiTestKit();
        var first = new MapLabelsPresenter(ui.Root, map, cam);
        first.Dispose();
        Assert.AreEqual(0, ui.Q<VisualElement>(MapLabelsPresenter.ContainerName).childCount, "dispose clears");
        map.Build();
        using var second = new MapLabelsPresenter(ui.Root, map, cam);
        using var third = new MapLabelsPresenter(ui.Root, map, cam);
        Assert.AreEqual(map.Settlements.Count, ui.Q<VisualElement>(MapLabelsPresenter.ContainerName).childCount);
    }

    /// <summary>Behavioural, not text (review N4/N6): with the real theme, a laid-out pill resolves to EB Garamond SemiBold on
    /// the navy fill with the gold edge, and the whole layer is display:none while the root carries "loading" (the slots'
    /// inline visibility cannot override that), and back once it is removed.</summary>
    [UnityTest]
    public IEnumerator ThePillIsEbGaramondOnNavyWithAGoldEdgeAndHiddenWhileLoading()
    {
        var ui = new UiTestKit();
        var labels = new MapLabelsPresenter(ui.Root, map, cam);
        try
        {
            var root = ui.Q<VisualElement>("root");
            var container = ui.Q<VisualElement>(MapLabelsPresenter.ContainerName);
            Assert.IsTrue(root.ClassListContains("loading"), "Game.uxml starts loading");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(DisplayStyle.None, container.resolvedStyle.display, "hidden while the game loads");
            root.RemoveFromClassList("loading");
            for (int i = 0; i < 10; i++) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, container.resolvedStyle.display, "shown once ready");
            var pill = labels.Labels.Single(l => l.name == "label-tel_aviv");
            var font = pill.resolvedStyle.unityFontDefinition;
            var fontName = font.font != null ? font.font.name : font.fontAsset != null ? font.fontAsset.name : "";
            StringAssert.Contains("EBGaramond-SemiBold", fontName);
            var bg = pill.resolvedStyle.backgroundColor;
            Assert.AreEqual(14f / 255f, bg.r, 0.01f); Assert.AreEqual(22f / 255f, bg.g, 0.01f); Assert.AreEqual(38f / 255f, bg.b, 0.01f);
            Assert.AreEqual(0.82f, bg.a, 0.01f);
            var gold = pill.resolvedStyle.borderTopColor;
            Assert.AreEqual(196f / 255f, gold.r, 0.01f); Assert.AreEqual(160f / 255f, gold.g, 0.01f); Assert.AreEqual(92f / 255f, gold.b, 0.01f);
            Assert.AreEqual(1f, pill.resolvedStyle.borderTopWidth, 0.01f);
            Assert.IsNull(GameObject.Find("tel_aviv_label"), "no world-space TextMesh labels remain");
        }
        finally { labels.Dispose(); ui.Dispose(); }
    }

    static IEnumerator Settle(MapLabelsPresenter labels)
    {
        for (int i = 0; i < 20 && labels.Labels.Any(l => float.IsNaN(l.resolvedStyle.width) || l.resolvedStyle.width <= 0); i++) yield return null;
        labels.Refresh(force: true); yield return null;   // the first refresh with sizes declutters
        labels.Refresh(force: true); yield return null;
    }

    /// <summary>Independent of the presenter's own projection call: the Tel Aviv pill's bottom centre is within 2 px of its
    /// marker box's top, projected with Camera.WorldToScreenPoint and the panel's scale, LiftPx above it.</summary>
    [UnityTest]
    public IEnumerator APillSitsJustAboveItsMarkerTop()
    {
        var rt = new RenderTexture(1920, 1080, 0); var camRt = new RenderTexture(1920, 1080, 24);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
        var go = new GameObject("LabelPanel"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        cam.targetTexture = camRt;
        var root = doc.rootVisualElement; root.Q("root")?.RemoveFromClassList("loading");
        var labels = new MapLabelsPresenter(root, map, cam);
        try
        {
            yield return Settle(labels);
            var box = GameObject.Find("Settlements").transform.Find("tel_aviv");
            var top = box.position + Vector3.up * box.lossyScale.y / 2;
            var p = cam.WorldToScreenPoint(top);                       // pixels of the 1920x1080 camera target, y up
            float scale = root.worldBound.width / 1920f;
            var expected = new Vector2(p.x * scale, (1080f - p.y) * scale - MapLabelsPresenter.LiftPx);
            var wb = labels.Labels.Single(l => l.name == "label-tel_aviv").worldBound;
            TestContext.WriteLine($"Tel Aviv pill {wb}, expected bottom centre {expected}");
            Assert.AreEqual(expected.x, wb.center.x, 2f, "centred on the marker");
            Assert.AreEqual(expected.y, wb.yMax, 2f, "LiftPx above the marker's top");
        }
        finally
        {
            labels.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            cam.targetTexture = null; camRt.Release(); Object.DestroyImmediate(camRt);
        }
    }

    static readonly string[] TelAvivCluster = { "tel_aviv", "jaffa", "petah_tikva", "rishon_lezion", "lod", "ramla", "rehovot" };

    static float Gap(Rect r, Vector2 p) => new Vector2(Mathf.Max(0, Mathf.Max(r.xMin - p.x, p.x - r.xMax)), Mathf.Max(0, Mathf.Max(r.yMin - p.y, p.y - r.yMax))).magnitude;

    /// <summary>M2's cluster test, on layout bounds (review I5): at 90 km (the flythrough's frame 120) and at the overview,
    /// no two shown pills overlap, Tel Aviv is always shown, and every shown pill of the Tel Aviv - Lod cluster is at most
    /// half as far from its own marker top as from any other cluster marker's top, so none reads as a neighbour's.</summary>
    [UnityTest]
    public IEnumerator ShownPillsNeverOverlapAndEachReadsAsItsOwn()
    {
        var rt = new RenderTexture(1920, 1080, 0); var camRt = new RenderTexture(1920, 1080, 24);
        var ps = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>(UiPaths.Panel)); ps.targetTexture = rt;
        var go = new GameObject("LabelPanel"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>(); doc.panelSettings = ps; doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiPaths.GameUxml);
        go.SetActive(true);
        cam.targetTexture = camRt;
        var root = doc.rootVisualElement; root.Q("root")?.RemoveFromClassList("loading");
        var labels = new MapLabelsPresenter(root, map, cam);
        try
        {
            foreach (var (name, lon, lat, distance) in new[] { ("90 km", 34.95, 31.3 + (32.9 - 31.3) * 120 / 239.0, 90000f), ("overview", PromoPose.Diorama02.Lon, PromoPose.Diorama02.Lat, CameraController.MaxDistance) })
            {
                Pose(lon, lat, distance);
                yield return Settle(labels);
                float scale = root.worldBound.width / 1920f;
                Vector2 MarkerTop(string id)
                {
                    var box = GameObject.Find("Settlements").transform.Find(id);
                    var p = cam.WorldToScreenPoint(box.position + Vector3.up * box.lossyScale.y / 2);
                    return new Vector2(p.x * scale, (1080f - p.y) * scale);
                }
                var shown = labels.Slots.Where(Shown).Select(s => (id: s.name.Substring("slot-".Length), rect: s.Q<Label>().worldBound)).ToList();
                TestContext.WriteLine($"{name}: {shown.Count} of {labels.Slots.Count} pills shown");
                Assert.IsTrue(shown.Any(s => s.id == "tel_aviv"), $"{name}: Tel Aviv is shown");
                for (int i = 0; i < shown.Count; i++)
                    for (int j = i + 1; j < shown.Count; j++)
                        Assert.IsFalse(shown[i].rect.Overlaps(shown[j].rect), $"{name}: {shown[i].id} {shown[i].rect} overlaps {shown[j].id} {shown[j].rect}");
                foreach (var (id, rect) in shown.Where(s => TelAvivCluster.Contains(s.id)))
                {
                    float own = Gap(rect, MarkerTop(id));
                    foreach (var other in TelAvivCluster.Where(o => o != id))
                        Assert.Greater(Gap(rect, MarkerTop(other)), 2f * own + 4f, $"{name}: {id} pill is {own:F0} px from its marker but closer to {other}'s");
                }
                if (distance > MapLabelsPresenter.DeclutterDistanceM)
                    foreach (var s in map.Settlements.Where(s => MapLabelsPresenter.HiddenByZoom(s.Type, distance)))
                        Assert.IsFalse(Shown(labels.Slots.Single(x => x.name == "slot-" + s.Id)), $"{name}: {s.Id} ({s.Type}) is decluttered");
            }
        }
        finally
        {
            labels.Dispose(); Object.DestroyImmediate(go); Object.DestroyImmediate(ps); rt.Release(); Object.DestroyImmediate(rt);
            cam.targetTexture = null; camRt.Release(); Object.DestroyImmediate(camRt);
        }
    }
}
```

In `unity/Assets/Tests/EditMode/SettlementTests.cs`, delete the tests `LabelsAreLightWithADarkShadowAboveTheMarkerFacingTheRig` and `EveryTelAvivClusterLabelIsUnambiguouslyClosestToItsOwnMarker`, then the helpers `TelAvivCluster`, `ScreenRect` and `Gap` (`grep -n "TelAvivCluster\|ScreenRect\|Gap(" unity/Assets/Tests/EditMode/SettlementTests.cs` must then print nothing; `MapLabelTests` has its own).

- [ ] **Step 2: Run them and confirm they fail**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -m2 "error CS" unity-tests.log
```
Expected: `exit=1`; `error CS0246: The type or namespace name 'MapLabelsPresenter' could not be found`.

- [ ] **Step 3: Implement**

In `unity/Assets/Scripts/Map/MapScene.cs`:
1. Replace `        public Material MarkerMaterial => markerMaterial;` with:

```csharp
        public Material MarkerMaterial => markerMaterial;
        /// <summary>The settlements the last Build placed (settlements_1948.yaml), for the UI's map labels.</summary>
        public IReadOnlyList<Settlement> Settlements { get; private set; } = new List<Settlement>();
```

2. After the line `            var list = SettlementPlacer.Parse(inputs.SettlementsYaml, (meta.lon0, meta.lon1, meta.lat0, meta.lat1), SettlementsPath);` add `            Settlements = list;`.

In `unity/Assets/Scripts/Map/SettlementPlacer.cs`:
1. Replace everything from the comment line `// Marker colour and box edge (m); label cap height (m).` through the line `static readonly Color LabelColour = new Color(0.98f, 0.97f, 0.93f), ShadowColour = new Color(0.06f, 0.05f, 0.04f, 0.9f);` with:

```csharp
        // Marker colour and box edge (m) per type: placeholder art until the settlement art track.
        static readonly Dictionary<string, (Color c, float h)> Marker = new Dictionary<string, (Color, float)>
        {
            ["city"] = (new Color(0.93f, 0.92f, 0.88f), 1800f), ["town"] = (new Color(0.88f, 0.84f, 0.76f), 1100f),
            ["kibbutz"] = (new Color(0.80f, 0.35f, 0.25f), 600f), ["moshav"] = (new Color(0.78f, 0.45f, 0.30f), 600f),
            ["arab_town"] = (new Color(0.82f, 0.74f, 0.58f), 1100f), ["village"] = (new Color(0.76f, 0.68f, 0.52f), 600f),
        };

        /// <summary>The marker box's edge (m) for a settlement type; the UI's label sits on its top.</summary>
        public static float MarkerHeightM(string type) => Marker[type].h;
```

2. Replace everything from the summary line `/// <summary>Placeholder markers (real per-era prefabs come in M3/M4)` through the end of the `MakeText` method with:

```csharp
        /// <summary>Placeholder markers (per-era art comes with the settlement art track): a box sized by type, under a returned
        /// "Settlements" root. The box is a plain MeshFilter + MeshRenderer (no collider) using the editor-made
        /// <paramref name="markerMaterial"/> (no runtime Shader.Find, M0 correction 1); the per-type colour goes through a
        /// MaterialPropertyBlock. Names are drawn by the UI (MapLabelsPresenter), not here.</summary>
        public static GameObject Place(List<Settlement> list, GeoFrame frame, Terrain terrain, Material markerMaterial)
        {
            var root = new GameObject("Settlements"); var parent = root.transform;
            var cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var block = new MaterialPropertyBlock();
            foreach (var s in list)
            {
                var p = frame.ToWorld(s.Lon, s.Lat, 0); p.y = terrain.SampleHeight(p) + terrain.transform.position.y;
                var (c, h) = Marker[s.Type];
                var box = new GameObject(s.Id); box.transform.SetParent(parent);
                box.AddComponent<MeshFilter>().sharedMesh = cube;
                var r = box.AddComponent<MeshRenderer>(); r.sharedMaterial = markerMaterial;
                block.SetColor(BaseColor, c); r.SetPropertyBlock(block);
                box.transform.position = p + Vector3.up * h / 2; box.transform.localScale = new Vector3(h, h, h);
            }
            return root;
        }
```

Create `unity/Assets/Scripts/UI/MapLabelsPresenter.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Settlement names as UI Toolkit pills (EB Garamond SemiBold on navy with a gold edge, Statecraft.uss
    /// ".map-label") in the click-through "map-labels" layer under the panels. Each pill sits in a zero-size absolute slot;
    /// Refresh (GameRoot.LateUpdate, after the camera has moved) moves each slot with an inline pixel translate to its
    /// marker's top as the camera sees it, LiftPx above it, so no layout pass runs per frame, and returns early when the
    /// camera's view-projection, the panel size and the zoom are unchanged. Pills are placed in priority order (city, then
    /// town and Arab town, then the rest, each in file order); a pill behind the camera or off screen, a minor place
    /// (village, kibbutz, moshav) beyond DeclutterDistanceM, or a pill that would overlap an already placed one (its last
    /// laid-out size plus MarginPx) is hidden with visibility, so it keeps its size and the choice does not flicker.
    /// Screen-space, so the tilt-shift never blurs them. The dense Tel Aviv - Lod cluster keeps M2's anchors and Ramla's
    /// nudge west. The anchors are read once, when the presenter is created (a later MapScene.Build needs a new presenter;
    /// GameRoot makes one per Begin).</summary>
    public sealed class MapLabelsPresenter : IDisposable
    {
        public const string ContainerName = "map-labels", SlotClass = "map-label-slot", LabelClass = "map-label";
        public const float LiftPx = 6f, MarginPx = 4f, DeclutterDistanceM = 150000f;

        static readonly HashSet<string> MinorTypes = new HashSet<string> { "village", "kibbutz", "moshav" };
        static readonly Dictionary<string, string> AnchorClass = new Dictionary<string, string>
        {
            ["jaffa"] = "anchor-lower-right", ["rishon_lezion"] = "anchor-lower-right", ["lod"] = "anchor-lower-left",
            ["petah_tikva"] = "anchor-lower-left", ["safed"] = "anchor-lower-right", ["rosh_pinna"] = "anchor-lower-left",
        };
        static readonly Dictionary<string, float> OffsetEastM = new Dictionary<string, float> { ["ramla"] = -1500f };

        sealed class Item
        {
            public Settlement Settlement = null!; public Vector3 Anchor; public VisualElement Slot = null!; public Label Label = null!;
            public string AnchorClass = "";
        }

        readonly VisualElement container;
        readonly Camera camera;
        readonly Func<float> zoom;
        readonly List<Item> items = new List<Item>();   // in placing (priority) order
        Matrix4x4 lastViewProjection; Vector2 lastPanelSize; float lastZoom = float.NaN; bool sizesKnown;

        public IReadOnlyList<Label> Labels { get; }
        public IReadOnlyList<VisualElement> Slots { get; }

        public MapLabelsPresenter(VisualElement root, MapScene map, Camera camera, Func<float>? zoomDistance = null)
        {
            container = root.Q<VisualElement>(ContainerName) ?? throw new InvalidOperationException($"Game.uxml has no '{ContainerName}' element");
            this.camera = camera;
            zoom = zoomDistance ?? (() => camera.TryGetComponent(out CameraRig rig) ? rig.Distance : 0f);
            container.Clear();
            var labels = new List<Label>(); var slots = new List<VisualElement>();
            foreach (var s in map.Settlements)
            {
                var slot = new VisualElement { name = "slot-" + s.Id, pickingMode = PickingMode.Ignore };
                slot.AddToClassList(SlotClass);
                var anchorClass = AnchorClass.TryGetValue(s.Id, out var a) ? a : "anchor-lower-centre";
                var label = new Label(s.Name) { name = "label-" + s.Id, pickingMode = PickingMode.Ignore };
                label.AddToClassList(LabelClass);
                label.AddToClassList("map-label-" + s.Type);
                label.AddToClassList(anchorClass);
                slot.Add(label);
                container.Add(slot);
                labels.Add(label); slots.Add(slot);
                items.Add(new Item { Settlement = s, Anchor = Anchor(map, s), Slot = slot, Label = label, AnchorClass = anchorClass });
            }
            // Stable sort: priority, then file order.
            var ordered = items.Select((it, i) => (it, i)).OrderBy(x => Priority(x.it.Settlement.Type)).ThenBy(x => x.i).Select(x => x.it).ToList();
            items.Clear(); items.AddRange(ordered);
            Labels = labels; Slots = slots;
            removeToggle?.Invoke();
            removeToggle = VisualToggles.Register("labels", on => container.style.display = on ? DisplayStyle.Flex : DisplayStyle.None);
            Refresh(force: true);
        }

        /// <summary>The world point a settlement's label hangs from: its marker's top, nudged east or west for a few crowded names.</summary>
        public static Vector3 Anchor(MapScene map, Settlement s)
        {
            var p = map.Frame.ToWorld(s.Lon, s.Lat, 0);
            p.x += OffsetEastM.TryGetValue(s.Id, out var dx) ? dx : 0f;
            p.y = map.Terrain.SampleHeight(p) + map.Terrain.transform.position.y + SettlementPlacer.MarkerHeightM(s.Type);
            return p;
        }

        /// <summary>Placing order: lower first. Cities, then towns and Arab towns, then villages, kibbutzim and moshavim.</summary>
        public static int Priority(string type) => type == "city" ? 0 : type == "town" || type == "arab_town" ? 1 : 2;

        public static bool HiddenByZoom(string type, float distance) => MinorTypes.Contains(type) && distance > DeclutterDistanceM;

        /// <summary>The pill's panel rectangle when its anchor point is <paramref name="at"/> (the anchor class says which corner
        /// of the pill sits there, USS translate).</summary>
        public static Rect PillRect(Vector2 at, Vector2 size, string anchorClass) => anchorClass switch
        {
            "anchor-lower-left" => new Rect(at.x, at.y - size.y, size.x, size.y),
            "anchor-lower-right" => new Rect(at.x - size.x, at.y - size.y, size.x, size.y),
            _ => new Rect(at.x - size.x / 2f, at.y - size.y, size.x, size.y),
        };

        public void Refresh(bool force = false)
        {
            var panel = container.panel;
            if (panel == null || camera == null) return;
            var viewProjection = camera.projectionMatrix * camera.worldToCameraMatrix;
            var panelSize = container.layout.size;
            float distance = zoom();
            if (!force && sizesKnown && viewProjection == lastViewProjection && panelSize == lastPanelSize && distance == lastZoom) return;
            lastViewProjection = viewProjection; lastPanelSize = panelSize; lastZoom = distance;
            sizesKnown = true;
            var placed = new List<Rect>();
            foreach (var it in items)
            {
                var v = camera.WorldToViewportPoint(it.Anchor);
                bool shown = v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f && !HiddenByZoom(it.Settlement.Type, distance);
                var at = Vector2.zero;
                if (shown)
                {
                    at = RuntimePanelUtils.CameraTransformWorldToPanel(panel, it.Anchor, camera);
                    at.y -= LiftPx;
                    var size = new Vector2(it.Label.resolvedStyle.width, it.Label.resolvedStyle.height);
                    if (float.IsNaN(size.x) || size.x <= 0f) sizesKnown = false;   // not laid out yet: show it, cull on a later refresh
                    else
                    {
                        var r = PillRect(at, size, it.AnchorClass);
                        var grown = new Rect(r.x - MarginPx, r.y - MarginPx, r.width + 2f * MarginPx, r.height + 2f * MarginPx);
                        if (placed.Any(p => p.Overlaps(grown))) shown = false; else placed.Add(r);
                    }
                }
                it.Slot.style.visibility = shown ? Visibility.Visible : Visibility.Hidden;
                if (shown) it.Slot.style.translate = new Translate(at.x, at.y);
            }
        }

        Action? removeToggle;   // the labels switch's removal token (VisualToggles.Register): a disposed presenter never removes a newer one

        public void Dispose() { removeToggle?.Invoke(); removeToggle = null; container.Clear(); items.Clear(); }
    }
}
```

In `unity/Assets/UI/Game.uxml`, replace

```xml
        <ui:Label name="loading" text="Loading the map of 1948…" class="loading-line" picking-mode="Ignore" />
```

with

```xml
        <ui:Label name="loading" text="Loading the map of 1948…" class="loading-line" picking-mode="Ignore" />
        <!-- Settlement names, positioned by MapLabelsPresenter under every panel; clicks pass through to the map. -->
        <ui:VisualElement name="map-labels" class="map-labels" picking-mode="Ignore" />
```

Append to the end of `unity/Assets/UI/Statecraft.uss` (after the playtest-2 loading block; every selector here is new: `grep -n "map-label\|anchor-" unity/Assets/UI/Statecraft.uss` prints nothing before this step):

```css
/* Map labels (visual v1 Task 9): the promo's dark pill with a thin gold edge, in EB Garamond SemiBold. Each pill sits in a
   zero-size slot that MapLabelsPresenter moves with an inline translate; the anchor class says which corner of the pill sits
   on the marker's top. Hidden while the game loads. */
.map-labels { position: absolute; left: 0; top: 0; right: 0; bottom: 0; }
.loading .map-labels { display: none; }
.map-label-slot { position: absolute; left: 0; top: 0; width: 0; height: 0; overflow: visible; }
.map-label {
    position: absolute;
    -unity-font-definition: url("project://database/Assets/UI/Fonts/EBGaramond-SemiBold.ttf");
    font-size: 18px;
    color: rgb(243, 234, 216);
    background-color: rgba(14, 22, 38, 0.82);
    border-width: 1px;
    border-color: var(--gold);
    border-radius: 12px;
    padding: 1px 10px 2px 10px;
    margin: 0;
    -unity-text-align: middle-center;
    white-space: nowrap;
}
.map-label-city { font-size: 22px; }
.map-label-town, .map-label-arab_town { font-size: 19px; }
.anchor-lower-centre { translate: -50% -100%; }
.anchor-lower-left { translate: 0 -100%; }
.anchor-lower-right { translate: -100% -100%; }
```

In `unity/Assets/Scripts/UI/GameRoot.cs`:
1. Replace `        MinimapPresenter? minimap;` with `        MinimapPresenter? minimap;\n        MapLabelsPresenter? labels;`.
2. Replace

```csharp
            var mini = minimap = Own(new MinimapPresenter(root, map.Hexes));
```

   with

```csharp
            var mini = minimap = Own(new MinimapPresenter(root, map.Hexes));
            labels = Own(new MapLabelsPresenter(root, map, cameraController.GetComponent<Camera>()));
```

3. Replace `            minimap = null; rig = null; drawn = null; logCamera = false;` with `            minimap = null; labels = null; rig = null; drawn = null; logCamera = false;`.
4. Replace

```csharp
        void Update()
        {
            SyncMinimap();
```

   with

```csharp
        /// <summary>After every Update (CameraController moves the camera in its Update), so the pills never trail the camera.</summary>
        void LateUpdate() => labels?.Refresh();

        void Update()
        {
            SyncMinimap();
```

- [ ] **Step 4: Run the tests and confirm they pass**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Tel Aviv pill|pills shown" unity-tests.log
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=0`; total = **B1 + 5** (7 new, 2 removed), `failed="0"`; the `Tel Aviv pill` line and two `pills shown` lines; no warnings. `UiFoundationTests`/`UiLayoutTests` still pass (the new layer is absolute and click-through, so it changes no layout and no picking). If `ShownPillsNeverOverlapAndEachReadsAsItsOwn` fails at 90 km on the cluster criterion for a shown pill, tune that settlement's anchor class (M2's method) rather than the criterion.

- [ ] **Step 5: Profile and run the art checkpoint (non-blocking)**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t09-labels
```
Expected: `PROFILE summary visual=-labels` lines, with `median(all) - median(-labels) <= 1 ms` plus the larger spread (review I10: priced against its own toggle, not against an old baseline); the checkpoint's `gameplay03.png` shows serif pills over Tel Aviv, Jaffa, Lod and Ramla, none overlapping, each touching its own marker. Record under `## Task 9: labels` with the checkpoint row.

- [ ] **Step 6: Commit**

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add unity/Assets/Scripts/UI/MapLabelsPresenter.cs unity/Assets/Scripts/UI/MapLabelsPresenter.cs.meta \
  unity/Assets/Scripts/UI/GameRoot.cs unity/Assets/Scripts/Map/SettlementPlacer.cs unity/Assets/Scripts/Map/MapScene.cs \
  unity/Assets/UI/Game.uxml unity/Assets/UI/Statecraft.uss docs/visual-v1.md \
  unity/Assets/Tests/EditMode/MapLabelTests.cs unity/Assets/Tests/EditMode/MapLabelTests.cs.meta \
  unity/Assets/Tests/EditMode/SettlementTests.cs
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(ui): settlement labels as decluttered EB Garamond pills in UI Toolkit, crisp above the tilt-shift; TextMesh labels removed

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

## Merge point

On `visual-v1`, after Tasks 2–9 are approved (each track's PRs to `main` follow this merge's checks):

```bash
git merge --no-ff vis-ground -m "merge: visual v1 track B (ground and water)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git merge --no-ff vis-light -m "merge: visual v1 track A (light and lens)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
git merge --no-ff vis-labels -m "merge: visual v1 track C (labels)

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

Expected conflicts and their resolution (keep both sides): `SettlementTests.cs` (A renamed `RebuildingDoesNotLeakVolumeComponents`, B changed `Configure` calls and the seam, rift and selection tests, C removed two label tests: all different hunks); `MapScene.cs` (B's five-material `Configure`, builder call and `SelectionClearanceM`, C's `Settlements` property and assignment); `GameRoot.cs` (Task 1's flag lines, C's labels field, `Own(...)` line, `LateUpdate` and `Shutdown` line); `docs/visual-v1.md` (append every track's sections and checkpoint rows). Then:

```bash
tools/build_sim_for_unity.sh
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
uv run --with pytest==9.1.1 --with rasterio==1.5.2 --with numpy==2.5.3 --with pyyaml==6.0.3 --with pillow==12.3.0 --with requests==2.32.5 pytest tools/tests -q | tail -1
dotnet test sim/Statecraft.sln 2>&1 | grep -E "Passed!|Failed!"
git diff --stat <task-base> -- sim content    # prints nothing
tools/art_checkpoint.sh merge
```
Expected: Unity `exit=0`, total **B1 + 47** (Track A 11 + 6 + 6, Track B 9 + 6 + 4, Track C 5), `failed="0"`. Every render test now runs with every feature: Track B's colour thresholds were derived with Track A's sun and an estimate of its grade (Task 7 Step 4), Track A's shadow test expects shadows on water (Task 7, review I6), and Track A's readability tests now see the real terrain. A failure here is a real interaction and goes back to the owning track. Tools **T1 + 15**; sim unchanged from task-base (`Passed!`); the merged checkpoint passes golden and shots.

## Phase 2, Track D: tiers (Task 10)

### Task 10: WebGL quality-tier review: what runs on the web, what is desktop-only, with evidence

*`visual-v1`, after the merge point. Effort 7–9 h.* Research §2.3 proposed tiers without measurements. This task measures every feature on the shipped build (headless and headed Chrome at DPR 1, WebGL 2, three interleaved repeats per configuration under the exclusive machine lock), a headed DPR-2 run as the fill-rate stress proxy for weaker GPUs (four times the pixels; review I1), a macOS IL2CPP player for the desktop tier, and SwiftShader as the weakest WebGL 2 there is (correctness only); applies the decisions to the URP assets through one editor script whose constants are the decisions (review minor 11 notes that the tests alone would pin the assets; the script is kept so a decision change is one constant and one command); pins them with tests; and records the evidence table in `docs/visual-v1.md`. A cost counts as over a budget only when `median(all) − median(-feature)` exceeds the budget plus the larger spread.

**Decision rules** (proposed budgets from Task 1, ratified here; each decision cites the measured lines):

| Setting | Web (WebGL 2, `Mobile_*`) | Desktop (`PC_*`) | Rule |
|---|---|---|---|
| Rendering path | Forward (unchanged) | Forward+ (unchanged) | Research §2.1: Forward+ on WebGL is reported to freeze and break decals |
| Depth and opaque textures | off | off | Nothing in visual v1 samples them (tilt-shift is screen-space, water uses the coast texture); the retired Gaussian DoF was the only reader. Off saves a depth copy and an opaque copy |
| Render scale | 0.8, or 1.0 if all hold on a build at 1.0: headless `all` median p95 ≤ 16.7 ms at both poses, headed (DPR 1) `all` median p95 ≤ 20 ms, and the headed DPR-2 stress run's `all` median p95 ≤ 33 ms | 1.0 | Sharper 1:1 text and lines when the budget allows; the DPR-2 run stands in for a GPU with a quarter of the fill rate |
| Main-light shadows | 1 cascade, 2048, hard | 4 cascades, 2048, soft | Drop the web map to 1024 if `median(all) − median(-shadows)` exceeds 2.5 ms plus the larger spread |
| Tilt-shift | half resolution (downsample 2); 3 if its cost is over 1.5 ms | half resolution | Same look on both; the budget sets the web downsample |
| SSAO | none | **off** | Its radius is in metres (0.3 m today) and cannot reach km-scale relief; the baked AO replaces it on both tiers. Re-evaluate for buildings in the art track |
| Grade (with the Neutral tonemapper), vignette, haze, grid, water, labels | on | on | LUT grading and analytic shader terms; on unless a feature's cost exceeds its budget (2.5 ms; labels 1 ms) plus the larger spread |
| Baked shading texture | block-compressed (Unity's WebGL default) | block-compressed | Review minor 9: desktop browsers sample DXT natively; a mobile browser without DXT support decompresses it at load (memory, not quality). Mobile browsers are outside the 1920×1080 target; recorded, not changed |
| WebGPU, GPU Resident Drawer, compute | not used | not used | WebGL 2 has no compute; WebGPU is an M5 evaluation (research §2.1) |

**Files:**
- Create: `unity/Assets/Scripts/Map/QualityTierValues.cs`, `unity/Assets/Editor/QualityTiers.cs`, `unity/Assets/Tests/EditMode/QualityTierTests.cs`
- Modify (by `QualityTiers.Apply`): `unity/Assets/Settings/Mobile_RPAsset.asset`, `unity/Assets/Settings/PC_RPAsset.asset`, `unity/Assets/Settings/Mobile_Renderer.asset`, `unity/Assets/Settings/PC_Renderer.asset`; and `docs/visual-v1.md`, `docs/dev-setup.md`

**Interfaces:**
- Consumes: `TiltShiftFeature.Downsample`, `?vprofile`/`-vprofile`, `web_check.py --swiftshader/--shots`, `headed.py profile --dpr` and `boot --runs --baseline --cpu`, `Build.Web`, `Build.Mac`.
- Produces: `QualityTierValues { const float WebRenderScale; const int WebShadowMap, WebTiltShiftDownsample, DesktopShadowMap, DesktopTiltShiftDownsample; }` (runtime assembly, so tests can read it); `QualityTiers { const string WebAsset, DesktopAsset, WebRenderer, DesktopRenderer; static void Apply(); }`; the "Tier decisions" table in `docs/visual-v1.md`.

- [ ] **Step 1: Measure the merged build**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
grep "BUILD " unity-build-web.log
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE" | tee "$PWD/unity-vprofile-headless.log"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py profile --build unity/Builds/Web-disabled --dpr 1 | grep "PROFILE" | tee "$PWD/unity-vprofile-headed.log"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py profile --build unity/Builds/Web-disabled --dpr 2 | grep "PROFILE" | tee "$PWD/unity-vprofile-dpr2.log"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 --baseline 443 --baseline-first-frame 3547 > "$PWD/unity-boot-t10.log"; echo exit=$?; grep "BOOT .*median\|OVER BUDGET" unity-boot-t10.log
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py boot --build unity/Builds/Web-disabled --runs 3 --cpu 4 --baseline 1813 --baseline-first-frame 6963 > "$PWD/unity-boot-t10-cpu4.log"; echo exit=$?; grep "BOOT .*median\|OVER BUDGET" unity-boot-t10-cpu4.log   # low-end boot proxy (4x CPU throttling), budgeted by the same rule
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile "$PWD/unity-build-mac.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
app="$(ls -d unity/Builds/Mac/Statecraft.app/Contents/MacOS/* | head -1)"
uv run tools/machine_lock.py --exclusive --wait-unity -- "$app" -vprofile -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile "$PWD/unity-mac-vprofile.log"; grep "PROFILE summary" unity-mac-vprofile.log
"$app" -golden -logFile "$PWD/unity-mac-golden.log"; echo exit=$?; grep "GOLDEN OK" unity-mac-golden.log
```
Expected: each profile run prints, per pose, three repeats of `all`, one `-<name>` per toggle (`-grade`, `-grid`, `-haze`, `-labels`, `-shadows`, `-tiltshift`, `-water`) and `none`, each line with `res=` and `scale=`, then the summary lines and `PROFILE visual done`. The headless and headed DPR-1 runs show `res=1920x1080` and the DPR-2 run is expected to show `res=3840x2160` (the tool fails on any other size). Both boot runs exit 0 (both spans within baseline + max(300 ms, 50%)); the throttled run is the low-end boot proxy. The macOS player quits by itself after its profile; `-golden` exits 0 with `GOLDEN OK 1950 71e9904d…` and `GOLDEN OK 1952 73590e32…`.

- [ ] **Step 2: Measure the render-scale candidate**

Temporarily set `m_RenderScale: 1` in `unity/Assets/Settings/Mobile_RPAsset.asset` (do not stage it), then:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -out Builds/Web-scale1 -logFile "$PWD/unity-build-web-scale1.log"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-scale1 --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary visual=all"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py profile --build unity/Builds/Web-scale1 --dpr 1 | grep "PROFILE summary visual=all"
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/headed.py profile --build unity/Builds/Web-scale1 --dpr 2 | grep "PROFILE summary visual=all"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
```
Expected: six `PROFILE summary visual=all` lines (two poses: headless, headed, headed DPR 2). Apply the render-scale rule from the table; the result is `WebRenderScale` in Step 4 (0.8 or 1.0).

- [ ] **Step 3: Write the failing tests**

Create `unity/Assets/Tests/EditMode/QualityTierTests.cs`:

```csharp
#nullable enable
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Plan Task 10: the tier decisions recorded in docs/visual-v1.md, pinned on the URP assets.</summary>
public class QualityTierTests
{
    static SerializedObject Asset(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path));

    [Test]
    public void TheWebTierIsForwardWithoutDepthOrOpaqueCopiesAndOneHardCascade()
    {
        var web = Asset("Assets/Settings/Mobile_RPAsset.asset");
        Assert.IsFalse(web.FindProperty("m_RequireDepthTexture").boolValue, "no depth copy on the web");
        Assert.IsFalse(web.FindProperty("m_RequireOpaqueTexture").boolValue, "no opaque copy on the web");
        Assert.AreEqual(1, web.FindProperty("m_ShadowCascadeCount").intValue);
        Assert.AreEqual(QualityTierValues.WebShadowMap, web.FindProperty("m_MainLightShadowmapResolution").intValue);
        Assert.IsFalse(web.FindProperty("m_SoftShadowsSupported").boolValue);
        Assert.AreEqual(QualityTierValues.WebRenderScale, web.FindProperty("m_RenderScale").floatValue, 1e-4f);
        var renderer = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset"));
        Assert.AreEqual(0, renderer.FindProperty("m_RenderingMode").intValue, "Forward on WebGL (research notes 2.1)");
        StringAssert.IsMatch(@"(?m)^\s+WebGL: 0\s*$", System.IO.File.ReadAllText("ProjectSettings/QualitySettings.asset"), "WebGL runs the Mobile tier (level 0)");
    }

    [Test]
    public void TheDesktopTierHasSoftCascadesAndNoSsao()
    {
        var pc = Asset("Assets/Settings/PC_RPAsset.asset");
        Assert.AreEqual(4, pc.FindProperty("m_ShadowCascadeCount").intValue);
        Assert.IsTrue(pc.FindProperty("m_SoftShadowsSupported").boolValue);
        Assert.IsFalse(pc.FindProperty("m_RequireDepthTexture").boolValue);
        Assert.IsFalse(pc.FindProperty("m_RequireOpaqueTexture").boolValue);
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        foreach (var f in data.rendererFeatures.Where(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion"))
            Assert.IsFalse(f.isActive, "SSAO off: the baked AO replaces it");
    }

    [Test]
    public void BothTiersRunTheTiltShiftAtTheDecidedResolution()
    {
        foreach (var (path, downsample) in new[] { ("Assets/Settings/Mobile_Renderer.asset", QualityTierValues.WebTiltShiftDownsample), ("Assets/Settings/PC_Renderer.asset", QualityTierValues.DesktopTiltShiftDownsample) })
        {
            var f = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path).rendererFeatures.OfType<TiltShiftFeature>().Single();
            Assert.IsTrue(f.isActive, path); Assert.AreEqual(downsample, f.Downsample, path);
        }
    }
}
```

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
grep -E "Assets/(Scripts|Editor|Tests)/.*warning CS[0-9]+" unity-tests.log
```
Expected: `exit=1`; `error CS0103: The name 'QualityTierValues' does not exist in the current context`.

- [ ] **Step 4: Implement**

The decided values live in the runtime assembly (tests cannot see `Assets/Editor`). Create `unity/Assets/Scripts/Map/QualityTierValues.cs` (set `WebRenderScale`, `WebShadowMap` and `WebTiltShiftDownsample` from Steps 1–2 by the table's rules; the values shown are the defaults the rules start from):

```csharp
#nullable enable
namespace Statecraft.Game.Map
{
    /// <summary>The quality-tier decisions of visual v1 Task 10 (evidence: docs/visual-v1.md "Tier decisions"). Change a value
    /// only with new measurements recorded there; QualityTiers.Apply writes them to the URP assets.</summary>
    public static class QualityTierValues
    {
        public const float WebRenderScale = 0.8f;
        public const int WebShadowMap = 2048, WebTiltShiftDownsample = 2;
        public const int DesktopShadowMap = 2048, DesktopTiltShiftDownsample = 2;
    }
}
```

Create `unity/Assets/Editor/QualityTiers.cs`:

```csharp
#nullable enable
using Statecraft.Game.Map;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace Statecraft.Game.EditorTools
{
    /// <summary>Writes the visual v1 tier decisions (QualityTierValues; docs/visual-v1.md "Tier decisions") to the URP assets:
    /// web (Mobile_*): Forward, no depth or opaque copy, one hard cascade; desktop (PC_*): four soft cascades, no depth or
    /// opaque copy, SSAO off (the baked AO replaces it); the tilt-shift's resolution on both. Idempotent. Run headless:
    /// Unity -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.QualityTiers.Apply</summary>
    public static class QualityTiers
    {
        public const string WebAsset = "Assets/Settings/Mobile_RPAsset.asset", DesktopAsset = "Assets/Settings/PC_RPAsset.asset";
        public const string WebRenderer = "Assets/Settings/Mobile_Renderer.asset", DesktopRenderer = "Assets/Settings/PC_Renderer.asset";

        public static void Apply()
        {
            Pipeline(WebAsset, QualityTierValues.WebRenderScale, 1, QualityTierValues.WebShadowMap, soft: false);
            Pipeline(DesktopAsset, 1f, 4, QualityTierValues.DesktopShadowMap, soft: true);
            Renderer(WebRenderer, QualityTierValues.WebTiltShiftDownsample, ssao: null);
            Renderer(DesktopRenderer, QualityTierValues.DesktopTiltShiftDownsample, ssao: false);
            AssetDatabase.SaveAssets();
        }

        static void Pipeline(string path, float renderScale, int cascades, int shadowMap, bool soft)
        {
            var so = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) ?? throw new System.IO.FileNotFoundException(path));
            so.FindProperty("m_RenderScale").floatValue = renderScale;
            so.FindProperty("m_ShadowCascadeCount").intValue = cascades;
            so.FindProperty("m_MainLightShadowmapResolution").intValue = shadowMap;
            so.FindProperty("m_SoftShadowsSupported").boolValue = soft;
            so.FindProperty("m_RequireDepthTexture").boolValue = false;
            so.FindProperty("m_RequireOpaqueTexture").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Renderer(string path, int tiltShiftDownsample, bool? ssao)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path) ?? throw new System.IO.FileNotFoundException(path);
            foreach (var f in data.rendererFeatures)
            {
                if (f is TiltShiftFeature t) { t.Downsample = tiltShiftDownsample; EditorUtility.SetDirty(t); }
                else if (f != null && ssao.HasValue && f.GetType().Name == "ScreenSpaceAmbientOcclusion") { f.SetActive(ssao.Value); EditorUtility.SetDirty(f); }
            }
            EditorUtility.SetDirty(data);
        }
    }
}
```

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.QualityTiers.Apply -logFile "$PWD/unity-tiers.log"; echo exit=$?
git diff --stat unity/Assets/Settings
```
Expected: `exit=0`; changes in `PC_RPAsset.asset` (depth and opaque copies off), `PC_Renderer.asset` (SSAO inactive), and in `Mobile_*` only where Steps 1–2 changed a value.

- [ ] **Step 5: Run the tests, the weak-GPU run and the final checkpoint**

Run:
```bash
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -projectPath unity -buildTarget OSXUniversal -runTests -testPlatform EditMode -testResults "$PWD/unity-editmode.xml" -logFile unity-tests.log; echo exit=$?
grep -o 'total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' unity-editmode.xml | head -1
git add unity/Assets/Settings/Mobile_RPAsset.asset unity/Assets/Settings/PC_RPAsset.asset unity/Assets/Settings/Mobile_Renderer.asset unity/Assets/Settings/PC_Renderer.asset
uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile "$PWD/unity-build-web.log"
git checkout -- unity/Assets/Settings unity/ProjectSettings/ProjectSettings.asset
uv run tools/web_check.py unity/Builds/Web-disabled --swiftshader --query "golden&shot" --expect "GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07" --expect "GOLDEN OK 1952 73590e324f19e9959a455a4da79e93d7089dfcf283c13aac02af6b0a624b9767" --expect "SHOT done" \
  --shots captures/visual/t10-swiftshader --timeout 1200 --fail 'GOLDEN FAIL|SMOKE FAIL|BOOT FAIL|Statecraft/.*(not supported|Unsupported)|(not supported|Unsupported).*Statecraft/'
uv run tools/machine_lock.py --exclusive --wait-unity -- uv run tools/web_check.py unity/Builds/Web-disabled --query "vprofile" --expect "PROFILE visual done" --timeout 1800 --stall 120 | grep "PROFILE summary"
tools/art_checkpoint.sh --no-build t10-final
```
Expected: total **B1 + 50**, `failed="0"`; the SwiftShader run passes (slow, software WebGL 2) and its screenshots show the same look as the GPU ones; the final profile meets the budgets or each overrun is named in the decisions table; the final checkpoint passes.

- [ ] **Step 6: Record the decisions**

Add to `docs/visual-v1.md`:

```markdown
## Tier decisions (Task 10)
| Setting | Web | Desktop | Evidence |
|---|---|---|---|
| Path | Forward | Forward+ | research notes 2.1 (Forward+ on WebGL reported unstable) |
| Depth / opaque copies | off / off | off / off | no visual v1 effect samples them; the Gaussian DoF that did is retired (Task 3) |
| Render scale | (0.8 or 1.0) | 1.0 | Step 2 lines: (paste the six `PROFILE summary visual=all` lines at scale 1.0) |
| Fill-rate stress (DPR 2, headed) | (paste the two `all` summary lines) | — | a stand-in for a GPU with a quarter of the fill rate; the 33 ms floor |
| Boot | both spans at `--cpu 1` and `--cpu 4` (paste the four `BOOT … median` lines) | — | each within max(300 ms, 50%) of its Task 1 baseline (443 / 3547; 1813 / 6963 ms) |
| Shadows | 1 cascade, (2048 or 1024), hard | 4 cascades, 2048, soft | `-shadows` cost: (paste) |
| Tilt-shift | downsample (2 or 3) | downsample 2 | `-tiltshift` cost: (paste) |
| SSAO | none | off | metre radius cannot reach km relief; baked AO replaces it (Tasks 5–6) |
| Grade, vignette, haze, grid, water, labels | on | on | `-grade`, `-haze`, `-grid`, `-water`, `-labels` costs with spreads: (paste) |
| Weak GPU | SwiftShader run passes golden and shots | — | captures/visual/t10-swiftshader |

### Final measurements
(paste the headless, headed and macOS `PROFILE visual` lines from Steps 1 and 5)
```

Fill every bracket with the decided value and every "(paste)" with the measured line; no bracket may remain. In `docs/dev-setup.md`, under the visual-checks bullet added in Task 1, add: `- Quality tiers: \`uv run tools/machine_lock.py --shared -- "$UNITY" -batchmode -quit -projectPath unity -executeMethod Statecraft.Game.EditorTools.QualityTiers.Apply\` writes the decisions in \`QualityTierValues\` to the URP assets (docs/visual-v1.md "Tier decisions").`

- [ ] **Step 7: Commit**

```bash
git diff --stat <task-base> -- sim content    # prints nothing
git add unity/Assets/Editor/QualityTiers.cs unity/Assets/Editor/QualityTiers.cs.meta \
  unity/Assets/Scripts/Map/QualityTierValues.cs unity/Assets/Scripts/Map/QualityTierValues.cs.meta \
  unity/Assets/Settings/Mobile_RPAsset.asset unity/Assets/Settings/PC_RPAsset.asset \
  unity/Assets/Settings/Mobile_Renderer.asset unity/Assets/Settings/PC_Renderer.asset \
  unity/Assets/Tests/EditMode/QualityTierTests.cs unity/Assets/Tests/EditMode/QualityTierTests.cs.meta \
  docs/visual-v1.md docs/dev-setup.md
git status --short   # nothing unexpected; unity/ProjectSettings must not appear (Global Constraints)
git commit -m "feat(visual): measured WebGL and desktop quality tiers; SSAO and the depth/opaque copies off, decisions pinned

Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

## Deferred to later plans

| Item | Plan | Note |
|---|---|---|
| Settlement kits, symbolic-scale buildings and trees, sprawl over hexes by population | Art track (next) | Owner ruling 1; nothing here fixes object scale; the 6 km hero zoom exists |
| Land-use texture (citrus, olive, fields, terraces) and detail texture arrays in the terrain shader | Art track | `StatecraftTerrain.hlsl`'s `ScGround` is the place; research §2.2 |
| Isotropic heightmap (three 2049² tiles) and per-zoom vertical exaggeration | Art track | Research §2.2; the bakes would be re-run by `tools/terrain_bake.py` |
| Roads and rail, the train, boats, units on the map | Art track / M4 war mode | Research §1 |
| Border ribbons with glow along owned hexes | Art track | Research §1 "Borders"; the shader grid stays faint |
| Label badges (size number, status chips) and icons | UI chrome track | The pill style is in place (`.map-label`) |
| The border skirt's land in the terrain's own look (biome colour, haze) instead of `BorderLand.mat`'s muted tone | Art track | Task 7 only holds the plateau's height |
| Desktop-only extras (Bokeh DoF, SSAO sized for buildings, refraction and depth foam) | After the art track | Task 10 table |
| WebGPU evaluation (GPU Resident Drawer, compute) | M5 | Research §2.1 |
| The wooden table and brass frame of `02-map-diorama.png` at maximum zoom-out | Art track | Promo 02 framing |
| A player option to turn the tilt-shift off | UI settings screen | `TiltShift.Enabled` and the `tiltshift` toggle exist |
| Runtime quality fallback: drop the render scale and then the tilt-shift when p95 stays over 33 ms (research §2.3 "0.8–1.0 dynamic") | M5 (web release hardening) | Review I1; Task 10's DPR-2 stress numbers size it |
| Baking the biome colour texture offline in `terrain_bake.py` (hexes.json and the palette are static) | When the boot budget needs it | Review I7; Tasks 6 and 8 measure the boot span |
| Border colours in the colour-blind check | Art track (borders) | `GradeTests.ColourBlindPlayersStillTellCoastAndSelectionApart` is where they go (review I9) |
| A map-label presenter that follows a rebuilt map without being recreated | When New Game rebuilds the map | Review minor 13: only the boot builds the map today |

## Self-review

Run against the owner's scope list, the controller input and rulings, the review (`.superpowers/sdd/2026-10-10-statecraft-m3/visual-v1-plan-review.md`), the spec and the research notes on 2026-10-11, re-anchored on `main` at `554ca2c` (rounds 2 and 3).

**1. Scope coverage.**
- Shadows that work at map scale, tied to camera distance, sun angle and warmth: Task 2 (`ShadowFit`, `MapLighting`, the 2048 web shadow map); the water receives them too (Task 7).
- A real two-band tilt-shift as a custom screen-space pass: Task 3 (`TiltShiftFeature`, `TiltShift.shader`, radius in UV units; Gaussian DoF retired).
- A stylised Terrain Lit Shader Graph with biome colour, slope rock and height blending: Task 6 (graph by `terrain_graph.py` in File mode, logic in `StatecraftTerrain.hlsl`; a two-sided gate).
- Baked AO, curvature and sky-view from the DEM in the tools pipeline, with tests: Task 5 (`terrain_bake.py`, 10 tests including the committed textures).
- A sea past the map with distance-to-coast colour and foam, coordinated with m3-fix's skirt (verified, not redone): Task 7 (the skirt's geometry is untouched; its sea material, the land fall closing the controller's plateau gap) and Task 8 (the grid continues on the skirt's sea).
- Colour grading, vignette and height fog: Task 4 (grade, Neutral tonemapper, vignette, readability checks) and Tasks 6–7 (analytic distance and rift haze applied after lighting on land and water; controller question 3).
- Shader-drawn, anti-aliased hex grid replacing the line mesh: Task 8.
- Labels in EB Garamond: Task 9 (decluttered, no per-frame layout, no lag).
- WebGL quality-tier review with evidence: Task 10 (DPR 1 and DPR 2, repeats and spreads, macOS player, SwiftShader, boot).
- Owner art checkpoint per visual task, scripted and non-blocking: `tools/art_checkpoint.sh` (Task 1), run in Tasks 2, 3, 4, 6, 7, 8, 9, 10 and at the merge point, with colour-blind copies.
- Objective tests: render-sample tests in Tasks 2, 3, 4, 6, 7, 8, 9; GLES3 compile (`ShaderChecks`, subshader-aware, base and shadow variants) in Tasks 3, 6, 7, 8; frame time on headless and headed WebGL via `?vprofile` in every visual task (the `PixelErrorProfile` approach, now with repeats and the machine lock); a boot budget in Tasks 1, 6, 8, 10; golden hashes in the WebGL player at every checkpoint and in the macOS player in Task 10; `git diff -- sim content` empty in every task.
- Owner ruling 1 not blocked: `CameraController.MinDistance = 6000` (Task 2); no object-scale assumption in any shader; `PromoPose.Gameplay03` explains why it matches hex count. Ruling 2 (promo wins): the palette, grade, haze, light grid lines and saturated sea follow the promo.
- Controller: the base is `main` at `554ca2c` (M3 merged at `4061a8b` and open-sourced); visual v1 branches from it in `.worktrees/visual-v1` and each task's PR goes to `main` (header, Parallel tracks, Phase 0 precondition); every hash quote is `71e9904d…`/`73590e32…`/`d3fdd459…`, and the checkpoint expects the full golden lines from `docs/determinism.md`; "Re-verify after M3 merge" has 27 rows.

**2. Placeholder scan.** No TBD or "similar to Task N"; steps that used to point at another task's command now carry the command. The values left to the implementer are measurements (profile and boot lines, the render-scale decision by a stated rule, the boot baseline written down in Task 1, art-log numbers) and the owner's verdicts, each with its exact command and destination in `docs/visual-v1.md`.

**3. Type and name consistency.** Checked across tasks after the amendments: `PromoPose.{Gameplay03, Diorama02, All, Apply}`; `VisualToggles.{Register, Unregister, Set, SetAll, Names, Clear}` and the toggles `shadows`, `tiltshift`, `grade`, `haze`, `water`, `grid`, `labels`; `FrameProfile.{Requested, Line(label, pose, resolution, renderScale, rep, frames), Summary, Order, Run, Repeats}`, `VisualShots.{Requested, Lines, Run, ChunkChars}`, `FrameStats.Summary`; `RenderSample.{Render, At, Mean, MeanLuma, GradientEnergy, MeanSaturation, LumaStd, FractionClipped, Luma, FractionDarker, Lab, DeltaE, Cvd, SimulateCvd}`; `ShaderChecks.{Gles3Errors, ExcludesGles3}`; `MapRuntimeAssets.OnTeardown`, `CameraRig.TeardownActions`; `MapLighting.{FindSun, Apply}`, `ShadowFit.{Distance, Apply, Restore, CoverFactor, MinDistanceM}`; `TiltShift.{Weight, FocusCentre, Strength, Params(topInset, distance), Apply(topInset, distance), Enabled, MaxRadiusUv, MinStrength, ParamsId}`, `PostFxPaths.TiltShiftMaterial`, `TiltShiftFeature.{Material, Downsample}`; `MapGrade.{Apply, SetActive, Saturation, Tonemapper}`; HLSL `_ScMap`, `_ScMapOrigin`, `_ScHaze`, `_ScHazeWater`, `_ScHazeRange`, `_ScWaterOn`, `_ScGrid`, `_ScBiomeTex`, `_ScBakeTex`, `_ScCoastTex`, `_ScTiltShift`, `ScGround`, `ScWater`, `ScHazeAmount`, `ScHazeWater`, `ScWaterShade`, `ScCoastRangeM`, `ScHexEdgeDistance`, `ScGridLine`, `ScGridAmount`, `ScGridLandSrgb`, `ScGridWaterSrgb`, `StatecraftTerrain_float`; `MapShaderGlobals.{Map, MapOrigin, Biome, Haze, HazeWater, HazeRange, WaterOn, Grid, Apply, GridLandOpacity, GridWaterOpacity, GridWidthPx}`, `MapTexturePaths.{Bake, Coast}`, `MapPalette.Ground`, `HexGrid.{Centre, NearestCentre, FromWorld, ToWorld, EdgeDistance}`; `MapBuilder.{BiomeTextureResolution, NoBasemapDistanceM, SkirtFall, SkirtRings, RingVertices, OverlaySegmentsPerEdge}`; `MapScene.{Settlements, Configure(5), SelectionClearanceM, SelectionWidth, SelectionName}`; `SettlementPlacer.MarkerHeightM`; `MapLabelsPresenter.{ContainerName, SlotClass, LabelClass, LiftPx, MarginPx, DeclutterDistanceM, Labels, Slots, Anchor, Priority, HiddenByZoom, PillRect, Refresh(force)}`; `QualityTierValues`, `QualityTiers.Apply`; Python `terrain_bake.{openness, curvature, water_mask, distance_to, to_texels, encode_coast, bake, main}`, `terrain_graph.{build, split, kind, OUTPUTS, FILE_MODE}`, `web_check.{ShotCollector, chrome_flags(profile, swiftshader)}`, `art_compare.{metrics, histogram_distance, simulate_cvd, compose, main(argv, root, promos)}`, `machine_lock.{parse, worktrees, lockfile_holders, unity_pids, wait_for_unity, main, LOCK}`, `headed.{browser_flags, boot_phases, map_build_ms, over_budget, boot, profile, main}`.

**4. Review Focus.** Five conditions (yaw, zoom extremes, window size and render scale, rebuild, weak GPU and colour-blind players), each pinned by a named test or step in its owning task.

**5. Test totals.** Unity: Task 1 +14, fix round 1 +7 (B1 = B0 + 21), Task 2 +11, Task 3 +6, Task 4 +6, Task 6 +9, Task 7 +6, Task 8 +4, Task 9 +5: merge point B1 + 47; Task 10 +3: B1 + 50. Tools (T0 = `109 passed, 1 skipped` on `554ca2c`): Task 1 +20, fix round 1 +14 (T1 = T0 + 34; web_check shots 15, art_compare 7, machine_lock 6, headed 6), Task 5 +10, Task 6 +5: T1 + 15; the skipped count stays 1.

**API facts checked against the installed packages (URP and Shader Graph 17.3 package cache, the 6000.3.25f1 editor DLLs) while writing:** `RealtimeLights.hlsl` guards with `UNIVERSAL_REALTIME_LIGHTS_INCLUDED`; `ScriptableRenderPass.requiresIntermediateTexture` exists; `Blitter.BlitTexture(RasterCommandBuffer, RTHandle, Vector4, Material, int)` exists; `TextureDesc` has `sizeMode`, `width`, `filterMode`, `wrapMode`, `msaaSamples`, `clearBuffer`; `UniversalCameraData.cameraType` exists; `UniversalRenderPipelineAsset.shadowDistance` and `mainLightShadowmapResolution` are public; the editor exposes `ShaderData.Pass.CompileVariant`, `ShaderData.Pass.SourceCode`, `GetSerializedSubshader` and `TextureImporterSingleChannelComponent`; Shader Graph's `HlslSourceType` is `{ File, String }` (the review's C1); URP's LDR grading applies the tonemapper before the LUT (`Common.hlsl`); the "Terrain Simple" template ships with Shader Graph 17.3 and contains every slot kind the generator copies. Checked by running, in a scratch copy, while amending: every Python file and test in this plan (extracted verbatim) with M3's `test_web_check.py` beside them: 41 passed (the committed-texture test needs the generated PNGs); and `terrain_bake.py` on the real heightmap: 42 s, water 20.2% of texels, and every assertion of `test_the_committed_coast_texture_marks_the_seas_and_lakes` holds (Mediterranean 0, Sea of Galilee 73, Dead Sea 69, Gulf of Aqaba 94, Jerusalem 255). Not checked by running: the C# and HLSL (they need the Unity tasks), the generated graph's import and render (Task 6's two-sided gate), and Chrome's console-line length for `SHOT` (Task 1 Step 9 fallback).

## Review round 1 response

Every finding of `.superpowers/sdd/2026-10-10-statecraft-m3/visual-v1-plan-review.md` and the controller's rulings on it, with where the plan changed.

| Finding | Change |
|---|---|
| **C1** Custom Function in String mode | `terrain_graph.py` writes `m_SourceType` 0 (`FILE_MODE`, Shader Graph's `HlslSourceType { File, String }`); the generator test expects `(0, ...)`. Task 6's gate is two-sided (controller ruling): the negative half (no import error, a Custom Function node, `"m_SourceType": 0` in the file) in Step 5, and the positive half first in Step 7, `TerrainShaderTests.TheCustomFunctionDrivesTheTerrain`: every generated pass's source is searched for `StatecraftTerrain.hlsl`, and a land pixel rendered with a magenta biome texture must be magenta and not black. Controller question 1 updated. |
| **C2** GLES3 check fails on DOTS subshaders | `ShaderChecks.Gles3Errors` skips passes whose source excludes GLES3 (`exclude_renderers ... gles3`, or `only_renderers` without it, `ExcludesGles3`), compiles the base and `_MAIN_LIGHT_SHADOWS` variants, and fails unless at least one subshader has passes built for GLES3 that all compile. Negative controls in Task 1: `SubshadersThatExcludeGles3AreSkippedButOneMustCompile` (an excluded broken subshader beside a good one: no errors; only an excluded one: "no subshader" error). |
| **I1** Measurements | the viewport is set to exactly 1920×1080 over CDP at the requested DPR (Task 1 fix round 1; `--dpr 2` for the stress proxy); every `PROFILE` line has `res=` and `scale=`; `FrameProfile` runs each configuration 3 times in interleaved orders (`Order`) and logs `PROFILE summary` (median, spread, median p95); a cost is over budget only beyond budget plus the larger spread; `tools/machine_lock.py` (exclusive for profiles with `--wait-unity`, shared for every Unity batch command, lock file shared by all worktrees) serialises measurements, plus a Global Constraint and a controller note; a headed DPR-2 fill-rate stress run in Task 10 and a deferred runtime fallback; the boot budget with `?bootlog` and `tools/boot_report.py` (baseline in Task 1, checks after Tasks 6, 8 and in Task 10, at most +1,000 ms on the map-build span). |
| **I2** Haze through Lit; sea loses its blue | Land haze is applied after lighting (`BaseColor × (1 − h)`, `Emission = haze × h`); water uses a cooler, weaker haze (`_ScHazeWater`, 0.4 × strength, (0.62, 0.74, 0.86)) on both sides of the edge. Task 7 Step 4 states the arithmetic for `TheBackdropIsLand…` (b − r ≈ 0.23), the new `TheOpenSeaStaysBlueUnderTheHaze` at the diorama pose (≈ 0.21) and `TheCoastSeparatesLandFromSea` (≈ 0.26), each against 0.15, with the merge-time grade estimate. |
| **I3** Grid stops at the map's edge | Option (a): `Sea.shader` draws the same `ScGridAmount` lines, so the lattice continues on the skirt's sea (as in the promo). The seam test samples hex centres (via `HexGrid.NearestCentre`/`ToWorld`), with the grid on and off, at four yaws. |
| **I4** Tilt-shift test symmetric; mixed pixel bases | New `AnOffCentreFocusFollowsTheTopBar` (TopInset 0.3: the 0.33–0.37 band keeps over 95% of its gradient energy, 0.70–0.78 falls below 80%). The radius is now a share of the picture height (`MaxRadiusUv`, `Params(topInset, distance)`); the shader converts it with the aspect ratio only, so render scale, window size and batch-mode captures (`MapCapture.Promo`) all get the same blur with no pixel-height source. The note on texture targets ignoring render scale is in Task 3. |
| **I5** Labels | `GameRoot.LateUpdate` refreshes them; slots move with an inline pixel `translate` and `Refresh` returns early when nothing changed; priority declutter (no overlaps, minor places hidden beyond 150 km, `visibility` so sizes persist); the tautological test is replaced by `APillSitsJustAboveItsMarkerTop` (marker box top through `Camera.WorldToScreenPoint` and the panel scale, against `worldBound`); M2's cluster test returns as `ShownPillsNeverOverlapAndEachReadsAsItsOwn` on `worldBound` at 90 km and the overview. |
| **I6** Water ignores shadows | `ScWaterShade` takes `GetMainLight(TransformWorldToShadowCoord(positionWS)).shadowAttenuation` on its direct and glint terms; `Sea.shader` compiles the main-light shadow keywords. Task 2's `ReliefCastsShadowsAtMapScale` states the post-merge expectation (shadows land on the Dead Sea). |
| **I7** Base moved | Planned against m3 `8978566`; visual v1 branches from `main` after M3 merges; hashes updated; R-table re-read with R21–R25 (DiagLog phases, the loading root, the USS tail, the template's DPR, the BiomeBlender signature); Task 6 renames the boot phase to `biome texture baked`, Task 8 deletes `hex overlay draped`; Task 9 anchors its UXML edit on the loading line and appends its USS after the loading block with new selectors only (and hides labels while loading); boot budget as in I1; the offline biome bake is a deferred remedy. |
| **I8** A lost SHOT chunk passes | `ShotCollector.feed("SHOT done")` raises when a shot is unfinished (named, with its part count); `art_checkpoint.sh` requires both pose PNGs; `art_compare.py` exits 2 on a near-uniform capture; the stale stack-trace comment in `web_check.py` is fixed. |
| **I9** Readability | Neutral tonemapper (reasoning in Task 4: hue-preserving, applied before the LUT in LDR grading) with `TheSunlitDesertDoesNotClip` (< 1% clipped in the Negev and at the gameplay pose); `TheSelectedHexStillStandsOutOnGradedLand` (ΔE76 ≥ 20 at both poses, graded and ungraded); `ColourBlindPlayersStillTellCoastAndSelectionApart` (Machado 2009 deuteranopia and protanopia, ΔE76 ≥ 15 for sea/land, selection/land, selection/sea; border colours deferred to the border task); `art_compare.py` writes deuteranopia and protanopia copies of every comparison. |
| **I10** Missing toggles | `labels` (container display) and `water` (`_ScWaterOn`, flat colour on both sides) are registered and priced; Task 9 Step 5 is `median(all) − median(-labels) ≤ 1 ms` plus the spread; Task 10's table covers both. |
| Minor 1 | `MapScene.SelectionClearanceM = 30` replaces `OverlayClearanceM + SelectionLiftM`; `OverlayClearanceM` is deleted; the selection test checks the new clearance. |
| Minor 2 | `GridLinesStayThinAtBothZoomExtremes` looks at a hex-edge midpoint. |
| Minor 3 | `PromoPose.Gameplay03`'s summary states why it matches hex count, not geography. |
| Minor 4 | Light land lines (0.93, 0.90, 0.80) at 0.25 opacity (promo wins); the grid and rift tests compare grid on and off without assuming dark lines. |
| Minor 5 | `art_checkpoint.sh --no-build`; every task builds once, profiles, then checkpoints without rebuilding. |
| Minor 6 | Every "run the command from Step N" is inlined. |
| Minor 7 | Every edit-mode run passes `-buildTarget OSXUniversal`; effort includes about 30 minutes per worktree import. |
| Minor 8 | Global Constraint: `git status --short unity/ProjectSettings` empty before commits; `art_checkpoint.sh` restores `QualitySettings.asset` after `MapCapture.Promo`; commit steps say so. |
| Minor 9 | Task 10 records the bake texture's compression decision (kept compressed; mobile browsers outside the target). |
| Minor 10 | Task 8 Step 5 has the `#pragma require derivatives` remedy if the GLES subshader rejects `fwidth`. |
| Minor 11 | Kept `QualityTierValues` + `QualityTiers`, with the reason (one constant and one command per decision) stated in Task 10. |
| Minor 12 | Task 2's checkpoint row tells the owner the 6 km zoom shows placeholder cubes. |
| Minor 13 | Stated in Task 9 and the presenter's summary; a deferred row. |
| Minor 14 | `art_compare.py --log`; `art_checkpoint.sh` writes the art log under the main checkout's `captures/` for every worktree. |

## Review round 2 response

The re-review (`.superpowers/sdd/2026-10-10-statecraft-m3/visual-v1-plan-rereview-1.md`) and the controller's round-2 rulings.

| Finding | Change |
|---|---|
| **N1** Seam test pairs 9–20 km apart, haze differs | Task 8's `TheSeaBeyondTheEdgeMatchesTheTerrainSea` now compares two hex centres on one odd row at x = +4330 m and −4330 m (the same z), for a west-coast row and the north-west row; the seam itself is checked with the haze off (grid on and off, four yaws, 0.012). One haze-on check per pair stays, at yaw 0 (the pair is equidistant from the camera), tolerance 0.012. The yaw-90 haze-on check was dropped in re-review 2 (R2-1): with correct sRGB slopes haze alone adds about 0.015 there. |
| **N2** Anchors moved | Re-anchored on `main` at `554ca2c` (M3 merged at `4061a8b`, open-sourced): `docs/dev-setup.md`'s edit appends to the current end of the "Page flags" bullet; `tools/README.md`'s edits anchor on the `headed.py` entry and keep the `Expected:` line's skipped count; T0 is recorded (`109 passed, 1 skipped` at `554ca2c`), not asserted; R16, R17, R23 updated, R26 (doc anchors) and R27 (the full expectations in `docs/determinism.md`, used by `art_checkpoint.sh` and Task 10's SwiftShader run) added. The plan branches from `main` at or after `554ca2c` in `.worktrees/visual-v1`, and every task's PR goes to `main`. |
| **N3** Task 1 count | B1 = B0 + 21 (Step 5's +14 and fix round 1's +7; the merge point, the self-review). |
| **N4** Labels during loading | `.loading .map-labels { display: none; }` (children cannot override display); the text check is replaced by `ThePillIsEbGaramondOnNavyWithAGoldEdgeAndHiddenWhileLoading`, which reads the container's resolved display while `loading` is on and off. |
| **N5** machine_lock | `test_a_shared_holder_really_blocks_an_exclusive_lock` runs `machine_lock.py --shared` in a child and shows `LOCK_EX | LOCK_NB` is refused (the lock path is overridable through `STATECRAFT_MACHINE_LOCK`); `--wait-unity` also waits for any process holding a worktree's `unity/Temp/UnityLockfile` (`lockfile_holders` over `git worktree list`), so an interactive editor counts, with `test_an_editor_holding_a_worktree_lockfile_counts_as_unity`; "How to measure" says so. |
| **N6** Text-only assertions | The tilt-shift's shader-text check is replaced by a render at 1920×1080 beside the 960×540 one: the top band loses the same share of detail (within 0.1). The pill's USS-text check is replaced by resolved-style checks (font, navy fill, gold edge and width). |
| **N7** Magenta gate point | The gate renders a flat coastal-plain point (34.90 E, 31.95 N), away from rock and the stone tint. |
| **N8** Overlap with `tools/headed.py` | `boot_report.py` and `web_check --headed/--dpr` are gone. `headed.py` (M3's CDP driver) gains `profile` (headed `?vprofile` at 1920×1080, `--dpr`, frame rate uncapped) and `boot --runs --baseline --budget` (the median map-build span, exit 1 over budget), with pure, tested `browser_flags`, `boot_phases`, `map_build_ms` and `over_budget`; Task 10 adds `boot --cpu 4` as the low-end boot proxy. `web_check.py` stays the headless entry point (DPR 1). |

Checked by running, in a scratch copy, after this round: every Python file and test in this plan, extracted verbatim, with the `headed.py` edits applied to `554ca2c`'s file and M3's `conftest.py`, `test_headed.py` and `test_web_check.py` beside them: 44 passed, 1 skipped (M3's headed browser check), the committed-texture test deselected (it needs Task 5's PNGs).
