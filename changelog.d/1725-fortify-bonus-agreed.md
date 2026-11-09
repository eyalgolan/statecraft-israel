### Changed

- GDD 0.3 catalogue (Refs #1725): `military.fortify_bonus` is now labelled Agreed instead of Default, after the
  owner-approved answer O1 (1 November 2026, #1668 issuecomment-6553884547): +25%, one turn to set, and the #2918
  C2 audit. The content loader refuses a `military.fortify_bonus` row that is not Agreed, as it already
  does for `military.fortify_turns` (R-ORDER-002). The value stays 25, so there is no change in the game.
