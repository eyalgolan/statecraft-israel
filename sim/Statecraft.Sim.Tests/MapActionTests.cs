using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Transit camps and housing projects on the fixture map (5 x 2 hexes, town_a on (1, 0), sea on (0, 0)).</summary>
public class MapActionTests
{
    static readonly Content C = ContentLoader.Load(Fixture.Write());

    /// <summary>A state on 1948-06-01 (map actions open) with the ma'abarot policy on unless told otherwise.</summary>
    static GameState Ready(bool maabarot = true)
    {
        var s = Simulation.NewGame(C, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);
        s.Policies["maabarot"] = maabarot ? 1.0 : 0.0;
        return s;
    }

    [Theory]
    [InlineData(1, 0, 0)] [InlineData(2, 0, 1)] [InlineData(0, 1, 1)] [InlineData(3, 1, 3)] [InlineData(4, 0, 3)]
    public void AxialDistanceFromTownA(int q, int r, int expected) => Assert.Equal(expected, MapActions.Distance(1, 0, q, r));

    [Fact]
    public void ATransitCampCostsMoneyAndMovesHousingAndMorale()
    {
        var s = Ready();
        var housing = s.Indicators["housing"]; var morale = s.Indicators["morale"]; var treasury = s.Treasury;
        var why = new List<Cause>();
        Assert.Null(MapActions.Apply(C, s, "transit_camp", 2, 0, why));
        Assert.Equal(treasury - 2, s.Treasury, 9);
        Assert.Equal(housing + 0.05, s.Indicators["housing"], 9);
        Assert.Equal(morale - 0.02, s.Indicators["morale"], 9);
        Assert.Equal("transit_camp", MapActions.At(s, 2, 0)!.Action);
        Assert.Contains(why, w => w.System == "map" && w.Message == "Built a transit camp: cost IL 2.0m, Housing +5.0%, Morale -2.0%");
    }

    [Fact]
    public void AShortfallIsBorrowed()
    {
        var s = Ready();
        s.Treasury = 3;
        var why = new List<Cause>();
        Assert.Null(MapActions.Apply(C, s, "housing", 1, 1, why));
        Assert.Equal(0, s.Treasury);
        Assert.Equal(5, s.Debt, 9);
        Assert.Contains(why, w => w.Message == "Borrowed IL 5.0m to build a housing project");
    }

