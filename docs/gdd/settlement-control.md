# Settlement control

## Player story

As the player, I want a captured settlement to present a sober choice between leaving and holding it, with the consequences stated before I act, so that control of territory is part of the war without turning harm to residents into a reward (GDD [S1]; owner rulings [S3]-[S7]).

## Rules

- Only `arab_town` rows of `settlements_1948.yaml` that a sourced operation names may enter the curated target list (planned R-CTRL-001). Jaffa and Gaza require the target research before they can enter it. Every target row is `sensitivity-reviewed`, and an unreviewed row is refused by the release gate (`Q-TGT-1`, `Q-TGT-3`, owner ruling [S3]).
- Enemy units may capture the player's localities. The capture changes their state, and every place tied by sources to a dated loss receives a sourced card. A capture that exists only in an alternate timeline also requires researched cards (`Q-TGT-2`; planned R-CTRL-006; owner ruling [S3]).
- Settlement state uses one immutable record per settlement in an ordinal `SortedDictionary<string, SettlementControl>`. It is written to the deterministic hash as `ctrl{...}` only when non-empty, and each field is written only when it differs from its default. `DisplacedByPlayer` is an integer count of places and is written only when non-zero (`Q-TECH-2`, `Q-CAP-2`; planned R-CTRL-002; owner rulings [S3], [S4]).
- Residents and combatants are distinct. The state records only whether a place is inhabited, not a resident count, and only enemy military units on its hex are combatants (`Q-CAP-2`, `Q-CAP-3`; planned R-CTRL-003; owner ruling [S4]).
- A target is captured at turn end when its hex contains no enemy combatant and a player unit holds it (`Q-CAP-1` / `D-F2`; planned R-CTRL-004; owner ruling [S4]).
- A capture awaiting a decision remains pending and the player's units stay in place. What happens if that decision is still pending at the MVP's end remains open (`Q-HIST-1`; planned R-CTRL-008; owner ruling [S5]).
- Capture outcomes use one `ResolveCapture(settlement, outcome)` command and one Attention-cost key. End-of-occupation outcomes likewise use one `EndOccupation(settlement, outcome)` command. The capture dialog reads its options and cost preview from public simulation helpers and sends the selected command through the existing queue; it holds no rule of its own (`Q-TECH-3`, `Q-TECH-5`; owner ruling [S3]).
- Raid makes the player's units leave immediately, returns the place to enemy control and does not change its residents (`Q-RAID-1`; planned R-CTRL-010; owner ruling [S5]). The reference player and `HistoricalAI` always choose Raid (`Q-REF-1`; planned R-CTRL-009; owner ruling [S5]).
- Occupy leaves one player unit on the hex, charges Money upkeep each turn and does not affect residents (`Q-OCC-1` / `D-F4`; planned R-CTRL-011; owner ruling [S5]). The upkeep is a labelled design value that agents propose and the owner may override (`Q-GEN-1`; owner rulings [S3], [S5]).
- Every occupation uses the same duration, X turns. X is a labelled design value that agents propose with reasoning and the owner may override (`Q-OCC-3` / `D-F4`; planned R-CTRL-012; owner ruling [S6]). Withdrawal before the timer ends is allowed and becomes a Raid (`Q-OCC-2` / `D-F2`; planned R-CTRL-013; owner ruling [S5]).
- The MVP has no puppet-regime outcome. When an occupation ends, the choices are annex or withdraw (`Q-PUP-1` / `D-F4`; planned R-CTRL-015; owner ruling [S6]).
- On capture, the residents of a target place are recorded as having fled, unless highly credible Israeli sources, such as an official or state-archive history, say otherwise for that place (in the owner's words, "default is fled, unless you have super credible israeli sources that say otherwise"). Every place's outcome is recorded in its reviewed target row. Where the residents stayed, annexation joins them to the player's population with their housing and jobs (`Q-ANNEX-1` / `D-F4`; planned R-CTRL-016; owner rulings [S6], [S7]).
- A place emptied by flight grants territory only. Annexation of it grants no housing, jobs or score (`Q-ANNEX-1` follow-up; planned R-CTRL-016; owner ruling [S7]).
- After Deport, the depopulated place offers neither puppet regime nor annexation; Deport ends the occupation permanently (`Q-ANNEX-2`; planned R-CTRL-017; owner ruling [S6]).
- An event tagged `displacement` may not give a positive effect to Money, Food, Arms, Social cohesion, International Support or score, with no exception. This supersedes the Phase two matching-cost form for this tag (`Q-DEP-6`; planned R-EFX-007; owner ruling [S4]).
- The release gate covers `displacement` events, target rows, English and Hebrew strings for both settlement-control dialogs, and map-state labels and tooltips (`Q-GATE-1`; planned R-GATE-001 and R-GATE-002; owner ruling [S4]).
- Phase six balance numbers live in `content/` as labelled design values and are listed by the design-values test. The owner sets the numbers (`Q-GEN-1`; owner ruling [S3]).
- The planned rule-ID areas are R-CTRL for settlement control, R-GATE for the release gate and the existing R-EFX area for the `displacement` no-gain rule. These IDs remain planned until their implementation PR defines each one with its citing test (`Q-TECH-6`; owner ruling [S3]).

## Data touched

- `content/settlement_targets.yaml` — the curated, sourced and sensitivity-reviewed target rows; it is inside the content hash (`Q-TECH-1`, owner ruling [S3]).
- `content/rules.yaml` (`mvp:`) — labelled design values for settlement control (`Q-GEN-1`, owner ruling [S3]).
- Simulation state — the ordinal settlement-control collection and `DisplacedByPlayer`; capture and occupation commands, helpers and resolver steps (`Q-TECH-2` to `Q-TECH-5`, owner ruling [S3]).
- `content/events/*.yaml` and the event schema — cards tagged `displacement` and their no-gain rule (`Q-DEP-6`, owner ruling [S4]).
- Unity — the capture and end-of-occupation dialogs, plus map-state labels and tooltips; rules remain in the simulation (`Q-GATE-1`, `Q-TECH-5`, owner rulings [S3], [S4]).
- `docs/rules/mvp.md` — planned R-CTRL, R-GATE and R-EFX definitions land only with their implementation and citing tests (`Q-TECH-6`, owner ruling [S3]).

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every decided rule above cites one of the owner's five rulings of 18 October 2026 [S3]-[S7], and every undecided owner pick remains under Open questions.
- Tests reach the not-yet-released Deport path through test `Fixture` content, not the removed `MvpSlice.Overlay` (`Q-TECH-3`, owner ruling [S3]).
- The dominance test clones each sampled capture state for every offered outcome, compares every resource, parameter, score and objective at turn end and MVP end, and covers the MVP goldens plus full-strength and minimum-strength fixture states (`Q-TECH-4`, owner ruling [S3]).
- `uv run scripts/oss/rule_ids.py` passes without defining any planned Phase six ID in this docs-only change.
- `uv run scripts/oss/check_repo.py` passes.

## Out of scope

- The researched target rows, per-place outcomes and cards themselves; this section records the rules for that later research and content work.
- Diplomacy and behaviour for the `arab_refugees` actor; Phase 6 records only `DisplacedByPlayer` [S2].
- Ministers' roles in occupation or military government; those belong to Phase 7 [S2].
- Any balance number not set by the owner. X and upkeep remain labelled design values, not values invented here [S3], [S5], [S6].
- Definitions in `docs/rules/mvp.md`; each planned ID lands with the implementation PR and its citing test [S2], [S3].

## Sources

- [S1] GDD, `docs/GDD.md` ("Military actions and settlement control").
- [S2] Phase 6 implementation plan, `docs/superpowers/plans/2026-10-15-gdd-phase-6.md`.
- [S3] [Owner decisions, batch 1](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249796458), 18 October 2026, checked 2026-10-18.
- [S4] [Owner decisions, batch 2](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249810256), 18 October 2026, checked 2026-10-18.
- [S5] [Owner decisions, batch 3](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249822320), 18 October 2026, checked 2026-10-18.
- [S6] [Owner decisions, batch 4](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249846499), 18 October 2026, checked 2026-10-18.
- [S7] [Owner follow-ups to Q-ANNEX-1](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249967916), 18 October 2026, checked 2026-10-18.

## Open questions

- `Q-HIST-1` follow-up — what happens when a capture choice is still pending at the MVP's end.
- `Q-DEP-1` to `Q-DEP-5` and `Q-DEP-7` — Deport's remaining rule, affected residents, destination, buildings, costs and indirect military advantage.
- `Q-SENS-2` — how the no-capacity rule applies to historical sites on land recorded as depopulated.
- `Q-CARD-1` to `Q-CARD-3` and `Q-SENS-1b` — card coverage, the M3 Lydda and Ramle ruling, images and wording about abandoned housing.
- `Q-UI-1` to `Q-UI-4` — option text, preview contents, map treatment and display of `DisplacedByPlayer`.
- `Q-STORE-1` — how the shipped system is described on the store page.
