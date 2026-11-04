### Fixed
- An objective completed on the game's last turn (or on a defeat turn) now shows its card before the end screen, as on
  any other turn, and so does an objective card an earlier turn's card limit held back; the score is unchanged and no
  other event fires then (#2536).
- A save of a finished game whose last turn recorded an objective, or that had an objective card held back, now replays
  with that card fired, so it fails the replay check on load. Saves of unfinished games replay as before (#2536).
