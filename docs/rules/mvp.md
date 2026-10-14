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

## Training

Training is step 4 of the turn in the approved order (§2.3, D-A6): it runs before production. One military camp and one Hakhshara exist in the slice's city at the start, a labelled design assumption (D-D5); pioneers will build more.

R-TRAIN-001: `Train(city, track, n)` takes `n` of the city's civilians without a job into a cohort that trains as `track` (a unit type with a `facility`: `recruit` in a military camp, `halutz` in a Hakhshara) for the type's `training_turns`, counting the turn it was ordered in. The Treasury pays `n * training_cost` at once and training does not borrow. The trainees leave the civilians at once; the cohort takes the next id, which units share. It is refused, with the state unchanged and the Attention refunded, when the city or track is unknown, `n` is below 1, the city lacks the facility, it has fewer civilians without a job, or the Treasury holds less than the cost. The legacy loop refuses it before charging Attention.

R-TRAIN-002: `CancelTraining(cohort)` returns the cohort's people to its city as civilians without a job and refunds none of the Money paid (D-A3). A cohort that does not exist, or has finished, is refused with the state unchanged.

R-TRAIN-003: Each turn, in ascending id, every cohort has one turn less to go. A cohort that reaches 0 ends: recruits are added to the city's count of recruits (a count until Phase 5), and halutzim become one movable unit per group at the city's settlement hex, taking the next ids in turn. A city's recruits and the cohorts are written into the state hash only when there are some.

## Sites

Construction is step 5 of the turn in the approved order (§2.3, D-A6): after training, before production. The designated sites are sourced facts in `content/sites.yaml`; Phase 1 has one, Mefalsim (owner decision on #378). The answers below are the plan's recommendations, which the owner took on 14 October 2026 (#378).

R-SITE-001: `PioneerWork(unit, build_yishuv, site)` starts building a yishuv at a designated site whose kinds include `yishuv` and which is neither being built nor built, with a halutz unit that has no other project (D-D6: one project per unit). The Treasury pays `build_yishuv_cost` at once and pioneer work does not borrow. The site has `build_yishuv_turns` to go, counting the turn of the order. The unit does not move. It is refused, with the state unchanged and the Attention refunded, for an unknown unit, a unit that is not a halutz or is busy, an unknown action or site, a site that is not for a yishuv, a site being built or built (one unit per site), or a Treasury that holds less than the cost. The legacy loop refuses it before charging Attention. Its Attention cost is 0 (D-A6: unit orders are free).

R-SITE-002: `StopWork(unit)` ends the unit's build: the site is available again, the unit is idle, and the progress and the Money paid are lost (D-A3). A unit that does not exist or is not building is refused with the state unchanged. Its Attention cost is 0.

R-SITE-003: Each turn, in ordinal site id order, every site being built has one turn less to go. A site that reaches 0 is built, on the turn's end date, and its unit is idle again. The unit stays a halutz unit, and no city, housing, job, population or Social cohesion changes. Sites being built or built are written into the state hash only when there are some.
