using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using Statecraft.Sim.Reference;

/// <summary>GDD Phase 3 PR 4's city projects (plan docs/superpowers/plans/2026-10-15-gdd-phase-3.md, "PR 4"; owner, 15 October
/// 2026, #375, "Take all recommendations"): StartProject (R-PROJ-001), CancelProject (R-PROJ-002) and construction in step 5
/// (R-PROJ-003). The fixture's town_a is on (1, 0); (1, 1) and (2, 0) are land next to it, (0, 0) is sea and (3, 0) is two
/// hexes away. Its kinds: housing (IL 3m, 2 turns, +4 capacity, +0.1 condition), transit_camp (IL 1m, 1 turn, +3, -0.1) and
/// industry (IL 2m, 2 turns, +3 job slots).</summary>
public class CityProjectsTests
{
    static Content Town() => ContentLoader.Load(Fixture.Write(Fixture.Gdd()));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static GameState New(Content c) => Simulation.NewGame(c, 1, Mode.Historical);

    static StartProject Housing(int q = 1, int r = 1) => new StartProject("housing", "town_a", q, r);

    // R-PROJ-003 (Review focus 5)
    [Fact]
    public void AHousingProjectAddsCapacityOnceWhenDone()
    {
        var c = Town();
        var s = New(c);
        var one = Step(c, s, Housing());
        Assert.Empty(one.Rejected);
        var p = one.State.CityProjects.Values.Single();
        Assert.Equal(("housing", "town_a", 1, 1, 1, (DateTime?)null), (p.Kind, p.City, p.Q, p.R, p.TurnsLeft, p.BuiltOn));   // 2 turns, this one the first
        Assert.Equal(Step(c, s).State.Treasury - 3, one.State.Treasury, 9);   // paid in full at the start (Q-PROJ-1)
        Assert.Contains(one.Why, x => x.System == "city" && x.Message == "Town A: a housing project began, cost IL 3.0m");
        Assert.Equal(s.Cities["town_a"].HousingCapacity, one.State.Cities["town_a"].HousingCapacity);   // nothing yet

        var two = Step(c, one.State);
        Assert.Equal(two.State.Date, two.State.CityProjects[p.Id].BuiltOn);   // built on the turn's end date
        Assert.Equal(one.State.Cities["town_a"].HousingCapacity + 4, two.State.Cities["town_a"].HousingCapacity);
        Assert.Contains(two.Why, x => x.System == "city" && x.Message == "Town A: a housing project was finished");

        var later = two.State;
        for (int i = 0; i < 3; i++) later = Step(c, later).State;
        Assert.Equal(two.State.Cities["town_a"].HousingCapacity, later.Cities["town_a"].HousingCapacity);   // once, not every turn
    }

    // R-PROJ-003
    [Fact]
    public void AnIndustryProjectAddsJobSlotsOnceWhenDone()
    {
        var c = Town();
        var s = New(c);
        var built = Step(c, Step(c, s, new StartProject("industry", "town_a", 2, 0)).State).State;
        Assert.Equal(s.Cities["town_a"].JobSlots + 3, built.Cities["town_a"].JobSlots);
        Assert.Equal(s.Cities["town_a"].JobSlots + 3, Step(c, built).State.Cities["town_a"].JobSlots);
    }

    // R-PROJ-003, R-HOUSE-001: a project's condition change is added once and kept on the 0 to 1 scale.
    [Theory]
    [InlineData("housing", 0.5, 0.6)]
    [InlineData("housing", 0.95, 1.0)]
    [InlineData("transit_camp", 0.5, 0.4)]
    [InlineData("transit_camp", 0.05, 0.0)]
    public void TheConditionMovesOnceAndStaysFrom0To1(string kind, double before, double after)
    {
        var c = Town();
        var s = New(c);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = x.WithCondition(before);
        s.CityProjects.Add(7, new CityProject(7, kind, "town_a", 1, 1, 1));
        Projects.AdvanceCity(c, s, s.Date.AddMonths(1), new List<Cause>());
        Assert.Equal(after, s.Cities["town_a"].Condition!.Value, 9);
        Projects.AdvanceCity(c, s, s.Date.AddMonths(2), new List<Cause>());
        Assert.Equal(after, s.Cities["town_a"].Condition!.Value, 9);   // a built project is not advanced again
    }

