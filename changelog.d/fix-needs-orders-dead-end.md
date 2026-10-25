### Fixed
- "A unit needs orders" always selects a unit again. A unit that moved next to its own units, where no clear spot was
  left in the hex, was not drawn, so the button did nothing and the unit could not be found. Such a unit is now drawn
  compact on its own hex, clicks on a hex with several units select each in turn, and as a last resort a unit that
  still cannot be shown skips the turn with a message (English, draft Hebrew).
