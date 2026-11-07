### Fixed

- The whole-town landmarks find the map's building layer once and keep it, instead of walking the whole map hierarchy
  for it on every town swap, up to three times per camera per frame (frame cost finding, 7 November 2026).
