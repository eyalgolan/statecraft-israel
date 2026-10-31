### Fixed
- The KitTests trees-toggle check can fail now (#2026): with the trees off it moves the camera to the gameplay pose,
  where a layer that still drew would report the near count, instead of rendering the same overview pose again.
