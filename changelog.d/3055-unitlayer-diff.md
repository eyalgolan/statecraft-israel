### Changed
- End turn no longer rebuilds every unit token on the map: a token whose unit did not change is kept as it is, and only
  the tokens of units that moved, changed or were lost are built again or removed. A moved unit's token still slides from
  its old place, a lost unit still raises its dust, and an attack or a hit on an unchanged token plays on untouched
  (Refs #3055).
