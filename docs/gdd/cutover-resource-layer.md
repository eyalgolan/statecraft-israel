# Resources, immigration and upkeep

## Player story

I can see where Money, Food and Arms come from, what is committed, and why housing changes when the budget runs short.

## Rules

### Resource ownership

Money is Treasury at the [balance](balance.md#campaign-and-indicators) IL-million scale. Food and spare Arms are national stocks; cohesion/support use bounded displayed points with compatible fractional storage. Stocks cannot become negative. Attention is removed, including pricing, replenishment, refunds, refusals and UI—not retained as a zero-cost resource.

Only staffed job income, explicit event effects and [settlement-control](settlement-control.md) taxes create recurring Money. Disable passive national per-capita tax/spending and automatic borrowing. Migrate events assuming them; do not collect old and new income together. Historical cabinet labels do not create another budget model.

### Money and upkeep

Construction, tools/courses, imports and workshop inputs require full uncommitted Money upfront. Reject atomically when unaffordable. No automatic loans, unpaid purchase orders or hidden debt. Paid work does not buy inputs twice.

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

- Rules/scenario stock inventory, purchase shipments, manifests and one-time delivery ids.
- Economy, resolver and effects/UI adapters require migration.
- Remove Attention, automatic debt, passive budget and refill paths from the revised ruleset; preserve source records.
- Convert indicator points using the catalogue scale before clamping.

## Done when

- The ledger reconciles starting stocks, income/deliveries, production, expenditure and ending stocks.
- Start Food breaks even without refill; subsequent shortages use only approved effects.
- Cash shortfall changes only housing condition and its existing downstream effects.
- Imports have a pending turn and no delivery/manifest duplicates after reload.
- Transit charges once per unit and port-queue cohesion once nationally.

## Out of scope

Automatic borrowing, passive national budget simulation, enemy supply economy, funding-priority UI, naval interception and arrears repayment.

## Sources

- [S1] Economic decisions/provenance, docs/GDD.md.
- [S2] Numerical catalogue, docs/gdd/balance.md.
- [S3] Sensitive content constraints, docs/CONTENT_POLICY.md.

## Open questions

No additional balance decisions are required. Remaining principle and authoring decisions are listed only in [the GDD](../GDD.md#deferred-decisions).
