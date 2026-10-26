### Changed

- `?navlog` now prints the pressed element and every ancestor above it, each with its classes, on every UI pointer
  down (#1728). A press that lands on an element nobody expected is usually a full-screen layer in front of the
  panels, and the rules that show those layers are descendant selectors, so the class that displays one can sit on
  any ancestor rather than only the HUD root. Diagnostic only, behind the existing flag.
