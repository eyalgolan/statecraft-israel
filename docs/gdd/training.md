# Training: military camp and Hakhshara

## Player story

As a player of the MVP, I want to send Tel Aviv's civilians without work to basic training or to pioneer training, so that the city has recruits for defence and halutzim to build new places (GDD "Progression" [S1]; decisions D-A3, D-D5 [S2]).
I can follow a population unit from arrival through reception, civilian work, military service and return, without losing its identity or duplicating people.

## Rules

- One military camp and one Hakhshara exist in Tel Aviv at the start. This is a design assumption, not a claim about the city (D-D5 [S2]). The GDD leaves training capacity open, so there is no capacity limit (unitless).
- Pioneers build more: `PioneerWork(unit, build_camp, site)` at a camp site takes 6 turns and IL 2m, labelled design values an agent proposed and the owner accepted (#927; the balance pass, #813, may tune them), and follows the yishuv build's rules. Built, the site's city has the facility from the next turn, with no capacity limit (`Q-CAMP-1`, `Q-CAMP-3` [S4]) (R-CAMP-001).
- The camp sites are `Camp 80` near Pardes Hanna, for Hadera, and Tzrifin, for Rishon LeZion, both military camps (`Q-CAMP-2` [S5]; owner, "Both military", #927). A camp serves the city nearest by hex, and a tie goes to the nearest by km (R-CAMP-002). Both rows are drafts: their sources were read by AI agents for the research note, and Tzrifin's exact position and a GIS clearance are still gaps.
- `Train(city, track, n)` takes civilians without a job into training as recruits (military camp) or halutzim (Hakhshara). It costs 1 Attention and IL 0.5m per group, both placeholders, paid at once. A short Treasury borrows the rest, as map actions do (owner decision on #468) (R-TRAIN-001).
- Basic training takes 1 unit of Arms a group when it starts, a design value the owner approved (#781); too few Arms refuses the order with a reason, and cancelling the group returns its Arms to the pool (owner, #1047). Pioneer training takes no Arms (R-ARMS-003).
- It is refused, with the state unchanged, for fewer than 1 group (unitless), more than the city's civilians without a job, an unknown city or track, a city without the facility, or the legacy loop (R-TRAIN-001).
- People in training are neither civilians nor recruits: they hold no job and are not counted by housing or Social cohesion while they train.
- `CancelTraining(cohort)` returns the people as civilians without a job; the Money spent is not returned (D-A3 [S2]). It costs 0.5 Attention, a placeholder (R-TRAIN-002).
- Training resolves before production in each turn, in the order the owner approved (D-A6, #456 [S2][S3]). A cohort ordered this turn counts this turn: basic training takes 3 turns and pioneer training 4 turns, both placeholders (R-TRAIN-003).
- A finished recruit cohort adds to the city's recruits, a count until soldiers become units. A finished halutz cohort becomes one movable unit per group at the city's hex, with consecutive ids (D-A2 [S2], R-TRAIN-003).

The owner later decided that craftsmen, halutzim and veteran pioneers may return to civilian life and retain their qualification, and qualified civilians may be reassigned without retraining (R-QUAL-001 to R-QUAL-007; #1511).

**Proposal (Amir, #1668), needs owner ruling:** make trained civilian professions permanent and mutually exclusive, with no profession switching or retraining UI. This conflicts with R-QUAL-001 to R-QUAL-007.
### Population identity and civilian professions

Each population unit has a stable id, reception progress, location/region, assignment, civilian profession/tier/work XP, military qualification/combat XP and equipment state. Scale and starting allocation are owned by [balance](balance.md#population-and-starting-state). Civilian-to-military conversion and demobilisation change the same unit; they do not spawn another group.

Farmer, pioneer and craftsman are mutually exclusive civilian professions. Farmer/pioneer tiers are novice, trained and veteran; craftsman tiers are novice, apprentice and expert. Generic builder is an assignment available to free civilians, not another profession. Once trained, a civilian profession cannot be replaced. Military service preserves civilian profession; military veterancy is independent of civilian tier.

Taking up farmer or pioneer tools pays the matching one-time entry cost. A novice may start work immediately. A later matching formal course does not charge tools again. Do not introduce profession switching while the novice lock-in boundary remains in [the GDD](../GDD.md#deferred-decisions).

Farmers earn work XP only on staffed farming turns. Pioneers earn it only while advancing an on-site pioneer project. At `profession.work_to_veteran`, novice or trained units become veteran, including the self-taught route. Travel, idle time, teaching and courses do not count. Promotion affects the next turn's output and does not reset project progress.

### Reception camps

The named facility roster is in [locations](../locations.md#facility-roster). A reception camp is a designated map facility or city upgrade. Its dedicated beds are separate from ordinary regional housing, military trainee capacity and teacher housing.

Under Amir's proposal, refugees cannot take city jobs, farm, build or enter civilian profession courses before completing `reception.turns` of reception. Progress advances only in a reception bed, except approved military credit. Completion changes the same unit into a free civilian, retaining id and experience.

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

- `unit_types.yaml` (`recruit`, `halutz`: `facility`, `training_turns`, `training_cost`), `cities.yaml` (`facilities`), `rules.yaml` `attention_cost.train`, `cancel_training`, `mvp.build_camp_turns` and `mvp.build_camp_cost`: labelled design values in `content/` since the cutover (Q-CUT-1, R-LOOP-005), the overlay's placeholders before it (ruling 2 [S3]).
- `GameState`: `Cohorts` (hashed as `train[...]` when there are some), a city's `Recruits` (in its hash record when not 0), `Units`, `NextId`, `Treasury`, and a built camp's `Sites` record, which is where `Training.HasFacility` reads a built camp from.
- `content/sites.yaml`: the `camp_80` and `tzrifin` rows, each with a `city`.
- Save files: the `train CITY TRACK N` and `untrain COHORT` lines (and `retcraft`, `regcraft`, `regpioneer`, `order UNIT return_civilian`, R-QUAL-007). Scenarios: `train` and `cancel_training` (and `return_craftsmen`, `reassign_craftsmen`, `reassign_pioneers`).
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: `mvp-slice` moved (retired at the cutover); `scenario-mvp-absorb-first` did not. Phase 4 PR 3 moves `hash1950`, `hash1952` and `mvp-end`: the reference player builds the two camps once the yishuv sites are taken (docs/determinism.md).
- Individually identifiable population units replace anonymous city-only recruit counts.
- Unit types, scenario inventory, facilities and training commands require coordinated migration.
- Reception beds, teacher housing, civilian jobs and military trainee slots are distinct capacity fields.
- Army size and real reception-facility availability stay deferred in the GDD.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `TrainingTests`, `CampsTests`, `SitesContentTests`, `CitiesContentTests`, `GddStateTests` and `ReferencePlayerTests`.
- `smoke5`, `scenario-cautious`, `scenario-reckless` and `scenario-mvp-absorb-first` do not move; `hash1950`, `hash1952` and `mvp-end` have a row for #787 in docs/determinism.md.
- Population identity reconciles through reception, courses, work, service and demobilisation.
- Refugees cannot work; military credit does not duplicate reception; combat veterancy clears the remaining obligation.
- An occupied teacher refuses another pupil and ordinary housing does not substitute for trainee beds.
- Conversion/cancellation cannot duplicate people, qualifications or equipment.
- Self-taught and trained farmers/pioneers use the same actual-work threshold; craftsmen retain their separate progression.

## Out of scope

- The start camps of Jerusalem (Schneller) and Petah Tikva (Tel HaShomer), which the owner ruled the cities already have (#375); this PR keeps the start facilities as they are. Training capacity, recruits as units and combat (Phase 5), veterans, craftsmen (D-D4), and any display (PRs 7 and 8).
Camp growth into permanent towns, switching trained professions, soldier training in civilian centres and unsourced opening dates.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 4, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] GDD Phase 4 plan, PR 3 and the Q-CAMP-1 to Q-CAMP-3 rows, docs/superpowers/plans/2026-10-15-gdd-phase-4.md; the owner's answer, "take all of the plan's recommendations", https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128, checked 2026-10-17
- [S5] The owner's Q-CAMP-2 answer, "Both", https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233777137; the research, https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233553842 and research_notes/gdd-phase-4-sourcing.md, rows C1 and C4; links checked 2026-10-17
- [S1] Interview decisions and deferred boundaries, docs/GDD.md.
- [S2] Numerical catalogue, docs/gdd/balance.md.
- [S3] Facility identities and evidence, docs/locations.md.

## Open questions

- The placeholder values (training turns, cost per group, the two Attention costs) wait for the owner.
- Whether people in training, and recruits, take up the city's housing. Housing counts civilians only (R-CITY-004), so a group in training frees a place that a waiting refugee then takes.
- Whether the turn of the order counts as a training turn. This is the plan's reading of D-A6, not part of the owner's ruling. The other reading adds a turn.
- Social cohesion's employed share rises while civilians without work are in training, because they leave the civilians (R-CITY-005).
No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
