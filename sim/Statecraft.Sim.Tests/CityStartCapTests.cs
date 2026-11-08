using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>#1700 slice B, R-PROJ-001 under gdd-0.3 (owner, 3 November 2026: "One per city, plus 2 nationally"): a city
/// starts at most one city project a turn, and the national map_actions_per_turn (2) still applies. gdd-0.1 keeps its two
/// starts a turn, counted nationally, with no city cap. Fixture values, not the game's: town_a on (1, 0), town_b on (1, 1),
/// town_c on (3, 0), with map_action_max_distance 1 and map_actions_per_turn 2.</summary>
public class CityStartCapTests
{
    static Content Load(bool gdd03)
    {
        var f = ArmsWorkshopTests.Files(gdd03);
        f["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 1, r: 1 }\n  - { id: town_c, q: 3, r: 0 }\n";
        f["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n"
            + "  - { id: town_c, name: Town C, lon: 34.51, lat: 33.35, type: town, founded: 1900, pre_modern: false }\n";
        f["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 8, 10, 6, "[military_camp, hakhshara]")
            + Fixture.City("town_b", 5, 2, 4, 1) + Fixture.City("town_c", 5, 2, 4, 1);
        return ContentLoader.Load(Fixture.Write(f));
    }

    static GameState New(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Treasury = 100; s.Debt = 0; s.Attention = 10;
        return s;
    }

    static StartProject A1() => new StartProject("industry", "town_a", 1, 0);
    static StartProject A2() => new StartProject("housing", "town_a", 0, 1);
    static StartProject B() => new StartProject("industry", "town_b", 1, 1);
    static StartProject C() => new StartProject("industry", "town_c", 3, 0);

    const string CityCapText = "Town A has started a city project this turn";

    // A second start for the same city in one step is refused with the city's reason, and everything (Money, debt, the
    // projects, the cities' people, the counters) is as after the first start alone.
    [Fact]
    public void Gdd03SecondStartInOneCityIsRefused()
    {
        var c = Load(true);
        var s = New(c);
        var one = Simulation.Step(c, s, new Command[] { A1() });
        var two = Simulation.Step(c, s, new Command[] { A1(), A2() });
        Assert.Empty(one.Rejected);
        var refused = Assert.Single(two.Rejected);
        Assert.Equal(CityCapText, refused.Reason);
        Assert.Equal((one.State.Treasury, one.State.Debt, one.State.CityProjects.Count), (two.State.Treasury, two.State.Debt, two.State.CityProjects.Count));
        Assert.Equal(one.State.Cities["town_a"].Canonical(false), two.State.Cities["town_a"].Canonical(false));
        Assert.Equal(one.State.Hash(), two.State.Hash());

        // The validator's stable kind and invariant values (#1122), as the order runs: after the city's first start.
        var mid = s.Clone();
        Assert.Null(Projects.StartCity(c, mid, "industry", "town_a", 1, 0, new List<Cause>()));
        var reason = Projects.CheckCityReason(c, mid, "housing", "town_a", 0, 1);
        Assert.NotNull(reason);
        Assert.Equal("city_cap", reason!.Kind);
        Assert.Equal(new[] { "town_a" }, reason.Values);
        Assert.Equal(CityCapText, reason.Text);
        Assert.Empty(s.CityStartsThisTurn);   // Clone copies the record
        Assert.Contains("cstart{", mid.Describe());   // hashed once it has a start
    }

    [Fact]
    public void Gdd03TwoCitiesMayEachStart()
    {
        var c = Load(true);
        var s = New(c);
        var r = Simulation.Step(c, s, new Command[] { A1(), B() });
        Assert.Empty(r.Rejected);
        Assert.Equal(new[] { "town_a", "town_b" }, r.State.CityProjects.Values.Select(p => p.City).OrderBy(x => x, System.StringComparer.Ordinal));
    }

    // The national map_actions_per_turn (2) still applies: a third start, in a third city, is refused as turn_cap.
    [Fact]
    public void Gdd03NationalCapStillApplies()
    {
        var c = Load(true);
        var s = New(c);
        var r = Simulation.Step(c, s, new Command[] { A1(), B(), C() });
        var refused = Assert.Single(r.Rejected);
        Assert.IsType<StartProject>(refused.Command);
        Assert.Equal("town_c", ((StartProject)refused.Command).City);
        Assert.Equal("No more than 2 city projects in one turn", refused.Reason);
        Assert.Equal(2, r.State.CityProjects.Count);

        var mid = s.Clone();
        Assert.Null(Projects.StartCity(c, mid, "industry", "town_a", 1, 0, new List<Cause>()));
        Assert.Null(Projects.StartCity(c, mid, "industry", "town_b", 1, 1, new List<Cause>()));
        Assert.Equal("turn_cap", Projects.CheckCityReason(c, mid, "industry", "town_c", 3, 0)!.Kind);
        // Over both limits, the city's own reason comes first.
        Assert.Equal("city_cap", Projects.CheckCityReason(c, mid, "housing", "town_a", 0, 1)!.Kind);
    }

    // R-PROJ-001 with R-PROJ-004 (#2466): pioneer work is not a city project, so in either order it takes neither a city's
    // start nor a national one. Both city starts and the pioneer order are accepted, and a third city start is turn_cap.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Gdd03PioneerWorkTakesNoCityStart(bool pioneerFirst)
    {
        var c = Load(true);
        var s = New(c);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0));
        var pioneer = new PioneerWork(id, Projects.BuildYishuv, "site_a");
        var alone = Simulation.Step(c, s, new Command[] { pioneer });
        Assert.Empty(alone.Rejected);
        Assert.Empty(alone.State.CityStartsThisTurn);
        Assert.Equal(0, alone.State.MapActionsTurnDate == s.Date ? alone.State.MapActionsThisTurn : 0);

        var orders = pioneerFirst ? new Command[] { pioneer, A1(), B(), C() } : new Command[] { A1(), B(), pioneer, C() };
        var r = Simulation.Step(c, s, orders);
        var refused = Assert.Single(r.Rejected);
        Assert.Same(orders[3], refused.Command);
        Assert.Equal("No more than 2 city projects in one turn", refused.Reason);
        Assert.Equal(id, r.State.Sites["site_a"].Unit);
        Assert.Equal(new[] { "town_a", "town_b" }, r.State.CityProjects.Values.Select(p => p.City).OrderBy(x => x, System.StringComparer.Ordinal));
        Assert.Equal(new[] { ("town_a", 1), ("town_b", 1) }, r.State.CityStartsThisTurn.Select(kv => (kv.Key, kv.Value)));
        Assert.Equal(2, r.State.MapActionsThisTurn);
    }

    [Fact]
    public void Gdd03CityCapResetsNextTurn()
    {
        var c = Load(true);
        var s = New(c);
        var one = Simulation.Step(c, s, new Command[] { A1() });
        var two = Simulation.Step(c, one.State, new Command[] { A2() });
        Assert.Empty(two.Rejected);
        Assert.Equal(2, two.State.CityProjects.Count);
    }

    // A start that waits for a builder (nobody free in the city, R-CITY-011) is still a start: it uses the city's slot and
    // a national one, so a second start in that city is city_cap and a third city's is turn_cap.
    [Fact]
    public void Gdd03StartWaitingForABuilderStillCounts()
    {
        var c = Load(true);
        var s = New(c);
        var a = s.Cities["town_a"];
        s.Cities["town_a"] = new City(a.Id, a.HousingCapacity, a.Civilians, a.Civilians, a.Civilians, 0, 0, a.Condition, jobsDefault: false);
        var r = Simulation.Step(c, s, new Command[] { A1(), A2(), B(), C() });
        var waiting = r.State.CityProjects.Values.Single(p => p.City == "town_a");
        Assert.False(waiting.Builder);   // nobody free: no builder, the project waits
        Assert.Equal(new[] { CityCapText, "No more than 2 city projects in one turn" }, r.Rejected.Select(x => x.Reason));
        Assert.Equal(new[] { "town_a", "town_c" }, r.Rejected.Select(x => ((StartProject)x.Command).City));
        Assert.Equal(1, r.State.CityStartsThisTurn["town_a"]);
        Assert.Equal(2, r.State.MapActionsThisTurn);
        Assert.Equal(new[] { "town_a", "town_b" }, r.State.CityProjects.Values.Select(p => p.City).OrderBy(x => x, System.StringComparer.Ordinal));
    }

    // The cap limits starts, not building: in a turn where the city has used its start and city_cap refuses another, the
    // city's project already running advances exactly as in a turn where the city starts nothing.
    [Fact]
    public void Gdd03RunningProjectsKeepProgressing()
    {
        var c = Load(true);
        var s = New(c);
        var one = Simulation.Step(c, s, new Command[] { A1() });
        var running = one.State.CityProjects.Values.Single();
        var idle = Simulation.Step(c, one.State, new Command[0]);
        var two = Simulation.Step(c, one.State, new Command[] { A2(), new StartProject("industry", "town_a", 2, 0) });
        Assert.Equal(CityCapText, Assert.Single(two.Rejected).Reason);
        Assert.Equal(1, two.State.CityStartsThisTurn["town_a"]);   // the slot is used; the refused start is not counted
        var advanced = idle.State.CityProjects[running.Id];
        Assert.True(advanced.TurnsLeft < running.TurnsLeft);
        Assert.Equal(advanced.Canonical(), two.State.CityProjects[running.Id].Canonical());
    }

    // Under gdd-0.1 nothing changes: two starts in one city in one turn are both accepted, and nothing new is recorded or
    // hashed (legacy byte identity is the golden manifest's job, not a literal here).
    [Fact]
    public void Gdd01KeepsTwoNationallyNoCityCap()
    {
        var c = Load(false);
        Assert.Equal(SaveGame.GddRuleset, c.Ruleset);
        var s = New(c);
        var r = Simulation.Step(c, s, new Command[] { A1(), A2() });
        Assert.Empty(r.Rejected);
        Assert.Equal(2, r.State.CityProjects.Count);
        Assert.Empty(r.State.CityStartsThisTurn);
        Assert.DoesNotContain("cstart{", r.State.Describe());
        Assert.Equal("No more than 2 city projects in one turn", Simulation.Step(c, s, new Command[] { A1(), B(), C() }).Rejected.Single().Reason);
    }

    // R-SAVE: two project lines for one city in one turn replay to the same refusal and the same state.
    [Fact]
    public void Gdd03CityCapRoundTripsThroughASave()
    {
        var c = Load(true);
        var g = GameSession.New(c, 1, Mode.Historical);
        var r = g.Step(new Command[] { A1(), A2() });
        Assert.Equal(CityCapText, Assert.Single(r.Rejected).Reason);
        var text = g.Save.Write();
        Assert.Contains("\nproject industry town_a 1 0\n", text);
        Assert.Contains("\nproject housing town_a 0 1\n", text);
        var loaded = GameSession.Load(c, text, out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
        Assert.Equal(CityCapText, Assert.Single(loaded.Last!.Rejected).Reason);
    }
}
