# Limestone draft verification, 12 November 2026

Base main: c494a289dd8b515335f1a9f6eb63ff015b0d940e. Unity 6000.3.25f1, isolated clone without ignored prototype art.

- RED `LimestoneIsRegisteredOutsideResourcesInItsOwnBundle`: 2 tests failed before registration; 0 C# warnings/errors.
- GREEN `HexPropsTests|HexCollectionFiguresTests|HexUnitTests`: total 40, passed 39, failed 0, skipped 1 LocalOnly; 0 C# warnings/errors. Includes catalog-before-prefab wildcard enumeration, both limestone stream callbacks and no-Resources/Collection checks.
- `uv run scripts/oss/check_repo.py`: OK over 5362 tracked files (before this evidence note); no release-clearance claim, asset rows remain owner-verify.
- `git diff --cached --check`: clean after trimming generated metadata trailing whitespace.

## Matched actual-board captures

Existing central_israel board, judean_highlands prototype, unchanged seed/camera/light, 1600x1000. Example hex (-3,27): 46 pieces before and after, 22 to 18 stand-ins, 10814 to 13533 triangles against 40000 budget. Four limestone placements change; olive/carob/pine remain stand-ins. Editor board build times 19889ms before / 19034ms after are one-run editor observations, not first-frame/player performance evidence. The brown scanned rock appearance needs Tal's batch review; limestone is the existing recipe name, not a geological identification.

## Blocked acceptance checks

A local macOS-only limestone bundle attempt reached BuildPipeline but failed compiling Unity package NativeList.cs at 850/864 with CS7036 (missing safety argument to NativeArray<T>.ReadOnly constructor). No valid bundle was produced, and no player/WebGL load, first-frame timing, CRC-load, memory or teardown pass is claimed. Full Unity suite and SDK 10.0.401 sim/aggregate/tools gates have not run. No package upgrade or security change was made to hide the failure.

Existing hex props use CollectionStream.Request, which pins their source bundle. Board disposal clears generated instances and ignores late callbacks, but does not unload these source assets. This draft does not complete #1954's cleanup/memory acceptance.

Original local checkout untouched. Promoted asset GUIDs are new to avoid colliding with ignored prototype copies. No restricted build was shared. Final named-batch approval and maintainer review are required before release.

## Final local optimization and budget check

Tal reviewed the two-rock before/after screenshots and explicitly approved public draft publication and GitHub blob use in chat. Final maintainer release acceptance remains outstanding; owner-verify rows are retained.

Four runtime imports are capped at512x512, with mipmaps and no readable CPU pixel copy. Original1K JPG bytes and source packages remain unchanged. Unity Editor runtime texture-memory estimates total8,392,608 bytes at1K and2,101,152 bytes at512, exactly6MiB less (about75%). Meshes total120,500 bytes (700/699 triangles). These are Editor asset-memory observations, not measured player/GPU/FPS guarantees.

Final focused command `HexPropsTests|HexCollectionFiguresTests|HexUnitTests`: PASS total=40 passed=39 failed=0 skipped=1 warning-CS=0 error-CS=0. Tests enforce512px and non-readable imports, triangle range699–700, catalog-before-download variant discovery and both streamed callbacks.

A deterministic flat-surface recipe sweep covered18 limestone-using recipe variants across5 fixed coordinate/seed cases:90 samples,85 within budgets,5 over. All5 are grazing_land at182297–182298 triangles against60000. Substituting only the limestone stand-ins still yields180259; sheep account for150456 triangles and donkey29071. The rocks add about2040, but do not explain that overall excess. This draft neither changes animals nor claims all recipes pass. No baseline performance regression/FPS claim is made.

Matched512 capture was inspected against1K at the same board zoom: no obvious visual loss there; other zooms/platforms unverified. Development-mode bundle retry hit the same Unity package CS7036. No package dependency was changed to hide it. No valid player bundle, loading/memory/cleanup or first-frame performance acceptance is claimed.
