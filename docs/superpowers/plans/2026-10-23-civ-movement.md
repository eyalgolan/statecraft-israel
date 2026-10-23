# Civ-style movement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Units move and attack the moment the player clicks, within their movement points; multi-turn go-to orders; End turn waits until every unit has orders, as in Civ.

**Architecture:** The sim keeps one command log. The turn's moves and unit orders are *committed* (final, in click order) and the screen shows `Simulation.Preview(turn start, committed)`, the same `RunOrders` code `Step` runs. End turn sends `committed ++ deferred` to `Simulation.Step`. The save gains an open turn (schema 3) so a reload keeps what was done. Go-to is a standing unit order continued by a new route step at the start of `RunOrders`.

**Tech Stack:** C# (netstandard2.1 sim, xUnit tests, `dotnet test sim/Statecraft.sln`), Unity 6 (UI Toolkit, NUnit EditMode tests), `scripts/check.sh`, `tools/web_check.py`.

**Spec:** [docs/superpowers/specs/2026-10-23-civ-movement-design.md](../specs/2026-10-23-civ-movement-design.md). Read it first; section numbers below are the spec's. Tracking issue #1398.

## Global Constraints

- No code before the owner approves the spec. Tasks 5 and 6 (and Task 11) wait for the owner's answer to OQ-1; the plan builds OQ-1 (a). Task 6's capture rule assumes OQ-2's recommendation (capture stays at End turn).
- Unchanged and not to be touched: Q-MOVE-1 to Q-MOVE-5 (terrain costs, points, stacking, no zone of control, no transport), D-A6's resolver order, R-ENEMY-001 to R-ENEMY-006, `Movement.Toward` (the enemy's march), `Movement.Closed` (#1324 and #1327 edit it).
- Instant orders: `Move` and every `UnitOrder`. Everything else stays deferred (spec 3.2). `Simulation.IsInstant` is the only definition.
- End turn's batch order: route step, committed orders in click order, deferred orders in queue order (spec 3.3).
- Every Why line that names a unit carries its key, unit snapshots and values (`UnitLines.Line`, Task 4a; #1399); Tasks 5 and 8 write their new lines that way.
- Every PR: one slice, `Refs #1398`, a `changelog.d/` fragment for user-visible paths, rule text in `docs/rules/mvp.md` in the same PR as the code, a citing test for every new or reworded R-ID (`uv run scripts/oss/rule_ids.py`).
- Sim gates: `bash scripts/check.sh origin/main` ends with "ALL CHECKS PASSED"; `dotnet test sim/Statecraft.sln` with 0 NEW failures against main; goldens unchanged unless the task says otherwise. A moved golden: `STATECRAFT_UPDATE_GOLDEN=1 dotnet test sim/Statecraft.sln`, a history row in `docs/determinism.md`, the SMOKE lines in `RELEASING.md`; never hand-edit a golden.
- Unity gates: everything above, plus `uv run tools/build_sim_for_unity.py`, a FULL EditMode run on the PR merged with current origin/main (`export UNITY_MCP_SERVER_PATH=<existing empty file>`, never `-quit` with `-runTests`, XML total > 0, 0 NEW failures), the WebGL gzip `web_check --query "golden&smoke=5"` with every manifest line and the SMOKE line, and the `smoke-orders` run (both commands in `docs/determinism.md:42-56`), and before/after screenshots in English and Hebrew, same state, camera and zoom, on `captures/pr-<n>`. Drop the settings churn after any Unity run: `git checkout -- unity/Assets/Settings unity/ProjectSettings`.
- Copy: `docs/WRITING_UI.md`, `docs/WRITING_UI_HE.md` (binding for new strings since #1395) and `docs/UX_GLOSSARY.yaml`. Keys and placeholders match in `strings.en.yaml` and `strings.he.yaml`. Hebrew is agent-drafted and marked for the native reviewer in a comment above the keys. No em dashes anywhere.
- Commits: `git -c user.name=eyalgolan -c user.email=eyalgolan@users.noreply.github.com commit`, with the trailers `Co-Authored-By: Claude Opus 5.5 (1M context) <noreply@anthropic.com>` and the session line. Never force-push; plain-merge origin/main.

## Review Focus

1. **Reload in the middle of a turn after an attack.** The player expects the attack, its losses and the Arms spent to be there after reload, with the same result (no undo, no re-roll). Pinned in Task 4 (`AReloadKeepsTheOpenTurn`) and Task 6 (`AReloadKeepsAnAttacksResult`).
2. **A deferred order changed after an attack.** Withdrawing or replacing a queued policy (a vote draws from the RNG) after an attack must not change the attack's result on screen or at End turn. Pinned in Task 4 (`DeferredOrdersRunAfterCommittedOnes`) and Task 6 (`AVoteQueuedAfterAnAttackDoesNotChangeIt`).
3. **"A unit needs orders" at phone width and in Hebrew.** The End turn label is longer; it must fit the button at 390 px, in both languages, with the hourglass. Pinned in Task 13 (`TheBlockedEndTurnLabelFitsAtPhoneWidth`).
4. **A unit that leaves the map mid-turn** (destroyed in its own attack, surrendered, or sent home) while its card or a pick is open, or while it is next in the cycle: the card closes, the cycle skips it, nothing throws. Pinned in Task 10 (`ASentHomeUnitClosesItsCard`) and Task 13 (`TheCycleSkipsAUnitThatLeftTheMap`).
5. **A go-to whose destination changes under it** (an enemy unit moves onto it, or the hex fills up): the unit stops, needs orders and is in the cycle. Pinned in Task 8 (`ARouteToAHexAnEnemyTookStops`, `ARouteWhoseHexIsFullStopsOnArrival`) and Task 13 (`AStoppedRouteNeedsOrders`).

---

## Slices and who drafts them

| Slice | Tasks | Needs | Drafted by |
|---|---|---|---|
| S1 sim: preview, points left, needs-orders | 1, 2 | owner's spec approval | Codex (cx135's draft on `codex/civ-move-reach-spent` is the start of Task 1) |
| S2 sim: session and save | 3, 4 | S1 | Codex |
| S5 sim: units named in Why lines (#1399) | 4a | S2 (lands before S3 and S4, so their new lines are keyed from the start) | Codex (cx137's sweep lists every site) |
| S3 sim: instant combat | 5, 6 | S2, OQ-1 | Codex |
| S4 sim: go-to | 7, 8 | S2 | Codex |
| U1 Unity: instant moves on screen | 9, 10 | S2, #1396 (#1394 is merged) | Claude lane |
| U5 Unity: units named in Why lines (#1399) | 10a | S5, #1396; in the U1 PR or right after it | Claude lane |
| U2 Unity: instant attacks on screen | 11 | S3, U1 | Claude lane |
| U3 Unity: go-to on screen | 12 | S4, U1 | Claude lane |
| U4 Unity: End turn waits | 13 | U1 (and U3 if landed, for the go-to toasts) | Claude lane |
| D docs: the GDD amendments | 14 | owner's approval of spec 9.3 | Claude lane |

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

    [Fact]
    public void OnlyMovesAndUnitOrdersAreInstant()
    {
        Assert.True(Simulation.IsInstant(new Move(1, 1, 1)));
        Assert.True(Simulation.IsInstant(new UnitOrder(1, Orders.Fortify)));
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

        /// <summary>R-TURN-004: a move or a unit order takes effect at once and is final; every other order waits for End turn.</summary>
        public static bool IsInstant(Command cmd) => cmd is Move || cmd is UnitOrder;
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

### Task 4a: Units named in the Why lines, sim side (S5, #1399)

**Files:**
- Modify: `sim/Statecraft.Sim/Cause.cs` (`Key`, `Units`, `Args`), create `sim/Statecraft.Sim/UnitLines.cs` (`UnitRef`, `WhyKeys`, `UnitLines.Line`)
- Modify: `sim/Statecraft.Sim/Movement.cs:78-82`, `Orders.cs:55-57`, `:69-73`, `Combat.cs:75-82`, `:118-124`, `:210-238`, `:248-252`, `:271-300`, `:305-318` (`Take` keeps snapshots), `Demobilization.cs:73-74`, `EnemyAI.cs:59-60`
- Modify, only once #1324 and #1327 have landed (else a one-commit follow-up right after they do): `SettlementControl.cs` (the withdrawal line and refusal, `:107-120` on `de78253e`)
- Test: create `sim/Statecraft.Sim.Tests/UnitLinesTests.cs`
- Read first: the sweep `/private/tmp/claude-501/-Users-eyalgolan/b56e4751-b38f-4be9-a118-5c60998902ef/scratchpad/cx137-raised-names/wt/raised-unit-sweep.md` (all 18 paths), and spec 8.4.

**Interfaces:**
- Produces: `public sealed class UnitRef { int Id; string Type; string Side; string HomeCity; public UnitRef(Unit u) }`; `Cause.Key : string` ("" for none), `Cause.Units : IReadOnlyList<UnitRef>`, `Cause.Args : IReadOnlyList<string>`; `public static class WhyKeys` with one `const string` per line and `public static readonly IReadOnlyList<string> All`; `internal static Cause UnitLines.Line(string key, string message, IReadOnlyList<Unit> units, params string[] args)` (system `map`, target the first unit's id, delta 0).
- The keys and their units and args (the Unity strings in Task 10a use the same order: the units' names first, then the args):

| Key | Units | Args |
|---|---|---|
| `unit_moved` | the unit | hex, cost |
| `fortify_lost` | the unit | hex |
| `fortifying` | the unit | hex |
| `holds` | the unit | hex |
| `attack_ended_truce` | the unit | hex |
| `attack_ordered` | the unit | hex |
| `attack_ended` | the unit | hex |
| `holds_fire` | the unit | hex |
| `attack_called_off` | the unit | hex, need, stock |
| `engagement` | the attackers, then the defenders (each in id order) | hex, ratio, attackers lost (people), defenders lost (people), number of attackers |
| `destroyed` | the unit | |
| `retreated` | the unit | hex |
| `surrendered_prisoners` | the unit | people |
| `surrendered_held` | the unit | people |
| `sent_home` | the unit | people, city id |
| `withdrew` | the unit | place id, hex |
| `on_its_way`, `stopped_enemy`, `stopped_no_route`, `stopped_full`, `arrived` | the unit | hex (`stopped_enemy`: the route's hex) |

The last row's keys are declared here and first used by Task 8; `attack_ordered`, `attack_ended`, `holds_fire` and `attack_called_off` stay for the enemy's standing attacks after Task 5.

- [ ] **Step 1: Write the failing tests** in `UnitLinesTests.cs`, one per message family, each for a starting unit (the real content's unit 0, Golani, `GoldenRun.Seed`) and a raised unit (an id at or past `C.StartingForces.Count`, type `recruit`, home city `tel_aviv`, added to the state as `CombatTests.With` does):

```csharp
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Xunit;

/// <summary>#1399 (spec 2026-10-23 section 8.4): every Why line that names a unit carries its key, a snapshot of each unit
/// it names and its values, so the UI can name the unit in the player's language, even once the unit has left the state.</summary>
public class UnitLinesTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    static (GameState s, int raised) Start()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var id = s.NextId;
        var golani = s.Units[0];
        s.Units.Add(id, new Unit(id, "recruit", golani.Q, golani.R, side: "israel", strength: 2, homeCity: "tel_aviv"));
        s.NextId = id + 1;
        return (s, id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AMoveNamesItsUnit(bool raised)
    {
        var (s, r) = Start();
        var id = raised ? r : 0;
        var to = Movement.Reachable(C, s, id).Keys.First();
        var line = Simulation.Preview(C, s, new Command[] { new Move(id, to.Q, to.R) }).Why.Single(w => w.Key == WhyKeys.UnitMoved);
        var u = Assert.Single(line.Units);
        Assert.Equal((id, s.Units[id].Type, s.Units[id].HomeCity), (u.Id, u.Type, u.HomeCity));
        Assert.Equal($"{to.Q},{to.R}", line.Args[0]);
    }

    [Fact]
    public void ADestroyedUnitKeepsItsIdentity()
    {
        // A one-strength player unit attacked by a strong enemy unit in step 2 (EnemyAI's own attack, or a fixture state
        // with the enemy's standing attack set as CombatTests.Attacking does): the "destroyed" line's unit is the removed
        // unit's snapshot, with its type and home city, though the state no longer holds it.
    }

    [Fact]
    public void EveryKeyIsListed() => Assert.Equal(WhyKeys.All.Distinct().Count(), WhyKeys.All.Count);
}
```

Write `ADestroyedUnitKeepsItsIdentity` and one test each for the order lines (fortifying, holds, fortify lost), the engagement (both lists, in id order, and the attacker count in `Args[4]`), retreat, surrender, sent home and (when it is in this slice) withdrew, concretely, from the setups `CombatTests`, `OrdersTests`, `DemobilizationTests` and `RaidTests` already use. Each asserts `Key`, every `UnitRef`'s id, type and home city, and `Args`; none asserts `Message` beyond what the existing tests already do.

- [ ] **Step 2: Run; expect compile errors.**

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
            SentHome = "sent_home", Withdrew = "withdrew", OnItsWay = "on_its_way", StoppedEnemy = "stopped_enemy",
            StoppedNoRoute = "stopped_no_route", StoppedFull = "stopped_full", Arrived = "arrived";
        public static readonly IReadOnlyList<string> All = new[] { UnitMoved, FortifyLost, Fortifying, Holds, AttackEndedTruce,
            AttackOrdered, AttackEnded, HoldsFire, AttackCalledOff, Engagement, Destroyed, Retreated, SurrenderedPrisoners,
            SurrenderedHeld, SentHome, Withdrew, OnItsWay, StoppedEnemy, StoppedNoRoute, StoppedFull, Arrived };
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

Convert each site to `why.Add(UnitLines.Line(WhyKeys.X, <the same English message>, new[] { u }, ...))`, taking the unit snapshot *before* the state row is replaced or removed. Keep each site's existing `delta`, `target`, `enemyOrder` and `enemyAttackers` where they differ from `Line`'s defaults (the engagement's target is its hex and it carries `enemyOrder`/`enemyAttackers`, #1297; `sent_home`'s delta is the strength): build those with the `Cause` constructor and the new named arguments instead of `Line`. `Combat.Take` collects the removed `Unit`s, not ids. The surrender line splits on `Prisoners.Take`'s two returns ("They are held as prisoners" / "They are held by the enemy", `Prisoners.cs:40-43`) into `surrendered_prisoners` and `surrendered_held`. The withdrawal refusal becomes "A unit has no free hex next to {name} to withdraw to" (a refusal string, no key).

- [ ] **Step 4: Run UnitLinesTests and the suite.** Expected: PASS; every existing test that reads `Message` unchanged; goldens unchanged (the state hash does not read causes: `GameState.Hash`, `GameState.cs:213-257`).

- [ ] **Step 5: Commit.** `git commit -m "sim: Why lines carry the units they name (Refs #1399, #1398)"`. Open the S5 PR (`Refs #1399`, `Refs #1398`). Sim gates.

### Task 5: The instant attack (S3; waits for OQ-1, builds OQ-1 (a))

**Files:**
- Modify: `sim/Statecraft.Sim/Combat.cs:73-240` (`Check`, new `Strike`, `Resolve`, remove `EndAttacksForTruce`)
- Modify: `sim/Statecraft.Sim/Simulation.cs:180-181` (attack goes to `Strike`)
- Modify: `sim/Statecraft.Sim/EventSystem.cs:153` (drop the `EndAttacksForTruce` call)
- Modify: `sim/Statecraft.Sim/Reference/ReferencePlayer.cs:43-63`
- Test: `sim/Statecraft.Sim.Tests/CombatTests.cs`, `CombatDrawCountTests.cs`, `ArtilleryTests.cs`, `TruceTests` (whichever file holds R-COMBAT-009's tests: `git grep -l "R-COMBAT-009" sim/Statecraft.Sim.Tests`)
- Docs: `docs/rules/mvp.md:217-237` (R-COMBAT-001, -002, -004, -007, -009); `docs/determinism.md` (new "Combat draws" section)

**Interfaces:**
- Consumes: `Movement.Points`, `Movement.PointsLeft` (Task 1); `spent` in `Execute`.
- Produces: `Combat.Check(Content, GameState, int unitId, int? q, int? r, bool enemy = false, IReadOnlyDictionary<int,int>? spent = null) -> string?`; `internal static string? Combat.Strike(Content, GameState, int unitId, int? q, int? r, Dictionary<int,int> spent, List<Cause> why)`. `Combat.Give` stays for the enemy only.

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

In `ArtilleryTests.cs`, replace the "holds its fire" test with: artillery alone in range attacks at once, pays `arms_per_fire`, and loses nothing (OQ-1 (a); R-COMBAT-007's sparing). In the R-COMBAT-009 tests, replace "accepting a truce ends the player's standing attacks" with: an attack given on a turn after acceptance is refused with "A truce is in force".

- [ ] **Step 2: Run; expect failures** (the attack is still a standing order).

- [ ] **Step 3: Implement.** `Check` gains `IReadOnlyDictionary<int, int>? spent = null`; for the player (`!enemy`): drop the "already has that order" line and the `committed` sum; after the reach and defender checks add `if (spent != null && Movement.PointsLeft(c, s, unitId, spent) == 0) return "That unit has no movement points left this turn";` and the Arms check becomes `s.Arms < need ? Short(need, s.Arms, 0) : null`. Keep the enemy's path as it is (its "already has that order" line stays for `EnemyAI.Goals`).

```csharp
        /// <summary>R-COMBAT-001 (owner, 23 October 2026, "Instant, like Civ"): the player's unit attacks (q, r) at once:
        /// refused, the state untouched, with <see cref="Check"/>'s reason; else it pays its Arms (R-COMBAT-004), loses its
        /// order and the rest of its movement points, and fights one engagement alone (OQ-1 (a)), drawing once.</summary>
        internal static string? Strike(Content c, GameState s, int unitId, int? q, int? r, Dictionary<int, int> spent, List<Cause> why)
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
            Engage(c, s, mvp, new List<Unit> { attacker }, Defenders(s, attacker, q!.Value, r!.Value), q.Value, r.Value, why);
            return null;
        }
```

In `Execute` (`Simulation.cs:181`): `if (o.Order == Combat.Attack) return Combat.Strike(c, s, o.Unit, o.Q, o.R, spent, why);`. In `Resolve`, delete the player's Arms branch (`Combat.cs:219-228`): only enemy units hold a standing attack now; say so in the summary. Delete `EndAttacksForTruce` and its call (`EventSystem.cs:153`). In `ReferencePlayer.Attack`, drop the `u.Order != Combat.Attack` filter and count `attacking` from 0 (its Arms check then counts this turn's attacks).

- [ ] **Step 4: Run CombatTests, ArtilleryTests, CombatDrawCountTests, the R-COMBAT-009 tests, EnemyAITests, DemobilizationTests, then the suite.** Expected: PASS, and the goldens unchanged (the reference player orders no attack in any golden run: spec 2.2). If a golden moves, stop and find out why before re-recording.

- [ ] **Step 5: Rule and doc text.** Rewrite R-COMBAT-001 per spec 4.1; R-COMBAT-002: "Resolver step 2 resolves the enemy's attacks ... A player's attack is fought when its order runs (R-COMBAT-001), one engagement with one draw"; R-COMBAT-004: "paid when the attack is given; an attack the stock cannot pay for is refused"; R-COMBAT-007 per OQ-1 (a): "alone, it attacks at once like any unit, and its losses spare it, so it loses nothing"; R-COMBAT-009: remove the standing-attack sentence. In `docs/determinism.md`, add "## Combat draws": a player engagement draws `Combat.DrawsPerEngagement` (1) when its order runs in `RunOrders`, after the route step and any earlier committed order, before every waiting order; the enemy's engagements draw in step 2, in ascending attacker id.

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

- [ ] **Step 3: Implement** in `Load`, after the ruleset check: `if (saved.Version < 3 && saved.Ruleset == SaveGame.GddRuleset && saved.Turns.Any(t => t.Any(x => x is UnitOrder o && o.Order == Combat.Attack))) throw new SaveException(0, "This save was made with an earlier version of the game's rules.", SaveException.OlderRules);`

- [ ] **Step 4: Run and the suite; expect PASS.**

- [ ] **Step 5: Rule text.** "R-SAVE-004: A schema 2 save of the GDD game with a player attack (`order <unit> attack <q> <r>`) is refused with the code `older_rules` (R-SAVE-001): its attacks were standing orders, so it would replay differently (spec 2026-10-23 section 3.4). Other schema 2 saves load."

- [ ] **Step 6: Commit; open the S3 PR (Tasks 5, 6).** Coordinate R-COMBAT-009's text with #750 (Agam) if it is open: whichever lands second merges the paragraph. Tell #752's owner that player attacks are now given each turn.

### Task 7: The route (S4)

**Files:**
- Modify: `sim/Statecraft.Sim/Movement.cs:149-178` (`Search` takes `maxStep`), new `Route`
- Test: create `sim/Statecraft.Sim.Tests/GoToTests.cs`
- Docs: `docs/rules/mvp.md` (R-MOVE-002 addition)

**Interfaces:**
- Produces: `Movement.Route(Content c, GameState s, int unitId, int q, int r, IReadOnlyDictionary<int,int> spent) -> IReadOnlyList<(int Q, int R, int Turn)>` (empty: no route). `Search(c, s, u, budget, maxStep = int.MaxValue)`.

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

    // R-MOVE-002: no route enters a hex that costs more than the unit's full points (the fixture's desert costs 3).
    [Fact]
    public void TheRouteGoesRoundAHexTheUnitCanNeverEnter()
    {
        var c = Board((2, 0, "desert"), (2, 1, "desert"));
        var s = CombatTests.With(c, new Unit(1, "gun", 0, 1, side: "israel", strength: 3));
        var route = Movement.Route(c, s, 1, 4, 1, None);
        Assert.NotEmpty(route);
        Assert.DoesNotContain(route, h => (h.Q, h.R) == (2, 0) || (h.Q, h.R) == (2, 1));
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

The straight path is the only one of cost 5 from 0,1 to 5,1 (the q distance is 5 and r does not change, so every step is (+1, 0)); the fixture's costs are coastal plain 1, hills 2, desert 3 (`MovementTests`' summary). From 0,1 to 4,1 round the two desert hexes the gun can go 0,2, 1,2, 2,2, 3,1, 4,1.

- [ ] **Step 2: Run; expect compile errors.**

- [ ] **Step 3: Implement.** `Search` gains `int maxStep = int.MaxValue` and skips a neighbour whose `step > maxStep` (after the road discount). Existing callers pass nothing, so `Reachable`, `Path`, `Move` and `Toward` are unchanged.

```csharp
        /// <summary>R-MOVE-002, R-MOVE-008: the cheapest path to (q, r) with no points limit, never into a hex that costs more
        /// than the unit's full points, each hex with the turn it is reached on (turn 1 spends the points left after
        /// <paramref name="spent"/>, later turns all of them). Empty for an unknown or enemy unit, one that cannot move, its
        /// own hex, or a hex no such path reaches.</summary>
        public static IReadOnlyList<(int Q, int R, int Turn)> Route(Content c, GameState s, int unitId, int q, int r, IReadOnlyDictionary<int, int> spent)
        {
            var result = new List<(int Q, int R, int Turn)>();
            if (!s.Units.TryGetValue(unitId, out var u) || IsEnemy(u) || (u.Q, u.R) == (q, r)) return result;
            var full = Points(c, u);
            if (full == 0) return result;
            var (dist, prev) = Search(c, s, u, int.MaxValue, full);
            if (!dist.ContainsKey((q, r))) return result;
            var hexes = new List<(int Q, int R)>();
            for (var h = (q, r); h != (u.Q, u.R); h = prev[h]) hexes.Add(h);
            hexes.Reverse();
            spent.TryGetValue(unitId, out var used);
            int left = full - used, turn = 1, before = 0;
            foreach (var h in hexes)
            {
                var step = dist[h] - before; before = dist[h];
                if (step > left) { turn++; left = full; }
                left -= step;
                result.Add((h.Q, h.R, turn));
            }
            return result;
        }
```

- [ ] **Step 4: Run and the suite; expect PASS, no golden moves.**

- [ ] **Step 5: Rule text.** R-MOVE-002: add "`Movement.Route(c, s, unit, q, r, spent)` is the cheapest path with no points limit that enters no hex costing more than the unit's full points, each hex numbered with the turn it is reached on: turn 1 spends the points left, later turns all of them, and a step that does not fit what is left of a turn starts the next."

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

    // R-MOVE-009: a route stops, and the unit needs orders, when an enemy unit stands next to it at the turn's start.
    [Fact]
    public void ARouteStopsNextToAnEnemy()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 2, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    new Unit(2, "enemy", 3, 0, side: "egypt", strength: 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Equal((2, 1), (now.State.Units[1].Q, now.State.Units[1].R));
        Assert.Contains(1, Movement.NeedsOrders(c, now.State, now.Spent));
    }

    [Fact]
    public void ARouteToAHexAnEnemyTookStops()
    {
        var c = Board();
        var s = CombatTests.With(c, new Unit(1, "soldier", 0, 1, side: "israel", strength: 3, order: Movement.GoTo, orderTarget: "5,1"),
                                    new Unit(2, "enemy", 5, 1, side: "egypt", strength: 1));
        var now = Simulation.Preview(c, s, new Command[0]);
        Assert.Equal("", now.State.Units[1].Order);
        Assert.Contains(now.Why, w => w.Message.Contains("no route reaches"));
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

        /// <summary>R-MOVE-009, the route step: at the start of the player's turn, in ascending id, each unit with a go-to stops
        /// next to an enemy military unit, else moves on (<see cref="Advance"/>).</summary>
        internal static void ContinueRoutes(Content c, GameState s, Dictionary<int, int> spent, List<Cause> why)
        {
            foreach (var id in s.Units.Keys.ToList())
            {
                if (!s.Units.TryGetValue(id, out var u) || u.Order != GoTo) continue;
                if (Neighbours.Any(d => s.Units.Values.Any(x => IsEnemy(x) && x.Q == u.Q + d.Q && x.R == u.R + d.R)))
                {
                    Stop(s, u, WhyKeys.StoppedEnemy, $"Unit {id} stopped on its way to hex {u.OrderTarget}: an enemy unit is next to it", u.OrderTarget, why);
                    continue;
                }
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

- [ ] **Step 5: Rule text.** R-MOVE-008 and R-MOVE-009 from spec 6.1 to 6.3, with "an enemy military unit next to it stops the route (agent default)". R-ORDER-001: "A new order replaces a go-to too."

- [ ] **Step 6: Commit; open the S4 PR (Tasks 7, 8).**

### Task 9: The bridge commits moves and unit orders (U1)

**Files:**
- Modify: `unity/Assets/Scripts/UI/SimBridge.cs`
- Modify: `unity/Assets/Scripts/UI/GameStart.cs:64-90` (autosave after each commit; receipt from `TurnStart`)
- Test: `unity/Assets/Tests/EditMode/SimBridgeGddTests.cs`, `SimBridgeGuardTests.cs`

**Interfaces:**
- Consumes: `GameSession.Commit`, `Now`, `Committed`, `EndTurn` (Task 4).
- Produces: `SimBridge.State` (now `Session.Now.State`), `SimBridge.TurnStart` (`Session.State`), `SimBridge.Spent`, `event Action<IReadOnlyList<Cause>>? Committed` (the new Why lines of a committed order), `Queued` (deferred only). `Queue(Command)` commits an instant order.

- [ ] **Step 1: Write the failing EditMode tests** in `SimBridgeGddTests.cs`:

```csharp
    /// <summary>R-TURN-004: a Move given through the bridge shows at once (State), and End turn keeps it.</summary>
    [Test]
    public void AMoveShowsAtOnce()
    {
        var bridge = new SimBridge(GameSession.New(Gdd, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Gdd, bridge.State);
        IReadOnlyList<Cause>? lines = null; bridge.Committed += w => lines = w;
        Assert.IsNull(bridge.Queue(move));
        Assert.AreEqual((move.Q, move.R), (bridge.State.Units[move.Unit].Q, bridge.State.Units[move.Unit].R));
        Assert.AreNotEqual((move.Q, move.R), (bridge.TurnStart.Units[move.Unit].Q, bridge.TurnStart.Units[move.Unit].R));
        Assert.IsEmpty(bridge.Queued, "a committed order is not queued");
        Assert.IsNotNull(lines);
        bridge.EndTurn();
        Assert.AreEqual(GoldenRun.PlaySmokeOrders(Gdd).Hash(), bridge.TurnStart.Hash());
    }

    /// <summary>No undo: two moves for one unit are both kept, and Withdraw does nothing to a committed one.</summary>
    [Test]
    public void ACommittedMoveCannotBeWithdrawn()
    {
        var bridge = new SimBridge(GameSession.New(Gdd, GoldenRun.Seed, Mode.Historical), Text);
        var move = GoldenRun.SmokeMove(Gdd, bridge.State);
        bridge.Queue(move);
        bridge.Withdraw(move);
        Assert.AreEqual(1, bridge.Session.Committed.Count);
    }
```

(`Gdd` and `Text` are the fixtures `SimBridgeGddTests` already uses; read its top and reuse them.) In `SimBridgeGuardTests`, the check that every command type has a key or is one per turn now also accepts `Simulation.IsInstant` types (they are never keyed).

- [ ] **Step 2: Run them in Unity** (single-class runs are fine while developing: `-testFilter SimBridgeGddTests`); expect compile errors.

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
```

`Price`, `AttentionLeft`, `DelegatedTo` and `Vacancies` read `TurnStart` and `All` (the dry runs start from the turn's start). In `Queue`, after the game-over check:

```csharp
            if (Simulation.IsInstant(c))
            {
                var before = Session.Now.Why.Count;
                var refused = Session.Commit(c);
                if (refused != null) return Refuse(refused);
                Notify(Changed);
                Notify(Committed, Session.Now.Why.Skip(before).ToList());
                return null;
            }
```

`EndTurn` steps with `deferred` (the default stepper becomes `(s, cmds) => s.EndTurn(cmds)`), sets `LastBatch` to the committed list plus `deferred`, and clears `deferred` only (committed orders stay with the session if the step throws). `Key` loses the `Move` and `UnitOrder` lines. If #1352 has landed, its `BudgetUnsubmitted` passes `deferred`.

In `GameStart.Autosave`, subscribe the same `Save` to `bridge.Committed` (`bridge.Committed += _ => Save(Array.Empty<Rejection>())`, unsubscribed in the same `Unsubscribe`), and take the receipt's state from `bridge.TurnStart` (the save's `end` hash is the turn start's).

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

- [ ] **Step 1: Write the failing EditMode tests** in `UnitOrdersTests.cs`:

```csharp
    /// <summary>R-MOVE-001 on screen: after a move the Move pick opens again and lights only what the points left reach.</summary>
    [Test]
    public void TheLitReachShrinksAfterAMove()
    {
        // Build the map and the card as TheCardOffersMoveOnlyWhereTheSimHasAHex does; select Golani; press Move;
        // click GoldenRun.SmokeMove's hex. Then:
        //   Assert the token moved (unitLayer's placed token for Golani is on the new hex);
        //   Assert the lit set equals Movement.Reachable(C, bridge.State, id, bridge.Spent).Keys and is smaller than before;
        //   Assert the card shows text.Format("unit.points", left, full).
    }

    /// <summary>Review Focus 4: a unit sent home closes its card and its pick; nothing throws.</summary>
    [Test]
    public void ASentHomeUnitClosesItsCard() { /* select a unit whose Demobilization.Check is null after an armistice (set ChosenOptions as DemobilizationTests does), press Send home, assert the card shows no unit */ }
```

Write both bodies concretely from the existing tests in the same file (`TheCardOffersMoveOnlyWhereTheSimHasAHex` builds the map, the card and the bridge); the comments above are the assertions each must make.

In `FirstTurnGuideTests`, the move step's text assertion takes the new `guide.move.text`.

- [ ] **Step 2: Run; expect failures.**

- [ ] **Step 3: Implement.** `MoveTargets` reads `Movement.Reachable(bridge.Content, bridge.State, id, bridge.Spent)`. In `PickTarget`, after a Move is taken, reopen the pick when `MoveTargets()` still has hexes (`TogglePick(false)`), so the lit area shows the points left. The card body gets, for the player's military units, `text.Format("unit.points", Movement.PointsLeft(c, s, id, bridge.Spent), full)` with `full` from `c.UnitTypes`. The standing-order line reads the unit's `Order` in `bridge.State` (fortify or fortifying: `unit.fortify_queued`; hold: `unit.hold_queued`), not `QueuedLike`. Drop the `unit.move_queued` and `unit.send_home_queued` lines and keys; change `guide.move.text`, `unit.move_pick` (its go-to half lands with Task 12: until then keep "Choose a lit hex to move to"), `unit.hold_queued`, and add `unit.points` in both files from spec 8.2, with the native-review comment. Add the glossary entry `movement-points` (status decision-needed; en "movement points"; he candidate "נקודות תנועה"; in_use `unit.points`) to the YAML, then the Markdown view.

In `GameRoot.SmokeOrders`, check `bridge.Session.Committed.OfType<Move>()` instead of `bridge.Queued`, and log `bridge.TurnStart.Hash()` (Smoke too).

- [ ] **Step 4: Run the full EditMode suite on the branch merged with origin/main; expect 0 NEW failures.**

- [ ] **Step 5: Gates for the U1 PR (Tasks 9, 10):** the Unity gates; WebGL `golden&smoke=5` and `smoke-orders` print the unchanged lines; screenshots EN and HE of a unit card before and after a move (points line, the shrunk lit area).

- [ ] **Step 6: Commit and open the U1 PR.** It needs #1396 on main (or #1396 in the same review round); say so in the PR body.

### Task 10a: Units named in the Why lines, on screen (U5, #1399; with U1)

**Files:**
- Create: `unity/Assets/Scripts/UI/UnitNames.cs`
- Modify: `unity/Assets/Scripts/UI/WhyPresenter.cs:45-60`, `unity/Assets/Scripts/UI/TurnReportPresenter.cs:112-128` (its `Name` becomes `UnitNames`)
- Modify: both strings files (`why.line.<key>` for every `WhyKeys.All`, `unit.raised_name`)
- Test: create `unity/Assets/Tests/EditMode/WhyUnitNamesTests.cs`

**Interfaces:**
- Consumes: `Cause.Key`, `Units`, `Args`, `WhyKeys.All` (Task 4a); #1396's raised-unit type label (use its helper and keys; if it adds none, the type label is `city.track_<type>`, `strings.en.yaml:498-501`).
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
    static UiText He => UiText.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(UiPaths.Strings.Replace(".en.", ".he.")).text);

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

Use the strings path constant the other Hebrew tests use if `UiPaths` has one for Hebrew (`HebrewUiTests` reads it); do not build the path by string replace if a constant exists.

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
| `why.line.stopped_enemy` | {0} stopped on its way to hex {1}: an enemy unit is next to it |
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
- Test: `unity/Assets/Tests/EditMode/UnitOrdersTests.cs`

**Interfaces:**
- Consumes: `Combat.Check(..., spent)` (Task 5), `SimBridge.Committed` (Task 9).
- Produces: `ToastPresenter` shows a committed attack's result.
- Consumes also: `UnitNames.Line` (Task 10a).

- [ ] **Step 1: Write the failing test.**

```csharp
    /// <summary>R-COMBAT-001 on screen: the attack is fought at the click; the toast says what each side lost.</summary>
    [Test]
    public void AnAttackShowsItsResultAtOnce()
    {
        // From the start, commit Moves from Movement.Reachable until a player unit's Combat.Check(C, bridge.State, id, q, r,
        // spent: bridge.Spent) takes an enemy hex (assert that precondition), then press Attack and click the hex.
        // Assert: bridge.State's Arms fell by the unit's need; the toast's first line starts with the English "The attack on hex" and names the unit, not its number;
        // the unit card shows "Movement points: 0 of N".
    }
```

Write the body concretely (as in Task 10).

- [ ] **Step 2: Run; expect failure.**

- [ ] **Step 3: Implement.** `AttackTargets` passes `spent: bridge.Spent` to `Combat.Check`. `Odds` is unchanged (Preview already counts the unit alone for the player). `ToastPresenter` subscribes to `bridge.Committed` and shows the committed order's keyed lines (`engagement`, `destroyed`, `retreated`, `surrendered_*`, `fortify_lost`) through `UnitNames.Line` (Task 10a), one a line. Unsubscribe in `Dispose`. Remove the queued-attack line.

- [ ] **Step 4: Full EditMode, 0 NEW; WebGL golden and smoke lines unchanged; screenshots EN and HE of the toast after an attack.**

- [ ] **Step 5: Commit and open the U2 PR.**

### Task 12: Go-to on screen (U3; after S4)

**Files:**
- Modify: `unity/Assets/Scripts/Map/MapScene.cs:156-200` (new `ShowRoute`)
- Modify: `unity/Assets/Scripts/UI/InfoPanelPresenter.cs` (pick takes an unlit hex with a route; touch confirm)
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs` (hover sends the route; turn-start toasts)
- Modify: both strings files (`unit.move_pick` full text, `unit.route_turns`, `unit.go_to_given`; the turn-start lines are Task 10a's `why.line.*`)
- Test: `unity/Assets/Tests/EditMode/UnitOrdersTests.cs`

**Interfaces:**
- Consumes: `Movement.Route` (Task 7), `Movement.GoTo` (Task 8).
- Produces: `MapScene.ShowRoute(IReadOnlyList<(int q, int r, int turn)> route)` (draws the path and each turn's last hex's number; empty clears).

- [ ] **Step 1: Write the failing tests.**

```csharp
    /// <summary>R-MOVE-008 on screen: in Move's pick, a click on an unlit hex a route reaches gives the go-to; the card says
    /// how many turns, and the map shows the route's turn numbers while it is hovered.</summary>
    [Test]
    public void AClickBeyondTheLitHexesGivesAGoTo() { /* pick Move for Golani; choose the first land hex not in Reachable whose Route is non-empty; hover it: MapScene.ShowRoute got that Route; click it: bridge.State's unit has Order go_to and the card shows unit.route_turns */ }

    /// <summary>R-MOVE-009 on screen: at the next turn's start, a stopped route says so in the toast.</summary>
    [Test]
    public void AStoppedRouteSaysSoAtTheTurnsStart() { /* set a go-to toward a hex, place an enemy unit next to the unit's next stop through the state, end the turn, assert the toast holds why.line.stopped_enemy's English with the unit's name */ }
```

Write both bodies concretely.

- [ ] **Step 2: Run; expect failures.**

- [ ] **Step 3: Implement.** The pick keeps its lit set; a click on a hex outside it with `Movement.Route(...)` non-empty gives `new UnitOrder(unit, Movement.GoTo, q, r)` through `bridge.Queue`; on touch (`Input.touchSupported` and the click came from a touch), the first tap calls `ShowRoute` and records the hex, and a second tap on the same hex confirms. The toast at a turn's start shows the route step's keyed lines (`arrived`, `stopped_enemy`, `stopped_no_route`, `stopped_full`) from `bridge.Session.Now.Why` through `UnitNames.Line` (Task 10a). `ShowRoute` follows `ShowReachable`'s mesh pattern (`MapScene.cs:160-200`) and draws turn numbers as world-space labels through `MapLabelsPresenter`'s existing label path, not a new font.

- [ ] **Step 4: Full EditMode, 0 NEW; WebGL lines unchanged; screenshots EN and HE of a route with turn numbers.**

- [ ] **Step 5: Commit and open the U3 PR.**

### Task 13: End turn waits for orders (U4)

**Files:**
- Modify: `unity/Assets/Scripts/UI/SimBridge.cs` (skips, `NeedsOrders`)
- Modify: `unity/Assets/Scripts/UI/HudPresenter.cs:130-140`, `:470-485` (label, tooltip, the press)
- Modify: `unity/Assets/Scripts/UI/GameRoot.cs` (focus a unit: extract #1394's centring (`GameRoot.cs:480-492`) into `FocusUnit(int simId)`; Shift+Enter; the smoke loops `:791-867`)
- Modify: `unity/Assets/Scripts/UI/InfoPanelPresenter.cs` (Skip button, auto-cycle), `unity/Assets/UI/Game.uxml` (the `info-skip` button)
- Modify: `unity/Assets/Scripts/Map/Units/UnitLayer.cs` (`Dim`)
- Modify: both strings files (`unit.order.skip`, `hud.end_turn_needs_orders`, `hud.end_turn_force`, `how_to_play.end_turn`)
- Test: `unity/Assets/Tests/EditMode/HudTests.cs`, `HebrewUiTests.cs`, `UiLayoutTests.cs`, `SimSmokeTests.cs`, `UnitOrdersTests.cs`

**Interfaces:**
- Consumes: `Movement.NeedsOrders` (Task 2), `SimBridge.Spent` (Task 9).
- Produces: `SimBridge.NeedsOrders : IReadOnlyList<int>` (less skipped units), `SimBridge.Skip(int unit)`, `SimBridge.IsSkipped(int unit)`; `UnitLayer.Dim(IReadOnlyCollection<string> rowIds)`.

- [ ] **Step 1: Write the failing tests.**

```csharp
    // SimBridgeGddTests
    [Test]
    public void SkippedUnitsDoNotNeedOrdersAndTheSkipEndsWithTheTurn()
    {
        var bridge = new SimBridge(GameSession.New(Gdd, GoldenRun.Seed, Mode.Historical), Text);
        var first = bridge.NeedsOrders.First();
        bridge.Skip(first);
        Assert.IsFalse(bridge.NeedsOrders.Contains(first));
        bridge.EndTurn();
        Assert.IsFalse(bridge.IsSkipped(first));
    }

    // HudTests
    [Test]
    public void EndTurnSelectsTheNextUnitWhileOneNeedsOrders()
    {
        // Start a game (as the HUD tests do); press end-turn: the turn does not end (TurnStart.Turn unchanged), the button
        // reads hud.end_turn_needs_orders, and the selected token is the first of bridge.NeedsOrders. Skip every unit through
        // the card's info-skip button; the button reads hud.end_turn; press it: the turn ends.
    }

    [Test]
    public void ShiftEnterEndsTheTurnAnyway() { /* with units needing orders and nothing focused, send Shift+Enter (the InputSystem test fixture HudTests or NavigationGuard tests use); the turn ends once */ }

    [Test]
    public void TheCycleSkipsAUnitThatLeftTheMap() { /* remove the next unit through a committed Send home or a test state; press end-turn; the selected unit is the one after it; nothing throws */ }

    [Test]
    public void AStoppedRouteNeedsOrders() { /* a go-to that stops next to an enemy at the turn's start: the unit is in bridge.NeedsOrders */ }

    // UiLayoutTests
    [Test]
    public void TheBlockedEndTurnLabelFitsAtPhoneWidth() { /* at 390 x 844, English and Hebrew, with a unit needing orders: the end-turn label's text is not clipped (layout width >= preferred width) and the hourglass padding is kept */ }
```

Write each body concretely from the neighbouring tests in those files. In `HebrewUiTests.InHebrewEveryStaticTextIsTheHebrewString` (`HebrewUiTests.cs:347-366`), the `("end-turn", "hud.end_turn")` pair (`:284`) now expects `hud.end_turn_needs_orders` at the start (every unit needs orders on the first turn); keep the Latin-letter assertion: the label has none (Shift+Enter is only in the tooltip and How to play, which the test does not cover; `outcome.skip` is the precedent for a key name in Hebrew text).

- [ ] **Step 2: Run; expect failures.**

- [ ] **Step 3: Implement.**

```csharp
        // SimBridge
        readonly HashSet<int> skipped = new HashSet<int>();
        /// <summary>R-MOVE-010: the units End turn waits for, in ascending id, less those skipped this turn.</summary>
        public IReadOnlyList<int> NeedsOrders => Movement.NeedsOrders(Content, State, Spent).Where(id => !skipped.Contains(id)).ToList();
        /// <summary>Skips the unit for this turn: not an order, not saved, nothing in the game changes (spec 7.2).</summary>
        public void Skip(int unit) { if (skipped.Add(unit)) Notify(Changed); }
        public bool IsSkipped(int unit) => skipped.Contains(unit);
        // EndTurn and Replace: skipped.Clear();
```

`HudPresenter`: the button's text is `hud.end_turn_needs_orders` while `bridge.NeedsOrders.Count > 0`, with the tooltip `hud.end_turn_force`; the press calls a `selectNext` callback from GameRoot when units need orders and the Shift key is not held, else `bridge.EndTurn()` (so Shift+Enter on the focused button and Shift+click both force). `GameRoot.Update`: Shift+Enter (either Enter key) with no focused element and no overlay open (the same guards as the PageUp keys, `GameRoot.cs:772-781`) calls `bridge.EndTurn()`. `selectNext` picks the first id in `NeedsOrders` after the selected unit's (wrapping), finds its token (the sim-id mapping #1396 lands), selects it, opens its card and calls `FocusUnit`. After Skip, Fortify, Hold, Send home, an attack, or a move that leaves 0 points, the card calls `selectNext` when `NeedsOrders` is not empty. `UnitLayer.Dim` sets a grey `_BaseColor` property block on each listed token's `Badge` renderers and clears it on the others; GameRoot calls it after `unitLayer.Show` with the rows of the player's units that have 0 points left (`Movement.PointsLeft`). `Smoke` and `SmokeOrders`: before pressing End turn, while `bridge.NeedsOrders.Count > 0` (guard 200), press `end-turn` then `info-skip`; log the same lines as before.

- [ ] **Step 4: Full EditMode on the merged tree, 0 NEW.** WebGL: `golden&smoke=5` prints `SMOKE OK 5 turns 1948-10-15 bc2ab42b...` unchanged, and `smoke-orders` prints `SMOKE ORDERS OK 1948-06-15 9f224bd4...` unchanged (Skip changes nothing in the sim; if either moves, stop: something besides the path changed). Screenshots EN and HE: the blocked button, a dimmed token, the Skip button.

- [ ] **Step 5: Commit and open the U4 PR.**

### Task 14: The GDD amendments (D; after the owner approves spec 9.3)

**Files:**
- Modify: `docs/GDD.md:38`, `:42`, `:76`; `docs/gdd/military-combat.md:13`, `:15`, `:17`, `:18`; `docs/superpowers/specs/2026-10-12-gdd-v0.1-impact-and-plan.md:251`

- [ ] **Step 1:** Apply exactly the edits of spec 9.3 the owner approved, quoting the ruling and date beside each, in the files' existing style. Nothing the owner did not approve.
- [ ] **Step 2:** `bash scripts/check.sh origin/main` ends with "ALL CHECKS PASSED".
- [ ] **Step 3:** Commit and open the docs PR (`Refs #1398`).

## Self-review

- **Spec coverage:** 3.1 and 3.3 (Tasks 1, 4), 3.2 (Tasks 1, 9), 3.4 (Tasks 3, 4, 6, 9), 4.1 to 4.6 (Tasks 5, 6, 11), 5 (Tasks 1, 7), 6 (Tasks 7, 8, 12), 7 (Tasks 2, 13), 8.1 to 8.3 (Tasks 10 to 13), 8.4 (Tasks 4a, 10a), 9.1 (every task's golden step), 9.2 (each task's rule step), 9.3 (Task 14), 11 (the slice table and Tasks 6, 9, 10).
- **Placeholders:** the Unity test bodies in Tasks 10 to 13 and `FirstAttack` in Task 6 are stated as the assertions to write from named neighbouring tests, because their setup depends on the real starting map and the scene fixtures; each lists every assertion. No other step defers a decision.
- **Names:** `PreviewResult`, `Simulation.Preview`, `Simulation.IsInstant`, `Movement.PointsLeft`, `Movement.NeedsOrders`, `Movement.Route`, `Movement.GoTo`, `GameSession.Commit`, `Now`, `Committed`, `EndTurn`, `SaveGame.SetOpen`, `Open`, `Version`, `Combat.Strike`, `SimBridge.State`, `TurnStart`, `Spent`, `Committed`, `NeedsOrders`, `Skip`, `IsSkipped`, `MapScene.ShowRoute`, `UnitLayer.Dim`, `UnitRef`, `WhyKeys`, `UnitLines.Line`, `UnitNames.Of`, `UnitNames.Line` are used with the same signatures in every task.
