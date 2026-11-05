# Spitfires AFTER (PR #2728, in play)

AFTER stills of the game's two Spitfires on draft PR #2728 (branch `claude/spitfire-prop-gloss-shadow`). They use the same views as the BEFORE set on branch `captures/spitfire-before` (commit 380be36eaabc8d295fca88349e0667a79babc84e, shot on `origin/main` 9d53cc544).

- **Code:** PR #2728 head 18c50de1821d54548100d1fb400c6d90e8e3e5b4, on main 9d53cc54417d2c0a43c62ecaab7a95941b25f878. A throwaway test rig was added for the run and is not committed.
- **Rig:** a copy of the BEFORE rig (`E21SpitAfter.cs`, a copy of `E21SpitBefore.cs`). The dates, camera distances and angles, loop times, crops and file names are the same. It differs from the BEFORE rig in four ways:
  - it logs the shadow's distance from the aircraft and the flag's position against the cockpit;
  - it sets `UnitLayer.still`, so the flying 101 token holds `UnitShowcase.StillPhase`;
  - it leaves the 101 aircraft's new `Shadow` child out of the subject box, so the aim and crop match the BEFORE set;
  - the captions read "PR #2728 18c50de18 (AFTER)".
- **Run:** Unity 6000.3.25f1, batchmode EditMode test in Play mode (`-nostart`), PC quality tier (URP `PC_RPAsset`), English UI. The test passed 1/1. `build_sim` ran and a csc compile check passed with 0 errors. The worktree was clean (porcelain 0) before and after the run.
- **Files:**
  - `captures-spitfire-after/after/`: 24 AFTER PNGs, 1920x1080 each plus a caption bar, with the same names as the BEFORE set.
  - `captures-spitfire-after/side-by-side/`: 24 PNGs, BEFORE on the left and AFTER on the right, at half size, one for every matching view.
  - `captures-spitfire-after/egy-normal-31km-t00.0-shadow-contrast-before-after.png`: a contrast-stretched crop showing where the shadows lie.

## Views (all match the BEFORE set)

| Subject | Views |
|---|---|
| Egyptian Spitfire, 15 May 1948, clock pinned via `SpitfireLoop.Clock` | `egy-close-16km-t{00,06,10,20,30}.0`, `egy-normal-31km-t{00,06,10,20,30}.0` (+ `-zoom` crops), `egy-min-6km-t{00,10}.0` (empty frames, as in BEFORE: at 6 km the camera is below the aircraft) |
| 101 Squadron token, 20 Oct 1948, parked (rung 1) | `sqn101-parked-min-6km`, `-close-12km`, `-normal-31km` (+ `-zoom`) |
| 101 Squadron token, flying (rung 4) | `sqn101-flying-close-16km`, `-normal-31km` (+ `-zoom`) |

**101 flying views do NOT match in phase.** BEFORE ran `ShowcaseFlight` on `Time.time` (t = 12.3 s and 12.5 s, west of Netanya near the coast). AFTER holds `StillPhase` 0.52, which puts the aircraft over open sea heading towards the camera. The camera distance and angle are the same, but the aircraft's place and heading differ. The AFTER flying shots now repeat exactly.

## Model facts, BEFORE and AFTER (from the rig's log)

| | BEFORE (main 9d53cc544) | AFTER (PR 18c50de18) |
|---|---|---|
| Egyptian airframe material | `generated-1948-13-egyptian-spitfire`, URP/Lit, smoothness **0.0**, metallic 0 | `... (glossy)` copy, URP/Lit, smoothness **0.4**, metallic 0 |
| Egyptian propeller blur | `SpitfirePropellerBlur`, 722 verts, near-black (0.06) | the same mesh name and size (722 verts), pale warm grey (0.82, 0.81, 0.78), alpha 0.22 to 0.55 |
| Egyptian shadow disc, horizontal distance from the aircraft | **7,687 to 7,895 m** (t = 0/6/10/20/30: 7741, 7687, 7853, 7881, 7895) | **1,900 to 1,973 m** (1911, 1957, 1900, 1966, 1973) |
| Egyptian flag | bottom 150 m over the box top (the raised wingtip in a bank) | bottom 150 m over the **tail** top; at bank 28 that is 588 m below the box top and 1,189 m over the centre |
| 101 airframe + propeller material | `UnitMiniature (palette)`, URP/Lit, smoothness **0.18** | `UnitMiniature (palette) (glossy)`, smoothness **0.4**, both parked and flying |
| 101 flying: propeller blur | none | `Aircraft/Propeller/PropellerBlur`, `SpitfirePropellerBlur101`, 482 verts, 864 tris, world 1.87 km across, `HexAttackBorder` (URP Particles/Unlit) |
| 101 flying: ground shadow | none | `Aircraft/Shadow`, `SpitfireShadow` disc, 4.70 x 5.13 km, laid **2,179 m** from the aircraft, on the sea (y 1,311 against sea y 1,281) |
| 101 parked | no blur, no shadow | no blur, no shadow (as intended: flying only) |
| 101 token totals (flying) | 2,215 verts, 1,734 tris | 2,938 verts, 3,030 tris |

