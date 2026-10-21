# Craftsman, and the default that fills jobs

## Player story

As a player of the MVP, I want to train craftsmen in a city that has industry, so that its jobs earn more Money (GDD "Population and units": a craftsman "Generates more money than an ordinary civilian" [S1]); and I want each city's free jobs filled without an order, so that I am not assigning jobs by hand in twelve cities every month (the impact plan's Phase 3 risk [S2]).

## Rules

- A craftsman is a count per city, not a movable unit, and not one of the city's civilians (D-A2 [S2]) (R-CRAFT-001).
- `Train(city, craftsman, n)` follows the training rules (`training.md`) in a city with a completed industry project; a project still being built does not count (`Q-CRAFT-1`, decided 15 October 2026 [S3]) (R-CRAFT-001).
- The course takes 3 turns, the order's turn counted, and costs IL 0.5m a group: labelled design values (`Q-CRAFT-3`, decided 15 October 2026 [S3]).
- A craftsman fills an ordinary job slot. Of a city's employed, the craftsmen are counted first, and each employed craftsman earns twice `income_per_job_per_year` (IL 0.06m a year), so IL 0.12m a year; the multiple is a labelled design value (`Q-CRAFT-2`, decided 15 October 2026 [S3]) (R-CRAFT-002).
- On each housing step, after the refugees are housed, every city whose job default is on sets its employed to the smaller of its job slots and its civilians plus craftsmen, in ascending city id, at a cost of 0 Attention (`Q-JOBS-1`, decided 15 October 2026 [S3]) (R-CITY-009).
- An accepted `AssignWorkers(city, job, n)` fixes that city's number and turns its default off for the rest of the game (R-CITY-001, R-CITY-009). `job` stays the only role (unitless).
- Craftsmen take housing (R-HOUSE-003), count in Social cohesion's employed share (R-CITY-005) and in the cities' part of the national head count (R-CITY-008).

## Data touched

- `content/unit_types.yaml` `craftsman` (`facility: industry`, `training_turns`, `training_cost`) and `content/rules.yaml` `mvp.craftsman_income_multiple`, labelled design values listed in `DesignValuesTests`.
- `GameState`: a city's `Craftsmen` (in its hash record as `craftsmen=` only when not 0) and `JobsDefault` (`jobs=manual` only when off).
- The reference player sends no `AssignWorkers`; the default fills its jobs.
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: `scenario-mvp-absorb-first` moves (its `AssignWorkers` turns Tel Aviv's default off); the other goldens do not (`docs/determinism.md`).

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `CraftsmanTests`, `CitiesTests.TheJobDefaultFillsFreeSlots`, `ReferencePlayerTests` and `DesignValuesTests`.
- `uv run scripts/oss/rule_ids.py` finds a citing test for R-CRAFT-001, R-CRAFT-002 and R-CITY-009.

## Out of scope

- Showing craftsmen or the job default in Unity (Phase 3 PR 8), a way to turn a city's default back on, the winery (Phase 3 PR 6), and any balance pass on the values.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 3 implementation plan, PR 0's table and PR 5, docs/superpowers/plans/2026-10-15-gdd-phase-3.md

## Open questions

- None.
