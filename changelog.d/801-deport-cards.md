- Deport's follow-up cards (GDD Phase 6 PR 9, #801): `deport_acre` and `deport_nazareth`, drafts tagged `displacement`,
  each stating the in-game consequence and one sourced line of the place's history (Q-CARD-1 (c)). A new event key,
  `deported: <target>`, fires a card on the turn the player's Deport depopulates that place, and only then, outside the
  cap of 3 soft cards (R-EVT-007); the loader refuses one for an unknown target or one whose residents fled, one without
  the `displacement` tag, one with more than one option, one with `to`, `prisoners` or any other key it would wait for,
  one that is not soft or opens after the game's start, and a second card for the same place. The release gate holds both cards until the owner's sensitivity review (R-GATE-001), and Deport
  stays out of play (`mvp.deport_in_play: false`).
