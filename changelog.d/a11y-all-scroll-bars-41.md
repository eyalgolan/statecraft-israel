### Fixed

- Keyboard: a scroll bar no longer takes a Tab stop, in any screen rather than only the Cabinet. A scroll view hides
  its bar while its content fits, so the stray stop came back as soon as a list overflowed: in a short window the
  Budget's bar and the Defence body's each took one, and any list can overflow with enough content or in a short
  window. The turn report, the end screen and the capture dialog keep their bar, because it is the only way to scroll
  them from the keyboard. Dragging the bar and the mouse wheel are unchanged, and PageUp, PageDown, Home and End
  still scroll the open screen.
