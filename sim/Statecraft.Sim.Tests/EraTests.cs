using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>eras.yaml (#56): load-time checks, the era lookup, the boundary turn and the end of the last era.</summary>
public class EraTests
{
    static readonly DateTime Start = new DateTime(1948, 5, 15);
    static readonly DateTime End = new DateTime(1957, 1, 1);

    static ContentException BadEras(string yaml) =>
        Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = yaml })));

    static string Eras(params string[] entries) => "eras:\n" + string.Join("\n", entries) + "\n";
    static string Era(string id, string from, string to, string name = "A Name") =>
        $"  - id: {id}\n    name: {name}\n    from: {from}\n    to: {to}";

    [Fact]
    public void RealContentHasTheFoundingEra()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var era = Assert.Single(c.Eras);
        Assert.Equal("founding", era.Id);
        Assert.Equal(c.Rules.Start, era.From);
        Assert.Equal(End, era.To);
    }

    [Fact]
    public void EraAtIsHalfOpenAndNullOutsideTheRange()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Null(c.EraAt(Start.AddDays(-1)));
        Assert.Equal("founding", c.EraAt(Start)!.Id);
        Assert.Equal("founding", c.EraAt(End.AddDays(-1))!.Id);
        Assert.Null(c.EraAt(End));
    }

    [Fact]
    public void EveryRealEventWindowLiesInsideTheErasRange()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var (first, last) = (c.Eras.First().From, c.Eras.Last().To);
        foreach (var e in c.Events)
        {
            Assert.True(e.From >= first, $"{e.File}: from {e.From:yyyy-MM-dd} is before the first era starts ({first:yyyy-MM-dd})");
            Assert.True((e.To ?? last) <= last, $"{e.File}: to {e.To:yyyy-MM-dd} is after the last era ends ({last:yyyy-MM-dd})");
        }
    }

    [Fact]
    public void TwoContiguousErasLoadAndEachDateHasOneEra()
    {
        var yaml = Eras(Era("a", "1948-05-15", "1950-01-01"), Era("b", "1950-01-01", "1957-01-01"));
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = yaml }));
        Assert.Equal("a", c.EraAt(new DateTime(1949, 12, 31))!.Id);
        Assert.Equal("b", c.EraAt(new DateTime(1950, 1, 1))!.Id);
    }

    [Fact]
    public void OverlappingErasAreALoadError()
    {
        var ex = BadEras(Eras(Era("a", "1948-05-15", "1950-06-01"), Era("b", "1950-01-01", "1957-01-01")));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Equal("eras[b].from", ex.Field);
        Assert.Contains("overlaps a", ex.Message);
    }

    [Fact]
    public void AGapBetweenErasIsALoadError()
    {
        var ex = BadEras(Eras(Era("a", "1948-05-15", "1950-01-01"), Era("b", "1950-02-01", "1957-01-01")));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Equal("eras[b].from", ex.Field);
        Assert.Contains("gap after a", ex.Message);
    }

    [Theory]
    [InlineData("1948-05-15")]   // empty range
    [InlineData("1948-05-14")]   // ends before it starts
    public void AnEmptyRangeIsALoadError(string to)
    {
        var ex = BadEras(Eras(Era("a", "1948-05-15", to)));
        Assert.Equal("eras[a].to", ex.Field);
        Assert.Contains("must be after from", ex.Message);
    }

    [Theory]
    [InlineData("from")]
    [InlineData("to")]
    public void AMissingDateIsALoadError(string field)
    {
        var era = field == "from" ? "  - id: a\n    name: A Name\n    to: 1957-01-01" : "  - id: a\n    name: A Name\n    from: 1948-05-15";
        var ex = BadEras(Eras(era));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Equal("eras[a]." + field, ex.Field);
        Assert.Contains("required", ex.Message);
    }

    [Theory]
    [InlineData("1957-13-01")]
    [InlineData("soon")]
    public void ABadDateIsALoadError(string to)
    {
        var ex = BadEras(Eras(Era("a", "1948-05-15", to)));
        Assert.Equal("eras.yaml", ex.File);
    }

    [Fact]
    public void ADuplicateEraIdIsALoadError()
    {
        var ex = BadEras(Eras(Era("a", "1948-05-15", "1950-01-01"), Era("a", "1950-01-01", "1957-01-01")));
        Assert.Equal("eras.yaml", ex.File);
        Assert.Equal("eras[a].id", ex.Field);
        Assert.Contains("duplicate", ex.Message);
    }

    [Fact]
    public void TheFirstEraMustStartOnTheGameStart()
    {
        var ex = BadEras(Eras(Era("a", "1948-05-16", "1957-01-01")));
        Assert.Equal("eras[a].from", ex.Field);
        Assert.Contains("rules.yaml", ex.Message);
    }

    [Fact]
    public void AnEraNeedsAName()
    {
        var ex = BadEras(Eras(Era("a", "1948-05-15", "1957-01-01", name: "''")));
        Assert.Equal("eras[a].name", ex.Field);
    }

    [Fact]
    public void NoErasIsALoadError()
    {
        var ex = BadEras("eras: []\n");
        Assert.Equal("eras", ex.Field);
    }

    [Fact]
    public void AnUnknownFieldIsALoadError() =>
        BadEras(Eras(Era("a", "1948-05-15", "1957-01-01") + "\n    carry: carry/founding.yaml"));

    // --- the boundary turn (spec 4.1) and the end of the last era (spec 4.5)

    static Content EndingOn(string to) =>
        ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["eras.yaml"] = Eras(Era("founding", "1948-05-15", to)) }));

    static GameState PlayToTheEnd(Content c, out List<DateTime> dates)
    {
        var s = Simulation.NewGame(c, 1948, Mode.Historical);
        dates = new List<DateTime> { s.Date };
        for (int i = 0; i < 100 && s.GameOver == null; i++)
        {
            s = Simulation.Step(c, s, new Command[0]).State;
            dates.Add(s.Date);
        }
        return s;
    }

    [Fact]
    public void ReachingTheLastErasEndSetsTheCodeOnThatDate()
    {
        var c = EndingOn("1948-07-03");   // the weekly grid from 15 May lands on 3 July
        var s = PlayToTheEnd(c, out var dates);
        Assert.Equal(GameOverCodes.EraEnded, s.GameOver);
        Assert.Equal(new DateTime(1948, 7, 3), s.Date);
        Assert.Equal(new DateTime(1948, 7, 3), dates.Last());
    }

    [Fact]
    public void ATurnThatWouldCrossTheEndIsAdjustedToLandOnIt()
    {
        var c = EndingOn("1948-07-01");   // the grid is 5, 12, 19, 26 June, 3 July: the last turn would cross 1 July
        var s = PlayToTheEnd(c, out var dates);
        Assert.Equal(new DateTime(1948, 7, 1), s.Date);
        Assert.DoesNotContain(new DateTime(1948, 7, 3), dates);
    }

    [Fact]
    public void ATurnThatWouldLeaveUnder14DaysIsLengthenedInstead()
    {
        var c = EndingOn("1948-07-01");
        PlayToTheEnd(c, out var dates);
        // The turn after 19 June would be 12 days, under 14, so the turn from 12 June runs to 1 July (19 days).
        Assert.Equal(new DateTime(1948, 6, 12), dates[^2]);
        Assert.Equal(new DateTime(1948, 7, 1), dates[^1]);
        Assert.All(dates.Zip(dates.Skip(1), (a, b) => (b - a).Days), d => Assert.InRange(d, 7, 21));
    }

    [Fact]
    public void ATurnLandsOnTheNextErasStartAndTheGameGoesOn()
    {
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string>
        {
            ["eras.yaml"] = Eras(Era("founding", "1948-05-15", "1948-07-01"), Era("next", "1948-07-01", "1948-10-10"),
                Era("last", "1948-10-10", "1948-12-01")),
        }));
        var s = PlayToTheEnd(c, out var dates);
        // Weekly while the fixture's war lasts, then quarterly from 12 June (the ceasefire ends the weekly turns).
        // 12 June: the quarter is cut at the nearest boundary, 1 July (not the later 10 October).
        // 1 July: the quarter would end 1 October, 9 days short of 10 October, so it is lengthened to it.
        // 10 October: the quarter is cut at the last era's end, which ends the game.
        var expected = new[] { (5, 15), (5, 22), (5, 29), (6, 5), (6, 12), (7, 1), (10, 10), (12, 1) }
            .Select(md => new DateTime(1948, md.Item1, md.Item2));
        Assert.Equal(expected, dates);
        Assert.Equal(GameOverCodes.EraEnded, s.GameOver);   // only the last era's end ends the game
    }

    [Fact]
    public void TheEndReasonShowsNoIdAndNoIsoDate()
    {
        var c = EndingOn("1948-07-03");
        var s = Simulation.NewGame(c, 1948, Mode.Historical);
        StepResult r = null!;
        while (s.GameOver == null) { r = Simulation.Step(c, s, new Command[0]); s = r.State; }
        var text = r.Why.Single(w => w.System == "era").Message;
        Assert.Equal("The chapter has ended", text);
        Assert.DoesNotContain("era_ended", text);
        Assert.DoesNotMatch(@"\d{4}-\d{2}-\d{2}", text);
        var refused = Simulation.Step(c, s, new Command[] { new SetMobilization(0.5) }).Rejected.Single();
        Assert.DoesNotContain("era_ended", refused.Reason);
        Assert.DoesNotMatch(@"\d{4}-\d{2}-\d{2}", refused.Reason);
    }

    [Fact]
    public void CommandsAfterTheEndAreRefusedAndTheStateDoesNotMove()
    {
        var c = EndingOn("1948-07-03");
        var s = PlayToTheEnd(c, out _);
        var after = Simulation.Step(c, s, new Command[] { new SetMobilization(0.5) });
        Assert.Single(after.Rejected);
        Assert.Equal(s.Date, after.State.Date);
        Assert.Equal(s.Hash(), after.State.Hash());
    }

    [Fact]
    public void AnAdjustedTurnCountsItsRealWeeksOnTheCeasefireClock()
    {
        // Rung 5 from the start; the clock starts at ceasefire_weeks (4 in the fixture) and runs 1 week per weekly turn.
        // Ending on 1 July makes the last turn 19 days: 19 / 7 = 2 whole weeks.
        var c = EndingOn("1948-07-01");
        var s = Simulation.NewGame(c, 1948, Mode.Historical);
        var weeklyTurns = 0;
        var before = s.CeasefireWeeksLeft;
        while (s.GameOver == null && s.Rung == 5)
        {
            var next = Simulation.Step(c, s, new Command[0]).State;
            var days = (next.Date - s.Date).Days;
            if (next.Rung == 5) Assert.Equal(Math.Max(0, s.CeasefireWeeksLeft - (days == 7 ? 1 : days / 7)), next.CeasefireWeeksLeft);
            s = next; weeklyTurns++;
        }
        Assert.True(weeklyTurns > 0 && before > 0);
    }
}
