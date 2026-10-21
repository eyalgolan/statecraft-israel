# City projects: housing types and industry

## Player story

As a player of the MVP, I want to build housing and industry in a city over several turns, so that the people arriving have homes and work (GDD "Cities and construction" [S1]; decisions D-A3, D-C6, D-D3 [S2]; the owner's answers [S3]).

## Rules

- Three kinds, in `content/projects.yaml`: permanent housing, the transit camp and industry. Their numbers are design values agents proposed and the owner may override [S3][S4].
- Permanent housing costs IL 6m, takes 6 turns and adds 5 units of capacity (5,000 people) and 0.03 housing condition, on its 0-to-1 scale (unitless) (R-PROJ-003).
- A transit camp costs IL 1.5m, takes 1 turn and adds 5 units of capacity but lowers the condition by 0.02 (unitless). It needs the ma'abarot policy, which carries its sourced date [S4] (R-PROJ-001).
- Industry costs IL 2m, takes 6 turns and adds 5 units of job slots (R-PROJ-003).
- `StartProject(kind, city, q, r)` goes on a land hex within 1 hex of the city's settlement hex that has no placement or other project. It is paid at once and borrows what the Treasury lacks. At most 2 projects (unitless) start a turn, nationally. It costs 1 Attention (R-PROJ-001).
- A project needs no workers (R-PROJ-001).
- `CancelProject(id)` stops a project being built; the Money is not returned. It costs 0.5 Attention (R-PROJ-002).
- Construction resolves on the turn's construction step, after the pioneer work; the turn of the order counts. A finished project adds its capacity, condition and job slots once (R-PROJ-003).
- A specialised project, such as the winery, needs a local resource: it starts only in a city whose `cities.yaml` row lists that resource with its sources, and otherwise it is an ordinary project. The winery needs vineyards, adds 10 units of job slots for IL 2m over 6 turns (design values, `Q-SPEC-2`) and goes in Rishon LeZion, its vineyards flagged `to_verify` (owner, 20 October 2026, on #737 [S6], replacing the 18 October default of Zikhron Ya'akov [S5]; R-PROJ-005).

## Data touched

- `content/projects.yaml` (new; design values), read only in the GDD loop.
- `rules.yaml` `attention_cost.start_project` and `cancel_project`.
- `GameState.CityProjects`, hashed as `cproj[...]` when there are some; the map actions' per-turn counter; the city's capacity, condition and job slots; `Treasury` and `Debt`.
- Save files: the `project KIND CITY Q R` and `unproject ID` lines. Scenarios: `start_project` and `cancel_project`.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `CityProjectsTests`, `DesignValuesTests`, `CommandGuardTests` and `SaveGameTests`.
- The goldens `hash1950`, `hash1952` and `mvp-end` (unitless) are re-pinned with a history row in docs/determinism.md.

## Out of scope

- Refusing the legacy map actions in the GDD loop (R-PROJ-004), a separate PR.
- The buttons and the drawing of projects on the map (Phase 3 PR 8).
- The craftsman (PR 5).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 3 plan, PR 0's recommended answers and PR 4, docs/superpowers/plans/2026-10-15-gdd-phase-3.md
- [S4] The ma'abara date and the map action costs, content/rules.yaml
- [S5] The owner's ruling on #737, 18 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/737#issuecomment-6249501060, checked 2026-10-18
- [S6] The owner's ruling on #737, 20 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/737#issuecomment-6296386086, checked 2026-10-20

## Open questions

- None for this slice: the owner took every recommendation on #375; the values remain his to override.
