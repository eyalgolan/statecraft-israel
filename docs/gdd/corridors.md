# Corridors: the Tel Aviv-Haifa railway and roads

## Player story

As a player of the MVP, I want my pioneer units to lay the Tel Aviv-Haifa railway and the road corridors segment by segment over several turns, so that the new yishuvim are connected and the infrastructure objective's railway part can be completed (GDD "MVP scope" [S1]; plan Phase 4 [S2]).

## Rules

- A corridor is a project with per-segment progress, drawn by `RouteLayer` from the sim's progress, in a new `corridors.yaml` (the plan [S2]).
- The MVP builds the Haifa-Tel Aviv rail link (GDD [S1]; `Q-RAIL-1` [S8]) and the Burma Road (G1 [S5]). The rail link is the 1949 Yarkon link alone, from the Petah Tikva branch to Tel Aviv North: no source read gives the line of the bypass west of Tulkarm, so by the owner's rule it is left out [S8][S9]. The owner also chose the Tel Aviv-Netanya road (`Q-COR-3`, [S8]). The 1946 map does not show it and no source read gives its 1950 line, so it is drawn as straight draft lines between the five places *The Jerusalem Post* names, four segments [S10].
- The location roster adds required map anchors, including Hulda, Latrun, Sha'ar HaGai and Jerusalem for the Jerusalem connection. Optional authoring at the Rosh HaAyin immigrant camp does not make a required route waypoint optional, and naming a front or regional hub adds no automatic road project [S6].
- Location placement and corridor geometry are separate authoring data. Moving a named endpoint preserves its identity, reports affected segment/site bindings and requires explicit dependency resolution without silently rewriting a sourced route [S7].
- A corridor is split into one segment between each pair of consecutive sourced waypoints of its route, so the Burma Road and the Tel Aviv-Netanya road have four segments each and the rail link one (`Q-COR-2`, [S8]). A segment's hexes are the hexes its stretch of the traced line crosses (`tools/corridor_hexes.py`). On screen a segment is the corridor's name and its number, never a waypoint's name (`Q-SENS-1`, [S8]).
- `PioneerWork(unit, build_road | build_rail, corridor:N)` starts any unbuilt segment that no other unit holds, in any order, so several units can build one corridor at once; the unit works from where it stands (`Q-COR-4`, `Q-COR-5` [S8]). One project per pioneer unit at a time (D-D6 [S2]). The kind's cost is paid in full at the start and borrowed if the Treasury is short; `StopWork` loses the progress and returns no Money (`Q-COR-6`, [S8]). A corridor is complete when every segment is built (R-COR-001 to R-COR-003).
- The Money and turns per segment are labelled design values in `rules.yaml` (`Q-COR-6`, [S8]): a road segment IL 2m and 6 turns, a rail segment IL 4m and 6 turns.
- A built corridor needs no upkeep, rolling stock or staff and yields no Money or Food in the MVP (`Q-COR-7`, [S8]).
- Completing the Tel Aviv-Haifa railway completes the railway part of the third objective, "Establish key infrastructure" (GDD "MVP scope" [S1]). The completion card's reference date is the owner's open decision D-B3 [S2]; one option is the Haifa-Tel Aviv passenger service that opened on 20 September 1949 [S3].
- The Burma Road corridor to Jerusalem is the Burma Road part of the third objective: the owner decided on 14 October 2026 that it is in the MVP (G1 [S5], GDD "MVP scope" [S1]). Its completion card's reference date is the opening of the Road of Valour, which replaced the Burma Road track, on 7 December 1948 [S4].
- Nir Am's infrastructure-protection rule is part of the third objective: the Negev water line that began there must stay in Israeli hands until the armistice with Egypt on 24 February 1949. Its card tells the pipeline's 1947–48 history from sources, with `checked:` dates and a person's fact check. Its points are a labelled design value for the balance pass; no value is selected here. **Then decided (owner, 18 Oct 2026, "Part of objective 3"; [#1006](https://github.com/eyalgolan/statecraft-israel/issues/1006#issuecomment-6254333583))** [S11]. (unitless)
- One military camp and one Hakhshara exist at the start, and pioneers build further camps (D-D5 [S2]); building a camp is one of the GDD's pioneer actions (GDD "Pioneer work" [S1]).

## Data touched

