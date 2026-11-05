# PR #2614 in-play captures: combat VFX, march dust, defenders' hit reaction, train steam

- **Head:** `423678c01a82d9af8700dce05d3a70ae019b5406` (PR #2614, with #2672 and #2638 merged in)
- **Before:** origin/main `c224092b0b7bd4d973d56bea3ea48996dcdc32a6`
- **Rig:** a throwaway EditMode test enters Play mode on `Map.unity` with `-nostart`, builds the map and runs `GameRoot.Begin` (15 May 1948). It then edits the turn-start state, with no order committed yet. One unit is placed on each side of the one enemy hex drawn at the start (0,25, Arab Legion unit 9, NW of Jerusalem): a player artillery unit (id 14, made by the rig) at 1,25, and Golani Brigade (unit 0) at 1,24. Both are set to strength 30. Unit 9 is set to strength 2, with one extra enemy unit (15, strength 1, not drawn) on its hex. Arms are topped up. The rig searched `RngState` (`Simulation.Preview`) for this outcome: the guns' strike costs the defenders 1 unit and destroys none (unit 9 shakes); the rifles' strike then destroys unit 9 (its dust); nobody retreats. The same setup and outcome were used on both builds.
- **Path under test:** each strike is queued with `SimBridge.Queue` (the real Queue, Changed, CombatResolved, UnitAttackPresenter path). `Time.captureDeltaTime` is pinned at 0.05 s, and every frame is shot from the order to 1.5 s. There are two views: **mid** (attacker and defender, 14 km) and **close** (the defender's spot, 8 km).
- **Logging:** the presenter's delegates are wrapped by reflection, so the log records the game time of each call. On head these are `timedPlay`, its `started` callback, `hit`, `defenderHit` and `destroyed`. On main it is `play`. Each frame also logs the gun's flash, the attacker's firing state, every burst-pool slot (start time, drawn or not) and the defenders' shake offset.
- Sides by side are `sbs/*.png` (left origin/main, right head). Timed contact sheets are `seq/*.png`. Full-size head frames are `full/*.png`.

## Item 5: timing order (the key check)

t = game seconds after `Queue` (frame 0). On the 0.05 s frame grid, the gun's own clock says it fired at t = 0.000-0.050 (its flash is drawn at 0.05).

**Guns engagement (artillery 14 on unit 9; a loss of 1, no unit destroyed)**

| t (s) | head event (presenter call or first frame drawn) | main |
|---|---|---|
| 0.000 | attack request (`timedPlay`) | attack request (`play`, untimed) |
| 0.050 | **fire take starts** (`started`), gun fires: **muzzle flash drawn** (alpha 0.62) | muzzle flash drawn, fire take running |
| 0.050 | then, in the same callback: **defender shake** call (`defenderHit`, loss 1000 people) and **impact** call (`hit`, guns=True) | none |
| 0.050 | defender shake and red hit flash drawn (offset 109 m), flash off by 0.15 s, shake over by 0.33 s | none |
| 0.350 | shell burst scheduled (impact + `ShellFlightS` 0.3) | none |
| 0.400 | **shell burst first drawn** on unit 9's token (first frame after 0.35) | none |
| no death dust | no unit destroyed in this strike | none |

**Rifle engagement (Golani Brigade 0 on unit 9 at strength 1; unit 9 destroyed)**

| t (s) | head event | main |
|---|---|---|
| 0.000 | attack request (`timedPlay`); unit 9's token is dropped at the rebuild (Changed comes before CombatResolved) | attack request (`play`); token dropped |
| 0.050 | **fire take starts** (`started`) | fire take running |
| 0.050 | then: `defenderHit` call (returns false: no defender token is left drawn), **impact** call (`hit`, smallArms=True), **death dust** call (`destroyed`, id 9) | none |
| 0.050 | death dust puffs scheduled at 0.05 / 0.20 / 0.35; the first is drawn from 0.05 (opening small) | none |
| 0.170 | small-arms volley scheduled at 0.17 / 0.39 / 0.61 (impact + `VolleyS` 0.12, gap 0.22) | none |
| 0.200 | **volley dust first drawn** | none |
| no shake | the only drawn defender was destroyed, so nothing is left to shake | none |

**Verdict:** the impact is never before the shot. On both strikes, the impact, shake and death-dust calls come after `fire-take-start`, inside the same callback, at t = 0.050. The first drawn impact visuals come at 0.05 (shake), 0.05 (death dust, at once by design), 0.20 (volley) and 0.40 (shell). The muzzle flash and fire take are at 0.05. The two meet in the frame at 0.05, with the shot first in call order. Nothing of the impact is drawn at t = 0.000.

Caveat: the destroyed unit's *token* disappears at t = 0.000, at the map rebuild before the shot. This is how the PR (and main) work: only its dust waits for the fire.

## Shot list

| # | What | Files | Head | Main |
|---|---|---|---|---|
| 1 | Guns attack, defender's hex 0.3-1.0 s after the flash | `sbs/guns-close-pre`, `sbs/guns-close-f09` (0.45 s), `-f12` (0.60), `-f18` (0.90), `sbs/guns-mid-f12`; `seq/{head,main}-guns-close-0to1.5s`; `full/head-guns-close-f09` | Explosion flipbook burst at the defender's feet from 0.40 s. It swells into fire, then black smoke, through about 1.5 s. | Nothing on the hex |
| 2 | Rifle attack, defender's hex 0.12-0.8 s | `sbs/rifle-close-pre`, `sbs/rifle-close-f04` (0.20), `-f08` (0.40), `-f14` (0.70); `seq/{head,main}-rifle-close-0to1.5s`; `full/head-rifle-close-f04`, `-f08` | Brown-grey dust puffs on the spot from 0.20 s. They build into a cloud by 0.7 s, mixed with the death dust (same spot). | Nothing |
| 3 | Unit destroyed: its former spot 0-1 s | `sbs/rifle-close-f01` (0.05), `-f06` (0.30), `-f16` (0.80), `-f20` (1.00); `full/head-rifle-close-f06` | Token gone. A dust cloud rises where it stood from about 0.2 s, widest at 0.8 s and thinning at 1.0 s. | Token gone, bare ground |
| 4 | Hex under attack after End Turn, turn report open, same camera; look held at fixed times | `sbs/endturn-mid` (with the report), `sbs/endturn-mid-maponly`, `sbs/endturn-close-k00-maponly`, `sbs/endturn-wide-maponly`; `seq/{head,main}-endturn-look-10to12.2s` (12 frames, look t = 10.0-12.2 s) | Darker, denser flipbook smoke columns. Small animated flame flecks (burning frames) at the column feet; they change from frame to frame. | Lighter grey octagon smoke and pale 5-sided flame cones |
| 5 | Timing order, dense sequences | `seq/{head,main}-guns-mid-0to1.5s`, `-guns-close-`, `-rifle-mid-`, `-rifle-close-`; `sbs/guns-mid-f01` (flash frame) | See the tables above | Flash and fire take only |
| 6 | #2672: march dust, defenders' hit reaction | `sbs/march-close-f06`, `-f10`, `-f14`; `seq/{head,main}-march-0to1.2s`; `full/head-march-close-f10`; shake in `sbs/guns-mid-f01` and `seq/head-guns-mid` (0.05-0.30 s) | 3 move-dust puffs, placed at 0.20 / 0.35 / 0.50 s along the slide (Carmeli Brigade 6,9 to 7,9). They are faint and small: light grey wisps among the orchard. The guns' defender shifts (up to 109 m) with a red badge flash at 0.05-0.10 s. | Slide with no dust; no shake |
| 7 | #2638: steam train smoke and steam while running | `sbs/train-near-k0`; `seq/head-train-steam` (16 frames, 0.3 s apart, 2.5 km and 6 km) | The rig set the date to 10 March 1950 (the service train runs from 2 March 1950). Dark smoke and white steam puffs trail back from the running locomotive; 8-9 puff renderers are on. | Same train, no steam |

## Problems and notes

- Only one enemy unit is drawn on the map on 15 May 1948. The extra defender on its hex (15) is not drawn. So the guns' strike shows the shake but no death, and the rifles' strike shows the death and dust but no shake (no drawn survivor). No single engagement shows both.
- Whenever the first defender is the destroyed one, the rifle volley lands on its former spot, so the volley and death dust overlap there.
- Item 7 needed a date change to 1950 (no train in 1948). That is a state edit, not a turn.
- Item 4 frames are map only where the turn report covers the hex (`sbs/endturn-mid` shows the report open; `report=True` in the log).
- The march dust is faint at 9 km.
- Rig runs: head run 1 failed (the rig wanted a second drawn enemy). Head run 2 had no shake (wrong defender strengths) and no train (1948). Head run 3 had framing fixes still to make. Head run 4 and the main run are the published set. All runs: `git status --porcelain` empty after.
