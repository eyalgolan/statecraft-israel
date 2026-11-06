### Fixed

- Content checks: the loader now refuses an objective's card that opens after the game's start, closes before its end,
  or has `requires` or `conditions`, naming the file and field. Such a card could fire late or never once its objective
  is completed (R-OBJ-003). No shipped card is affected.
