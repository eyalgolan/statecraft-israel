# Ministers, events and the end of the MVP

## Player story

As a player of the MVP, I want the game to run its events and my chosen ministers through to an end screen on 15 January 1952 [S2], so that a full game resolves into a score and its historical completion cards (GDD "MVP scope" and "Historical events on objective completion" [S1]).
I read historical choices/national conditions and reach an ending explaining my achievements without hidden failure rules.

## Rules

- The MVP runs 44 turns (monthly), from 15 May 1948 to 15 January 1952: D-B1 fixes the start and the monthly grid [S2], and the owner ended the MVP on 15 January 1952, keeping the 15th grid with no adjusted turn (D-E5, decided 15 October 2026 [S2]). The GDD text still says "approximately 48 turns" [S1]; the owner settled 44 turns [S2].
- **Correction:** The current GDD now specifies 44 turns (monthly) ([GDD, MVP scope](../GDD.md#mvp-scope-1948-to-1951)) [S1]; the sentence above preserves superseded wording from the earlier baseline.
- Objective cards and scoring belong to the first phase (#379, `objectives-and-score.md`; D-B2, D-B6 [S2]) and are not restated here. This section covers only what the final phase adds: every card complete, the end screen (D-H4, decided 17 October 2026 [S6] and built as R-END-005 and R-END-006, `docs/rules/mvp.md`), defeat (D-B5, decided 15 October 2026 [S2]), the events (D-H1 and D-H2, decided 17 October 2026 [S5]) and the ministers (D-H3, first slice decided 17 October and 18 October 2026 and built as R-MIN-001 to R-MIN-003, and declared complete for the MVP on 23 October 2026 [S7]) [S2].
- Defeat: not all objectives are required to finish. The game is lost when Money and Food are exhausted for several turns in a row or on a war loss, and coalition collapse is off in the MVP (D-B5, decided 15 October 2026 [S2]). The defeat is built in this phase, with the counter set then (`Q-GOV-2`, decided 15 October 2026 [S4]). A war loss is enemy units holding the hex of one of the player's cities at turn end (`Q-WAR-5`, decided 15 October 2026 [S4]); "Money exhausted" is a debt at or above the debt ceiling (`Q-DEF-1`, owner, 17 October 2026, [#784](https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6232838937) [S5]; built as R-END-001, `docs/rules/mvp.md`).
- The war-survival objective completes on the last armistice event, the Syria-Israel agreement of 20 July 1949 (D-B2 [S2]; [S3]). The four signings were Egypt on 24 February 1949, Lebanon on 23 March 1949, Jordan on 3 April 1949 and Syria on 20 July 1949 [S3].
- The existing cabinet and Attention stay (D-C5 [S2]). Minister roles, candidates and effects are D-H3 [S2]: the first slice is decided and built - each office's competence scales the GDD systems in its remit (`Q-MIN-1` to `Q-MIN-3`, owner, 17 October and 18 October 2026; R-MIN-001 to R-MIN-003, `docs/rules/mvp.md`). The owner declared that slice the complete MVP minister mechanic on 23 October 2026 [S7].

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed removing Attention from cabinet, events and map actions.* [S1]

The retained existing office-competence slice remains the complete MVP minister mechanic [S7].
- Events (D-H1): the owner took the recommendations for `Q-EVT-1` to `Q-EVT-4` on 17 October 2026 [S5]. Windows, hard anchors and a cap of three soft events a turn stay; an unanswered event is delegated to its office's holder; no effect is delayed; the card shows every effect of every option from the sim (`R-EVT-001` to `R-EVT-005`, `docs/rules/mvp.md`). An early armistice's place ahead of the other soft events comes with `front_quiet`, in the war-objective PR of the military phase (#752).
- Event effects (D-H2): the owner took the recommendations for `Q-HFX-1` to `Q-HFX-3` on 17 October 2026 [S5]. An option can grant units, free, at the military camp or a named city (a site once sites carry a date), and change a city's housing capacity or job slots; a sensitive option gains nothing from them; each is checked at load (`R-EFX-003` to `R-EFX-006`, `docs/rules/mvp.md`). Housing condition is part of R-EFX-004 and project unlock is R-EFX-008, built beside R-EFX-003, R-EFX-005 and R-EFX-006 (#959, #1002, #1072; #806 closed).
[GDD scope](../GDD.md#mvp-scope-1948-to-1951) owns the end boundary [S1]; final scoring precedes ending. Show score, achieved/pending objective groups, historical comparisons and national/resource state. All goals are not required merely to finish.

**Amir's proposal, declined by the owner on 30 October 2026 [S8]:** Money/Food exhaustion and any-city war loss would cease to be defeat triggers. The retained Money/Food exhaustion and any-city war-loss rules in [Rules](#rules) remain authoritative; generic region loss and coalition instability are not additional triggers.

Historical offices, people and event roles remain, and the complete MVP minister mechanic is the existing office-competence slice under [owner ruling #1513](https://github.com/eyalgolan/statecraft-israel/issues/1513#issuecomment-6372357178) [S7]. **Amir's proposal to remove Attention was declined by the owner on 30 October 2026 [S1].**

Social cohesion retains `morale` compatibility; support retains `intl_standing`. [Balance](balance.md#campaign-and-indicators) owns scale/hysteresis. Low situations expose state to reviewed events without secret extra penalties or defeat. Amir's proposed regional employment aggregate and replacement Food/housing penalties are non-operative [S1].

**Amir's proposal, declined by the owner on 30 October 2026 [S1]:** the proposed event-accounting restatement prohibited borrowing. The retained borrowing rule remains authoritative; hard/soft windows and sensitivity restrictions still apply.

[Objective-card](objective-card.md) owns completion/images/dates/points and [combat](military-combat.md) owns armistices/prisoner return. Do not duplicate their tables. Changed historical/sensitive content still requires human factual, licence and sensitivity review.

## Data touched

- `objectives.yaml` (new, Phase 1): completion rules and card references.
- `events/`: the MVP's event set (#135 research).
- `content/governments.yaml`, `offices.yaml`, `people.yaml`: minister candidates.
- The end screen and score display (Unity).
- Government/event adapters, indicator compatibility, low-state visibility and ending views.
- Reviewed content is not rewritten/re-approved by this documentation.
- Event/optional-spending adapters need ruleset-aware migration.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, the plan, or a sourced historical date.
- A normal ending is reachable with unfinished goals and reports actual results.
- **Amir's proposal, declined by the owner on 30 October 2026 [S1]:** exhausted resources and any-city war loss would not call the retained defeat triggers.
- **Amir's proposal, declined by the owner on 30 October 2026 [S1]:** Attention would be absent across government and event interactions.
- Effects apply once under the existing release gates.

## Out of scope

- Minister roles and effects (D-H3) beyond the first slice (R-MIN-001 to R-MIN-003): none in the MVP [S7].
- The objectives panel UI and the score's presentation in Phase 1.
- The exact completion conditions of each objective (GDD "MVP completion rules" [S1]): open.
- **Correction:** The immigrant-absorption objective has an operative completion test at [objective-card.md:33](objective-card.md#rules); only completion conditions not settled there remain open.
- Objective 1's bonus for capturing enemy soldiers lands at the prisoners' return (the plan, section 4.3 [S2]): Phase 5/6.
Invented minister perks, a new final defeat principle, unreviewed event rewrites and removal or replacement of the retained passive budget behavior.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, sections 5 and 6, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] The 1949 armistice layer, content/map/armistice_hexes_1949.yaml (signing dates per agreement)
- [S4] The owner's phase answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15
- [S5] The owner's Phase 7 decision on #784, 17 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6232838937, checked 2026-10-17
- [S6] The owner's decision on #784, 17 October 2026 (D-H2, D-H3, D-H4 as recommended), https://github.com/eyalgolan/statecraft-israel/issues/784#issuecomment-6231517464, checked 2026-10-21
- [S7] The owner's decision on #1513, 23 October 2026 ("Yes, complete (Recommended)"), https://github.com/eyalgolan/statecraft-israel/issues/1513#issuecomment-6372357178, checked 2026-10-23
- [S8] Later decisions/deferred principles, docs/GDD.md.

## Open questions

- Bonus amounts and the timing formula's remaining parts (GDD "MVP completion rules" [S1]).
- The GDD's "approximately 48 turns" [S1] against the decided 44 turns [S2]: a one-line GDD fix for the owner.
See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
