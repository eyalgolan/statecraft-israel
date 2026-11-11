### Changed

- Map performance (Refs #2905): Tal Heuberger's hex-tile props no longer re-plan on every new game date. They re-plan
  when something their build reads changes: the dated stations, the special buildings' rows or built arms workshops,
  the unit tokens still on the map, the civilian figures' ground, the farm scenes' plan or the whole-town models. The
  civilian figures changing at the same date (for example a builder at a new city project) now also re-plans them
  when no farm scenes are shown.
