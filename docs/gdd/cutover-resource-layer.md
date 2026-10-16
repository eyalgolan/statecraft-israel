# The cutover and the resource layer

## Player story

As a player of the MVP, I want the real game to switch to the GDD loop with monthly turns and the GDD's five resources moving on screen, so that from the cutover on, the game I play is the MVP's game (the Phase 2 plan [S2]; GDD "MVP scope" [S1]).

## Rules

- The cutover sets `systems.loop: gdd` and monthly `turn_lengths` in the real `content/eras.yaml` and `content/rules.yaml`, so the real game runs `TurnResolver` and saves name their ruleset `gdd-0.1` (the plan's cutover PR [S2]). It starts after the first phase's remaining PRs merge (#382).
- The cutover re-pins every golden once; after it, every PR that changes an MVP rule re-pins the goldens it moves in the same PR and says why (the spec, §3 [S2]).
- The legacy loop is not kept runnable after the cutover (D-E1 [S2]), and old saves are refused with a clear message (D-E4 [S2]).
- Money is the Treasury, in IL millions, and every GDD cost and income moves it (the plan's Money PR [S2]). Debt and foreign currency stay internal, under Money, at least through the cutover, and are hidden if they confuse (D-C3, decided 15 October 2026 [S2]).
- A GDD cost above the Treasury is still paid and the shortfall moves to debt: the owner allowed borrowing for training and pioneer work, and the plan reads this as applying to every GDD cost (D-C3, decided 15 October 2026 [S2]; proposed R-MONEY-003; the plan's reading [S2]).
- Food and Arms are national pools, whole numbers (D-C2, decided 15 October 2026 [S2]). Arms comes from sourced arms-deal events and from Money, and training and attacks consume it (D-G6, decided 15 October 2026 [S2]).
- One unit of Food is one population unit's food for a month. The nation eats its head count in units each turn, and a monthly import keeps the stock at a set number of months of cover, except while foreign currency is exhausted, when nothing is imported. The stock never goes below zero, and the share left unmet is kept (`Q-POOL-2`, `Q-POOL-4` and `Q-POOL-5`, decided 15 October 2026 [S3]; R-FOOD-001 and R-FOOD-002).
- Food supply stays the zero-to-one level the screens and events read. It carries a term from the months of cover: none at a full stock, the most when the stock is empty, and the unmet share lowers it further; rationing and events still act on top (the maintainer's food rule, 16 October 2026 [S6]; R-FOOD-003). The two numbers, `k_food` and `cover_months`, are labelled design values.
- Social cohesion is the `morale` indicator on its zero-to-one scale, shown to the player as Social cohesion, its thresholds as situations, and International Support is the `intl_standing` indicator on its zero-to-one scale, under its player name (D-C1, decided 15 October 2026 [S2]; proposed R-NAT-001 and R-NAT-003).
- An event option's effects may target Money, Food, Arms, Social cohesion and International Support; any other target is a load error (proposed R-EFX-001 [S2]). An option tagged `holocaust`, `terror`, `massacre` or `war-casualties` cannot raise any of the five resources or foreign currency unless the same option carries a matching cost (the sensitive-option answer of 15 October 2026 [S2]; proposed R-EFX-002). The owner chose to add a real cost: the check is a net of zero or less across the five resources, with the cost falling on a different resource than the gain (owner, 15 October 2026 [S3]; the plan [S2]).
- The historical run has at most six months without any event, and at most two months in wartime, the full-war level of the war state counting as wartime (owner, 15 October 2026 [S3] and [S4]; the gap rule, docs/m3-rulings.md). Five war event cards are rewritten as decisions taken during or after the event, and the first truce and Rhodes cards are accepted as arriving late (`Q-CUT-3`, "Mixed" [S2]). The Altalena card opens on 15 June 1948 without the Kfar Vitkin clause, and the Ten Days card is rewritten (the owner on #567, recorded in the plan [S2]). Cards whose text changed go back to `fact-checked` until the owner re-reviews them (`Q-CUT-4`, decided 15 October 2026 [S3]).
- Every balance number lives in `content/`, tagged on its line as a design value, not a historical figure, and one test lists them all, so no number slips in untagged (the design-values answer of 15 October 2026 [S2]). The numbers themselves stay the owner's to set or accept in review.
- The cabinet and Attention stay; coalition is optional with no collapse; the budget is optional; policies are internal; diplomacy is deferred (D-C5 [S2]).

## Data touched

- `content/eras.yaml` and `content/rules.yaml`: `systems.loop: gdd` and monthly `turn_lengths` (the plan, PR 1 [S2]).
- `content/` balance lines: each tagged `# DESIGN VALUE: owner-set balance, not a historical figure` (Q-CUT-1 [S2]).
- `GameState`: the new Food and Arms pools; `GameState.Cohesion` and its hash line go as cohesion becomes `morale` under the player name (the plan, PR 4 [S2]).
- `model.yaml`: player names and the effects web for the two national indicators (the plan, PR 4 [S2]).
- The event-effect loader: the five allowed targets (proposed R-EFX-001 [S2]).
- The HUD: the existing meters stay (`Q-HUD-1`, decided 15 October 2026 [S3]); the five resources are shown, and approval appears as Public support (`Q-HUD-2`, decided 15 October 2026 [S3]). The Hebrew words are כסף, מזון, נשק and תמיכה בינלאומית, with Social cohesion staying לכידות חברתית (`Q-HUD-3` and `Q-MONEY-4`, decided 15 October 2026 [S4]).

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, or the phase's plan.

## Out of scope

- Upkeep: none until the third phase, then the values enter `content/` as labelled design values (`Q-MONEY-2`, decided 15 October 2026 [S3]; D-C6 [S2]).
- The sensitive options' real costs: the Altalena, Bernadotte and reparations options each get one (owner, 15 October 2026 [S3]); each amount is a labelled design value set in `#577`; the reparations claim's cost falls on Money (owner, 15 October 2026 [S4]).
- D-B3 and D-B4: moved to Phases 3 and 4 (owner, 15 October 2026 [S5]).
- The war objective, prisoners and the ceasefire clock (Phase 5).
- Defeat: built in the final phase, with the counter set then (`Q-GOV-2`, decided 15 October 2026 [S3]); what "Money exhausted" means now that borrowing is allowed stays open.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md, with the owner's answers of 15 October 2026 recorded by #575; and the Phase 2 plan, docs/superpowers/plans/2026-10-14-gdd-phase-2.md
- [S3] The owner's phase answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15
- [S4] The owner's decisions recorded on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183538544, checked 2026-10-15
- [S5] The owner's answers on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6178976670, checked 2026-10-15
- [S6] The maintainer's food rule on #382, 16 October 2026 (option (b)), https://github.com/eyalgolan/statecraft-israel/issues/382#issuecomment-6213237277, checked 2026-10-16

## Open questions

- No Phase 2 plan question is open (owner, 15 October 2026 [S3] and [S4]); the plan's table records the decided rows (`Q-CUT-4` to `Q-CUT-8`, `Q-POOL-2/4/5`, `Q-NAT-2/3/5`, `Q-EFX-1/3`, `Q-GOV-1/2`, `Q-HUD-1` to `Q-HUD-3`, `Q-MONEY-2` and `Q-MONEY-4`) [S2], `Q-HUD-3` and `Q-MONEY-4` included, as merged by #604.
- D-C6: the balance values themselves. Each enters `content/` as a labelled design value for the owner to set or accept in review (decided 15 October 2026 [S2]).
- D-B3 (the railway card's reference date) and D-B4: open, moved to Phases 3 and 4 [S5].
