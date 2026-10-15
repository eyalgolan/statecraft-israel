# Visual v1: measurements, budgets and checkpoints

Plan: `docs/superpowers/plans/2026-10-11-statecraft-visual-v1.md`. Promo targets: `promo/final/03-gameplay-1936.png`,
`promo/final/02-map-diorama.png`. Poses: `PromoPose.Gameplay03` (34.86 E, 32.00 N, 60 km, yaw 10) and
`PromoPose.Diorama02` (35.05 E, 31.55 N, 250 km, yaw 0). Since #174 the camera keeps its view on the map, and centres
it east to west wherever the view is wider than the map. In the player, which runs that fit every frame, diorama02 can
shift by up to a few hundred metres from its pose. The controller accepted this (owner-delegated). The other poses do
not move.

## Art direction (owner ruling, 14 October 2026)

The owner ruled "this is the art direction" on four references (Slack, #visual-design-art-techart, 14 October 2026):

- demo2 shot T29, "26 May 1948: one army": the coast-to-Jordan diorama, units on white ring bases, Arab armour with
  flags (`promo/demo2/out/demo2-final-16x9.mp4`, about 1:11);
- demo2 shot L14, "Latrun is captured by the enemy": a low-angle close-up of the fortress (same file, about 0:32);
- demo2 shot D15, "20 May: Degania stops the Syrian tanks": a close-up at the kibbutz gate (same file, about 0:36);
- `promo/final/03-gameplay-1936.png`.

The shot ids are those of `promo/demo2/SHOTLIST-v2.md` and `shots-v2.py`; demo2's files are local to the promo work and
not in the repository. Two rules from that thread: lock the visual language before more models are made ("it's a
question of visual language, not quality"), and the next step is a vertical slice of a few terrain hexes with the
models on them. New models and visual PRs are judged against these four references.

## How to measure
- Frame time: `?vprofile` (WebGL) or `-vprofile` (desktop player, quits when done), always under
  `uv run tools/machine_lock.py --exclusive --wait-unity -- ...` (no other Unity job on the machine). Per pose, every
  configuration (`all`, each toggle off alone as `-<name>`, `none`) runs 3 times in interleaved orders, 300 frames after
  60 warm-up frames (and, before the first pose, one discarded warm-up pass of every configuration); Chrome runs with
  `--disable-frame-rate-limit --disable-gpu-vsync` and the anti-throttling flags (`--disable-backgrounding-occluded-windows
  --disable-renderer-backgrounding --disable-background-timer-throttling`), headless through `web_check.py` (add
  `--stall 120`), headed through `headed.py profile`. Both fix the page's viewport to exactly 1920x1080 CSS pixels at the
  DPR over CDP (`Emulation.setDeviceMetricsOverride`, before the page loads), so headed and headless runs draw the same
  pixels even when the laptop screen clamps the headed window (its final composite is then scaled; the backbuffer is not).
  The runtime quality fallback (issue #44, "Runtime quality fallback") is off under `?vprofile`, `?profile` and `?shot`,
  so these see the tier's own scale and tilt-shift. Every line states `res=` and the render `scale=`, and both tools FAIL on a `res=` other than 1920x1080 times the DPR
  (and on a stall: no console line for 120 s). Read the `PROFILE summary` lines: a feature costs
  `median(all) - median(-name)`. The WebGL frame clock is quantised to about 1 ms: averages are fine, but every p95 is a
  whole millisecond, so a p95 budget is only good to about 1 ms.
- Boot: `uv run tools/headed.py boot --runs 3 [--baseline MS] [--baseline-first-frame MS] [--cpu R]` (three cold loads in
  a kept-awake headed window with a 1920x1080 viewport). Two spans, each the median of the runs: `map_build_ms`
  (heightmap decoded to map built) and `first_frame_ms` (navigation start to the second animation frame after `GAME OK`:
  download, scene load and texture uploads, map build and the first draw with its shader compiles). `--cpu 4` adds CPU
  throttling as a cheap low-end proxy; every line states `cpu=`.
- Headed frame time: `uv run tools/headed.py profile --dpr 1` (`--dpr 2` is the fill-rate stress proxy). `--wait-unity`
  also waits for an interactive Unity editor that has any worktree open (it holds `unity/Temp/UnityLockfile`): close it,
  or the profile waits.
- Screenshots: `tools/art_checkpoint.sh [--no-build] [--units] <name>` (WebGL `?shot` + editor renders of both tiers + side-by-side
  images and their deuteranopia/protanopia copies under `captures/visual/<name>/`, git-ignored; rows in the main
  checkout's `captures/visual/art-log.tsv` with the branch, short commit and the image's absolute path). It refuses to run
  while `QualitySettings.asset` (and, when it builds, `unity/Assets/Settings` or `ProjectSettings.asset`) has unstaged
  changes, restores them on every exit, and takes the pose list from `PromoPose.cs`. Each checkpoint switches the
  worktree's build target WebGL -> OSXUniversal (editor renders) and back at the next build, a reimport each way: budget
  a few minutes per checkpoint. `--units` adds the units (`docs/units.md`): `&units` on the WebGL shots and
  `-units` on the editor renders. That shows both the unit art slice and the units in play as a new game starts.
  Normal play draws the units in play, but a `?shot` run switches them off unless `&units` is given
  (`UnitLayer.HiddenInCaptures`), and the editor draws them only with `-units`. So plain checkpoints never show units, and
  the `--units` rows are named `<name>-units`, so they are never compared with plain ones.
  The kits do change for the units, though: from units-in-play's 943184c they keep a clearing where a unit stands in a town
  (Tel Aviv, West Jerusalem; `MapScene.UnitClearings`). That is a kit baseline break in the art log.

## Budgets (proposed; ratified in Task 10: see "Tier decisions")
Measured on the development Mac (Apple silicon, integrated GPU), 1920x1080 at DPR 1, Chrome for Testing:
- headless, all features on: median p95 <= 16.7 ms at both poses, and median(all) - median(none) <= 8 ms;
- each feature: median(all) - median(-feature) <= 2.5 ms (tilt-shift <= 1.5 ms, labels <= 1 ms); a cost counts as over only
  when it exceeds the budget plus the larger of the two spreads;
- headed numbers are recorded, plus a DPR-2 headed run in Task 10 as a fill-rate stress proxy for weaker GPUs; the 30 fps
  floor for an integrated-GPU laptop (research notes 2.3) is 33 ms p95;
- boot: each span (`map_build_ms` and `first_frame_ms`, median of 3, at `--cpu 1` and at `--cpu 4`) grows by at most
  max(300 ms, 50% of its Task 1 baseline): the tool's default when `--budget` is not given (controller ruling 3).

## GLES3 check: the `require` allowlist (Task 3 Step 0)
`ShaderChecks.GlslErrors` fails any `#extension X : require` not in `ShaderChecks.AllowedRequires` (default-deny). An
entry is added only after a WebGL 2 `compileShader` of a shader carrying that line succeeds in headless Chrome.
- Report (`-executeMethod Gles3Report.Run`, 2026-10-10): URP Lit, URP Terrain Lit and `Statecraft/HexSelection` show
  **0 errors and no `require` line**, so `AllowedRequires` stays **empty**. Running the report exposed a harness bug, fixed
  here: `Gles3Errors` looped over `SubshaderCount` but indexed `GetSerializedSubshader`. URP Lit has 2 subshaders and 1
  serialized one, so the report threw. The loop now uses `SerializedSubshaderCount`.
- **Probe (corrected in Task 3 fix round 1).** In WebGL 2, `#extension GL_X : require` compiles only after the page has
  called `gl.getExtension("<WebGL name>")`. The first probe never called it, so it could only ever say FAILS. The procedure
  (page and command: see "Probe page and command" below):
  1. Run Playwright's `chrome-headless-shell` (Chrome for Testing 149.0.7827.55) with `--use-angle=swiftshader
     --enable-unsafe-swiftshader --dump-dom` under a timeout. Full Chrome's `--headless=new --dump-dom` never exits on this
     Mac. Match the output with `grep -E`, because BSD grep ignores `\|`.
  2. Log `gl.getSupportedExtensions()`.
  3. **Positive control.** For supported extensions that have a GLSL directive, compile a `require` shader before and after
     `gl.getExtension`. It must FAIL before and print OK after. If no control passes, the probe is broken, not the
     extension.
  4. Enable every supported extension, as Unity's loader does, then compile the `require` under test (vertex and
     fragment).
  5. **Unity's loader.** The WebGL framework (`*.framework.js`, emscripten) calls `GLctx.getExtension(ext)` for every
     entry of `getSupportedExtensions()` except names containing `lose_context` or `debug`. So step 4 matches the player,
     and a GLSL extension whose WebGL extension is supported is also enabled in game.
- **Result (2026-10-10, SwiftShader).** Supported: EXT_clip_control, EXT_color_buffer_float, EXT_color_buffer_half_float,
  EXT_conservative_depth, EXT_depth_clamp, EXT_disjoint_timer_query_webgl2, EXT_float_blend, EXT_polygon_offset_clamp,
  EXT_texture_compression_bptc/rgtc, EXT_texture_filter_anisotropic, EXT_texture_mirror_clamp_to_edge,
  NV_shader_noperspective_interpolation, OES_draw_buffers_indexed, OES_sample_variables,
  OES_shader_multisample_interpolation, OES_texture_float_linear, OVR_multiview2, WEBGL_clip_cull_distance,
  WEBGL_compressed_texture_*, WEBGL_multi_draw, WEBGL_polygon_mode, WEBGL_stencil_texturing (plus the debug and
  lose_context ones).
  - Positive controls: `GL_OVR_multiview2`, `GL_EXT_conservative_depth`, `GL_NV_shader_noperspective_interpolation`,
    `GL_OES_sample_variables`, `GL_OES_shader_multisample_interpolation` and `GL_ANGLE_multi_draw` each FAIL before
    `getExtension` and are **OK after** it, in both stages.
  - With `WEBGL_clip_cull_distance` enabled, `GL_ANGLE_clip_cull_distance` is OK but `GL_EXT_clip_cull_distance` FAILS.
    WebGL's GLSL names can differ from GLES's, so the probe must use the exact name that Unity's GLSL requires.
  - Targets: `GL_EXT_shader_non_constant_global_initializers` and `GL_EXT_texture_buffer` still FAIL ("extension is not
    supported") with every supported extension enabled. Neither is a WebGL extension, so the negatives stand.
  - `AllowedRequires` stays empty, because none of our shaders requires anything. An extension is added only when one of
    our shaders needs it and its probe prints OK. Note that SwiftShader's list is not every GPU's list: a desktop-GPU run
    can list more.

### Probe page and command
Save the page as `$S/probe2/probe.html`, outside the repo. Pass the GLSL names under test in the URL hash, comma-separated,
and take each name verbatim from the shader report's `#extension ... : require` line (or the Gles3 error text), never from
the WebGL extension name. To map a WebGL name to its GLSL name when you add a target to `GLSL`: `OVR_multiview2` ->
`GL_OVR_multiview2`, `WEBGL_multi_draw` -> `GL_ANGLE_multi_draw`, `WEBGL_clip_cull_distance` -> `GL_ANGLE_clip_cull_distance`.
A target FAIL for a name that is not in the page's `GLSL` table only counts if a mapped positive control passed in the
same run.

```html
<html><body><script>
// WebGL extension name -> the GLSL #extension name it unlocks (null: a WebGL-only extension with no GLSL directive)
const GLSL = { OVR_multiview2: 'GL_OVR_multiview2', WEBGL_clip_cull_distance: 'GL_ANGLE_clip_cull_distance',
  WEBGL_multi_draw: 'GL_ANGLE_multi_draw', EXT_conservative_depth: 'GL_EXT_conservative_depth',
  WEBGL_draw_instanced_base_vertex_base_instance: 'GL_ANGLE_base_vertex_base_instance_shader_builtin',
  WEBGL_shader_pixel_local_storage: 'GL_ANGLE_shader_pixel_local_storage', NV_shader_noperspective_interpolation: 'GL_NV_shader_noperspective_interpolation',
  OES_sample_variables: 'GL_OES_sample_variables', OES_shader_multisample_interpolation: 'GL_OES_shader_multisample_interpolation',
  EXT_texture_shadow_lod: 'GL_EXT_texture_shadow_lod', WEBGL_blend_func_extended: 'GL_EXT_blend_func_extended' };
const gl = document.createElement('canvas').getContext('webgl2');
const lines = [];
function compile(ext, stage) {
  const sh = gl.createShader(stage === 'v' ? gl.VERTEX_SHADER : gl.FRAGMENT_SHADER);
  const body = stage === 'v' ? 'void main() { gl_Position = vec4(0.0); }' : 'precision highp float;\nout vec4 o;\nvoid main() { o = vec4(1.0); }';
  gl.shaderSource(sh, '#version 300 es\n#extension ' + ext + ' : require\n' + body);
  gl.compileShader(sh);
  const ok = gl.getShaderParameter(sh, gl.COMPILE_STATUS);
  return ok ? 'OK' : 'FAILS(' + (gl.getShaderInfoLog(sh) || '').trim().split('\n')[0] + ')';
}
if (!gl) lines.push('NO WEBGL2'); else {
  const sup = gl.getSupportedExtensions();
  lines.push('SUPPORTED ' + sup.join(' '));
  const targets = (location.hash.slice(1) || 'GL_EXT_shader_non_constant_global_initializers,GL_EXT_texture_buffer').split(',');
  // before getExtension: everything must fail (control that the require gate is real)
  for (const w of sup) if (GLSL[w]) lines.push('BEFORE ' + w + ' ' + GLSL[w] + ' v=' + compile(GLSL[w], 'v') + ' f=' + compile(GLSL[w], 'f'));
  for (const w of sup) if (GLSL[w]) { gl.getExtension(w); lines.push('AFTER ' + w + ' ' + GLSL[w] + ' v=' + compile(GLSL[w], 'v') + ' f=' + compile(GLSL[w], 'f')); }
  // the GLSL names under test: enable every supported WebGL extension first, then compile
  for (const w of sup) gl.getExtension(w);
  for (const t of targets) lines.push('TARGET ' + t + ' v=' + compile(t, 'v') + ' f=' + compile(t, 'f'));
}
document.body.textContent = lines.join('\n');
</script></body></html>
```

```bash
S=<scratch dir outside the repo>
HS=$(find ~/Library/Caches/ms-playwright/chromium_headless_shell-1228 -name chrome-headless-shell -type f | head -1)
perl -e 'alarm 60; exec @ARGV' "$HS" --user-data-dir=$S/probe2/prof --use-angle=swiftshader --enable-unsafe-swiftshader \
  --dump-dom "file://$S/probe2/probe.html#GL_EXT_foo,GL_EXT_bar" 2>/dev/null | sed -e 's/<[^>]*>//g' \
  | grep -E "^(SUPPORTED|BEFORE|AFTER|TARGET|NO)"
grep -oE 'getSupportedExtensions\(\)\|\|\[\];exts.forEach[^}]*' unity/Builds/Web-disabled/Build/*.framework.js   # the loader's enable-all
```

Read the output as follows:
- At least one `AFTER ... v=OK f=OK` line (the control) is required; without it, the probe is broken.
- `TARGET <name> v=OK f=OK` means the extension may be added to `ShaderChecks.AllowedRequires`, with the comment
  `// <name>: verified to work in WebGL 2 in headless Chrome (Chrome for Testing 149.0.7827.55, <date>)`, but only if one
  of our shaders actually requires it.
- `TARGET ... FAILS` means that shader cannot run on the web tier.
- Re-check the loader `grep` after a Unity upgrade.

## Baseline (Task 1, before any visual change; re-recorded in fix round 1)
Viewport fixed to 1920x1080 at DPR 1 over CDP; Chrome kept awake; build from `0b6058e` (harness only).

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.29 spread_ms=0.17 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.35 spread_ms=0.14 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=6.84 spread_ms=0.06 median_p95_ms=7.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=6.92 spread_ms=0.05 median_p95_ms=8.00`<br>run line: `PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=0 frames=300 avg_ms=2.43 p95_ms=4.00` |
| headed (DPR 1) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.31 spread_ms=0.57 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=1.83 spread_ms=0.76 median_p95_ms=3.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.18 spread_ms=0.29 median_p95_ms=8.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.15 spread_ms=0.40 median_p95_ms=8.00`<br>run line: `PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=0 frames=300 avg_ms=1.78 p95_ms=3.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=443 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3547 runs=3 cpu=1` |
| boot, `--cpu 4` | `BOOT map_build_ms median=1813 runs=3 cpu=4`<br>`BOOT first_frame_ms median=6963 runs=3 cpu=4` |

Boot budgets from these (ruling 3, baseline + max(300 ms, 50%)): `--cpu 1` map build 443 -> 743 ms, first frame
3547 -> 5321 ms; `--cpu 4` map build 1813 -> 2720 ms, first frame 6963 -> 10445 ms.

Notes:
- The headed window is clamped by the laptop screen (1728 px wide); the emulated 1920x1080 viewport still gives a
  1920x1080 backbuffer (`res=1920x1080`), only the final composite to the window is scaled.
- No toggles exist yet, so `all` and `none` are the same scene: their difference and the spreads are the noise floor.
  Headless spreads are 0.05-0.17 ms; headed gameplay03 is noisier (spreads 0.57-0.76 ms, `all` vs `none` 0.48 ms apart).
- Superseded first recording (commit `33fb072`, before the CDP viewport): headless ran at `res=1920x937` and headed at
  `res=1728x860`, with a first-pose spread of 0.68 ms (headless) and one headed run that stalled; do not compare with it.

## Task 2: sun and shadows
Headless, viewport 1920x1080 at DPR 1 (`res=1920x1080 scale=0.80`), web tier (shadow map 2048, one cascade, cascade
border 0.1).

**Shadow fit (fix round 1, review I1).** URP fades shadows by straight-line distance from the camera, from
(1 - cascadeBorder) x shadowDistance out (URP 17.3 `Shadows.hlsl`, `ShadowUtils.GetScaleAndBiasForLinearDistanceFade`).
The first delivery fitted 1.75 x the rig distance from *view depth*, which left the top corners partly or wholly faded
(about 76% faded on the web tier at a 0.08 top inset). `ShadowFit` now takes, on every rig `Apply`, the farthest
straight-line distance from the camera to the ground hits of the four viewport corners (the current aspect and top inset
included; ground plane y = 0, the terrain's lowest point, so the fit is never short) and divides it by
(1 - cascadeBorder). It clamps to 3 km .. 1,000 km (the far clip). `LightingTests.TheShadowDistanceCoversTheWholeView`
checks 6/45/120/250 km x 16:9, 21:9, 4:3 x top insets 0, 0.08, 0.3, 0.5 x borders 0.1 and 0.2.

**Decision: web-tier cascade border 0.2 -> 0.1** (`Mobile_RPAsset` `m_CascadeBorder`). With one cascade, the border is
the fade band at the edge of the fitted range. A tenth keeps the fade off-screen while spending less of the 2048 map past
the view (the fitted distance is 1/0.9 rather than 1/0.8 of the farthest corner). PC keeps 0.108 (4 cascades).

| Run | Lines |
|---|---|
| headless, first delivery (`8b20507`, 1.75 x D view-depth fit, border 0.2) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.11 spread_ms=0.10 median_p95_ms=3.00`<br>`PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.43 spread_ms=0.11 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.12 spread_ms=0.43 median_p95_ms=11.00`<br>`PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.39 spread_ms=0.17 median_p95_ms=8.00` |
| headless, fix round 1 (corner fit, border 0.1) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.03 spread_ms=0.09 median_p95_ms=3.00`<br>`PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.42 spread_ms=0.08 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.87 spread_ms=0.46 median_p95_ms=11.00`<br>`PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.12 spread_ms=0.11 median_p95_ms=8.00` |

Budget check, fix round 1 (`median(all) - median(-shadows) <= 2.5 ms + the larger spread`):
- gameplay03: -0.39 ms against 2.59 ms: within.
- diorama02: 2.75 ms against 2.96 ms (2.5 + 0.46): within, with 0.21 ms to spare.

**For Task 10 (review M5).** At gameplay03, `all` is faster than `-shadows` by more than both spreads, in both deliveries.
Three runs understate the run-to-run noise, so the 0.2 ms headroom at diorama02 means little. Re-measure diorama02
shadows with more repeats (at least 7) and headed. If the tier review needs headroom, the levers are:
- terrain shadow LOD or pixel error (the whole terrain is drawn into the shadow map at about 400 km);
- a cheaper fit at the far zoom (fit to the map's extent rather than the frustum once the frustum is wider than the map);
- a web-tier shadow map of 1024 at the far zoom only.

**Rendered coverage (review I2, I3).** `ReliefCastsShadowsAtMapScale` runs at both tiers over the Dead Sea escarpment at
60 km. Every other registered visual except `water` is off and the settlement markers are hidden, so only relief casts.
Results: shadowed share **3.06% (web)** and **3.12% (PC)**; the control at the asset's own 50 m is **0.00%** at both.
Hiding the markers moved the web share from 3.03% to 3.06%, which is noise, so the cubes were never a material part of
it. The 2% floor stays: it allows a third of the measured share to be lost to the merge (Task 3's tilt-shift softening
edges, Task 4's grade compressing darks, Track B's haze lifting distant shadow), and the control's 0.00% shows the
separation is clean. If the merge squeezes it further, switch to a relative test (`lit < flat * 0.9`), not a lower floor.

**Ambient (review M4). Superseded by Task 4:** the explicit probe described here was never read. In Trilight mode Unity
replaces it with its own linear conversion of the colours, and Task 4 removed `MapLighting.AmbientProbe`. The evaluated
values below are that conversion, and they match the constants once converted to sRGB; see "Task 4: grade". The original
note follows. `MapLighting.AmbientProbe` builds the trilight from `AddAmbientLight` plus two
`AddDirectionalLight` lobes. The lobes add constant and linear terms in every direction, so the probe evaluates darker
than its constants, which are inputs, not the evaluated colours. Evaluated values, pinned in `LightingTests`: up
(0.216, 0.255, 0.326), horizon (0.184, 0.179, 0.171), down (0.119, 0.103, 0.080); the constants are sky (0.52, 0.60,
0.72), equator (0.48, 0.46, 0.42), ground (0.26, 0.22, 0.17). The images were judged on the evaluated values, and Track
B's thresholds were derived with them, so a least-squares refit would be a look change for Task 4, not a fix here.

## Task 3: tilt-shift
Two bands, blurred by a half-resolution render-graph pass (`TiltShiftFeature` on both renderers, `Statecraft/TiltShift`):
- the blur is a separable 9-tap Gaussian whose radius is `TiltShift.Weight(y) * MaxRadiusUv`;
- the focus sits at the middle of the map area below the top bar;
- the blur weakens to 35% between 60 km and the 250 km overview.
- since issue #390, the far (top) band is lighter at the gameplay zoom (see "Issue #390" below).
- since issue #458 (the owner's ruling of 2026-10-14), the near (bottom) band is lighter at the gameplay zoom and about
  gone at the overview (see "Issue #458" below).

URP's far-only Gaussian DoF is retired. Toggle: `tiltshift`; the URL flag `notiltshift` (desktop: `-notiltshift`) boots with
it off.

**Constants (fix round 1, review I2).** HalfWidth 0.13, Feather 0.17 and MaxRadiusUv 14/1080 (was 0.17, 0.18 and
10/1080). Full blur now starts 0.30 from the focus. That is the bottom and top 20% with no bar; under the web tier's top
bar (8.8% of the screen) it is the bottom 15.5%, and 15.5% between the band and the bar. The promo has about 15% at the
bottom and 20% at the top.

**Edit-mode ratios** (gradient energy shifted / sharp, gameplay pose, 960x540, no bar):

| Tier | top 0.90–0.98 | bottom 0.02–0.10 | middle | top 0.81–0.87 | bottom 0.13–0.19 | top at 1920x1080 |
|---|---|---|---|---|---|---|
| Mobile | 0.16 | 0.24 | 1.00 | 0.17 | 0.22 | 0.14 |
| PC | 0.11 | 0.18 | 1.00 | 0.12 | 0.15 | 0.11 |

TopInset 0.3: focus band 1.00, upper band 0.13. (Before fix round 1: Mobile top 0.22 and bottom 0.31; PC 0.16 and 0.24.)

**WebGL band check, with the real top bar (review m3).** I compared the checkpoint's `gameplay03.png` with a
`?shot&notiltshift` shot from the same build, using columns 30–80% (clear of the info panel and minimap) and rows measured
from the top. The bar ends at 0.088.
- Ratios by row (from the top):

  | Rows | 0.10–0.275 | 0.30 | 0.325 | 0.35 | 0.375 | 0.425–0.675 | 0.70 | 0.725 | 0.75 | 0.775 | 0.80–1.00 |
  |---|---|---|---|---|---|---|---|---|---|---|---|
  | Ratio | 0.01 | 0.13 | 0.32 | 0.60 | 0.86 | 1.00 | 0.74 | 0.45 | 0.22 | 0.07 | 0.01 |

- The sharp band is centred at about 0.54 from the top. The focus is at (1 − 0.088)/2 = 0.456 from the bottom, so this is
  the right way up; a flipped composite would centre at 0.456 from the top.
- The review's check: rows 0.20–0.30 from the top (nearer the bar) keep **0.02** of their detail, and rows 0.70–0.80 keep
  **0.37**.
- **Coverage.** Fully blurred (ratio <= 0.05) from the bar to 0.30 (**21% of the screen at the top**) and from 0.80 to the
  bottom (**20% at the bottom**). The promo has about 20% at the top and 15% at the bottom. By the design weight (weight =
  1), the coverage is 15.6% at each end.

**Profile** (1920x1080, DPR 1, render scale 0.80, 3 interleaved runs per configuration), fix round 1 build:
```
headless (web_check.py)
PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.96 spread_ms=0.13 median_p95_ms=3.00
PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=2.21 spread_ms=0.79 median_p95_ms=4.00
PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.65 spread_ms=0.20 median_p95_ms=10.00
PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=9.72 spread_ms=0.26 median_p95_ms=11.00
headed (headed.py profile --dpr 1)
PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.01 spread_ms=0.39 median_p95_ms=3.00
PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=1.89 spread_ms=0.35 median_p95_ms=3.00
PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.11 spread_ms=0.54 median_p95_ms=11.00
PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=10.55 spread_ms=0.59 median_p95_ms=11.00
```
Cost `median(all) - median(-tiltshift)`, all within the 1.5 ms budget and within the spreads:

| | gameplay03 | diorama02 |
|---|---|---|
| headless | −0.25 ms | −0.07 ms |
| headed | +0.12 ms | −0.44 ms |
| first build: headless | −0.08 ms | +0.48 ms |
| first build: headed | +0.01 ms | +0.07 ms |

## Task 4: grade
`MapGrade` adds the following to the rig's runtime `VolumeProfile`, all behind the `grade` toggle, with LDR grading on both tiers:
- a Neutral tonemapper;
- post exposure +0.15, contrast 14, saturation 16 (superseded at the Merge point: `MapGrade.Saturation` is 0, see "Merge point");
- white balance temperature **5** (the plan says 9; see the merge arithmetic below) and tint 2;
- split toning: teal shadows (0.36, 0.52, 0.60), amber highlights (0.96, 0.78, 0.55), balance −15;
- a dark-navy vignette: 0.26, smoothness 0.45.

**What was there before (measured).** The pipeline assets' own volume profile, `Settings/SampleSceneProfile` (the URP template's), already applied the following to every frame:
- Neutral tonemapping;
- bloom 0.25 above 1 (high quality);
- a 0.2 black vignette.

So the build was not "without a tonemapper". The `grade` toggle off falls back to that profile, not to a raw image. Two consequences:
- The control in `TheSunlitDesertDoesNotClip` sets the grade's tonemapper to None. That overrides the template's Neutral, and the graded Negev then clips 2.3% of its pixels, against 0.00% with Neutral.
- URP strips the tonemapper's uber variants unless some VolumeProfile *asset* has a Tonemapping component. The runtime profile is not an asset, so it does not count. `TheGradeIsWarmLandTealShadowsAndAGentleVignette` checks that such an asset exists. Today two assets carry one: the template's `SampleSceneProfile` and the global settings' `DefaultVolumeProfile`. The check fails only if both lose their Tonemapping component. If Task 10 drops the template profile (the bloom decision), it should add a project-owned VolumeProfile with Neutral, so the guarantee no longer rests on URP template leftovers.

Bloom is the template's, not this plan's; Task 10 decided: bloom ships off on both tiers (see "Tier decisions").

**Readability (review I9), from edit mode at 960x540, with the tilt-shift off for these detail checks.**
- ΔE76 between the selection band and the land 1 km inside the hex:

  | Pose | Graded | Ungraded |
  |---|---|---|
  | gameplay03 | 32.3 | 24.5 |
  | diorama02 | 48.6 | 36.6 |

- Colour-blind check (Machado 2009, gameplay pose):

  | Simulation | sea vs land | selection vs land | selection vs sea |
  |---|---|---|---|
  | Deuteranopia | 68.6 | 31.8 | 98.6 |
  | Protanopia | 67.8 | 31.5 | 97.6 |

- Saturation rises from 0.438 to 0.564.
- The vignette's corner/centre ratio drops from 0.603 to 0.483.
- In the checkpoint's deuteranopia and protanopia copies, the coast (blue against yellow) and the hex lines stay clear.

**Ambient (review M4, closed by measurement).**
- **How the probe actually works.** `MapLighting.AmbientProbe()` was never what the shaders read. In Trilight mode Unity replaces any assigned probe with its own conversion of the three colours, at once. It does this in the editor, and in the WebGL player too: a diagnostic build logged the getter at Apply, frame 120 and frame 600.
  - Editor renders with our probe, a least-squares refit and a black probe are identical to 3 decimals. Changing the trilight colours changes the image.
- **Why it looked like 40%.** Unity converts the sRGB colours to linear. The "40% of nominal" (horizon 0.18 against 0.48) compared linear values with sRGB constants. In sRGB the horizon is (0.466, 0.460, 0.450) against the equator's (0.48, 0.46, 0.42).
- **Decision: keep the look.** The dead explicit probe is removed, which changes nothing rendered. `LightingTests` now asserts the horizon is within 0.05 of the equator in sRGB.
- **Why no brighter refit.** A refit that brightens the ambient towards the constants (trilight colours raised so the probe's linear values reach the constants) does not help. Measured with the grade, in editor renders:

  | Pose | Warmth | Saturation | Luma | Contrast |
  |---|---|---|---|---|
  | gameplay03 | 0.142 → 0.082 | 0.572 → 0.518 | +0.03 | unchanged |
  | diorama02 | 0.154 → 0.090 | 0.526 → 0.458 | +0.03 | unchanged |

  That works against the merge's warmth target. It would also shift every Track B threshold, which were derived with the current ambient. Nothing Track B measures moves.

**Grade terms at diorama02 (editor, Mobile tier, no UI; each line takes one term out of the full grade).** With the full grade: warmth 0.154, saturation 0.526, contrast 0.110. With the grade off: warmth 0.120, saturation 0.419, contrast 0.088. The temperature here was 9.

| Term | Warmth | Saturation | Other |
|---|---|---|---|
| Saturation 16 | +0.027 | +0.084 | |
| White balance 9 | +0.030 | +0.014 | |
| Split toning | −0.039 (the teal shadows) | −0.041 | |
| Vignette | −0.018 | | |
| Contrast | | | +0.011 contrast |

**Merge arithmetic (diorama02 warmth, WebGL frame).**
- **Track A alone at Temperature 9:** t04 is 0.118 (t03 was 0.088).
- **Track B's contribution:** t06-terrain-r1 0.0384 − t01 0.0171 = **+0.021** before the grade.
- **How the grade scales it:** the grade's saturation (×1.16), contrast and exposure scale chroma by about 1.0–1.3, so Track B's +0.021 becomes +0.021 to +0.027.
- **At Temperature 9:** 0.118 + 0.021…0.027 = **0.139–0.145**. That is already at the promo's 0.145, before Track B's Task 7 haze and palette work.
- **Lowering the temperature:** one unit is about 0.003 of frame warmth: 0.030 for 9 units in the editor, × 0.88 for the WebGL frame's UI share. So Temperature 5 gives 0.118 − 4 × 0.003 = 0.106, and the checkpoint measured **0.106**.
- **Expected merged diorama02 warmth: 0.106 + 0.021…0.027 = 0.127–0.133.** That is at least 0.087 by 0.04, and leaves 0.012–0.018 for Track B's later warming.
- **Contrast:** t03 0.153 → t04 0.170, plus Track B's +0.014 (t06-r1 0.1731 − t01 0.1588), gives about **0.18–0.19** after the merge, moving towards the promo's 0.200.

| WebGL | Saturation | Contrast | Warmth | Promo (sat / contrast / warmth) |
|---|---|---|---|---|
| diorama02, t03 | 0.422 | 0.153 | 0.088 | 0.425 / 0.200 / 0.145 |
| diorama02, t04 (temp 9, `9603700`) | 0.52 | 0.169 | 0.118 | |
| diorama02, t04 (temp 5, `2ac7b6e`) | 0.51 | 0.170 | 0.106 | |
| gameplay03, t03 | 0.444 | 0.155 | 0.082 | 0.382 / 0.203 / 0.064 |
| gameplay03, t04 (temp 5) | 0.55 | 0.174 | 0.107 | |

**Over the promo (owner's call).**
- Saturation is now about 0.09 above the promo at diorama02 and 0.17 above it at gameplay03. The lever is `Saturation` (16 → 8 is about −0.04).
- Gameplay03 warmth is already above its promo (0.064) on Track A alone, and Track B adds about +0.03. The promo's gameplay frame has green fields and orchards, which is the art track's vegetation, and a global grade cannot fix that without cooling the diorama.

**Seam test.** `TheSeaBeyondTheEdgeMatchesTheTerrainSea` measures the seam twice:
- **Before the grade, against 0.012.** This isolates the grade, which is a per-pixel LUT plus a vignette that is flat this close to the centre, so it cannot open a seam.
- **On the shipped, graded frame, against 0.024.** The grade scales the sea's chroma by about 2: the sub-step mismatch between the terrain sea and the skirt shader goes from 0.008 to 0.016.

At the merge, Track B's patch-mean version should keep this graded/ungraded loop.

**Profile** (headless, 1920x1080, DPR 1, scale 0.80, 3 interleaved runs; build `9603700`. Temperature only changes LUT values, so the cost is the same at `2ac7b6e`):
```
PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.95 spread_ms=0.39 median_p95_ms=3.00
PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=1.93 spread_ms=0.37 median_p95_ms=3.00
PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.74 spread_ms=0.06 median_p95_ms=11.00
PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=9.74 spread_ms=0.02 median_p95_ms=11.00
```
The grade costs +0.02 ms at gameplay03 and 0.00 ms at diorama02, inside the noise. The tonemapper and the vignette were already in the uber pass (the template profile), and the LUT pass ran before too.

## Baked textures (Task 5)
`tools/terrain_bake.py` regenerates `unity/Assets/Textures/Map/terrain_bake.png`, `coast.png` and `ao.png` from the committed
heightmap in about 10 s. The output is deterministic per toolchain: re-running it with the pinned numpy 2.5.3 and
pillow 12.3.0 on the same machine gives byte-identical PNGs. Another platform may differ in its zlib build or in
1-ulp `sin`/`arctan` results. Compare decoded pixels (`np.array_equal`), not file bytes; R and B may then differ by
one level here and there.

## Task 6: terrain
The Statecraft Terrain Lit Shader Graph (`tools/terrain_graph.py` + `StatecraftTerrain.hlsl`): one terrain layer (one
pass) instead of seven, no basemap fallback. WebGL build (`-compression disabled`): `BUILD Succeeded WebGL ->
Builds/Web-disabled: 73.4 MB on disk, 0 errors` (Task 1: 70.9 MB; +2.5 MB, the two baked textures), no `Shader error`.
GLES3 check: `ShaderChecks.Gles3Errors` is empty for `Statecraft/StatecraftTerrain` (no `#extension ... : require`;
`AllowedRequires` not needed).

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.58 spread_ms=0.93 median_p95_ms=2.00`<br>`PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=1.88 spread_ms=0.87 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=1.67 spread_ms=0.40 median_p95_ms=3.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.25 spread_ms=0.47 median_p95_ms=8.00`<br>`PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=7.13 spread_ms=0.11 median_p95_ms=8.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.16 spread_ms=0.20 median_p95_ms=8.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=509 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3851 runs=3 cpu=1` (budgets 743 / 5321 ms: within) |

Against the Task 1 baseline (`all`): gameplay03 2.29 -> 1.58 ms, diorama02 6.84 -> 7.25 ms (+0.41 ms, inside 2.5 ms plus
the spread). Haze costs nothing measurable (`all` - `-haze`: -0.30 / +0.12 ms, both inside the spreads). The boot phase
`biome texture baked` takes 94-106 ms (it replaces Task 1's `biome colours blended`).

### Task 6 fix round 1
Haze retuned (review I1: start 200 km, scale 250 km, max 0.45, rift boost 0.6, colour sRGB (0.84, 0.74, 0.58)), ground
palette x1.107 in sRGB, curvature gain 0.40, graph and nodes at Single precision, HDRP target dropped, trilinear map
textures (bake aniso 2), the fine noise octave faded by its pixel footprint. The AO is its own uncompressed R8 texture
(`ao.png`): measured against the source PNG, BC1 stepped it by 2+ levels on 404 of 681 broad Judean texels (59%); R8
gives 0 of 681 (Judean slopes) and 0 of 6206 (Negev plateau). WebGL build: 76.9 MB (+3.5 MB for `ao.png`), no
`Shader error`. GLES3 report (with vis-light `e8b1c48`'s serialized-subshader loop): 0 errors and no `require` for
`Statecraft/StatecraftTerrain`, `Statecraft/HexSelection`, URP Lit (the sea on this branch) and URP Terrain Lit.

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.41 spread_ms=0.88 median_p95_ms=4.00`<br>`PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=2.34 spread_ms=0.48 median_p95_ms=4.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.46 spread_ms=0.07 median_p95_ms=4.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.29 spread_ms=0.07 median_p95_ms=8.00`<br>`PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=7.31 spread_ms=0.10 median_p95_ms=8.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.34 spread_ms=0.09 median_p95_ms=8.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=452 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3559 runs=3 cpu=1` (budgets 743 / 5321 ms: within) |

Art (WebGL, whole frame; vis-ground has no Task 2 light, so compare with t01): diorama02 warmth 0.017 (t01) / 0.007
(t06) -> 0.038, saturation 0.36 -> 0.41, contrast 0.171 -> 0.173, palette distance 0.85 -> 0.78; map land alone (column
x 0.45-0.6) R-B 0.158 -> 0.229, saturation 0.254 -> 0.363, luma 0.576 -> 0.572. gameplay03 warmth 0.009 (t01) / 0.017
-> 0.040, contrast 0.163 -> 0.173, palette distance 0.69 -> 0.66. The checkpoint also shoots the 6 km hero zoom over the
Judean slopes and the Negev (`PromoPose.CloseChecks`, haze off): no AO steps.

## Task 7: sea
One water function (`StatecraftMap.hlsl` `ScWaterShade`) on both sides of the map's edge: the terrain graph outputs it as
emission on a black, fully metallic, fully occluded surface, and the border skirt's `Sea.mat` (`Statecraft/Sea`, passes
`Sea`, `DepthOnly`, `DepthNormals`) returns it directly. (Since issue #195 the terrain graph outputs only the black surface;
`MapScene`'s `TerrainWater` mesh adds the water on top with `Sea.mat` in its `_ScTerrainWater` mode. See "Issue #195".) It is driven by Task 5's coast distance: a turquoise -> teal -> navy
ramp over 12 km, foam within 350 m, the Dead Sea pale turquoise with a white salt rim. It samples the main light's shadows
itself, takes the cooler water haze (`ScHazeWater`), and falls back to the flat sea colour under the `water` toggle. WebGL
build: 77.1 MB, no `Shader error`. GLES3: `ShaderChecks.Gles3Errors` is empty for `Statecraft/Sea` and the terrain (no
`require`).

Deviations from the plan:
- **Glint 0.06, not 0.25.** With `Map.unity`'s default 50-degree sun, the camera's 50-degree pitch at 120 km meets the
  reflection at yaw 180. There the 0.25 lobe lit a warm patch rising 0.08 in sRGB red over 6 km of open sea, and the seam
  test (samples 5 km apart) read it as a 0.031 seam. A profile across the edge showed no step at the edge itself, and the
  difference scaled with the glint. At 0.06, every seam line is <= 0.004.
- **The held skirt is averaged along the edge.** Holding each edge vertex's own height for 20 km
  (`SkirtFall = {0, 0.05, 0.15, 0.45, 1}`) drew the edge's ridges out as long stripes across the Jordan skirt (checkpoint
  t07-sea, first run). Rings 1-4 now hold the edge height averaged over +-8 km (`MapBuilder.SkirtHoldSmoothingM`). Ring 0
  still meets the terrain exactly. New test `TheHeldSkirtCarriesThePlateauNotTheEdgesRidges`: the mean spoke-to-spoke step
  20 km out is 8.5 m against 36.7 m at the edge (31.2 m before).

| Run | Lines |
|---|---|
| headless | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.38 spread_ms=0.42 median_p95_ms=4.00`<br>`PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=2.35 spread_ms=0.08 median_p95_ms=4.00`<br>`PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=1.70 spread_ms=0.61 median_p95_ms=3.00`<br>`PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=1.72 spread_ms=0.27 median_p95_ms=2.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.64 spread_ms=1.56 median_p95_ms=9.00`<br>`PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=7.67 spread_ms=0.29 median_p95_ms=8.00`<br>`PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=7.78 spread_ms=0.35 median_p95_ms=9.00`<br>`PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.48 spread_ms=0.02 median_p95_ms=8.00` |
| boot, `--cpu 1` | `BOOT map_build_ms median=472 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3592 runs=3 cpu=1` (budgets 743 / 5321 ms: within) |

Against Task 6 fix round 1 (`all`): gameplay03 2.41 -> 2.38 ms, diorama02 7.29 -> 7.64 ms (+0.35 ms, inside 2.5 ms plus the
spread). Water costs 0.68 ms at gameplay03 (`all` - `-water`, against a 0.61 ms spread) and nothing measurable at
diorama02 (-0.14 ms), well inside 2.5 ms. The profile ran on the build before the skirt averaging, which changes no
shader. The boot lines are from the final build.

Art (WebGL): diorama02 saturation 0.41 -> 0.39, contrast 0.173 -> 0.186, warmth 0.038 -> 0.058, palette distance 0.78 ->
0.76. gameplay03 contrast 0.173 -> 0.194, palette distance 0.66 -> 0.65. The sea grades from turquoise shallows with a
foam line to navy, and the Dead Sea is pale turquoise with white salt rims. Coast streaks: none visible. A 12x high-pass of
the gameplay03 coast shows only the foam band's waves and single-level output steps in the deep water, so the bake's
distance needs no smoothing (Task 5 carry). Plateau edge (diorama02): the Jordanian plateau now continues past the east
edge at the edge's broad height instead of sagging at once, as a flat muted band level with the map's eastern rim. A
faint low-contrast banding remains there at grazing angles.

### Task 7 fix round 1
- **Haze at full width (review I1, the Task 6 carry).**
  - At diorama02, the haze toggle moves the far band 0.047 and the near band 0.002 over the full width. Task 6 measured
    0.023 / 0.026, with the sea and skirt unhazed. Restricted to the land column it was 0.069 / 0.026.
  - So `TheHazeThickensWithDistance` and `TheHazeStartsBeyondTheNearestDioramaLandAndGrowsWithDistance` sample x 0..1
    again. By distance: near 0.0019, mid 0.0212, far 0.0473.
  - `TheOpenSeaStaysBlueUnderTheHaze` now asserts that the water haze applies. The open sea at about 300 km moves 0.059
    in luma with the haze on, and 0.000 with `ScHazeWater` dropped on both sides (the red check).
- **Seam test close pair (M1).**
  - The pair sits at +-600 m across the edge. Each side is the mean of five 7x7 patches, 2 km apart along the edge,
    because a single 1 km patch carries one wave cell or glint sparkle on its own (up to 0.012).
  - A step of 5% on the skirt sea only within 1.5 km of the edge passes the +-2.5 km pairs (0.000) and fails the close
    pair (0.015). All 16 lines are <= 0.004.
- **Skirt hold (M4).**
  - The hold is now a running mean over the edge's own samples (prefix sums, +-8 km). It replaces the 17 taps 1 km apart.
  - East edge, spoke-to-spoke step 20 km out: 8.5 -> 5.2 m. What remains is the plateau's own trend along the edge.
  - The test bound is under 0.18 x the edge's step.
  - The faint grazing-angle banding in diorama02 is gone.
- **Land skips the water shading (M3).** `[branch]` on `water > 0`.
  - gameplay03 `all` minus `-water`: 0.03 ms before, 0.01 ms after. Both are inside the spreads, so there is no
    measurable change on this GPU.
  - Before, on the build at `6757168`: `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.49 spread_ms=0.17`
    and `visual=-water ... median_avg_ms=2.46 spread_ms=0.06`.
  - After: `visual=all ... median_avg_ms=2.55 spread_ms=0.07`, `visual=-water ... median_avg_ms=2.54 spread_ms=0.02`;
    diorama02 `all` 7.34 ms (before 7.38 ms).
  - The earlier 0.68 ms water cost was run-to-run noise.
- **Build and boot.** WebGL 77.1 MB, no `Shader error`. Boot `--cpu 1`: `map_build_ms` 469, `first_frame_ms` 3598
  (budgets 743 / 5321).

## Task 8: grid
The hex grid is drawn by the terrain graph and by the skirt's `Sea.shader` from one distance field (`ScHexEdgeDistance`,
tools/hexgrid.py's layout; `HexGrid` is the C# twin), anti-aliased with `fwidth`, 1.5 px wide, light on land (0.25) and
water (0.35), fading to 0.6 of that from 40 km to 240 km camera distance (superseded: land 0.35, water 0.10, fade to
0.35 plus a footprint fade; see "Task 8 fix round 1" and "Merge point"). The `MeshTopology.Lines` overlay, its material,
`OverlayClearanceM` and its boot phase are gone; the selected-hex band sits 30 m above the ground (was 140 m). Toggle
`grid`, registered in `MapShaderGlobals.Apply` through `assets.OnTeardown`. All numbers here are pre-grade (Track A's grade
and tilt-shift are not in this worktree).

- **Rendered tests (EditMode).** Edge luma change 0.059 (hills, 30 km). Grid coverage: 3.08% at 20 km, 18.15% at
  250 km (bound 0.05%-25%). Rift hex lines: Beit She'an 0.062, Jericho 0.058, Dead Sea shore / Arava 0.016 (light lines
  on the bright desert are faint; that test's bound is 0.012, the plan's 0.03 holds only off the desert).
- **Seam.** Task 7's pairs and close strip, grid off: 16 lines, max 0.004. Grid on: 12 asserted lines, max 0.008; the
  close strip at yaw 90/270 is logged only (0.014 at yaw 270: the x = 0 hex line falls unequally into the two
  foreshortened patches). The plan's hex-centre pairs at x = +-4330 m were dropped: the middle-row pair differs by 0.027
  at yaw 180 with the grid off too (a warm sun glint on the terrain side), a pre-existing water effect.
- **Profile (WebGL, 1920x1080).**
  - `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.42 spread_ms=0.74 median_p95_ms=4.00`
  - `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=2.41 spread_ms=0.22 median_p95_ms=4.00`
  - `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.30 spread_ms=0.12 median_p95_ms=8.00`
  - `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=7.28 spread_ms=0.34 median_p95_ms=8.00`
  - The grid costs 0.01-0.02 ms, inside the spreads; with the line mesh's draw call gone, gameplay03 `all` is 2.42 ms
    (Task 7 fix round 1: 2.55 ms).
- **Build and boot.** WebGL 77 MB, no `Shader error`; no GLES derivative error (no `#pragma require derivatives`
  needed). Boot `--cpu 1`: `BOOT map_build_ms median=419`, `BOOT first_frame_ms median=3442` (Task 7: 469 / 3598).
- **Look (t08-grid).** The sea lattice runs on past the map's edge with no step. At the 250 km overview the sea lattice is
  dense and reads strongly; land lines are faint, nearly invisible on the desert.

### Task 8 fix round 1
- **Line anti-aliasing (review I1).** `fwidth` of the V-shaped edge distance collapsed on the line itself. The fix projects
  the derivatives of the continuous grid coordinate on the nearest edge's normal (metres per pixel across the edge) and
  box-filters: `saturate(0.5 * widthPx + 0.5 - d / aa)`.
  - New rendered test `GridLinesAreUnbrokenAndAboutTheirWidthAtGameplay03` (1920x1080, opacities forced to 1, linear luma).
    - Red at b409955: sea peak ratio 0.41 along one edge, FWHM 2.74 px.
    - Green: sea ratio 0.85, FWHM 1.66 px (1.51-1.95); land FWHM 1.62 px (1.37-1.97).
- **Weights (review I2).** `GridWaterOpacity` 0.35 -> 0.10. The distance fade is now lerp(1, 0.35). Lines also fade out as a hex
  shrinks from 30 to 10 px on screen (circumradius over the pixel footprint). Land stays 0.25 until the grade merges.
  - Sea line in `gameplay03` (x 120-170, y 480-560): background 49, peak 83, +34, ratio 1.69, 2 px wide. Before: +86,
    2.75, 4 px. Promo (x 470-520, y 550-580): background 109, peak 151, +43, ratio 1.39. Our ratio is higher only
    because our sea is darker (pre-grade).
  - diorama02 near sea line: +6 over 73 (before +50). Grid coverage at 1920x1080: 0.74% at 20 km, 0.76% at 250 km
    (was 3.1% / 18.2% at 960x540).
- **Seam (M1).** The close strips sit on even hex rows (x = 0 a hex centre); all 32 lines (grid off and on, four yaws)
  are asserted, max 0.004.
- **Rift.** Arava line change 0.015 (bound 0.012, land opacity unchanged).
- **Profile** (machine load average about 7, spreads up to 1 ms):
  - `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.71 spread_ms=0.97 median_p95_ms=3.00`
  - `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=2.22 spread_ms=0.99 median_p95_ms=4.00`
  - `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=7.23 spread_ms=0.67 median_p95_ms=8.00`
  - `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=7.22 spread_ms=0.54 median_p95_ms=8.00`
- **Build.** WebGL, no `Shader error`; golden values OK in the checkpoint run.

## Checkpoints

The images live in the owner's local `captures/visual/` folder (git-ignored); owner verdicts are filled in as the checkpoints are reviewed.

| Task | Name | Images | Owner verdict |
|---|---|---|---|
| 1 | t01-before | captures/visual/t01-before/*-compare*.png | (owner) |
| 2 | t02-light | captures/visual/t02-light/*-compare*.png | (owner) Note: at the new 6 km hero zoom the view is filled by the 600-1,800 m placeholder marker cubes, so judge the zoom range, not the close-up art, until the settlement art track replaces them. |
| 3 | t03-tiltshift | captures/visual/t03-tiltshift/*-compare*.png (re-shot in fix round 1: bands 0.13/0.17, radius 14/1080) | (owner) Note: the map labels are still world-space text, so they blur with the map in the top and bottom bands until Task 9 moves them to UI Toolkit. |
| 4 | t04-grade | captures/visual/t04-grade/*-compare*.png (with -deuteranopia / -protanopia copies) | (owner) Note: saturation is above the promo (0.51/0.43 diorama, 0.55/0.38 gameplay); `MapGrade.Saturation` is the lever. |
| 6 | t06-terrain | captures/visual/t06-terrain/*-compare*.png, editor/*-compare*.png | (owner) |
| 6 (fix 1) | t06-terrain-r1 | captures/visual/t06-terrain-r1/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 7 | t07-sea | captures/visual/t07-sea/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 7 (fix 1) | t07-sea-r1 | captures/visual/t07-sea-r1/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 8 | t08-grid | captures/visual/t08-grid/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 8 (fix 1) | t08-grid-r1 | captures/visual/t08-grid-r1/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png | (owner) |
| 9 | t09-labels | captures/visual/t09-labels/*-compare*.png | (owner) |
| borders | promo-borders-before, promo-borders-noproof, promo-borders-r2 | captures/visual/promo-borders-before/*-compare*.png (main 807e300), captures/visual/promo-borders-noproof/*-compare*.png (the branch as played: no data, no border), captures/visual/promo-borders-r2/editor/*-compare*.png (the synthetic `--borderproof` checker, labelled on screen; editor renders only), all at d1973db | (owner) Note: no real region data yet, so play shows no border (see "Region borders"). The earlier promo-borders-proof / -after rows ran on uncommitted work over 807e300 (old ring pattern). |
| merge | t-merge | captures/visual/t-merge/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png (before the grade re-tune: t-merge-pre, t-merge-r1) | (owner) Note: the land is one dry-grass gold with no fields, orchards or cities yet; that gap is content (art track), not the grade. |
| merge (fix 1) | t-merge-r3 | captures/visual/t-merge-r3/*-compare*.png (t-merge-r2 = with the rejected border colour) | (owner) Note: olive coastal plain and valleys; the selected hex is now the promo's ivory ring colour, gold stays the UI chrome's. |
| 10 | t10-tiers | captures/visual/t10-tiers/*-compare*.png, hero06judea.png, hero06negev.png, editor/*-compare*.png; weak GPU: captures/visual/t10-swiftshader | (owner) Note: render scale 1.0 on the web (sharper lines and pills); bloom, SSAO and the depth/opaque copies off; the colour is unchanged within 0.001. |

## Task 9: labels

Settlement names are UI Toolkit pills (`MapLabelsPresenter`, EB Garamond SemiBold on navy with a gold edge) in the
click-through `map-labels` layer, placed in `GameRoot.LateUpdate` with an inline pixel `translate` and hidden while the
game loads. The world-space `TextMesh` labels are gone.

Frame time, headless, DPR 1, `res=1920x1080 scale=0.80` (`?vprofile`, `machine_lock.py --exclusive --wait-unity`):

| Pose | all | -labels | none | all - (-labels) | budget |
|---|---|---|---|---|---|
| gameplay03 | 2.30 (spread 0.15) | 2.36 (0.06) | 2.34 (0.04) | -0.06 ms | <= 1 + 0.15 ms: within |
| diorama02 | 6.82 (0.06) | 6.55 (0.02) | 6.61 (0.14) | +0.27 ms | <= 1 + 0.06 ms: within |

The profile holds the camera still, so after the first frames `Refresh` returns early (unchanged view-projection,
panel size and zoom); the cost measured is the drawing of the pills, not the per-frame placement while the camera moves.

Deviations from the plan text, each forced by a test:
- Projection: `RuntimePanelUtils.CameraTransformWorldToPanel` flips y with `Screen.height`, which is wrong when the camera
  draws to a texture of another size (edit-mode tests: 600 px off). The presenter maps the camera's viewport point
  through `camera.rect` onto the panel's own size instead.
- Declutter at the overview: the Tel Aviv - Lod cluster's markers sit 10-60 px apart there, and a town's pill could not
  avoid a neighbour's marker with any anchor (Rishon LeZion's pill ended 10 px under Jaffa's marker). A pill other than
  a city's is now also hidden when it comes within `NeighbourClearPx` (2 x LiftPx + MarginPx = 16 px) of another
  settlement's marker top. At 90 km this hides nothing (11 of 25 shown, as before); at the overview 10 of 25 are shown.
- City pills: 20 px text and 8 px side padding at first. Fix round 1 restores the plan's 22 px and 10 px (see below).
- `ThePillIsEbGaramondOnNavyWithAGoldEdgeAndHiddenWhileLoading` uses a 1920x1080 panel instead of `UiTestKit`'s: the
  batch-mode screen gives that panel about 0.39 px per point, and UI Toolkit rounds the 1 px border up to one physical
  pixel (2.57 points).

### Task 9 fix round 1 (review I1-I5, M1-M7)

- Panels: pills are culled against the cached rects of the top bar, the info panel, the minimap and the event card,
  marked dirty by GeometryChangedEvent; a pill must lie wholly on screen, and a marker under a panel is no one's
  neighbour. A container resize re-places the pills before the repaint (M4).
- Anchors: the USS anchor class is tried first, then lower right, lower left, lower centre and centred below the marker,
  as a pixel shift on the slot's translate. The legibility rule (more than 2 x own gap + MarginPx from every other
  visible marker top) applies to every pill; a city whose anchors all crowd a neighbour keeps the first that is clear of
  panels and pills. A pill hidden in the last refresh needs HysteresisPx = 2 more (M7).
- Shown sets (MapLabelTests): 90 km: tel_aviv jaffa lod ramla petah_tikva rishon_lezion rehovot netanya (every visible
  marker); overview: jerusalem tel_aviv lod gaza beersheba petah_tikva netanya; diorama02 with the top-bar inset also
  labels Hadera (checkpoint).
- City pills are back to the plan's 22 px and 10 px padding (towns 19 px), so cities read larger (M5). At the
  overview Tel Aviv's pill takes the lower-right anchor, over the sea.
- Refresh allocates nothing (`Is.Not.AllocatingGCMemory`). Editor Mono, 240 refreshes while turning and zooming:
  `LABELS refresh moving avg_us=27 p95_us=38`.
- `?vprofile&vmoving` (FrameProfile, additive): every run restarts at its pose and turns 0.5 degrees per frame.
  Headless, DPR 1, `res=1920x1080 scale=0.80`:

| Pose | all | -labels | none | all - (-labels) | budget |
|---|---|---|---|---|---|
| gameplay03-moving | 2.24 (spread 0.65) | 2.28 (0.78) | 2.44 (0.63) | -0.04 ms | <= 1 + 0.78 ms: within |
| diorama02-moving | 10.21 (0.10) | 10.20 (0.16) | 10.10 (0.34) | +0.01 ms | <= 1 + 0.16 ms: within |
| gameplay03 (still) | 2.47 (0.51) | 2.44 (0.04) | 2.35 (0.34) | +0.03 ms | within |
| diorama02 (still) | 6.76 (0.38) | 6.53 (0.29) | 6.58 (0.41) | +0.23 ms | within |

  diorama02 costs about 3.4 ms more while turning than still, with or without labels (the rest of the scene).

### Task 9 fix round 2 (re-review N1-N3, shown-set robustness)

- A GeometryChangedEvent on a panel (event card, info panel, minimap, top bar) re-places the pills at once, inside the
  layout pass, so a card that opens never covers a pill for a frame (N1). The pills' summed sizes are part of the
  early-out key, so a pill that grows with the camera still is re-placed (N2). `LABELS refresh moving avg_us=28 p95_us=38`.
- N3: WebGL `?smoke=5&navlog` (`SMOKE OK 5 turns 1948-06-19 d3fdd459...`) shows no layout or style warning in the
  console; the only non-game lines are the existing `WebGL: INVALID_ENUM: getInternalformatParameter` driver notices.
- `ShownPillsNeverOverlapAndEachReadsAsItsOwn` pins only unambiguous names (90 km: Tel Aviv, Petah Tikva; overview: Tel
  Aviv, Jerusalem, Gaza, Beersheba), at least 8 and 7 pills, Hadera or Netanya at the overview, and completeness: every
  visible marker not decluttered by zoom with no other visible marker within 2 pill heights + MarginPx is labelled.
  Disabling the anchor fallback turns it red (`overview: shown pills Expected: greater than or equal to 7 But was: 6`).

## Merge point (Tracks A, B, C and main on `visual-v1`)
Merged in order vis-light, vis-labels, main, then vis-ground (`d5037af`). Conflicts: `SettlementTests.cs` (the seam test
now runs Track A's graded/ungraded loop around Track B's pairs and even-row strips, grid off and on, four yaws: 64
asserted checks, max 0.006 ungraded against 0.012 and 0.005 graded against 0.024), `docs/visual-v1.md`,
`tools/README.md`. Carries closed: the stale `_label` filter (Task 9 M8) is gone; Track B's colour and detail render
tests (`TerrainShaderTests`, `SeaTests`, the seam and rift tests) run with the tilt-shift off (task-3 carry m6).

**Counts.** Unity edit mode: base `0a2a391` 346; Track A `5fa3fa9` 382 (+36); Track B `ee555ac` 371 (+25); Track C
`ebfdb73` 361 (+15); main since the base +15 (`UnityMcpGuardTests` 10, `GameInputsTests` 5); minus 1 for
`VisualHarnessTests.AnAllowedRequireIsAcceptedAndAnUnknownOneFailsWithHowToAddIt`, which both A and B carry (the Task 3
Step 0 cherry-pick). 346 + 36 + 25 + 15 + 15 - 1 = **436**, measured `total="436" passed="436" failed="0"`, no CS
warning from `Assets/(Scripts|Editor|Tests)`. Tools 143 + 1 (main) + 29 (Track B) = **173 passed, 1 skipped**. Sim 625
(`git diff 0a2a391 -- sim content` shows only main's owner sign-off `25d2ef6`; no visual track touches either).

**Cross-track interactions found by the render tests.**
- `GradeTests.TheSelectedHexStillStandsOutOnGradedLand` failed at gameplay03 ungraded: deltaE76 18.2 (< 20; graded
  23.1; Track A alone 24.5 / 32.3). Track B's coastal plain is dry-grass gold, the hue of the gold selection band. The
  selection is now warm ivory (1, 0.95, 0.80): 31.9 / 34.1 at gameplay03, 30.7 / 32.2 at diorama02.
- Shadows (`ReliefCastsShadowsAtMapScale`, water on): 4.53% web, 6.99% PC (floor 2%; Track A alone 3.05 / 3.23).
  Haze tests: near 0.004, mid 0.028, far 0.054. Zoom extremes at MinDistance 6 km: grid covers 0.22% (1.57% at
  250 km). Rift line change (bound 0.012): Beit She'an 0.096, Jericho 0.085, Arava 0.026.

**Grade re-tune (Task 4 review I1).** WebGL headless, 1920x1080 at DPR 1:

| Shot | Saturation | Contrast | Warmth |
|---|---|---|---|
| promo 02 / promo 03 | 0.425 / 0.382 | 0.200 / 0.203 | 0.145 / 0.064 |
| diorama02, `Saturation` 16 (t-merge-pre) | 0.5345 | 0.2058 | 0.1405 |
| gameplay03, `Saturation` 16 | 0.6629 | 0.2147 | 0.1509 |
| diorama02, `Saturation` 4 (t-merge-r1) | 0.4860 | 0.2070 | 0.1181 |
| diorama02, **`Saturation` 0 (t-merge)** | **0.4729** | **0.2074** | **0.1099** |
| gameplay03, **`Saturation` 0** | **0.5822** | **0.2168** | **0.1078** |

Diorama02 now meets both bars (warmth in [0.087, 0.145], saturation <= 0.48). `TheGradeLiftsSaturation`: 0.568 ->
0.598 (+5.3%, bar 5%; replaced in fix round 1). Gameplay03 stays 0.20 over its promo in saturation and 0.04 in warmth: the promo's frame is
green fields and orchards, which no global grade can supply.

**Grid (Task 8 N2).** Land opacity 0.25 -> 0.35: at 0.25 the graded land lines were invisible on the plain at both
poses. At 0.35 gameplay03 shows a faint land lattice and diorama02 a faint near-field one, as in the promos. Fade
thresholds unchanged.

**Web.** `BUILD Succeeded WebGL -> Builds/Web-disabled: 77.1 MB on disk, 0 errors`; `?golden&smoke=5` PASS with all
three values from docs/determinism.md. Boot (headed, `machine_lock --exclusive`, median of 3): `--cpu 1` map_build 456 /
first_frame 3585 ms (budgets 743 / 5321); `--cpu 4` 1863 / 6952 ms (budgets 2720 / 10445). Both within.

### Merge point fix round 1 (merge review)
- **I1.** `TheGradeLiftsSaturation` is replaced by `TheGradeKeepsSaturationNearTheUngradedFrame`: graded/ungraded
  saturation at gameplay03 in [0.98, 1.12], and `MapGrade.Saturation` in [-10, 4]. Measured x1.042.
- **Palette (art gap a).** `coastal_plain` Lit(0.52, 0.54, 0.33), `valley` Lit(0.50, 0.53, 0.33). WebGL:

| Shot | Saturation | Contrast | Warmth |
|---|---|---|---|
| promo 02 / promo 03 | 0.425 / 0.382 | 0.200 / 0.203 | 0.145 / 0.064 |
| diorama02, t-merge -> t-merge-r3 | 0.4729 -> 0.4694 | 0.2074 -> 0.2034 | 0.1099 -> 0.1045 |
| gameplay03, t-merge -> t-merge-r3 | 0.5822 -> 0.5646 | 0.2168 -> 0.1959 | 0.1078 -> 0.0774 |

  EditMode 436/436 with no threshold change: selection vs land 33.4 / 35.5 (gameplay03), colour-blind selection-land
  34.5 / 36.4, rift lines 0.121 / 0.104 / 0.026.
- **Border land colour (art gap b): not taken.** `BorderLandColour` (0.76, 0.66, 0.47) (t-merge-r2) rendered the
  south-west slab at (0.69, 0.58, 0.37), against (0.56, 0.50, 0.37) before. The terrain next to it renders (0.82, 0.69,
  0.43), so the step shrank. But the skirt is 17% of diorama02, and its saturation rose 0.34 -> 0.47. That added +0.021
  to the frame, which reached 0.488 against the 0.48 bar (warmth 0.129). Reverted. A lower-chroma colour of the same
  luma is NOT the next step: the final ruling is in "Open items for the art track" (fix the table/frame and the skirt in
  the terrain's own look, not a colour patch).

## Tier decisions (Task 10)
Measured under `machine_lock.py --exclusive --wait-unity`, each run started at a 1-minute load average of 3.0 or less
(the machine idles at 3-4: other sessions; every run's before/after load is in the Task 10 report). Step 1 is the merged
build at render scale 0.8 (`8d45a93`, which adds the `bloom` toggle); the candidates are that build with one URP value
changed; "final" is the build with the decisions applied. A cost is `median(all) - median(-name)`; over budget only
beyond budget + the larger spread.

| Setting | Web | Desktop | Evidence |
|---|---|---|---|
| Path | Forward | Forward+ | research notes 2.1 (Forward+ on WebGL reported unstable) |
| Depth / opaque copies | off / off | off / off (were on) | no visual v1 effect samples them; the Gaussian DoF that did is retired (Task 3) |
| Render scale | 1.0 (was 0.8) | 1.0 | all three rules hold at 1.0 (headless p95 <= 16.7 ms, headed <= 20 ms, DPR 2 <= 33 ms): headless `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.58 spread_ms=0.18 median_p95_ms=5.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.96 spread_ms=0.05 median_p95_ms=11.00`; headed `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.39 spread_ms=0.10 median_p95_ms=5.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.14 spread_ms=0.20 median_p95_ms=11.00`; DPR 2 `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=4.01 spread_ms=0.00 median_p95_ms=5.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=18.59 spread_ms=0.36 median_p95_ms=19.00` |
| Fill-rate stress (DPR 2, headed, final at 1.0) | `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=3.78 spread_ms=0.05 median_p95_ms=5.00`<br>`PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=18.08 spread_ms=0.06 median_p95_ms=20.00` | — | a stand-in for a GPU with a quarter of the fill rate; under the 33 ms floor with 13 ms to spare |
| Boot | `BOOT map_build_ms median=432 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3563 runs=3 cpu=1`<br>`BOOT map_build_ms median=1751 runs=3 cpu=4`<br>`BOOT first_frame_ms median=6797 runs=3 cpu=4`<br>`BOOT map_build_ms median=426 runs=3 cpu=1`<br>`BOOT first_frame_ms median=3477 runs=3 cpu=1`<br>`BOOT map_build_ms median=1746 runs=3 cpu=4`<br>`BOOT first_frame_ms median=6696 runs=3 cpu=4` (Step 1, then final) | — | each within max(300 ms, 50%) of its Task 1 baseline (443 / 3547; 1813 / 6963 ms); both runs exit 0 |
| Shadows | 1 cascade, 2048, hard | 4 cascades, 2048, soft | `-shadows` at diorama02: Step 1 headless 10.32 - 7.36 = **2.96 ms** (spread 0.32: over 2.82), headed 2.90 (0.41: within), DPR 2 4.99 (0.23); final headless 2.89 (0.41: within 2.91), headed 2.92 (0.29: **over** 2.79), DPR 2 4.80. A 1024 build measured 2.84 (headless, spread 0.09), 2.94 (headed) and 5.03 (DPR 2): (1024 lines, headless: `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.08 spread_ms=0.09 median_p95_ms=11.00`<br>`PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.24 spread_ms=0.03 median_p95_ms=9.00`<br>headed: `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.20 spread_ms=0.28 median_p95_ms=11.00`<br>`PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.26 spread_ms=0.13 median_p95_ms=9.00`<br>DPR 2: `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=18.34 spread_ms=0.21 median_p95_ms=19.00`<br>`PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=13.31 spread_ms=0.10 median_p95_ms=14.00`): the map's resolution is not the cost (the caster pass redraws the whole map's geometry at the overview), so 1024 would blur the relief for nothing. **2048 kept; the overrun (about 0.1-0.4 ms beyond budget + spread at the 250 km overview only) is named here.** gameplay03: 0.33 ms final |
| Tilt-shift | downsample 2 | downsample 2 | `-tiltshift` cost: Step 1 headless 0.09 / 0.09 ms (gameplay03 / diorama02), final headless 0.03 / -0.04, DPR 2 0.29 / -0.04: far under 1.5 ms |
| Tilt-shift bottom band | unchanged (HalfWidth 0.13 / Feather 0.17) | same | final gameplay03 shot against `?notiltshift`, gradient energy per 1% row band (columns 30-80%): fully blurred (ratio <= 0.05-0.09) from under the bar to about 26% and from about 78-83% to the bottom, so the bottom band is 17-22% against the promo's ~15%. The 2-7 point gap is the one Task 3 offered the owner (bottom-only 0.16 / 0.19). Owner's ruling (2026-10-13, issue #390): the bands stay as they are, except that the far band is lighter at the gameplay zoom ("Issue #390"); the bottom band is not changed. Owner's ruling (2026-10-14, issue #458): the bottom band's height stays, but it is lighter at the gameplay zoom and about gone at the overview ("Issue #458") |
| Bloom | off (was the URP template's 0.25 above 1) | off | rendered on both tiers at both poses it changed at most 2/255 of any pixel (mean 0.25/255 at diorama02, warmth +0.0003, measured with a temporary `bloom` toggle, commit `8d45a93`, removed in fix round 1 once bloom shipped off); `-bloom` cost Step 1 headless 0.37 ms (diorama02), headed 0.18, macOS 0.17 / 0.13. Off in the pipeline profile (`Settings/SampleSceneProfile`), which keeps its Neutral tonemapper (the stripping guarantee, GradeTests) |
| SSAO | none | off (was on) | metre radius cannot reach km relief; baked AO replaces it (Tasks 5-6) |
| Grade, vignette, haze, grid, water, labels | on | on | Step 1 headless costs (gameplay03 / diorama02, ms): grade -0.02 / 0.27, haze 0.08 / 0.11, grid 0.07 / -0.14, water 0.04 / 0.14, labels -0.14 / -0.02; final headless: grade 0.00 / -0.03, haze -0.03 / -0.10, grid 0.00 / -0.01, water -0.05 / -0.06, labels -0.01 / 0.01; all within budget. The grid, unmeasured on a quiet machine since Task 8, costs nothing measurable |
| Baked shading texture | block-compressed | block-compressed | review minor 9: recorded, not changed (mobile browsers are outside the 1920x1080 target) |
| WebGPU, GPU Resident Drawer, compute | not used | not used | research notes 2.1 (an M5 evaluation) |
| Weak GPU | SwiftShader run passes golden and shots | — | `captures/visual/t10-swiftshader` (both GOLDEN OK lines and SHOT done; gameplay03 saturation / contrast / warmth 0.5656 / 0.1960 / 0.0773 against the GPU's 0.5655 / 0.1960 / 0.0771) |
| Runtime quality fallback (issue #44) | on | off | see "Runtime quality fallback" below: when the frame-time p95 stays over the 30 fps floor, render scale 1.0 -> 0.9 -> 0.8, then the tilt-shift off; back up in reverse |

Colour after the decisions (WebGL `?shot`, t10-tiers against t-merge-r3): diorama02 saturation 0.4694 -> 0.4686, contrast
0.2034 -> 0.2032, warmth 0.1045 -> 0.1038 (bars: warmth 0.087-0.145, saturation <= 0.48); gameplay03 saturation 0.5646 ->
0.5655 (+0.0009, away from the promo's 0.382 by a rounding-level step: the 1:1 render), contrast 0.1959 -> 0.1960, warmth
0.0774 -> 0.0771. The sea, palette and grade are untouched.

The built macOS player (`unity/Builds/Mac/Statecraft.app`) does not run in the background (`runInBackground` is off): its
`-vprofile` waits at `GAME OK` until its window is in front. Bring that player forward (Task 10 used
`open -a unity/Builds/Mac/Statecraft.app`); never script, `open -a` or AppleScript the Unity editor (team rule).

The `-bloom` lines below come from the temporary toggle (`8d45a93`); in the final runs bloom already ships off, so
`-bloom` equals `all` there. At gameplay03 in Step 1 (scale 0.8), `-bloom`, `-shadows` and `none` all read about 0.4 ms
*above* `all` (headless 2.51 / 2.40 / 2.40 against 2.03; headed 2.34 / 2.37 / 2.24 against 1.85). `none` cannot really be
slower than `all`; the pose runs at about 2 ms, at the WebGL clock's ~1 ms resolution, and the readings look bimodal
(~2.0 / ~2.4 ms). Unexplained; it changes no decision (every gameplay03 cost is far inside its budget).

### Runtime quality fallback (issue #44)
The Deferred row "Runtime quality fallback" (review I1), sized by the DPR-2 stress numbers above. `QualityFallback`
(pure logic) and `QualityFallbackDriver` (feeds it `Time.unscaledDeltaTime`, applies its step), attached by
`CameraRig.Init` through the map's teardown like the visual toggles.
- **Steps:** level 0 is the tier (scale 1.0, tilt-shift on); 1: scale 0.9; 2: scale 0.8 (the research's 0.8-1.0 dynamic
  range, notes 2.3); 3: scale 0.8 and the tilt-shift off. One step per decision, up in reverse. The scale is the active
  URP asset's `renderScale` (never above the tier's own); the tilt-shift uses its own `TiltShift.FallbackOff`, so stepping
  up never turns on a tilt-shift the player (`?notiltshift`) or a profile turned off. The map's teardown puts both back.
- **Window: 3 s, back to back, one decision each.** The p95 of a 3 s window is over only when at least 5% of its frames
  are (5 of 90 at 30 fps), so a one-off hitch (End Turn's redraw, a GC, a first shader compile) never trips it and a real
  shortfall does within 3 s. Nothing is measured in the first 3 s after the map is built, or 0.5 s after each change; a
  frame over 1 s (a hidden tab coming back) drops the window; a window needs at least 3 frames, which a full 3 s window
  of counted frames (each at most 1 s) always has, so the slowest devices (1-3 fps) step down too (fix round 1, review M2;
  a minimum of 10 had left them with no decision).
- **Down: p95 > 34 ms.** The floor is 33 ms (30 fps). The WebGL frame clock is quantised to about 1 ms, so exactly 30 fps
  (a 30 Hz cap from a display or a battery saver, or vsync halving 60 Hz) reads as 33 and 34 ms frames: that meets the
  floor, and a lower quality could not raise a capped rate.
- **Up: 3 windows in a row (9 s) with p95 < 20 ms; hysteresis.** One step up draws at most 1.27 times the pixels
  (0.8 -> 0.9, 0.9 -> 1.0), so a fill-bound 20 ms frame lands near 25 ms, under the floor; the tilt-shift costs under
  1.5 ms (`-tiltshift` above: 0.29 ms at DPR 2). Windows between 20 and 34 ms change nothing and restart the count. A step up
  undone within two windows doubles the windows the next one needs (3, 6, 12 ... at most 48, 2.4 minutes), so a device on
  the edge settles instead of flipping.
- **Web tier only.** `QualityFallback.Wanted` is true only in the WebGL player: the desktop tier is the development and
  capture target (its renders and `-vprofile` must show the tier's own look), and in the editor a scale change would
  change the URP asset. Even on WebGL it stays off under `?vprofile`, `?profile` and `?shot` (instruments that must see the
  tier as configured) and with `?nofallback`; every change logs `QUALITY level=<n> scale=<s> tiltshift=<on|off>
  p95_ms=<ms>`.
- **Presentation only:** it reads no sim state and writes only the render scale and the tilt-shift flag, so turns, hashes
  and saves cannot change. The goldens and the 5-turn smoke match with it in the build: `uv run tools/web_check.py
  unity/Builds/Web-disabled --query "golden&smoke=5"` with the three `--expect` values from `docs/determinism.md`: PASS.
- **Tests** (`QualityFallbackTests`, EditMode, synthetic frame times): the step order and range, one step per window and
  not before a full window, nothing during start-up, hitches, a pause and 33/34 ms frames do not trigger, recovery in
  reverse only after 3 good windows, the dead band holds and restarts the count, a failed step up doubles the wait (capped);
  integration: no driver in the editor; with the web tier's answer forced, `MapScene.Build` attaches one, its steps reach
  the URP asset and `TiltShift.FallbackOff` (not `TiltShift.Enabled`), and a rebuild or teardown removes it and restores the
  scale.
- **In a real browser** (headed Chrome for Testing, ANGLE Metal, 1920x1080 at DPR 1, under `machine_lock.py --exclusive
  --wait-unity`, a scratch CDP script on `headed.Browser`): 30 s idle at `--cpu 1`, no `QUALITY` line. With CPU throttling
  at 20x from t = 30 s: `QUALITY level=1 scale=0.90 tiltshift=on p95_ms=97.00`, `level=2 scale=0.80 tiltshift=on
  p95_ms=97.00`, `level=3 scale=0.80 tiltshift=off p95_ms=107.00` (level 3 at t = 39.6 s). Throttling lifted at t = 49.6 s:
  `level=2 ... tiltshift=on p95_ms=17.00`, `level=1 scale=0.90`, `level=0 scale=1.00` (t = 77.4 s, about 9.5 s apart), then
  no line in the next 15 s. At DPR 2 (the fill-rate stress proxy, frame rate uncapped) 60 s idle: no `QUALITY` line, as the
  DPR-2 p95 (20 ms at diorama02) is under the floor.
- **Boot with it (`headed.py boot --runs 3`, on top of the packed heightmap):** `--cpu 1` `BOOT map_build_ms median=472`,
  `BOOT first_frame_ms median=3609`; `--cpu 4` `map_build_ms median=1983`, `first_frame_ms median=7158`: within the boot
  budget (exit 0).

### Final measurements
Step 1 headless (scale 0.80):
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.03 spread_ms=0.16 median_p95_ms=3.00`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=2.51 spread_ms=0.58 median_p95_ms=5.00`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=2.05 spread_ms=0.25 median_p95_ms=3.00`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=1.96 spread_ms=0.27 median_p95_ms=3.00`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=1.95 spread_ms=0.53 median_p95_ms=3.00`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=2.17 spread_ms=0.31 median_p95_ms=4.00`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.40 spread_ms=0.03 median_p95_ms=4.00`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=1.94 spread_ms=0.21 median_p95_ms=3.00`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=1.99 spread_ms=0.09 median_p95_ms=3.00`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.40 spread_ms=0.09 median_p95_ms=4.00`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.32 spread_ms=0.19 median_p95_ms=11.00`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=9.95 spread_ms=0.02 median_p95_ms=11.00`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=10.05 spread_ms=0.51 median_p95_ms=11.00`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=10.46 spread_ms=0.84 median_p95_ms=12.00`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=10.21 spread_ms=0.06 median_p95_ms=11.00`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=10.34 spread_ms=0.47 median_p95_ms=12.00`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.36 spread_ms=0.32 median_p95_ms=10.00`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=10.23 spread_ms=0.21 median_p95_ms=11.00`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=10.18 spread_ms=0.08 median_p95_ms=11.00`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.16 spread_ms=0.33 median_p95_ms=9.00`

Run line: `PROFILE visual=all pose=gameplay03 res=1920x1080 scale=0.80 rep=0 frames=300 avg_ms=2.05 p95_ms=3.00`

Step 1 headed, DPR 1 (scale 0.80):
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.85 spread_ms=0.08 median_p95_ms=3.00`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=2.34 spread_ms=0.48 median_p95_ms=4.00`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=2.00 spread_ms=0.17 median_p95_ms=3.00`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=1.85 spread_ms=0.04 median_p95_ms=3.00`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=1.86 spread_ms=0.02 median_p95_ms=3.00`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=1.84 spread_ms=0.35 median_p95_ms=3.00`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.37 spread_ms=0.02 median_p95_ms=4.00`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=1.85 spread_ms=0.86 median_p95_ms=3.00`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=1.86 spread_ms=0.11 median_p95_ms=3.00`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.24 spread_ms=0.04 median_p95_ms=4.00`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.27 spread_ms=0.41 median_p95_ms=11.00`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=10.09 spread_ms=0.47 median_p95_ms=11.00`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=10.18 spread_ms=0.09 median_p95_ms=11.00`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=10.70 spread_ms=0.64 median_p95_ms=12.00`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=10.51 spread_ms=0.52 median_p95_ms=12.00`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=10.60 spread_ms=0.49 median_p95_ms=12.00`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.37 spread_ms=0.09 median_p95_ms=10.00`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=10.52 spread_ms=0.33 median_p95_ms=12.00`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=10.55 spread_ms=0.22 median_p95_ms=12.00`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.15 spread_ms=0.11 median_p95_ms=9.00`

Step 1 headed, DPR 2 (scale 0.80):
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=3.49 spread_ms=0.15 median_p95_ms=5.00`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=3.28 spread_ms=0.02 median_p95_ms=5.00`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=3.41 spread_ms=0.09 median_p95_ms=5.00`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=3.36 spread_ms=0.21 median_p95_ms=5.00`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=3.49 spread_ms=0.23 median_p95_ms=5.00`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=3.47 spread_ms=0.01 median_p95_ms=5.00`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=3.11 spread_ms=0.07 median_p95_ms=5.00`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=3.36 spread_ms=0.09 median_p95_ms=5.00`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=3.48 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.57 spread_ms=0.21 median_p95_ms=4.00`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=18.24 spread_ms=0.23 median_p95_ms=19.00`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=18.12 spread_ms=1.06 median_p95_ms=20.00`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=18.35 spread_ms=0.27 median_p95_ms=19.00`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=18.27 spread_ms=0.11 median_p95_ms=19.00`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=18.24 spread_ms=0.11 median_p95_ms=19.00`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=18.23 spread_ms=0.11 median_p95_ms=19.00`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=13.25 spread_ms=0.16 median_p95_ms=14.00`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=18.23 spread_ms=0.19 median_p95_ms=19.00`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=18.28 spread_ms=0.17 median_p95_ms=19.00`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=13.05 spread_ms=0.07 median_p95_ms=14.00`

Step 1 macOS IL2CPP player (PC tier as merged: SSAO, depth and opaque copies, bloom on):
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.45 spread_ms=0.01 median_p95_ms=1.48`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=1.32 spread_ms=0.03 median_p95_ms=1.34`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=1.45 spread_ms=0.00 median_p95_ms=1.47`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=1.45 spread_ms=0.00 median_p95_ms=1.47`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=1.45 spread_ms=0.00 median_p95_ms=1.48`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=1.45 spread_ms=0.00 median_p95_ms=1.47`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=1.16 spread_ms=0.00 median_p95_ms=1.19`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=1.38 spread_ms=0.00 median_p95_ms=1.40`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=1.35 spread_ms=0.00 median_p95_ms=1.37`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=0.88 spread_ms=0.00 median_p95_ms=0.90`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=3.73 spread_ms=0.01 median_p95_ms=3.98`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=3.56 spread_ms=0.05 median_p95_ms=3.70`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=3.71 spread_ms=0.06 median_p95_ms=3.82`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=3.70 spread_ms=0.04 median_p95_ms=3.82`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=3.72 spread_ms=0.04 median_p95_ms=3.90`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=3.69 spread_ms=0.03 median_p95_ms=3.81`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=2.34 spread_ms=0.02 median_p95_ms=2.41`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=3.66 spread_ms=0.02 median_p95_ms=3.79`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=3.68 spread_ms=0.04 median_p95_ms=3.81`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=2.18 spread_ms=0.02 median_p95_ms=2.30`

Final headless (scale 1.00):
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.70 spread_ms=0.02 median_p95_ms=5.00`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=2.72 spread_ms=0.03 median_p95_ms=5.00`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=2.70 spread_ms=0.10 median_p95_ms=5.00`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=2.70 spread_ms=0.07 median_p95_ms=5.00`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=2.73 spread_ms=0.11 median_p95_ms=5.00`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=2.71 spread_ms=0.04 median_p95_ms=5.00`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.37 spread_ms=0.02 median_p95_ms=4.00`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=2.67 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=2.75 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.34 spread_ms=0.05 median_p95_ms=4.00`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=10.06 spread_ms=0.41 median_p95_ms=11.00`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=10.05 spread_ms=0.34 median_p95_ms=11.00`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=10.09 spread_ms=0.27 median_p95_ms=11.00`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=10.07 spread_ms=0.25 median_p95_ms=11.00`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=10.16 spread_ms=0.07 median_p95_ms=11.00`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=10.05 spread_ms=0.07 median_p95_ms=11.00`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.17 spread_ms=0.01 median_p95_ms=8.00`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=10.10 spread_ms=0.04 median_p95_ms=11.00`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=10.12 spread_ms=0.07 median_p95_ms=11.00`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.17 spread_ms=0.05 median_p95_ms=9.00`

Run line: `PROFILE visual=all pose=gameplay03 res=1920x1080 scale=1.00 rep=0 frames=300 avg_ms=2.71 p95_ms=5.00`

Final headed, DPR 1:
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.72 spread_ms=0.06 median_p95_ms=5.00`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=2.66 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=2.68 spread_ms=0.01 median_p95_ms=5.00`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=2.70 spread_ms=0.01 median_p95_ms=5.00`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=2.71 spread_ms=0.07 median_p95_ms=5.00`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=2.67 spread_ms=0.03 median_p95_ms=5.00`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=2.37 spread_ms=0.01 median_p95_ms=4.00`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=2.69 spread_ms=0.09 median_p95_ms=5.00`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=2.67 spread_ms=0.10 median_p95_ms=5.00`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=2.31 spread_ms=0.01 median_p95_ms=4.00`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=9.98 spread_ms=0.29 median_p95_ms=10.00`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=10.03 spread_ms=0.33 median_p95_ms=11.00`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=9.95 spread_ms=0.31 median_p95_ms=11.00`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=9.97 spread_ms=0.28 median_p95_ms=10.00`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=10.03 spread_ms=0.07 median_p95_ms=11.00`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=10.00 spread_ms=0.04 median_p95_ms=11.00`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=7.06 spread_ms=0.04 median_p95_ms=8.00`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=10.01 spread_ms=0.04 median_p95_ms=11.00`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=10.06 spread_ms=0.06 median_p95_ms=11.00`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=7.01 spread_ms=0.02 median_p95_ms=9.00`

Final headed, DPR 2:
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=3.78 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=3.78 spread_ms=0.06 median_p95_ms=5.00`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=3.75 spread_ms=0.04 median_p95_ms=5.00`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=3.75 spread_ms=0.02 median_p95_ms=5.00`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=3.76 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=3.75 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=3.50 spread_ms=0.02 median_p95_ms=5.00`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=3.49 spread_ms=0.05 median_p95_ms=5.00`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=3.54 spread_ms=0.06 median_p95_ms=5.00`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=3.08 spread_ms=0.01 median_p95_ms=4.00`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=18.08 spread_ms=0.06 median_p95_ms=20.00`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=18.11 spread_ms=0.15 median_p95_ms=19.00`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=18.13 spread_ms=0.10 median_p95_ms=19.00`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=18.12 spread_ms=0.10 median_p95_ms=19.00`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=18.13 spread_ms=0.07 median_p95_ms=19.00`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=18.14 spread_ms=0.14 median_p95_ms=19.00`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=13.28 spread_ms=0.09 median_p95_ms=14.00`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=18.12 spread_ms=0.08 median_p95_ms=19.00`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=18.12 spread_ms=0.19 median_p95_ms=20.00`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=13.21 spread_ms=0.08 median_p95_ms=14.00`

Final macOS player (SSAO, copies and bloom off):
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=1.02 spread_ms=0.02 median_p95_ms=1.05`
- `PROFILE summary visual=-bloom pose=gameplay03 runs=3 median_avg_ms=1.03 spread_ms=0.00 median_p95_ms=1.05`
- `PROFILE summary visual=-grade pose=gameplay03 runs=3 median_avg_ms=1.02 spread_ms=0.00 median_p95_ms=1.05`
- `PROFILE summary visual=-grid pose=gameplay03 runs=3 median_avg_ms=1.02 spread_ms=0.00 median_p95_ms=1.05`
- `PROFILE summary visual=-haze pose=gameplay03 runs=3 median_avg_ms=1.03 spread_ms=0.00 median_p95_ms=1.05`
- `PROFILE summary visual=-labels pose=gameplay03 runs=3 median_avg_ms=1.02 spread_ms=0.00 median_p95_ms=1.05`
- `PROFILE summary visual=-shadows pose=gameplay03 runs=3 median_avg_ms=0.73 spread_ms=0.00 median_p95_ms=0.74`
- `PROFILE summary visual=-tiltshift pose=gameplay03 runs=3 median_avg_ms=0.95 spread_ms=0.00 median_p95_ms=0.98`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=0.91 spread_ms=0.00 median_p95_ms=0.94`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=0.59 spread_ms=0.00 median_p95_ms=0.61`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=2.72 spread_ms=0.01 median_p95_ms=2.81`
- `PROFILE summary visual=-bloom pose=diorama02 runs=3 median_avg_ms=2.73 spread_ms=0.01 median_p95_ms=2.81`
- `PROFILE summary visual=-grade pose=diorama02 runs=3 median_avg_ms=2.72 spread_ms=0.05 median_p95_ms=2.81`
- `PROFILE summary visual=-grid pose=diorama02 runs=3 median_avg_ms=2.72 spread_ms=0.00 median_p95_ms=2.81`
- `PROFILE summary visual=-haze pose=diorama02 runs=3 median_avg_ms=2.73 spread_ms=0.03 median_p95_ms=2.81`
- `PROFILE summary visual=-labels pose=diorama02 runs=3 median_avg_ms=2.72 spread_ms=0.04 median_p95_ms=2.81`
- `PROFILE summary visual=-shadows pose=diorama02 runs=3 median_avg_ms=1.41 spread_ms=0.01 median_p95_ms=1.51`
- `PROFILE summary visual=-tiltshift pose=diorama02 runs=3 median_avg_ms=2.70 spread_ms=0.03 median_p95_ms=2.81`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=2.73 spread_ms=0.01 median_p95_ms=2.81`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=1.38 spread_ms=0.01 median_p95_ms=1.41`

macOS `-golden`, both builds: `GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07`,
`GOLDEN OK 1952 73590e324f19e9959a455a4da79e93d7089dfcf283c13aac02af6b0a624b9767`.

## Region borders (promo-borders, art track gap 5)
Promo 03 outlines regions with coloured bands along hex edges, over the faint hex lattice; promo 02 draws none (its era has
no such layer), so diorama02 is checked only for staying thin. Measured on promo/final/03-gameplay-1936.png (2752 px wide,
row and column profiles across the lines):

| Promo 03 band | From the region's edge inwards | Widths (2752 px, mid-depth) | sRGB |
|---|---|---|---|
| Blue | blue stripe, white stripe, a grey-blue wash fading inwards | 6 + 6 px, wash about 10 px | 125,140,182 / 251,251,248 |
| Red-orange | red-orange stripe, cream stripe, a faint warm wash | 4 + 3 px, wash about 8 px | 176,93,61 / 246,188,124 |

Each band lies inside its region, as Civ draws territory, so two outlined regions meet as a double band (blue+white next
to red+cream: promo 03 near Tel Aviv). The bands are world-space: they widen towards the camera.

**What is drawn.** `MapBorders` (C#) bakes one texel per hex (`RegionHexes`, RGBA8 linear, point-sampled, 47 x 58: r + 256 g
the region id, b the style; since issue #195, a's low six bits say which edges border another region) and sets the
globals; `StatecraftBorders.hlsl`'s `ScBorderApply`, called last in the terrain
function, draws the band along each edge whose neighbour is in another region. A pixel farther from its hex's edges than
the widest band plus wash can reach fetches nothing (exact: a segment is never nearer than its line); one nearer fetches its
own hex and only the neighbours within reach (one, or two near a corner); since issue #195 it fetches only its own hex,
whose edge bits replace the neighbours' fetches. The first version fetched all seven texels on
every land pixel and cost 3.3 ms at gameplay03 (`visual=all` 14.97 against `-borders` 11.64, a loaded machine): over the
2.5 ms budget. The distance is to the edge segment (corners rounded, so bands join without notches), in pixels
across the edge (Task 8's anisotropic filter), box-filtered. Stripes and wash are unlit (they replace BaseColor and go out
as Emission), so they read the same in sun and shadow, as the promo's glowing lines do. A hex in no region (id 0) gets no
band and stops its neighbours' bands: the coast is never outlined. Bands stay on land: where a boundary between two coastal
land hexes crosses the shore, the band fades with the water mask (`ScWater`) instead of running up to 4 km out to the hex
edge (review M7; `BandsStayOffTheWater`). Styles (`MapBorders.Primary` / `Secondary`): band 230 m / 200 m (colour share
0.5 / 0.6), wash 0.9 / 1.0 of the band at 0.40 / 0.18. The band is at least 2 px wide unless that would make band plus wash
wider than half a hex's inradius, which wins: below about 8 px of inradius on screen (the far overview) it goes under 2 px
(review M2). A style with no wash is safe (the wash ramp's width is floored, review M3). The light stripes are set above
white (sRGB 1.08 / 1.05 red) so the graded result reaches the promo's. Toggle `borders`. `_ScBorders.z` (test seam
`MapBorders.NoEarlyOut`) turns the early-out off; `TheEarlyOutIsExact` renders both promo poses both ways and finds 0
pixels apart (review M6). Visual only: no sim state read or written, goldens unchanged.

**The no-data path** (a map built with no layer: `MapScene.Build` alone; play now draws the 1948 sub-districts, below).
Owner ruling (2026-10-11): no invented political boundaries; borders come
from real data only (the 1945-48 districts and sub-districts, the 1949 armistice lines when dated, fronts). The repository
has none of these: `hexes.json` holds elevation, land and biome; `settlements_1948.yaml` points; the armistice events
(`content/events/armistice_*.yaml`) dates and text but no geometry; the sim has no fronts (war mode, #21 / M4 N4). So
`MapBorders.Apply` runs on every build with no layer. Against main (review I1, branch merged with main 632edbd, both editor
renders from the same Library):
- `MapCapture.SurfFrames` (gameplay03, web tier, the water's clock pinned, 60 frames): **0 of 124,416,000 pixels differ**.
  The renders are kept in `captures/visual/promo-borders-i1/` (`borders-maincheck/` main, `promo-borders/` the branch,
  `main2/` main's second `Promo` run).
- `MapCapture.Promo` (both poses, both tiers; the water's clock is not pinned there): main against the branch differs in
  1,334 / 15,089 / 116,761 / 19,904 pixels (Mobile-diorama02, Mobile-gameplay03, PC-diorama02, PC-gameplay03; largest step
  26/255 in PC-diorama02, 2/255 elsewhere), and main against a second main run in 513 / 1,737 / 116,542 / 10,498 pixels
  with the same largest steps: the water's clock, not the branch.
- Inside the branch, `WithoutRegionDataTheToggleIsANoOp` only shows the toggle does nothing without data.
- The cost of the no-data path (the hoisted `ddx`/`ddy` and one uniform branch) against main's build is not measured:
  the machine ran at load 22 with 13 Unity processes from other tracks, so a cross-build difference would be noise. The
  in-build `all` against `-borders` difference (-0.02 / +0.05 ms) is the noise floor, not that cost.

`BorderProof` is a synthetic pattern for tests and captures only (review I3): a checker of 4 x 4 axial hex blocks
(parallelograms in a regular lattice over the whole map, unrelated to any town or line), alternately Primary and
Secondary, land only. While it is drawn the camera carries the label "SYNTHETIC TEST PATTERN — not real boundaries", so
every capture labels itself: a legacy `TextMesh` (bold, white with a dark drop copy, 3.3% of the frame high) that renders
under URP, placed before each render from the camera's aspect, 35% down the frame inside the tilt-shift's sharp band
(legible in `captures/visual/promo-borders-r2/editor/*-gameplay03.png` and `*-diorama02.png`).
`TheProofLabelRendersAtTheTopLeft` renders it shown and hidden at 1920x1080 and 1440x1080: about 12,800 pixels change in
the upper left, none elsewhere, and its left end is on screen; `TheProofIsLabelledOnScreenWhileDrawn` checks it goes with
the toggle and the layer. Only the editor and development builds honour
its flag (`Application.isEditor || Debug.isDebugBuild`; `AReleaseBuildIgnoresTheProofFlag`): `-borderproof` in the editor's
promo renders and tests, `tools/art_checkpoint.sh --borderproof` (editor renders; the release WebGL shots ignore it, so the
WebGL frame profile can no longer draw it), and `?borderproof` / `-borderproof` in a development player.

**Data: the 1948 sub-districts (promo-borderdata).** The sixteen sub-districts of six districts in force on 15 May 1948,
the game's start: the Administrative Divisions (Amendment) Proclamation, 1947 (Palestine Gazette No. 1552, Supplement No. 2,
30 Jan 1947, pp. 125-133, in force 1 Jan 1947) revoked and replaced the whole Schedule, so it alone defines them; the 1942
(No. 1163) and 1945 (No. 1415) changes are superseded (cited, not read). No later amendment was found (two web searches,
2026-10-11); **the check stays open**: the Palestine Gazette's 1947 and 1948 indexes, or "Laws of Palestine 1947"
(subsidiary legislation) at the National Library of Israel, would close it.
- *True lines:* `content/map/subdistricts_1948.geojson` (status `sensitivity-reviewed`, checked 2026-10-11; names as in
  the Gazette, Hebrew and Arabic from Wikipedia, the source noted per row), written by `tools/borders_digitise.py` from the Survey of Palestine map
  "Administrative Divisions" (1:750,000, drawn April 1946, red boundary overprint May 1947; map 6 of the "Maps of
  Palestine" prepared for UNSCOP, June 1947), two Commons scans of different prints (National Library of Israel), SHA-256
  checked. Method: the graticule (35-36 E x 30-33 N) georeferences each scan (affine, rms 5-6 px, about 0.2 km); the red
  overprint plus the black dots between its dashes, with the district lettering and the name underlines erased, is closed
  and flood-labelled from each sub-district's chief town (a leak fails the run); a fine closing then gives the narrow
  salients the coarse one filled back to their own side, and Gazette-listed Metulla (Safad, p. 127), which the 1:750,000
  map draws on the frontier's tip, is kept in Safad; six connectors where a line runs under a black label or stops short
  of the coast, and the Metulla tip, are recorded as uncertain segments; water (sea, the Dead Sea at its 1948 level)
  from the game's DEM, so the coast is the game's coast. Accuracy about +-1 km: areas within -4.1% to +3.7% of Village
  Statistics 1945 (which follows the 1945 Schedule; Beersheba's rounded figure makes its check nominal); the second print
  agrees to a mean 0.11-0.23 km, at most 1.23 km (per feature in the file); 41 independent Gazette-listed places (none a
  digitiser seed) plus Metulla (kept by its patch) fall in their sub-district (Ra'anana 0.1 km over the Jaffa/Tulkarm
  line, within the accuracy). The
  file is written one vertex per line and is byte-identical on a rerun.
- *Display:* `tools/borders_bake.py` gives each land hex the sub-district containing its centre (a shore hex whose centre
  is in the water: the side of its nearest land, so the hexes off Taba, west of Rafah's frontier and by the Lisan in the
  Dead Sea are outside), the hex that holds a Gazette-listed place that place's sub-district, never outside (controller
  ruling, owner-delegated, 2026-10-11; `GAZETTE_PLACES` / `GAZETTE_SETTLEMENTS`, placed as the game places settlements:
  this puts Rafah town's hex (-15, 31), whose centre is in Sinai, in Gaza, Metulla's (15, 1) in Safad and Ein Gev's
  (12, 8) in Tiberias, which the centre rule had left outside, and moves the Kefar Sava / Ra'anana and Kafr Qasim hexes
  to their own sub-districts), land beyond the Mandate frontier the `outside` region (style none, so the
  bands run along the frontier and nothing is drawn beyond it), sea and lake hexes none (no coast band) ->
  `content/map/subdistrict_hexes_1948.yaml`. Every sub-district is Primary (blue). `GameRoot.Begin` and
  `MapCapture.Promo` draw it (`Subdistricts1948.Show`); the info panel names a hex's sub-district and district ("Jaffa
  Sub-District, Lydda District (Mandate sub-districts, 1947)"). Snapping moves a line by up to half a hex (about 4 km);
  the true line is the GeoJSON's. The "Why" screen lists sim causes only, so it does not name map lines (the sim must not
  read this file).
- *When it shows (owner ruling, 2026-10-11):* from the 1948 start until the 1949 armistice lines replace it, each front
  switching to its armistice line on that agreement's date (February-July 1949) once that data is built; until then the
  layer stays, labelled on screen "Mandate sub-districts, 1947" (a UI label above the minimap while the layer is drawn;
  not the renderer's 3D banner, which the tilt-shift blurs; editor-only renders have no UI and so no label) (the baked
  file's `label`, `shown_from: 1948-05-15`,
  `shown_until: null`; `Subdistricts1948.ShowsOn`, applied on every turn; `SubdistrictTests`). Recorded in decision D-B.
- *Kept out of the sim:* the sim reads files by name and its content hash covers only those, so neither file moves the
  hash or the goldens (`SubdistrictTests.TheSimIgnoresTheFileTheContentHashDoesNotMove`; `test_the_sim_never_reads_the_border_files`).
- *Licence:* Government of Palestine works of 1945-47, public domain in Israel and the UK, with a possible URAA US
  copyright in the map's drawing that does not reach facts (Feist): decision D-B (docs/oss/DECISIONS.md), decided
  (owner-delegated, 2026-10-11): allowed; facts redrawn, no image copied; `ASSETS.yaml` row verified.
- *Rulings (owner-delegated to the controller, 2026-10-11):* the near-frontier shore hexes (Taba, west of Rafah, the
  Lisan) stay as built; the Mandate frontier outline stays; Samaria District in Arabic stays "لواء السامرة" (no official
  Arabic Mandate text reachable online; "لواء نابلس" is the common usage; unverified, noted in the file); the Hebrew and
  Arabic names from Wikipedia are accepted for now; the sensitivity review is done (`sensitivity-reviewed`), while the
  1947-48 amendment check stays open.
- *1949 armistice lines (armistice-lines, #196):* `content/map/armistice_lines_1949.geojson` (true lines, draft) and
  `armistice_hexes_1949.yaml` (snapped), from the agreements and their annexed maps
  (`research_notes/Map borders/armistice-lines-1949.md`, "Built"): Syria (line and three-part DMZ), Lebanon (the
  frontier), Egypt (El Auja DMZ; the Gaza-Rafah line from the Wadi Hasi to Gaza; from 22 Feb 1950 the line on the
  signed modus vivendi map, S/1471), Jordan (low accuracy, from the low-resolution UNISPAL copy of map 1; line A by
  its Art. VI.3 stages; Jerusalem a gap). Each front switches on its agreement's date, which the sub-district layer records per
  front (`Subdistricts1948.FrontReplaced`, used by `MapRegions`): from then its zones are drawn over the sub-districts in the
  secondary style and its lines named in the info panel with their own agreement and date, while the sub-district bands
  stay as the base; the map label adds "1949 armistice lines: <fronts>", with "Jordan (low
  accuracy)" and "Egypt (and the 1950 modus vivendi)" (`ArmisticeTests`); its box takes up to the right column's
  480 px (`.map-layer`, was 160 px), so the longest label is three lines, checked beside a real event card at seven window
  shapes (`ArmisticeTests.TheLongestMapLabelFitsBesideTheEventCard`). The sub-district layer stays as the base
  while the lines leave gaps. A line is visible as a band only where it bounds a zone or is the Mandate frontier; at
  5-km hexes two of the three Syrian DMZ parts take no hex (true-polyline overlay: #237).

**Shared edges and the far line (refs #196, `.superpowers/borders-routes-r1/PLAN.md` B1 and B3).** Every sub-district
is Primary, so where two meet both drew their full band: blue, white, white, blue, about 11-13 px across at gameplay03
against promo 03's one band of about 8 px, on every internal boundary. Promo 03 never shows two same-colour regions side
by side. `MapBorders.Use` now also bakes, per hex, which border edges the neighbour draws in the same style (b's upper six
bits; the style keeps the low two), and `ScBorderApply` draws 0.6 of the band and of the wash each side of such a shared
edge (`ScSharedBand`), so the two make one band of the promo's width centred on the edge, with no jog where the drawing
side would change. At exactly half, each half lost its partly covered outer pixel and the pair measured 6 px at
gameplay03 against a full band's 8; at 0.6 the pair measures as one full band.
The nearest edge is chosen in units of its own band, so where a shared edge meets the frontier at a corner each keeps
its own width. Edges against a region of style none (the Mandate frontier against "outside") and primary-secondary edges
(the double band promo 03 draws near Tel Aviv) keep the full band. Still one texel a pixel (`TheBordersReadOneTexelAPixel`).
Far off, where the band is under about 4 px (diorama02), the 2 px floor band was mostly its light stripe and read as a
white line; the light stripe now turns into the colour between 4.5 and 2.5 px and the wash fades with it, so a far band
is a thin line of its colour. At gameplay03 (about 8 px) nothing changes. Tests:
`ASharedEdgeDrawsOneCentredBandAndTheFrontierKeepsTheFullBand` (the proof's blocks, all primary and then primary
against none), `AtTheOverviewTheBandsAreThin` (now also: few near-white pixels, a blueness gain), and
`UseBakesWhichEdgesBorderAnotherRegion` (the shared bits). The proof pattern alternates styles, so the proof's
measurements and `ColourBlindPlayersTellTheBorderStylesApart` are unchanged.

**Two colours: who held which areas (owner rulings 2026-10-14, refs #196).** The owner ruled that the borders use promo
03's two colours: a deeper blue for the Israeli side (about 89,119,156 on screen, against main's lighter 120,142,192) and
orange-red for the other side, from sourced data only, with grey for contested ground, no-man's land and the demilitarized
zones. Further rulings the same day: (a) the legend in English and Hebrew; (b) the localities stay as of 1948 ("start of
game only"): the colour claims military control only; (c) the first-truce snapshot from 11 June may use the archive map's
line dated 9 July (the closest sourced line, labelled as such), and grey marks areas changing hands only for the days an
operation ran there; (d) no outward request to the archives: the best public copy of ISA map 6158/1, the accuracy (about
+-1-2 km) stated in the data. Research note: `research_notes/Map borders/control-areas-1948-49.md`. Licence basis: decision
D-C (owner-verify).
- *Data:* `content/map/control_1948_49.geojson` (true polygons, status draft, owner-verify), written by
  `tools/control_digitise.py` from Israel State Archives map 6158/1 (1949; the NYU Faculty Digital Archive copy, 2368 x 4078 px:
  the ISA viewer refused a plain client and Commons has no copy, checked 2026-10-14). The scan is georeferenced on its own
  printed 10-km Palestine grid (a cubic fit, rms about 0.1 km); its red (9 July 1948), blue (18 July 1948) and green (end of
  October 1948) front lines are traced along their own pen colours between waypoints read off the scan; the partition line
  (teal) and the Mediator's demanded line (yellow) are not control lines and are not traced (where the green and the
  partition line run close, Resolution 181's own wording and the points the map labels in teal tell them apart). A later
  line is drawn only where the front moved, so each snapshot is the earlier lines less the stretches a later line replaces,
  plus enclaves leaving on the dates the sources give (Tira 16 July; Operation Shoter 24-26 July; Operation Hiram). The
  lines and the Mandate outline are polygonised and each face takes a side by parity from Tel Aviv; a face reached both ways
  fails the run. Closure checks: 44-50 sourced anchor places per snapshot land on their side unless within 2 km of a line
  (3 km once the low-accuracy Jordan line is in), and every holder place (`HOLDERS`) lands on the other side with no
  tolerance. The scan stops at the 60-km northing: south of it nothing is coloured until the Jordan agreement's line d
  closes the south on 3 April 1949. Each feature cites the source it comes from, with that source's accuracy: the 1948
  snapshots and the operations' grey ISA 6158/1 (and the operations' pages); the 1949 sides, the zones and the Egypt
  closure the agreements' lines (`armistice_lines_1949.geojson`, D-A) and UNTS vol. 42, the Jordan-derived sides flagged
  low accuracy.
- *Periods* (`content/map/control_hexes_1948_49.yaml`, `tools/control_bake.py`: each land hex of Mandatory Palestine takes
  the code at its centre, per period): 15 May-11 June 1948 the 1947 sub-districts in grey (no sourced line for 15 May);
  11 June the 9 July line; 8-18 July (the Ten Days) grey where the 9 and 18 July lines differ; 18 July the blue line;
  24-26 July grey at Ijzim, Jaba and Ein Ghazal; 15-31 October grey where the July and October lines differ in the south
  (Yoav, Ha-Har, Beit Jibrin, al-Dawayima), 29-31 October in the Galilee (Hiram); from 31 October the green line, until the
  Egypt agreement (Operation Horev is not drawn: its fighting lay south of the scan or beyond the frontier, and no source
  gives a line for January 1949); from each agreement's date (24 February, 23 March, 3 April with Art. VI.3's stages on 8
  May, 22 May and 17 July, 20 July 1949) that front takes its armistice line, each 1948 piece of that front leaving on the
  date of the nearest line of its front, and the demilitarized zones are grey. Gaps closed: Jerusalem by the 1948 lines
  within 4.5 km of the city (map 2 is not available); the Egypt line south of Gaza straight to the frontier within Art.
  VI.3's bounds, the land within 3 km of that stretch on its Israeli side left without colour (the Strip itself stays
  Egypt's: Gaza, Deir al-Balah, Khan Yunis and Rafah read Egypt in every period, pinned by `test_control.py`).
- *Pinned towns* (`pinned_towns`; the bake gives the town's hex the sourced side, as D-B's rule 5 does for Gazette places):
  Mishmar HaYarden, Syria's from June 1948 to 20 July 1949 (en.wiki), whose bridgehead is narrower than the map's accuracy;
  Qalqilya, on the Jordanian side from 3 April 1949 (en.wiki quotes Hadawi, who argues that the 1949 lines severed its groves
  in favour of Israel, which implies the town stayed on the Jordanian side: an inference; on the UNISPAL copy of map 1 the pen line runs along the town's western edge, which 51 m a pixel cannot settle),
  which the low-accuracy Jordan trace puts 2 km to the Israeli side; Huleiqat, Egypt's from 8 July to 19 October 1948 (en.wiki: retaken by the
  Egyptian army on 8 July, captured by Giv'ati in Operation Yoav on 19 October), where ISA 6158/1's 9 July line, about
  1.3 km away, puts it on the Israeli side (the source and the archive map disagree; recorded in the data); a pin covers
  whole periods only, so the Yoav period shows Huleiqat's hex Israeli from 15 October, four days before its capture.
- *Drawing:* `MapRegions` draws `Control1948`'s layer from 11 June 1948: Israel Primary, the other side Secondary,
  contested, no-man's land and zones the new Neutral style (`BorderStyle.Neutral`, the shader's third style, dashed along
  its edge every 2 km, 65% drawn: a cue that is not colour), the land beyond the frontier style none, no colour left out (no
  band, so the edge of the sourced data is not drawn as a front). Before it the sub-districts are drawn in Neutral, since
  blue now means the Israeli side. Colours: Primary pre-grade sRGB 0.29, 0.40, 0.56 (was 0.40, 0.50, 0.80), measured on
  screen at deltaE76 0.6 from 89,119,156; Neutral 0.46, 0.46, 0.44 with a pale stripe 0.98, 0.97, 0.93.
- *Label* (`map-layer`, `MapRegions.ControlLabel`): the legend names each side before its colour, and grey as dashed:
  1948 "Held by Israel: blue · Held by Arab forces: orange-red · Contested or no-man's land: grey, dashed", with the caption
  "Front lines, {date}, approximate (Israel State Archives)"; once a front has its agreement "Israeli side of the armistice
  line: blue · Arab side: orange-red · No-man's land or demilitarized zone: grey, dashed", with "Armistice lines: {fronts}"
  (all switched) or "Armistice lines: {fronts}; elsewhere front lines of {date}" (some), Jordan named "Jordan (low
  accuracy)" (D-A ruling 2). Dates in the UI's language (Hebrew month names). The label may take up to six lines in its
  narrow box; `ControlTests.TheLongestLegendLabelFitsBesideTheEventCard` checks EN and HE at eleven window shapes. The info
  panel says who held the hex ("Held by Arab forces (Egypt)"; Jordan is named as the state from its agreement; in 1948 the West Bank's holders share one connected area no sourced line divides, so it
  is named only as "Arab forces"), nothing for a hex beyond the frontier, and no sub-district once its bands are gone.
- *Known staleness and limits:* the October 1948 line stands until the Egypt agreement (24 February 1949), so Isdud (taken
  28 October), Majdal (4-5 November) and Yad Mordechai stay on the other side until then (the caption names the source
  line's date). Huleiqat is pinned (above). The ground
  east of the Beersheba-Hebron front reads as held by Arab forces in 1948, since the source draws only front lines; no "not
  held" area is invented. Jerusalem's line falls between two hexes (the Old City's hex is the other side's, the one west of
  it Israeli; the grey rule for a hex holding both sides does not fire).
- *Not done, recorded in the data (coordinator rulings, 2026-10-14):* the Syrian and Israeli truce lines of S/1353/Rev.1 (on
  its colour scan the Israeli line is legible only in parts and the Syrian line cannot be told from the zone's outline and
  the river; ISA 6158/1's red and blue cover that front, and Mishmar HaYarden is pinned); the CIA ORE 38-48 map as a
  cross-check (its reading-room copy was unreachable and the map exists online only as a JVL copy; the anchors come from
  Wikipedia); January 1949; the Lebanese villages held from Hiram to the armistice (outside the frontier: not coloured);
  sub-hex features (Kfar Darom, Mount Scopus, the Faluja pocket's exact edge) kept in the true polygons only.

**Research item: region data (to source, then the owner's sensitivity review).**
- *What:* per-hex region labels for (a) the sub-districts of Mandatory Palestine at the end of the Mandate (six districts:
  Galilee, Haifa, Samaria, Jerusalem, Lydda, Gaza; sixteen sub-districts: Acre, Beisan, Nazareth, Safad, Tiberias, Haifa,
  Jenin, Nablus, Tulkarm, Hebron, Jerusalem, Ramallah, Jaffa, Ramle, Beersheba, Gaza), dated by the Gazette changes of
  1942, 1945 and 1947; (b) the 1949 armistice lines, each dated by its agreement (Egypt 24 Feb, Lebanon 23 Mar, Jordan
  3 Apr, Syria 20 Jul 1949; the events already in content/); (c) fronts, from war mode's data once #21 defines it.
- *Sources to check:* Palestine Gazette, Supplement No. 2, No. 1163 (15 Jan 1942) pp. 119-132, No. 1415 (7 Jun 1945)
  pp. 621-629, No. 1552 (30 Jan 1947) pp. 125-133 (cited by https://en.wikipedia.org/wiki/Districts_of_Mandatory_Palestine,
  checked 2026-10-11); the Survey of Palestine 1:100,000 sheets (1944-48), which draw district and sub-district boundaries
  (Princeton Digital Maps, https://maps.princeton.edu/catalog/princeton-dc5999ng80p); UN map "Palestine: Land ownership by
  sub-districts" (1945 data), https://digitallibrary.un.org/record/498482; the armistice agreements themselves (UN S/1264/Corr.1,
  S/1296, S/1302/Rev.1, S/1353; per https://en.wikipedia.org/wiki/1949_Armistice_Agreements, checked 2026-10-11), whose
  lines are given as Palestine-grid map references; whether maps were annexed is to verify. No open, licensed vector file of the sub-districts was found (search 2026-10-11): AUB's
  "Palestine Population Sources" (2018, https://scholarworks.aub.edu.lb/handle/10938/22154) covers four sub-districts and
  states no licence; Palestine Open Maps states none either. So the boundaries must be digitised from a primary source
  (the Gazette schedules or the Survey sheets), with the method and its error recorded; their copyright status (Government
  of Palestine publications of the 1940s) is a licence question for the owner (docs/oss/DECISIONS.md) before any import.
- *Shape:* sourced, dated geometry in content (the chapters design's `lines.yaml`, §448, or a polygon file beside it), and a
  tool that labels each hex by its centre (as `tools/settlement_hexes.py` does for settlements) into the per-hex layer the
  renderer takes (`RegionLayer`). Every entry is submitted `fact-checked`; the owner sets `sensitivity-reviewed` before a
  shareable build (chapters design §13).
- *Limits of the renderer's data seam (review M8), for the data work to plan around:*
  1. Two drawn styles (Primary, Secondary) plus None. The 1949 lines set Israel against four neighbours; with two styles
     the four share the red-orange band (the texel's style takes b's low two bits since the shared edges, so one more style fits; each new style needs colours that stay
     apart for colour-blind players, so it is a design task, not a constant). Districts may want a neutral third style.
  2. One layer at a time: districts and armistice lines cannot show together.
  3. Styles are per region, not per edge: an edge's look comes from the regions on its two sides.
  4. Hex snapping moves a line by up to half a hex (see the owner decision below).
  Follow-up: more styles or per-edge styles when the data and the owner's ruling on snapping need them.
- *Owner decision (ruled 2026-10-11: store the true lines, snap to hex edges only for display, as the promo does):* the promo snaps borders to hex edges, but spec §3 (map state) says hex boundaries carry no
  political meaning and the chapters design draws lines as true polylines. Snapping a line to hex edges moves it by up to
  half a hex (about 4 km). Options: (1) hex-snapped regions (this renderer, the promo look); (2) true polylines (a separate
  overlay, not the promo look); (3) both, hex-snapped at the gameplay zoom and true lines in a map mode.

**Measured.**
- Rendered tests (EditMode, `BorderTests`, 1920x1080 at gameplay03's distance and yaw, tilt-shift off, on the proof's
  primary-secondary edge (3, 11)-(4, 11) on the flat coastal plain, where the promo's lines are measured): along 9 points
  of the edge a blue stripe (graded sRGB 0.48, 0.58, 0.79; promo 0.49, 0.55, 0.71), a white one (0.93, 0.91, 0.89; promo
  0.98), a red-orange one (0.74, 0.37, 0.18; promo 0.69, 0.36, 0.24), a cream one (0.92, 0.79, 0.53; promo 0.96, 0.74,
  0.49). Band widths (median, stripes classified by colour so the wash is not counted): primary **8 px**, secondary
  **6 px**, against the promo's about 8 and 5 (bounds 6-11 and 3.5-8). Nothing changes 1.2 km or more from the edge; the
  middle of a coast edge is unchanged (0/134 pixels); no clearly-water pixel moves at gameplay03 (0 of 631,101); diorama02
  3.2% of pixels changed (thin, label included); renders repeat exactly.
- Colour-blind (`GradeTests.ColourBlindPlayersTellTheBorderStylesApart`, Machado 2009 severity 1, deuteranopia and
  protanopia, five points on a coastal-plain edge and five on a Negev desert edge): blue against red-orange 69-80;
  primary colour-land 54-77, light-land 38-47, colour-light 44-50; secondary colour-land 9-31, light-land 3-49,
  colour-light 30-35. Asserted per point: styles >= 15; each band's stronger stripe >= 15 from the land; its two stripes
  >= 20 apart; its weaker stripe >= 5 unless the stronger one is >= 25. Not both stripes >= 15 (review M5), because a
  light stripe is meant to be light: on pale Negev sand the promo's cream stripe melts into the ground for normal vision
  too (3.2-6.1, logged), and the band reads by its red-orange stripe there (19-31). The red-orange is a little more
  saturated than the promo's (set 0.75, 0.31, 0.13) so it keeps that contrast on olive and on dry grass.
- Frame cost (WebGL, 1920x1080, headless). Measured on the first, wider version (300 m / 230 m, before the water fade),
  when the release WebGL player still honoured `?borderproof`; the machine was loaded (`visual=none` 10 ms against
  Task 8's 2.4 ms `all`), so read the differences:
  - Seven fetches on every land pixel: gameplay03 `all` 14.97 - `-borders` 11.64 = 3.3 ms, over the 2.5 ms budget; hence
    the early-out.
  - With the early-out, the proof drawn: gameplay03 13.33 (spread 0.31) - 12.42 (0.22) = **0.91 ms**, within budget.
    diorama02 16.93 (1.06) - 15.82 (0.73) = 1.11 ms is within one spread: **not resolved**; re-measure on an idle machine
    with real data (promo-borderdata), in a development build or a release build with a data layer.
  - Without data: see I1 above (not measurable against main on this machine).
- WebGL build: no `Shader error`, no "not supported" line (GLES3: `LOAD_TEXTURE2D` is `texelFetch`, static const arrays,
  no new derivative use beyond Task 8's `ddx`/`ddy` in uniform control flow); both goldens OK (Hash1952 62f8ec61 since
  #137), and `golden&smoke=5` passes on the build of d1973db.
- Texture units with promo-land merged (review M10): the terrain fragment's own textures are 6 (Bake, Coast,
  Ao and Land through the graph, `_ScBiomeTex`, `_ScRegionTex`; promo-land dropped Tracks), plus URP's (main-light shadow
  map, reflection probe and the like): about 10-11 by the source, under WebGL2's guaranteed 16. Built, not only counted:
  a local test merge of d1973db with origin/promo-land 7db1a3a (not pushed) built for WebGL with 0 errors and no
  `Shader error` (76.0 MB), and booted in Chrome for Testing with `GOLDEN OK 1950` and `GOLDEN OK 1952` and no "not
  supported" or `Shader` line (web_check's fail pattern).
- Checkpoints (art-log rows): `promo-borders-before` (main 807e300); `promo-borders-proof` and `promo-borders-after` were run
  on 807e300's tree plus the then-uncommitted branch, so the art log records 807e300 for them (now marked `807e300+wip`
  there); the rings in them are the old pattern. The review rounds' checkpoints are on d1973db (art-log rows
  `promo-borders-noproof` and `promo-borders-r2-borderproof`): `promo-borders-noproof` (no proof: the branch as played,
  goldens OK) and `promo-borders-r2` (`--borderproof`: the editor renders show the labelled checker; its WebGL shots, a
  release build, ignore the flag and match `-noproof`'s numbers exactly).

## promo-sea: water, surf and beach (art track, open item 3, issue #38)
`ScWaterShade` (`StatecraftMap.hlsl`) is reworked on both sides of the edge, and a new `ScShore` paints the land's shore.
Review round 1 (staff review I1-I2, M1-M8) is folded in; the values below are the shipped ones.
- **Depth.** The map has no bathymetry (GLO-30 is a land model; its sea texels are 0 m), so depth is still the distance to
  the shore from `coast.png`. The ramp:
  - turquoise shallows `ScShallowM` 2.6 km wide, on a linear weight so the band reads at its width; the edge wanders
    +-35% on a 3 km noise;
  - then teal (mid), then a steel blue (deep) by 12 km (`ScCoastRangeM`), on `pow(smoothstep(0, 12 km), 0.6)`.
- **Colours** (sRGB, before the grade; Task 7's in brackets):

  | Constant | Value | Task 7 |
  |---|---|---|
  | `ScShallowSrgb` | (0.40, 0.72, 0.70) | (0.38, 0.74, 0.72) |
  | `ScMidSrgb` | (0.29, 0.50, 0.59) | (0.13, 0.47, 0.58) |
  | `ScDeepSrgb` | (0.27, 0.40, 0.54) | (0.06, 0.24, 0.40) |
  | `ScFlatSeaSrgb` (the `water` toggle's flat sea, x 0.8) | (0.20, 0.36, 0.47) | (0.16, 0.42, 0.55) |
  | `ScGalileeSrgb` (fresh water, -300 to -100 m) | (0.20, 0.45, 0.55) | (0.15, 0.48, 0.60) |
  | `ScSandSrgb` / `ScSaltSrgb` | (0.88, 0.76, 0.56) / (0.92, 0.90, 0.85) | none |

- **Waves.** Two layers drive the wave normal:
  - the 700 m swell (unchanged);
  - a 170 m chop moving the other way, `ScChopGain` 1.4 times the swell, full under a 40 m pixel and gone by 170 m.

  The tilt is `ScWaveTilt` 0.6 under a 50 m pixel (gameplay03 is about 45 m: chop and sparkle show). It eases to
  `ScWaveTiltFar` 0.10 by 120 m (Task 7's 0.15 until sea-fix): the far sea reads calm, and the seam test's patch means
  stay within bound (at a flat 0.6, one 7x7 patch at 120 km carried 0.032 against 0.012). The glint (0.06) fades out
  between 60 and 120 m pixels (sea-fix, below).

  Bug B10 (sea-fix): both layers were value noise differenced along the map's axes, on axis-aligned lattices. Their
  slope jumped at every cell edge, so the open sea showed a blocky checker at 6-20 km. Both layers are now gradient noise
  with its analytic slope (`ScGradientNoiseD`, quintic fade: smooth across cell edges), the swell's lattice turned
  0.6 rad and the chop's -0.9 rad (`ScWaveSlope`), scaled by `ScWaveSlopeGain` 0.22 to the old layers' strength (open
  sea luma std at 6 / 12 / 18 km: 5.5 / 6.6 / 6.8 before, 6.0 / 6.8 / 7.1 after, in 8-bit levels). Fewer hashes than
  before: 4 gradients per layer against 16 value-noise lookups. `TheOpenSeaHasNoBlockGrid` renders the open sea straight
  down at 4 m a pixel and folds the luma gradient by its position in each old lattice's cell; the block-edge energy
  ((largest bin - smallest) / mean, bound 0.25) was 0.660 before (170 m along z) and 0.122 after. `MapCapture.SeaClose`
  renders the sea at 6, 12 and 18 km. With the new swell the seam test's single +-2.5 km patches (one 7x7 patch is about
  one swell cell at 120 km) read up to 0.016 apart (bound 0.012) at the far tilt of 0.15, so the far tilt is now 0.10,
  and the glint, the near zoom's sparkle, fades out between 60 and 120 m pixels.
  Frame time (headless; another session loaded the machine, load average 4-18, so the spreads are wide):
  - `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=8.35 spread_ms=0.11 median_p95_ms=10.00`
  - `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=8.03 spread_ms=2.69 median_p95_ms=9.00`
  - `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=13.54 spread_ms=0.22 median_p95_ms=14.00`
  - `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=13.24 spread_ms=0.21 median_p95_ms=14.00`

  Water and shore cost 0.32 ms at gameplay03 (inside a 2.69 ms spread) and 0.30 ms at diorama02, against the 2.5 ms
  budget (0.89 / 0.13 ms in promo-sea round 1); p95 with everything on is 10 / 14 ms, under 16.7 ms.
- **Surf** (review I2).
  - A swash line, full to `ScSwashM` 180 m and gone by 324 m.
  - Breakers through a `ScSurfM` 900 m zone. They are a squared triangle wave in the distance from the shore, spacing
    `ScBreakerSpacingM` 300 m, rolling shoreward at `ScBreakerRate` 0.05 cycles/s (15 m/s, a quarter pixel a frame at
    gameplay03 and 60 fps).
  - The breakers are box-filtered in closed form (`ScTriSqBox`), so they never alias and tend to their mean (1/3) as the
    pixel grows. The previous `pow(tri, 5)` crest was about 21 m wide on 45 m pixels: hairline, dashed crests.
  - The box is `fwidth(phase)` x `ScBreakerFilterPx` 1.25, with the phase taken by `ScBreakers` outside the water branch
    in both callers (re-review N2). The phase's true gradient is not 1 / 300 m: its noise term adds up to 0.0025
    cycles/m and the 8-bit coast's slope varies up to 2x, so a width from the nominal spacing was up to 2x too narrow in
    places.
  - The 300 m break-up noise fades to its mean between 75 and 150 m pixels.
- **Beach** (`ScShore`).
  - A sand strip along every sea-level coast (above -100 m): the Mediterranean and the Gulf of Eilat, both sand beaches,
    but not the rift lakes. `ScBeachM` is 650 m on average, +-40% on a 4 km noise. The width is symbolic, after the
    promo's beach: wider than the real 30-100 m strip, inside the coastal dune belt.
  - Wet sand (x 0.78) to 60-160 m; steep faces keep their rock.
  - White salt flats along the Dead Sea (below -300 m).
  - Both elevation thresholds are 20 m ramps (review M7: no hard contour where a shore slope crosses them).
  - The shore is off with the `water` toggle (review M1), so `all - (-water)` measures water and shore together.
- **Coast precision (review M6).**
  - `coast.png` is L8 over +-12 km: 94 m a level (texels 79 m E-W by about 211 m N-S), linearly interpolated between
    texels. A crest or band edge can sit up to 47 m off its true place, about one pixel at gameplay03 (about 45 m), and
    under the breakers' 2-cycle phase noise.
  - The wet-sand band (60-160 m) is about one level wide: a soft darkening, not a measured feature.
  - A 16-bit coast, or a fine near-shore channel, is the fix if surf detail is pushed below about 100 m.
- **Test and capture hooks.**
  - `MapShaderGlobals.PinWaterTime(seconds)` pins the water's clock (`_ScWaterTime`), so tests and captures can render
    chosen moments.
  - `MapCapture.SurfFrames` renders 60 gameplay03 frames on the web tier, consecutive at 60 fps or `-surfDt` seconds
    apart.
- **Not done here.**
  - Boats: the owner wants no civilian boats; navy ships are units, on another track. (Superseded by #536: see "Sea and
    boats".)
  - Harbour water and piers at Jaffa need geometry, which belongs to the settlement track.
  - Issue #16 (the 1948 Dead Sea level, Lake Hula) is untouched.

### Measured
WebGL `?shot`, from `tools/art_checkpoint.sh`. The checkpoints:
- before: `promo-sea-before`, at `0f13336`;
- round 0: `promo-sea-after`, at `45a357e`;
- round 1: `promo-sea-r1`, at `af6e3cc`.

Sea pixels are those with blue above red by 0.06, outside the top bar, the bottom-left panel, the minimap and promo 03's
icon column. Spread is the p90 minus p10 of sea-pixel luma; "sea sat p10/p90" is the same for HSV saturation.

| | before | round 0 | round 1 | promo |
|---|---|---|---|---|
| gameplay03 frame saturation | 0.57 | 0.49 | 0.48 | 0.38 |
| gameplay03 contrast | 0.196 | 0.182 | 0.174 | 0.203 |
| gameplay03 warmth | 0.077 | 0.103 | 0.102 | 0.064 |
| gameplay03 palette distance | 0.69 | 0.61 | 0.60 | - |
| gameplay03 sea mean sRGB | (21, 77, 105) | (49, 85, 107) | (62, 101, 121) | (57, 81, 103) |
| gameplay03 sea saturation | 0.82 | 0.55 | 0.49 | 0.47 |
| gameplay03 sea sat p10/p90 | 0.57/0.93 | 0.44/0.63 | 0.43/0.55 | 0.29/0.58 |
| gameplay03 sea luma p10/p90 (spread) | 0.16/0.45 (0.29) | 0.26/0.39 (0.13) | 0.31/0.44 (0.12) | 0.09/0.48 (0.39) |
| diorama02 frame saturation | 0.47 | 0.43 | 0.42 | 0.43 |
| diorama02 contrast | 0.203 | 0.192 | 0.187 | 0.200 |
| diorama02 warmth | 0.104 | 0.117 | 0.117 | 0.145 |
| diorama02 palette distance | 0.67 | 0.67 | 0.59 | - |
| diorama02 sea mean sRGB | (40, 82, 111) | (63, 97, 118) | (74, 109, 129) | (80, 120, 151) |
| diorama02 sea saturation | 0.66 | 0.48 | 0.43 | 0.47 |
| diorama02 sea sat p10/p90 | 0.59/0.73 | 0.42/0.52 | 0.41/0.46 | 0.30/0.56 |
| diorama02 sea luma p10/p90 (spread) | 0.24/0.42 (0.19) | 0.32/0.40 (0.08) | 0.36/0.44 (0.08) | 0.38/0.53 (0.15) |

Against the bars:
- diorama02 warmth stays in [0.087, 0.145], and saturation stays at or under 0.48.
- The sea means now sit between the two promos.

What still differs:
- The sea's spread is a third of promo 03's and half of promo 02's. Promo 03's dark tail is boat hulls and building
  shadows on the water, and its bright tail is glints and foam lace. (The boats are superseded by #536: see "Sea and
  boats".)
- gameplay03 contrast falls as the sea brightens (0.196 -> 0.174).
- gameplay03 warmth rises as the sea loses blue and gains a beach (0.077 -> 0.102 against the promo's 0.064).

`SeaTests` (round 1, against Task 7, each with its bound):

| Check | Task 7 | Round 1 | Bound |
|---|---|---|---|
| Foam over shallows (luma) | 0.154 | 0.180 | 0.02 |
| Shallows over the open sea | 0.344 | 0.211 | 0.05 |
| Dead Sea over the Mediterranean | 0.46 | 0.27 | 0.1 |
| Diorama open sea b - r | 0.299 | 0.230 | 0.15 |
| Haze luma shift | 0.069 | 0.034 | 0.01 |
| Open sea pre-grade HSV saturation | 0.91 | 0.49 | < 0.7 |

`TheSeaBeyondTheEdgeMatchesTheTerrainSea`: 32 lines pre-grade max 0.007 (bound 0.012; Task 7 0.004), graded max 0.014
(0.024).

New tests:
- `ASandStripLinesTheMediterraneanShore`.
  - Strip off: RGBA(0.716, 0.656, 0.378), luma 0.65, b/g 0.58, 0.25 over the plain.
  - Strip on: RGBA(0.906, 0.771, 0.516), luma 0.78, b/g 0.67, 0.39 over the plain.
  - Bounds 0.72, 0.62 and +0.32: all three fail with the strip off.
- `TheBreakersAreFilteredToThePixel` (re-review N1).
  - It renders at 30 km and 960x540, about 45 m a pixel: the same footprint as gameplay03's 60 km at 1920x1080. There an
    unfiltered crest is sub-pixel but not yet faded.
  - Each water pixel of a 49x49 patch around the surf zone (6067 pixels over four moments of a cycle) must match its 4x4
    block in the same view rendered at 4x. Pixels touching land or markers are left out: the shoreline's step and the
    markers' edges are not filtered by design.
  - With the filter: p99 0.0082, worst 0.0176 (bounds 0.014 and 0.035).
  - Red with Round 0's crest swapped back in temporarily: p99 0.0200, worst 0.0620, and the test fails.
  - The same squared crest without its box measured p99 0.0087, worst 0.0247. At 300 m spacing it is about two pixels
    wide by itself, so the box is what keeps it exact as the zoom changes.
  - The surf pixel spans luma 0.642-0.788 over one breaker cycle (bound 0.03).
  - The overview is still: 0.0036 over 0.5 s (bound 0.01).

In motion (`MapCapture.SurfFrames`, a 200x400 px coast crop at gameplay03):
- Over one second of consecutive 60 fps frames, no pixel changes more than 2/255 from one frame to the next, either
  before or after the change.
- Over the whole second, the old crests moved single pixels by up to 46/255 (2.8% of the crop over 2/255). The filtered
  breakers move them by at most 9/255 (2.5%), the same with the `fwidth(phase)` box. The open sea's waves move them by
  at most 14/255 (1.7%).
- A 15 s clip at `-surfDt 0.25` shows soft crests rolling in, broken along the shore.

Frame time: headless, the machine loaded by other sessions (load average 4-12), so the absolutes are inflated; read the
differences.
- `PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=8.19 spread_ms=0.05 median_p95_ms=9.00`
- `PROFILE summary visual=-water pose=gameplay03 runs=3 median_avg_ms=7.30 spread_ms=0.05 median_p95_ms=9.00`
- `PROFILE summary visual=none pose=gameplay03 runs=3 median_avg_ms=7.01 spread_ms=0.05 median_p95_ms=8.00`
- `PROFILE summary visual=all pose=diorama02 runs=3 median_avg_ms=13.63 spread_ms=0.18 median_p95_ms=15.00`
- `PROFILE summary visual=-water pose=diorama02 runs=3 median_avg_ms=13.50 spread_ms=0.14 median_p95_ms=15.00`
- `PROFILE summary visual=none pose=diorama02 runs=3 median_avg_ms=10.31 spread_ms=0.03 median_p95_ms=13.00`

Water and shore together (the toggle now covers both):
- gameplay03: 0.89 ms, against a 2.5 ms budget. Round 0 measured 0.50 ms for the water alone; the shore was then
  unmeasured.
- diorama02: 0.13 ms, below the noise (spread 0.18 ms).
- p95 is at or under 16.7 ms at both poses.

### Sea round 2 (branch `sea-round2`, refs #388)
At the gameplay and overview zooms the sea had faded to a smooth surface. The chop is gone by a 170 m pixel, and the
wave tilt and glint fade out by 120 m (Task 7, sea-fix), so gameplay03's 46 m pixels and diorama02's 190-300 m pixels saw
a flat gradient. Sea detail on main measured 0.013 against promo 03's 0.060, and 0.019 against promo 02's 0.063. Every
change is in `ScWaterShade` and `ScHazeWater` (`StatecraftMap.hlsl`), which only the water's own pass runs since #195,
plus `ScShore`'s two constants. `Sea.shader` is unchanged.
- **The far sea's texture**, all behind a new `seatexture` visual toggle (`_ScSeaTexture`), so the profile prices it as
  one feature:
  - wind streaks: a gradient-noise brightness variation (`ScMottleCellM` 2.5 km, 3x narrower across the wind, so 833 m
    there, with a finer octave of about 309 m, +-`ScMottleGain` 0.32) on the open Mediterranean, past the shallows, not on
    the rift lakes. Neither octave is faded by the footprint;
  - a ripple: the waves' slope towards or away from the camera, read as their sky reflection. Under a 50 m pixel it is
    +-0.54 (the tilted normal: swell and chop), faded out over 50-90 m; the 170 m chop's limit is 85 m. From 90 m it is
    +-0.28 (the 700 m swell's own slope), faded over 250-450 m. That leaves it at half strength at the swell's 350 m limit,
    and at 84-100% at diorama02's 190-300 m pixels (2.3-3.7 px a swell cell). It runs at 40% in the turquoise shallows,
    so they keep their pale band;
  - whitecaps: a fleck in some 120 m cells on the streaks' bright side, its brightness the share of the pixel it covers,
    gone by an 80 m pixel.
- **Glint:** 0.12 under a 40 m pixel, easing to Task 7's 0.06 by 60 m, so nothing changes from 60 m out.
- **Surf line:** the swash is never under 1.5 pixels wide (`max(ScSwashM, 1.5 x footprint)`). The breakers and their box
  filter are unchanged; at the breaker test's zoom the swash is still its own 180 m.
- **Beach:** `ScBeachM` from 650 to 900 m; `ScSandSrgb` (0.88, 0.76, 0.56) to (0.90, 0.80, 0.62), drier and paler.
- **Water haze:** the water's haze starts `ScWaterHazeLeadM` (80 km) nearer than the land's, at 120 km, so the
  overview's sea pales towards the horizon as promo 02's does. The gameplay zoom, under 100 km, has none.
- **The seam test** (reviewer-agreed option A). A 2.5 km streak pattern varies by itself between the seam test's points
  5 km apart, so `TheSeaBeyondTheEdgeMatchesTheTerrainSea` pins `seatexture` off and keeps its bounds and cases. The new
  `SeaTests.TheFarSeaTextureHasNoStepAtTheMapEdge` checks the edge with the texture on. It renders the west edge straight
  down at the seam test's 110 m a pixel, over a 40 km strip, with the water's clock pinned, and runs two checks:
  - per row, the change between the two pixel columns either side of x = 0 against neighbouring columns elsewhere. The
    RMS excess at the edge must stay under 0.012;
  - the mean signed change across the edge, summed over r, g and b, must stay under 0.012.

  | run | RMS excess | mean change |
  |---|---|---|
  | green (final values) | -0.0032 | 0.0008 |
  | red: the skirt's map coordinates offset by one streak cell (a texture that does not continue; streaks at 0.25) | 0.101 | |
  | red: the skirt 4% brighter (an input step; streaks at 0.25) | | 0.0195 |

  Both red runs fail. Fitting a line to each side and extrapolating was tried first. Over the whole strip, a mismatched
  texture's steps cancel; per 880 m segment, the texture's own variation swamped the fit (0.049 with no seam, 0.059 with
  one).

- **`MapEdgeTests.PanningIntoACornerShowsNoSkirtSeamOrFlatFill` pins the water's clock.** It compares a render with the
  skirt against one without, and the water moved between the two. A few pixels of the north-west corner's coast changed at
  random: 0-14 a run, with the texture on or off, and main has the same exposure. The far sea's ripple made a failure
  likelier (5 and 1 pixels in two branch runs). Pinned, it passed 3 of 3 runs with 0 pixels at every corner.

Measured: WebGL `?shot` at main `db99f3a` (with towns round 2, land round 2 and #399's lighter far tilt-shift), then the
branch on it (art-log rows `main-db99f3a` and `sea-round2-r`). The sea crops are those of #388, plus gameplay03's focus
band (outside the tilt-shift's bands). Detail is the std of luma minus a 4 px blur, with the crop resized to 480 wide.
Spread is luma p90 - p10.

| | main | round 2 | promo | target |
|---|---|---|---|---|
| gameplay03 sea, #388 crop: detail / spread | 0.014 / 0.089 | **0.045** / **0.160** | 0.060 / 0.20 | >= 0.035 / >= 0.15 |
| gameplay03 sea, focus band: detail / spread | 0.017 / 0.088 | **0.055** / **0.185** | 0.075 / 0.32 | >= 0.035 / >= 0.15 |
| diorama02 sea: detail / spread | 0.019 / 0.038 | 0.026 / 0.069 | 0.063 / 0.11 | - / >= 0.07 |
| gameplay03 palette distance | 0.361 | **0.347** | - | no worse |
| diorama02 palette distance | 0.563 | **0.534** | - | no worse |
| diorama02 warmth / saturation | 0.109 / 0.406 | 0.111 / 0.400 | 0.145 / 0.43 | [0.087, 0.145] / <= 0.48 |

The gameplay03 floors above (detail >= 0.035, spread >= 0.15) are superseded by "Sea and boats" below (#388 and #536;
the detail floor was the maintainer's call, the spread floor the owner's). Promo 03's own open sea measures 0.012 in
edge density, so the far sea is now calmed on purpose. The floors are now:
- detail >= 0.025 in #388's crop and >= 0.033 in the focus band, just under the branch's 0.029 and 0.037;
- spread >= 0.09, under the branch's 0.096 and 0.104. The author proposed it and the reviewer recommended it; the owner
  decided "Lower to 0.09" on 2026-10-14 (#536, "Changed acceptance criteria").

They are kept so the sea cannot go flat. #388's "Done when" was updated to match.

diorama02's spread misses its target by 0.001. Strengthening the streaks barely moves it (0.25 to 0.32 added 0.003). A
stronger far ripple could close it, but the far ripple already runs at 84-100% at diorama02, at 2.3-3.7 px a swell cell.
More contrast at that sampling would push it towards aliasing, so it was not raised. In the editor (web tier) the same
spread is 0.079. The sea's faint hex lines (`GridWaterOpacity` 0.10, a measured grid ruling) are harder to see against
the textured sea; that belongs to the grid, not here.

**In motion** (review of #402: the far ripple and the streaks' fine octave sit near or under their sampling limit at
diorama02). A temporary editor probe rendered diorama02 (web tier, tilt-shift on) during a slow pan (60 m east and 30 m
north a frame, about 0.25 px) and a slow zoom (0.25% a frame), 24 frames each, with the water's clock at 1/60 s a frame.
It was run for main `b10e95e`, the branch, and the branch with the far ripple faded over 175-350 m. For each 64 px
open-sea patch and each pair of consecutive frames, the patch's sub-pixel shift was found by phase correlation and taken
out. What remained was compared with the patch's fine texture (luma minus a 3 px blur):

| sea band | main: fine texture / residual | branch | branch, far ripple at 175-350 m |
|---|---|---|---|
| in focus (y 30-60%), pan / zoom | residual 38% / 38% of 1.0 level | 19% / 19% of 4.7 levels | 20% / 18% |
| far (y 13-30%, in the tilt-shift's band), pan / zoom | 0.35 level, residual 0.24 / 0.35 level | 0.60 level, residual 0.44 / 0.57 level | - |

In focus the texture moves coherently: its residual is half main's share, at far more texture. In the far band, which is
already blurred by the tilt-shift, what is left is under one 8-bit level, and so is its frame-to-frame change. Moving the
far ripple's fade to 175-350 m changes neither. No shimmer or crawl was found, so the tuning stays.


Frame time on the rebased head, against main `db99f3a`: one interleaved round, headless, under the exclusive lock (load
about 5); three earlier rounds, before the rebase, agree:

| pose | `all` main / branch | `-water` main / branch | `-seatexture` (branch) | `none` main / branch | p95 `all` main / branch |
|---|---|---|---|---|---|
| gameplay03 | 8.04 / 8.23 | 7.86 / 7.99 | 8.17 | 5.27 / 5.48 | 13 / 13 |
| diorama02 | 19.94 / 18.72 | 19.41 / 18.94 | 18.73 | 12.66 / 12.35 | 22 / 21 |

The texture costs 0.06 ms at gameplay03 and nothing measurable at diorama02. gameplay03's `all` rises 0.19 ms, as does its
`none` (0.21 ms) with every feature off: build-to-build noise, not the water.

Boot (against `4abb64f`, before the rebase; the change adds no boot work): `headed.py boot --runs 3 --cpu 4`, 3 rounds
interleaved, 9 cold loads each, medians. Every phase but one equals main's (`biome texture baked` 433 / 436 ms, `kits built` 696 / 690, `terrain water` 20 / 20). `terrain heights set` is
1395 / 1471 ms: the wasm build's own tier-up draw (see "Issue #195": every build lands near 1390 or 1490 ms). That gives
`map_build_ms` 3063 / 3131 and `first_frame_ms` 10958 / 11010. The change adds no boot work beyond one shader global and
a toggle registration.
The goldens and the smoke run are unchanged: `GOLDEN OK 1950 71e9904d...`, `GOLDEN OK 1952 62f8ec61...` and
`SMOKE OK 5 turns 1948-06-19 d3fdd459...`. EditMode 747/747 on the merged head, with 0 `warning CS`.

Still different from the promo:
- promo 03's sea has boats and building shadows in its dark tail (out of scope here: no civilian boats; superseded by
  #536, "Sea and boats");
- promo 02's sea sparkles finely at the overview, where ours is held back from aliasing;
- the open sea's turquoise near the coast reads a little paler against the brighter, textured open sea at diorama02. The
  shallow-to-open gradient is unchanged (19 levels on both), and the turquoise thins slightly (g - r 43 to 40, 12-24 px
  from the coast).

### Sea and boats (branch `sea-boats`, refs #388)
The gap audit of 2026-10-13 (`.superpowers/audit-2026-10-13/`) found three gaps against promo 03:
- the open sea at gameplay03 4x busier than the promo's (edge density 0.048 against 0.012, in its open-sea crop at
  400 x 300);
- the overview's sea flat;
- 5 small wooden hulls with stub sails, where the promo has about 10 white-sailed boats and craft moored at Jaffa.

#402 had raised the far sea's texture on purpose, so the calm applies only far from the shore and only at the gameplay
zoom. There, #402's surf, shallows and near-coast chop are unchanged. Everything below is behind a new `seacalm` toggle,
and the calm and swells are also behind `seatexture`.
- **The zoom, not the pixel.** The calm and the swells fade out, and the overview's shelf fades in, with the water's
  distance from the camera, not its pixel size, so a 1440p canvas or a device pixel ratio of 2 sees the same sea as
  1080p. The calm and swells go between 110 and 190 km, and the shelf comes in between 150 and 220 km. The gameplay
  pose's water lies 50-90 km from the camera; the overview's (`CameraController.MaxDistance`, 250 km) lies 205-360 km
  away.
- **The calm** (`ScWaterShade`, `StatecraftMap.hlsl`).
  - It runs from `ScCalmNearM` (3.6 km offshore, past the shallows, whose edge reaches at most 3.5 km, and past the
    surf) to `ScCalmFarM` (8 km), on the open Mediterranean only, not the rift lakes.
  - Across it the ripple eases to 30%, the mottle's fine octave to 30% and the glint to 40%, and the whitecaps go. The
    coarse streaks grow 1.3x.
- **Broad swells:** a 14 km gradient-noise brightness variation, +-0.25, on the same far sea and at the same zooms as the
  calm, so the calmed water keeps promo 03's broad lighter and darker reaches. It is off at the overview, where its
  cells would read as blotches.
- **The overview's coastal shelf.** The sea within 12 km of the shore lightens towards a pale teal (0.36, 0.62, 0.66),
  by 60% times how far past the shallows' gradient the water lies, fading to nothing at 12 km. That is promo 02's
  lighter shelf. It tints the shallows' outer gradient a little at the overview and is not applied to the rift lakes.
- **The boats** (`Kits/SeaCraft.cs`, one more mesh under the kits' root, drawn with Kit.mat, so the "kits" toggle covers
  it):
  - 10 under way, 2.4 to 7.5 km out: 3 off Jaffa, 2 off Tel Aviv, 1 off Bat Yam, 2 off Haifa, 1 off Acre and 1 off Gaza.
    The Jaffa, Tel Aviv and Haifa points are the ones the kits build those ports at; the others are approximate shore
    points chosen for placement.
  - Two sailing rigs of the period: lateen-rigged feluccas and gaff cutters.
  - 7 moored: 2 in Jaffa's basin inside its breakwater, 2 at Tel Aviv's mole (in its lee or at anchor south of it,
    clear of the Jaffa label the gameplay pose shows north-west of it), and at Haifa 2 fishing boats by the quays and a small coastal steamer at anchor off the breakwater.
  - No motor launches or modern craft.
  - Each hull, plus a margin, lies in open sea and meets no breakwater or mole piece. The kits now record each piece as
    a "breakwater" in the census. `SeaCraftTests.NoHullEverMeetsABreakwaterOrMole` sweeps five landmark scales and 16
    draws each.
  - Sizes are symbolic, like the buildings: a felucca is 34 landmark metres (2 km at the default scale), a quarter of a
    hex across, about promo 03's share.
  - Every boat heads east or west, give or take 35 degrees, so the north-looking gameplay camera sees its sails side-on.
  - The sails are lit as cloth turned mostly to the sky (each face's normal tipped up by 0.7). Lit as flat plates, the
    face the camera saw was often the one turned from the sun, and the sails read grey.
  - `KitMesh.Triangle` gains an overload with an explicit normal for this. The old overload produces the same vertices
    as before.
- **Jaffa's old boats** are gone from `SettlementKits.Port`. Their random draws stay, so every later Jaffa house is drawn
  from the same stream. In pinned-clock renders at 25 km against main:
  - every pixel of the old town, the lighthouse and the clock tower is unchanged (0 of 371,200 differ);
  - so is Tel Aviv with its port;
  - the breakwater differs only on its 29 edge pixels where it meets the calmer sea; none inside it differs.

Measured: WebGL `?shot` of main `58d7b5d8` (with #525's frame) against the branch rebased on it, with the audit's
`measure.py` crops and #388's crop:

| | main | branch | promo |
|---|---|---|---|
| gameplay03 open sea: edge density (audit crop) | 0.048 | **0.020** | 0.012 |
| the same, sea only (x 0.02-0.22, clear of the boats) | 0.044 | **0.015** | |
| gameplay03 open sea: luma contrast | 0.054 | 0.055 | 0.052 |
| gameplay03 sea, #388 crop: detail / spread | 0.046 / 0.163 | 0.029 / 0.096 | 0.060 / 0.20 |
| gameplay03 sea, focus band: detail / spread | 0.055 / 0.185 | 0.037 / 0.104 | 0.075 / 0.32 |
| diorama02 sea pixels (inside the frame): luma std / p10-p90 | 0.100 / 0.279 | 0.099 / 0.279 | 0.086 / 0.11 (audit crop) |
| gameplay03 / diorama02 palette distance | 0.35 / 0.58 | 0.36 / 0.58 | |
| diorama02 warmth / saturation | 0.129 / 0.36 | 0.129 / 0.36 | 0.145 / 0.43 |

- #525's frame now crops diorama02's sea to the coastal strip. The audit's overview crop holds the frame, and the strip's
  own spread is already above promo 02's, so the "flat overview sea" gap is closed by the frame.
- The coastal shelf moves diorama02's sea pixels by 1.7 levels on average (18% of them by more than 2). In the editor
  test below, on the open water off the strip, it is 0.086 against main's 0.048.
- **The same at any resolution.** These are pinned-clock editor renders at 1920 x 1080 and at 3840 x 2160 (a device pixel
  ratio of 2, scaled back to 1080p to measure), comparing `seacalm` on and off:

  | pose | 1080p | 2160p |
  |---|---|---|
  | gameplay03, open-sea edge density, off / on | 0.051 / 0.016 | 0.061 / 0.018 |
  | gameplay03, share of sea pixels changed | 61% | 64% |
  | diorama02, share of sea pixels changed / mean change | 18.4% / 1.68 levels | 18.4% / 1.68 levels |
- Before the rebase (main `dc4b272`, no frame), the audit's overview crop measured contrast 0.030 on main and 0.031 on
  the branch.

- #388's crop detail falls under #402's 0.035 floor. That crop's promo figure counts the promo's boats and Jaffa, and
  promo 03's open water alone measures 0.012, so the floor contradicted the target. The new floors are in "Sea round 2"
  and #388. The near-shore texture is unchanged at the gameplay zoom.
- The shelf itself: `SeaTests.TheOverviewSeaIsLighterAlongTheCoast`. At diorama02, water 5 km out is 0.086 lighter in
  luma than water 25 km out; main measures 0.048, and the bound is 0.07.
- `SeaTests.TheOpenSeaCalmsAwayFromTheShore`, at gameplay03's distance with the water's clock pinned: the fine texture
  20 km out against 2.2 km out is 0.23 on the branch and 0.89 on main (bound 0.6).
- Three tests compare `seacalm` on and off, pixel for pixel. Each was run red against a mutated shader:

  | test | what it checks | mutation | red |
  |---|---|---|---|
  | `TheGameplayZoomKeepsTheNearShoreSea` | gameplay03, 0.3-3.4 km out on nine rows: no change | `ScCalmNearM` 0 | 12/255 |
  | | | the shelf at the gameplay zoom | 19/255 |
  | `TheOverviewGetsNoCalmOrSwells` | diorama02, 15-40 km out: no change | no zoom fade | 13/255 |
  | `TheShelfShowsAtTheOverview` | diorama02, 4-8 km out: changes by more than 3/255 (15/255 now) | | |
- `SeaCraftTests` checks the boats:
  - the count and the kinds;
  - every hull and its margin in open sea, not a rift lake, with no two boats overlapping;
  - the size against the hex;
  - no hull meeting a breakwater or mole piece, over 1,337 boats from 5 scales x 16 draws;
  - the seeding, and that the boats are drawn under the kits and toggled with them.
- **The census's cost.** The kits now run with the census on, to read the ports and their works. The game towns' kits,
  built 5 times each way, take a median of 11 ms without it and 12 ms with it.

Frame time: `headed.py profile`, under the exclusive lock. The final round is main `58d7b5d8` first, then the branch, at a
load of 4-6:

| pose | `all` main / branch | `-seacalm` (branch) | `-water` main / branch | `none` main / branch | p95 `all` |
|---|---|---|---|---|---|
| gameplay03 | 3.35 / 3.62 | 3.47 | 3.51 / 3.16 | 2.64 / 2.50 | 5 / 5 |
| diorama02 | 12.69 / 12.56 | 12.36 | 12.73 / 12.41 | 8.37 / 8.11 | 14 / 14 |

- `seacalm` prices the calm, the swells and the shelf at about 0.15 ms at gameplay03 and 0.2 ms at diorama02.
- `all` rises 0.27 ms at gameplay03, against spreads of 0.14 and 0.33 ms, and falls at diorama02.
- p95 is unchanged.
- Two earlier rounds, before the zoom-keyed fades and on a busier machine (load 5-27), gave gameplay03 `all` +0.35 ms
  main first (spread 1.1-1.3 ms) and +0.97 ms branch first. In the branch-first round `-kits` and `-seatexture` rose
  by as much as `all`.

Boot (`headed.py boot --cpu 4 --runs 3`, one round each, before the rebase): map_build median 3389 ms on main and 3106
on the branch, and `kits built` 828 against 785 ms in the last run. The branch is not slower: the boats' placement and
one more mesh are not measurable against the run-to-run spread.

## Promo track: land use (issue #33, branch promo-land)
The flat mustard/olive ground becomes the promo's patchwork. `tools/land_bake.py` (no network, about 20 s, deterministic:
`test_the_committed_textures_are_the_bake` checks the committed PNGs against a fresh bake) decides where from real data
and paints what procedurally, baked:
- where (158 x 211 m grid): fields on gentle slopes (< ~7%) with enough rain for dry farming (CHELSA bio12,
  station-corrected as `hexgrid.py`: smoothstep 180-380 mm; the irrigated Jordan valley floor at least 0.7); the citrus
  belt on the flat coastal plain and valley floor below 150 m near the 1948 towns (the hex-wise biome inputs softened over
  1.5 km, so nothing steps at hex edges); garrigue, terraces and scrub where the rain is 250-600 mm, the ground unfarmed
  and not the shader's rock;
- what (`landuse.png`, 2048 x 4096 RGBA, about 79 x 105 m): strip-field parcels in rotated 5 km blocks (green, ochre
  stubble, ploughed, fallow, grass, groves), soft grassy rims, hedges, some block edges as farm tracks; on the wet hills patches
  of stony soil and grass, terraces along the real contours every 40 m of rise, scrub; 2 x 2 samples per texel; sRGB
  colour (the sampler decodes it) spread into the empty texels so mips and BC3 blocks never blend with black; weight in
  alpha;
- no roads between the towns: the first rounds joined each town to its three nearest with seeded meandering tracks
  (`tracks.png`); a controller ruling removed them, as invented routes would draw a second road beside the real 1948 roads
  and railways the roads track traces from the 1946 Survey of Palestine map (`tools/routes_1948.py`). A finer track
  network, if wanted, is traced there, not generated.
The shader lays the land between `ScGroundBase` and `ScGroundShade` (relief, AO and breakup shade the fields), up to the
waterline: promo-sea's beach band (`ScShore`, drawn after) covers it where there is a beach, and fields reach the water
where there is none (the Sea of Galilee, cliffs). The `land` visual toggle lerps it away: a visual switch, not a price
(the sample still runs).

**Why baked, not procedural in the shader.** The first cut drew the same pattern in the shader (about 150 instructions).
WebGL headless, same night: base 2.72 / 9.76 ms (gameplay03 / diorama02 `all`), procedural 13.94 / 15.44; with a
per-pixel `[branch]` and one shared noise 10.37 / 13.60; branch-free 9.58 / 13.58; flattened 10.91 / 15.01. The cost stayed
when no pixel took the branch (`-land` 8.11 at gameplay03), and sampling the textures alone cost nothing (2.69 ms):
most likely register pressure / occupancy from the shader's size (the GPU reserves registers for the worst path, and
ANGLE may flatten the branch), not its arithmetic. Single builds differ by a few tenths of a millisecond, so the
bisecting steps (fields alone +4.3 ms) are indicative only. The decision to bake holds either way.

**Cost, measured base vs branch (separate builds; the toggle cannot price it).**
- Night of 2026-10-11, idle machine: main base 2.72 / 9.76 ms -> promo-land (`4b3c307`, before the review fixes) 2.68 / 9.93
  ms (`PROFILE summary visual=all pose=gameplay03 runs=3 median_avg_ms=2.68 spread_ms=0.09 median_p95_ms=5.00`,
  `... pose=diorama02 ... median_avg_ms=9.93 spread_ms=0.02 median_p95_ms=11.00`): within the noise between builds.
- Review round 1 (`24b540f` on promo-sea), A/B against the same branch with the land call cut from the shader
  (`Builds/Web-noland`), profiled back to back while the machine was busy (another browser on the GPU: every number about
  5 ms above the night's): gameplay03 `all` 8.20 -> 9.09 ms (+0.89, spreads 0.09 / 0.07), `none` 7.10 -> 7.73; diorama02
  `all` 14.07 -> 13.70 (-0.37, inside the spreads 0.49 / 0.25). p95 10 / 16 ms.
- Idle re-measure (re-review R1): **not measured.** Waiting for a 1-minute load average under 3, checked every 5 minutes,
  2026-10-11 09:02-10:47 and 14:32-15:32: it went under 3 once (2.64 at 09:22), but the A/B taken then was still inflated
  (`none` 7.36 ms at gameplay03 against the night's 2.33) and the branch half was disturbed (spreads up to 10 ms), so it
  is not used; otherwise the load stayed between 3.9 and 51. The busy-machine A/B above is the only cost figure; re-measure
  on an idle machine before the tier numbers are re-ratified.

**Size and memory.** `landuse.png` is 10.7 MB in the build report (2048 x 4096 BC3 with mips: 10.7 MiB of GPU memory).
The WebGL `.data` is LZ4-compressed, so the download grows less: `Web-disabled.data` 21.53 MB (promo-sea) -> 24.40 MB
(+2.86 MB, then with the since-removed tracks texture's 0.02 MB). Halving the texture (the platforms' 2048 cap gives 1024 x 2048) blurred the
hedges, rims and rows (gameplay03 local gradient on the plain 0.0220 -> 0.0174, -21%), so it stays 4096 rows; BC1 cannot
hold the weight, so BC3 stays.

**Platforms.** The default web build targets desktop browsers (S3TC/BC, as the map's other textures already need). The land
texture's WebGL override pins only the size (4096) and leaves the format automatic, so the build's texture subtarget picks
it: DXT5 by default; `Build.Web -textureSubtarget astc` (or `etc2`) makes a build for browsers without S3TC (iOS Safari,
most Android Chrome), where the land is ASTC 6x6, 4.8 MB (`unity-build-astc.log`), and golden + smoke=5 pass. A desktop
build opened on such a browser would decompress the land to RGBA32 on the CPU: 42.7 MiB. WebGL2 guarantees only 2048-texel
textures; virtually every GPU offers 4096.

Tests, review round 1: EditMode 465 passed (`LandUseTests`: import settings for every platform, the subtarget argument,
`-extraPose` validation, the land follows slope/rain/towns, gameplay detail; `TheTerrainShaderCompilesForWebGL2`); tools
pytest 194 passed; `dotnet test` 628; golden and smoke unchanged on `Web-disabled` and `Web-astc`.

Colour (WebGL `?shot`, checkpoint `promo-land-r5` against `promo-sea-r2`): gameplay03 saturation 0.4792 -> 0.4681
(promo 0.38), warmth 0.1018 -> 0.0921 (promo 0.064), contrast 0.1738 -> 0.1730, palette distance 0.5971 -> 0.5148;
diorama02 saturation 0.4183 -> 0.4164 (bar <= 0.48), warmth 0.1171 -> 0.1154 (bar 0.087-0.145), palette distance
0.5869 -> 0.5868. Open item 3: the land lowers gameplay03 saturation by 0.011. Open item 5: the olive plain / gold hills
seam is now fields against garrigue, a softer step (owner to judge).
Licence: CC BY-SA 4.0, decided by the owner on 2026-10-14 (D13 in `docs/oss/DECISIONS.md`).
Still different from the promo: soft at close zoom (rows, hedges and bushes read at the gameplay zoom, blur at 15-20 km
and closer); terraces read faintly; the orchard ground and promo-art's groves follow separate rules; the promo's ground is warmer and browner under
its grade.

### Land round 2 (branch `land-round2`, refs #33)
Round 1's land read as yellow-olive. It also lost most of its field lines, because the bake's 2 x 2 supersampling averages
an 18 m hedge or a 30 m track away inside a 79 x 105 m texel. Round 2 changes only the colours and the bake; the shader,
the texture's size and format, the trees and the sea are untouched.
- **Ground colour.** The land's colours in `tools/land_bake.py` (`SRGB`) and the plain, valley, lowland, hill and mountain
  colours in `MapPalette.Ground` move toward promo 03's ground, (177, 144, 114) in the ground crop below: less green, more
  blue, at its hue (28.6 degrees). The hill colours (soil, hillgrass, stone, and MapPalette's hills and mountains) are an
  orange brown at 22-25 degrees, saturation 0.24-0.34. With round 2's first values (soil at 8 degrees and saturation 0.26;
  stone, dust and fallow at about 18) the Judean hills read rose (review of #392): a red hue at low saturation. At 28.6
  degrees they rendered at 34, yellower than promo 03's hills (26) and the video's (28), and diorama02 lost promo 02's
  browns, about (208, 144, 112) and (176, 112, 80). The brightness (luma) of each colour is kept. The desert is unchanged.
- **Fields.** Hedges and tree lines are `HEDGE_HALF_M` 75 m either side of a parcel edge. Farm tracks are `TRACK_HALF_M`
  70 m either side of a block edge. Each line is chosen once per edge, so the parcels on both sides draw it. The crops are
  further apart in luma: a darker green, darker ploughland, paler stubble and fallow.
  `test_field_lines_are_wide_enough_for_the_texture` holds both lines to at least 1.5 texels across. It fails at round 1's
  widths, measuring 15 m and 28 m.
- **Hills.** Round bushes about 200 m across, at most one per 300 m cell. A 1.5 km noise sets the share of cells with one
  (20-80%), so they clump. Each sample takes its dot from the 2 x 2 cells round it, so no dot is cut. Each bush has a
  softer shadow offset east-south-east, away from the sun (`MapLighting.SunAzimuth` 285 degrees). The garrigue's weight
  rises from 0.7 to 0.9. Terraces cover more of the slopes, with stronger stone lines.
- **Bake.** About 30 s. Two runs give byte-identical PNGs. `test_the_committed_texture_is_the_bake` passes. The land weight
  mean is 0.192 (round 1: 0.171), because the garrigue's weight rose.

Measured: WebGL `?shot` at main `4abb64f`, then the branch on it, art-log rows `lc-r2-main` and `land-round2-fix5`. The
ground crop is gameplay03's land (x 0.36-0.88, y 0.30-0.82), pixels at luma >= 0.4. The towns' tan walls and their ground
tint are in it and unchanged, so it moves less than the ground does. The Judean hills are `-extraPose
judea30:35.10:31.85:30000:10` (editor, web tier): the median hue of their pixels at luma >= 0.25.

| | main | round 2 | promo |
|---|---|---|---|
| gameplay03 saturation / contrast / warmth | 0.447 / 0.181 / 0.070 | 0.429 / 0.178 / 0.062 | 0.38 / 0.203 / 0.064 |
| gameplay03 palette distance | 0.435 | **0.378** | - |
| diorama02 saturation / contrast / warmth | 0.414 / 0.184 / 0.113 | 0.406 / 0.183 / 0.109 | 0.43 / 0.200 / 0.145 |
| diorama02 palette distance | 0.5633 | 0.5643 | - |
| gameplay03 ground crop (sRGB) | (166, 148, 99) | (169, 143, 106) | (177, 144, 114) |
| judea30 hill hue (degrees) | 39.8 | 30.5 | 26 (promo 03), 28 (video 0:21) |

The bars hold: diorama02 warmth 0.109 is inside [0.087, 0.145], and its saturation is under 0.48. diorama02's palette
distance is 0.001 above main's; promo 02's browns (about (208, 144, 112)) are what the hills' hue trades against.
Every ground channel is within 8 levels of the promo's (it was within 15).

Frame time: headless, main and the branch back to back under one exclusive lock, 3 rounds, rotated. The load average was
about 6 from other sessions, so the absolute times are inflated. The table gives medians over the rounds, in ms:

| pose | `all` main / branch | `-land` | `none` |
|---|---|---|---|
| gameplay03 | 8.13 / 8.05 | 8.05 / 7.81 | 5.09 / 4.97 |
| diorama02 | 19.49 / 19.71 | 19.08 / 19.45 | 12.36 / 12.72 |

Every difference is within about 0.4 ms, which is also how far `none` moves between the two builds. The review fix (the hill hues) changes only colours: the same texture size and format, no shader, so these numbers and the boot below stand. The shader and the
texture's size are the same. diorama02's p95 is 21-22 ms on main and on the branch alike, under that load.

Boot: `headed.py boot --runs 3 --cpu 4`, 3 rounds interleaved, 9 cold loads each, medians:
- `map_build_ms`: main 3032, branch 3039;
- `first_frame_ms`: main 10842, branch 10819;
- `biome texture baked` (MapPalette's colours): main 437, branch 431;
- `downloaded 53 files`: main 1048, branch 1040.

Both builds' map build is over #321's budget at this load; that is main's, not this change's.
The goldens and the smoke run are unchanged: `GOLDEN OK 1950 71e9904d...`, `GOLDEN OK 1952 62f8ec61...` and
`SMOKE OK 5 turns 1948-06-19 d3fdd459...`. The tree count is unchanged (7,814).

Still different from the promo: the trees and bushes, so far only painted in the bake (promo 03's land is 27% dark trees
and shadow; ours is 10%). The grade's shadows are cool, where the promo's are warm brown (#391).

## Open items for the art track
Each item below was ruled to the art track (controller, rank 10). None blocks landing the branch.

1. **Border skirt look.** The skirt (the slab around the map) reads as a different material from the terrain next to it.
   The fix is the table or frame and the skirt in the terrain's own look (promo 02), not a colour patch. A patch,
   `BorderLandColour` (0.76, 0.66, 0.47), was measured and rejected: the south-west slab rendered (0.69, 0.58, 0.37) against
   the terrain's (0.82, 0.69, 0.43), so the step shrank, but the skirt is 17% of diorama02 and its saturation rose 0.34 ->
   0.47. That took the frame to saturation 0.488 against the 0.48 bar (warmth 0.129). Do not move the bar to exclude the
   skirt. The earlier line saying a lower-chroma colour is the next candidate is withdrawn.
   **Done in #36 (frame round the map).** The skirt is gone: the board ends in a bevel (cut earth where the edge stands
   above the frame, the frame's lip where the sea and the low coast meet it), a walnut frame and a dark wooden table
   (`MapBuilder.BuildFrame`, `MapFrame.shader`). The frame and table read the haze at sea level or higher (at world y 0 the
   rift boost made it 1.6x), and the table takes 0.4 of that; the full haze lifted it to a flat khaki. WebGL diorama02
   against main (c2774ecc, with #502's olives): saturation 0.405 -> 0.359, contrast 0.173 -> 0.222 (promo 0.200), warmth
   0.118 -> 0.129 (bar 0.087-0.145). gameplay03 and both hero shots: no land pixel moves by more than 3/255; 1 sea pixel
   of gameplay03 moves by up to 13/255 (the water's own clock, unpinned in the player). The sea no longer runs past the map's west edge, so the sea-seam tests went with the skirt.
2. **Settlement markers float.** At the web shadow texel (about 100 m at 60 km with a 2048 map) the markers (symbolic
   scale, 300-700 m) show long detached shadows, so they look as if they hover. Set the sun's shadow bias (depth and normal
   bias) at kit scale: offsets of about 100-150 m at that texel. Do not just turn marker shadows off.
3. **Saturation of gameplay03 and warmth of diorama02.** gameplay03 saturation is +0.18 over promo 03, mostly the sea (0.83
   against 0.51) and the gold `hills` biome. Sea part done in promo-sea (frame 0.57 -> 0.48, sea 0.82 -> 0.49); the
   `hills` biome remains. Measure the `SeaTests` margins before any sea change. diorama02 warmth
   (0.1038) sits near its 0.087 floor, so any sea desaturation must re-check it.
4. **Tilt-shift bottom band.** The bottom blurred band measures 17-22% of the frame against about 15% in the promo. It
   awaits the owner's verdict on the offer `BottomHalfWidth` 0.16 / `BottomFeather` 0.19.
5. **Olive plain against gold hills.** The seam between the olive `coastal_plain`/`valley` palette and the gold `hills`
   biome is for the owner to judge.
6. **Overview shadow overrun.** The shadow pass overruns the budget by 0.1-0.4 ms at the overview pose; the levers are in
   "Tier decisions".
7. **Sea brightness spread (promo-sea re-review N3).** The sea's luma p90 - p10 is 0.12 at gameplay03 against promo 03's
   0.39, and 0.08 at diorama02 against promo 02's 0.15. Its saturation p10/p90 is narrower too (0.43/0.55 against
   0.29/0.58). The means match ("promo-sea", Measured). The causes:
   - promo 03's dark tail is boat hulls and building shadows on the water. Boats are out of scope: navy ships are units,
     and there are no civilian boats (owner). Superseded by #536 ("Sea and boats").
   - Its bright tail is foam lace and glints. The glint is held at 0.06 by the seam test (Task 7), and the surf is a
     smooth band, not lace.
   - diorama02's sea has a near-to-far gradient that ours keeps flat.

### Rulings digest
One line each: ruling - reason.
- White balance Temperature 5 (plan: 9) - the merged diorama warmth must land below the promo's 0.145 (measured 0.127-0.133 at 9).
- `AmbientProbe` removed - dead code: Trilight ambient replaces any assigned probe, so it never rendered.
- Sea glint 0.06 (was 0.25) - at 0.25 the lobe lit a warm patch over 6 km of open sea at yaw 180 and broke the seam test.
- Skirt hold smoothing (running mean, 8 km beyond the brief) - the skirt follows the coast without steps.
- Grid opacities land 0.35 / water 0.10, plus a 30 -> 10 px footprint fade and fade to 0.35 - the sea lattice was about 5x
  the promo's weight; land lines were invisible at 0.25 once graded.
- Grade `Saturation` 0 (plan: 16) - the palette now carries the colour; graded/ungraded saturation x1.042 (bar 0.98-1.12).
- Selection ring ivory (promo ring g/r 0.915, b/r 0.785) - matches the promo and passes the colour-blind separation tests.
- Olive `coastal_plain`/`valley` palette - gameplay03 was one flat mustard; warmth 0.108 -> 0.077, saturation 0.582 -> 0.565.
- Web shadow map stays 2048 - 1024 measured equal cost, so shrinking would lose quality for nothing.
- Web render scale 1.0 - measured WebGL tier decision (see "Tier decisions").
- Bloom, SSAO and the depth/opaque copies off - bloom changed at most 2/255 of any pixel; see "Tier decisions" for the costs.
- NOT taken: dropping the bake's R channel - zero memory gain (BC1 either way) and it churns the assets; R stays as the AO reference.
- NOT taken: deduplicating the `OpenMap` helper across `SeaTests` and `TerrainShaderTests` - test-only duplication; refactor risk with no behaviour gain.

## Art spike: procedural settlement kits and trees (branch `promo-art`)
Question: can code-generated settlement and vegetation art at Civ-style symbolic scale bring gameplay03 close to promo 03?
Code: `unity/Assets/Scripts/Map/Kits/` (built at load, seeded per settlement id by `KitRandom`, no sim state touched),
`Assets/Shaders/Kit.shader` (`Kit.mat` buildings, `Tree.mat` instanced trees), `KitTests`.
- **Archetypes** (`SettlementKits`): white city (Tel Aviv, Haifa: 2-9 storey flat-roof blocks, ribbon windows and
  balcony lines in the shader, L-shaped wings, roof rooms), old town (Jaffa, Acre, Gaza, Jerusalem: packed stone houses,
  domes; Jaffa's clock tower, belfry, mosque, breakwater, lighthouse and boats), Arab town (tan cubes on slopes, a mosque),
  moshava/kibbutz/moshav (red gable roofs, yards, a water tower; Nahalal as a ring). One merged mesh per settlement; all
  towns share one lot list, so a smaller neighbour never overlaps a bigger town's buildings. The `kits` toggle off shows
  the marker boxes again.
- **Footprint** = (1945 population / 60,000) hexes, floored at 0.08 hex. The populations live in
  `content/map/settlement_population_1945.yaml` (Village Statistics, 1945, read from the Wikipedia table on 2026-10-11:
  owner-verify until checked against a scan; Beersheba's row there is garbled, 5,570 is its likely reading), read through
  `MapInputs.PopulationYaml`. The sim never reads the file: `SaveGameTests.TheKitPopulationFileIsOutsideTheContentHash`, and
  the player's `BOOT OK content 96035735...` is unchanged. Towns share seams (distance over radius), stay on land, and tint
  their ground in the baked biome texture.
- **Trees** (`Vegetation`, `VegetationLayer`): citrus groves in 2x2 patches (every tree of a patch checked for water and
  towns, else one tree) round the coastal-plain towns, olives over the hills (and lone ones on the plain), palms by the
  coastal towns, cypress windbreaks; `Graphics.RenderMeshInstanced` per 30 km tile from `beginCameraRendering`, thinned as
  (110 km / distance)^2 past 110 km. The WebGL cost is per instance, not per triangle (at 25x a 30% triangle cut changed
  nothing; the 2x2 patches took the instances from 51,000 to 21,165 and the gameplay03 cost from 2.9 to 1.3 ms).
- **Scale (owner ruling 2026-10-11, superseding 10-25x):** buildings and trees match promo 03's measured scale per class:
  city blocks about 60x real size, houses and trees about 100x (`KitScale`; landmarks take the city-block scale). Counts
  were tuned at 25x and fall with the square of the scale. `?kitscale=`, `?housescale=`, `?treescale=` (clamped to 5-200)
  override them in the player; `MapCapture.Kits` takes `-kitScale`, `-houseScale`, `-treeScale`.
- **#37:** the kits use the sun's own URP shadow bias, unchanged (review I3: a kit-only bias and receiver offset were
  removed, as the probe could not show a need for them). `KitTests.AKitShadowStartsAtItsWalls` walks out from a lone box's
  shadow-side wall from 1 m in 1 m steps, on both tiers: the first shadowed sample is 3 m out at 12 km (pixel 7 m), 10 m at
  60 km (pixel 35 m) and 1 m at 250 km, the same as with the old marker material, and the sunlit roof darkens at most 1.9%
  with shadows on (no acne). No bias gap reproduces, so the floating look of the old markers was not a bias gap.
- **Grid readability:** `KitTests.TheGridStaysReadableThroughTownsAndGroves`: at gameplay03, 76 of the 90 land edge
  midpoints whose line shows on bare ground still show it with kits and trees on (84%; the floor is 75%).
- **Measured at the 60x/100x defaults** (`d4e7275`, headless WebGL, `machine_lock.py --exclusive --wait-unity`, load 4-9,
  other sessions running; "none" means every visual feature off, not only the kits and trees):
  gameplay03 all 2.34 ms (p95 3), -trees 2.57, -kits 2.34, none 2.34; diorama02 all 11.22 (p95 12), -trees 10.41, -kits
  11.35, none 7.29. Trees -0.23 / 0.81 ms and kits 0.00 / -0.13 ms (gameplay03 sits in the ~1 ms clock's bimodal
  2.0 / 2.4 ms noise, so its negative costs are noise): every feature is within budget, all - none 0.00 / 3.93 ms, p95
  3 / 12 ms. The `main` build in the same session: gameplay03 all 2.78, diorama02 all 10.12 (so +1.10 ms at diorama02).
  The diorama02 shadow overrun is unchanged (2.95 ms against `main`'s 2.91). Headed DPR 1: all 2.46 / 10.95, trees
  -0.24 / 0.75. Headed DPR 2: all p95 6 / 20 ms (floor 33), trees 0.35 / 0.89. Boot (`headed.py boot --runs 3`, `main`
  then this build): cpu 1 map_build 491 -> 508, first_frame 3631 -> 3610; cpu 4 1874 -> 2035, 6795 -> 6991 (within
  max(300 ms, 50%)). The player logs `KITS buildings=306 vertices=12374 triangles=8556 trees=8147 build_ms=20 plant_ms=19`.
- **After merging `main` with promo-sea and promo-units** (`689880d`, headless, load 18.7 at the start and 4.4 at the end,
  so absolute times are noisy): gameplay03 all 9.57 (p95 11), -trees 8.75, -kits 9.45, none 7.47; diorama02 all 14.99
  (p95 16), -trees 14.28, -kits 15.40, none 10.92. Trees 0.82 / 0.71 ms and kits 0.12 / -0.41 ms stay within budget, as
  do all - none (2.10 / 4.07 ms) and p95 (11 / 16 ms). The rise in "none" (2.34 -> 7.47 ms at gameplay03) is not the kits
  (both "none" runs draw no kits or trees); it should be re-measured on a quiet machine against `main`.
- **Build size** (WebGL, `main` -> this branch): uncompressed +318,885 bytes (+0.44%: wasm +286,873, data +28,978,
  framework.js +3,034); Brotli (quality 11, as shipped) +33,407 bytes (+0.17%: wasm +13,092, data +20,091). No texture or
  model files: everything is generated at load.
- **Authored meshes later:** trees swap as is (`Vegetation.Build(kind)` returns a mesh in real metres). Buildings will need
  `Emit` to return placements (archetype, variant, position, yaw, scale) drawn through the same tile instancing and a
  textured, instanced variant of `Kit.shader`; that refactor waits for the first authored mesh.

## The 1948 localities and Bedouin camps (branch `promo-villages`)
Owner rulings (2026-10-11): show the places of 1,000 or more people first, by their 1948 names, as town and village kits
beside the 25 game settlements, and a light Bedouin encampment scatter sized by the Village Statistics tribal totals,
inventing no names or points. Owner rulings (2026-10-12, after the staff review): the "Today:" tooltip only where a living
place carries on; Lydda and Ramle for the game settlements `lod` and `ramla`; Nesher drawn as a mixed locality.
- **Data:** `content/map/localities_1948.yaml` (status draft; its header lists every field and rule). It has 271 places
  and 8 camp areas.
  - The places are the 293 of 1,000+ in VS 1945, less the 20 game settlements among them, the Jahalin tribe row (counted
    with the camps) and Huj (810 plus 230 Jews of kibbutz Dorot: the village alone is under 1,000).
  - The camp areas total 64,670 people: Beersheba 47,980, Jerusalem 7,070 (its "Tribal Units" row only), Ramle 3,780,
    Jaffa 2,270, Hebron 2,000, Safad 820, Gaza 530 and Nablus 220. Tulkarm's "Tribal Units" row has no figure.
  - The file is presentation only, outside the content hash (`SaveGameTests.TheLocalitiesFileIsOutsideTheContentHash`),
    `tools/tests/test_localities.py` checks it. The player reads a baked runtime form, `localities_1948.tsv` (see
    "Boot" below), through `MapInputs.LocalitiesTsv`.
  - Left for later rounds: places under 1,000, the 70 VS rows with no point, and the 29 foundings dated only "1948".
- **Today's name:** a `today` entry (`{name, name_he, source}`) exists only for a place that carries on. There are 161 of
  them.
  - None is given where Wikidata classes the place's item as a depopulated village or a former settlement. The test's
    QID list is `tools/tests/data/localities_no_today_qids.txt`; the content records no such class.
  - There are two exceptions:
    - Majdal, whose item is today's Ashkelon (the owner's example).
    - Sha'b → Sha'ab (שעב). Its item, Q2655669, is also the living Arab local council. This applies the owner's rule
      (2026-10-12 re-review).
  - Walaja, Samakh and Lubiya are classed both ways too, but no living place carries them on, so they have no today
    name.
  - Jewish places merged into a later municipality take the municipality: Magdiel and Ramatayim become Hod HaSharon, and
    Kfar Ganim becomes Petah Tikva.
- **Kits:**
  - Villages and Arab towns take the Arab-town archetype (`village` type: a smaller mosque).
  - Jewish places take the red-roofed archetype: kibbutz, moshav ring or moshava, and the 14 Jewish towns too, since the
    repo maps them to `town` and several began as moshavot.
  - Nesher (VS: 1,430 Muslims, 1,400 Jews) is a mixed locality: Arab houses on its west side and Jewish houses on its
    east, with a mosque and a water tower. Its tooltip says "Mixed locality".
  - No Templer colony reaches 1,000. When they come, the moshava archetype is the closest.
  - The landmark follows VS's religion columns: 11 churches (Shafa 'Amr by its Urban row) and none in the 5 Druze
    villages.
  - Every place shares the one lot list. Every game settlement's landmark claims its spot first, then every locality's,
    then all houses, largest place first. The 25 game settlements keep their cells and their landmarks (tested); a
    village fills in around them.
  - Localities and camps are merged into one mesh per 30 km tile.
- **Camps:** one per 1,000 of an area's total, rounded half up with no floor, so Nablus draws none: 65 camps, 175 tents (after #250).
  - The areas are the sub-districts as `content/map/subdistrict_hexes_1948.yaml` draws them, read at load
    (`Localities.FillHexes`).
  - Each camp is placed at a seeded spot, dry, gentle, outside every town and off every route.
  - It has two to four brown goat-hair tents in a loose line.
- **Labels:** smaller pills (`.map-label-locality`), only nearer than 40 km (`MapLabelsPresenter.LocalityDistanceM`), placed
  after every game settlement with the same declutter rules.
  - Hovering one shows its tooltip, in the UI's language (`ui.lang`).
  - Touch and keyboard players have no route to the tooltip yet (#245).
- **Units in play (review I5, merged with #250):** the unit tokens keep off the villages and camps as they keep off the
  game towns.
  - The units are placed before the kits are built, since their clearings feed the kit build. So
    `MapScene.SettlementFootprints`, which the units place against, holds the game settlements' and the localities' discs
    (`SettlementKits.FootprintRadiusM`), the same discs the kit build claims.
  - A token standing in a town or village gets a clearing. The kits take the clearings as corridors, so village houses,
    tents and trees keep off them, as they do on main.
  - The camps are placed after the units. They keep off every token outside the towns (`KitGround.Tokens`): a camp's
    centre stays `SettlementKits.CampReach` house-scale metres plus the token's ring away from it.
  - `UnitsInPlayTests.NoTokenStandsInAVillageOrACamp` builds the map with the kits on. It checks that every token outside
    a town has its ring clear of every `MapScene.KitFootprints` entry (towns, localities, camps), and that every token in
    one has its clearing.
- **Grid readability** with the villages: 79% (70 of 89 edge midpoints) at gameplay03, against 84% before. The floor stays at 75%.
  - Merging #250 made the units keep off the villages and the camps keep off the units. The camps and village houses
    moved, and the share fell to 72%, under the floor.
  - The fix is the plan from review M10. A 1948 locality's houses and a camp's tents keep off the edge midpoints of the
    hex they stand in (`KitGround.OnGridMark`). This costs a few locality buildings (1,332 after #251) and a few tents (175).
  - The game settlements are unchanged.
  - The next data round (places under 1,000) gets the same rule. Don't lower the floor.
- **Bands** stay legible through the villages: `BorderTests.TheBandsStillReadThroughTheVillages` reads the bands with the
  kits and trees on (18 of 18 walks at gameplay03 and Tulkarm). The other border tests measure the bands with the kits
  and trees off.
- **Start of game only.** The file records no later fate, depopulation date or post-May-1948 status, and the test
  `test_start_of_game_only_no_later_fate` keeps it so. A dated map (#61) needs the owner's framing decision first: whether
  the settlement layer is ever dated after the start, and if so how places that ceased to exist are shown. The research
  note `.superpowers/handover-2026-10-11/promo/localities-1948.md` ("Sensitivity note") lists the options.
- **Measured.**
  - Frame time (headless WebGL `vprofile`, exclusive lock, aa1a40a against main c2089b4, 2026-10-12):

    | | gameplay03 | diorama02 |
    |---|---|---|
    | all | 13.83 ms (p95 15); main 14.33 (16) | 18.01 ms (p95 20); main 17.53 (20) |
    | all − none (budget 8 ms) | 3.33 ms; main 3.91 | 5.29 ms; main 4.96 |
    | trees (budget 2.5 ms) | 0.48 ms; main 0.52 | 1.26 ms; main 0.90 |
    | labels | 0.02 ms | 0.06 ms |

    The kits' cost is within the noise. The 20 ms p95 at diorama02 is main's too.
  - Boot (`headed.py boot --runs 3`, main c2089b4 → this branch, 2026-10-12; budgets at cpu 4: 2720 / 10445 ms):

    | | map_build | first_frame |
    |---|---|---|
    | cpu 1 | 601 → 640 ms | 3860 → 3884 ms |
    | cpu 4 | 2506 → 2572 ms (148 ms under budget) | 8141 → 8006 ms |

    Before this review round, cpu 4 map_build was 2712 against 2720.

    **Re-measured after merging #248, #250 and #251 (2026-10-12, cpu 4, three runs, exclusive lock):**

    | build | map_build | first_frame |
    |---|---|---|
    | main f974c9f | 2581 ms | 8114 ms |
    | this branch | 2768 ms | 8508 ms |
    | this branch, light read of the sub-district file | 2745 ms (25 over 2720) | 8489 ms |
    | the same after merging #253, #255, #261 and #267 (4b9283a) | 2850 ms (130 over) | 8904 ms |

    - The `localities parsed` phase included a second, full parse of `subdistrict_hexes_1948.yaml` (the camps' areas,
      review I4), done with the reflection deserializer. GameRoot parses that file in full again later in the boot.
    - The map build now reads only the regions' keys and the hexes' regions, with the event reader
      (`Localities.FillHexes(doc, string, ...)`); KitTests checks that it gives the same areas. The phase went from 221
      to 172 ms.
    - Main itself grew 75 ms since c2089b4 (#250's unit placement, #251's routes). The branch adds about 165 ms over
      main at cpu 4, nearly all of it the localities file's YAML scan.
    - **Resolved by a baked runtime file (coordinator ruling 2026-10-12: fix the root cause, not defer the kit build or
      re-baseline the budget):**
      - `localities_1948.yaml` stays the sourced, hand-edited source of truth.
      - `tools/localities_bake.py` bakes it into `localities_1948.tsv`: 26k characters against 176k, one tab-separated
        row per place or camp.
      - The player reads the file with `Localities.ParseRuntime` (a split, no YAML), with the same checks as the YAML
        reader. Errors name `file:line`.
      - The manifest ships the .tsv and no longer the YAML.
      - pytest fails a stale bake; `KitTests.TheRuntimeFileYieldsTheSameRowsAsTheYaml` compares every row.
      - The `localities parsed` phase went from 172 ms to 53 ms. Main and the branch now boot within noise of each other.

      Measured at cpu 4, three runs each, interleaved, exclusive lock, 2026-10-12:

      | build | map_build | first_frame |
      |---|---|---|
      | main 316a042, round 1 | 2573 ms | 8113 ms |
      | this branch f715717, round 1 | 2588 ms | 8229 ms |
      | main 316a042, round 2 | 2610 ms | 8234 ms |
      | this branch f715717, round 2 | 2553 ms | 8131 ms |
      | this branch after merging main (05390da), final checks | 2658 ms | 8367 ms |

      The branch is 130 to 170 ms under the 2720 ms budget: real headroom for the towns' round 2. That round should bake
      the same way.
  - The growth is the localities file's parse: the `localities parsed` boot phase is 50 ms at cpu 1 and 220 ms at cpu 4.
    It is not the ground tint, which the player logs as `KITS ground_tint_ms=0.8`. My earlier attribution to the tint was
    checked with that timer and was wrong; the earlier "terrain heights set" growth was the parse that ran before it.
  - Two changes cut the parse:
    - An event-based reader (`Localities.Read`) replaces YamlDotNet's reflection deserializer.
    - Each row's sources are written as a plain URL list, so the file went from 266k to 176k characters. Raw scanning is
      about 90% of the parse, so file size is the lever.
  - The tint was made cheaper anyway: rows are clipped to the circle, the inner half skips the smoothstep, and the blend
    is in integers.
  - The next data round (places under 1,000) must not ship as a bigger file of this kind without re-measuring.
  - History: a first version scanned every lot and footprint linearly, taking 350 ms (kits) and 330 ms (trees) in the
    editor. `DiscIndex` (a 4 km grid) brought that to about 30 ms each.
  - Build: wasm +111,913 bytes and data +14,441, plus the localities file fetched at boot.
## Towns round 2: dense, era-correct towns (branch `promo-towns-2`, issue #31)
Question: why do Tel Aviv, Jaffa, Lod, Ramla, Rishon and Petah Tikva read as a sparse handful of boxes at gameplay03
when promo 03 shows dense miniature towns, and what closes the gap within the frame-time and boot budgets?

### Diagnosis (main `bdb5cd2`, measured before any change)
Measured with `TownCensusTests.TheCensusIsLogged`. It builds the kits as `MapScene.Build` does, with the routes' and the
units' clearings as corridors, and logs one `CENSUS` line per town. It reports:
- **Buildings:** counted per town. Wings are a block's lower part.
- **Cover:** the share of the town's own land under a building or landmark footprint. "Own land" is its footprint disc,
  on land, in its cell. It is sampled every 100 m, over the whole disc and over its inner half radius ("core").
- **Wall heights:** symbolic metres; one storey is 198 m at 60x and 330 m at 100x.
- **Wall colours:** distinct sRGB colours at 4 bits per channel.

The player logged `KITS buildings=261 vertices=10244 triangles=6892 trees=7283`.

| town | footprint R | buildings (+wings) | landmarks | cover: all / core | wall height min / median / max | wall colours |
|---|---|---|---|---|---|---|
| Tel Aviv | 7.6 km | 43 (+11) | none | 24.7% / 24.9% | 396 / 594 / 1188 m | 7 |
| Jaffa | 5.7 km | 16 | mosque, clock tower, church, lighthouse, breakwater | 42.6% / 14.6% | 330 / 660 / 990 m | 4 |
| Haifa | 6.9 km | 34 (+10) | none | 17.8% / 23.7% | 396 / 594 / 990 m | 8 |
| Jerusalem | 7.4 km | 70 | a generic mosque | 32.2% / 16.2% | 330 / 660 / 990 m | 8 |
| Lod | 2.4 km | 5 | mosque | 13.6% / 11.8% | 330 / 330 / 660 m | 4 |
| Ramla | 2.3 km | 5 | mosque | 19.5% / 27.6% | 330 m only | 3 |
| Petah Tikva | 2.4 km | 3 | water tower | 10.5% / 7.0% | 330 / 330 / 660 m | 3 |
| Rishon LeZion | 1.7 km | 2 | water tower | 9.5% / 2.7% | 330 m only | 2 |
| all 25 | | 235 (+21) | 26 | | | |

Jaffa's whole-disc cover is above its core cover because its cell is small (Tel Aviv's footprint takes its north) and
four landmarks of up to 1 km sit in it.

Causes, in order of weight:
1. **Counts.** `TargetCount` was tuned at 25x and falls with the square of the scale. At the 60x / 100x ruling, every
   town under about 30,000 people gets its floor of 6 buildings, and then places fewer: 1 to 5 in the moshavot, and 5 in
   Lod and Ramla.
2. **Placement.** Buildings went to random spots over the whole footprint (radius drawn as u^0.62), tried against
   every lot. Each town had about 30 tries per building, and the footprint could stretch to 1.6 R.
   - That spreads a town evenly at low cover, with no dense core and no street rows. Promo 03's towns are a solid core
     with streets as the only gaps.
   - The routes' corridors (300-680 m ribbons plus a 100 m margin) and the units' clearings then cut each disc further.
3. **Old-town houses too big.** Old towns drew 7-14 m houses at 100x, so each was 0.7-1.4 km wide. Jaffa came out as 16
   large blocks, not a packed old city.
4. **Heights.** Tel Aviv's median block was 3 storeys and its tallest 6. Promo 03 shows 5-8. The owner's "promo over
   era" ruling covers this.
5. **Landmarks.** Only Jaffa had its own. Jerusalem had a generic mosque, and Haifa and Tel Aviv had none. Jaffa's
   lighthouse stood at the breakwater's head; the real one (1865) stands on the hilltop above the old port.
6. **Palette.** It is not the gap: each archetype's colours already match promo 03 (white and cream city, honey stone,
   tan villages, terracotta roofs). Variety is limited by the counts.

### Plan
- **Plot lattices** replace the random tries (`SettlementKits.PlaceHouses`).
  - Each town is laid out in rows of plots at its street grid's angle. Old towns and villages stagger alternate rows
    and jitter each plot.
  - Each plot is built with a probability that is near 1 in the core and falls towards the edge, to none at 1.1 R. That
    is `UnitGround.FootprintMargin`: the units assume no building past it, and `PaintGround` tints the same disc.
  - A plot by a road, or by another town's lot, tries a smaller building, then a small nudge within the plot, before it
    stays empty.
  - Plots are visited nearest the centre first. Each plot draws its random numbers whether or not it is built, so one
    plot's fate never shifts another's.
- **Building styles per quarter, at the ruled scales** (city blocks 60x, houses 100x).
  - **Tel Aviv:** white International Style blocks with flat roofs (the White City, 1930s, Geddes plan 1925-29), some
    stepped. Taller in the core under the promo ruling.
  - **Jaffa and the old towns:** small stone houses (4.6-6.6 m real), packed, with domes and roof rooms. Some red tiled
    roofs outside the core.
  - **Haifa:** the lower town by the port in stone houses (below about 35 m elevation). Hadar HaCarmel up the slope in
    white blocks.
  - **Jerusalem:** stone-faced blocks in the new city to the west (Storrs' 1918 stone ordinance), a few with red roofs.
    Arab-quarter houses round the other sides. Old-town houses within the walls.
  - **Large moshavot** (Petah Tikva, Rehovot, Rishon LeZion, Hadera; 7,500+ people in 1945): a plastered two- and
    three-storey centre inside red-roofed houses.
  - **Arab towns and villages:** smaller stone houses (4.4-6.6 m) on a staggered lattice.
- **Landmarks**, placed before any town's houses. Positions keep the real bearing from the settlement's point to the
  landmark's sourced coordinate; distances and sizes are symbolic.
  - **Jaffa:** the Mahmoudiya Mosque, the clock tower on the plaza east of it, St Peter's Church, the lighthouse on the
    hill above the port (red and white bands), and the breakwater and boats.
  - **Jerusalem:** Suleiman's walls round a symbolic square Old City with five gates, the citadel, the Haram platform,
    the Dome of the Rock and Al-Aqsa.
    - The Dome of the Rock is blue and white tiles with a dark lead dome: the gilded dome dates from the 1959-64
      restoration, so it would be an anachronism in 1948.
    - Al-Aqsa's dome is lead.
  - **Haifa:** the 1933 port (main and lee breakwaters, quay sheds) and the German Colony: the Templer houses of 1868 in
    stone with red roofs, in two rows along its north-south main street.
  - **Tel Aviv:** its port (1936/1938) at its sourced coordinate.
  - **Ramla:** the White Tower east of the town's heart.
- **No anachronisms:**
  - The Shrine of the Báb's dome (1953) is not drawn.
  - Neither is the Dagon silo (1955), the gold Dome of the Rock, or any post-1948 tower.
- **Performance:** one merged mesh per town as before, so the draw calls do not change. The lot index is promo-villages'
  `DiscIndex`, ported byte for byte with its `KitGround` hunks so the two branches merge cleanly there. It also takes the
  trees' lot test off its linear scan.
- **Tests** (`TownCensusTests`):
  - The towns are deterministic.
  - Each city's landmarks are present.
  - No building stands on a route or in the sea.
  - Each of the four cities has a floor on its built cover.

### Result (after merging the villages, #291)
The census as the map now builds it: with the 1948 localities, whose landmarks claim their hearts before any town's
houses (review M8). "Main" is `a3202ad`, with the villages and before towns round 2; same census, same corridors.

| town | buildings, main → round 2 | cover all / core, main → round 2 | median wall height | landmarks, round 2 |
|---|---|---|---|---|
| Tel Aviv | 36 (+12 wings) → 43 (+11) | 18.4 / 19.7% → 17.4 / 18.5% | 396 → 792 m (2 → 4 storeys) | port, 3 hangar sheds |
| Jaffa | 15 → 33 | 29.0 / 21.8% → 32.6 / 31.9% | 660 m | Mahmoudiya Mosque, clock tower, St Peter's, lighthouse on the hill, breakwater |
| Haifa | 30 (+10) → 45 (+5) | 16.4 / 19.4% → 21.0 / 20.6% | 594 → 660 m | port (main and lee breakwaters, 4 quay sheds), German Colony (Templer houses in two rows) |
| Jerusalem | 70 → 110 | 25.7 / 19.8% → 22.7 / 24.9% | 660 → 330 m | walls (a gap where a road crosses), 13 gates, citadel, Haram, Dome of the Rock, Al-Aqsa |
| Lod / Ramla | 5 / 5 → 11 / 11 | | | mosque; Ramla also the White Tower |
| Petah Tikva / Rehovot / Rishon | 2 / 3 / 2 → 8 / 5 / 3 | | | water tower |
| all 25 | 216 → 365 (413 after the review: the small-town floor, #317) | | | 27 → 55 |

Distinct wall colours: Haifa 7 → 16 (white blocks, the lower town's stone, the Templer houses), Jerusalem 8 → 14 (stone
blocks, village houses, red-roofed villas), Jaffa 6 → 7, Tel Aviv 8 → 7.

- **Tel Aviv's cover did not rise.** Before the merge, round 2 had 62 blocks and 30.0% / 25.6%.
  - The villages' landmarks inside its 7.6 km disc (Sheikh Muwannis, Summeil, Jammasin, Ramat Gan, Bnei Brak and
    others) now claim their spots first. The lattice loses every plot their mosque or water-tower lots reach.
  - What changed there is the form: narrower (12-16 m real), taller blocks in rows on the Geddes grid, with a 700 m beach
    strip in front. The close-up crops show the difference better than the cover does.
  - Taking the villages' spots back would break the villages' rule that a village keeps its heart, so it is left for the
    owner.
- **Jerusalem's Old City is symbolic and squeezed by the roads.**
  - It is 4 km across, about 4 times the real one. The drawn 1948 roads (300 m ribbons) meet by Jaffa Gate and loop
    round the north and east, so at any readable size they cross inside the square, through gaps in the walls.
  - Only one place in its south-east quadrant fits a platform clear of every route: 1.3 x 1.1 km, wider than long. Al-Aqsa
    therefore stands in its south-east corner, not across its south end.
  - The citadel is moved east off the drawn Hebron road.
  - The houses inside the walls are capped at two storeys, and the Dome's walls and drum are 30x high, so the walls and
    the Dome stand above the roofs.
- **Readability and colour:**
  - The grid stays readable: 78% of edge midpoints (69 of 89), against the 75% floor and main's 79%.
  - The beach beside Tel Aviv reads as sand at 26 of 33 samples (79%, floor 75%).
  - At gameplay03 (WebGL), the palette distance to promo 03 is 0.43 (main 0.44), warmth 0.068 (0.070) and saturation
    0.45 (0.45).
- **Trees:** `Vegetation.Plant` now draws each tree's random numbers before its lot and route test. A town's new lots
  remove only the trees they touch, instead of reshuffling every later grove.
  - Two grade tests sample the selection ring against the land and could land on a crown. They now measure with the
    trees off: the ring against the ground's graded colour, which is what they test.
- **Frame time** (headless WebGL `?vprofile`, `machine_lock.py --exclusive --wait-unity`, main `a3202ad` against
  `49bc88e`, two interleaved rounds each, 2026-10-12, load 3.6 at the start and 7.3 at the end):

  | | gameplay03 all (p95) | gameplay03 all − none | diorama02 all (p95) | diorama02 all − none |
  |---|---|---|---|---|
  | main | 13.70 / 14.22 ms (15 / 16) | 3.00 / 3.45 ms | 18.07 / 18.71 ms (20 / 20) | 4.84 / 5.60 ms |
  | round 2 | 14.15 / 13.97 ms (16 / 15) | 3.59 / 3.15 ms | 18.06 / 18.70 ms (21 / 21) | 5.54 / 5.53 ms |

  - The kits stay one merged mesh per town: 63 draws, as on main. The triangles grow from 61,054 to 65,684.
  - "-kits" draws the marker boxes instead, so the kits' own cost is within the noise (all − (-kits) is −0.6 / −1.1 ms
    at gameplay03).
  - Every budget holds: all − none is under 8 ms, and the p95 at gameplay03 is under 16.7 ms. The diorama02 p95 of 20-21
    ms is main's too (the villages section).

- **Boot** (`headed.py boot --runs 3`, three interleaved rounds per side, so 9 runs each; main `2b7212e` against this
  branch's final head; `machine_lock.py --exclusive --wait-unity`, 2026-10-12).
  - Other sessions loaded the machine (load average 6.5 rising to 25), so single runs reach 3.3 s at cpu 4. The medians
    and the "kits built" phase are the fair comparison.

  | | map_build median | `kits built` phase median | first_frame median |
  |---|---|---|---|
  | cpu 4, main | 2739 ms | 570 ms | 9494 ms |
  | cpu 4, round 2 | 2679 ms | 563 ms | 8750 ms |
  | cpu 1, main | 659 ms | 132 ms | 4029 ms |
  | cpu 1, round 2 | 662 ms | 138 ms | 4020 ms |

  - The towns add about nothing to the boot: the kit phase is 7 ms lighter at cpu 4 and 6 ms heavier at cpu 1.
  - The lattice checks are cheap first, and only towns with water near them run the beach test.
  - The earlier session on a quieter machine (main `a3202ad`, before those cuts) measured map_build 2668 → 2683 ms at
    cpu 4 (6 runs each), with the cpu 1 kit phase 129 → 137 ms.
  - The budget at cpu 4 is 2720 ms; #195 needs about 100 ms of it. Towns round 2 takes none.

### Review (2026-10-12, on the merge with main `471c300`)
- **Fixed on the branch:**
  - The beach setback tested any water, so it also applied on the Sea of Galilee. It now tests the sea only (`Sea`: water
    that is not a lake hex). Degania goes from 0 houses back to main's 1.
  - Nahalal's ring ended at 0.88 of the radius, but its lattice's first ring of plots stands at 0.76 and 0.89, so it drew
    no houses. The ring now runs out to the radius: 0 → 1 (main 1).
  - Tel Aviv's blocks are capped at eight storeys, promo 03's tallest: the cap was ten. The tallest block is now 7
    storeys (1,386 m) and the median is still 4.
  - `TownCensusTests.SmallTownsKeepTheirHousesAndNoBlockOutgrowsThePromo` covers all three.
- **Each test fails under a mutation:**
  - Reverting the lake test, the ring and the cap fails the new test on all three counts.
  - A per-build counter in the plot yaw fails `EveryBuildPlacesTheSameTowns`.
  - Dropping the plots' route test fails `NoBuildingStandsOnARouteOrInTheSea` (268 placements).
  - Thinning the core to 0.4 fails `TheCitiesCoverTheirGround`.
- **Small-town floor (#317, fixed on the branch; reworked after the review of #339):**
  - At 100x a plot is about 1 km, so a small town (1.3-2 km footprint radius) has about 7 plots, and the routes, the sea or
    a neighbour's lots take most of them. On the first lattice Tiberias drew 0 houses (main 3), Netanya 1 (main 2) and
    Beersheba 3 (main 5).
  - **The lake was half the cause.** The kits' `KitGround.Water` counted every lake hex as water, and a lake hex also holds
    its shore. Tiberias stands in one, 60-160 m above the Sea of Galilee (about -214 m), so it had no ground left.
    - A point is now lake water only at or next to the lake's surface, in any hex that is not sea (a shore hex of another
      biome holds some of the lake too). It uses the rule the drawn water uses
      (`MapBuilder.LakeBelowM` and `LakeFlatM`, tools/terrain_bake.py): below -150 m and flat within 0.25 m over the
      heightmap's 3 x 3 window, at the point or within one heightmap sample of it.
    - The drawn shore (coast.png's distance, interpolated between texel centres) runs up to about a sample past the last
      flat sample. A first version without that sample's margin put a cypress in the drawn Kinneret, and one that looked
      only in lake hexes put a palm and a citrus tree in it by Ghuweir Abu Shusha.
    - The test runs only in a lake's hexes and their neighbours, and each sample's flatness is cached: the rift's floor
      is asked about thousands of times while the kits are built.
    - Sea hexes keep their test. The Hula is not water in the heightmap's rule (its surface is about 70 m above sea
      level), on main or here.
    - `KitTests.TheLakeIsItsWaterNotItsWholeHex` checks that Tiberias's shore at 35.530 E, 32.7944 N (lake hex, -111 m)
      is land and that the lake at 35.58 E, 32.80 N (-214.5 m) is water. Each half fails when the lake branch is made
      always water, or never water.
    - `KitTests.NothingStandsInALakesDrawnWater` decodes coast.png itself, the file the water shader samples. It checks
      that no lot centre and no tree trunk in the rift (a lake hex, or ground below -150 m; 129 of them) lies under the
      drawn water. It fails on the cypress without the sample's margin, and on the palm and the citrus tree with lake
      hexes only.
    - Tiberias's lattice now draws 4 houses on its shore, and the fallback a fifth, full-size, beside them.
  - **The fallback.** A town whose lattice places fewer than 6 houses (round 1's floor, `SmallTownFloor`) is filled up
    the way round 1 drew it.
    - Each house is the archetype's full-size building, the size main drew, and keeps its whole footprint as its lot.
    - It tries 240 spots (`ScatterTries`) from its own stream (`KitRandom(id, 3)`), nearest the town's sourced point
      first. Every spot is tried at full size before any at 0.72; the half-size fit is never used. Every house keeps the
      beach setback.
    - `Build` runs the fallback after every place's own houses (towns, the 1948 localities) and the camps, so it takes
      only ground no other place uses. `KitTests.TheSmallTownFloorMovesNoOtherPlace` checks this: every lot of every
      place is the same, in the same order, with the fallback off and on.
    - It fires in 15 towns (the moshavot and kibbutzim, Tiberias, Netanya, Beersheba and Acre) and adds 27 houses in 13
      of them.
      Acre and Yad Mordechai have no free ground left.
  - Every town now draws at least main's count: Tiberias 5, Netanya 4, Beersheba 5, Rosh Pinna 2, Nahalal 3. The 25
    towns draw 397.
  - **Tests:**
    - `SmallTownsKeepTheirHousesAndNoBlockOutgrowsThePromo` pins every town at main's count (`MainBuildings`).
    - `TownCensusTests.NoTwoHousesOfATownOverlap` checks the footprints by separating axes. It fails (20 pairs) when the
      fallback skips its own lattice's lots.
  - The grid stays readable at 80% (71 of 89).
  - **Perf, after the review's fixes** (WebGL, main `4abb64f` against the branch, `machine_lock.py --exclusive
    --wait-unity`, two interleaved rounds, load 5-9, 2026-10-13):
    - Frame time is equal. At gameplay03 `all` is 8.08 / 7.73 ms on main against 8.14 / 8.02 ms on the branch, and at
      diorama02 19.43 / 19.50 against 18.90 / 19.42 ms. The p95s are equal, and there are 63 draws on both.
    - Boot at cpu 4: the "kits built" phase is about 45 ms heavier, 691 / 709 ms on main against 743 / 750 ms.
    - An attribution round (main, the branch, the branch with the fallback off, and the branch with the old whole-hex
      lake rule) put about 10 ms of that on the fallback: 689 / 738 / 729 ms in the quieter round. The lake rule costs
      nothing measurable. The rest is round 2's own placement against this main.
    - The author's boot against `2b7212e` measured the kit phase within noise. The ~35 ms not explained here is not
      attributed further.
  - Perf, one interleaved round with the floor (`95710e7` against main `471c300`, exclusive lock, the machine loaded by
    other sessions to load 15, so every number is high):
    - Frame time at gameplay03 is 25.62 ms (main) against 25.24 ms (branch), and at diorama02 27.78 against 27.67 ms.
    - Boot map_build median is 2982 against 3075 ms. The "kits built" phase is 746 against 686 ms.
    - The editor's kit build_ms is 85-89 ms, with or without the floor.
    - The floor adds no measurable cost; the boot difference is load. Still 63 draws.
  - The label refresh is unchanged: `MapLabelTests.RefreshWhileTheCameraMovesIsCheapAndAllocationFree` measured 162-163 µs
    with the floor and 171-180 µs without it, interleaved. The single 238 µs (fail) came in a full run at load 24.
- **Still open, filed as issues:**
  - #318: Tel Aviv's cover is held down by the localities' landmark lots.
  - #319: the symbolic Old City has roads crossing inside its walls.
- **Unchanged after the fixes:** the grid stays readable at 78% (69 of 89), and the beach beside Tel Aviv reads as sand at
  79% (26 of 33).
- **Perf, re-measured by the review:** frame time is headless WebGL `?vprofile`; boot is `headed.py boot --runs 3 --cpu
  4`. Main `471c300` against the branch at `4a68d05`, each round under `machine_lock.py --exclusive --wait-unity`, in
  interleaved order. A third round was cut short by a stop.

  | | gameplay03 all (p95) | gameplay03 none | diorama02 all (p95) | diorama02 none | boot cpu 4: map_build / first_frame |
  |---|---|---|---|---|---|
  | main, round 1 (load 3.1) | 14.21 ms (16) | 10.72 ms | 19.47 ms (23) | 13.20 ms | 2584 / 8431 ms |
  | branch, round 1 (load 7.5) | 15.40 ms (17) | 11.32 ms | 18.18 ms (21) | 13.06 ms | 2598 / 8369 ms |
  | branch, round 2 (load 13.6) | 14.36 ms (16) | 11.01 ms | 17.80 ms (20) | 13.00 ms | 2619 / 8367 ms |
  | main, round 2 (load 4.2) | 14.23 ms (16) | 11.03 ms | 17.81 ms (20) | 13.02 ms | 2595 / 8390 ms |

  - Round 2 is equal within noise. In round 1, the branch's gameplay03 cost rises with its `none` row (+0.6 ms both),
    that is, with the machine's load.
  - Boot's map_build is 14-24 ms over main, within the runs' spread, under the 2720 ms budget at cpu 4.

### Sources (checked 2026-10-12)
- Tel Aviv:
  - White City (1930s International Style, flat roofs; Geddes plan 1925-29): https://en.wikipedia.org/wiki/White_City,_Tel_Aviv
  - Port of Tel Aviv (opened 1936, completed 1938, closed 1965; 32.0965 N, 34.7733 E): https://en.wikipedia.org/wiki/Port_of_Tel_Aviv
- Jaffa:
  - Clock Tower (1900-03, limestone, at the north end of Yefet Street): https://en.wikipedia.org/wiki/Jaffa_Clock_Tower
  - Mahmoudiya Mosque (mostly 1812, the old town's north-east corner, its east gate on the clock-tower plaza; 32.0549 N,
    34.7555 E): https://en.wikipedia.org/wiki/Mahmoudiya_Mosque
  - St Peter's Church (1888-94, on a hill near the shore, bell tower; 32.0548 N, 34.7521 E): https://en.wikipedia.org/wiki/St._Peter%27s_Church,_Jaffa
  - Jaffa Light (1865, 10 m, red and white bands, on the hilltop above the old port; 32.0535 N, 34.7505 E): https://en.wikipedia.org/wiki/Jaffa_Light
- Haifa:
  - Port of Haifa (opened 31 October 1933; 32.8201 N, 35.0045 E): https://en.wikipedia.org/wiki/Port_of_Haifa
  - German Colony (Templer colony of 1868; main street north-south down to the harbour; 32.8200 N, 34.9906 E): https://en.wikipedia.org/wiki/German_Colony,_Haifa
  - Templer houses (stone, red roofs): https://en.wikipedia.org/wiki/Templers_(religious_believers)
  - Shrine of the Báb (superstructure and dome completed 1953): https://en.wikipedia.org/wiki/Shrine_of_the_B%C3%A1b
- Jerusalem:
  - Walls (1537-41, 4,018 m, about 12 m high): https://en.wikipedia.org/wiki/Walls_of_Jerusalem
  - Old City (0.9 km^2; 31.7767 N, 35.2342 E; the gates' sides): https://en.wikipedia.org/wiki/Old_City_of_Jerusalem
  - Damascus Gate (the wall's north-west side): https://en.wikipedia.org/wiki/Damascus_Gate
  - Tower of David (near Jaffa Gate; 31.7761 N, 35.2278 E): https://en.wikipedia.org/wiki/Tower_of_David
  - Temple Mount (the Old City's eastern section, platform about 488 x 315 m; Al-Aqsa at its south end): https://en.wikipedia.org/wiki/Temple_Mount
  - Dome of the Rock (blackened lead before 1959; Ottoman blue and white tiles): https://en.wikipedia.org/wiki/Dome_of_the_Rock
  - Al-Aqsa Mosque (lead dome before 1969): https://en.wikipedia.org/wiki/Al-Aqsa_Mosque
  - Jerusalem stone (Storrs' ordinance, 1918): https://en.wikipedia.org/wiki/Jerusalem_stone
- Ramla:
  - White Tower (square, five storeys, 27 m; rebuilt after the 1318 earthquake; 31.9275 N, 34.8661 E): https://en.wikipedia.org/wiki/White_Tower_(Ramla)

## Settlement quick wins (branch `art/settlements-quickwins`, refs #31)
The 2026-10-13 gap audit (`.superpowers/audit-2026-10-13/renders`: `measure.py`, `metrics.json`) ranked the settlements
the biggest gap to promo 03. Measured with its crops and poses: editor renders (`MapCapture.Promo`, Mobile tier, with
`-extraPose close30:34.83:32.00:30000:10`) and the WebGL `?shot` of gameplay03. Main is `7aeec3f`.

**Changes.**
- **White City** (`Style.WhiteBlock`):
  - Walls are lime-white plaster (0.94, 0.945, 0.94; was 0.94, 0.92, 0.87), with 8% of blocks cream (was 12%).
  - Roofs are paler (0.90, 0.89, 0.86).
  - Each upper storey has a balcony with a solid plastered parapet on the street side, and on most blocks the back too
    (`WhiteBlockDetail`). Each balcony stays inside the plot's lot.
  - Roof rooms are on 80% of blocks (was 50%), and a second room is on 45% of the blocks that do not step.
  - `Kit.shader`, White City walls only: discrete windows with plaster piers between them, the glass near black
    (`_CityWindowDark` 0.95; the other styles keep `_WindowDark` 0.55), and a darker band under the balcony slab. The
    storey pattern holds until a storey is about two pixels (was three), so the 30 km look keeps its window rows.
- **Old towns, Arab towns and villages** (`Style.OldHouse`, `Style.Village`, `Archetype.OldTown`, `Archetype.ArabTown`;
  `StoneHouse`):
  - Three in four houses of two storeys or more step back: the top storey is a smaller block on one corner of the lower
    roof. Single-storey houses often have a roof room.
  - Walls are grey-buff limestone (0.77, 0.70, 0.60, jittered from grey to buff) and roofs lime plaster.
  - A village below 120 m (the coastal plain and the valleys) is mud brick (0.78, 0.69, 0.56) with flat roofs.
  - Domes are fewer and lower: a cap rising 0.6 of its radius, not a hemisphere, in the roof's lime colour, not white.
    - Old towns: 8% of flat-roofed houses (was 22%).
    - Hill villages: 7% (was 10% of every village).
    - Plain villages: none.
  - The mosques' and landmarks' domes are unchanged.
- **Historical basis.** The owner's ruling (m3-rulings, "Owner rulings"): where the promo departs from the era, the promo
  wins, but the architecture stays plausible for 1948. Domes are reduced, not removed.
  - The hill country built in limestone, and its houses were roofed with stone vaults and cupolas.
  - The coastal plain and the valleys built in mud brick, with flat roofs of beams and earth.
  - So domes stay on hill houses only, as the low cap a vault shows from outside.
  - Sources are under "Sources" below.
- **Layout unchanged.** A 1948 locality's houses share the locality's random stream with its spots. `Emit(Archetype.ArabTown)`
  still draws exactly five numbers from it (wall, roof, the dome roll), and each house's detail comes from a stream seeded
  off it. The lattice towns' houses already had a stream of their own.
  - Every building stands where it did: same lots, same trees, buildings 1954 (WebGL) / 1953 (editor), locality buildings
    1339, trees 8358, draws 63.
  - So #318 (the localities' landmark lots in Tel Aviv) and #249 (tree holes under the figures) are untouched.

**Result** (main → branch; promo in brackets):

| | main | branch | promo 03 | target |
|---|---|---|---|---|
| gameplay03 white share (V > 0.72, S < 0.28; WebGL shot, map area) | 2.49% | 6.55% | 7.46% (audit) | >= 4.5%: met |
| gameplay03 white share (editor, Mobile) | 2.36% | 6.84% | | |
| close30 Tel Aviv edge density (crop 0.32-0.58 x 0-0.30, 600x400) | 0.038 | 0.054 | 0.114 | >= 0.07: not met |
| close30 Tel Aviv p5 / p95 | 0.241 / 0.753 | 0.221 / 0.787 | 0.136 / 0.896 | p5 <= 0.18: not met |
| close30 Tel Aviv warmth | 0.131 | 0.114 | 0.090 | <= 0.09: not met |
| close30 Jaffa edge / warmth | 0.041 / 0.141 | 0.047 / 0.112 | 0.100 / 0.107 | |
| close30 village crop edge / warmth | 0.035 / 0.222 | 0.036 / 0.198 | 0.071 / 0.132 | |

- **Why Tel Aviv's edge, p5 and warmth fall short.**
  - At close30 a storey is about three pixels. The crop's top third lies in the far tilt-shift band: the edge per band
    rose from 0.031-0.046 to 0.039-0.058, against the promo's 0.05-0.16.
  - About half the crop is the town's sand ground. Its tint comes from `PaintGround`, and
    `SeaTests.TheBeachBesideTelAvivStaysSandWithTheKitsOn` pins it.
  - The walls are already near-neutral white; the remaining yellow is the sun and the grade.
  - Closing the gap needs denser, smaller blocks (a layout change that meets #318), palms between the buildings (the
    vegetation) and a ground or light change. None is a quick win, so they are left as follow-ups.
- **Kit numbers** (WebGL `KITS` line, main → branch): buildings 1954 → 1954, vertices 85,401 → 99,131, triangles
  66,436 → 68,366, draws 63 → 63.
- **Boot** (`tools/headed.py boot --runs 3 --cpu 4`, three rounds, order rotated, each round under one exclusive
  `machine_lock`, 2026-10-14; builds of main `7aeec3f` and this branch; 9 cold loads each):

  | | map_build median (range) | `kits built` phase median | first_frame median |
  |---|---|---|---|
  | main | 3067 ms (3042-3085) | 747 ms | 11075 ms |
  | branch | 3055 ms (3036-3077) | 743 ms | 11081 ms |

  - Within noise: the per-round medians alternate (+19, -26, -16 ms), and the kit phase is unchanged.
  - The machine was loaded (other sessions' jobs), so both sides sit over the 2720 ms budget. Main does too; that is
    #321's margin, not this change.
- **Goldens:** the WebGL `golden&smoke=5` run shows `GOLDEN OK 1950 71e9904d`, `GOLDEN OK 1952 62f8ec61` and
  `SMOKE OK 5 turns 1948-06-19 d3fdd459`, unchanged.
- **Tests:** EditMode passes 768/768 with 0 `warning CS`, on the branch and on main `7aeec3f` alike. An earlier run on
  the old base lost 52 UI, label and border coroutine tests to destroyed scene objects. It did not reproduce: all 768
  pass on both.

### Sources (checked 2026-10-13)
- Stone in the mountains, mud brick and wood on the coast and in the valleys: Palestine Ministry of Tourism and Antiquities,
  virtual museum, "Traditional Palestinian House - Al Hosh - The Courtyard":
  https://virtualmuseum.tourism.ps/en/article/72/Traditional-Palestinian-House---Al-Hosh---The-Courtyard
- Vaults, cupolas and roofing of the Palestinian traditional house: M. Abuarkub, "Traditional Building Materials and
  Techniques in Palestine", Prostor 25 (2017) 140-153, https://doi.org/10.31522/p.25.1(53).9
- The White City (1930s International Style, flat roofs, balconies): https://en.wikipedia.org/wiki/White_City,_Tel_Aviv

## Issue #195: the water and the border lookups out of the terrain's fragment shader
- **Why.** promo-sea (#138) put the water shading (`ScWaterShade`, the breakers' `fwidth`, the footprint) in the terrain
  graph's function, and promo-borders (#206) six neighbour fetches in `ScBorderApply`'s loop. Either one makes the shader
  big enough to slow every terrain pixel in WebGL, the water and borders toggled off included
  (`.superpowers/handover-2026-10-11/promo/perf-investigation.md`): none@gameplay03 measured 2.5 ms without both, 7.3 ms
  with the borders' fetches alone, about 10 ms with both.
- **Water.** `StatecraftTerrain.hlsl` outputs a black, fully metallic, fully occluded surface with no emission on the water
  (Lit adds 0 there). `MapBuilder.BuildWaterMesh` copies the terrain's surface wherever the water's coverage can be above
  0: wet heightmap samples (the sea within 0.5 m of 0 m, flat rift lakes) and two samples around them, at full resolution,
  with flat blocks as T-junction-free fans (129,882 vertices, 239,958 triangles). It is drawn with a copy of `Sea.mat` in
  its `_ScTerrainWater` mode: additive, colour times coverage (the land adds 0: no per-pixel discard to split the shore's 2x2 quads, the lesson of #306), no depth write, its depth pulled two
  pixels' worth towards the camera (the terrain's LOD splits the surface its own way). The terrain draws one queue earlier
  (`Terrain.mat` 1998) and the water at 1999, before every other opaque, so boats and harbours cover it.
- **Borders.** `MapBorders.Use` bakes, per hex, which of its six edges border another region (alpha: 192 plus a bit per
  edge, `MapBorders.NeighbourSteps` order). `ScBorderApply` reads its own hex's texel only, where it read up to seven, and
  skips a hex with no border edge (it would draw nothing). Same math, same pixels: `BorderTests` unchanged, and the border
  pixels of the player's screenshots are identical. An overlay mesh along the bands, like the water's, was the plan; the
  bake met the target with no mesh, no boot or memory cost and no rebuild hitch, so no mesh was built.
- **Exactness.** On the water and its shore the sum is what the terrain drew before, except where a border band meets the
  140 m coverage ramp at the shore, and the extra rounding of one blend.
- **Measured** (WebGL, headless, 1920x1080, DPR 1; `PROFILE summary` medians over 3 interleaved rounds, main 1cdb356 and
  the fix back to back under one exclusive machine lock per round, order rotated; [min-max] across rounds):

  | pose, toggles | main 1cdb356 | fix |
  |---|---|---|
  | gameplay03, none | 10.48 [10.29-10.79] | 2.52 [2.44-2.55] |
  | gameplay03, all | 13.94 [13.85-14.14] | 3.19 [3.16-3.19] |
  | diorama02, none | 13.56 [12.40-13.78] | 8.18 [8.02-8.33] |
  | diorama02, all | 18.72 [17.92-19.03] | 12.05 [11.93-12.35] |

- **Baked.** The mesh's samples and triangles are baked into `Assets/Textures/Map/terrain_water.bytes` (3.4 MB;
  `Statecraft.Game.EditorTools.TerrainWaterBake.Bake`, re-run whenever `heightmap.r16z` changes), referenced by
  `Map.unity`'s `MapScene`, so a player only places the vertices on the heights it loaded. The file carries the
  heightmap's resolution and a check of its heights; a stale or missing bake is refused (a warning) and the mesh derived at
  load as before (22 ms unthrottled, about 87 ms at `--cpu 4`). `SeaTests.TheBakedWaterMeshMatchesTheHeightmap` fails on
  a stale bake.
- **Boot** (`tools/headed.py boot --runs 3 --cpu 4`, interleaved under one exclusive lock per round, order rotated; six
  rounds, 18 runs each, 2026-10-13, main 244723f against the fix merged on it, cc3c954): `map_build_ms` median main 3023
  [3006-3042] ms, the fix 3039 [3017-3064] ms. `terrain water` takes 19 [17-22] ms, the fix's whole share; `terrain
  heights set` is 1389 [1374-1411] ms on main and 1397 [1372-1409] ms on the fix.
- **`SetHeights` moves with the build, not with the fix.** The review measured the fix merged on main 6d439a7 (44e1d63)
  at about 105 ms over that main, 85 ms of it in `terrain heights set`, and it reproduces here (1473 [1462-1496] ms
  against 1385 [1375-1418], nine runs each). It is not the 3.4 MB bake, and not the WebGL heap:
  - The same commit rebuilt without `Map.unity`'s reference to the bake (the same wasm, byte for byte; the water mesh
    then derived at load) keeps the slow `terrain heights set`: 1487 [1466-1503] ms against 1491 [1463-1499] with it.
  - With heap probes (a `WebAssembly.Memory.grow` hook and the heap size after every boot phase) in builds of main 244723f
    and of the fix merged on it, both grow the wasm heap at the same points to the same sizes (286 MB to 343 MB at
    `new TerrainData`, none inside `SetHeights`); the bake adds 3 MB of reserved native memory and no grow. The
    `SetHeights` call alone measured 1185-1198 ms on both.
  - A small C# change that runs only after `SetHeights` (reading the bake through `TextAsset.GetData`, which cut
    `terrain water` from 20 to 17 ms) made `terrain heights set` 1511 [1489-1530] ms in its build, against the fix's
    1397. It was dropped.
  - With V8's tiering off (`--js-flags=--no-wasm-dynamic-tiering`: every function compiled by TurboFan up front), five
    builds (both mains, the fix, 44e1d63 and the `GetData` variant) all take 1426-1455 ms in `terrain heights set`.
  So each wasm binary gets its own `SetHeights` time, about 1390 or about 1490 ms at `--cpu 4`, from when V8 tiers up
  the code it runs. Any change can move it either way, and the next merge of main draws again. So judge a boot change
  by its own phases, and read `terrain heights set` apart from them.
- **A layer change** (`MapBorders.Use`) takes 0.8 ms for the 870-hex 1948 layer, texture upload included: no rebuild, no
  hitch.
- **Memory.** The water mesh keeps about 4.5 MB on the GPU (its CPU copy is dropped in players, `UploadMeshData`); placing
  it from the bake allocates its 3.4 MB of bytes and about 1.5 MB of vertices, short-lived. The borders add nothing (the edge bits ride in the existing texel).

## Issue #390: a lighter far tilt-shift band at the gameplay zoom
- **What happened.** #320 did not change the tilt-shift. Before it, normal play on the development machine was slow enough
  that the runtime quality fallback stepped down to level 3 and turned the tilt-shift off (headed Chrome, 1920x1080, New
  Game then End Turn: `QUALITY level=1` p95 51 ms, `level=2` 45, `level=3 ... tiltshift=off` 42). #320 made the frame
  about 3.7x faster, so the fallback stays at level 0 and the far band drew in full: Tel Aviv, Hadera and the brigade
  token were a smear. Main before #320 with `?nofallback` draws the same frame as 4abb64f (0.109% of pixels more than 24
  levels apart, identical gradient energy per tenth of the rows). `?shot` and `?vprofile` turn the fallback off, so the
  gameplay03 pixel A/B never showed it.
- **Owner's ruling (2026-10-13).** Keep a soft tilt-shift, so the game still reads like promo 03, but keep the far towns
  readable at the play zoom; the wide views (diorama02) keep the full effect. This also settles the open bottom-band note
  above: the bottom band is not changed.
- **What changed.** `TiltShift.FarStrength(distance)` scales the far (top) band's radius and its blend: 0.35
  (`FarGameplayStrength`) up to 150 km, rising to 1 at the 250 km overview. The near band is unchanged. It is one global,
  `_ScTiltShiftFar`, set with `_ScTiltShift` in `TiltShift.Apply`: no new keyword or variant.
- **Readability bound.** Promo 03's far band (rows 10-18% from the top, columns 30-80%) has 0.34 of the gradient energy of
  its focus band (rows 45-60%); a sharp frame of ours (`?notiltshift`, normal play) has 0.93 there, so the promo keeps about
  37% of the far detail. `TiltShiftTests.TheFarBandStaysReadableAtTheGameplayZoom` asks for at least 60% (shifted / sharp,
  rows 0.78-0.90 from the bottom, under the web bar) at 60 km and at the 120 km starting zoom. The fix keeps 0.72 and 0.69;
  4abb64f kept 0.17 and 0.14. `TheDioramaKeepsBothBands` asks for at most 0.30 far and 0.45 near at diorama02 (0.20 and
  0.33, unchanged). Mutants killed: the far strength at 1 at the gameplay zoom (4abb64f's look), a shader that ignores
  `_ScTiltShiftFar`, a far strength stuck at 0.35 at the overview, and the far strength applied to both bands.
- **WebGL captures**, 4abb64f against the fix. Gradient energy per tenth of the rows from the top, columns 30-80%:

  | frame | build | 0-10% | 10-20% | 20-30% | 30-40% | 40-70% | 70-100% |
  |---|---|---|---|---|---|---|---|
  | normal play, 22 May 1948 (fallback on) | 4abb64f | 13.4 | 5.2 | 6.6 | 18.3 | 29.4 26.3 23.2 | 13.9 3.3 2.5 |
  | | fix | 14.6 | 17.7 | 17.4 | 23.4 | 29.4 26.3 23.2 | 13.9 3.3 2.5 |
  | | `?notiltshift` | 15.3 | 24.7 | 23.1 | 25.8 | 29.4 26.3 23.4 | 21.6 20.5 16.7 |
  | gameplay03 (`?shot`) | 4abb64f | 12.4 | 3.4 | 4.4 | 12.1 | 17.3 17.1 17.0 | 8.9 2.8 2.2 |
  | | fix | 13.2 | 12.7 | 13.6 | 15.0 | 17.3 17.1 17.0 | 8.9 2.8 2.2 |

  diorama02 is pixel-identical (0 pixels more than 24 levels apart). The near band and the focus band do not move.
- **Frame time** (WebGL headless `?vprofile`, 1920x1080, 2 interleaved rounds under the exclusive lock, medians): gameplay03
  none 5.02 / 5.06 ms against 4abb64f's 5.07 / 5.20, all 7.90 / 7.99 against 7.89 / 7.87; diorama02 none 12.63 / 12.62
  against 12.20 / 12.21, all 19.51 / 19.49 against 19.08 / 18.94. Main before #320 measured 20.98 ms at gameplay03 none
  in the same session, so #320's gain stands. The 0.4 ms at diorama02 repeated in both rounds; there the far band's
  strength is 1 and the frame is pixel-identical, so it is the extra uniform read and compare per tilt-shift pixel or
  build-to-build noise, within the tilt-shift's 1.5 ms budget.

## Issue #458: a lighter near tilt-shift band, by distance
- **Owner's ruling (2026-10-14).** The near (bottom) band should match promo 03: less blur at the gameplay zoom and almost
  none at the overview. The far band stays as ruled for #390 (`FarGameplayStrength` 0.35). This replaces the 2026-10-13
  note above that the bottom band is not changed.
- **The gap** (the 2026-10-13 audit, `.superpowers/audit-2026-10-13/renders/measure.py`): mean |Laplacian| of the luma
  in rows 80-95% from the top, columns 30-75%, on the WebGL `?shot`. Main had 0.0056 at gameplay03 against promo 03's
  0.021, and 0.0064 at diorama02 against promo 02's 0.077: the near band drew in full at every distance.
- **What changed.** `TiltShift.NearStrength(distance)` scales the near band's radius and its blend, as `FarStrength` does
  the far band's: `NearGameplayStrength` (0.75) up to 150 km (`NearLightBelowM`, the far band's breakpoint), falling to 0
  at the 250 km overview. One global, `_ScTiltShiftNear`, set with the others in `TiltShift.Apply`; no new keyword or
  variant. The band's height (HalfWidth 0.13, Feather 0.17) is unchanged.
- **Why 0.75, not 0.35-0.5.** The ruling's brief guessed 0.35-0.5. The half-resolution copy the blur composites is softer
  than the frame on its own, so the blend, not the radius, sets most of the measure. A model of the pass in numpy, checked
  against main (it gives 0.0050 for main's 0.0056), puts 0.4 at about 0.046 (twice promo 03's sharpness), 0.5 at 0.039, 0.7
  at 0.024, 0.75 at 0.021 and 0.8 at 0.018. Scaling the radius alone cannot reach the target: a zero radius at full blend
  still measures only 0.023. 0.75 is the value that matches promo 03.
- **Measured** (WebGL `?shot`, main `88787ab` against the branch, same machine):

  | pose | main | branch | promo | target |
  |---|---|---|---|---|
  | gameplay03 | 0.0056 | **0.0195** | 0.021 (promo 03) | 0.015-0.025 |
  | diorama02 | 0.0064 | **0.0575** | 0.077 (promo 02) | at least 0.05 |

  diorama02's `?notiltshift` shot measures 0.0575 too: the overview's foreground is now as sharp as the frame. Above the
  focus, main and the branch differ less than two `?shot` runs of main do (gameplay03: 7346 pixels more than 2 levels
  apart against 14749 run to run, all on the animated sea), so the far band is unchanged.
- **Tests.** `TheNearBandIsGentleAtTheGameplayZoomAndGoneAtTheOverview` covers the curve. At the gameplay pose the near
  band now keeps 30-80% of its gradient energy (`TheBottomBlurs...`, `TheFarBandStaysReadable...`; it kept about 20-25%
  before), and `TheDioramaKeepsTheFarBandAndASharpForeground` asks for at least 95% of the near band at diorama02 (main:
  33%) with the far band still at most 30%. Measured: near 0.37 / 0.39 (Mobile / PC, no bar), 0.41 at 60 km
  and 0.35 at 120 km under the web bar, 1.00 at diorama02; far unchanged (0.71, 0.68, 0.20). Mutants killed: a shader
  that ignores `_ScTiltShiftNear` (near 0.19-0.33: the three render tests) and the near strength applied to both bands
  (far 0.34 at 60 km, 1.00 at diorama02: three render tests); the C# curve held at 1, or not reaching 0 at the
  overview, fails the unit test.
- **Quality fallback.** Unchanged: `TiltShift.Draws` still needs `!FallbackOff`, so the fallback's last step still turns
  both bands off (`QualityFallbackTests` pass). The pass draws as before whenever the tilt-shift is on, even where the near
  band is 0, so frame time does not depend on the new strength.
- **Fill rate.** Headed `?vprofile` at DPR 2 (`headed.py profile --dpr 2`), main, branch, branch, main under one
  `machine_lock.py --exclusive --wait-unity`, 2026-10-14. The machine was loaded by other sessions (load 6-13), so the
  spreads are 1.5-7 ms. diorama02 `all`: main 41.81 / 33.81 ms, branch 41.09 / 39.46; `none`: main 28.02 / 23.97, branch
  26.36 / 24.83. gameplay03 `all`: main 11.95 / 13.99, branch 13.27 / 12.40. The branch sits inside main's range in
  every row: within noise, as expected for one more uniform in an unchanged pass.

## HUD round 1: the info panel and the gilt trim (#398, #397)

Art-track gap 4, first step. The gap list and the later steps (the minimap and settlement badges; the top bar and card
head, which wait for owner rulings) are in the issue.
- The info panel: before a hex is chosen it is a one-line hint (promo 03 shows no panel until a tile is chosen). The
  hex's facts are icon rows with gold signs (`UiIcon` region, house, village, flag): the region, the settlements, the 1948
  localities and the placement. There are no yields (owner ruling) and no land-use name, because the land-use bake is
  modelled, not a sourced record. The region line wraps (#397). One refusal shared by both map actions is said once,
  without buttons, which takes the 1948 panel from about 385 to 300 px tall.
- `UiFrame.Filigree`: a second inset rule and a scroll at each corner (on a panel at least 120 px tall the scrolls also
  run down the sides). It is drawn with Painter2D on the date plate, the event card, the info panel and the minimap, and
  takes no clicks.

Allocations: GC.Alloc samples over 200 renders (`Recorder("GC.Alloc")`, a temporary probe run on both trees) were the
same on main e5bbf0f and on the branch: the info panel 15,600 and the top bar 12,800. No per-frame work is added.

Frame time, `?vprofile`, WebGL headless 1920x1080, DPR 1, `machine_lock.py --exclusive`. Two interleaved rounds per
build, main e5bbf0f against the branch on it (median avg ms, spread in brackets):

| Pose | main all | branch all | main none | branch none |
|---|---|---|---|---|
| gameplay03 | 8.09 (0.03), 8.03 (0.07) | 7.96 (0.16), 8.06 (0.06) | 5.25 (0.10), 5.32 (0.23) | 5.32 (0.04), 5.27 (0.10) |
| diorama02 | 18.87 (0.72), 19.10 (0.61) | 18.88 (0.61), 18.99 (0.59) | 12.23 (0.02), 12.33 (0.19) | 11.66 (0.32), 11.57 (0.16) |

Headed at DPR 2 (`headed.py profile --dpr 2`). The first run compared main e5bbf0f with the branch; then two
interleaved rounds compared main db99f3a with the branch merged with it. Median avg ms, then p95, main first:

| Pose, config | run 1 (e5bbf0f) | round 2 (db99f3a) | round 3 (db99f3a) |
|---|---|---|---|
| gameplay03 all | 14.61 / 14.62, p95 18 / 18 | 14.53 / 14.59, p95 18 / 18 | 14.59 / 14.56, p95 18 / 18 |
| gameplay03 none | 8.66 / 8.64, p95 14 / 14 | 8.78 / 8.40, p95 14 / 14 | 8.57 / 8.43, p95 14 / 14 |
| diorama02 all | 30.77 / 31.03, p95 36 / 40 | 30.95 / 31.26, p95 34 / 39 | 31.90 / 30.99, p95 36 / 39 |
| diorama02 none | 20.46 / 20.46, p95 23 / 22 | 21.32 / 20.69, p95 24 / 23 | 21.17 / 20.63, p95 23 / 23 |

The averages are within the spread everywhere; diorama02 has no hex chosen, so the branch draws a smaller panel. The
p95 at diorama02 `all` at DPR 2 was 3 to 5 ms higher on the branch in all three runs, so it was not noise.

**Bisect (DPR 2, diorama02 `all` p95, two interleaved rounds, main db99f3a first).** At the shot poses the event card is
closed, so variant (c) draws no visible filigree:

| Build | round 1 | round 2 |
|---|---|---|
| main | 36 | 35 |
| (a) the branch without the filigree | 36 | 36 |
| (b) only the info panel and refusal changes | 35 | 36 |
| (c) the filigree on the event card only | 35 | 36 |
| the branch (filigree on the date plate, info panel, minimap, card) | 39 | 39 |

**Cause.** The filigree drawn as Painter2D curves. An EditMode probe found no regeneration: over 120 idle frames, and
with the minimap's view box set every frame (still or moving), no `UiFrame` regenerated its mesh and no element got a
`GeometryChangedEvent`. So the cost is in drawing the curves' tessellated, antialiased geometry each frame, not in
rebuilding it.

**Fix.** `UiFrame.Ornament` rasterises the scrolls and the leaf once, in code, into a 64x64 alpha texture (two variants,
with and without the side scroll; no asset). Each corner draws one tinted, mirrored, textured quad, and the second rule
is four plain quads. The look is unchanged at 1920x1080. Re-run, DPR 2, interleaved, main db99f3a, the fix, then the old
branch (avg ms, then p95):

| Pose, config | round 1 | round 2 |
|---|---|---|
| diorama02 all | 30.78 / 30.61 / 31.29, p95 35 / 33 / 39 | 31.57 / 30.53 / 31.93, p95 36 / 34 / 38 |
| diorama02 none | 20.49 / 20.77 / 20.82, p95 23 / 24 / 23 | 21.35 / 20.35 / 21.09, p95 24 / 23 / 23 |
| gameplay03 all | 14.64 / 14.99 / 16.10, p95 18 / 23 / 23 | 14.51 / 14.53 / 14.45, p95 18 / 18 / 18 |
| gameplay03 none | 8.74 / 8.77 / 9.66, p95 14 / 14 / 17 | 8.64 / 8.67 / 8.37, p95 14 / 14 / 14 |

The fix's diorama02 `all` p95 (33, 34) is back at or under main's (35, 36). In round 1, gameplay03 `all` was disturbed
for both branch builds in the same stretch: spreads 1.36 and 1.68 against main's 0.01, and the old branch's `none` was
up too. Round 2 is clean, with all three at p95 18. `golden&smoke=5` is unchanged on the fix build.

## HUD round 2, part 1: the minimap (#398)

The owner chose all three round-2 pieces (2026-10-14), one PR each: the minimap, the left icon column, the top bar. This is
the minimap, against promo 03's (T54: "the minimap is a pixelated hex strip").
- The picture is a bake, `tools/minimap_bake.py` -> `unity/Assets/Resources/Map/MinimapRelief.png` (256 x 1024, powers of two so it is block-compressed on the GPU,
  mipmapped, trilinear): a hill shade of the heightmap (heights averaged over 3 x 3 samples, x12, lit from the north-west)
  over the biome colour bake and the land use (softened, a third parchment), the sea from `coast.png` (dark navy, a lighter
  band within 6 km of the shore, a thin pale line at the edge), and land outside Mandatory Palestine greyed with a gold rim.
  It replaces the 128-pixel point-filtered strip that `MinimapPresenter.Paint` drew at boot, so boot loses that loop and
  nothing is drawn per frame. `tools/tests/test_minimap_bake.py` re-bakes it in CI and compares the pixels.
- Promo 03's three round buttons, beside the picture: zoom all the way out (the camera's `MaxDistance`, over the middle
  of the map), show or hide the settlement names (the `labels` switch), show or hide the borders and their caption (the
  `borders` switch). A switched-off layer dims its button. The picture stays mouse-only (accessibility audit B1d); the
  buttons take Tab after End Turn.
- The panel is 222 x 380 (was 160 x 340): the picture 133 x 356, the buttons 50 px.

Measured on the WebGL `?shot` frames at 1920x1080 against promo 03's minimap (T54's definitions; the picture resized to
300 px tall first; land is every pixel that is not blue-dominant):

| | promo 03 | main gameplay03 | branch gameplay03 | branch diorama02 |
|---|---|---|---|---|
| panel, share of the frame | 4.97% | 2.62% | 4.07% | 4.07% |
| picture, share of the frame | 3.11% | 1.80% | 2.30% | 2.30% |
| land luma, contrast (std) | 0.467, 0.109 | 0.609, 0.078 | 0.492, 0.098 | 0.497, 0.106 |
| land p10-p90 | 0.260 | 0.163 | 0.259 | 0.271 |
| land relief (mean abs Laplacian) | 0.063 | 0.024 | 0.069 | 0.074 |
| land saturation | 0.327 | 0.353 | 0.277 | 0.275 |
| dE to promo: land, sea | 0, 0 | 17.2, 27.4 | 3.3, 2.4 | 3.7, 2.6 |

The picture is still taller and narrower than promo 03's, which crops the country; the minimap shows all of it. The
promo's region tints (orange and blue by side) are not in the bake: the two-colour borders (#555) change by date.

Frame time, `?vprofile` headless 1920x1080 DPR 1, and boot (`headed.py boot --runs 3`), one `machine_lock.py
--exclusive` slot, main df17ac6f first, then the branch (median avg ms, spread):

| | main | branch |
|---|---|---|
| gameplay03 all / none | 8.39 (0.09) / 7.18 (0.10) | 8.38 (0.06) / 7.11 (0.03) |
| diorama02 all / none | 15.96 (0.48) / 11.41 (0.09) | 16.52 (0.73) / 11.99 (0.10) |
| boot map_build / first_frame, cpu 1 | 565 / 3924 ms | 563 / 3917 ms |

That first pair was not interleaved. The re-run (one `--exclusive` slot, 10 minutes; both builds with a temporary,
uncommitted FrameProfile patch that measures only `all` and `none`, in the order base, branch, branch, base; two
diagnostic branch builds; a headed DPR 2 pair) shows no cost:

| run (order) | diorama02 all | diorama02 none | gameplay03 all | gameplay03 none |
|---|---|---|---|---|
| 1 main | 16.64 (0.48) | 11.83 (0.29) | 8.60 (0.61) | 7.12 (0.28) |
| 2 branch | 16.46 (2.90) | 11.81 (1.21) | 9.92 (0.08) | 8.19 (0.09) |
| 3 branch | 15.59 (0.44) | 11.24 (0.07) | 8.37 (0.09) | 7.08 (0.05) |
| 4 main | 15.45 (0.33) | 11.23 (0.06) | 8.25 (0.07) | 7.07 (0.09) |
| 5 branch, picture without mipmaps | 15.39 (0.42) | 11.20 (0.06) | 8.30 (0.03) | 7.01 (0.03) |
| 6 branch, main's 160 x 340 panel, no buttons | 15.42 (0.37) | 11.19 (0.15) | 8.30 (0.07) | 7.07 (0.02) |
| DPR 2 headed, main | 33.73 (0.15) | 27.90 (0.15) | 19.71 (0.94) | 18.33 (0.56) |
| DPR 2 headed, branch | 33.49 (0.05) | 27.86 (0.10) | 19.34 (0.29) | 17.81 (0.02) |

Runs in the same position agree within 0.1 ms (1 and 2; 3 and 4), and the 0.6 ms step follows the position in the slot,
not the build: the machine was slower early in the slot (run 2's gameplay03 is the same drift). The variants that remove
the mipmaps or the larger panel and its buttons cost the same as the branch. The first pair's +0.56 ms was this drift.

## HUD round 2, part 2: the left icon column (#398)

Promo 03 and T29 have a column of round icons at the left edge under the bar. This step moves the four screens' buttons
(Cabinet, Budget, Defence, Why?) there from the top bar. It adds no action and no second copy of a button.
- Icon-only, as in the promo. Each button keeps its name as its text, because the event log and the strings read it, but
  draws it clear; the name is also its tooltip, and `HoverTip` (from #584) shows it beside the disc on hover and on focus.
- The column comes before the top bar in Game.uxml, so Tab still runs Cabinet, Budget, Defence, Why?, New Game, End
  Turn. It is covered where the top bar is (the start screen, an outcome clip) and stays usable under the decision card,
  as the top bar does: the card's backdrop starts at its right edge. The card stays centred in the window.
- The screens, the wide screens, the narrow HUD's full-width screens and the toast start right of the column (84 px), so
  a screen never hides the button that closes it. The toast is 460 px wide (was 520), to stay clear of the screens.
- The Hebrew UI mirrors rows, not the screen's corners (#550): the column stays at the left, as the minimap stays at the
  right and the info panel at the left.
- The top bar now holds New Game and End Turn. Its height and its tiers are part 3.

Measured on the WebGL `?shot` frames at 1920x1080. The discs are found as the navy fill in the left 5% of the frame,
between 7% and 47% of its height:

| frame | discs | disc width, share of height | pitch | left edge, share of width | first disc's top |
|---|---|---|---|---|---|
| promo 03 | 5 | 5.21% | 6.32% | 0.87% | 12.1% |
| demo2 T29 | 5 | 5.56% | 6.30% | 0.31% | 12.1% |
| branch gameplay03 (EN, HE, diorama02 the same) | 4 | 4.72% | 6.30% | 0.94% | 10.8% |

Frame time (`?vprofile` with the same temporary all/none-only FrameProfile patch on both builds, headless 1920x1080, then a
headed DPR 2 pair) and boot (`headed.py boot --runs 3`), one `--exclusive` slot of 8 minutes, interleaved base, branch,
branch, base. Base is #584's head 916cbad. Median avg ms (spread):

| | base | branch | branch | base |
|---|---|---|---|---|
| gameplay03 all | 8.16 (0.05) | 8.21 (0.08) | 8.18 (0.01) | 8.21 (0.04) |
| gameplay03 none | 7.02 (0.04) | 6.97 (0.04) | 7.02 (0.03) | 6.98 (0.03) |
| diorama02 all | 15.49 (0.40) | 15.55 (0.43) | 15.61 (0.36) | 15.46 (0.43) |
| diorama02 none | 11.08 (0.08) | 10.73 (0.09) | 10.74 (0.17) | 11.16 (0.05) |
| boot map_build / first_frame, cpu 1 | 533 / 3877 ms | 530 / 3866 ms | 532 / 3862 ms | 528 / 3852 ms |

DPR 2 headed, base then branch (spread): gameplay03 all 19.14 (0.29) / 19.09 (0.19), none 17.81 (0.03) / 17.84 (0.05);
diorama02 all 33.79 (0.21) / 33.81 (0.25), none 27.91 (0.04) / 27.87 (0.01). The column costs nothing measurable.
Diorama02 `none` is 0.35-0.4 ms lower on the branch in both positions, outside the spreads; at DPR 2 the same cell is
equal (27.91 / 27.87). It is not explained (the branch draws four fewer captioned buttons in the bar, but that is not
measured on its own), so it is not claimed as a gain.

## Issue #391: the terrain shader's frame-time cliff (warm land shade, not shipped)
#391 asked for the shaded hill ground to read warm brown, as promo 03's does (about (67, 53, 46)), rather than olive-grey.
The look was solved; the cost was not, so nothing shipped. What was measured is a constraint on any future addition to
`StatecraftTerrain.hlsl`.

**Diagnosis (main, editor, Mobile tier).** On bare terrain (kits, trees, routes, borders and tilt-shift off) the shaded
ground at `hills12a` (35.05, 31.76, 12 km) measured (71, 62, 55): hue 27 degrees, G/R 0.87. Most of the olive cast in the
full frame comes from tree crowns and kit shadows, which belong to vegetation round 2 (#34). The grade's split toning
cannot fix the ground's part: every shadow-colour candidate also moved the sea at gameplay03 by 4-18/255.

**The look that worked.** Scaling green by 0.87 and blue by 0.76 against red, weighted by 1 - smoothstep(0, 1, N.L) for the
fixed sun, gave on bare terrain: hue 24.0 / 25.7 / 27.0 degrees and G/R 0.845 / 0.847 / 0.838 at hills12a, judea30 and
gameplay03, sea drift 0.00-0.01/255, palette distance 0.463 -> 0.454 (gameplay03) and 0.581 -> 0.558 (diorama02), and
WebGL diorama02 warmth 0.121 -> 0.127. Baked into `terrain_bake.png`'s R (unread at run time) it cost 120 KB of
`data.unity3d` under LZ4.

**The cost.** Headless `?vprofile`, 1920x1080, DPR 1, interleaved under `--exclusive`, median avg ms, main `7040351`:

| Shader change | `all` gameplay03 | `none` gameplay03 | `all` diorama02 | `none` diorama02 |
|---|---|---|---|---|
| main | 8.23-8.42 | 4.98-5.07 | 19.3-21.6 | 12.6-13.2 |
| no-op function call (control) | 8.05 | 4.86 | 22.55 | 14.67 |
| `GetMainLight()`, dot, smoothstep, lerp | 14.96 | 13.23 | 21.4 | 16.8 |
| `_MainLightPosition` uniform, dot, smoothstep, lerp | 12.59 | 11.71 | 25.26 | 17.46 |
| baked mask: `bake.r` kept, one constant lerp | 14.87 | 12.55 | 22.09 | 17.35 |
| baked mask used at once, AO sampled after it | 16.72 | 13.72 | 21.22 | 18.06 |

Every variant that adds work to the function costs 4-8 ms at gameplay03, and as much with every visual toggle off as on:
the toggles do not remove shader code. The no-op control matches main, so the build and the call itself are free.

**Constraint.** Treat the terrain fragment shader as at a cliff: one extra lerp of a value already in a register cost as
much as per-pixel lighting. The likely cause is register pressure or occupancy in the WebGL (ANGLE/Metal) compile of the
whole function, not the arithmetic, as #195 found for the water (3 ms from its size alone). Shortening a value's live
range did not help. Before adding anything to `StatecraftTerrain.hlsl`, profile it as above against main; prefer changing
baked data the shader already reads in its existing operations (land colours in `tools/land_bake.py`, `MapPalette`), or
moving work out of the function as #195 did.

## Issue #321: boot headroom, the biome colours from a bake

The budget is `map_build_ms` at `--cpu 4` within 2720 ms (Task 1's 1813 ms plus 50%). After #502's hill scatter, main
measured about 30 ms under it on a quiet machine and over it under load.

**Where the map build goes.** Main `72e2aee`, `tools/headed.py boot --runs 3 --cpu 4`, two rounds under one
`machine_lock --exclusive` (machine load 4.5-7), median dt of each phase in ms. The branch adds three `?bootlog` markers
(`terrain object created`, `units placed`, `kit buildings built`) that split the old phases, so its rows are the same
work in finer pieces.

| phase | main | branch | what it is |
|---|---|---|---|
| heightmap decoded | 190-193 | 190-192 | r16z decode (counted from its start) |
| hexes parsed | 12 | 10-12 | |
| localities parsed | 81-83 | 78-79 | the baked TSV (#291) |
| terrain heights set | 1386-1395 | 1390-1400 | `new TerrainData` (about 195 ms, #320's diag) and `SetHeights` |
| terrain object created | (in the next row) | 82-88 | `Terrain.CreateTerrainGameObject` (the collider) and the layer |
| biome texture baked | 438-442 | 38-41 | main: the blend of 1024 x 1024 texels; branch: the bake read, the tint, the upload |
| border skirt | 35-36 | 34-35 | |
| terrain water | 17-19 | 19-21 | the bake (#320) |
| settlements placed | 7 | 7-8 | |
| units placed | (in kits built) | 354-357 | route YAML read and corridors, footprints, unit data and spots |
| kit buildings built | (in kits built) | 253-260 | `SettlementKits.Build` and the kit meshes |
| kits built | 812-828 | 216-221 | main: all three rows above; branch: the trees (`Vegetation.Plant`, `VegetationLayer.Init`) |
| routes built | 47 | 44-46 | the ribbon bake (#436) |
| map built | 14-15 | 14 | |
| **map_build_ms** (median of 3) | **3050 / 3073** | **2745 / 2761** | |

The three largest: `terrain heights set` (about 1390 ms, 46%), the kits phase (about 820 ms: units placed 355, kit
buildings 255, trees 220) and the biome texture (about 440 ms). `first_frame_ms`: main 11225 / 11162, branch
10821 / 10929.

**The change.** `MapBuilder.BiomePixels` (the BiomeBlender weights of every texel over `MapPalette.Ground`) is baked by
Editor `BiomeColourBake` into `unity/Assets/Resources/Map/BiomeColours.bytes` (3 MB raw, 71 KB under Brotli -q 11, and
155 KB more `.data` in a `-compression disabled` build, whose `.data` is LZ4: "SCB1", version, resolution, a
check of the frame, hex size, blend radius, palette and every hex's centre and biome, then RGB). `MapScene` reads it
through `BiomeColourFile.Load`; a bake for other inputs is refused with a warning and the colours are blended at load as
before. The ground tint, the texture and its mipmaps are made as before, from the same texels:
`BiomeColourBakeTests.TheMapGetsTheSameGroundFromTheBake` compares every texel handed to the tint with and without the
bake. `tools/tests/test_biome_colour_bake.py` recomputes the header's check in CI, re-blends the texels with a numpy
port (within 1 per channel: 7 of 3,145,728 channels differ by 1, at x.5 roundings) and pins a fingerprint of the C# that
makes them, so an edit to the blend fails CI until it is re-baked (review of #529). Re-bake with
`-executeMethod Statecraft.Game.EditorTools.BiomeColourBake.Bake` after changing `hexes.json`, the heightmap's bounds,
the palette or the blend.

The biome phase went from about 440 ms to 120 ms (82-88 terrain object, 38-41 the rest); the map build from 3050-3073 to
2745-2761 ms, about 310 ms less. At this load both are over 2720 ms; on the quieter machine where main measured about
2690 ms, the same cut leaves about 340 ms of headroom.

**What is left, by expected saving at `--cpu 4`:**
- `terrain heights set` (about 1390 ms): ship the TerrainData as an asset instead of `SetHeights` at boot. Up to about
  1.2 s, but the riskiest: the asset must be copied before the map owns and destroys it, its LOD data must load without
  a recompute, and the build grows by about 8 MB before compression. Prototype and measure before planning further.
- `units placed` (about 355 ms): not yet split. Likely the two YAML reads (route paths, units) and the unit spots; a
  baked runtime form, as #291 did for the localities, would save most of the parse. Low to medium risk, no visible
  change.
- The kits (about 255 + 220 ms): bake the kit meshes and tree matrices from their inputs (settlements, localities,
  population, routes, unit clearings, the `?kitscale` flags fall back). About 300-400 ms, medium risk (many inputs).
  Without a bake, the hill scatter's per-cell checks could be reordered for 10-20 ms with the same trees.
- `heightmap decoded` (about 190 ms): smaller, untested.

### #321 round 2: the units' step (T47)

The `units placed` span #529 uncovered, split by four more `?bootlog` markers. Branch build with the markers only, one
`--exclusive` run of 3 cold loads at `--cpu 4`, median ms: `route paths read` 127, `route corridors` 8, `footprints` 3,
`unit data read` 51, `units placed` (now the unit spots alone: `UnitGround` and `UnitPlacer.PlaceAll`) 184.

- **Route paths** (127 ms): YamlDotNet over route_paths_1948.yaml's 1,544 points. `tools/route_paths_bake.py` writes
  `content/map/route_paths_1948.tsv` (29 KB: one row per route, its points in one field, the version, the YAML's FNV-1a
  check, the counts of routes and points, and a last `body_check` row, FNV-1a over the route rows), and
  `RoutePaths.ParseRuntime` reads it with the YAML reader's checks. The YAML still ships (WebGL now fetches both: 29 KB
  more). A runtime file that is missing, of another version, made from another YAML, cut anywhere (even inside a
  number: the review of #547 found that version 1's counts missed that case, so version 2 adds `body_check`) or edited
  by hand is refused with a warning and the YAML is read. CI: `test_route_paths_bake.py` fails a stale file and pins a fingerprint
  of the C# readers; Unity's `RoutePathsRuntimeTests` compares every route and point bit for bit.
- **Unit spots** (184 ms): `UnitPlacer.PlaceAll` made every hex's candidate spots (19 a hex in the compact rounds, with
  their trigonometry) for each anchor and round, then dropped all but the seven hexes within its one-hex limit. It now
  lists those seven first and makes only their candidates, in the same order. `UnitPlacerSpeedTests` keeps the old
  algorithm verbatim and gets exactly its spots and skips on four grounds, from all clear to none clear.
- **Not changed:** `unit data read` (about 35-51 ms; the unit YAML is also read again by `GameRoot`), the corridors and
  the footprints.

Base `abe2263` against the branch, ABBA under one `--exclusive` hold (machine load 3.7-4.6), median of 3 each:

| | base round 1 | branch round 1 | branch round 2 | base round 2 |
|---|---|---|---|---|
| `map_build_ms` | 2507 | 2214 | 2196 | 2379 |
| units' span (base: `units placed`; branch: its five phases) | 315 | 90 | 91 | 291 |
| `start screen shown` | 576 | 427 | 426 | 528 |
| `first_frame_ms` | 9165 | 8249 | 8217 | 8351 |

The units' span goes from about 300 to 90 ms. The map build is at least 165 ms shorter: the closest base and branch
rounds differ by 165 ms, and the two base rounds spread by 128 ms, so the mean difference (about 240 ms) is not a firm
figure.
`start screen shown` falls by about 120 ms, since the units layer places the tokens again there.

### #321 round 3: the units file parsed once (T52)

`units_1948.yaml` was parsed at boot by the map (`unit data read`, for the unit spots) and again by `GameRoot` for the
units layer. Three `?bootlog` markers round the layer (`screens wired`, `units layer data read`, `units layer built`),
one `--exclusive` run of 3 cold loads at `--cpu 4` on main `4875d0e` plus the markers, median ms: the map's parse 31,
the layer's parse 25, the layer's build 62 (its own run of the unit placer included), and `screens wired` 272.

The map now keeps its parse (`MapScene.UnitEntries`) and `GameRoot` takes it through `MapScene.UnitEntriesFor`, which
hands back the same list when the text is the one the map read and parses otherwise. `UnitDataOnceTests` checks that
the layer's list is the map's (it failed with the old second parse), that it holds the same units as a fresh parse,
field for field, and that a rebuild or other text never hands on a stale parse. New Game replaces only the session;
the units are content, read once for the layer's life as before.

Base `4875d0e` against the branch, base, branch, branch, base under one `--exclusive` hold (load 2.8-11.9; the first
base round stalled after one cold load when another process loaded the machine, so the base has four loads):

| | base | branch round 1 | branch round 2 |
|---|---|---|---|
| `units layer data read` | (25, the split) | 1 | 1 |
| map built to start screen shown, per load | 437, 444, 445, 456 | 417, 422, 434 | 415, 416, 419 |
| `map_build_ms` (median) | 2221 | 2206 | 2214 |

About 25 ms off the time from the map to the start screen; the map build is unchanged, since the parse that stays is the
map's. Not done: the layer's second run of the unit placer (about 40 ms of `units layer built`) could take the map's
spots too, and `screens wired` (about 270 ms) is not split yet.
