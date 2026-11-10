### Changed

- Map performance (Refs #3056): Tal Heuberger's special buildings no longer re-plan on every new game date. They
  re-plan when something their plan reads changes: the dated rows or built arms workshops, the unit tokens still on
  the map, the civilian figures' ground or the farm scenes' plan. The civilian figures changing at the same date (for
  example a builder at a new city project) now also re-plans them when no farm scenes are shown.
