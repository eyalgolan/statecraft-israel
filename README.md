# #2594 Hebrew baseline captures (before any fix)

- Code: origin/main `0c976fb2102c425dc29f98e25ee3932f7d4d7d7a`, Unity 6000.3.25f1, captured 5 Nov 2026 04:37-04:39 IST.
- How: a throwaway EditMode rig (never committed) opens Map.unity with GameBoot off, enters Play mode (`-nostart`, no `-shot`),
  builds the map and calls `GameRoot.Begin`, so the real presenters and `UiDirection` run (`.lang-he` on the root in Hebrew).
  UI rendered at 1920x1080 (PanelSettings ScaleWithScreenSize, reference 1920x1080, one-row HUD), composited over the map camera.
  Same new game (15 May 1948, turn 0), same camera, for every shot; the turn report is after one End Turn (15 June 1948, turn 1).
- Boxes: red/yellow/cyan as named in each caption. Rects are the elements' worldBound in panel pixels, from the run log.
- Each shot in Hebrew (`-he`) and English (`-en`, for comparison). `-zoom` files are crops of the same frame.

| file | shows |
|---|---|
| a-topbar-security-caption-{he,en}.png, -zoom-, -unboxed- | (a) top bar, Security meter: `.meter-caption-row` (red), caption label (yellow), `#security-level` (cyan) |
| b-turn-report-causes-{he,en}.png, -zoom- | (b) turn report after End Turn: each `.turn-report-causes` block (yellow) and its first cause line (cyan) |
| c0-advisor-popup-{he,en}.png, -zoom- | (c) context: the opening tour's first advisor step (opening.greeting) before Watch; popup still fading in |
| c-advisor-media-close-{he,en}.png, -zoom- | (c) inline media playback (`.advisor-watching`): `.advisor-head` (yellow) holding only `#advisor-close` (cyan) |

Measured (panel px):
- (a) HE: caption x=1215..1255 (left), level x=1265..1351 (right), `flex-direction: row`; EN: caption 596..646, level 656..726.
- (b) HE and EN: causes block x=662..1258 with padding-left 14 / padding-right 2; first cause line x=676..1256 (HE text right-aligned, flush to the 2px side).
- (c) HE before Watch: close x=1526..1570 (left, head is row-reverse); HE during playback: close x=1851..1895 (right), same as EN (1851..1895).

Notes:
- (c) EN used the real Watch button and the real clip (Media.Playing=True). HE: main's catalog has narration only under `en`, so the
  Watch button is hidden in Hebrew (watchDisplay=None) and a player cannot reach this state today. The HE shot forces it: same
  picture players, a silent narration put under the `he` key, `AdvisorPresenter.Watch` called on opening.greeting, ForceInline=true.
- The opening tour was opened with `AdvisorPresenter.OpenChain()` (the tour a player reopens); a fresh settings folder has no file, so it does not auto-open.
