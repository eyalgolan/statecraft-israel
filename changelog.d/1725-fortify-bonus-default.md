### Changed

- GDD 0.3 catalogue check (Refs #1725): the content loader now refuses a `military.fortify_bonus` row whose basis is
  not Default, as R-ORDER-002 names it, the way it already refuses a `military.fortify_turns` row that is not Agreed.
  The shipped catalogue row is Default, so there is no change in the game.
