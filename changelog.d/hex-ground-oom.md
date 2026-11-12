### Fixed
- The hex-tile board (`?hex-tiles=israel`) no longer runs out of memory at the first End turn on the web. It paints its
  ground 256 pixels square at a time through one reused buffer, instead of building the whole 2048 x 2048 alphamap at
  once (184 MB of floats that the WebGL heap kept for the whole session). The WebGL heap cap goes from 2048 MB to
  4096 MB, so there is room for all of Tal Heuberger's models on that board (Refs #3164).