- `content/corridors.yaml` (new, #786): each corridor's kind, names, map route and segments, with sources; the state holds each segment's progress (`corr{}` in the hash, only when a segment is started).
- `Corridors.cs` (new) and `Projects.cs`: the orders, step 5's progress after the sites, and `StopWork`.
- `content/map/routes_1948.yaml`: the new `yarkon_rail_link` route and the `tlv_netanya_road` route (`trace: straight`, drawn as straight lines between its waypoints), traced into `route_paths_1948.yaml` and baked into the `.tsv` and the route ribbons; `tools/corridor_hexes.py` derives the segment hexes from the traced line.
- `RouteLayer` (Unity): drawing by progress comes with Phase 4 PR 8; until then the new route draws by its `from` date, as every route does.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, the plan, or a sourced historical date.

## Out of scope

- Drawing corridors from progress (Phase 4 PR 8), the objective's railway and Burma Road parts and their cards (Phase 4 PR 7), and road and rail effects on movement (Phase 5, D-G2).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] Haboker, 14 September 1949: the Tel Aviv-Haifa line opens to passengers on the 20th of the month, "next Tuesday", https://www.nli.org.il/he/newspapers/hbkr/1949/09/14/01/article/70 (OCR), checked 2026-10-14; confirmed by Maariv, 22 September 1949, a report from the line's first days, https://www.nli.org.il/he/newspapers/mar/1949/09/22/01/article/21 (OCR), checked 2026-10-14
- [S4] Hayom (Jerusalem), 8 December 1948: the Road of Valour was opened "yesterday", 7 December 1948, https://www.nli.org.il/he/newspapers/hayomjlm/1948/12/08/01/article/2 (OCR), checked 2026-10-14
- [S5] Owner decisions, G1 (the Burma Road in the MVP, 14 October 2026), docs/oss/DECISIONS.md
- [S6] MVP map priorities and transport anchors, docs/locations.md
- [S7] Internal map-authoring requirements, docs/gdd/map-editor.md; route geometry and bake pipeline, docs/routes.md
- [S8] Owner decision of 17 October 2026, "take all of the plan's recommendations" (Phase 4 PR 0), https://github.com/eyalgolan/statecraft-israel/issues/782 ; the recommendations are in docs/superpowers/plans/2026-10-15-gdd-phase-4.md, PR 0
- [S9] Research note, the 1949 rail link and the Tulkarm bypass, research_notes/gdd-phase-4-sourcing.md, section 2
- [S10] Owner decision of 18 October 2026, Q-COR-3 (b): "the Tel Aviv–Netanya line is drawn as straight draft lines between the five sourced places", https://github.com/eyalgolan/statecraft-israel/issues/786#issuecomment-6249500143 , checked 2026-10-18; its waypoints, research_notes/gdd-phase-4-sourcing.md, section 3
- [S11] [Owner decision on Nir Am's objective](https://github.com/eyalgolan/statecraft-israel/issues/1006#issuecomment-6254333583), 18 October 2026, checked 2026-10-18.

## Open questions

- Decided (owner, 17 Oct 2026, #782): the corridor list is the 1949 Haifa to Tel Aviv link, the Burma Road, and the Tel Aviv to Netanya coastal road (D-D7).
- Decided (owner, 17 Oct 2026, #782): the railway is the 1949 Haifa to Tel Aviv link: the Rosh HaAyin to Tel Aviv North link along the Yarkon and the bypass west of Tulkarm. If Task 2.0 finds no source for the bypass's alignment, the corridor is the Yarkon link alone. The card's reference date is 20 September 1949, worded "Haifa to Tel Aviv rail service restored" [S3] (D-B3).
- Decided (owner, 17 Oct 2026, #782): one segment between each pair of consecutive sourced waypoints. A road segment costs IL 2m and takes 6 turns; a rail segment costs IL 4m and takes 6 turns. The numbers are labelled design values (D-D7).
- Decided (owner, 15 Oct 2026, "Take all recommendations", #375; Phase 5 plan Q-MOVE-2): "Roads and rail cheaper", and "Only the corridors Phase 4 builds count" (D-G2). Phase 4 Q-COR-7 (owner, 17 Oct 2026, #782) adds that a built road or railway counts for movement in Phase 5.
- Decided (owner, 18 Oct 2026, #786, Q-COR-3 (b)): the Tel Aviv-Netanya road is drawn as straight draft lines between the five sourced places, with no source for its 1950 line; its segments' hexes are derived from those lines [S10].
