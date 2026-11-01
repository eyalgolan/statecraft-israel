### Fixed
- The unit card's Attack and Reinforce refusals ("A truce is in force", "Jerusalem has no military camp" and the others
  `Combat.Check` and `Training.Reinforce` give) now read in the UI's language, and each shows once: every reason is a
  public `Combat.Refused*` / `Training.Refused*` constant in the sim (a format where it carries a number, a town or a
  course), with its English unchanged, and `SimBridge.ReasonText` maps it to a `unit.attack_refused.*` or
  `unit.reinforce_refused.*` string for the card, the order queue and the end-of-turn toast (on the card and in
  the toast the town in its Hebrew name).
  The Hebrew lines wait for the native-speaker read (#564). Saves and goldens did not move (Fixes #2072).
