# #2594 Hebrew after-captures: origin/main vs PR #2687

- Before: origin/main `06f4ae6b82ec517c4858bbf6fb551db25c19528e` (folder `main/`).
- After: PR #2687 head `3ac17cb3f6a4f9ae7a4112370546941e92e59aa1` (folder `head/`).
- Side by side, main on the left and head on the right (folder `sbs/`): every zoom, plus the phone-width full shots.
- Unity 6000.3.25f1, captured 5 Nov 2026 09:20-09:26 IST: one run per SHA, in the same worktree, with the same rig.
- Baseline for comparison: branch `captures/2594-he-baseline` (`4e6cd7d31`, main `0c976fb21`).

## Rig

The rig is the #2594 baseline rig, copied. It is a throwaway EditMode test that was never committed. It opens Map.unity with GameBoot off and enters Play mode (`-nostart`, no `-shot`). It then builds the map and calls `GameRoot.Begin`, so the real presenters and `UiDirection` run (`.lang-he` on the root in Hebrew). Every shot comes from the same new game (15 May 1948) with the same camera. The turn report opens after one End Turn (15 June 1948).

- The UI renders at 1920x1080 (one-row HUD) and is composited over the map camera.
- Boxes come from each element's worldBound in panel px, taken from the run log. Each caption names the SHA, the language and the width.

Changes from the baseline rig:

1. **Phone-width pass (new).** After the turn-report shot, the HUD root is sized to an upright 390x844 phone's panel width. That width is 979 panel px under the ScaleWithScreenSize 1920x1080 reference with match 0.5, so the HUD takes the three-row tier (`hud-three-row`).
   - The rig first tried swapping the panel's target texture to 780x1688. The play-mode panel did not resize, so the rig fell back to sizing the HUD root on the 1920x1080 texture, as `AdvisorMediaTests` does.
   - The root's height was clamped to 1080 panel px (not 2118). The top bar and the three-row layout are real, but the phone shots show the top 979x1080 panel px of the phone, not its full height.
   - The rig shoots the turn report at that width (`b-phone-*`), closes the report with its Close button, then shoots the top bar (`a-phone-*`).
2. Captions now also print flex-direction, margins and paddings from the log.
3. The `-unboxed-` files were dropped.

Notes, the same as the baseline:

- (c) EN uses the real Watch button and the real clip.
- (c) HE: main's catalog (and the PR's) has narration under `en` only, so Watch is hidden in Hebrew (`watchDisplay=None`). The rig forces inline playback the same way the baseline did: the same picture players, a silent narration under the `he` key, and `AdvisorPresenter.Watch` with ForceInline=true. This is the state a player would reach once Hebrew narration exists.

## Measured (panel px; HE)

| area | main 06f4ae6b8 | #2687 head 3ac17cb3f |
|---|---|---|
| (a) one-row: Security caption / level | `flex-direction: row`; caption 1215..1255 (LEFT), level 1265..1351 (right); level margin L/R 6/4 | `row-reverse`; caption 1311..1351 (RIGHT), level 1217..1303 (left); level margin L/R 0/6 |
| (a) phone three-row | `row`; caption 349..389 (left), level 399..485 | `row-reverse`; caption 445..485 (right, at the column's right edge), level 351..437 |
| (b) Security causes block padding L/R | 14/2 (indent on the left) | 2/14 (indent on the right, the reading side) |
| (c) X during inline playback (popup 1504..1904) | 1851..1895 (right corner) | 1513..1557 (left corner) |
| (c0) X before Watch | 1527..1571 (left) | 1527..1571 (left, unchanged) |

English is identical on both SHAs in every area: caption 596..646 then level 656..726, causes padding 14/2, and X at 1851..1895.

## Files (each `-he` and `-en`; the same names in `main/` and `head/`; `sbs/sbs-<name>` side by side)

| file | shows |
|---|---|
| a-topbar-security-caption, -zoom | (a) one-row 1920x1080 top bar: red `.meter-caption-row`, yellow the Security caption, cyan `#security-level` |
| a-phone-topbar-security-caption, -zoom | (a) phone width (three-row bar), the same boxes |
| b-turn-report-causes, -zoom | (b) turn report at 1920x1080: red `#turn-report`, yellow each `.turn-report-causes` block, cyan its first cause line |
| b-phone-turn-report-causes, -zoom | (b) the same at phone width |
| c0-advisor-popup, -zoom | (c) context: the opening tour's first advisor step before Watch |
| c-advisor-media-close, -zoom | (c) inline playback (`.advisor-watching`): red popup, yellow `.advisor-head`, cyan `#advisor-close` |
