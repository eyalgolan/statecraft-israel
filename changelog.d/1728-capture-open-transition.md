### Changed

- An EditMode guard that the capture dialog does not stay in its invisible start state (#1728). `ScreenNav.Show`
  opens a window in `window-opening`, which pins `opacity: 0` and `scale: 0.8`, and that class is taken off in one
  place only — on a `GeometryChangedEvent` — so a dialog that never gets one sits in the layout taking every press
  while invisible. `HudTests` covers the screens and the decision card this way; the capture dialog was not covered,
  and edit mode leaves `ScreenNav.Moves` off, so no test entered the transition path for it at all. No player code
  changes.