    [Fact]
    public void HousingReplacesATransitCamp()
    {
        var s = Ready();
        MapActions.Apply(C, s, "transit_camp", 2, 0, new List<Cause>());
        Assert.Null(MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>()));
        Assert.Equal("housing", MapActions.At(s, 2, 0)!.Action);
        Assert.Single(s.Placements);
    }

    [Theory]
    [InlineData("airport", 2, 0, "There is no such map action")]
    [InlineData("transit_camp", 0, 0, "This hex is not land")]
    [InlineData("transit_camp", 7, 7, "This hex is not land")]
    [InlineData("transit_camp", 4, 0, "This hex is more than 1 hex from a settlement")]
    public void IllegalPlacementsAreRejectedWithAReason(string action, int q, int r, string reason)
    {
        var s = Ready();
        var before = s.Hash();
        Assert.Equal(reason, MapActions.Apply(C, s, action, q, r, new List<Cause>()));
        Assert.Equal(before, s.Hash());
    }

    [Fact]
    public void NothingBeforeTheStartDate()
    {
        var s = Ready();
        s.Date = new DateTime(1948, 5, 31);
        Assert.Equal("Transit camps and housing projects can be placed from 1 June 1948", MapActions.Check(C, s, "housing", 2, 0));
    }

    [Fact]
    public void AReasonHasAStableKindAndInvariantValuesBesideItsEnglishText()
    {
        var s = Ready();
        s.Date = new DateTime(1948, 5, 31);
        var reason = MapActions.CheckReason(C, s, "housing", 2, 0)!;
        Assert.Equal("all_from", reason.Kind);
        Assert.Equal(new[] { "1948-06-01" }, reason.Values);
        Assert.Equal("Transit camps and housing projects can be placed from 1 June 1948", reason.Text);
    }

    [Fact]
    public void TransitCampsNeedTheMaabarotPolicy()
    {
        var s = Ready(maabarot: false);
        Assert.Equal("Transit camps need the ma'abarot policy", MapActions.Check(C, s, "transit_camp", 2, 0));
        Assert.Null(MapActions.Check(C, s, "housing", 2, 0));
    }

    [Fact]
    public void AtMostTwoMapActionsInOneTurn()
    {
        var s = Ready();
        Assert.Null(MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>()));
        Assert.Null(MapActions.Apply(C, s, "housing", 1, 1, new List<Cause>()));
        Assert.Equal("No more than 2 map actions in one turn", MapActions.Check(C, s, "housing", 0, 1));
        s.Date = s.Date.AddMonths(3);                                      // the next turn
        Assert.Null(MapActions.Check(C, s, "housing", 0, 1));
    }

    [Fact]
    public void UpgradingACampDoesNotEscapeTheTurnCap()
    {
        var s = Ready();
        Assert.Null(MapActions.Apply(C, s, "transit_camp", 2, 0, new List<Cause>()));
        Assert.Null(MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>()));     // replaces the camp: still a second action
        Assert.Equal("No more than 2 map actions in one turn", MapActions.Apply(C, s, "housing", 1, 1, new List<Cause>()));
        Assert.Null(MapActions.At(s, 1, 1));
    }

    [Fact]
    public void SpecificReasonsComeBeforeTheTurnCap()
    {
        var s = Ready();
        MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>());
        MapActions.Apply(C, s, "housing", 1, 1, new List<Cause>());
        Assert.Equal("This hex is not land", MapActions.Check(C, s, "housing", 7, 7));
        Assert.Equal("This hex is more than 1 hex from a settlement", MapActions.Check(C, s, "housing", 4, 0));
        Assert.Equal("This hex already has a housing project", MapActions.Check(C, s, "housing", 2, 0));
        Assert.Equal("There is no such map action", MapActions.Check(C, s, "airport", 0, 1));
        Assert.Equal("No more than 2 map actions in one turn", MapActions.Check(C, s, "housing", 0, 1));
    }

    [Fact]
    public void AnActionTheContentDoesNotDefineIsNotAvailable()
    {
        var rules = Fixture.Files["rules.yaml"].Replace("  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project }\n", "");
        Assert.NotEqual(Fixture.Files["rules.yaml"], rules);
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = rules }));
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);
        Assert.Equal("That map action is not available in this game", MapActions.Check(c, s, "housing", 2, 0));
    }

    [Fact]
    public void ALakeHexIsRefused()
    {
        var hexes = Fixture.Files["map/hexes.json"].Replace(@"{""q"":2,""r"":0,""lon"":34.38,""lat"":33.35,""elev_m"":250.0,""land"":true,""biome"":""hills""}",
                                                              @"{""q"":2,""r"":0,""lon"":34.38,""lat"":33.35,""elev_m"":0.0,""land"":false,""biome"":""lake""}");
        Assert.NotEqual(Fixture.Files["map/hexes.json"], hexes);
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/hexes.json"] = hexes }));
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);
        Assert.Equal("This hex is not land", MapActions.Check(c, s, "housing", 2, 0));
    }

    [Fact]
    public void AHexBesideTwoSettlementsAndAHexTwoSettlementsShareAreAccepted()
    {
        // town_b shares town_a's hex (1, 0), as Jaffa and Tel Aviv share (-3, 19); town_c sits on (3, 0), so (2, 0) touches two settlement hexes.
        var hexes = Fixture.Files["map/settlement_hexes.yaml"] + "  - { id: town_b, q: 1, r: 0 }\n  - { id: town_c, q: 3, r: 0 }\n";
        var towns = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.29, lat: 33.35, type: town, founded: 1900, pre_modern: false }\n"
            + "  - { id: town_c, name: Town C, lon: 34.47, lat: 33.35, type: town, founded: 1900, pre_modern: false }\n";
        var c = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["map/settlement_hexes.yaml"] = hexes, ["map/settlements_1948.yaml"] = towns }));
        var s = Simulation.NewGame(c, 42, Mode.Historical);
        s.Date = new DateTime(1948, 6, 1);
        Assert.Null(MapActions.Check(c, s, "housing", 2, 0));
        Assert.Null(MapActions.Check(c, s, "housing", 1, 0));
    }

    [Fact]
    public void JaffaAndTelAvivShareAHexAndBuildingThereWorks()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var jaffa = c.SettlementHexes.Single(x => x.Id == "jaffa");
        var tlv = c.SettlementHexes.Single(x => x.Id == "tel_aviv");
        Assert.Equal((-3, 19), (tlv.Q, tlv.R));
        Assert.Equal((tlv.Q, tlv.R), (jaffa.Q, jaffa.R));
        var s = GoldenRun.Play(c, new DateTime(1950, 9, 1));
        Assert.Null(MapActions.Check(c, s, "housing", tlv.Q, tlv.R));
    }

    [Fact]
    public void AnOccupiedHexIsRejected()
    {
        var s = Ready();
        MapActions.Apply(C, s, "housing", 2, 0, new List<Cause>());
        Assert.Equal("This hex already has a housing project", MapActions.Check(C, s, "transit_camp", 2, 0));
        Assert.Equal("This hex already has a housing project", MapActions.Check(C, s, "housing", 2, 0));
    }

    [Fact]
    public void TheCommandGoesThroughStepAndCostsAttention()
    {
        var s0 = Simulation.NewGame(C, 42, Mode.Historical);
        s0.Date = new DateTime(1948, 6, 1);
        // At war (rung 5) each command costs 2 of the 3 attention: the refused first order is refunded, so the second fits.
        var r = Simulation.Step(C, s0, new Command[] { new MapAction("housing", 4, 0), new MapAction("housing", 2, 0) });
        Assert.Equal("This hex is more than 1 hex from a settlement", r.Rejected.Single().Reason);
        Assert.Equal("housing", MapActions.At(r.State, 2, 0)!.Action);
        Assert.Empty(s0.Placements);                                       // Step never mutates its input
        Assert.NotEqual(Simulation.Step(C, s0, new Command[0]).State.Hash(), r.State.Hash());
    }

    [Fact]
    public void AnUnknownActionInRulesIsALoadError()
    {
        var bad = Fixture.Files["rules.yaml"].Replace("  housing: { cost: 8", "  airport: { cost: 8");
        var ex = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = bad })));
        Assert.Equal("map_actions.airport", ex.Field);
    }

    [Fact]
    public void RealContentOpensCampsWithTheMaabarotDecision()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = GoldenRun.Play(c, new DateTime(1950, 9, 1));
        var tlv = c.SettlementHexes.Single(x => x.Id == "tel_aviv");
        Assert.Null(MapActions.Check(c, s, "transit_camp", tlv.Q, tlv.R));
        var s1949 = GoldenRun.Play(c, new DateTime(1949, 6, 1));
        Assert.Equal("Transit camps and housing projects can be placed from 1 January 1950", MapActions.Check(c, s1949, "housing", tlv.Q, tlv.R));
    }

    /// <summary>Fix round 3 (m-6): a save that built something the content no longer defines still loads: the replay
    /// refuses that order with a reason (no id, no exception), so no placement of it exists and no formatter ever meets
    /// the stale id.</summary>
    [Fact]
    public void AStaleMapActionInASaveIsRefusedOnReplayNotACrash()
    {
        var g = GameSession.New(C, 7, Mode.Historical);
        while (g.State.Date < C.Rules.MapActionsFrom) g.Step(new Command[0]);
        var hex = C.Hexes.First(h => MapActions.Check(C, g.State, MapActions.Housing, h.Q, h.R) == null);
        g.Step(new Command[] { new MapAction(MapActions.Housing, hex.Q, hex.R) });
        Assert.NotNull(MapActions.At(g.State, hex.Q, hex.R));
        var save = g.Save.Write();
        var rules = Fixture.Files["rules.yaml"].Replace("  housing: { cost: 8, housing: 0.08, morale: 0.01, name: a housing project }\n", "");
        Assert.NotEqual(Fixture.Files["rules.yaml"], rules);
        var stale = ContentLoader.Load(Fixture.Write(new Dictionary<string, string> { ["rules.yaml"] = rules }));
        var loaded = GameSession.Load(stale, save, out var warnings);
        Assert.Contains(GameSession.ContentChanged, warnings);
        Assert.Null(MapActions.At(loaded.State, hex.Q, hex.R));
        var refused = Assert.Single(loaded.Last!.Rejected);
        Assert.Equal("That map action is not available in this game", refused.Reason);
        Assert.Equal("That map action is not available in this game", MapActions.Check(stale, loaded.State, MapActions.Housing, hex.Q, hex.R));
    }
}
