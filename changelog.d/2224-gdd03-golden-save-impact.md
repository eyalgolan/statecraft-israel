### Changed
- The Israel Bonds card (`israel_bonds`) stays out of gdd-0.3, and its exclusion in `content/gdd03-events.yaml` now
  gives the real blocker: the owner's 2 November 2026 rulings (#2224) put an equal Debt on the launch option's $52m at
  the dollar rate in force on the event's date, and Debt is not yet an event target. Only the reason text changes, but
  that file is part of the gdd-0.3 content hash, so this change by itself moves it (`7e09d2b3...` to `9501253b...`).
  A gdd-0.3 save made before it still loads, with the "content changed" warning only (a 44-turn save replays to the
  same state). gdd-0.1, which the shipped content plays, never reads the file: its content hash (`6527512f...`),
  Hash1950, Hash1952 and mvp-end stay the same. Measured with a throwaway probe (not committed), command and full
  output in #2251.
- Measured for #2229's change (the three restored GDD 0.3 cards, merged before this), not for this one, with the same
  probe: no pinned golden moves (gdd-0.1 GoldenRun 1950 `8578966e...` and 1952 `385ba3b5...`, equal to the manifest's
  `hash1950` and `hash1952`/`mvp-end`, with and without it). Under gdd-0.3 it moves the content hash (`6d6d629b...` to
  `7e09d2b3...`) and the end state of a 44-turn no-orders run to 15 January 1952 (`4154a8e0...` to `9c5ecf53...`). A
  gdd-0.3 save made before #2229 loads with the "content changed" warning, plus "replay differs" when its replay ends on
  a different state (the 44-turn save did; a 1-turn save gave only "content changed"). The save format is unchanged.
