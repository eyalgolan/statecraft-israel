# PR #2614 recapture: combat VFX timing after the fix (v2)

- **New head:** `b20a111275950ffeb43cd75044c0ee8f5bc51a27` (PR #2614, "the defenders' shake and flash and the destroyed unit's dust start when the hit lands")
- **Old head (first capture):** `423678c01a82d9af8700dce05d3a70ae019b5406`, branch `captures/2614-combat-vfx`, commit `8ed1b3198842c0ee83226b39af7d4873eda6202b`
- **origin/main:** `9d53cc54417d2c0a43c62ecaab7a95941b25f878` (guns t = 0.00-0.10 s only)
- **Rig:** the same rig as the first capture, copied. Play mode on `Map.unity` with `-nostart`, `GameRoot.Begin` (15 May 1948). Arab Legion unit 9 at 0,25 (strength 2, plus an undrawn unit 15 at strength 1), the rig-made artillery 14 at 1,25, Golani Brigade 0 at 1,24. The orders go through `SimBridge.Queue`. `Time.captureDeltaTime` is pinned at 0.05 s. The presenter's delegates are wrapped to log game times. v2 changes: close view only; the `destroyed` wrapper takes the new `(id, delay)` signature; each frame also logs the defender's `HitPending`; the run stops after the engagements (no march, End Turn or train).
- **Outcome:** the rig searches `RngState` for the same outcome on each build: guns cost the defenders 1,000 people and destroy nobody (unit 9 shakes); rifles destroy unit 9 (its dust); no retreat. The found state differs per build, because the boot state differs (head tries=1, main tries=66, old head tries=29). The outcome lines are identical.
- t = game seconds after `Queue` (frame 0). "Drawn" = the first rig frame where the probe shows the effect on.

## Timing: old head vs new head

**Guns (artillery 14 on unit 9; loss 1,000, nobody destroyed)**

| Event | old 423678c01 | new b20a11127 |
|---|---|---|
| attack request (`timedPlay`) | 0.000 | 0.000 |
| fire take starts (`started`) | 0.050 | 0.050 |
| muzzle flash drawn | 0.050 | 0.050 |
| impact call (`hit`, guns) | 0.050 | 0.050 |
| shake call (`defenderHit`) | 0.050 | 0.050 (queued: `HitPending` true 0.05-0.30) |
| defender shake + red hit flash drawn | **0.050** | **0.350** |
| shell burst scheduled | 0.350 | 0.350 |
| shell burst first drawn | 0.400 | 0.400 |

**Rifle (Golani 0 on unit 9; unit 9 destroyed)**

| Event | old 423678c01 | new b20a11127 |
|---|---|---|
| attack request (`timedPlay`); unit 9's token dropped at the rebuild | 0.000 | 0.000 |
| fire take starts (`started`) | 0.050 | 0.050 |
| impact call (`hit`, small arms) | 0.050 | 0.050 |
| shake call (returns false: no drawn defender left) | 0.050 | 0.050 |
| death-dust call (`destroyed`) | 0.050 | 0.050, `delay=0.120` |
| death dust puffs scheduled | **0.05 / 0.20 / 0.35** | **0.17 / 0.32 / 0.47** |
| volley puffs scheduled | 0.17 / 0.39 / 0.61 | 0.17 / 0.39 / 0.61 |
| death dust first drawn | **0.050** | **0.200** |
| first volley puff drawn | 0.200 | 0.200 |

**Verdict:** rifle: the death dust now starts with the first volley puff (both scheduled 0.17, both first drawn 0.20). Guns: nothing is drawn before 0.35 s now. The shake and flash are drawn at 0.35, which is the burst's scheduled time. The burst's first drawn frame is 0.40, so the shake shows one 0.05 s frame before the visible burst. The same 0.35-scheduled / 0.40-drawn gap was on the old head. Hypothesis, not checked: the token's `Tick` uses up a frame of the delay in the frame of the call, while the burst pool counts from the clock and lights its slot on the next frame. At 60 fps the gap would be about 17 ms.

## Main check: empty plinth at t = 0.00 (guns)

`main-vs-head-guns-t0-0.10.png`: **yes, main shows the same empty plinth at t = 0.00.** On main 9d53cc544 the guns' frame at 0.00 shows unit 9's plinth with no figure. The figure is back at 0.05 and 0.10. The head frames are the same. So this one-frame gap comes from main's rebuild (Changed before CombatResolved), not from #2614. In the probe, unit 9's token motion is still there at offset 0 at t=0.00 on both builds; only the figure is not drawn. (Separately: the "before order" frame on head shows two figures on the plinth.)

## Files

- `before-after-guns.png`, `before-after-rifle.png`: old head (top) vs new head (bottom), crops on the defender's spot, frames 0.00-0.60 (guns) and 0.00-0.80 (rifle).
- `head-guns-close-0to1.5s.png`, `head-rifle-close-0to1.5s.png`: new head, every 0.05 s frame from the order to 1.5 s, close view (8 km), cropped.
- `main-vs-head-guns-t0-0.10.png`: main vs head, guns, t = 0.00 / 0.05 / 0.10.
- `full/`: full frames: head guns 0.35 (shake, red badge flash, no burst yet), head guns 0.40 (burst), head rifle 0.20 (first volley and death puff, faint), main guns 0.00 (empty plinth).

## Notes

- The rifle's first puffs are faint at 0.20 s; the cloud reads clearly from about 0.30 s.
- One run per build. `git status --porcelain` was empty before and after each run.
