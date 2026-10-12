# Command prototype

Command is an opt-in experiment in direct unit control. It uses a fictional short mission on the existing map:
select brigades, scouts, a convoy and worker crews; harvest citrus, shelter immigrant groups, deliver supplies and
construct a camp and a settlement. This is a playability exercise, not a historical reconstruction of an operation.

## Launch and controls

For a WebGL build, open `index.html?prototype=command`. For a desktop player, pass `-prototype command`.
The flag must have the exact value `command`. Without it, the campaign and its start screen run normally.
Command opens its own paused briefing. Press its Begin button or Space to start.

- Click or drag a box to select; Shift adds units. Right-click moves units or attacks a patrol.
- 1 selects brigades, 2 scouts, 3 the convoy, and 4 workers.
- G assigns workers to citrus harvesting. C builds a camp at the depot. B builds a settlement at the southern outpost.
- H sends selected units to the depot; O sends them to the outpost. R recruits a brigade.
- Space pauses and resumes. Orders remain available while paused. F frames the mission.
- WASD/arrows pan, the wheel zooms, and the middle mouse button drags the view. The sector map moves the camera.
- Models opens a paused viewer of all 34 imported models; Previous/Next changes the model and Escape closes it.
- Restart resets the experiment. Reload without the flag to return to the campaign.

The briefing explains the objectives, costs and counters. Complete two deliveries, shelter two groups and finish the
southern settlement before the eight-minute limit. The convoy reloads and units repair near the coastal depot.

## Isolation and design review

Command starts a fresh session with the shared golden seed before any save-store access. It does not read, write,
move or delete the campaign autosave and does not wire the campaign's New Game or End Turn controls. Its mission state
is separate from `Statecraft.Sim`. Existing campaign unit and placement layers are hidden during the experiment and
restored on disposal. The existing terrain, map kits and routes remain in place.

`Assets/Scripts/Prototype/CommandModel.cs` deliberately contains experimental mission rules in Unity. This is an
exception proposed for maintainer design review, not a production rules architecture. Any decision to retain the mode
must first resolve that exception and plan migration into a separately tested deterministic simulation. The model
uses fixed steps for repeatable prototype tests; it makes no cross-runtime campaign determinism claim.

The flag controls runtime activation. It does **not** exclude the prototype code from release builds.
The prototype uses imported soldier, scout, worker, immigrant, convoy and building prefabs where available.
Known walking/idle states play on rigged figures; root motion is disabled so orders control movement. Threats remain
neutral procedural symbols. Procedural art is also the fallback when a bound model is unavailable. All 34 imported
models are accessible in the separate inspection viewer, including ships, aircraft and civilian models that have no
mission role. Model permissions and provenance are recorded with the asset collection; the code license does not
supersede those asset terms.
No new content rows, historical event claims, simulation goldens, scenes or project settings are required.

## Validation and limits

`CommandPrototypeTests` covers exact flag parsing, geometry, pause, economy, worker-only harvesting, convoy reloads,
construction, rescue, fixed-step repeatability, failure, restart and a complete winning mission.
`CommandArtTests` checks every catalog resource, mesh/material completeness, unit bindings and symbolic scaling.
`CommandIntegrationTests` opens the actual map scene to check save protection, default startup, restoration and teardown.
Run these alongside the existing Unity EditMode suite and repository checks in `CONTRIBUTING.md`; verify the controls
in a WebGL player as well. Test results belong in the PR, not in an assumption that an opt-in path is safe.

Units move directly between clamped mission coordinates. There is no pathfinding, terrain avoidance or fog of war.
The mode is currently intended for mouse and keyboard. Its timings, costs and combat are toy values for comparison.
