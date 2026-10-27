# Runtime Collection figures — prototype follow-up to #1153

- [x] **TASK-000** library check: reuse existing `CollectionCatalog` and `CollectionStream.Request`; the local library dashboard was unavailable. No new dependency or model.
- [x] Establish cause at #1153 head `06c80f37`: Collection aliases are Editor-only, the proto/null cache is sticky, and placed visuals do not refresh after arrival.
- [x] Reproduce capsules in an actual one-hex WebGL player.
- [x] Add a hex-only owner-scoped adapter, fallback and asynchronous refresh.
- [x] Check failure, repeated requests, teardown, reload, pose, scale and picking with focused native tests and player checks.
- [x] Obtain independent source/image review and matched actual-player screenshots.

## Boundary and behavior

The adapter uses `CollectionStream.Request(id, CollectionDetail, done)` and the existing alias/detail table. `UnitFigures`, `UnitFigureDetail`, `RuralLife`, controllers, catalog, bundle import and loader internals remain unchanged (#665/#667). No models move into Resources.

Pending or failed requests keep their Resources/stand-in fallback. Arrivals refresh only alias visuals; terrain and picking geometry retain their identity. A unit retains its latest distance, moving state, facing and formation. This is presentation code and does not read or change simulation state.

The adapter borrows streamed prefabs and cancels its callbacks on teardown/destruction. Generated fallback meshes/materials have local owners and are released on replacement/removal. Cancellation of shared downloads and unloading the game-wide cache remain the Collection API's responsibility; hex owners must not destroy those borrowed assets.

The first candidate player exposed unsupported non-Legacy `AnimationClip.SampleAnimation` calls. Both hex sampling paths now use one hex-local guard: Editor/Legacy sampling remains available; player non-Legacy figures keep their authored Animator/controller playback. Runtime stride sampling is not provided. This does not implement or change the maintainer's main-map animation work.

## Actual validation

Unity 6000.3.25f1, Development WebGL, local diagnostic scene using the production hex renderer. Camera, light, board, unit formation and default civilian alias match before/after. The scene is one bare hex with an authored eight-member squad, not the full game or a sim roster.

| Check | Result |
| --- | --- |
| Baseline #1153 player | Eight capsules and a civilian stand-in; 0 skins in all 3 phases; expected-8 assertion fails. |
| Lifetime regressions before fixes | 17/20 native cases pass; the 3 added cancellation/material-lifetime cases fail. |
| Final focused native run | 20/20 pass, exit 0, no C# warnings/errors. Covers request/failure/cancellation, synchronous arrival, pose/scale, picking and generated/borrowed ownership. |
| First candidate player | Real figures load, but runtime sampling errors appear; strengthened player checker fails. |
| Final default player | 8 skins/8 Animators plus textured far civilian in all 3 phases; no sampling errors or exceptions. |
| Near-placement variant | Existing C01 near civilian plus squad: 9 skins/9 Animators in all 3 phases; both sampling paths exercised without player errors. |
| Missing people bundle, final-build copy | Capsules remain; 0 skins, exactly 1 bundle warning, all 3 phases complete. Original build/bundles remain intact. |

Every final player case retains one terrain collider and the same pick `(0, 73.87, 0)`. Destroy/rebuild reports zero remaining board owners and zero retained generated meshes. These are focused ownership checks, not a full memory-profile claim. Separate-context Sol review inspected source, XML/logs and actual images. Baseline engine/browser WebGL capability warnings remain; the runs are not described as warning-free. Existing optimized-sampling messages remain in the Editor fixture; runtime errors are absent after the guard.

Non-Unity checks: .NET 10.0.401 sim **1,967 passed**, no warnings; policy tests **305 passed**; repository policy and rule-ID checks pass. Tools full sandbox run: **1,437 passed, 4 skipped**, with 10 failures/3 teardown errors from process/server and NuGet access restrictions; affected-file recheck with SDK/cache and required access: **154 passed**. Tools used Python 3.12.10 and available Rasterio 1.4.3 rather than pinned 1.5.2.

## Loading and performance limits

The existing people bundle is **6,309,797 bytes**, byte-identical before/after; successful cold profiles request it once through the runtime stream. All five existing Collection bundles total 15,321,910 bytes. The final Development probe has 19,027,122 data bytes and 123,203,755 Wasm bytes (baseline 19,013,856 / 123,193,808); the difference includes probe instrumentation. No new model payload is added.

Runtime timing logs were collected on a busy host and are excluded from performance comparisons. No frame-time improvement, WAN loading budget, full-map budget, zoom LOD, full locomotion or production acceptance is established. The alias table still selects near squads at every hex zoom. Arrivals coalesce per frame, but the aliased portion of an emitted layer is rebuilt when needed. A quiet production profile remains separate work.

## Live integration boundaries

Checked on 20 October 2026: [#1153](https://github.com/eyalgolan/statecraft-israel/pull/1153) remains draft at `06c80f37`, [#1131](https://github.com/eyalgolan/statecraft-israel/pull/1131) is `668580cf`, and main was checked at `107a45bb`. This follow-up starts at #1153; it does not validate the newly updated combined stack. The maintainer's painter/material, terrain and editor-reload fixes remain in #1131.

Read-only check of draft [#1205](https://github.com/eyalgolan/statecraft-israel/pull/1205), head `288c84117ca8c023ba21a6814c5d75b646b36ec6`, finds no overlapping production file or consumed API change. Its `UnitRoster.FromState`, strength-based 3/2/1 figures and moved-anchor/spot caches apply to the main-map `UnitLayer`/`UnitPlacer` path. This prototype builds authored `BoardHex.unit` entries through `HexUnitTypes` and `HexUnitMotion`; the adapter resolves supplied members and does not choose count or placement.

This neither replaces nor integrates #1205/#1186. The eight-member probe proves neither sim-roster membership nor strength changes/moved-token placement. Wiring that roster into the opt-in hex board remains separate work; combined behavior has not been run. The public Collection request API is unchanged by the inspected roster diff.
