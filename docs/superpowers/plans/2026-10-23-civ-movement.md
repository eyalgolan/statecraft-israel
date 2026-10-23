# Civ-style movement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Units move and attack the moment the player clicks, within their movement points; towns are captured the moment a unit takes one; multi-turn go-to orders; End turn waits until every unit has orders, as in Civ.

**Architecture:** The sim keeps one command log. The turn's moves, go-tos, attacks, fortify and hold orders are *committed* (in click order) and the screen shows `Simulation.Preview(turn start, committed)`, the same `RunOrders` code `Step` runs. End turn sends `committed ++ deferred` to `Simulation.Step`. The save gains an open turn (schema 3) so a reload keeps what was done. Go-to is a standing unit order continued by a route step at the start of `RunOrders`; the capture check runs after each order.

**Tech Stack:** C# (netstandard2.1 sim, xUnit tests, `dotnet test sim/Statecraft.sln`), Unity 6 (UI Toolkit, NUnit EditMode tests), `scripts/check.sh`, `tools/web_check.py`.

**Spec:** [docs/superpowers/specs/2026-10-23-civ-movement-design.md](../specs/2026-10-23-civ-movement-design.md) (revision 2). Read it first; section numbers below are the spec's. Tracking issue #1398.

## Global Constraints

- The owner approved the spec and this plan on 23 October 2026: "Approve, start building" (recorded on #1402). The owner's answers of 23 October 2026 are built in: OQ-1 "One unit per attack, Civ" (Tasks 5, 11), OQ-2 "Instant" capture (Tasks 8a, 12a, after #1324 and #1327 merge), OQ-3 "No, strict like Civ VI" with the full-points exception (Tasks 2a, 7), old saves "Refuse with a clear message" (Task 6). OQ-A "No force": the plan builds no force-end key (Task 13a). The capture answer: "Yes, answer at End turn": `ResolveCapture` stays deferred (Task 8a).
- Unchanged and not to be touched: Q-MOVE-1 to Q-MOVE-5 (terrain costs, points, stacking, no zone of control, no transport), D-A6's resolver order, R-ENEMY-001 to R-ENEMY-006, `Movement.Toward` (the enemy's march), `Movement.Closed` (#1324 and #1327 edit it).
- Instant orders: `Move`, and `UnitOrder` with `attack`, `go_to`, `fortify` or `hold`. Send home and every other order, `ResolveCapture` included, stay deferred (spec 3.2). `Simulation.IsInstant` is the only definition.
- End turn's batch order: route step, committed orders in click order, deferred orders in queue order (spec 3.3).
- Every Why line that names a unit carries its key, unit snapshots and values (`UnitLines.Line`, Task 4a; #1399); Tasks 5 and 8 write their new lines that way.
- Every PR: one slice, `Refs #1398`, a `changelog.d/` fragment for user-visible paths, rule text in `docs/rules/mvp.md` in the same PR as the code, a citing test for every new or reworded R-ID (`uv run scripts/oss/rule_ids.py`).
- Sim gates: `bash scripts/check.sh origin/main` ends with "ALL CHECKS PASSED"; `dotnet test sim/Statecraft.sln` with 0 NEW failures against main; no golden hash changes unless the task says so. A moved golden: `STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln`, a history row in `docs/determinism.md`, the SMOKE lines in `RELEASING.md`; never hand-edit a golden.
- Unity gates: everything above, plus `uv run tools/build_sim_for_unity.py`, a FULL EditMode run on the PR merged with current origin/main (`export UNITY_MCP_SERVER_PATH=<existing empty file>`, never `-quit` with `-runTests`, XML total > 0, 0 NEW failures), the WebGL gzip `web_check --query "golden&smoke=5"` with every manifest line and the SMOKE line, and the `smoke-orders` run (both commands in `docs/determinism.md:42-56`), and before/after screenshots in English and Hebrew, same state, camera and zoom, on `captures/pr-<n>`. Drop the settings churn after any Unity run: `git checkout -- unity/Assets/Settings unity/ProjectSettings`.
- Copy: `docs/WRITING_UI.md`, `docs/WRITING_UI_HE.md` (binding for new strings since #1395) and `docs/UX_GLOSSARY.yaml`. Keys and placeholders match in `strings.en.yaml` and `strings.he.yaml`. Hebrew is agent-drafted and marked for the native reviewer in a comment above the keys. No em dashes anywhere.
- Commits use the repository's author identity and the trailers the lane rules give; AI assistance is disclosed in each PR description (`CONTRIBUTING.md`). Never force-push; plain-merge origin/main.
- Every test below is written as code. Where a step says "run; expect failure", the failure named is the one to see before implementing.

## Review Focus

1. **Reload in the middle of a turn after an attack.** The player expects the attack, its losses and the Arms spent to be there after reload, with the same result (no undo, no re-roll). Pinned in Task 4 (`AReloadKeepsTheOpenTurn`), Task 6 (`AReloadKeepsAnAttacksResult`) and Task 9 (`AutosaveWritesEveryCommittedOrder`, through the real autosave file).
2. **A deferred order changed after an attack.** Withdrawing or replacing a queued policy (a vote draws from the RNG) after an attack must not change the attack's result on screen or at End turn. Pinned in Task 4 (`DeferredOrdersRunAfterCommittedOnes`) and Task 6 (`AVoteQueuedAfterAnAttackDoesNotChangeIt`).
3. **"A unit needs orders" at phone width and in Hebrew.** The End turn label is longer; it must fit the button at 390 px, in both languages, with the hourglass. Pinned in Task 13c (`TheBlockedEndTurnLabelFitsAtPhoneWidth`).
4. **A unit that leaves the map mid-turn** (destroyed in its own attack or surrendered), or one with a Send home queued, while its card is open or it is next in the cycle: the card closes, the cycle skips it, nothing throws. Pinned in Task 11 (`AUnitDestroyedInItsOwnAttackClosesItsCard`) and Task 13a (`TheCycleSkipsUnitsGoingHome`).
5. **A go-to whose destination changes under it** (an enemy unit moves onto it, or the hex fills up): the unit stops, needs orders and is in the cycle. Pinned in Task 8 (`ARouteToAHexAnEnemyTookStops`, `ARouteWhoseHexIsFullStopsOnArrival`) and Task 13a (`AStoppedRouteNeedsOrders`).

---

## Slices, who drafts them, and estimates

Estimates are for one focused agent lane including the gates; Unity slices include one full EditMode run and a WebGL build (about 1.5 hours of machine time each).

| Slice | Tasks | Needs | Drafted by | Estimate | Main risk |
|---|---|---|---|---|---|
| S1 sim: preview, points left, needs-orders, the full-points hex | 1, 2, 2a | none (the owner approved the spec, 23 October 2026) | Codex (cx135's draft on `codex/civ-move-reach-spent` starts Task 1) | 0.5 day | `RunOrders` refactor touching `Step`: guarded by the unchanged goldens |
| S2 sim: session and save | 3, 4 | S1 | Codex | 0.5 day | save format: old-save round trips in `SaveGameTests` |
| S5a sim: Why lines carry units: the mechanism and the move, order, send-home and enemy-truce lines | 4a | S2 | Codex (cx137's sweep lists every site) | 0.5 day | constructor overloads on `Cause`: compile-only risk |
| S5b sim: the combat lines | 4b | S5a | Codex | 0.5 day | `Combat.Take` keeps snapshots: engagement order must not change |
| S5c sim: the withdrawal lines | 4c | S5a, #1324 and #1327 merged | Codex | 1 hour | inside #1327's hunk: done only after it merges |
| S3 sim: instant combat | 5, 6 | S5b | Codex | 1 day | the largest rule change: R-COMBAT-001/002/004/007/009 and R-UNIT-005 together |
| S4 sim: go-to | 7, 8 | S5a | Codex | 1 day | route turn numbers against real moves: pinned by `ARouteArrivesOnTheTurnItShowed` |
| S6 sim: capture at the click | 8a | S4, #1324 and #1327 merged | Codex | 0.5 day | #1327's merged capture code must be read first |
| U1 Unity: the bridge and instant moves on screen | 9, 10 | S2, #1396 | Claude lane | 1.5 days | `SimBridge.State` changes meaning for every presenter |
| U5 Unity: units named in Why lines | 10a | S5a, S5b, #1396 | Claude lane | 0.5 day | Hebrew review of 20 new lines |
| U2 Unity: instant attacks on screen | 11 | S3, U1 | Claude lane | 0.5 day | toast ordering after `Changed` |
| U3 Unity: go-to on screen | 12 | S4, U1 | Claude lane | 1 day | route numbers on the map, touch confirm |
| U6 Unity: the capture dialog after a capturing move | 12a | S6, U1 | Claude lane | 2 hours | none beyond the test |
| U4a Unity: End turn waits; Skip | 13a | U1 | Claude lane | 0.5 day | the smoke runs: both must keep their hashes |
| U4b Unity: the cycle and the camera | 13b | U4a | Claude lane | 0.5 day | camera focus helper shared with the guide (#1394) |
| U4c Unity: dimmed tokens and the phone layout | 13c | U4a | Claude lane | 0.5 day | material property blocks on badge renderers |
| D docs: the GDD amendments | 14 | none (the owner approved spec 9.3, 23 October 2026) | Claude lane | 1 hour | approval scope only |

Codex workers cannot run Unity; a Claude lane reviews, runs the gates and lands every Codex draft. Each slice is one PR. Before each: `git fetch`, and check it is not already done on origin/main.

---

### Task 1: The turn preview and points left (S1)

**Files:**
- Modify: `sim/Statecraft.Sim/Commands.cs` (add `PreviewResult` after `StepResult`, `:169-175`)
- Modify: `sim/Statecraft.Sim/Simulation.cs:31-116` (`Preview`, `IsInstant`, `RunOrders` takes `spent`)
- Modify: `sim/Statecraft.Sim/Movement.cs:25-51` (`Points` internal, `PointsLeft`, `Reachable`/`Path` overloads)
- Test: `sim/Statecraft.Sim.Tests/MovementTests.cs` (cx135's four tests), create `sim/Statecraft.Sim.Tests/TurnPreviewTests.cs`
- Docs: `docs/rules/mvp.md:189-191` (R-MOVE-001, R-MOVE-002)

**Interfaces:**
- Produces: `public sealed class PreviewResult { GameState State; IReadOnlyDictionary<int,int> Spent; IReadOnlyList<Cause> Why; IReadOnlyList<Rejection> Rejected; }`; `Simulation.Preview(Content c, GameState state, IReadOnlyList<Command> commands) -> PreviewResult`; `Simulation.IsInstant(Command cmd) -> bool`; `Movement.Reachable(Content, GameState, int unitId, IReadOnlyDictionary<int,int> spent)`; `Movement.Path(Content, GameState, int unitId, int q, int r, IReadOnlyDictionary<int,int> spent)`; `Movement.PointsLeft(Content, GameState, int unitId, IReadOnlyDictionary<int,int> spent) -> int`; `internal static int Movement.Points(Content, Unit)`.

- [ ] **Step 1: Start from cx135's draft.** Copy the four tests and the `Reachable`/`Path` overloads from branch `codex/civ-move-reach-spent` (uncommitted in `/private/tmp/claude-501/-Users-eyalgolan/b56e4751-b38f-4be9-a118-5c60998902ef/scratchpad/cx135-reach-spent/wt`; read its diff in full first). Its `PreviewResult` has `State` and `Spent`; this task adds `Why` and `Rejected`.

- [ ] **Step 2: Write the failing tests** in `TurnPreviewTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>Spec 2026-10-23 section 3: the screen shows Simulation.Preview(turn start, committed orders), the same RunOrders
/// Step runs (R-MOVE-001, R-MOVE-002, R-TURN-004).</summary>
public class TurnPreviewTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    internal static Content Plain() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(), ["corridors.yaml"] = "corridors: []\n"
    })));

    internal static GameState With(Content c, params Unit[] units) => CombatTests.With(c, units);
    internal static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel", strength: 3);

    // R-TURN-004: the preview's Why lines are the start of End turn's: what the player saw is what End turn ran.
    [Fact]
    public void ThePreviewsLinesStartEndTurnsLines()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        var orders = new Command[] { new Move(1, 1, 1), new Move(1, 2, 1) };
        var now = Simulation.Preview(c, s, orders);
        var ended = Simulation.Step(c, s, orders);
        Assert.Equal(now.Why.Select(w => w.Message), ended.Why.Take(now.Why.Count).Select(w => w.Message));
        Assert.Equal(2, now.Spent[1]);
        Assert.Empty(now.Rejected);
    }

    // R-TURN-004: a refused order is in Rejected with the sim's reason, and the state is as it was.
    [Fact]
    public void ARefusedOrderIsRejectedAndChangesNothing()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 5, 1) });
        Assert.Equal("That hex is out of reach this turn", Assert.Single(now.Rejected).Reason);
        Assert.Equal(s.Hash(), now.State.Hash());
    }

    // A game that is over takes no order, as Step (Simulation.cs:42-46).
    [Fact]
    public void AGameThatIsOverTakesNoOrder()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        s.GameOver = GameOverCodes.MvpEnded;
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 1, 1) });
        Assert.StartsWith("The game is over", Assert.Single(now.Rejected).Reason);
    }

    // R-TURN-004 (spec 3.2): moves, go-to, attacks, fortify and hold are instant; Send home and the rest wait for End turn.
    [Fact]
    public void WhichOrdersAreInstant()
    {
        Assert.True(Simulation.IsInstant(new Move(1, 1, 1)));
        Assert.True(Simulation.IsInstant(new UnitOrder(1, Orders.Fortify)));
        Assert.True(Simulation.IsInstant(new UnitOrder(1, Orders.Hold)));
        Assert.True(Simulation.IsInstant(new UnitOrder(1, Combat.Attack, 2, 2)));
        Assert.False(Simulation.IsInstant(new UnitOrder(1, Demobilization.SendHome)));
        Assert.False(Simulation.IsInstant(new BuyArms(1)));
        Assert.False(Simulation.IsInstant(new ResolveCapture("lod", SettlementControl.Raid)));
        Assert.False(Simulation.IsInstant(new SetPolicy("rationing", 0.5)));
    }

    // R-MOVE-001: the points left are the type's less what the unit spent this turn.
    [Fact]
    public void PointsLeftCountTheTurnsMoves()
    {
        var c = Plain();
        var s = With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 2, 1) });
        Assert.Equal(1, Movement.PointsLeft(c, now.State, 1, now.Spent));
        Assert.Equal(3, Movement.PointsLeft(c, s, 1, new Dictionary<int, int>()));
    }
}
```

`CombatTests.With` is already `internal static` (`CombatTests.cs:24`); `MovementTests.Board` is `internal static` (`MovementTests.cs:26`).

- [ ] **Step 3: Run them to see them fail.** `dotnet test sim/Statecraft.sln --filter "FullyQualifiedName~TurnPreviewTests|FullyQualifiedName~MovementTests"`. Expected: compile errors for `Simulation.Preview`, `IsInstant`, `PointsLeft`, `PreviewResult.Why`.

- [ ] **Step 4: Implement.** In `Commands.cs`:

```csharp
    /// <summary>The state after the turn's committed orders (spec 2026-10-23 section 3, R-TURN-004), before End turn: what
    /// the screen shows, the movement points each unit has spent, the turn's Why lines so far and the refused orders.</summary>
    public sealed class PreviewResult
    {
        public GameState State { get; }
        public IReadOnlyDictionary<int, int> Spent { get; }
        public IReadOnlyList<Cause> Why { get; }
        public IReadOnlyList<Rejection> Rejected { get; }
        public PreviewResult(GameState state, IReadOnlyDictionary<int, int> spent, IReadOnlyList<Cause> why, IReadOnlyList<Rejection> rejected)
        { State = state; Spent = spent; Why = why; Rejected = rejected; }
    }
```

In `Simulation.cs`, `RunOrders` takes the `spent` dictionary from its caller (`Step` and `DelegatedTo` pass a new one), and:

```csharp
        /// <summary>R-TURN-004: the state after <paramref name="commands"/> run as End turn would run them first (RunOrders,
        /// shared with Step), on a clone. Never mutates <paramref name="state"/>.</summary>
        public static PreviewResult Preview(Content c, GameState state, IReadOnlyList<Command> commands)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            if (commands.Any(x => x == null)) throw new ArgumentException("Commands cannot contain null.", nameof(commands));
            CommandText.Validate(commands);
            var s = state.Clone();
            var why = new List<Cause>(); var rejected = new List<Rejection>(); var spent = new Dictionary<int, int>();
            if (s.GameOver != null)
                foreach (var cmd in commands) rejected.Add(new Rejection(cmd, $"The game is over: {PlayerText.GameOver(s.GameOver)}"));
            else RunOrders(c, s, commands, spent, why, rejected);
            return new PreviewResult(s, spent, why, rejected);
        }

        /// <summary>R-TURN-004: a move, a go-to, an attack, fortify or hold takes effect at once; Send home and every other
        /// order waits for End turn (spec 2026-10-23 section 3.2).</summary>
        public static bool IsInstant(Command cmd) => cmd is Move || (cmd is UnitOrder o && o.Order != Demobilization.SendHome);
```

In `Movement.cs`: make `Points` `internal static`, take cx135's overloads, and add:

```csharp
        /// <summary>R-MOVE-001: the movement points unit <paramref name="unitId"/> has left this turn; 0 for an unknown unit.</summary>
        public static int PointsLeft(Content c, GameState s, int unitId, IReadOnlyDictionary<int, int> spent)
        {
            if (!s.Units.TryGetValue(unitId, out var u)) return 0;
            spent.TryGetValue(unitId, out var used);
            return Math.Max(0, Points(c, u) - used);
        }
```

- [ ] **Step 5: Run the tests to see them pass**, then the whole suite: `dotnet test sim/Statecraft.sln`. Expected: all pass; `DeterminismTests` and `GoldenManifestTests` unchanged (no golden moves).

- [ ] **Step 6: Rule text.** R-MOVE-001 (`mvp.md:189`): after "the next turn has them all again", add "The points left are the type's points less those the unit spent this turn (`Movement.PointsLeft`)." R-MOVE-002 (`:191`): "`Movement.Reachable(c, s, unit, spent)` and `Movement.Path(c, s, unit, q, r, spent)` search with the points left; without `spent`, with all of them." Cite both in the new tests' comments (already there).

- [ ] **Step 7: Commit.** `git add sim docs/rules/mvp.md && git commit -m "sim: the turn preview and movement points left (Refs #1398)"`

### Task 2: Which units need orders (S1)

**Files:**
- Modify: `sim/Statecraft.Sim/Movement.cs`
- Test: `sim/Statecraft.Sim.Tests/TurnPreviewTests.cs`
- Docs: `docs/rules/mvp.md` (new R-MOVE-010 after R-MOVE-007, `:201`)

**Interfaces:**
- Consumes: `Movement.Points` (Task 1).
- Produces: `Movement.NeedsOrders(Content c, GameState s, IReadOnlyDictionary<int,int> spent) -> IReadOnlyList<int>` (ascending id).

- [ ] **Step 1: Write the failing test.**

```csharp
    // R-MOVE-010: the player's military units with points that have neither moved nor attacked this turn and have no order.
    [Fact]
    public void NeedsOrdersListsIdleUnitsThatHaveNotMoved()
    {
        var c = Plain();
        var fortified = new Unit(3, "soldier", 4, 1, side: "israel", strength: 3, order: Orders.Fortify, orderTarget: "4,1");
        var held = new Unit(4, "soldier", 5, 1, side: "israel", strength: 3, order: Orders.Hold);
        var s = With(c, Soldier(1, 0, 1), Soldier(2, 0, 2), fortified, held,
                     new Unit(5, "enemy", 5, 2, side: "egypt", strength: 3), new Unit(6, "halutz", 3, 2));
        var now = Simulation.Preview(c, s, new Command[] { new Move(2, 1, 2) });
        Assert.Equal(new[] { 1 }, Movement.NeedsOrders(c, now.State, now.Spent));
    }
```

(Unit 2 moved, 3 is fortified, 4 holds, 5 is the enemy's, 6 is a halutz with no points. The fixture's halutz type id is `halutz`, `Fixture.GddUnitTypes`.)

- [ ] **Step 2: Run it; expect a compile error** for `NeedsOrders`.

- [ ] **Step 3: Implement.**

```csharp
        /// <summary>R-MOVE-010: the player's military units, in ascending id, that have movement points, have neither moved nor
        /// attacked this turn (nothing in <paramref name="spent"/>) and have no order (fortify, fortifying, hold and go-to are
        /// orders). End turn waits for them (spec 2026-10-23 section 7); Skip is the UI's and not in the sim.</summary>
        public static IReadOnlyList<int> NeedsOrders(Content c, GameState s, IReadOnlyDictionary<int, int> spent) =>
            s.Units.Values.Where(u => u.Side == PlayerSide && Points(c, u) > 0 && u.Order == ""
                    && !(spent.TryGetValue(u.Id, out var used) && used > 0))
                .Select(u => u.Id).ToList();
```

- [ ] **Step 4: Run it and the suite; expect PASS.**

- [ ] **Step 5: Rule text.** Add after R-MOVE-007: "R-MOVE-010: A unit needs orders when it is one of the player's military units, its type has movement points, it has neither moved nor attacked this turn, and it has no order (`fortify`, `fortifying`, `hold` and `go_to` are orders) (`Movement.NeedsOrders`, ascending id). End turn waits until no unit needs orders, apart from those the player skips this turn (the owner, 23 October 2026: \"Block like Civ\"; spec 2026-10-23 section 7). Skipping is not an order and changes nothing in the game."

- [ ] **Step 6: Commit.** `git commit -m "sim: which units need orders (Refs #1398)"`. Open the S1 PR (Tasks 1 and 2), run the sim gates, land.

### Task 2a: With full points a unit can always move one hex (S1, OQ-3)

**Files:**
- Modify: `sim/Statecraft.Sim/Movement.cs:30-84`, `:149-178` (`Search` takes `fresh`; `Reachable`, `Path`, `Move` pass it)
- Test: `sim/Statecraft.Sim.Tests/MovementTests.cs`
- Docs: `docs/rules/mvp.md:189` (R-MOVE-001)

**Interfaces:**
- Consumes: `Movement.Reachable(..., spent)`, `Path(..., spent)` (Task 1).
- Produces: `Search(Content, GameState, Unit, int budget, bool fresh = false)`: with `fresh`, a neighbour of the unit's own hex that costs more than `budget` is reached at exactly `budget` (all the points).

- [ ] **Step 1: Write the failing tests** in `MovementTests.cs` (its fixture: soldier 3 points, desert 3, hills 2; add a two-point type to its `Types` string: `"  - { id: gun, kind: unit, side: israel, class: infantry, movement_points: 2 }\n"`):

```csharp
    // R-MOVE-001 (owner, 23 October 2026, OQ-3: "No, strict like Civ VI": a unit needs the full cost of the next hex, "with
    // full points it can always move one hex"): a unit that has not moved may enter one neighbour whatever it costs, and
    // that spends all its points.
    [Fact]
    public void WithFullPointsAUnitCanAlwaysMoveOneHex()
    {
        var c = Load(Board((3, 1, "desert")));
        var s = With(c, new Unit(1, "gun", 2, 1, side: "israel"));
        Assert.Equal(2, Movement.Reachable(c, s, 1)[(3, 1)]);   // all of its two points
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 3, 1), new Move(1, 4, 1) });
        Assert.Equal((3, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Equal("That hex is out of reach this turn", now.Rejected.Single().Reason);   // no points left
    }

    // R-MOVE-001: only with full points: after a move the strict rule holds.
    [Fact]
    public void AfterAMoveTheNextHexNeedsItsFullCost()
    {
        var c = Load(Board((4, 1, "desert")));
        var s = With(c, new Unit(1, "gun", 2, 1, side: "israel"));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 3, 1) });
        Assert.False(Movement.Reachable(c, now.State, 1, now.Spent).ContainsKey((4, 1)));
        Assert.Equal("That hex is out of reach this turn", Simulation.Preview(c, s, new Command[] { new Move(1, 3, 1), new Move(1, 4, 1) }).Rejected.Single().Reason);
    }

    // R-MOVE-001: the exception reaches one hex only, never a hex beyond it.
    [Fact]
    public void TheExceptionIsOneHexOnly()
    {
        var c = Load(Board((3, 1, "desert")));
        var s = With(c, new Unit(1, "gun", 2, 1, side: "israel"));
        Assert.False(Movement.Reachable(c, s, 1).ContainsKey((4, 1)));
    }
```

- [ ] **Step 2: Run; expect the first test to fail** (`Reachable` has no 3,1 for the gun).

- [ ] **Step 3: Implement.** In `Search`, after `var total = cost + step;`: `if (fresh && cost == 0 && total > budget) total = budget;   // R-MOVE-001: with full points a unit can always move one hex (OQ-3)`. `Reachable`, `Path` and `Move` pass `fresh: used == 0` (the points the unit spent this turn are 0). `Toward` (the enemy's march) passes nothing and is unchanged; the enemy's `Move` gets the same exception, which changes nothing in play (every enemy type has 3 points and no hex costs more than 3).

- [ ] **Step 4: Run MovementTests, EnemyAITests and the suite; expect PASS, no golden moves.**

- [ ] **Step 5: Rule text.** R-MOVE-001: add "A unit needs the full cost of the next hex, with one exception: a unit that has not moved this turn may enter one neighbouring land hex whatever it costs, and that move spends all its points (the owner, 23 October 2026, OQ-3: \"No, strict like Civ VI\", \"with full points it can always move one hex\")."

- [ ] **Step 6: Commit.** `git commit -m "sim: with full points a unit can always move one hex (Refs #1398)"`

### Task 3: The save's open turn (S2)

**Files:**
- Modify: `sim/Statecraft.Sim/SaveGame.cs:30-186`
- Test: `sim/Statecraft.Sim.Tests/SaveGameTests.cs`
- Docs: `docs/rules/mvp.md:51-55` (new R-SAVE-003)

**Interfaces:**
- Produces: `SaveGame.SchemaVersion = 3`; `public int Version { get; }` (the parsed schema, 3 for a new save); `public IReadOnlyList<Command> Open` (fresh objects, like `Turns`); `internal void SetOpen(IReadOnlyList<Command> commands)`; `Record` clears the open turn.

- [ ] **Step 1: Write the failing tests** in `SaveGameTests.cs`:

```csharp
    // R-SAVE-003: the open turn's committed orders are written after the turns and read back in order.
    [Fact]
    public void TheOpenTurnRoundTrips()
    {
        var save = new SaveGame(new string('a', 64), 7, Mode.Historical, SaveGame.GddRuleset, new string('b', 64));
        save.SetOpen(new Command[] { new Move(1, 2, 3), new UnitOrder(1, Orders.Hold) });
        var text = save.Write();
        Assert.StartsWith("statecraft-save 3\n", text);
        Assert.Contains("open\nmove 1 2 3\norder 1 hold\nend ", text);
        var back = SaveGame.Parse(text);
        Assert.Equal(3, back.Version);
        Assert.Equal(new[] { "move", "unit_order" }, back.Open.Select(x => x.Kind));
    }

    // R-SAVE-003: a finished turn ends the open one.
    [Fact]
    public void RecordingATurnClearsTheOpenTurn()
    {
        var c = TurnPreviewTests.Plain();
        var session = GameSession.New(c, 1, Mode.Historical);
        session.Save.SetOpen(new Command[] { new Move(0, 1, 1) });
        session.Step(new Command[0]);
        Assert.Empty(session.Save.Open);
        Assert.DoesNotContain("\nopen\n", session.Save.Write());
    }

    // R-SAVE-002 is unchanged: an older schema has no open turn, and a line "open" in it is refused.
    [Fact]
    public void ASchema2SaveWithAnOpenLineIsRefused()
    {
        var text = "statecraft-save 2\ncontent " + new string('a', 64) + "\nseed 1\nmode Historical\nruleset gdd-0.1\nturn\nopen\nend " + new string('b', 64) + "\n";
        Assert.Throws<SaveException>(() => SaveGame.Parse(text));
    }
```

- [ ] **Step 2: Run; expect compile errors** (`SetOpen`, `Open`, `Version`).

- [ ] **Step 3: Implement.** `SchemaVersion = 3`. A `readonly List<string> open` of canonical lines. `Write`: after the turns, if `open.Count > 0`, `open\n` then each line. `Parse`: for `version >= 3` a line `open` switches the target list to `open` (a second `open`, or a `turn` after it, throws `SaveException(line, "'open' must come last, once")`); for `version < 3` an `open` line is an unknown command (`ParseCommand` already throws). `Record` ends with `open.Clear();`. `SetOpen` stores `commands.Select(Format)`. `Version` is the parsed `version` (the constructor sets `SchemaVersion`). Update the format comment at `:30-46` with the `open` line.

- [ ] **Step 4: Run SaveGameTests and the suite; expect PASS.** `SaveGameTests` that pin `statecraft-save 2` text change to 3: update them in this step and say so in the PR.

- [ ] **Step 5: Rule text.** New "R-SAVE-003: Save schema 3 adds the open turn: after the last `turn`, a line `open` and the turn in progress's committed orders (R-TURN-004), one a line; the `end` hash is still the state's after the last finished turn. Loading replays the turns, then commits the open orders again; one refused on the same content means the save is damaged (`SaveException`), and on other content the rest are dropped with the `replay_differs` warning. A schema 2 save has no open turn and loads as before (but see R-SAVE-004 in the combat slice)." Leave the R-SAVE-004 sentence out until Task 6.

- [ ] **Step 6: Commit.** `git commit -m "sim: the save keeps the turn in progress (schema 3) (Refs #1398)"`

### Task 4: The session commits orders (S2)

**Files:**
- Modify: `sim/Statecraft.Sim/GameSession.cs`
- Test: create `sim/Statecraft.Sim.Tests/GameSessionTurnTests.cs`
- Docs: `docs/rules/mvp.md:11-17` (new R-TURN-004)

**Interfaces:**
- Consumes: `Simulation.Preview`, `Simulation.IsInstant` (Task 1); `SaveGame.SetOpen`, `Open` (Task 3).
- Produces: `GameSession.Committed : IReadOnlyList<Command>`; `GameSession.Now : PreviewResult`; `GameSession.Commit(Command) -> string?` (null, or the sim's reason; throws `ArgumentException` for a deferred kind); `GameSession.EndTurn(IReadOnlyList<Command> deferred) -> StepResult`; `GameSession.Step(commands)` throws `InvalidOperationException` while `Committed` is not empty.

- [ ] **Step 1: Write the failing tests.**

```csharp
using System;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>R-TURN-004 and R-SAVE-003 (spec 2026-10-23 sections 3.3 and 3.4): committed orders are final, shown at once,
/// run first at End turn, and survive a reload.</summary>
public class GameSessionTurnTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    static GameSession Fresh() => GameSession.New(C, GoldenRun.Seed, Mode.Historical);

    [Fact]
    public void ACommittedMoveShowsAtOnceAndEndTurnRunsIt()
    {
        var session = Fresh();
        var move = GoldenRun.SmokeMove(C, session.State);
        var before = (session.State.Units[move.Unit].Q, session.State.Units[move.Unit].R);
        Assert.Null(session.Commit(move));
        Assert.Equal((move.Q, move.R), (session.Now.State.Units[move.Unit].Q, session.Now.State.Units[move.Unit].R));
        Assert.Equal(before, (session.State.Units[move.Unit].Q, session.State.Units[move.Unit].R));   // the turn's start is untouched
        var ended = session.EndTurn(new Command[0]);
        Assert.Equal(GoldenRun.PlaySmokeOrders(C).Hash(), ended.State.Hash());   // the smoke-orders golden, unchanged
        Assert.Empty(session.Committed);
    }

    [Fact]
    public void ARefusedOrderIsNotCommitted()
    {
        var session = Fresh();
        Assert.Equal("That unit is already there", session.Commit(new Move(0, session.State.Units[0].Q, session.State.Units[0].R)));
        Assert.Empty(session.Committed);
        Assert.Throws<ArgumentException>(() => session.Commit(new BuyArms(1)));
    }

    // R-TURN-004: deferred orders run after the committed ones, whatever order they were given in.
    [Fact]
    public void DeferredOrdersRunAfterCommittedOnes()
    {
        var session = Fresh();
        var move = GoldenRun.SmokeMove(C, session.State);
        session.Commit(move);
        var r = session.EndTurn(new Command[] { new BuyArms(1) });
        Assert.Equal(new Command[] { move }.Concat(new Command[] { new BuyArms(1) }).Select(x => x.Kind),
                     session.Save.Turns.Last().Select(x => x.Kind));
    }

    // R-SAVE-003: a reload in the middle of a turn keeps what was done.
    [Fact]
    public void AReloadKeepsTheOpenTurn()
    {
        var session = Fresh();
        var move = GoldenRun.SmokeMove(C, session.State);
        session.Commit(move);
        var loaded = GameSession.Load(C, session.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(session.Now.State.Hash(), loaded.Now.State.Hash());
        Assert.Equal(new[] { "move" }, loaded.Committed.Select(x => x.Kind));
    }

    [Fact]
    public void StepRefusesWhileOrdersAreCommitted()
    {
        var session = Fresh();
        session.Commit(GoldenRun.SmokeMove(C, session.State));
        Assert.Throws<InvalidOperationException>(() => session.Step(new Command[0]));
    }
}
```

- [ ] **Step 2: Run; expect compile errors.**

- [ ] **Step 3: Implement** in `GameSession.cs`:

```csharp
        readonly List<Command> committed = new List<Command>();
        /// <summary>R-TURN-004: the turn's committed orders, in the order given: final, shown at once, run first at End turn.</summary>
        public IReadOnlyList<Command> Committed => committed;
        /// <summary>The state the screen shows: the turn's start after its committed orders (Simulation.Preview).</summary>
        public PreviewResult Now { get; private set; }

        // in the constructor, after State is set:
        //     Now = Simulation.Preview(c, s, Array.Empty<Command>());

        /// <summary>R-TURN-004: commits a move or a unit order. Null when taken (it is shown at once and saved in the open
        /// turn, R-SAVE-003), else the sim's reason, with nothing changed. A deferred order is a programmer error.</summary>
        public string? Commit(Command cmd)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            if (!Simulation.IsInstant(cmd)) throw new ArgumentException($"'{cmd.Kind}' waits for End turn; only a move or a unit order is committed.", nameof(cmd));
            var next = Simulation.Preview(Content, State, committed.Concat(new[] { cmd }).ToList());
            var refused = next.Rejected.FirstOrDefault(r => ReferenceEquals(r.Command, cmd));
            if (refused != null) return refused.Reason;
            committed.Add(cmd);
            Now = next;
            Save.SetOpen(committed);
            return null;
        }

        /// <summary>Ends the turn: the committed orders, then <paramref name="deferred"/>, as one batch (R-TURN-004).</summary>
        public StepResult EndTurn(IReadOnlyList<Command> deferred)
        {
            if (deferred == null) throw new ArgumentNullException(nameof(deferred));
            var batch = committed.Concat(deferred).ToList();
            var r = Simulation.Step(Content, State, batch);
            Save.Record(batch, r.State);
            Previous = State; State = r.State; BeforeLast = Last; Last = r;
            committed.Clear();
            Now = Simulation.Preview(Content, State, committed);
            return r;
        }

        /// <summary>Runs one turn with these commands and records it (no orders may be committed).</summary>
        public StepResult Step(IReadOnlyList<Command> commands)
        {
            if (committed.Count > 0) throw new InvalidOperationException("Orders are committed this turn: end it with EndTurn.");
            return EndTurn(commands);
        }
```

In `Load`, after the turns replay and the hash check, commit `saved.Open` in order: a refusal throws `new SaveException(0, $"The open turn's order '{cmd.Kind}' was refused: {reason}")` when `sameContent`, else adds `ReplayDiffers` (once) and stops.

- [ ] **Step 4: Run the new tests and the suite; expect PASS.** The CLI (`sim/Statecraft.Cli/Program.cs`) keeps calling `Step`.

- [ ] **Step 5: Rule text.** New R-TURN-004 under "## Turns": "R-TURN-004: A move (`Move`) and a unit order (`UnitOrder`) take effect at once and are final (the owner, 23 October 2026: \"No undo, like Civ\"): each is committed to the turn in progress, in the order given, and the game shows the state after them (`Simulation.Preview`). Every other order waits for End turn, may be changed or withdrawn until then, and costs and runs as before. End turn runs the route step (R-MOVE-009), then the committed orders in the order given, then the waiting orders in the order queued, as one batch (`Simulation.Step`), so a waiting order never changes the result of a committed one (spec 2026-10-23 section 3)."

- [ ] **Step 6: Commit and open the S2 PR** (Tasks 3, 4). Gates: sim gates. No golden moves.

### Task 4a: Why lines carry the units they name: the mechanism and the move, order, send-home and truce lines (S5a, #1399)

**Files:**
- Modify: `sim/Statecraft.Sim/Cause.cs` (`Key`, `Units`, `Args`); create `sim/Statecraft.Sim/UnitLines.cs` (`UnitRef`, `WhyKeys`, `UnitLines.Line`)
- Modify: `sim/Statecraft.Sim/Movement.cs:78-82`, `Orders.cs:55-57`, `:69-73`, `Demobilization.cs:73-74`, `EnemyAI.cs:59-60`, `Combat.cs:75-82` (the truce line; Task 5 deletes it later)
- Test: create `sim/Statecraft.Sim.Tests/UnitLinesTests.cs`
- Read first: the sweep `/private/tmp/claude-501/-Users-eyalgolan/b56e4751-b38f-4be9-a118-5c60998902ef/scratchpad/cx137-raised-names/wt/raised-unit-sweep.md` (all 18 paths), and spec 8.4.

**Interfaces:**
- Produces: `public sealed class UnitRef { int Id; string Type; string Side; string HomeCity; public UnitRef(Unit u) }`; `Cause.Key : string` ("" for none), `Cause.Units : IReadOnlyList<UnitRef>`, `Cause.Args : IReadOnlyList<string>`; `public static class WhyKeys` with one `const string` per line and `public static readonly IReadOnlyList<string> All`; `internal static Cause UnitLines.Line(string key, string message, IReadOnlyList<Unit> units, params string[] args)` (system `map`, target the first unit's id, delta 0).
- The keys and their units and args (the Unity strings in Task 10a use the same order: the units' names first, then the args):

| Key | Units | Args | Written by |
|---|---|---|---|
| `unit_moved` | the unit | hex, cost | 4a |
| `fortify_lost` | the unit | hex | 4a |
| `fortifying` | the unit | hex | 4a |
| `holds` | the unit | hex | 4a |
| `attack_ended_truce` | the unit | hex | 4a |
| `sent_home` | the unit | people, city id | 4a |
| `attack_ordered`, `attack_ended`, `holds_fire` | the unit | hex | 4b (the enemy's standing attacks after Task 5) |
| `attack_called_off` | the unit | hex, need, stock | 4b |
| `engagement` | the attackers, then the defenders (each in id order) | hex, ratio, attackers lost (people), defenders lost (people), number of attackers | 4b |
| `destroyed` | the unit | | 4b |
| `retreated` | the unit | hex | 4b |
| `surrendered_prisoners`, `surrendered_held` | the unit | people | 4b |
| `withdrew` | the unit | place id, hex | 4c |
| `on_its_way`, `stopped_no_route`, `stopped_full`, `arrived` | the unit | hex | Task 8 |

- [ ] **Step 1: Write the failing tests.**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>#1399 (spec 2026-10-23 section 8.4): every Why line that names a unit carries its key, a snapshot of each unit
/// it names and its values, so the UI can name the unit in the player's language, even once the unit has left the state.</summary>
public class UnitLinesTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The real start, plus a raised recruit (an id past the starting forces, home Tel Aviv) on Golani's hex.</summary>
    static (GameState s, int raised) Start()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var id = s.NextId;
        var golani = s.Units[0];
        s.Units.Add(id, new Unit(id, "recruit", golani.Q, golani.R, side: "israel", strength: 2, homeCity: "tel_aviv"));
        s.NextId = id + 1;
        return (s, id);
    }

    static void Names(Cause line, string key, GameState s, int id)
    {
        Assert.Equal(key, line.Key);
        var u = Assert.Single(line.Units);
        Assert.Equal((id, s.Units[id].Type, s.Units[id].HomeCity), (u.Id, u.Type, u.HomeCity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AMoveNamesItsUnit(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        var to = Movement.Reachable(C, s, id).Keys.First();
        var line = Simulation.Step(C, s, new Command[] { new Move(id, to.Q, to.R) }).Why.Single(w => w.Key == WhyKeys.UnitMoved && w.Units[0].Id == id);
        Names(line, WhyKeys.UnitMoved, s, id);
        Assert.Equal($"{to.Q},{to.R}", line.Args[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FortifyHoldAndFortifyLostNameTheirUnit(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        var hex = $"{s.Units[id].Q},{s.Units[id].R}";
        Names(Simulation.Step(C, s, new Command[] { new UnitOrder(id, Orders.Fortify) }).Why.Single(w => w.Key == WhyKeys.Fortifying), WhyKeys.Fortifying, s, id);
        Names(Simulation.Step(C, s, new Command[] { new UnitOrder(id, Orders.Hold) }).Why.Single(w => w.Key == WhyKeys.Holds), WhyKeys.Holds, s, id);
        var u = s.Units[id];
        s.Units[id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, Orders.Fortify, hex, u.HomeCity);
        var to = Movement.Reachable(C, s, id).Keys.First();
        var lost = Simulation.Step(C, s, new Command[] { new Move(id, to.Q, to.R) }).Why.Single(w => w.Key == WhyKeys.FortifyLost);
        Names(lost, WhyKeys.FortifyLost, s, id);
        Assert.Equal(hex, lost.Args[0]);
    }

    // The unit leaves the state when it goes home; its line keeps its identity.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SentHomeKeepsTheUnitsIdentity(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        foreach (var front in Prisoners.Fronts) s.ChosenOptions["armistice_" + front] = "sign";
        Assert.Null(Demobilization.Check(C, s, id));   // precondition: every armistice signed
        var ended = Simulation.Step(C, s, new Command[] { new UnitOrder(id, Demobilization.SendHome) });
        Assert.False(ended.State.Units.ContainsKey(id));
        Names(ended.Why.Single(w => w.Key == WhyKeys.SentHome), WhyKeys.SentHome, s, id);
    }

    [Fact]
    public void AnEnemyAttackEndedByATruceNamesTheEnemyUnit()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var (id, e) = s.Units.First(kv => kv.Value.Side != "" && kv.Value.Side != Movement.PlayerSide);
        s.Units[id] = new Unit(e.Id, e.Type, e.Q, e.R, e.Service, e.Side, e.Strength, Combat.Attack, $"{e.Q + 1},{e.R}", e.HomeCity);
        s.Date = new DateTime(1948, 6, 15);   // a truce turn (R-ENEMY-006)
        var why = new List<Cause>();
        EnemyAI.Order(C, s, why, new DateTime(1948, 7, 15));
        Assert.Equal(id, why.Single(w => w.Key == WhyKeys.AttackEndedTruce).Units[0].Id);
    }

    [Fact]
    public void EveryKeyIsListedOnce() => Assert.Equal(WhyKeys.All.Distinct().Count(), WhyKeys.All.Count);
}
```

- [ ] **Step 2: Run** `dotnet test sim/Statecraft.sln --filter FullyQualifiedName~UnitLinesTests`; expect compile errors (`Cause.Key`, `WhyKeys`).

- [ ] **Step 3: Implement.** In `Cause.cs`, the constructor gains `string key = "", IReadOnlyList<UnitRef>? units = null, IReadOnlyList<string>? args = null` after `enemyAttackers`, stored as `Key`, `Units` (empty when null) and `Args` (empty when null). `UnitLines.cs`:

```csharp
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>#1399: a unit as a Why line names it, taken when the line is written, so a unit that then leaves the state
    /// (destroyed, surrendered, sent home) keeps its identity. The UI names it: a starting force by its row, a raised unit by
    /// its type and home city.</summary>
    public sealed class UnitRef
    {
        public int Id { get; } public string Type { get; } public string Side { get; } public string HomeCity { get; }
        public UnitRef(Unit u) { Id = u.Id; Type = u.Type; Side = u.Side; HomeCity = u.HomeCity; }
    }

    /// <summary>#1399: the keys of the Why lines that name units; the UI words each as why.line.&lt;key&gt;.</summary>
    public static class WhyKeys
    {
        public const string UnitMoved = "unit_moved", FortifyLost = "fortify_lost", Fortifying = "fortifying", Holds = "holds",
            AttackEndedTruce = "attack_ended_truce", AttackOrdered = "attack_ordered", AttackEnded = "attack_ended",
            HoldsFire = "holds_fire", AttackCalledOff = "attack_called_off", Engagement = "engagement", Destroyed = "destroyed",
            Retreated = "retreated", SurrenderedPrisoners = "surrendered_prisoners", SurrenderedHeld = "surrendered_held",
            SentHome = "sent_home", Withdrew = "withdrew", OnItsWay = "on_its_way", StoppedNoRoute = "stopped_no_route",
            StoppedFull = "stopped_full", Arrived = "arrived";
        public static readonly IReadOnlyList<string> All = new[] { UnitMoved, FortifyLost, Fortifying, Holds, AttackEndedTruce,
            AttackOrdered, AttackEnded, HoldsFire, AttackCalledOff, Engagement, Destroyed, Retreated, SurrenderedPrisoners,
            SurrenderedHeld, SentHome, Withdrew, OnItsWay, StoppedNoRoute, StoppedFull, Arrived };
    }

    static class UnitLines
    {
        /// <summary>A map line naming <paramref name="units"/> (its target the first one's id), with its key and values.</summary>
        internal static Cause Line(string key, string message, IReadOnlyList<Unit> units, params string[] args) =>
            new Cause("map", units[0].Id.ToString(CultureInfo.InvariantCulture), 0, message,
                key: key, units: units.Select(u => new UnitRef(u)).ToList(), args: args);
    }
}
```

Convert this task's sites to `why.Add(UnitLines.Line(WhyKeys.X, <the same English message>, new[] { u }, ...))`, taking the unit snapshot before the state row is replaced or removed. `sent_home` keeps its delta (the strength): build it with the `Cause` constructor and the new named arguments. `Message` is unchanged everywhere.

- [ ] **Step 4: Run UnitLinesTests and the suite.** Expected: PASS; every existing test that reads `Message` unchanged; no golden hash changes (the state hash does not read causes, `GameState.Hash`, `GameState.cs:213-257`).

- [ ] **Step 5: Commit.** `git commit -m "sim: Why lines carry the units they name (Refs #1399, #1398)"`. Open the S5a PR. Sim gates.

### Task 4b: The combat lines carry their units (S5b, #1399)

**Files:**
- Modify: `sim/Statecraft.Sim/Combat.cs:118-124`, `:210-238`, `:248-252`, `:271-300`, `:305-318` (`Take` keeps snapshots)
- Test: `sim/Statecraft.Sim.Tests/UnitLinesTests.cs`

**Interfaces:**
- Consumes: `UnitLines.Line`, `WhyKeys` (Task 4a).
- Produces: `Combat.Take(GameState s, List<int> ids, int loss, List<Unit> removed)` (snapshots, not ids).

- [ ] **Step 1: Write the failing tests** in `UnitLinesTests.cs`, on `CombatTests`' fixture (its table: below 2:1 both sides lose 1; from 2:1 the defenders lose 1 and retreat, whatever the draw):

```csharp
    static Content Fixture6x3() => TurnPreviewTests.Plain();
    static Unit Ours(int id, int q, int r, int strength) => new Unit(id, "soldier", q, r, side: "israel", strength: strength, homeCity: "town_a");
    static Unit Theirs(int id, int q, int r, int strength, int tq, int tr) =>
        new Unit(id, "enemy", q, r, side: "egypt", strength: strength, order: Combat.Attack, orderTarget: $"{tq},{tr}");

    // The engagement names the attackers, then the defenders, and the count of attackers; a destroyed unit keeps its identity.
    [Fact]
    public void TheEngagementAndADestroyedUnitNameTheirUnits()
    {
        var c = Fixture6x3();
        var s = CombatTests.With(c, Ours(1, 2, 1, 1), Theirs(2, 3, 1, 9, 2, 1));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        var fight = why.Single(w => w.Key == WhyKeys.Engagement);
        Assert.Equal(new[] { 2, 1 }, fight.Units.Select(u => u.Id));
        Assert.Equal(("2,1", "1"), (fight.Args[0], fight.Args[4]));
        Assert.False(s.Units.ContainsKey(1));
        var gone = why.Single(w => w.Key == WhyKeys.Destroyed).Units.Single();
        Assert.Equal((1, "soldier", "town_a"), (gone.Id, gone.Type, gone.HomeCity));
    }

    [Fact]
    public void ARetreatNamesTheUnit()
    {
        var c = Fixture6x3();
        var s = CombatTests.With(c, Ours(1, 2, 1, 3), Theirs(2, 3, 1, 9, 2, 1));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        Assert.Equal(1, why.Single(w => w.Key == WhyKeys.Retreated).Units.Single().Id);
    }

    // 0,0's land neighbours on the 6 x 3 board are 1,0 and 0,1: with enemy units on both, the defender has nowhere to go.
    [Fact]
    public void ASurrenderNamesTheUnit()
    {
        var c = Fixture6x3();
        var s = CombatTests.With(c, Ours(1, 0, 0, 3), Theirs(2, 1, 0, 9, 0, 0), new Unit(3, "enemy", 0, 1, side: "egypt", strength: 1));
        var why = new List<Cause>();
        Combat.Resolve(c, s, why);
        var line = why.Single(w => w.Key == WhyKeys.SurrenderedHeld);   // the player's soldiers, held by the enemy (Prisoners.cs:40)
        Assert.Equal((1, "town_a"), (line.Units.Single().Id, line.Units.Single().HomeCity));
    }
```

- [ ] **Step 2: Run; expect failures** (no `Key` on the combat lines).

- [ ] **Step 3: Implement.** Convert the attack-ordered, attack-ended, holds-fire and called-off lines with `UnitLines.Line`; build the engagement's cause with the `Cause` constructor (its target stays the hex, and it keeps `enemyOrder`/`enemyAttackers`, #1297) and `units:` the attackers then the defenders, each in id order, `args:` hex, ratio, attackers lost and defenders lost in people, and the attacker count. `Take` collects removed `Unit`s; the destroyed line uses them. The surrender line splits on `Prisoners.Take`'s two returns (`Prisoners.cs:40-43`) into `surrendered_prisoners` and `surrendered_held`.

- [ ] **Step 4: Run CombatTests, CombatDrawCountTests, ArtilleryTests, EnemyAITests, PrisonersTests and the suite.** Expected: PASS, no golden hash changes (the engagement order and the draws are unchanged).

- [ ] **Step 5: Commit; open the S5b PR.**

### Task 4c: The withdrawal lines (S5c, #1399; after #1324 and #1327 merge)

**Files:** `sim/Statecraft.Sim/SettlementControl.cs` (the withdrawal line and refusal, `:107-120` on `de78253e`; re-read the merged file first); `sim/Statecraft.Sim.Tests/UnitLinesTests.cs`.

- [ ] **Step 1: Write the failing test** (on `CaptureTests`' fixture: town_a at 1,0, an enemy-held target):

```csharp
    [Fact]
    public void AWithdrawalNamesTheUnit()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var s = CombatTests.With(c, CaptureTests.Soldier(1, 1, 0));
        s = Simulation.Step(c, s, new Command[0]).State;   // captured at the turn's end
        var raid = Simulation.Step(c, s, new Command[] { new ResolveCapture("town_a", SettlementControl.Raid) });
        var line = raid.Why.Single(w => w.Key == WhyKeys.Withdrew);
        Assert.Equal((1, "town_a"), (line.Units.Single().Id, line.Args[0]));
    }
```

- [ ] **Step 2: Run; expect failure. Step 3: Implement** the `withdrew` line with `UnitLines.Line` (snapshot from the `moved` list's `From`), and the refusal as "A unit has no free hex next to {name} to withdraw to" (a refusal string; R-CTRL-010 fixes no wording). **Step 4:** run RaidTests, CaptureTests and the suite. **Step 5:** commit; open the S5c PR (`Refs #1399`; close #1399 when U5 has also landed).

### Task 5: The instant attack (S3; OQ-1: "One unit per attack, Civ")

**Files:**
- Modify: `sim/Statecraft.Sim/Combat.cs:73-240` (`Check`, new `Strike`, `Resolve`, remove `EndAttacksForTruce`)
- Modify: `sim/Statecraft.Sim/Simulation.cs:180-181` (attack goes to `Strike`)
- Modify: `sim/Statecraft.Sim/EventSystem.cs:153` (drop the `EndAttacksForTruce` call)
- Modify: `sim/Statecraft.Sim/Reference/ReferencePlayer.cs:43-63`
- Modify: `sim/Statecraft.Sim/Demobilization.cs:21-75` (`Front` and `Check` read the turn's attacked hexes, spec 4.6), `sim/Statecraft.Sim/Commands.cs` (`PreviewResult.Attacked`)
- Test: `sim/Statecraft.Sim.Tests/CombatTests.cs`, `CombatDrawCountTests.cs`, `ArtilleryTests.cs`, `TruceTests` (whichever file holds R-COMBAT-009's tests: `git grep -l "R-COMBAT-009" sim/Statecraft.Sim.Tests`)
- Docs: `docs/rules/mvp.md:217-237` (R-COMBAT-001, -002, -004, -007, -009), `:163` (R-UNIT-005); `docs/determinism.md` (new "Combat draws" section)
- Test also: `sim/Statecraft.Sim.Tests/ArtilleryTests.cs`, `DemobilizationTests.cs`

**Interfaces:**
- Consumes: `Movement.Points`, `Movement.PointsLeft` (Task 1); `spent` in `Execute`.
- Produces: `Combat.Check(Content, GameState, int unitId, int? q, int? r, bool enemy = false, IReadOnlyDictionary<int,int>? spent = null) -> string?`; `internal static string? Combat.Strike(Content, GameState, int unitId, int? q, int? r, Dictionary<int,int> spent, Dictionary<int,(int Q, int R)> attacked, List<Cause> why)`; `PreviewResult.Attacked : IReadOnlyDictionary<int,(int Q, int R)>` (the hex each unit attacked this turn; `RunOrders` keeps it beside `spent`); `Demobilization.Check(Content, GameState, int unitId, IReadOnlyDictionary<int,(int Q, int R)>? attacked = null)`. `Combat.Give` stays for the enemy only.

- [ ] **Step 1: Write the failing tests** in `CombatTests.cs` (it has `Load`, `With`, `Soldier(id, q, r, strength)`, `Enemy(...)`, `Step`):

```csharp
    // R-COMBAT-001 (owner, 23 October 2026: "Instant, like Civ"): the attack is fought when it is given; the unit keeps no order.
    [Fact]
    public void AnAttackIsFoughtWhenGiven()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1, 6), Enemy(2, 3, 1, 1));
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 1) });
        Assert.Empty(now.Rejected);
        Assert.Contains(now.Why, w => w.Message.StartsWith("The attack on hex 3,1"));
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Equal(s.Arms - 1, now.State.Arms);   // the fixture's arms_per_attack
    }

    // R-COMBAT-001: the attack ends the unit's movement; a unit with no points left cannot attack.
    [Fact]
    public void AnAttackEndsTheUnitsMovement()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1, 6), Enemy(2, 3, 1, 1));
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 1), new Move(1, 2, 2) });
        Assert.Equal(0, Movement.PointsLeft(c, now.State, 1, now.Spent));
        Assert.Equal("That hex is out of reach this turn", now.Rejected.Single().Reason);
        // Three one-point moves on the fixture's coastal plain spend all three points, ending on 2,2, next to 3,1.
        var tired = Simulation.Preview(c, s, new Command[] { new Move(1, 2, 2), new Move(1, 2, 1), new Move(1, 2, 2), new UnitOrder(1, Combat.Attack, 3, 1) });
        Assert.Equal("That unit has no movement points left this turn", tired.Rejected.Single().Reason);
    }

    // R-COMBAT-002: a player's engagement draws once, when its order runs; step 2 draws only for the enemy.
    [Fact]
    public void APlayersAttackDrawsWhenGiven()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1, 6), Enemy(2, 3, 1, 1));
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 1) });
        Assert.NotEqual(s.RngState, now.State.RngState);
        var ended = Step(c, s, new UnitOrder(1, Combat.Attack, 3, 1));
        Assert.DoesNotContain(ended.State.Units.Values, u => u.Side == "israel" && u.Order == Combat.Attack);
    }

    // R-COMBAT-004: the Arms are checked and paid at the order, from the stock as it is.
    [Fact]
    public void AnAttackTheStockCannotPayIsRefused()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1, 6), Enemy(2, 3, 1, 1));
        s.Arms = 0;
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 1) });
        Assert.StartsWith("An attack needs", now.Rejected.Single().Reason);
        Assert.Equal(s.Hash(), now.State.Hash());
    }
```

Also in `CombatTests.cs` (R-COMBAT-009, which replaces the test that pinned `EndAttacksForTruce`: `git grep -n EndAttacksForTruce sim/Statecraft.Sim.Tests`, deleted with it):

```csharp
    // R-COMBAT-009: after accepting a truce, an attack in its window is refused at the click.
    [Fact]
    public void AnAttackInAnAcceptedTruceIsRefused()
    {
        var c = Load();
        var s = With(c, Soldier(1, 2, 1, 6), Enemy(2, 3, 1, 1));
        s.ChosenOptions["first_truce"] = "accept";
        s.Date = new DateTime(1948, 6, 15);   // before 8 July 1948
        Assert.Equal("A truce is in force", Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 3, 1) }).Rejected.Single().Reason);
    }
```

In `ArtilleryTests.cs` (its `Load`, `Of`, `Enemy`; a gun with range 2 and 3 Arms a firing), replacing the "holds its fire" test:

```csharp
    // R-COMBAT-007 (owner, 23 October 2026, OQ-1: "One unit per attack, Civ"): artillery attacks alone from range, at once,
    // pays its arms_per_fire and loses nothing.
    [Fact]
    public void ArtilleryAloneFiresAtOnceAndLosesNothing()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2), Enemy(2, 2, 0, 3));
        var arms = s.Arms;
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 2, 0) });
        Assert.Empty(now.Rejected);
        Assert.Contains(now.Why, w => w.Key == WhyKeys.Engagement);
        Assert.Equal(arms - 3, now.State.Arms);
        Assert.Equal(2, now.State.Units[1].Strength);
        Assert.Equal((2, 0), now.Attacked[1]);
    }

    // R-UNIT-005 (spec 4.6): a gun's front is that of the enemy it attacked this turn, two hexes away, though it borders none.
    [Fact]
    public void AGunsFrontIsTheEnemyItAttackedThisTurn()
    {
        var c = Load();
        var s = CombatTests.With(c, Of("gun", 1, 4, 0, 2), Enemy(2, 2, 0, 9));
        s.ChosenOptions["armistice_syria"] = "sign";   // an armistice, but not Egypt's
        Assert.Null(Demobilization.Check(c, s, 1));   // borders no enemy: any armistice will do
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 2, 0) });
        Assert.Equal("The armistice with Egypt is not signed", Demobilization.Check(c, now.State, 1, now.Attacked));
        Assert.Equal("The armistice with Egypt is not signed",
            Simulation.Step(c, s, new Command[] { new UnitOrder(1, Combat.Attack, 2, 0), new UnitOrder(1, Demobilization.SendHome) }).Rejected.Single().Reason);
    }
```

- [ ] **Step 2: Run; expect failures** (the attack is still a standing order).

- [ ] **Step 3: Implement.** `Check` gains `IReadOnlyDictionary<int, int>? spent = null`; for the player (`!enemy`): drop the "already has that order" line and the `committed` sum; after the reach and defender checks add `if (spent != null && Movement.PointsLeft(c, s, unitId, spent) == 0) return "That unit has no movement points left this turn";` and the Arms check becomes `s.Arms < need ? Short(need, s.Arms, 0) : null`. Keep the enemy's path as it is (its "already has that order" line stays for `EnemyAI.Goals`).

```csharp
        /// <summary>R-COMBAT-001 (owner, 23 October 2026, "Instant, like Civ"): the player's unit attacks (q, r) at once:
        /// refused, the state untouched, with <see cref="Check"/>'s reason; else it pays its Arms (R-COMBAT-004), loses its
        /// order and the rest of its movement points, and fights one engagement alone (OQ-1), drawing once.</summary>
        internal static string? Strike(Content c, GameState s, int unitId, int? q, int? r, Dictionary<int, int> spent, Dictionary<int, (int Q, int R)> attacked, List<Cause> why)
        {
            var refused = Check(c, s, unitId, q, r, spent: spent);
            if (refused != null) return refused;
            var mvp = c.RulesAt(s.Date).Mvp!;
            var u = s.Units[unitId];
            s.Arms -= Need(c, mvp, u);
            var attacker = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, "", "", u.HomeCity);
            s.Units[unitId] = attacker;
            if (u.Order == Orders.Fortify)
                why.Add(UnitLines.Line(WhyKeys.FortifyLost, $"Unit {unitId} left hex {u.OrderTarget} and is no longer fortified", new[] { u }, u.OrderTarget));
            spent[unitId] = Movement.Points(c, attacker);
            attacked[unitId] = (q!.Value, r!.Value);   // R-UNIT-005: the front of the enemy it attacked this turn (spec 4.6)
            Engage(c, s, mvp, new List<Unit> { attacker }, Defenders(s, attacker, q!.Value, r!.Value), q.Value, r.Value, why);
            return null;
        }
```

`RunOrders` keeps `attacked` beside `spent` and passes both to `Execute`; `Preview` returns it as `PreviewResult.Attacked`. In `Execute` (`Simulation.cs:181`): `if (o.Order == Combat.Attack) return Combat.Strike(c, s, o.Unit, o.Q, o.R, spent, attacked, why);`, and Send home calls `Demobilization.Give(c, s, o.Unit, attacked, why)`. `Demobilization.Front(s, u, attacked)` reads `attacked[u.Id]` where it read the attack order; `Check` and `Give` pass it on. In `Resolve`, delete the player's Arms branch (`Combat.cs:219-228`): only enemy units hold a standing attack now; say so in the summary. Delete `EndAttacksForTruce` and its call (`EventSystem.cs:153`). In `ReferencePlayer.Attack`, drop the `u.Order != Combat.Attack` filter and count `attacking` from 0 (its Arms check then counts this turn's attacks).

- [ ] **Step 4: Run CombatTests, ArtilleryTests, CombatDrawCountTests, the R-COMBAT-009 tests, EnemyAITests, DemobilizationTests, then the suite.** Expected: PASS, and the goldens unchanged (the reference player orders no attack in any golden run: spec 2.2). If a golden moves, stop and find out why before re-recording.

- [ ] **Step 5: Rule and doc text.** Rewrite R-COMBAT-001 per spec 4.1; R-COMBAT-002: "Resolver step 2 resolves the enemy's attacks ... A player's attack is fought when its order runs (R-COMBAT-001), one engagement with one draw"; R-COMBAT-004: "paid when the attack is given; an attack the stock cannot pay for is refused"; R-COMBAT-007 per OQ-1: "alone, it attacks at once like any unit, and its losses spare it, so it loses nothing"; R-COMBAT-009: remove the standing-attack sentence. R-UNIT-005: "A unit's front is the front of the enemy side it attacked this turn or borders" (spec 4.6). In `docs/determinism.md`, add "## Combat draws": a player engagement draws `Combat.DrawsPerEngagement` (1) when its order runs in `RunOrders`, after the route step and any earlier committed order, before every waiting order; the enemy's engagements draw in step 2, in ascending attacker id.

- [ ] **Step 6: Commit.** `git commit -m "sim: attacks are fought when given (Refs #1398)"`

### Task 6: Old saves and the attack's reload (S3)

**Files:**
- Modify: `sim/Statecraft.Sim/GameSession.cs` (`Load`)
- Test: `sim/Statecraft.Sim.Tests/GameSessionTurnTests.cs`
- Docs: `docs/rules/mvp.md` (R-SAVE-004)

**Interfaces:**
- Consumes: `SaveGame.Version` (Task 3), `Combat.Strike` (Task 5).

- [ ] **Step 1: Write the failing tests.**

```csharp
    // R-SAVE-004: a schema 2 GDD save with a player attack (a standing order then) is refused as older rules.
    [Fact]
    public void ASchema2SaveWithAnAttackIsRefusedAsOlderRules()
    {
        var session = Fresh();
        var text = session.Save.Write().Replace("statecraft-save 3", "statecraft-save 2")
            .Replace("end ", "turn\norder 0 attack 10 10\nend ");
        var e = Assert.Throws<SaveException>(() => GameSession.Load(C, text, out _));
        Assert.Equal(SaveException.OlderRules, e.Code);
    }

    // R-SAVE-003: reloading keeps an attack's result; the same draw comes out.
    [Fact]
    public void AReloadKeepsAnAttacksResult()
    {
        var session = Fresh();
        var (unit, q, r) = FirstAttack(session);
        Assert.Null(session.Commit(new UnitOrder(unit, Combat.Attack, q, r)));
        var loaded = GameSession.Load(C, session.Save.Write(), out _);
        Assert.Equal(session.Now.State.Hash(), loaded.Now.State.Hash());
    }

    // R-TURN-004: a vote queued after an attack runs after it, so it cannot change the attack's draw.
    [Fact]
    public void AVoteQueuedAfterAnAttackDoesNotChangeIt()
    {
        var a = Fresh(); var b = Fresh();
        var (unit, q, r) = FirstAttack(a);
        a.Commit(new UnitOrder(unit, Combat.Attack, q, r)); b.Commit(new UnitOrder(unit, Combat.Attack, q, r));
        var policy = C.At(a.State.Date).Policies.First().Id;
        var withVote = a.EndTurn(new Command[] { new SetPolicy(policy, 0.5) });
        var without = b.EndTurn(new Command[0]);
        Assert.Equal(without.Why.First(w => w.Message.StartsWith("The attack on hex")).Message,
                     withVote.Why.First(w => w.Message.StartsWith("The attack on hex")).Message);
    }

    /// <summary>The first (unit, hex) of the player's that Combat.Check takes on the session's shown state. At the start Golani
    /// (unit 0, 10,9) is next to the Syrian unit on 10,10 (docs/determinism.md, #1240's row), so one exists.</summary>
    static (int, int, int) FirstAttack(GameSession s)
    {
        var st = s.Now.State;
        foreach (var u in st.Units.Values.Where(x => x.Side == Movement.PlayerSide))
            foreach (var e in st.Units.Values.Where(x => x.Side != "" && x.Side != Movement.PlayerSide))
                if (Combat.Check(C, st, u.Id, e.Q, e.R, spent: s.Now.Spent) == null) return (u.Id, e.Q, e.R);
        throw new InvalidOperationException("precondition: no attack is possible at the start");
    }
```


- [ ] **Step 2: Run; expect the first test to fail** (the save loads and the replay differs).

- [ ] **Step 3: Implement** in `Load`, after the ruleset check: `if (saved.Version < 3 && saved.Ruleset == SaveGame.GddRuleset && saved.Turns.Any(t => t.Any(x => x is UnitOrder o && o.Order == Combat.Attack))) throw new SaveException(0, "This save was made with an earlier version of the game's rules.", SaveException.OlderRules);` (the owner's ruling). And where the replay's hash is checked (`GameSession.cs:68-72`), a schema 2 GDD save whose replay does not end on its saved hash throws the same `OlderRules` exception instead of "damaged" or the `replay_differs` warning (spec 3.4: instant capture and the full-points step can change an old save's replay too). Its test, in `GameSessionTurnTests`:

```csharp
    // R-SAVE-004: a schema 2 GDD save that does not replay to its saved hash gets the clear older-rules message; a schema 3
    // one that does not is still damaged (no code).
    [Fact]
    public void ASchema2SaveThatReplaysDifferentlyIsRefusedAsOlderRules()
    {
        static string WrongEnd(string text) { var end = text.Substring(text.LastIndexOf("end ", StringComparison.Ordinal) + 4, 64); return text.Replace("end " + end, "end " + new string('0', 64)); }
        var two = WrongEnd(Fresh().Save.Write().Replace("statecraft-save 3", "statecraft-save 2"));
        Assert.Equal(SaveException.OlderRules, Assert.Throws<SaveException>(() => GameSession.Load(C, two, out _)).Code);
        var three = WrongEnd(Fresh().Save.Write());
        Assert.Null(Assert.Throws<SaveException>(() => GameSession.Load(C, three, out _)).Code);
    }
```


- [ ] **Step 4: Run and the suite; expect PASS.**

- [ ] **Step 5: Rule text.** "R-SAVE-004: A schema 2 save of the GDD game with a player attack (`order <unit> attack <q> <r>`) is refused with the code `older_rules` (R-SAVE-001): its attacks were standing orders, so it would replay differently (the owner, 23 October 2026: \"Refuse with a clear message\"). So is a schema 2 GDD save whose replay under the current rules does not end on its saved hash (instant capture, the full-points step). Other schema 2 saves load."

- [ ] **Step 6: Commit; open the S3 PR (Tasks 5, 6).** Coordinate R-COMBAT-009's text with #750 (Agam) if it is open: whichever lands second merges the paragraph. Tell #752's owner that player attacks are now given each turn.

### Task 7: The route (S4)

**Files:**
- Modify: `sim/Statecraft.Sim/Movement.cs`, new `Route`
- Test: create `sim/Statecraft.Sim.Tests/GoToTests.cs`
- Docs: `docs/rules/mvp.md` (R-MOVE-002 addition)

**Interfaces:**
- Produces: `Movement.Route(Content c, GameState s, int unitId, int q, int r, IReadOnlyDictionary<int,int> spent) -> IReadOnlyList<(int Q, int R, int Turn)>` (empty: no route).

- [ ] **Step 1: Write the failing tests.**

```csharp
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>R-MOVE-002, R-MOVE-008, R-MOVE-009 (owner, 23 October 2026: multi-turn go-to "Now"; spec 2026-10-23 section 6).</summary>
public class GoToTests
{
    static Content Board(params (int Q, int R, string Biome)[] biomes) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Fixture.GddUnitTypes
            + "  - { id: soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n"
            + "  - { id: gun, kind: unit, side: israel, class: infantry, movement_points: 2 }\n"
            + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n",
        ["map/hexes.json"] = MovementTests.Board(biomes), ["corridors.yaml"] = "corridors: []\n"
    })));

    static readonly IReadOnlyDictionary<int, int> None = new Dictionary<int, int>();

    // R-MOVE-002: the route's hexes carry the turn they are reached on: turn 1 spends the points left, later turns all.
    [Fact]
    public void TheRouteNumbersItsTurns()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 3));
        var route = Movement.Route(c, s, 1, 5, 1, new Dictionary<int, int> { [1] = 1 });
        Assert.Equal(new[] { (1, 1, 1), (2, 1, 1), (3, 1, 2), (4, 1, 2), (5, 1, 2) }, route.Select(h => (h.Q, h.R, h.Turn)));
    }

    // R-MOVE-001, R-MOVE-002 (OQ-3): a step that costs more than the unit's full points is a turn of its own.
    [Fact]
    public void AStepCostingMoreThanFullPointsTakesATurn()
    {
        var c = Board((0, 0, "desert"), (0, 2, "desert"), (1, 0, "desert"), (1, 2, "desert"), (2, 0, "desert"), (2, 1, "desert"), (2, 2, "desert"));
        var s = CombatTests.With(c, new Unit(1, "gun", 0, 1, side: "israel", strength: 3));
        // 1,1 costs 1 on turn 1; 2,1 (desert, 3 > the gun's 2) cannot follow on turn 1 and takes all of turn 2; 3,1 is turn 3.
        Assert.Equal(new[] { (1, 1, 1), (2, 1, 2), (3, 1, 3) }, Movement.Route(c, s, 1, 3, 1, None).Select(h => (h.Q, h.R, h.Turn)));
    }

    [Fact]
    public void NoRouteToWaterOrAnEnemysHex()
    {
        var c = Board((5, 1, "sea"));
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 3), new Unit(2, "enemy", 4, 1, side: "egypt", strength: 1));
        Assert.Empty(Movement.Route(c, s, 1, 5, 1, None));
        Assert.Empty(Movement.Route(c, s, 1, 4, 1, None));
    }
}
```

The straight path is the only one of cost 5 from 0,1 to 5,1 (the q distance is 5 and r does not change, so every step is (+1, 0)); the fixture's costs are coastal plain 1, hills 2, desert 3 (`MovementTests`' summary). In the second test the desert walls every way round 2,1 on the three-row board, so the route must cross it.

- [ ] **Step 2: Run; expect compile errors.**

- [ ] **Step 3: Implement.** `Route` searches with no budget (`Search(c, s, u, int.MaxValue)`), so every land hex is a step of its own cost; the segmentation applies the full-points exception (Task 2a). `Reachable`, `Path`, `Move` and `Toward` are unchanged.

```csharp
        /// <summary>R-MOVE-002, R-MOVE-008: the cheapest path to (q, r) with no points limit, each hex with the turn it is
        /// reached on (turn 1 spends the points left after <paramref name="spent"/>, later turns all of them; a step that costs
        /// more than the full points takes a whole turn, R-MOVE-001). Empty for an unknown or enemy unit, one that cannot
        /// move, its own hex, or a hex no path reaches.</summary>
        public static IReadOnlyList<(int Q, int R, int Turn)> Route(Content c, GameState s, int unitId, int q, int r, IReadOnlyDictionary<int, int> spent)
        {
            var result = new List<(int Q, int R, int Turn)>();
            if (!s.Units.TryGetValue(unitId, out var u) || IsEnemy(u) || (u.Q, u.R) == (q, r)) return result;
            var full = Points(c, u);
            if (full == 0) return result;
            var (dist, prev) = Search(c, s, u, int.MaxValue);
            if (!dist.ContainsKey((q, r))) return result;
            var hexes = new List<(int Q, int R)>();
            for (var h = (q, r); h != (u.Q, u.R); h = prev[h]) hexes.Add(h);
            hexes.Reverse();
            spent.TryGetValue(unitId, out var used);
            int left = full - used, turn = 1, before = 0;
            foreach (var h in hexes)
            {
                var step = dist[h] - before; before = dist[h];
                // A step that does not fit starts the next turn, except one costing more than the full points taken with all
                // of them: the full-points exception (R-MOVE-001, OQ-3) makes it that turn's only step.
                if (step > left && !(left == full && step > full)) { turn++; left = full; }
                left = step > left ? 0 : left - step;
                result.Add((h.Q, h.R, turn));
            }
            return result;
        }
```

- [ ] **Step 4: Run and the suite; expect PASS, no golden moves.**

- [ ] **Step 5: Rule text.** R-MOVE-002: add "`Movement.Route(c, s, unit, q, r, spent)` is the cheapest path with no points limit, each hex numbered with the turn it is reached on: turn 1 spends the points left, later turns all of them, a step that does not fit what is left of a turn starts the next, and a step costing more than the full points is a turn of its own (R-MOVE-001)."

- [ ] **Step 6: Commit.** `git commit -m "sim: the go-to route with turn numbers (Refs #1398)"`

### Task 8: The go-to order and the route step (S4)

**Files:**
- Modify: `sim/Statecraft.Sim/Movement.cs` (`GoTo`, `GiveGoTo`, `ContinueRoutes`, `Advance`, `Stop`, `Target`)
- Modify: `sim/Statecraft.Sim/Simulation.cs:100-116` (route step), `:180-185` (dispatch)
- Test: `sim/Statecraft.Sim.Tests/GoToTests.cs`, `SaveGameTests.cs`
- Docs: `docs/rules/mvp.md` (R-MOVE-008, R-MOVE-009; R-ORDER-001 note)

**Interfaces:**
- Consumes: `Route` (Task 7), `Move`, `Full`, `Closed`, `Points`.
- Produces: `Movement.GoTo = "go_to"`; `internal static string? GiveGoTo(Content, GameState, int unitId, int? q, int? r, Dictionary<int,int> spent, List<Cause> why)`; `internal static void ContinueRoutes(Content, GameState, Dictionary<int,int> spent, List<Cause> why)`.

- [ ] **Step 1: Write the failing tests** in `GoToTests.cs`:

```csharp
    static Unit Soldier(int id, int q, int r) => new Unit(id, "soldier", q, r, side: "israel", strength: 3);

    // R-MOVE-008: a go-to moves the unit at once as far as its points reach and keeps the order.
    [Fact]
    public void AGoToMovesAtOnceAndStands()
    {
        var c = Board();
        var s = CombatTests.With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new UnitOrder(1, Movement.GoTo, 5, 1) });
        Assert.Empty(now.Rejected);
        Assert.Equal((3, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Equal((Movement.GoTo, "5,1"), (now.State.Units[1].Order, now.State.Units[1].OrderTarget));
    }

    // R-MOVE-009: the route step moves it on at the next turn's start, and it arrives on the turn the route showed.
    [Fact]
    public void ARouteArrivesOnTheTurnItShowed()
    {
        var c = Board();
        var s = CombatTests.With(c, Soldier(1, 0, 1));
        var shown = Movement.Route(c, s, 1, 5, 1, new Dictionary<int, int>()).Last().Turn;
        Assert.Equal(2, shown);   // three hexes on turn 1, two on turn 2, on the fixture's one-point plain
        var endOfTurn1 = Simulation.Step(c, s, new Command[] { new UnitOrder(1, Movement.GoTo, 5, 1) }).State;
        Assert.Equal((3, 1), (endOfTurn1.Units[1].Q, endOfTurn1.Units[1].R));
        var startOfTurn2 = Simulation.Preview(c, endOfTurn1, new Command[0]).State;   // the route step has run
        Assert.Equal((5, 1), (startOfTurn2.Units[1].Q, startOfTurn2.Units[1].R));
        Assert.Equal("", startOfTurn2.Units[1].Order);
    }

    // R-MOVE-009, R-MOVE-004 (Q-MOVE-4, no zone of control): a route does not stop because an enemy unit is next to it.
    [Fact]
    public void ARouteGoesOnPastAnEnemy()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 2, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    new Unit(2, "enemy", 3, 0, side: "egypt", strength: 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal((5, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Equal("", now.State.Units[1].Order);   // arrived
    }

    [Fact]
    public void ARouteToAHexAnEnemyTookStops()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    new Unit(2, "enemy", 5, 1, side: "egypt", strength: 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Contains(now.Why, w => w.Key == WhyKeys.StoppedNoRoute);
        Assert.Contains(1, Movement.NeedsOrders(c, now.State, now.Spent));   // it needs orders (Review Focus 5)
    }

    [Fact]
    public void ARouteWhoseHexIsFullStopsOnArrival()
    {
        var c = Board();   // the fixture's stack limit is 2 a side (MovementTests' summary)
        var s = CombatTests.With(c, new Unit(1, "soldier", 4, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    Soldier(2, 5, 1), Soldier(3, 5, 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Equal((4, 1), (now.State.Units[1].Q, now.State.Units[1].R));
    }

    [Fact]
    public void AGoToIsSavedAsAUnitOrderWithAHex()
    {
        var save = new SaveGame(new string('a', 64), 1, Mode.Historical, SaveGame.GddRuleset, new string('b', 64));
        save.SetOpen(new Command[] { new UnitOrder(1, Movement.GoTo, 5, 1) });
        Assert.Contains("order 1 go_to 5 1", save.Write());
    }
```

- [ ] **Step 2: Run; expect compile errors.**

- [ ] **Step 3: Implement** in `Movement.cs`:

```csharp
        /// <summary>The go-to order (R-MOVE-008): <see cref="Unit.Order"/> while a unit is on its way, with the hex "q,r".</summary>
        public const string GoTo = "go_to";

        static string HexText(int q, int r) => string.Format(CultureInfo.InvariantCulture, "{0},{1}", q, r);

        static (int Q, int R) Target(Unit u)
        {
            var parts = u.OrderTarget.Split(',');
            return (int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        static void Stop(GameState s, Unit u, string key, string line, string hex, List<Cause> why)
        {
            s.Units[u.Id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, "", "", u.HomeCity);
            why.Add(UnitLines.Line(key, line, new[] { u }, hex));
        }

        /// <summary>R-MOVE-008: gives the player's unit the go-to order and moves it at once as far as its points left reach.
        /// Refused, the state untouched, for no hex, an unknown or enemy unit, one that cannot move or is there, water or off
        /// the board, an enemy unit's hex, a closed locality (R-MOVE-005) or a hex no route reaches.</summary>
        internal static string? GiveGoTo(Content c, GameState s, int unitId, int? q, int? r, Dictionary<int, int> spent, List<Cause> why)
        {
            if (q == null || r == null) return "A route names a hex";
            if (!s.Units.TryGetValue(unitId, out var u)) return "There is no such unit";
            if (IsEnemy(u)) return "That unit is not yours";
            if (Points(c, u) == 0) return "That unit cannot move";
            if (u.Q == q && u.R == r) return "That unit is already there";
            if (!(c.HexAt(q.Value, r.Value) is MapHex hex) || !hex.Land) return "Units cannot enter water or leave the map";
            if (s.Units.Values.Any(x => x.Q == q && x.R == r && IsEnemy(x))) return "An enemy unit holds that hex";
            if (Closed(c, s, false).Contains((q.Value, r.Value))) return "Units may not enter an enemy-held locality before its capture is possible";
            if (Route(c, s, unitId, q.Value, r.Value, spent).Count == 0) return "No route reaches that hex";
            var target = HexText(q.Value, r.Value);
            var given = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, u.Strength, GoTo, target, u.HomeCity);
            s.Units[unitId] = given;
            why.Add(UnitLines.Line(WhyKeys.OnItsWay, $"Unit {unitId} is on its way to hex {target}", new[] { given }, target));
            if (u.Order == "fortify")
                why.Add(UnitLines.Line(WhyKeys.FortifyLost, $"Unit {unitId} left hex {u.OrderTarget} and is no longer fortified", new[] { u }, u.OrderTarget));
            Advance(c, s, given, q.Value, r.Value, spent, why);
            return null;
        }

        /// <summary>R-MOVE-009, the route step: at the start of the player's turn, in ascending id, each unit with a go-to moves
        /// on (<see cref="Advance"/>). An enemy unit next to it does not stop it (Q-MOVE-4: no zone of control).</summary>
        internal static void ContinueRoutes(Content c, GameState s, Dictionary<int, int> spent, List<Cause> why)
        {
            foreach (var id in s.Units.Keys.ToList())
            {
                if (!s.Units.TryGetValue(id, out var u) || u.Order != GoTo) continue;
                var (q, r) = Target(u);
                Advance(c, s, u, q, r, spent, why);
            }
        }

        /// <summary>R-MOVE-008, R-MOVE-009: moves the unit along a fresh route to (q, r) as far as this turn's points reach,
        /// stopping short of a full hex; stops it (order cleared) when no route reaches the hex or the hex is full on arrival,
        /// and clears the order on arrival. With no hex of this turn free, it waits, keeping the order.</summary>
        static void Advance(Content c, GameState s, Unit u, int q, int r, Dictionary<int, int> spent, List<Cause> why)
        {
            var route = Route(c, s, u.Id, q, r, spent);
            if (route.Count == 0) { Stop(s, u, WhyKeys.StoppedNoRoute, $"Unit {u.Id} stopped: no route reaches hex {HexText(q, r)}", HexText(q, r), why); return; }
            var today = route.TakeWhile(h => h.Turn == 1).ToList();
            for (int i = today.Count - 1; i >= 0; i--)
            {
                var h = today[i];
                if (Full(c, s, u, h.Q, h.R) != null)
                {
                    if ((h.Q, h.R) == (q, r)) { Stop(s, u, WhyKeys.StoppedFull, $"Unit {u.Id} stopped: hex {HexText(q, r)} is full", HexText(q, r), why); return; }
                    continue;
                }
                if (Move(c, s, u.Id, h.Q, h.R, spent, why) is string refused) { Stop(s, u, WhyKeys.StoppedNoRoute, $"Unit {u.Id} stopped: {refused}", HexText(q, r), why); return; }
                if ((h.Q, h.R) == (q, r)) Stop(s, s.Units[u.Id], WhyKeys.Arrived, $"Unit {u.Id} arrived at hex {HexText(q, r)}", HexText(q, r), why);
                return;
            }
        }
```

In `Simulation.cs`: `RunOrders` begins with `if (c.IsGdd) Movement.ContinueRoutes(c, s, spent, why);`. `Execute`'s `UnitOrder` case begins with `if (o.Order == Movement.GoTo) return Movement.GiveGoTo(c, s, o.Unit, o.Q, o.R, spent, why);`. `Orders.Check` already refuses an unknown order, so no other path takes `go_to`.

- [ ] **Step 4: Run GoToTests, MovementTests, OrdersTests, EnemyAITests and the suite.** Expected: PASS, goldens unchanged (no unit has a go-to in any golden).

- [ ] **Step 5: Rule text.** R-MOVE-008 and R-MOVE-009 from spec 6.1 to 6.3, (an enemy unit next to the route does not stop it: Q-MOVE-4). R-ORDER-001: "A new order replaces a go-to too."

- [ ] **Step 6: Commit; open the S4 PR (Tasks 7, 8).**

### Task 8a: Capture at the click (S6, OQ-2; after #1324 and #1327 have merged)

**Before starting:** `git fetch`; check #1324 and #1327 are merged and read their merged `SettlementControl.cs`, `GddState.cs` and `TurnResolver.cs` in full. This task adapts that code; it does not change Occupy's, Deport's or the timer's rules, and the answer (`ResolveCapture`) stays a deferred order (spec 3.2, 4.4).

**Files:**
- Modify: `sim/Statecraft.Sim/SettlementControl.cs` (`Capture` split into `CaptureHeld` and the step-3 call; new `Stamp`)
- Modify: `sim/Statecraft.Sim/Simulation.cs` (`RunOrders` runs `CaptureHeld` after the route step and after each command; `Step` calls `Stamp` right after computing `next`, `Simulation.cs:56`)
- Test: `sim/Statecraft.Sim.Tests/CaptureTests.cs`
- Docs: `docs/rules/mvp.md:39` (R-CTRL-004), `docs/determinism.md:21-29` (the settlement-control bullets)

**Interfaces:**
- Consumes: `RunOrders`' route step (Task 8).
- Produces: `internal static void SettlementControl.CaptureHeld(Content c, GameState s, List<Cause> why)` (the player's branch of today's `Capture`, writing records with no `Since`); `internal static void SettlementControl.Stamp(GameState s, DateTime next)` (sets `Since` to `next` on every record of the player's whose `Since` is default).

- [ ] **Step 1: Write the failing tests** in `CaptureTests.cs` (its fixture: town_a, an enemy-held target, on 1,0; the 6 x 3 coastal plain):

```csharp
    // R-CTRL-004 (owner, 23 October 2026, OQ-2: "Instant"): a move that ends on an enemy-held target with no enemy unit
    // there captures it at once, before End turn; End turn stamps the turn's end as Since, as step 3 does.
    [Fact]
    public void AMoveEndingOnATargetCapturesItAtOnce()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        var now = Simulation.Preview(c, s, new Command[] { new Move(1, 1, 0) });
        Assert.Equal(SettlementControl.AwaitingChoice, now.State.Settlements["town_a"].State);
        Assert.Equal(default(DateTime), now.State.Settlements["town_a"].Since);
        var ended = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0) }).State;
        Assert.Equal(ended.Date, ended.Settlements["town_a"].Since);
    }

    // R-CTRL-004: passing through a target captures nothing (R-MOVE-005 lets a path pass through it): 0,0 to 2,0 crosses 1,0.
    [Fact]
    public void PassingThroughATargetCapturesNothing()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 0));
        Assert.Equal(new[] { (1, 0), (2, 0) }, Movement.Path(c, s, 1, 2, 0));   // precondition: the path crosses town_a
        Assert.Empty(Simulation.Preview(c, s, new Command[] { new Move(1, 2, 0) }).State.Settlements);
    }

    // R-CTRL-004: a unit that captured and left keeps the capture; only enemy units alone on it take it back, in step 3.
    [Fact]
    public void ACaptureStaysWhenTheUnitLeaves()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        var ended = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0), new Move(1, 0, 1) }).State;
        Assert.Equal(SettlementControl.AwaitingChoice, ended.Settlements["town_a"].State);
    }

    // R-CTRL-004: the check draws nothing.
    [Fact]
    public void TheInstantCheckDrawsNothing()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        Assert.Equal(s.RngState, Simulation.Preview(c, s, new Command[] { new Move(1, 1, 0) }).State.RngState);
    }

    // R-CTRL-007: the answer stays deferred: given in the capture's turn, it runs at End turn after the capturing move.
    [Fact]
    public void TheAnswerGivenInTheCapturesTurnRunsAtEndTurn()
    {
        var c = Load(AllTargets);
        var s = With(c, Soldier(1, 0, 1));
        var ended = Simulation.Step(c, s, new Command[] { new Move(1, 1, 0), new ResolveCapture("town_a", SettlementControl.Raid) });
        Assert.Empty(ended.Rejected);
        Assert.False(ended.State.Settlements.ContainsKey("town_a"));
    }
```

- [ ] **Step 2: Run** `dotnet test sim/Statecraft.sln --filter FullyQualifiedName~CaptureTests`; expect the first, third and fifth to fail (no record before step 3; a raid before the capture is refused).

- [ ] **Step 3: Implement.** Move the player's branch of `Capture` (on `de78253e`, `SettlementControl.cs:32-36`) into `CaptureHeld`, writing `new SettlementControl(Movement.PlayerSide, AwaitingChoice)` with the same Why line; `Capture` (step 3) calls `CaptureHeld`, then `Stamp(s, next)`, and keeps its "under enemy control again" branch. In `RunOrders`: `if (c.IsGdd) SettlementControl.CaptureHeld(c, s, why);` after the route step and after each command. In `Step`, right after `next` is computed and before `TurnResolver.Resolve`: `if (c.IsGdd) SettlementControl.Stamp(s, next);`. `HistoricalAI`'s answers (R-CTRL-009) need no change.

- [ ] **Step 4: Run CaptureTests, RaidTests, OccupyTests, DeportTests, CaptureOptionsTests, SettlementStateTests and the suite.** Expected: PASS; no golden hash changes (no golden captures a place, `docs/determinism.md:18-23`). #1327's tests that read `since=` on a step-3 record still hold (`Stamp` writes the same date).

- [ ] **Step 5: Rule and doc text.** R-CTRL-004 as spec 4.4. `docs/determinism.md`: the capture check runs after each order as well as in step 3, in ordinal id order, with no draw.

- [ ] **Step 6: Commit; open the S6 PR.** Tell Beri in one short comment on #798 that a capture now happens when the move ends, the answer still waits for End turn, and nothing is needed from them.

### Task 9: The bridge commits moves and unit orders (U1)

**Files:**
- Modify: `unity/Assets/Scripts/UI/SimBridge.cs`
- Modify: `unity/Assets/Scripts/UI/GameStart.cs:64-90` (autosave after each commit; the receipt's hash from `TurnStart`)
- Modify: both strings files (`order.going_home`)
- Test: `unity/Assets/Tests/EditMode/SimBridgeGddTests.cs`, `SimBridgeGuardTests.cs`, `SaveUiTests.cs`

**Interfaces:**
- Consumes: `GameSession.Commit`, `Now`, `Committed`, `EndTurn` (Task 4).
- Produces: `SimBridge.State` (now `Session.Now.State`), `SimBridge.TurnStart` (`Session.State`), `SimBridge.Spent`, `event Action<IReadOnlyList<Cause>>? Committed` (the Why lines a committed order added), `Queued` (deferred orders only). `Queue(Command)` commits an instant order.

- [ ] **Step 1: Write the failing EditMode tests** in `SimBridgeGddTests.cs` (it has `Slice`, the real content, and `Text`):

```csharp
    /// <summary>R-TURN-004: a Move given through the bridge shows at once (State), and End turn keeps it.</summary>
    [Test]
    public void AMoveShowsAtOnce()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Slice, bridge.State);
        var start = (bridge.State.Units[move.Unit].Q, bridge.State.Units[move.Unit].R);
        IReadOnlyList<Cause>? lines = null; bridge.Committed += w => lines = w;
        Assert.IsNull(bridge.Queue(move));
        Assert.AreEqual((move.Q, move.R), (bridge.State.Units[move.Unit].Q, bridge.State.Units[move.Unit].R));
        Assert.AreEqual(start, (bridge.TurnStart.Units[move.Unit].Q, bridge.TurnStart.Units[move.Unit].R));
        Assert.IsEmpty(bridge.Queued, "a committed order is not queued");
        Assert.IsTrue(lines!.Any(w => w.Key == WhyKeys.UnitMoved));
        bridge.EndTurn();
        Assert.AreEqual(GoldenRun.PlaySmokeOrders(Slice).Hash(), bridge.TurnStart.Hash());
    }

    /// <summary>No undo: a committed move is not withdrawn; a second move for the same unit is kept beside it.</summary>
    [Test]
    public void ACommittedMoveCannotBeWithdrawn()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Slice, bridge.State);
        bridge.Queue(move);
        bridge.Withdraw(move);
        Assert.AreEqual(1, bridge.Session.Committed.Count);
    }

    /// <summary>Spec 3.2: Send home stays deferred, and a unit with it queued takes no more moves or orders.</summary>
    [Test]
    public void AUnitWithSendHomeQueuedTakesNoMoreOrders()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Slice, bridge.State);
        Assert.IsNull(bridge.Queue(new UnitOrder(move.Unit, Demobilization.SendHome)));
        Assert.AreEqual(1, bridge.Queued.Count, "deferred, as today");
        Assert.AreEqual(Text["order.going_home"], bridge.Queue(move));
        Assert.IsEmpty(bridge.Session.Committed);
    }
```

In `SaveUiTests.cs`:

```csharp
    /// <summary>Spec 3.4 (No undo across a reload): the autosave is written after every committed order, with the open
    /// turn in click order, and reloading it shows the same state.</summary>
    [Test]
    public void AutosaveWritesEveryCommittedOrder()
    {
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        using (GameStart.Autosave(bridge, store, Text))
        {
            var move = GoldenRun.SmokeMove(C, bridge.State);
            Assert.IsNull(bridge.Queue(move));
            var first = store.ReadAutosave()!;
            StringAssert.Contains($"\nopen\nmove {move.Unit} {move.Q} {move.R}\n", first);
            var loaded = GameSession.Load(C, first, out var warnings);
            Assert.IsEmpty(warnings);
            Assert.AreEqual(bridge.State.Hash(), loaded.Now.State.Hash());
            Assert.IsNull(bridge.Queue(new UnitOrder(move.Unit, Orders.Hold)));
            var again = GameSession.Load(C, store.ReadAutosave()!, out _);
            CollectionAssert.AreEqual(new[] { "move", "unit_order" }, again.Committed.Select(x => x.Kind).ToArray());
            Assert.AreEqual(bridge.State.Hash(), again.Now.State.Hash());
        }
    }
```

(`TempStore` and `C` are `SaveUiTests.cs:16-24`'s.) In `SimBridgeGuardTests`, the rule that every command type has a key or is one per turn also accepts `Simulation.IsInstant` commands.

- [ ] **Step 2: Run** (`-testFilter SimBridgeGddTests;SaveUiTests;SimBridgeGuardTests` while developing); expect compile errors (`TurnStart`, `Committed`).

- [ ] **Step 3: Implement** in `SimBridge.cs`. Replace the class summary's "Orders take effect at End Turn" with R-TURN-004's split. Rename `queue` to `deferred`. Add:

```csharp
        /// <summary>The state the map and the cards show: the turn's start after its committed orders (R-TURN-004).</summary>
        public GameState State => Session.Now.State;
        /// <summary>The state the turn began with (the saved one): what End turn and the orders' prices start from.</summary>
        public GameState TurnStart => Session.State;
        /// <summary>The movement points each unit has spent this turn.</summary>
        public IReadOnlyDictionary<int, int> Spent => Session.Now.Spent;
        /// <summary>Raised after a move or unit order is committed, with the Why lines it added (an attack's result).</summary>
        public event Action<IReadOnlyList<Cause>>? Committed;

        IReadOnlyList<Command> All => Session.Committed.Concat(deferred).ToList();

        bool GoingHome(int unit) => deferred.Any(x => x is UnitOrder o && o.Unit == unit && o.Order == Demobilization.SendHome);
```

`Price`, `AttentionLeft`, `DelegatedTo` and `Vacancies` read `TurnStart` and `All`. In `Queue`, after the game-over check:

```csharp
            if (Simulation.IsInstant(c))
            {
                var unit = c is Move m ? m.Unit : ((UnitOrder)c).Unit;
                if (GoingHome(unit)) return Refuse(text["order.going_home"]);
                var before = Session.Now.Why.Count;
                var refused = Session.Commit(c);
                if (refused != null) return Refuse(refused);
                Notify(Changed);
                Notify(Committed, Session.Now.Why.Skip(before).ToList());
                return null;
            }
```

`EndTurn` steps with `deferred` (the default stepper becomes `(s, cmds) => s.EndTurn(cmds)`), sets `LastBatch` to the committed list plus `deferred`, and clears `deferred` only (committed orders stay with the session if the step throws). `Key` loses the `Move` line; `UnitOrder` keeps its key for Send home only. If #1352 has landed, its `BudgetUnsubmitted` passes `deferred`. Strings: `order.going_home`: "That unit is going home when the turn ends" / HE draft "היחידה חוזרת הביתה בסיום התור".

In `GameStart.Autosave`, keep the commit handler in a local (`Action<IReadOnlyList<Cause>> onCommit = _ => Save(Array.Empty<Rejection>());`), subscribe it to `bridge.Committed` beside `TurnEnded`, and unsubscribe that same instance in the returned `Unsubscribe`. The receipt's state is `bridge.TurnStart` (the save's `end` hash is the turn start's).

- [ ] **Step 4: Run SimBridgeGddTests, SimBridgeGuardTests, SimBridgeKindTests, SaveUiTests; expect PASS.**

- [ ] **Step 5: Commit.** `git commit -m "ui: the bridge commits moves and unit orders at once (Refs #1398)"`

### Task 10: Moves on the map and the card (U1)

**Files:**
- Modify: `unity/Assets/Scripts/UI/InfoPanelPresenter.cs:178-272`, `:395-440`
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs:835-867` (smoke-orders path)
- Modify: `unity/Assets/UI/strings.en.yaml:87-89`, `:450-470`; `unity/Assets/UI/strings.he.yaml` (same keys)
- Modify: `docs/UX_GLOSSARY.yaml`, `docs/UX_GLOSSARY.md` (`movement-points`)
- Test: `unity/Assets/Tests/EditMode/UnitOrdersTests.cs`, `FirstTurnGuideTests.cs`

**Interfaces:**
- Consumes: `SimBridge.State`, `Spent`, `Committed` (Task 9); `Movement.Reachable(..., spent)`, `PointsLeft` (Task 1).

- [ ] **Step 1: Write the failing EditMode test** in `UnitOrdersTests.cs` (its `BuildMap`, `Units`, `Text` and `C`):

```csharp
    /// <summary>R-MOVE-001 on screen: a click on a lit hex moves the unit at once, the Move pick opens again on what the
    /// points left reach, and the card shows the points.</summary>
    [Test]
    public void TheLitReachShrinksAfterAMove()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
        layer.Show(bridge.Content, bridge.State);
        var lit = new List<IReadOnlyCollection<(int q, int r)>>();
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(),
            selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
            showReachable: hexes => lit.Add(hexes.ToList()));
        var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
        var id = UnitCard.SimUnit(C, bridge.State, golani.Token.Unit)!.Value;
        var cheap = Movement.Reachable(C, bridge.State, id).Where(kv => kv.Value == 1).Select(kv => kv.Key).ToList();
        Assert.IsNotEmpty(cheap, "precondition: Golani has a one-point step");
        GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
        UiTestKit.Press(ui.Q<Button>("info-move"));
        var before = lit.Last().Count;
        GameRoot.OnHexClicked(map, layer, info, cheap[0].Q, cheap[0].R);
        Assert.AreEqual((cheap[0].Q, cheap[0].R), (bridge.State.Units[id].Q, bridge.State.Units[id].R), "the unit moved at once");
        CollectionAssert.AreEquivalent(Movement.Reachable(C, bridge.State, id, bridge.Spent).Keys.Select(h => (h.Q, h.R)).ToList(), lit.Last());
        Assert.Less(lit.Last().Count, before);
        var full = C.UnitTypes.Single(t => t.Id == bridge.State.Units[id].Type).MovementPoints;
        StringAssert.Contains(Text.Format("unit.points", full - 1, full), ui.Q<Label>("info-settlements").text);
        info.Dispose();
    }
```

Update `AClickOnALitHexQueuesTheMoveAndAnyOtherClickEndsThePick` (`UnitOrdersTests.cs:89-140`): the Move is in `bridge.Session.Committed`, not `bridge.Queued`, and the `unit.move_queued` line is gone. In `FirstTurnGuideTests`, the move step's text assertion takes the new `guide.move.text`.

- [ ] **Step 2: Run; expect failures** (the pick does not reopen; no points line).

- [ ] **Step 3: Implement.** `MoveTargets` reads `Movement.Reachable(bridge.Content, bridge.State, id, bridge.Spent)`. In `PickTarget`, after a Move is taken, reopen the pick when `MoveTargets()` still has hexes (`TogglePick(false)`). The card body gets, for the player's military units, `text.Format("unit.points", Movement.PointsLeft(c, s, id, bridge.Spent), full)` with `full` from `c.UnitTypes`. The standing-order line reads the unit's `Order` in `bridge.State` (fortify or fortifying: `unit.fortify_queued`; hold: `unit.hold_queued`); a queued Send home still shows `unit.send_home_queued`. Drop `unit.move_queued`; change `guide.move.text`, `unit.hold_queued`, and add `unit.points` in both files from spec 8.2, with the native-review comment (`unit.move_pick`'s go-to half lands with Task 12). Add the glossary entry `movement-points` (decision-needed; en "movement points"; he candidate "נקודות תנועה"; in_use `unit.points`) to the YAML, then the Markdown view. In `GameRoot.SmokeOrders`, check `bridge.Session.Committed.OfType<Move>()` instead of `bridge.Queued`, and log `bridge.TurnStart.Hash()` (in `Smoke` too).

- [ ] **Step 4: Full EditMode on the merged tree, 0 NEW.**

- [ ] **Step 5: Gates for the U1 PR (Tasks 9, 10):** the Unity gates; WebGL `golden&smoke=5` and `smoke-orders` print the unchanged lines (the End turn block arrives only in U4a); screenshots EN and HE of a unit card before and after a move.

- [ ] **Step 6: Commit and open the U1 PR.** It needs #1396 on main (or in the same review round); say so in the PR body.

### Task 10a: Units named in the Why lines, on screen (U5, #1399; with U1)

**Files:**
- Create: `unity/Assets/Scripts/UI/UnitNames.cs`
- Modify: `unity/Assets/Scripts/UI/WhyPresenter.cs:45-60`, `unity/Assets/Scripts/UI/TurnReportPresenter.cs:112-128` (its `Name` becomes `UnitNames`)
- Modify: both strings files (`why.line.<key>` for every `WhyKeys.All`, `unit.raised_name`)
- Test: create `unity/Assets/Tests/EditMode/WhyUnitNamesTests.cs`

**Interfaces:**
- Consumes: `Cause.Key`, `Units`, `Args`, `WhyKeys.All` (Tasks 4a, 4b); #1396's raised-unit type label (use its helper and keys; if it adds none, the type label is `city.track_<type>`, `strings.en.yaml:498-501`).
- Produces: `UnitNames.Of(UnitRef unit, Content c, IReadOnlyList<UnitEntry> rows, UiText text) -> string`; `UnitNames.Line(Cause cause, Content c, IReadOnlyList<UnitEntry> rows, UiText text) -> string` (the keyed line in the player's language, or `Message` for a cause with no key).

- [ ] **Step 1: Write the failing tests.**

```csharp
#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Statecraft.Game.Map.Units;
using Statecraft.Game.UI;
using Statecraft.Sim;
using UnityEditor;
using UnityEngine;

/// <summary>#1399: the Why lines name units by their names, in English and Hebrew, never by the sim's number.</summary>
public class WhyUnitNamesTests
{
    static readonly Content C = ContentLoader.Load(Paths.Content);
    static List<UnitEntry> Rows => UnitData.Parse(File.ReadAllText(Path.Combine(Paths.Content, "map", UnitData.DefaultFileName)));
    static UiText En => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings).text);
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.StringsHebrew).text, "strings.he.yaml");   // as HebrewUiTests.cs:27

    [Test]
    public void EveryKeyHasALineInBothLanguages()
    {
        foreach (var key in WhyKeys.All)
        {
            Assert.IsTrue(En.Has("why.line." + key), key);
            Assert.IsTrue(He.Has("why.line." + key), key);
        }
    }

    [Test]
    public void AStartingUnitIsNamedByItsRowAndARaisedUnitByItsTypeAndHome()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var golani = Rows.Single(r => r.Id == C.StartingForces[0].Row);
        Assert.AreEqual(golani.Name, UnitNames.Of(new UnitRef(s.Units[0]), C, Rows, En));
        var raised = new UnitRef(new Unit(s.NextId, "recruit", 0, 0, side: "israel", strength: 2, homeCity: "tel_aviv"));
        var name = UnitNames.Of(raised, C, Rows, En);
        StringAssert.Contains("Tel Aviv", name);
        StringAssert.DoesNotContain(raised.Id.ToString(), name);
    }

    [Test]
    public void AMoveLineHasNoSimNumberInEitherLanguage()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var move = GoldenRun.SmokeMove(C, s);
        var line = Simulation.Preview(C, s, new Command[] { move }).Why.Single(w => w.Key == WhyKeys.UnitMoved);
        foreach (var text in new[] { En, He })
        {
            var shown = UnitNames.Line(line, C, Rows, text);
            StringAssert.DoesNotContain("Unit 0", shown);
            StringAssert.DoesNotMatch(@"\bunit \d", shown);
        }
    }
}
```

- [ ] **Step 2: Run; expect compile errors.**

- [ ] **Step 3: Implement.** `UnitNames.Of`: a starting force (`unit.Id < c.StartingForces.Count` and that force's `Type` and `Side` match the snapshot's, as `EnemyAI.Script` checks, `EnemyAI.cs:73-75`) is its row's name (`NameHe` in Hebrew where it has one); otherwise `text.Format("unit.raised_name", typeLabel, ContentNames.Settlement(...homeCity...))`, or the type label alone with no home city. `UnitNames.Line`: with no key, `cause.Message`; else `text.Format("why.line." + key, values)`, where the values are the units' names then the args (the engagement's two name lists joined with ", ", split by `Args[4]`). `WhyPresenter` shows `UnitNames.Line(w, ...)`; `TurnReportPresenter`'s `Name(id)` uses `UnitNames.Of` for its enemy units (same output as today for a starting force). Add the strings, English first:

| Key | English |
|---|---|
| `unit.raised_name` | {0}, {1} |
| `why.line.unit_moved` | {0} moved to hex {1} for {2} movement points |
| `why.line.fortify_lost` | {0} left hex {1} and is no longer fortified |
| `why.line.fortifying` | {0} is fortifying hex {1}; it is fortified from the end of the turn |
| `why.line.holds` | {0} holds hex {1} |
| `why.line.attack_ended_truce` | {0}'s attack on hex {1} ends: a truce is on |
| `why.line.attack_ordered` | {0} will attack hex {1} |
| `why.line.attack_ended` | {0}'s attack on hex {1} ends: no enemy unit is there |
| `why.line.holds_fire` | {0} holds its fire: no attack on hex {1} to support |
| `why.line.attack_called_off` | {0}'s attack on hex {1} is called off: it needs {2} Arms and the stock has {3} |
| `why.line.engagement` | The attack on hex {2} ({0} against {1}, strength ratio {3}%): the attackers lost {4:N0} people and the defenders lost {5:N0} people |
| `why.line.destroyed` | {0} was destroyed |
| `why.line.retreated` | {0} retreated to hex {1} |
| `why.line.surrendered_prisoners` | {0} surrendered with {1:N0} people: it had no hex to retreat to. They are held as prisoners |
| `why.line.surrendered_held` | {0} surrendered with {1:N0} people: it had no hex to retreat to. They are held by the enemy |
| `why.line.sent_home` | {0} is sent home: its {1:N0} people are civilians in {2} now |
| `why.line.withdrew` | {0} withdrew from {1} to hex {2} |
| `why.line.on_its_way` | {0} is on its way to hex {1} |
| `why.line.stopped_no_route` | {0} stopped: no route reaches hex {1} |
| `why.line.stopped_full` | {0} stopped: hex {1} is full |
| `why.line.arrived` | {0} arrived at hex {1} |

The engagement's values are, in order: the attackers' names, the defenders' names, then `Args` (hex, ratio, attackers lost, defenders lost); the formatter drops `Args[4]` (the count) after splitting. The city and place ids (`sent_home`, `withdrew`) are shown through `ContentNames.Settlement`. Write the Hebrew from the English with the same placeholders, plural address as `strings.he.yaml` does, the native-review comment above the block; numbers and hexes go through `UiText.Format`'s marks (UI-16).

- [ ] **Step 4: Full EditMode with U1, 0 NEW.** Screenshots EN and HE of the Why screen after a move (the unit's name, not its number).

- [ ] **Step 5: Commit** into the U1 PR (or its own PR right after U1), `Refs #1399`. Close #1399 only when the withdrawal lines are converted too (after #1324 and #1327), with a raised unit's Why line shown in EN and HE.

### Task 11: Attacks on screen (U2; after S3)

**Files:**
- Modify: `unity/Assets/Scripts/UI/InfoPanelPresenter.cs:188-237`, `:430-440`
- Modify: `unity/Assets/Scripts/UI/ToastPresenter.cs:15-25`
- Modify: both strings files (`unit.attack_queued` removed)
- Test: `unity/Assets/Tests/EditMode/UnitOrdersTests.cs`, `SaveUiTests.cs`

**Interfaces:**
- Consumes: `Combat.Check(..., spent)`, `PreviewResult.Attacked` (Task 5), `SimBridge.Committed` (Task 9), `UnitNames.Line` (Task 10a).
- Produces: `ToastPresenter` shows a committed order's unit lines.

- [ ] **Step 1: Write the failing tests.** At the real start Golani (unit 0, 10,9) is next to the Syrian unit on 10,10 (`docs/determinism.md`, #1240's row).

```csharp
    // UnitOrdersTests
    /// <summary>R-COMBAT-001 on screen: the attack is fought at the click; the toast names the units, and the card shows
    /// the unit spent, or no unit if its own attack destroyed it (Review Focus 4). The seed fixes the draw, so one branch runs.</summary>
    [Test]
    public void AnAttackShowsItsResultAtOnce()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var tokens = UnitRoster.FromState(Units, C, bridge.State, _ => true, (row, a, q, r) => a);
        var golani = tokens.Single(t => t.Unit.Id == "golani");
        PlacedToken? shown = new PlacedToken(golani, new AnchorSpot(0, 0, 0, 0, false));
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), selectedUnit: () => shown);
        using var toast = new ToastPresenter(ui.Root, bridge, Text);
        var syrian = bridge.State.Units.Values.First(u => u.Side == "syria" && MapActions.Distance(u.Q, u.R, bridge.State.Units[0].Q, bridge.State.Units[0].R) == 1);
        var arms = bridge.State.Arms;
        Assert.IsNull(bridge.Queue(new UnitOrder(0, Combat.Attack, syrian.Q, syrian.R)));
        Assert.AreEqual(arms - 1, bridge.State.Arms, "paid at once (arms_per_attack 1)");
        var line = ui.Q<Label>("toast").text;
        StringAssert.StartsWith("The attack on hex", line);
        StringAssert.DoesNotContain("unit 0", line);
        StringAssert.Contains(golani.Unit.Name, line);
        info.ShowUnit();
        if (bridge.State.Units.ContainsKey(0))
        {
            var full = C.UnitTypes.Single(t => t.Id == bridge.State.Units[0].Type).MovementPoints;
            StringAssert.Contains(Text.Format("unit.points", 0, full), ui.Q<Label>("info-settlements").text);
        }
        else
            Assert.IsFalse(info.ShowsUnit, "a unit its own attack destroyed has no card");
        info.Dispose();
    }

    // SaveUiTests
    /// <summary>Spec 3.4: an attack's result survives a reload of the autosave, unchanged (no re-roll).</summary>
    [Test]
    public void AutosaveKeepsAnAttacksResult()
    {
        var store = TempStore();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        using (GameStart.Autosave(bridge, store, Text))
        {
            var syrian = bridge.State.Units.Values.First(u => u.Side == "syria" && MapActions.Distance(u.Q, u.R, bridge.State.Units[0].Q, bridge.State.Units[0].R) == 1);
            Assert.IsNull(bridge.Queue(new UnitOrder(0, Combat.Attack, syrian.Q, syrian.R)));
            var loaded = GameSession.Load(C, store.ReadAutosave()!, out _);
            Assert.AreEqual(bridge.State.Hash(), loaded.Now.State.Hash());
        }
    }
```

(`MapActions.Distance` is public, `MapActions.cs:42`.) Update `UnitOrdersTests`' attack tests that read `bridge.Queued` or `unit.attack_queued`: the attack is now in `Session.Committed`, with no queued line.

- [ ] **Step 2: Run; expect failures** (the toast shows nothing on a commit).

- [ ] **Step 3: Implement.** `AttackTargets` passes `spent: bridge.Spent` to `Combat.Check`. `ToastPresenter` subscribes a handler held in a field to `bridge.Committed` and shows the lines that have a key through `UnitNames.Line` (Task 10a), one a line; it unsubscribes it in `Dispose`. The subscription is made after `Changed`'s `Clear`, so the result is not cleared by the commit's own `Changed`. Remove the queued-attack line and key. The card's Send home check passes `bridge.Session.Now.Attacked` (Task 5).

- [ ] **Step 4: Full EditMode, 0 NEW; WebGL golden and smoke lines unchanged; screenshots EN and HE of the toast after an attack.**

- [ ] **Step 5: Commit and open the U2 PR.**

### Task 12: Go-to on screen (U3; after S4)

**Files:**
- Modify: `unity/Assets/Scripts/Map/MapScene.cs:156-200` (new `ShowRoute`, `RouteName`)
- Modify: `unity/Assets/Scripts/UI/InfoPanelPresenter.cs` (a pick click on an unlit hex a route reaches gives the go-to; touch confirm)
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs` (hover sends the route to `ShowRoute`; the turn-start toast)
- Modify: both strings files (`unit.move_pick` full text, `unit.route_turns`, `unit.go_to_given`)
- Test: `unity/Assets/Tests/EditMode/UnitOrdersTests.cs`

**Interfaces:**
- Consumes: `Movement.Route` (Task 7), `Movement.GoTo` (Task 8), `UnitNames.Line` (Task 10a).
- Produces: `MapScene.ShowRoute(IReadOnlyList<(int q, int r, int turn)> route)`, `MapScene.RouteName`.

- [ ] **Step 1: Write the failing tests** in `UnitOrdersTests.cs`:

```csharp
    /// <summary>R-MOVE-008 on screen: in Move's pick, a click on an unlit hex a route reaches gives the go-to, and the card
    /// says how many turns.</summary>
    [Test]
    public void AClickBeyondTheLitHexesGivesAGoTo()
    {
        var map = BuildMap();
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
        layer.Show(bridge.Content, bridge.State);
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(),
            selectedUnit: () => layer.Selected is string sel ? layer.Tokens.First(p => p.Token.Unit.Id == sel) : (PlacedToken?)null,
            showReachable: map.ShowReachable);
        var golani = layer.Tokens.Single(p => p.Token.Unit.Id == "golani");
        var id = UnitCard.SimUnit(C, bridge.State, golani.Token.Unit)!.Value;
        var lit = Movement.Reachable(C, bridge.State, id).Keys.ToHashSet();
        var none = new Dictionary<int, int>();
        var far = C.Hexes.Where(h => h.Land && !lit.Contains((h.Q, h.R)) && (h.Q, h.R) != (golani.Q, golani.R))
            .Select(h => (h.Q, h.R)).First(h => Movement.Route(C, bridge.State, id, h.Q, h.R, none).LastOrDefault().Turn == 2);
        GameRoot.OnHexClicked(map, layer, info, golani.Q, golani.R);
        UiTestKit.Press(ui.Q<Button>("info-move"));
        GameRoot.OnHexClicked(map, layer, info, far.Q, far.R);
        Assert.AreEqual((Movement.GoTo, $"{far.Q},{far.R}"), (bridge.State.Units[id].Order, bridge.State.Units[id].OrderTarget));
        StringAssert.Contains(Text.Format("unit.route_turns", 1), ui.Q<Label>("info-settlements").text);   // one more turn after this one
        info.Dispose();
    }

    /// <summary>R-MOVE-009 on screen: a go-to that arrives at the next turn's start says so in the toast, by the unit's name.</summary>
    [Test]
    public void ARouteThatArrivesSaysSo()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        using var toast = new ToastPresenter(ui.Root, bridge, Text);
        var none = new Dictionary<int, int>();
        var lit = Movement.Reachable(C, bridge.State, 0).Keys.ToHashSet();
        var far = C.Hexes.Where(h => h.Land && !lit.Contains((h.Q, h.R)))
            .Select(h => (h.Q, h.R)).First(h => Movement.Route(C, bridge.State, 0, h.Q, h.R, none).LastOrDefault().Turn == 2);
        Assert.IsNull(bridge.Queue(new UnitOrder(0, Movement.GoTo, far.Q, far.R)));
        foreach (var u in bridge.NeedsOrders.ToList()) bridge.Skip(u);   // U4a's block, if it has landed
        bridge.EndTurn();
        Assert.AreEqual((far.Q, far.R), (bridge.State.Units[0].Q, bridge.State.Units[0].R));
        var golaniName = Units.Single(u => u.Id == C.StartingForces[0].Row).Name;
        StringAssert.Contains(golaniName, ui.Q<Label>("toast").text);
    }
```

(If U4a has not landed when this task runs, drop the `Skip` line: `NeedsOrders` and `Skip` come with it. If an enemy moves next to the route in the enemy phase, the route still goes on: no zone of control.)

- [ ] **Step 2: Run; expect failures.**

- [ ] **Step 3: Implement.** The pick keeps its lit set; a click on a hex outside it whose `Movement.Route(...)` is not empty gives `new UnitOrder(unit, Movement.GoTo, q, r)` through `bridge.Queue`; on touch (the click came from a touch), the first tap calls `ShowRoute` and records the hex, and a second tap on the same hex confirms. The card shows `unit.route_turns` for a unit with a go-to, from `Route(...).Last().Turn - 1` (the turns after this one). The toast after End turn shows the route step's keyed lines (`arrived`, `stopped_no_route`, `stopped_full`) from `bridge.Session.Now.Why` through `UnitNames.Line`. `ShowRoute` follows `ShowReachable`'s mesh pattern (`MapScene.cs:160-200`) and draws the turn numbers through `MapLabelsPresenter`'s existing label path.

- [ ] **Step 4: Full EditMode, 0 NEW; WebGL lines unchanged; screenshots EN and HE of a route with turn numbers.**

- [ ] **Step 5: Commit and open the U3 PR.**

### Task 12a: The capture dialog after a capturing move (U6; after S6)

**Files:**
- Modify: `unity/Assets/Scripts/UI/CaptureDialogPresenter.cs:15-25` (its summary: the dialog opens when a move captures)
- Test: the capture dialog's EditMode tests (`git grep -l CaptureDialogPresenter unity/Assets/Tests`)

**Interfaces:**
- Consumes: the capture check at the click (Task 8a), `SimBridge.State` (Task 9).

- [ ] **Step 1: Write the failing test.** At the real start Nazareth's hex is in Golani's reach (`docs/determinism.md:42-49`: the smoke-orders move skips it) and no enemy unit is on it:

```csharp
    /// <summary>R-CTRL-004 (OQ-2, "Instant"): the dialog opens as soon as a move captures a place; its answer waits for End turn.</summary>
    [Test]
    public void TheDialogOpensRightAfterACapturingMove()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        using var dialog = new CaptureDialogPresenter(ui.Root, bridge, Text, eventCard: null);
        var naz = C.SettlementHexes.Single(h => h.Id == "nazareth");
        Assert.IsTrue(Movement.Reachable(C, bridge.State, 0).ContainsKey((naz.Q, naz.R)), "precondition");
        Assert.IsNull(bridge.Queue(new Move(0, naz.Q, naz.R)));
        Assert.AreEqual("nazareth", dialog.Shown);
        UiTestKit.Press(ui.Root.Q<Button>(className: CaptureDialogPresenter.OptionClass));   // the first option, Raid (R-CTRL-008's order)
        Assert.IsTrue(bridge.Queued.OfType<ResolveCapture>().Any(), "the answer is queued for End turn");
    }
```

- [ ] **Step 2: Run; expect failure** (no record before End turn). **Step 3: Implement:** nothing beyond the summary's wording is expected, since the dialog reads `bridge.State`; fix whatever the test shows. **Step 4:** full EditMode, 0 NEW; screenshots EN and HE of the dialog right after a capturing move. **Step 5:** commit and open the U6 PR.

### Task 13a: End turn waits for orders; Skip (U4a)

**Files:**
- Modify: `unity/Assets/Scripts/UI/SimBridge.cs` (skips, `NeedsOrders`)
- Modify: `unity/Assets/Scripts/UI/HudPresenter.cs:130-140`, `:470-485` (the label; the press selects instead of ending)
- Modify: `unity/Assets/Scripts/UI/InfoPanelPresenter.cs` and `unity/Assets/UI/Game.uxml` (the `info-skip` button)
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs:791-867` (both smoke runs skip the units that need orders)
- Modify: both strings files (`unit.order.skip`, `hud.end_turn_needs_orders`, `how_to_play.end_turn`)
- Test: `unity/Assets/Tests/EditMode/SimBridgeGddTests.cs`, `HudTests.cs`, `HebrewUiTests.cs`, `SimSmokeTests.cs`

**Interfaces:**
- Consumes: `Movement.NeedsOrders` (Task 2), `SimBridge.Spent` (Task 9).
- Produces: `SimBridge.NeedsOrders : IReadOnlyList<int>` (less skipped units and units with Send home queued), `SimBridge.Skip(int unit)`, `SimBridge.IsSkipped(int unit)`; `HudPresenter(VisualElement root, SimBridge bridge, UiText text, Action<int>? selectUnit = null)`.

- [ ] **Step 1: Write the failing tests.**

```csharp
    // SimBridgeGddTests
    [Test]
    public void SkippedUnitsAndUnitsGoingHomeDoNotNeedOrders()
    {
        var bridge = new SimBridge(GameSession.New(Slice, GoldenRun.Seed, Mode.Historical), Text);
        var first = bridge.NeedsOrders[0]; var second = bridge.NeedsOrders[1];
        bridge.Skip(first);
        bridge.Queue(new UnitOrder(second, Demobilization.SendHome));
        CollectionAssert.DoesNotContain(bridge.NeedsOrders.ToList(), first);
        CollectionAssert.DoesNotContain(bridge.NeedsOrders.ToList(), second);
        bridge.EndTurn();
        Assert.IsFalse(bridge.IsSkipped(first), "a skip ends with the turn");
    }

    // HudTests
    [Test]
    public void EndTurnWaitsWhileAUnitNeedsOrders()
    {
        using var ui = new UiTestKit();
        var bridge = GddBridge();
        var selected = new List<int>();
        using var hud = new HudPresenter(ui.Root, bridge, Text, selectUnit: selected.Add);
        var endTurn = ui.Q<Button>("end-turn");
        var turn = bridge.TurnStart.Turn;
        Assert.AreEqual(Text["hud.end_turn_needs_orders"], endTurn.text);
        UiTestKit.Press(endTurn);
        Assert.AreEqual(turn, bridge.TurnStart.Turn, "the turn did not end");
        Assert.AreEqual(bridge.NeedsOrders[0], selected.Single());
        foreach (var id in bridge.NeedsOrders.ToList()) bridge.Skip(id);
        Assert.AreEqual(Text["hud.end_turn"], endTurn.text);
        UiTestKit.Press(endTurn);
        Assert.AreEqual(turn + 1, bridge.TurnStart.Turn);
    }

    // HudTests: Review Focus 4: a unit with Send home queued is not selected.
    [Test]
    public void TheCycleSkipsUnitsGoingHome()
    {
        using var ui = new UiTestKit();
        var bridge = GddBridge();
        var selected = new List<int>();
        using var hud = new HudPresenter(ui.Root, bridge, Text, selectUnit: selected.Add);
        var first = bridge.NeedsOrders[0];
        Assert.IsNull(bridge.Queue(new UnitOrder(first, Demobilization.SendHome)));
        UiTestKit.Press(ui.Q<Button>("end-turn"));
        Assert.AreNotEqual(first, selected.Single());
        Assert.AreEqual(bridge.NeedsOrders[0], selected.Single());
    }
```

`HebrewUiTests.InHebrewEveryStaticTextIsTheHebrewString` (`HebrewUiTests.cs:347-366`): the `("end-turn", "hud.end_turn")` pair (`:284`) now expects `hud.end_turn_needs_orders` at the start, since every unit needs orders on the first turn; the Latin-letter check still holds (the label has none). `SimSmokeTests`: the smoke and smoke-orders coroutines still print their unchanged lines.

- [ ] **Step 2: Run; expect failures** (no `NeedsOrders`; End turn ends the turn).

- [ ] **Step 3: Implement.**

```csharp
        // SimBridge
        readonly HashSet<int> skipped = new HashSet<int>();
        /// <summary>R-MOVE-010: the units End turn waits for, in ascending id, less those skipped this turn and those going home.</summary>
        public IReadOnlyList<int> NeedsOrders => Movement.NeedsOrders(Content, State, Spent).Where(id => !skipped.Contains(id) && !GoingHome(id)).ToList();
        /// <summary>Skips the unit for this turn: not an order, not saved, nothing in the game changes (spec 7.2).</summary>
        public void Skip(int unit) { if (skipped.Add(unit)) Notify(Changed); }
        public bool IsSkipped(int unit) => skipped.Contains(unit);
        // EndTurn and Replace: skipped.Clear();
```

`HudPresenter`: the button's text is `hud.end_turn_needs_orders` while `bridge.NeedsOrders.Count > 0`; the press calls `selectUnit(bridge.NeedsOrders[0])` then, and `bridge.EndTurn()` otherwise. There is no force path (OQ-A, owner: "No force"). The card's `info-skip` button ("Skip this turn") calls `bridge.Skip(id)`. `Smoke` and `SmokeOrders`: before pressing End turn, `foreach (var id in bridge.NeedsOrders.ToList()) bridge.Skip(id);` through the card is the player's path, so press `end-turn` (which selects the unit and opens its card) then `info-skip`, repeated while `bridge.NeedsOrders.Count > 0` (guard 200); log the same lines as before. Strings per spec 8.2.

- [ ] **Step 4: Full EditMode on the merged tree, 0 NEW.** WebGL: `golden&smoke=5` prints `SMOKE OK 5 turns 1948-10-15 bc2ab42b...` unchanged and `smoke-orders` prints `SMOKE ORDERS OK 1948-06-15 9f224bd4...` unchanged (Skip changes nothing in the sim; if either moves, stop). Screenshots EN and HE: the blocked button and the Skip button.

- [ ] **Step 5: Commit and open the U4a PR.**

### Task 13b: The cycle and the camera (U4b)

**Files:**
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs` (extract #1394's centring, `GameRoot.cs:480-492`, into `FocusUnit(string rowId)`; HUD's `selectUnit` selects the token and focuses it; automatic cycling after an order that finishes a unit)
- Test: `unity/Assets/Tests/EditMode/FirstTurnGuideTests.cs` (it already opens the real scene for #1394), `UnitOrdersTests.cs`

**Interfaces:**
- Consumes: `HudPresenter`'s `selectUnit` (Task 13a), the sim-id to token mapping #1396 lands.

- [ ] **Step 1: Write the failing tests.**

```csharp
    // FirstTurnGuideTests (its OpenScene, TempDir, Inputs and Ui, as #1394's TheMoveStepShowsTheUnitAndAReachableHexOnTheMap uses them)
    /// <summary>Spec 7.3: End turn, while a unit needs orders, selects it and centres the camera on its hex.</summary>
    [UnityTest]
    public IEnumerator EndTurnFocusesTheNextUnitThatNeedsOrders()
    {
        var game = OpenScene(new SaveStore(TempDir()), new SettingsStore(TempDir()));
        game.Begin(Inputs, 0, startScreen: true);
        UiTestKit.Press(Ui.Q<Button>("start-new-game"));
        UiTestKit.Press(Ui.Q<Button>("guide-skip"));
        yield return null;
        var bridge = game.Bridge!; var units = game.Units!;
        var next = bridge.NeedsOrders[0];
        UiTestKit.Press(Ui.Q<Button>("end-turn"));
        yield return null;
        var token = units.Tokens.Single(t => UnitCard.SimUnit(bridge.Content, bridge.State, t.Token.Unit) == next);
        Assert.AreEqual(token.Token.Unit.Id, units.Selected);
        var hex = bridge.Content.HexAt(token.Q, token.R)!;
        var map = Object.FindFirstObjectByType<MapScene>();
        var rig = Object.FindFirstObjectByType<CameraRig>();
        Assert.Less(Vector3.Distance(map.Frame.ToWorld(hex.Lon, hex.Lat, 0), rig.LookAt), 1f);
    }

    // UnitOrdersTests
    /// <summary>Spec 7.3: an order that finishes the unit for the turn asks for the next unit.</summary>
    [Test]
    public void AFinishingOrderAsksForTheNextUnit()
    {
        using var ui = new UiTestKit();
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        var tokens = UnitRoster.FromState(Units, C, bridge.State, _ => true, (row, a, q, r) => a);
        PlacedToken? shown = new PlacedToken(tokens.Single(t => t.Unit.Id == "golani"), new AnchorSpot(0, 0, 0, 0, false));
        var asked = 0;
        var info = new InfoPanelPresenter(ui.Root, bridge, Text, new Dictionary<string, string>(), selectedUnit: () => shown, unitDone: () => asked++);
        info.ShowUnit();
        UiTestKit.Press(ui.Q<Button>("info-fortify"));
        Assert.AreEqual(1, asked);
        info.Dispose();
    }
```

(`SimUnit` here is the sim-id mapping after #1396; use #1396's replacement if it renames it.) `InfoPanelPresenter` gains an optional `Action? unitDone` it calls after Skip, Fortify, Hold, Send home, an attack, or a move that leaves 0 points; GameRoot passes the same selection the HUD uses.

- [ ] **Step 2: Run; expect failures. Step 3: Implement** `FocusUnit` from #1394's lines (the guide calls it too, so both stay one code path); after Skip, Fortify, Hold, Send home, an attack, or a move that leaves 0 points, the card calls the same selection when `NeedsOrders` is not empty; a move that leaves points does not move the camera. **Step 4:** full EditMode, 0 NEW. **Step 5:** commit and open the U4b PR.

### Task 13c: Dimmed tokens and the phone layout (U4c)

**Files:**
- Modify: `unity/Assets/Scripts/Map/Units/UnitLayer.cs` (`Dim`), `unity/Assets/Scripts/UI/GameRoot.cs:344-345` (dim after `Show`)
- Test: `unity/Assets/Tests/EditMode/UnitOrdersTests.cs`, `UiLayoutTests.cs`

**Interfaces:**
- Produces: `UnitLayer.Dim(IReadOnlyCollection<string> rowIds)`, `UnitLayer.DimColor`.

- [ ] **Step 1: Write the failing tests.**

```csharp
    // UnitOrdersTests
    [Test]
    public void ATokenWithNoPointsLeftIsDimmed()
    {
        var map = BuildMap();
        var layer = UnitLayer.Create(map, Units, C.Hexes, still: true);
        var bridge = new SimBridge(GameSession.New(C, GoldenRun.Seed, Mode.Historical), Text);
        layer.Show(bridge.Content, bridge.State);
        layer.Dim(new[] { "golani" });
        var block = new MaterialPropertyBlock();
        var badge = layer.transform.Find("Tokens/golani/Badge").GetComponentInChildren<Renderer>();
        badge.GetPropertyBlock(block);
        Assert.AreEqual(UnitLayer.DimColor, block.GetColor("_BaseColor"));
        layer.Dim(new string[0]);
        badge.GetPropertyBlock(block);
        Assert.IsTrue(block.isEmpty, "undimmed when it has points again");
    }
```

In `UiLayoutTests`, `TheBlockedEndTurnLabelFitsAtPhoneWidth`: with the layout set-up the file uses for 390 x 844, English and Hebrew, set the end-turn button's text to `hud.end_turn_needs_orders` and assert its resolved width is at least its text's preferred width plus the hourglass padding (`Statecraft.uss` `.end-turn`), as the file's other fit tests do. (Find the token's badge path from `UnitLayer.Rebuild`, `UnitLayer.cs:401-480`: the token's object is named by its row id under the `Tokens` root and its badge child is `Badge`; adjust the path in the test to what `Rebuild` builds.)

- [ ] **Step 2: Run; expect failures. Step 3: Implement** `Dim`: for each token under the tokens root, set a `MaterialPropertyBlock` with `_BaseColor = DimColor` on its `Badge` renderers when its row is listed, else `SetPropertyBlock(null)`; GameRoot calls it after `unitLayer.Show` with the rows of the player's units whose `Movement.PointsLeft` is 0. Shorten the label only if the layout test fails, with the copy reviewed again under `WRITING_UI.md`. **Step 4:** full EditMode, 0 NEW; screenshots EN and HE of a dimmed token and the blocked button at 390 px. **Step 5:** commit and open the U4c PR.

### Task 14: The GDD amendments (D; the owner approved spec 9.3 on 23 October 2026)

**Files:**
- Modify: `docs/GDD.md:38`, `:42`, `:76`; `docs/gdd/military-combat.md:13`, `:15`, `:17`, `:18`; `docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md:251`

- [ ] **Step 1:** Apply exactly the edits of spec 9.3 the owner approved, quoting the ruling and date beside each, in the files' existing style. Nothing the owner did not approve.
- [ ] **Step 2:** `bash scripts/check.sh origin/main` ends with "ALL CHECKS PASSED".
- [ ] **Step 3:** Commit and open the docs PR (`Refs #1398`).

## Self-review

- **Spec coverage (revision 2):** 3.1 and 3.3 (Tasks 1, 4), 3.2 (Tasks 1, 9), 3.4 (Tasks 3, 4, 6, 9, 11), 4.1 to 4.3 and 4.5 (Tasks 5, 6, 11), 4.4 instant capture (Tasks 8a, 12a), 4.6 Send home's front (Task 5), 5 and the full-points hex (Tasks 1, 2a, 7), 6 (Tasks 7, 8, 12), 7 (Tasks 2, 13a, 13b), 8.1 to 8.3 (Tasks 10 to 13c), 8.4 (Tasks 4a to 4c, 10a), 9.1 (every task's golden step), 9.2 (each task's rule step), 9.3 (Task 14), 11 (the slice table and Tasks 4c, 8a, 9).
- **Placeholders:** every test is code. Two lean on an existing harness by name and say exactly what to assert: `UiLayoutTests`' phone-width fit (Task 13c) and the token badge's object path (Task 13c, from `UnitLayer.Rebuild`). Fixture facts the tests rely on were checked: Golani (unit 0) beside the Syrian unit at the start, Nazareth in Golani's reach, the straight-line route uniqueness, the fixture's stack limit (2) and costs (plain 1, hills 2, desert 3), `CaptureTests`' town_a on 1,0.
- **Names:** `PreviewResult` (`State`, `Spent`, `Why`, `Rejected`, `Attacked`), `Simulation.Preview`, `Simulation.IsInstant`, `Movement.PointsLeft`, `Movement.NeedsOrders`, `Movement.Route`, `Movement.GoTo`, `GameSession.Commit`, `Now`, `Committed`, `EndTurn`, `SaveGame.SetOpen`, `Open`, `Version`, `Combat.Strike`, `Demobilization.Check(..., attacked)`, `SettlementControl.CaptureHeld`, `Stamp`, `SimBridge.State`, `TurnStart`, `Spent`, `Committed`, `NeedsOrders`, `Skip`, `IsSkipped`, `HudPresenter(..., selectUnit)`, `InfoPanelPresenter(..., unitDone)`, `MapScene.ShowRoute`, `UnitLayer.Dim`, `UnitRef`, `WhyKeys`, `UnitLines.Line`, `UnitNames.Of`, `UnitNames.Line` are used with the same signatures in every task.
