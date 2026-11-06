### Fixed
- The content loader now refuses a `cities.yaml` that is not exactly the `city` and `town` rows of
  `map/settlements_1948.yaml`, in that file's order (R-CITY-006, Fixes #2732). It names the missing city, the city out of
  place, or the row that is not a city or town. The shipped twelve cities already match, so the game is unchanged.
