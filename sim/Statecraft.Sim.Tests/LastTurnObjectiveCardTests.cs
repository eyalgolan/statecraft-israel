using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#2536: an objective completed on the turn that ends the game still shows its card (R-OBJ-003, R-LOOP-004; the
/// owner's choice "Show the card (Recommended)", 4 November 2026). No other event fires on that turn, and the score is the
/// one step 11 records.</summary>
public class LastTurnObjectiveCardTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The mvp-end run's state one turn before the game's end (GoldenRun.Orders, seed 1948, Historical mode).</summary>
    static GameState BeforeTheEnd()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        for (var i = 0; i < 1000; i++)
        {
            var next = Simulation.Step(Real, s, GoldenRun.Orders(Real, s)).State;
            if (next.GameOver != null) return s;
            s = next;
        }
        throw new InvalidOperationException("the run never ended");
    }

    /// <summary>Every segment of the Haifa to Tel Aviv rail link built, so step 11 records the railway objective (R-OBJ-005)
    /// at the end of the next turn. The mvp-end run builds none of it.</summary>
    static void BuildRailway(GameState s)
    {
        var def = Real.Corridors.Single(x => x.Id == "tlv_haifa_rail");
        s.Corridors["tlv_haifa_rail"] = new CorridorWork(def.Id, def.Segments.Select(_ => new SegmentWork(null, 0, s.Date)).ToArray());
    }

    static int Points(StepResult r, string objective) =>
        (int)r.Why.Single(w => w.Key == CauseKeys.ObjectiveCompleted && w.Target == objective).Delta;

    // R-OBJ-003, R-LOOP-004: the objective completed on the final turn (the one ending 15 January 1952) shows its card on
    // that turn, and only its card: an event otherwise eligible then stays unfired. The score is step 11's.
    [Fact]
    public void AnObjectiveCompletedOnTheLastTurnShowsItsCard()
    {
        var before = BeforeTheEnd();
        Assert.False(before.Objectives.ContainsKey("railway"), "precondition: the mvp-end run does not complete the railway");
        BuildRailway(before);
        // The road done earlier (its card shown then), so the railway is the infrastructure objective's last part and pays
        // its base (R-OBJ-006): the run has already recorded the yishuvim.
        Assert.True(Objectives.Record(Real, before, Real.RulesAt(before.Date).Mvp!, "burma_road", before.Date, new List<Cause>()));
        before.FiredEvents.Add("burma_road_complete");
        // An event open on the last turn that nothing else holds back, made unfired so it would be eligible then.
        var other = Real.Events.First(e => e.Objective == "" && e.Site == "" && e.Prisoners == "" && e.Deported == ""
            && e.Requires.Count == 0 && e.Conditions.Count == 0 && e.From <= before.Date && (e.To == null || e.To.Value > before.Date));
        before.FiredEvents.Remove(other.Id);

        var last = Simulation.Step(Real, before, Array.Empty<Command>());
        var s = last.State;
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.Equal(new DateTime(1952, 1, 15), s.Date);
        Assert.Equal(new DateTime(1952, 1, 15), s.Objectives["railway"]);
        Assert.Equal(new[] { "railway_complete" }, s.PendingEvents);
        Assert.Equal(new[] { "railway_complete" }, s.FiredEvents.Except(before.FiredEvents));
        Assert.DoesNotContain(other.Id, s.FiredEvents);
        Assert.Contains(last.Why, w => w.Key == CauseKeys.EventNew && w.Target == "railway_complete");
        Assert.True(Points(last, "railway") > 0, "the objective scores");
        Assert.Equal(before.Score + Points(last, "railway"), s.Score);
        Assert.Equal(s.Score, EndReport.For(Real, s).Score);
    }

    // R-LOOP-004: a game ending with no objective completed on its last turn still raises no event on that turn.
    [Fact]
    public void TheLastTurnRaisesNoOtherEvent()
    {
        var before = BeforeTheEnd();
        var other = Real.Events.First(e => e.Objective == "" && e.Site == "" && e.Prisoners == "" && e.Deported == ""
            && e.Requires.Count == 0 && e.Conditions.Count == 0 && e.From <= before.Date && (e.To == null || e.To.Value > before.Date));
        before.FiredEvents.Remove(other.Id);
        var s = Simulation.Step(Real, before, Array.Empty<Command>()).State;
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.Empty(s.PendingEvents);
        Assert.DoesNotContain(other.Id, s.FiredEvents);
    }

    // R-OBJ-003, R-LOOP-004: the same on a turn the economic defeat ends (R-END-002): the card shows, nothing else fires.
    [Fact]
    public void AnObjectiveCompletedOnAnEconomicDefeatTurnShowsItsCard()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s = Simulation.Step(Real, s, GoldenRun.Orders(Real, s)).State;
        s.PendingEvents.Clear();
        var r = Real.RulesAt(s.Date).Mvp!;
        s.Debt = r.DebtCeiling!.Value * 100;
        s.Food = 0;
        s.FxReserves = -1e12;
        s.ActiveSituations.Add(EconomySystem.FxCrisis);
        s.ExhaustedTurns = r.DefeatTurns!.Value - 1;
        BuildRailway(s);
        var before = s.Clone();

        var last = Simulation.Step(Real, before, Array.Empty<Command>());
        Assert.Equal(GameOverCodes.DefeatEconomy, last.State.GameOver);
        Assert.True(last.State.Objectives.ContainsKey("railway"));
        Assert.Equal(new[] { "railway_complete" }, last.State.PendingEvents);
        Assert.Equal(new[] { "railway_complete" }, last.State.FiredEvents.Except(before.FiredEvents));
        Assert.Equal(before.Score + Points(last, "railway"), last.State.Score);
    }
}
