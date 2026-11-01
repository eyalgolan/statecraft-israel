- Deport's follow-up cards (GDD Phase 6 PR 9, #801): `deport_acre` and `deport_nazareth`, drafts tagged `displacement`,
  each stating the in-game consequence and one sourced line of the place's history (Q-CARD-1 (c)). A new event key,
  `deported: <target>`, fires a card once the player's Deport has depopulated that place, and only then (R-EVT-007); the
  loader refuses one for an unknown target or one whose residents fled, one without the `displacement` tag, and one with
  more than one option. The release gate holds both cards until the owner's sensitivity review (R-GATE-001), and Deport
  stays out of play (`mvp.deport_in_play: false`).
