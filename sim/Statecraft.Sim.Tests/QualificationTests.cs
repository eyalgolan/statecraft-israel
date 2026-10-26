using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;

/// <summary>Return to civilian life and reassignment (R-QUAL-001 to R-QUAL-007; #1511, owner ruling "Two-way, keep training"
/// and its three answers, 23 October 2026): instant and Money-free, qualified civilians are ordinary civilians for jobs, housing
/// and training, and a halutz's partial service is kept. The fixture's town_a has housing for 12, 8 jobs and 10 civilians, 6
/// in work; a halutz becomes a veteran after 4 turns of service.</summary>
public class QualificationTests
{
    static Content Town() => ContentLoader.Load(Fixture.Write(Fixture.Gdd()));

    /// <summary>town_a (1, 0) and town_b (2, 1), as DemobilizationTests lays them out.</summary>
    static Content TwoTowns() => ContentLoader.Load(Fixture.Write(Fixture.Gdd(new Dictionary<string, string>
    {
        ["map/settlement_hexes.yaml"] = "settlements:\n  - { id: town_a, q: 1, r: 0 }\n  - { id: town_b, q: 2, r: 1 }\n",
        ["map/settlements_1948.yaml"] = Fixture.Files["map/settlements_1948.yaml"]
            + "  - { id: town_b, name: Town B, lon: 34.42, lat: 33.28, type: town, founded: 1900, pre_modern: false }\n",
        ["map/locations_1948.yaml"] = Fixture.Files["map/locations_1948.yaml"]
            + "  - id: town_b\n    name: Town B\n    name_he: עיר ב\n    name_he_sources: [{ url: 'https://example.org/town-b-he', checked: 2026-10-18 }]\n    kind: city\n    lon: 34.42\n    lat: 33.28\n    q: 2\n    r: 1\n    priority: must_have\n    marker: true\n    managed_city: true\n    designated_site: false\n    settlement_control_target: false\n    sources: [{ url: 'https://example.org/town-b', checked: 2026-10-18 }]\n    status: draft\n",
        ["cities.yaml"] = "cities:\n" + Fixture.City("town_a", 40, 8, 10, 0) + Fixture.City("town_b", 40, 8, 10, 0),
    })));

    static StepResult Step(Content c, GameState s, params Command[] commands) => Simulation.Step(c, s, commands);

