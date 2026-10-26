using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Residents flee at capture (#799; OWNER QUESTION 2 (b), owner, 24 October 2026: "2.b"). Capturing a target whose
/// row says <c>residents: fled</c> records the flight at once, the record of it outlasts every return to enemy control, and
/// the place can be captured again. A stayed target is captured as before. Flight is not Deport: it changes nothing but the
/// record. The places are CaptureTests' fixture values, not history; town_a is on hex 1,0.</summary>
public class FledAtCaptureTests
{
    static Content Fled() => CaptureTests.LoadFled(new[] { "town_a" }, CaptureTests.AllTargets);
    static Unit Soldier(int id, int q, int r) => CaptureTests.Soldier(id, q, r);

    /// <summary>The state with only the settlement record and the cause lines left out, to compare what else moved.</summary>
    static string WithoutRecord(GameState s)
    {
        var x = s.Clone();
        x.Settlements.Clear();
        return x.Describe();
    }

    // R-CTRL-005 (OWNER QUESTION 2 (b)): a fled target's capture writes fled=1 at once and changes nothing else: no
    // displaced count, indicator, Money, city, score or RNG draw.
    [Fact]
    public void AFledTownIsRecordedAsFledAtCapture()
    {
        var c = Fled();
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        var twin = CaptureTests.Load(CaptureTests.AllTargets);   // the same board with town_a stayed
        var result = Simulation.Step(c, s, Array.Empty<Command>());
        var stayed = Simulation.Step(twin, CombatTests.With(twin, Soldier(1, 1, 0)), Array.Empty<Command>());
        var ctrl = result.State.Settlements["town_a"];
        Assert.Equal((Movement.PlayerSide, SettlementControl.AwaitingChoice, result.State.Date, true), (ctrl.Owner, ctrl.State, ctrl.Since, ctrl.Fled));
        Assert.Contains("town_a=owner=israel,state=awaiting_choice,since=" + result.State.Date.ToString("yyyy-MM-dd") + ",fled=1", result.State.Describe());
        Assert.Equal(0, result.State.DisplacedByPlayer);
        Assert.Equal(WithoutRecord(stayed.State), WithoutRecord(result.State));
        Assert.Equal(stayed.State.RngState, result.State.RngState);
        Assert.Contains(result.Why, x => x.Target == "town_a" && x.Message.Contains("Its residents fled"));
    }

    // R-CTRL-005: a stayed target is captured as before, with no fled field and no line about residents.
    [Fact]
    public void AStayedTownIsCapturedAsToday()
    {
        var c = CaptureTests.Load(CaptureTests.AllTargets);
        var result = Simulation.Step(c, CombatTests.With(c, Soldier(1, 1, 0)), Array.Empty<Command>());
        Assert.False(result.State.Settlements["town_a"].Fled);
        Assert.DoesNotContain("fled", result.State.Describe());
        Assert.DoesNotContain(result.Why, x => x.Message.Contains("fled"));
    }

    // R-CTRL-010 (OWNER QUESTION 2 (b): "the record of the flight stays after a Raid"): a capture answered with Raid in the
    // same turn leaves an ownerless fled record with the turn's date (Stamp dates it), the units gone, and the line does
    // not say the residents are unchanged.
    [Fact]
    public void RaidOfAFledTownKeepsTheFlight()
    {
        var c = Fled();
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        var result = Simulation.Step(c, s, new Command[] { new ResolveCapture("town_a", SettlementControl.Raid) });
        Assert.Empty(result.Rejected);
        Assert.Contains("town_a=since=" + result.State.Date.ToString("yyyy-MM-dd") + ",fled=1\n", result.State.Describe());
        Assert.NotEqual((1, 0), (result.State.Units[1].Q, result.State.Units[1].R));
        Assert.DoesNotContain(result.Why, x => x.Message.Contains("unchanged"));
        Assert.Contains(result.Why, x => x.Target == "town_a" && x.Message.Contains("Its residents fled when it was captured"));
    }

    public static IEnumerable<object[]> Returns() => new[] { new object[] { "timer" }, new object[] { "end" }, new object[] { "enemy" } };

    // R-CTRL-008, R-CTRL-013, R-CTRL-004: the timer's raid with no unit left, the end-of-game raid and enemy units retaking
    // an awaiting capture each leave the ownerless fled record with its capture date.
    [Theory]
    [MemberData(nameof(Returns))]
    public void TheFlightOutlastsEveryReturnToEnemyControl(string how)
    {
        var c = Fled();
        var s = CombatTests.With(c, how == "enemy" ? CaptureTests.Enemy(1, 1, 0) : Soldier(1, 2, 0));
        var since = s.Date;
        var why = new List<Cause>();
        if (how == "timer")
        {
            s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.Occupied, since, turns: 2, fled: true);
            s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        }
        else if (how == "end")
        {
            s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, since, fled: true);
            s.Date = c.End.AddDays(-10);   // the next turn ends on the game's end
            since = s.Settlements["town_a"].Since;
            s = Simulation.Step(c, s, Array.Empty<Command>()).State;
            Assert.NotNull(s.GameOver);
        }
        else
        {
            s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, since, fled: true);
            SettlementControl.Capture(c, s, s.Date.AddDays(7), why);
            Assert.Contains(why, x => x.Target == "town_a" && x.Message.Contains("Its residents fled when it was captured"));
        }
        var x = s.Settlements["town_a"];
        Assert.Equal(("", "", since, true, false), (x.Owner, x.State, x.Since, x.Fled, x.Depopulated));
    }

    // R-CTRL-004: an ownerless fled record is no record for the capture check, so the town can be captured again; the new
    // record keeps fled=1 and takes the new turn's date.
    [Fact]
    public void AFledTownCanBeCapturedAgain()
    {
        var c = Fled();
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(since: s.Date.AddDays(-30), fled: true);
        var next = Simulation.Step(c, s, Array.Empty<Command>()).State;
        var x = next.Settlements["town_a"];
        Assert.Equal((Movement.PlayerSide, SettlementControl.AwaitingChoice, next.Date, true), (x.Owner, x.State, x.Since, x.Fled));
    }

    // R-CTRL-011, R-CTRL-012: Occupy and the timer carry the flight on every record, and Occupy's line says so.
    [Fact]
    public void OccupyAndTheTimerCarryTheFlight()
    {
        var c = Fled();
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date, fled: true);
        var first = Simulation.Step(c, s, new Command[] { new ResolveCapture("town_a", SettlementControl.Occupy) });
        Assert.Contains(first.Why, x => x.Target == "town_a" && x.Message.Contains("is occupied") && x.Message.Contains("Its residents fled when it was captured"));
        s = first.State;
        for (int i = 0; i < 4; i++)
        {
            Assert.True(s.Settlements["town_a"].Fled);
            s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        }
        Assert.Equal((SettlementControl.AwaitingEnd, true), (s.Settlements["town_a"].State, s.Settlements["town_a"].Fled));
    }
}
