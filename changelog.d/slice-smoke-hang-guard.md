### Fixed

- The whole-slice smoke tests check each turn's progress and replay work instead of a one-minute time limit, so a
  busy machine no longer fails them; a five-minute hang guard remains.
