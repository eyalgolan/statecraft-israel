# Spitfires AFTER v2 (PR #2728, in play)

These are re-shoots of the AFTER views for draft PR #2728 at its tuned head. The strips compare three versions:

| Set | Code | Captures |
|---|---|---|
| BEFORE | `origin/main` 9d53cc54417d2c0a43c62ecaab7a95941b25f878 | branch `captures/spitfire-before` (380be36eaabc8d295fca88349e0667a79babc84e) |
| AFTER v1 | PR head 18c50de1821d54548100d1fb400c6d90e8e3e5b4 | branch `captures/spitfire-after` (99d993dd9cdd74a20871610c20c1dbacc2c6ae1e) |
| **AFTER v2** | **PR head c641b08edc785991c091c1cdf79f17a028331e48** | this branch |

- **Rig:** `E21SpitAfter2.cs`, a copy of the v1 rig. The cameras, loop times, crops and file names are the same as v1. It is a throwaway and is not committed. The copy adds:
  - logs of each renderer's `shadowCastingMode` (`CAST`/`FACTS` lines);
  - the design meshes' alpha and layer count (`DESIGN` lines);
  - the screen point where a real-time shadow would land, found by marching along the sun's ray;
  - two widened t=30 shots;
  - the 101 flying over land, plus the same frame with the disc hidden.
- **Run:** Unity 6000.3.25f1, batchmode EditMode in Play mode (`-nostart`), PC tier, English.
  - Ran through `gate_slots.py` and `machine_lock.py --shared`, after `build_sim` and a csc compile check (0 errors).
  - The test passed 1/1. Porcelain was 0 before and after.
  - Run 2 only added the disc-hidden shot. All PNGs here come from run 2. The Egyptian frames are byte-identical to run 1.

## Files

- `strips/`: BEFORE | AFTER v1 | AFTER v2 side by side, at 1/3 size.
  - `egy-close-16km-t10.0`: haze and flag.
  - `egy-normal-31km-t00.0` and its `-zoom`.
  - `sqn101-flying-close-16km`: 101 haze. StillPhase 0.52 in v1 and v2; BEFORE ran on `Time.time`.
  - `egy-close-16km-t30.0`: the flag is cropped at the top in this framing.
- `checks/egy-normal-31km-t00.0-shadow-contrast-3way.png`: a contrast-stretched crop of the same area in all three sets.
- `after-v2/`: full-size v2 frames.
  - `egy-close-16km-t10.0`.
  - `egy-wide-20km-t30.0` and `egy-wide-24km-t30.0`: the t=30 flag, uncropped.
  - `sqn101-flying-close-16km`.
  - **`sqn101-flying-OVERLAND-close-16km` and `-normal-31km`: NEW, the 101 flying over land.** The rig placed `ShowcaseFlight` at phase 0.95, the phase out of 100 sampled that is highest over the sea for both the plane and the disc. At that phase the plane is east of Kafr Qasim, with 444 m of ground under it above sea level and 421 m under the disc.
- `checks/sqn101-flying-OVERLAND-*-disc-check.png`: three panels of the same frame: disc on, disc hidden, and the difference ×5 (what the disc darkens).

## The five items

1. **Double shadow: fixed.**
   - Logged `shadowCastingMode` values:
     - Egyptian `el-arish-spitfire` and its `Propeller`: **Off** (v1: On).
     - Flying 101 `Airframe`, `Propeller`, `PropellerBlur` and `Shadow`: **Off**.
     - Parked 101 `Airframe` and `Propeller`: **On** (unchanged).
     - Dakotas `tel-aviv-dakota-lead` and `-wing`: airframe **On**, propellers Off (unchanged; the diff touches no Dakota code).
     - Ships: On.
   - On screen, the crisp silhouette about 7.7 km away is gone. At t=0 (31 km) a real-time shadow would land at px (1478, 881), 7,751 m from the plane. The mean luma of a 40 px box there:

     | Set | Luma at the old silhouette | Clear sand nearby |
     |---|---|---|
     | BEFORE | 109 | 200 |
     | v1 | 125 | 200 |
     | v2 | **188** | 200 |

   - The soft disc is now the only shadow: see `checks/egy-normal-31km-t00.0-shadow-contrast-3way.png` and `strips/egy-normal-31km-t00.0-en-3way.png`.
2. **Darker disc: confirmed.**
   - `SpitfireShadowDisc` has alpha 0.600 at its middle. The Dakotas' `ShadowDisc` stays at 0.420.
   - The Egyptian disc centre at t=0 (31 km) has luma 154 in v2, 170 in v1 and 195 in BEFORE, against 200 for clear sand. It reads as a soft dark blob below and right of the plane, about 1.9 km off.
3. **Lighter 101 haze: confirmed.**
   - `SpitfirePropellerBlur101`: 1 layer, maximum alpha 0.333. The Egyptian `SpitfirePropellerBlur`: 2 layers, each with maximum alpha 0.549. `PropellerBlurOpacity` is 0.65.
   - On screen the 101 disc is no longer a near-opaque white plate: the red spinner and the sea show through it, and the dark X of the blades stays on top (`strips/sqn101-flying-close-16km-en-3way.png`).
4. **Smoothness 0.5: confirmed.**
   - Logged: Egyptian `... (glossy)` 5.0/10; 101 `UnitMiniature (palette) (glossy)` 5.0/10, parked and flying.
   - Slightly more sheen on the wing tops than v1. Still no hot specular spot.
5. **Flag at t=30, unchanged by design.**
   - Uncropped in `after-v2/egy-wide-20km-t30.0` and `-24km`. The plane heads towards the camera.
   - The flag bottom is 588 m *below* the box top, which is the raised wingtip. On screen the flag floats clear above the tail, with a visible gap: about 120 px at 20 km and 90 px at 24 km.
   - It never covers the cockpit (`cockpitInsideFlagRect=False`, 237 to 313 px below the flag).
   - It reads as a label hovering behind the aircraft rather than one attached to it. That is acceptable, but it is less tied to the plane than in the level and t=10 frames.

## 101 over land (new)

The disc is drawn and enabled:
- `Shadow` enabled=True, 5.4 x 5.7 km, about 2.0 km from the plane, on land;
- material `HexAttackBorder`, queue 3000.

The disc-hidden A/B shows it darkens a patch by at most about 38 of 255 levels, and about 11 to 13 on average:
- 31 km: about 250 x 230 px around (1090, 756);
- 16 km: at the lower right, partly under the legend.

Over the busy brown hills, the trees and the village props, **it is hard to see by eye** in the plain frames. Only the difference panel shows it clearly. The trees inside it are not darkened, because they draw over the disc. Possible cause, not confirmed: the on-screen darkening (about 25% at most) is weaker than the 0.6 vertex alpha suggests, so the blur material may scale it down. The Egyptian disc over flat sand reads much better.

## Reproduce

The rig is in the session scratchpad at `e21-cap-spitfire-after2/`: `E21SpitAfter2.cs`, `rig/run.sh`, `rig/drive.sh`, `rig/compose.py`, `rig/strip3.py` and `rig/contrast3.py`. `drive.sh` checks five things before it runs `run.sh` through `gate_slots.py`:
- the time window;
- at most 2 other batch Unity processes;
- an unlocked console;
- a clean tree;
- HEAD = c641b08ed.

It also runs `build_sim` and the csc compile check.
