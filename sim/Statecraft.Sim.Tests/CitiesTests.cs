using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD slice's city (Phase 1 PR 3): AssignWorkers (R-CITY-001), and the turn's production, housing, Social
/// cohesion and arrivals in TurnResolver (R-CITY-002 to R-CITY-005, in the order of spec 2026-10-12 section 2.3, D-A6).</summary>
public class CitiesTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The GDD fixture with one city: 20 jobs, 10 civilians, 6 of them employed, housing for 12.</summary>
    static Content Town(string? city = null) =>
        ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string> { ["cities.yaml"] = "cities:\n" + (city ?? Fixture.City("town_a", 12, 20, 10, 6)) })));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    [Fact]
    public void ANewGddGameStartsWithItsCities()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Equal(Mvp.Cities.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal), s.Cities.Keys);
        var city = s.Cities["tel_aviv"];   // the census start of Tel Aviv-Yafo (R-CITY-007)
        Assert.Equal(("tel_aviv", 248, 106, 248, 106, 0), (city.Id, city.HousingCapacity, city.JobSlots, city.Civilians, city.Employed, city.Refugees));
    }

    [Fact]
    public void ALegacyGameHasNoCities() =>   // the legacy loop lives on only in test fixtures since the cutover
        Assert.Empty(Simulation.NewGame(ContentLoader.Load(Fixture.Write()), GoldenRun.Seed, Mode.Historical).Cities);

    // R-CITY-001
    [Fact]
    public void AssigningWorkersSetsEmployed()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var r = Step(Mvp, s, new AssignWorkers("tel_aviv", "job", 90));
        Assert.Empty(r.Rejected);
        Assert.Equal(90, r.State.Cities["tel_aviv"].Employed);
        Assert.Contains(r.Why, x => x.System == "city" && x.Message == "Tel Aviv: 90 groups in work");
    }

    // R-CITY-001
    [Theory]
    [InlineData("town_a", "job", -1, "The number of workers cannot be negative")]
    [InlineData("town_a", "job", 21, "Town A has 20 jobs")]
    [InlineData("town_a", "job", 11, "Town A has 10 civilians")]
    [InlineData("nowhere", "job", 1, "There is no such city")]
    [InlineData("town_a", "craftsman", 1, "There is no such role")]
    public void TheLimitsAreRefusedAndChangeNothing(string city, string role, int count, string reason)
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new AssignWorkers(city, role, count));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());   // as if the order had not been given: Attention refunded too
    }

    // R-CITY-001
    [Fact]
    public void TheLegacyLoopRefusesTheOrderAndRefundsAttention()
    {
        var c = ContentLoader.Load(Fixture.Write());   // the legacy loop (a fixture since the cutover)
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var r = Step(c, s, new AssignWorkers("town_a", "job", 1));
        Assert.Equal("That order is not available in this game", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-CITY-001
    [Fact]
    public void AnAssignOrderSurvivesASave()
    {
        var g = GameSession.New(Mvp, GoldenRun.Seed, Mode.Historical);
        g.Step(new Command[] { new AssignWorkers("tel_aviv", "job", 85) });
        var text = g.Save.Write();
        Assert.Contains("\nwork tel_aviv job 85\n", text);
        Assert.Equal(g.State.Hash(), GameSession.Load(Mvp, text, out _).State.Hash());
    }

    // ---- the turn (TurnResolver): production, housing then Social cohesion, arrivals (spec section 2.3, D-A6) ----

    static GameState With(GameState s, string city, int civilians, int employed, int refugees)
    {
        var x = s.Clone();
        x.Cities[city] = x.Cities[city].WithCounts(civilians, employed, refugees);
        return x;
    }

    // R-CITY-002
    [Fact]
    public void ProductionPaysPerEmployedGroup()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var paid = Step(c, s).State.Treasury - Step(c, With(s, "town_a", 10, 0, 0)).State.Treasury;
        Assert.Equal(6 * 1.2 * SimTime.YearFraction(TurnLength.Week), paid, 9);   // the fixture's first turn is a week
    }

    // R-CITY-004
    [Fact]
    public void RefugeesAreHousedOnlyUpToCapacity()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        var city = Step(c, s).State.Cities["town_a"];   // the first week brings under one unit of arrivals
        Assert.Equal((12, 6, 3), (city.Civilians, city.Employed, city.Refugees));
    }

    // R-CITY-003, R-CITY-004 (D-A6: arrivals come after housing, so they are housed on the next turn)
    [Fact]
    public void ATurnsArrivalsAreHousedOnTheNextTurn()
    {
        // Tel Aviv starts full (Q-START-1: housing equals the start people), so 20 of its people are moved out to free room.
        var s = With(Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical), "tel_aviv", 228, 106, 0);
        var one = Step(Mvp, s).State;
        var arrived = one.Cities["tel_aviv"].Refugees;
        Assert.True(arrived > 0);
        Assert.Equal(228, one.Cities["tel_aviv"].Civilians);   // housing for 20 more was free, but it ran before they came
        var two = Step(Mvp, one).State.Cities["tel_aviv"];
        Assert.Equal(228 + Math.Min(arrived, 20), two.Civilians);
    }

    // R-CITY-003
    [Fact]
    public void ArrivalsOverTheWholeRunEqualTheFloorOfTheTotal()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var start = s.Date;
        int People(GameState x) => x.Cities["tel_aviv"].Civilians + x.Cities["tel_aviv"].Refugees;
        var before = People(s);
        for (int i = 0; i < 20; i++)
        {
            var next = Step(Mvp, s).State;
            Assert.Equal(Cities.ArrivedUnits(Mvp, s.Date, next.Date, 1000), People(next) - People(s));
            s = next;
        }
        var total = PopulationSystem.Arrivals(Mvp, start, s.Date).OrderBy(kv => kv.Key, StringComparer.Ordinal).Sum(kv => kv.Value);
        Assert.Equal((int)Math.Floor(total / 1000), People(s) - before);
    }

    /// <summary>Before cutover the same arrivals are in two views: the legacy head count by origin (GameState.Population, which
    /// the legacy economy reads) and the city's refugees. This pins that overlap so Phase 3, which makes arrivals counts, has
    /// to change it on purpose.</summary>
    // R-CITY-003
    [Fact]
    public void BeforeCutoverArrivalsAreInTheLegacyHeadCountToo()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        for (int i = 0; i < 20; i++) s = Step(c, s).State;
        var people = PopulationSystem.Arrivals(c, c.Eras[0].From, s.Date).Values.Sum();
        Assert.Equal(Cities.ArrivedUnits(c, c.Eras[0].From, s.Date, 10), s.Cities["town_a"].Refugees + s.Cities["town_a"].Civilians - 10);
        Assert.True(s.Population["europe"] >= people, "the legacy head count also holds every arrival");
    }

    // R-CITY-005
    [Fact]
    public void CohesionFallsWhenRefugeesWaitAndRisesWhenTheyAreHousedAndEmployed()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(0.5 * 1 + 0.5 * 0.6, s.Cohesion, 12);                       // all housed, 6 of 10 employed
        var waiting = Step(c, With(s, "town_a", 10, 6, 10)).State;               // 2 housed: 12 of 20 housed, 6 of 12 employed
        Assert.Equal(0.5 * 0.6 + 0.5 * 0.5, waiting.Cohesion, 12);
        var working = Step(c, waiting, new AssignWorkers("town_a", "job", 12)).State;
        Assert.Equal(0.5 * 0.6 + 0.5 * 1, working.Cohesion, 12);
        Assert.True(waiting.Cohesion < s.Cohesion && working.Cohesion > waiting.Cohesion);
    }

    /// <summary>The GDD fixture's town with cohesion weights 0.8 on housing and 0.2 on jobs (fixture values), so a change in
    /// either share shows apart from the other.</summary>
    static Content Unequal() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 20, 10, 6),
        ["rules.yaml"] = Fixture.Files["rules.yaml"].Replace("map_action: 1 }", Fixture.GddCosts)
            + Fixture.GddRules.Replace("cohesion_housing_weight: 0.5", "cohesion_housing_weight: 0.8").Replace("cohesion_jobs_weight: 0.5", "cohesion_jobs_weight: 0.2"),
    })));

    // R-CITY-005
    [Fact]
    public void UnequalWeightsWeighTheHousedAndEmployedSharesApart()
    {
        var c = Unequal();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(0.8 * 1 + 0.2 * 0.6, s.Cohesion, 12);                       // all housed, 6 of 10 employed
        var working = Step(c, s, new AssignWorkers("town_a", "job", 10)).State;  // all housed, all employed
        Assert.Equal(1.0, working.Cohesion, 12);
    }

    /// <summary>D-A6: housing comes before Social cohesion in the turn, so cohesion reads the counts after this turn's housing.
    /// Read before housing, the same state would give 0.8 * 10/20 + 0.2 * 6/10 = 0.52.</summary>
    // R-CITY-004, R-CITY-005
    [Fact]
    public void CohesionReadsTheCountsAfterThisTurnsHousing()
    {
        var c = Unequal();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 10);
        var after = Step(c, s).State;                                             // 2 housed: 12 of 20 housed, 6 of 12 employed
        Assert.Equal((12, 8), (after.Cities["town_a"].Civilians, after.Cities["town_a"].Refugees));
        Assert.Equal(0.8 * 0.6 + 0.2 * 0.5, after.Cohesion, 12);
    }

    /// <summary>Social cohesion is a labelled placeholder while D-C1 is open (an owner question on #375): no effect, morale or
    /// threshold reads it, and only the top bar displays it (#380). Two states that differ only in it step to the same state,
    /// and no code outside the city rules, the state and the top bar names it.</summary>
    // R-CITY-005
    [Fact]
    public void CohesionFeedsNothingElse()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        for (int i = 0; i < 3; i++) s = Step(Mvp, s).State;
        var other = s.Clone();
        other.Cohesion = 1 - s.Cohesion;
        Assert.NotEqual(s.Hash(), other.Hash());
        Assert.Equal(Step(Mvp, s).State.Describe(), Step(Mvp, other).State.Describe());
        var repo = System.IO.Directory.GetParent(RealContentTests.ContentRoot())!.FullName;
        var readers = new[] { "sim/Statecraft.Sim", "sim/Statecraft.Cli", "unity/Assets" }
            .Select(d => System.IO.Path.Combine(repo, d)).Where(System.IO.Directory.Exists)
            .SelectMany(d => System.IO.Directory.GetFiles(d, "*.cs", System.IO.SearchOption.AllDirectories))
            .Where(f => System.Text.RegularExpressions.Regex.IsMatch(System.IO.File.ReadAllText(f), @"\bCohesion\b"))
            .Select(f => System.IO.Path.GetFileName(f)).OrderBy(f => f, StringComparer.Ordinal);
        // The top bar shows it (the owner's ruling of 15 October 2026, #380): the HUD's model, presenter and tests read it to
        // display it, and nothing else in Unity or the sim does.
        Assert.Equal(new[] { "Cities.cs", "GameState.cs", "HudModel.cs", "HudPresenter.cs", "HudTests.cs" }, readers);
    }

    [Fact]
    public void TheResolverDoesNotRunInTheLegacyLoop()
    {
        var c = ContentLoader.Load(Fixture.Write());   // the legacy loop (a fixture since the cutover)
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        s = Step(c, s).State;
        Assert.Empty(s.Cities);
        Assert.Equal(0, s.Cohesion);
        Assert.DoesNotContain("coh=", s.Describe());
    }
}
