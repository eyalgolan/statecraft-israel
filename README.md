# Spitfires BEFORE (origin/main, in play)

Baseline stills of the game's two Spitfires as they look on `origin/main`, taken before any VFX work. The owner asked: "can we improve the spitfirs using vfx? they currently look very low quality".

- **Code:** `origin/main` 9d53cc54417d2c0a43c62ecaab7a95941b25f878, the main code only, with a throwaway test rig that is not committed.
- **Run:** Unity 6000.3.25f1 batchmode EditMode test in Play mode (`-nostart`), on the PC quality tier (URP asset `PC_RPAsset`), English UI.
- **Frames:** each map frame is 1920x1080 with the UI composited on top. Every PNG has a white caption bar above the frame giving the SHA, the subject, the date, the camera distance and the loop time.
- **Camera:** 50 degrees down, 35 degree field of view, near clip 50 m. The game's zoom limits are 6 km (`CameraController.MinDistance`) and 250 km, and the default play distance is 31 km (`CameraRig.StartDistance`).
- **Distances in captions:** "camera X km" is the distance from the camera to its look-at point, which is what the zoom sets. "plane Y km from camera" is the aircraft's depth in the view. The rig puts the aircraft on the view's central ray.

## Subject 1: Tal's Egyptian Spitfire (SpitfireLoop)

- **Code:** `CollectionCatalog.EgyptianSpitfire` (`generated-1948-13-egyptian-spitfire`), drawn by `SeaAirCollection.Fly`. It flies a loop by `SpitfireLoop`.
- **Date window:** `SeaAirCollection.SpitfireFrom` to `SpitfireTo`, which is 15 May 1948 to 2 January 1949. The shots are at **15 May 1948**, the game's start.
- **The loop:** an ellipse 5 km by 3 km flown in 40 s. Its centre is 30 km in from the western board edge, at el-Arish's latitude. The aircraft flies 4,200 m above the highest ground under the loop, at world y 6,440. It banks up to 28 degrees in the end turns.
- **Clock:** pinned through `SpitfireLoop.Clock`, so these shots repeat exactly. Loop times are given as t from the loop's start.
  - t=0: south pass heading east, level.
  - t=6: rolling into the east turn, bank 9.7.
  - t=10: east turn, bank 28.
  - t=20: north pass heading west, level.
  - t=30: west turn, bank 28.
- **Land or sea:** the whole loop is over **land**, the desert near the coast. At every sampled time the ground under the aircraft (y 1,720 to 1,895) is above the sea's surface (y 1,281). No loop time puts it over the sea. For an over-sea contrast, see the flying 101 Squadron token below, which is over the sea off Netanya.
- **Minimum zoom:** at 6 km the camera (y 4,596) is below the aircraft (y 6,440). Aiming at the aircraft then puts it 2.4 km behind the camera, so it cannot be framed at the minimum zoom; `egy-min-6km-t00.0` shows the empty frame. This matches the `SeaAirCollection.EdgeSpot` doc comment. The close-ups use a 16.4 km camera instead, which puts the aircraft 8 km from the camera, and the normal shots also have zoom crops.

## Subject 2: 101 Squadron's Spitfire token (UnitModels.Spitfire)

- **Code:** `content/map/units_1948.yaml` `squadron_101` flies the `spitfire` type from **15 October 1948**. Before that date it flies the Avia S-199. The token is the code-built `UnitModels.Spitfire` ("LF Mk IXe of 101 Squadron, from October 1948"), with no authored prefab.
- **Date and place:** shot on **20 October 1948**, after the bridge redraw, at its anchor, the Herzliya strip. The token was placed at hex -2,20.
- **Parked:** at security rung 1 the token stands parked. Shots are at 6 km (the minimum zoom, where the aircraft overfills the frame), 12 km (close) and 31 km (normal), plus a zoom crop.
- **Flying:** at rung 4 or above (`UnitRoster.FlyingFromRung`) the token flies `ShowcaseFlight`, a 16 km circle 4,200 m up, flown every 48 s. Its clock is `Time.time` and is NOT pinnable, so the rig re-aims on the aircraft in the frame it shoots. Here the aircraft was over the sea off Netanya.
- **Scale:** both aircraft are drawn at the tokens' aircraft scale of 420x, about 4.7 km across.

## Model facts (read by the rig in play)

