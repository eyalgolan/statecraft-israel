# #2649 in-play captures: the Dakotas on 15 July 1948, the S-199s, the flak and the kill

Refs #2649, #2482. Recorded by an agent, 5 November 2026. Facts below are what the agent saw in the frames and the run logs; a person's check is pending.

- **before:** origin/main `c224092b0b7bd4d973d56bea3ea48996dcdc32a6`
- **after:** PR #2649 head `427aabab3024993c54f5a3516b92eec14bb2f9c9` (unchanged, no local fix; the rig compiled on both trees with 0 errors)
- Runs (IST): head 10:37-10:38 (the shots here; two earlier head runs at 10:24 and 10:26 were camera tries), main 10:28-10:30. All through `gate_slots.py` and `machine_lock.py --shared`, with `build_sim_for_unity.py` on each checkout. Head 2/2 tests passed (English, Hebrew), main 1/1 (English). `git status --porcelain` was empty before and after each run (`captures-2649/logs/*-status`).

## How they were taken

`captures-2649/rig/E21Play2649.cs` is a throwaway EditMode test, never committed (a copy of the #2613 capture rig):
- It opens `Map.unity` with GameBoot off, enters Play mode with `-nostart`, builds the map and calls `GameRoot.Begin` (the 15 May 1948 turn). GameRoot's craft layer places the two Dakotas.
- It pins the map's animation clock (`SpitfireLoop.Clock`) to 480 s + t (four 120 s loops in), so main and head show the same instant. **t is the lead's loop time; the wing's own time is t - 26.4 s.**
- 15 May: shots at t = 0.
- 15 July: it then sets the game state's date to 15 July 1948 and raises the bridge's `Changed`, so GameRoot redraws its layers as on a turn change (`SeaAirCollection.ShowAt` with the new date). The top bar then reads "15 July 1948" (logged). On head the air defence (`TelAvivAirDefence`, two S-199s) appears with both Dakotas; on main no Dakota is drawn on 15 July (log: `dakotas=0 defence=no`).
- `TelAvivAirDefence` is read by reflection, so the same rig compiles on main. The head run wrote its subject cameras to `logs/head-cams.txt`; the main run read them, so main and head frames use the same camera.
- 1920x1080, camera 50 degrees down, yaw 0 (north up). Captions by `rig/compose.py`. The run logs (`logs/*-lines.txt`) have one `SHOT` line per frame: the Dakotas' and fighters' screen rects, and how many tracer, hit, flak and trail renderers are on, the crash blast, and whether the wing is drawn.
- Published PNGs are 256-colour quantized (size); the composites keep the full-colour frames' layout.

**Cameras**
- `jul-air-45km` / `jul-air-28km` / `jul-air-16km`: on the subject (look-at = the ground point behind it along the view): t = 0 and 2, the midpoint of the lead and fighter 1; t = 60, the lead and fighter 2; flak and fall frames, the wing; t = 57, the wing's crash point. 16 km is head only (published for t = 38.4, 38.7, 39.2 and 60).
- `jul-tlv-31km`: fixed on Tel Aviv, 31 km.
- `may-air-45km` / `may-air-28km`: on the lead Dakota; `may-tlv-31km`: Tel Aviv, 31 km.
- `-zoom`: a 2x crop of the frame's centre (head 45 km frames).

**File names**: `head-` / `main-` captioned frames, `sbs-` main left and head right, at the same date, clock and camera. `-en` / `-he` is the UI language.

## The PR's shot list, item by item

1. **t = 0: Dakotas over Tel Aviv bombing; fighter 1 firing on the lead, the first hit puff.**
   Head: both Dakotas, the lead over Tel Aviv with its bomb smoke; fighter 1 (S-199, Israel's flag) behind and above the lead, its muzzle flash showing, three tracers on and one hit puff (log). The hit puff is small at 28 km. Main: no aircraft over Tel Aviv.
   Files: `head-jul-air-28km-t000.0-en.png`, `head-jul-air-45km-t000.0-en.png`, `head-jul-tlv-31km-t000.0-en.png`, `sbs-jul-air-28km-t000.0-en.png`, `sbs-jul-air-45km-t000.0-en.png`, `sbs-jul-tlv-31km-t000.0-en.png`. Hebrew: `head-jul-tlv-31km-t000.0-he.png` (תל אביב and the other names), `head-jul-air-45km-t000.0-he.png`.
2. **t = 2: fighter 1 at its pass, the hits on the lead.**
   Head: fighter 1 just over the lead, two dark hit puffs on the lead's right wing, a hit flash (log: hits 2, tracers 0). Main: empty sky.
   Files: `head-jul-air-28km-t002.0-en.png`, `head-jul-air-45km-t002.0-en.png`, `head-jul-tlv-31km-t002.0-en.png`, `main-jul-air-28km-t002.0-en.png`.
3. **t = 28.9 to 38.4: flak bursts round the wing, the last on it at t = 38.4.**
   Head: t = 28.9 a flak flash beside the wing; t = 29.2 a small black puff near it; then two large dark puffs beside the wing (2 on from t = 31.2, as each fades the next bursts); at t = 37.2 a gun flash on the ground. At t = 38.4 the last burst's flash is on (log `FlakFlash`) and the first trail puff starts at the wing's centre; at 38.7 and 39.2 the trail puffs grow (trail 1, then 2). **The last burst itself reads weakly**: it is at the wing's own centre, so the Dakota's airframe hides most of it; the side bursts are what show. Main: no wing, no flak.
   Files: `head-jul-air-45km-t028.9-en.png`, `-t029.2-` (and `-zoom`), `-t031.2-`, `-t033.2-` (and `-zoom`), `-t035.2-`, `-t037.2-`, `-t038.4-`, `-t038.7-` (and `-zoom`), `-t039.2-` (and `-zoom`); `head-jul-air-28km-t033.2-en.png`, `-t038.4-`, `-t038.7-`; `head-jul-air-16km-t038.4-en.png`, `-t038.7-`, `-t039.2-`; `head-jul-tlv-31km-t028.9-en.png` to `-t039.2-`; `sbs-jul-air-28km-t038.7-en.png`, `sbs-jul-air-45km-t038.7-en.png`. Main: `main-jul-air-45km-t028.9-en.png` to `-t038.7-`, `main-jul-tlv-31km-t028.9-en.png` to `-t038.7-`.
4. **t = 45 and t = 54: the wing falling and trailing smoke.**
   Head: the wing nose down and banked, turning back toward the sea (t = 45 over the coast north of Tel Aviv, t = 54 further round); 10 trail puffs on (log). The trail reads as a few dark puffs behind the tail, not a long streak. Main: nothing.
   Files: `head-jul-air-45km-t045.0-en.png` (and `-zoom`), `head-jul-air-28km-t045.0-en.png`, `head-jul-air-45km-t054.0-en.png` (and `-zoom`), `sbs-jul-air-45km-t045.0-en.png`, `main-jul-air-45km-t045.0-en.png`, `main-jul-air-45km-t054.0-en.png`.
5. **t = 57: its blast on the ground; the wing gone.**
   Head: the blast flipbook (fire and dark smoke) on the ground inland of the coast, north of Tel Aviv; no wing Dakota (log: `wingShown=False`, crash blast on). Main: nothing.
   Files: `head-jul-air-28km-t057.0-en.png`, `head-jul-air-45km-t057.0-en.png` (and `-zoom`), `head-jul-tlv-31km-t057.0-en.png`, `sbs-jul-air-28km-t057.0-en.png`, `sbs-jul-air-45km-t057.0-en.png`.
6. **t = 60: fighter 2 on the northern pass, firing on the lead.**
   Head: the lead alone (no wing) over the coast at Netanya, fighter 2 over it with its muzzle flash and three tracer dashes along the lead (clear at 16 km), one hit (log). Main: nothing.
   Files: `head-jul-air-16km-t060.0-en.png`, `head-jul-air-28km-t060.0-en.png`, `head-jul-air-45km-t060.0-en.png`, `sbs-jul-air-45km-t060.0-en.png`.
7. **t = 107: the fresh wing back on the loop over the sea.**
   Head: the wing Dakota drawn again over the sea (log: `wingShown=True`), with Egypt's flag; fighter 1 is drawn over its nose on screen (it flies 5.4 km above). Main: open sea.
   Files: `head-jul-air-45km-t107.0-en.png` (and `-zoom`), `head-jul-tlv-31km-t107.0-en.png`, `sbs-jul-air-45km-t107.0-en.png`.
8. **15 May 1948, t = 0, before and after: unchanged, no fighters and no flak.**
   Both trees: the two Dakotas bombing Tel Aviv, no fighters, no flak, no air defence object (log `DEFENCE none`). Pixel diff main vs head, English: max channel difference 4, 7 and 10 of 255, **0 pixels differ by more than 15** in each of the three frames.
   Files: `sbs-may-air-45km-t000.0-en.png`, `sbs-may-air-28km-t000.0-en.png`, `sbs-may-tlv-31km-t000.0-en.png`, and the `head-may-*` / `main-may-*` frames.

## Seen, beyond the list
- On screen the S-199s often overlap the Dakota they are near (t = 0 and 2: fighter 2 over the lead; t = 107: fighter 1 over the wing's nose), and a fighter's Israeli flag can sit over a Dakota. They are kilometres apart in height (the PR's tests check world boxes); from this 50-degree camera they still overlap.
- The wing's Egyptian flag at t = 107 floats well off the aircraft.
- Tel Aviv's building models stream in and out between frames on both trees (for example main and head `jul-tlv-31km-t060.0`), so some frames differ in the city's buildings. This is unrelated to the PR.
- Repeat runs on head were not pixel-identical in the July frames, for the same streaming reason; the cameras were identical (`logs/head-cams.txt`).
