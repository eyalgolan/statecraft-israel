### Added

- Dragging the map with the left mouse button pans it, the way map apps do (#998; owner's ruling of 22 October 2026).
  A plain left click still selects the hex and gives its orders. The pan starts only once the press leaves the click's
  30 px slop, so a click never nudges the map, and a drag selects nothing. The click is decided on the release rather
  than the press — a press cannot be known to be a drag until it moves — and is still queued once per physical press,
  so each click acts exactly once. Right-drag, the touch gestures and the keyboard are unchanged.
