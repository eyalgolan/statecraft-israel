using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The end report (GDD Phase 7 PR 7, D-H4; #810): a pure reading of the final state. It carries the outcome code, the
/// state's score, each objective with the player's date beside the historical date, the national parameters and the one
/// comparison with history Q-END-2 allows, the population against its CBS anchor. The score's parts are #809's (ScoreParts).</summary>
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
        // The parts are ScoreParts.For's reading of the same state (R-SCORE-001), so they sum to the score.
        Assert.Equal(ScoreParts.For(s).ObjectivePoints, r.ScoreParts.ObjectivePoints);
        Assert.Equal(ScoreParts.For(s).TimingBonuses, r.ScoreParts.TimingBonuses);
        Assert.Equal(ScoreParts.For(s).PrisonerPoints, r.ScoreParts.PrisonerPoints);
        Assert.Equal(r.Score, r.ScoreParts.ObjectivePoints + r.ScoreParts.TimingBonuses + r.ScoreParts.PrisonerPoints);
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

    // Q-SCORE-2, R-OBJ-006: the score of a run that completes every objective on its historical date, read from content.
    [Fact]
    public void TheReferenceScoreIsWhatCompletingEveryObjectiveOnItsHistoricalDateScores()
    {
        var s = Ended();
        Assert.Equal(200, EndReport.For(Real, s).ReferenceScore);   // 100 each: the three infrastructure parts count once, plus absorption

        // The same number by playing it: every objective recorded on its reference date, no timing bonus on time.
        var perfect = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        foreach (var o in Real.Objectives)
            Assert.True(Objectives.Record(Real, perfect, Real.RulesAt(perfect.Date).Mvp!, o.Id, o.ReferenceDate, new List<Cause>()));
        perfect.GameOver = GameOverCodes.MvpEnded;
        Assert.Equal(perfect.Score, EndReport.For(Real, perfect).ReferenceScore);
    }

    // Q-SCORE-2 across an era override: each paying objective takes objective_points from the rules of the turn that ends on its own
    // reference date, as scoring would. The real content split at 1950 with a later era whose rules pay 1: absorption (20 December
    // 1949) pays in the founding era's 100, the infrastructure group with its latest part (yishuvim, 15 December 1950) in the later
    // era. The game ends in the later era. The split is set on the loaded content: the real rules.yaml dates a line past 1950, which
    // the loader would refuse in a founding era that short, and only RulesAt matters here.
    [Fact]
    public void TheReferenceScoreReadsEachObjectivesOwnEra()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var cut = new DateTime(1950, 1, 1);
        c.Eras.Add(new Era { Id = "later", Name = "A Later Chapter", From = cut, To = c.Eras[0].To, Systems = c.Eras[0].Systems });
        c.Eras[0].To = cut;
        c.EraRules["later"] = new Rules { Mvp = new MvpRules { ObjectivePoints = 1, ObjectiveBonusPerMonth = 0, ObjectiveBonusCap = 0 } };
        Assert.Equal(1, c.RulesAt(c.End.AddDays(-1)).Mvp!.ObjectivePoints);   // the game ends after the override

        // A perfect run: every objective on its reference date, in date order, scored with the rules of the turn that ends there.
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        foreach (var o in c.Objectives.OrderBy(o => o.ReferenceDate))
            Assert.True(Objectives.Record(c, s, c.RulesAt(o.ReferenceDate.AddDays(-1)).Mvp!, o.Id, o.ReferenceDate, new List<Cause>()));
        s.Date = c.End;
        s.GameOver = GameOverCodes.MvpEnded;
        Assert.Equal(101, s.Score);
        Assert.Equal(s.Score, EndReport.For(c, s).ReferenceScore);
    }

    // A group id and a standalone objective id are separate namespaces: a part_of equal to a standalone objective's id still pays both.
    [Fact]
    public void AGroupIdEqualToAStandaloneObjectiveIdStillCountsBoth()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        foreach (var o in c.Objectives.Where(o => o.PartOf != "")) o.PartOf = "absorb_immigrants";
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        foreach (var o in c.Objectives)
            Assert.True(Objectives.Record(c, s, c.RulesAt(s.Date).Mvp!, o.Id, o.ReferenceDate, new List<Cause>()));
        s.GameOver = GameOverCodes.MvpEnded;
        Assert.Equal(200, s.Score);
        Assert.Equal(s.Score, EndReport.For(c, s).ReferenceScore);
    }

    /// <summary>The fixture above has no prisoner points; here a real exchange (Prisoners.Return, the producer) scores some and the
    /// report carries that part.</summary>
    // R-END-005
    [Fact]
    public void ThePrisonerPartCarriesAScoreFromARealExchange()
    {
        var s = Ended();
        s.PrisonersHeld["egypt"] = 2;
        Prisoners.Return(Real, s, "egypt", new List<Cause>());
        var expected = 2 * Real.RulesAt(s.Date).Mvp!.PointsPerPrisonerUnitReturned!.Value;
        Assert.True(expected > 0);
        var r = EndReport.For(Real, s);
        Assert.Equal(expected, r.ScoreParts.PrisonerPoints);
        Assert.Equal(s.Score, r.Score);
        Assert.Equal(r.Score, r.ScoreParts.ObjectivePoints + r.ScoreParts.TimingBonuses + r.ScoreParts.PrisonerPoints);
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

    // R-POW-004, Q-END-3: the prisoners returned, in people as Defence shows them, over every front's exchange.
    [Fact]
    public void TheReportCarriesThePrisonersReturned()
    {
        var s = Ended();
        Assert.Equal(0, EndReport.For(Real, s).PrisonersReturned);
        s.PrisonersReturned["lebanon"] = 4; s.PrisonersReturned["egypt"] = 2;
        // What the Defence screen shows for these six units (DefencePresenter: units times rules.yaml's 1,000 people a unit).
        Assert.Equal(1000, Real.RulesAt(s.Date).Mvp!.PeoplePerUnit);
        Assert.Equal(6000, EndReport.For(Real, s).PrisonersReturned);
    }

    [Fact]
    public void AGameStillOnHasNoReport()
    {
        var s = Simulation.NewGame(Real, GoldenRun.Seed, Mode.Historical);
        Assert.Throws<InvalidOperationException>(() => EndReport.For(Real, s));
    }
}
