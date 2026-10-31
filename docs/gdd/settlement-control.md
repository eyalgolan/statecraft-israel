# Settlement control

## Player story

As the player, I want a captured settlement to present a sober choice between leaving and holding it, with the consequences stated before I act, so that control of territory is part of the war without turning harm to residents into a reward (GDD [S1]; owner rulings [S3]-[S7]).
I can recover from losing a region and understand controlling an enemy town without making its residents assignable workers.

## Rules

- Only `arab_town` rows of `settlements_1948.yaml` that a sourced operation names may enter the curated target list (R-CTRL-001). Jaffa and Gaza require the target research before they can enter it. Every target row is `sensitivity-reviewed`, and an unreviewed row is refused by the release gate (`Q-TGT-1`, `Q-TGT-3`, owner ruling [S3]). **Then decided (owner, 18 Oct 2026, "Only in a what-if"; [#793](https://github.com/eyalgolan/statecraft-israel/issues/793#issuecomment-6254333177)):** Jaffa stays on the target list, but is a target in play only if an alternate-timeline card returns it to Arab control; in the historical course it is Israeli-held from the start and no capture decision is offered [S9].
- Enemy units may capture the player's localities. The capture changes their state, and every place tied by sources to a dated loss receives a sourced card. A capture that exists only in an alternate timeline also requires researched cards (`Q-TGT-2`; planned R-CTRL-006; owner ruling [S3]).
- Settlement state uses one immutable record per settlement in an ordinal `SortedDictionary<string, SettlementControl>`. It is written to the deterministic hash as `ctrl{...}` only when non-empty, and each field is written only when it differs from its default. `DisplacedByPlayer` is an integer count of places and is written only when non-zero (`Q-TECH-2`, `Q-CAP-2`; R-CTRL-002 for the collection; owner rulings [S3], [S4]).
- Residents and combatants are distinct. The state records whether a place is inhabited, fled or depopulated, not a resident count, and only enemy military units on its hex are combatants (`Q-CAP-2`, `Q-CAP-3`; R-CTRL-003; owner ruling [S4]).
- A target is captured the moment a player unit's move ends on its hex while no enemy combatant is there. The capture answer still takes effect at End turn (`Q-CAP-1` / `D-F2`; R-CTRL-004, R-CTRL-005; owner ruling [S4]; owner, 23 October 2026, #1402 and #1398: "Instant", "Yes, answer at End turn" and "Approve, start building").
- A capture awaiting a decision remains pending and the player's units stay in place. What happens if that decision is still pending at the MVP's end remains open (`Q-HIST-1`; planned R-CTRL-008; owner ruling [S5]). **Then decided (owner, 18 Oct 2026, "Counts as Raid"; [#797](https://github.com/eyalgolan/statecraft-israel/issues/797#issuecomment-6254333420)):** at the MVP's end the units leave, nothing changes for the residents and the pending capture adds nothing to the score [S10].
- Capture outcomes use one `ResolveCapture(settlement, outcome)` command and one Attention-cost key. End-of-occupation outcomes likewise use one `EndOccupation(settlement, outcome)` command. The capture dialog reads its options and cost preview from public simulation helpers and sends the selected command through the existing queue; it holds no rule of its own (`Q-TECH-3`, `Q-TECH-5`; owner ruling [S3]).
- Raid makes the player's units leave immediately, returns the place to enemy control and does not change its residents (`Q-RAID-1`; planned R-CTRL-010; owner ruling [S5]). The reference player and `HistoricalAI` always choose Raid (`Q-REF-1`; planned R-CTRL-009; owner ruling [S5]).
- Occupy leaves one player unit on the hex, charges Money upkeep each turn and does not affect residents (`Q-OCC-1` / `D-F4`; planned R-CTRL-011; owner ruling [S5]). The upkeep is a labelled design value that agents propose and the owner may override (`Q-GEN-1`; owner rulings [S3], [S5]).
- Every occupation uses the same duration, X turns. X is a labelled design value that agents propose with reasoning and the owner may override (`Q-OCC-3` / `D-F4`; planned R-CTRL-012; owner ruling [S6]). Withdrawal before the timer ends is allowed and becomes a Raid (`Q-OCC-2` / `D-F2`; planned R-CTRL-013; owner ruling [S5]).
- The MVP has no puppet-regime outcome. When an occupation ends, the choices are annex or withdraw (`Q-PUP-1` / `D-F4`; planned R-CTRL-015; owner ruling [S6]).
- On capture, the residents of a target place are recorded as having fled, unless highly credible Israeli sources, such as an official or state-archive history, say otherwise for that place (in the owner's words, "default is fled, unless you have super credible israeli sources that say otherwise"). Every place's outcome is recorded in its reviewed target row. Where the residents stayed, annexation joins them to the player's population with their housing and jobs (`Q-ANNEX-1` / `D-F4`; planned R-CTRL-016; owner rulings [S6], [S7]).
- A place emptied by flight grants territory only. Annexation of it grants no housing, jobs or score (`Q-ANNEX-1` follow-up; planned R-CTRL-016; owner ruling [S7]).
- **Then decided (owner, 24 Oct 2026, in the coordinator session; [#799](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433)):** annexing a stayed town (Acre, Nazareth) adds one city by the starting-city rule, from a sourced resident count the owner approves (R-CTRL-016); a fled town's residents are recorded as fled at capture, and the record stays after any return to enemy control (R-CTRL-003 to R-CTRL-005, R-CTRL-010); an annexed town stays Israeli for good, with no upkeep and no unit that has to stay (R-CTRL-016). Deport of a town whose residents fled at capture stays listed third and is refused, with its reason on the button (OWNER QUESTION 4 (a), "Hide it, button explains (Recommended)"; R-CTRL-018; owner ruling [S12]). (unitless)

The owner answers for plan #1520 are catalogued in [S11].

*Considered and declined (owner, 30 Oct 2026; Regional pooling conflict, option A; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed keeping annexed residents outside the player workforce. For his tax, garrison, upkeep and International Support values for occupation and annexation, the review did not rule; `R-CTRL-011` to `R-CTRL-016` (`docs/rules/mvp.md:55`) stay operative.* [S1]
- After Deport, the depopulated place offers neither puppet regime nor annexation; Deport ends the occupation permanently (`Q-ANNEX-2`; planned R-CTRL-017; owner ruling [S6]).
- Deport follows impact-plan §4.1 items 1, 2, 3, 5 and 6 as written, with the two clerical fixes. It affects every resident the state holds and records only `DisplacedByPlayer`, with no destination on the board (`Q-DEP-1` to `Q-DEP-3`; owner ruling [S8]). (unitless)
- Deportation and razing are separate rules, with no razing option in the MVP (`Q-DEP-4` / `D-F3`; owner ruling [S8]).
- Deport has a one-time cost to Social cohesion and International Support, a fixed International Support drag per turn and a lower ceiling on International Support for the rest of the game. Their sizes are labelled design values set by the balance pass; no value is selected here, and `DeportNeverDominates` must hold with a margin (`Q-DEP-5`; owner ruling [S8]).
- A depopulated place behaves for enemy units exactly as a raided one does. `DeportNeverDominates` also compares the war objective's timing and the war-loss reading, and a depopulated hex gives nothing to a Phase 4 pioneer action. (`Q-DEP-7`; owner ruling [S8]). (unitless)
- The no-capacity constraint applies to places the player depopulates in play. A designated site on land that sources record as depopulated in 1948 is not barred by that constraint, and its card states the history factually under D-F1 (`Q-SENS-2`; owner ruling [S8]).
- A displacement follow-up card states the in-game consequence with one sourced line of history. `ten_days` stays a military card; only the new cards state displacement. A `displacement` card has no image, and Phase 6 text does not name abandoned Arab housing. Every card still needs a person's source check and sensitivity review before it ships (`Q-CARD-1` to `Q-CARD-3`, `Q-SENS-1b`; owner ruling [S8]). (unitless)
- The capture options use the GDD's words — Raid and withdraw, Occupy and Deport — with direct Hebrew equivalents and a one-line plain description under each. The preview shows resource and parameter changes only. The map keeps the place's name and shows a marker per state. `DisplacedByPlayer` is shown on the International Support tooltip as a cause (`Q-UI-1` to `Q-UI-4`; owner ruling [S8]).
- When Phase 6 ships, the store page adds the drafted settlement-control sentence (`Q-STORE-1`; owner ruling [S8]). (unitless)
- An event tagged `displacement` may not give a positive effect to Money, Food, Arms, Social cohesion, International Support or score, with no exception. This supersedes the Phase two matching-cost form for this tag (`Q-DEP-6`; planned R-EFX-007; owner ruling [S4]).
- The release gate covers `displacement` events, target rows, English and Hebrew strings for both settlement-control dialogs, and map-state labels and tooltips (`Q-GATE-1`; planned R-GATE-001 and R-GATE-002; owner ruling [S4]).
- Phase six balance numbers live in `content/` as labelled design values and are listed by the design-values test. The owner sets the numbers (`Q-GEN-1`; owner ruling [S3]).
- The planned rule-ID areas are R-CTRL for settlement control, R-GATE for the release gate and the existing R-EFX area for the `displacement` no-gain rule. These IDs remain planned until their implementation PR defines each one with its citing test (`Q-TECH-6`; owner ruling [S3]).
### Player regions and automatic evacuation

**Amir's proposal, declined by the owner on 30 October 2026 (Regional pooling conflict, option A) [S13]:** capturing a regional capital would transfer pooled regional capacity. These regional mechanics are non-operative. Automatic evacuation through reception, port queues and transit was not ruled on by the 30 October 2026 conflict review; main has no such rule, so it is `open (owner)`. Civilians are not automatically casualties, captives or enemy workers; the existing content-policy and review protections remain authoritative. Campaign defeat follows the retained exhausted-resource (economic) rule; under `gdd-0.3` losing a city or region alone does not end the game, and there is no generic region-loss rule.

### Enemy towns and cities

Each enemy town/city has individual control. Capturing it does not transfer a surrounding region or every nearby locality. Preserve the curated capture-target list separately from the [map roster](../locations.md); inclusion grants no capture permission. Shared-hex markers keep separate ids rather than merged resident/administration records.

Capture requires a curated target, friendly ground unit at its hex and no defending enemy unit. Combat may clear defenders without automatically executing a civilian-control action. Change only the selected locality. Preview eligibility and control consequences before confirmation; refuse unavailable/review-gated outcomes.

### Occupation and annexation proposal

The retained `R-CTRL-011` rule requires an Israeli military unit on the place's hex for Occupy—the one unit that stays—and moves no unit [S14]. **Amir's proposal [S1]:** occupation would add proposed tax rules. The owner's 30 October 2026 conflict review did not rule on this; `R-CTRL-011` to `R-CTRL-013` (`docs/rules/mvp.md:55`) stay operative and add no tax.

The retained ruling has no puppet-regime outcome [S6]. **Amir's proposal, declined by the owner on 30 October 2026 (Annexation conflict, option A) [S1]:** town/city regime taxes would be added; that addition is non-operative. For an annexation cost, the 30 October 2026 conflict review did not rule; `R-CTRL-016` (`docs/rules/mvp.md:65`) stays operative.

**Amir's proposal, declined by the owner on 30 October 2026 [S1]:** enemy recapture would clear annexation. The retained ruling says an annexed town stays Israeli for good [S11].

**Amir's proposal, declined by the owner on 30 October 2026 [S1]:** residents would remain outside the player's civilian systems and produce regime taxes. The retained rule adds residents of stayed towns under the starting-city rule [S11].

### Civilian protection and review boundaries

Old references to Raid, Occupy and Deport do not approve every sensitive outcome. Only occupation/regime rules above are completed here. Do not infer displacement, razing, civilian capture, forced labour or rewards from suffering. [CONTENT_POLICY](../CONTENT_POLICY.md) and curator/sensitivity gates remain [S15]. Prisoners of war are combatants under a separate system.

## Data touched

- `content/settlement_targets.yaml` — the curated, sourced and sensitivity-reviewed target rows; it is inside the content hash (`Q-TECH-1`, owner ruling [S3]).
- `content/rules.yaml` (`mvp:`) — labelled design values for settlement control (`Q-GEN-1`, owner ruling [S3]).
- Simulation state — the ordinal settlement-control collection and `DisplacedByPlayer`; capture and occupation commands, helpers and resolver steps (`Q-TECH-2` to `Q-TECH-5`, owner ruling [S3]).
- `content/events/*.yaml` and the event schema — cards tagged `displacement` and their no-gain rule (`Q-DEP-6`, owner ruling [S4]).
- Unity — the capture and end-of-occupation dialogs, plus map-state labels and tooltips; rules remain in the simulation (`Q-GATE-1`, `Q-TECH-5`, owner rulings [S3], [S4]).
- `docs/rules/mvp.md` — planned R-CTRL, R-GATE and R-EFX definitions land only with their implementation and citing tests (`Q-TECH-6`, owner ruling [S3]).
- **Amir's proposal, declined by the owner on 30 October 2026 (Regional pooling conflict, option A) [S13]:** region/capital membership would be added. That data change is non-operative. Automatic evacuation state was not ruled on by the 30 October 2026 conflict review and is `open (owner)`. Per-locality control, target allow-list, occupation progress and annexation remain.
- Capture commands/events require migration and review; this edit does not add capture targets or change review status.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every decided rule above cites one of the owner's five rulings of 18 October 2026 [S3]-[S7], and every undecided owner pick remains under Open questions.
- Tests reach the not-yet-released Deport path through test `Fixture` content, not the removed `MvpSlice.Overlay` (`Q-TECH-3`, owner ruling [S3]).
- The dominance test clones each sampled capture state for every offered outcome, compares every resource, parameter, score and objective at turn end and MVP end, and covers the MVP goldens plus full-strength and minimum-strength fixture states (`Q-TECH-4`, owner ruling [S3]).
- `uv run scripts/oss/rule_ids.py` passes without defining any planned Phase six ID in this docs-only change.
- `uv run scripts/oss/check_repo.py` passes.
- **Amir's proposal, declined by the owner on 30 October 2026 (Regional pooling conflict, option A) [S13]:** capital capture would disable a region. That criterion is non-operative. Route-blocked evacuation was not ruled on by the 30 October 2026 conflict review and is `open (owner)`.
- Pre-annexation withdrawal becomes a Raid; after annexation the town stays Israeli for good, with no unit that has to stay [S11].
- The proposal's resident treatment is not implemented unless the owner supersedes R-CTRL-016.
- Unreviewed outcomes remain unavailable and shared-hex identities stay distinct.

## Out of scope

- The researched target rows, per-place outcomes and cards themselves; this section records the rules for that later research and content work.
- Diplomacy and behaviour for the `arab_refugees` actor; Phase 6 records only `DisplacedByPlayer` [S2].
- Ministers' roles in occupation or military government; those belong to Phase 7 [S2].
- Any balance number not set by the owner. X and upkeep remain labelled design values, not values invented here [S3], [S5], [S6].
- Definitions in `docs/rules/mvp.md`; each planned ID lands with the implementation PR and its citing test [S2], [S3].
All historical civilian fates, capture eligibility for every marker, civilian labour and final terminal-defeat conditions.

## Sources

- [S1] GDD, `docs/GDD.md` ("Military actions and settlement control").
- [S2] Phase 6 implementation plan, `docs/superpowers/plans/2026-10-15-gdd-phase-6.md`.
- [S3] [Owner decisions, batch 1](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249796458), 18 October 2026, checked 2026-10-18.
- [S4] [Owner decisions, batch 2](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249810256), 18 October 2026, checked 2026-10-18.
- [S5] [Owner decisions, batch 3](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249822320), 18 October 2026, checked 2026-10-18.
- [S6] [Owner decisions, batch 4](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249846499), 18 October 2026, checked 2026-10-18.
- [S7] [Owner follow-ups to Q-ANNEX-1](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6249967916), 18 October 2026, checked 2026-10-18.
- [S8] [Owner decision, "Accept all 16 drafts"](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676), 18 October 2026, checked 2026-10-18.
- [S9] [Owner decision, "Only in a what-if"](https://github.com/eyalgolan/statecraft-israel/issues/793#issuecomment-6254333177), 18 October 2026, checked 2026-10-18.
- [S10] [Owner decision, "Counts as Raid"](https://github.com/eyalgolan/statecraft-israel/issues/797#issuecomment-6254333420), 18 October 2026, checked 2026-10-18.
- [S11] [Owner's answers to plan #1520](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6374876433), 24 October 2026, checked 2026-10-23.
- [S12] [Owner ruling on OWNER QUESTION 4, "Hide it, button explains (Recommended)"](https://github.com/eyalgolan/statecraft-israel/issues/799#issuecomment-6375591758), 24 October 2026, checked 2026-10-24.
- [S13] Region/control interview decisions, docs/GDD.md.
- [S14] Retained Occupy garrison rule, `docs/rules/mvp.md` (`R-CTRL-011`).
- [S15] Sensitive-content constraints, docs/CONTENT_POLICY.md.

## Open questions

- `Q-HIST-1` follow-up — what happens when a capture choice is still pending at the MVP's end. **Then decided (owner, 18 Oct 2026, "Counts as Raid"; [#797](https://github.com/eyalgolan/statecraft-israel/issues/797#issuecomment-6254333420)):** the units leave, nothing changes for the residents and it adds nothing to the score.
- `Q-DEP-1` to `Q-DEP-5` and `Q-DEP-7` — Deport's remaining rule, affected residents, destination, buildings, costs and indirect military advantage. **Then decided (owner, 18 Oct 2026, "Accept all 16 drafts"; [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676)):** Q-DEP-1 (a) with the two clerical fixes, Q-DEP-2 (a), Q-DEP-3 (a), Q-DEP-4 (a), Q-DEP-5 (c) and Q-DEP-7 (c); sizes remain labelled design values for the balance pass, and `DeportNeverDominates` must hold with a margin.
- `Q-SENS-2` — how the no-capacity rule applies to historical sites on land recorded as depopulated. **Then decided (owner, 18 Oct 2026, "Accept all 16 drafts"; [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676)):** (c).
- `Q-CARD-1` to `Q-CARD-3` and `Q-SENS-1b` — card coverage, the M3 Lydda and Ramle ruling, images and wording about abandoned housing. **Then decided (owner, 18 Oct 2026, "Accept all 16 drafts"; [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676)):** Q-CARD-1 (c), Q-CARD-2 (a), Q-CARD-3 (a) and Q-SENS-1b (a); every card still needs a person's source check and sensitivity review before it ships.
- `Q-UI-1` to `Q-UI-4` — option text, preview contents, map treatment and display of `DisplacedByPlayer`. **Then decided (owner, 18 Oct 2026, "Accept all 16 drafts"; [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676)):** Q-UI-1 (b), Q-UI-2 (a), Q-UI-3 (a) and Q-UI-4 (a).
- `Q-STORE-1` — how the shipped system is described on the store page. **Then decided (owner, 18 Oct 2026, "Accept all 16 drafts"; [#783](https://github.com/eyalgolan/statecraft-israel/issues/783#issuecomment-6254310676)):** (a), add the drafted sentence when Phase 6 ships.
Unresolved principle and authoring decisions are maintained only in [the GDD](../GDD.md#deferred-decisions).
