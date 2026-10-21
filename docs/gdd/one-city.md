# One city: housing, jobs, refugees

## Player story

As a player of the MVP, I want to put Tel Aviv's people to work and house the immigrants who arrive, so that the city earns Money and its people hold together (GDD objective 2, "Absorb immigrants" [S1]; decisions D-A4, D-A5 [S2]).

## Rules

- Civilians and refugees are counts per city, in population units of `people_per_unit` people (D-A1, D-A2 [S2]). The slice has one city, Tel Aviv (D-A4 [S2]).
- `AssignWorkers(city, job, n)` sets how many of the city's civilians work. It is refused, with the state unchanged, for a negative count, more than the city's jobs or civilians, an unknown city or role, or the legacy loop (R-CITY-001). It costs 0.5 Attention (a placeholder) (unitless).
- Each turn resolves in the order the owner approved for the plan's resolution phase (D-A6, #456 [S2][S3]): production, then housing and Social cohesion, then arrivals. A turn's arrivals are housed on the next turn.
- Production: each employed unit earns `income_per_job_per_year` IL millions a year, for the turn's share of a year (R-CITY-002).
- Arrivals: the CBS series in `content/aliyah.yaml` arrives as refugees, in whole units, counted cumulatively so that no unit is lost or counted twice (R-CITY-003, D-A5 [S2]). With twelve cities they are spread over all of them: see `cities.md`.
- Housing: every refugee becomes a civilian, without a job, on the housing step, with no hard cap; the people beyond the city's housing capacity are its overflow and lower its housing condition (R-CITY-004, R-HOUSE-001 to R-HOUSE-004, D-D1 decided 15 October 2026 [S2]). With twelve cities: see `cities.md`.
- Social cohesion: a share from 0% to 100%, the weighted housed share and employed share over all cities (R-CITY-005). At first no other system read it; the top bar shows it as a resource meter, in the GDD loop only (the owner's ruling of 15 October 2026 [S4]). It is now the `morale` indicator (R-NAT-001), which approval and the `low_cohesion` situation read; the employed share is one of its effect-web sources and housing reaches it through the `housing` indicator.

## Data touched

- `cities.yaml`, `unit_types.yaml` and `rules.yaml` `mvp:` and `attention_cost.assign_workers`: labelled design values in `content/` since the cutover (Q-CUT-1, R-LOOP-005), the overlay's placeholders before it (ruling 2 [S3]).
- `GameState`: `Cities` (counts), `Cohesion` (hashed as `coh=` when the state has cities; since removed: folded into `morale`, R-NAT-001), `Treasury`.
- Save files: the `work CITY ROLE N` line. Scenarios: `assign_workers` (and `content: mvp` until the cutover).
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: `mvp-slice` moved (retired at the cutover); `scenario-mvp-absorb-first` was new.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `CitiesContentTests`, `CitiesTests` and `ReferencePlayerTests`.
- `hash1950`, `hash1952`, `smoke5`, `scenario-cautious` and `scenario-reckless` do not move.

## Out of scope

- Training, sites, the objective card and the score (PRs 4 to 6), and any display (PRs 7 and 8).
- Housing condition (D-D2), overflow beyond capacity (D-D1), craftsmen (D-D4) and Social cohesion's effects (D-C1).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 3, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] The owner's ruling on showing Social cohesion, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/380, checked 2026-10-16

## Open questions

- The placeholder values (city start values, people per unit, income per job, the cohesion weights, the Attention cost) wait for the owner.