    public static IEnumerable<object[]> Refusals() => new List<object[]>
    {
        new object[] { "unknown kind", "There is no such city project" },
        new object[] { "unknown city", "There is no such city" },
        new object[] { "camp without the policy", "Transit camps need the ma'abarot policy" },
        new object[] { "sea", "This hex is not land" },
        new object[] { "too far", "This hex is more than 1 hex from Town A" },
        new object[] { "a project there", "This hex already has a housing project" },
        new object[] { "a placement there", "This hex already has a transit camp" },
    };

    // R-PROJ-001: each refusal leaves the state as if the order had not been given, Attention refunded.
    [Theory]
    [MemberData(nameof(Refusals))]
    public void TheLimitsAreRefusedAndChangeNothing(string why, string reason)
    {
        var c = Town();
        var s = New(c);
        if (why == "a project there") s.CityProjects.Add(s.NextId, new CityProject(s.NextId++, "housing", "town_a", 1, 1, 2));
        if (why == "a placement there") s.Placements.Add(new Placement(MapActions.TransitCamp, 1, 1, s.Date));
        Command order = why switch
        {
            "unknown kind" => new StartProject("harbour", "town_a", 1, 1),
            "unknown city" => new StartProject("housing", "atlantis", 1, 1),
            "camp without the policy" => new StartProject("transit_camp", "town_a", 1, 1),
            "sea" => Housing(0, 0),
            "too far" => Housing(3, 0),
            _ => Housing(),
        };
        var r = Step(c, s, order);
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-PROJ-001 (Q-PROJ-4): map_actions_per_turn starts a turn, nationally; the next turn starts from zero.
    [Fact]
    public void AThirdStartInOneTurnIsRefused()
    {
        var c = Town();
        var s = New(c);
        s.Attention = 10;
        var two = new Command[] { Housing(1, 1), new StartProject("industry", "town_a", 2, 0) };
        var r = Step(c, s, two.Append(new StartProject("industry", "town_a", 0, 1)).ToArray());
        Assert.Equal("No more than 2 map actions and city projects in one turn", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s, two).State.Hash(), r.State.Hash());
        Assert.Empty(Step(c, r.State, new StartProject("industry", "town_a", 0, 1)).Rejected);
    }

