### Fixed
- The unit card's standing-order refusals ("Artillery does not hold ground" and the five others `Orders.Check` gives)
  now read in the UI's language: each is a public `Orders.Refused*` constant in the sim, with its English unchanged,
  and `SimBridge.ReasonText` maps it to a `unit.order_refused.*` string for the card, the order queue and the
  end-of-turn toast. The Hebrew lines wait for the native-speaker read (#564). Saves and goldens did not move
  (Refs #2040).
