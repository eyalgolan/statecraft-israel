### Changed

- The country's head count adds up its population groups in a fixed order, so the same game gives the same numbers
  however the groups are stored (Refs #2804). Debt and foreign currency move only in their last digits; nothing a
  player sees changes. A saved game made before it can load with "replay differs" (owner, 6 November 2026).
