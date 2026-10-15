# Cities and absorption

## Player story

As a player of the MVP, I want the twelve Jewish cities and towns to start at their real 1948 sizes and to absorb the arriving immigrants through housing, jobs and city projects, so that absorbing the immigration wave is the heart of the game (GDD "Cities and construction" and objective two [S1]; the Phase 3 plan [S2]).

## Rules

- The MVP's cities are the twelve Jewish `city` and `town` rows of `settlements_1948.yaml` (the plan [S2]); the six kibbutzim and moshavim are not cities in this phase (`Q-START-4`, decided 15 October 2026 [S3]).
- Each city starts at its census population of 8 November 1948, every field recorded with a status: sourced, derived, wikipedia-only or design (the plan, proposed R-CITY-006 [S2]; the CBS census table [S4]). The owner chose the census values plus defaults for the gaps ("Census values + defaults", the owner on #569, recorded in the plan [S2]), and the census-day count is used as the 15 May 1948 start value.
- People become units at one thousand people per unit, rounding to nearest with a minimum of one unit for any city that has people, so Rosh Pinna starts with one (`Q-START-2`, decided 15 October 2026 [S3]).
- The five cities with a sourced working share start with derived employment [S4]; every other city starts from the national working share of 43.9 percent, its job slots equal its start employed count, and its housing capacity equals its start population (`Q-START-1`, decided 15 October 2026 [S3]).
- Arrivals spread over the cities in proportion to census population, by cumulative largest-remainder apportionment with an ordinal tie-break (`Q-ARRV-1`, decided 15 October 2026 [S3]). The cities take arrivals only from the census day, while the national head count takes every arrival from 15 May 1948, so no immigrant is counted twice (`Q-ARRV-3`, decided 15 October 2026 [S3]).
- `GameState.Population` stays the one record of the nation's people; the cities are the part placed in towns, and an invariant test checks they never exceed it (`Q-ARRV-2`, decided 15 October 2026 [S3]).
- Housing has per-city capacity and an average condition on a zero-to-one scale (`Q-START-3`). One capacity point houses one unit, and every refugee becomes a civilian on the next housing step (`Q-HOUSE-1`, decided 15 October 2026 [S3]). There is no hard cap: people above capacity are the city's overflow, and overflow lowers condition and Social cohesion (`Q-HOUSE-1` and `Q-HOUSE-2`, decided 15 October 2026 [S3]; D-D1, D-D2). The start condition and the size of the overflow effect are labelled design values taken from the plan's PR zero table (the plan's table merged by #604).
- Two housing types: permanent housing adds capacity and condition from the game's start; the transit camp is cheap, adds capacity and lowers condition, and keeps the ma'abarot policy gate in 1950 (`Q-HOUSE-3`, decided 15 October 2026 [S3]). Residents are assigned automatically, and people in training and recruits count against their city's housing while halutz units do not (`Q-HOUSE-4` and `Q-HOUSE-5`, decided 15 October 2026 [S3]).
- City projects (housing and industry) are paid in full at the start and borrowed if the Treasury is short, need no workers, take a free land hex within one hex of the settlement hex that holds no other placement, and at most two start per turn nationally (`Q-PROJ-1` to `Q-PROJ-4`, decided 15 October 2026 [S3]). Starting a project costs Attention and cancelling costs half as much, both labelled design values (`Q-PROJ-5`, decided 15 October 2026 [S3]). Their costs, times and capacities are labelled design values taken from the plan's PR zero table (the plan's table merged by #604; `Q-PROJ-1`).
- The craftsman trains in a city with a completed industry project, fills an ordinary job slot and earns a multiple of the job income (`Q-CRAFT-1` and `Q-CRAFT-2`, decided 15 October 2026 [S3]; D-D4). The income multiple, training time and training cost are labelled design values taken from the plan's PR zero table (the plan's table merged by #604; `Q-CRAFT-2` and `Q-CRAFT-3`). Free job slots fill automatically at no Attention cost until the player fixes a city's number with `AssignWorkers` (`Q-JOBS-1`, decided 15 October 2026 [S3]).
- The one specialised industry is the winery, in Rishon LeZion, subject to a dated 1948 to 1951 source for the town's vineyards (`Task 6.0`) (`Q-SPEC-1`, decided 15 October 2026 [S3]; Carmel Winery's first winery is in Rishon LeZion [S5]). The winery's job slots are a labelled design value taken from the plan's PR zero table (the plan's table merged by #604; `Q-SPEC-2`).
- The "absorb immigrants" objective completes when, over all city residents, no refugees wait for housing, overflow stays within its bound, and employed civilians reach their share (`Q-ABS-2`, decided 15 October 2026 [S3]; D-B4; the two shares are labelled design values taken from the plan's PR zero table, merged by #604). Its milestone is the Jewish population passing one million, marked on 20 December 1949 (`Q-ABS-1`, the owner's pick, 15 October 2026 [S3]; JTA [S6]). With that milestone the card shows no photograph unless the owner approves a generic arrival one (`Q-ABS-3`, decided 15 October 2026 [S3]).
- The on-screen Hebrew words: craftsman בעל מלאכה, housing condition מצב הדיור, housing project מיזם דיור, industry תעשייה, winery יקב (`Q-UI-2`, all five approved, decided 15 October 2026 [S11]).

## Data touched

- `content/cities.yaml`: the twelve cities' start values, each field with a status (the plan, PR 1 [S2]).
- `content/rules.yaml` (`mvp:`): the arrival split, overflow and condition values, project costs and turns, craftsman values and Attention costs, all labelled design values (the plan [S2]).
- `Cities.cs`, `PopulationSystem.cs`, `Projects.cs` (new, the first phase): arrivals, housing and projects (the plan [S2]).
- `objectives.yaml` (new, the first phase): the absorb objective's completion rule and card (the plan [S2]).
- The city panel (Unity) at twelve cities (the plan [S2]).

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, or the phase's plan.

## Out of scope

- The yishuvim part of D-B4: Phase 4.
- Kit density on the map by housing condition (Q-UI-1): left out of this phase (decided 15 October 2026 [S3]).
- No text in this phase names abandoned Arab housing; the panel and the absorb card describe housing in general terms, and any later text goes through D-F1 review (Q-SENS-1, owner, 15 October 2026 [S3]).
- Upkeep (D-C6): Q-MONEY-2 adds no upkeep "until Phase 3 has cities to charge" (the Phase 2 plan's Q-MONEY-2 row [S12]), and `docs/gdd/cutover-resource-layer.md` says the values arrive in Phase 3, but no Phase 3 PR owns them yet; recorded for the owner to assign. Income is owned: Q-MONEY-2 keeps the legacy per-head tax and spending until Phase 3, with job income on top, and `income_per_job_per_year` stays a labelled design value [S12].

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

## Open questions

- Rosh Pinna's start figure stays open: it keeps the accepted Hebrew Wikipedia figure, about three hundred and fifty in May 1948, corroborated by the town council's own history (about one hundred and fifty absorbed during the war, reaching five hundred [S9]); the CBS list enters Rosh Pinna with no population (p. 144 [S8]), and the Village Statistics 1945 count of eight hundred and forty at the end of 1944 is logged as a conflict in the research epic [S10]. Zikhron Ya'akov is sourced: 1,935 on 8 November 1948 (CBS Statistical Abstract No. 3, the settlements list, p. 133 [S8]).
