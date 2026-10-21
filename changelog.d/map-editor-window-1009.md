- The map override editor window (Statecraft/Map override editor) edits land terrain, one control period at a time and
  named-location positions in `content/map/overrides.yaml`, with map selection, coordinate and drag moves, Unity
  undo/redo, discard, and the city, site and corridor records a move touches listed before saving. Save checks the whole
  content through the game's loader first, so an edit that would break it (a settlement moved onto a designated site)
  leaves the saved file as it was; saving unchanged data does not rewrite the file. Unsaved edits survive a script
  reload, and closing the window or choosing Reload with them asks first (refs #1009).
