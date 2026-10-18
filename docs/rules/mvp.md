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

R-CITY-001: `AssignWorkers(city, role, n)` sets how many of the city's people work in `role` (only `job` exists: a craftsman fills an ordinary job slot, R-CRAFT-002). It is refused, with the state unchanged and the Attention refunded, when the city or role is unknown, `n` is negative, or `n` is above the city's jobs or its civilians and craftsmen. An accepted order turns the city's job default off (R-CITY-009). The legacy loop refuses it before charging Attention.

R-CITY-002: Each turn every city's employed units add `employed * income_per_job_per_year * the turn's share of a year` to the Treasury (an employed craftsman earns more, R-CRAFT-002).

R-CITY-003: Arrivals from the CBS series (`aliyah.yaml`) become refugees in the cities. Let `P(t)` be the people the series brings from `city_arrivals_from` to the date `t` (0 before it), summed over the origin groups in ordinal order. `city_arrivals_from` is 8 November 1948, the census day the cities' start values count: the census already holds the people who arrived from 15 May, so the cities take none before it (Q-ARRV-3, owner, 15 October 2026). The cities' arrived units by `t` are `U(t) = floor(P(t) / people_per_unit)`, and a turn from date `d` to date `e` adds `U(e) - U(d)` refugees over all cities. `people_per_unit` and `city_arrivals_from` are the same in every era (the loader refuses an era that changes either), so over any run of turns the refugees added are `U(end) - U(start)`: no unit is lost or counted twice. The units are split in proportion to the cities' census `people` (Q-ARRV-1): the first `U` units are handed out one at a time, unit `n` to the city with the largest remainder `n * people / total people - units it holds`, a tie to the lower city id (ordinal), and a turn gives each city its share of `U(e)` minus its share of `U(d)`. Building the split up one unit at a time keeps it cumulative: a fresh largest-remainder split of each total could take a unit back from a city as the total grows (the Alabama paradox; on the twelve cities it first happens at 7 units). The loader refuses a GDD game with no city.

R-CITY-008: `GameState.Population` is the one record of the nation's people: it takes every arrival from 15 May 1948 and is the count the CBS anchors check. The cities are the part of it placed in towns, so their people (civilians, craftsmen, refugees, recruits and the groups in training, times `people_per_unit`) never exceed it (Q-ARRV-2, owner, 15 October 2026). No person is counted twice.

R-CITY-004: Each turn, on the housing step, all of a city's refugees become civilians without a job (R-CITY-009's default may then give them jobs). There is no hard cap (D-D1, Q-HOUSE-1, owner, 15 October 2026): the people beyond the city's housing stay in the city as its overflow (R-HOUSE-002), and residents are housed automatically (Q-HOUSE-4).

R-CITY-009: The job default (Q-JOBS-1, owner, 15 October 2026). On each housing step, after the refugees become civilians (R-CITY-004), each city whose default is on sets its employed to `min(job slots, civilians + craftsmen)`, in ascending city id, at no Attention cost. Every city's default is on at the start; an accepted `AssignWorkers` order fixes that city's number and turns its default off for the rest of the game. Each change is a `city` cause on the Why screen ("Town: N groups in work"). A city's record writes `jobs=manual` into the state hash only when its default is off.

R-CITY-005: The cities' employed share (employed over civilians and craftsmen, summed over all cities; 1 with none) is an effect-web source of `morale`, Social cohesion (`work_lifts_cohesion`, source id `employed_share`), read after the turn's housing. The legacy loop has no cities, so there it reads 0 and adds nothing. Housing reaches Social cohesion through the existing `housing` link (`homes_lift_morale`) until Phase 3 makes it the cities' aggregate (Q-NAT-2, decided 15 October 2026). Phase 1's separate cohesion value and its two weights are gone (D-C1, Reading 2).

R-CITY-006: The cities are the `city` and `town` rows of `map/settlements_1948.yaml`, in that file's order, and their start values are `cities.yaml`'s. Each of a city's values (`people`, `employed_share`, `housing_capacity`, `job_slots`) is required and has a status: `sourced` (read from the source named), `derived` (computed from sourced values), `wikipedia-only` (an owner-accepted Wikipedia figure) or `design` (a labelled design value, R-LOOP-005). A `sourced`, `derived` or `wikipedia-only` value has at least one source, each with a url and a checked date, and the day (YYYY-MM-DD) or month (YYYY-MM) it describes; the loader refuses a value without them, naming the file and field (GDD Phase 3 plan, review focus 3).

R-CITY-007: A city starts with `civilians` = `people / people_per_unit` to the nearest unit (a half rounds up), and at least 1 when `people` is above 0 (Q-START-2, owner, 15 October 2026); `employed` = `people * employed_share / people_per_unit` to the nearest unit; `housing_capacity` and `job_slots` as written, in units. The loader refuses a city whose start employed is above its job slots. The arithmetic is decimal, so it is exact on every runtime.

R-HOUSE-001: Each city has a housing condition from 0 to 1, an average over the city (D-D2, Q-HOUSE-2, owner, 15 October 2026). Every city starts at `mvp.housing_start_condition` (Q-START-3: one design value for all the cities, 0.35, `model.yaml`'s `housing` base). The condition is per city: one city's counts never change another's. It is in the state hash (R-LOOP-002).

R-HOUSE-002: A city's overflow is `max(0, housed - housing_capacity)`, one capacity point housing one population unit (D-D1, Q-HOUSE-1, owner, 15 October 2026). On the housing step, after R-CITY-004 and before Social cohesion (D-A6), a city with overflow loses `overflow_condition_loss * overflow / housed` of its condition, never below 0; a city without overflow keeps its condition. `overflow_condition_loss` is 0.15, a design value. The loader requires both values, from 0 to 1. Overflow reaches Social cohesion through R-HOUSE-004 (a lower condition lowers the base of the national `housing` indicator) and the existing `homes_lift_morale` link (k 0.15) from `housing` into `morale`, which is Social cohesion (R-NAT-001); there is no direct overflow term (Q-HOUSE-1, decided by the owner 15 October 2026, plan:165 of `docs/superpowers/plans/2026-10-15-gdd-phase-3.md`: cohesion follows through the existing `housing` to `morale` link, "so it needs no number of its own"; the cohesion half was built after #728, merged as #857, owner 17 October 2026).

R-HOUSE-003: The people who take a city's housing (`housed`) are its civilians, its craftsmen, its recruits and its groups in training, a halutz group included until it becomes a movable unit. Halutz units do not (Q-HOUSE-5, owner, 15 October 2026). Refugees wait for housing and are not in it.

R-HOUSE-004: In the GDD loop the base of the national `housing` indicator is the people-weighted mean of the cities' housing condition, each city weighed by its housed people (R-HOUSE-003): `sum(condition * housed) / sum(housed)`. Policy effects, map actions and events still act on top, as on any indicator. Decided (owner, 17 Oct 2026, option (a) of Q-PR3-A, relayed on [#855 at 06:33Z](https://github.com/eyalgolan/statecraft-israel/pull/855#issuecomment-6230731177)): the national housing base is the people-weighted mean of city condition, with policy, map actions and events still applied on top. When no city houses anybody the mean is undefined, and the base is `model.yaml`'s. Capacity reaches the level through overflow and condition (R-HOUSE-002).

## City projects

A city project is built in a city (GDD "Cities and construction"; Phase 3 PR 4, #735). The kinds are in `content/projects.yaml`, every number a design value (Q-PROJ-1, Q-PROJ-5, owner, 15 October 2026, #375: "Take all recommendations", with agent-proposed values the owner may override): permanent housing (`housing`), the transit camp (`transit_camp`, D-D3, Q-HOUSE-3) and `industry`. The legacy map actions still run beside them; refusing them in the GDD loop (R-PROJ-004) is a separate PR. Until then a map action does not check the city projects, so one can still be placed on a project's hex; R-PROJ-004 removes that path.

R-PROJ-001: `StartProject(kind, city, q, r)` starts a city project of a `projects.yaml` kind for a city on hex (q, r). The Treasury pays the kind's cost at once and borrows what it lacks (Q-PROJ-1, Q-MONEY-3, "Allow borrowing"); the project takes the next id and the kind's turns, the turn of the order the first (D-A3). It needs no workers (Q-PROJ-3). It is refused, state and Attention unchanged, for an unknown kind or city; a transit camp without the `maabarot` policy at 0.5 or more (Q-HOUSE-3: the policy carries the camp's sourced date, and the map actions' 1 January 1950 window does not apply); a hex that is not land, is more than `map_action_max_distance` from the city's settlement hex, or holds a map-action placement or another city project, built or not (Q-PROJ-2); and once `map_actions_per_turn` projects were started this turn, counted nationally with the map actions (Q-PROJ-4). The legacy loop refuses it before charging Attention. Its Attention cost is `attention_cost.start_project` (1, Q-PROJ-5).

R-PROJ-002: `CancelProject(id)` removes a city project that is being built. The Money paid is not returned, and there are no people to return (D-A3, Q-PROJ-3). An unknown or built project is refused with the state unchanged. Its Attention cost is `attention_cost.cancel_project` (0.5, Q-PROJ-5).

R-PROJ-003: On step 5 of the turn (D-A6), after the pioneer work, every city project being built has one turn less to go, in ascending id. One that reaches 0 is built on the turn's end date and adds its kind's `capacity` to the city's housing capacity, its `condition` to the city's housing condition (kept from 0 to 1, R-HOUSE-001) and its `job_slots` to the city's job slots, once: a built project keeps its record, with its hex, and is never advanced again. `GameState.CityProjects` is hashed as `cproj[...]` only when it has a project (R-LOOP-002).

R-PROJ-005: A specialised project is allowed only in a city that has its resource (D-D8: "A winery requires areas for grapes"; Phase 3 PR 6, #737). A `projects.yaml` kind may name a `requires:` resource; a `cities.yaml` city lists its `resources:`, each with an id and sources (a url and a checked date each), and may say what the sources leave open (`to_verify`). `StartProject` of such a kind in a city without the resource is refused like the other R-PROJ-001 refusals, before the hex checks; a city founded in play has no resources. The loader refuses a resource without sources and a kind whose resource no city has. Otherwise the kind is an ordinary city project (R-PROJ-003). The one such kind is the winery, which needs vineyards, in Zikhron Ya'akov with its vineyards flagged `to_verify` (owner, 18 October 2026, on #737, "all defaults"); its content rows enter once a person has checked the vineyards' source (CONTENT_POLICY section 2, rule 6).

## Training

Training is step 4 of the turn in the approved order (§2.3, D-A6): it runs before production. One military camp and one Hakhshara exist in Tel Aviv at the start, a labelled design assumption (D-D5); pioneers build more at camp sites (R-CAMP-001).

R-TRAIN-001: `Train(city, track, n)` takes `n` of the city's civilians without a job into a cohort that trains as `track` (a unit type with a `facility`: `recruit` in a military camp, `halutz` in a Hakhshara, `craftsman` at industry, R-CRAFT-001) for the type's `training_turns`, counting the turn it was ordered in. The Treasury pays `n * training_cost` at once. When the Treasury holds less, the shortfall is borrowed, as for map actions: it is added to the debt and the Treasury stops at 0 (owner decision "Allow borrowing", 15 October 2026). The trainees leave the civilians at once; the cohort takes the next id, which units share. It is refused, with the state unchanged and the Attention refunded, when the city or track is unknown, `n` is below 1, the city lacks the facility, or it has fewer civilians without a job. A city has a facility when `cities.yaml` gives it one at the start or a camp site of that kind built for it is in the state (R-CAMP-001). The legacy loop refuses it before charging Attention.

R-TRAIN-002: `CancelTraining(cohort)` returns the cohort's people to its city as civilians without a job and refunds none of the Money paid (D-A3). A cohort that does not exist, or has finished, is refused with the state unchanged.

R-TRAIN-003: Each turn, in ascending id, every cohort has one turn less to go. A cohort that reaches 0 ends: recruits are added to the city's count of recruits (a count until Phase 5), craftsmen to its count of craftsmen (R-CRAFT-001), and halutzim become one movable unit per group at the city's settlement hex, taking the next ids in turn. A city's recruits and the cohorts are written into the state hash only when there are some.

## Craftsmen

R-CRAFT-001: A civilian becomes a craftsman by training (D-D4; Q-CRAFT-1, owner, 15 October 2026): `Train(city, craftsman, n)` follows R-TRAIN-001 to R-TRAIN-003, and the city's facility is a completed `industry` project (R-PROJ-003); one still being built does not count. `unit_types.yaml` gives the course's `training_turns` (3) and `training_cost` (IL 0.5m a group), labelled design values (Q-CRAFT-3). A finished cohort adds to the city's count of craftsmen, which is not part of its civilians and is written into the state hash only when not 0.

R-CRAFT-002: A craftsman fills an ordinary job slot (Q-CRAFT-2, owner, 15 October 2026). Of a city's employed, its craftsmen are counted first, and each employed craftsman earns `craftsman_income_multiple` (2, a labelled design value) times `income_per_job_per_year`; every other employed unit earns the R-CITY-002 rate.

## Sites

Construction is step 5 of the turn in the approved order (§2.3, D-A6): after training, before production. The designated sites are sourced facts in `content/sites.yaml`; Phase 1 has one, Mefalsim (owner decision on #378). The answers below are the plan's recommendations, which the owner took on 14 October 2026 (#378).

R-SITE-001: `PioneerWork(unit, build_yishuv, site)` starts building a yishuv at a designated site whose kinds include `yishuv` and which is neither being built nor built, with a halutz or veteran halutz unit that has no other project (D-D6: one project per unit). The Treasury pays `build_yishuv_cost` at once. When the Treasury holds less, the shortfall is borrowed, as for map actions: it is added to the debt and the Treasury stops at 0 (owner decision "Allow borrowing", 15 October 2026). The site has `build_yishuv_turns` to go, counting the turn of the order. The unit does not move. It is refused, with the state unchanged and the Attention refunded, for an unknown unit, a unit that is not a halutz or veteran halutz or is busy, an unknown action or site, a site that is not for a yishuv, or a site being built or built (one unit per site). The legacy loop refuses it before charging Attention. Its Attention cost is 0 (D-A6: unit orders are free).

R-SITE-002: `StopWork(unit)` ends the unit's build: the site is available again, the unit is idle, and the progress and the Money paid are lost (D-A3). A unit that does not exist or is not building is refused with the state unchanged. Its Attention cost is 0.

R-SITE-003: Each turn, in ordinal site id order, every site being built has one turn less to go. A site that reaches 0 is built, on the turn's end date, and its unit is idle again. A veteran's site has `veteran_progress` turns less to go (R-VET-002), and the unit's type changes only by R-VET-001. No city, housing, job, population or Social cohesion changes. Sites being built or built are written into the state hash only when there are some.

## Veteran pioneers

R-VET-001: In step 5, after its site's progress, a halutz unit whose site is being built gains one turn of service, the turn the site is built included; an idle unit gains none. At `veteran_halutz_service` turns of service it becomes a `veteran_halutz` at the end of that turn, keeping its id, hex, service and project, and a cause says so. A veteran gains no more service. A unit's service is written into the state hash only when it is not 0 (D-D9; Q-VET-1, owner, 17 October 2026, #782).

R-VET-002: A veteran's site being built has `veteran_progress` turns less to go each turn, against a halutz's 1; one with that many or fewer left is built that turn. A veteran does the same pioneer work as a halutz (R-SITE-001) (D-D9; Q-VET-2, owner, 17 October 2026, #782).

R-SITE-004: Every designated site has one or more kinds from a fixed list: `yishuv`, `field`, `swamp`, `military_camp` and `hakhshara` (Phase 4, Q-FIELD-1, Q-SWAMP-1, Q-CAMP-3). A pioneer action is accepted only at a site of its kind; any other is refused with the state unchanged and the Attention refunded. A yishuv site has a founding date inside the MVP's years and no other kind has one. A camp site (`military_camp` or `hakhshara`) names a city of `cities.yaml`, the city it trains for (Q-CAMP-1), and no other kind names one. The loader refuses any other row, naming the file and field.

R-SITE-005: At most one `yishuv` site sits in a hex, so a hex shows at most one new yishuv. A site of another kind may share a hex with a yishuv site, as a yishuv's field site does (Q-SITE-3).

R-FIELD-001: `PioneerWork(unit, plow_fields, site)` plows a field at a site whose kinds include `field`, by R-SITE-001 to R-SITE-003's rules, with `plow_fields_turns` and `plow_fields_cost` (labelled design values, Q-FIELD-3, decided 17 October 2026). A field site in a yishuv site's hex is ready only once that yishuv is built; before that the order is refused with the state unchanged and the Attention refunded (Q-FIELD-1, decided 17 October 2026). Built, the field site is plowed. A field site is not also a yishuv site, so building a yishuv cannot plow it; the loader refuses a row that gives it both kinds.

R-FIELD-002: Each turn in step 6, after the cities' production and in ordinal site id order, every plowed field adds `field_food_per_turn` Food units to the national stock (R-FOOD-001), with no workers (Q-FIELD-2, decided 17 October 2026). A field being plowed adds nothing. The Food arrives before the nation eats in step 7, so in a turn with imports (R-FOOD-002) the import tops the stock up by that much less. `field_food_per_turn` is 1, a labelled design value (Q-FIELD-3: "1 Food a turn"), and required in the GDD loop.

R-SWAMP-001: `PioneerWork(unit, dry_swamp, site)` drains a swamp at a site whose kinds include `swamp`, by R-SITE-001 to R-SITE-003's rules, with `dry_swamp_turns` and `dry_swamp_cost` (12 turns and IL 4m, labelled design values, required in the GDD loop; Q-SWAMP-1, decided 17 October 2026). Built, the swamp site is drained, and the field site in its hex becomes ready: before that a `plow_fields` order there is refused with the state unchanged and the Attention refunded, as at an unbuilt yishuv (R-FIELD-001). Drained, the swamp does nothing else. A field site is not also a swamp site, since draining would plow it; the loader refuses a row that gives it both kinds. The MVP's one swamp site is the Hula (Q-SWAMP-1; Q-SWAMP-3, "Allow as planned", owner, 17 October 2026, #789).

R-SWAMP-002: An event with `site: <id>` names a swamp site (the loader checks it in the GDD loop) and, in the GDD loop, can fire only while that site is being worked: it has a record that is not built. With no works, or once the swamp is drained, it waits, and it can still fire later in its window (Q-SWAMP-2, decided 17 October 2026). The legacy loop reads no sites and ignores the key, so the legacy goldens hold. `hula_drainage` carries `site: hula`.

## Camps

R-CAMP-001: `PioneerWork(unit, build_camp, site)` builds a training camp at a site whose kinds include `military_camp` or `hakhshara`, by R-SITE-001 to R-SITE-003's rules, with `build_camp_turns` and `build_camp_cost` (labelled design values, required in the GDD loop). One action builds either facility, as the site's kind says (Q-CAMP-3, owner, 17 October 2026, #782). Built, on the turn's end date, the camp gives the site's city (Q-CAMP-1) the site's facility, with no capacity limit, so `Train` accepts that city for the matching track from the next turn (R-TRAIN-001). A camp being built gives nothing. The facility is read from the built site's record, so the state hash gains nothing beyond the site's own record. The MVP's camp sites are Camp 80 (for Hadera) and Tzrifin (for Rishon LeZion), both `military_camp` (Q-CAMP-2, owner, 17 October 2026, #785; "Both military", owner, 17 October 2026, #927).

R-CAMP-002: A camp site's `city` is the MVP city (`cities.yaml`) nearest to the site by hex distance (Q-CAMP-1). When several are equally near, it is the one of them nearest by straight-line km from the point the site's hex comes from to the city's point in `map/settlements_1948.yaml` (owner, 17 October 2026, #927). Tzrifin's hex is one hex from Tel Aviv, Petah Tikva, Rehovot and Rishon LeZion, so it serves Rishon LeZion, about 3.9 km away.

## Military units

Phase 5 PR 2 (#741) adds the record and the types only; nothing moves, fights or trains into them yet (owner, 18 October 2026, #741: movement points in Phase 5 PR 5, upkeep in PR 3, enemy types in PR 4).

R-UNIT-001: A military unit is a group at formation scale, one token per formation (D-A1; Q-UNIT-1). It has a side, a type, a hex, a strength (an integer, in population units) and a standing order with its target. A military type in `unit_types.yaml` is a `unit` with a `side` (one of `units_1948.yaml`'s sides) and a `class` (infantry, artillery or armour); the loader refuses a type with only one of them, an unknown side or class, or a population type with either, naming the file and field. The MVP's military types are Israel's `veteran_soldier`, `artillery` and `armoured_car`; `recruit` stays a count per city (R-TRAIN-003).

R-UNIT-002: A unit's side, strength, order and order target are written into the state hash only when they are not at their default ("" or 0), after its service (R-LOOP-002's rule), so a halutz unit's hash text, and every golden, is unchanged.

## Objectives

R-OBJ-001: An objective (`objectives.yaml`) names its condition, `completes_when: { site_built: <site> }` (R-OBJ-004's `absorbed` has no site, so the site rules here apply only to `site_built`), a designated site of kind yishuv whose `founded` date must equal the objective's `reference_date`. In the last step of each GDD turn (spec §2.3, step 11; D-A6), in id order, an objective whose site is built and that is not yet recorded is recorded once, with the turn's end date (the site's `BuiltOn`). Recording it again, or in the legacy loop, changes nothing; a stopped build completes nothing.

R-OBJ-002: Recording an objective adds `objective_points + min(objective_bonus_cap, objective_bonus_per_month * monthsEarly)` to the score, once. `monthsEarly` is `max(0, monthsFromHistory)`, and `monthsFromHistory` counts calendar months from the completion's month to the reference date's month, `(y2 - y1) * 12 + (m2 - m1)`: 0 in the same month ("on time, as in history"), negative when late (owner's card wording, #379, 14 October 2026). Late costs nothing. The owner set 100 points (D-B6) and +5 a month capped at +60 (D-B2) on 14 October 2026 (#456); they are balance values and stay tunable, as `rules.yaml` design values (R-LOOP-005). The rules are those of the date the turn starts from (docs/determinism.md, "Rules per era"), so a turn that ends on the next era's first day scores with the era it started in.

R-OBJ-003: An event with `objective:` is that objective's card. It is eligible only once its objective is recorded, so it fires once, at the end of the turn the objective is completed in, and never in the legacy loop. A card has exactly one option, and an objective and its card name each other (the loader checks both). An unanswered card resolves to its option without drawing from the RNG.

R-OBJ-004: An objective may instead name `completes_when: { absorbed: { population_at_least, overflow_at_most, employed_at_least } }` (D-B4; owner, 18 October 2026, #738, option (a)). It holds at step 11 once the Jewish population (`GameState.TotalPopulation`, the national count as step 11 reads it, before the turn's population step) is at or above `population_at_least`, and then the cities' overflow (R-HOUSE-002), summed, is at most `overflow_at_most` of their housed people (R-HOUSE-003), summed, and their employed civilians, summed, are at least `employed_at_least` of their civilians (as R-CITY-005 counts them). Refugees waiting do not count: each turn's arrivals (step 9) wait at step 11, after housing (step 8). The gate keeps the census start values (41% employed, no overflow) from completing it on the first turn. Sums are integers, compared with the decimal share, so city order cannot change the result. An objective names exactly one kind; all three keys are required (a missing key is never read as 0), the shares are from 0 to 1 and the population above 0 (the loader). Recorded and scored as R-OBJ-001 and R-OBJ-002 say; its card follows R-OBJ-003.

## Money

R-MONEY-001: The GDD's Money is the Treasury (`model.yaml` money `treasury`, in IL millions): the player sees it as Money, כסף in Hebrew, on the top bar, the Budget screen and in the causes (spec §1.2, "Treasury becomes Money"; owner, 15 October 2026, Q-MONEY-4 and Q-HUD-3). Debt and foreign currency stay in the sim and the state hash, internal under Money, so the sourced 1951 currency crisis still happens (D-C3, Q-MONEY-1). The MVP keeps both abstractions approved on #884: the per-head tax represents ordinary revenue, while jobs pay the player-sensitive income in R-CITY-002. The rates remain `tax_per_capita_per_year` 0.0000545 IL million per person-year, `income_per_job_per_year` 0.06 IL million per employed 1,000-person unit-year, and `spending_per_capita_per_year` 0.0001036 IL million per person-year, all labelled design values in `rules.yaml` (owner, 18 October 2026, option 3 and the per-head-tax draft).

R-MONEY-002: In step 7, after Food consumption and imports, the Treasury pays each existing civilian kind's annual upkeep for the turn's share of a year; an unfinished project pays none, and R-MONEY-003 covers a shortfall. In the MVP, cities, completed permanent-housing projects, completed industry projects, completed transit camps, military camps and Hakhsharot each have an explicit upkeep of **IL 0m per year** in `rules.yaml`. These are design values, not historical claims. Phase 5 decides military-unit upkeep separately (#742; owner, 18 October 2026, #884 option 3).

R-MONEY-003: A GDD cost above the Treasury is paid, and the shortfall moves to debt, as map actions do: the Treasury stops at 0, and a cause names what the shortfall was borrowed for (Q-MONEY-3, owner decision "Allow borrowing", 15 October 2026, for training and pioneer work; the Phase 2 plan applies it to every GDD cost). It covers the deficit, map actions, training (R-TRAIN-001), pioneer work (R-SITE-001) and an event option's Treasury cost.

The top bar shows the net position, Treasury minus debt (`HudModel.From`), so it reads below 0 once anything is borrowed.

## Food

The national Food pool (D-C2, "National pools", decided 15 October 2026), built on the maintainer's food rule of 16 October 2026 (#382, option (b): "food_supply adds a stock term: it rises and falls with months of cover (new design values `k_food`, `cover_months`), rationing and events still act on top, and the unmet share lowers it further").

R-FOOD-001: Food is a national stock of whole units (`GameState.Food`); one unit is one population unit's food for one month, and there is no storage cap (Q-POOL-2, decided 15 October 2026). A new game starts the stock at `cover_months` of the start's need, the need being the national head count in population units (`people_per_unit`), rounded half away from zero. `k_food` and `cover_months` are required in the GDD loop (`rules.yaml` `mvp:`, labelled design values); a missing or out-of-range value is a load error.

R-FOOD-002: In step 7 the nation eats the turn's need from the stock as far as it goes: the stock stops at 0, and the rest is the unmet share, kept in the state with the need (Q-POOL-5, decided 15 October 2026). Then a monthly import brings the stock back to `cover_months` of the need, unless foreign currency is exhausted (the `fx_crisis` situation at the turn's start), when nothing is imported (Q-POOL-4, decided 15 October 2026). Imports are already paid for in foreign currency per head (`imports_per_capita_per_year`), so they are not charged again. In the GDD loop the crisis cuts the imports, not `food_supply` directly: `fx_crisis_food_per_year` acts in the legacy loop only.

R-FOOD-003: `food_supply` stays the 0 to 1 level the UI, the situations and the events read (Q-POOL-3, decided 15 October 2026), with a stock term inside its effect sum: `k_food * (stock - full) / full - unmet / need`, where `full` is `cover_months` of the last need. The term is exactly 0 at a full stock, `-k_food` when the stock is empty, and the unmet share lowers it further; rationing, the other effects and event shifts act on top as before, and the level's clamp to 0..1 bounds the result. The GDD loop requires the `food_supply` indicator.

## Defeat

The game can be lost (D-B5, decided: "Money and Food exhausted for N turns, or a war loss. Values for the owner. Coalition collapse is off in the MVP"; Phase 7 PR 5, #8). This part covers the economic defeat; the war loss waits for Phase 5's enemy units (#749). The defeat texts are the owner's pick (Q-DEF-4, #784, open): until then the game ends with the code `defeat_economy`, and its reason reads "Money and Food ran out", the plan's short option (a).

R-END-001: Money is exhausted when the debt is at or above `debt_ceiling` (Q-DEF-1, owner, 17 October 2026, #784: a debt ceiling; below it borrowing goes on as R-MONEY-003 allows). Food is exhausted when the national stock is 0 (R-FOOD-002; Q-POOL-5, "The stock stops at 0"). One of them alone ends nothing. `debt_ceiling` is IL 250m, a labelled design value (the recommended first value; the Phase 7 balance pass, PR 12, sets the final number), required in the GDD loop, a finite number >= 0.

R-END-002: In the resolver's last step, after the objectives and the score (spec §2.3, step 11), a turn end with Money and Food both exhausted adds 1 to `ExhaustedTurns`, and any other turn end sets it back to 0. When it reaches `defeat_turns`, the game ends with `defeat_economy` and a cause in the turn's causes, under the game's end (`era`) like the MVP end. `defeat_turns` is 3, a labelled design value (Q-DEF-2, owner, 18 October 2026, #784: "3 turns"), required in the GDD loop, at least 1. The step reads the debt as it finds it: the turn's deficit is borrowed after the resolution, so it counts from the next turn's end. A defeat on the game's last turn ends it as a defeat. `ExhaustedTurns` is in the state hash only when above 0, so a run that never has both exhausted hashes as before; the legacy loop never counts.

## Government

D-C5 as decided (12 October 2026): the cabinet and Attention stay, the coalition is optional with no collapse. The coalition votes, the budget shares and the policies keep running and visible as before (Q-GOV-1, decided 15 October 2026, "Take all recommendations", #375). The defeat (D-B5) is built in Phase 7 (Q-GOV-2, decided the same day).

R-GOV-001: In the GDD loop the coalition's stability never ends the game: it still moves toward public approval each turn and still weights the coalition votes, but a value below 0.2 (`PoliticsSystem.CollapseBelow`) sets no `coalition_collapse` (D-B5: "Coalition collapse is off in the MVP"). The legacy loop keeps its collapse while the legacy code exists (D-E1).

R-GOV-002: Every GDD order kind has an `attention_cost` in the real rules (a design value, R-LOOP-005), the unit orders `pioneer_work` and `stop_work` cost 0 (D-A6: unit orders are free), and a refused order costs nothing: its Attention is refunded before the next order in the turn is priced.

## National parameters

The GDD's Social cohesion and International Support (D-C1, decided 15 October 2026: "0–1 indicators through the effects web. Cohesion replaces `morale` on screen. Thresholds as situations"). Their numbers are labelled design values in `model.yaml` (Reading 1).

R-NAT-001: Social cohesion is the 0 to 1 `morale` indicator, shown to the player as Social cohesion; its id stays, so no event changes (Reading 2). Its sources are the existing homes, rationing and security links, the cities' employed share (R-CITY-005) and `food_supply`, so a Food shortage lowers it (`food_lifts_cohesion`; Q-NAT-2). Approval keeps its weights, which now read Social cohesion through `morale` (Q-NAT-5).

R-NAT-002: "Low social cohesion" (`low_cohesion`) is a situation on `morale`: on below 0.30, off above 0.45, as `food_crisis`. It has no mechanical effect of its own; events can read it through their indicator conditions (Q-NAT-3).

R-NAT-003: International Support is the 0 to 1 `intl_standing` indicator, shown as International support; event effects on `intl_standing` move it (Q-NAT-4).

R-NAT-004: "Low international support" (`low_intl_support`) is a situation on `intl_standing` with the same bands and, likewise, no effect of its own (Q-NAT-3).

R-EFX-001: An option's effect may target an indicator id from `model.yaml`, `treasury` or `fx_reserves` (game money), or `food`, the national Food stock: the effect moves the stock by whole units (the delta rounds half away from zero) and the stock never drops below 0, so a cut larger than the stock applies only to what is there. Any other target is a load error naming file and field. The Arms stock becomes a target with its pool (#781).

R-EFX-002: An option of an event tagged `holocaust`, `terror`, `massacre` or `war-casualties` cannot raise `treasury`, `fx_reserves`, `food`, `food_supply`, `morale` or `intl_standing` (Q-EFX-2, decided 15 October 2026: the no-gain rule covers all five resources, with the legacy indicators standing for Food, Social cohesion and International Support; `food` is Food's own target, and Arms joins the rule with its pool, #781). A cost stays allowed. The rule for the Phase 6 `displacement` tag is Q-DEP-6 (open).

D-H2 in the GDD loop (GDD Phase 7 PR 2, #806). The owner took the plan's recommendations for Q-HFX-1 to Q-HFX-3 on 17 October 2026 (#784): grants of units, a city's housing capacity, condition and job slots, and project unlock, each validated at load; a grant is free and stands at the military camp or at the site the event names (in this slice, a city the event names: a site waits until sites carry a date, R-EFX-006); the no-reward rule covers them. The first slice built unit grants and the housing capacity and job slots; the second adds the housing condition (#855); project unlock comes with the records it acts on (#735).

R-EFX-003: An option's `grant_units` entry `{ type, n, at }` adds `n` (at least 1) units of a `unit_types.yaml` type of kind `unit`, free, taking the next ids in turn. They stand at the settlement hex of the city `at` names or, with no `at`, at the settlement hex of the one city whose `facilities` include `military_camp` (Q-HFX-2). With no such single city, `at` is required. A site is not yet a target (R-EFX-006).

R-EFX-004: An option's `city_effects` entry `{ city, field, delta }` changes only that city's `housing_capacity` or `job_slots`, by a whole, non-zero `delta`, or its housing `condition` by a non-zero `delta` from -1 to 1. Housing capacity never drops below 0, and job slots never drop below the city's employed, so a larger cut applies only to what is free. Capacity may fall below the city's civilians, as overcrowding already can. The condition stays from 0 to 1 (R-HOUSE-001). The other fields of the city, its condition included, do not change. The cause carries the change that applied.

R-EFX-005: An option of an event tagged `holocaust`, `terror`, `massacre` or `war-casualties` carries no unit grant and no `city_effects` gain, whether capacity, slots or condition (Q-HFX-3; extends R-EFX-002). A cut stays allowed.

R-EFX-006: The structured effects apply after the numeric effects, grants first and then city effects, each in the option's list order. A target that does not exist on the date (an unbuilt site, a city not yet in the game) is refused at load with file and field, never skipped at run time; so is an unknown type, city or field. Every `cities.yaml` city is in the game from the first turn. A site has no window and starts unbuilt, so a site id as `at` is refused until sites carry a date. The legacy loop, which has no cities or units, refuses them. The preview (R-EVT-005) lists them in the same order.

## Events

D-H1 in the GDD loop (GDD Phase 7 PR 1, #805). The owner took the plan's recommendations for Q-EVT-1 to Q-EVT-4 on 17 October 2026 (#784). As the plan's Decided rows record them (`docs/superpowers/plans/2026-10-15-gdd-phase-7.md`), that keeps windows, hard anchors and the cap of 3 soft events a turn, keeps delegation, adds no delayed effects in the MVP, and shows every effect of every option from the sim's preview. These rules state what the code already does; no behaviour changed. On 18 October 2026 the owner added loyalty to delegation (#107: "Yes: minister loyalty changes a delegated event's outcome, with labelled values in the PR"); R-EVT-003 states it, with two design values (`delegation_loyalty_floor`, `disloyal_effect_share`).

R-EVT-001: An event fires on the first turn whose span overlaps its window `[from, to)` (or at the new game, when the start date is in it), once its `requires` is met and its conditions hold; an objective's card also waits for its objective (R-OBJ-003). Every eligible hard anchor fires. At most 3 soft events fire a turn (`EventSystem.MaxSoftEventsPerTurn`); they take the slots the hard anchors leave, `max(0, 3 - hard anchors)`, and the rest wait for a later turn (Q-CUT-6).

R-EVT-002: The soft events take the free slots in order: consequence cards first (R-ALT-008), then the rest by `from`, then id (ordinal). The decided place of an early armistice ahead of the other soft events (Q-EVT-1, through `front_quiet`, Q-OBJ-1) lands with that key in Phase 5 PR 13 (#752), and this rule is reworded then.

R-EVT-003: An event the player leaves unanswered at End Turn is delegated to its office's holder (Q-EVT-2): with probability competence/10 the holder picks the recommended option (else the first), otherwise a random option. In Historical mode a hard anchor takes its historical option, and an objective's card its one option, with no RNG draw. A holder whose loyalty is below `delegation_loyalty_floor` (5) applies only `disloyal_effect_share` (0.5) of each numeric effect of the option drawn; the grants, city effects, policies and security level apply whole, and so do a hard anchor's historical option and an objective's card. Why: competence decides which option a minister picks, loyalty how hard he works to carry out a decision the player left to him (#107, owner, 18 October 2026: "minister loyalty changes a delegated event's outcome"). Both are design values (R-LOOP-005), required by the loader: the floor 0 to 10, the share 0 to 1. Every minister in `people.yaml` is rated 5 or more today (5 is the deputies' neutral rating), so the rule changes no shipped game until a rating goes below the floor.

R-EVT-004: An option's effects apply when it is chosen or delegated, on that turn, and never later; an option has no delayed form, and the loader refuses an unknown key such as `delay` (Q-EVT-3).

R-EVT-005: One sim helper, `EventSystem.OptionEffects`, lists every effect of an option in the order choosing it applies them: the numeric effects, the unit grants and city effects (R-EFX-006), the policies it sets (by id), then the security level, then the way back to the record (`rejoin`, R-ALT-009), each with the option's own amount (Q-EVT-4). The event card will show this list (GDD Phase 7 PR 8), so no number will be computed in Unity.

## Alternate timelines

The alternate-timeline mechanism ([the plan](../superpowers/plans/2026-10-17-alternate-timelines.md); the owner's answers, 17 October 2026, #945).

R-ALT-001: An event's `requires` is a list, and the event is eligible only when every entry holds. An entry `{ event, option }` holds when that event was resolved (chosen or delegated) with that option. A list names each event at most once.

R-ALT-002: An entry `{ event, diverged: true }` holds when that event was resolved with any option except its historical one. An event with no historical option cannot diverge, and the loader refuses `diverged` on it.

R-ALT-003: A non-historical option may carry `alternative: { basis, by, date, sources }`. `basis` is `proposed` or `debated` (a body or office, in words, is recorded putting it forward on that date; at least one https source with a checked date) or `design` (nobody is recorded proposing it: a design assumption, which names nobody and no date; sources optional). The historical option carries none.

R-ALT-005: An event of `kind: consequence` is soft, has no historical option, has at least one requirement naming a road not taken (`diverged: true`, a non-historical option, or an option of another consequence card), carries an `alternative` record on every option, and lies at most 2 cards deep from the record (`EventRules.MaxConsequenceDepth`, a labelled design value, Q-ALT-5). It is never an objective's card, and only a consequence card may require one.

R-ALT-006: A consequence card may follow only an option whose `alternative.basis` is `proposed` or `debated`; with `diverged: true`, every non-historical option of that event must be (the Ferguson test, Q-ALT-3).

R-ALT-007: A consequence card carries every sensitive tag (case aside) of each event it requires, so the no-gain rule (R-EFX-002, R-EFX-005) covers it.

R-ALT-008: Consequence cards take a turn's soft slots before the other soft events, then by `from` and id (Q-ALT-14). A `front_quiet` armistice (Q-EVT-1, Phase 5 PR 13) will come before them. With no consequence card eligible the order is unchanged.

R-ALT-009: An option of a consequence card may `rejoin` an event whose road the card follows (and which has a historical option). Once it is chosen, a requirement on that event's historical option holds, so the record's later cards fire again if their windows are still open. The event's own chosen option is not rewritten, and `diverged: true` on it still holds.

R-ALT-010: An event of `kind: reconvergence` has a historical option and may carry `variants`. The first variant whose every `when` entry holds supplies the card's title (if it gives one) and text; options and effects never vary. Each variant follows a road not taken, carries sources, has its placeholders checked over the event's window, and names only roads whose windows close on or before the event's `from` (for a road's historical option, also every consequence card that can `rejoin` it, R-ALT-009). Only a reconvergence event carries variants.

R-ALT-011: A shareable build refuses while any consequence card, or any event with variants, has no `reviewed_by` (a person's sign-off), alongside the sensitivity gate (Q-ALT-9). `Timeline.Entries` lists each resolved event, in window order, with the player's option beside the record's, and `Timeline.Origin` names the road a consequence card follows.

R-ALT-012: The historical run (the reference player's orders from 15 May 1948 to 15 January 1952) raises no consequence card and diverges nowhere.
