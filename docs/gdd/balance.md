# Balance values

## Player story

The player sees consistent prices, capacities and progress. This is Amir's #1668 proposal catalogue; after the 30 October 2026 owner decision [S1], entries that conflict with a recorded ruling are non-operative and the recorded ruling controls. On 1 November 2026 the owner answered the values left open and made those conflicting entries post-MVP design [S12].

## Rules

Every value below is a game design value, not a historical estimate. **Agreed** means an explicit interview decision or a retained approved baseline. **Amir's proposal, declined by the owner on 30 October 2026 [S1]** marks option B of one of the thirteen conflicts and is non-operative and must not be implemented. A row the 30 October 2026 conflict review did not rule on says so and names the operative rule or the matching `content/gdd03-balance.yaml` entry [S11]. Derived quantities are labelled. Any value not retained by a recorded ruling is `open (owner)`; this catalogue does not supply operative defaults. The owner answered every value this catalogue had left `open (owner)` on 1 November 2026, and each row below cites its answer ID (D1-D6, O1-O8, Q and 27M numbers) [S12]. Implement active values with exact decimal or integer arithmetic and stable iteration order. (unitless)

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
| `population.people_per_unit` | `1000` in the Historical MVP; `10000` for Open mode | people represented by a population unit | Owner, 1 November 2026, D2 [S12]: the Historical MVP keeps main's census cities at the `Q-START-2` scale of 1,000 people (`R-CITY-007`) [S4]; `content/gdd03-balance.yaml` reads 10,000 people (`R-POP-001`), Amir's scale, which applies to Open mode when it exists |
| `start.population` | derived per city | total player population units, all statuses included | Census-city rule [S4] at the `population.people_per_unit` scale (owner, 1 November 2026, D2) [S12]; Amir's `65` at 10,000 people per unit is the Open-mode start |
| `start.money` / `start.food` | `20` / `cover_months` (3 months of need) | Money / Food | Owner, 1 November 2026, D2 [S12]: the Historical MVP keeps main's `treasury_start` and a Food stock at `cover_months` of the start's need (`R-FOOD-001`) [S10]; Amir's `25 / 10`, sized for 10,000 people per population unit, is the Open-mode start |
| `start.arms` | `12` | Arms in the national pool | Retained `arms_start` value (`R-ARMS-001`, `docs/rules/mvp.md`) [S10]; Amir's `3` spare Arms is Agreed as `start.spare_arms` in `content/gdd03-balance.yaml`, and the 30 October 2026 conflict review did not rule on it [S11] |
| `start.army` | sourced seed in Historical mode; `A` otherwise | military population units | Historical mode keeps `content/map/units_1948.yaml` (`Q-START-1`, decided 15 October 2026); `A` applies only to non-Historical modes and remains deferred (owner, 30 Oct 2026) [S1] |
| `start.farmers` | not used in the Historical MVP | novice / trained / veteran units | Owner, 1 November 2026, D2 [S12]: Historical cities start from the census; Amir's `5 / 10 / 5`, sized for 10,000 people per unit, is the Open-mode start. Each host's starting farmers split 1:2:1 novice / trained / veteran, largest remainder with ties to trained (owner, 1 November 2026, "Amir's 1:2:1 ratio per city (Recommended)" and "all approved" [S12]; per host, which gives 1 / 18 / 1 nationally, owner, 7 November 2026, "Per host, 1/18/1 (Recommended)", [#1689](https://github.com/eyalgolan/statecraft-israel/issues/1689#issuecomment-6704985642)); at 2 more than a multiple of 4 the leftover unit goes to trained, so 6 farmers split 1 / 4 / 1 (owner, 6 November 2026, "Trained: 1/4/1 (Recommended)", [#2465](https://github.com/eyalgolan/statecraft-israel/issues/2465#issuecomment-6676926351)) (unitless) |
| `start.prepared_fields` | `20` | staffed farming slots, across multiple friendly regions | Owner, 1 November 2026, Q36 and MAN-1 [S12]: the authored list from the owner-approved per-region split of 27 October 2026 (20 farmer units and 8 Industry units), kept as symbolic weighting; Southern Coast and Negev get one housing place each for their farmer once a source confirms a settlement there on 15 May 1948 (D5). Amir's regional-capital staffing was declined on 30 October 2026 [S1] (unitless) |
| `start.pioneer_teachers` | `1` | veteran pioneer unit, included in population total | Retained owner-decided inventory [S1] |
| `start.military_camps` | `2` | operating camps at the already approved locations | Retained owner ruling, historical `Q-CAMP-2` |
| `start.training_centres` | `1` | operating centre, with that teacher | Retained owner-decided inventory [S1]; hosted at Kiryat Shmuel, Haifa (owner, 1 November 2026, O8) [S12] |
| `start.food_production` / `start.food_consumption` | `65 / 65` | Food per turn, initially breaking even | Amir's no-refill derivation; the 30 October 2026 conflict review did not rule on it [S11]; on 1 November 2026 the owner kept the monthly import of `R-FOOD-002` (`docs/rules/mvp.md`) and the census start for the MVP (D1, D2), so this break-even is post-MVP [S12] |

All operative starting roles but the farmers and the army partition the retained census-derived population total, and teachers are not added on top. The army stands beside that total: under R-POP-004 each Israeli starting formation gives its strength as active soldiers, beside the census cities' free civilians (owner, 7 November 2026: "Say the army is beside it (Recommended)", [#1689](https://github.com/eyalgolan/statecraft-israel/issues/1689#issuecomment-6704985642)). This is wording only and changes no game value. The farmers' 20 units are added to the census total (owner, 7 November 2026: "On top, change the rule (Recommended)", which reverses "army, teachers and farmers are not added on top", [recorded on #1689](https://github.com/eyalgolan/statecraft-israel/issues/1689#issuecomment-6704565066)), and the training centre's one veteran pioneer teacher is a Haifa census unit ("Yes to all three (Recommended)"). Neither is wired into `Simulation.NewGame` yet. Starting professions, tools and completed buildings are already owned: do not deduct their entry/construction costs again from starting Money. Historical mode keeps the sourced army seed from `content/map/units_1948.yaml`; starting-army parameter `A` applies only to non-Historical modes and remains an owner question pending (owner, 30 Oct 2026). This catalogue does not choose `A`. Amir's proposed absolute population/farmer counts, spare-equipment inventory, regional prepared-slot allocation and no-refill Food break-even model are non-operative [S1][S4]; under the owner's 1 November 2026 answers the counts and spare equipment are the Open-mode start (D2) and the no-refill model is post-MVP (D1) [S12].

### Work, professions and training

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `population.food_per_turn` | `1` | Food per own population unit, every status | Agreed |
| `industry.income` | `1 / 2 / 3` | Money per staffed turn: ordinary or novice / apprentice / expert | Amir's staffed-tier model; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `industry.income` as `1 / 2 / 3` (basis Agreed, transcribed from #1668); live job income follows `R-CITY-002` and `R-CRAFT-002` (`docs/rules/mvp.md`). At 1,000 people a unit the tiers are 1x / 2x / 3x the `R-CITY-002` job income (owner, 1 November 2026, "Main's base x 0.3's tiers (Recommended)") [S12] (unitless) |
| `farmer.output` | `2 / 3 / 5` | Food per staffed turn: novice / trained / veteran | The tier rates agree with main's `gdd-0.3` rule (`R-VET-002`) and are Agreed in `content/gdd03-balance.yaml` [S11]; the regional field model belongs to the declined regional-pooling proposal [S1]. At 1,000 people a unit the tiers are 2x / 3x / 5x `field_food_per_turn` (owner, 1 November 2026, "Main's base x 0.3's tiers (Recommended)") [S12] (unitless) |
| `profession.farmer_tools` / `profession.pioneer_tools` | `3 / 5` | Money once on entering matching profession | Agreed |
| `pioneer.training_turns` | `4` | turns for retained pioneer training | Retained placeholder in `R-TRAIN-003` [S5]; Amir's proposed combined three-turn farmer/pioneer course is non-operative [S1] |
| `profession.trainees_per_teacher` | `1` | trainee at a time per centre and matching veteran teacher | Agreed; replaces the briefly proposed larger class |
| `profession.work_to_veteran` | `9` | actual working turns for novice or trained farmer/pioneer | Agrees with main's `gdd-0.3` rule (`R-VET-001`) and is Agreed in `content/gdd03-balance.yaml`; the 30 October 2026 review lists it as not a conflict [S11] |
| `pioneer.work_rate` | `0.75 / 1 / 1.5` | trained-equivalent work units per turn: novice / trained / veteran | Agrees with main's `gdd-0.3` rule (`R-VET-002`) and is Agreed in `content/gdd03-balance.yaml`; the 30 October 2026 review lists it as not a conflict [S11]; fixed pioneer progress with veteran doubling applies to `legacy` and `gdd-0.1` |
| `craft.course_cost` / `craft.course_turns` | `0.5 / 3` | Money / turns; initial Industry apprenticeship course | Owner-set values ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S11] |
| `craft.course_cost` / `craft.course_turns` (proposal) | `5 / 6` | Money / turns; initial Industry apprenticeship course | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11], and the same day the owner kept the set values instead ("Keep the set values (Recommended)", #2013 balance rows), which `content/gdd03-balance.yaml` holds (#2115) |
| `craft.course_income` | `1` | Money per staffed course turn | Agreed |
| `craft.expert_work_turns` | `36` | actual apprentice working turns to expert | Agreed |
| `builder.slots_per_project` | `3` | simultaneous builders | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `3` (basis Agreed, transcribed from #1668); the owner adopted builders for the MVP on 3 November 2026 ("Builders in the MVP (Recommended)") [S16]; live city projects need no workers until #1700 lands (`R-PROJ-001`, `docs/rules/mvp.md`). **RULED (owner, 3 November 2026): "One builder, today's times (Recommended)":** one builder a project in the MVP, not `3`; under `gdd-0.3` a city project has at most one builder (`R-CITY-011`) [S16]. |
| `builder.work_rate` | `1 / 1.5 / 3` | work units per turn: ordinary or novice / apprentice / expert | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `1 / 1.5 / 3` (basis Agreed, transcribed from #1668); live city projects advance one turn a turn (`R-PROJ-003`, `docs/rules/mvp.md`). **RULED (owner, 3 November 2026): "One builder, today's times (Recommended)":** the MVP keeps each kind's existing `turns`; under `gdd-0.3` a project with its builder gains a turn's progress as `R-PROJ-003` says (`R-CITY-011`) [S16]. |
| `construction.starts_per_turn` | `2` | new city projects nationwide per turn | Retained `R-PROJ-001` limit, configured by `map_actions_per_turn` [S10]; Amir's proposed `1`: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `1` (basis Agreed, transcribed from #1668) |
| `field.slots` | `1` | farmer per prepared field | Owner, 3 November 2026, "One farmer per field (Recommended)" ("Each field takes one farmer; an unworked field produces nothing") [S17]: operative under `gdd-0.3`, the farmer living in the field's region (Q27). Amir's regional pooling through a capital stays declined [S1] |
| `field.cost` / `field.work` | `5 / 9` | Money / trained-equivalent pioneer work units | Amir's proposal, declined by the owner on 30 October 2026 [S1] (non-operative regional-field model) |
| `field.nominal_turns` | `12 / 9 / 6` | turns at constant novice / trained / veteran tier | Derived only from declined proposal values; non-operative |

Amir's farmer/pioneer work XP, counting actual qualifying turns and allowing promotion to veteran without the formal course, agrees with main's `gdd-0.3` scoping (`R-VET-001` and `R-VET-002`, `docs/rules/mvp.md`); the 30 October 2026 review lists it as not a conflict [S11].

### Project catalogue

**Amir's proposal, declined by the owner on 30 October 2026 (Upkeep conflict, option A) [S11]:** completed facilities could have upkeep. For Amir's assigned builders and work in city construction, the owner adopted builders for the MVP on 3 November 2026 ("Builders in the MVP (Recommended)") [S16]; the worker-free fixed-time `R-PROJ-001` to `R-PROJ-003` (`docs/rules/mvp.md`) describe the game until #1700 lands. For on-site pioneer work for standalone facilities, the 30 October 2026 conflict review did not rule; `R-CAMP-001` (`docs/rules/mvp.md`) stays operative.

| Project key | Cost (Money) | Retained duration / proposed work | Completion effect | Upkeep (Money/turn) | Basis |
| --- | --- | --- | --- | --- | --- |
| `housing` | `6` | `6` turns | housing capacity `+5`; condition `+0.03` | `0` | Retained `content/projects.yaml` value [S10]; proposed permanent-housing cost `15` is non-operative [S1] |
| `transit_camp` | `1.5` | `1` turn | housing capacity `+5`; condition `-0.02` | `0` | Retained `content/projects.yaml` value [S10]; proposed maabara cost `5` is non-operative [S1] |
| `industry` | `2` | `6` turns | `+5` Industry job slots | `0` | Retained `content/projects.yaml` value [S10]; proposed cost `10` is non-operative [S1] |
| `reception_camp` | `10` | non-operative proposed `3` work units | dedicated reception beds per reception capacity key | non-operative | Proposed reception construction and capacity model: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `reception_camp` with basis Agreed; reception beds are post-MVP (owner, 1 November 2026, "No reception-bed layer (Recommended)") [S12] |
| `training_centre` | `10` | non-operative proposed `3` work units | matching-teacher course slot and separate teacher housing | non-operative | Proposed centre/reception construction model: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `training_centre` with basis Agreed |
| `arms_workshop` | `15` | non-operative proposed `3` builder work units | `+1` workshop worker slot | non-operative | Proposed assigned-builder construction model: the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `arms_workshop` with basis Agreed |
| `military_camp` | `2` | `6` turns | operating military camp at its approved site | `0` | Retained `build_camp_cost` / `build_camp_turns` values [S10] |
| `plough_field` | `0.5` | `3` turns | prepared field produces `1` Food per turn | `0` | Retained `plow_fields_cost` / `plow_fields_turns` / `field_food_per_turn` values [S10] |
| `housing_repair` | `0.5` | `1` work | condition `+10` pp; no additional capacity or upkeep | `0` | Owner, 1 November 2026, "Fixed price 0.5 Money + 1 work" [S12] (labelled design value); Amir's `2 * current regional housing upkeep` was declined by the owner on 30 October 2026 [S1] (non-operative regional-upkeep rule) (unitless) |
| `designated_yishuv` | `2` | `6` turns | authored site built; no automatic city/population grant | `0` | Retained approved baseline [S8] |
| `hula_drainage` | `4` | `12` turns | Hula field site becomes ready to plow | `0` | Retained approved baseline [S8] |
| `road_segment` | `2` | `6` turns | built corridor segment | `0` | Retained approved baseline [S8] |
| `rail_segment` | `4` | `6` turns | built corridor segment | `0` | Retained approved baseline [S8] |

**Amir's proposal, declined by the owner on 30 October 2026 (Upkeep conflict, option A) [S11]:** centre/camp upkeep and repair access through regional housing upkeep would use the proposed catalogue. These mechanics are non-operative. For one-start construction, the owner's 30 October 2026 conflict review did not rule on this; `R-PROJ-001` (`docs/rules/mvp.md`) stays operative.

### Reception, housing and shortages

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `reception.capacity` | `3` | refugee or course-trainee beds per camp | Amir's proposal; the 30 October 2026 conflict review did not rule on it [S11]; Agreed in `content/gdd03-balance.yaml` for the inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`); the reception model is post-MVP (owner, 1 November 2026, "Keep R-CITY-004 (Recommended)") [S12] |
| `reception.turns` | `6` | actual reception turns before free-civilian status | Amir's proposal; the 30 October 2026 conflict review did not rule on it [S11]; Agreed in `content/gdd03-balance.yaml` for the inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`), while live conversion follows `R-CITY-004` (`docs/rules/mvp.md`); the reception model is post-MVP (owner, 1 November 2026, "Keep R-CITY-004 (Recommended)") [S12] |
| `reception.basic_credit` | `3` | reception turns credited by completed basic military training | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `3` (basis Agreed, transcribed from #1668); live conversion follows `R-CITY-004` (`docs/rules/mvp.md`); the reception model is post-MVP (owner, 1 November 2026, "Keep R-CITY-004 (Recommended)") [S12] |
| `housing.condition_max` | `1` | unitless fraction; display as percent | Agreed |
| `start.housing_condition` | `0.35` | unitless fraction; one starting condition for every city | Owner, 1 November 2026, 27M-16: "0.35, your 15 Oct value (Recommended)" [S12]; main's `mvp.housing_start_condition` (`R-HOUSE-001`, `Q-START-3`); `content/gdd03-balance.yaml` holds this `0.35`, Agreed (#2115), in place of #1668's Default of 70 percentage points per region (unitless) |
| `housing.money_threshold` / `housing.money_slope` | `0.50 / 0.1` | condition fraction / Money per housed population unit per condition fraction below the threshold, a turn | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; the owner scaled the slope from #1668's `1` to the unit size on 2 November 2026: "Scale it to unit size (Recommended)"; the option read: "0.1 per unit (as #2112 did for Industry), about 7.6 Money a turn at start." [S15]. `content/gdd03-balance.yaml` lists them as `0.50 / 0.1` (basis Agreed). Under `gdd-0.3` they are live, per city, through `R-HOUSE-005` (`docs/rules/mvp.md`): the opening turn charges 7.665 Money; `gdd-0.1` and legacy housing follow `R-HOUSE-001` to `R-HOUSE-004` (unitless) |
| `housing.cohesion_threshold` | `0.25` | condition fraction below which cohesion declines | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `housing.cohesion_threshold` as `0.25` (basis Agreed, transcribed from #1668); live under `gdd-0.3` through `R-HOUSE-005` (`docs/rules/mvp.md`), the issue's term, kept by the owner on 2 November 2026: "Keep the 2-point loss (Recommended)"; the option read: "As issue #1703 specifies, GDD 0.3 only."; `gdd-0.1` and legacy housing follow `R-HOUSE-001` to `R-HOUSE-004` [S15] (unitless) |
| `housing.max_cohesion_loss` | `2` | national displayed points per turn at maximal weighted severity | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `housing.max_cohesion_loss` as `2` (basis Agreed, transcribed from #1668); live under `gdd-0.3` through `R-HOUSE-005` (`docs/rules/mvp.md`), the issue's term, kept by the owner on 2 November 2026: "Keep the 2-point loss (Recommended)"; the option read: "As issue #1703 specifies, GDD 0.3 only."; `gdd-0.1` and legacy housing follow `R-HOUSE-001` to `R-HOUSE-004` [S15] (unitless) |
| `housing.overflow_deterioration` | `5` | condition pp per turn multiplied by ordinary-resident overflow share | Owner, 2 November 2026 (Q1): "Yes, 5 pp under GDD 0.3 (Recommended)". Live under `gdd-0.3` only through `R-HOUSE-005` (`docs/rules/mvp.md`), in place of `R-HOUSE-002`'s `overflow_condition_loss` `0.15`; `gdd-0.1` and legacy keep `0.15`. `content/gdd03-balance.yaml` lists it as `5`, basis Agreed with the ruling in its note [S15] (unitless) |
| `housing.max_deterioration` | `10` | condition pp per turn with all housing upkeep unpaid | Amir's proposal, declined by the owner on 30 October 2026 [S1] (non-operative; basis Default in `content/gdd03-balance.yaml`, #2115) |
| `transit.cost` | `0.50` | Money per civilian population unit ending turn in transit | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `transit.cost` as `0.50`, basis Default and not operative in the MVP (#2115); main has no live transit charge, and transit is post-MVP (owner, 1 November 2026: "30 Oct wins for the MVP (Recommended)") [S12] |
| `reception.queue_cohesion_loss` | `1` | national displayed point with a port reception queue | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `reception.queue_cohesion_loss` as `1`, basis Default and not operative in the MVP (#2115); main has no live port queue, and port queues are post-MVP (owner, 1 November 2026: "30 Oct wins for the MVP (Recommended)") [S12] |
| `food.shortage_cohesion_max` | `10` | national points per turn multiplied by unmet share | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `food.shortage_cohesion_max` as `10`, basis Default and not operative in the MVP (owner, 10 November 2026: the S3 answer for `military.strength`, "Default, not operative (Recommended)", given the same for all four rows, [#2918](https://github.com/eyalgolan/statecraft-israel/issues/2918#issuecomment-6766460488)); live shortage effects follow `R-FOOD-002` and `R-FOOD-003` (`docs/rules/mvp.md`) |
| `food.shortage_combat_slope` | `1` | next-turn own strength reduction multiplied by unmet share | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `food.shortage_combat_slope` as `1`, basis Default and not operative in the MVP (owner, 10 November 2026: the S3 answer for `military.strength`, "Default, not operative (Recommended)", given the same for all four rows, [#2918](https://github.com/eyalgolan/statecraft-israel/issues/2918#issuecomment-6766460488)); live shortage effects follow `R-FOOD-002` and `R-FOOD-003` (`docs/rules/mvp.md`) |

### Imports, immigration and Arms production

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `import.arms_price` | `10` | Money per Arms; IL millions at the display scale | Agreed in `content/gdd03-balance.yaml`, matching Amir's value [S11]; the live `R-ARMS-002` purchase charges IL 1m an Arms [S9] |
| `import.food_per_money` | `3` | Food per Money | Agreed |
| `import.delivery_turns` | `0` | turns after an Arms purchase | Retained immediate `R-ARMS-002` delivery [S9]; Amir's next-turn shipment is post-MVP (owner, 1 November 2026, D1) [S12] |
| `immigration.ship_interval` / `immigration.units_per_ship` | `2 / 3` | turns / population units, average | Amir's proposed manifest schedule, declined by the owner on 30 October 2026 [S1] and post-MVP (owner, 1 November 2026, Q38 and Q39) [S12]; retained arrivals use the CBS series in `content/aliyah.yaml` [S7] |
| `workshop.staffed_turns` | `3` | staffed turns per Arms at every worker tier | Agreed |
| `workshop.cost` | `5 / 4 / 3` | Money per Arms: novice or ordinary / apprentice / expert | Agreed latest correction |

The proposed immigration mix and ship schedule are non-operative [S1]. Retained arrivals use the CBS series in `content/aliyah.yaml`, in whole units counted cumulatively so that no unit is lost or counted twice [S7]. Imported, immigration-carried and locally produced equipment are recorded separately from population to avoid double charging or free duplication.

### Treasury

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `money.debt_ceiling` | `250` | IL millions of debt at which Money is exhausted | Retained `debt_ceiling` value [S10]; only the threshold of the economic defeat, not a credit cap: no order is refused or part-funded at it (owner, 1 November 2026, Q22) [S12], except an Arms purchase whose borrowing would take Debt past it, which is refused (owner, 2 November 2026, #2198: "Refuse buys over ceiling (Recommended)", https://github.com/eyalgolan/statecraft-israel/issues/2198#issuecomment-6564721486) |
| starting Debt | `0` | IL millions | Owner, 1 November 2026, Q23 [S12]: main has no start key, and Money is IL 1m a unit (`money.il_millions`) |
| `defeat_turns` | `3` | turn ends in a row with Money and Food exhausted | Retained `R-END-002` value [S10]; owner, 1 November 2026, Q25 [S12] |
| `mvp.dollars_per_turn` | `4` | US$ millions of foreign currency converted into Money in one turn of the war | Owner, 2 November 2026 (`R-MONEY-004`) [S13] |
| `mvp.dollars_total` | `100` | US$ millions converted in the whole game | Owner, 2 November 2026 (`R-MONEY-004`); the Export-Import Bank's 1949 credit [S13] |
| `mvp.il_per_dollar` | `0.333`, then `0.357` from 19 September 1949 | IL millions per US$ million converted | Sourced effective dollar rates (`R-MONEY-004`) [S14] |

### Military and control

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `military.camp_capacity` | `50` | simultaneous trainees per camp | Owner, 1 November 2026, 27M-25 [S12]: every camp uses the same capacity, with no per-camp exception; main's `R-TRAIN-001` applies no separate simultaneous-trainee cap yet [S10]. #1668's 5 is sized for 10,000 people a unit; at 1,000 people a unit it is 50 units (owner, 1 November 2026, "Main's base x 0.3's tiers (Recommended)") [S12] (unitless) |
| `military.basic_turns` | `3` | training turns | Agreed |
| `military.issue_arms` | `1` | Arms consumed per recruit group when training starts | Retained `R-ARMS-003` value [S9] |
| `military.draft_money` / `military.draft_wait` | non-operative proposed `0 / 0` | Money / waiting turns for immediate draft | The owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists them as `0 / 0` (basis Agreed, transcribed from #1668); retained `R-TRAIN-001` (`docs/rules/mvp.md`) uses a paid training course rather than immediate draft [S10] |
| `military.demobilise_money` / `military.demobilise_wait` | non-operative proposed `0 / 0` | Money / waiting turns for demobilisation at a camp | The owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists them as `0 / 0` (basis Agreed, transcribed from #1668); retained `R-UNIT-005` (`docs/rules/mvp.md`) makes Send home the only way home [S10]; camp demobilisation is post-MVP (owner, 1 November 2026, "Keep R-UNIT-005 (Recommended)") [S12] |
| `military.demobilise_return` | `1` | issued Arms returned once on valid demobilisation | Amir's reusable-equipment proposal, which the 30 October 2026 review lists as not a conflict [S11]; Agreed in `content/gdd03-balance.yaml` |
| `military.repeat_issue` | `0` | additional Arms under Amir's reusable-equipment model | Amir's reusable-equipment proposal, which the 30 October 2026 review lists as not a conflict [S11]; Agreed in `content/gdd03-balance.yaml` |
| `military.attack_arms` / `military.artillery_arms` | `0` under `gdd-0.3` (`1 / 2` under `gdd-0.1`) | Arms per player attack / artillery firing | `R-COMBAT-004` (owner ruling, 25 October 2026): `gdd-0.3` attacks and artillery fire spend no Arms; `R-COMBAT-004` and `R-COMBAT-007` keep 1 unit of Arms per attack and 2 units per artillery firing for `gdd-0.1` [S9] |
| `military.training_money` | `0.5` | Money per basic course | Owner-set value ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S11] |
| `military.training_money` (proposal) | `0` | Money per basic course | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; the same day the owner kept the set value `0.5` instead ("Keep the set values (Recommended)", #2013 balance rows), which `content/gdd03-balance.yaml` holds (#2115) |
| `military.active_money` | `0.05` | Money per active formation per turn, independent of strength, excluding basic trainees | Owner-set value ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S11]; a formation pays from the turn it graduates (owner, 1 November 2026, `27M-8`; `R-UNIT-010`) [S12] |
| `military.active_money` (proposal) | `1` | Money per active army unit per turn, excluding basic trainees | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; the same day the owner kept the set value `0.05` instead ("Keep the set values (Recommended)", #2013 balance rows), which `content/gdd03-balance.yaml` holds (#2115) |
| `military.strength` | `1 / 2 / 3` | proposed strength units: untrained / trained / veteran | Amir's qualification-factor model; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists it as `1 / 2 / 3`, basis Default and not operative in the MVP (owner, 10 November 2026, S3: "Default, not operative (Recommended)", [#2918](https://github.com/eyalgolan/statecraft-israel/issues/2918#issuecomment-6766460488)); live Strength counts population units (`R-UNIT-001`, `docs/rules/mvp.md`) [S6] |
| `military.combat_xp_gain` / `military.combat_xp_full` | `1 / 5` | XP ticks per survived engagement / ticks to veteran | Owner, 1 November 2026, Q14 to Q16 [S12]: under `gdd-0.3` veterancy comes from combat experience, every unit (artillery firing alone included) earns it, and it stops at veterancy; time-served `R-UNIT-004` (`docs/rules/mvp.md`) stays for `gdd-0.1` |
| `military.refugee_multiplier` | `0.75` | unitless strength multiplier until reception complete | Amir's proposal; the owner's 30 October 2026 conflict review did not rule on this [S11]; `content/gdd03-balance.yaml` lists `military.refugee_multiplier` as `0.75`, basis Default and not operative in the MVP (owner, 10 November 2026: the S3 answer for `military.strength`, "Default, not operative (Recommended)", given the same for all four rows, [#2918](https://github.com/eyalgolan/statecraft-israel/issues/2918#issuecomment-6766460488)); live Strength counts population units (`R-UNIT-001`, `docs/rules/mvp.md`); the reception model is post-MVP (owner, 1 November 2026, "Keep R-CITY-004 (Recommended)") [S12] |
| `military.fortify_turns` | `1` | turns for Fortify order; defence bonus +25% | Retained turn; the bonus is main's 125% (`R-ORDER-002`), owner, 1 November 2026, O1 [S12] |
| `occupation.turns` | `3` | turns before annex-or-withdraw choice | Owner, 1 November 2026, "3 turns (27 Oct)" [S12]; `mvp.occupation_turns` reads 3 turns (`R-CTRL-012`) since [#2123](https://github.com/eyalgolan/statecraft-israel/pull/2123) |

Biome movement costs use the existing approved eight-biome framework; this catalogue does not invent the pending terrain taxonomy. Training consumes Arms (`R-ARMS-003`); under `gdd-0.3` attacks and artillery fire do not (`R-COMBAT-004`, owner ruling, 25 October 2026) [S9]. No equipment refund occurs automatically on destruction or surrender. Hold grants no combat bonus beyond hex/fort effects and no interception.

### Movement and engagement

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `combat.draw_count` | `1` | RNG draw per engagement | Retained single draw; result bands are main's `mvp.combat_table` (owner, 1 November 2026, O3) [S12] |
| `combat.retreat_hexes` | `1` | hex after retreat result | Retained baseline |
| `combat.enemy_attack_ratio` | `1.5` under `gdd-0.3` (`2` under `gdd-0.1`) | unitless minimum ratio for goal-driven enemy attack | Owner, 1 November 2026, O4: "1.5:1 in gdd-0.3 (Recommended)" [S12]; `gdd-0.1` keeps `R-ENEMY-003`'s 2:1 (unitless) |
| `combat.artillery_range` | `2` | hex support range | Owner, 1 November 2026, O5 [S12]; main's `R-COMBAT-007` real value |
| `prisoner.food` | `1` | Food per held enemy population unit per turn; each unit of Food missing costs `0.01` Social cohesion | Owner, 1 November 2026, O6 [S12]; main's `R-POW-002` real values |
| `prisoner.return_points` | `10` | points per returned enemy population unit | Owner, 1 November 2026, O7: "10 per unit, main (Recommended)" [S12]; main's `R-POW-004` real value, not Amir's 5 points a cohort |

Retreat without a legal destination becomes surrender. Result bands and loss granularity follow main's combat table: losses fall one population unit at a time on the side's strongest unit (`R-COMBAT-002`, `R-COMBAT-003`; owner, 1 November 2026, O3) [S12].

### Objectives

| Key | Value | Unit / meaning | Basis |
| --- | --- | --- | --- |
| `objective.base_points` | `100` | points per completed core objective group | Retained approved baseline |
| `objective.early_per_month` / `objective.early_cap` | `5 / 60` | points per calendar month early / maximum per dated group or part | Retained approved baseline |
| `absorption.population_target` | `1000000` | people represented; reached at or above it (owner ruling on #1714: "At or above, both (Recommended)") | Retained approved baseline |
| `absorption.max_overflow` / `absorption.min_employment` | `10 / 40` | percent ordinary-housing overflow / eligible civilian employment | Retained approved baseline; apply per the retained city objective |


## Data touched

- Target keys are mapped to validated scenario/rules fields during implementation. This file does not edit `content/rules.yaml` or claim its legacy values are current.
- Starting population roles and retained facility inventory must be validated with the sourced Historical army seed; the unresolved parameter `A` is validated only for non-Historical modes (owner, 30 Oct 2026) [S1]. Declined regional field bindings are not implemented.
- Historical values retain their content/research sources; the values left open (owner) were answered on 1 November 2026 [S12]. `content/gdd03-balance.yaml` labels the Open-mode start rows the Historical MVP does not use (D2: `start.population`, `start.money`, `start.food`, `start.spare_arms`, `start.farmers` and the Food break-even derived from them) Default, not Agreed (#2115).

## Done when

- Each active numerical design constant has one catalogue key here and a semantic owner in the feature index.
- Starting roles sum to total population; the staffed-field break-even calculation (declined with regional pooling) and the no-refill derivation (not ruled on by the 30 October 2026 conflict review; `R-FOOD-002` stays operative) are not acceptance criteria [S11].
- Implementation tests distinguish agreed values and deferred parameters; the army count is not guessed.

## Out of scope

Historical census estimates, real camp opening dates, authored region borders, actual military headcounts and the pending terrain taxonomy.

## Sources

- [S1] Design authority and provenance, docs/GDD.md.
- [S3] Owner play-time ruling, [#1687](https://github.com/eyalgolan/statecraft-israel/issues/1687#issuecomment-6511461164) (checked 2026-10-30).
- [S4] Retained census scale and city projects, docs/gdd/cities.md.
- [S5] Retained pioneer training duration, docs/gdd/training.md.
- [S6] Retained military-unit Strength rule, docs/gdd/military-combat.md.
- [S7] Retained CBS arrivals rule, docs/gdd/one-city.md.
- [S8] Retained site, swamp and corridor durations, docs/gdd/build-yishuv.md, docs/gdd/fields-and-swamps.md and docs/gdd/corridors.md.
- [S9] Retained Arms purchase, training and combat values, `docs/rules/mvp.md` (`R-ARMS-002`, `R-ARMS-003`, `R-COMBAT-004` and `R-COMBAT-007`; under `gdd-0.1` attack 1 Arms and artillery 2 Arms, under `gdd-0.3` none).
- [S10] Retained numerical and rule catalogue, `content/projects.yaml`, `content/rules.yaml` and `docs/rules/mvp.md` (`R-PROJ-001`, `R-TRAIN-001` and `R-UNIT-005`).
- [S11] Owner's 30 October 2026 rulings, [GDD conflict decisions](../GDD.md#30-october-2026-conflict-decisions).
- [S12] Owner's 1 November 2026 answers, [GDD 1 November 2026 answers](../GDD.md#1-november-2026-answers).
- [S13] Owner's 2 November 2026 ruling on #813, "Spend the dollars (Recommended)", [GDD later rulings, 2 November 2026](../GDD.md#later-rulings-2-november-2026); the $100m is the Export-Import Bank's 1949 credit (FRUS 1949 vol. VI, editorial note d407).
- [S14] Israel's dollar rates, 1948-1952: IMF, Annual Report 1950, Appendix I, pp. 88-89; Federal Reserve Bulletin, November 1949, p. 1336; Michaely, *Israel's Foreign Exchange Rate System* (1971), p. 10 and Table 2-1; Patinkin, *The Israel Economy: The First Decade*, Table 43, p. 121 ([#813](https://github.com/eyalgolan/statecraft-israel/issues/813#issuecomment-6565039439), checked 2026-11-02).
- [S15] Owner's 2 November 2026 ruling on the housing surcharge slope (#1703), recorded in `content/gdd03-balance.yaml` (`housing.money_slope`) and `docs/rules/mvp.md` (`R-HOUSE-005`); and the owner's 2 November 2026 answers on #2291, "Yes, 5 pp under GDD 0.3 (Recommended)" for the overflow and "Keep the 2-point loss (Recommended)" for the cohesion term, recorded in `docs/rules/mvp.md` (`R-HOUSE-005`).
- [S16] Owner's 3 November 2026 ruling on #1700, "Builders in the MVP (Recommended)", [GDD later rulings, 3 November 2026](../GDD.md#later-rulings-3-november-2026).
- [S17] Owner's 3 November 2026 ruling on #1694, "One farmer per field (Recommended)", [recorded on #1694](https://github.com/eyalgolan/statecraft-israel/issues/1694#issuecomment-6610258738) (checked 2026-11-03).

## Open questions

The owner answered the values this catalogue marked open (owner) on 1 November 2026 ([answers](../GDD.md#1-november-2026-answers)). Remaining principle and authoring decisions require owner rulings before implementation; the central register is [the GDD](../GDD.md#deferred-decisions).
