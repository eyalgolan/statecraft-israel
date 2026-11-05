- Sim hardening (Refs #2513): a qualified halutz list and a corridor's segment list can no longer be written through
  from a cloned game state, so a clone cannot change the original's hash. No change to play.
