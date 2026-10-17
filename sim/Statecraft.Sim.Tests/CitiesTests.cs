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

    // R-CITY-004 (Q-HOUSE-1: no hard cap; the 3 beyond the housing for 12 are the city's overflow, HousingTests)
    [Fact]
    public void EveryRefugeeIsHousedWithNoHardCap()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        var city = Step(c, s).State.Cities["town_a"];   // the first week brings under one unit of arrivals
        Assert.Equal((15, 6, 0), (city.Civilians, city.Employed, city.Refugees));
    }

    // R-CITY-003, R-CITY-004 (D-A6: arrivals come after housing, so they are housed on the next turn)
    [Fact]
    public void ATurnsArrivalsAreHousedOnTheNextTurn()
    {
        // The cities take arrivals from the census day, 8 November 1948 (Q-ARRV-3), so the turn from 15 November has some.
        // Tel Aviv's civilians are set to 228 so the test reads them apart from the start count.
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        while (s.Date < new DateTime(1948, 11, 15)) s = Step(Mvp, s).State;
        s = With(s, "tel_aviv", 228, 106, 0);
        var one = Step(Mvp, s).State;
        var arrived = one.Cities["tel_aviv"].Refugees;
        Assert.True(arrived > 0);
        Assert.Equal(228, one.Cities["tel_aviv"].Civilians);   // the housing step ran before they came
        var two = Step(Mvp, one).State.Cities["tel_aviv"];
        Assert.Equal(228 + arrived, two.Civilians);           // no hard cap (Q-HOUSE-1): all of them, on the next turn
    }

    /// <summary>D-A6: housing comes before Social cohesion in the turn. The effects web reads the cities' employed share after
    /// the resolution, so it sees this turn's housed people: read before housing, the same state would give 6 of 10.</summary>
    // R-CITY-004, R-CITY-005
    [Fact]
    public void TheEmployedShareReadsTheCountsAfterThisTurnsHousing()
    {
        // A work effect with k 1 and inertia 1 (fixture values), so its contribution is the share it read.
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 20, 10, 6),
            ["model.yaml"] = Fixture.Files["model.yaml"].Replace("effects:\n", "effects:\n  - { id: work, source: employed_share, target: morale, k: 1, inertia: 1, name: Work }\n"),
        })));
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 10);
        var after = Step(c, s).State;                                             // all 10 housed (no hard cap): 6 of 20 employed
        Assert.Equal((20, 0), (after.Cities["town_a"].Civilians, after.Cities["town_a"].Refugees));
        Assert.Equal(6.0 / 20, Cities.EmployedShare(after), 12);
        Assert.Equal(6.0 / 20, after.EffectContributions["work"], 12);
    }

    [Fact]
    public void TheResolverDoesNotRunInTheLegacyLoop()
    {
        var c = ContentLoader.Load(Fixture.Write());   // the legacy loop (a fixture since the cutover)
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        s = Step(c, s).State;
        Assert.Empty(s.Cities);
        Assert.DoesNotContain("coh=", s.Describe());
    }
}
