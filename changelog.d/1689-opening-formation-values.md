- GDD 0.3 starting population (Refs #1689): in the inactive Historical ledger fixture, each starting soldier now carries
  its formation's class (infantry) as its military qualification and its formation's opening combat experience from
  `content/gdd03-start.yaml`: 1 for Golani, Carmeli, Alexandroni, Kiryati, Givati, Etzioni and Harel, 0 for Negev, a
  labelled stand-in (owner, 7 November 2026: "Veteran for all 8 (Recommended)" and "1 each, Negev 0 (Recommended)").
  Keeping the veteran level on the formation and reading the class from `unit_types.yaml` is a maintainer call
  (eyalgolan-30's coordinator, 7 November 2026), not an owner ruling. The units a new game starts with keep 0 combat
  experience. No new-game wiring, so there is no change in the game.
