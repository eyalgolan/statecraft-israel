- GDD Phase 6 PR 4: capture, resolver step 3 (refs #796, #386). At turn end, in ordinal id order, a curated target that
  starts under enemy control is captured when Israeli military units are on its hex and no enemy military unit is
  (Q-CAP-1 (a), Q-CAP-3 (a); R-CTRL-004). Its settlement record names Israel, awaits the player's choice and starts on
  the turn's end; enemy units alone on a captured target return it to enemy control (#796). A halutz holds nothing, and
  "enemy-held" means `mvp.enemy_held_localities` (owner, 21 October 2026, #796). A target that is not enemy-held at the
  start (Jaffa, #793) is never captured. The loader refuses a target whose hex is not its hex in
  `map/settlement_hexes.yaml` (R-CTRL-001). Capture changes no resident, unit, resource, city, score or objective and
  draws nothing from the RNG (R-CTRL-005). The shipped target list is empty, so no golden moves.
