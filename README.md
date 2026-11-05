# #2613 in-play captures: Dakota smoke columns and keep-clear box

Refs #2613, #2604. Recorded by an agent, 5 November 2026.

- **before:** origin/main `06f4ae6b82ec517c4858bbf6fb551db25c19528e`
- **after:** PR #2613 head `7c52b5c75ab88bf2a9e6f4a2e7802362801216f3` (unchanged, no local fix. The rig compiled on both trees with 0 errors)
- Runs: main 10:01-10:03 IST, head 10:04-10:05 IST. Both through `gate_slots.py` and `machine_lock.py --shared`, with `build_sim_for_unity.py` on each checkout. 2/2 tests passed on each tree, and `git status --porcelain` was empty after each run (`captures-2613/logs/*-status`).

## How they were taken

`captures-2613/rig/E21Play2613.cs` is a throwaway EditMode test that was never committed. It works the same way as the #2490 v3 rig:
- It opens `Map.unity` with GameBoot off and enters Play mode with `-nostart`.
- It builds the map and calls `GameRoot.Begin`, which opens the 15 May 1948 turn (the Dakota bombing). GameRoot's own craft layer places the two Dakotas and their keep-clear boxes.
- It then pins the map's animation clock (`SpitfireLoop.Clock`) to fixed values, so main and head show the same instant.
- Each shot is 1920x1080, with the camera aspect pinned to 16:9 and the camera 50 degrees down at yaw 0 (north up).
- Captions are added by `rig/compose.py`. The run logs are in `captures-2613/logs/*-lines.txt`: one `SHOT` line per frame with the plane positions, each Dakota's keep-clear box and aircraft/flag screen rects, and the names shown, plus `COLS` lines with each column's puffs (height, width, alpha, tint).

**Clock.** The clock is set to 480 s (four 120 s loops in, every column standing) plus the lead's loop time `u`. The lead's lag is 0 and the wing's is 26.4 s. Frame names carry `u`: `u063` is the issue's frame, with both Dakotas about 18 km north, just off the top edge of a 31 km view of Tel Aviv. Timed frames are taken at u = 63, 67, 71, 75 and 79 s (4 s apart, over one 16 s column life on head and 14 s on main). The growth frames are taken in the first loop, 2, 6, 10 and 15 s after the first column's first hit.

**Cameras:**
- `wide-31km`: Tel Aviv, 31 km.
- `opening-31km`: the #2490 v3 opening look-at, 31 km.
- `close-20km`: Tel Aviv, 20 km.
- `column-9km`: the column nearest Tel Aviv, 9 km.
- `plane-31km` / `plane-20km`: the camera on the lead Dakota (look-at = the ground point behind it along the view, so it sits mid-frame).
- `seq3-fireball-12km`: a lead drop, release + 3 s.

**Overlays** (only on plane, wide and opening shots): yellow is each Dakota's keep-clear box projected to the screen (its 8 corners), cyan is the aircraft's renderer bounds and magenta is its flag. The names are the game's own UI layer.

## The PR's check list, item by item

1. **Wide (31 km opening view): four separate thin columns over Tel Aviv, not blobs. Herzliya, Magdiel and Petah Tikva show when no Dakota is in frame.**
   - Files: `sbs-wide-31km-u063-en.png`, `sbs-wide-31km-u063-he.png`, `sbs-zoom-wide-31km-u063-en.png`, `sbs-opening-31km-u063-en.png`, `sbs-opening-31km-u063-he.png`.
   - Main: six columns read as two dark-brown blobs that cover the city's north blocks. The lead's keep-clear box (yellow, about 13.9 km per side) reaches into the frame's top third, and Herzliya is hidden.
   - Head: four thin, pale grey-white wisps over the north blocks. The yellow boxes now stay at the frame's top edge, and Herzliya, Abu Kishk, Kafr Qasim and Qalqilya show (EN and HE; see `labels-diff.txt`). Main showed no name that head hides.
   - Petah Tikva shows on both trees in the wide view and on neither in the opening view (the same on main, so not caused by the Dakotas). **Magdiel shows in no frame on either tree** (its label never appears in the logs), so the Magdiel part is not confirmed.
