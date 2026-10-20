# One-building LOD experiment — rejected candidate, draft report

**Result: do not adopt the distant card.** The matched gameplay capture loses the lower wall and its two windows
at the terrain boundary. The independent reviewer rejected it at the exact baked camera. The close pair is
pixel-identical because it uses the original mesh. This deliverable preserves a reproducible negative result;
it demonstrates no safe quality improvement and enables no production replacement.

This is a bounded investigation of the wall/window losses documented in [PR #1153](https://github.com/eyalgolan/statecraft-israel/pull/1153).
It does not replace a production building, change `HexModels`, or complete WP10's whole-board performance requirement.

- [x] **TASK-000** library check: local library dashboard unavailable; reuse the repo's Blender decimator,
  cleared building, hex placement, camera, materials and terrain tooling. No new dependency, paid tool or model upload.
- [x] **TASK-001** inspect source topology and try a conservative reduction without changing the source.
- [x] **TASK-002** implement an opt-in single-building colour impostor, retaining original near and shadow geometry.
- [x] **TASK-003** focused Unity validation, matched captures and independent review; candidate rejected.

## Source and rejected mesh candidate

`b05a-moshava-house-v2` is already admitted by the existing `ASSETS.yaml` building rule (PR #507 / #659).
The FBX, its UVs, material, texture, metadata and existing licensing records are preserved byte for byte.
The asset is already a reduced shipped model; this experiment has no access to its high-resolution master.

Source FBX SHA-256: `48529542a9036dbf8fb1ea740fe462155501e21cb7430f566418c6df389ccec0`.

Blender 4.4.0 inspection, `tools/building_lod_probe.py`:

| Property | Measured |
| --- | ---: |
| Triangles | 6,000 |
| Vertices | 4,975 |
| UV layers / material slots | 1 / 1 |
| Custom normals | present |
| Vertices after inspection-only weld at 0.000001 file units | 4,970 |
| Boundary/non-manifold edges after weld | 3,356 |
| UV seam edges after weld | 3,985 |
| Vertices touching boundary or seam | 4,967 |

The existing `slim_collection.py` uses weld plus collapse decimation. It is for collection figures/vehicles/towns,
not these buildings; forcing its cap onto this fragmented surface would repeat the topology risk described in #1153.
The local sandbox importer refers to another untracked asset-staging decimator; that tool/source kit is not available
in this worktree and is not a cleared input to this experiment.

Blender's planar dissolve at 5 degrees, preserving UV, normal, sharp, seam and material delimiters and keeping
boundary dissolution disabled, left **5,988 triangles**. The 12-triangle reduction (0.2%) is rejected as negligible.
The script records that result; it does not export or adopt the candidate. These topology counts explain the
constraint but do not by themselves establish which surfaces are visually sound.

## Distance candidate

The isolated fixture uses the `tel_aviv` board's existing moshava hex **(-3,21)**, seed 1948, and the first
`sandbox:kit_moshava` placement emitted by its real prototype. It instantiates the cleared b05a asset explicitly,
without depending on the sandbox alias or changing any loader. Terrain comes from the existing `HexBoardSurface`
and `HexBoardBuilder`. Missing local ground textures use their flat-colour fallback.

At the captured distant camera, a locally baked transparent quad supplies the colour pass. It attempts to retain the
projected appearance without editing topology or UVs, but the flat card fails at the terrain/depth boundary: the
lower wall and both lower windows disappear. This is consistent with the card intersecting the ground, rather than
mesh simplification damage. The original renderer continues casting the
original shadow. Close view draws the original mesh. The temporary image, quad, material and volume components
belong to the fixture root and are destroyed with it; no generated prefab/image enters Resources or the game build.

This is deliberately a **single-view, fixed-light prototype**. The component defaults off. The experiment menu is
the only creator. Eligibility requires the source transform, perspective camera direction/rotation (0.1 degree),
baked distance (within 1%, also beyond 10,000 units), sun and ambient colours to match. Other views fall back to
the original. Shadow modes other than On/Off also fall back. It is not an octahedral or multi-angle impostor system.
Additional cameras, changed materials, animated geometry, external occluders, night lighting and varied projections
have not been validated. A transparent card has no solid depth and can sort incorrectly with other transparent objects.

| Building work | Original | Distant candidate |
| --- | ---: | ---: |
| Colour triangles | 6,000 | 2 |
| Colour submeshes / materials / expected submissions | 1 / 1 / 1 | 1 / 1 / 1 |
| Shadow geometry | original 6,000 triangles | same original 6,000 triangles |
| Shadow submissions | determined by the active URP shadow passes | unchanged |
| Total resident building representations | source mesh/material/512 px source texture | source plus temporary quad/unlit material/baked colour texture |

**No draw-call reduction or frame-time benefit is demonstrated.** Manual `Camera.Render` counters are recorded only
as whole-scene editor observations; they do not isolate this building or constitute a steady-state player benchmark.
Alpha blending, sampling, overdraw, retained shadows and switching can outweigh the colour-geometry saving.

## Reproduction and acceptance limits

Use Unity 6000.3.25f1. Before running Unity, inspect processes and acquire the shared atomic
`/tmp/statecraft-unity-validation.lock`, then use the repo's machine lock as below. Do not run beside other workers.

```sh
Blender --background --threads 2 --python tools/building_lod_probe.py
uv run tools/machine_lock.py --shared --wait-unity -- "$UNITY" \
  -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -projectPath unity \
  -buildTarget OSXUniversal -runTests -testPlatform EditMode \
  -testFilter BuildingLodProbeTests -testResults unity-building-lod.xml -logFile unity-building-lod.log
uv run tools/machine_lock.py --shared --wait-unity -- "$UNITY" \
  -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity \
  -buildTarget OSXUniversal -executeMethod Statecraft.HexTiles.Capture.BuildingLodCapture.Run \
  -logFile unity-building-lod-capture.log
```

The capture method runs only in batch mode and opens a fresh empty scene. Interactive use is through
**Statecraft → Hex Tiles → Experiments → Preview One Building LOD**, which first offers to save modified scenes.
`Create` refuses existing renderers/lights/volumes, preventing contaminated comparisons.

Each before/after pair keeps the same source transform, real hex, terrain, daylight, camera pitch 50 degrees,
yaw 0, FOV 35 and resolution 1920×1080. Gameplay distance is 12,500; close distance is 3,000. The close-after shot
must use the original automatically. Output is under ignored `captures/building-lod/`.

Validation and captured-image assessment are recorded below. This draft records a rejected experiment,
not a production performance improvement. No simulation/content or golden hash changes are intended.

## Validation status

- Focused audit tests: **6 passed**. The initial test collection failed because the audit module did not yet exist.
- Markdown-link policy tests: **8 passed**.
- Repository policy checker: clean over **2,774 tracked files**, including staged prototype/report files.
- Isolated Roslyn 4.8 compile of the runtime component, capture tool and test file against cached Unity 6000.3.25f1
  API references: **pass, no warnings/errors**. Runtime-only compile without `UNITY_EDITOR`: also pass.
  These checks do not execute Unity, test the importer, build a player or validate a shader variant.
- Independent corrective code review: no remaining blocking findings; see [review](evidence/building-lod/review.md).
- Native Unity EditMode tests: **21/21 passed, 0 skipped, 0 C# warnings/errors**, Unity 6000.3.25f1,
  `-buildTarget OSXUniversal -testFilter BuildingLodProbeTests`; [per-case summary](evidence/building-lod/native-tests.json).
  This includes source import/UV/material checks, a real bake, cleanup and shared render-state restoration.
- Matched captures: **completed**, Unity exit 0. Gameplay uses the card, close view automatically uses the original.
- Independent visual review: **reject**; missing lower wall/windows at the terrain boundary. See [review](evidence/building-lod/review.md).
- Pixel comparison: close pair identical; gameplay 11,449 changed pixels in bounding box (898,452)–(1020,586),
  mean absolute RGB delta 21.456 within that region on the 0–255 scale. Full-frame mean 0.169 is diluted by the
  unchanged ground and is not an acceptance metric. See [measurements](evidence/building-lod/pixel-comparison.json).
- Unity importer reports 11,300 vertices / 6,000 triangles / 1 submesh / 1 material slot for the source;
  Blender's 4,975 vertices count is before Unity's import splitting. The card is 4 vertices / 2 triangles /
  1 submesh / 1 material slot, with an additional 264×369 RGBA32 texture (389,664 bytes base pixels, no mipmaps).
  The original shadow geometry remains. Manual Editor draw-call counters returned 0 in every shot and are unusable;
  actual draw calls and frame cost remain unmeasured.
- Post-capture reporting correction: the logger now identifies only the card's additional texture. The captured
  run mistakenly also labelled the source's 512×512 texture as an impostor; no source format/mipmap inference is made.
  This one-line reporting correction was compile-checked; native tests/captures preceded it.
- Full sim/tools/policy suites and a standalone/WebGL player build: not run for this bounded presentation experiment.
