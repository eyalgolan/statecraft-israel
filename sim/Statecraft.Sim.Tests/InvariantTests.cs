using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Invariant checks (#294): the committed file passes on the real golden runs, and the runner and the format checks
/// fail the way they should, with the check, the value and the band in the message.</summary>
public class InvariantTests
{
    static readonly Content C = ContentLoader.Load(RealContentTests.ContentRoot());

    [Fact]
    public void TheCommittedInvariantsHoldOnTheGoldenRuns()
    {
        var all = Invariants.Parse(System.IO.File.ReadAllText(Invariants.FilePath()));
        Assert.NotEmpty(all);
        var failures = new List<string>();
        foreach (var run in Invariants.Runs)
        {
            var mine = all.Where(i => i.Run == run).ToList();
            if (mine.Count == 0) continue;
            var states = Invariants.History(C, Invariants.Until(run));
            Assert.Equal(GoldenRun.Play(C, Invariants.Until(run)).Hash(), states[states.Count - 1].Hash());   // the same run the goldens pin
            failures.AddRange(Invariants.Evaluate(mine, states));
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    static List<GameState> Fixture3()
    {
        var a = Simulation.NewGame(C, GoldenRun.Seed, Mode.Historical);
        var b = a.Clone(); b.Date = new DateTime(1949, 1, 1); b.Treasury = 10; b.FxReserves = 5;
        var c = b.Clone(); c.Date = new DateTime(1950, 1, 1); c.Treasury = 30; c.FxReserves = -2;
        return new List<GameState> { a, b, c };
    }

    static Invariant Inv(string value, string when, double? min, double? max) =>
        new Invariant { Name = "t", Run = "1950", Value = value, When = when, Min = min, Max = max, Reason = "test" };

    [Fact]
    public void AValueInsideItsBandPasses()
    {
        Assert.Empty(Invariants.Evaluate(new[] { Inv("treasury", "end", 20, 40), Inv("treasury", "1949-01-01", 5, 15), Inv("fx", "always", -5, 100) }, Fixture3()));
    }

    [Fact]
    public void AFailureNamesTheCheckTheValueAndTheBand()
    {
        var f = Invariants.Evaluate(new[] { new Invariant { Name = "treasury-high", Run = "1950", Value = "treasury", When = "end", Max = 25, Reason = "why" } }, Fixture3()).Single();
        Assert.Contains("treasury-high", f);
        Assert.Contains("treasury = 30", f);
        Assert.Contains("1950-01-01", f);
        Assert.Contains("[-inf, 25]", f);
        Assert.Contains("why", f);
    }

    [Fact]
    public void AlwaysFailsOnTheFirstStateThatLeavesTheBandAndReportsItOnce()
    {
        var f = Invariants.Evaluate(new[] { Inv("fx", "always", 0, null) }, Fixture3());
        Assert.Single(f);
        Assert.Contains("fx = -2", f[0]);
    }

    [Fact]
    public void ADateUsesTheFirstStateOnOrAfterIt()
    {
        Assert.Single(Invariants.Evaluate(new[] { Inv("treasury", "1949-06-01", 0, 20) }, Fixture3()));   // 1950-01-01 is the first on or after it: 30
        Assert.Single(Invariants.Evaluate(new[] { Inv("treasury", "2000-01-01", 0, 20) }, Fixture3()));    // the run ends first
    }

    [Fact]
    public void ANonFiniteValueFailsWhateverTheBand()
    {
        var states = Fixture3();
        states[2].FxReserves = double.NaN;
        Assert.Contains("fx = NaN", Invariants.Evaluate(new[] { Inv("fx", "end", -5, 100) }, states).Single());
        states[2].FxReserves = double.PositiveInfinity;
        Assert.Single(Invariants.Evaluate(new[] { Inv("fx", "end", -5, null) }, states));
    }

    [Fact]
    public void AnUnknownValueFails()
    {
        Assert.Contains("neither a state value nor an indicator", Invariants.Evaluate(new[] { Inv("no_such", "end", 0, 1) }, Fixture3()).Single());
    }

    [Fact]
    public void AnIndicatorIsReadById()
    {
        var states = Fixture3();
        var id = states[0].Indicators.Keys.OrderBy(k => k, StringComparer.Ordinal).First();
        Assert.Empty(Invariants.Evaluate(new[] { Inv(id, "end", states[2].Indicators[id] - 0.001, states[2].Indicators[id] + 0.001) }, states));
    }

    [Theory]
    [InlineData("invariants:\n  - {name: a, run: '1950', value: fx, when: end, min: 0}\n", "no reason or source")]
    [InlineData("invariants:\n  - {name: a, run: '1950', value: fx, when: end, reason: r}\n", "neither min nor max")]
    [InlineData("invariants:\n  - {name: a, run: '1950', value: fx, when: end, min: 5, max: 1, reason: r}\n", "min above max")]
    [InlineData("invariants:\n  - {name: a, run: '1960', value: fx, when: end, min: 0, reason: r}\n", "names run '1960'")]
    [InlineData("invariants:\n  - {name: a, run: '1950', value: fx, when: sometime, min: 0, reason: r}\n", "when 'sometime'")]
    [InlineData("invariants:\n  - {name: a, run: '1950', value: fx, when: end, min: 0, reason: r}\n  - {name: a, run: '1950', value: fx, when: end, min: 0, reason: r}\n", "listed twice")]
    public void AMalformedInvariantIsRefusedWithTheProblemNamed(string yaml, string expected)
    {
        var e = Assert.Throws<System.IO.InvalidDataException>(() => Invariants.Parse(yaml));
        Assert.Contains(expected, e.Message);
    }
}
