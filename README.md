# Spitfires AFTER v3 (PR #2728, in play)

Re-shoots of the AFTER views for draft PR #2728 at its new head.

| Set | Code | Captures |
|---|---|---|
| BEFORE | `origin/main` 9d53cc54417d2c0a43c62ecaab7a95941b25f878 | branch `captures/spitfire-before` (380be36eaabc8d295fca88349e0667a79babc84e) |
| AFTER v2 | PR head c641b08edc785991c091c1cdf79f17a028331e48 | branch `captures/spitfire-after-v2` (a1962130f6fca2b00d2c4955a24d536da00ab996) |
| **AFTER v3** | **PR head 6982901b103ca3056afeb785ec940725def12fe4** | this branch |

## What changed vs v2 (code)

- `SpitfireLoop.FlagClearM` 150 -> 60 m (flag closer to the tail).
- `SeaAirCollection.SpitfireShadowDisc` 0.6 -> 0.75 at its middle, same fade (logged alphaCentre 0.749). The Dakotas' disc stays 0.420.
- Blur, shadow and no-cast now also apply to an authored model (`UnitShowcase.FlyingSpitfire`). There is no authored model yet, so the code-built 101 looks the same: its disc is the same size as in v2 (5.4 x 5.7 km over land, 4.7 x 5.1 km over the sea), and the 101 frames with the disc hidden are pixel-identical to v2.

## Run

- Rig `E21SpitAfter3.cs` is a copy of the v2 rig, renamed. The cameras, clocks, crops and file names are the same. It is a throwaway and is not committed.
- Unity 6000.3.25f1, batchmode EditMode in Play mode (`-nostart`), PC tier, English.
- Ran through `gate_slots.py` and `machine_lock.py --shared`, after `build_sim` and a csc compile check (0 errors).
- The test passed 1/1 in one run. Porcelain was 0 before and after.

## Numbers (v2 -> v3)

| Check | v2 | v3 |
|---|---|---|
| Flag bottom to top of the aircraft, 20 km, t=30 | 125 px | **116 px** |
| Same, 24 km, t=30 | 91 px | **84 px** |
| Egyptian disc centre, 31 km, t=0 (20-40 px box luma) | 154 | **137** |
| Clear sand near it (W / E / S) | 201 / 198 / 183 | 201 / 198 / 183 |
| 101 over land, disc darkening max (luma, 16 km / 31 km) | 41 / 40 | **55 / 55** |
| Same, mean over the footprint (diff >= 3) | 13.9 / 12.2 | **17.4 / 15.2** |

Methods (scripts in the rig):
- **Gap** (`gap.py`): the last row with flag green, then the first row below it with aircraft pixels.
- **Disc** (`disc.py`): a box at the logged `shadowPx` (1083, 791) and sand boxes 300 px W and E and 220 px S.
- **Darkening** (`darken.py`): the luma of the disc-hidden frame minus the disc-on frame.

The v2 numbers re-measured this way match the v2 README (about 120 px, 154, max about 38, mean 11-13).

## Files

- `strips/*-v2-v3.png`: v2 | v3 at 1/2 size:
  - `egy-close-16km-t10.0`;
  - `egy-normal-31km-t00.0` and its `-zoom`;
  - `sqn101-flying-close-16km`;
  - `egy-wide-20km-t30.0` and its `-zoom`, and `egy-wide-24km-t30.0` (the flag);
  - `sqn101-flying-OVERLAND-close-16km`, `-normal-31km` and its `-zoom`.
- `strips/*-before-v2-v3.png`: BEFORE | v2 | v3 for `egy-close-16km-t10.0` and `sqn101-flying-close-16km`.
- `checks/egy-normal-31km-t00.0-shadow-contrast-v2-v3.png`: a contrast-stretched crop around the disc.
- `checks/sqn101-flying-OVERLAND-*-disc-check-v2-v3.png`: two rows (v2, v3), each with disc on, disc hidden, and the darkening x5.
- `after-v3/`: full-size v3 frames.

## What it looks like

- **Flag at t=30: still detached.** It moved down only about 10 px. Most of the gap comes from the flag sitting about 2 km behind the plane's centre (`flagHorizFromCentre` 1973 m) at a 50 deg camera, not from the 60/150 m clearance. Cutting 90 m of height cannot close it. At t=10 the flag now sits a little lower over the rear fuselage. It still does not cover the cockpit.
- **Egyptian disc over sand: clearly darker** (137 against 200 for sand). It reads as a soft shadow below and right of the plane. It is still soft-edged, with no hard rim.
- **101 disc over land: stronger but still hard to see by eye.** The difference panels show it clearly, about 35% more darkening than v2. In the plain frames it is lost in the brown hills, the trees and the villages, as in v2. Trees draw over it.
- **101 haze, paint and sea frames:** unchanged from v2, as expected.
