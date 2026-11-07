using Statecraft.Sim;

/// <summary>#1725 PR 7, producer draft 06: BuyArms (R-ARMS-001, R-ARMS-002) runs on a game with an initialized Historical
/// ledger, the dark start and the ledger fixture. A bought Arms joins the national stock: it is not an issued item on any
/// record, so no ledger record changes (R-POP-002). The price stays rules.yaml's arms_price, 1.</summary>
public class LedgerBuyArmsTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // R-ARMS-002, R-POP-002: the ledger fixture buys Arms into the stock at arms_price; no record or issued item changes.
    [Fact]
    public void TheLedgerFixtureBuysArms()
    {
        var c = Gdd03();
        var s = HistoricalPopulation.LedgerFixture(c);
        s.Date = c.Rules.Start;
        s.Treasury = 10; s.Arms = 12;
        var ledger = s.PopulationLedger.Canonical();
        var why = new List<Cause>();
        Assert.Null(Pools.BuyArms(c, s, 3, why));
        Assert.Equal((15, 7.0, 0.0), (s.Arms, s.Treasury, s.Debt));
        Assert.Contains(why, x => x.System == "economy" && x.Target == "arms" && x.Delta == 3);
        s.PopulationLedger.Reconcile();
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
    }

    // R-ARMS-002 with R-MONEY-003: the dark start buys as the aggregate start of the same content does, at arms_price 1,
    // borrowing what a short Treasury lacks; Arms, Treasury, Debt and the causes match, and the ledger is unchanged.
    [Fact]
    public void TheDarkStartBuysAndBorrowsAsTheAggregateStart()
    {
        var c = Gdd03();
        Assert.Equal(1, c.RulesAt(c.Rules.Start).Mvp!.ArmsPrice);
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        Assert.Equal(pub.Arms, dark.Arms);
        foreach (var s in new[] { dark, pub }) { s.Treasury = 1; s.Debt = 2; }
        var ledger = dark.PopulationLedger.Canonical(); var arms = dark.Arms;
        var darkWhy = new List<Cause>(); var pubWhy = new List<Cause>();
        Assert.Null(Pools.BuyArms(c, dark, 4, darkWhy));
        Assert.Null(Pools.BuyArms(c, pub, 4, pubWhy));
        Assert.Equal((arms + 4, 0.0, 5.0), (dark.Arms, dark.Treasury, dark.Debt));
        Assert.Equal((pub.Arms, pub.Treasury, pub.Debt), (dark.Arms, dark.Treasury, dark.Debt));
        Assert.Equal(pubWhy.Select(x => (x.System, x.Target, x.Delta, x.Message)), darkWhy.Select(x => (x.System, x.Target, x.Delta, x.Message)));
        dark.PopulationLedger.Reconcile();
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
    }

    // R-ARMS-002 (#2198; owner, 2 November 2026: "Refuse buys over ceiling (Recommended)"): on the dark start a purchase
    // that would borrow past debt_ceiling is refused with the state, ledger and causes unchanged.
    [Fact]
    public void TheDarkStartRefusesABuyPastTheDebtCeiling()
    {
        var c = Gdd03();
        var s = Dark(c);
        s.Treasury = 0; s.Debt = c.RulesAt(s.Date).Mvp!.DebtCeiling!.Value;
        var hash = s.Hash(); var before = s.Describe();
        var why = new List<Cause>();
        Assert.Equal(Pools.RefusedDebtCeiling, Pools.BuyArms(c, s, 1, why));
        Assert.Empty(why);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(before, s.Describe());
        s.PopulationLedger.Reconcile();
    }

    // R-POP-002 (#1725 PR 7 review S2): only a ledger initialized with mode Historical is let through. An initialized Open
    // ledger, like a synthetic one (no mode), is still refused before any purchase, with the state unchanged.
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        s.Treasury = 23; s.Arms = 5;
        var why = new List<Cause>();
        var before = s.Describe(); var hash = s.Hash();
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(() => Pools.BuyArms(c, s, 1, why)).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }
}
