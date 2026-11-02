### Fixed

- The turn report's Money line and the Money tip now list the cities' job income and the Money paid for Arms, training, projects,
  pioneer work, corridors, held soldiers and occupation, so the causes behind the Money (net) change add up to it. A loan is
  no longer listed as a Money cause: it adds as much to the debt as to the Treasury (Fixes #2199). The Why screen gains a
  Money line for each such cost, before its borrowing line. A map action's cost gets the same line (only the legacy loop
  builds map actions; the GDD game refuses them).
- The turn report's Money line and the Money tip still name the two largest causes, then one "other causes" line with the
  rest, so the lines shown add up to the Money (net) change. There is no such line when nothing is left over or the rest
  shows as IL 0.0m (#2212).
