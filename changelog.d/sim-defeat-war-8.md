- GDD Phase 7 PR 5, the war loss (refs #8, #387): an enemy military unit holding the hex of one of the player's GDD
  cities at the end of a turn ends the game at once with `defeat_war` (R-END-003; Q-WAR-5 and Q-DEF-3, owner, 17 October
  2026, #784: one turn is enough, any of the GDD cities, truces included), with its own reason line. The check runs in
  resolver step 11 after the economic count, so a turn the economic defeat also ends keeps `defeat_economy`. The
  reference run is never defeated, so no golden moves. The end-screen history note beside the cause (Q-DEF-4 (b)) goes
  with #812.
