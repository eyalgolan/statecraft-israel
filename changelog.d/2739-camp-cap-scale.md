### Fixed
- In the gdd-0.3 basic training test fixture, a camp's cap of 50 now means 50,000 people at any ledger scale: 50 groups
  at 1,000 people a group, 5 at 10,000. Before, the synthetic fixture let 500,000 people into one camp. A scale that
  doesn't divide evenly rounds down (Refs #2739). It is not in the game yet; the game players play (gdd-0.1) is unchanged.
