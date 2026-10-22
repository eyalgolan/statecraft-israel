# Ministers, events and the end of the MVP

## Player story

As a player of the MVP, I want the game to run its events and my chosen ministers through to an end screen on 15 January 1952 [S2], so that a full game resolves into a score and its historical completion cards (GDD "MVP scope" and "Historical events on objective completion" [S1]).

## Rules

- The MVP runs 44 turns (monthly), from 15 May 1948 to 15 January 1952: D-B1 fixes the start and the monthly grid [S2], and the owner ended the MVP on 15 January 1952, keeping the 15th grid with no adjusted turn (D-E5, decided 15 October 2026 [S2]). The GDD text still says "approximately 48 turns" [S1]; the owner settled 44 turns [S2].
- Objective cards and scoring belong to the first phase (#379, `objectives-and-score.md`; D-B2, D-B6 [S2]) and are not restated here. This section covers only what the final phase adds: every card complete, the end screen (D-H4, decided 17 October 2026 [S6] and built as R-END-005 and R-END-006, `docs/rules/mvp.md`), defeat (D-B5, decided 15 October 2026 [S2]), the events (D-H1 and D-H2, decided 17 October 2026 [S5]) and the ministers (D-H3, first slice decided 17 October and 18 October 2026 and built as R-MIN-001 to R-MIN-003; the rest open) [S2].
- Defeat: not all objectives are required to finish. The game is lost when Money and Food are exhausted for several turns in a row or on a war loss, and coalition collapse is off in the MVP (D-B5, decided 15 October 2026 [S2]). The defeat is built in this phase, with the counter set then (`Q-GOV-2`, decided 15 October 2026 [S4]). A war loss is enemy units holding the hex of one of the player's cities at turn end (`Q-WAR-5`, decided 15 October 2026 [S4]); "Money exhausted" is a debt at or above the debt ceiling (`Q-DEF-1`, owner, 17 October 2026, [#784](https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6232838937) [S5]; built as R-END-001, `docs/rules/mvp.md`).
- The war-survival objective completes on the last armistice event, the Syria-Israel agreement of 20 July 1949 (D-B2 [S2]; [S3]). The four signings were Egypt on 24 February 1949, Lebanon on 23 March 1949, Jordan on 3 April 1949 and Syria on 20 July 1949 [S3].
- The existing cabinet and Attention stay (D-C5 [S2]). Minister roles, candidates and effects are D-H3 [S2]: the first slice is decided and built - each office's competence scales the GDD systems in its remit (`Q-MIN-1` to `Q-MIN-3`, owner, 17 October and 18 October 2026; R-MIN-001 to R-MIN-003, `docs/rules/mvp.md`) - and the rest stays open.
- Events (D-H1): the owner took the recommendations for `Q-EVT-1` to `Q-EVT-4` on 17 October 2026 [S5]. Windows, hard anchors and a cap of three soft events a turn stay; an unanswered event is delegated to its office's holder; no effect is delayed; the card shows every effect of every option from the sim (`R-EVT-001` to `R-EVT-005`, `docs/rules/mvp.md`). An early armistice's place ahead of the other soft events comes with `front_quiet`, in the war-objective PR of the military phase (#752).
- Event effects (D-H2): the owner took the recommendations for `Q-HFX-1` to `Q-HFX-3` on 17 October 2026 [S5]. An option can grant units, free, at the military camp or a named city (a site once sites carry a date), and change a city's housing capacity or job slots; a sensitive option gains nothing from them; each is checked at load (`R-EFX-003` to `R-EFX-006`, `docs/rules/mvp.md`). Housing condition is part of R-EFX-004 and project unlock is R-EFX-008, built beside R-EFX-003, R-EFX-005 and R-EFX-006 (#959, #1002, #1072; #806 closed).

## Data touched

- `objectives.yaml` (new, Phase 1): completion rules and card references.
- `events/`: the MVP's event set (#135 research).
- `content/governments.yaml`, `offices.yaml`, `people.yaml`: minister candidates.
- The end screen and score display (Unity).

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, the plan, or a sourced historical date.

## Out of scope

- Minister roles and effects (D-H3) beyond the first slice (R-MIN-001 to R-MIN-003): open.
- The objectives panel UI and the score's presentation in Phase 1.
- The exact completion conditions of each objective (GDD "MVP completion rules" [S1]): open.
- Objective 1's bonus for capturing enemy soldiers lands at the prisoners' return (the plan, section 4.3 [S2]): Phase 5/6.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] The 1949 armistice layer, content/map/armistice_hexes_1949.yaml (signing dates per agreement)
- [S4] The owner's phase answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15
- [S5] The owner's Phase 7 decision on #784, 17 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6232838937, checked 2026-10-17
- [S6] The owner's decision on #784, 17 October 2026 (D-H2, D-H3, D-H4 as recommended), https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6231517464, checked 2026-10-21

## Open questions

- Minister roles, candidates and effects (D-H3), beyond the first slice.
- Bonus amounts and the timing formula's remaining parts (GDD "MVP completion rules" [S1]).
- The GDD's "approximately 48 turns" [S1] against the decided 44 turns [S2]: a one-line GDD fix for the owner.
