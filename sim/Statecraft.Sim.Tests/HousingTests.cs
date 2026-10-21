using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>GDD Phase 3 PR 3 (#734): housing capacity, condition and overflow in step 8 of the turn (D-D1, D-D2; Q-HOUSE-1,
/// Q-HOUSE-2, Q-HOUSE-4, Q-HOUSE-5, Q-START-3, owner, 15 October 2026, "Take all recommendations", #375). The fixture's
/// values (start condition 0.5, a loss of 0.2 at a full overflow share) are fixture values, not the game's.</summary>
public class HousingTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The GDD fixture with the given cities.yaml rows (default: town_a, housing for 12, 20 jobs, 10 civilians, 6 employed).</summary>
    static Content Town(params string[] rows) =>
        ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["cities.yaml"] = "cities:\n" + (rows.Length == 0 ? Fixture.City("town_a", 12, 20, 10, 6, "[military_camp, hakhshara]") : string.Concat(rows)),
        })));

    static GameState With(GameState s, string city, int civilians, int employed, int refugees)
    {
        var x = s.Clone();
        x.Cities[city] = x.Cities[city].WithCounts(civilians, employed, refugees);
        return x;
    }

    static GameState Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands).State;

    /// <summary>Everyone the city holds: civilians, refugees, recruits and the groups in training there.</summary>
    static int People(GameState s, string city) =>
        s.Cities[city].Civilians + s.Cities[city].Craftsmen + s.Cities[city].Refugees + s.Cities[city].Recruits + s.Cohorts.Values.Where(k => k.City == city).Sum(k => k.Count);

    // R-HOUSE-001
    [Fact]
    public void EveryCityStartsAtTheOneStartCondition()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var start = Mvp.Rules.Mvp!.HousingStartCondition!.Value;
        Assert.Equal(0.35, start);
        Assert.Equal(12, s.Cities.Count);
        Assert.All(s.Cities.Values, x => Assert.Equal(start, x.Condition));
        // Q-START-3: the start is model.yaml's housing base, so the cities start where the national level does.
        Assert.Equal(Mvp.Indicators.Single(i => i.Id == "housing").Base, start);
    }

    // R-CITY-004, R-HOUSE-002 (review focus 4: overflow that makes people vanish or appear)
    [Fact]
    public void OverflowIsCountedNotLost()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        var after = Step(c, s);   // the fixture's first week brings no unit of arrivals
        var x = after.Cities["town_a"];
        Assert.Equal((15, 15, 0), (x.Civilians, x.Employed, x.Refugees));   // no hard cap: every refugee is housed (and R-CITY-009 fills 15 of the 20 jobs)
        Assert.Equal(People(s, "town_a"), People(after, "town_a"));
        Assert.Equal(15, Cities.Housed(after, x));
        Assert.Equal(3, Cities.Overflow(after, x));                     // 15 people in housing for 12
        Assert.Contains(Simulation.Step(c, s, Array.Empty<Command>()).Why, w => w.System == "city" && w.Target == "town_a" && w.Message == "Town A: 3 groups more than its homes hold");
    }

    // R-HOUSE-002
    [Fact]
    public void OverflowLowersCondition()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        Assert.Equal(0.5, s.Cities["town_a"].Condition);
        var after = Step(c, s);
        Assert.Equal(0.5 - 0.2 * 3 / 15.0, after.Cities["town_a"].Condition!.Value, 12);   // the loss times 3 over 15 people
        Assert.Equal(after.Cities["town_a"].Condition, Step(c, With(after, "town_a", 12, 6, 0)).Cities["town_a"].Condition);   // no overflow, no change
    }

    // R-HOUSE-002: a city record without a condition still reports its overflow, and gains no condition.
    [Fact]
    public void OverflowIsReportedWhenACityHasNoCondition()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        var x = s.Cities["town_a"];
        s.Cities["town_a"] = new City(x.Id, x.HousingCapacity, x.JobSlots, x.Civilians, x.Employed, x.Refugees);
        var r = Simulation.Step(c, s, Array.Empty<Command>());
        Assert.Null(r.State.Cities["town_a"].Condition);
        Assert.Contains(r.Why, w => w.System == "city" && w.Target == "town_a" && w.Message == "Town A: 3 groups more than its homes hold");
    }

    // R-HOUSE-002
    [Fact]
    public void ConditionNeverFallsBelowZero()
    {
        var c = Town(Fixture.City("town_a", 0, 20, 10, 6));   // no housing: all of the people are overflow
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var seen = new List<double>();
        for (int i = 0; i < 4; i++) { s = Step(c, s); seen.Add(s.Cities["town_a"].Condition!.Value); }
        Assert.Equal(0.3, seen[0], 12);
        Assert.Equal(0.1, seen[1], 12);
        Assert.Equal(new[] { 0.0, 0.0 }, seen.Skip(2));
    }

    /// <summary>D-A6: the condition reads the counts after this turn's housing. Read before it, the 10 civilians fit the
    /// housing for 12 and the condition would not move.</summary>
    // R-HOUSE-002, R-CITY-004
    [Fact]
    public void ConditionReadsTheCountsAfterThisTurnsHousing()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        Assert.True(Step(c, s).Cities["town_a"].Condition < 0.5);
    }

    // R-HOUSE-001
    [Fact]
    public void ConditionIsPerCity()
    {
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
            ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
                + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 20, 10, 6) + Fixture.City("town_b", 12, 20, 10, 6),
        })));
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        var after = Step(c, s);
        Assert.True(after.Cities["town_a"].Condition < 0.5);
        Assert.Equal(0.5, after.Cities["town_b"].Condition);
    }

    // R-HOUSE-003 (Q-HOUSE-5): people in training count against their city's housing.
    [Fact]
    public void TraineesTakeHousing()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 4);
        var after = Step(c, s, new Train("town_a", "recruit", 2));   // 8 civilians and 2 in training, then 4 housed
        var x = after.Cities["town_a"];
        Assert.Equal((12, 0), (x.Civilians, x.Refugees));
        Assert.Equal(2, after.Cohorts.Values.Single().Count);
        Assert.Equal(14, Cities.Housed(after, x));
        Assert.Equal(2, Cities.Overflow(after, x));
    }

    // R-HOUSE-003 (Q-HOUSE-5): recruits count against the city they trained in.
    [Fact]
    public void RecruitsTakeHousing()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Cities["town_a"] = s.Cities["town_a"].WithRecruits(3);
        var after = Step(c, s);
        var x = after.Cities["town_a"];
        Assert.Equal(13, Cities.Housed(after, x));   // 10 civilians and 3 recruits in housing for 12
        Assert.Equal(1, Cities.Overflow(after, x));
        Assert.Equal(0.5 - 0.2 * 1 / 13.0, x.Condition!.Value, 12);
    }

    // R-HOUSE-003 (Q-HOUSE-5): halutz units are movable (D-A2) and leave the city's housing when their training ends.
    [Fact]
    public void HalutzUnitsDoNotTakeHousing()
    {
        var c = Town(Fixture.City("town_a", 9, 20, 10, 6, "[military_camp, hakhshara]"));
        var s = Step(c, Simulation.NewGame(c, 1, Mode.Historical), new Train("town_a", "halutz", 2));
        Assert.Equal(10, Cities.Housed(s, s.Cities["town_a"]));   // 8 civilians and 2 in training
        while (s.Cohorts.Count > 0) s = Step(c, s);
        Assert.Equal(2, s.Units.Count);
        Assert.Equal(0, s.Cities["town_a"].Recruits);
        Assert.Equal(s.Cities["town_a"].Civilians, Cities.Housed(s, s.Cities["town_a"]));   // the two units hold no housing
    }

    /// <summary>Two towns, A and B, with housing for 100 each (no overflow) and the given civilians. Fixture values.</summary>
    static Content TwoTowns(int a, int b, int capacityA = 100) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
        ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", capacityA, 20, a, 0) + Fixture.City("town_b", 100, 20, b, 0),
    })));

    static Indicator Housing(Content c) => c.Indicators.Single(i => i.Id == "housing");

    /// <summary>Decided (owner, 17 Oct 2026, option (a) of Q-PR3-A, relayed on #855 at 06:33Z,
    /// https://github.com/eyalgolan/statecraft-israel/pull/855#issuecomment-6230731177): the national housing base is the
    /// people-weighted mean of city condition. Town A houses 10 at condition 0.5, town B 30 at 0.2:
    /// (0.5 * 10 + 0.2 * 30) / 40 = 0.275. The plain mean would be 0.35, and the fixture's base is 0.3. The fixture has no
    /// effect on housing, so the inner level is the mean itself.</summary>
    // R-HOUSE-004
    [Fact]
    public void TheNationalHousingLevelIsThePeopleWeightedMeanCondition()
    {
        var c = TwoTowns(10, 30);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Cities["town_b"] = s.Cities["town_b"].WithCondition(0.2);
        Assert.Equal(0.275, Cities.HousingLevel(s)!.Value, 12);
        Assert.Equal(0.275, EffectsSystem.Inner(c, s, Housing(c)), 12);
        s.Cohorts.Add(99, new Cohort(99, "recruit", "town_a", 10, 1));   // people in training weigh as housed people (R-HOUSE-003)
        Assert.Equal((0.5 * 20 + 0.2 * 30) / 50, EffectsSystem.Inner(c, s, Housing(c)), 12);
    }

    // R-HOUSE-004: policy, map actions and events still act on top of the cities' mean.
    [Fact]
    public void ShiftsStillActOnTopOfTheCitiesMean()
    {
        var c = TwoTowns(10, 30);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Cities["town_b"] = s.Cities["town_b"].WithCondition(0.2);
        Assert.Equal(0.1, EffectsSystem.Shift(c, s, "housing", 0.1), 12);
        Assert.Equal(0.375, EffectsSystem.Level(c, s, Housing(c)), 12);
        s.Cities["town_b"] = s.Cities["town_b"].WithCondition(0.0);   // the cities move under a standing shift
        Assert.Equal(0.5 * 10 / 40 + 0.1, EffectsSystem.Level(c, s, Housing(c)), 12);
    }

    // R-HOUSE-004: with nobody in the cities' housing the mean is undefined, and the level reads model.yaml's base.
    [Fact]
    public void WithNobodyHousedTheLevelReadsTheBase()
    {
        var c = TwoTowns(0, 0);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Null(Cities.HousingLevel(s));
        Assert.Equal(0.3, EffectsSystem.Inner(c, s, Housing(c)), 12);
    }

    /// <summary>Plan:303: two states differing only in one city's capacity give `housing` in the stated order. Under the
    /// owner's option (a) capacity reaches the level through overflow and condition, so the smaller capacity, with more
    /// overflow, gives the lower level after a turn.</summary>
    // R-HOUSE-004, R-HOUSE-002
    [Fact]
    public void TheNationalHousingLevelFollowsTheCities()
    {
        double After(int capacityA) { var c = TwoTowns(30, 30, capacityA); return Simulation.Step(c, Simulation.NewGame(c, 1, Mode.Historical), Array.Empty<Command>()).State.Indicators["housing"]; }
        Assert.True(After(20) < After(25));
        Assert.True(After(25) < After(30));
        Assert.Equal(After(30), After(40));   // no overflow either way: the same level
        Assert.Equal(0.5, After(30), 12);
    }

    /// <summary>The chain the owner set (17 October 2026; R-HOUSE-002 through R-HOUSE-004 and homes_lift_morale): two
    /// states that differ only in town A's capacity, with model.yaml's link from housing to morale (k 0.15, inertia 2, as in
    /// the real content). More overflow gives a lower condition, a lower housing level and a lower morale, which is Social
    /// cohesion (R-NAT-001). Everything else, the employed share included, is the same in both.</summary>
    // R-HOUSE-002, R-HOUSE-004
    [Fact]
    public void OverflowLowersSocialCohesionThroughTheHousingLevel()
    {
        Content Chain(int capacityA) => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
            ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
                + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", capacityA, 20, 30, 10) + Fixture.City("town_b", 100, 20, 30, 10),
            ["model.yaml"] = Fixture.Files["model.yaml"].Replace("effects:\n", "effects:\n  - { id: homes_lift_morale, source: housing, target: morale, k: 0.15, inertia: 2, name: Homes lift morale }\n"),
        })));
        GameState Play(Content c) { var s = Simulation.NewGame(c, 1, Mode.Historical); for (int i = 0; i < 3; i++) s = Simulation.Step(c, s, Array.Empty<Command>()).State; return s; }
        var crowded = Play(Chain(20));   // 10 of town A's 30 beyond its housing
        var housed = Play(Chain(30));    // the same people, all housed
        Assert.Equal(Cities.EmployedShare(housed), Cities.EmployedShare(crowded));
        Assert.True(crowded.Cities["town_a"].Condition < housed.Cities["town_a"].Condition);
        Assert.Equal(housed.Cities["town_b"].Condition, crowded.Cities["town_b"].Condition);
        Assert.True(crowded.Indicators["housing"] < housed.Indicators["housing"]);
        Assert.True(crowded.Indicators["morale"] < housed.Indicators["morale"]);
    }

    // R-HOUSE-004 (Q-START-3): the real cities start where the national level did, so the first day's housing is unchanged.
    [Fact]
    public void TheRealGameStartsAtTheSameHousingLevel()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(0.35, Cities.HousingLevel(s)!.Value, 12);
        Assert.Equal(0.35, s.Indicators["housing"], 12);
    }

    // R-LOOP-002, R-HOUSE-001: the condition reaches the hash, and a city record made without one hashes as before.
    [Fact]
    public void TheConditionReachesTheHash()
    {
        var none = new City("a", 1, 2, 3, 1, 0);
        Assert.Equal("a=1,2,3,1,0", none.Canonical());
        Assert.Equal("a=1,2,3,1,0,c3FD6666666666666", none.WithCondition(0.35).Canonical());
        Assert.Equal("a=1,2,3,1,0,4,c3FE0000000000000", none.WithRecruits(4).WithCondition(0.5).Canonical());
        Assert.Equal("a=1,2,3,1,0,c0.35 [3FD6666666666666]", none.WithCondition(0.35).Canonical(true));
        Assert.Equal(0.35, none.WithCondition(0.35).WithCounts(5, 1, 0).WithRecruits(2).Condition);   // other changes keep it
        var a = new GameState(); a.Cities["a"] = none.WithCondition(0.35);
        var b = new GameState(); b.Cities["a"] = none.WithCondition(0.36);
        Assert.NotEqual(a.Hash(), b.Hash());
    }

    // R-HOUSE-001, R-HOUSE-002: both values are required, from 0 to 1, so a missing key is never read as 0.
    [Theory]
    [InlineData("  housing_start_condition: 0.5\n", "", "mvp.housing_start_condition", "required")]
    [InlineData("  overflow_condition_loss: 0.2\n", "", "mvp.overflow_condition_loss", "required")]
    [InlineData("  housing_start_condition: 0.5\n", "  housing_start_condition: 1.5\n", "mvp.housing_start_condition", "from 0 to 1")]
    [InlineData("  overflow_condition_loss: 0.2\n", "  overflow_condition_loss: -0.1\n", "mvp.overflow_condition_loss", "from 0 to 1")]
    [InlineData("  housing_start_condition: 0.5\n", "  housing_start_condition: -0.1\n", "mvp.housing_start_condition", "from 0 to 1")]
    [InlineData("  overflow_condition_loss: 0.2\n", "  overflow_condition_loss: 1.5\n", "mvp.overflow_condition_loss", "from 0 to 1")]
    public void TheHousingValuesAreRequiredAndInRange(string line, string replacement, string field, string reason)
    {
        var files = Fixture.Gdd();
        Assert.Contains(line, files["rules.yaml"]);
        files["rules.yaml"] = files["rules.yaml"].Replace(line, replacement);
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Contains(field, e.Message);
        Assert.Contains(reason, e.Message);
    }
}
