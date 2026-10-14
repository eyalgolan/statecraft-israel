# Training: military camp and Hakhshara

## Player story

As a player of the MVP, I want to send Tel Aviv's civilians without work to basic training or to pioneer training, so that the city has recruits for defence and halutzim to build new places (GDD "Progression" [S1]; decisions D-A3, D-D5 [S2]).

## Rules

- One military camp and one Hakhshara exist in Tel Aviv at the start. This is a design assumption, not a claim about the city, and pioneers will build more in Phase 4 (D-D5 [S2]). The GDD leaves training capacity open, so there is no capacity limit (unitless).
- `Train(city, track, n)` takes civilians without a job into training as recruits (military camp) or halutzim (Hakhshara). It costs 1 Attention and IL 0.5m per group, both placeholders, paid at once. A short Treasury borrows the rest, as map actions do (owner decision on #468) (R-TRAIN-001).
- It is refused, with the state unchanged, for fewer than 1 group (unitless), more than the city's civilians without a job, an unknown city or track, a city without the facility, or the legacy loop (R-TRAIN-001).
- People in training are neither civilians nor recruits: they hold no job and are not counted by housing or Social cohesion while they train.
- `CancelTraining(cohort)` returns the people as civilians without a job; the Money spent is not returned (D-A3 [S2]). It costs 0.5 Attention, a placeholder (R-TRAIN-002).
- Training resolves before production in each turn, in the order the owner approved (D-A6, #456 [S2][S3]). A cohort ordered this turn counts this turn: basic training takes 3 turns and pioneer training 4 turns, both placeholders (R-TRAIN-003).
- A finished recruit cohort adds to the city's recruits, a count until soldiers become units. A finished halutz cohort becomes one movable unit per group at the city's hex, with consecutive ids (D-A2 [S2], R-TRAIN-003).

## Data touched

- `unit_types.yaml` (`recruit`, `halutz`: `facility`, `training_turns`, `training_cost`), `cities.yaml` (`facilities`) and `rules.yaml` `attention_cost.train` and `cancel_training`: supplied by `MvpSlice.Overlay` as placeholders, never under `content/` (ruling 2 [S3]).
- `GameState`: `Cohorts` (hashed as `train[...]` when there are some), a city's `Recruits` (in its hash record when not 0), `Units`, `NextId`, `Treasury`.
- Save files: the `train CITY TRACK N` and `untrain COHORT` lines. Scenarios: `train` and `cancel_training`.
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: `mvp-slice` moves; `scenario-mvp-absorb-first` does not.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `TrainingTests`, `CitiesContentTests`, `GddStateTests` and `ReferencePlayerTests`.
- `hash1950`, `hash1952`, `smoke5`, `scenario-cautious`, `scenario-reckless` and `scenario-mvp-absorb-first` do not move.

## Out of scope

- Building more camps or a Hakhshara (Phase 4), training capacity, recruits as units and combat (Phase 5), veterans, craftsmen (D-D4), Arms consumed by training (D-G6), and any display (PRs 7 and 8).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 4, docs/superpowers/plans/2026-10-12-gdd-phase-1.md

## Open questions

- The placeholder values (training turns, cost per group, the two Attention costs) wait for the owner.
- Whether people in training, and recruits, take up the city's housing. Housing counts civilians only (R-CITY-004), so a group in training frees a place that a waiting refugee then takes.
- Whether the turn of the order counts as a training turn. This is the plan's reading of D-A6, not part of the owner's ruling. The other reading adds a turn.
- Social cohesion's employed share rises while civilians without work are in training, because they leave the civilians (R-CITY-005).
