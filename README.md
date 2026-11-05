# #2687 top bar at the new head (v2)

This set re-captures the top bar for PR #2687 at the head that will land. Only the Security caption shots are included.

- New head: `3d69baeb59638534b89e8051dad9b5dd0c46a1f9` (folder `head/`).
- origin/main: `9d53cc54417d2c0a43c62ecaab7a95941b25f878` (folder `main/`).
- Old head, the one the owner approved: `3ac17cb3f6a4f9ae7a4112370546941e92e59aa1`. Its PNGs are reused from v1 (branch `captures/2594-he-after`, commit `30006187179c00d11ed8a42e8635854fd3813c45`). They were not re-shot.
- Captured on 5 Nov 2026, 11:33-11:37 IST, with Unity 6000.3.25f1. There was one run per SHA, in the same worktree, with the same rig as v1 (copied unchanged).

## What changed vs v1

The new head adds one visible USS change, at `Statecraft.uss:118`: `#security-level { ... margin-right: 0 }`. This removes the default theme's 4 px trailing Label margin after the Security level.

- **EN, one-row bar (1920x1080):** the Security meter is 4 px narrower (189 -> 185 panel px), and so is its caption row (136 -> 132). Every meter to its right, from Public support to Foreign currency, moves 4 px left. In a pixel diff of the bar, the only change is at panel x=730..1379. See `sbs-old-vs-new/strip-a-topbar-onerow-en.png`.
- **EN, phone width (three-row bar):** the margin is now 0, but nothing moves. The meter fills its column, so its width stays 240. The bar is pixel-identical to v1.
- **HE, both widths:** pixel-identical to v1. The `.lang-he #security-level` rule (Statecraft.uss:1024) already set both sides (0/6).
- **Caption order (the HE fix) is unchanged:** in HE the caption is on the right and the level on the left (`row-reverse`), exactly as in v1.

## Security caption margins (panel px; the caption row's `#security-level` child; margin L/R)

| shot | main 9d53cc544 | old head 3ac17cb3f (approved) | new head 3d69baeb5 |
|---|---|---|---|
| EN one-row | level 656..726, margin 6/4 | 656..726, 6/4 | 656..726, **6/0** |
| EN phone | level 551..620, 6/4 | 551..620, 6/4 | 551..620, **6/0** |
| HE one-row | `row`: caption 1215..1255 (left), level 1265..1351, 6/4 | `row-reverse`: caption 1311..1351 (right), level 1217..1303, 0/6 | same as old head: 0/6 |
| HE phone | `row`: caption 349..389, level 399..485, 6/4 | `row-reverse`: caption 445..485, level 351..437, 0/6 | same as old head: 0/6 |

The Security caption label itself has margin 2/4 on every SHA. The EN Security meter / caption row widths are 189/136 (main and old head) and 185/132 (new head) on the one-row bar, and 240/233 on all three at phone width.

## Files

Each file comes in `-en` and `-he`.

- `main/`, `head/`: `a-topbar-security-caption` (the full 1920x1080 frame), `a-phone-topbar-security-caption` (phone width, three-row bar), and the `-zoom` of each (x3 on the Security meter). Red is `.meter-caption-row`, yellow the Security caption, cyan `#security-level`.
- `sbs-main-vs-new/sbs-*`: origin/main on the left, the new head on the right. Covers the zooms and the phone full shots.
- `sbs-old-vs-new/sbs-*`: the approved old head on the left, the new head on the right, with the same shots.
- `sbs-old-vs-new/strip-a-topbar-onerow-*`: the one-row bar (panel x=480..1440, x2), old head above and new head below. Magenta ticks every 20 panel px. This file shows the 4 px shift in EN and that HE does not change.

## Rig

The rig is the same throwaway EditMode test as in v1 (never committed). It opens Map.unity, enters Play mode, and calls `GameRoot.Begin` with `UiDirection` on. The phone width is the HUD root sized to an upright 390x844 panel width (979 panel px) on the 1920x1080 texture. Rects are each element's worldBound, read from the run log.
