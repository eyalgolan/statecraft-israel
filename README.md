# PR #2691 top-bar tier captures (Fixes #2688)

Recorded by an agent. In play: `Map.unity` with GameBoot off, `EnterPlayMode`, `GameRoot.Begin` (`-nostart`), so UiDirection runs. A throwaway EditMode test, never committed, ran on the e21-cap worktree through `gate_slots.py` and `machine_lock.py --shared`.

- main: `origin/main` `06f4ae6b82ec517c4858bbf6fb551db25c19528e`
- head: PR #2691 head `c9b625e2a3da42d7934d29bc2c9b5a7353617bad`
- Sizes: 1920x1080 (the size named in #2688 and the PR), 2560x1080 (21:9, panel 2194x926 logical, also one-row), and 1920x1200 (16:10, panel 1819x1137 logical, below `NarrowBelowPx` 1860, so roomy is impossible in both languages; this is the control).
- Red box: meters. Cyan box: screen buttons (`nav`). Values come from the run log (worldBound, panel px).

## Tier per shot

| Size | Lang | main 06f4ae6b8 | head c9b625e2a |
|---|---|---|---|
| 1920x1080 | HE | not roomy (icons 30 px, values 21 px, gaps mean 146 px) | **roomy** (icons 38 px, values 24 px, gaps mean 155 px) |
| 1920x1080 | EN | roomy (38 px, 24 px, mean 162 px) | roomy (unchanged) |
| 2560x1080 | HE | not roomy (30 px, 21 px, mean 146 px) | **roomy** (38.6 px, 24 px, mean 155 px) |
| 2560x1080 | EN | roomy (38.6 px, 24 px, mean 164 px) | roomy (unchanged) |
| 1920x1200 | HE | narrow tier, not roomy | narrow tier, not roomy (unchanged) |
| 1920x1200 | EN | narrow tier, not roomy | narrow tier, not roomy (unchanged) |

UpdateRoomy inputs in Hebrew 1920x1080: on main, the old formula gives used=-737, spare=-542 (meaningless, as #2688 says). On the head, used=1091, spare=265, and roomy needs spare >= 54.6.

## Files

- `sbs-zoom-<size>-<lang>.png`: top-bar zoom, main on top, head below.
- `sbs-full-<size>-<lang>.png`: whole frame at half size, main left, head right.
- `main-<size>-<lang>.png`, `head-<size>-<lang>.png`: the full frame with a caption.
- `main-<size>-<lang>-zoom.png`, `head-<size>-<lang>-zoom.png`: the top-bar zoom (x2 at 1920 wide, x1.5 at 2560).

- `head-1920x1080-en-zoom.png`
- `head-1920x1080-en.png`
- `head-1920x1080-he-zoom.png`
- `head-1920x1080-he.png`
- `head-1920x1200-en-zoom.png`
- `head-1920x1200-en.png`
- `head-1920x1200-he-zoom.png`
- `head-1920x1200-he.png`
- `head-2560x1080-en-zoom.png`
- `head-2560x1080-en.png`
- `head-2560x1080-he-zoom.png`
- `head-2560x1080-he.png`
- `main-1920x1080-en-zoom.png`
- `main-1920x1080-en.png`
- `main-1920x1080-he-zoom.png`
- `main-1920x1080-he.png`
- `main-1920x1200-en-zoom.png`
- `main-1920x1200-en.png`
- `main-1920x1200-he-zoom.png`
- `main-1920x1200-he.png`
- `main-2560x1080-en-zoom.png`
- `main-2560x1080-en.png`
- `main-2560x1080-he-zoom.png`
- `main-2560x1080-he.png`
- `sbs-full-1920x1080-en.png`
- `sbs-full-1920x1080-he.png`
- `sbs-full-1920x1200-en.png`
- `sbs-full-1920x1200-he.png`
- `sbs-full-2560x1080-en.png`
- `sbs-full-2560x1080-he.png`
- `sbs-zoom-1920x1080-en.png`
- `sbs-zoom-1920x1080-he.png`
- `sbs-zoom-1920x1200-en.png`
- `sbs-zoom-1920x1200-he.png`
- `sbs-zoom-2560x1080-en.png`
- `sbs-zoom-2560x1080-he.png`
