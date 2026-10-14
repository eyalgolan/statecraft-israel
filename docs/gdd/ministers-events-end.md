# Ministers, events and the end of the MVP

## Player story

As a player of the MVP, I want the game to run its events and my chosen ministers through to an end screen on 15 January 1952 [S2], so that a full game resolves into a score and its historical completion cards (GDD "MVP scope" and "Historical events on objective completion" [S1]).

## Rules

- The MVP runs 44 turns (monthly), from 15 May 1948 to 15 January 1952: D-B1 fixes the start and the monthly grid [S2], and the owner ended the MVP on 15 January 1952, keeping the 15th grid with no adjusted turn (D-E5, decided 15 October 2026 [S2]). The GDD text still says "approximately 48 turns" [S1]; the owner settled 44 turns [S2].
- Objective cards and scoring belong to the first phase (#379, `objectives-and-score.md`; D-B2, D-B6 [S2]) and are not restated here. This section covers only what the final phase adds: every card complete, the end screen (D-H4, open), defeat (D-B5, decided 15 October 2026 [S2]), the events (D-H1, D-H2, open) and the ministers (D-H3, open) [S2].
- Defeat: not all objectives are required to finish. The game is lost when Money and Food are exhausted for several turns in a row or on a war loss, and coalition collapse is off in the MVP (D-B5, decided 15 October 2026 [S2]). The defeat is built in this phase, with the counter set then (`Q-GOV-2`, decided 15 October 2026 [S4]). A war loss is enemy units holding the hex of one of the player's cities at turn end (`Q-WAR-5`, decided 15 October 2026 [S4]); what "Money exhausted" means now that borrowing is allowed stays open.
- The war-survival objective completes on the last armistice event, the Syria-Israel agreement of 20 July 1949 (D-B2 [S2]; [S3]). The four signings were Egypt on 24 February 1949, Lebanon on 23 March 1949, Jordan on 3 April 1949 and Syria on 20 July 1949 [S3].
- The existing cabinet and Attention stay (D-C5 [S2]). Minister roles, candidates and effects are D-H3, open [S2].

## Data touched

- `objectives.yaml` (new, Phase 1): completion rules and card references.
- `events/`: the MVP's event set (#135 research).
- `content/governments.yaml`, `offices.yaml`, `people.yaml`: minister candidates.
- The end screen and score display (Unity).

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, the plan, or a sourced historical date.

## Out of scope

- Event frequency, windows and caps (D-H1), event effect targets (D-H2) and minister roles and effects (D-H3): all open.
- The objectives panel UI and the score's presentation in Phase 1.
- The exact completion conditions of each objective (GDD "MVP completion rules" [S1]): open.
- Objective 1's bonus for capturing enemy soldiers lands at the prisoners' return (the plan, section 4.3 [S2]): Phase 5/6.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] The 1949 armistice layer, content/map/armistice_hexes_1949.yaml (signing dates per agreement)
- [S4] The owner's phase answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15

## Open questions

- Minister roles, candidates and effects (D-H3); event frequency and caps (D-H1); event effects on units, cities and projects (D-H2).
- Bonus amounts and the timing formula's remaining parts (GDD "MVP completion rules" [S1]).
- What "Money exhausted" means for defeat now that borrowing is allowed (D-B5 follow-up [S4]).
- The GDD's "approximately 48 turns" [S1] against the decided 44 turns [S2]: a one-line GDD fix for the owner.
