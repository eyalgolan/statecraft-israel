# Visual v1: measurements, budgets and checkpoints

Plan: `docs/superpowers/plans/2026-10-11-statecraft-visual-v1.md`. Promo targets: `promo/final/03-gameplay-1936.png`,
`promo/final/02-map-diorama.png`. Poses: `PromoPose.Gameplay03` (34.86 E, 32.00 N, 60 km, yaw 10) and
`PromoPose.Diorama02` (35.05 E, 31.55 N, 250 km, yaw 0). Since #174 the camera keeps its view on the map, and centres
it east to west wherever the view is wider than the map. In the player, which runs that fit every frame, diorama02 can
shift by up to a few hundred metres from its pose. The controller accepted this (owner-delegated). The other poses do
not move.

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
`Sea`, `DepthOnly`, `DepthNormals`) returns it directly. It is driven by Task 5's coast distance: a turquoise -> teal -> navy
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
| Tilt-shift bottom band | unchanged (HalfWidth 0.13 / Feather 0.17) | same | final gameplay03 shot against `?notiltshift`, gradient energy per 1% row band (columns 30-80%): fully blurred (ratio <= 0.05-0.09) from under the bar to about 26% and from about 78-83% to the bottom, so the bottom band is 17-22% against the promo's ~15%. The 2-7 point gap is the one Task 3 offered the owner (bottom-only 0.16 / 0.19); no owner verdict yet, so not changed |
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
the region id, b the style) and sets the globals; `StatecraftBorders.hlsl`'s `ScBorderApply`, called last in the terrain
function, draws the band along each edge whose neighbour is in another region. A pixel farther from its hex's edges than
the widest band plus wash can reach fetches nothing (exact: a segment is never nearer than its line); one nearer fetches its
own hex and only the neighbours within reach (one, or two near a corner). The first version fetched all seven texels on
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
     the four share the red-orange band (the texel's style byte allows 255, but each new style needs colours that stay
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
  - Boats: the owner wants no civilian boats; navy ships are units, on another track.
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
  shadows on the water, and its bright tail is glints and foam lace.
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
Licence: the texture is `owner-verify` in `ASSETS.yaml` pending decision D13 (`docs/oss/DECISIONS.md`).
Still different from the promo: soft at close zoom (rows, hedges and bushes read at the gameplay zoom, blur at 15-20 km
and closer); terraces read faintly; the orchard ground and promo-art's groves follow separate rules; the promo's ground is warmer and browner under
its grade.

## Open items for the art track
Each item below was ruled to the art track (controller, rank 10). None blocks landing the branch.

1. **Border skirt look.** The skirt (the slab around the map) reads as a different material from the terrain next to it.
   The fix is the table or frame and the skirt in the terrain's own look (promo 02), not a colour patch. A patch,
   `BorderLandColour` (0.76, 0.66, 0.47), was measured and rejected: the south-west slab rendered (0.69, 0.58, 0.37) against
   the terrain's (0.82, 0.69, 0.43), so the step shrank, but the skirt is 17% of diorama02 and its saturation rose 0.34 ->
   0.47. That took the frame to saturation 0.488 against the 0.48 bar (warmth 0.129). Do not move the bar to exclude the
   skirt. The earlier line saying a lower-chroma colour is the next candidate is withdrawn.
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
     and there are no civilian boats (owner).
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
