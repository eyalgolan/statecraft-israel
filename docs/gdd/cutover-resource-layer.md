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

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed 3 starting spare Arms, 10 Money per imported Arms and a separate 10,000-people population scale.* (unitless) [S1]
- Social cohesion is the `morale` indicator on its zero-to-one scale, shown to the player as Social cohesion, its thresholds as situations, and International Support is the `intl_standing` indicator on its zero-to-one scale, under its player name (D-C1, decided 15 October 2026 [S2]; proposed R-NAT-001 and R-NAT-003).
- An event option's effects may target Money, Food, Arms, Social cohesion and International Support; any other target is a load error (proposed R-EFX-001 [S2]). An option tagged `holocaust`, `terror`, `massacre` or `war-casualties` cannot raise any of the five resources or foreign currency unless the same option carries a matching cost (the sensitive-option answer of 15 October 2026 [S2]; proposed R-EFX-002). The owner chose to add a real cost: the check is a net of zero or less across the five resources, with the cost falling on a different resource than the gain (owner, 15 October 2026 [S3]; the plan [S2]).
- The historical run has at most six months without any event, and at most two months in wartime, the full-war level of the war state counting as wartime (owner, 15 October 2026 [S3] and [S4]; the gap rule, docs/m3-rulings.md). Five war event cards are rewritten as decisions taken during or after the event, and the first truce and Rhodes cards are accepted as arriving late (`Q-CUT-3`, "Mixed" [S2]). The Altalena card opens on 15 June 1948 without the Kfar Vitkin clause, and the Ten Days card is rewritten (the owner on #567, recorded in the plan [S2]). Cards whose text changed go back to `fact-checked` until the owner re-reviews them (`Q-CUT-4`, decided 15 October 2026 [S3]).
- Every balance number lives in `content/`, tagged on its line as a design value, not a historical figure, and one test lists them all, so no number slips in untagged (the design-values answer of 15 October 2026 [S2]). The numbers themselves stay the owner's to set or accept in review.
- The cabinet and Attention stay; coalition is optional with no collapse; the budget is optional; policies are internal; diplomacy is deferred (D-C5 [S2]).

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed removing Attention, requiring full Money upfront without borrowing, and removing automatic monthly Food import/refill.* (unitless) [S1]
### Resource ownership

Under Amir's proposal, Money is Treasury, Food and spare Arms are national stocks, and cohesion/support use bounded displayed points with compatible fractional storage. Stocks cannot become negative.

Only staffed job income, explicit event effects and [settlement-control](settlement-control.md) taxes create recurring Money. Disable passive national per-capita tax/spending and automatic borrowing. Migrate events assuming them; do not collect old and new income together. Historical cabinet labels do not create another budget model.

### Money and upkeep

Under Amir's proposal, construction, tools/courses, imports and workshop inputs require full uncommitted Money upfront and reject atomically when unaffordable. Paid work does not buy inputs twice.

At settlement collect permitted income/effects, then total active-army upkeep, civilian transit, facility upkeep and condition costs. Finished facilities owe upkeep from completion, even vacant. Teachers/basic trainees still eat; basic trainees owe no active-army Money upkeep.

Automatically pay non-housing mandatory expenses first, limited by Money; prorate remaining Money across regional housing upkeep due. No funding-priority UI is added. If even non-housing expenses exceed Money, Money floors and no extra penalty or arrears ledger appears. Housing deterioration is the only upkeep-shortfall consequence.

For regional housing upkeep due `H_r` and paid `h_r`, lose `housing.max_deterioration * (H_r - h_r) / H_r` condition pp, clamped. No due upkeep means no maintenance deterioration. Do not automatically charge repairs or create repayment obligations. Price condition Money costs from pre-deterioration condition, then deteriorate and apply condition-to-cohesion once.

Unpaid Money adds no military effectiveness loss, shutdown or direct cohesion hit. The existing [housing chain](cities.md#housing-and-condition) and separate Food/queue effects remain.

### Food

Every own unit consumes `population.food_per_turn` regardless of status/work/service/transit. Count once nationally; camp/city subcounts add no extra meal. Enemy residents are outside demand. Held enemy combatants add separately accounted provisions under [combat](military-combat.md#prisoners).

Fields produce before consumption. Initial break-even is specific to the authored start; arrivals/role changes do not create automatic Food or refill. Purchases and reviewed explicit effects are the only import/grant sources.

Let `need` be own Food plus held-enemy-combatant provisions and `available` stock after due deliveries/production. Allocate proportionally so demands have the same unmet share; own captured units already belong to own demand and are not added again. Consume available stock to its floor and set `q = max(0, need - available) / need`, zero for no need. Cohesion loss is `food.shortage_cohesion_max * q` points; next-turn own strength multiplier is `max(0, 1 - food.shortage_combat_slope * q)`. Replace the previous shortage modifier rather than permanently stacking. No automatic deaths or worker-output penalty occurs.

Legacy `food_supply`/effects interfaces may be readouts during migration but cannot refill stocks or duplicate penalties. [Event/indicator integration](ministers-events-end.md) owns compatibility.

### Purchases and equipment

Buy Arms/Food at [catalogue](balance.md#imports-immigration-and-arms-production) price/yield and delivery delay. Pay on ordering; delivery is at the next turn's opening after the ordered turn resolves, never current consumption. Show pending arrival and an authored friendly port. No naval-interception/blockade system is added.

An Arms item is the reusable equipment package for a population-unit military cohort, not an individual rifle or ammunition. Issue removes spare stock; valid demobilisation returns equipment once. Attacks/artillery spend none. Already equipped units pay no second issue. An arriving trained soldier's accompanying equipment is explicitly declared and goes onto that unit, never also into spare stock. Destruction/surrender gives no automatic refund.

### Immigration and arrival queues

Use [balance](balance.md#imports-immigration-and-arms-production) ship cadence, cohort size and mix. Initial schedule places ships on interval boundaries; a stable repeating manifest expresses the mix across passenger slots with ordinal ids and no new RNG. It is a game schedule, not a replacement claim about the CBS series; the latter remains reference data and must not also add the same people.

A ship adds people once per manifest id at a friendly port. Most are refugees; other entries are free civilians, apprentice craftsmen or trained soldiers. Refugees cannot work until [reception](training.md#reception-camps). Qualified non-refugee entries explicitly have completed reception in their manifests.

Assign available reception beds in stable unit/camp order. Without a bed, refugees wait in port without reception credit, retaining the military-training alternative. Free arrivals may move to friendly regional housing; arrival creates no houses/jobs.

Every civilian ending a turn in transit pays `transit.cost` plus normal Food, counted once. This includes port queues and destinationless travel/evacuation. If any refugee awaits a reception bed at any port, lose `reception.queue_cohesion_loss` once nationally, not per unit or port. New arrivals start reception/ordinary consumption on the next resolved turn, without retroactive work or meals.

### Sensitive effects and accounting

Retain [CONTENT_POLICY](../CONTENT_POLICY.md) prohibitions on economic/score rewards for civilian suffering. Validate allowed event targets/tags and apply each effect once. Governance taxes are not loot or a labour mechanic. This documentation does not fact-check or sensitivity-approve changed content.

## Data touched

- `content/eras.yaml` and `content/rules.yaml`: `systems.loop: gdd` and monthly `turn_lengths` (the plan, PR 1 [S2]).
- `content/` balance lines: each tagged `# DESIGN VALUE: owner-set balance, not a historical figure` (Q-CUT-1 [S2]).
- `GameState`: the new Food and Arms pools; `GameState.Cohesion` and its hash line go as cohesion becomes `morale` under the player name (the plan, PR 4 [S2]).
- `model.yaml`: player names and the effects web for the two national indicators (the plan, PR 4 [S2]).
- The event-effect loader: the five allowed targets (proposed R-EFX-001 [S2]).
- The HUD: the existing meters stay (`Q-HUD-1`, decided 15 October 2026 [S3]); the five resources are shown, and approval appears as Public support (`Q-HUD-2`, decided 15 October 2026 [S3]). The Hebrew words are כסף, מזון, נשק and תמיכה בינלאומית, with Social cohesion staying לכידות חברתית (`Q-HUD-3` and `Q-MONEY-4`, decided 15 October 2026 [S4]).
- Rules/scenario stock inventory, purchase shipments, manifests and one-time delivery ids.
- Economy, resolver and effects/UI adapters require migration.
- Remove Attention, automatic debt, passive budget and refill paths from the revised ruleset; preserve source records.
- Convert indicator points using the catalogue scale before clamping.

## Done when

- This file passes `uv run scripts/oss/gdd_lint.py`.
- Every rule above names its source: a decided row of #375, the GDD, or the phase's plan.
- The ledger reconciles starting stocks, income/deliveries, production, expenditure and ending stocks.
- Start Food breaks even without refill; subsequent shortages use only approved effects.
- Cash shortfall changes only housing condition and its existing downstream effects.
- Imports have a pending turn and no delivery/manifest duplicates after reload.
- Transit charges once per unit and port-queue cohesion once nationally.

## Out of scope

- Upkeep: none until the third phase, then the values enter `content/` as labelled design values (`Q-MONEY-2`, decided 15 October 2026 [S3]; D-C6 [S2]).
- The sensitive options' real costs: the Altalena, Bernadotte and reparations options each get one (owner, 15 October 2026 [S3]); each amount is a labelled design value set in `#577`; the reparations claim's cost falls on Money (owner, 15 October 2026 [S4]).
- D-B3 and D-B4: moved to Phases 3 and 4 (owner, 15 October 2026 [S5]).
- The war objective, prisoners and the ceasefire clock (Phase 5).
- Defeat: built in the final phase, with the counter set then (`Q-GOV-2`, decided 15 October 2026 [S3]); what "Money exhausted" means now that borrowing is allowed stays open.
Automatic borrowing, passive national budget simulation, enemy supply economy, funding-priority UI, naval interception and arrears repayment.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md, with the owner's answers of 15 October 2026 recorded by #575; and the Phase 2 plan, docs/superpowers/plans/2026-10-14-gdd-phase-2.md
- [S3] The owner's phase answers announced on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6180592337, checked 2026-10-15
- [S4] The owner's decisions recorded on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6183538544, checked 2026-10-15
- [S5] The owner's answers on #375, 15 October 2026, https://github.com/eyalgolan/statecraft-israel/issues/375#issuecomment-6178976670, checked 2026-10-15
- [S6] The maintainer's food rule on #382, 16 October 2026 (option (b)), https://github.com/eyalgolan/statecraft-israel/issues/382#issuecomment-6213237277, checked 2026-10-16
- [S7] The owner's decision on #781, 18 October 2026, "Approve as drafted", https://github.com/eyalgolan/statecraft-israel/issues/781#issuecomment-6251995002, checked 2026-10-18
- [S8] The owner's decision on #782, 17 October 2026 (the railway and its card date, and the required yishuvim of D-B4), https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128, checked 2026-10-21
- [S1] Economic decisions/provenance, docs/GDD.md.
- [S2] Numerical catalogue, docs/gdd/balance.md.
- [S3] Sensitive content constraints, docs/CONTENT_POLICY.md.

## Open questions

- No Phase 2 plan question is open (owner, 15 October 2026 [S3] and [S4]); the plan's table records the decided rows (`Q-CUT-4` to `Q-CUT-8`, `Q-POOL-2/4/5`, `Q-NAT-2/3/5`, `Q-EFX-1/3`, `Q-GOV-1/2`, `Q-HUD-1` to `Q-HUD-3`, `Q-MONEY-2` and `Q-MONEY-4`) [S2], `Q-HUD-3` and `Q-MONEY-4` included, as merged by #604.
- D-C6: the balance values themselves. Each enters `content/` as a labelled design value for the owner to set or accept in review (decided 15 October 2026 [S2]).
- D-B3 and D-B4 are no longer open. D-B3, the railway card's reference date: 20 September 1949 (owner, 17 October 2026 [S8]). D-B4: the immigrants part decided 15 October 2026 and the required yishuvim 17 October 2026 (#782 [S8]), as the plan's table records [S2].
No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
