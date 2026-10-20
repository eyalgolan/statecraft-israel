# One-hex construction reveal prototype

Opt-in presentation prototype. No gameplay flow invokes it; simulation timing, picking and completion identity remain unchanged.

## Direction

Three lightweight directions were considered:

1. **Perimeter**: low soft dust ring, a few earth chunks, intact building rises and settles. Selected for a readable silhouette and short reveal.
2. **Staggered**: dust groups and rise start later, suggesting assembly without splitting existing art. Available as a timing preset.
3. **Cloud**: wider expanding dust, crisp reveal. Available as a restrained intensity/width preset, at the cost of more cover at the base.

The refined default retains the original dust material and radial softness. Eight small faceted stones pop out in short arcs,
make two diminishing bounces, then shrink away. A smaller completion dust pulse and gentle building settle connect the action.
The comparison selector preserves the original v1 sampling on the **same real game hex**; it does not substitute the old diagnostic polygon.

## Preview

Open this worktree's `unity/` with **Unity 6000.3.25f1**, then **Statecraft > Construction > One Hex Preview**.
No Map scene, simulation, network or full board is started. The tool builds the existing `central_israel` hex at q=1, r=13
(`moshava_village`) using `HexBoardBuilder`, `HexBoardSurface`, `HexPrototypes` and `HexBoardRenderer`.
Its existing terrain paint, main street and prop placements are retained. Immediate neighbouring ground descriptors preserve
terrain continuity; only the selected hex's props are drawn. One existing roofed `sandbox:kit_moshava_2` house receives the reveal.
The game renderer owns the generated terrain/meshes/materials. Closing the private preview disposes them without changing the active scene.

Local game hex art remains in its existing ignored folders. It is not copied into Git or relicensed by this prototype.
When a local model is unavailable, the game's existing stand-in is shown and the preview reports the missing model count.
Ground textures are read without changing importers. The preview uses the game sun, camera pitch and FOV, without the global post-processing profile.

**Play / Replay** restarts; **Stop / Final** restores the completed model. Comparison (**Original / Stones**), direction,
duration, dust intensity, visual seed, speed and scrub support iteration. **Gameplay zoom** uses the existing hex capture's
26,000 m mid distance; **Tile zoom** frames the composition; **Close zoom** inspects the selected building at 5,980 m.

Default duration is 1.8 seconds: anticipation through 0.22s, rise through about 0.92s, completion pulse and short settle,
then clear. These are local visual values, not construction rules or turn durations. Completion never gates game state.

## Integration boundary

