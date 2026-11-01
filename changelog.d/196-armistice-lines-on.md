### Changed
- The 1949 armistice lines drawn as true polylines are on by default (#196; owner, 1 November 2026: "On by default
  (Recommended)"). A release build still draws and ships only the 11 lines the owner has signed off; the five draft ones
  stay out. Each line shows from its own agreement's date, so nothing changes on the map before 24 February 1949.
  `?noarmisticelines` in the web page's URL (`-noarmisticelines` on desktop) turns the lines off; `?armisticelines` is
  no longer needed. Display only: the sim never reads the lines and the goldens do not move.
- The minimap's Borders button hides and shows the true armistice lines with the borders (owner, 1 November 2026: "Tie
  to Borders button (Recommended)").
- The low-accuracy Jordan lines are drawn dotted (300 m dots, 450 m gaps) instead of dashed, so the legend's grey dash
  stays the sign of no-man's land and the demilitarized zones (owner, 1 November 2026: "Restyle Jordan lines
  (Recommended)").
