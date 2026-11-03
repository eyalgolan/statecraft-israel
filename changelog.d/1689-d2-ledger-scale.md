### Changed
- Under the gdd-0.3 rules only, the sim's test fixture of the Historical start's population ledger counts each population
  unit as 1,000 people, the scale its census cities and formations are sized at, not the 10,000 meant for Open mode
  (Refs #1689, owner decision D2). It refuses a missing or nonpositive scale, and census or formation rows outside the
  Historical start. The ledger is not yet in the game; the game players play (gdd-0.1) is unchanged.
