# Corridors: the Tel Aviv-Haifa railway and roads

## Player story

As a player of the MVP, I want my pioneer units to lay the Tel Aviv-Haifa railway and the road corridors segment by segment over several turns, so that the new yishuvim are connected and the infrastructure objective's railway half can be completed (GDD "MVP scope" [S1]; plan Phase 4 [S2]).

## Rules

- A corridor is a project with per-segment progress, drawn by `RouteLayer` from the sim's progress, in a new `corridors.yaml` (the plan [S2]).
- The MVP's corridors are the Tel Aviv-Haifa railway and two to three roads (unitless) (the plan [S2]). The corridor list is the owner's decision D-D7 [S2].
- Corridor work is multi-turn, with one project per pioneer unit at a time (D-D6 [S2]). Segment counts, costs and durations are balance values the owner sets; until then they are placeholders in the dev and test overlay, never in `content/` [S2].
- Completing the Tel Aviv-Haifa railway completes the railway half of objective 3 (unitless), "Establish key infrastructure" (GDD "MVP scope" [S1]). The completion card's reference date is the opening of passenger service on 20 September 1949 [S3].
- One military camp and one Hakhshara exist at the start, and pioneers build further camps (D-D5 [S2]); building a camp is one of the six pioneer actions (unitless) (the plan [S2]).

## Data touched

- `corridors.yaml` (new): corridor and segment definitions with progress.
- `Projects.cs` and the project's save lines: corridor progress.
- `RouteLayer` (Unity): draws corridor progress.
- `sites.yaml`: corridor endpoints.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, the plan, or a sourced historical date.

## Out of scope

- The corridor list itself (D-D7), segment costs and durations, and road effects on unit movement (D-G2).
- Whether the Road of Valour (Burma Road) is an MVP corridor: scope question #531.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] Haboker, 14 September 1949: the Tel Aviv-Haifa line opens to passengers on the 20th of the month, "next Tuesday", https://www.nli.org.il/he/newspapers/hbkr/1949/09/14/01/article/70 (OCR), checked 2026-10-14
- [S4] Hayom (Jerusalem), 8 December 1948: the Road of Valour was opened "yesterday", 7 December 1948, https://www.nli.org.il/he/newspapers/hayomjlm/1948/12/08/01/article/2 (OCR), checked 2026-10-14

## Open questions

- The corridor list: which 2 to 3 roads (unitless) join the railway (D-D7). The Road of Valour, opened 7 December 1948 [S4], is a candidate; its place in the MVP is #531.
- Segment counts, costs and durations: owner balance values.
- Whether roads affect unit movement (D-G2) or only settlement founding.
