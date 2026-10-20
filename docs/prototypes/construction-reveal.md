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
terrain continuity; only the selected hex's props are drawn. One existing `sandbox:kit_moshava` building receives the reveal.
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
- Native red first: 10 cases, 5 passed / 5 failed. Three failures reproduced the material leak and destroyed child accesses.
  Two failures caught an outer-row house whose conservative effect footprint did not fit the inset boundary.
  Recovery now rebuilds missing systems and renderer configuration while releasing the old dust material; the preview
  selects an existing inner farmhouse without moving/resizing placements or weakening the containment guard.
- An independent Sol 6.1 reviewer verified the red/green XML and final code. No remaining code findings.
  The logs contain existing licensing/MCP shutdown noise; this is not an assertion that the entire log is error-free.
- .NET SDK 10.0.401: sim/content **2,023 passed**; Release warnings-as-errors build **0 warnings, 0 errors**.
  Policy tests **305 passed**; repository policy, GDD lint, rule-ID checks and whitespace checks passed.
- The CI-pinned Rasterio 1.5.2 could not install here without GDAL build tools. A compatible Rasterio 1.4.3 tools run had
  1,430 passed / 4 SDK-8-related failures / 4 skipped; all 10 packaging tests passed when rerun directly with SDK 10.
  This is not a clean validation of the exact CI dependency environment.

Current real-hex video/keyframes and independent visual review are queued under the shared Unity locks.
The preserved v1 media below shows the earlier diagnostic hex and is not evidence for this refined real-hex stage.

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

AI assistance: Codex Sol 6.1 implemented the prototype, tests and notes; an independent Sol 6.1 agent verified the code and native red/green XML. Visual review is pending current captures.
