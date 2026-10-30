# Veteran pioneer

## Player story

As a player of the MVP, I want a group of halutzim that has worked long enough to become veteran pioneers who build faster, so that keeping a group at work pays off (GDD "Veteran pioneer: Experienced pioneer unit" and "Progression: Halutz to veteran pioneer" [S1]; decision D-D9 [S2]).
I see work experience and predict its effect on future projects.

## Rules

- A halutz unit gains 1 turn of service in each turn its project progressed, the turn the build finishes included. An idle turn adds no service [S3][S4] (R-VET-001).
- At 6 turns of service, `veteran_halutz_service`, the unit becomes a `veteran_halutz` at the end of that turn. It keeps its id, hex, service and project, and the Why screen says "A group of halutzim became veteran pioneers". A build takes 6 turns, so one completed yishuv makes a veteran pioneer (R-VET-001).
- A veteran's project gains 2 turns of progress a turn, `veteran_progress`, against a halutz's 1 turn, so a build of 6 turns takes a veteran 3 turns [S3][S4] (R-VET-002).
- A veteran does the same pioneer work as a halutz, with the same cost and the same refusals (R-SITE-001). A veteran gains no more service.
- Both numbers are labelled design values that an agent proposed and the owner took (owner answer on #782 [S4]); the owner may still change them.

*Considered and declined (owner, 30 Oct 2026; keep the recorded ruling; [conflict decision](../GDD.md#30-october-2026-conflict-decisions)): Amir's #1668 proposed replacing the six-turn service threshold and veteran double-progress rule with working-turn XP and catalogue tier rates.* (unitless) [S1]
[Training](training.md#population-identity-and-civilian-professions) owns civilian advancement. [Balance](balance.md#work-professions-and-training) owns thresholds/rates. This existing path stays for compatibility/migration and defines no competing multiplier or threshold.

Under Amir's proposal, replace founding/service shortcuts and veteran double-progress with actual work turns and current tier rate. Novices may become veteran through work without courses. Promotion creates no extra people, tool refunds, progress reset or military-XP change.

All pioneer projects use shared work/XP logic. Teaching, travel, idle time and courses do not count. [Sites](build-yishuv.md)/[corridors](corridors.md) own eligibility/outcomes.

## Data touched

- `content/rules.yaml` `mvp.veteran_halutz_service` and `mvp.veteran_progress` (design values, listed in `DesignValuesTests`; the loader requires both, at least 1 (unitless)).
- `content/unit_types.yaml` `veteran_halutz` (a unit type with no facility: it is promoted, never trained).
- `GameState.Units`: a unit's `Service`, written into the state hash as `/N` only when it is not 0, so a unit with none hashes as before. Phase 5 Q-UNIT-2 counts soldiers' service in the same field.
- `Projects.Start` and `Projects.Advance` (step 5), and the reference player, which sends a veteran to build as it does a halutz.
- `sim/Statecraft.Sim.Tests/golden/manifest.txt`: `hash1950`, `hash1952` and `mvp-end` move, because the reference player's halutz group becomes a veteran after Beit Kama and builds the other three sites in 3 turns each, so Mefalsim and the first new yishuv come six months sooner.
- Legacy service/progress constants require migration.
- Profession/tier/work XP belongs to the same population unit.

## Done when

- `dotnet test sim/Statecraft.sln` passes, including `VeteranTests`, `ProjectsTests`, `GddStateTests` and `DesignValuesTests`.
- `smoke5`, `scenario-cautious`, `scenario-reckless` and `scenario-mvp-absorb-first` do not move; `hash1950`, `hash1952` and `mvp-end` have a row in docs/determinism.md.
- Every pioneer project calls shared advancement.
- Existing links reach current rules instead of old thresholds.

## Out of scope

- Corridor segments (roads and rail): `Corridors.cs` is Phase 4 PR 2 (#786), which applies `veteran_progress` to segment progress there.
- A Hebrew name or a token for the veteran on the map: no screen shows a unit's type yet. The owner approved "חלוץ ותיק" on #782 [S4] for when one does.
- Veteran soldiers (Phase 5, #742), and any benefit other than speed.
Separate progression rules, elapsed-service XP and exchanging military/civilian experience.

## Sources

- [S1] The game design of record, docs/GDD.md
- [S2] GDD v0.1 impact analysis and phased plan, section 5, docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md
- [S3] GDD Phase 4 plan, PR 6 and the Q-VET-1 and Q-VET-2 rows, docs/superpowers/plans/2026-10-15-gdd-phase-4.md
- [S4] The owner's answer, "take all of the plan's recommendations", https://github.com/eyalgolan/statecraft-israel/issues/782#issuecomment-6231352128, checked 2026-10-17
- [S1] Progression owner, docs/gdd/training.md.
- [S2] Catalogue, docs/gdd/balance.md.

## Open questions

- None.
See the single [deferred-decision register](../GDD.md#deferred-decisions). Implementation must not silently decide its open principles.
