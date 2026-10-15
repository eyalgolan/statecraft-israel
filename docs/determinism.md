# Determinism

The simulation must produce the same state, bit for bit, on .NET, Unity Mono and IL2CPP. Saves, replays and the
"Why did this happen?" report depend on this.

## How it is kept

- **One RNG:** SplitMix64 (`Statecraft.Sim.Rng`). Its whole state is one `ulong` in `GameState.RngState`.
- **Reference vectors:** `DeterminismTests.SplitMix64MatchesTheReferenceVectors` checks the first three outputs for
  seeds 0 and 1234567. The expected values come from Sebastiano Vigna's reference `splitmix64.c`
  (https://prng.di.unimi.it/splitmix64.c), compiled and run separately. They were not copied from this implementation.
- **No trigonometry in the sim:** the sim calls only `Math.Abs`, `Math.Min` and `Math.Max`. Geometry that needs
  cos/sin (which hex owns a settlement) is precomputed by `tools/settlement_hexes.py` and read as content.
- **Canonical hash:** `GameState.Hash()` is SHA-256 over a canonical text. Keys are sorted ordinally. Doubles are
  written as IEEE bit patterns, with -0.0 written as 0.0. Culture is invariant.

## Golden playthroughs

`Statecraft.Sim.GoldenRun.Play(content, until)` starts `Simulation.NewGame(content, GoldenRun.Seed, Mode.Historical)`
with `GoldenRun.Seed = 1948` on the real `content/`, then steps with `GoldenRun.Orders` until `Date >= until`
(or the game ends, or 1000 turns pass). Since the GDD cutover (Phase 2 PR 1) the orders are the reference player's
(`Statecraft.Sim.Reference.ReferencePlayer`, decision D-A7): a stated test policy, not game AI and not history. It sends
`HistoricalAI`'s orders, then fills each city's jobs, then trains one halutz group when there is none and none in training,
while the turn's Attention and the Treasury last. No game mode or UI reaches it (`ReferencePlayerTests`); every copy of the
golden loop (the writer preview, the invariants, the era goldens) takes its orders from `GoldenRun.Orders`. The real game
plays the GDD loop on monthly turns from 15 May 1948 and ends on 15 January 1952 after 44 turns (R-LOOP-004). Each run's
final `GameState.Hash()` is pinned:

| Run | Ends | Hash |
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-01-15, turn 20 | `ad8067765007988a2ea52226a50622259b695cd72a5b735b298207e472b81504` |
| to 1952-01-01 (`Hash1952`) | 1952-01-15, turn 44 (the game's end) | `0f54dc51a42575774b12a4b712d6d6ad69a45573232bd5b54b8527b24f4281f2` |
| to the game's end (`mvp-end`) | 1952-01-15, turn 44 | `0f54dc51a42575774b12a4b712d6d6ad69a45573232bd5b54b8527b24f4281f2` |

`hash1952` stops at the first turn on or after 1 January 1952, which is the game's last turn, so it is the same run as
`mvp-end` (`DeterminismTests.TheWholeGameToItsEndIsFixed`). Both names are kept (spec 2026-10-12 section 3 step 6): a
later end date moves only `mvp-end`. `mvp-end` is pinned in the manifest and its snapshot; the players and `web_check`
check `hash1950` and `hash1952`.

A further value is pinned for the browser smoke run (`?smoke=5`: the golden seed, five turns played through the UI's own
buttons, answering each event with its first option while the turn's Attention pays for it, the rest left to their
ministers): `SMOKE OK 5 turns 1948-10-15 85191d48688be4595726b17778ecdda0175eff8f6f0fa1d976dbe3b48b42823b`.
It depends on the UI path as well as the sim (which option is first, the turn grid), so re-record it when either changes.
Check every build with:

```
uv run tools/web_check.py unity/Builds/Web-<compression> --query "golden&smoke=5" \
  --expect "GOLDEN OK 1950 ad8067765007988a2ea52226a50622259b695cd72a5b735b298207e472b81504" \
  --expect "GOLDEN OK 1952 0f54dc51a42575774b12a4b712d6d6ad69a45573232bd5b54b8527b24f4281f2" \
  --expect "SMOKE OK 5 turns 1948-10-15 85191d48688be4595726b17778ecdda0175eff8f6f0fa1d976dbe3b48b42823b"
```

**When a golden moves.** The test fails with the lines that changed, grouped by section (`[ind]`, `[pol]`, `[bud]`,
`[sit]` and so on), against a committed snapshot of the run's final state: `sim/Statecraft.Sim.Tests/golden/hash1950.txt`
and `hash1952.txt`, written by `GameState.Describe()`, which uses the same writer as `Hash()` (so the two cannot list
different fields) and shows every number as a plain value next to its IEEE bits. If the change is deliberate, run
`STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln`: it rewrites the snapshots, so the PR's diff of them shows
reviewers what moved. The same command rewrites `sim/Statecraft.Sim.Tests/golden/manifest.txt` (the pinned hashes) and the
`GoldenRun.cs` constants. That run still fails for every golden that moved, against the value it started with; the tests
never take a hash from the run without the variable, so a second plain run is the check. Then update the docs below by hand (`GoldenManifestTests` names any place still showing the old
hash), add a row to the rebaseline history, and say why in the commit message. The snapshot test also fails if a snapshot is
stale while the hash is unchanged.

**Adding an era golden.** In a test, call `EraGolden.Check("hash_<era>_<year>", content, "<era id>", new DateTime(<year>, 1, 1))`. It plays a fresh
start of that era (golden seed unless one is passed, Historical mode, `GoldenRun.Orders`, the same loop as `GoldenRun.Play`) and checks the hash
against the manifest line of that name and the snapshot `golden/<name>.txt`. The run stays inside the era: it stops before the turn that would
reach the era's `to`, because that turn ends on the boundary and its state already holds the next era's attention and events, so the
next era's rules would move this era's golden (`EraGoldenTests.TheRunStaysInsideTheEraSoTheNextErasRulesDoNotMoveIt`). Bless it with `STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln`, which adds the
manifest line and the snapshot; list the name in `GoldenManifestTests.TheManifestHoldsExactlyTheKnownGoldens`. Today only the first era can
be started fresh, because `NewGame` reads the base rules' start; a later era fails with a message saying its start values are not read yet.
The carry-over check (play era N into N+1 and compare with a fresh N+1 on each carried quantity, chapters design section 6) waits on that
and on `EraSystem.Cross`.

**Scenario goldens.** The hash goldens follow the reference player, `HistoricalAI`'s orders on the path history took. A scenario is a path a player might take
instead: `sim/Statecraft.Sim.Tests/scenarios/<name>.yaml` lists commands by date (`on:`), the runner (`Scenario`, in the sim library) plays
them from a seed (default 1948) in Historical mode until `until:`, and `ScenarioTests` checks the final hash against the manifest line
`scenario-<name>` and the snapshot `golden/scenario-<name>.txt`. The file's header comment lists the command kinds. A turn with no
scripted command passes. A command the sim rejects fails the test with the date, the order and the reason, and a step that never runs
fails too. To add one: write the file, run `STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln --filter ScenarioTests` (it adds the
manifest line and the snapshot), then run the tests plain. A scenario that moves is re-pinned with the hash goldens by the same
command; its snapshot diff in the PR shows what moved, and the commit message says why. All three play the real content.
`cautious` and `reckless` stay in 1948, so they move only when a 1948 rule or event does; `mvp-absorb-first` (GDD Phase 1
PR 3) fills Tel Aviv's jobs on the first turn and plays to 15 June 1949.

**MVP goldens after the cutover.** The real game is the GDD game, so every golden above is an MVP golden. Each PR that
changes an MVP rule or a design value (R-LOOP-005) moves the goldens it reaches, re-pins them in the same PR, adds a row to
the history below and says why in the commit and the PR (spec 2026-10-12 section 3, "After cutover"). PRs merge one at a
time, so there is no "re-record at integration".

**Before cutover.** The last legacy goldens (the legacy loop, `HistoricalAI` alone, weekly, monthly and quarterly turns by
security level, play to 1957) were `hash1950` `71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07` (1950-02-27,
turn 35), `hash1952` `62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8` (1952-02-27, turn 43), `smoke5`
`SMOKE OK 5 turns 1948-06-19 d3fdd459ddb876bd8d775c20edd1202cf56ca7134a8ad06f873d4d92034aa5b6`, `scenario-cautious`
`1d5b8d71`, `scenario-reckless` `c0bd12bc`, and on the Phase 1 overlay `mvp-slice` `ec6f8c80` (after #566; retired with the overlay,
R-LOOP-003) and `scenario-mvp-absorb-first` `59ea3181`. The owner tags the last pre-cutover commit `pre-gdd-v0.1`.

`Scenario` lives in `Statecraft.Sim` next to `GoldenRun`, so Unity, which gets only `Statecraft.Sim.dll` and
`YamlDotNet.dll` from `tools/build_sim_for_unity.sh`, can play the same runs (#394). The scenario files, the manifest and the
snapshots stay with the tests and are not shipped to the player.

**Invariants.** A hash says something changed; `sim/Statecraft.Sim.Tests/golden/invariants.yaml` says whether it is
still plausible. Each entry names a golden run (`1950` or `1952`), a value (an indicator id, or `treasury`, `debt`,
`fx`, `population`, `stability`), when it is read (`end`, `always`, or a date: the first state on or after it), a `min`
and/or `max`, and a `reason` (a source, or the test or note the band comes from). `InvariantTests` plays the golden runs
and fails with the check, the value, the date and the band, for example `invariant 'foreign-currency-runs-out' (...): fx
= 12.5 on 1952-02-27 is outside [-inf, 0]`. The invariants only add checks: a tuned rate that stays in its band still
moves the hash goldens (rebaseline as above), but not an invariant. The shipped bands are loose guards set around the
current runs; the design document and the maintainers decide which ranges matter.

**Rules per era.** A system reads `Content.RulesAt(date)`, where `date` is the date of the state the turn starts from (`s.Date`
before the step); `AttentionSystem.Refill` runs after the date moves, so it fills the next turn from that turn's era. Rules
change only at era boundaries (chapters spec section 3.3). Spec section 4.1 will end the turn that reaches a boundary exactly
on it, so every turn then lies in one era; until that lands, only the last era's end is a boundary turn, and a turn that
starts in one era and ends in the next uses the starting era's rules for its whole length
(`EraRulesSwitchTests.ARunPicksUpTheOverrideFromTheFirstTurnThatStartsInTheNewEra`). Start
values (`start`, `treasury_start`, `fx_start`, `start_rung`, `initial_population`, `budget_start`, `budget_owners`) are read once by `NewGame`
from the base `rules.yaml`: an era override cannot set them. A map action's display name is also read from the base rules,
because an override cannot remove an action. `EraRulesSwitchTests.OnlyStartValuesAndMapActionNamesReadTheBaseRulesDirectly`
fails if a file in `sim/Statecraft.Sim` reads `Rules` directly for anything else, including a bare `var r = c.Rules;`.

**Cause text is derived, not hashed.** The "Why did this happen?" lines (`Cause.Message`, `Cause.Delta`) are output
computed from the hashed state; replaying a save's commands regenerates them. Wording changes therefore do not move the
goldens (playtest 2 changed every line's casing with no hash change). They are still deterministic:
`FinalWaveTests.AReloadedSaveReproducesTheLastTurnsCauses` checks a reloaded save gives the same last-turn Causes, and
`TheGoldenRunsCausesAreTheSameInEveryCulture` runs the 1952 golden under de-DE and ar-SA against the invariant culture.

Each golden hash is pinned in `sim/Statecraft.Sim.Tests/golden/manifest.txt` (`hash1950`, `hash1952`, `mvp-end`, `smoke5`, and the scenario lines above). The .NET tests
compare against it. These places must agree with it, and `GoldenManifestTests` fails naming the one that does not:
- `sim/Statecraft.Sim/GoldenRun.cs`, which Unity code and players compare against (the update command rewrites it);
- this file, `docs/RELEASING.md` and `tools/web_check.py`'s usage example (by hand).

`tools/art_checkpoint.sh` reads its expected hashes from the manifest, so it needs no edit.

`unity/Assets/Tests/EditMode/SimSmokeTests.cs` runs the same playthroughs inside Unity against `GoldenRun`'s constants
(run `uv run tools/build_sim_for_unity.py` first so Unity has the current DLLs).

Verified on .NET, Unity editor Mono, a macOS IL2CPP player (Apple clang, no `-ffp-contract=off` needed) and a WebGL
IL2CPP player in Chrome (M3 Task 22). The platform checks below used the prior `73590e32` 1952 baseline. The issue #2
content correction has since been rechecked on .NET, in the Unity EditMode tests and in WebGL (see below and
`docs/visual-v1.md`), and on a macOS IL2CPP player (2026-10-14, below).

Re-checked at the M3 merge candidate (2026-10-11, commit a099655):
- macOS IL2CPP player (`Build.Mac`, then `-batchmode -nographics -golden`): **exit 0**, printing
  `GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07` and
  `GOLDEN OK 1952 73590e324f19e9959a455a4da79e93d7089dfcf283c13aac02af6b0a624b9767`.
- WebGL IL2CPP player (Brotli release build, `web_check.py` with the three full values above): PASS.

Re-checked on macOS after the issue #2 correction (2026-10-14, commit `bd683ee0` on the #542 branch, Unity 6000.3.25f1, a
`--dev` build through `tools/release_build.py --platform mac`, which runs the player with `-batchmode -nographics -golden`):
**exit 0**, printing `GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07` and
`GOLDEN OK 1952 62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8` on an IL2CPP Universal player
(x86_64 and arm64; this Mac ran the arm64 slice). The run is recorded in #542. These are the legacy values (see
"Before cutover"); the GDD values are not yet re-checked on a desktop player.

Re-check with:

```
"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log
uv run tools/web_check.py unity/Builds/Web-disabled --query golden --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952"
"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile unity-build-mac.log
"unity/Builds/Mac/Statecraft.app/Contents/MacOS/Statecraft Israel" -batchmode -nographics -golden -logFile "$PWD/unity-golden-mac.log"; echo exit=$?
```

The player needs an absolute `-logFile` path: with a relative one it wrote no log file in the check run. The editor
commands are fine with relative paths.

**A content change can move the content hash without moving a golden.** Settlement names, for example, are content (the
hash covers `content/map/settlements_1948.yaml`) but not state. On 2026-10-12 the owner ruled that the game settlements
`lod` and `ramla` take their 1948 names, Lydda and Ramle, as Village Statistics 1945 lists them ("Lydda (Urban)", "Ramle
(Urban)"). That commit changed only those two names and added a source to each. Both goldens and the five-turn smoke are
unchanged: .NET 667 of 667, and the WebGL `golden&smoke=5` check passes with the three values above. Only the content hash
moved. A save made before it therefore loads with the "different game content" warning (`GameSession.Load`) and replays
to the same state.

**Any change to a golden hash is a deliberate rules or content change.** Such a change could be to a rule, a system,
content in `content/`, or the hash format. When it happens, update the manifest and every place above in the same commit, add a row to
the history below, and say in the commit message why the hash moved. If a hash changes and you did not mean to change
rules or content, treat it as a determinism bug.

## Content windows

Records with `from`/`to` (chapters spec 3.2) are resolved by date through `Content.At(date)`. The active lists are built
once at load, one per segment between cut dates, so a turn does a binary search and no allocation. When the date crosses
a cut, `ContentWindows.Update` adds starting records at their defaults and removes ending ones, in ordinal id order, right
after the date moves (before the cabinet, attention and events; the turn's effects already ran on the turn's start date).
The hash reads the state keys, so it never sees a record outside its window. An event that fires late (after its window,
`EventSystem.Trigger`) can find a record it names already gone: a condition on it is not met, and an option's effect or
`set_policy` on it is left out. With no windows there is one segment and the same view everywhere.

## Rebaseline history

| Plan task | Run | New hash (first 8) | Why |
|---|---|---|---|
| M3 Task 2 | 1950 | `d46ce5ff` | the Immigration budget line moves to the Prime Minister (the Ministry of Immigration ends on 1951-10-08), which renames a key in the hashed budget |
| M3 Task 3 | 1950 | `3d360deb` | CBS immigration for 1950 adds January-February 1950 arrivals inside the run |
| M3 Task 4 | 1950 | `e2fa3896` | frumkin_commission (window from 1950-01-01) fires inside the run |
| M3 Task 5 | 1950 | `bf6baf3f` | economy calibrated (rules.yaml) and HistoricalAI sends reserves home after the armistices; capital_inflow_per_year 193.8 acts from 1948, so it moves this hash too |
| M3 Task 5 | 1952 | `1753fb88` | new run covering the M3 content range. Task 4 fix round 1 changed 1951 event windows/effects (hula, forex, bonds, clothing rationing, education, reparations), so capital_inflow_per_year was re-fitted from 192 to 193.8 (first fx crisis in H2 1951) |
| M3 Task 5 fix 1 | 1950 | `43cc7773` | capital_inflow_per_year re-fitted 193.8 -> 200 (it acts from 1948) after the missing $45m credit maturity was added |
| M3 Task 5 fix 1 | 1952 | `0359fa91` | forex_crisis_1951 now applies the documented -45 fx (FRUS d513) in both options, and capital_inflow_per_year is 200 |
| M3 Task 6 | 1950 | `549b49cb` | hash format adds the player's appointments (empty in the run); no state change |
| M3 Task 6 | 1952 | `860928f2` | same |
| M3 Task 8 | 1950 | `f52fa804` | hash format adds map placements (empty in the run); no state change |
| M3 Task 8 | 1952 | `d07cd3d2` | same |
| M3 Task 8 fix | 1950 | `ee5d0826` | Task 8 fix: per-turn map-action counter in state (hash text gains mapturn=); no state change in the run |
| M3 Task 8 fix | 1952 | `91bb6fb5` | same |
| playtest 1 E | 1950 | `a540b52e` | 1948 war-period events added (playtest 1 E): thirteen events whose windows open between 26 May and 31 December 1948; the second truce puts the run on monthly turns from 17 July to 17 October 1948, so the run ends 1950-02-27 after 35 turns (was 1950-02-26, turn 45) |
| playtest 1 E | 1952 | `16c16d7e` | same; ends 1952-02-27 after 43 turns (was 1952-02-26, turn 53) |
| playtest 1 E fix 1 | 1950 | `d463d5b2` | events gain `requires: {event, option}`; the hash text gains chosen{event=option;...} (the options each resolved event was resolved with); no change in the run |
| playtest 1 E fix 1 | 1952 | `f78fc68d` | same |
| playtest 1 E fix 1 | 1950 | `71e9904d` | 1948 war-period events revised after review: windows open on the turn before each decision, Yoav becomes the Hebron-hills decision, follow-ups use `requires`, Bernadotte's shared standing hit, security deltas halved, the elections a one-option card in January, two events added (Jerusalem cease-fire, Rhodes talks); still ends 1950-02-27, turn 35 |
| playtest 1 E fix 1 | 1952 | `73590e32` | same; still ends 1952-02-27, turn 43 |
| Issue #2 | 1952 | `62f8ec61` | sourced Yosef food-supply/rationing responsibility from 8 October 1951 replaces a vacancy and changes late-1951 rationing competence; 1950 and five-turn smoke unchanged |
| GDD Phase 1 PR 3 | mvp-slice | `f6931e24` | the slice gains its city (placeholder values) and the GDD turn: Money from jobs, housing, Social cohesion and arrivals, with the reference player filling the jobs (was `0451920a`); the legacy goldens do not move |
| GDD Phase 1 PR 4 | mvp-slice | `6343eee8` | training runs in the slice (step 4 of the GDD turn) and the reference player trains one halutz group on the first turn: it becomes a unit at Tel Aviv's hex on the fourth, its IL 0.5m adds to the debt, and the civilian it took frees a place that one more refugee is housed in (was `f6931e24`); the legacy goldens and `scenario-mvp-absorb-first` do not move |
| GDD Phase 1 PR 5 | mvp-slice | `ec6f8c80` | owner decision "Allow borrowing" (15 October 2026): training and pioneer work borrow what the Treasury lacks. The reference player's halutz group, ready on 15 September 1948, starts Mefalsim that turn (its IL 2m adds to the debt) and builds it on 15 March 1949; `MvpGolden.Until` moves from 15 January to 15 March 1949 (ten monthly turns) so the build finishes inside the run (was `6343eee8`); the legacy goldens and `scenario-mvp-absorb-first` do not move |
| GDD Phase 2 PR 1 | 1950 | `f97e9f81` | GDD v0.1: monthly turns and the GDD loop on (spec 2026-10-12 section 3 step 5). The real content runs `systems.loop: gdd` with monthly turns on every rung, and the golden runs play the reference player, so the run ends 1950-01-15 after 20 turns (was 1950-02-27, turn 35) and gains the GDD sections (`city{}`, `coh=`, `units[]`, `next=`: the halutz group trained from 15 August 1948; `site{mefalsim=done,1949-06-15}`: the build of #566, with its IL 2m in the debt). Event windows re-timed per #567 (idf_founding, altalena, jerusalem_military_government, latrun_proposal, operation_horev, elections_1949), and PR 1a (#577) merged in: the five war cards rewritten (options `keep` and `stop_at_ceasefire`) and the sensitive options' gains replaced by costs (morale and International Support lower); the Phase 1 and #566 values in content as design values, unchanged; the FRUS anchor holds with no refit; `mvp-slice` (`ec6f8c80`) retired; #315 merged earlier (#475) and moved no golden (was `71e9904d`) |
| GDD Phase 2 PR 1 | 1952 | `75d38299` | same; the game now ends on 15 January 1952 (R-LOOP-004), the first turn on or after 1 January 1952: ends 1952-01-15, turn 44, `over=mvp_ended` (was 1952-02-27, turn 43, still playing); the reparations claim's cost moves to Money (`treasury` -1, #577), so the debt is IL 1m higher (was `62f8ec61`) |
| GDD Phase 2 PR 1 | mvp-end | `75d38299` | new: the reference player over the whole game, to 15 January 1952; the same run as hash1952 |
| GDD Phase 2 PR 1 | smoke5 | `85191d48` | five monthly turns end on 1948-10-15 (was 1948-06-19); the smoke answers cards while the turn's Attention pays for them, so the third card of 15 July 1948 goes to its minister (was `d3fdd459`) |
| GDD Phase 2 PR 1 | scenario-cautious | `58eb01db` | monthly turns: ends 1949-01-15, turn 8 (was 1949-01-02, turn 23); rhodes_talks and elections_1949 now fire inside the run; the GDD sections appear; #577's rewritten cards and costs (was `1d5b8d71`) |
| GDD Phase 2 PR 1 | scenario-reckless | `e2f409a5` | same (was `c0bd12bc`) |
| GDD Phase 2 PR 1 | scenario-mvp-absorb-first | `f8858f6e` | plays the real content (the overlay is retired, `content: mvp` dropped); the Horev window edit makes rhodes_talks and sinai_withdrawal fire; #577's rewritten cards and costs (was `59ea3181`) |
| GDD Phase 1 PR 6 | 1950 | `ad806776` | the first new yishuv objective (R-OBJ-001 to R-OBJ-003), rebased onto the cutover: the reference player builds Mefalsim on 15 June 1949, the month of its 12 June 1949 founding, so the objective completes on time for 100 points (`obj{first_new_yishuv=1949-06-15}`, `score=100`) and its card fires and is answered (`first_new_yishuv=continue`); no other event moves, so the card displaces nothing under the soft-event cap; `objective_points`, `objective_bonus_per_month` and `objective_bonus_cap` enter `rules.yaml` as design values (D-B6, D-B2) (was `f97e9f81`) |
| GDD Phase 1 PR 6 | 1952 | `0f54dc51` | same (was `75d38299`) |
| GDD Phase 1 PR 6 | mvp-end | `0f54dc51` | same; the same run as hash1952 (was `75d38299`); `smoke5` and the three scenarios do not move |
