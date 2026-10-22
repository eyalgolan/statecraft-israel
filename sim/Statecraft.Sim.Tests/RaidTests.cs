using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>ResolveCapture and Raid and withdraw (GDD Phase 6 PR 5, #797). The owner's rulings of 18 October 2026 (#783,
/// batch 3): Q-RAID-1 (a), the player's units leave and the place returns to enemy control at once with its residents
/// unchanged; Q-HIST-1 (a), a capture the player does not answer waits and the units stay. The owner's ruling of 18 October
/// 2026 on #797, "Counts as Raid": a capture still waiting at the MVP's end makes the units leave, changes nothing for the
/// residents and adds nothing to the score. Where the units go is the default proposed on #797: each steps to the first
/// free neighbouring hex in the fixed order. The places are CaptureTests' fixture values, not history: town_a sits on
/// (1, 0) of the 6 x 3 board, whose neighbours on the board are (2, 0), (0, 0), (0, 1) and (1, 1), in that order.</summary>
public class RaidTests
{
    static Content Load() => CaptureTests.Load(CaptureTests.AllTargets);
    static Unit Soldier(int id, int q, int r) => CaptureTests.Soldier(id, q, r);
    static Unit Enemy(int id, int q, int r) => CaptureTests.Enemy(id, q, r);

    /// <summary>A new game with <paramref name="units"/> and town_a captured and awaiting the choice.</summary>
    static GameState Awaiting(Content c, params Unit[] units)
    {
        var s = CombatTests.With(c, units);
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, SettlementControl.AwaitingChoice, s.Date);
        return s;
    }

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static ResolveCapture Raid(string id = "town_a") => new ResolveCapture(id, SettlementControl.Raid);

    // R-CTRL-010 (Q-RAID-1 (a)): the player's units leave, the soldier and a halutz alike, each to the first free
    // neighbouring hex, and the place's record goes at once: it is under enemy control again, so the turn's capture check
    // finds nobody there and nothing to take.
    [Fact]
    public void RaidWithdrawsThePlayersUnitsAndReturnsThePlaceToEnemyControl()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), new Unit(2, "halutz", 1, 0));
        var result = Step(c, s, Raid());
        Assert.Empty(result.Rejected);
        Assert.Empty(result.State.Settlements);
        Assert.Equal((2, 0), (result.State.Units[1].Q, result.State.Units[1].R));
        Assert.Equal((2, 0), (result.State.Units[2].Q, result.State.Units[2].R));
        Assert.Contains(result.Why, x => x.Target == "town_a" && x.Message.Contains("under enemy control again"));
        Assert.Contains(Movement.NoEntry(c, result.State), h => h == (1, 0));   // the player may not walk back in (R-MOVE-005)
    }

    // R-CTRL-010 (Q-RAID-1 (a), "residents unchanged"): Raid changes the units' hexes and removes the record, and nothing
    // else: the turn ends exactly as it does for a twin whose units were moved by hand and whose record was never written.
    [Fact]
    public void RaidChangesOnlyTheUnitsHexesAndTheRecord()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var twin = CombatTests.With(c, Soldier(1, 2, 0));
        Assert.Equal(Step(c, twin).State.Describe(), Step(c, s, Raid()).State.Describe());
    }

    // R-CTRL-010: a neighbour with an enemy unit, or one already full under the stack limit (the fixture's 2), is passed
    // over for the next in the fixed order.
    [Fact]
    public void AUnitSkipsAHexWithAnEnemyOrNoRoom()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Soldier(2, 1, 0), Enemy(3, 2, 0), Soldier(4, 0, 0));
        var next = Step(c, s, Raid()).State;
        Assert.Equal((0, 0), (next.Units[1].Q, next.Units[1].R));   // (2, 0) holds an enemy unit
        Assert.Equal((0, 1), (next.Units[2].Q, next.Units[2].R));   // (0, 0) now holds two Israeli units
        Assert.Empty(next.Settlements);
    }

    // R-CTRL-010, R-CTRL-007: when a unit has no free hex to withdraw to, Raid is refused with a reason, the Attention is
    // refunded and the state is unchanged, a unit that had already found a hex included, so the choice waits. Here unit 1
    // fits on (2, 0) beside unit 7, which leaves no room there for unit 6, and enemy units hold the other three neighbours.
    [Fact]
    public void RaidWithNowhereToGoIsRefused()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Soldier(6, 1, 0), Soldier(7, 2, 0), Enemy(3, 0, 0), Enemy(4, 0, 1), Enemy(5, 1, 1));
        var before = s.Clone();
        var why = new List<Cause>();
        Assert.Contains("Unit 6 has no free hex", SettlementControl.Resolve(c, s, "town_a", SettlementControl.Raid, why));
        Assert.Empty(why);
        Assert.Equal(before.Describe(), s.Describe());
        Assert.Single(Step(c, before, Raid()).Rejected);
    }

    // R-CTRL-007: ResolveCapture is valid only for a settlement awaiting the choice, and only for an outcome that is built;
    // otherwise it is refused with a reason and the state is unchanged.
    [Theory]
    [InlineData("town_c", "raid", "not awaiting a decision")]   // a target with no record
    [InlineData("nowhere", "raid", "not awaiting a decision")]   // not a settlement
    [InlineData("town_a", "surrender", "not known")]
    [InlineData("town_a", "occupy", "not built yet")]
    [InlineData("town_a", "deport", "not built yet")]
    public void ResolveCaptureIsRefusedWithAReason(string id, string outcome, string reason)
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var before = s.Clone();
        var why = new List<Cause>();
        Assert.Contains(reason, SettlementControl.Resolve(c, s, id, outcome, why));
        Assert.Empty(why);
        Assert.Equal(before.Describe(), s.Describe());
        Assert.Contains(reason, Assert.Single(Step(c, before, new ResolveCapture(id, outcome)).Rejected).Reason);
    }

    // R-CTRL-007: a record that is not awaiting the choice (a later state, PR 6) is refused too.
    [Fact]
    public void ARecordInAnotherStateIsRefused()
    {
        var c = Load();
        var s = CombatTests.With(c, Soldier(1, 1, 0));
        s.Settlements["town_a"] = new SettlementControl(Movement.PlayerSide, "occupied", s.Date);
        Assert.Contains("not awaiting a decision", SettlementControl.Resolve(c, s, "town_a", SettlementControl.Raid, new List<Cause>()));
    }

    // R-CTRL-008 (Q-HIST-1 (a)): before the MVP's end, a capture the player does not answer waits and the units stay.
    [Fact]
    public void AnUnansweredCaptureWaits()
    {
        var c = Load();
        var next = Step(c, Awaiting(c, Soldier(1, 1, 0))).State;
        Assert.Equal(SettlementControl.AwaitingChoice, next.Settlements["town_a"].State);
        Assert.Equal((1, 0), (next.Units[1].Q, next.Units[1].R));
    }

    // R-CTRL-008 (#797, "Counts as Raid"): on the turn that reaches the game's end, a capture still waiting resolves as
    // Raid: the units leave and the record goes, and the end state, score included, is the twin's that raided by hand.
    [Fact]
    public void ACaptureStillWaitingAtTheEndCountsAsRaid()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var twin = CombatTests.With(c, Soldier(1, 2, 0));
        s.Date = twin.Date = c.End.AddDays(-10);   // the next turn ends on the game's end
        var result = Step(c, s);
        Assert.NotNull(result.State.GameOver);
        Assert.Empty(result.State.Settlements);
        Assert.Equal((2, 0), (result.State.Units[1].Q, result.State.Units[1].R));
        Assert.Contains(result.Why, x => x.Target == "town_a" && x.Message.Contains("counts as a raid"));
        var other = Step(c, twin).State;
        Assert.Equal(other.Score, result.State.Score);
        Assert.Equal(other.Describe(), result.State.Describe());
    }

    // R-CTRL-008: at the end, a unit with nowhere to go stays while the others leave, and the record still goes (the game
    // is over). Unit 1 fits on (2, 0) beside unit 7; unit 6 then has no room, and enemy units hold the other neighbours.
    [Fact]
    public void AtTheEndTheRecordGoesEvenWithNowhereToWithdraw()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Soldier(6, 1, 0), Soldier(7, 2, 0), Enemy(3, 0, 0), Enemy(4, 0, 1), Enemy(5, 1, 1));
        s.Date = c.End.AddDays(-10);   // the next turn ends on the game's end
        var next = Step(c, s).State;
        Assert.NotNull(next.GameOver);
        Assert.Empty(next.Settlements);
        Assert.Equal((2, 0), (next.Units[1].Q, next.Units[1].R));
        Assert.Equal((1, 0), (next.Units[6].Q, next.Units[6].R));
    }

    // R-CTRL-010: a unit keeps its orders when it withdraws, as after a Move; a fortify names the place's hex, so the
    // turn's order step drops it (R-ORDER-002), and hold goes with the unit.
    [Fact]
    public void AFortifyIsLostAndHoldKeptOnWithdrawal()
    {
        var c = Load();
        var fortified = new Unit(1, "soldier", 1, 0, side: "israel", strength: 2, order: Orders.Fortify, orderTarget: "1,0");
        var holding = new Unit(2, "soldier", 1, 0, side: "israel", strength: 2, order: Orders.Hold);
        var next = Step(c, Awaiting(c, fortified, holding), Raid()).State;
        Assert.Equal("", next.Units[1].Order);
        Assert.Equal(Orders.Hold, next.Units[2].Order);
    }

    // R-CTRL-009: HistoricalAI leaves out a Raid the sim would refuse, so its orders are never rejected and the capture waits.
    [Fact]
    public void HistoricalAIWaitsWhenRaidIsRefused()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0), Enemy(2, 2, 0), Enemy(3, 0, 0), Enemy(4, 0, 1), Enemy(5, 1, 1));
        Assert.DoesNotContain(HistoricalAI.Commands(c, s), x => x is ResolveCapture);
    }

    // R-CTRL-010, determinism: Raid draws nothing from the RNG.
    [Fact]
    public void RaidDrawsNothing()
    {
        var c = Load();
        var s = Awaiting(c, Soldier(1, 1, 0));
        var rng = s.RngState;
        Assert.Null(SettlementControl.Resolve(c, s, "town_a", SettlementControl.Raid, new List<Cause>()));
        Assert.Equal(rng, s.RngState);
    }
}
