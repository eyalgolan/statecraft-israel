using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Occupy, the occupation timer and withdrawal (GDD Phase 6 PR 6, #798). The owner's rulings of 18 October 2026
/// (#783, batches 3 and 4): Q-OCC-1 (a), one unit stays on the hex, with Money upkeep per turn and no effect on residents;
/// Q-OCC-2 (a), withdrawal before the timer ends is allowed and becomes a Raid; Q-OCC-3 (a), one X for every place. X and
/// the upkeep are design values proposed on #798. The places are CaptureTests' fixture values, not history, and the
/// fixture's X is 3 turns and its upkeep IL 0.25m a turn.</summary>
public class OccupyTests
{
    static Content Load() => CaptureTests.Load(CaptureTests.AllTargets);
    static Unit Soldier(int id, int q, int r) => CaptureTests.Soldier(id, q, r);

    /// <summary>A new game with <paramref name="units"/> and town_a captured and awaiting the choice.</summary>
    static GameState Awaiting(Content c, params Unit[] units)
    {
        var s = CombatTests.With(c, units);
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        return s;
    }

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static ResolveCapture Occupy(string id = "town_a") => new ResolveCapture(id, SettlementControl.Occupy);

    /// <summary>The Money a state holds, the debt taken off: borrowing moves Money between the two and changes neither.</summary>
    static double Net(GameState s) => s.Treasury - s.Debt;

    // R-CTRL-011 (Q-OCC-1 (a)), R-CTRL-012: Occupy keeps the place under the player's control and starts the timer at X,
    // and this turn's step 10 already counts one turn off. Occupy moves no unit: the soldier and the halutz stay.
    [Fact]
    public void OccupyKeepsControlAndStartsTheTimer()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), new Unit(2, "halutz", 1, 0));
        var result = Step(c, s, Occupy());
        Assert.Empty(result.Rejected);
        var ctrl = result.State.Settlements["town_a"];
        Assert.Equal((Movement.PlayerSide, SettlementControl.Occupied, 2), (ctrl.Owner, ctrl.State, ctrl.Turns));
        Assert.Equal((1, 0), (result.State.Units[1].Q, result.State.Units[1].R));
        Assert.Equal((1, 0), (result.State.Units[2].Q, result.State.Units[2].R));
        Assert.Contains("town_a=owner=israel,state=occupied,since=1948-05-15,turns=2", result.State.Describe());
        Assert.Contains(result.Why, x => x.Target == "town_a" && x.Message.Contains("is occupied"));
    }

    // R-CTRL-011 (Q-OCC-1 (a), "one unit stays on the hex"): Occupy needs an Israeli military unit on the place's hex; a
    // halutz holds nothing (#796). Refused with a reason, the state unchanged and the Attention refunded.
    [Fact]
    public void OccupyNeedsAnIsraeliMilitaryUnitOnTheHex()
    {
        var c = Load();
        var s = Awaiting(c, new Unit(2, "halutz", 1, 0), Soldier(1, 2, 0));
        var before = s.Clone();
        var why = new List<Cause>();
        Assert.Contains("needs an Israeli military unit", SettlementControl.Resolve(c, s, "town_a", SettlementControl.Occupy, why));
        Assert.Empty(why);
        Assert.Equal(before.Describe(), s.Describe());
        Assert.Contains("needs an Israeli military unit", Assert.Single(Step(c, before, Occupy()).Rejected).Reason);
    }

    // R-CTRL-011 (Q-OCC-1 (a), "Money upkeep per turn"): every turn the occupation lasts, the waiting for EndOccupation
    // included, costs mvp.occupation_upkeep_per_turn, and nothing else: the turn ends as the twin's with no record, less
    // the upkeep.
    [Fact]
    public void OccupationCostsTheUpkeepEachTurn()
    {
        var c = Load();
        var s = Step(c, Awaiting(c, Soldier(1, 1, 0)), Occupy()).State;
        var twin = CombatTests.With(c, Soldier(1, 1, 0));
        twin.Date = s.Date;
        Assert.Equal(Net(Step(c, twin).State) - 0.25, Net(s), 9);   // the Occupy turn pays too
        for (int turn = 0; turn < 4; turn++)   // two more turns of the timer, then two waiting for EndOccupation
        {
            var next = Step(c, s).State;
            var other = Step(c, Without(s)).State;
            Assert.Equal(Net(other) - 0.25, Net(next), 9);
            Assert.Equal((other.Food, other.Arms, other.Score, other.Indicators.GetValueOrDefault("morale"), other.Indicators.GetValueOrDefault("intl_standing")),
                         (next.Food, next.Arms, next.Score, next.Indicators.GetValueOrDefault("morale"), next.Indicators.GetValueOrDefault("intl_standing")));
            s = next;
        }
        Assert.Equal(SettlementControl.AwaitingEnd, s.Settlements["town_a"].State);
    }

    /// <summary>A copy of <paramref name="s"/> with no settlement record: the twin a capture never happened in.</summary>
    static GameState Without(GameState s)
    {
        var t = s.Clone();
        t.Settlements.Clear();
        return t;
    }

    // R-CTRL-012 (Q-OCC-3 (a)): one X for every place. Step 10 counts each occupation down once a turn, and at 0 the place
    // awaits EndOccupation (PR 7) and the timer stops.
    [Fact]
    public void EveryPlaceAwaitsEndOccupationAfterXTurns()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Soldier(2, 4, 0));
        s.Settlements["town_c"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        var turns = new List<(int, int)>();
        var first = Step(c, s, Occupy("town_c"), Occupy());
        Assert.Empty(first.Rejected);
        s = first.State;
        turns.Add((s.Settlements["town_a"].Turns, s.Settlements["town_c"].Turns));
        for (int i = 0; i < 3; i++)
        {
            s = Step(c, s).State;
            turns.Add((s.Settlements["town_a"].Turns, s.Settlements["town_c"].Turns));
        }
        Assert.Equal(new[] { (2, 2), (1, 1), (0, 0), (0, 0) }, turns);
        Assert.All(s.Settlements.Values, x => Assert.Equal(SettlementControl.AwaitingEnd, x.State));
        Assert.Contains("town_a=owner=israel,state=awaiting_end,since=1948-05-15", s.Describe());
    }

    // R-CTRL-012, the resolver's order (D-A6): the timer runs in step 10, after step 9's arrivals. The occupation's last
    // soldier leaves this turn, and a brigade dated to this turn's end arrives on the place's hex in step 9 (R-UNIT-009), so
    // step 10 finds the hex held and counts down; run before step 9, it would find the hex empty and end the occupation.
    // The places tick in ordinal id order.
    [Fact]
    public void TheTimerCountsDownInStepTen()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Soldier(2, 4, 0));
        s.Settlements["town_c"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        var first = Step(c, s, Occupy("town_c"), Occupy());
        Assert.Equal(new[] { "town_a", "town_c" }, first.Why.Where(x => x.Message.Contains("turns of occupation left")).Select(x => x.Target));
        s = first.State;
        var next = Step(c, s).State;
        var id = c.StartingForces.Count;   // R-UNIT-009: a dated row's unit takes its row's index as id
        c.StartingForces.Add(new StartingForce { Row = "late", Type = "soldier", Side = Movement.PlayerSide, Strength = 2, Q = 1, R = 0, Arrives = next.Date });
        var result = Step(c, s, new Move(1, 2, 0));
        Assert.Equal((1, 0), (result.State.Units[id].Q, result.State.Units[id].R));
        var ctrl = result.State.Settlements["town_a"];
        Assert.Equal((SettlementControl.Occupied, 1), (ctrl.State, ctrl.Turns));
    }

    // R-CTRL-013 (Q-OCC-2 (a)): withdrawal before the timer ends is allowed and becomes a Raid. The occupation lasts while
    // an Israeli military unit is on the hex; when step 10 finds none (the player moved them out), the record goes, the
    // place is under enemy control again with its residents unchanged, no upkeep is paid that turn and nothing else
    // changes: the turn ends as the twin's that never captured it.
    [Fact]
    public void WithdrawalBeforeTheTimerEndsIsARaid()
    {
        var c = Load();
        var s = Step(c, Awaiting(c, Soldier(1, 1, 0)), Occupy()).State;
        var result = Step(c, s, new Move(1, 2, 0));
        Assert.Empty(result.Rejected);
        Assert.Empty(result.State.Settlements);
        Assert.Contains(result.Why, x => x.Target == "town_a" && x.Message.Contains("under enemy control again"));
        Assert.Contains(Movement.NoEntry(c, result.State), h => h == (1, 0));
        // Spec4.4 now captures a unit already on the target before orders. A never-captured twin must start outside.
        var twin = Without(s);
        Assert.Null(Movement.Move(c, twin, 1, 2, 0, new Dictionary<int, int>(), new List<Cause>()));
        Assert.Equal((2, 0), (result.State.Units[1].Q, result.State.Units[1].R));
        Assert.Equal(Step(c, twin).State.Describe(), result.State.Describe());
    }

    // R-CTRL-013: a halutz left on the hex holds nothing (#796), so the occupation still ends.
    [Fact]
    public void AHalutzLeftBehindDoesNotKeepTheOccupation()
    {
        var c = Load();
        var s = Step(c, Awaiting(c, Soldier(1, 1, 0), new Unit(2, "halutz", 1, 0)), Occupy()).State;
        Assert.Empty(Step(c, s, new Move(1, 2, 0)).State.Settlements);
    }

    // R-CTRL-003, R-CTRL-011 (Q-OCC-1 (a), "no effect on residents"): Occupy and the occupation change the record's state
    // and timer and nothing else in it: the residents stay inhabited, and the combatants, buildings, owner and the capture
    // date stay as they were. Occupy itself changes nothing outside the record.
    [Fact]
    public void OccupationTouchesResidentsOnlyAsRuled()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var captured = s.Settlements["town_a"];
        var resolved = s.Clone();
        Assert.Null(SettlementControl.Resolve(c, resolved, "town_a", SettlementControl.Occupy, new List<Cause>()));
        Assert.Equal(Without(s).Describe(), Without(resolved).Describe());
        for (int i = 0; i < 5; i++)
        {
            s = Step(c, s, i == 0 ? new Command[] { Occupy() } : Array.Empty<Command>()).State;
            var x = s.Settlements["town_a"];
            Assert.Equal((captured.Owner, captured.Since, captured.Depopulated, captured.Combatants, captured.Buildings),
                         (x.Owner, x.Since, x.Depopulated, x.Combatants, x.Buildings));
        }
    }

    // R-CTRL-007, R-CTRL-008: Occupy is built, so the options helper offers it with a preview, which is all zeros: Occupy
    // costs nothing at once (the upkeep is paid in step 10). Deport is still refused as not built yet.
    [Fact]
    public void OccupyIsOfferedWithItsPreview()
    {
        var c = Load();
        var options = SettlementControl.CaptureOptions(c, Awaiting(c, Soldier(1, 1, 0)), "town_a");
        var occupy = options.Single(o => o.Outcome == SettlementControl.Occupy);
        Assert.Null(occupy.Refusal);
        var p = occupy.Preview!;
        Assert.Equal((0.0, 0.0, 0.0, 0, 0, 0.0, 0.0, 0), (p.Attention, p.Money, p.Debt, p.Food, p.Arms, p.SocialCohesion, p.InternationalSupport, p.Score));
        Assert.Contains("not built yet", options.Single(o => o.Outcome == SettlementControl.Deport).Refusal);
    }

    // R-CTRL-007: an occupied place is no longer awaiting the capture choice, so a second ResolveCapture is refused.
    [Fact]
    public void AnOccupiedPlaceIsNotAnsweredAgain()
    {
        var c = Load();
        var s = Step(c, Awaiting(c, Soldier(1, 1, 0)), Occupy()).State;
        Assert.Contains("not awaiting a decision", Assert.Single(Step(c, s, new ResolveCapture("town_a", SettlementControl.Raid)).Rejected).Reason);
    }

    // Determinism: Occupy and the timer draw nothing from the RNG.
    [Fact]
    public void OccupationDrawsNothing()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var rng = s.RngState;
        Assert.Null(SettlementControl.Resolve(c, s, "town_a", SettlementControl.Occupy, new List<Cause>()));
        SettlementControl.Tick(c, s, c.RulesAt(s.Date).Mvp!, new List<Cause>());
        Assert.Equal(rng, s.RngState);
    }

    // The mvp-occupy-cost golden (#798) pins the real content's run: the scenario must really occupy Nazareth, and the run
    // must end the turn its timer does, or the golden would pin a no-op.
    [Fact]
    public void TheOccupyCostScenarioOccupiesNazarethToTheTimersEnd()
    {
        var real = ContentLoader.Load(RealContentTests.ContentRoot());
        var sc = Scenario.Parse(System.IO.File.ReadAllText(System.IO.Path.Combine(ScenarioTests.Dir(), "mvp-occupy-cost.yaml")));
        var s = sc.Play(real);
        Assert.Equal(new DateTime(1948, 12, 15), s.Date);
        var ctrl = s.Settlements["nazareth"];
        Assert.Equal((Movement.PlayerSide, SettlementControl.AwaitingEnd, 0, new DateTime(1948, 6, 15)), (ctrl.Owner, ctrl.State, ctrl.Turns, ctrl.Since));
    }

    [Theory]
    [InlineData("occupation_turns: 3", "", "mvp.occupation_turns", "required")]
    [InlineData("occupation_turns: 3", "occupation_turns: 0", "mvp.occupation_turns", "must be >= 1")]
    [InlineData("occupation_upkeep_per_turn: 0.25", "", "mvp.occupation_upkeep_per_turn", "required")]
    [InlineData("occupation_upkeep_per_turn: 0.25", "occupation_upkeep_per_turn: -1", "mvp.occupation_upkeep_per_turn", "must be a finite number >= 0")]
    public void BadValuesFailNamingTheField(string from, string to, string field, string message)
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace(from, to);
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(("rules.yaml", field), (ex.File, ex.Field));
        Assert.Contains(message, ex.Message);
    }
}
