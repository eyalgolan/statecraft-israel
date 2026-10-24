# Generated buildings and props: source editor review

**Not finished game assets. All eleven models are editor-only, and all new campaign activation is held.** At Eyal's 24 October 2026 request on #1379 ("Ask Tal to regenerate"), Tal regenerated 01, 03 and 05 in Tripo Studio. The provider originals had roof gaps (01 and 05) and a broad rear opening (03); the new deliveries have one closed hipped roof (01), four closed walls (03) and one continuous L-shaped roof deck (05). Each is a provider delivery, not a local repair. The earlier local roof caps/rear panel stay offline and are not used. None of the eleven has Eyal's final art/sensitivity sign-off here.

The [admission receipt](generated-buildings-props-admission.json) identifies every source by hash. Byte-identical provider GLBs are archived under `asset-sources/generated-1948/<id>/<id>.glb`, outside Unity’s import tree. Each matching directory under `unity/Assets/Editor/Generated1948Review/` contains the exact embedded 4096×4096 colour-map bytes, a local FBX compatibility export, material/prefab and provenance. The FBX is not a provider-original export. Conversion preserves triangle positions/connectivity and UVs within documented float serialization tolerance; its stored corner normals exactly match the Blender-imported source values. Blender's reimport quantization is recorded separately, rather than claimed to be exporter loss. No geometry centering, decimation, roof repair, atlas or texture resize/recompression is performed. The GLB remains the exact source of record.

| Number | Source triangles | Current readiness |
|---|---:|---|
|01 tiled house|9324|Regenerated 24 Oct 2026; closed hipped roof; editor review captured, activation/sign-off held|
|02 balcony house|4798|Full-source editor review captured; activation/sign-off held|
|03 porch house|9261|Regenerated 24 Oct 2026; four closed walls; editor review captured, activation/sign-off held|
|04 lighter basalt variant|5624|Full-source editor review captured; activation/sign-off held|
|05 courtyard house|8193|Regenerated 24 Oct 2026; continuous roof deck; editor review captured, activation/sign-off held|
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

The [QA receipt](generated-buildings-props-qa.json) records current verification and remaining gates. For the 24 October regeneration, each new geometry passed a single-sided opening check before it was textured: rays cast from above, from four sides and from four elevated diagonals, and they find no roof or wall opening beyond doorway and window recesses and the open porch. Rear views were 11.2% see-through for the old 03 and are 0.0% now. Top views of 01 were 2.0% see-through and are 0.0% now, and 05's roof deck is closed over both wings. Hits that remain are well under 1% from the elevated views and come from open doorway or window recesses and the 01 porch, which is open by design. The FBX conversion keeps triangle positions, connectivity and UVs, and its stored corner normals match the source exactly. For 03, Blender's reimport reports a 1.0 normal difference on a degenerate face; Unity imports the stored normals, so this does not affect the review. Unity 6000.3.25f1 EditMode `Generated1948AssetTests|Generated1948BuildingsPropsTests` passed 5/5 with no compiler warnings or errors, covering actual import and settings, source hashes, triangle counts, player dependency isolation and layout. `tools/tests/test_generated_buildings_props_package.py` passes. Local Editor Play Mode evidence is in `captures/buildings-props-regenerated-review-v3/`: `all11-overview.png`, `regenerated-overview.jpg`, and front and rear views of the three regenerated houses in `regenerated-front-rear-contact.jpg`. Previous original-source captures stay in `captures/buildings-props-originals-review-v2/` for comparison. No player/WebGL result or final art approval is claimed.

New model/map licensing is CC BY-NC-ND 4.0, credited to **Tal Heuberger**, under the precise new row in [LICENSING.md](../LICENSING.md). Older asset licences are unchanged. This records the requested file licence; it does not tick the contributor-consent checkbox.

Model 04 uses the lighter basalt colour variant. Its stable `generated-1948-04-dark-basalt-house` ID is retained for reference continuity; the ID retains the earlier naming rather than describing this lighter appearance.
