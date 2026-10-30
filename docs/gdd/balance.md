# Balance values

## Player story

The player sees consistent prices, capacities and progress. This is Amir's #1668 proposal catalogue; after the 30 October 2026 owner decision [S1], entries that conflict with a recorded ruling are non-operative and the recorded ruling controls.

## Rules

Every value below is a game design value, not a historical estimate. **Agreed** means an explicit interview decision or a retained approved baseline. **Amir's proposal, declined by the owner on 30 October 2026 [S1]** marks option B of one of the thirteen conflicts and is non-operative and must not be implemented. A row the 30 October 2026 conflict review did not rule on says so and names the operative rule or the matching `content/gdd03-balance.yaml` entry [S11]. Derived quantities are labelled. Any value not retained by a recorded ruling is `open (owner)`; this catalogue does not supply operative defaults. Implement active values with exact decimal or integer arithmetic and stable iteration order.

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
| `population.people_per_unit` | `10000` (current `gdd-0.3` catalogue) | people represented by a population unit | Not decided by the 30 October 2026 rulings [S11]; `content/gdd03-balance.yaml` reads 10,000 people (`R-POP-001`), while the `Q-START-2` scale of 1,000 people [S4] governs `legacy` and `gdd-0.1` |
| `start.population` | derived per city | total player population units, all statuses included | Census-city rule [S4] at the `population.people_per_unit` scale; Amir's proposed `65` at 10,000 people per unit is not decided by the 30 October 2026 rulings [S11] |
| `start.money` / `start.food` | `open (owner)` | Money / Food | Proposed `25 / 10` was sized for Amir's scale of 10,000 people per population unit; the 30 October 2026 rulings do not decide these values [S11] |
| `start.arms` | `12` | Arms in the national pool | Retained `arms_start` value (`R-ARMS-001`, `docs/rules/mvp.md:417`) [S10]; Amir's `3` spare Arms is Agreed as `start.spare_arms` in `content/gdd03-balance.yaml`, and the 30 October 2026 conflict review did not rule on it [S11] |
| `start.army` | sourced seed in Historical mode; `A` otherwise | military population units | Historical mode keeps `content/map/units_1948.yaml` (`Q-START-1`, decided 15 October 2026); `A` applies only to non-Historical modes and remains deferred (owner, 30 Oct 2026) [S1] |
| `start.farmers` | `open (owner)` | novice / trained / veteran units | Proposed `5 / 10 / 5` came from Amir's scale of 10,000 people per unit; the 30 October 2026 rulings do not decide these values [S11] |
| `start.prepared_fields` | `20` | staffed farming slots, across multiple friendly regions | Amir's proposal, declined by the owner on 30 October 2026 [S1] (non-operative regional-field derivation) |
| `start.pioneer_teachers` | `1` | veteran pioneer unit, included in population total | Retained owner-decided inventory [S1] |
| `start.military_camps` | `2` | operating camps at the already approved locations | Retained owner ruling, historical `Q-CAMP-2` |
| `start.training_centres` | `1` | operating centre, with that teacher | Retained owner-decided inventory [S1] |
| `start.food_production` / `start.food_consumption` | `65 / 65` | Food per turn, initially breaking even | Amir's no-refill derivation; the 30 October 2026 conflict review did not rule on it [S11]; the monthly import of `R-FOOD-002` (`docs/rules/mvp.md:409`) stays operative |

All operative starting roles partition the retained census-derived population total; army, teachers and farmers are not added on top. Starting professions, tools and completed buildings are already owned: do not deduct their entry/construction costs again from starting Money. Historical mode keeps the sourced army seed from `content/map/units_1948.yaml`; starting-army parameter `A` applies only to non-Historical modes and remains an owner question pending (owner, 30 Oct 2026). This catalogue does not choose `A`. Amir's proposed absolute population/farmer counts, spare-equipment inventory, regional prepared-slot allocation and no-refill Food break-even model are non-operative [S1][S4].