    // R-PROJ-001 (Q-PROJ-4): the cap counts map actions too, so two map actions leave no start for a project.
    [Fact]
    public void TwoMapActionsUseUpTheTurnsStarts()
    {
        var c = Town();
        var s = New(c);
        s.Attention = 10;
        s.Date = new DateTime(1948, 6, 1);   // map_actions_from in the fixture
        var two = new Command[] { new MapAction("housing", 1, 1), new MapAction("housing", 2, 0) };
        var r = Step(c, s, two.Append(new StartProject("industry", "town_a", 0, 1)).ToArray());
        Assert.Equal("No more than 2 map actions and city projects in one turn", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s, two).State.Hash(), r.State.Hash());
    }

    // R-PROJ-001 (Q-MONEY-3): a short Treasury borrows, it is not refused.
    [Fact]
    public void AShortTreasuryBorrows()
    {
        var c = Town();
        var s = New(c);
        s.Treasury = 1;
        var r = Step(c, s, Housing());
        Assert.Empty(r.Rejected);
        Assert.Contains(r.Why, x => x.System == "economy" && x.Message == "Borrowed IL 2.0m to build a housing project");
    }

    // R-PROJ-001 (Q-HOUSE-3, D-D3): the transit camp keeps the ma'abarot policy gate, which carries its sourced date; with the
    // policy it is accepted and, built in one turn, adds its capacity at the end of the same turn.
    [Fact]
    public void ATransitCampOpensWithTheMaabarotPolicy()
    {
        var c = Town();
        var s = New(c);
        var camp = new StartProject("transit_camp", "town_a", 1, 1);
        Assert.Equal("Transit camps need the ma'abarot policy", Assert.Single(Step(c, s, camp).Rejected).Reason);
        s.Policies[MapActions.MaabarotPolicy] = 1;
        var r = Step(c, s, camp);
        Assert.Empty(r.Rejected);
        Assert.Equal(r.State.Date, r.State.CityProjects.Values.Single().BuiltOn);
        Assert.Equal(s.Cities["town_a"].HousingCapacity + 3, r.State.Cities["town_a"].HousingCapacity);
    }

    /// <summary>town_a and town_b (on (2, 1)); only town_b has vineyards, with a fixture source, and the fixture's kinds plus a
    /// winery (IL 2m, 2 turns, +6 job slots) that requires them. Fixture values, not the game's.</summary>
    static Content WineCountry() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
        ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 8, 10, 6)
            + Fixture.City("town_b", 5, 2, 2, 1).Replace("facilities: [] }", Fixture.Vineyards + " }"),
        ["projects.yaml"] = Fixture.GddProjects + Fixture.Winery,
    })));

    // R-PROJ-005: a specialised project is allowed only in a city that has its resource, and is then an industry project.
    [Fact]
    public void AWineryNeedsVineyards()
    {
        var c = WineCountry();
        var s = New(c);
        var refused = Step(c, s, new StartProject("winery", "town_a", 1, 1));
        Assert.Equal("Town A has no vineyards for a winery", Assert.Single(refused.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), refused.State.Hash());

        var built = Step(c, Step(c, s, new StartProject("winery", "town_b", 3, 1)).State);
        Assert.Empty(built.Rejected);
        Assert.Equal(s.Cities["town_b"].JobSlots + 6, built.State.Cities["town_b"].JobSlots);
    }

    // R-PROJ-002 (D-A3): cancelling returns no Money (and, with no workers, no people) and the project adds nothing.
    [Fact]
    public void CancellingReturnsPeopleNotMoney()
    {
        var c = Town();
        var s = New(c);
        var started = Step(c, s, Housing()).State;
        var id = started.CityProjects.Keys.Single();
        var r = Step(c, started, new CancelProject(id));
        Assert.Empty(r.Rejected);
        Assert.Empty(r.State.CityProjects);
        Assert.Equal(Step(c, started).State.Treasury, r.State.Treasury, 9);   // the Money spent is not returned
        Assert.Equal(Step(c, started).State.Cities["town_a"].Civilians, r.State.Cities["town_a"].Civilians);   // no workers to return (Q-PROJ-3)
        Assert.Equal(started.Cities["town_a"].HousingCapacity, Step(c, r.State).State.Cities["town_a"].HousingCapacity);
        Assert.Contains(r.Why, x => x.Message == "Town A: a housing project was cancelled; the Money spent is not returned");
    }

    // R-PROJ-002
    [Fact]
    public void CancellingAnUnknownOrBuiltProjectIsRefused()
    {
        var c = Town();
        var s = New(c);
        Assert.Equal("There is no such city project", Assert.Single(Step(c, s, new CancelProject(42)).Rejected).Reason);
        var built = Step(c, Step(c, s, Housing()).State).State;
        var r = Step(c, built, new CancelProject(built.CityProjects.Keys.Single()));
        Assert.Equal("A housing project is already built", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, built).State.Hash(), r.State.Hash());
    }

    // R-PROJ-001: the legacy loop refuses both orders before charging Attention.
    [Fact]
    public void TheLegacyLoopRefusesTheOrders()
    {
        var c = ContentLoader.Load(Fixture.Write());
        var s = New(c);
        var r = Step(c, s, Housing(), new CancelProject(1));
        Assert.All(r.Rejected, x => Assert.Equal("That order is not available in this game", x.Reason));
        Assert.Equal(2, r.Rejected.Count);
    }

    [Fact]
    public void BothOrdersRoundTripThroughASaveAndReplay()
    {
        var c = Town();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { Housing() });
        g.Step(new Command[] { new CancelProject(g.State.CityProjects.Keys.Single()) });
        var text = g.Save.Write();
        Assert.Contains("\nproject housing town_a 1 1\n", text);
        Assert.Contains("\nunproject ", text);
        Assert.Equal(text, SaveGame.Parse(text).Write());
        Assert.Equal(g.State.Hash(), GameSession.Load(c, text, out _).State.Hash());
    }

    [Fact]
    public void CloneIsIndependentAndTheHashOmitsAnEmptyList()
    {
        var c = Town();
        var s = New(c);
        Assert.DoesNotContain("cproj", s.Describe());   // omit-when-empty (R-LOOP-002's rule)
        var before = s.Hash();
        var copy = s.Clone();
        copy.CityProjects.Add(1, new CityProject(1, "housing", "town_a", 1, 1, 2));
        Assert.Empty(s.CityProjects);
        Assert.Equal(before, s.Hash());
        Assert.Contains("cproj[", copy.Describe());
        Assert.NotEqual(before, copy.Hash());
    }

    // D-A7: the reference player's stated policy starts a housing project, one at a time, in the city with the most overflow.
    [Fact]
    public void TheReferencePlayerStartsHousingWhereTheOverflowIsLargest()
    {
        var c = Town();
        var s = New(c);
        s.Attention = 10;   // the fixture's rung 5 doubles every price
        Assert.DoesNotContain(ReferencePlayer.Commands(c, s), x => x is StartProject);   // no overflow, nothing to build
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = x.WithCounts(x.HousingCapacity + 2, x.Employed, x.Refugees);
        var start = Assert.Single(ReferencePlayer.Commands(c, s).OfType<StartProject>());
        Assert.Equal(("housing", "town_a"), (start.ProjectKind, start.City));
        Assert.Null(Projects.CheckCity(c, s, start.ProjectKind, start.City, start.Q, start.R));
        s.Treasury = 0;   // it borrows, as the reference player's other orders do
        Assert.Single(ReferencePlayer.Commands(c, s).OfType<StartProject>());
        s.Attention = 1;   // but not past the turn's Attention
        Assert.DoesNotContain(ReferencePlayer.Commands(c, s), x => x is StartProject);
        s.Attention = 10;
        s.CityProjects.Add(9, new CityProject(9, "industry", "town_a", 2, 0, 1));   // one at a time
        Assert.DoesNotContain(ReferencePlayer.Commands(c, s), x => x is StartProject);
    }

    /// <summary>town_a on (1, 0) and town_b on (1, 1), next to it, both with the fixture's kinds; town_b has vineyards and the
    /// winery needs them (R-PROJ-005). Fixture values, not the game's.</summary>
    static Content Neighbours(bool lockIndustry = false) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 1, r: 1 }\n",
        ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 8, 10, 6)
            + Fixture.City("town_b", 5, 2, 2, 1).Replace("facilities: [] }", Fixture.Vineyards + " }"),
        ["projects.yaml"] = (lockIndustry ? Fixture.GddProjects.Replace("job_slots: 3 }", "job_slots: 3, locked: true }") : Fixture.GddProjects) + Fixture.Winery,
    })));

    // #1122 (owner ruling 1): the cities whose settlement hex is within map_action_max_distance of the hex, by CheckCity's own
    // range check, nearest first and then in ordinal id order; none for a hex out of every city's reach.
    [Fact]
    public void CitiesForListsTheCitiesInReachNearestFirst()
    {
        var c = Neighbours();
        var s = New(c);
        Assert.Equal(new[] { "town_a" }, Projects.CitiesFor(c, s, 0, 0));
        Assert.Equal(new[] { "town_a", "town_b" }, Projects.CitiesFor(c, s, 1, 0));   // its own hex first
        Assert.Equal(new[] { "town_b", "town_a" }, Projects.CitiesFor(c, s, 1, 1));
        Assert.Equal(new[] { "town_a", "town_b" }, Projects.CitiesFor(c, s, 2, 0));   // a tie: ordinal id order
        Assert.Empty(Projects.CitiesFor(c, s, 5, 0));
        var land = new[] { (1, 0), (2, 0), (1, 1), (3, 0) };
        Assert.All(land, x => Assert.True(c.HexAt(x.Item1, x.Item2)?.Land, "precondition: land, so the validator gets to its distance check"));
        Assert.Empty(Projects.CitiesFor(c, s, 3, 0));   // so both answers are compared below
        foreach (var (q, r) in land)
            foreach (var city in s.Cities.Keys)   // the same answer as the validator's distance refusal, never a second rule
                Assert.Equal(Projects.CitiesFor(c, s, q, r).Contains(city), Projects.CheckCityReason(c, s, "industry", city, q, r)?.Kind != "too_far");
    }

    // #1122 (owner ruling 2): every refusal has a stable kind and invariant values for the UI, and the same English text
    // CheckCity returns.
    [Theory]
    [InlineData("unknown kind", "unknown_kind", "")]
    [InlineData("unknown city", "unknown_city", "")]
    [InlineData("a locked kind", "locked", "industry")]
    [InlineData("camp without the policy", "maabarot_policy", "")]
    [InlineData("no resource", "no_resource", "town_a|vineyards|winery")]
    [InlineData("sea", "not_land", "")]
    [InlineData("too far", "too_far", "1|town_a")]
    [InlineData("a placement there", "occupied", "transit_camp")]
    [InlineData("a project there", "occupied_project", "housing")]
    [InlineData("the turn's cap", "turn_cap", "2")]
    public void EveryRefusalHasAKindAndInvariantValues(string why, string kind, string values)
    {
        var c = Neighbours(lockIndustry: why == "a locked kind");
        var s = New(c);
        if (why == "a project there") s.CityProjects.Add(s.NextId, new CityProject(s.NextId++, "housing", "town_a", 1, 1, 2));
        if (why == "a placement there") s.Placements.Add(new Placement(MapActions.TransitCamp, 1, 1, s.Date));
        if (why == "the turn's cap") { s.MapActionsTurnDate = s.Date; s.MapActionsThisTurn = 2; }
        var (k, city, q, r) = why switch
        {
            "unknown kind" => ("harbour", "town_a", 1, 1),
            "unknown city" => ("housing", "atlantis", 1, 1),
            "a locked kind" => ("industry", "town_a", 1, 1),
            "camp without the policy" => ("transit_camp", "town_a", 1, 1),
            "no resource" => ("winery", "town_a", 1, 1),
            "sea" => ("housing", "town_a", 0, 0),
            "too far" => ("housing", "town_a", 3, 0),
            _ => ("housing", "town_a", 1, 1),
        };
        var reason = Projects.CheckCityReason(c, s, k, city, q, r);
        Assert.NotNull(reason);
        Assert.Equal((kind, values), (reason!.Kind, string.Join("|", reason.Values)));
        Assert.Equal(Projects.CheckCity(c, s, k, city, q, r), reason.Text);
        Assert.Null(Projects.CheckCityReason(c, New(c), "housing", "town_a", 1, 1));
    }

    // #1122 (owner ruling 3): a kind may carry a draft Hebrew name pending a native speaker's review (#564); a review flag
    // other than agent-checked, or one without a name, is refused; a kind without Hebrew still loads.
    [Fact]
    public void AKindMayCarryADraftHebrewName()
    {
        Content Load(string extra) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["projects.yaml"] = Fixture.GddProjects + "  - { id: depot, name: a depot, cost: 1, turns: 1, capacity: 0, condition: 0, job_slots: 1" + extra + " }\n",
        })));
        var c = Load(", name_he: מחסן, name_he_review: agent-checked");
        var depot = c.ProjectKinds.Single(x => x.Id == "depot");
        Assert.Equal(("מחסן", "agent-checked"), (depot.NameHe, depot.NameHeReview));
        Assert.Equal("", c.ProjectKinds.Single(x => x.Id == "housing").NameHe);
        Assert.Contains("name_he_review", Assert.Throws<ContentException>(() => Load(", name_he: מחסן, name_he_review: reviewed")).Message);
        Assert.Contains("name_he_review", Assert.Throws<ContentException>(() => Load(", name_he: מחסן, name_he_review: ''")).Message);   // a blank flag is not "reviewed"
        Assert.Contains("name_he", Assert.Throws<ContentException>(() => Load(", name_he_review: agent-checked")).Message);
    }
}
