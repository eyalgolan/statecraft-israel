### Fixed

- Content loading (Fixes #2512): a map override hex without `q` or `r` (`map/overrides.yaml`) and an event effect
  without `delta` are now refused at load with an error naming the file and the field, instead of loading as
  coordinate 0 or a change of 0. Shipped content already has both, so nothing in the game changes.