### Work, professions and training

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `population.food_per_turn` | `1` | Food per own population unit, every status | Agreed |
| `industry.income` | `1 / 2 / 3` | Money per staffed turn: ordinary or novice / apprentice / expert | Amir's staffed-tier model; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `industry.income` as `1 / 2 / 3` (basis Agreed, transcribed from #1668); live job income follows `R-CITY-002` and `R-CRAFT-002` (`docs/rules/mvp.md:95`, `docs/rules/mvp.md:179`) |
| `farmer.output` | `2 / 3 / 5` | Food per staffed turn: novice / trained / veteran | The tier rates agree with main's `gdd-0.3` rule (`R-VET-002`) and are Agreed in `content/gdd03-balance.yaml` [S11]; the regional field model belongs to the declined regional-pooling proposal [S1] |
| `profession.farmer_tools` / `profession.pioneer_tools` | `3 / 5` | Money once on entering matching profession | Agreed |
| `pioneer.training_turns` | `4` | turns for retained pioneer training | Retained placeholder in `R-TRAIN-003` [S5]; Amir's proposed combined three-turn farmer/pioneer course is non-operative [S1] |
| `profession.trainees_per_teacher` | `1` | trainee at a time per centre and matching veteran teacher | Agreed; replaces the briefly proposed larger class |
| `profession.work_to_veteran` | `9` | actual working turns for novice or trained farmer/pioneer | Agrees with main's `gdd-0.3` rule (`R-VET-001`) and is Agreed in `content/gdd03-balance.yaml`; the 30 October 2026 review lists it as not a conflict [S11] |
| `pioneer.work_rate` | `0.75 / 1 / 1.5` | trained-equivalent work units per turn: novice / trained / veteran | Agrees with main's `gdd-0.3` rule (`R-VET-002`) and is Agreed in `content/gdd03-balance.yaml`; the 30 October 2026 review lists it as not a conflict [S11]; fixed pioneer progress with veteran doubling applies to `legacy` and `gdd-0.1` |
| `craft.course_cost` / `craft.course_turns` | `0.5 / 3` | Money / turns; initial Industry apprenticeship course | Owner-set values ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S11] |
| `craft.course_cost` / `craft.course_turns` (proposal) | `5 / 6` | Money / turns; initial Industry apprenticeship course | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `craft.course_cost` / `craft.course_turns` as `5 / 6` (basis Agreed, transcribed from #1668) |
| `craft.course_income` | `1` | Money per staffed course turn | Agreed |
| `craft.expert_work_turns` | `36` | actual apprentice working turns to expert | Agreed |
| `builder.slots_per_project` | `3` | simultaneous builders | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `3` (basis Agreed, transcribed from #1668); live city projects need no workers (`R-PROJ-001`, `docs/rules/mvp.md:145`) |
| `builder.work_rate` | `1 / 1.5 / 3` | work units per turn: ordinary or novice / apprentice / expert | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `1 / 1.5 / 3` (basis Agreed, transcribed from #1668); live city projects advance one turn a turn (`R-PROJ-003`, `docs/rules/mvp.md:149`) |
| `construction.starts_per_turn` | `2` | new city projects nationwide per turn | Retained `R-PROJ-001` limit, configured by `map_actions_per_turn` [S10]; Amir's proposed `1`: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `1` (basis Agreed, transcribed from #1668) |
| `field.slots` | `1` | farmer per prepared field | Amir's proposal, declined by the owner on 30 October 2026 [S1] (non-operative regional-field model) |
| `field.cost` / `field.work` | `5 / 9` | Money / trained-equivalent pioneer work units | Amir's proposal, declined by the owner on 30 October 2026 [S1] (non-operative regional-field model) |
| `field.nominal_turns` | `12 / 9 / 6` | turns at constant novice / trained / veteran tier | Derived only from declined proposal values; non-operative |

Amir's farmer/pioneer work XP, counting actual qualifying turns and allowing promotion to veteran without the formal course, agrees with main's `gdd-0.3` scoping (`R-VET-001` and `R-VET-002`, `docs/rules/mvp.md:193` and `:195`); the 30 October 2026 review lists it as not a conflict [S11].

### Project catalogue

**Amir's proposal, declined by the owner on 30 October 2026 (Upkeep conflict, option A) [S11]:** completed facilities could have upkeep. For his assigned builders and work in city construction and on-site pioneer work for standalone facilities, the owner's 30 October 2026 conflict review did not rule on this; the worker-free fixed-time `R-PROJ-001` to `R-PROJ-003` (`docs/rules/mvp.md:145`) and `R-CAMP-001` (`docs/rules/mvp.md:231`) stay operative.

| Project key | Cost (Money) | Retained duration / proposed work | Completion effect | Upkeep (Money/turn) | Basis |
| --- | --- | --- | --- | --- | --- |
| `housing` | `6` | `6` turns | housing capacity `+5`; condition `+0.03` | `0` | Retained `content/projects.yaml` value [S10]; proposed permanent-housing cost `15` is non-operative [S1] |
| `transit_camp` | `1.5` | `1` turn | housing capacity `+5`; condition `-0.02` | `0` | Retained `content/projects.yaml` value [S10]; proposed maabara cost `5` is non-operative [S1] |
| `industry` | `2` | `6` turns | `+5` Industry job slots | `0` | Retained `content/projects.yaml` value [S10]; proposed cost `10` is non-operative [S1] |
| `reception_camp` | `10` | non-operative proposed `3` work units | dedicated reception beds per reception capacity key | non-operative | Proposed reception construction and capacity model: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `reception_camp` with basis Agreed |
| `training_centre` | `10` | non-operative proposed `3` work units | matching-teacher course slot and separate teacher housing | non-operative | Proposed centre/reception construction model: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `training_centre` with basis Agreed |
| `arms_workshop` | `15` | non-operative proposed `3` builder work units | `+1` workshop worker slot | non-operative | Proposed assigned-builder construction model: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `arms_workshop` with basis Agreed |
| `military_camp` | `2` | `6` turns | operating military camp at its approved site | `0` | Retained `build_camp_cost` / `build_camp_turns` values [S10] |
| `plough_field` | `0.5` | `3` turns | prepared field produces `1` Food per turn | `0` | Retained `plow_fields_cost` / `plow_fields_turns` / `field_food_per_turn` values [S10] |
| `housing_repair` | `2 * current regional housing upkeep` | `1` | condition `+10` pp; no additional capacity or upkeep | `0` | Amir's proposal, declined by the owner on 30 October 2026 [S1] (non-operative regional-upkeep rule) |
| `designated_yishuv` | `2` | `6` turns | authored site built; no automatic city/population grant | `0` | Retained approved baseline [S8] |
| `hula_drainage` | `4` | `12` turns | Hula field site becomes ready to plow | `0` | Retained approved baseline [S8] |
| `road_segment` | `2` | `6` turns | built corridor segment | `0` | Retained approved baseline [S8] |
| `rail_segment` | `4` | `6` turns | built corridor segment | `0` | Retained approved baseline [S8] |

**Amir's proposal, declined by the owner on 30 October 2026 (Upkeep conflict, option A) [S11]:** centre/camp upkeep and repair access through regional housing upkeep would use the proposed catalogue. These mechanics are non-operative. For one-start construction, the owner's 30 October 2026 conflict review did not rule on this; `R-PROJ-001` (`docs/rules/mvp.md:145`) stays operative.

### Reception, housing and shortages

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `reception.capacity` | `3` | refugee or course-trainee beds per camp | Amir's proposal; the 30 October 2026 conflict review did not rule on it [S11]; Agreed in `content/gdd03-balance.yaml` for the inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`) |
| `reception.turns` | `6` | actual reception turns before free-civilian status | Amir's proposal; the 30 October 2026 conflict review did not rule on it [S11]; Agreed in `content/gdd03-balance.yaml` for the inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`), while live conversion follows `R-CITY-004` (`docs/rules/mvp.md:105`) |
| `reception.basic_credit` | `3` | reception turns credited by completed basic military training | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `3` (basis Agreed, transcribed from #1668); live conversion follows `R-CITY-004` (`docs/rules/mvp.md:105`) |
| `housing.condition_max` | `1` | unitless fraction; display as percent | Agreed |
| `housing.money_threshold` / `housing.money_slope` | `0.50 / 1` | condition fraction / Money per resident per fraction below threshold | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `housing.money_threshold` / `housing.money_slope` as `0.50 / 1` (basis Agreed, transcribed from #1668) for the inactive `gdd-0.3` housing arithmetic (`docs/rules/mvp.md:133`); live housing follows `R-HOUSE-001` to `R-HOUSE-004` |
| `housing.cohesion_threshold` | `0.25` | condition fraction below which cohesion declines | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `housing.cohesion_threshold` as `0.25` (basis Agreed, transcribed from #1668) for the inactive `gdd-0.3` housing arithmetic (`docs/rules/mvp.md:133`); live housing follows `R-HOUSE-001` to `R-HOUSE-004` |
| `housing.max_cohesion_loss` | `2` | national displayed points per turn at maximal weighted severity | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `housing.max_cohesion_loss` as `2` (basis Agreed, transcribed from #1668) for the inactive `gdd-0.3` housing arithmetic (`docs/rules/mvp.md:133`); live housing follows `R-HOUSE-001` to `R-HOUSE-004` |
| `housing.overflow_deterioration` | `5` | condition pp per turn multiplied by ordinary-resident overflow share | Default (non-operative; not an owner ruling) |
| `housing.max_deterioration` | `10` | condition pp per turn with all housing upkeep unpaid | Amir's proposal, declined by the owner on 30 October 2026 [S1] (non-operative) |
| `transit.cost` | `0.50` | Money per civilian population unit ending turn in transit | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `transit.cost` as `0.50` (basis Agreed, transcribed from #1668); main has no live transit charge, so it is `open (owner)` |
| `reception.queue_cohesion_loss` | `1` | national displayed point with a port reception queue | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `reception.queue_cohesion_loss` as `1` (basis Agreed, transcribed from #1668); main has no live port queue, so it is `open (owner)` |
| `food.shortage_cohesion_max` | `10` | national points per turn multiplied by unmet share | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `food.shortage_cohesion_max` as `10` (basis Agreed, transcribed from #1668); live shortage effects follow `R-FOOD-002` and `R-FOOD-003` (`docs/rules/mvp.md:409`) |
| `food.shortage_combat_slope` | `1` | next-turn own strength reduction multiplied by unmet share | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `food.shortage_combat_slope` as `1` (basis Agreed, transcribed from #1668); live shortage effects follow `R-FOOD-002` and `R-FOOD-003` (`docs/rules/mvp.md:409`) |

### Imports, immigration and Arms production

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `import.arms_price` | `10` | Money per Arms; IL millions at the display scale | Agreed in `content/gdd03-balance.yaml`, matching Amir's value [S11]; the live `R-ARMS-002` purchase charges IL 1m an Arms [S9] |
| `import.food_per_money` | `3` | Food per Money | Agreed |
| `import.delivery_turns` | `0` | turns after an Arms purchase | Retained immediate `R-ARMS-002` delivery [S9]; proposed next-turn shipment is non-operative [S1] |
| `immigration.ship_interval` / `immigration.units_per_ship` | `2 / 3` | turns / population units, average | Amir's proposed manifest schedule, declined by the owner on 30 October 2026 [S1] (non-operative); retained arrivals use the CBS series in `content/aliyah.yaml` [S7] |
| `workshop.staffed_turns` | `3` | staffed turns per Arms at every worker tier | Agreed |
| `workshop.cost` | `5 / 4 / 3` | Money per Arms: novice or ordinary / apprentice / expert | Agreed latest correction |

The proposed immigration mix and ship schedule are non-operative [S1]. Retained arrivals use the CBS series in `content/aliyah.yaml`, in whole units counted cumulatively so that no unit is lost or counted twice [S7]. Imported, immigration-carried and locally produced equipment are recorded separately from population to avoid double charging or free duplication.

### Treasury

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `money.debt_ceiling` | `250` | IL millions of debt at which Money is exhausted | Retained `debt_ceiling` value [S10] |

### Military and control

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `military.camp_capacity` | no separate limit | simultaneous trainees per camp | Retained `R-TRAIN-001` applies no separate simultaneous-trainee cap [S10]; proposed `5` is non-operative [S1] |
| `military.basic_turns` | `3` | training turns | Agreed |
| `military.issue_arms` | `1` | Arms consumed per recruit group when training starts | Retained `R-ARMS-003` value [S9] |
| `military.draft_money` / `military.draft_wait` | non-operative proposed `0 / 0` | Money / waiting turns for immediate draft | The owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists them as `0 / 0` (basis Agreed, transcribed from #1668); retained `R-TRAIN-001` (`docs/rules/mvp.md:159`) uses a paid training course rather than immediate draft [S10] |
| `military.demobilise_money` / `military.demobilise_wait` | non-operative proposed `0 / 0` | Money / waiting turns for demobilisation at a camp | The owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists them as `0 / 0` (basis Agreed, transcribed from #1668); retained `R-UNIT-005` (`docs/rules/mvp.md:247`) makes Send home the only way home [S10] |
| `military.demobilise_return` | `1` | issued Arms returned once on valid demobilisation | Amir's reusable-equipment proposal, which the 30 October 2026 review lists as not a conflict [S11]; Agreed in `content/gdd03-balance.yaml` |
| `military.repeat_issue` | `0` | additional Arms under Amir's reusable-equipment model | Amir's reusable-equipment proposal, which the 30 October 2026 review lists as not a conflict [S11]; Agreed in `content/gdd03-balance.yaml` |
| `military.attack_arms` / `military.artillery_arms` | `0` under `gdd-0.3` (`1 / 2` under `gdd-0.1`) | Arms per player attack / artillery firing | `R-COMBAT-004` (owner ruling, 25 October 2026): `gdd-0.3` attacks and artillery fire spend no Arms; `R-COMBAT-004` and `R-COMBAT-007` keep 1 unit of Arms per attack and 2 units per artillery firing for `gdd-0.1` [S9] |
| `military.training_money` | `0.5` | Money per basic course | Owner-set value ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S11] |
| `military.training_money` (proposal) | `0` | Money per basic course | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `0` (basis Agreed, transcribed from #1668) |
| `military.active_money` | `0.05` | Money per active army unit per turn, excluding basic trainees | Owner-set value ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S11] |
| `military.active_money` (proposal) | `1` | Money per active army unit per turn, excluding basic trainees | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `1` (basis Agreed, transcribed from #1668) |
| `military.strength` | `1 / 2 / 3` | proposed strength units: untrained / trained / veteran | Amir's qualification-factor model; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `1 / 2 / 3` (basis Agreed, transcribed from #1668); live Strength counts population units (`R-UNIT-001`, `docs/rules/mvp.md:239`) [S6] |
| `military.combat_xp_gain` / `military.combat_xp_full` | `1 / 5` | XP ticks per survived engagement / ticks to veteran | Amir's proposal; the 30 October 2026 conflict review did not rule on it [S11]; time-served `R-UNIT-004` (`docs/rules/mvp.md:245`) stays operative |
| `military.refugee_multiplier` | `0.75` | unitless strength multiplier until reception complete | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `military.refugee_multiplier` as `0.75` (basis Agreed, transcribed from #1668); live Strength counts population units (`R-UNIT-001`, `docs/rules/mvp.md:239`) |
| `military.fortify_turns` | `1` | turns for Fortify order; defence bonus open (owner) | Retained turn |
| `occupation.turns` | `6` | turns before annex-or-withdraw choice | Retained owner ruling |

Biome movement costs use the existing approved eight-biome framework; this catalogue does not invent the pending terrain taxonomy. Training consumes Arms (`R-ARMS-003`); under `gdd-0.3` attacks and artillery fire do not (`R-COMBAT-004`, owner ruling, 25 October 2026) [S9]. No equipment refund occurs automatically on destruction or surrender. Hold grants no combat bonus beyond hex/fort effects and no interception.

### Movement and engagement

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `combat.draw_count` | `1` | RNG draw per engagement; result bands open (owner) | Retained single draw |
| `combat.retreat_hexes` | `1` | hex after retreat result | Retained baseline |
| `combat.enemy_attack_ratio` | `open (owner)` | unitless minimum ratio for goal-driven enemy attack | Proposed default `1.50` is non-operative and is not an owner ruling |
| `combat.artillery_range` | `open (owner)` | hex support range | Proposed default `2` is non-operative and is not an owner ruling |
| `prisoner.food` | `open (owner)` | Food per held enemy combatant cohort per turn | Proposed default `1` is non-operative and is not an owner ruling |
| `prisoner.return_points` | `open (owner)` | points per returned enemy combatant cohort | Proposed default `5` is non-operative and is not an owner ruling |

Retreat without a legal destination becomes surrender. Result bands and loss granularity remain open (owner).

### Objectives

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `objective.base_points` | `100` | points per completed core objective group | Retained approved baseline |
| `objective.early_per_month` / `objective.early_cap` | `5 / 60` | points per calendar month early / maximum per dated group or part | Retained approved baseline |
| `absorption.population_target` | `1000000` | people represented; threshold must be exceeded | Retained approved baseline |
| `absorption.max_overflow` / `absorption.min_employment` | `10 / 40` | percent ordinary-housing overflow / eligible civilian employment | Retained approved baseline; apply per the retained city objective |


## Data touched

- Target keys are mapped to validated scenario/rules fields during implementation. This file does not edit `content/rules.yaml` or claim its legacy values are current.
- Starting population roles and retained facility inventory must be validated with the sourced Historical army seed; the unresolved parameter `A` is validated only for non-Historical modes (owner, 30 Oct 2026) [S1]. Declined regional field bindings are not implemented.
- Historical values retain their content/research sources; unresolved values remain open (owner).

## Done when

- Each active numerical design constant has one catalogue key here and a semantic owner in the feature index.
- Starting roles sum to total population; the staffed-field break-even calculation (declined with regional pooling) and the no-refill derivation (not ruled on by the 30 October 2026 conflict review; `R-FOOD-002` stays operative) are not acceptance criteria [S11].
- Implementation tests distinguish agreed values and deferred parameters; the army count is not guessed.

## Out of scope

Historical census estimates, real camp opening dates, authored region borders, actual military headcounts and the pending terrain taxonomy.

## Sources

- [S1] Design authority and provenance, docs/GDD.md.
- [S3] Owner play-time ruling, [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164) (checked 2026-10-30).
- [S4] Retained census scale and worker-free city projects, docs/gdd/cities.md.
- [S5] Retained pioneer training duration, docs/gdd/training.md.
- [S6] Retained military-unit Strength rule, docs/gdd/military-combat.md.
- [S7] Retained CBS arrivals rule, docs/gdd/one-city.md.
- [S8] Retained site, swamp and corridor durations, docs/gdd/build-yishuv.md, docs/gdd/fields-and-swamps.md and docs/gdd/corridors.md.
- [S9] Retained Arms purchase, training and combat values, `docs/rules/mvp.md` (`R-ARMS-002`, `R-ARMS-003`, `R-COMBAT-004` and `R-COMBAT-007`; under `gdd-0.1` attack 1 Arms and artillery 2 Arms, under `gdd-0.3` none).
- [S10] Retained numerical and rule catalogue, `content/projects.yaml`, `content/rules.yaml` and `docs/rules/mvp.md` (`R-PROJ-001`, `R-TRAIN-001` and `R-UNIT-005`).
- [S11] Owner's 30 October 2026 rulings, [GDD conflict decisions](../GDD.md#30-october-2026-conflict-decisions).

## Open questions

Values marked open (owner), plus the remaining principle and authoring decisions, require owner rulings before implementation; the central register is [the GDD](../GDD.md#deferred-decisions).
