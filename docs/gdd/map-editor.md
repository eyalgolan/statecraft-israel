# Map editor: hex edits and location placement

## Player story

As a map author, I want to change tiles/hexes and move named locations, then reopen the map in the game with the same data and placement, so that I can author the MVP geography without hand-editing generated outputs (GDD "Map and locations" [S1]; location-record requirements [S2]).
A designer edits existing hex properties and moves locations, inspects affected dependencies and regenerates without losing authored choices. The player views the map from a stable direction.

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
- The host is a Unity editor window. It saves edits to one override file, `content/map/overrides.yaml`, which map generation applies last so regeneration keeps the edits. The editable properties are a hex's terrain and control and a location's position; a location's id and sources stay unchanged. **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592))** [S4].

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed editing workable-land flags, region membership and supported static infrastructure while retaining `content/map/overrides.yaml`.* [S1]
### Camera and editing surface

The gameplay camera has fixed direction/pitch, pan and dolly in/out along its view, with no yaw/orbit/tilt control. Close views retain readable labels/selections. Military camps use enlarged highlighted markers without moving logical hexes or evidence positions.

Use the existing internal map/Unity authoring workflow as the first host. This is a designer tool, not campaign commands or running-save editing. Open generated inputs plus authored overrides.

### Tile and location operations

Under Amir's declined proposal, select a hex to edit supported existing biome/land fields, workable-land flags, authored region membership and supported static infrastructure. Show generated/override values and validation. The [GDD](../GDD.md#terrain-type-tilehex-type-new-types-tbd) owns the deferred expanded catalogue; the editor cannot invent it.

Select a location by marker/id/search, move by drag or explicit hex/coordinate entry, and rebind authored logical hex/visual placement together. Preserve id, aliases, historical coordinates, sources, review state and period names. Separately record visual offsets for shared-hex readability. Moving a marker never merges identities or edits source evidence.

Keep military/reception camps, founding sites, capitals and ordinary towns separately bound. Moves cannot create capacity, regions or people. Explain affected authored assignments before removing a field's eligibility.

### Persistence and regeneration

Save versioned overrides keyed by stable hex/location ids with base revision/hash in the owner-ruled `content/map/overrides.yaml` path.

Pipeline order is import, deterministic generation, overrides, dependency validation, bake/export. Generated outputs are not the persistence layer. Applying the same overlay reproduces authored placement. Missing ids, base conflicts or unsupported schemas fail explicitly rather than dropping edits.

Save atomically after validation; failure preserves the prior usable overlay. Support undo/redo, selected reversion and diff preview. Designer changes are distinct from source/review metadata; source coordinates require an explicit evidence edit rather than dragging.

### Dependency validation and review

Validate bounds, land/passability, biome ids, duplicate identities, missing capitals/regions, invalid facility parents, unworkable prepared fields and broken references. Inspect affected corridor geometry/endpoints, force anchors, targets, objectives, regional slots, shared-hex markers and label offsets.

Report impacts and required regeneration/manual changes. Do not silently reroute sourced railways, move historical anchors, change membership, delete places or alter target eligibility to pass validation. Invalid bindings block canonical export; a clearly marked separate draft overlay may save without being baked.

Historical edits retain content/sensitivity review gates. The editor never marks evidence checked or events reviewed. Gameplay remains in the sim; previews reuse validators where applicable.

## Data touched

- Existing map inputs and derived data: `content/map/hexes.json`, `content/map/settlements_1948.yaml`, `content/map/settlement_hexes.yaml` and `content/map/localities_1948.yaml`. The implementation must document which inputs or authored overrides are saved and which outputs are regenerated; a storage schema has not been selected [S1][S2].
- Related designated-site, city and corridor records: validate their identity and hex references when the authored placement changes. Their gameplay fields and historical evidence are retained [S2][S3].
- Transport authoring and bake inputs/outputs described in `docs/routes.md`: surface dependencies without treating a marker move as a new historical route [S3].
- Editor session state: selection, edit history, unsaved-change state and validation messages. No campaign save or simulation state is edited by this workflow [S1].
- Proposed override schema/importer/validator, existing generated tiles, placement and bake inputs.
- Evidence coordinates remain separate; ids align with docs/locations.md and content registries.
- Undo history, diff preview and diagnostics are internal state.

## Done when

- An author loads a map, selects a hex and a location, changes an allowed hex property, and moves the location both by dragging and by entering coordinates.
- Undo and redo restore both hex edits and location moves, including the corresponding logical binding and displayed position; discarding unsaved edits returns to the saved map.
- Save, close and reopen reproduce the same valid data in the editor and the game. Regenerating and baking the map retains the authored edits, and saving unchanged data produces no file diff.
- A moved location keeps its identity and historical evidence. City, site and corridor dependencies are reported, with no silently rewritten route geometry or dangling references.
- Invalid coordinates, unknown identities and unresolved broken references fail clearly without replacing the last valid saved map.
- Editing a hex and moving a location survives save, reload and regeneration.
- Undo/redo restores binding/placement without modifying identity/evidence.
- Broken route, regional-slot or parent-facility dependencies surface before export.
- Shared-hex markers stay individually selectable and retain ids.
- Failed save/export preserves prior usable data.
- Fixed-direction pan/dolly keeps enlarged camp markers readable.

## Out of scope

- Selecting the terrain-type catalogue, numerical terrain modifiers or new campaign rules.
- Changing historical source evidence or human review sign-offs as a side effect of moving a marker.
- Automatic route design, campaign settlement founding, unit orders, ownership changes and editing a running campaign.
- Replacing existing city, site or objective decisions with the location-priority roster.
Public player editing, runtime save editing, a replacement terrain catalogue, automatic research/rerouting and review bypass.

## Sources

- [S1] Game design of record, "Map and locations", docs/GDD.md
- [S2] MVP location roster and location-record requirements, docs/locations.md
- [S3] Route authoring, tracing and bake pipeline, docs/routes.md; designated corridors, docs/gdd/corridors.md
- [S4] [Owner decision on host, override file and editable properties](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592), 18 October 2026, checked 2026-10-18.
- [S1] Authoring/camera decisions and deferred terrain, docs/GDD.md.
- [S2] Location contract, docs/locations.md.
- [S3] Route pipeline, docs/routes.md.
- [S4] Map evidence/input records, content/map/hexes.json; content/map/settlements_1948.yaml; content/map/localities_1948.yaml.

## Open questions

- Which host environment runs the authoring tool? **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** a Unity editor window.
- Which authoring-data format or override layer stores edits, and how does the generation pipeline apply it? **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** one override file, `content/map/overrides.yaml`, applied last by map generation.
- Which hex properties are editable, and how is the future terrain catalogue mapped to the existing biome-based movement and defence framework? **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** a hex's terrain and control and a location's position are editable; its id and sources stay. The future terrain catalogue mapping remains open.
- Which placement rules apply to each location kind and to shared-hex city, site and corridor bindings?
Historical/scenario bindings remain in [the root register](../GDD.md#deferred-decisions). Host/path recommendations may vary only while satisfying this persistence and validation contract.
