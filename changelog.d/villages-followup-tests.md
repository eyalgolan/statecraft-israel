### Changed

- Panning a close view no longer allocates a sort delegate every frame while the map orders the towns and villages in
  view for loading; the order (nearest the screen centre first, then by id) is unchanged.
