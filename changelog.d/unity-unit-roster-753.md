### Changed
- GDD Phase 5 PR 14: the map's army tokens come from the sim's units (refs #753, #385). In the GDD loop `UnitRoster`
  draws one token per military unit in `GameState.Units` on its sim hex, grouped per hex; a unit that moved is placed
  once within one hex of its new hex, off the other tokens' hexes, and the spot is cached. A token's figures follow
  Q-UI-3 (3 at two thirds or more of the unit's starting strength, 2 at a third or more, 1 below), not the reserve
  call-up. The squadron and the ships still come from their rows. Etzioni (no sourced strength) and the brigades whose
  windows open later draw no token in the GDD loop until a sim unit exists for them. In the GDD loop the unit card no
  longer leads with the reserve call-up: an army token's card drops that line, and the squadron's shows only the security
  level. The legacy loop is unchanged.
