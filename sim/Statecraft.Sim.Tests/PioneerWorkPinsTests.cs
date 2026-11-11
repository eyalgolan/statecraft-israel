using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Two rows of #1713 (GDD 0.3 pioneer work units) that hold on main today, pinned so a later change cannot break
/// them unseen. Not red-first: both hold already (#803).</summary>
public class PioneerWorkPinsTests
{
    const string Types = Fixture.GddUnitTypes
        + "  - { id: enemy, kind: unit, side: egypt, class: infantry, movement_points: 3 }\n";

    /// <summary>The GDD fixture on MovementTests' 6 x 3 board (town_a on (1, 0), site_a on (2, 0), road_a along r = 0),
    /// with an enemy unit type.</summary>
    static Content Load() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["unit_types.yaml"] = Types, ["map/hexes.json"] = MovementTests.Board(),
    })));

    // #1713 "a lost worker counts as stopped" (R-SITE-002, Q51). No order or step removes a pioneer that is at work, so
    // Projects.Advance and Corridors.Advance always find their unit. The paths that remove a unit, each with a busy halutz:
    // Return refuses it (R-QUAL-003), Demobilization is for military units only (R-UNIT-005), an enemy attack has no target
    // in a hex with only halutzim (R-UNIT-001, Combat.Military), and an armistice removes that front's sides only
    // (R-WAR-007). After all of them, the next turn's work goes on.
    [Fact]
    public void ABusyPioneerIsNeverRemovedFromTheMap()
    {
        var c = Load();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        int Add(Unit u) { s.Units.Add(u.Id, u); s.NextId = System.Math.Max(s.NextId, u.Id + 1); return u.Id; }
        var site = Add(new Unit(s.NextId, "halutz", 3, 1));
        var seg = Add(new Unit(s.NextId, "halutz", 3, 2));
        var enemy = Add(new Unit(s.NextId, "enemy", 4, 1, side: "egypt", strength: 9,
            order: Combat.Attack, orderTarget: "3,1"));
        var started = Simulation.Step(c, s, new Command[] { new PioneerWork(site, Projects.BuildYishuv, "site_a"),
            new PioneerWork(seg, Corridors.BuildRoad, "road_a:1") });
        Assert.Empty(started.Rejected);
        s = started.State;
        Assert.True(Projects.IsBusy(s, site) && Projects.IsBusy(s, seg));

        foreach (var id in new[] { site, seg })
        {
            Assert.Equal(Qualification.RefusedPioneerBusy, Qualification.CheckReturn(s, id));
            Assert.Equal(Orders.RefusedNotMilitary, Demobilization.Check(c, s, id));
        }
        Assert.Null(Combat.EnemyPreview(c, s, enemy, 3, 1));
        foreach (var front in Prisoners.Fronts) Armistice.RemoveFront(s, front);
        Assert.False(s.Units.ContainsKey(enemy));   // the egypt front's unit left the board

        var next = Simulation.Step(c, s, new Command[0]);
        Assert.Empty(next.Rejected);
        Assert.Equal((site, "halutz"), (next.State.Units[site].Id, next.State.Units[site].Type));
        Assert.Equal((seg, "halutz"), (next.State.Units[seg].Id, next.State.Units[seg].Type));
        Assert.Equal(1, next.State.Sites["site_a"].TurnsLeft);                        // 3 turns, two of a halutz's
        Assert.Equal(2, next.State.Corridors["road_a"].Segments[0]!.Progress);        // of 3, two of a halutz's
    }

    // #1713 "a facility id can be built by builders (#1700) or a pioneer, never both". City projects (projects.yaml, which
    // a city's builders build under gdd-0.3, R-CITY-011) and pioneer work (sites and corridor segments, R-SITE-001,
    // R-COR-001) share no id: a pioneer order that names a project kind is no pioneer work, and a city project that names a
    // pioneer action, a site or a site kind is no city project. In the shipped content under gdd-0.1 and under gdd-0.3,
    // whose arms_workshop and housing_repair only gdd-0.3 loads.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NoFacilityIsBuiltByBothBuildersAndAPioneer(bool gdd03)
    {
        var c = HistoricalPopulationTests.Real(gdd03);
        Assert.Equal(gdd03, c.Ruleset == SaveGame.Gdd03Ruleset);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var unit = s.NextId++;
        s.Units.Add(unit, new Unit(unit, "halutz", 0, 0));
        var site = c.Sites.First(Projects.IsYishuvSite).Id;
        var city = s.Cities.Keys.First();
        var pioneer = new[] { Projects.BuildYishuv, Projects.BuildCamp, Projects.PlowFields, Projects.DrySwamp, Corridors.BuildRoad, Corridors.BuildRail };
        Assert.Equal(gdd03, c.ProjectKinds.Any(x => x.Id == Projects.HousingRepair));
        foreach (var k in c.ProjectKinds)
            Assert.Equal(Projects.RefusedNoWork, Projects.Start(c, s, unit, k.Id, site, new List<Cause>()));
        foreach (var id in pioneer.Concat(c.Sites.Select(x => x.Id)).Concat(c.Sites.SelectMany(x => x.Kinds)).Distinct())
            Assert.Equal("unknown_kind", Projects.CheckCityReason(c, s, id, city, 0, 0)!.Kind);
        Assert.False(Projects.IsBusy(s, unit));   // every refusal left the unit idle
    }
}