2. **Close (20 km): each column's dark foot stands on its impact, no gap to the ground. It widens upward, turns light grey and see-through toward the top, and leans a little east-north-east. Buildings around and between can be seen.**
   - Files: `strip-close-20km-en.png` (5 timed frames), `head-close-20km-u063-en.png`, `sbs-zoom-close-20km-u063-en.png`, `sbs-close-20km-u063-he.png`, `strip-column-9km-en.png`, `sbs-column-9km-u063-en.png`, `sbs-zoom-column-9km-u063-en.png`, `strip-growth-20km-en.png`.
   - Main: the columns are opaque dark brown top to bottom and hide the buildings under them.
   - Head: the buildings around and between the columns can be seen. The columns are thin and light grey, and see-through aloft. At 9 km the dark, thin feet show as narrow dark stems rising from the ground beside the blocks. The pale upper parts are faint, and at 20 km the columns read as quite faint white wisps that are largely hidden by the tall blocks. A lean is hard to judge from straight-on north-up frames.
   - In-scene numbers (`COLS` lines, u063): the lowest head puff is 112-133 m up and 237-261 m wide (alpha 0.85-0.88 just above it), widening to about 1.5 km and alpha 0.02-0.10 near 4 km, drawn with `Particles/Unlit`. Main puffs are about 980-1060 m wide from 540-780 m up, with alpha 0.95 up to about 3 km, drawn with `Particles/Simple Lit`.
   - The timed frames show the puffs moving with no pops on either tree. The growth strip shows head's columns rising thin out of the fireball over the first life, where main's start as wide dark puffs.
3. **Mid-run frame with a plane in view: names under or right next to the plane still hide, and nothing else does. The bomb, blast and fireball are unchanged.**
   - Files: `sbs-plane-31km-u110-en.png`, `sbs-zoom-plane-31km-u110-en.png`, `sbs-plane-31km-u030-en.png`, `sbs-plane-31km-u030-he.png`, `sbs-plane-31km-u063-en.png`, `sbs-plane-31km-u090-en.png`, `sbs-plane-20km-u110-en.png`, `sbs-seq3-fireball-12km-en.png`.
   - Main: the lead's keep-clear box covers almost the whole frame, so only Tel Aviv shows at u110 and only Kafr Zibad at u030.
   - Head: the yellow box hugs the aircraft and its flag. Names outside it show: at u110 Jaffa, Herzliya, Ra'anana, Kfar Saba, Ramatayim and Abu Kishk; at u030 Kafr Zibad, Azzun, Kafr Thulth, Kafr Qasim and Anabta (in HE too).
   - The Tel Aviv settlement name shows inside the box on both trees, so settlement names seem to be exempt from the keep-clear. This is not checked in code.
   - In `seq3-fireball-12km`, head hides Jammasin el Gharbi, which lies under the lead's projected box with the aircraft overhead. Main shows it because main's box has a corner behind the camera (logged `keep=[behind]`), which seems to make the labels skip it.
   - The bomb's fireball and blast look the same on both trees.
4. **Whether the unlit light-grey top looks right against the lit board, and that the top tint over 1 doesn't clip to flat white.**
   - Files: `sbs-zoom-close-20km-u063-en.png`, `sbs-zoom-column-9km-u063-en.png`, `strip-column-9km-en.png`.
   - No flat-white clipping is visible: the tops read as soft light grey, partly see-through, over the buildings. Whether they look right against the lit board is the owner's call. They are pale and faint.

## Files

`captures-2613/`:
- Timed strips: `strip-close-20km-en.png`, `strip-column-9km-en.png`, `strip-growth-20km-en.png`.
- Full frame: `head-close-20km-u063-en.png`.
- Main left, head right: `sbs-wide-31km-u063-en.png`, `sbs-wide-31km-u063-he.png`, `sbs-opening-31km-u063-en.png`, `sbs-opening-31km-u063-he.png`, `sbs-close-20km-u063-he.png`, `sbs-column-9km-u063-en.png`, `sbs-plane-31km-u110-en.png`, `sbs-plane-31km-u030-en.png`, `sbs-plane-31km-u030-he.png`, `sbs-plane-31km-u063-en.png`, `sbs-plane-31km-u090-en.png`, `sbs-plane-20km-u110-en.png`, `sbs-seq3-fireball-12km-en.png`.
- Zooms, main top and head bottom: `sbs-zoom-wide-31km-u063-en.png`, `sbs-zoom-close-20km-u063-en.png`, `sbs-zoom-column-9km-u063-en.png`, `sbs-zoom-plane-31km-u110-en.png`.
- `labels-diff.txt`: the names shown per frame, main vs head, over all 34 frames each, including the ones not published here.
- `logs/`: the `E21PLAY` lines and the run status for each tree.
- `rig/`: the test, `run.sh`, `drive.sh`, `compilecheck.sh` and `compose.py`.

## Limits

- The camera aspect is pinned to 16:9 in batch mode. There is no window, so the camera is rendered into a texture each frame, as in the earlier rigs.
- The clock is pinned to fixed values instead of real play time. Both trees use the same values.
- The yellow box is the projected 3D box. The game hides a name when its pill overlaps that projection (plus a margin).
