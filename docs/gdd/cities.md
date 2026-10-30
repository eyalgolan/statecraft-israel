# Cities and absorption

## Player story

As a player of the MVP, I want the twelve Jewish cities and towns to start at their real 1948 sizes and to absorb the arriving immigrants through housing, jobs and city projects, so that absorbing the immigration wave is the heart of the game (GDD "Cities and construction" and objective two [S1]; the Phase 3 plan [S2]).
I send a population unit to a regional capital and assign work there, while the map explains which fields and facilities make it possible.

## Rules

- This phase's managed cities are the twelve Jewish `city` and `town` rows of `settlements_1948.yaml` (the plan [S2]); the six kibbutzim and moshavim are not cities in this phase (`Q-START-4`, decided 15 October 2026 [S3]). The broader map-production roster is separate [S13]: a must-have marker does not automatically become a managed city, and optional authoring does not remove an existing city.
- Tel Aviv and Jaffa retain distinct map identities while sharing the approved Tel Aviv-Yafo city economy. Jaffa does not receive a second population or immigrant-allocation pool (the plan's city-start decision [S2]; the location roster [S13]).
- Each city starts at its census population of 8 November 1948, every field recorded with a status: sourced, derived, wikipedia-only or design (the plan, proposed R-CITY-006 [S2]; the CBS census table [S4]). The owner chose the census values plus defaults for the gaps ("Census values + defaults", the owner on #569, recorded in the plan [S2]), and the census-day count is used as the 15 May 1948 start value.
- People become units at one thousand people per unit, rounding to nearest with a minimum of one unit for any city that has people, so Rosh Pinna starts with one (`Q-START-2`, decided 15 October 2026 [S3]).

**Proposal (Amir, #1668), needs owner ruling:** use 10,000 people per population unit instead of the Q-START-2 scale. (unitless)
- The five cities with a sourced working share start with derived employment [S4]; every other city starts from the national working share of 43.9 percent, its job slots equal its start employed count, and its housing capacity equals its start population (`Q-START-1`, decided 15 October 2026 [S3]).
- Arrivals spread over the cities in proportion to census population, by cumulative largest-remainder apportionment with an ordinal tie-break (`Q-ARRV-1`, decided 15 October 2026 [S3]). The units are handed out one at a time, each to the city with the largest remainder, so a growing total never takes a unit back from a city (R-CITY-003). The cities take arrivals only from the census day, while the national head count takes every arrival from 15 May 1948, so no immigrant is counted twice (`Q-ARRV-3`, decided 15 October 2026 [S3]).
- `GameState.Population` stays the one record of the nation's people; the cities are the part placed in towns, and an invariant test checks they never exceed it (`Q-ARRV-2`, decided 15 October 2026 [S3]).
- Housing has per-city capacity and an average condition on a zero-to-one scale (`Q-START-3`). One capacity point houses one unit, and every refugee becomes a civilian on the next housing step (`Q-HOUSE-1`, decided 15 October 2026 [S3]). There is no hard cap: people above capacity are the city's overflow, and overflow lowers condition and Social cohesion (`Q-HOUSE-1` and `Q-HOUSE-2`, decided 15 October 2026 [S3]; D-D1, D-D2). The start condition and the size of the overflow effect are labelled design values taken from the plan's PR zero table (the plan's table merged by #604).

**Proposal (Amir, #1668), needs owner ruling:** refugees require six turns in reception before civilian work, rather than becoming civilians on the next housing step.
- Two housing types: permanent housing adds capacity and condition from the game's start; the transit camp is cheap, adds capacity and lowers condition, and keeps the ma'abarot policy gate in 1950 (`Q-HOUSE-3`, decided 15 October 2026 [S3]). Residents are assigned automatically, and people in training and recruits count against their city's housing while halutz units do not (`Q-HOUSE-4` and `Q-HOUSE-5`, decided 15 October 2026 [S3]).
- City projects (housing and industry) are paid in full at the start and borrowed if the Treasury is short, need no workers, take a free land hex within one hex of the settlement hex that holds no other placement, and at most two start per turn nationally (`Q-PROJ-1` to `Q-PROJ-4`, decided 15 October 2026 [S3]). Starting a project costs Attention and cancelling costs half as much, both labelled design values (`Q-PROJ-5`, decided 15 October 2026 [S3]). Their costs, times and capacities are labelled design values taken from the plan's PR zero table (the plan's table merged by #604; `Q-PROJ-1`).

**Proposal (Amir, #1668), needs owner ruling:** city projects require assigned builders, only one new city/region project may start nationally each turn, full Money must be available without borrowing, and completed facilities may have non-zero upkeep. This conflicts with Q-PROJ-1/4, D-C3 and the D-C6 option-3 upkeep ruling. (unitless)
- The craftsman trains in a city with a completed industry project, fills an ordinary job slot and earns a multiple of the job income (`Q-CRAFT-1` and `Q-CRAFT-2`, decided 15 October 2026 [S3]; D-D4). The income multiple, training time and training cost are labelled design values taken from the plan's PR zero table (the plan's table merged by #604; `Q-CRAFT-2` and `Q-CRAFT-3`). Free job slots fill automatically at no Attention cost until the player fixes a city's number with `AssignWorkers` (`Q-JOBS-1`, decided 15 October 2026 [S3]).
- The one specialised industry is the winery, in Rishon LeZion, its vineyards flagged `to_verify`: `Task 6.0` found no dated 1948 to 1951 source for any town's vineyards [S14], and the owner ruled the city on #737 (20 October 2026, as GDD v0.2 and the map roster say [S16], [S13]; Carmel Winery's first winery is in Rishon LeZion [S5]). This replaces the 18 October default of Zikhron Ya'akov [S15], which has no winery. The winery's job slots are a labelled design value taken from the plan's PR zero table (the plan's table merged by #604; `Q-SPEC-2`).
- The "absorb immigrants" objective completes when, over all city residents, no refugees wait for housing, overflow stays within its bound, and employed civilians reach their share (`Q-ABS-2`, decided 15 October 2026 [S3]; D-B4; the two shares are labelled design values taken from the plan's PR zero table, merged by #604). Its milestone is the Jewish population passing one million, marked on 20 December 1949 (`Q-ABS-1`, the owner's pick, 15 October 2026 [S3]; JTA [S6]). With that milestone the card shows no photograph unless the owner approves a generic arrival one (`Q-ABS-3`, decided 15 October 2026 [S3]).
- The on-screen Hebrew words: craftsman בעל מלאכה, housing condition מצב הדיור, housing project מיזם דיור, industry תעשייה, winery יקב (`Q-UI-2`, all five approved, decided 15 October 2026 [S11]).
### Regional management

Each player region has an authored capital and member tiles/locations. The capital is the civilian interface. Housing and Industry/workshop jobs are pooled regionally. A map marker does not automatically create another managed city. Small rural places retain map identity without separate population-management screens.

Farm assignments link a unit based at the capital to an available prepared field tile in that region. The tile must be workable, prepared, friendly controlled and have a free slot. [Site projects](build-yishuv.md) own preparation. Only staffed fields produce Food at [balance](balance.md#work-professions-and-training) tier rates; farming creates no Money. Farmers need not individually commute between each kibbutz and capital every turn.

Enemy control disables a field slot and makes its farmer idle at the capital. Retaking restores access without automatic rebuilding. [Settlement control](settlement-control.md) owns capital/region loss and evacuation. Enemy towns use its separate tax model, not this workforce pool.

### Assignments and income

A unit has one active assignment. Eligible free civilians may take Industry jobs or generic builder slots; trained farmers/pioneers may use generic assignments without learning another profession. Craftsmen modify Industry income and builder rates. Industry produces only when staffed; courses use [training](training.md) course income.

Farm work, Industry, building, teaching, courses and active service cannot run simultaneously for the same unit. Reassignment releases old capacity atomically. Invalid capacity/eligibility refuses before Money or state changes.

### Housing and condition

Ordinary regional capacity is limited. A civilian resident without dedicated reception, teacher or military-training housing uses ordinary housing. Overflow does not create beds, delete population or convert refugees. Free arrivals/evacuated free civilians may become overcrowded residents; incomplete-reception refugees use camp/port queues. Deployed active army units are not ordinary civilian residents.

Let `R_r` be ordinary civilian residents, `c_r` condition fraction, and `P` total own population. Project capacity/condition effects live in [balance](balance.md#project-catalogue), clamped to condition bounds. No separate household simulation is introduced.

Additional Money per turn is `R_r * max(0, housing.money_threshold - c_r) * housing.money_slope`. It rises smoothly; crossing the threshold never adds a flat fee to every resident.

Housing cohesion loss is `housing.max_cohesion_loss * sum((R_r / P) * max(0, (housing.cohesion_threshold - c_r) / housing.cohesion_threshold))`. With no own population it is zero. Camp residents do not receive a second ordinary-housing penalty. Food and port queue penalties have separate owners; do not also charge legacy effects for the same housing loss.

Overflow lowers condition by `housing.overflow_deterioration * max(0, R_r - capacity_r) / R_r` pp per turn, zero with no residents. Apply once alongside maintenance deterioration, then use the condition-to-cohesion rule without a second direct overflow penalty.

Unpaid upkeep lowers housing condition proportionally; [resources](cutover-resource-layer.md#money-and-upkeep) owns cash-shortfall allocation. Repairs pay the catalogue cost/work, improve condition and add no capacity. Only houses deteriorate from unpaid upkeep; no facility shutdown, army penalty or direct cash-shortage cohesion hit is added.

### City projects and builders

Under Amir's proposal, city projects include housing, Industry, reception camps, centres, military camps, workshops and repairs. Start requires full upfront Money and a national city-project start. Decided amounts are catalogued in [balance](balance.md); upkeep remains open (owner) where Amir did not set it.

Advance by assigned builder work rates up to `builder.slots_per_project`. No workers means pause, not timer completion. Ongoing projects may coexist while the nationwide new-start cap applies. Discard excess work on completion and release workers. Builders earn no simultaneous Industry income. Completed projects begin upkeep that turn but provide no second production pass.

Winery Industry uses these shared job/production rules; place/theme binding belongs to [locations](../locations.md). The maabara retains its existing reviewed ma'abarot-policy availability gate in 1950 [S4]; reception beds are separate, and the gate is not a fixed construction timer.

Stopping/moving workers pauses paid progress. Explicit cancellation discards unfinished work without refund. Field/road/rail/standalone pioneer work is outside the city-start cap.

### Arms workshops

A workshop has its own worker slot. Reserve the full cycle cost upfront at the worker's tier. Each staffed turn advances it; no worker means pause. Completion adds spare Arms nationally. All tiers share cycle duration; expertise lowers Money cost. Pause or worker change retains progress and paid cost; the next cycle uses the new tier. Refuse an unaffordable new cycle. The workshop pays no simultaneous Industry income.

### Initial authoring

[Starting inventory](balance.md#population-and-starting-state) binds population/facilities to authored friendly regions. Initial prepared fields must support the agreed staffed farmer mix across multiple regions. Census records remain evidence; do not round each small location into another whole game unit.

## Data touched

- `content/cities.yaml`: the twelve cities' start values, each field with a status (the plan, PR 1 [S2]).
- `content/rules.yaml` (`mvp:`): the arrival split, overflow and condition values, project costs and turns, craftsman values and Attention costs, all labelled design values (the plan [S2]).
- `Cities.cs`, `PopulationSystem.cs`, `Projects.cs` (new, the first phase): arrivals, housing and projects (the plan [S2]).
- `objectives.yaml` (new, the first phase): the absorb objective's completion rule and card (the plan [S2]).
- The city panel (Unity) at twelve cities (the plan [S2]).
- Region id, capital, member hexes, housing/condition, facilities and jobs.
- Per-unit assignments reference regions and specific fields, projects, facilities and jobs.
- Legacy cities data and map classifications need migration rather than automatically instantiating regional economies.
- Commands and UI previews use shared sim validation.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, or the phase's plan.
- A unit cannot occupy multiple jobs, builder slots, farm slots or training places simultaneously.
- Farm loss/retaking does not delete farmers or automatically destroy fields.
- Condition costs are continuous and national housing cohesion is population weighted.
- City construction requires actual work and the national start allowance.
- Workshops produce equipment without duplicate people or job income.

## Out of scope

- The yishuvim part of D-B4: Phase 4.
- Kit density on the map by housing condition (Q-UI-1): left out of this phase (decided 15 October 2026 [S3]).
- No text in this phase names abandoned Arab housing; the panel and the absorb card describe housing in general terms, and any later text goes through D-F1 review (Q-SENS-1, owner, 15 October 2026 [S3]).
- Upkeep and income (D-C6): the owner chose option 3 on 18 October 2026 (#884). Cities, permanent housing, industry,
  transit camps, military camps and Hakhsharot each have IL 0m annual upkeep in the MVP; the values are labelled design
  values, not historical claims. Step 7 pays a kind only when its configured rate is non-zero (R-MONEY-002). The MVP
  keeps the per-head tax and spending alongside job income at the accepted calibrated values. Phase 5 owns unit upkeep.
Individual workforce screens per kibbutz, enemy resident workforce, mothballing, non-housing deterioration and automatically granting a city economy on founding completion.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 Phase 3 plan, docs/superpowers/plans/2026-10-15-gdd-phase-3.md; GDD v0.1 impact analysis and phased plan, section 5 group D, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] The owner's answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15
- [S4] CBS Statistical Abstract of Israel No. 14 (1963), Table 11, pp. 30-31 (populations) and No. 1 (1949/50), Table 9, p. 26 (working shares), as recorded in #569's research, https://github.com/eyalgolan/statecraft-israel/pull/569, checked 2026-10-15
- [S5] English Wikipedia, "Carmel Winery": the first winery is in Rishon LeZion and its first vineyards were planted near the town in the 1880s, https://en.wikipedia.org/wiki/Carmel_Winery, checked 2026-10-15
- [S6] JTA, "Israel Marks 'Ingathering of Exiles Day'; Arrival of Second Million Jews Anticipated", 20 December 1949, https://www.jta.org/archive/israel-marks-ingathering-of-exiles-day-arrival-of-second-million-jews-anticipated, checked 2026-10-15
- [S7] The owner's decisions recorded on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183538544, checked 2026-10-15
- [S8] CBS Statistical Abstract of Israel No. 3 (1952), the settlements list, pp. 133 and 144, https://www.cbs.gov.il/he/publications/DocLib/shnaton_saruk/shnaton1952_num3.pdf, checked 2026-10-15
- [S9] Rosh Pinna local council, "The history of Rosh Pinna", http://www.rosh-pina.co.il/%D7%94%D7%99%D7%A1%D7%98%D7%95%D7%A8%D7%99%D7%94, checked 2026-10-15
- [S10] Village Statistics, April 1945 (Government of Palestine), Safad Sub-District, serial 66, p. 10, https://users.cecs.anu.edu.au/~bdm/yabber/census/VillageStatistics1945orig.pdf, checked 2026-10-15
- [S11] The owner's decision on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6190619484, checked 2026-10-15
- [S12] GDD v0.1 Phase 2 plan, docs/superpowers/plans/2026-10-14-gdd-phase-2.md, the Q-MONEY-2 row; and docs/gdd/cutover-resource-layer.md (merged by #580), "Out of scope": "Upkeep: none until the third phase", checked 2026-10-15
- [S13] MVP location scope, map priorities and separate location identities, docs/locations.md
- [S14] The winery's city: a dated 1948-51 source for its vineyards, research note merged by #823, research_notes/GDD phase 3/winery-vineyards-1948-51.md
- [S15] The owner's ruling on #737, 18 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/737#issuecomment-6249501060, checked 2026-10-18
- [S16] The owner's ruling on #737, 20 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/737#issuecomment-6296386086, checked 2026-10-20
- [S1] Regional/housing decisions, docs/GDD.md.
- [S2] Numerical catalogue, docs/gdd/balance.md.
- [S3] Population eligibility, docs/gdd/training.md.
- [S4] Retained availability/winery decisions, docs/superpowers/plans/2026-10-15-gdd-phase-3.md, historical Q-HOUSE-3 and Q-SPEC-1/2.

## Open questions

- Rosh Pinna's start figure stays open: it keeps the accepted Hebrew Wikipedia figure, about three hundred and fifty in May 1948, corroborated by the town council's own history (about one hundred and fifty absorbed during the war, reaching five hundred [S9]); the CBS list enters Rosh Pinna with no population (p. 144 [S8]), and the Village Statistics 1945 count of eight hundred and forty at the end of 1944 is logged as a conflict in the research epic [S10]. Zikhron Ya'akov is sourced: 1,935 on 8 November 1948 (CBS Statistical Abstract No. 3, the settlements list, p. 133 [S8]).
No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
