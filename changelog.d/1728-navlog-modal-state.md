### Changed

- `?navlog` now also prints each modal card's own classes, resolved opacity, box and enabled state on a pointer down
  (#1728). Neither the capture dialog nor the decision card is an ancestor of the backdrop, so the pressed element's
  ancestor chain cannot show them: a press outside a card lands on the scrim, and the chain above the scrim says
  nothing about the card the scrim is protecting. A card held in `window-opening` stays in the layout and takes
  presses at `opacity: 0`, which is a modal the player cannot see; the class and the opacity together say whether
  that is what happened. Diagnostic only, behind the existing flag.
