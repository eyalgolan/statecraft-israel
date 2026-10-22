- GDD Phase 5 PR 15, slice 2: the unit card's Fortify and Hold orders (refs #754). In the GDD loop a unit's card shows
  Fortify and Hold (Q-UI-2's names, "Fortify" / "ביצור", "Hold" / "החזקה"), each only where the sim's new
  `Orders.Check` takes the order; where it does not, the card shows the sim's reason. A press queues the sim's
  `UnitOrder`, and the card says when the order takes effect. `Orders.Give` now runs `Orders.Check` first, so the card
  and the sim refuse the same orders for the same reasons.
