- Round 2 of the opt-in hex-tile map, still presentation only and off by default.
  - **Buildings:** they draw the shipped b*-v2 town models at full detail. Our own decimated copies had lost faces, so
    grass showed through walls and windows. The tile budgets are raised to match, and a test counts see-through view
    rays.
  - **Regional variants:** two each for the north, centre and south of the wheat, citrus, olive, mixed-farm and forest
    tiles (a new `mixed_farm`).
  - **Time of day:** day, dusk and night presets (`?hex-tiles&hex-time=dusk|night`, desktop `-hex-time`). Night has
    lit windows. Everything is restored on teardown.
  - **Under attack:** a look for a hex (smoke, glow and a pulsing border) that the sim can drive later. It is not
    saved to any board.
  - **Hex Painter:** a palette of every tile, by kind and region, and every unit, with thumbnails. A brush paints
    tiles, units are placed with a facing, and day, dusk, night and under attack can be previewed.
  - **Units:** a character unit is one model × 8. The hex figures draw the Collection's whole figures in the editor;
    the grazing flock's sheep and donkey do too. Our decimated copies showed the ground through clothes, fleece and faces.
  - **Board capture:** the whole-board shot no longer cuts off the board's south edge.
  (docs/hex-tiles/README.md; refs #18)
