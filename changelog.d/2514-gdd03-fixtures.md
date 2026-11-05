### Changed

- GDD 0.3 test fixtures follow the later rulings (Fixes #2514): the detached project-build fixture now gives a project
  one builder and counts each turn with it as one of the kind's turns, and the detached absorption check no longer
  blocks on refugees waiting for a bed. Nothing in the game calls either one, so play is unchanged.
