- GDD 0.3 starting population (Refs #1689, Refs #1725): an internal, test-only "dark start" builds a GDD 0.3 Historical
  game with its population ledger in place before the Food stock is set, so the stock starts at three months of the
  ledger's need (`cover_months` 3 in `content/rules.yaml`, one ration per person record). It still places the starting
  brigades, checked against the ledger's soldiers, and fires no opening event yet. The public new game is unchanged,
  so there is no change in the game.
