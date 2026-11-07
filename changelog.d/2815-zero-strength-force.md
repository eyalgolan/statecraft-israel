### Fixed

- A starting force whose `map/units_1948.yaml` strength is 0 is now refused when the game loads, with "must be a whole
  number >= 1", instead of being skipped without a word. R-UNIT-006 says a force is never below 1. No row in the real
  file has 0 people, so nothing changes for players and no golden moves.
