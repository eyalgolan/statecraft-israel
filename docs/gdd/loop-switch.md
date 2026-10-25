# Game loop, deterministic resolution and saves

## Player story

I plan standing orders, resolve the month predictably and reload under the rules that produced the save.

## Rules

### Ruleset boundary

This design targets `gdd-0.3`. Existing `legacy`/`gdd-0.1` code/saves are implementation history, not alternative rules within a new run. Save ruleset, content hash and schema. Use schema `3` for the new population/region/facility state (unitless version). Refuse earlier incompatible rulesets with the earlier-rules reason and unknown later rulesets/schemas with the newer-version reason. Never replay old commands through changed scales/conversion/economics. Older replay support stays within its own implementation.

Standing orders persist until changed/completed. Commands only mutate validated assignments, commitments and orders, without Attention. Validate all constraints before payment/mutation and refuse atomically with localisable reasons. Direct movement uses turn allowance, not real-time travel.

### Turn boundaries and resolution

Apply due purchase shipments once at opening, before planning. They cannot supply their ordered turn's consumption. Snapshot monthly work/course assignments so graduation cannot give another full assignment's output that turn. Units/facilities lost before work cannot produce through stale snapshots.

Resolve in stable-id order:

1. Enemy orders/scripts.
2. Planned player military movement/combat, respecting the combat owner's prescribed draw count.
3. Eligible captures/control changes, automatic evacuation and released assignments.
4. Existing courses and reception, completing qualifications without assigning another job.
5. Staffed city/pioneer work and completions; new capacity is usable next planning phase.
6. Staffed fields, Industry and workshops produce; actual-work XP follows current output.
7. Consume Food for own people/prisoners present during the resolved month and record unmet share. Prepare Money income/expenses for a single final settlement.
8. Add immigration manifests and place arrivals in legal housing or queues, without retroactive work, courses, reception credit or meals.
9. Finalise Money once, including end-turn civilian transit. Apply housing-only shortfall allocation/deterioration, overcrowding and the defined housing/Food/queue cohesion effects once. Use pre-deterioration condition for the surcharge.
10. Advance occupation timers for garrisons still present.
11. Evaluate objectives/cards/score, finalise end date and apply campaign boundary.

Arrivals precede final housing/queue settlement to avoid another Money/deterioration pass. Food uses the resolved month's population; arrivals join next turn's demand. New arrivals did not spend the preceding month overcrowded: use pre-arrival residents for overcrowding deterioration and final residents for end-turn summaries. Purchase shipments and passenger manifests are distinct.

Commit indicator changes in one bounded final update, without duplicate legacy shortage/housing effects. Preserve historical hard/soft event windows and established delegation; explicit event population/resource grants use the same one-time ledger.

### Determinism and state

Use exact arithmetic, fixed neighbour order, ordinal ids and existing RNG. Manifests, evacuation, XP and completion cards add no RNG. Hash population/status/assignments/XP/equipment, regions/condition, facilities/work, shipments/manifests, occupations/regimes, captives and awards. Empty compatibility sections must not accidentally drift earlier-rule hashes.

Do not claim old goldens hold under revised rules. Implementation must migrate validation/content/saves/commands/reference-player tests together and explain intended hash changes. Documentation alone neither implements this nor regenerates goldens.

## Data touched

- Simulation/resolver, commands, save parser/format, canonical state and reference scenarios.
- This documentation does not edit current schema/ruleset code.
- Shared snapshots prevent duplicate work, courses, meals or arrivals.

## Done when

- Replay/save round trips match state/draw count under identical rules/content.
- Earlier incompatible saves refuse clearly.
- Course completion, new facilities and arrivals cannot receive another month's output in one resolution.
- Final expenses/condition commit once, including transit and excluding retroactive Food.

## Out of scope

Claiming current code implements the new resolver, retaining old balance inside the new ruleset and adding chapter checkpoints.

## Sources

- [S1] Authority, docs/GDD.md.
- [S2] Determinism, docs/determinism.md.
- [S3] Catalogue, docs/gdd/balance.md.

## Open questions

See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
