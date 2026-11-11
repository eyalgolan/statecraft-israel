### Fixed

- The enemy moves after End turn now take at most 12 seconds with the camera's glide back to your view included,
  not 12 seconds before it (Refs #3004). While they play, a held decision card's AI illustration stops, and plays
  again when the card comes back. End turn pressed again while they play, on a turn with nothing to play, opens the
  turn report once, for the new turn, and marks the attacked hexes for that turn only.
