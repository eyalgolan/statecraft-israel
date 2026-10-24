- Occupy and the occupation timer, GDD Phase 6 PR 6 (#798; R-CTRL-011 to R-CTRL-013): `ResolveCapture(settlement,
  occupy)` now keeps a captured place under Israeli control, with no effect on its residents. It needs an Israeli
  military unit on the hex, and no unit moves. Each turn the occupation costs `mvp.occupation_upkeep_per_turn` (IL
  0.005m), and resolver step 10 counts it down from `mvp.occupation_turns` (6 turns). At 0 the place waits for the
  end-of-occupation choice (PR 7). If no Israeli military unit is left on the hex, the occupation ends as a raid: the
  place is under enemy control again, its residents unchanged. Both numbers are design values, accepted by the owner on
  22 October 2026 (#1324). With the garrison formation's own upkeep (IL 0.05m, R-UNIT-010), holding a place costs
  IL 0.055m a turn in all. No golden moves: no golden captures a place.
