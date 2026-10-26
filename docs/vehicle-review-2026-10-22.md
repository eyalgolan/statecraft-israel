# Generated vehicle review — original sources

Refs #756. PR #1378 keeps three candidates as **editor-only review art** until a sourced gameplay placement and art/sensitivity sign-off exist. This follows the [23 October owner rulings](https://github.com/eyalgolan/statecraft-israel/pull/1378#issuecomment-6358296929) and [original review](https://github.com/eyalgolan/statecraft-israel/pull/1378#issuecomment-6357803018).

| Candidate | Original GLB triangles | Original colour map | Original animation clips |
| --- | --- | --- | --- |
| Egyptian Spitfire (13) | 7,177 | 4096×4096 | 0 |
| Syrian Renault R35 (14) | 6,329 | 4096×4096 | 0 |
| Egyptian Dakota C-47 (16) | 6,854 | 4096×4096 | 0 |

These are candidate labels. Paint, markings and exact subtype are unverified; the review environment asserts no geographic location, date, formation or historical deployment. Current [unit data](../content/map/units_1948.yaml), [UnitData](../unity/Assets/Scripts/Map/Units/UnitData.cs) and [SeaAirCollection](../unity/Assets/Scripts/Map/Collection/SeaAirCollection.cs) supply no supported deployment for them. Existing Israeli aircraft, C54, armor and scenario rows are unchanged.

## Original files and provenance

The two original delivered ZIPs are preserved byte-for-byte in [asset-sources/generated-1948/vehicles](../asset-sources/generated-1948/vehicles/). They contain the original textured GLBs with embedded colour maps; they contain **no provider FBX**. [sources.json](../asset-sources/generated-1948/vehicles/sources.json) records archive/member hashes, provider model IDs, geometry counts and round-trip checks. Per-model provenance beside each editor asset records the same identity. Original archives and backup commit `623ca73c` are preserved.

Unity has no glTF importer installed in this project. The `*-compatibility.fbx` files are explicitly documented, full-geometry GLB→FBX compatibility conversions, not renamed originals. The export does not decimate, weld, alter the pivot or rewrite mesh/UV data; its reimport checks triangle connectivity and vertex/UV error below 1e-6. There are no original clips to discard. The 4096px JPEG bytes are extracted from the original GLB buffer without resizing or re-encoding.

[VehicleReviewSources.py](../unity/Assets/Editor/VehicleReviewSources.py) reproduces and verifies the preparation locally, without a service or paid job:

```sh
Blender --background --factory-startup --python unity/Assets/Editor/VehicleReviewSources.py -- --repo REPO --batch ORIGINAL_BATCH
python3 unity/Assets/Editor/VehicleReviewSources.py --verify --repo .
```

## Lossless editor import

`Statecraft.Game.EditorTools.VehicleReviewAssets.Import` imports only these three candidates into `Assets/Editor/Generated1948VehicleReview/`. Mesh and animation compression are Off; animation import keeps all source clips; texture import is uncompressed with Crunch disabled and a cap at least the 4096px source dimensions. Platform compression/size overrides are cleared. There are no near/far variants, decimated meshes, LODs or zoom-dependent model swaps.

Models and colour maps are under **CC BY-NC-ND 4.0, credit Tal Heuberger**, as authorized for these new candidates on 23 October 2026. [ASSETS.yaml](../ASSETS.yaml) retains `ai_generated: true`; [LICENSING.md](../LICENSING.md) adds only the new vehicle model/map paths. Existing model licences and contribution terms are unchanged.

## Review and player exclusion

Open `unity/Assets/Editor/Generated1948VehicleReview/VehicleReview.unity` and press Play. Keys `0` and `1`–`3` show the overview or focus a candidate; the existing game camera supports wheel zoom, Q/E orbit and right-drag pan. The permanent notice identifies a non-historical display. Camera, lighting, token presentation envelopes and runtime-only centering/grounding serve visual review; they do not assert physical relative sizes or modify original source pivots.

All review types are compiled only under `UNITY_EDITOR`. The catalog, scene, prefabs, compatibility models and textures are under `Editor/`; original archives are outside Unity. There is no Resources catalog, player bootstrap, query/desktop review flag, native capture/quit hook, player build helper, build-listed review scene or asset-bundle label. Tests check the enabled player-scene and Resources dependency closures and all named bundles for vehicle references. Source/archive bytes describe repository storage, not player download size; a fresh player build has not been measured for this revision.

Future gameplay placement depends on sourced deployment data, art/sensitivity sign-off and **an own streamed bundle loaded after the first frame, outside the 1948 Collection**. This PR does not implement that future loading system or invent a placement.

## Evidence

Offline original-archive/colour-map verification and compatibility geometry/UV round trips pass for all three. Nine focused Unity tests cover provenance, lossless import, editor exclusion, full-geometry placement at both camera distances, labels and cleanup. Current-revision Unity import passed with zero C# warnings/errors; all nine focused tests passed, and the explicit actual Editor Play Mode capture passed 1/1. All five refreshed pairs were visually inspected and their recorded camera/state/quality fields match; earlier reduced-model screenshots are superseded. The current-upstream simulation suite passed 2,493 tests with zero warnings/errors; policy tests passed 323, and repository policy passed over 3,213 tracked files. No current player-build or compressed-download-size success is claimed.

The explicit `VehicleReviewPlayModeEvidenceTests.CaptureRunningReviewScene` route enters the saved editor-only scene in actual Editor Play Mode. It produces five matched 3840×2160 before/after pairs at 40/60 km overview and 12 km inspections. Before frames show empty neutral slots; after frames show the candidates, at identical camera/state/quality. They are scene-camera renders with world labels and without the screen-space HUD, not native-player or historical-map screenshots.
