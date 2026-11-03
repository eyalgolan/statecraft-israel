### Changed
- Under the gdd-0.3 rules only, the sim's test fixture of the Historical start's population ledger puts each census
  city's population units in the region of `content/regions.yaml` that holds the city, and each starting formation's
  units in the region of its starting hex (Refs #1689). A unit with no region is refused. The ledger is not yet in the
  game; the game players play (gdd-0.1) is unchanged.
