### Fixed
- The unit card's attack refusals ("A truce is in force", "That unit has no movement points left this turn", and "An attack
  needs N Arms and the stock has M") now read in the UI's language. Each is a public `Combat.Refused*` constant in the sim,
  with its English unchanged, and `SimBridge.ReasonText` maps it to a `unit.refused.*` string. A refusal the card repeats
  after translation shows once. The Hebrew lines wait for the native-speaker read (#564). Saves and goldens did not move
  (Refs #2072).
