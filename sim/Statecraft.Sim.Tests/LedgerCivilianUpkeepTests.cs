using Statecraft.Sim;

/// <summary>#1725 PR 7, producer draft 05: civilian upkeep (R-MONEY-002) runs on a game with an initialized Historical
/// ledger, the dark start and the ledger fixture. It counts places, not people, so it pays what the aggregate start of the
/// same content pays and changes no ledger record (R-POP-002).</summary>
public class LedgerCivilianUpkeepTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // A copy of the real gdd-0.3 content with six distinct non-zero R-MONEY-002 rates (the shipped ones are all IL 0m).
    static Content WithRates()
    {
        var root = RealContentTests.ContentRoot();
        return ContentLoader.Load(rel =>
        {
            var path = Path.Combine(root, rel);
            var text = File.Exists(path) ? File.ReadAllText(path) : null;
            if (rel == "eras.yaml") return text!.Replace("loop: gdd", "loop: gdd, ruleset: gdd-0.3");
            if (rel != "rules.yaml") return text;
            foreach (var (key, rate) in new[] { ("city", "1.0"), ("housing", "2.0"), ("industry", "3.0"), ("transit_camp", "4.0"),
                ("military_camp", "5.0"), ("hakhshara", "6.0") })
            {
                var line = $"  {key}_upkeep_per_year: 0.0 ";
                Assert.Contains(line, text!);
                text = text!.Replace(line, $"  {key}_upkeep_per_year: {rate} ");
            }
            return text;
        }, Directory.GetFiles(Path.Combine(root, "events"), "*.yaml").Select(p => "events/" + Path.GetFileName(p)));
    }

    // One completed project of each paying kind in the first census city, on both states, so every rate is read.
    static void Build(Content c, GameState s)
    {
        var city = c.Cities[0].Id;
        foreach (var (id, kind) in new[] { (9001, "housing"), (9002, "industry"), (9003, "transit_camp") })
            s.CityProjects.Add(id, new CityProject(id, kind, city, 0, 0, 0, s.Date));
    }

    // R-MONEY-002 (IL 0m under gdd-0.3, the owner's 30 October 2026 "Keep ruling"), R-POP-002: the ledger fixture's
    // step-7 upkeep payment runs, pays nothing with the shipped rates, and leaves every record as it was.
    [Fact]
    public void TheLedgerFixturePaysCivilianUpkeep()
    {
        var c = Gdd03();
        var s = HistoricalPopulation.LedgerFixture(c);
        s.Date = c.Rules.Start;
        var ledger = s.PopulationLedger.Canonical(); var treasury = s.Treasury; var debt = s.Debt;
        var why = new List<Cause>();
        EconomySystem.PayCivilianUpkeep(c, s, c.RulesAt(s.Date).Mvp!, 1 / 12.0, why);
        Assert.Empty(why);
        Assert.Equal((treasury, debt), (s.Treasury, s.Debt));
        s.PopulationLedger.Reconcile();
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
    }

    // R-MONEY-002: the dark start pays the aggregate start's upkeep, 0 with the shipped rates.
    [Fact]
    public void TheDarkStartPaysTheAggregateUpkeepWithTheShippedRates()
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        Build(c, dark); Build(c, pub);
        var r = c.RulesAt(dark.Date).Mvp!;
        Assert.Equal(0, Cities.AnnualUpkeep(c, pub, r));
        Assert.Equal(Cities.AnnualUpkeep(c, pub, r), Cities.AnnualUpkeep(c, dark, r));
        dark.PopulationLedger.Reconcile();
    }

    // R-MONEY-002, R-MONEY-003: with non-zero rates in a content copy, the dark start pays exactly the aggregate start's
    // upkeep, a short Treasury borrows the rest as in the aggregate game, and no ledger record changes.
    [Fact]
    public void TheDarkStartPaysAndBorrowsTheAggregateUpkeepWithNonZeroRates()
    {
        var c = WithRates();
        Assert.True(c.UsesPopulationLedger);
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        Build(c, dark); Build(c, pub);
        var r = c.RulesAt(dark.Date).Mvp!;
        var expected = Cities.AnnualUpkeep(c, pub, r);
        Assert.True(expected >= c.Cities.Count * 1.0 + 2 + 3 + 4);   // every city plus the three projects
        Assert.Equal(expected, Cities.AnnualUpkeep(c, dark, r));
        Assert.True(expected / 12.0 > 1);   // the Treasury of 1 below is short

        var ledger = dark.PopulationLedger.Canonical();
        foreach (var s in new[] { dark, pub }) { s.Treasury = 1; s.Debt = 2; }
        var darkWhy = new List<Cause>(); var pubWhy = new List<Cause>();
        EconomySystem.PayCivilianUpkeep(c, dark, r, 1 / 12.0, darkWhy);
        EconomySystem.PayCivilianUpkeep(c, pub, r, 1 / 12.0, pubWhy);
        Assert.Equal(0, dark.Treasury);
        Assert.Equal(2 + expected / 12.0 - 1, dark.Debt, 9);
        Assert.Equal((pub.Treasury, pub.Debt), (dark.Treasury, dark.Debt));
        Assert.Equal(pubWhy.Select(x => (x.System, x.Target, x.Delta, x.Message)), darkWhy.Select(x => (x.System, x.Target, x.Delta, x.Message)));
        Assert.Contains(darkWhy, x => x.Message.Contains("civilian upkeep", StringComparison.Ordinal));
        dark.PopulationLedger.Reconcile();
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
    }
}
