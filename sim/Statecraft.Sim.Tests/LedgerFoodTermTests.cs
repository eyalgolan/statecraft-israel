using Statecraft.Sim;

/// <summary>#1704, #1725 PR 7: food_supply's stock term (R-FOOD-003, Pools.FoodTerm) runs on a game with an initialized
/// Historical ledger, the dark start and the ledger fixture. It reads only the Food stock, the last need and the unmet
/// share, never a record, so it returns what the aggregate game returns for the same Food state, and the ledger and the
/// hash are unchanged. A synthetic or Open ledger is still refused (R-POP-002). Step 7's Pools.Consume is not in scope.</summary>
public class LedgerFoodTermTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // R-FOOD-003, R-FOOD-001: the dark start's stock is cover_months of the ledger's need, so the term is exactly 0.
    [Fact]
    public void TheDarkStartsFoodTermIsZeroAtAFullStock()
    {
        var c = Gdd03();
        var s = Dark(c);
        Assert.True(s.PopulationLedger.IsHistorical);
        Assert.True(s.FoodNeed > 0);
        Assert.Equal((long)c.RulesAt(s.Date).Mvp!.CoverMonths!.Value * s.FoodNeed, s.Food);
        Assert.Equal(0, s.FoodUnmet);
        var hash = s.Hash(); var ledger = s.PopulationLedger.Canonical();
        Assert.Equal(0.0, Pools.FoodTerm(c, s));
        Assert.Equal(hash, s.Hash());
        s.PopulationLedger.Reconcile();
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
    }

    // R-FOOD-003: with the same Food, need and unmet share, the dark start and the aggregate start of the same content give
    // the same double, the formula k_food * (stock - full) / full - unmet / need; the ledger and the hash do not move.
    [Theory]
    [InlineData(0, 0)] [InlineData(10, 0)] [InlineData(0, 60)] [InlineData(40, 60)] [InlineData(900, 0)]
    public void TheDarkStartsFoodTermIsTheAggregateGamesTerm(int food, int unmet)
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(pub.Date, dark.Date);
        foreach (var s in new[] { dark, pub }) { s.Food = food; s.FoodNeed = 100; s.FoodUnmet = unmet; }
        var hash = dark.Hash(); var ledger = dark.PopulationLedger.Canonical();
        var r = c.RulesAt(dark.Date).Mvp!;
        var full = (long)r.CoverMonths!.Value * 100;
        var expected = r.KFood!.Value * (food - full) / full - (double)unmet / 100;
        var term = Pools.FoodTerm(c, dark);
        Assert.Equal(Pools.FoodTerm(c, pub), term);
        Assert.Equal(expected, term);
        Assert.Equal(hash, dark.Hash());
        dark.PopulationLedger.Reconcile();
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
    }

    // R-FOOD-003: the ledger fixture reads the term too; with no need it is 0, as in the aggregate game.
    [Fact]
    public void TheLedgerFixtureReadsTheTerm()
    {
        var c = Gdd03();
        var s = HistoricalPopulation.LedgerFixture(c);
        s.Date = c.Rules.Start;
        var ledger = s.PopulationLedger.Canonical();
        s.FoodNeed = 0;
        Assert.Equal(0.0, Pools.FoodTerm(c, s));
        s.Food = 0; s.FoodNeed = 50; s.FoodUnmet = 50;
        var k = c.RulesAt(s.Date).Mvp!.KFood!.Value;
        Assert.Equal(-k - 1.0, Pools.FoodTerm(c, s), 12);
        s.PopulationLedger.Reconcile();
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
    }

    // R-POP-002: only a ledger initialized with mode Historical is let through. An initialized Open ledger, like a
    // synthetic one (no mode), is still refused, with the state unchanged.
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        s.Food = 10; s.FoodNeed = 5; s.FoodUnmet = 1;
        var before = s.Describe(); var hash = s.Hash();
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(() => Pools.FoodTerm(c, s)).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
    }
}
