- GDD 0.3 starting population (Refs #1725): on the inactive dark start's Historical ledger, ending a turn now runs the
  player's orders on a working copy and reaches the turn itself, which still refuses that game until a later change;
  an order whose system is not ready for the ledger refuses on the copy, so the game is unchanged. Delegated event
  cards are handed over only when one is waiting, and that is still refused. No new-game wiring, so there is no change
  in the game.
