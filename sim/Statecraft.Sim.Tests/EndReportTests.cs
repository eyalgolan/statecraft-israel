using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The end report (GDD Phase 7 PR 7, D-H4; #810): a pure reading of the final state. It carries the outcome code, the
/// state's score, each objective with the player's date beside the historical date, the national parameters and the one
/// comparison with history Q-END-2 allows, the population against its CBS anchor. The score's parts wait for #809.</summary>
public class EndReportTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>A real GDD game on its last day, ended as the MVP ends, with the first objective completed a month early.</summary>
    static GameState Ended()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        var first = Real.Objectives[0];
        Assert.True(Objectives.Record(Real, s, Real.RulesAt(s.Date).Mvp!, first.Id, first.ReferenceDate.AddMonths(-1), new List<Cause>()));
        s.Date = Real.End;
        s.GameOver = GameOverCodes.MvpEnded;
        return s;
    }

    // R-END-005
    [Fact]
    public void TheReportCarriesTheOutcomeTheScoreAndEveryObjectivesDates()
    {
        var s = Ended();
        var r = EndReport.For(Real, s);
        Assert.Equal(GameOverCodes.MvpEnded, r.Outcome);
        Assert.Equal(s.Date, r.Date);
        Assert.True(s.Score > 0);
        Assert.Equal(s.Score, r.Score);   // the state's own total: no part is recomputed here (#809)
        // Every objective in content order, done or not, with its historical date; the player's date only when completed.
        Assert.Equal(Real.Objectives.Select(o => o.Id), r.Objectives.Select(o => o.Id));
        var first = Real.Objectives[0];
        var done = r.Objectives[0];
        Assert.Equal(first.ReferenceDate, done.Historical);
        Assert.Equal(s.Objectives[first.Id], done.Completed);
        Assert.Equal(1, done.MonthsFromHistory);   // Objectives.MonthsFromHistory: a month early
        Assert.Equal(first.Card, done.Card);
        Assert.All(r.Objectives.Skip(1), o => { Assert.Null(o.Completed); Assert.Null(o.MonthsFromHistory); });
        // The GDD's two national parameters, as the state holds them, with no historical value (Q-END-2).
        Assert.Equal(s.Indicators["morale"], r.SocialCohesion);
        Assert.Equal(s.Indicators["intl_standing"], r.InternationalSupport);
    }

    // R-END-005
    [Fact]
    public void ADefeatReportsItsCause()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        s.GameOver = GameOverCodes.DefeatEconomy;
        var r = EndReport.For(Real, s);
        Assert.Equal(GameOverCodes.DefeatEconomy, r.Outcome);
        Assert.Equal(s.Date, r.Date);
        Assert.Equal(0, r.Score);
        Assert.All(r.Objectives, o => Assert.Null(o.Completed));
    }

    /// <summary>Review focus 5: the report writes nothing, and two calls read the same.</summary>
    // R-END-005
    [Fact]
    public void TheReportIsPure()
    {
        var s = Ended();
        var hash = s.Hash();
        var a = EndReport.For(Real, s);
        var b = EndReport.For(Real, s);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(a.Outcome, b.Outcome);
        Assert.Equal(a.Date, b.Date);
        Assert.Equal(a.Score, b.Score);
        Assert.Equal(a.SocialCohesion, b.SocialCohesion);
        Assert.Equal(a.InternationalSupport, b.InternationalSupport);
        Assert.Equal(a.Population, b.Population);
        Assert.Same(a.PopulationHistory, b.PopulationHistory);
        Assert.Equal(a.Objectives.Select(o => (o.Id, o.Card, o.Historical, o.Completed, o.MonthsFromHistory)),
            b.Objectives.Select(o => (o.Id, o.Card, o.Historical, o.Completed, o.MonthsFromHistory)));
    }

    /// <summary>Q-END-2: the population is the one measure compared with history, against the latest CBS anchor on or before
    /// the end, and that anchor carries its source.</summary>
    // R-END-006
    [Fact]
    public void EveryHistoryLineHasASource()
    {
        var s = Ended();
        var r = EndReport.For(Real, s);
        Assert.Equal(s.TotalPopulation, r.Population);
        var anchor = r.PopulationHistory!;
        Assert.Equal(Real.PopulationAnchors.Where(a => a.Date <= s.Date).Max(a => a.Date), anchor.Date);
        Assert.NotEmpty(anchor.Sources);
        Assert.All(anchor.Sources, x => Assert.StartsWith("https://", x.Url));

        // Before the first anchor there is nothing to compare with, so no line.
        var early = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        Assert.True(early.Date < Real.PopulationAnchors.Min(a => a.Date));
        early.GameOver = GameOverCodes.DefeatEconomy;
        Assert.Null(EndReport.For(Real, early).PopulationHistory);
    }

    [Fact]
    public void AGameStillOnHasNoReport()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        Assert.Throws<InvalidOperationException>(() => EndReport.For(Real, s));
    }
}
