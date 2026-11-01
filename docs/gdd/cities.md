# Cities and absorption

## Player story

As a player of the MVP, I want the twelve Jewish cities and towns to start at their real 1948 sizes and to absorb the arriving immigrants through housing, jobs and city projects, so that absorbing the immigration wave is the heart of the game (GDD "Cities and construction" and objective two [S1]; the Phase 3 plan [S2]).
I assign population units to the retained city economy while the map explains which fields and facilities make it possible.

## Rules

- This phase's managed cities are the twelve Jewish `city` and `town` rows of `settlements_1948.yaml` (the plan [S2]); the six kibbutzim and moshavim are not cities in this phase (`Q-START-4`, decided 15 October 2026 [S3]). The broader map-production roster is separate [S13]: a must-have marker does not automatically become a managed city, and optional authoring does not remove an existing city.
- Tel Aviv and Jaffa retain distinct map identities while sharing the approved Tel Aviv-Yafo city economy. Jaffa does not receive a second population or immigrant-allocation pool (the plan's city-start decision [S2]; the location roster [S13]).
- Each city starts at its census population of 8 November 1948, every field recorded with a status: sourced, derived, wikipedia-only or design (the plan, proposed R-CITY-006 [S2]; the CBS census table [S4]). The owner chose the census values plus defaults for the gaps ("Census values + defaults", the owner on #569, recorded in the plan [S2]), and the census-day count is used as the 15 May 1948 start value.
- People become units at one thousand people per unit, rounding to nearest with a minimum of one unit for any city that has people, so Rosh Pinna starts with one (`Q-START-2`, decided 15 October 2026 [S3]).

*Amir's #1668 proposed 10,000 people per population unit instead of the Q-START-2 scale. The owner's 30 October 2026 rulings do not decide this ([conflict decision](../GDD.md#30-october-2026-conflict-decisions)); the `gdd-0.3` catalogue currently reads 10,000 (`R-POP-001`, [rules](../rules/mvp.md#population-identity-foundation-gdd-03-only)). On 1 November 2026 the owner kept main's census cities at 1,000 people a unit for the Historical MVP (`R-CITY-007`), and Amir's scale is for Open mode (D2).* (unitless) [S1] [S21]
- The five cities with a sourced working share start with derived employment [S4]; every other city starts from the national working share of 43.9 percent, its job slots equal its start employed count, and its housing capacity equals its start population (`Q-START-1`, decided 15 October 2026 [S3]).
- Arrivals spread over the cities in proportion to census population, by cumulative largest-remainder apportionment with an ordinal tie-break (`Q-ARRV-1`, decided 15 October 2026 [S3]). The units are handed out one at a time, each to the city with the largest remainder, so a growing total never takes a unit back from a city (R-CITY-003). The cities take arrivals only from the census day, while the national head count takes every arrival from 15 May 1948, so no immigrant is counted twice (`Q-ARRV-3`, decided 15 October 2026 [S3]).
- `GameState.Population` stays the one record of the nation's people; the cities are the part placed in towns, and an invariant test checks they never exceed it (`Q-ARRV-2`, decided 15 October 2026 [S3]).
- Housing has per-city capacity and an average condition on a zero-to-one scale (`Q-START-3`). One capacity point houses one unit, and every refugee becomes a civilian on the next housing step (`Q-HOUSE-1`, decided 15 October 2026 [S3]). There is no hard cap: people above capacity are the city's overflow, and overflow lowers condition and Social cohesion (`Q-HOUSE-1` and `Q-HOUSE-2`, decided 15 October 2026 [S3]; D-D1, D-D2). The start condition and the size of the overflow effect are labelled design values taken from the plan's PR zero table (the plan's table merged by #604).

The later owner decisions recorded in [S7] do not replace this retained per-city housing rule.

*Amir's #1668 proposed six reception turns before refugees can do civilian work instead of conversion on the next housing step; the owner's 30 October 2026 conflict review did not rule on this ([conflict decision](../GDD.md#30-october-2026-conflict-decisions)). On 1 November 2026 the owner kept `R-CITY-004` (`docs/rules/mvp.md:105`) with "Keep R-CITY-004 (Recommended)", so the reception turns are post-MVP; main has an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`).* [S1] [S21]
- Two housing types: permanent housing adds capacity and condition from the game's start; the transit camp is cheap, adds capacity and lowers condition, and keeps the ma'abarot policy gate in 1950 (`Q-HOUSE-3`, decided 15 October 2026 [S3]). Residents are assigned automatically, and people in training and recruits count against their city's housing while halutz units do not (`Q-HOUSE-4` and `Q-HOUSE-5`, decided 15 October 2026 [S3]).
- City projects (housing and industry) are paid in full at the start and borrowed if the Treasury is short, need no workers, take a free land hex within one hex of the settlement hex that holds no other placement, and at most two start per turn nationally (`Q-PROJ-1` to `Q-PROJ-4`, decided 15 October 2026 [S3]). Starting a project costs Attention and cancelling costs half as much, both labelled design values (`Q-PROJ-5`, decided 15 October 2026 [S3]). Their costs, times and capacities are labelled design values taken from the plan's PR zero table (the plan's table merged by #604; `Q-PROJ-1`).

*Considered and declined (owner, 30 Oct 2026; Upkeep conflict, option A; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed potentially non-zero completed-facility upkeep. For Amir's assigned builders, one national city/region project start per turn and no borrowing, the review did not rule; `R-PROJ-001` and `R-MONEY-003` (`docs/rules/mvp.md:145` and `:399`) stay operative.* (unitless) [S1]
- The craftsman trains in a city with a completed industry project, fills an ordinary job slot and earns a multiple of the job income (`Q-CRAFT-1` and `Q-CRAFT-2`, decided 15 October 2026 [S3]; D-D4). The income multiple, training time and training cost are labelled design values taken from the plan's PR zero table (the plan's table merged by #604; `Q-CRAFT-2` and `Q-CRAFT-3`). Free job slots fill automatically at no Attention cost until the player fixes a city's number with `AssignWorkers` (`Q-JOBS-1`, decided 15 October 2026 [S3]).
- The one specialised industry is the winery, in Rishon LeZion, its vineyards flagged `to_verify`: `Task 6.0` found no dated 1948 to 1951 source for any town's vineyards [S14], and the owner ruled the city on #737 (20 October 2026, as GDD v0.2 and the map roster say [S16], [S13]; Carmel Winery's first winery is in Rishon LeZion [S5]). This replaces the 18 October default of Zikhron Ya'akov [S15], which has no winery. The winery's job slots are a labelled design value taken from the plan's PR zero table (the plan's table merged by #604; `Q-SPEC-2`).
- The "absorb immigrants" objective completes when, over all city residents, no refugees wait for housing, overflow stays within its bound, and employed civilians reach their share (`Q-ABS-2`, decided 15 October 2026 [S3]; D-B4; the two shares are labelled design values taken from the plan's PR zero table, merged by #604). Its milestone is the Jewish population passing one million, marked on 20 December 1949 (`Q-ABS-1`, the owner's pick, 15 October 2026 [S3]; JTA [S6]). With that milestone the card shows no photograph unless the owner approves a generic arrival one (`Q-ABS-3`, decided 15 October 2026 [S3]).
- The on-screen Hebrew words: craftsman בעל מלאכה, housing condition מצב הדיור, housing project מיזם דיור, industry תעשייה, winery יקב (`Q-UI-2`, all five approved, decided 15 October 2026 [S11]).
### Regional management

**Amir's proposal, declined by the owner on 30 October 2026 [S17]:** each player region would pool housing, farms and Industry/workshop jobs through an authored capital. The retained per-city capacity rule remains authoritative.

The proposed regional farm-assignment and field-slot rules are part of that declined proposal and are non-operative; the farmer tier rates agree with main's `gdd-0.3` rule (`R-VET-002`). Existing field and site rules remain authoritative.

### Assignments and income

A unit has one active assignment. **Amir's proposal [S1]:** eligible civilians would take proposed generic builder slots and Industry would use proposed staffed tier rates. The owner's 30 October 2026 conflict review did not rule on this; `R-PROJ-001` and `R-CITY-002` (`docs/rules/mvp.md:145`, `docs/rules/mvp.md:95`) stay operative. The retained qualification-reassignment rule still permits a qualified civilian to return to a role without retraining [S19]. A worker works in their own city, and farmers live in the region they farm; employed means civilians and craftsmen in job slots over civilians plus craftsmen, and the `work_lifts_cohesion` link stays (owner, 1 November 2026, Q27 and Q35) [S21].

Farm work, Industry, building, teaching, courses and active service cannot run simultaneously for the same unit. Reassignment releases old capacity atomically. Invalid capacity/eligibility refuses before Money or state changes.

### Housing and condition

**Amir's proposal, declined by the owner on 30 October 2026 (Regional pooling and Upkeep conflicts, option A) [S1]:** the regional-capacity model, regional resident formula and proportional housing upkeep are non-operative. For housing-only shortfall effects, the owner's 30 October 2026 conflict review did not rule; borrowing under `R-MONEY-003` (`docs/rules/mvp.md:399`) stays operative (owner, 1 November 2026, D1) [S21]. Every city starts at the one housing condition 0.35 (`R-HOUSE-001`; owner, 1 November 2026, 27M-16: "0.35, your 15 Oct value (Recommended)") [S21] (unitless). The retained per-city housing, refugee-conversion and exhausted-resource defeat rules control.

### City projects and builders

**Amir's proposal [S1]:** city projects would require full upfront Money, assigned builders and one national start. The owner's 30 October 2026 conflict review did not rule on this; `R-PROJ-001` and `R-MONEY-003` (`docs/rules/mvp.md:145` and `:399`) stay operative, with borrowing, worker-free construction and two starts a turn; full upfront Money is post-MVP (owner, 1 November 2026, D1) [S21].

The completed-upkeep rule is non-operative (Upkeep conflict, option A). For the assigned-builder, paused-progress and one-start rules, the 30 October 2026 conflict review did not rule; `R-PROJ-001` to `R-PROJ-003` stay operative [S1].

The retained winery is in Rishon LeZion under the existing city-project ruling [S16][S20]; Amir's shared regional winery-job model was declined [S1]. The maabara retains its existing reviewed ma'abarot-policy availability gate in 1950 [S20].

The proposed worker pause/cancellation semantics are non-operative. Retained city projects need no workers and use their existing cancellation rule.

### Arms workshops

A workshop has its own worker slot. Each staffed turn advances it; no worker means pause. Completion adds spare Arms nationally. All tiers share cycle duration; expertise lowers Money cost. Pause or worker change retains progress; the next cycle uses the new tier. The workshop pays no simultaneous Industry income [S18]. Affordability follows the retained borrowing rule; Amir's full-upfront, no-borrowing workshop-input rule is non-operative [S1] and post-MVP (D1). A workshop reserves its cycle at the monthly production step and repeats automatically each month while staffed ("Automatic monthly (Recommended)"); any free civilian may work it at their cost tier; on destructive cancellation, destruction or transfer its cycle progress is lost and the Money paid is not refunded; and workshop work by a novice changes no qualification (owner, 1 November 2026, 27M-11 to 27M-14 and Q55) [S21]. (unitless)

### Initial authoring

**Amir's proposal, declined by the owner on 30 October 2026 [S17]:** starting inventory would bind population and facilities to authored friendly regions and distribute prepared fields across multiple regions. Census records remain evidence; do not round each small location into another whole game unit. The starting fields are the authored list from the owner-approved per-region split of 27 October 2026, with no numeric threshold, and only an Israeli-held hex can be farmed; a retaken field resumes through the city's job default, and farming counts whole months from the month's snapshot (owner, 1 November 2026, Q36, Q37, 27M-1 and 27M-2) [S21]. Southern Coast and Negev each get one opening housing place for their farmer, once a sourced research note confirms a Jewish settlement in each region on 15 May 1948 (D5) [S21]. (unitless)

## Data touched

- `content/cities.yaml`: the twelve cities' start values, each field with a status (the plan, PR 1 [S2]).
- `content/rules.yaml` (`mvp:`): the arrival split, overflow and condition values, project costs and turns, craftsman values and Attention costs, all labelled design values (the plan [S2]).
- `Cities.cs`, `PopulationSystem.cs`, `Projects.cs` (new, the first phase): arrivals, housing and projects (the plan [S2]).
- `objectives.yaml` (new, the first phase): the absorb objective's completion rule and card (the plan [S2]).
- The city panel (Unity) at twelve cities (the plan [S2]).
- **Amir's proposal, declined by the owner on 30 October 2026 [S1]:** add region/capital membership and regional housing, facilities, jobs and per-unit assignment references. These data changes are non-operative.
- Commands and UI previews use shared sim validation.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, or the phase's plan.
- A unit cannot occupy multiple retained jobs or training places simultaneously.
- **Amir's proposal, declined by the owner on 30 October 2026 (Regional pooling conflict, option A) [S1]:** regional farm-slot state and continuous regional condition costs would be required. These acceptance criteria are non-operative. For builder slots and one national construction start, the 30 October 2026 conflict review did not rule; `R-PROJ-001` stays operative.
- Workshops produce equipment without duplicate people or job income.

## Out of scope

- The yishuvim part of D-B4: Phase 4.
- Kit density on the map by housing condition (Q-UI-1): left out of this phase (decided 15 October 2026 [S3]).
- No text in this phase names abandoned Arab housing; the panel and the absorb card describe housing in general terms, and any later text goes through D-F1 review (Q-SENS-1, owner, 15 October 2026 [S3]).
- Upkeep and income (D-C6): the owner chose option 3 on 18 October 2026 (#884). Cities, permanent housing, industry,
  transit camps, military camps and Hakhsharot each have IL 0m annual upkeep in the MVP; the values are labelled design
  values, not historical claims. Step 7 pays a kind only when its configured rate is non-zero (R-MONEY-002). The MVP
  keeps the per-head tax and spending alongside job income at the accepted calibrated values. Phase 5 owns unit upkeep.
  The earlier Phase 2 boundary is recorded in [S12].
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
- [S17] Regional/housing decisions, docs/GDD.md.
- [S18] Numerical catalogue, docs/gdd/balance.md.
- [S19] Population eligibility, docs/gdd/training.md.
- [S20] Retained availability/winery decisions, docs/superpowers/plans/2026-10-15-gdd-phase-3.md, historical Q-HOUSE-3 and Q-SPEC-1/2.
- [S21] Owner's 1 November 2026 answers, [GDD 1 November 2026 answers](../GDD.md#1-november-2026-answers).

## Open questions

- Rosh Pinna's start figure stays open: it keeps the accepted Hebrew Wikipedia figure, about three hundred and fifty in May 1948, corroborated by the town council's own history (about one hundred and fifty absorbed during the war, reaching five hundred [S9]); the CBS list enters Rosh Pinna with no population (p. 144 [S8]), and the Village Statistics 1945 count of eight hundred and forty at the end of 1944 is logged as a conflict in the research epic [S10]. Zikhron Ya'akov is sourced: 1,935 on 8 November 1948 (CBS Statistical Abstract No. 3, the settlements list, p. 133 [S8]).
No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
