- Prototype, for the owner to choose from (draft, refs #196): Civ-like region borders. The bands keep their weight when
  the camera pulls out (no far fade; floors of 3 px colour, 1 px light, 12 px wash), both sides wash their territory
  alike to a quarter of the hex, a light territory tint grows from 0.08 up close to 0.25 at the overview, and the
  neutral dashes stay at least 8 px. Variants: A (default), B (A with a dark outer rim), C (A with Okabe-Ito blue), and
  A2, B2, C2, the same on a stronger tier for gameplay03 (6 px colour floor, a 0.45 wash that holds farther in, a 0.12
  tint up close), switched by `bordervariant=a|b|c|a2|b2|c2` in the WebGL URL or `-bordervariant ...` on desktop.
