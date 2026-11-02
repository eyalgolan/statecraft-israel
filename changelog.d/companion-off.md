### Changed
- The read-only WebGL companion (#1119) is off and unavailable for now (the owner's ruling of 2 November 2026, "Remove it
  from the game (Recommended)"): Settings has no "Screen reader companion" row, and `?companion=1` does nothing. The code
  stays in, switched off by one flag (`WebCompanion.Available`), so it can come back later.
