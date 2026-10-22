### Added

- The war state's last slice: R-WAR-004 pins that military actions move no parameter in the GDD loop - attacks,
  attacks refused in a truce and moves (the sim has no frontier, Q-MOVE-4, so crossing one is a move), with their
  losses, leave International Support and Social cohesion exactly where a turn without them does (Q-CIV-3,
  "No effect in Phase 5"); the prisoners' Food-shortfall cohesion cost is the one exemption. The behaviour was
  already true; the slice adds the rule and its
  regression/mutation test. Refs #750.
