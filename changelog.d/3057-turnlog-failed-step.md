- The `?turnlog` End turn probe now times a refused or failed sim step (its `step_ms` was 0.00), and always closes its
  turn, even when End turn throws, so nothing after it is timed by mistake (Refs #3057). It changes nothing in the game.
