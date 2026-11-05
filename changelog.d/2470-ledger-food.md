### Changed

- GDD 0.3: the monthly Food settlement can now read its need from the month's frozen population snapshot, one ration for
  each living person record whatever it is doing (working, on a course, travelling, serving), counted once each, so a
  change during the month does not move that month's need (Fixes #2470). The
  stock still stops at zero, and the import refill and the foreign-currency crisis work as before. No game uses the
  ledger yet, so play is unchanged.