## The five changes, item by item

1. **Light propeller haze.** Works on both aircraft.
   - Egyptian: a pale disc with brighter streaks and a clear spinner. The dark still blades stay readable through it. See `side-by-side/egy-close-16km-t{00,06,20,30}.0-en-sbs.png`. At 31 km it reads as a small pale disc (`egy-normal-31km-t10.0-en-zoom-sbs.png`).
   - 101 flying: the new `PropellerBlur` child is present only when flying (log: `blur=.../Propeller/PropellerBlur` flying, `blur=none` parked). It reads as a bright, almost opaque white plate with the dark X of the blades on top, which is much stronger than the Egyptian haze. See `sqn101-flying-close-16km-en-sbs.png` and `sqn101-flying-normal-31km-en-zoom-sbs.png`.
2. **Satin paint, smoothness 0.4.** Confirmed in the log: Egyptian 0.0 to 0.4, 101 0.18 to 0.4. On screen the change is subtle: the upper surfaces are slightly lighter, with a soft sheen on the wing tops (`egy-close-16km-t00.0-en-sbs.png`, `sqn101-parked-close-12km-en-sbs.png`). There is no hot specular spot. It is not too shiny, and at 31 km it still reads mostly matte.
3. **The flying 101 casts the soft disc.** The disc is present and laid 2.18 km from the aircraft on the sea. Over the dark sea it is only a faint smudge below and right of the aircraft (`after/sqn101-flying-normal-31km-en.png`, around x 1100, y 950).
4. **Shadow from 0.25 of the drawn height.** Numerically confirmed: the Egyptian disc moved from about 7.8 km to about 1.9 km (on the sun's line), and the 101 disc is at 2.18 km. On screen the near soft disc is faint at 31 km. Meanwhile the **crisp URP real-time shadow** of the Egyptian airframe (its renderer has `cast=On`) still lands about 7.7 km away, where the disc used to sit on it. AFTER therefore shows two shadows: a faint soft blob near the plane and a sharp aircraft silhouette far off. See `egy-normal-31km-t00.0-shadow-contrast-before-after.png` and `side-by-side/egy-normal-31km-t{00,06,10,20,30}.0-en-sbs.png`.
5. **Flag over the tail, clear of the cockpit in the 28-degree turns.** Fixed at t=10: BEFORE drew the flag over the cockpit, while AFTER floats it over the rear fuselage, clear of the canopy. See `egy-close-16km-t10.0-en-sbs.png` and `egy-normal-31km-t10.0-en-zoom-sbs.png`. In the log, the box centre (about the cockpit) is outside the flag's screen rect in every shot (`cockpitInsideFlagRect=False`). At t=10 normal the flag spans y 604 to 639 px, below the cockpit point at y 575. At t=30 (heading towards the camera) the flag stands well above the tail, about 65 px above it at 31 km and cut off at the top of the 16 km frame (`egy-close-16km-t30.0`). In level flight it still floats beside the tail, unchanged from BEFORE.

## Reproduce

The rig (`E21SpitAfter.cs`, `rig/run.sh`, `rig/drive.sh`, `rig/compose.py`, `rig/sbs.py`) lives in the session scratchpad `e21-cap-spitfire-after/`. `drive.sh` checks the time window, that at most 2 other batch Unity processes are running, that the console is unlocked, that the tree is clean and that HEAD is 18c50de18. It then runs `build_sim` and a csc compile check, and runs `run.sh` through `gate_slots.py` and `machine_lock.py --shared`.
