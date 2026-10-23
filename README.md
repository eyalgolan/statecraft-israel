# Hint chip vs the border layer's label on the two-row bar: before and after

- Before: main `e22dadf2c`. After: branch `ui/hint-chip-clears-layer-label` (the CSS fix).
- Map.unity booted as a player starts it (`GameRoot.Begin`), the opening decision card hidden with its own Hide button,
  no hex chosen. The UI is drawn into a RenderTexture of the window's size, so the 3D map is not in the picture (the
  white area is the map viewport's backdrop). Each `.txt` gives the root classes, the chip's and the label's bounds and
  whether they overlap.
- Before, at 960x1080, EN and HE: `overlap=True` (the chip covers the label's lower lines). After: `overlap=False`;
  1920x1080 and 390x844 are unchanged.
