using Statecraft.Sim;

/// <summary>#1725 PR 7, producer draft 04: the gdd-0.3 housing step (R-HOUSE-005) on a game with an initialized Historical
/// ledger. R, a city's housed people (R-HOUSE-003), and P, the own population, are counted from the ledger's records, not
/// the aggregate city fields. The 20 opening field farmers are civilians, so the 18 in census hosts are housed there, beside
/// the place each adds (PR 4); the two at Nir Am and Nirim have no city record, so they are in no city's R and not in P
/// ("The 12 cities' people (Recommended)", "As built: only the cities whose housing is tracked.").</summary>
public class LedgerHousingConditionTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    // The farmers per census host city (PR 4: one per field, F01 to F20).
    static Dictionary<string, int> FarmersByCity(Content c, GameState s) => s.PopulationLedger.Units
        .Where(u => u.Profession == CivilianProfession.Farmer && u.Location?.City is string city && s.Cities.ContainsKey(city))
        .GroupBy(u => u.Location!.City!).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

    // The aggregate twin of a dark start: the public start of the same content, with each census host's farmers added to its
    // civilians and the dark start's housing capacity, so each city holds the same housed people as the ledger's records.
    static GameState Twin(Content c, GameState dark)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var farmers = FarmersByCity(c, dark);
        foreach (var x in s.Cities.Values.ToList())
        {
            var d = dark.Cities[x.Id];
            s.Cities[x.Id] = new City(x.Id, d.HousingCapacity, x.JobSlots, x.Civilians + farmers.GetValueOrDefault(x.Id), x.Employed,
                x.Refugees, x.Recruits, x.Condition, x.Craftsmen, x.JobsDefault, x.Qualified);
        }
        return s;
    }

    // R-HOUSE-005, R-POP-002: the ledger fixture's housing step runs and changes no record.
    [Fact]
    public void TheLedgerFixtureRunsTheHousingStep()
    {
        var c = Gdd03();
        var s = HistoricalPopulation.LedgerFixture(c);
        s.Date = c.Rules.Start;
        var ledger = s.PopulationLedger.Canonical();
        Cities.UpdateCondition(c, s, c.RulesAt(s.Date).Mvp!, new List<Cause>());
        s.PopulationLedger.Reconcile();
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
    }

    // R-HOUSE-003 on the ledger: a city's housed people are its civilian records (census and farmers), so each census host
    // houses its farmers beside the census; R-HOUSE-005: P is the tracked cities' people plus the own soldiers, so the two
    // farmers with no city record (Nir Am, Nirim) are in neither.
    [Fact]
    public void HousedAndOwnPopulationAreTheLedgersRecords()
    {
        var c = Gdd03();
        var dark = Dark(c); var pub = Simulation.NewGame(c, 1, Mode.Historical);
        var farmers = FarmersByCity(c, dark);
        Assert.Equal(18, farmers.Values.Sum());
        foreach (var x in pub.Cities.Values)
            Assert.Equal(Cities.Housed(pub, x) + farmers.GetValueOrDefault(x.Id), Cities.Housed(dark, dark.Cities[x.Id]));
        Assert.Equal(Cities.OwnPopulation(pub) + 18, Cities.OwnPopulation(dark));
        var soldiers = dark.PopulationLedger.Units.Count(u => u.Status == PopulationStatus.ActiveSoldier);
        Assert.Equal(dark.Cities.Values.Sum(x => Cities.Housed(dark, x)) + soldiers, Cities.OwnPopulation(dark));
        var unhoused = dark.PopulationLedger.Units.Where(u => u.Profession == CivilianProfession.Farmer && !farmers.ContainsKey(u.Location?.City ?? ""))
            .Select(u => u.Location?.City).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.Equal(2, unhoused.Count);
        Assert.All(unhoused, city => Assert.False(dark.Cities.ContainsKey(city!)));
    }

    // R-HOUSE-005 (#1703's order: surcharge, then deterioration, then cohesion): the dark start's housing step gives the
    // surcharge, every city's condition, the Social cohesion loss and the causes of an aggregate game with the same people
    // per city, borrows the same way (R-MONEY-003), and changes no ledger record.
    [Fact]
    public void TheDarkStartsHousingStepMatchesAnAggregateGameWithTheSamePeople()
    {
        var c = Gdd03();
        var dark = Dark(c); var twin = Twin(c, dark);
        foreach (var x in twin.Cities.Values) Assert.Equal(Cities.Housed(twin, x), Cities.Housed(dark, dark.Cities[x.Id]));
        var r = c.RulesAt(dark.Date).Mvp!;
        foreach (var s in new[] { dark, twin }) { s.Treasury = 1; s.Debt = 2; }
        var ledger = dark.PopulationLedger.Canonical();
        var darkWhy = new List<Cause>(); var twinWhy = new List<Cause>();
        Cities.UpdateCondition(c, dark, r, darkWhy);
        Cities.UpdateCondition(c, twin, r, twinWhy);
        Assert.Contains(darkWhy, x => x.Key == CauseKeys.HousingSurcharge);
        Assert.Equal(twinWhy.Select(x => (x.System, x.Target, x.Delta, x.Message)), darkWhy.Select(x => (x.System, x.Target, x.Delta, x.Message)));
        Assert.Equal((twin.Treasury, twin.Debt), (dark.Treasury, dark.Debt));
        Assert.Equal(twin.Cities.Values.Select(x => (x.Id, x.Condition)), dark.Cities.Values.Select(x => (x.Id, x.Condition)));
        Assert.Equal(twin.IndicatorShifts[Cities.CohesionIndicator], dark.IndicatorShifts[Cities.CohesionIndicator]);
        dark.PopulationLedger.Reconcile();
        Assert.Equal(ledger, dark.PopulationLedger.Canonical());
    }
}
