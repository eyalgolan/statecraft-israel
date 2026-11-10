- A diagnostic End turn probe, off unless asked for: `?turnlog` in the WebGL page address (or `-turnlog` on desktop)
  writes one `TURNLOG` console line per End turn with the sim step's time, each event handler's time and the map
  layers' times, so the freeze at End turn can be measured in a real browser (Refs #2905). It changes nothing in the
  game.
