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
        Assert.False(r.State.Cities["tel_aviv"].JobsDefault);   // R-CITY-009: the order fixes the number
    }

    // R-CITY-001
    [Theory]
    [InlineData("town_a", "job", -1, "The number of workers cannot be negative")]
    [InlineData("town_a", "job", 21, "Town A has 20 jobs")]
    [InlineData("town_a", "job", 11, "Town A has 10 civilians and craftsmen")]
    [InlineData("nowhere", "job", 1, "There is no such city")]
    [InlineData("town_a", "craftsman", 1, "There is no such role")]   // a craftsman fills an ordinary job slot (R-CRAFT-002)
    public void TheLimitsAreRefusedAndChangeNothing(string city, string role, int count, string reason)
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new AssignWorkers(city, role, count));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());   // as if the order had not been given: Attention refunded too
    }

    // R-CITY-001: MaxWorkers is the limit Assign applies, so the panel can ask it instead of repeating the rule (#869). Town A
    // has 20 jobs and 10 civilians; with 30 civilians the jobs are the limit.
    [Theory]
    [InlineData(10, 10)]
    [InlineData(30, 20)]
    public void MaxWorkersIsTheCountAssignAccepts(int civilians, int max)
    {
        var c = Town(Fixture.City("town_a", 40, 20, civilians, 6));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(max, Cities.MaxWorkers(s.Cities["town_a"]));
        Assert.Empty(Step(c, s, new AssignWorkers("town_a", "job", max)).Rejected);
        Assert.Single(Step(c, s, new AssignWorkers("town_a", "job", max + 1)).Rejected);
    }

    // R-CITY-001 with R-TRAIN-001: the queue's Train orders take their civilians first, so an Assign up to MaxWorkers(s, city,
    // pending) and those orders are all accepted, in either order, and one more group is refused.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaxWorkersWithTheQueueCountsItsTrainOrders(bool assignFirst)
    {
        var c = Town(Fixture.City("town_a", 12, 20, 10, 6, "[military_camp, hakhshara]"));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Attention = 100;   // enough for three orders: Attention is not what this test is about
        var trains = new Command[] { new Train("town_a", "recruit", 2), new Train("town_a", "halutz", 1), new Train("elsewhere", "recruit", 5) };
        var max = Cities.MaxWorkers(s, "town_a", trains);
        Assert.Equal(7, max);   // 10 civilians, 3 of them ordered to train; the other city's order does not count
        Command[] Orders(int n) => assignFirst
            ? new Command[] { new AssignWorkers("town_a", "job", n), trains[0], trains[1] }
            : new Command[] { trains[0], trains[1], new AssignWorkers("town_a", "job", n) };
        Assert.Empty(Step(c, s, Orders(max)).Rejected);
        Assert.Single(Step(c, s, Orders(max + 1)).Rejected);
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

    /// <summary>Two cities with free job slots: town_b (listed first in cities.yaml) has more civilians than jobs, town_a more
    /// jobs than civilians, so each limit of min(job slots, civilians + craftsmen) is met once.</summary>
    static Content TwoTowns() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
        ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_b", 40, 8, 10, 0) + Fixture.City("town_a", 40, 20, 10, 6),
    })));

    // R-CITY-009 (Q-JOBS-1): after housing, free slots fill from the people without a job, in ascending city id, with no
    // Attention cost; an AssignWorkers order fixes that city's number and turns its default off.
    [Fact]
    public void TheJobDefaultFillsFreeSlots()
    {
        var c = TwoTowns();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Cities["town_a"] = s.Cities["town_a"].WithCraftsmen(2);   // 10 civilians and 2 craftsmen for 20 jobs
        var r = Step(c, s);
        Assert.Equal((12, 8), (r.State.Cities["town_a"].Employed, r.State.Cities["town_b"].Employed));
        Assert.True(r.State.Cities["town_a"].JobsDefault && r.State.Cities["town_b"].JobsDefault);
        Assert.Equal(new[] { ("town_a", 6.0, "Town A: 12 groups in work"), ("town_b", 8.0, "Town B: 8 groups in work") },
                     r.Why.Where(x => x.System == "city" && x.Message.EndsWith(" groups in work")).Select(x => (x.Target, x.Delta, x.Message)));

        // No Attention cost: a turn begun with no Attention left fills the same jobs.
        var tired = s.Clone();
        tired.Attention = 0;
        Assert.Equal(r.State.Hash(), Step(c, tired).State.Hash());

        // AssignWorkers fixes town_a's number from then on; town_b keeps its default.
        var manual = Step(c, s, new AssignWorkers("town_a", "job", 3));
        Assert.Empty(manual.Rejected);
        Assert.Equal((3, 8), (manual.State.Cities["town_a"].Employed, manual.State.Cities["town_b"].Employed));
        Assert.False(manual.State.Cities["town_a"].JobsDefault);
        var later = Step(c, manual.State).State;
        Assert.Equal((3, 8), (later.Cities["town_a"].Employed, later.Cities["town_b"].Employed));
    }

    // R-CITY-004 (Q-HOUSE-1: no hard cap; the 3 beyond the housing for 12 are the city's overflow, HousingTests)
    [Fact]
    public void EveryRefugeeIsHousedWithNoHardCap()
    {
        var c = Town();
        var s = With(Simulation.NewGame(c, 1, Mode.Historical), "town_a", 10, 6, 5);
        var city = Step(c, s).State.Cities["town_a"];   // the first week brings under one unit of arrivals
        Assert.Equal((15, 15, 0), (city.Civilians, city.Employed, city.Refugees));   // then the job default fills 15 of the 20 jobs (R-CITY-009)
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
        var after = Step(c, s).State;                                             // all 10 housed, then the job default fills 20 of 20 jobs (R-CITY-009)
        Assert.Equal((20, 0), (after.Cities["town_a"].Civilians, after.Cities["town_a"].Refugees));
        Assert.Equal(1.0, Cities.EmployedShare(after), 12);
        Assert.Equal(1.0, after.EffectContributions["work"], 12);
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
