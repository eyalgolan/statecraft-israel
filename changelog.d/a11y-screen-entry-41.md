### Fixed

- Keyboard: opening a screen from the side rail now puts the focus in the screen instead of leaving it on the rail
  button. Reaching the screen's first control took 16 Tab presses for the Cabinet and the Budget and 14 for Defence,
  because the focus stayed outside and Tab had to walk the rest of the HUD first. The focus goes to the screen's first
  control, leaving its close X one Shift+Tab back; a screen with nothing else that can take the focus takes its X. A
  screen opened with the mouse still moves no focus, and Escape and the X still hand the focus back to the rail button.
