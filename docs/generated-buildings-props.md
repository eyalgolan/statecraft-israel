# Generated buildings and props integration

This branch is independent of the vehicle and character branches. It contains five house variations (including two separately named local roof repairs), two tents and four props, with original asset GUIDs preserved. The scoped [admission receipt](generated-buildings-props-admission.json) records source/runtime hashes copied from combined checkpoint `623ca73c`. Original provider GLBs, 4K maps and earlier files remain in local staging. No simulation/content rules or historical deployments change.

| Asset | Placement |
|---|---|
| 01 tiled-roof repair, 02 Netanya balcony and 03 porch houses | Existing Netanya house pool, alongside the original B13 house |
| 04 lighter stone house | Existing Tiberias pool, alongside the original B17 courtyard house |
| 05 courtyard roof repair | Existing generic Arab-town / Village-style house plots, alongside B17; unrelated OldHouse and StoneBlock roles stay unchanged |
| 06 enclosed Bedouin tent | Existing sourced encampment tent plots only; never ordinary Arab-town houses |
| 08 khaki canvas tent | Existing non-white ridge-tent plots in player-built transit camps; white/bell tents and military barracks remain |
| 09 well, 10 sandbags, 11 crate stack, 12 wall | Playable review display only: no suitable exact runtime detail role is currently defined |

The Village style follows ForArchetype so Tiberias actually reaches its own house pool. Shared TownBlock plots outside Netanya retain original B13 only, preventing Netanya variations from leaking into another town. Jerusalem's existing Village-style plots can use the generic courtyard variation; its other architectural roles retain their original pools. Tent selection adds no random draws and changes no camp locations, counts, census or far kit geometry. Missing model data retains the original kit rendering. Imported bounds are Blender mesh-space x=width,y=depth,z=height; the runtime fit rotates z into Unity y. Tent models retain their existing camp plot footprints; house fitting keeps the existing model world scale.

## Playable review

Launch the normal game with `-buildingpropsreview` or WebGL `?buildingpropsreview`. The additive `Assets/GeneratedBuildingsPropsReview.unity` scene waits for the actual MapScene and CameraRig, then places and names every scoped asset on a clearly labeled display surface. Camera controls remain the normal game controls. Display sizes are art-review choices, not historical scale. Ordinary MapScene does not load the review scene or place the review-only props.

In the Editor, Statecraft → Generated1948 → Play buildings and props review opens Map plus the additive scene and enters Play mode. This is a playable route, not the older editor-only asset render.

## Preparation and validation

Run `Statecraft.Game.EditorTools.Generated1948BuildingsPropsAssets.Prepare` only in the coordinated Unity window. It calls append-only TownModelAssets.AppendMissing, retains existing catalog indices and baked mesh objects, and checks every existing serialized mesh block remains unchanged. Do not call the full TownModelAssets.Create for this branch. The two tent and two repaired-roof entries append their own LOD groups. All 54 original sub-assets from main remain byte-identical; no original LOD is regenerated. Unity can round-trip compressed normal floats during SaveAssets, so preparation restores the exact existing serialized mesh blocks before reimporting and verifies them.

Regression coverage checks requested-town house mapping, no tent leakage into town-house pools, actual encampment models, identical transit-camp census/far geometry, explicit review flags and the scene's full scoped asset roster/material references. Unity import and actual Play Mode elevated front/rear views passed for both repairs. The first scoped EditMode run passed25/25; after role activation,24/26 passed with two stale allowlists, then the focused allowlist rerun exposed the shared TownBlock leak described above. Exact-current mapping tests and captures validate that correction. Final result counts and logs are recorded in the [category QA receipt](generated-buildings-props-qa.json). No native-player or WebGL result is claimed.

House 03 retains the documented local rear-plaster backing caveat. Houses 01 and 05 remain separately named local roof derivatives with their original exports untouched. Unit/hash, culled-roof and actual elevated Unity checks passed before the owner-approved pool activation. Their added geometry is not claimed to be an authentic reconstruction. House01 has5,055triangles (original5,021+34); house05 has5,494 (original5,466+28), each with512px colour maps. Their measured roof surfaces are closed; the courtyard notch and porch openings remain intentional. No vehicle, character, whole-town concept or rejected model is included. No LFS or existing licence policy changes are made.

## Visual evidence

Actual Unity Editor Play Mode at PC quality produced the all11 display, elevated roof front/rear crops and matched campaign pairs using the real MapScene CameraRig. Captures exclude screen-space GUI/HUD; the live scene shows names through OnGUI. This is not a native-player/WebGL or historical-site screenshot.

All review JPEGs remain local and ignored under `captures/buildings-props-playmode/reviewer-jpegs/`:

- `generated-1948-buildings-props-overview.jpg`
- `generated-1948-buildings-props-roofs.jpg`
- `generated-1948-buildings-props-netanya-before.jpg` and `generated-1948-buildings-props-netanya-after.jpg`
- `generated-1948-buildings-props-tiberias-before.jpg` and `generated-1948-buildings-props-tiberias-after.jpg`
- `generated-1948-buildings-props-courtyard-plot-before.jpg` and `generated-1948-buildings-props-courtyard-plot-after.jpg`

Normal GitHub attachments remain pending: the browser extension lacked file-URL access and the upload was blocked before execution. These screenshots are not committed or uploaded by an alternate route. Raw3840×2160 PNGs remain in the parent capture folder; nothing was discarded.

Matched campaign pairs use the same map inputs, camera and quality. The before frame temporarily restores only original B13/B17 pool contents in memory, rebuilds the map, and restores the approved pools before the after frame. No source asset or simulation state is written by that comparison.
