- GDD Phase 5 PR 15, slice 4: the `smoke-orders` golden (refs #754). The WebGL player's `?smoke-orders` run (desktop
  `-smoke-orders`) plays the first turn as a player gives one unit order: it hides the event card, clicks Golani, presses
  the card's Move button, clicks the first lit hex and presses End turn, then logs `SMOKE ORDERS OK 1948-06-15 <hash>`.
  The sim's `GoldenRun.PlaySmokeOrders` pins the same state in the golden manifest; no other golden moves.
