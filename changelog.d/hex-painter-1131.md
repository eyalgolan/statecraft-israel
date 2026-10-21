- A map builder for the opt-in hex-tile map: region recipes (`tools/hex_tiles/regions.yaml`) lay out the whole map in
  one command (the `israel` board: 920 hexes, the game's settlements and the 1948 localities as towns and villages by
  type and population, the traced 1948 main roads), give each region its own tiles and ground and its own trees round
  the edge (oak and pine in the north, citrus and date palm on the coast, acacia and desert rock in the south). Hand
  edits live in `<board>.overrides.json`, in the shape of the map editor's `content/map/overrides.yaml` but outside
  `content/` (presentation only: tile, ground, unit, facing, region), and survive regeneration; boards also follow the
  map editor's terrain and location overrides. The editor-only Hex Painter (Statecraft/Hex Tiles/Hex Painter) picks a
  hex in the Scene view, changes its tile, ground, unit, facing or region, rebuilds just that hex and writes the edit
  there. Still presentation only and off by default (refs #18).

### Fixed
- Generated Hex Painter meshes, materials and terrain data are released when a preview is destroyed after an editor script reload (refs #18).
- Editor Hex Painter previews stay out of scene saves while remaining discoverable for the next load; ordinary scene objects and terrain picking are preserved (refs #18).
- Rebuilding a hex in the opt-in Hex Painter releases the removed unit's generated material while preserving shared assets and untouched units (refs #18).