| | Egyptian Spitfire | 101 Sqn token |
|---|---|---|
| Airframe renderers | 1 fused mesh, plus a propeller blur disc. The shadow and flag are separate objects beside it | 2: airframe and propeller. The badge plate and glyph are separate |
| Airframe mesh | `tripo_node_7faa8cca-...` (Tripo AI-generated FBX): **9,141 verts, 7,177 tris**, 1 submesh. Attributes: Position, Normal, Tangent, UV0 | `spitfire-mk9e-101sqn-1948` (code-built): **1,987 verts, 1,426 tris**. Attributes: Position, Normal, UV0. There are no tangents |
| Propeller | Blades modelled still in the fused mesh, plus a separate translucent 3-blade blur disc (`SpitfirePropellerBlur`, 722 verts / 1,296 tris, turns at 1,500 deg/s) | Separate solid 4-blade mesh (`spitfire-propeller`, 192 verts / 280 tris) that turns. There is **no blur disc** |
| Material | `generated-1948-13-egyptian-spitfire`, URP/Lit: **smoothness 0, metallic 0**, no keywords | `UnitMiniature (palette)`, URP/Lit: smoothness 0.18, metallic 0. Shared by all unit miniatures |
| Textures | `_BaseMap` only: **4096x4096 DXT1**, 13 mips, bilinear, aniso 1. There is **no normal, metallic or occlusion map** | `_BaseMap` = `UnitPalette`, **104x32 RGBA32**, 1 mip, bilinear. The colours come from palette UVs |
| Blur disc / shadow material | `HexAttackBorder`, URP/Particles/Unlit, transparent (queue 3000) | n/a |
| Shadows | Airframe casts and receives real shadows. It also has a separate soft blur-disc shadow (`SpitfireShadow`, 241 verts) laid where the sun ray hits the ground | Airframe and propeller cast and receive shadows. There is no separate ground shadow disc |
| Flag | `UnitFlag/egypt`, 536 verts, shader `Statecraft/HexSelection` | Token badge `UnitBadgePlate` and the air glyph, shader `Statecraft/HexSelection` |
| World size at 420x | 3.9 x 1.5 x 4.7 km (level) | 5.5 x 1.1 x 5.4 km (parked, yawed 200 deg) |
| Totals (whole object) | 9,863 verts / 8,473 tris, plus the marks: 777 verts / 748 tris | 2,215 verts / 1,734 tris |

## What each looks like as seen

**Egyptian Spitfire**

- **The model is good.** It is a detailed textured model with desert camo, panel lines, cockpit glazing, aerial wires and cannon barrels.
- **Material.** The surface is fully matte (smoothness 0) with no normal map. Up close it reads a little like a painted plastic toy, with no specular sheen or rim light.
- **Propeller.** The still modelled blades stay crisp while a translucent blur disc turns behind them, so it reads as two propellers.
- **Undercarriage.** The landing gear is down in flight; the wheels show in the banked shot at t=30.
- **Tilt-shift.** At close range the depth-of-field blur softens the tail and the far wing.
- **No VFX.** There is no exhaust, smoke, contrail, glow or motion streak.
- **Shadow.** The ground shadow lies far from the aircraft, about 7.5 km to the east-south-east, because the aircraft flies 4.2 km up under a 30 degree sun. At the normal zoom the shadow sits at the bottom-right edge of the frame, detached from the aircraft.
- **Flag.** At the banked t=10 the flag floats over the cockpit and covers the middle of the aircraft.
- **At 31 km** the aircraft is about 330 to 420 px across and reads fine.

**101 Squadron token**

- **Low-poly and flat.** The mesh has 1,426 tris with smooth-shaded but faceted surfaces, a box-like fuselage, a flat slab wing and a tube spinner.
- **Palette texture.** The 104x32 palette texture gives blocky camo patches, with stair-stepped texel edges visible in the zoom crops and at 6 km.
- **Propeller.** The 4-blade propeller is solid and crisp with no blur, even in flight.
- **No VFX.** There is no exhaust or other effect.
- **Unexplained shapes.** When flying, two small grey ellipses show beside the fuselage, one of them with a star. I did not establish where they come from.
- **Parked.** The aircraft overlaps the orchard trees and a house, since it is wider than the town. The token's badge covers its tail.
- **Overall** it reads as a board-game miniature next to Tal's model, a very different level of detail.

## Files (captures-spitfire-before/)

Egyptian Spitfire, 15 May 1948:

- `egy-close-16km-t00.0-en.png`, `egy-close-16km-t06.0-en.png`, `egy-close-16km-t10.0-en.png`, `egy-close-16km-t20.0-en.png`, `egy-close-16km-t30.0-en.png`: close-ups, camera 16.4 km, aircraft 8 km from the camera.
- `egy-normal-31km-t00.0-en.png`, `egy-normal-31km-t10.0-en.png`, `egy-normal-31km-t20.0-en.png`, `egy-normal-31km-t30.0-en.png`: the default play distance.
- `egy-normal-31km-t00.0-en-zoom.png` (x2.5) and `egy-normal-31km-t10.0-en-zoom.png` (x2.1): zoom crops of the 31 km frames.
- `egy-min-6km-t00.0-en.png`: the minimum zoom. The aircraft is behind the camera, so the frame is empty.

101 Squadron token, 20 October 1948:

- `sqn101-parked-min-6km-en.png`: the minimum zoom; the aircraft overfills the frame.
- `sqn101-parked-close-12km-en.png`
- `sqn101-parked-normal-31km-en.png` and `sqn101-parked-normal-31km-en-zoom.png` (x2.8)
- `sqn101-flying-close-16km-en.png`: rung 4, over the sea.
- `sqn101-flying-normal-31km-en.png` and `sqn101-flying-normal-31km-en-zoom.png` (x2.1)
