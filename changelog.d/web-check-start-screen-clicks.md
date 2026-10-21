### Fixed
- `tools/web_check.py --startscreen` and `--persistence` pass again: they clicked New game at a fixed point that #1178's
  Star on GitHub button moved it away from. Under `?navlog` the start screen now logs each shown button's centre
  (`NAV ... start buttons start-new-game=0.4167,0.9278 ...`), whenever the layout moves it, and both runs add `navlog`
  and click there; a run whose page never logs the button fails at once instead of stalling. Diagnostic only: without
  `?navlog` nothing changes, and no golden moves.
