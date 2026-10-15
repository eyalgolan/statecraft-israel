using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD cutover (Phase 2 PR 1): the real content runs the GDD loop on monthly turns from 15 May 1948 to
/// 15 January 1952 (spec 2026-10-12 section 3; owner decisions D-B1, D-E1 and D-E5).</summary>
public class CutoverTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    // R-LOOP-001
    [Fact]
    public void TheRealGameRunsTheGddLoop()
    {
        Assert.True(C.IsGdd);
        Assert.Equal("gdd-0.1", C.Ruleset);
        Assert.All(C.Eras, e => Assert.Equal(EraSystems.Gdd, e.Systems.Loop));
    }

    // R-TURN-001
    [Fact]
    public void EveryRungIsAMonthInTheRealGame()
    {
        foreach (var era in C.Eras)
            for (int rung = 1; rung <= 5; rung++)
                Assert.Equal(TurnLength.Month, SecuritySystem.TurnLengthFor(C, era.From, rung));
    }

    // R-TURN-001
    [Fact]
    public void TheFirstTurnsFallOnTheFifteenth()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(new DateTime(1948, 5, 15), s.Date);
        var dates = new List<DateTime>();
        for (int i = 0; i < 3; i++) { s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State; dates.Add(s.Date); }
        Assert.Equal(new[] { new DateTime(1948, 6, 15), new DateTime(1948, 7, 15), new DateTime(1948, 8, 15) }, dates);
    }

    // R-LOOP-004
    [Fact]
    public void TheGameEndsOnFifteenJanuary1952()
    {
        Assert.Equal(new DateTime(1952, 1, 15), C.End);
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        StepResult? last = null;
        while (s.GameOver == null && s.Turn < 1000) { last = Simulation.Step(C, s, HistoricalAI.Commands(C, s)); s = last.State; }
        Assert.Equal(new DateTime(1952, 1, 15), s.Date);
        Assert.Equal(44, s.Turn);
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
        Assert.Contains(last!.Why, w => w.System == "era" && w.Target == GameOverCodes.MvpEnded);
        Assert.DoesNotContain(last.Why, w => w.System == "events" && w.Message.StartsWith("New:"));   // the last turn raises no event
        // A finished game takes no more orders.
        Assert.NotEmpty(Simulation.Step(C, s, new Command[] { new SecurityOrder(-1) }).Rejected);
    }

    // R-LOOP-004
    [Fact]
    public void NoTurnIsAdjustedInsideTheMvp()
    {
        var s = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        while (s.GameOver == null && s.Turn < 1000)   // capped: a broken end must fail, not hang
        {
            var before = s.Date;
            s = Simulation.Step(C, s, HistoricalAI.Commands(C, s)).State;
            Assert.Equal(before.AddMonths(1), s.Date);
            Assert.Equal(15, s.Date.Day);
        }
        Assert.Equal(GameOverCodes.MvpEnded, s.GameOver);
    }

    // R-LOOP-004
    [Theory]
    [InlineData("1948-05-15", "must be after start")]
    [InlineData("1957-01-02", "before the last era's end")]
    [InlineData("1957-01-01", "before the last era's end")]   // equal to the last era's end: the game would end as era_ended
    public void AnEndOutsideThePlayFailsNamingTheField(string end, string message)
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] += "  end: " + end + "\n";
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal(("rules.yaml", "mvp.end"), (e.File, e.Field));
        Assert.Contains(message, e.Message);
    }

    // R-LOOP-004
    [Fact]
    public void WithoutAnEndTheGameRunsToTheLastEra()
    {
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd()));
        Assert.Null(c.Rules.Mvp!.End);
        Assert.Equal(c.Eras[c.Eras.Count - 1].To, c.End);
    }
}