PR [#1146](https://github.com/eyalgolan/statecraft-israel/pull/1146) owns completed-city-project presentation in
`GameRoot.RenderPlacementsIfChanged` and `PlacementLayer.Render`; hex-map PRs #1153–#1155 own their board builders.
This prototype changes none of those files. Other runtime-figure and building-LOD experiments remain separate.

After the completed-visual flow settles, diff stable placement/project IDs following authoritative state updates. On a newly
completed visual, put `ConstructionReveal` on its stationary pick root and create a direct mesh-only child pivot, then call
`Bind(pivot, conservativeWorldFootprintRadius)` and `Play()`. Preserve the existing mesh, material and final transform.
Set `GroundOffset` to surface height minus the placed building base when the game intentionally sinks that base.
Keep at least **2.25 × radius** inside the tile's inset boundary. Do not wrap colliders, picking metadata or the whole placement layer.
Initial load, save restore, camera changes, streaming/LOD replacement and layer redraws must not replay completion effects;
keep completion identity independently of ephemeral render objects.

`Bind` restores the old target before reuse. `Cancel`, disabling, completion and destruction restore the final transform and clear particles.
Replay reuses its systems and owned resources. No coroutines, delayed callbacks, persistent objects, global RNG or global settings.
No project reduced-motion setting was found. Callers can skip `Play()` or call `Cancel()` to show the completed model immediately;
no global setting was invented.

## Rendering budget

Two native CPU ParticleSystems, at most **36 soft puffs + 8 mesh stones**. The stones share one eight-triangle mesh and one opaque material;
maximum added geometry is 136 triangles (72 dust + 64 stones), with two particle renderers. The original look uses 88 billboard triangles.
No rigidbodies, collision, persistent debris, trails, lights, sub-emitters, compute shaders, VFX Graph, new packages or textures.
The tiny Resources materials retain their URP shaders. Dust alpha blends with depth testing and no depth writes; stones depth-write.
Particles use Ignore Raycast and no colliders. Only the visual pivot rises; scale, pick root and collider remain fixed.
Sampling uses preallocated arrays and a local visual hash; it never consumes Unity's global RNG.

No managed allocations are introduced in sampled frames after initialization, by code inspection. GC/frame timing was not profiled.
Simultaneous completions still need an integration-level visibility/concurrency cap; this is a single-effect prototype.

## Validation and current evidence

Unity 6000.3.25f1 / URP, 20 October 2026:

- Focused native EditMode suite: **10 passed, 0 failed, 0 skipped**, 47.54 seconds; 0 C# compile warnings/errors.
  Includes all three deleted-particle-root/child recovery cases, whole-timeline bounds for every style/look,
  repeatability without global RNG consumption, stationary picking, replay/cancel/disable/completion/rebind/destruction,
  real game terrain and repeated private-preview disposal with scene/ambient/render-target/resource preservation.
- After the final roofed-house selector change, the native preview tests ran again: **2 passed, 0 failed, 0 skipped**,
  29.33 seconds; 0 C# compile warnings/errors. Runtime code is unchanged from the full 10-case run.
- Native red first: 10 cases, 5 passed / 5 failed. Three failures reproduced the material leak and destroyed child accesses.
  Two failures caught an outer-row house whose conservative effect footprint did not fit the inset boundary.
  Recovery now rebuilds missing systems and renderer configuration while releasing the old dust material; the preview
  selects an existing roofed inner house without moving/resizing placements or weakening the containment guard.
- An independent Sol 6.1 reviewer verified the red/green XML and final code. No remaining code findings.
  The logs contain existing licensing/MCP shutdown noise; this is not an assertion that the entire log is error-free.
- .NET SDK 10.0.401: sim/content **2,023 passed**; Release warnings-as-errors build **0 warnings, 0 errors**.
  Policy tests **305 passed**; repository policy, GDD lint, rule-ID checks and whitespace checks passed.
- The CI-pinned Rasterio 1.5.2 could not install here without GDAL build tools. A compatible Rasterio 1.4.3 tools run had
  1,430 passed / 4 SDK-8-related failures / 4 skipped; all 10 packaging tests passed when rerun directly with SDK 10.
  This is not a clean validation of the exact CI dependency environment.

The final actual real-hex capture completed on the existing roofed `kit_moshava_2`: 269 PNGs at 960×720/30 fps;
36 placements, zero missing models, 44 live particles observed at peak. The matched 6.4-second video includes normal
and close views. Cleared final frames are pixel-identical between looks. An independent Sol 6.1 reviewer inspected
the rise, dust, stone arcs and final frames: no remaining blockers, with the gameplay-scale subtlety noted.
The close view is retained so the restrained stones remain assessable. This is sampled visual review, not frame-time profiling.

| Final v2 (roofed source house) | Library ID | File ID |
| --- | --- | --- |
| construction-real-hex-v2.mp4 | libfile_562d7963c2b88191a408aa6b3873e271 | file_00000000aa2881f58e0e365c99236225 |
| construction-real-hex-v2.gif | libfile_8131a21a52788191bb9e7ca8283df687 | file_00000000687c81f58b0d0828ad4de6ae |
| construction-real-hex-keyframes.png | libfile_e82df7eb21848191b7d98fd95a6864bb | file_00000000a05881f5a0f7bb666b738b49 |
| construction-normal-comparison.png | libfile_85dd719f1d448191b756fe8c62839701 | file_00000000410881f5bbf86a34afca60f4 |
| construction-close-comparison.png | libfile_bcebb3fe246c8191b7620b7e88a13b7d | file_00000000af5481f5989afcd17dcd3749 |
| construction-real-hex-final.png | libfile_1486918ee84c8191b5a1bdda868fddd8 | file_00000000281881f5a158da05b02ecb07 |
| construction-final-identity.png | libfile_a5a0a99221dc81918aafc8f1d73763f5 | file_000000004c0881f59e2b4292170685f5 |
| evidence.json | libfile_8da70ef2d5f481919dfe9ba6a6f2b4ee | file_0000000075f881f5b24d6904014ff9b6 |

The initial v2 capture below used an unroofed source shell. Review prompted the final roofed selection without moving
or resizing placements, changing runtime motion, or weakening containment. The two candidate files remain available
for traceability; use the final v2 table above for the current result.

| Initial v2 candidate (unroofed source house) | Library ID | File ID |
| --- | --- | --- |
| construction-v2-framehouse-candidate.mp4 | libfile_8b8280b9c64481918df9c6752944119b | file_00000000ce1081f5b3447c8d3f58be85 |
| construction-v2-framehouse-candidate-keyframes.png | libfile_0661e7824bc48191869e537170ae75ba | file_00000000c1dc81f5ac71f0d75dc8a8f2 |

These actual candidate captures establish the earlier motion on the real hex; they are not final evidence for the roofed-house selection.
The preserved v1 media below shows the earlier diagnostic hex.

A one-hex desktop URP editor capture is not WebGL/browser validation. No construction WebGL build or performance profile
was run. Public screenshot sharing remains subject to the existing local sandbox art's provenance status; no new asset
rights declaration or legal attestation is made here.

## Preserved v1 evidence

These files are the earlier diagnostic-hex exploration, retained unchanged. Use the refined real-hex evidence above for the current result.

Library deliverables (actual rendered footage; no generated imagery):

| File | Library ID | File ID |
| --- | --- | --- |
| construction-reveal.mp4 | libfile_538b931ebfe8819198c59e2c65cb770f | file_00000000589c820c95467dc7b8f13bdd |
| construction-reveal.gif | libfile_db774cc5cc6481919e49eda4c9ed8c2a | file_000000006d7881f9a294a8c91d55e9c9 |
| construction-keyframes.png | libfile_03051b07c4488191a82030cbb636f0f0 | file_00000000432881f58375610a7f041649 |
| construction-original-final.png | libfile_e1f7fbc7943881919166e7c92b1daef4 | file_00000000076c81fda047dee9eb7a5c78 |
| construction-directions.png | libfile_45515109bbd8819199f57bf8479f46e1 | file_000000005e8c823081d699fdd375cebc |


Capture entry point: `Statecraft.Editor.Construction.ConstructionPreview.Capture`, argument `-constructionCapture ABSOLUTE_DIRECTORY`.
Before Unity, inspect processes, atomically acquire `/tmp/statecraft-unity-validation.lock`, and wrap it in
`uv run tools/machine_lock.py --shared --wait-unity -- ...`. Do not launch competing Unity jobs.

AI assistance: Codex Sol 6.1 implemented the prototype, tests and notes; an independent Sol 6.1 agent verified the code,
native red/green XML and sampled final roofed-house capture sequence.
