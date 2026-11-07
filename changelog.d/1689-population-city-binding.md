- GDD 0.3 starting population (Refs #1689): a population record's location can now name its city, and the inactive
  Historical ledger fixture binds each census record to its city. A city outside the location's region is refused,
  naming both, and so is a member location that is neither a census city nor a field host. A location without a city keeps its saved text and hash. No new-game wiring, so there is no change in
  the game.
