using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>GDD Phase 3 PR 2: the CBS arrivals spread over the cities (R-CITY-003, Q-ARRV-1 and Q-ARRV-3), and the one
/// relation between the national head count and the cities (R-CITY-008, Q-ARRV-2). Owner, 15 October 2026, #375: "Take all
/// recommendations".</summary>
public class ArrivalsTests
{
    static StepResult Step(Content c, GameState s) => Simulation.Step(c, s, Array.Empty<Command>());

    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The fixture with three towns on their own hexes, of 10, 10 and 20 people (shares 1:1:2), housing for all the
    /// fixture's arrivals. Fixture values, not history.</summary>
    static Content Three() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n  - { id: town_c, q: 3, r: 0 }\n",
        ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n"
            + "  - { id: town_c, name: Town C, lon: 34.47, lat: 33.35, type: town, founded: 1900, pre_modern: false }\n",
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 40, 1, 1, 0) + Fixture.City("town_b", 40, 1, 1, 0) + Fixture.City("town_c", 40, 2, 2, 0),
    })));

    /// <summary>The cities' people in units: civilians, refugees, recruits and the groups in training (Q-ARRV-2).</summary>
    static int CityUnits(GameState s) =>
        s.Cities.Values.Sum(x => x.Civilians + x.Craftsmen + x.Refugees + x.Recruits) + s.Cohorts.Values.Sum(x => x.Count);

    /// <summary>The cities' arrivals P(t): the series' people from the census day to <paramref name="t"/> (Q-ARRV-3).</summary>
    static double CityPeople(Content c, DateTime t)
    {
        var from = c.Rules.Mvp!.CityArrivalsFrom;
        return t <= from ? 0 : PopulationSystem.Arrivals(c, from, t).OrderBy(kv => kv.Key, StringComparer.Ordinal).Sum(kv => kv.Value);
    }

    // R-CITY-003 (review focus 1: no unit lost or doubled by the split)
    [Fact]
    public void TheSplitLosesNoUnit()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var start = s;
        for (int i = 0; i < 44 && s.GameOver == null; i++)
        {
            var next = Step(Mvp, s).State;   // no orders: only arrivals change the cities' people
            Assert.Equal(Cities.ArrivedUnits(Mvp, s.Date, next.Date, 1000), CityUnits(next) - CityUnits(s));
            s = next;
        }
        Assert.Equal((int)Math.Floor(CityPeople(Mvp, s.Date) / 1000) - (int)Math.Floor(CityPeople(Mvp, start.Date) / 1000), CityUnits(s) - CityUnits(start));
    }

    // R-CITY-003 (Q-ARRV-1): in proportion to the census people, one unit at a time to the largest remainder, ties to the
    // lower id (ordinal).
    [Fact]
    public void EachCityGetsItsShare()
    {
        var c = Three();
        Assert.Equal(new[] { 0, 0, 1 }, Cities.Apportion(c, 1).Values);   // remainders 0.25, 0.25, 0.5
        Assert.Equal(new[] { 1, 0, 1 }, Cities.Apportion(c, 2).Values);   // remainders 0.5, 0.5, 0: the tie goes to town_a
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var before = s.Cities.Values.Select(x => x.Civilians + x.Refugees).ToArray();
        while (s.Date < new DateTime(1949, 1, 15)) s = Step(c, s).State;
        var added = s.Cities.Values.Select((x, i) => x.Civilians + x.Refugees - before[i]).ToArray();
        Assert.Equal(new[] { 6, 6, 11 }, added);   // the fixture's 230 people are 23 units: quotas 5.75, 5.75, 11.5
    }

    // R-CITY-003: the split is a function of the cities' cumulative units alone, so a city never gives a unit back, as a fresh
    // largest-remainder split of each total would (the Alabama paradox: at 7 units Petah Tikva would lose one).
    [Fact]
    public void TheSplitNeverTakesAUnitBack()
    {
        var prev = Cities.Apportion(Mvp, 0);
        Assert.All(prev.Values, v => Assert.Equal(0, v));
        for (int n = 1; n <= 1000; n++)
        {
            var cur = Cities.Apportion(Mvp, n);
            Assert.Equal(n, cur.Values.Sum());
            foreach (var kv in cur) Assert.True(kv.Value >= prev[kv.Key], $"{kv.Key} gives back a unit at {n}");
            prev = cur;
        }
    }

    // R-CITY-003 (Q-ARRV-3): the census of 8 November 1948 already counts the people who arrived from 15 May, so the cities
    // take arrivals only from that day.
    [Fact]
    public void TheCitiesHoldTheirCensusCountsOnTheCensusDay()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(511, CityUnits(s));   // the census start total (R-CITY-007)
        while (s.Date < new DateTime(1948, 10, 15)) s = Step(Mvp, s).State;
        Assert.Equal(new DateTime(1948, 10, 15), s.Date);   // the last turn that starts before the census day
        Assert.Equal(511, CityUnits(s));
        var next = Step(Mvp, s).State;
        Assert.Equal((int)Math.Floor(CityPeople(Mvp, next.Date) / 1000), CityUnits(next) - 511);
        Assert.True(CityUnits(next) > 511);
    }

    // R-CITY-008 (Q-ARRV-2): GameState.Population is the one record of the nation's people; the cities are the part placed
    // in towns, so they never hold more people than it. Replaces CitiesTests.BeforeCutoverArrivalsAreInTheLegacyHeadCountToo,
    // which pinned the two as separate views of the same arrivals.
    [Fact]
    public void ThePopulationAndTheCitiesAgree()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var ppu = Mvp.Rules.Mvp!.PeoplePerUnit;
        while (true)
        {
            Assert.True((double)CityUnits(s) * ppu <= s.Population.Values.Sum(), $"{s.Date:yyyy-MM-dd}: the cities hold more people than the nation");
            if (s.Date >= Mvp.End || s.GameOver != null) break;
            s = Simulation.Step(Mvp, s, GoldenRun.Orders(Mvp, s)).State;
        }
        Assert.Equal(Mvp.End, s.Date);
    }

    // R-CITY-008: with natural increase disabled, every change in the national population is an arrival.
    [Fact]
    public void TheNationalPopulationReceivesEveryArrival()
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace("natural_increase_per_year: 0.017", "natural_increase_per_year: 0");
        var c = ContentLoader.Load(Fixture.Write(files));
        var before = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var after = Step(c, before).State;
        var arrived = PopulationSystem.Arrivals(c, before.Date, after.Date).Values.Sum();

        Assert.True(arrived > 0, "the fixture's first turn has arrivals");
        Assert.Equal(arrived, after.TotalPopulation - before.TotalPopulation, 9);
    }
}
