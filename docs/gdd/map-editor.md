# Internal map editor and camera

## Player story

A designer edits existing hex properties and moves locations, inspects affected dependencies and regenerates without losing authored choices. The player views the map from a stable direction.

## Rules

### Camera and editing surface

The gameplay camera has fixed direction/pitch, pan and dolly in/out along its view, with no yaw/orbit/tilt control. Close views retain readable labels/selections. Military camps use enlarged highlighted markers without moving logical hexes or evidence positions.

Use the existing internal map/Unity authoring workflow as the first host. This is a designer tool, not campaign commands or running-save editing. Open generated inputs plus authored overrides.

### Tile and location operations

Select a hex to edit supported existing biome/land fields, workable-land flags, authored region membership and supported static infrastructure. Show generated/override values and validation. The [GDD](../GDD.md#terrain--tile--hex-types--tbd) owns the deferred expanded catalogue; the editor cannot invent it.

Select a location by marker/id/search, move by drag or explicit hex/coordinate entry, and rebind authored logical hex/visual placement together. Preserve id, aliases, historical coordinates, sources, review state and period names. Separately record visual offsets for shared-hex readability. Moving a marker never merges identities or edits source evidence.

Keep military/reception camps, founding sites, capitals and ordinary towns separately bound. Moves cannot create capacity, regions or people. Explain affected authored assignments before removing a field's eligibility.

### Persistence and regeneration

Save versioned overrides keyed by stable hex/location ids with base revision/hash. Suggested path: `content/map/authored_overrides.yaml`, a proposed implementation contract rather than a content file supplied by this doc revision.

Pipeline order is import, deterministic generation, overrides, dependency validation, bake/export. Generated outputs are not the persistence layer. Applying the same overlay reproduces authored placement. Missing ids, base conflicts or unsupported schemas fail explicitly rather than dropping edits.

Save atomically after validation; failure preserves the prior usable overlay. Support undo/redo, selected reversion and diff preview. Designer changes are distinct from source/review metadata; source coordinates require an explicit evidence edit rather than dragging.

### Dependency validation and review

Validate bounds, land/passability, biome ids, duplicate identities, missing capitals/regions, invalid facility parents, unworkable prepared fields and broken references. Inspect affected corridor geometry/endpoints, force anchors, targets, objectives, regional slots, shared-hex markers and label offsets.

Report impacts and required regeneration/manual changes. Do not silently reroute sourced railways, move historical anchors, change membership, delete places or alter target eligibility to pass validation. Invalid bindings block canonical export; a clearly marked separate draft overlay may save without being baked.

Historical edits retain content/sensitivity review gates. The editor never marks evidence checked or events reviewed. Gameplay remains in the sim; previews reuse validators where applicable.

## Data touched

- Proposed override schema/importer/validator, existing generated tiles, placement and bake inputs.
- Evidence coordinates remain separate; ids align with docs/locations.md and content registries.
- Undo history, diff preview and diagnostics are internal state.

## Done when

- Editing a hex and moving a location survives save, reload and regeneration.
- Undo/redo restores binding/placement without modifying identity/evidence.
- Broken route, regional-slot or parent-facility dependencies surface before export.
- Shared-hex markers stay individually selectable and retain ids.
- Failed save/export preserves prior usable data.
- Fixed-direction pan/dolly keeps enlarged camp markers readable.

## Out of scope

Public player editing, runtime save editing, a replacement terrain catalogue, automatic research/rerouting and review bypass.

## Sources

- [S1] Authoring/camera decisions and deferred terrain, docs/GDD.md.
- [S2] Location contract, docs/locations.md.
- [S3] Route pipeline, docs/routes.md.
- [S4] Map evidence/input records, content/map/hexes.json; content/map/settlements_1948.yaml; content/map/localities_1948.yaml.

## Open questions

Historical/scenario bindings remain in [the root register](../GDD.md#deferred-decisions). Host/path recommendations may vary only while satisfying this persistence and validation contract.
