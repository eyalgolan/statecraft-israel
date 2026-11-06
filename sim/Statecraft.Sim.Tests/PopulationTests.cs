using System;
using System.Collections.Generic;
using Statecraft.Sim;

public class PopulationTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    [Fact]
    public void ArrivalsAreSpreadByOverlappingDays()
    {
        // Fixture period 1948-05-15..1948-12-31 inclusive = 231 days, 230 from europe.
        var a = PopulationSystem.Arrivals(C, new DateTime(1948, 5, 15), new DateTime(1948, 8, 15)); // 92 days
        Assert.Equal(230.0 * 92 / 231, a["europe"], 9);
        Assert.Equal(0.0, a["asia"], 9);
    }

    [Fact]
    public void WholePeriodSumsToSeriesTotal()
    {
        var a = PopulationSystem.Arrivals(C, new DateTime(1948, 1, 1), new DateTime(1949, 1, 1));
        Assert.Equal(230.0, a["europe"], 9);
    }

    [Fact]
    public void NoArrivalsOutsideSeries() =>
        Assert.Empty(PopulationSystem.Arrivals(C, new DateTime(1950, 1, 1), new DateTime(1950, 4, 1)));

    // R-FOOD-001: the national head count feeds the economy's per-capita tax and spending (EconomySystem), which moves the
    // hashed debt and fx, so it sums the groups in ordinal key order, never in the Dictionary's enumeration order, which
    // .NET does not promise. These three doubles give a different sum in each order.
    [Fact]
    public void TotalPopulationSumsTheGroupsInOrdinalKeyOrder()
    {
        var s = new GameState();
        s.Population["b"] = 1;
        s.Population["c"] = 1;
        s.Population["a"] = 1e16;
        Assert.NotEqual((1e16 + 1) + 1, (1.0 + 1) + 1e16);   // precondition: the order changes the sum
        Assert.Equal((1e16 + 1) + 1, s.TotalPopulation);
    }

    [Fact]
    public void ApplyAddsNaturalIncreaseThenArrivals()
    {
        var s = new GameState { Date = new DateTime(1948, 5, 15) };
        PopulationSystem.Init(C, s);
        var why = new List<Cause>();
        PopulationSystem.Apply(C, s, new DateTime(1948, 8, 15), 0.25, why);
        Assert.Equal(1000 * (1 + 0.017 * 0.25), s.Population["veterans"], 9);
        Assert.Equal(230.0 * 92 / 231, s.Population["europe"], 9);
        Assert.Contains(why, w => w.Target == "europe");
    }

    [Fact]
    public void ArrivalsAcrossLeapDayAndYearEndSumToThePeriodTotal()
    {
        // 1951-12-10..1952-03-20 inclusive = 22 + 31 + 29 (leap year) + 20 = 102 days.
        var aliyah = "regions:\n  - { id: europe, name: Europe }\nseries:\n  - from: 1951-12-10\n    to: 1952-03-20\n    origins: { europe: 1000 }\n";
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["aliyah.yaml"] = aliyah }));
        double sum = 0;
        for (var d = new DateTime(1951, 10, 1); d < new DateTime(1952, 7, 1); d = d.AddMonths(3))
            sum += PopulationSystem.Arrivals(c, d, d.AddMonths(3)).TryGetValue("europe", out var v) ? v : 0;
        Assert.Equal(1000.0, sum, 6);
        Assert.Equal(1000.0 * 80 / 102, PopulationSystem.Arrivals(c, new DateTime(1952, 1, 1), new DateTime(1952, 4, 1))["europe"], 6);
    }
}
