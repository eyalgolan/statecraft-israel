### Fixed

- Keep soft events that exceed a turn's three-card cap waiting until a later free slot, and let objective cards fire without taking a slot.
  Saves now record the waiting events and are written as save schema 5. An older save still loads; if its game now plays
  out differently, it loads with the notice that some parts differ from the save, instead of being refused.
