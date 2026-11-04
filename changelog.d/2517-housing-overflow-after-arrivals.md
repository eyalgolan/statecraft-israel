### Fixed
- Under the gdd-0.3 rules only, the detached housing test fixtures now count housing overflow from the housed residents
  after the step's arrivals, as the owner ruled (27M-15), not from the residents before them (Fixes #2517). The synthetic
  month context drops its unused pre-arrival resident list. The live housing step already counted after arrivals, so
  the game is unchanged.
