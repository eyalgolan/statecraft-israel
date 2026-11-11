### Changed

- GDD 0.3 catalogue (Refs #1725): `combat.artillery_range` is now labelled Agreed instead of Default, after the
  owner-approved answer O5 (1 November 2026, #1668 issuecomment-6553884547): "2 hexes.", and the #2918 C2 audit.
  The combat catalogue reader refuses a `combat.artillery_range` row that is not Agreed (R-COMBAT-007). The value
  stays 2, so there is no change in the game.
- GDD 0.3 catalogue (Refs #2918): `combat.artillery_factor` is now Agreed at 2 instead of Default 1, after the
  owner's answer C1 (10 November 2026, #2918 issuecomment-6765662598): "200% wins (Recommended)". The row is a
  unitless multiplier, so 2 is the same 200% as artillery's live `combat_percent` in `unit_types.yaml`. The combat
  catalogue reader refuses a `combat.artillery_factor` row that is not Agreed (R-COMBAT-007). The catalogue is not
  read by combat yet, so there is no change in the game.
