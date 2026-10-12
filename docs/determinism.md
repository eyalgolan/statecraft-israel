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
with `GoldenRun.Seed = 1948` on the real `content/`, then steps with `HistoricalAI.Commands` until `Date >= until`
(or the game ends, or 1000 turns pass). Each run's final `GameState.Hash()` is pinned:

| Run | Ends | Hash |
|---|---|---|
| to 1950-01-01 (`Hash1950`) | 1950-02-27, turn 35 | `71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07` |
| to 1952-01-01 (`Hash1952`) | 1952-02-27, turn 43 | `62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8` |

A third value is pinned for the browser smoke run (`?smoke=5`: the golden seed, five turns played through the UI's own
buttons, answering each event with its first option): `SMOKE OK 5 turns 1948-06-19 d3fdd459ddb876bd8d775c20edd1202cf56ca7134a8ad06f873d4d92034aa5b6`.
It depends on the UI path as well as the sim (which option is first, the turn grid), so re-record it when either changes.
Check every build with:

```
uv run tools/web_check.py unity/Builds/Web-<compression> --query "golden&smoke=5" \
  --expect "GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07" \
  --expect "GOLDEN OK 1952 62f8ec61a14acff4eab503381994a7c94d7a2f786dcfde5c4c19439ac8b4c5e8" \
  --expect "SMOKE OK 5 turns 1948-06-19 d3fdd459ddb876bd8d775c20edd1202cf56ca7134a8ad06f873d4d92034aa5b6"
```

**When a golden moves.** The test fails with the lines that changed, grouped by section (`[ind]`, `[pol]`, `[bud]`,
`[sit]` and so on), against a committed snapshot of the run's final state: `sim/Statecraft.Sim.Tests/golden/hash1950.txt`
and `hash1952.txt`, written by `GameState.Describe()`, which uses the same writer as `Hash()` (so the two cannot list
different fields) and shows every number as a plain value next to its IEEE bits. If the change is deliberate, run
`STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln`: it rewrites the snapshots, so the PR's diff of them shows
reviewers what moved. Then update the hash constants in the three places below by hand, and say why in the commit
message. The snapshot test also fails if a snapshot is stale while the hash is unchanged.

**Cause text is derived, not hashed.** The "Why did this happen?" lines (`Cause.Message`, `Cause.Delta`) are output
computed from the hashed state; replaying a save's commands regenerates them. Wording changes therefore do not move the
goldens (playtest 2 changed every line's casing with no hash change). They are still deterministic:
`FinalWaveTests.AReloadedSaveReproducesTheLastTurnsCauses` checks a reloaded save gives the same last-turn Causes, and
`TheGoldenRunsCausesAreTheSameInEveryCulture` runs the 1952 golden under de-DE and ar-SA against the invariant culture.

Each golden constant is written in three places that must agree:
- `sim/Statecraft.Sim/GoldenRun.cs`, which Unity code and players compare against;
- `sim/Statecraft.Sim.Tests/DeterminismTests.cs`, which pins the value on .NET and asserts the library constant equals it;
- this file.

`unity/Assets/Tests/EditMode/SimSmokeTests.cs` runs the same playthroughs inside Unity against `GoldenRun`'s constants
(run `tools/build_sim_for_unity.sh` first so Unity has the current DLLs).

Verified on .NET, Unity editor Mono, a macOS IL2CPP player (Apple clang, no `-ffp-contract=off` needed) and a WebGL
IL2CPP player in Chrome (M3 Task 22). The platform checks below used the prior `73590e32` 1952 baseline. The issue #2
content correction has been rechecked on .NET only; Unity/WebGL/macOS revalidation is still needed.

Re-checked at the M3 merge candidate (2026-10-11, commit a099655):
- macOS IL2CPP player (`Build.Mac`, then `-batchmode -nographics -golden`): **exit 0**, printing
  `GOLDEN OK 1950 71e9904d0ee48083db9bbba281f3f046136482ba83ee056e48d57b62e76a5b07` and
  `GOLDEN OK 1952 73590e324f19e9959a455a4da79e93d7089dfcf283c13aac02af6b0a624b9767`.
- WebGL IL2CPP player (Brotli release build, `web_check.py` with the three full values above): PASS.

Re-check with:

```
"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget WebGL -executeMethod Statecraft.Game.EditorTools.Build.Web -compression disabled -logFile unity-build-web.log
uv run tools/web_check.py unity/Builds/Web-disabled --query golden --expect "GOLDEN OK 1950" --expect "GOLDEN OK 1952"
"$UNITY" -batchmode -CI false -GITHUB_ACTIONS false -TF_BUILD false -quit -projectPath unity -buildTarget OSXUniversal -executeMethod Statecraft.Game.EditorTools.Build.Mac -logFile unity-build-mac.log
"unity/Builds/Mac/Statecraft.app/Contents/MacOS/Statecraft Israel" -batchmode -nographics -golden -logFile "$PWD/unity-golden-mac.log"; echo exit=$?
```

The player needs an absolute `-logFile` path: with a relative one it wrote no log file in the check run. The editor
commands are fine with relative paths.

**Any change to a golden hash is a deliberate rules or content change.** Such a change could be to a rule, a system,
content in `content/`, or the hash format. When it happens, update every place above in the same commit, add a row to
the history below, and say in the commit message why the hash moved. If a hash changes and you did not mean to change
rules or content, treat it as a determinism bug.

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
