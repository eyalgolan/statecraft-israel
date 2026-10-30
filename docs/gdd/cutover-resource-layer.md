# The cutover and the resource layer

## Player story

As a player of the MVP, I want the real game to switch to the GDD loop with monthly turns and the GDD's five resources moving on screen, so that from the cutover on, the game I play is the MVP's game (the Phase 2 plan [S2]; GDD "MVP scope" [S1]).
I can see where Money, Food and Arms come from, what is committed, and why housing changes when the budget runs short.

## Rules

- The cutover sets `systems.loop: gdd` and monthly `turn_lengths` in the real `content/eras.yaml` and `content/rules.yaml`, so the real game runs `TurnResolver` and saves name their ruleset `gdd-0.1` (the plan's cutover PR [S2]). It starts after the first phase's remaining PRs merge (#382).
- The cutover re-pins every golden once; after it, every PR that changes an MVP rule re-pins the goldens it moves in the same PR and says why (the spec, §3 [S2]).
- The legacy loop is not kept runnable after the cutover (D-E1 [S2]), and old saves are refused with a clear message (D-E4 [S2]).
- Money is the Treasury, in IL millions, and every GDD cost and income moves it (the plan's Money PR [S2]). Debt and foreign currency stay internal, under Money, at least through the cutover, and are hidden if they confuse (D-C3, decided 15 October 2026 [S2]).
- A GDD cost above the Treasury is still paid and the shortfall moves to debt: the owner allowed borrowing for training and pioneer work, and the plan reads this as applying to every GDD cost (D-C3, decided 15 October 2026 [S2]; proposed R-MONEY-003; the plan's reading [S2]).
- Food and Arms are national pools, whole numbers (D-C2, decided 15 October 2026 [S2]). Arms comes from sourced arms-deal events and from Money, and training and attacks consume it (D-G6, decided 15 October 2026 [S2]).
- One unit of Food is one population unit's food for a month. The nation eats its head count in units each turn, and a monthly import keeps the stock at a set number of months of cover, except while foreign currency is exhausted, when nothing is imported. The stock never goes below zero, and the share left unmet is kept (`Q-POOL-2`, `Q-POOL-4` and `Q-POOL-5`, decided 15 October 2026 [S3]; R-FOOD-001 and R-FOOD-002).
- Food supply stays the zero-to-one level the screens and events read. It carries a term from the months of cover: none at a full stock, the most when the stock is empty, and the unmet share lowers it further; rationing and events still act on top (the maintainer's food rule, 16 October 2026 [S6]; R-FOOD-003). The two numbers, `k_food` and `cover_months`, are labelled design values.
- One unit of Arms is the equipment package for a recruit group of 1,000 people. The stock starts at 12 units, a `BuyArms` order adds units at IL 1m each and borrows a short Treasury, and recruit training takes 1 unit a group (R-ARMS-001 to R-ARMS-003). These numbers and the buy order's cost of 1 attention are labelled design values the owner approved [S7].

*Amir's #1668 proposed 3 starting spare Arms and 10 Money per imported Arms. Both are Agreed in main's `gdd-0.3` catalogue (`content/gdd03-balance.yaml`: `start.spare_arms`, `import.arms_price`). For the starting stock, the owner's 30 October 2026 conflict review did not rule on this ([conflict decision](../GDD.md#30-october-2026-conflict-decisions)); the live rules are `R-ARMS-001` and `R-ARMS-002` (`docs/rules/mvp.md:417` and `:419`).* (unitless) [S1]
- Social cohesion is the `morale` indicator on its zero-to-one scale, shown to the player as Social cohesion, its thresholds as situations, and International Support is the `intl_standing` indicator on its zero-to-one scale, under its player name (D-C1, decided 15 October 2026 [S2]; proposed R-NAT-001 and R-NAT-003).
- An event option's effects may target Money, Food, Arms, Social cohesion and International Support; any other target is a load error (proposed R-EFX-001 [S2]). An option tagged `holocaust`, `terror`, `massacre` or `war-casualties` cannot raise any of the five resources or foreign currency unless the same option carries a matching cost (the sensitive-option answer of 15 October 2026 [S2]; proposed R-EFX-002). The owner chose to add a real cost: the check is a net of zero or less across the five resources, with the cost falling on a different resource than the gain (owner, 15 October 2026 [S3]; the plan [S2]).
- The historical run has at most six months without any event, and at most two months in wartime, the full-war level of the war state counting as wartime (owner, 15 October 2026 [S3] and [S4]; the gap rule, docs/m3-rulings.md). Five war event cards are rewritten as decisions taken during or after the event, and the first truce and Rhodes cards are accepted as arriving late (`Q-CUT-3`, "Mixed" [S2]). The Altalena card opens on 15 June 1948 without the Kfar Vitkin clause, and the Ten Days card is rewritten (the owner on #567, recorded in the plan [S2]). Cards whose text changed go back to `fact-checked` until the owner re-reviews them (`Q-CUT-4`, decided 15 October 2026 [S3]).
- Every balance number lives in `content/`, tagged on its line as a design value, not a historical figure, and one test lists them all, so no number slips in untagged (the design-values answer of 15 October 2026 [S2]). The numbers themselves stay the owner's to set or accept in review.
- The cabinet and Attention stay; coalition is optional with no collapse; the budget is optional; policies are internal; diplomacy is deferred (D-C5 [S2]).

*Amir's #1668 proposed requiring full Money upfront without borrowing and removing automatic monthly Food import/refill; the owner's 30 October 2026 conflict review did not rule on this ([conflict decision](../GDD.md#30-october-2026-conflict-decisions)). The operative main rules are `R-MONEY-003` and `R-FOOD-002` (`docs/rules/mvp.md:399` and `:409`). Its removal of Attention is not declined: under `gdd-0.3` there is no Attention resource (`R-GOV-002`).* (unitless) [S1]
### Resource ownership

**Amir's proposal [S9]:** the resource layer would be restated around the proposal catalogue [S10]. Catalogue mechanics that are option B of a 30 October 2026 conflict (Upkeep, Immigration source, Regional pooling) are declined and non-operative; for the others the review did not rule, and each note below names the operative rule.

**Amir's proposal [S1]:** only staffed job income, explicit event effects and settlement-control taxes would create recurring Money, while automatic borrowing would be disabled. The owner's 30 October 2026 conflict review did not rule on borrowing or recurring income; the operative main rules are `R-CITY-002` and `R-MONEY-003` (`docs/rules/mvp.md:95` and `:399`).

### Money and upkeep

**Amir's proposal [S1]:** construction, tools/courses, imports and workshop inputs would require full uncommitted Money upfront. The owner's 30 October 2026 conflict review did not rule on this; the operative main rule is borrowing under `R-MONEY-003` (`docs/rules/mvp.md:399`).

**Amir's proposal, declined by the owner on 30 October 2026 (Upkeep conflict, option A) [S1]:** proposed facility upkeep is non-operative. For his settlement ordering, transit cost and active-army upkeep, the owner's 30 October 2026 conflict review did not rule on this; `R-MONEY-002` (`docs/rules/mvp.md:397`) stays operative, `transit.cost` and `military.active_money` are listed in `content/gdd03-balance.yaml` (basis Agreed, transcribed from #1668), and main has no live transit charge (`open (owner)`).

The proposed non-housing-first payment and regional housing-proration rule is part of that declined proposal and is non-operative.

The proposed regional housing-upkeep formula and its settlement ordering are part of that declined proposal and are non-operative.

Money and Food exhaustion remain defeat triggers under [the retained end rules](ministers-events-end.md#rules). For Amir's proposal to limit unpaid Money to housing effects, the owner's 30 October 2026 conflict review did not rule on this [S1]; borrowing under `R-MONEY-003` (`docs/rules/mvp.md:399`) stays operative.

### Food

Every own unit consumes Food under the retained national-pool rule. Enemy residents remain separate from player demand. Prisoner provisions and shortfall effects retain their existing owner-decided/open boundaries in [combat](military-combat.md#prisoners).

**Amir's proposal [S1]:** arrivals and role changes would not create automatic Food or refill, and purchases/reviewed effects would be the only import/grant sources. The owner's 30 October 2026 conflict review did not rule on this; the operative main rule is the automatic monthly import of `R-FOOD-002` (`docs/rules/mvp.md:409`).

**Amir's proposal [S1]:** own and prisoner Food would be allocated proportionally under a new shortage formula. The owner's 30 October 2026 conflict review did not rule on this; `R-FOOD-002` and `R-POW-002` (`docs/rules/mvp.md:409`, `docs/rules/mvp.md:361`) stay operative. The retained prisoner rule keeps its provision and shortfall amounts open (owner).

Legacy `food_supply`/effects interfaces may be readouts during migration. The retained automatic monthly import rule remains authoritative, and migration must not duplicate its refill or penalties. [Event/indicator integration](ministers-events-end.md) owns compatibility.

### Purchases and equipment

`BuyArms(count)` adds the ordered whole Arms units immediately, with no delivery delay, at IL 1m per Arms; it pays on ordering and borrows a short Treasury (`R-ARMS-002`) [S7]. Food continues to follow the retained automatic monthly import rule above. Amir's catalogue Arms price matches `import.arms_price`, Agreed in `content/gdd03-balance.yaml`; his next-turn delivery, pending shipment and authored-port requirement are non-operative [S1]. No naval-interception/blockade system is added.

Amir's proposal that Arms be reusable equipment, with attacks and artillery spending none, agrees with the retained `gdd-0.3` rule (`R-COMBAT-004`, owner ruling, 25 October 2026); the owner's 30 October 2026 review lists Arms as not a conflict [S1]. Training still consumes Arms (`R-ARMS-003`).

### Immigration and arrival queues

**Amir's proposal, declined by the owner on 30 October 2026 (Immigration source and Regional pooling conflicts, option A) [S1]:** immigration would use the proposed manifest and regional destinations. Those mechanics are non-operative; retained arrivals come from the CBS series and become civilians on the next housing step (`R-CITY-003`, `R-CITY-004`). For his reception-bed/port-queue pipeline and per-unit transit charge, the 30 October 2026 conflict review did not rule; main has only an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`) and no transit charge (`open (owner)`).

### Sensitive effects and accounting

Retain [CONTENT_POLICY](../CONTENT_POLICY.md) prohibitions on economic/score rewards for civilian suffering [S11]. Validate allowed event targets/tags and apply each effect once. Governance taxes are not loot or a labour mechanic. This documentation does not fact-check or sensitivity-approve changed content.

## Data touched

- `content/eras.yaml` and `content/rules.yaml`: `systems.loop: gdd` and monthly `turn_lengths` (the plan, PR 1 [S2]).
- `content/` balance lines: each tagged `# DESIGN VALUE: owner-set balance, not a historical figure` (Q-CUT-1 [S2]).
- `GameState`: the new Food and Arms pools; `GameState.Cohesion` and its hash line go as cohesion becomes `morale` under the player name (the plan, PR 4 [S2]).
- `model.yaml`: player names and the effects web for the two national indicators (the plan, PR 4 [S2]).
- The event-effect loader: the five allowed targets (proposed R-EFX-001 [S2]).
- The HUD: the existing meters stay (`Q-HUD-1`, decided 15 October 2026 [S3]); the five resources are shown, and approval appears as Public support (`Q-HUD-2`, decided 15 October 2026 [S3]). The Hebrew words are כסף, מזון, נשק and תמיכה בינלאומית, with Social cohesion staying לכידות חברתית (`Q-HUD-3` and `Q-MONEY-4`, decided 15 October 2026 [S4]).
- Rules/scenario stock inventory, purchase shipments and one-time delivery ids.
- Economy, resolver and effects/UI adapters require migration.
- **Amir's proposal [S1]:** remove automatic debt, passive budget and refill paths. The owner's 30 October 2026 conflict review did not rule on this; `R-MONEY-003` and `R-FOOD-002` (`docs/rules/mvp.md:399` and `:409`) stay operative.
- Convert indicator points using the catalogue scale before clamping.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, or the phase's plan.
- The ledger reconciles starting stocks, income/deliveries, production, expenditure and ending stocks.
- **Amir's proposal [S1]:** start Food would break even without refill and subsequent shortages would use only proposed effects. The owner's 30 October 2026 conflict review did not rule on this; `R-FOOD-002` (`docs/rules/mvp.md:409`) stays operative.
- **Amir's proposal [S1]:** cash shortfall would change only housing condition and its downstream effects; the owner's 30 October 2026 conflict review did not rule on this, and `R-MONEY-003` stays operative.
- Arms purchases arrive at once (`R-ARMS-002`) and the Food import refills in the same turn (`R-FOOD-002`); a reload duplicates no delivery. Amir's proposed pending import turn is non-operative [S1].
- **Amir's proposal [S1]:** transit would charge once per unit and port queues would reduce cohesion once nationally. The owner's 30 October 2026 conflict review did not rule on this; `content/gdd03-balance.yaml` lists `transit.cost` and `reception.queue_cohesion_loss` (basis Agreed, transcribed from #1668), and main has no live transit charge or port queue (`open (owner)`).

## Out of scope

- Upkeep: none until the third phase, then the values enter `content/` as labelled design values (`Q-MONEY-2`, decided 15 October 2026 [S3]; D-C6 [S2]).
- The sensitive options' real costs: the Altalena, Bernadotte and reparations options each get one (owner, 15 October 2026 [S3]); each amount is a labelled design value set in `#577`; the reparations claim's cost falls on Money (owner, 15 October 2026 [S4]).
- D-B3 and D-B4: moved to Phases 3 and 4 (owner, 15 October 2026 [S5]).
- The war objective, prisoners and the ceasefire clock (Phase 5).
- Defeat: built in the final phase, with the counter set then (`Q-GOV-2`, decided 15 October 2026 [S3]); what "Money exhausted" means now that borrowing is allowed stays open.
Enemy supply economy, funding-priority UI, naval interception and arrears repayment. Automatic borrowing and the retained budget behavior remain in scope under the owner rulings above.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md, with the owner's answers of 15 October 2026 recorded by #575; and the Phase 2 plan, docs/superpowers/plans/2026-10-14-gdd-phase-2.md
- [S3] The owner's phase answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15
- [S4] The owner's decisions recorded on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183538544, checked 2026-10-15
- [S5] The owner's answers on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6178976670, checked 2026-10-15
- [S6] The maintainer's food rule on #382, 16 October 2026 (option (b)), https://github.com/eyalgolan/statecraft-israel/issues/382#issuecomment-6213237277, checked 2026-10-16
- [S7] The owner's decision on #781, 18 October 2026, "Approve as drafted", https://github.com/eyalgolan/statecraft-israel/issues/781#issuecomment-6251995002, checked 2026-10-18
- [S8] The owner's decision on #782, 17 October 2026 (the railway and its card date, and the required yishuvim of D-B4), https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128, checked 2026-10-21
- [S9] Economic decisions/provenance, docs/GDD.md.
- [S10] Numerical catalogue, docs/gdd/balance.md.
- [S11] Sensitive content constraints, docs/CONTENT_POLICY.md.

## Open questions

- No Phase 2 plan question is open (owner, 15 October 2026 [S3] and [S4]); the plan's table records the decided rows (`Q-CUT-4` to `Q-CUT-8`, `Q-POOL-2/4/5`, `Q-NAT-2/3/5`, `Q-EFX-1/3`, `Q-GOV-1/2`, `Q-HUD-1` to `Q-HUD-3`, `Q-MONEY-2` and `Q-MONEY-4`) [S2], `Q-HUD-3` and `Q-MONEY-4` included, as merged by #604.
- D-C6: the balance values themselves. Each enters `content/` as a labelled design value for the owner to set or accept in review (decided 15 October 2026 [S2]).
- D-B3 and D-B4 are no longer open. D-B3, the railway card's reference date: 20 September 1949 (owner, 17 October 2026 [S8]). D-B4: the immigrants part decided 15 October 2026 and the required yishuvim 17 October 2026 (#782 [S8]), as the plan's table records [S2].
No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
