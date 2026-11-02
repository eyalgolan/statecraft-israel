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

*Considered and declined (owner, 30 Oct 2026; Regional pooling conflict, option A; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed editing region membership. For Amir's workable-land flags and supported static infrastructure, the review did not rule; main's `R-MEDIT-001` to `R-MEDIT-003` (`docs/rules/mvp.md:558`) cover only the authoring envelope. On 1 November 2026 the owner made these fields post-MVP: regions come from sub-districts and fields are authored data (Q36).* [S1] [S9]

The retained owner ruling keeps `content/map/overrides.yaml` as the override file [S4].
### Camera and editing surface

The gameplay camera has fixed direction/pitch, pan and dolly in/out along its view, with no yaw/orbit/tilt control [S5]. Close views retain readable labels/selections. Military camps use enlarged highlighted markers without moving logical hexes or evidence positions.

Use the existing internal map/Unity authoring workflow as the first host. This is a designer tool, not campaign commands or running-save editing. Open generated inputs plus authored overrides.

### Tile and location operations

**Amir's proposal, declined by the owner on 30 October 2026 (Regional pooling conflict, option A) [S1]:** the editor would add region membership to the retained terrain/control/location-position fields. That addition is non-operative. Workable-land flags and static infrastructure were not ruled on by the 30 October 2026 conflict review; they are post-MVP (owner, 1 November 2026: "30 Oct wins for the MVP (Recommended)"), since starting fields are an authored list (Q36) and `R-MEDIT-001` (`docs/rules/mvp.md:558`) is the MVP envelope [S9].

The [GDD](../GDD.md#terrain-type-tilehex-type-new-types-tbd) owns the deferred expanded catalogue; the editor cannot invent it.

Select a location by marker/id/search, move by drag or explicit hex/coordinate entry, and rebind authored logical hex/visual placement together. Preserve id, aliases, historical coordinates, sources, review state and period names [S6]. Separately record visual offsets for shared-hex readability. Moving a marker never merges identities or edits source evidence.

Keep military/reception camps, founding sites and ordinary towns separately bound. Moves cannot create capacity, regions or people. Proposed regional assignments and field-eligibility dependencies are non-operative [S1].

### Persistence and regeneration

Save versioned overrides keyed by stable hex/location ids with base revision/hash in the owner-ruled `content/map/overrides.yaml` path.

Pipeline order is import, deterministic generation, overrides, dependency validation, bake/export [S7]. Generated outputs are not the persistence layer. Applying the same overlay reproduces authored placement. Missing ids, base conflicts or unsupported schemas fail explicitly rather than dropping edits.

Save atomically after validation; failure preserves the prior usable overlay. Support undo/redo, selected reversion and diff preview. Designer changes are distinct from source/review metadata; source coordinates require an explicit evidence edit rather than dragging.

### Dependency validation and review

**Amir's proposal, declined by the owner on 30 October 2026 (Regional pooling conflict, option A) [S1]:** validation would include proposed region, prepared-field and regional-slot dependencies. Facility dependencies were not ruled on by the 30 October 2026 conflict review; `R-MEDIT-003` (`docs/rules/mvp.md:562`) names them as a missing adapter. They are post-MVP, because facilities are per city in the MVP (owner, 1 November 2026: "30 Oct wins for the MVP (Recommended)") [S9]. Retained editor validation remains limited to the approved editable fields and their existing dependencies.

Validate bounds, land/passability, biome ids, duplicate identities and broken references. Inspect affected corridor geometry/endpoints, force anchors, targets, objectives, shared-hex markers and label offsets. Broken corridor endpoints, force anchors, target bindings and objective bindings block export, since the sim's loader would refuse them; visual and label offsets only warn (owner, 1 November 2026, Q58) [S9]. Offsets use the axial hex (q, r) for placement plus an offset in metres from the hex centre, valid while the point stays inside its own hex (Q59) [S9].

Report impacts and required regeneration/manual changes. Do not silently reroute sourced railways, move historical anchors, change membership, delete places or alter target eligibility to pass validation. Invalid bindings block canonical export; a clearly marked separate draft overlay may save without being baked.

Historical edits retain content/sensitivity review gates. The editor never marks evidence checked or events reviewed. Gameplay remains in the sim; previews reuse validators where applicable.

## Data touched

- Existing map inputs and derived data: `content/map/hexes.json`, `content/map/settlements_1948.yaml`, `content/map/settlement_hexes.yaml` and `content/map/localities_1948.yaml`. The implementation must document which inputs or authored overrides are saved and which outputs are regenerated; a storage schema has not been selected [S1][S2].
- The map evidence/input records are catalogued in [S8].
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
- Broken retained route bindings surface before export. Proposed regional-slot and parent-facility validation is non-operative [S1].
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
- [S5] Authoring/camera decisions and deferred terrain, docs/GDD.md.
- [S6] Location contract, docs/locations.md.
- [S7] Route pipeline, docs/routes.md.
- [S8] Map evidence/input records, content/map/hexes.json; content/map/settlements_1948.yaml; content/map/localities_1948.yaml.
- [S9] Owner's 1 November 2026 answers, [GDD 1 November 2026 answers](../GDD.md#1-november-2026-answers).

## Open questions

- Which host environment runs the authoring tool? **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** a Unity editor window.
- Which authoring-data format or override layer stores edits, and how does the generation pipeline apply it? **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** one override file, `content/map/overrides.yaml`, applied last by map generation.
- Which hex properties are editable, and how is the future terrain catalogue mapped to the existing biome-based movement and defence framework? **Then decided (owner, 18 Oct 2026, "Unity editor window"; [#1009](https://github.com/eyalgolan/statecraft-israel/issues/1009#issuecomment-6254354592)):** a hex's terrain and control and a location's position are editable; its id and sources stay. The future terrain catalogue mapping remains open.
- Which placement rules apply to each location kind and to shared-hex city, site and corridor bindings?
Historical/scenario bindings remain in [the root register](../GDD.md#deferred-decisions). Host/path recommendations may vary only while satisfying this persistence and validation contract.
