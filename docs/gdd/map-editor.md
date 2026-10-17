# Map editor: hex edits and location placement

## Player story

As a map author, I want to change tiles/hexes and move named locations, then reopen the map in the game with the same data and placement, so that I can author the MVP geography without hand-editing generated outputs (GDD "Map and locations" [S1]; location-record requirements [S2]).

## Rules

- This is an internal authoring tool. Campaign construction, unit movement, ownership and combat remain simulation actions; an editor operation is not a campaign command [S1].
- Load the existing map and show its hex grid, coordinates, current hex data and named location markers. Support navigation, hex and location selection, and inspection of the selected record [S1].
- Let the author change the selected hex's permitted properties. Preserve unrelated fields. Terrain-type assignment uses the owner's agreed catalogue once defined; the existing biome data is not an automatic new terrain catalogue [S1].
- Let the author move a named location by dragging it to a target hex or entering hex coordinates. Preview the change and update its authored position, logical hex binding, label and rendered placement consistently [S1][S2].
- Preserve stable location identities, aliases, historical display names, sourced reference coordinates, dates, sources and review metadata. Store authored gameplay placement separately from historical reference evidence, and reuse existing identities rather than create a second record for an alias [S2].
- Support undo and redo for hex edits and location moves. Show unsaved changes and let the author discard them [S1].
- Save version-controlled authoring inputs in stable order. Reloading those inputs must reproduce the edited map in the editor and in the game. Authoring changes must survive the map-generation and bake pipeline; editing a generated output alone does not satisfy save [S1][S3].
- Reject unknown identities, invalid coordinates, out-of-map positions and edits that break applicable placement rules. Distinct locations can share a hex where the approved rules allow it; a shared hex alone is not a duplicate identity [S2].
- Before saving, report affected site bindings, city/project associations and corridor endpoints or segment bindings. Keep references by identity. Require the author to resolve broken dependencies explicitly; moving a location must not silently reroute sourced transport geometry [S1][S3].
- Report validation failures against the affected hex, location or dependent record. A failed save must leave the last valid saved map intact [S1].

## Data touched

- Existing map inputs and derived data: `content/map/hexes.json`, `content/map/settlements_1948.yaml`, `content/map/settlement_hexes.yaml` and `content/map/localities_1948.yaml`. The implementation must document which inputs or authored overrides are saved and which outputs are regenerated; a storage schema has not been selected [S1][S2].
- Related designated-site, city and corridor records: validate their identity and hex references when the authored placement changes. Their gameplay fields and historical evidence are retained [S2][S3].
- Transport authoring and bake inputs/outputs described in `docs/routes.md`: surface dependencies without treating a marker move as a new historical route [S3].
- Editor session state: selection, edit history, unsaved-change state and validation messages. No campaign save or simulation state is edited by this workflow [S1].

## Done when

- An author loads a map, selects a hex and a location, changes an allowed hex property, and moves the location both by dragging and by entering coordinates.
- Undo and redo restore both hex edits and location moves, including the corresponding logical binding and displayed position; discarding unsaved edits returns to the saved map.
- Save, close and reopen reproduce the same valid data in the editor and the game. Regenerating and baking the map retains the authored edits, and saving unchanged data produces no file diff.
- A moved location keeps its identity and historical evidence. City, site and corridor dependencies are reported, with no silently rewritten route geometry or dangling references.
- Invalid coordinates, unknown identities and unresolved broken references fail clearly without replacing the last valid saved map.

## Out of scope

- Selecting the terrain-type catalogue, numerical terrain modifiers or new campaign rules.
- Changing historical source evidence or human review sign-offs as a side effect of moving a marker.
- Automatic route design, campaign settlement founding, unit orders, ownership changes and editing a running campaign.
- Replacing existing city, site or objective decisions with the location-priority roster.

## Sources

- [S1] Game design of record, "Map and locations", docs/GDD.md
- [S2] MVP location roster and location-record requirements, docs/locations.md
- [S3] Route authoring, tracing and bake pipeline, docs/routes.md; designated corridors, docs/gdd/corridors.md

## Open questions

- Which host environment runs the authoring tool?
- Which authoring-data format or override layer stores edits, and how does the generation pipeline apply it?
- Which hex properties are editable, and how is the future terrain catalogue mapped to the existing biome-based movement and defence framework?
- Which placement rules apply to each location kind and to shared-hex city, site and corridor bindings?
