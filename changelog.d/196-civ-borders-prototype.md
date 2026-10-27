### Changed
- Region borders are stronger and more Civ-like (refs #196; owner ruling 27 Oct 2026, "A2: stronger bands
  (Recommended)"): the bands keep their weight when the camera pulls out (floors of 6 px colour, 1 px light, 12 px
  wash), both sides wash alike at 0.45 to a quarter of the hex, a light territory tint grows from 0.12 up close to
  0.25 at the overview, and the neutral dashes stay at least 8 px. The earlier look stays selectable with
  `bordervariant=legacy` in the WebGL URL (or `-bordervariant legacy` on desktop), and the prototype's other variants
  with `a`, `b`, `c`, `b2` or `c2`: B adds a dark outer rim, C an Okabe-Ito blue, and A is the lighter tier of A2.