    static GameState WithCity(Content c, City city, params Unit[] units)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Cities[city.Id] = city;
        foreach (var u in units) { s.Units.Add(u.Id, u); s.NextId = Math.Max(s.NextId, u.Id + 1); }
        return s;
    }

    static int People(GameState s) => s.Cities.Values.Sum(x => x.Civilians + x.Craftsmen) + s.Units.Values.Count(u => Projects.IsPioneer(u.Type));

    // R-QUAL-002: instant, no Money; the craftsmen become qualified civilians, the jobs stay as they were.
    [Fact]
    public void CraftsmenReturnToCivilianLifeAndKeepTheirTraining()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 4, 4, 0, craftsmen: 3, jobsDefault: false));
        var r = Step(c, s, new ReturnCraftsmen("town_a", 2));
        Assert.Empty(r.Rejected);
        var x = r.State.Cities["town_a"];
        Assert.Equal((6, 1, 2, 4), (x.Civilians, x.Craftsmen, x.Qualified.Craftsmen, x.Employed));
        Assert.Equal(People(s), People(r.State));
        // no Money is paid, and the 2 who returned earn the ordinary rate (R-QUAL-002): as the same city built by hand does
        var ordinary = WithCity(c, new City("town_a", 12, 8, 6, 4, 0, craftsmen: 1, jobsDefault: false));
        Assert.Equal(Step(c, ordinary).State.Treasury, r.State.Treasury, 9);
        Assert.Equal(Cities.Housed(s, s.Cities["town_a"]), Cities.Housed(r.State, x));
    }

    [Fact]
    public void ReturningCraftsmenIsRefusedForAnUnknownCityTooFewCraftsmenOrNoCount()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 4, 0, 0, craftsmen: 1));
        foreach (var (cmd, why) in new (Command, string)[] {
            (new ReturnCraftsmen("nowhere", 1), "There is no such city"), (new ReturnCraftsmen("town_a", 0), "Return at least one group"),
            (new ReturnCraftsmen("town_a", 2), "Town A has 1 craftsmen") })
        {
            var r = Step(c, s, cmd);
            Assert.Equal(why, Assert.Single(r.Rejected).Reason);
            Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
        }
    }

    // R-QUAL-004: no training, no Money, no turns; the craftsmen are back at once.
    [Fact]
    public void QualifiedCraftsmenReturnWithoutTrainingOrMoney()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 6, 0, 0, jobsDefault: false, qualified: new Qualified(2, 0, new int[0])));
        var r = Step(c, s, new ReassignCraftsmen("town_a", 2));
        Assert.Empty(r.Rejected);
        Assert.Empty(r.State.Cohorts);
        var x = r.State.Cities["town_a"];
        Assert.Equal((4, 2, 0), (x.Civilians, x.Craftsmen, x.Qualified.Craftsmen));
        Assert.Equal(People(s), People(r.State));
        Assert.Equal(Step(c, s).State.Treasury, r.State.Treasury, 9);
    }

    // R-QUAL-004: a qualified civilian needs no job, nor does it take one that is taken.
    [Fact]
    public void ReassigningCraftsmenIsRefusedWithoutQualifiedOrIdleCivilians()
    {
        var c = Town();
        var q = new Qualified(2, 0, new int[0]);
        var busy = WithCity(c, new City("town_a", 12, 8, 4, 3, 0, jobsDefault: false, qualified: q));    // 3 of the 4 in work: 1 idle
        var r = Step(c, busy, new ReassignCraftsmen("town_a", 2));
        Assert.Equal("Town A has 1 civilians without work", Assert.Single(r.Rejected).Reason);
        Assert.Equal(Step(c, busy).State.Hash(), r.State.Hash());
        var none = WithCity(c, new City("town_a", 12, 8, 4, 0, 0, jobsDefault: false, qualified: q));
        Assert.Equal("Town A has 2 qualified craftsmen", Assert.Single(Step(c, none, new ReassignCraftsmen("town_a", 3)).Rejected).Reason);
        Assert.Equal("Reassign at least one group", Assert.Single(Step(c, none, new ReassignCraftsmen("town_a", 0)).Rejected).Reason);
        Assert.Equal("There is no such city", Assert.Single(Step(c, none, new ReassignCraftsmen("x", 1)).Rejected).Reason);
    }

    // Owner ruling 2: a qualified civilian is an ordinary civilian for jobs, income and housing.
    [Fact]
    public void QualifiedCiviliansWorkHouseAndEarnAsOrdinaryOnes()
    {
        var c = Town();
        var plain = WithCity(c, new City("town_a", 12, 8, 5, 5, 0));
        var qualified = WithCity(c, new City("town_a", 12, 8, 5, 5, 0, jobsDefault: false, qualified: new Qualified(2, 1, new[] { 0, 3 })));
        Assert.Equal(Step(c, plain).State.Treasury, Step(c, qualified).State.Treasury, 9);          // ordinary rate, not the craftsman's
        Assert.Equal(Cities.Housed(plain, plain.Cities["town_a"]), Cities.Housed(qualified, qualified.Cities["town_a"]));
        Assert.Equal(Cities.MaxWorkers(plain.Cities["town_a"]), Cities.MaxWorkers(qualified.Cities["town_a"]));
        Assert.Equal(Training.Idle(plain.Cities["town_a"]), Training.Idle(qualified.Cities["town_a"]));
    }

    // R-QUAL-006: training takes the unqualified first; a qualified civilian taken loses its qualification.
    [Fact]
    public void TrainingTakesUnqualifiedCiviliansFirstAndThenQualifiedOnes()
    {
        var c = Town();
        var q = new Qualified(2, 1, new[] { 2, 3 });
        var s = WithCity(c, new City("town_a", 12, 8, 6, 0, 0, jobsDefault: false, qualified: q));   // 5 qualified of 6 civilians
        var one = Step(c, s, new Train("town_a", "halutz", 1));
        Assert.Empty(one.Rejected);
        Assert.Equal(5, one.State.Cities["town_a"].Qualified.Total);             // the unqualified one went
        var three = Step(c, s, new Train("town_a", "halutz", 3));
        Assert.Empty(three.Rejected);
        var left = three.State.Cities["town_a"];
        Assert.Equal(3, left.Civilians);
        Assert.Equal((0, 2, 1), (left.Qualified.Craftsmen, left.Qualified.Halutzim.Count, left.Qualified.Veterans));   // the craftsmen went first
        var four = Step(c, s, new Train("town_a", "halutz", 5)).State.Cities["town_a"];
        Assert.Equal((1, 0, 0, 1), (four.Civilians, four.Qualified.Craftsmen, four.Qualified.Halutzim.Count, four.Qualified.Veterans));   // then the halutzim, the veterans last
        Assert.All(new[] { left, four }, x => Assert.True(x.Qualified.Total <= x.Civilians));
    }

    // R-QUAL-003 and owner ruling 3: a halutz keeps the service it had.
    [Fact]
    public void AnIdleHalutzReturnsToTheNearestCityKeepingItsService()
    {
        var c = TwoTowns();
        var s = WithCity(c, new City("town_a", 40, 8, 1, 0, 0), new Unit(7, "halutz", 2, 1, 3));   // on town_b's hex
        s.Cities["town_b"] = new City("town_b", 40, 8, 1, 0, 0);
        var r = Step(c, s, new UnitOrder(7, Qualification.ReturnOrder));
        Assert.Empty(r.Rejected);
        Assert.DoesNotContain(7, r.State.Units.Keys);
        var b = r.State.Cities["town_b"];
        Assert.Equal(2, b.Civilians);
        Assert.Equal(new[] { 3 }, b.Qualified.Halutzim);
        Assert.Equal(1, r.State.Cities["town_a"].Civilians);
        Assert.Equal(People(s), People(r.State));
    }

    // R-QUAL-003: the nearest city, ties by city id (R-UNIT-005's rule).
    [Fact]
    public void TheReturningUnitTiesGoToTheLowestCityId()
    {
        var c = TwoTowns();
        var s = WithCity(c, new City("town_a", 40, 8, 1, 0, 0), new Unit(7, "veteran_halutz", 2, 0, 4));   // 1 from town_a (1,0) and 1 from town_b (2,1)
        s.Cities["town_b"] = new City("town_b", 40, 8, 1, 0, 0);
        var r = Step(c, s, new UnitOrder(7, Qualification.ReturnOrder));
        Assert.Empty(r.Rejected);
        Assert.Equal((2, 1), (r.State.Cities["town_a"].Civilians, r.State.Cities["town_a"].Qualified.Veterans));
        Assert.Equal(0, r.State.Cities["town_b"].Qualified.Total);
    }

    [Fact]
    public void AUnitAtWorkAndAUnitThatIsNotAPioneerAreRefused()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 4, 0, 0), new Unit(7, "halutz", 1, 0), new Unit(8, "recruit", 1, 0, side: "israel", strength: 2));
        var working = Step(c, s, new PioneerWork(7, Projects.BuildYishuv, "site_a")).State;
        Assert.True(Projects.IsBusy(working, 7));
        var busy = Step(c, working, new UnitOrder(7, Qualification.ReturnOrder));
        Assert.Equal("That unit is at work; stop the work first", Assert.Single(busy.Rejected).Reason);
        Assert.Contains(7, busy.State.Units.Keys);
        Assert.Equal("Only a pioneer unit takes that order", Assert.Single(Step(c, s, new UnitOrder(8, Qualification.ReturnOrder)).Rejected).Reason);
        Assert.Equal("There is no such unit", Assert.Single(Step(c, s, new UnitOrder(99, Qualification.ReturnOrder)).Rejected).Reason);
        Assert.Equal("That order names no hex", Assert.Single(Step(c, s, new UnitOrder(7, Qualification.ReturnOrder, 1, 0)).Rejected).Reason);
        // stopped, it goes at once
        var stopped = Step(c, working, new StopWork(7), new UnitOrder(7, Qualification.ReturnOrder));
        Assert.Empty(stopped.Rejected);
        Assert.DoesNotContain(7, stopped.State.Units.Keys);
    }

    // R-QUAL-005: units appear at the settlement hex, the halutz with the most service first, a veteran as a veteran.
    [Fact]
    public void QualifiedPioneersReappearAsUnitsAtTheSettlementHex()
    {
        var c = Town();
        var q = new Qualified(0, 2, new[] { 0, 2, 3 });
        var s = WithCity(c, new City("town_a", 12, 8, 6, 0, 0, jobsDefault: false, qualified: q));
        var halutz = Step(c, s, new ReassignPioneers("town_a", "halutz", 2));
        Assert.Empty(halutz.Rejected);
        var units = halutz.State.Units.Values.OrderBy(u => u.Id).ToList();
        Assert.Equal(new[] { 3, 2 }, units.Select(u => u.Service));
        Assert.All(units, u => Assert.Equal(("halutz", 1, 0, ""), (u.Type, u.Q, u.R, u.Side)));
        Assert.Equal(new[] { 0 }, halutz.State.Cities["town_a"].Qualified.Halutzim);
        Assert.Equal(4, halutz.State.Cities["town_a"].Civilians);
        Assert.Equal(People(s), People(halutz.State));
        Assert.Equal(Step(c, s).State.Treasury, halutz.State.Treasury, 9);

        var vet = Step(c, s, new ReassignPioneers("town_a", "veteran_halutz", 1));
        Assert.Empty(vet.Rejected);
        var u1 = Assert.Single(vet.State.Units.Values);
        Assert.Equal(("veteran_halutz", 4, 1, 0), (u1.Type, u1.Service, u1.Q, u1.R));
        Assert.Equal(1, vet.State.Cities["town_a"].Qualified.Veterans);
        Assert.Equal(3, vet.State.Cities["town_a"].Qualified.Halutzim.Count);   // the halutzim are untouched
    }

    [Fact]
    public void ReassigningPioneersIsRefusedAsStated()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 3, 2, 0, jobsDefault: false, qualified: new Qualified(0, 1, new[] { 1, 1 })));   // 1 idle
        foreach (var (cmd, why) in new (Command, string)[] {
            (new ReassignPioneers("nowhere", "halutz", 1), "There is no such city"),
            (new ReassignPioneers("town_a", "soldier", 1), "There is no such qualification"),
            (new ReassignPioneers("town_a", "halutz", 0), "Reassign at least one group"),
            (new ReassignPioneers("town_a", "veteran_halutz", 2), "Town A has 1 qualified veteran pioneers"),
            (new ReassignPioneers("town_a", "halutz", 3), "Town A has 2 qualified halutzim"),
            (new ReassignPioneers("town_a", "halutz", 2), "Town A has 1 civilians without work") })
        {
            var r = Step(c, s, cmd);
            Assert.Equal(why, Assert.Single(r.Rejected).Reason);
            Assert.Equal(Step(c, s).State.Hash(), r.State.Hash());
        }
    }

    // Done-when: nobody is duplicated or lost over a full round trip, a veteran stays one, service is kept.
    [Fact]
    public void ARoundTripKeepsEveryPersonAndTheirStatus()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 4, 0, 0, craftsmen: 1, jobsDefault: false), new Unit(5, "halutz", 1, 0, 2), new Unit(6, "veteran_halutz", 1, 0, 4));
        var before = People(s);
        s = Step(c, s, new UnitOrder(5, Qualification.ReturnOrder), new UnitOrder(6, Qualification.ReturnOrder), new ReturnCraftsmen("town_a", 1)).State;
        Assert.Equal(before, People(s));
        Assert.Empty(s.Units);
        Assert.Equal((7, 0, 3), (s.Cities["town_a"].Civilians, s.Cities["town_a"].Craftsmen, s.Cities["town_a"].Qualified.Total));
        s = Step(c, s, new ReassignPioneers("town_a", "halutz", 1), new ReassignPioneers("town_a", "veteran_halutz", 1), new ReassignCraftsmen("town_a", 1)).State;
        Assert.Equal(before, People(s));
        var x = s.Cities["town_a"];
        Assert.Equal((4, 1, 0), (x.Civilians, x.Craftsmen, x.Qualified.Total));
        Assert.Equal(new[] { ("halutz", 2), ("veteran_halutz", 4) }, s.Units.Values.OrderBy(u => u.Id).Select(u => (u.Type, u.Service)));
    }

    // R-QUAL-007: nothing is written for a city without qualified civilians; the order they returned in does not matter.
    [Fact]
    public void QualifiedCiviliansReachTheHashOnlyWhenThereAreSomeInASortedOrder()
    {
        string H(City x) { var g = new GameState(); g.Cities[x.Id] = x; return g.Hash(); }
        var none = new City("a", 1, 1, 3, 1, 0);
        Assert.Equal(H(none), H(new City("a", 1, 1, 3, 1, 0, qualified: Qualified.None)));
        Assert.Equal("a=1,1,3,1,0", none.Canonical());
        var hashes = new[] { new Qualified(1, 0, new int[0]), new Qualified(0, 1, new int[0]), new Qualified(0, 0, new[] { 2 }), new Qualified(0, 0, new[] { 2, 3 }),
            }.Select(q => H(new City("a", 1, 1, 3, 1, 0, qualified: q))).Append(H(none)).ToList();
        Assert.Equal(hashes.Count, new HashSet<string>(hashes).Count);
        Assert.Equal(new Qualified(0, 0, new int[0]).AddHalutz(3).AddHalutz(2).Canonical(), new Qualified(0, 0, new int[0]).AddHalutz(2).AddHalutz(3).Canonical());
        Assert.Equal(",qcraftsmen=1,qhalutz=2:3,qveterans=2", new Qualified(1, 2, new[] { 2, 3 }).Canonical());
    }

    // R-SAVE: the new orders are saved and replayed to the same state, in a scenario and as a unit order.
    [Fact]
    public void TheNewOrdersSaveReplayAndRunInAScenario()
    {
        var c = Town();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.State.Cities["town_a"] = new City("town_a", 12, 8, 4, 0, 0, craftsmen: 1);
        g.State.Units.Add(g.State.NextId, new Unit(g.State.NextId, "halutz", 1, 0, 2));
        var unit = g.State.NextId++;
        g.Step(new Command[] { new ReturnCraftsmen("town_a", 1), new UnitOrder(unit, Qualification.ReturnOrder) });
        var text = g.Save.Write();
        Assert.Contains("retcraft town_a 1", text);
        Assert.Contains("order " + unit + " return_civilian", text);
        foreach (var cmd in new Command[] { new ReassignCraftsmen("town_a", 1), new ReassignPioneers("town_a", "halutz", 1) })
        {
            var other = GameSession.New(c, 1, Mode.Historical);
            other.Step(new[] { cmd });   // refused here, but written and parsed back as the same order
            Assert.Equal(cmd.GetType(), SaveGame.Parse(other.Save.Write()).Turns.Single().Single().GetType());
        }
        var yaml = "name: q\nuntil: 1948-06-01\nsteps:\n  - on: 1948-05-15\n    commands:\n      - { kind: reassign_pioneers, city: town_a, track: halutz, count: 1 }\n";
        var built = Scenario.Parse(yaml).Steps.Single().Built.Single();
        Assert.Equal(("town_a", "halutz", 1), (((ReassignPioneers)built).City, ((ReassignPioneers)built).Track, ((ReassignPioneers)built).Count));
    }

    // R-QUAL-002 and R-QUAL-004: Employed is untouched by a return and by a reassignment, and Idle stays as it was.
    [Fact]
    public void EmployedAndIdleAreUnchangedByReturnAndReassignment()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 4, 5, 0, craftsmen: 3, jobsDefault: false));   // 5 in work: 3 craftsmen and 2 civilians
        var idle = Training.Idle(s.Cities["town_a"]);
        var back = Step(c, s, new ReturnCraftsmen("town_a", 2)).State.Cities["town_a"];
        Assert.Equal((5, idle), (back.Employed, Training.Idle(back)));
        var again = Step(c, WithCity(c, back), new ReassignCraftsmen("town_a", 2));
        Assert.Empty(again.Rejected);
        var x = again.State.Cities["town_a"];
        Assert.Equal((5, idle, 3, 0), (x.Employed, Training.Idle(x), x.Craftsmen, x.Qualified.Total));
    }

    // R-SAVE: a played session of the new orders replays to the live state, and the other scenario kinds build.
    [Fact]
    public void ASessionOfTheNewOrdersReplaysToTheSameHash()
    {
        var c = Town();
        var g = GameSession.New(c, 1, Mode.Historical);
        g.Step(new Command[] { new Train("town_a", "halutz", 1) });
        var loaded = GameSession.Load(c, g.Save.Write(), out var warnings);
        Assert.Empty(warnings);
        Assert.Equal(g.State.Hash(), loaded.State.Hash());
        var yaml = "name: q\nuntil: 1948-06-01\nsteps:\n  - on: 1948-05-15\n    commands:\n      - { kind: return_craftsmen, city: town_a, count: 1 }\n      - { kind: reassign_craftsmen, city: town_a, count: 2 }\n      - { kind: unit_order, unit: 3, order: return_civilian }\n";
        var built = Scenario.Parse(yaml).Steps.Single().Built;
        Assert.Equal(new[] { typeof(ReturnCraftsmen), typeof(ReassignCraftsmen), typeof(UnitOrder) }, built.Select(x => x.GetType()));
        Assert.Equal(Qualification.ReturnOrder, ((UnitOrder)built[2]).Order);
    }

    // Owner ruling 1: "the normal Attention of an order": a craftsman's is assign_workers' 0.5, a pioneer unit's is free.
    [Fact]
    public void TheOrdersCostTheNormalAttentionOfAnOrder()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(AttentionSystem.Cost(c, s, "assign_workers"), AttentionSystem.Cost(c, s, Qualification.ReturnCraftsmenKind));
        Assert.Equal(AttentionSystem.Cost(c, s, "assign_workers"), AttentionSystem.Cost(c, s, Qualification.ReassignCraftsmenKind));
        Assert.Equal(AttentionSystem.Cost(c, s, "unit_order"), AttentionSystem.Cost(c, s, Qualification.ReassignPioneersKind));
        Assert.Equal(0, AttentionSystem.Cost(c, s, "unit_order"));
    }

    // R-TRAIN-001 with R-QUAL-004, R-QUAL-005 (#1548; owner, 24 October 2026: "Yes, count them",
    // https://github.com/eyalgolan/statecraft-israel/issues/1511#issuecomment-6378360813): a queued reassignment takes its
    // city's idle civilians as a queued Train does, for its own city only.
    [Fact]
    public void AQueuedReassignCountsAgainstTheCitysFreeCivilians()
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 10, 0, 0, craftsmen: 0, qualified: new Qualified(3, 1, new[] { 1, 2 })));
        var queue = new Command[] { new Train("town_a", "recruit", 1), new ReassignCraftsmen("town_a", 2), new ReassignPioneers("town_a", "halutz", 1),
            new ReassignPioneers("town_a", "veteran_halutz", 1), new ReassignCraftsmen("elsewhere", 5) };
        Assert.Equal(5, Training.Ordered("town_a", queue));
        Assert.Equal(5, Training.Available(s, "town_a", queue));   // 10 idle civilians less those 5
        Assert.Equal(10, Training.Available(s, "town_a", new Command[] { new ReassignCraftsmen("elsewhere", 5), new ReassignPioneers("elsewhere", "halutz", 2) }));
    }

    // The count is the one the sim accepts: the orders up to Available all run, in either order, and one group more is refused.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AvailableAfterQueuedReassignsKeepsEveryOrderAccepted(bool trainFirst)
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 4, 0, 0, craftsmen: 0, qualified: new Qualified(2, 0, new[] { 1 })));
        s.Attention = 100;
        var reassigns = new Command[] { new ReassignCraftsmen("town_a", 1), new ReassignPioneers("town_a", "halutz", 1) };
        Assert.Equal(2, Training.Available(s, "town_a", reassigns));
        Command[] Orders(int recruits) => trainFirst
            ? new Command[] { new Train("town_a", "recruit", recruits), reassigns[0], reassigns[1] }
            : new Command[] { reassigns[0], reassigns[1], new Train("town_a", "recruit", recruits) };
        Assert.Empty(Step(c, s, Orders(2)).Rejected);
        Assert.Single(Step(c, s, Orders(3)).Rejected);
    }

    // R-CITY-001 with R-QUAL-004, R-QUAL-005 (#1548): every queued reassignment counts against MaxWorkers in full, so the job
    // order up to it is accepted with the reassignments whether it runs first or last (the bound is safe, not tight).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaxWorkersCountsEveryQueuedReassignInAnyOrder(bool jobsFirst)
    {
        var c = Town();
        var s = WithCity(c, new City("town_a", 12, 8, 6, 0, 0, craftsmen: 0, jobsDefault: false, qualified: new Qualified(2, 0, new[] { 1 })));
        Assert.Equal(6, Cities.MaxWorkers(s, "town_a", new Command[0]));
        Assert.Equal(4, Cities.MaxWorkers(s, "town_a", new Command[] { new ReassignCraftsmen("town_a", 2) }));
        Assert.Equal(5, Cities.MaxWorkers(s, "town_a", new Command[] { new ReassignPioneers("town_a", "halutz", 1) }));
        var queue = new Command[] { new ReassignCraftsmen("town_a", 2), new ReassignPioneers("town_a", "halutz", 1) };
        var max = Cities.MaxWorkers(s, "town_a", queue);
        Assert.Equal(3, max);
        s.Attention = 100;
        Command[] Orders(int jobs) => jobsFirst
            ? new Command[] { new AssignWorkers("town_a", "job", jobs) }.Concat(queue).ToArray()
            : queue.Append(new AssignWorkers("town_a", "job", jobs)).ToArray();
        Assert.Empty(Step(c, s, Orders(max)).Rejected);
    }
}
