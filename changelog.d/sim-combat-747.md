- GDD Phase 5 PR 8: combat (refs #747, #385). `UnitOrder(unit, attack, q, r)` gives one of Israel's units a standing attack
  on an adjacent hex holding an enemy unit, and resolver step 2 resolves the attacks of both sides in ascending attacker
  id (R-COMBAT-001 to R-COMBAT-006): joint attacks sum their strengths, the defender's fortify and biome multiply its
  strength, one RNG draw per engagement picks a face of the combat table's row for the ratio, losses fall on the
  strongest unit, a unit at 0 is destroyed, a defender the row sends back retreats one hex or surrenders when it has
  none, and the attacker does not advance. Each Israeli attacker spends 1 Arms. Combat changes only the units and the
  Arms, never a city, locality, resident or the score, and each engagement's Why line gives the people both sides lost
  in the same words. The table, the terrain percents and the Arms per attack are new labelled design values in
  `content/rules.yaml`, accepted by the owner on #747 (20 October 2026, all six defaults: https://github.com/eyalgolan/statecraft-israel/issues/747#issuecomment-6299184133). The reference player now attacks at 3:2 or better, so the 1950, 1952
  and mvp-end goldens move (re-pinned, docs/determinism.md): unit 0 fights the Syrian unit beside it from 15 May 1948.
