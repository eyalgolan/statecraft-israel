### Changed

- Choosing which nearby map models stay loaded (people, rural scenes, aircraft and ships, special buildings, unit
  figures and camp tents) builds fewer temporary collections each frame; camp tents build none. The choice itself is
  unchanged.
- The people, rural scenes, aircraft and ships, special buildings and unit figures layers no longer build new anchor
  lists or key comparisons for that choice each frame, and people no longer build new component arrays when they
  switch between their far and near figures; what they show is unchanged.
