# #2751 captures: a parked aircraft keeps houses, trees and its badge off itself (Refs #2729)

Throwaway capture branch (orphan). Not for merging.

| | SHA |
|---|---|
| BEFORE (origin/main) | `ae4bfdaab5e92815faed8515ecc2604aed52c8ea` |
| AFTER (#2751 head, contains main) | `b9afed44fface9512f9df6c776254f9295302aa2` |

Between them are only three eyalgolan commits (`d18aabe60`, `fa349ee78`, `b9afed44f`).

How it ran: a throwaway EditMode test (`E21Park2751.cs`, kept here and never committed to the repo), in Play mode with `-nostart`, through `gate_slots.py` and `machine_lock.py --shared`, one Unity at a time. The Hebrew run used `he_IL` (`UiLanguage.Hebrew`, UI right to left), and the English run used `en_US`. Every run passed 1/1. The view is 101 Squadron's Spitfire, parked at rung 1 on 20 October 1948. The frames are 1920x1080 and use the same camera distances as `captures/spitfire-before`: min 6 km, close 12 km and normal 31 km, at 50 degrees down with yaw 0.

## Strips (`strips/`, BEFORE | AFTER)

- `sqn101-parked-*`: the camera is aimed on the plane in both panels. The token moved (see below), so the ground behind it is different.
- `sqn101-fixed-*`: the AFTER camera is aimed on the BEFORE plane centre, so the world view matches BEFORE exactly.

The outlines come from geometry the rig logged, not from hand placement:
- orange: the tree hole, a disc that reaches the corners of the plane's world-XZ box (r 3848 m);
- red: the house clearance, the 11.23 m span at 420x (r 2358 m);
- cyan: the badge;
- magenta: the projected rear 40% of the airframe (the tail).

The full-resolution frames are in `before/` and `after/`.

## What changed (logged in Play mode, same in EN and HE)

| | BEFORE | AFTER |
|---|---|---|
| Token hex / spot | -2,20 at (59538, 301700) | -1,19 at (61919, 305823), about 4.8 km NNE |
| Tree hole at the plane | none | 1, r 3848 m |
| Drawn tree crowns reaching the footprint | 41 | 0 |
| Town houses inside the span disc | 3 (Herzliya, nearest 1642 m) | 0 (nearest is a Ra'anana barn, 2666 m) |
| Badge vs tail, 12 km | badge y 175-337 inside the tail box (y 160-704) | badge bottom y 152, tail top y 160 |
| Badge vs tail, 31 km | badge y 420-513 over the tail (y 427-619) | badge bottom y 420, tail top y 427 |

## Odd things seen

- At 12 km, the badge is pushed up so far that its upper part sits behind the HUD's top bar (badge y -10 to 152, and the bar covers about y 0 to 70). Only its lower half shows.
- The token's new spot is next to the army token north of it (soldiers on a plinth, near Netanya). At 31 km the 101 badge now sits on that token's plinth. At 12 km the plinth's rim shows round the badge at the top of the frame.
- At 6 km the badge is above the frame in both BEFORE and AFTER, so that view shows only the trees.
- Unity rewrote `prickly_pear-rock.mat`, `tent-rock.mat` and `ProjectSettings.asset` on import in both trees, as on every run. They were restored, and the worktree was left clean.

## v4: re-shot at the new #2751 head (5 Nov)

| | SHA |
|---|---|
| BEFORE (origin/main, unchanged) | `ae4bfdaab5e92815faed8515ecc2604aed52c8ea` |
| AFTER v4 (#2751 head) | `2532a4d05d7c0ec456c9250a3a8dde9363dbc88f` |
| AFTER v3 (old #2751 head, above) | `b9afed44fface9512f9df6c776254f9295302aa2` |

Between BEFORE and AFTER v4 are only eyalgolan commits (`d18aabe60`, `fa349ee78`, `b9afed44f`, `dc7a50781`, `448b2f2dc`, `2532a4d05`). The three new ones change only `UnitLayer.cs` (the badge), its test and `docs/units.md`.

Same rig and cameras as v3: `E21Park2751.cs` and `run.sh` are byte-identical, so the BEFORE frames in `before/` are reused. Both runs (he_IL and en_US) passed 1/1. Unity rewrote the same three files on import; they were restored. `sbs2751v4.py` is the v3 strip script with the tags and labels as arguments.

- `strips-v4/`: BEFORE ae4bfdaab | AFTER 2532a4d05 (same layout and outlines as `strips/`).
- `strips-v4-vs-v3/`: old head b9afed44f | new head 2532a4d05, same camera in both panels.
- `after-v4/`: full-resolution AFTER frames. `crops-v4/`: 2x crops of the badge by the Netanya plinth at 31 km.

What the v4 frames show (EN and HE give the same geometry):

| | v3 (b9afed44f) | v4 (2532a4d05) |
|---|---|---|
| Token hex / spot | -1,19 at (61919, 305823) | the same |
| Badge, 12 km | y -10 to 152, top half under the HUD bar | x 452-614, y 175-337: all of it shows, below the bar |
| Badge vs tail box, 12 km | above it (bottom 152, tail top 160) | left of it (right edge 614, tail left 625) |
| Badge, 31 km | x 901-995, y 327-420: on the Netanya soldiers' plinth | x 743-837, y 420-513: clear of the plinth, left of the tail box (848) |
| Trees on the footprint / houses in the span | 0 / 0 | 0 / 0 |

Odd things seen:
- The badge now sits beside the aircraft, to the left of its tailplane over the beach, not over the fuselage.
- In the `sqn101-fixed-normal-31km` view (camera aimed on the BEFORE plane centre, 31 km depth), the badge's top-right corner still overlaps the lower-left rim of the Netanya plinth (`crops-v4/fixed-normal-31km-en-x2.png`). In the view aimed on the plane it is clear by a few pixels.
- At 6 km the badge is still above the frame.
