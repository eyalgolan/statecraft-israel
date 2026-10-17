# Training: military camp and Hakhshara

## Player story

As a player of the MVP, I want to send Tel Aviv's civilians without work to basic training or to pioneer training, so that the city has recruits for defence and halutzim to build new places (GDD "Progression" [S1]; decisions D-A3, D-D5 [S2]).

## Rules

- One military camp and one Hakhshara exist in Tel Aviv at the start. This is a design assumption, not a claim about the city (D-D5 [S2]). The GDD leaves training capacity open, so there is no capacity limit (unitless).
- Pioneers build more: `PioneerWork(unit, build_camp, site)` at a camp site takes 6 turns and IL 2m, labelled design values an agent proposed (the owner may override), and follows the yishuv build's rules. Built, the site's city has the facility from the next turn, with no capacity limit (`Q-CAMP-1`, `Q-CAMP-3` [S4]) (R-CAMP-001).
- The camp sites are `Camp 80` near Pardes Hanna, a Hakhshara for Hadera, and Tzrifin, a military camp for Rishon LeZion (`Q-CAMP-2` [S5]). Both rows are drafts: their sources were read by AI agents for the research note, and Tzrifin's exact position and a GIS clearance are still gaps.
- `Train(city, track, n)` takes civilians without a job into training as recruits (military camp) or halutzim (Hakhshara). It costs 1 Attention and IL 0.5m per group, both placeholders, paid at once. A short Treasury borrows the rest, as map actions do (owner decision on #468) (R-TRAIN-001).
- It is refused, with the state unchanged, for fewer than 1 group (unitless), more than the city's civilians without a job, an unknown city or track, a city without the facility, or the legacy loop (R-TRAIN-001).
- People in training are neither civilians nor recruits: they hold no job and are not counted by housing or Social cohesion while they train.
- `CancelTraining(cohort)` returns the people as civilians without a job; the Money spent is not returned (D-A3 [S2]). It costs 0.5 Attention, a placeholder (R-TRAIN-002).
- Training resolves before production in each turn, in the order the owner approved (D-A6, #456 [S2][S3]). A cohort ordered this turn counts this turn: basic training takes 3 turns and pioneer training 4 turns, both placeholders (R-TRAIN-003).
- A finished recruit cohort adds to the city's recruits, a count until soldiers become units. A finished halutz cohort becomes one movable unit per group at the city's hex, with consecutive ids (D-A2 [S2], R-TRAIN-003).

## Data touched

- `unit_types.yaml` (`recruit`, `halutz`: `facility`, `training_turns`, `training_cost`), `cities.yaml` (`facilities`), `rules.yaml` `attention_cost.train`, `cancel_training`, `mvp.build_camp_turns` and `mvp.build_camp_cost`: labelled design values in `content/` since the cutover (Q-CUT-1, R-LOOP-005), the overlay's placeholders before it (ruling 2 [S3]).
- `GameState`: `Cohorts` (hashed as `train[...]` when there are some), a city's `Recruits` (in its hash record when not 0), `Units`, `NextId`, `Treasury`, and a built camp's `Sites` record, which is where `Training.HasFacility` reads a built camp from.
- `content/sites.yaml`: the `camp_80` and `tzrifin` rows, each with a `city`.
- Save files: the `train CITY TRACK N` and `untrain COHORT` lines. Scenarios: `train` and `cancel_training`.
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: `mvp-slice` moved (retired at the cutover); `scenario-mvp-absorb-first` did not. Phase 4 PR 3 moves `hash1950`, `hash1952` and `mvp-end`: the reference player builds the two camps once the yishuv sites are taken (docs/determinism.md).

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `TrainingTests`, `CampsTests`, `SitesContentTests`, `CitiesContentTests`, `GddStateTests` and `ReferencePlayerTests`.
- `smoke5`, `scenario-cautious`, `scenario-reckless` and `scenario-mvp-absorb-first` do not move; `hash1950`, `hash1952` and `mvp-end` have a row for #787 in docs/determinism.md.

## Out of scope

- The start camps of Jerusalem (Schneller) and Petah Tikva (Tel HaShomer), which the owner ruled the cities already have (#375); this PR keeps the start facilities as they are. Training capacity, recruits as units and combat (Phase 5), veterans, craftsmen (D-D4), Arms consumed by training (D-G6), and any display (PRs 7 and 8).

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 1 implementation plan, PR 4, docs/superpowers/plans/2026-10-12-gdd-phase-1.md
- [S4] GDD Phase 4 plan, PR 3 and the Q-CAMP-1 to Q-CAMP-3 rows, docs/superpowers/plans/2026-10-15-gdd-phase-4.md; the owner's answer, "take all of the plan's recommendations", https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128, checked 2026-10-17
- [S5] The owner's Q-CAMP-2 answer, "Both", https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233777137; the research, https://github.com/eyalgolan/statecraft-israel/issues/785#issuecomment-6233553842 and research_notes/gdd-phase-4-sourcing.md, rows C1 and C4; links checked 2026-10-17

## Open questions

- The placeholder values (training turns, cost per group, the two Attention costs) wait for the owner.
- The camp build's 6 turns and IL 2m are the yishuv build's pair, proposed by an agent; the owner may set others.
- Camp 80's kind, `hakhshara`, is a design mapping of the Gadna training of settlement nuclei there, as the #785 research proposes, not a claim that it was an agricultural training farm.
- Whether people in training, and recruits, take up the city's housing. Housing counts civilians only (R-CITY-004), so a group in training frees a place that a waiting refugee then takes.
- Whether the turn of the order counts as a training turn. This is the plan's reading of D-A6, not part of the owner's ruling. The other reading adds a turn.
- Social cohesion's employed share rises while civilians without work are in training, because they leave the civilians (R-CITY-005).
