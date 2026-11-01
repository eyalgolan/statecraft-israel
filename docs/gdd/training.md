# Training: military camp and Hakhshara

## Player story

As a player of the MVP, I want to send Tel Aviv's civilians without work to basic training or to pioneer training, so that the city has recruits for defence and halutzim to build new places (GDD "Progression" [S1]; decisions D-A3, D-D5 [S2]).
I can follow a population unit through civilian work, military service and return without losing its identity or duplicating people.

## Rules

- One military camp and one Hakhshara exist in Tel Aviv at the start. This is a design assumption, not a claim about the city (D-D5 [S2]). The GDD leaves training capacity open, so there is no capacity limit (unitless).
- Pioneers build more: `PioneerWork(unit, build_camp, site)` at a camp site takes 6 turns and IL 2m, labelled design values an agent proposed and the owner accepted (#927; the balance pass, #813, may tune them), and follows the yishuv build's rules. Built, the site's city has the facility from the next turn, with no capacity limit (`Q-CAMP-1`, `Q-CAMP-3` [S4]) (R-CAMP-001).
- The camp sites are `Camp 80` near Pardes Hanna, for Hadera, and Tzrifin, for Rishon LeZion, both military camps (`Q-CAMP-2` [S5]; owner, "Both military", #927). A camp serves the city nearest by hex, and a tie goes to the nearest by km (R-CAMP-002). Both rows are drafts: their sources were read by AI agents for the research note, and Tzrifin's exact position and a GIS clearance are still gaps.
- `Train(city, track, n)` takes civilians without a job into training as recruits (military camp) or halutzim (Hakhshara). It costs 1 Attention and IL 0.5m per group, both placeholders, paid at once. A short Treasury borrows the rest, as map actions do (owner decision on #468) (R-TRAIN-001).

**Then decided ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)):** Basic military training costs 0.5 per course in Money; the Money value is no longer a placeholder [S10].
- Basic training takes 1 unit of Arms a group when it starts, a design value the owner approved (#781); too few Arms refuses the order with a reason, and cancelling the group returns its Arms to the pool (owner, #1047). Pioneer training takes no Arms (R-ARMS-003).
- It is refused, with the state unchanged, for fewer than 1 group (unitless), more than the city's civilians without a job, an unknown city or track, a city without the facility, or the legacy loop (R-TRAIN-001).
- People in training are neither civilians nor recruits: they hold no job and are not counted by housing or Social cohesion while they train.
- `CancelTraining(cohort)` returns the people as civilians without a job; the Money spent is not returned (D-A3 [S2]). It costs 0.5 Attention, a placeholder (R-TRAIN-002).
- Training resolves before production in each turn, in the order the owner approved (D-A6, #456 [S2][S3]). A cohort ordered this turn counts this turn: basic training takes 3 turns and pioneer training 4 turns, both placeholders (R-TRAIN-003).
- A finished recruit cohort adds to the city's recruits, a count until soldiers become units. A finished halutz cohort becomes one movable unit per group at the city's hex, with consecutive ids (D-A2 [S2], R-TRAIN-003).

The owner later decided that craftsmen, halutzim and veteran pioneers may return to civilian life and retain their qualification, and qualified civilians may be reassigned without retraining (R-QUAL-001 to R-QUAL-007; #1511).

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed permanent, mutually exclusive trained civilian professions with no switching or retraining UI.* [S1] *On 1 November 2026 the owner kept two-way professions for the MVP (D3, "Approve all six (Recommended)"); the lock is post-MVP.* [S11]
### Population identity and civilian professions

Each population unit has a stable id, location, retained civilian qualification and retained military service state. Civilian-to-military conversion and demobilisation change the same unit; they do not spawn another group. Amir's proposed regional location, reception progress, combat XP and reusable-equipment fields are non-operative [S6]; Amir's working-turn XP agrees with main's `gdd-0.3` rule (`R-VET-001`).

**Amir's proposal, declined by the owner on 30 October 2026 [S6]:** farmer, pioneer and craftsman would be mutually exclusive, and a trained civilian profession could not be replaced. The retained rule permits qualified civilians to return and be reassigned without retraining. On 1 November 2026 the owner kept this two-way rule for the MVP (`R-QUAL-001` to `R-QUAL-007`): a Train order can take a qualified civilian, who then loses the qualification (`R-QUAL-006`), and the lock at the first qualified level (Q9) is post-MVP (D3) [S11].

**Amir's proposal [S6]:** a novice could pay a one-time tool cost and start farmer or pioneer work immediately. The owner's 30 October 2026 conflict review did not rule on this; live `PioneerWork` requires a halutz (`R-SITE-001`, `docs/rules/mvp.md:185`) and pioneer training takes four turns, while `content/gdd03-balance.yaml` lists `profession.farmer_tools` / `profession.pioneer_tools` (basis Agreed) for the inactive `gdd-0.3` helpers (`R-QUAL-008`, `docs/rules/mvp.md:215`). Under the retained [owner ruling #1511](https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6372356482), qualified civilians may return and be reassigned while keeping their qualification.

Amir's rule that farmers and pioneers earn working-turn XP and can promote through a self-taught route agrees with main's `gdd-0.3` scoping (`R-VET-001` and `R-VET-002`, `docs/rules/mvp.md:193` and `:195`); the 30 October 2026 review lists it as not a conflict [S6]. Six service turns and veteran double progress apply to `legacy` and `gdd-0.1`.

### Reception camps

The named facility roster is in [locations](../locations.md#facility-roster) [S8]. The roster does not make Amir's proposed reception-bed pipeline operative [S6].

**Amir's proposal [S6]:** refugees would need six reception turns before civilian work. The owner's 30 October 2026 conflict review did not rule on this; on 1 November 2026 the owner kept `R-CITY-004` (`docs/rules/mvp.md:105`) with "Keep R-CITY-004 (Recommended)", so the reception turns are post-MVP; main has an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`) [S11].

The proposed basic-training reception credit, combat-veterancy completion, demobilisation queue, port queue and reception combat penalty are part of Amir's reception model, which the 30 October 2026 conflict review did not rule on [S6]; main has only an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`). Under the retained city rule, every refugee becomes a civilian on the next housing step, and this reception model is post-MVP (owner, 1 November 2026, "Keep R-CITY-004 (Recommended)"; Q40, Q41 and Q54) [S11]. Refugees cannot train until they are civilians (27M-23, 27M-10). (unitless)

### Farmer and pioneer courses

A retained training centre uses its matching veteran teacher under the approved course rules. Amir's proposed dependency on an operating reception camp, separate teacher housing and reception-bed capacity is non-operative [S6]. A centre has one veteran teacher and one pupil, and the teacher is housed as an ordinary resident of the centre's city; the starting centre is at Kiryat Shmuel, Haifa (owner, 1 November 2026, Q42 and O8) [S11] (unitless).

An eligible free civilian may train under the retained course rules. Completion grants the retained qualification; there is no actual-work route to qualification, while under `gdd-0.3` novice and trained farmers/pioneers reach veteran through actual work (`R-VET-001`). These centres do not train soldiers. The starting centre and veteran pioneer teacher belong to the initial population inventory, not additional people. A course occupies its people from the turn it starts, for any job, other course, pioneer work or military order, without changing their housing; they are released in the turn its last course turn counts and can be assigned from the next planning, and their work starts the turn after (owner, 1 November 2026, Q43 and the #1697 follow-up) [S11].

### Craftsmen

The initial craftsman course uses an Industry job slot, pays `craft.course_cost` of 0.5 per course in Money upfront and takes `craft.course_turns` of 3 turns ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S10]. It earns only `craft.course_income` during the course, then grants apprentice status. No simultaneous ordinary Industry income or extra mentor population unit is added.

Apprentice-craftsman progression remains separate from the farmer/pioneer working-turn rule. Under the retained #1511 ruling, qualified civilians may return and be reassigned without retraining.

Apprentices become experts after `craft.expert_work_turns` actual turns in Industry or an arms workshop. Each turn counts once, regardless of output/work rate. Initial course, travel, idle time and worker-free city construction do not count [S6].

### Military conversion

Under retained `R-TRAIN-001`, `Train(city, track, n)` takes civilians without a job into a military course when the city has the matching camp; the course charges its retained Money and Arms costs [S9]. **Amir's proposal [S6]:** an eligible civilian at an operating camp could instead take up arms immediately without a wait. The owner's 30 October 2026 conflict review did not rule on this; `R-TRAIN-001` stays operative, and `content/gdd03-balance.yaml` lists `military.draft_money` / `military.draft_wait` (basis Agreed). Amir's reusable equipment agrees with main (the review lists Arms as not a conflict); for Amir's reception exceptions the review did not rule [S6][S7].

Retained training, teaching and active service are exclusive assignments. Active service costs 0.05 per active army unit per turn in Money, excluding basic trainees ([owner, 30 Oct 2026](../GDD.md#30-october-2026-conflict-decisions)) [S10]. Under `gdd-0.3` there is no Attention resource (`R-GOV-002`), as Amir's proposal also has; it was not one of the 30 October 2026 conflicts [S10].

Under retained `R-UNIT-005`, Send home is the only way home: no automatic demobilisation happens, and the order is unavailable until its armistice condition is met; when it runs, the unit leaves the map and its strength joins the nearest city as civilians without a job [S9]. **Amir's proposal [S6]:** demobilisation at an operating camp would be immediate/free, preserve the proposal's military state and route incomplete-reception units through the proposed queue. The owner's 30 October 2026 conflict review did not rule on this; `R-UNIT-005` (`docs/rules/mvp.md:247`) stays operative, and `content/gdd03-balance.yaml` lists `military.demobilise_money` / `military.demobilise_wait` (basis Agreed). Amir's return of issued Arms to a reusable pool agrees with main (`military.demobilise_return`; the review lists Arms as not a conflict).

Cancelled retained courses award no unfinished qualification. A course whose slot is lost, or whose people are reassigned, is cancelled like `CancelTraining`: people return at once as civilians without a job, Money and tools are not refunded, and Arms return to the pool; a novice may then take another profession, and graduates may re-enrol at any time (owner, 1 November 2026, Q44, Q52, 27M-3 to 27M-5) [S11]. The novice craftsman label starts with the craftsman course and ends on cancelling (#1698 follow-up) [S11]. Proposed reception credit and reusable-equipment cancellation rules are non-operative [S6]. [Resources](cutover-resource-layer.md) owns retained affordability rules. (unitless)

## Data touched

- `unit_types.yaml` (`recruit`, `halutz`: `facility`, `training_turns`, `training_cost`), `cities.yaml` (`facilities`), `rules.yaml` `attention_cost.train`, `cancel_training`, `mvp.build_camp_turns` and `mvp.build_camp_cost`: labelled design values in `content/` since the cutover (Q-CUT-1, R-LOOP-005), the overlay's placeholders before it (ruling 2 [S3]).
- `GameState`: `Cohorts` (hashed as `train[...]` when there are some), a city's `Recruits` (in its hash record when not 0), `Units`, `NextId`, `Treasury`, and a built camp's `Sites` record, which is where `Training.HasFacility` reads a built camp from.
- `content/sites.yaml`: the `camp_80` and `tzrifin` rows, each with a `city`.
- Save files: the `train CITY TRACK N` and `untrain COHORT` lines (and `retcraft`, `regcraft`, `regpioneer`, `order UNIT return_civilian`, R-QUAL-007). Scenarios: `train` and `cancel_training` (and `return_craftsmen`, `reassign_craftsmen`, `reassign_pioneers`).
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: `mvp-slice` moved (retired at the cutover); `scenario-mvp-absorb-first` did not. Phase 4 PR 3 moves `hash1950`, `hash1952` and `mvp-end`: the reference player builds the two camps once the yishuv sites are taken (docs/determinism.md).
- Finished recruit cohorts remain city recruit counts until soldiers become units; Amir's proposal to replace those counts during training is non-operative [S6].
- Unit types, scenario inventory, facilities and training commands require coordinated migration.
- **Amir's proposal [S6]:** reception beds, teacher housing and proposed military trainee slots would be distinct capacity fields; the owner's 30 October 2026 conflict review did not rule on this, and main has only inactive `gdd-0.3` fixtures for them (`docs/rules/mvp.md:107`, `docs/rules/mvp.md:167`). Reception beds are post-MVP, the teacher lives as an ordinary resident, and every military camp uses `military.camp_capacity` (owner, 1 November 2026, "Keep R-CITY-004 (Recommended)", Q42 and 27M-25) [S11].
- Historical army size comes from the sourced `content/map/units_1948.yaml` seed (`Q-START-1`, decided 15 October 2026); only the non-Historical parameter `A` and real reception-facility availability stay deferred in the GDD (owner, 30 Oct 2026) [S6]; the starting centre and reception host is Kiryat Shmuel, Haifa (owner, 1 November 2026, O8 and 27M-25) [S11].

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `TrainingTests`, `CampsTests`, `SitesContentTests`, `CitiesContentTests`, `GddStateTests` and `ReferencePlayerTests`.
- `smoke5`, `scenario-cautious`, `scenario-reckless` and `scenario-mvp-absorb-first` do not move; `hash1950`, `hash1952` and `mvp-end` have a row for #787 in docs/determinism.md.
- Population identity reconciles through retained courses, work, service and demobilisation; the proposed reception state is non-operative.
- **Amir's proposal [S6]:** refugees could not work until proposed reception completed, military credit would offset reception, and combat veterancy would clear it. The owner's 30 October 2026 conflict review did not rule on this; on 1 November 2026 the owner kept `R-CITY-004` (`docs/rules/mvp.md:105`) with "Keep R-CITY-004 (Recommended)", so this is post-MVP; main has an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`) [S11].
- Retained teacher/course capacity prevents duplicate pupils; proposed reception-bed substitution rules are non-operative.
- Conversion/cancellation cannot duplicate people, qualifications or equipment.
- Under `gdd-0.3` self-taught and trained farmers/pioneers use the same actual-work threshold (`R-VET-001`), as Amir's proposal and main agree [S6].

## Out of scope

- The start camps of Jerusalem (Schneller) and Petah Tikva (Tel HaShomer), which the owner ruled the cities already have (#375); this PR keeps the start facilities as they are. Training capacity, recruits as units and combat (Phase 5), veterans, craftsmen (D-D4), and any display (PRs 7 and 8).
Camp growth into permanent towns, soldier training in civilian centres and unsourced opening dates. The retained qualification rule permits switching back to qualified civilian roles without retraining.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 4, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] GDD Phase 4 plan, PR 3 and the Q-CAMP-1 to Q-CAMP-3 rows, docs/superpowers/plans/2026-10-15-gdd-phase-4.md; the owner's answer, "take all of the plan's recommendations", https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128, checked 2026-10-17
- [S5] The owner's Q-CAMP-2 answer, "Both", https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233777137; the research, https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233553842 and research_notes/gdd-phase-4-sourcing.md, rows C1 and C4; links checked 2026-10-17
- [S6] Interview decisions and deferred boundaries, docs/GDD.md.
- [S7] Numerical catalogue, docs/gdd/balance.md.
- [S8] Facility identities and evidence, docs/locations.md.
- [S9] Retained training and Send home rules, `docs/rules/mvp.md` (`R-TRAIN-001` and `R-UNIT-005`).
- [S10] Owner's 30 October 2026 rulings, [GDD conflict decisions](../GDD.md#30-october-2026-conflict-decisions).
- [S11] Owner's 1 November 2026 answers, [GDD 1 November 2026 answers](../GDD.md#1-november-2026-answers).

## Open questions

- The placeholder values (training turns, cost per group, the two Attention costs) wait for the owner.
- Whether people in training, and recruits, take up the city's housing. Housing counts civilians only (R-CITY-004), so a group in training frees a place that a waiting refugee then takes.
- Whether the turn of the order counts as a training turn. This is the plan's reading of D-A6, not part of the owner's ruling. The other reading adds a turn.
- Social cohesion's employed share rises while civilians without work are in training, because they leave the civilians (R-CITY-005).
No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
