# Rules of the GDD v0.1 MVP

The rules of the 1948–1951 MVP in [docs/GDD.md](../GDD.md), as Phase 1 builds them behind the `systems.loop` switch
([the approved plan](../superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md), §6). Balance values are the owner's.
Until the owner sets them, they are placeholders in the dev and test overlay (`MvpSlice.Overlay`) or in test fixtures, never in `content/`.

## Turns

R-TURN-001: The turn length on each security rung (1 to 5) is read from the rules key `turn_lengths` of the era the turn starts in. Without the key it is quarter, quarter, quarter, month, week, today's table. The GDD's monthly turns set all five to month.

## The loop switch

R-LOOP-001: `systems.loop` in eras.yaml is `legacy` (the default) or `gdd`, and every era has the same value. Its ruleset id, which a save records, is `legacy` or `gdd-0.1`.

R-LOOP-003: `MvpSlice.Overlay` loads the real content with `systems.loop: gdd` in every era and `turn_lengths` all month, and adds the slice's placeholder city data (`cities.yaml`, `unit_types.yaml`, the rules' `mvp:` block and the GDD command kinds' `attention_cost`) where the content has none. It changes nothing else. Tests, the MVP goldens and the dev flag use it until cutover.

R-LOOP-002: The GDD state (cities, units, the next unit id, completed objectives, the score) is written into the state hash only when it is set. With the legacy loop the hash text is unchanged, byte for byte.

## Saves

R-SAVE-001: A save names the ruleset it was played under (`ruleset legacy` or `ruleset gdd-0.1`, save schema 2). A game refuses a save made under another ruleset, with a message to start a new game. A schema 1 save has no ruleset line and is legacy.

## Cities

Each GDD turn resolves in the order of the approved plan's section 2.3 (decision D-A6): production, then housing and Social cohesion, then arrivals.

R-CITY-001: `AssignWorkers(city, role, n)` sets how many of the city's civilians work in `role` (only `job` exists). It is refused, with the state unchanged and the Attention refunded, when the city or role is unknown, `n` is negative, or `n` is above the city's jobs or its civilians. The legacy loop refuses it before charging Attention.

R-CITY-002: Each turn every city's employed units add `employed * income_per_job_per_year * the turn's share of a year` to the Treasury.

R-CITY-003: Arrivals from the CBS series (`aliyah.yaml`) become refugees in the rules' `arrivals_city`. Let `P(t)` be the people the series brings from the first era's start to the date `t`, summed over the origin groups in ordinal order. A turn from date `d` to date `e` adds `floor(P(e) / people_per_unit) - floor(P(d) / people_per_unit)` refugees. `people_per_unit` is the same in every era (the loader refuses an era that changes it), so over any run of turns the refugees added are `floor(P(end) / people_per_unit) - floor(P(start) / people_per_unit)`: no unit is lost or counted twice. Before cutover the legacy population (`GameState.Population`) also counts the same arrivals; the two are separate views of the same people until Phase 3 makes arrivals counts.

R-CITY-004: Each turn, `min(refugees, max(0, housing_capacity - civilians))` of a city's refugees become civilians without a job. The cap is a placeholder while overflow (D-D1) is open.

R-CITY-005: Social cohesion is `clamp(0, 1, cohesion_housing_weight * housed + cohesion_jobs_weight * employed)`, where `housed` is civilians over civilians and refugees (1 with nobody) and `employed` is employed over civilians (1 with none), over all cities. It is a placeholder while D-C1 is open: no other system reads it.
