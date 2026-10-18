using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>The GDD slice's training (Phase 1 PR 4): Train (R-TRAIN-001) and CancelTraining (R-TRAIN-002) in a military camp
/// and a Hakhshara, and the cohorts' progress in TurnResolver (R-TRAIN-003, step 4 of spec 2026-10-12 section 2.3, D-A6).</summary>
public class TrainingTests
{
    static readonly Content Mvp = ContentLoader.Load(RealContentTests.ContentRoot());

    /// <summary>The GDD fixture's town: 10 civilians, 6 of them employed, so 4 without work. The fixture's recruit trains for 2
    /// turns at 0.5 a group, its halutz for 3 turns at 0.25.</summary>
    static Content Town(string facilities = "[military_camp, hakhshara]") =>
        ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 20, 10, 6, facilities),
        })));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    // D-D5 (#456): one military camp and one Hakhshara exist at the start, a labelled design assumption in the overlay.
    [Fact]
    public void TheSliceStartsWithOneCampAndOneHakhshara()
    {
        Assert.Equal(new[] { "military_camp", "hakhshara" }, Assert.Single(Mvp.Cities, x => x.Facilities.Count > 0 && x.Id == "tel_aviv").Facilities);
        Assert.Single(Mvp.Cities, x => x.Facilities.Count > 0);   // the twelve cities have one camp and one Hakhshara between them
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        Assert.Empty(s.Cohorts);
        Assert.Equal(0, s.Cities["tel_aviv"].Recruits);
    }

    // R-TRAIN-001
    [Fact]
    public void TrainingTakesCiviliansWithoutWorkAndPays()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId;
        var r = Step(c, s, new Train("town_a", "recruit", 3));
        Assert.Empty(r.Rejected);
        var city = r.State.Cities["town_a"];
        Assert.Equal((7, 7, 0), (city.Civilians, city.Employed, city.Recruits));   // in training: neither civilians nor recruits; the job default (R-CITY-009) employs the 7
        var k = Assert.Single(r.State.Cohorts.Values);
        Assert.Equal((id, "recruit", "town_a", 3, 1), (k.Id, k.Type, k.City, k.Count, k.TurnsLeft));   // 2 turns, one of them this one
        Assert.Equal(id + 1, r.State.NextId);
        Assert.Equal(Step(c, s).State.Treasury - 1.5, r.State.Treasury, 9);
        Assert.Contains(r.Why, x => x.System == "city" && x.Message == "Town A: 3 groups began basic training, cost IL 1.5m");
    }

    // R-TRAIN-001
    [Theory]
    [InlineData("town_a", "recruit", 0, "Train at least one group")]
    [InlineData("town_a", "recruit", -1, "Train at least one group")]
    [InlineData("town_a", "recruit", 5, "Town A has 4 civilians without work")]
    [InlineData("town_a", "halutz", 5, "Town A has 4 civilians without work")]
    [InlineData("nowhere", "recruit", 1, "There is no such city")]
    [InlineData("town_a", "soldier", 1, "There is no such training")]
    [InlineData("town_a", "civilian", 1, "There is no such training")]   // a type with no facility
    public void TheLimitsAreRefusedAndChangeNothing(string city, string track, int count, string reason)
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new Train(city, track, count));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());   // as if the order had not been given: Attention refunded too
    }

    // R-TRAIN-001: Idle is the limit Train applies (Town A: 10 civilians, 6 employed), so the panel can ask it (#869).
    [Fact]
    public void IdleIsTheCountTrainAccepts()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(4, Training.Idle(s.Cities["town_a"]));
        Assert.Empty(Step(c, s, new Train("town_a", "recruit", 4)).Rejected);
        Assert.Single(Step(c, s, new Train("town_a", "recruit", 5)).Rejected);
    }

    // R-TRAIN-001 with R-CITY-001: Available counts both tracks' Train orders and an Assign order's rise in jobs in the queue,
    // so orders up to it are all accepted, in either order, and one group more is refused (#869: recruits and halutzim were
    // each checked alone).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AvailableCountsEveryTrackAndTheAssignOrder(bool assignFirst)
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Attention = 100;   // enough for three orders: Attention is not what this test is about
        Assert.Equal(4, Training.Available(s, "town_a", new Command[0]));
        Assert.Equal(1, Training.Available(s, "town_a", new Command[] { new Train("town_a", "recruit", 2), new Train("town_a", "halutz", 1) }));
        Assert.Equal(4, Training.Available(s, "town_a", new Command[] { new Train("elsewhere", "recruit", 2) }));
        Assert.Equal(4, Training.Available(s, "town_a", new Command[] { new AssignWorkers("town_a", "job", 3) }));   // fewer jobs free no one before the turn
        var assign = new AssignWorkers("town_a", "job", 7);
        Assert.Equal(2, Training.Available(s, "town_a", new Command[] { assign, new Train("town_a", "recruit", 1) }));   // 10 civilians - 7 in jobs - 1 recruit
        Command[] Orders(int halutzim) => assignFirst
            ? new Command[] { assign, new Train("town_a", "recruit", 1), new Train("town_a", "halutz", halutzim) }
            : new Command[] { new Train("town_a", "recruit", 1), new Train("town_a", "halutz", halutzim), assign };
        Assert.Empty(Step(c, s, Orders(2)).Rejected);
        Assert.Single(Step(c, s, Orders(3)).Rejected);
    }

    // R-TRAIN-001
    [Theory]
    [InlineData("[military_camp]", "halutz", "Town A has no Hakhshara")]
    [InlineData("[hakhshara]", "recruit", "Town A has no military camp")]
    public void ACityWithoutTheFacilityRefuses(string facilities, string track, string reason)
    {
        var c = Town(facilities);
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new Train("town_a", track, 1));
        Assert.Equal(reason, Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-TRAIN-001: training borrows the shortfall, as map actions do (owner decision "Allow borrowing", 2026-10-15, #468).
    [Fact]
    public void AShortTreasuryBorrowsTheShortfall()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Treasury = 1.4;
        s.Debt = 3;
        var after = s.Clone();
        var why = new List<Cause>();
        Assert.Null(Training.Start(c, after, "town_a", "recruit", 3, why));
        Assert.Equal(0, after.Treasury);
        Assert.Equal(3.1, after.Debt, 9);   // the IL 0.1m the Treasury lacked
        Assert.Single(after.Cohorts);
        Assert.Contains(why, x => x.System == "economy" && x.Target == "debt" && x.Message == "Borrowed IL 0.1m for training");
        var none = s.Clone();
        none.Treasury = 0;
        Assert.Empty(Step(c, none, new Train("town_a", "recruit", 3)).Rejected);   // an empty Treasury trains too
    }

    // R-TRAIN-001: a Treasury of exactly the cost pays it in full, with nothing borrowed.
    [Fact]
    public void ATreasuryOfExactlyTheCostPaysIt()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Treasury = 1.5;
        var after = s.Clone();
        Assert.Null(Training.Start(c, after, "town_a", "recruit", 3, new List<Cause>()));
        Assert.Equal(0, after.Treasury);
        Assert.Equal(s.Debt, after.Debt);   // nothing borrowed
        Assert.Single(after.Cohorts);
    }

    // R-TRAIN-001, R-TRAIN-002
    [Fact]
    public void TheLegacyLoopRefusesBothOrdersAndRefundsAttention()
    {
        var c = ContentLoader.Load(Fixture.Write());   // the legacy loop (a fixture since the cutover)
        var s = Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical);
        var r = Step(c, s, new Train("town_a", "recruit", 1), new CancelTraining(0));
        Assert.All(r.Rejected, x => Assert.Equal("That order is not available in this game", x.Reason));
        Assert.Equal(2, r.Rejected.Count);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
    }

    // R-TRAIN-002 (D-A3: cancelling returns the people but not the Money spent)
    [Fact]
    public void CancellingReturnsPeopleNotMoney()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Cities["town_a"] = s.Cities["town_a"].WithJobsDefault(false);   // R-TRAIN-002 alone: the job default (R-CITY-009) would refill the jobs
        var trained = Step(c, s, new Train("town_a", "halutz", 4)).State;
        var id = Assert.Single(trained.Cohorts.Keys);
        var cancelled = Step(c, trained, new CancelTraining(id));
        Assert.Empty(cancelled.Rejected);
        var kept = Step(c, trained).State;
        var city = cancelled.State.Cities["town_a"];
        Assert.Equal((10, 6), (city.Civilians, city.Employed));   // back as civilians without work
        Assert.Empty(cancelled.State.Cohorts);
        Assert.Equal(kept.Treasury, cancelled.State.Treasury, 9);   // the IL 1.0m paid stays paid
        var never = Step(c, Step(c, s).State).State;
        Assert.Equal(never.Treasury - 1.0, cancelled.State.Treasury, 9);
        Assert.Contains(cancelled.Why, x => x.System == "city" && x.Message == "Town A: 4 groups left pioneer training; the Money spent is not returned");
    }

    /// <summary>R-TRAIN-002 on its own, without the turn's housing step (which would house returned refugees and hide where
    /// the people went): they come back as civilians without work, not as refugees.</summary>
    // R-TRAIN-002
    [Fact]
    public void CancelledPeopleComeBackAsCiviliansWithoutWorkNotAsRefugees()
    {
        var c = Town();
        var s = Step(c, Simulation.NewGame(c, 1, Mode.Historical), new Train("town_a", "recruit", 3)).State;
        var before = s.Cities["town_a"];
        var after = s.Clone();
        Assert.Null(Training.Cancel(c, after, Assert.Single(s.Cohorts.Keys), new List<Cause>()));
        var city = after.Cities["town_a"];
        Assert.Equal((before.Civilians + 3, before.Employed, before.Refugees, before.Recruits), (city.Civilians, city.Employed, city.Refugees, city.Recruits));
        Assert.Equal(s.Treasury, after.Treasury);
    }

    // R-TRAIN-002
    [Fact]
    public void CancellingAnUnknownOrFinishedCohortIsRefusedAndChangesNothing()
    {
        var c = Town();
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var r = Step(c, s, new CancelTraining(999));
        Assert.Equal("There is no such training group", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());

        var one = Step(c, s, new Train("town_a", "recruit", 1)).State;
        var id = Assert.Single(one.Cohorts.Keys);
        var done = Step(c, one).State;   // the fixture's recruit trains for 2 turns
        Assert.Empty(done.Cohorts);
        var late = Step(c, done, new CancelTraining(id));
        Assert.Equal("There is no such training group", Assert.Single(late.Rejected).Reason);
        Assert.Equal(Step(c, done).State.Hash(), late.State.Hash());
        Assert.Equal(1, late.State.Cities["town_a"].Recruits);
    }

    // R-TRAIN-001, R-TRAIN-002
    [Fact]
    public void BothOrdersSurviveASave()
    {
        var g = GameSession.New(Mvp, GoldenRun.Seed, Mode.Historical);
        g.Step(new Command[] { new Train("tel_aviv", "halutz", 2) });
        var id = Assert.Single(g.State.Cohorts.Keys);
        g.Step(new Command[] { new CancelTraining(id), new Train("tel_aviv", "recruit", 3) });
        g.Step(new Command[0]);
        var text = g.Save.Write();
        Assert.Contains("\ntrain tel_aviv halutz 2\n", text);
        Assert.Contains($"\nuntrain {id}\n", text);
        Assert.Contains("\ntrain tel_aviv recruit 3\n", text);
        var loaded = GameSession.Load(Mvp, text, out _).State;
        Assert.Equal(g.State.Hash(), loaded.Hash());
        Assert.Single(loaded.Cohorts);
    }

    // ---- progress in the resolver (R-TRAIN-003, step 4 of section 2.3) ----

    // R-TRAIN-003
    [Fact]
    public void ARecruitCohortCompletesAfterExactlyItsTrainingTurns()
    {
        var turns = Mvp.UnitTypes.Single(t => t.Id == "recruit").TrainingTurns;
        Assert.Equal(3, turns);
        var s = Step(Mvp, Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical), new Train("tel_aviv", "recruit", 3)).State;
        for (int turn = 1; turn < turns; turn++)
        {
            Assert.Equal(turns - turn, Assert.Single(s.Cohorts.Values).TurnsLeft);
            Assert.Equal(0, s.Cities["tel_aviv"].Recruits);
            s = Step(Mvp, s).State;
        }
        Assert.Empty(s.Cohorts);
        Assert.Equal(3, s.Cities["tel_aviv"].Recruits);
        Assert.Empty(s.Units);   // a recruit is a count until Phase 5
    }

    /// <summary>The timing rule, the plan's reading of D-A6 (Task 4.3) and not part of the owner's ruling: the turn a cohort is ordered in is its first training turn, so a cohort
    /// ordered on turn T with training_turns 3 is done at the end of turn T+2. The other reading (done at the end of T+3) is a
    /// one-line change: the TurnsLeft a cohort opens with in Training.Start.</summary>
    // R-TRAIN-001, R-TRAIN-003
    [Fact]
    public void ACohortOrderedOnTurnTWithThreeTurnsFinishesAtTheEndOfTurnTPlusTwo()
    {
        Assert.Equal(3, Mvp.UnitTypes.Single(t => t.Id == "recruit").TrainingTurns);
        var start = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var t0 = Step(Mvp, start, new Train("tel_aviv", "recruit", 2)).State;   // the end of turn T
        var t1 = Step(Mvp, t0).State;                                            // the end of turn T+1
        var t2 = Step(Mvp, t1).State;                                            // the end of turn T+2
        Assert.Single(t1.Cohorts);
        Assert.Equal(0, t1.Cities["tel_aviv"].Recruits);
        Assert.Empty(t2.Cohorts);
        Assert.Equal(2, t2.Cities["tel_aviv"].Recruits);

        var u0 = Step(Mvp, start, new Train("tel_aviv", "halutz", 2)).State;     // halutz: 4 turns, done at the end of T+3
        var u2 = Step(Mvp, Step(Mvp, u0).State).State;
        Assert.Empty(u2.Units);
        Assert.Equal(2, Step(Mvp, u2).State.Units.Count);
    }

    // R-TRAIN-003
    [Fact]
    public void AHalutzCohortBecomesUnitsWithConsecutiveIdsAtTheCitysHex()
    {
        var s = Simulation.NewGame(Mvp, GoldenRun.Seed, Mode.Historical);
        var r = Step(Mvp, s, new Train("tel_aviv", "halutz", 3));
        s = r.State;
        var cohort = Assert.Single(s.Cohorts.Keys);
        for (int turn = 1; turn < Mvp.UnitTypes.Single(t => t.Id == "halutz").TrainingTurns; turn++)
        {
            Assert.Empty(s.Units);
            s = Step(Mvp, s).State;
        }
        Assert.Empty(s.Cohorts);
        Assert.Equal(new[] { cohort + 1, cohort + 2, cohort + 3 }, s.Units.Keys);
        Assert.All(s.Units.Values, u => Assert.Equal(("halutz", -3, 19), (u.Type, u.Q, u.R)));   // Tel Aviv's settlement hex
        Assert.Equal(cohort + 4, s.NextId);
        Assert.Equal(0, s.Cities["tel_aviv"].Recruits);
    }

    // R-TRAIN-003: cohorts end in ascending id, each taking its unit ids in turn. Two cities on different hexes show the order.
    [Fact]
    public void CohortsThatEndOnTheSameTurnEndInIdOrder()
    {
        var c = ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
        {
            ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
            ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
                + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
            ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 12, 20, 10, 6, "[hakhshara]")
                + Fixture.City("town_b", 12, 20, 10, 6, "[hakhshara]"),
        })));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Attention = 10;   // two orders on the fixture's first turn
        var r = Step(c, s, new Train("town_b", "halutz", 1), new Train("town_a", "halutz", 2));
        Assert.Empty(r.Rejected);
        s = Step(c, Step(c, r.State).State).State;   // the fixture's halutz trains for 3 turns
        Assert.Empty(s.Cohorts);
        Assert.Equal(new[] { (2, 1), (1, 0), (1, 0) }, s.Units.Values.Select(u => (u.Q, u.R)));   // town_b's cohort first: its id is lower
    }

    [Fact]
    public void TheResolverTrainsNothingInTheLegacyLoop()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Step(c, Simulation.NewGame(c, GoldenRun.Seed, Mode.Historical)).State;
        Assert.Empty(s.Cohorts);
        Assert.DoesNotContain("train[", s.Describe());
    }
}
