# Generated buildings and props: original-source editor review

**Not finished game assets. All eleven models are editor-only, and all new campaign activation is held.** Provider originals 01 and 05 have known roof gaps, and 03 has a broad rear opening. Restoring original deliveries intentionally removes the previous local roof caps/rear panel. Those adaptations remain preserved separately in offline staging; they are not relabeled as originals or included in this source-original revision. Tal can request separately identified adapted delivery, but neither version has Eyal's final art/sensitivity sign-off here.

The [admission receipt](generated-buildings-props-admission.json) identifies every source by hash. Byte-identical provider GLBs are archived under `asset-sources/generated-1948/<id>/<id>.glb`, outside Unity’s import tree. Each matching directory under `unity/Assets/Editor/Generated1948Review/` contains the exact embedded 4096×4096 colour-map bytes, a local FBX compatibility export, material/prefab and provenance. The FBX is not a provider-original export. Conversion preserves triangle positions/connectivity and UVs within documented float serialization tolerance; its stored corner normals exactly match the Blender-imported source values. Blender's reimport quantization is recorded separately, rather than claimed to be exporter loss. No geometry centering, decimation, roof repair, atlas or texture resize/recompression is performed. The GLB remains the exact source of record.

| Number | Original triangles | Current readiness |
|---|---:|---|
|01 tiled house|5021|Known original roof defects; prior repair excluded|
|02 balcony house|4798|Full-source editor review captured; activation/sign-off held|
|03 porch house|4929|Known original rear opening; prior plaster panel excluded|
|04 lighter basalt variant|5624|Full-source editor review captured; activation/sign-off held|
|05 courtyard house|5466|Known original roof defects; prior repair excluded|
|06 enclosed Bedouin tent|3184|Full-source editor review captured; activation/sign-off held|
|08 khaki canvas tent|3988|Full-source editor review captured; activation/sign-off held|
|09 well|3992|Editor-only; no sourced map role|
|10 sandbags|4074|Editor-only; no sourced map role|
|11 crates|2126|Editor-only; no sourced map role|
|12 wall|2146|Editor-only; no sourced map role|

Mesh and animation import compression are Off. Every source clip is kept; all eleven provider GLBs contain zero clips and zero skins. Texture import is Compressed (GPU block compression, per the owner's 24 October 2026 ruling), Crunch is off for every platform, and max size is 4096, matching the source. There are no `-near`/`-far` files or new generated LODs. Existing main catalog entries, placements and 54 baked mesh sub-assets are unchanged.

## Loading and placement dependency

Eyal's [23 October rulings](https://github.com/eyalgolan/statecraft-israel/pull/1379#issuecomment-6358297184) require own streamed bundles after the first frame, or the houses may use #1273's town-building streaming once it lands. Current main `703d87cf` has no compatible town-stream interface. This branch does not build that system: it removes its new campaign/catalog references, leaves existing towns/camps unchanged and records only [prepared mappings](generated-buildings-props-mapping.json) outside runtime code. Nothing new enters Resources, the 1948 Collection or the initial player art dependency graph. No WebGL download measurement is claimed.

The intended mappings remain Netanya houses, Tiberias stone house, generic Village/ArabTown courtyard variation, existing Bedouin tent plots and existing non-white transit-camp ridge tents. They are inactive. Future activation still needs before/after screenshots at matching state/camera for each affected role, including Jerusalem, an Arab village and both camp types, and Eyal's art/sensitivity sign-off. Tal's earlier asset/repair approval is not presented as Eyal's approval. The four props remain editor review art until a sourced role exists.

## Editor review and validation

The review scene lives under `Assets/Editor/Generated1948Review/`, is absent from build settings and has no runtime loader. Its component is guarded by `UNITY_EDITOR`. Use **Statecraft → Generated 1948 → Prepare buildings and props**, then **Play buildings and props review**. Preparation validates lossless imports and sets up only editor prefabs/scene references. It never appends a catalog entry, writes TownModels.asset or generates LODs.

The [QA receipt](generated-buildings-props-qa.json) records current offline verification and remaining gates. Old repaired/slim captures and test results are superseded for this restored-original revision. The last original-source Unity validation, before this archive-only move, passed 6/6 tests with no compiler warnings/errors: actual import/settings, source hashes/triangle counts, player dependency isolation, layout and Editor Play Mode captures. Local evidence is in `captures/buildings-props-originals-review-v2/`, including `all11-overview.png`, `original-source-defect-contact.jpg` and `original-source-overview.jpg`. Visual inspection confirms the known 01 roof defects and 03 rear opening; 05 visibly lacks its roof deck. These source views do not clear those assets. Offline policy/integrity checks passed 329 tests, and the merged-tree sim passed 2493 tests. The archive-path change has 34 affected package/policy tests passing; Unity was not relaunched for that change. No player/WebGL result or final art approval is claimed.

New model/map licensing is CC BY-NC-ND 4.0, credited to **Tal Heuberger**, under the precise new row in [LICENSING.md](../LICENSING.md). Older asset licences are unchanged. This records the requested file licence; it does not tick the contributor-consent checkbox.

Model 04 uses the lighter basalt colour variant. Its stable `generated-1948-04-dark-basalt-house` ID is retained for reference continuity; the ID retains the earlier naming rather than describing this lighter appearance.
