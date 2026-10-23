using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Statecraft.Sim;

/// <summary>GDD Phase 7 PR 6 (#809), slice 1: the final score is the sum of the parts Q-SCORE-1 names (R-SCORE-001), and two
/// objective cards on one turn fire in objectives.yaml's order (Q-CARD-2, R-OBJ-007). The war objective's card waits for #752.</summary>
public class ObjectiveCardsTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    static void AssertSums(GameState s)
    {
        var p = ScoreParts.For(s);
        Assert.Equal(s.Score, p.ObjectivePoints + p.TimingBonuses + p.PrisonerPoints);
    }

    // R-SCORE-001: over the mvp-end run (which completes no objective) and over the one scenario that completes one, Score is
    // exactly its parts. A month early adds its bonus to the bonus part, not to the objective's base.
    [Fact]
    public void TheScoreIsItsParts()
    {
        AssertSums(GoldenRun.PlayToEnd(Real));

        var absorb = Scenario.Parse(File.ReadAllText(Path.Combine(ScenarioTests.Dir(), "mvp-absorb-first.yaml"))).Play(Real);
        Assert.True(absorb.Score > 0, "precondition: the scenario completes an objective");
        AssertSums(absorb);
        Assert.Equal(absorb.Score, ScoreParts.For(absorb).ObjectivePoints);

        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var o = Real.Objectives.Single(x => x.Id == "absorb_immigrants");
        var r = Real.RulesAt(s.Date).Mvp!;
        Assert.True(Objectives.Record(Real, s, r, o.Id, o.ReferenceDate.AddMonths(-1), new List<Cause>()));
        var parts = ScoreParts.For(s);
        Assert.Equal((r.ObjectivePoints!.Value, r.ObjectiveBonusPerMonth!.Value, 0),
            (parts.ObjectivePoints, parts.TimingBonuses, parts.PrisonerPoints));
        AssertSums(s);
    }

    // R-SCORE-001 (D-B2, D-B6): completing every objective on its historical date scores objective_points once per objective
    // (the infrastructure parts are one objective, R-OBJ-006) and no timing bonus.
    [Fact]
    public void HistoricalDatesScoreNoBonus()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var r = Real.RulesAt(s.Date).Mvp!;
        foreach (var o in Real.Objectives)
            Assert.True(Objectives.Record(Real, s, r, o.Id, o.ReferenceDate, new List<Cause>()));
        var objectives = Real.Objectives.Select(o => o.PartOf == "" ? o.Id : o.PartOf).Distinct().Count();
        var parts = ScoreParts.For(s);
        Assert.Equal(objectives * r.ObjectivePoints!.Value, parts.ObjectivePoints);
        Assert.Equal(0, parts.TimingBonuses);
        AssertSums(s);
    }

    // R-OBJ-007 (Q-CARD-2): two objectives completed on one turn fire their cards in objectives.yaml's order, which is neither
    // the objectives' id order nor the cards' id order here.
    [Fact]
    public void TwoCardsInOneTurnFireInOrder()
    {
        Assert.Equal(new[] { "railway", "burma_road", "yishuvim", "absorb_immigrants" }, Real.Objectives.Select(o => o.Id));
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s.Objectives["absorb_immigrants"] = s.Date;
        s.Objectives["railway"] = s.Date;
        EventSystem.Trigger(Real, s, new List<Cause>());
        var cards = s.PendingEvents.Where(e => e == "railway_complete" || e == "absorb_immigrants").ToList();
        Assert.Equal(new[] { "railway_complete", "absorb_immigrants" }, cards);
    }
}
