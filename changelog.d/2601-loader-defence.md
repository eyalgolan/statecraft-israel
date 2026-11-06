### Fixed
- The content loader now refuses a `recruit` type that is not infantry, an artillery or armour type that trains anywhere
  but a military camp, and a gdd-0.3 `military.fortify_turns` row that is not Agreed, each naming the file and field
  (Fixes #2601). Today's content already keeps all three, so the game is unchanged.
