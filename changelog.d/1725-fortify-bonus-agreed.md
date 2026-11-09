### Changed

- GDD 0.3 catalogue (Refs #1725): `military.fortify_bonus` is now labelled Agreed instead of Default, after the
  owner's O1 answer of 1 November 2026 on #1668 ("+25% (main's 125%; the catalogue's 25 agrees). One turn to set.")
  and the #2918 C2 audit. The content loader refuses a `military.fortify_bonus` row that is not Agreed, as it already
  does for `military.fortify_turns` (R-ORDER-002). The value stays 25, so there is no change in the game.
