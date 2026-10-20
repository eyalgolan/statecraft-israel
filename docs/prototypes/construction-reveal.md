# One-hex construction reveal prototype

Local prototype, opt-in only. No gameplay flow currently invokes it. No simulation rules or content change.

- [x] **TASK-000** library check: no dashboard answered; reuse existing Unity building art and native particles, no new dependencies.
- [x] Inspect current main and active PRs; isolate this worktree.
- [x] Draft three directions; implement the perimeter direction with cheap timing presets.
- [x] Run focused Unity lifecycle tests and inspect actual animation captures.
- [x] Save animation, keyframes and comparison as native Library deliverables.

## Preview

Use Unity **6000.3.25f1**, open this worktree's `unity/`, then **Statecraft > Construction > One Hex Preview**.
The tool creates a private preview scene with the existing `b05a-moshava-house-v2` FBX and material, one hex of dry ground,
and warm directional light using the map's palette. It does not open Map.unity, build terrain, start the sim or request models.
Close the window to release its scene, objects, meshes, materials and render textures. The current scene is not modified.

Play / Replay restarts the reveal; Stop / Final restores the completed model. Scrub, duration, intensity, seed, speed and
three zoom buttons support iteration. The default **Perimeter** direction keeps dust below the model's main silhouette.
**Staggered** delays several puffs and the rise, suggesting short assembly. **Cloud** widens the puffs for a stronger reveal.
These are cheap parameter presets on the same system; Staggered does not disassemble the original model into pieces.

Default visual duration is 1.8 seconds: anticipation to 0.22s, rise through about 0.92s, restrained settle to 1.40s, then clear.
These are local presentation values, not construction rules or turn durations. No completion callback gates game state.

## Integration boundary (deliberately deferred)

PR [#1146](https://github.com/eyalgolan/statecraft-israel/pull/1146) owns completed-city-project presentation in
`GameRoot.RenderPlacementsIfChanged` and `PlacementLayer.Render`; hex-map PRs #1153–#1155 own their board builders.
This prototype changes none of those files.

When that work is settled, diff stable completed placement/project IDs after authoritative state updates. On a *newly completed*
visual only, add `ConstructionReveal` to its stationary pick root, create a direct mesh-only child pivot, then call
`Bind(pivot, conservativeWorldFootprintRadius)` and `Play()`. Keep the existing mesh/material and base-at-zero pivot.
Do not wrap colliders, picking metadata, or the whole placement layer. Supply a radius whose **2.25x** circle fits inside
the tile's inset boundary. Initial load, save restore, camera changes, streaming LOD replacement and layer redraws must not
replay completion effects. Preserve that completion identity independently of ephemeral render objects.

`Bind` restores the previous target before reuse. `Cancel`, disabling or destroying the component restore the final transform
and clear particles. Replays reuse two particle systems and one material. Destroying the host releases owned resources.
There are no coroutines, delayed callbacks, persistent objects, global RNG consumption or global settings changes.
No project reduced-motion preference was found; callers can skip `Play()` or call `Cancel()` to show the final model immediately.
No global accessibility setting was added.

## Rendering and budget

Two native CPU ParticleSystems: at most 36 base puffs + 8 debris flecks (88 billboard triangles), two transparent draws.
No collision, trails, lights, sub-emitters, sorting layers, compute shaders, VFX Graph, packages or added texture downloads.
A tiny URP transparent shader computes a soft radial falloff; its Resources material retains the shader in player builds.
It uses alpha blending and depth testing with no depth writes. Particles are on Ignore Raycast and have no colliders.
Only the mesh-only visual pivot rises through the terrain; its original scale stays intact. The pick root stays fixed. Visual hash sampling is independent of Unity's global RNG.
The reveal reuses its arrays and creates no managed objects per sampled frame after initialization; GC/frame timing was not profiled. Simultaneous completions still need an integration-level
visibility/concurrency cap; this prototype validates one effect, not a whole-map completion storm.

## Validation and evidence

Unity 6000.3.25f1 / URP, 20 October 2026:

- `ConstructionRevealTests;ConstructionPreviewTests`: **5 passed, 0 failed**, 0 `warning CS`, 0 `error CS`.
  Tests cover repeatability, particle bounds, stable pick-root transforms/collider, replay reuse, cancellation, disable,
  completion, rebind, material release, component destruction, repeated preview disposal, active-scene/ambient preservation
  and render-target restoration. The initial lifecycle run caught edit-time disable/destroy cleanup; the fix uses
  `ExecuteAlways` for cleanup, while automatic time advancement remains Play Mode only.
- Baseline presence test against the existing game assembly failed with “a standalone construction reveal must exist”.
- Actual Unity capture: 960×720 at 30 fps; packaged video has 152 frames, 5.07 seconds, tile and wider gameplay views.
  Peak observed: **44 live particles**, matching the 36 + 8 hard budget. Dust remains below the main silhouette.
- Original still / final cleared still: **pixel-identical**. The model's proportions/material/identity are preserved.
- Exact simulation dependency built successfully; `uv run scripts/oss/check_repo.py` and `git diff --check` passed.
- Existing Unity-MCP plugin logs a path-with-spaces message and shutdown warnings; these are unrelated to the prototype.
  Capture uses a worktree-local pending marker and resumes after assembly reload, then disposes its preview before exiting.

Library deliverables (actual rendered footage; no generated imagery):

| File | Library ID | File ID |
| --- | --- | --- |
| construction-reveal.mp4 | libfile_538b931ebfe8819198c59e2c65cb770f | file_00000000589c820c95467dc7b8f13bdd |
| construction-reveal.gif | libfile_db774cc5cc6481919e49eda4c9ed8c2a | file_000000006d7881f9a294a8c91d55e9c9 |
| construction-keyframes.png | libfile_03051b07c4488191a82030cbb636f0f0 | file_00000000432881f58375610a7f041649 |
| construction-original-final.png | libfile_e1f7fbc7943881919166e7c92b1daef4 | file_00000000076c81fda047dee9eb7a5c78 |
| construction-directions.png | libfile_45515109bbd8819199f57bf8479f46e1 | file_000000005e8c823081d699fdd375cebc |

A one-hex editor render is desktop URP evidence; WebGL/browser compatibility is a design target, **not a validated platform result**.
Before a PR, run the repository's full required checks and an actual WebGL capture. This task intentionally uses bounded checks.

Capture entry point: `Statecraft.Editor.Construction.ConstructionPreview.Capture`, argument `-constructionCapture ABSOLUTE_DIRECTORY`.
Always inspect existing processes, acquire `/tmp/statecraft-unity-validation.lock` atomically and wrap Unity in
`uv run tools/machine_lock.py --shared --wait-unity -- ...`. Never run competing heavy Unity jobs.

AI assistance: Codex Sol 6.1 wrote the prototype, tests and notes. The existing building art and its provenance remain unchanged.
