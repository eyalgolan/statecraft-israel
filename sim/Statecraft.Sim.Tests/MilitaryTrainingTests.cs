using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Phase 5 PR 3: a recruit cohort becomes one military formation, conserving its population strength.</summary>
public class MilitaryTrainingTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The units after the starting forces: their ids, arrivals' included, are reserved at the start (R-UNIT-009),
    /// so this counts what training and the test added whether or not a dated brigade has arrived yet.</summary>
    static int After(GameState s) => s.Units.Keys.Count(id => id >= Mvp.StartingForces.Count);

    // R-TRAIN-003, R-UNIT-003 (Q-UNIT-1): one cohort, one unit; strength is population units, not a figure count.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void AFinishedRecruitCohortBecomesUnits(int count)
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var cohortId = s.NextId++;
        s.Cohorts.Add(cohortId, new Cohort(cohortId, "recruit", "tel_aviv", count, 1, count));
        var next = s.NextId;
        var arms = s.Arms;
        Training.Progress(Mvp, s, new List<Cause>());

        Assert.Equal(1, After(s));
        var unit = s.Units[next];
        var home = Mvp.SettlementHexes.Single(h => h.Id == "tel_aviv");
        Assert.Equal((next, "recruit", "israel", count, home.Q, home.R, 0),
            (unit.Id, unit.Type, unit.Side, unit.Strength, unit.Q, unit.R, unit.Service));
        Assert.Equal("tel_aviv", unit.HomeCity);
        Assert.Equal(next + 1, s.NextId);
        Assert.Empty(s.Cohorts);
        Assert.Equal(0, s.Cities["tel_aviv"].Recruits);
        Assert.Equal(arms, s.Arms); // Completing neither charges again nor returns the Arms reserved at the start.
    }

    // R-HOUSE-003, R-TRAIN-003: Q-HOUSE-5 still counts recruits after they leave their training cohort.
    [Fact]
    public void AGraduatedRecruitCohortRemainsHousedInItsCity()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var city = s.Cities["tel_aviv"].WithCounts(0, 0, 0).WithRecruits(0).WithCraftsmen(0);
        s.Cities[city.Id] = city;
        var id = s.NextId++;
        s.Cohorts.Add(id, new Cohort(id, "recruit", city.Id, 3, 1, 3));
        Assert.Equal(3, Cities.Housed(s, city));

        Training.Progress(Mvp, s, new List<Cause>());

        Assert.Empty(s.Cohorts);
        Assert.Equal(1, After(s));
        Assert.Equal(3, Assert.Single(s.Units.Values, u => u.Type == "recruit").Strength);
        Assert.Equal(3, Cities.Housed(s, s.Cities[city.Id]));
    }

    // R-HOUSE-003: formations belong to their training city's housing, not their current hex.
    [Fact]
    public void AMovingFormationKeepsItsHomeHousingThroughOrdersAndPromotion()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var city = s.Cities["tel_aviv"].WithCounts(0, 0, 0).WithRecruits(0).WithCraftsmen(0);
        s.Cities[city.Id] = city;
        var id = s.NextId++;
        s.Cohorts.Add(id, new Cohort(id, "recruit", city.Id, 3, 1, 3));
        var why = new List<Cause>();
        Training.Progress(Mvp, s, why);
        var unit = Assert.Single(s.Units.Values, u => u.Type == "recruit");
        var target = Movement.Reachable(Mvp, s, unit.Id).First(h => h.Value > 0).Key;
        Assert.Null(Movement.Move(Mvp, s, unit.Id, target.Q, target.R, new Dictionary<int, int>(), why));
        Assert.Equal(city.Id, s.Units[unit.Id].HomeCity);
        Assert.Equal(3, Cities.Housed(s, city));
        Assert.Null(Orders.Give(Mvp, s, unit.Id, Orders.Fortify, why));
        Assert.Equal(city.Id, s.Units[unit.Id].HomeCity);
        Orders.Settle(s, why);
        Assert.Equal(Orders.Fortify, s.Units[unit.Id].Order);
        Assert.Equal(city.Id, s.Units[unit.Id].HomeCity);
        var rules = Mvp.RulesAt(s.Date).Mvp!;
        for (int turn = 0; turn < rules.VeteranSoldierService!.Value; turn++)
        {
            Military.Serve(s, rules, why);
            Assert.Equal(city.Id, s.Units[unit.Id].HomeCity);
            Assert.Equal(3, Cities.Housed(s, city));
        }
        Assert.Equal("veteran_soldier", s.Units[unit.Id].Type);
    }

    // R-HOUSE-003: another city, an enemy formation and an unknown home do not fill this city's homes.
    [Fact]
    public void HousingCountsOnlyTheIsraeliFormationStrengthWithThatHome()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var city = s.Cities["tel_aviv"].WithCounts(0, 0, 0).WithRecruits(0).WithCraftsmen(0);
        s.Cities[city.Id] = city;
        var homeId = s.NextId++;
        var otherCityId = s.NextId++;
        var enemyId = s.NextId++;
        var unknownHomeId = s.NextId++;
        s.Units.Add(homeId, new Unit(homeId, "recruit", 0, 0, side: "israel", strength: 3, homeCity: city.Id));
        s.Units.Add(otherCityId, new Unit(otherCityId, "veteran_soldier", 0, 0, side: "israel", strength: 5, homeCity: "jerusalem"));
        s.Units.Add(enemyId, new Unit(enemyId, "enemy", 0, 0, side: "egypt", strength: 7, homeCity: city.Id));
        s.Units.Add(unknownHomeId, new Unit(unknownHomeId, "recruit", 0, 0, side: "israel", strength: 11));
        Assert.Equal(4, After(s));
        Assert.Equal(3, Cities.Housed(s, city));
    }

    // R-HOUSE-003: the established exception for halutz units remains unchanged.
    [Fact]
    public void AGraduatedPioneerCohortLeavesCityHousing()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var home = Mvp.Cities.Single(c => c.Facilities.Contains("hakhshara")).Id;
        var city = s.Cities[home].WithCounts(0, 0, 0).WithRecruits(0).WithCraftsmen(0);
        s.Cities[home] = city;
        var id = s.NextId++;
        s.Cohorts.Add(id, new Cohort(id, "halutz", home, 3, 1));
        Assert.Equal(3, Cities.Housed(s, city));
        Training.Progress(Mvp, s, new List<Cause>());
        Assert.Equal(3, After(s));
        Assert.All(s.Units.Values, u => Assert.Empty(u.HomeCity));
        Assert.Equal(0, Cities.Housed(s, city));
    }

    // R-UNIT-003: ascending completion order and the same people after differently sized cohorts finish.
    [Fact]
    public void NoRecruitIsLostOrDoubled()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var starting = s.Units.Values.Select(u => u.Canonical()).ToArray();
        var cohort = s.NextId;
        s.Cohorts.Add(cohort + 2, new Cohort(cohort + 2, "recruit", "tel_aviv", 4, 1, 4));
        s.Cohorts.Add(cohort, new Cohort(cohort, "recruit", "tel_aviv", 2, 1, 2));
        s.Cohorts.Add(cohort + 1, new Cohort(cohort + 1, "recruit", "tel_aviv", 1, 2, 1));
        s.NextId += 3;
        var firstUnit = s.NextId;
        const int people = 7;
        for (var turn = 0; turn < 2; turn++)
        {
            Training.Progress(Mvp, s, new List<Cause>());
            Assert.Equal(people, s.Cohorts.Values.Sum(k => k.Count) + s.Units.Values.Where(u => u.Id >= firstUnit).Sum(u => u.Strength));
            Assert.Equal(starting, s.Units.Values.Where(u => u.Id < cohort).Select(u => u.Canonical()));
            Assert.Equal(0, s.Cities["tel_aviv"].Recruits);
        }
        var recruits = s.Units.Values.Where(u => u.Id >= firstUnit).ToArray();
        Assert.Equal(3, After(s));
        Assert.Equal(new[] { firstUnit, firstUnit + 1, firstUnit + 2 }, recruits.Select(u => u.Id));
        Assert.Equal(new[] { 2, 4, 1 }, recruits.Select(u => u.Strength));
        Assert.Equal(firstUnit + 3, s.NextId);
    }

    // R-TRAIN-003, R-UNIT-003: exercise the real resolver and save replay, not a manually serialised unit.
    [Fact]
    public void ACompletedRecruitUnitSurvivesCommandLogReplay()
    {
        var g = GameSession.New(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Empty(g.Step(new Command[] { new Train("tel_aviv", "recruit", 3) }).Rejected);
        Assert.Single(g.State.Cohorts);
        g.Step(new Command[0]);
        Assert.Single(g.State.Cohorts);
        g.Step(new Command[0]);

        Assert.Equal(1, After(g.State));
        var unit = Assert.Single(g.State.Units.Values, u => u.HomeCity == "tel_aviv");
        Assert.Equal(("recruit", "israel", 3, 0), (unit.Type, unit.Side, unit.Strength, unit.Service));
        Assert.Equal(0, g.State.Cities["tel_aviv"].Recruits);
        var loaded = GameSession.Load(Mvp, g.Save.Write(), out var warning);
        Assert.Empty(warning);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
        Assert.Equal("tel_aviv", loaded.State.Units[unit.Id].HomeCity);
        Assert.Equal(unit.Canonical(), loaded.State.Units[unit.Id].Canonical());
    }

    // #1130 owner decision: a new recruit has three movement points; movement and service replay together.
    [Fact]
    public void AGraduatedRecruitMovesWithinItsBudgetAndReplays()
    {
        var g = GameSession.New(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Empty(g.Step(new Command[] { new Train("tel_aviv", "recruit", 3) }).Rejected);
        for (int turn = 1; turn < Mvp.UnitTypes.Single(t => t.Id == "recruit").TrainingTurns; turn++) g.Step(new Command[0]);
        var recruit = Assert.Single(g.State.Units.Values, u => u.Type == "recruit");
        var target = Movement.Reachable(Mvp, g.State, recruit.Id).First(h => h.Value == 3).Key;

        var step = g.Step(new Command[] { new Move(recruit.Id, target.Q, target.R), new Move(recruit.Id, recruit.Q, recruit.R) });
        Assert.Equal("That hex is out of reach this turn", Assert.Single(step.Rejected).Reason);
        var moved = g.State.Units[recruit.Id];
        Assert.Equal((target.Q, target.R, "recruit", 3, 1), (moved.Q, moved.R, moved.Type, moved.Strength, moved.Service));
        var loaded = GameSession.Load(Mvp, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
        Assert.Equal("tel_aviv", moved.HomeCity);
        Assert.Equal("tel_aviv", loaded.State.Units[recruit.Id].HomeCity);
        Assert.Equal(moved.Canonical(), loaded.State.Units[recruit.Id].Canonical());
    }

    // R-UNIT-004: six service turns is owner accepted; training time is not service.
    [Fact]
    public void ARecruitPromotesAfterServiceAndKeepsItsFormation()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "recruit", 3, 4, 4, "israel", 3, "hold", "8"));
        var why = new List<Cause>();
        TurnResolver.Resolve(Mvp, s, s.Date.AddDays(7), 7 / 365.0, why);
        Assert.Equal(("recruit", 5), (s.Units[id].Type, s.Units[id].Service));
        TurnResolver.Resolve(Mvp, s, s.Date.AddDays(7), 7 / 365.0, why);
        var veteran = s.Units[id];
        Assert.Equal((id, "veteran_soldier", 3, 4, 6, "israel", 3, "hold", "8"),
            (veteran.Id, veteran.Type, veteran.Q, veteran.R, veteran.Service, veteran.Side, veteran.Strength, veteran.Order, veteran.OrderTarget));
        Assert.Single(why, x => x.Message.Contains("veteran soldiers"));
        TurnResolver.Resolve(Mvp, s, s.Date.AddDays(7), 7 / 365.0, why);
        Assert.Equal(veteran.Canonical(), s.Units[id].Canonical());
    }

    // R-UNIT-004: service, promotion and upkeep must all replay through the existing command-log save schema.
    [Fact]
    public void ATrainedVeteranSurvivesReplay()
    {
        var g = GameSession.New(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Empty(g.Step(new Command[] { new Train("tel_aviv", "recruit", 3) }).Rejected);
        for (int turn = 1; turn < Mvp.UnitTypes.Single(t => t.Id == "recruit").TrainingTurns; turn++) g.Step(new Command[0]);
        var threshold = Mvp.Rules.Mvp!.VeteranSoldierService!.Value;
        for (int service = 1; service <= threshold; service++)
        {
            g.Step(new Command[0]);
            var u = Assert.Single(g.State.Units.Values, x => x.HomeCity == "tel_aviv");
            Assert.Equal("tel_aviv", u.HomeCity);
            Assert.Equal(service, u.Service);
            Assert.Equal(service < threshold ? "recruit" : "veteran_soldier", u.Type);
        }
        var loaded = GameSession.Load(Mvp, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
        Assert.Equal(g.State.Describe(), loaded.State.Describe());
    }

    // R-UNIT-001, R-UNIT-004: content must not create a recruit that cannot pay upkeep or promotes to an unknown type.
    [Theory]
    [InlineData("side: israel, class: infantry, facility: military_camp", "facility: military_camp", "types[recruit].side")]
    [InlineData("  - { id: veteran_soldier, kind: unit, side: israel, class: infantry, movement_points: 3 }\n", "", "types[veteran_soldier]")]
    public void TrainingRequiresTheMilitaryTypes(string oldText, string newText, string field)
    {
        var files = Fixture.Gdd();
        Assert.Contains(oldText, files["unit_types.yaml"]);
        files["unit_types.yaml"] = files["unit_types.yaml"].Replace(oldText, newText);
        var e = Assert.Throws<ContentException>(() => ContentLoader.Load(Fixture.Write(files)));
        Assert.Equal("unit_types.yaml", e.File);
        Assert.Equal(field, e.Field);
    }
}
