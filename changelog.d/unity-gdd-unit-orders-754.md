- GDD Phase 5 PR 15, slice 1: the unit card's Move order (refs #754, #385). In the GDD loop a unit's card shows a Move
  button (Q-UI-2's name, "Move" / "תנועה") when the sim's `Movement.Reachable` has a hex for it. Pressing it lights
  exactly those hexes on the map. A click on a lit hex queues the sim's `Move` and the card says the unit moves at End
  turn. Any other click, a second press, a new turn or another selection ends the pick. Fortify, hold, attack and the
  `smoke-orders` golden come in later slices.
