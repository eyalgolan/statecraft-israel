### Fixed
- The start screen on a phone held upright (390x844) with the recording's row shown no longer logs Unity's "Layout
  update is struggling" error in its first layout: the band layout starts the panel's area under the painting on a
  whole screen pixel, so the panel's height and its centred position stop chasing each other (StartScreenPresenter.Fit).
