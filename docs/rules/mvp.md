# Rules of the GDD v0.1 MVP

The rules of the 1948–1951 MVP in [docs/GDD.md](../GDD.md), as Phase 1 built them behind the `systems.loop` switch
([the approved plan](../superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md), §6). Since the cutover (Phase 2 PR 1)
the real game runs them. Balance values are the owner's.

**15 October 2026 (owner, Q-CUT-1: "Labelled design values").** From the cutover, every balance number is a labelled
design value in `content/`, tagged on its line and listed by `DesignValuesTests` (R-LOOP-005). This replaces controller
ruling 2 of 12 October 2026, under which placeholders lived only in the dev and test overlay, never in `content/`.

## Turns

R-TURN-001: The turn length on each security rung (1 to 5) is read from the rules key `turn_lengths` of the era the turn starts in. Without the key it is quarter, quarter, quarter, month, week, the legacy table. The real game sets all five to month (D-B1), so turns from 15 May 1948 end on the 15th of each month.

R-TURN-002: An event that asks for a decision opens its window on the 15th on or before the historical decision, so the card reaches the player on the turn before it (#567 §4, owner, 15 October 2026). The exceptions are listed in the tests: `first_truce` (+5 days) and `rhodes_talks` (+2 days) arrive late by design, and the five war cards PR 1a rewrites as decisions during or after the fact.

R-TURN-003: In the historical run, no more than 6 months pass from one card to the next (or from the last card to the game's end), and no more than 2 when the country is at full war (security level 5) on any turn of the stretch (owner, 15 October 2026, Q-CUT-8: "6 months, 2 in wartime"; the M3 gap ruling restated in months).

## The loop switch

R-LOOP-001: `systems.loop` in eras.yaml is `legacy` (the default) or `gdd`, and every era has the same value. Its ruleset id, which a save records, is `legacy` or `gdd-0.1`.

R-LOOP-002: The GDD state (cities, units, the next unit id, completed objectives, the score) is written into the state hash only when it is set. With the legacy loop the hash text is unchanged, byte for byte.

R-LOOP-004: The GDD game ends on the rules key `mvp.end`, 15 January 1952 in the real content (owner, 15 October 2026, D-E5: "End on 15 Jan 1952"), after 44 monthly turns. The end is a boundary the last turn reaches, like the last era's end; the game is then over (`mvp_ended`) and raises no new event. It is the same in every era, after the start and before the last era's end (so the game ends as `mvp_ended`, not `era_ended`). The eras after it stay in the repository and load (G2).

R-LOOP-005: Every balance number of the GDD loop (all of `cities.yaml` and `unit_types.yaml`, the rules' `mvp:` block, and the GDD orders' `attention_cost`) carries the tag `# DESIGN VALUE: owner-set balance, not a historical figure` and its decision row on its own line, and `DesignValuesTests` lists every one of them: a number missing from the list, or a listed one without the tag, fails.

The ID R-LOOP-003 (the Phase 1 overlay, `MvpSlice.Overlay`) is retired with the overlay at the cutover and is not reused.

## Saves

R-SAVE-001: A save names the ruleset it was played under (`ruleset legacy` or `ruleset gdd-0.1`, save schema 2). A game refuses a save made under another ruleset. A schema 1 save has no ruleset line and is legacy, so the GDD game refuses it as made under earlier rules. The refusal carries a code (`older_rules`) the UI translates, and the sim's reason does not itself ask for a new game: the player reads that once (N4).

R-SAVE-002: A save that names a ruleset this build does not know, or a newer save schema, was made by a newer version of the game and is refused with the code `newer_version` (N2).

## Cities

Each GDD turn resolves in the order of the approved plan's section 2.3 (decision D-A6): production, then housing and Social cohesion, then arrivals.

R-CITY-001: `AssignWorkers(city, role, n)` sets how many of the city's civilians work in `role` (only `job` exists). It is refused, with the state unchanged and the Attention refunded, when the city or role is unknown, `n` is negative, or `n` is above the city's jobs or its civilians. The legacy loop refuses it before charging Attention.

R-CITY-002: Each turn every city's employed units add `employed * income_per_job_per_year * the turn's share of a year` to the Treasury.

R-CITY-003: Arrivals from the CBS series (`aliyah.yaml`) become refugees in the rules' `arrivals_city`. Let `P(t)` be the people the series brings from the first era's start to the date `t`, summed over the origin groups in ordinal order. A turn from date `d` to date `e` adds `floor(P(e) / people_per_unit) - floor(P(d) / people_per_unit)` refugees. `people_per_unit` is the same in every era (the loader refuses an era that changes it), so over any run of turns the refugees added are `floor(P(end) / people_per_unit) - floor(P(start) / people_per_unit)`: no unit is lost or counted twice. The legacy population (`GameState.Population`) also counts the same arrivals; the two are separate views of the same people until Phase 3 makes arrivals counts.

R-CITY-004: Each turn, `min(refugees, max(0, housing_capacity - civilians))` of a city's refugees become civilians without a job. The cap is a placeholder while overflow (D-D1) is open.

R-CITY-005: Social cohesion is `clamp(0, 1, cohesion_housing_weight * housed + cohesion_jobs_weight * employed)`, where `housed` is civilians over civilians and refugees (1 with nobody) and `employed` is employed over civilians (1 with none), over all cities. It is a placeholder while D-C1 is open: no other system reads it.

## Training

Training is step 4 of the turn in the approved order (§2.3, D-A6): it runs before production. One military camp and one Hakhshara exist in Tel Aviv at the start, a labelled design assumption (D-D5); pioneers will build more.

R-TRAIN-001: `Train(city, track, n)` takes `n` of the city's civilians without a job into a cohort that trains as `track` (a unit type with a `facility`: `recruit` in a military camp, `halutz` in a Hakhshara) for the type's `training_turns`, counting the turn it was ordered in. The Treasury pays `n * training_cost` at once. When the Treasury holds less, the shortfall is borrowed, as for map actions: it is added to the debt and the Treasury stops at 0 (owner decision "Allow borrowing", 15 October 2026). The trainees leave the civilians at once; the cohort takes the next id, which units share. It is refused, with the state unchanged and the Attention refunded, when the city or track is unknown, `n` is below 1, the city lacks the facility, or it has fewer civilians without a job. The legacy loop refuses it before charging Attention.

R-TRAIN-002: `CancelTraining(cohort)` returns the cohort's people to its city as civilians without a job and refunds none of the Money paid (D-A3). A cohort that does not exist, or has finished, is refused with the state unchanged.

R-TRAIN-003: Each turn, in ascending id, every cohort has one turn less to go. A cohort that reaches 0 ends: recruits are added to the city's count of recruits (a count until Phase 5), and halutzim become one movable unit per group at the city's settlement hex, taking the next ids in turn. A city's recruits and the cohorts are written into the state hash only when there are some.

## Sites

Construction is step 5 of the turn in the approved order (§2.3, D-A6): after training, before production. The designated sites are sourced facts in `content/sites.yaml`; Phase 1 has one, Mefalsim (owner decision on #378). The answers below are the plan's recommendations, which the owner took on 14 October 2026 (#378).

R-SITE-001: `PioneerWork(unit, build_yishuv, site)` starts building a yishuv at a designated site whose kinds include `yishuv` and which is neither being built nor built, with a halutz unit that has no other project (D-D6: one project per unit). The Treasury pays `build_yishuv_cost` at once. When the Treasury holds less, the shortfall is borrowed, as for map actions: it is added to the debt and the Treasury stops at 0 (owner decision "Allow borrowing", 15 October 2026). The site has `build_yishuv_turns` to go, counting the turn of the order. The unit does not move. It is refused, with the state unchanged and the Attention refunded, for an unknown unit, a unit that is not a halutz or is busy, an unknown action or site, a site that is not for a yishuv, or a site being built or built (one unit per site). The legacy loop refuses it before charging Attention. Its Attention cost is 0 (D-A6: unit orders are free).

R-SITE-002: `StopWork(unit)` ends the unit's build: the site is available again, the unit is idle, and the progress and the Money paid are lost (D-A3). A unit that does not exist or is not building is refused with the state unchanged. Its Attention cost is 0.

R-SITE-003: Each turn, in ordinal site id order, every site being built has one turn less to go. A site that reaches 0 is built, on the turn's end date, and its unit is idle again. The unit stays a halutz unit, and no city, housing, job, population or Social cohesion changes. Sites being built or built are written into the state hash only when there are some.

## Objectives

R-OBJ-001: An objective (`objectives.yaml`) names its condition, `completes_when: { site_built: <site> }`, a designated site of kind yishuv whose `founded` date must equal the objective's `reference_date`. In the last step of each GDD turn (spec §2.3, step 11; D-A6), in id order, an objective whose site is built and that is not yet recorded is recorded once, with the turn's end date (the site's `BuiltOn`). Recording it again, or in the legacy loop, changes nothing; a stopped build completes nothing.

R-OBJ-002: Recording an objective adds `objective_points + min(objective_bonus_cap, objective_bonus_per_month * monthsEarly)` to the score, once. `monthsEarly` is `max(0, monthsFromHistory)`, and `monthsFromHistory` counts calendar months from the completion's month to the reference date's month, `(y2 - y1) * 12 + (m2 - m1)`: 0 in the same month ("on time, as in history"), negative when late (owner's card wording, #379, 14 October 2026). Late costs nothing. The owner set 100 points (D-B6) and +5 a month capped at +60 (D-B2) on 14 October 2026 (#456); they are balance values and stay tunable, as `rules.yaml` design values (R-LOOP-005). The rules are those of the date the turn starts from (docs/determinism.md, "Rules per era"), so a turn that ends on the next era's first day scores with the era it started in.

R-OBJ-003: An event with `objective:` is that objective's card. It is eligible only once its objective is recorded, so it fires once, at the end of the turn the objective is completed in, and never in the legacy loop. A card has exactly one option, and an objective and its card name each other (the loader checks both). An unanswered card resolves to its option without drawing from the RNG.

## Money

R-MONEY-001: The GDD's Money is the Treasury (`model.yaml` money `treasury`, in IL millions): the player sees it as Money, כסף in Hebrew, on the top bar, the Budget screen and in the causes (spec §1.2, "Treasury becomes Money"; owner, 15 October 2026, Q-MONEY-4 and Q-HUD-3). Debt and foreign currency stay in the sim and the state hash, internal under Money, so the sourced 1951 currency crisis still happens (D-C3, Q-MONEY-1). Income is the per-head tax and spending until Phase 3, with job income (R-CITY-002) on top, and nothing pays upkeep until Phase 3 has cities to charge (Q-MONEY-2, decided 15 October 2026).

R-MONEY-003: A GDD cost above the Treasury is paid, and the shortfall moves to debt, as map actions do: the Treasury stops at 0, and a cause names what the shortfall was borrowed for (Q-MONEY-3, owner decision "Allow borrowing", 15 October 2026, for training and pioneer work; the Phase 2 plan applies it to every GDD cost). It covers the deficit, map actions, training (R-TRAIN-001), pioneer work (R-SITE-001) and an event option's Treasury cost.

The top bar shows the net position, Treasury minus debt (`HudModel.From`), so it reads below 0 once anything is borrowed.
