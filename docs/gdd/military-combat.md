# Military movement and combat

## Player story

I command identifiable units between hexes, preview strength, and earn combat experience by surviving engagements without rewarding civilian suffering.

## Rules

### Units and equipment

Military units are the same people managed by [training](training.md), not additional starting formations. Historical art/brigade anchors are placement evidence, not another manpower ledger. Infantry, artillery and armoured roles are distinct from qualification. Specific equipment models stay in the GDD's evidence queue.

Strength uses `military.strength` by qualification, multiplied by incomplete-reception and previous-turn own Food-shortage factors. [Balance](balance.md#movement-and-engagement-defaults) owns role/defence factors. [Training](training.md#military-conversion) owns equipment issue/return. Neither attacks nor artillery fire spends Arms.

Historical mode binds existing sourced anchors to available scenario army units without duplicating people or guessing its deferred count. Enemy names/dated positions retain source records and per-row fact checks. Open mode retains its earlier no-starting-army choice; this does not select Historical army size. No playable air/naval units are introduced; delivery ships are presentation.

### Movement and orders

Movement is direct turn-based hex-to-hex movement using integer points and existing eight biome ids. Costs, allowances, corridor reduction and stacking are [balance](balance.md#military-and-control) keys; expanded terrain remains deferred. Lakes/sea are impassable, with no embarkation or zone of control. Enter legal land within allowance. Built corridors use the lower of their cost and biome cost.

Orders are Move, Fortify, Hold and Attack, without Attention. Fortify takes its catalogue duration, gives its defence bonus and is lost immediately on movement. Hold grants no interception or additional modifier. Validate routes, capacity and truces before mutation.

A hostile combatant hex needs combat, not peaceful entry. Locality capture is a distinct [validated control action](settlement-control.md); movement cannot bypass review/target gates. Tile control and individual locality administration remain distinct.

### Engagements and XP

Attack adjacent combatants, combining committed attackers. Apply per-unit qualification, role, reception and Food factors, then the defender's biome, permanent fort and Fortify. Add defence percentages before multiplying. The adjusted ratio selects the [catalogue table](balance.md#movement-and-engagement-defaults) and uses exactly `combat.draw_count` draw. XP, loss selection and retreat add no further draw.

Losses affect combatant cohorts only; removed own cohorts leave population/Food demand. Do not infer historical casualties from nominal population scale. Retreat `combat.retreat_hexes` in stable legal neighbour order; blocked retreat becomes surrender. Loss selection follows catalogue tie breaks. Attackers do not automatically advance. Civilians are excluded from combat losses, including artillery-supported fights.

Surviving participants earn `military.combat_xp_gain` toward visible `military.combat_xp_full`. Attack, defence and survived losses/retreats count; destroyed/surrendered units earn none. At threshold grant veteran qualification and complete reception. Travel, service time and unused support orders do not count. New qualification affects subsequent engagements, not a resolved draw.

Artillery supports a valid ground attack within `combat.artillery_range`; it cannot attack alone or hold ground. Armoured units use their own allowance/factor. Both retain shared population and reusable equipment accounting. Permanent forts benefit whichever army occupies them and persist when vacated, separately from temporary Fortify.

### Enemy actions and truces

Enemy orders resolve before planned player actions. Historical mode uses sourced opening/first-truce scripts then deterministic goals; Open mode uses goals. Hold forts or attack adjacent enemies at `combat.enemy_attack_ratio`, without new decision RNG. Add no unsourced reinforcements or enemy supply model.

Operation cards order only already present sourced units; exact orders remain reviewed authoring work. Event changes reset applicable sensitivity review. Armistice removes that front's forces, releases player units and leaves agreement lines display-only.

Truces refuse attacks but permit movement. First refusal ends with the turn covering 8 July 1948 and second with the turn covering 15 October 1948 under the retained MFA chronology [S10]. The old security-rung timer no longer forces war. Early armistices cannot open before the turn on/after 16 November 1948 and need the quiet-turn period; hard anchors retain agreement dates [S2]. A refused offer can return after another qualifying period.

[Settlement control](settlement-control.md) owns capital loss and continuation; automatic defeat on losing any city is superseded. Battlefield actions add no direct indicator effects beyond the separately defined prisoner provisioning/control consequences.

### Prisoners

Only surrendered combatants become prisoners. Residents cannot be captured, traded, used as labour or resources. Kills/losses award no points. Enemy prisoners need `prisoner.food` separately from own people. [Resources](cutover-resource-layer.md#food) owns proportional allocation and the single shortage effect; no second prisoner shortage charge is added and shortage does not delete captives.

Returned enemy cohorts score only `prisoner.return_points` once through sourced front return cards. Egypt retains 9 March 1949, the exchange's end [S12]; other fronts need sourced return records. Text recognises ending captivity/fighting with fewer lost lives, not celebrating capture.

Own captured units retain id, profession, XP and reception while unavailable. They remain in own Food demand without active-army Money upkeep, return through sourced exchanges and score nothing. Surrender refunds no equipment. Return gives equipment only if explicitly authored; otherwise equip before service without duplicating old inventory.

### Preview and display

The sim supplies effective ratio, modifiers and outcome bands before attack. Cards show qualification, reception penalty, equipment and XP. Miniatures are symbolic; figure count creates no extra people/strength. Unsourced flags use the neutral placeholder pending evidence. Historical totals/art still require named human review; game unit scale is not a factual source.

## Data touched

- Military movement/orders, combat table, deterministic goals, truces, captives and return-event ids.
- Historical map anchors remain evidence; starting military allocation needs a scenario binding.
- Retire attack Arms costs, time-served veterancy and any-city war loss.
- Migrate combat/conversion/movement/saves together; defaults require playtesting.

## Done when

- Attacks/artillery do not spend Arms and conversion cannot duplicate equipment.
- Preview/resolver share the table and prescribed draw count.
- Survived engagements fill XP; combat veterancy completes reception.
- Permanent forts benefit either occupant and persist.
- Combat never converts residents into casualties, prisoners, workers or score; return bonuses pay once.
- Region loss permits continued play.

## Out of scope

Enemy supply simulation, unsourced reinforcements/orders, playable air/naval combat, civilian capture outcomes and replacement terrain taxonomy.

## Sources

- [S1] GDD — `docs/GDD.md` (the "Military actions and settlement control" section).
- [S2] Phase 5 plan — `docs/superpowers/plans/2026-10-15-gdd-phase-5.md`, whose "Recommended answers" table records every decided row used here.
- [S3] [#375](https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337) — owner, 15 Oct 2026: "Take all recommendations" for the Phase 2, 3 and 5 plan tables. Checked 2026-10-16.
- [S4] [#375](https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183538544) — owner, 15 Oct 2026: Q-START-4, Q-CIV-1 to Q-CIV-4, Q-ENEMY-5, Q-POW-1 and Q-POW-2. Checked 2026-10-16.
- [S5] [#375](https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183586870) — owner, 15 Oct 2026: Q-POW-3 to Q-POW-5, Q-ART-1, Q-ART-3. Checked 2026-10-16.
- [S6] [#375](https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183762894) — owner, 15 Oct 2026: Q-CIV-4 re-ruled "A: Israeli total only". Checked 2026-10-16.
- [S7] [#375](https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6184081266) — owner, 15 Oct 2026: Q-HOLD-1, Q-HOLD-2, Q-UI-2. Checked 2026-10-16.
- [S8] Impact plan — `docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md` (`2.2 units, `2.3 resolver, `4.3 prisoners, `5 the D-rows).
- [S9] `research_notes/GDD phase 5/arab-forces-1948.md` — Arab-force positions and `10 sourcing notes (research file; per-row fact-check before `-shareable`).
- [S10] `research_notes/M4 war mode/war-1948.md` — the MFA chronology (the armies entering on 15 May 1948; the truce and fighting-resumed dates), and the note's contested IDF-order date (signed 26 or 28 May, published 31 May 1948).
- [S11] Population identity/conversion, docs/gdd/training.md.
- [S12] [#385](https://github.com/eyalgolan/statecraft-israel/issues/385#issuecomment-6213042416) — owner, 16 Oct 2026: "the Egypt prisoner-return card carries 9 March 1949, the end of the main exchange (7–9 March 1949), when the prisoners were home" (sources: `research_notes/M4 war mode/war-1948.md` `9 item 8). Checked 2026-10-16.
- [S13] Later interview/supersession authority, docs/GDD.md.
- [S14] Numerical catalogue, docs/gdd/balance.md.

## Open questions

Unresolved principle and authoring decisions are maintained only in [the GDD](../GDD.md#deferred-decisions).
