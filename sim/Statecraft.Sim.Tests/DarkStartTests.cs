using Statecraft.Sim;

/// <summary>#1689 / #1725 PR 5, the dark start: the internal NewGame overload that initializes the Historical ledger in
/// place (HistoricalPopulation.Initialize) before Pools.Init, under exact gdd-0.3 only. The public NewGame is unchanged
/// and still starts with no ledger (R-POP-004); the dark start is fixture and overload only.</summary>
public class DarkStartTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // R-POP-003, docs/determinism.md: the same content and seed give the same dark start, hash for hash; its ledger is
    // initialized and written, so it is not the public start's hash.
    [Fact]
    public void DarkStartHashRepeats()
    {
        var c = Gdd03();
        var s = Dark(c);
        Assert.True(s.PopulationLedger.IsInitialized);
        Assert.Equal(s.Hash(), Dark(Gdd03()).Hash());
        Assert.Equal(s.Describe(), Dark(c).Describe());
        Assert.NotEqual(Simulation.NewGame(c, 1, Mode.Historical).Hash(), s.Hash());
    }

    // R-POP-001, R-POP-004: the dark start's ledger reconciles, and it holds exactly the Historical ledger fixture's
    // records, binding for binding (the in-place initializer and LedgerFixture build the same ledger).
    [Fact]
    public void DarkStartLedgerReconcilesAndIsTheFixturesLedger()
    {
        var c = Gdd03();
        var l = Dark(c).PopulationLedger;
        l.Reconcile();
        var fixture = HistoricalPopulation.LedgerFixture(c).PopulationLedger;
        Assert.Equal(fixture.Canonical(), l.Canonical());
        Assert.Equal(HistoricalPopulation.Inventory(c).Count, l.Units.Count);
    }

    // R-FOOD-001 (a new game starts the stock at cover_months of the start's need) under gdd-0.3 with an initialized
    // ledger (#2470: the need is one ration for each living own population id, Pools.Rations): the ledger is in place
    // before Pools.Init, so the dark start's Food is cover_months (rules.yaml mvp.cover_months, 3) times the ledger's need.
    [Fact]
    public void DarkStartFoodIsThreeTimesTheLedgersNeed()
    {
        var c = Gdd03();
        var s = Dark(c);
        Assert.Equal(3, c.RulesAt(c.Rules.Start).Mvp!.CoverMonths);
        var need = Pools.Rations(s.PopulationLedger.Units);
        Assert.Equal(s.PopulationLedger.Units.Count, need);
        Assert.Equal(need, s.FoodNeed);
        Assert.Equal(3 * need, s.Food);
        Assert.Equal(0, s.FoodUnmet);
        // the aggregate start's need is the national head count, a different number: the dark start reads the ledger's
        Assert.NotEqual(Simulation.NewGame(c, 1, Mode.Historical).FoodNeed, s.FoodNeed);
    }

    // R-UNIT-006, R-POP-004: the dark start still places the starting forces, ids 0 to n-1 in content order, and each
    // own opening formation's strength is its active-soldier records in the ledger, the formation their service target.
    [Fact]
    public void DarkStartPlacesTheStartingForcesAndTheyMatchTheLedgersSoldiers()
    {
        var c = Gdd03();
        var s = Dark(c);
        var aggregate = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(aggregate.Units.Keys, s.Units.Keys);
        Assert.Equal(aggregate.Units.Values.Select(u => u.Canonical()), s.Units.Values.Select(u => u.Canonical()));
        var own = s.Units.Values.Where(u => u.Side == Movement.PlayerSide).ToList();
        Assert.NotEmpty(own);
        foreach (var u in own)
        {
            var target = HistoricalPopulation.FormationPrefix + c.StartingForces[u.Id].Row;
            Assert.Equal(u.Strength, s.PopulationLedger.Units.Count(p => p.Status == PopulationStatus.ActiveSoldier
                && p.Assignment.Kind == PopulationAssignmentKind.ActiveService && p.Assignment.Target == target));
        }
    }

    // R-POP-002: EventSystem.Trigger still refuses a ledger game (its producer is a later PR), so the dark start fires no
    // opening event. The aggregate parts the ledger does not refuse keep the public start's values.
    [Fact]
    public void DarkStartFiresNoEventAndKeepsTheAggregateStartElsewhere()
    {
        var c = Gdd03();
        var s = Dark(c);
        var aggregate = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Empty(s.FiredEvents);
        Assert.Equal(aggregate.Arms, s.Arms);
        Assert.Equal(aggregate.Treasury, s.Treasury);
        Assert.Equal(aggregate.Rung, s.Rung);
        Assert.Equal(aggregate.Cities.Keys, s.Cities.Keys);
    }

    // R-POP-001: only exact gdd-0.3 has a ledger, so gdd-0.1 and legacy content refuse a dark start; the public NewGame is
    // the overload's aggregate path, hash for hash, and still has no ledger (R-POP-004).
    [Fact]
    public void DarkStartNeedsGdd03AndThePublicStartIsUnchanged()
    {
        Assert.Throws<InvalidOperationException>(() => Dark(HistoricalPopulationTests.Real(false)));
        Assert.Throws<InvalidOperationException>(() => Dark(RealContentTests.Legacy()));
        var c = Gdd03();
        var pub = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.False(pub.PopulationLedger.IsInitialized);
        Assert.Equal(pub.Hash(), Simulation.NewGame(c, 1, Mode.Historical, darkStart: false).Hash());
    }

    // R-POP-002: the in-place initializer refuses a ledger that is already initialized, leaving it as it was.
    [Fact]
    public void InPlaceInitializerRefusesASecondInitialization()
    {
        var c = Gdd03();
        var s = HistoricalPopulation.LedgerFixture(c);
        var before = s.Hash();
        Assert.Throws<InvalidOperationException>(() => HistoricalPopulation.Initialize(c, s));
        Assert.Equal(before, s.Hash());
    }
    // R-FOOD-001 under gdd-0.3 (#2470): an initialized ledger's need is one ration for each living own population id
    // (Pools.Rations), not the head count over rules.yaml's people_per_unit. At the Historical scale the two agree (each
    // record is people_per_unit people, and GameState.TotalPopulation reads the ledger), so this test sets the rules'
    // people_per_unit to twice the scale the ledger froze at initialization (R-POP-003 writes it): the aggregate head
    // count over the rules' unit is then half the records, and Pools.Init must still give one ration a record.
    [Fact]
    public void PoolsInitNeedCountsLedgerRecordsNotTheHeadCountOverTheRulesUnit()
    {
        var c = Gdd03();
        var s = HistoricalPopulation.LedgerFixture(c);
        s.Date = c.Rules.Start;
        var mvp = c.RulesAt(s.Date).Mvp!;
        var records = s.PopulationLedger.Units.Count;
        Assert.Equal(records * (double)mvp.PeoplePerUnit, s.TotalPopulation);
        mvp.PeoplePerUnit *= 2;
        Assert.NotEqual(records, (int)Math.Round(s.TotalPopulation / c.RulesAt(s.Date).Mvp!.PeoplePerUnit, MidpointRounding.AwayFromZero));
        Pools.Init(c, s);
        Assert.Equal(records, s.FoodNeed);
        Assert.Equal(mvp.CoverMonths!.Value * records, s.Food);
    }
}
