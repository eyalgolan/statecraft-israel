# The GDD loop switch

## Player story

As the maintainer, I want the GDD v0.1 loop built behind one switch, so that the real game and its goldens do not change until the cutover turns it on, and a save never replays under rules it was not played with [S1].
I plan standing orders, resolve the month predictably and reload under the rules that produced the save.

## Rules

- `systems.loop` in eras.yaml is `legacy` or `gdd`, the same in every era; absent means legacy (R-LOOP-001).
- The GDD state (cities, units, the next unit id, completed objectives, the score) is hashed only when it is set, so the legacy hash text is unchanged (R-LOOP-002).
- Until the cutover, `MvpSlice.Overlay` gave the real content with the switch on and monthly turns. The cutover turned the switch on in the real content and retired the overlay with its rule ID.
- A save names its ruleset, `legacy` or `gdd-0.1`; a game refuses a save from another ruleset (R-SAVE-001, decision D-E4 [S2]), and a save from a ruleset it does not know as made by a newer version (R-SAVE-002).
- Before the cutover, balance values the owner had not set lived only in the overlay or in test fixtures [S3]; since it they are labelled design values in `content/` (R-LOOP-005).
### Ruleset boundary

This design targets `gdd-0.3`. Existing `legacy`/`gdd-0.1` code/saves are implementation history, not alternative rules within a new run. Save ruleset, content hash and schema. Refuse incompatible earlier rulesets with the earlier-rules reason and unknown later rulesets/schemas with the newer-version reason. Never replay old commands through changed scales or economics. A schema number remains implementation work; declined regional state (Regional pooling conflict) is not encoded, and for reception state the 30 October 2026 conflict review did not rule (main has only an inactive `gdd-0.3` reception fixture, `docs/rules/mvp.md:107`) [S6]; on 1 November 2026 the owner kept `R-CITY-004` ("Keep R-CITY-004 (Recommended)"), so reception state is post-MVP [S7].

Amir's proposal that commands and standing orders operate without Attention agrees with the retained `gdd-0.3` rule: there is no Attention resource (`R-GOV-002`, owner ruling, 25 October 2026; [rules](../rules/mvp.md#government)). It was not one of the 30 October 2026 conflicts [S4].

### Turn boundaries and resolution

Apply retained due deliveries once at opening, before planning. Snapshot retained courses and projects so completion cannot produce a second turn's output. Units or facilities lost before retained work cannot produce through stale snapshots.

Resolve in stable-id order:

1. Enemy orders/scripts.
2. **Amir's proposal, declined by the owner on 30 October 2026 [S4]:** planned player military movement/combat would resolve here. The retained immediate Move and Attack rule controls.
3. Eligible captures/control changes and released assignments. The proposed automatic evacuation is post-MVP [S4]; while an enemy unit holds an Israeli city's hex, that city's jobs pay nothing and its Food production and projects pause until it is retaken (owner, 1 November 2026, D6) [S7].
4. Existing retained courses complete qualifications without assigning another job.
5. Retained fixed-time city/pioneer projects and completions resolve; new capacity is usable next planning phase.
6. Existing city production resolves.
7. Resolve retained housing and Social cohesion changes.
8. Consume Food for own people and apply retained prisoner provisions; prepare retained Money income/expenses.
9. Add retained immigration arrivals once, without retroactive work or meals.
10. Finalise Money under the retained borrowing, budget and defeat rules.
11. Advance retained occupation timers.
12. Evaluate objectives/cards/score, finalise end date and apply campaign boundary.

**Amir's proposal, declined by the owner on 30 October 2026 [S4] ([conflict decision](../GDD.md#30-october-2026-conflict-decisions)):** resolver steps would add staffed regional/city/pioneer work, manifests, transit charges, housing-only shortfall effects and garrison-dependent occupation. These additions are non-operative. Actual-work XP agrees with main's `gdd-0.3` rule (`R-VET-001`) and was not a 30 October 2026 conflict. For a reception step, the owner's 30 October 2026 conflict review did not rule on this; on 1 November 2026 the owner kept `R-CITY-004` (`docs/rules/mvp.md:105`) with "Keep R-CITY-004 (Recommended)", so the reception step is post-MVP; main has an inactive `gdd-0.3` reception fixture (`docs/rules/mvp.md:107`) [S7].

Commit retained indicator changes in one bounded final update. Preserve historical hard/soft event windows and established delegation; explicit event resource grants apply once.

### Determinism and state

Use exact arithmetic, fixed neighbour order, ordinal ids and existing RNG [S5]. Completion cards add no RNG. Hash only retained state required by the implemented rules. Empty compatibility sections must not accidentally drift earlier-rule hashes.

Do not claim old goldens hold under revised rules. Implementation must migrate validation/content/saves/commands/reference-player tests together and explain intended hash changes. Documentation alone neither implements this nor regenerates goldens.

## Data touched

- `content/eras.yaml`: `systems.loop` (`gdd` in the real content since the cutover).
- `GameState`: `Cities`, `Units`, `NextId`, `Objectives`, `Score`.
- Save files: schema 2 and the `ruleset` line.
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: the `mvp-slice` line (until the cutover).
- Simulation/resolver, commands, save parser/format, canonical state and reference scenarios.
- This documentation does not edit current schema/ruleset code.
- Shared snapshots prevent duplicate retained projects, courses, meals or arrivals.

## Done when

- `dotnet test sim/Statecraft.sln` passes, and `hash1950`, `hash1952`, `smoke5`, `scenario-cautious` and `scenario-reckless` do not move.
- The WebGL `web_check` with `golden&smoke=5` passes.
- Replay/save round trips match state/draw count under identical rules/content.
- Earlier incompatible saves refuse clearly.
- Course completion, new facilities and arrivals cannot receive another month's output in one resolution.
- Final retained expenses and conditions commit once; Amir's transit charge is not applied: the 30 October 2026 conflict review did not rule on it, main has no live transit charge, and transit is post-MVP (owner, 1 November 2026: "30 Oct wins for the MVP (Recommended)") [S4] [S7].

## Out of scope

- Any GDD rule: cities, training, sites, objectives and the score are built once the owner rules on the open decisions.
- Turning the switch on for the real game: the cutover PR, Phase 2 PR 1.
Claiming current code implements the new resolver, retaining old balance inside the new ruleset and adding chapter checkpoints.

## Sources

- [S1] GDD v0.1 impact analysis and phased plan, section 2.1, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S2] The same plan, section 5, decision D-E4, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, Global constraints, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] Authority, docs/GDD.md.
- [S5] Determinism, docs/determinism.md.
- [S6] Catalogue, docs/gdd/balance.md.
- [S7] Owner's 1 November 2026 answers, [GDD 1 November 2026 answers](../GDD.md#1-november-2026-answers).

## Open questions

- None.
See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
