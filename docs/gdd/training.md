# Population, professions, reception and training

## Player story

I can follow a population unit from arrival through reception, civilian work, military service and return, without losing its identity or duplicating people.

## Rules

### Population identity and civilian professions

Each population unit has a stable id, reception progress, location/region, assignment, civilian profession/tier/work XP, military qualification/combat XP and equipment state. Scale and starting allocation are owned by [balance](balance.md#population-and-starting-state). Civilian-to-military conversion and demobilisation change the same unit; they do not spawn another group.

Farmer, pioneer and craftsman are mutually exclusive civilian professions. Farmer/pioneer tiers are novice, trained and veteran; craftsman tiers are novice, apprentice and expert. Generic builder is an assignment available to free civilians, not another profession. Once trained, a civilian profession cannot be replaced. Military service preserves civilian profession; military veterancy is independent of civilian tier.

Taking up farmer or pioneer tools pays the matching one-time entry cost. A novice may start work immediately. A later matching formal course does not charge tools again. Do not introduce profession switching while the novice lock-in boundary remains in [the GDD](../GDD.md#deferred-decisions).

Farmers earn work XP only on staffed farming turns. Pioneers earn it only while advancing an on-site pioneer project. At `profession.work_to_veteran`, novice or trained units become veteran, including the self-taught route. Travel, idle time, teaching and courses do not count. Promotion affects the next turn's output and does not reset project progress.

### Reception camps

The named facility roster is in [locations](../locations.md#facility-roster). A reception camp is a designated map facility or city upgrade. Its dedicated beds are separate from ordinary regional housing, military trainee capacity and teacher housing.

Refugees cannot take city jobs, farm, build or enter civilian profession courses before completing `reception.turns` of reception. Progress advances only in a reception bed, except approved military credit. Completion changes the same unit into a free civilian, retaining id and experience.

Refugees may enter basic military training directly without first waiting out reception. Completing basic training grants `reception.basic_credit`, capped at the full requirement; remaining reception must later be completed before civilian work. Military veterancy earned through combat completes reception immediately. A demobilised unit with incomplete reception returns to the reception/queue path, not the free workforce. The combat penalty persists until reception is complete.

When beds are full, new refugees wait at their arrival port as civilians in transit. [Resources](cutover-resource-layer.md#immigration-and-arrival-queues) owns queue costs and arrival scheduling. Port waiting gives no reception credit. Direct military training remains possible where an operating camp has capacity.

### Farmer and pioneer courses

A training-centre upgrade needs an operating reception camp and a matching veteran teacher. It supplies separate teacher housing. The teacher is fully occupied and cannot work, fight, build, travel or teach another pupil simultaneously. The trainee occupies a reception bed. Capacity and duration use [balance](balance.md#work-professions-and-training).

A free civilian or matching equipped novice may train. Completion grants the trained tier, separately from the actual-work route to veteran. Teacher/trainee each retain normal Food consumption. These centres do not train soldiers. The starting centre and veteran pioneer teacher belong to the initial population inventory, not additional people.

### Craftsmen

The initial craftsman course uses an Industry job slot, pays `craft.course_cost` upfront and takes `craft.course_turns`. It earns only `craft.course_income` during the course, then grants apprentice status. No simultaneous ordinary Industry income or extra mentor population unit is added.

Apprentices become experts after `craft.expert_work_turns` actual turns in Industry, an arms workshop or city construction. Each turn counts once, regardless of output/work rate. Initial course, travel and idle time do not count. The course cannot replace another trained civilian profession.

### Military conversion

At an operating military camp, an eligible free civilian can take up arms immediately or enter basic training. Immediate draft issues `military.issue_arms` only if needed and activates the unit without a wait. A first-time unqualified recruit is untrained; a returning soldier retains military qualification and XP. Basic training occupies a trainee place for `military.basic_turns`, costs `military.training_money` and equips only an unequipped unit. Already armed units pay no additional Arms. Already trained/veteran soldiers reactivate without repeating basic training or using it to avoid active-army upkeep. Refugees use the direct basic-training exception above.

Simultaneous trainee capacity is `military.camp_capacity`. Food continues during training; active-army Money upkeep starts only when active. No Attention cost/limit exists. Training, teaching and active service are exclusive assignments.

Demobilisation at an operating camp is immediate/free. It returns issued equipment to spare Arms once, preserves civilian profession/military experience and routes incomplete-reception units correctly. Draft/cancel/demobilise sequences cannot duplicate equipment.

Cancelled courses retain paid entry costs and earned reception credit but award no unfinished qualification. Basic equipment stays assigned until demobilisation or a valid return action; cancellation never creates both spare and equipped copies. Unfinished course progress clears, and restarting needs a fresh course. [Resources](cutover-resource-layer.md) owns affordability.

## Data touched

- Individually identifiable population units replace anonymous city-only recruit counts.
- Unit types, scenario inventory, facilities and training commands require coordinated migration.
- Reception beds, teacher housing, civilian jobs and military trainee slots are distinct capacity fields.
- Army size and real reception-facility availability stay deferred in the GDD.

## Done when

- Population identity reconciles through reception, courses, work, service and demobilisation.
- Refugees cannot work; military credit does not duplicate reception; combat veterancy clears the remaining obligation.
- An occupied teacher refuses another pupil and ordinary housing does not substitute for trainee beds.
- Conversion/cancellation cannot duplicate people, qualifications or equipment.
- Self-taught and trained farmers/pioneers use the same actual-work threshold; craftsmen retain their separate progression.

## Out of scope

Camp growth into permanent towns, switching trained professions, soldier training in civilian centres and unsourced opening dates.

## Sources

- [S1] Interview decisions and deferred boundaries, docs/GDD.md.
- [S2] Numerical catalogue, docs/gdd/balance.md.
- [S3] Facility identities and evidence, docs/locations.md.

## Open questions

No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
