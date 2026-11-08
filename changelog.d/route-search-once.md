### Fixed

- Behind the scenes: the automated player that plays the test games searched the whole map once for every hex it
  considered when moving a brigade into place for an operation card. It now searches once per brigade and picks the same
  moves. A full test game had used about 300 MB more memory since #2621. The game you play does not use this player.
