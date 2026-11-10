### Fixed
- Event and outcome clips (Refs #2564): when one clip follows another on the same player (the next card, or the next
  End Turn outcome), a late "ready" or error from the previous clip can no longer start the new clip early or drop it to
  its photograph. Each clip now gets its own video player, and callbacks from the old one are ignored.
