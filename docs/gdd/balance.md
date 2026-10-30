# Balance values

## Player story

The player sees consistent prices, capacities and progress. This is Amir's #1668 proposal catalogue; after the 30 October 2026 owner decision [S1], entries that conflict with a recorded ruling are non-operative and the recorded ruling controls.

## Rules

Every value below is a game design value, not a historical estimate. **Agreed** records a value present in Amir's #1668 source or a retained approved baseline; it does not override a declined conflict. Derived quantities are labelled. Any value not present in Amir's #1668 source and not retained by a recorded ruling is `open (owner)`; this catalogue does not supply defaults. Implement active values with exact decimal or integer arithmetic and stable iteration order.

### Campaign and indicators

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `campaign.play_minutes` | `120` | minutes per campaign target | Owner ruling [S3], 30 Oct 2026 (`"120 (data wins)"`, #1687) |
| `indicator.display_max` | `100` | points, displayed maximum | Agreed |
| `indicator.storage_max` | `1` | unitless, stored maximum | Retained baseline |
| `indicator.low_on` / `indicator.low_off` | `30 / 45` | points, low-indicator hysteresis | Retained baseline |
| `event.max_gap_peace` / `event.max_gap_war` | `6 / 2` | months between historical-run cards | Retained baseline |

### Population and starting state

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `population.people_per_unit` | `10000` | people nominally represented by a population unit | Agreed |
| `start.population` | `65` | total player population units, all statuses included | Accepted through later start-state correction |
| `start.money` / `start.food` / `start.spare_arms` | `25 / 10 / 3` | Money / Food / unequipped Arms | Agreed |
| `start.army` | `A` | military population units; explicitly deferred | Deferred, not a chosen number |
| `start.farmers` | `5 / 10 / 5` | novice / trained / veteran units | Agreed |
| `start.prepared_fields` | `20` | staffed farming slots, across multiple friendly regions | Derived from agreed farmers |
| `start.pioneer_teachers` | `1` | veteran pioneer unit, included in population total | Agreed |
| `start.military_camps` | `2` | operating camps at the already approved locations | Retained owner ruling, historical `Q-CAMP-2` |
| `start.training_centres` | `1` | operating centre, with that teacher | Agreed |
| `start.food_production` / `start.food_consumption` | `65 / 65` | Food per turn, initially breaking even | Derived from agreed farmer mix and population |

All starting roles partition the population total; army, teachers and farmers are not added on top. Starting professions, tools and completed buildings are already owned: do not deduct their entry/construction costs again from starting Money. Starting army equipment is issued inventory, separate from starting spare Arms. `A` must be an integer compatible with the remaining allocation. Final location bindings distribute the farmers over multiple regions with enough prepared slots. They are authored scenario data, not census rounding. Food break-even applies only at the start; it does not scale automatically with arrivals.

### Work, professions and training

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `population.food_per_turn` | `1` | Food per own population unit, every status | Agreed |
| `industry.income` | `1 / 2 / 3` | Money per staffed turn: ordinary or novice / apprentice / expert | Agreed |
| `farmer.output` | `2 / 3 / 5` | Food per staffed turn: novice / trained / veteran | Agreed |
| `profession.farmer_tools` / `profession.pioneer_tools` | `3 / 5` | Money once on entering matching profession | Agreed |
| `profession.course_turns` | `3` | turns for farmer or pioneer formal course | Agreed |
| `profession.trainees_per_teacher` | `1` | trainee at a time per centre and matching veteran teacher | Agreed; replaces the briefly proposed larger class |
| `profession.work_to_veteran` | `9` | actual working turns for novice or trained farmer/pioneer | Agreed threshold; consistent trained-tier application |
| `pioneer.work_rate` | `0.75 / 1 / 1.5` | trained-equivalent work units per turn: novice / trained / veteran | Agreed |
| `craft.course_cost` / `craft.course_turns` | `5 / 6` | Money / turns; initial Industry apprenticeship course | Agreed |
| `craft.course_income` | `1` | Money per staffed course turn | Agreed |
| `craft.expert_work_turns` | `36` | actual apprentice working turns to expert | Agreed |
| `builder.slots_per_project` | `3` | simultaneous builders | Agreed |
| `builder.work_rate` | `1 / 1.5 / 3` | work units per turn: ordinary or novice / apprentice / expert | Agreed |
| `construction.starts_per_turn` | `1` | new city/region project nationwide per turn | Agreed |
| `field.slots` | `1` | farmer per prepared field | Agreed |
| `field.cost` / `field.work` | `5 / 9` | Money / trained-equivalent pioneer work units | Agreed |
| `field.nominal_turns` | `12 / 9 / 6` | turns at constant novice / trained / veteran tier | Derived; promotion can shorten remaining work |

Work XP counts actual qualifying turns, not output or work quantity. Promotion applies on the next turn; project progress is retained. A farmer/pioneer can become veteran through work without the formal course. The craftsman threshold is separate and is not shortened by the farmer/pioneer decision.

### Project catalogue

Construction work is a requirement, not a fixed timer. The city variant uses builders. A standalone camp or training-centre variant uses the same cost/work with an on-site pioneer and does not consume a city project start. Completed facility upkeep is payable even when vacant, beginning on the completion turn.

| Project key | Cost (Money) | Required work units | Completion effect | Upkeep (Money/turn) | Basis |
| --- | --- | --- | --- | --- | --- |
| `maabara` | `5` | `1` | ordinary housing `+1` place; condition `-10` pp | `open (owner)` | Agreed except upkeep |
| `permanent_housing` | `15` | `3` | ordinary housing `+3` places; condition `+10` pp | `open (owner)` | Agreed except upkeep |
| `industry` | `10` | `3` | `+1` Industry job slot | `open (owner)` | Agreed except upkeep |
| `reception_camp` | `10` | `3` | dedicated reception beds per reception capacity key | `open (owner)` | Agreed city baseline; standalone application and upkeep open (owner) |
| `training_centre` | `10` | `3` | matching-teacher course slot and separate teacher housing | `open (owner)` | Agreed city baseline; standalone application and upkeep open (owner) |
| `arms_workshop` | `15` | `3` | `+1` workshop worker slot | `open (owner)` | Agreed except upkeep |
| `housing_repair` | `2 * current regional housing upkeep` | `1` | condition `+10` pp; no additional capacity or upkeep | `0` | Agreed |
| `designated_yishuv` | `2` | `6` pioneer work units | authored site built; no automatic city/population grant | `0` | Retained approved baseline |
| `hula_drainage` | `4` | `12` pioneer work units | unlock field-preparation site; no unstaffed Food yield | `0` | Retained approved baseline, adapted to staffed fields |
| `road_segment` | `2` | `6` pioneer work units | built corridor segment | `0` | Retained approved baseline |
| `rail_segment` | `4` | `6` pioneer work units | built corridor segment | `0` | Retained approved baseline |

Centre upkeep adds to its parent reception camp. No per-hex field, road or railway upkeep is introduced. A repair uses a city project start and is available only where housing upkeep exists. Existing camps, housing and Industry incur the same upkeep as new completed facilities.

### Reception, housing and shortages

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `reception.capacity` | `3` | refugee or course-trainee beds per camp | Agreed |
| `reception.turns` | `6` | actual reception turns before free-civilian status | Agreed |
| `reception.basic_credit` | `3` | reception turns credited by completed basic military training | Agreed |
| `housing.condition_max` | `1` | unitless fraction; display as percent | Agreed |
| `housing.money_threshold` / `housing.money_slope` | `0.50 / 1` | condition fraction / Money per resident per fraction below threshold | Agreed linear rule |
| `housing.cohesion_threshold` | `0.25` | condition fraction below which cohesion declines | Agreed |
| `housing.max_cohesion_loss` | `2` | national displayed points per turn at maximal weighted severity | Agreed |
| `housing.max_deterioration` | `10` | condition pp per turn with all housing upkeep unpaid | Agreed |
| `transit.cost` | `0.50` | Money per civilian population unit ending turn in transit | Agreed |
| `reception.queue_cohesion_loss` | `1` | national displayed point with a port reception queue | Agreed amount; cadence and stacking open (owner) |
| `food.shortage_cohesion_max` | `10` | national points per turn multiplied by unmet share | Agreed |
| `food.shortage_combat_slope` | `1` | next-turn own strength reduction multiplied by unmet share | Agreed |

### Imports, immigration and Arms production

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `import.arms_price` | `10` | Money per Arms; IL millions at the display scale | Agreed |
| `import.food_per_money` | `3` | Food per Money | Agreed |
| `import.delivery_turns` | `1` | turns after purchase, by ship | Agreed |
| `immigration.ship_interval` / `immigration.units_per_ship` | `2 / 3` | turns / population units, average | Agreed |
| `workshop.staffed_turns` | `3` | staffed turns per Arms at every worker tier | Agreed |
| `workshop.cost` | `5 / 4 / 3` | Money per Arms: novice or ordinary / apprentice / expert | Agreed latest correction |

The immigration mix is a deterministic design schedule, not a historical composition claim. Imported, immigration-carried and locally produced equipment are recorded separately from population to avoid double charging or free duplication.

### Military and control

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `military.camp_capacity` | `5` | simultaneous trainees per camp | Agreed |
| `military.basic_turns` | `3` | training turns | Agreed |
| `military.issue_arms` | `1` | Arms issued once to an unequipped unit | Agreed |
| `military.draft_money` / `military.draft_wait` | `0 / 0` | Money / waiting turns for immediate draft | Agreed |
| `military.demobilise_money` / `military.demobilise_wait` | `0 / 0` | Money / waiting turns for demobilisation at a camp | Agreed |
| `military.demobilise_return` | `1` | issued Arms returned once on valid demobilisation | Agreed |
| `military.repeat_issue` / `military.combat_arms` | `0 / 0` | additional Arms for equipped unit / Arms per attack or artillery firing | Agreed |
| `military.training_money` | `0` | Money per basic course | Agreed |
| `military.active_money` | `1` | Money per active army unit per turn, excluding basic trainees | Agreed |
| `military.strength` | `1 / 2 / 3` | strength units: untrained / trained / veteran | Agreed |
| `military.combat_xp_gain` / `military.combat_xp_full` | `1 / 5` | XP ticks per survived engagement / ticks to veteran | Agreed |
| `military.refugee_multiplier` | `0.75` | unitless strength multiplier until reception complete | Agreed |
| `military.fortify_turns` | `1` | turns for Fortify order; defence bonus open (owner) | Retained turn |
| `occupation.turns` | `6` | turns before annex-or-withdraw choice; garrison requirement open (owner) | Retained owner ruling |

Biome movement costs use the existing approved eight-biome framework; this catalogue does not invent the pending terrain taxonomy. Arms are not spent on attacks or artillery fire. No equipment refund occurs automatically on destruction or surrender. Hold grants no combat bonus beyond hex/fort effects and no interception.

### Movement and engagement

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `combat.draw_count` | `1` | RNG draw per engagement; result bands open (owner) | Retained single draw |
| `combat.retreat_hexes` | `1` | hex after retreat result | Retained baseline |

Retreat without a legal destination becomes surrender. Result bands and loss granularity remain open (owner).

### Objectives

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `objective.base_points` | `100` | points per completed core objective group | Retained approved baseline |
| `objective.early_per_month` / `objective.early_cap` | `5 / 60` | points per calendar month early / maximum per dated group or part | Retained approved baseline |
| `absorption.population_target` | `1000000` | people represented; threshold must be exceeded | Retained approved baseline |
| `absorption.max_overflow` / `absorption.min_employment` | `10 / 40` | percent ordinary-housing overflow / eligible civilian employment | Retained approved baseline, regional application |


## Data touched

- Target keys are mapped to validated scenario/rules fields during implementation. This file does not edit `content/rules.yaml` or claim its legacy values are current.
- Starting population roles, facility inventory, field bindings and unresolved army parameter must be validated together.
- Historical values retain their content/research sources; unresolved values remain open (owner).

## Done when

- Each active numerical design constant has one catalogue key here and a semantic owner in the feature index.
- Starting roles sum to total population and staffed farming exactly covers initial own Food consumption.
- Implementation tests distinguish agreed values and deferred parameters; the army count is not guessed.

## Out of scope

Historical census estimates, real camp opening dates, authored region borders, actual military headcounts and the pending terrain taxonomy.

## Sources

- [S1] Design authority and provenance, docs/GDD.md.
- [S2] Feature owners and historical baseline references, docs/gdd/README.md.
- [S3] Owner play-time ruling, [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164) (checked 2026-10-30).

## Open questions

Values marked open (owner), plus the remaining principle and authoring decisions, require owner rulings before implementation; the central register is [the GDD](../GDD.md#deferred-decisions).
