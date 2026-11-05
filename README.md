# Spitfire AFTER captures: the 101 Squadron Blender model (#2728 + #2734)

Throwaway capture branch (orphan). Not for merging.

## Tree

Built locally, never pushed:

| | SHA |
|---|---|
| origin/main | `1ce0395b7b07a2afb0954731eca4d05a6442efa7` |
| #2728 head (Spitfire VFX) | `3941162d43c5b8d973d62de4633f0632c2927150` |
| #2734 head (Blender FBX, authored-model hook) | `ad4cf9fc94c7c9debe09bcc94663f8acb4cdf563` |
| local combined tree (`merge --no-ff` main + 2728 + 2734) | `bc56de2672fc895f11c5007451ba2a67dc9b2ce6` |

Both merges were clean. #2728 moved from `970b6032d` to `3941162d4` during the run. The only change between them is a test clock seam on `ShowcaseFlight` (`Clock = () => Time.time`), with no change to how the game behaves. The captures use `3941162d4`.

The run was a throwaway EditMode test in Play mode with `-nostart`, through `gate_slots.py` and `machine_lock.py --shared`. It passed (1/1). The 101 Squadron shots use still mode (`UnitLayer.still`, `UnitShowcase.StillPhase` 0.52). The Egyptian Spitfire's clock is pinned. Language EN, 1920x1080. Date 20 October 1948 for the 101 Squadron shots (parked at rung 1, flying at rung 4) and 15 May 1948 for the Egyptian Spitfire.

BEFORE images come from `captures/spitfire-before` (`380be36ea`, main `9d53cc544`). The BEFORE flying shots ran on `Time.time`, not on the still phase, so the flying 101's pose and place differ between BEFORE and AFTER. The parked shots and the Egyptian t=30 shot show the same pose in both sets.

## Views

- `strips/`: BEFORE | AFTER for 101 parked close 12 km, flying close 16 km, flying normal 31 km, parked normal 31 km, parked min 6 km, and Egyptian close 16 km at t=30.
- `after/`: the AFTER frames, plus `sqn101-flying-tight-12km` (camera 4 km from the plane, used for the zoom checks) and `egy-wide-20km-t30.0` (flag check; the BEFORE set has no such view). `sqn101-flying-OVERLAND-close-16km` is the flying 101 at a rig-chosen phase where both the plane and its disc are over land.
- `checks/`: zoomed crops for checks a to d.

## Model facts (logged in Play mode from the renderers under `squadron_101`)

| | Authored FBX (AFTER) | Code-built (BEFORE) |
|---|---|---|
| Renderers | 2: `Airframe/Body`, `Airframe/Propeller` | airframe + propeller |
| Triangles | Body 6,272 + Propeller 1,424 = **7,696** | 1,426 |
| Vertices | Body 4,377 + Propeller 1,062 = 5,439 | - |
| Vertex attributes | Position, Normal, TexCoord0 (no tangents, no vertex colours) | - |
| Material | one, `spitfire-mk9e-101sqn-1948`, `Universal Render Pipeline/Lit`, smoothness 0.5, metallic 0, queue 2000 | units' palette paint |
| Texture | `spitfire-mk9e-101sqn-1948_basecolor`, 1024x1024, DXT1, 11 mips, bilinear, aniso 4 (base colour only, no normal map) | palette |
| Body local size | 10.2 x 2.2 x 8.9 m | |

When flying, the token also has the `PropellerBlur` haze (241 verts, URP Particles/Unlit, transparent) and the `Shadow` disc (241 verts, same material).

`shadowCastingMode`:
- Flying: Body Off, Propeller Off, PropellerBlur Off, Shadow Off. The badge plate and glyph are Off too.
- Parked: Body On, Propeller On. A parked aircraft casts a normal shadow on the ground under it.

## Checks (as seen in the frames)

**(a) Roundels are on the skin.** See `checks/a-*` and `after/sqn101-flying-tight-12km-en.png`. All of them are painted into the base-colour texture. There is no gap, no offset and no edge standing off the surface. The fuselage roundel wraps round the curved side and is cut cleanly by the fuselage's silhouette, which a separate decal could not do. The wing roundels lie flat in the wing, and the camouflage boundary runs through them. One nit: at 3x the white ring's edge is slightly soft, and the panel lines next to the wing roundel show small stair-steps. Both come from the texture's resolution (1024 px over about 10 m).

**(b) No faceting.** See `checks/b-fuselage-canopy-parked-6km-x2.png` and `checks/a-fuselage-roundel-*`. The fuselage and canopy shade smoothly at 6 km and in the 4 km tight view. I saw no hard shading bands or polygon steps along the fuselage, spine, canopy or wing roots. The only hard lines are the painted panel lines and the canopy frame.

**(c) Propeller haze.** See `checks/c-*`. The disc is centred on the red spinner in both the 16 km sea shot and the over-land shot, to within a few pixels by eye. Its ellipse matches the blades' plane: all four blades lie inside it, and their tips reach the rim (in the over-land frame one tip sits right at the edge). The disc is thin and light. It reads as haze, not as a plate.

**(d) One soft shadow.** See `checks/d-*`.
- With the disc on, the disc hidden, and the darkening between them (x5) in the same over-land frame, the soft disc is the only darkening the plane adds.
- At 31 km, the point where a real-time shadow would land (sun ray from the plane to the sea, at 1504,896) shows plain sea, even after contrast stretching.
- No crisp silhouette appears in any flying frame.
- All flying renderers are set to `shadowCastingMode` Off (above).

Egyptian Spitfire at t=30, 20 km: the flag is above the aircraft, with 211 px between the flag's bottom and the cockpit. The disc and haze render as in #2728.

## What still looks weak

- The textures are soft at close zoom (1024 DXT1, no normal map): roundel edges and panel lines blur or step at 3x. This is not visible at 12 km or more.
- Next to Tal's Egyptian model, the 101 model is simpler (it has exhaust stubs but no aerial wire, and plainer shading because it has no normal map). Its paint and shape now read as a Spitfire, which BEFORE's flat-tinted panels did not.
- Import churn: Unity rewrote the PR's `spitfire-mk9e-101sqn-1948.mat` on first import (`m_DoubleSidedGI: 1 -> 0`, and whitespace in `m_LockedProperties` and `m_Name`). This churn was restored locally. Committing the .mat as Unity writes it would stop the churn (NIT for #2734).
