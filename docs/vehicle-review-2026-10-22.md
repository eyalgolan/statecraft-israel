# Generated vehicle review — 22 October 2026

Three owner-generated candidates are placed in a playable, explicitly **non-historical** review environment:

| Candidate label | Asset ID |
| --- | --- |
| Egyptian Spitfire | `generated-1948-13-egyptian-spitfire` |
| Syrian Renault R35 | `generated-1948-14-syrian-renault-r35` |
| Egyptian Dakota C47 | `generated-1948-16-egyptian-dakota-c47` |

These are candidate labels. Their markings and exact subtype details still require art/content review; the models' `provenance.json` files retain those caveats, provider model IDs and source hashes. Only the three new asset directories were copied from combined backup `623ca73c`. M22 and the refused Emir are excluded.

## Why a neutral review environment

The current [unit data](../content/map/units_1948.yaml) provides Egyptian and Syrian **army** rows but no Egyptian air-service/base row or sourced R35 equipment entry. [UnitData](../unity/Assets/Scripts/Map/Units/UnitData.cs) supplies the existing Israeli aircraft types; [SeaAirCollection](../unity/Assets/Scripts/Map/Collection/SeaAirCollection.cs) places the existing C54 in its separate sourced context. None supplies an appropriate deployment for these three candidates.

The review plane therefore has no geographic coordinates, date, formation, faction control, combat behavior or simulation state. It is not an Egyptian airfield or a Syrian tank deployment. Existing Israeli aircraft, C54, armor and scenario data are untouched. A historical placement later needs a sourced date/location/formation and any required owner design decision.

## Open and use

Open `unity/Assets/VehicleReview.unity` and press Play. Existing game builds also enter the same runtime environment with the exact desktop flag `-vehicle-review` or Web query `?vehicle-review`. A normal launch does not activate the review environment.

- `0`: overview; `1`–`3`: focus a candidate.
- Wheel: zoom; `Q`/`E`: orbit; right-drag or camera keys: pan.
- In the flagged normal game launch, `Esc` restores the ordinary scene and starts its normal boot.

The permanent notice says **Vehicle review · Non-historical display** and states that no historical deployment is represented. Labels identify candidates, not military deployments. Aircraft use the existing aircraft-token presentation envelope; the tank uses the existing vehicle-token envelope. These are review sizes, not assertions of the candidates' relative physical dimensions. Each LOD is centered and grounded independently while preserving the source prefab's axis rotation. The camera uses `CameraRig` (35° FOV, 50° pitch), `CameraController`, map lighting/grade and the existing 40/45 km near/far switch.

## Import and evidence

The scoped `Statecraft.Game.EditorTools.VehicleReviewAssets.Import` method imports these three directories, creates their own simplified far meshes/prefabs, writes `Assets/Resources/VehicleReview/Models.asset`, and saves only the new `Assets/VehicleReview.unity` scene. It never rebuilds existing town LODs or overwrites old art. Far meshes must be below 4,500 triangles, lighter than their source, and preserve dimensions within the importer's tolerance.

`Statecraft.Game.EditorTools.VehicleReviewEvidence.Capture -captureDir <path>` opens the playable review scene and renders its runtime environment at matched overview, strategic and focused poses. Before frames show the same labelled slots empty; after frames show the candidate placements. Captures include world labels. `evidence.json` records camera/visibility settings. These are **Unity editor renders of the dedicated runtime review scene**, not historical gameplay-map or player/WebGL screenshots.

## Payload and validation

The Resources catalog directly references both prefab levels and their textures. Its assets are **included in player build payload even when review mode is not requested**; opt-in activation does not avoid those build bytes. Before far-LOD generation, the three copied asset directories contain 421,340, 382,016 and 387,652 bytes respectively (1,191,008 bytes total, including metadata/provenance). After import, independent far-LOD generation and trailing-whitespace normalization of the new Unity text files, the directories contain 612,251, 587,382 and 572,400 bytes (1,772,033 total); the Resources catalog is 1,324 bytes before its metadata. These are repository artifact bytes, not compressed build/download bytes. Compressed player download size and frame cost require a build/profile; neither is inferred from repository size.

`VehicleReviewTests` checks explicit activation, isolation from scenario catalogs, all three placements, pivot/ground contact, actual near/far reduction, texture dimensions, game-camera projection, label viewport fit/model separation and lighting/camera cleanup. Scoped Unity import passed with zero C# warnings. All nine `VehicleReviewTests` passed with zero failures and zero C# warnings. The label regression first failed on overlap, then passed after short world labels and mesh-height clearance replaced the oversized labels; full candidate names remain in the on-screen review legend. Near/far triangle counts are 7,177/4,437 for the Spitfire, 6,329/4,332 for the R35, and 6,854/4,315 for the Dakota. No paid jobs or new concepts are part of this integration.

For actual player screenshots, `VehicleReviewEvidence.BuildNativeReview` builds only the dedicated review scene into ignored `unity/Builds/VehicleReview/StatecraftVehicleReview.app`. Launch its executable with `-vehicle-proof <absolute-output-dir> -screen-width 3840 -screen-height 2160 -screen-fullscreen 0`. It captures the running player screen, including its non-historical notice, at the same five before/after poses, writes `player-evidence.json` with the actual screen dimensions and camera values, and exits. The proof path reuses the existing `VisualShots` settling interval and end-of-frame capture approach. No scene list or player settings are saved by the build helper.

If the native player build is blocked, the explicit `VehicleReviewPlayModeEvidenceTests.CaptureRunningReviewScene` test enters actual Editor Play Mode through the saved review scene, waits for `VehicleReviewScene.Start` to place all three models, and captures ten matched 4K scene-camera frames into `captures/vehicle-review-playmode`. Its provenance labels them **actual Editor Play Mode scene-camera renders, with world labels and without the screen-space HUD**. They must not be called native-player screenshots. Package/compiler validation is never bypassed to obtain a build.

The one bounded native-player attempt stopped at `Error building Player: Currently selected scripting backend (IL2CPP) is not installed.` No backend change, package patch or validation bypass was attempted. The native-player screenshot route therefore remains unverified on this machine; Editor Play Mode evidence must keep its own provenance label.

The final explicit Play Mode capture test passed 1/1 with zero C# warnings and produced all ten corrected-label frames. The five pairs have identical recorded camera/quality fields at 40 km overview, 60 km strategic view and 12 km individual inspections, all 3840×2160, PC tier and 35° FOV. Visual inspection confirmed clear, grounded models with readable labels that do not cover them. The wrapper also printed five `Invalid ILPostProcessor configuration: Configuration not initialized` diagnostics after its passing test verdict; no player success or clean player-build log is inferred. Separately, the simulation suite passed 2,423 tests with zero warnings/errors, repository policy tests passed 320, and the repository/rule-ID checks passed.
