# Corridors: the Tel Aviv-Haifa railway and roads

## Player story

As a player of the MVP, I want my pioneer units to lay the Tel Aviv-Haifa railway and the road corridors segment by segment over several turns, so that the new yishuvim are connected and the infrastructure objective's railway part can be completed (GDD "MVP scope" [S1]; plan Phase 4 [S2]).

## Rules

- A corridor is a project with per-segment progress, drawn by `RouteLayer` from the sim's progress, in a new `corridors.yaml` (the plan [S2]).
- The MVP's decided corridors are the Tel Aviv-Haifa railway (GDD [S1]) and the Burma Road (G1 [S5]). Any further road is the owner's decision D-D7, open (the plan recommends two to three roads [S2]).
- The location roster adds required map anchors, including Hulda, Latrun, Sha'ar HaGai and Jerusalem for the Jerusalem connection. Optional authoring at the Rosh HaAyin immigrant camp does not make a required route waypoint optional, and naming a front or regional hub adds no automatic road project [S6].
- Location placement and corridor geometry are separate authoring data. Moving a named endpoint preserves its identity, reports affected segment/site bindings and requires explicit dependency resolution without silently rewriting a sourced route [S7].
- Corridor work is multi-turn, with one project per pioneer unit at a time (D-D6 [S2]). Segment counts, costs and durations are balance values the owner sets; until then they are placeholders in the dev and test overlay, never in `content/` [S2].
- Completing the Tel Aviv-Haifa railway completes the railway part of the third objective, "Establish key infrastructure" (GDD "MVP scope" [S1]). The completion card's reference date is the owner's open decision D-B3 [S2]; one option is the Haifa-Tel Aviv passenger service that opened on 20 September 1949 [S3].
- The Burma Road corridor to Jerusalem is the Burma Road part of the third objective: the owner decided on 14 October 2026 that it is in the MVP (G1 [S5], GDD "MVP scope" [S1]). Its completion card's reference date is the opening of the Road of Valour, which replaced the Burma Road track, on 7 December 1948 [S4].
- One military camp and one Hakhshara exist at the start, and pioneers build further camps (D-D5 [S2]); building a camp is one of the GDD's pioneer actions (GDD "Pioneer work" [S1]).

## Data touched

- `corridors.yaml` (new): corridor and segment definitions with progress.
- `Projects.cs` (new, Phase 1) and the project's save lines: corridor progress.
- `Corridors.cs` (new) and `tools/routes_1948.py` (extended): the Phase 4 corridor model and route data (the plan, Phase 4 Touches [S2]).
- `RouteLayer` (Unity): draws corridor progress.
- `sites.yaml` (new, Phase 1): corridor endpoints.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, the plan, or a sourced historical date.

## Out of scope

- The corridor list itself beyond the Burma Road (D-D7), segment costs and durations, and road and rail effects on movement (D-G2).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] Haboker, 14 September 1949: the Tel Aviv-Haifa line opens to passengers on the 20th of the month, "next Tuesday", https://www.nli.org.il/he/newspapers/hbkr/1949/09/14/01/article/70 (OCR), checked 2026-10-14; confirmed by Maariv, 22 September 1949, a report from the line's first days, https://www.nli.org.il/he/newspapers/mar/1949/09/22/01/article/21 (OCR), checked 2026-10-14
- [S4] Hayom (Jerusalem), 8 December 1948: the Road of Valour was opened "yesterday", 7 December 1948, https://www.nli.org.il/he/newspapers/hayomjlm/1948/12/08/01/article/2 (OCR), checked 2026-10-14
- [S5] Owner decisions, G1 (the Burma Road in the MVP, 14 October 2026), docs/oss/DECISIONS.md
- [S6] MVP map priorities and transport anchors, docs/locations.md
- [S7] Internal map-authoring requirements, docs/gdd/map-editor.md; route geometry and bake pipeline, docs/routes.md

## Open questions

- Decided (owner, 17 Oct 2026, #782): the corridor list is the 1949 Haifa to Tel Aviv link, the Burma Road, and the Tel Aviv to Netanya coastal road (D-D7).
- Decided (owner, 17 Oct 2026, #782): the railway is the 1949 Haifa to Tel Aviv link: the Rosh HaAyin to Tel Aviv North link along the Yarkon and the bypass west of Tulkarm. If Task 2.0 finds no source for the bypass's alignment, the corridor is the Yarkon link alone. The card's reference date is 20 September 1949, worded "Haifa to Tel Aviv rail service restored" [S3] (D-B3).
- Decided (owner, 17 Oct 2026, #782): one segment between each pair of consecutive sourced waypoints. A road segment costs IL 2m and takes 6 turns; a rail segment costs IL 4m and takes 6 turns. The numbers are labelled design values (D-D7).
- Decided (owner, 15 Oct 2026, "Take all recommendations", #375; Phase 5 plan Q-MOVE-2): "Roads and rail cheaper", and "Only the corridors Phase 4 builds count" (D-G2). Phase 4 Q-COR-7 (owner, 17 Oct 2026, #782) adds that a built road or railway counts for movement in Phase 5.
