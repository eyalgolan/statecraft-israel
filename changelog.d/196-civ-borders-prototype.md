- Prototype, for the owner to choose from (draft, refs #196): Civ-like region borders behind a switch. With no switch the
  map draws exactly as before (Legacy). `bordervariant=a|b|c|a2|b2|c2` in the WebGL URL (or `-bordervariant ...` on
  desktop) picks one: A keeps the bands' weight when the camera pulls out (floors of 3 px colour, 1 px light, 12 px
  wash), washes both sides alike to a quarter of the hex, adds a light territory tint growing from 0.08 up close to
  0.25 at the overview and keeps the neutral dashes at least 8 px; B adds a dark outer rim, C an Okabe-Ito blue; A2, B2
  and C2 are the same on a stronger tier for gameplay03 (6 px colour floor, a 0.45 wash that holds farther in, a 0.12
  tint up close).
