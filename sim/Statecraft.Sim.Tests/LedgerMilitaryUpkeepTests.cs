using Statecraft.Sim;

/// <summary>#1699 part 1, #1725 PR 7: military upkeep (R-UNIT-010) runs on a game with an initialized Historical ledger,
/// the dark start. Owner, 8 November 2026, on #1725: "Formations, unchanged (Recommended)". Each Israeli formation on
/// the map pays the same rate whatever its strength, and no soldier record is read or written (R-POP-002). The rate stays
/// rules.yaml's military_upkeep_per_turn.</summary>
public class LedgerMilitaryUpkeepTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static int Formations(Content c, GameState s) =>
        s.Units.Values.Count(u => u.Side == "israel" && c.UnitTypes.Any(t => t.Id == u.Type && t.Side == "israel"));

    static IEnumerable<(string, string, double, string, string, string)> Lines(List<Cause> why) =>
        why.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key, string.Join("|", x.Args)));

    // R-UNIT-010: the dark start pays the number of its own formations times the rate, exactly as the aggregate start of
    // the same content with the same units, with the same Why lines; the ledger is unchanged.
    [Fact]
    public void TheDarkStartPaysTheAggregateUpkeepPerFormation()
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var r = c.RulesAt(dark.Date).Mvp!;
        Assert.Equal(0.05, r.MilitaryUpkeepPerTurn);
        Assert.Equal(pub.Units.Keys, dark.Units.Keys);
        var count = Formations(c, dark);
        Assert.True(count > 0);
        Assert.Equal(Formations(c, pub), count);
        foreach (var s in new[] { dark, pub }) { s.Treasury = 100; s.Debt = 2; }
        var ledger = dark.PopulationLedger.Canonical();
        var darkWhy = new List<Cause>(); var pubWhy = new List<Cause>();
        Military.PayUpkeep(c, dark, r, darkWhy);
        Military.PayUpkeep(c, pub, r, pubWhy);
        Assert.Equal(100 - count * 0.05, dark.Treasury, 9);
        Assert.Equal(2.0, dark.Debt);
        Assert.Equal((pub.Treasury, pub.Debt), (dark.Treasury, dark.Debt));
        Assert.Equal(Lines(pubWhy), Lines(darkWhy));
        Assert.Contains(darkWhy, x => x.Key == CauseKeys.MilitaryUpkeep && x.Delta == -count * 0.05);
        dark.PopulationLedger.Reconcile();
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
    }

    // R-UNIT-010 with R-MONEY-003: a short Treasury borrows the rest, as in the aggregate game, with the same Why lines.
    [Fact]
    public void AShortTreasuryBorrowsAsTheAggregateGame()
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var r = c.RulesAt(dark.Date).Mvp!;
        var cost = Formations(c, dark) * r.MilitaryUpkeepPerTurn!.Value;
        foreach (var s in new[] { dark, pub }) { s.Treasury = 0.01; s.Debt = 2; }
        Assert.True(cost > 0.01);
        var ledger = dark.PopulationLedger.Canonical();
        var darkWhy = new List<Cause>(); var pubWhy = new List<Cause>();
        Military.PayUpkeep(c, dark, r, darkWhy);
        Military.PayUpkeep(c, pub, r, pubWhy);
        Assert.Equal(0.0, dark.Treasury);
        Assert.Equal(2 + cost - 0.01, dark.Debt, 9);
        Assert.Equal((pub.Treasury, pub.Debt), (dark.Treasury, dark.Debt));
        Assert.Equal(Lines(pubWhy), Lines(darkWhy));
        Assert.True(darkWhy.Count >= 2);   // the upkeep line and the borrow line
        dark.PopulationLedger.Reconcile();
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
    }

    // "Formations, unchanged (Recommended)": strength is not read. Halving one formation's strength on the map changes
    // neither the cost nor the ledger, so the upkeep reads no soldier record.
    [Fact]
    public void UpkeepIsPerFormationWhateverItsStrength()
    {
        var c = Gdd03();
        var s = Dark(c);
        var r = c.RulesAt(s.Date).Mvp!;
        var u = s.Units.Values.First(x => x.Side == "israel" && x.Strength > 1);
        s.Units[u.Id] = new Unit(u.Id, u.Type, u.Q, u.R, u.Service, u.Side, 1, u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp);
        s.Treasury = 100;
        var ledger = s.PopulationLedger.Canonical();
        var why = new List<Cause>();
        Military.PayUpkeep(c, s, r, why);
        Assert.Equal(100 - Formations(c, s) * 0.05, s.Treasury, 9);
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
    }

    // R-POP-002: only a ledger initialized with mode Historical is let through. An initialized Open ledger, like a
    // synthetic one (no mode), is still refused before anything is read, with the state unchanged.
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var before = s.Describe(); var hash = s.Hash(); var why = new List<Cause>();
        Assert.Contains("integration is held",
            Assert.Throws<NotSupportedException>(() => Military.PayUpkeep(c, s, new MvpRules(), why)).Message);
        Assert.Empty(why);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
    }
}
