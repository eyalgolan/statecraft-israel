- GDD Phase 5 PR 15, slice 3: the unit card's Attack order with its odds (refs #754). In the GDD loop a unit's card shows
  Attack (Q-UI-2's name, "Attack" / "התקפה") only where the sim's new `Combat.Check` takes some hex; where it refuses
  an enemy hex in reach, the card shows the sim's reason. Pressing it lights those hexes, and the card gives each the
  sim's strength ratio and the combat table's outcome bands for it (Q-UI-1; `Combat.Preview`, new `Combat.Outcomes`).
  A click on a lit hex queues the sim's attack, and the card says it comes at End turn. `Combat.Give` now runs
  `Combat.Check` first, so the card and the sim refuse the same attacks for the same reasons.
