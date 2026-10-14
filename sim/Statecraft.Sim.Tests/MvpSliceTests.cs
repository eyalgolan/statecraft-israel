using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>MvpSlice (GDD v0.1 Phase 1): the real content with the loop switch on and monthly turns, for tests, the MVP
/// goldens and the dev flag. The real eras.yaml and rules.yaml keep the defaults until cutover.</summary>
public class MvpSliceTests
{
    static readonly Content Real = ContentLoader.Load(RealContentTests.ContentRoot());
    static readonly Content Mvp = MvpSlice.Load(RealContentTests.ContentRoot());

    // R-LOOP-003
    [Fact]
    public void TheOverlayTurnsTheSwitchOnAndSetsMonthlyTurns()
    {
        Assert.False(Real.IsGdd);
        Assert.True(Mvp.IsGdd);
        Assert.All(Mvp.Eras, e => Assert.Equal("gdd", e.Systems.Loop));
        Assert.Equal(new[] { "month", "month", "month", "month", "month" }, Mvp.Rules.TurnLengths);
        Assert.NotEqual(Real.Hash, Mvp.Hash);   // different content: a save made on one warns on the other
    }

    // R-LOOP-003
    [Fact]
    public void TheOverlayChangesNothingElse()
    {
        Assert.Equal(Real.Events.Select(e => e.Id), Mvp.Events.Select(e => e.Id));
        Assert.Equal(Real.Rules.Start, Mvp.Rules.Start);
        // The slice adds the GDD command kinds' costs (placeholders) and keeps every other cost.
        Assert.Equal(Real.Rules.AttentionCost.Append(new KeyValuePair<string, double>("assign_workers", 0.5))
                     .Append(new KeyValuePair<string, double>("train", 1)).Append(new KeyValuePair<string, double>("cancel_training", 0.5)).OrderBy(kv => kv.Key, StringComparer.Ordinal),
                     Mvp.Rules.AttentionCost.OrderBy(kv => kv.Key, StringComparer.Ordinal));
        Assert.Equal(Real.Rules.BudgetStart, Mvp.Rules.BudgetStart);
        Assert.Equal(Real.Rules.CapitalInflowPerYear, Mvp.Rules.CapitalInflowPerYear);
        Assert.Equal(Real.Eras.Select(e => (e.Id, e.Name, e.From, e.To, e.Systems.Legislature, string.Join(",", e.Systems.MapActions))),
                     Mvp.Eras.Select(e => (e.Id, e.Name, e.From, e.To, e.Systems.Legislature, string.Join(",", e.Systems.MapActions))));
        Assert.Equal(Real.EraRules.Keys.OrderBy(k => k), Mvp.EraRules.Keys.OrderBy(k => k));
    }

    // R-LOOP-003
    [Fact]
    public void TheSlicePlaysMonthlyFromTheFifteenthOfMay1948()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var dates = new System.Collections.Generic.List<DateTime>();
        for (int i = 0; i < 3; i++) { s = Simulation.Step(Mvp, s, HistoricalAI.Commands(Mvp, s)).State; dates.Add(s.Date); }
        Assert.Equal(new[] { new DateTime(1948, 6, 15), new DateTime(1948, 7, 15), new DateTime(1948, 8, 15) }, dates);
    }

    [Fact]
    public void TheOverlayKeepsAnEraSwitchAlreadySet()
    {
        var read = MvpSlice.Overlay(rel => rel == "eras.yaml"
            ? "eras:\n  - id: founding\n    name: F\n    from: 1948-05-15\n    to: 1957-01-01\n    systems: { legislature: assembly }\n"
            : null);
        var text = read("eras.yaml")!;
        Assert.Contains("legislature: assembly", text);
        Assert.Contains("loop: gdd", text);
        Assert.Null(read("missing.yaml"));
    }

    [Fact]
    public void LoadKeepsTheLoadersDirectoryChecks()
    {
        var dir = Fixture.Write(Fixture.Gdd());
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(dir, "rules"));
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "rules", "nosuchera.yaml"), "attention_per_turn: 5\n");
        var ex = Assert.Throws<ContentException>(() => MvpSlice.Load(dir));
        Assert.Equal("rules/nosuchera.yaml", ex.File);
        Assert.Contains("must be the id of an era", ex.Message);
    }
}
