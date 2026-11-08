using Statecraft.Sim;

/// <summary>#1725 PR 7, producer draft 02, arrivals only (the coordinator's ruling (a), 8 November 2026): on a game with an
/// initialized Historical ledger, each whole unit <c>Cities.Arrive</c> brings from the CBS series (R-CITY-003) is one
/// refugee record, located in its city by <c>Cities.Apportion</c>'s split, reception 0, with a stable source key and
/// ordinal, so a replay of the turn adds nobody twice. <c>PopulationSystem.Apply</c> adds no second count of the same
/// people (R-CITY-008: the ledger is authoritative, "never extra people"). Natural increase on the ledger is
/// <see cref="LedgerNaturalIncreaseTests"/>.</summary>
public class LedgerArrivalsTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    // A turn after the census day (city_arrivals_from, 8 November 1948) that brings CBS arrivals to the cities.
    internal static readonly DateTime From = new DateTime(1949, 1, 1);
    internal static readonly DateTime To = new DateTime(1949, 2, 1);

    // The dark start (#1725 PR 5): an initialized Historical ledger beside the game's city records, moved to the turn.
    internal static GameState Fixture(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);
        s.Date = From;
        return s;
    }

    internal static List<PopulationUnit> Arrived(GameState s) =>
        s.PopulationLedger.Units.Where(u => u.Status == PopulationStatus.Refugee).ToList();

    static Content NoNaturalIncrease()
    {
        var c = Gdd03();
        c.Rules.NaturalIncreasePerYear = 0;
        foreach (var r in c.EraRules.Values) r.NaturalIncreasePerYear = 0;
        return c;
    }

    // R-CITY-003 on the ledger: the turn's whole arrived units become one refugee record each, split over the cities by
    // Apportion as the aggregate game splits them, with the aggregate game's Why lines, reception 0, located in the city.
    // R-CITY-008: the records are the people; the aggregate city fields are not written, and the total rises by exactly the
    // arrived units times people_per_unit.
    [Fact]
    public void ArrivedUnitsBecomeOneRefugeeRecordEachInTheApportionSplit()
    {
        var c = Gdd03();
        var r = c.RulesAt(From).Mvp!;
        var s = Fixture(c);
        var twin = Simulation.NewGame(c, 1, Mode.Historical); twin.Date = From;
        var units = Cities.ArrivedUnits(c, From, To, r.PeoplePerUnit);
        Assert.True(units > 0);
        var before = twin.Cities.Values.ToDictionary(x => x.Id, x => x.Refugees, StringComparer.Ordinal);
        var cities = s.Cities.Values.Select(x => (x.Id, x.Refugees)).ToList();
        var next = s.PopulationLedger.NextPopulationId;
        var total = s.TotalPopulation;
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Cities.Arrive(c, s, r, To, why);
        Cities.Arrive(c, twin, r, To, twinWhy);

        var arrived = Arrived(s);
        Assert.Equal(units, arrived.Count);
        Assert.Equal(Enumerable.Range(next, units), arrived.Select(u => u.Id));
        Assert.All(arrived, u =>
        {
            Assert.Equal(0, u.ReceptionTicks);
            Assert.Equal(PopulationAssignmentKind.Unassigned, u.Assignment.Kind);
            Assert.Equal(CivilianProfession.None, u.Profession);
            Assert.Equal(c.PlayerRegions.AtLocation(u.Location!.City!)!.Id, u.Location.Region);
        });
        var byCity = arrived.GroupBy(u => u.Location!.City!).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        foreach (var x in twin.Cities.Values)
            Assert.Equal(x.Refugees - before[x.Id], byCity.GetValueOrDefault(x.Id));
        Assert.Equal(twinWhy.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key)), why.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key)));
        Assert.Equal(cities, s.Cities.Values.Select(x => (x.Id, x.Refugees)));
        s.PopulationLedger.Reconcile();
        Assert.Equal(total + units * r.PeoplePerUnit, s.TotalPopulation);
    }

    // R-CITY-003, R-POP-001: a record's source is the city's CBS arrivals and its ordinal the unit's place in the city's
    // cumulative share, so the same turn replayed from the same state, or run again on the state it produced, adds nobody
    // twice; and two turns in a row give the split of the whole run (no unit lost or counted twice).
    [Fact]
    public void ReplayingTheTurnAddsNobodyTwice()
    {
        var c = Gdd03();
        var r = c.RulesAt(From).Mvp!;
        var s = Fixture(c);
        var saved = s.Clone();
        Cities.Arrive(c, s, r, To, new List<Cause>());
        var once = s.PopulationLedger.Canonical().ToList();

        var replay = saved.Clone();
        Cities.Arrive(c, replay, r, To, new List<Cause>());
        Assert.Equal(once, replay.PopulationLedger.Canonical());

        var again = new List<Cause>();
        Cities.Arrive(c, s, r, To, again);
        Assert.Equal(once, s.PopulationLedger.Canonical());
        Assert.Empty(again);

        var later = new DateTime(1949, 3, 1);
        s.Date = To;
        Cities.Arrive(c, s, r, later, new List<Cause>());
        s.PopulationLedger.Reconcile();
        var split = Cities.Apportion(c, Cities.ArrivedUnits(c, From, later, r.PeoplePerUnit)
            + Cities.ArrivedUnits(c, c.RulesAt(From).Mvp!.CityArrivalsFrom, From, r.PeoplePerUnit));
        var had = Cities.Apportion(c, Cities.ArrivedUnits(c, c.RulesAt(From).Mvp!.CityArrivalsFrom, From, r.PeoplePerUnit));
        var byCity = Arrived(s).GroupBy(u => u.Location!.City!).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        foreach (var kv in split) Assert.Equal(kv.Value - had[kv.Key], byCity.GetValueOrDefault(kv.Key));
    }

    // R-CITY-008 on the ledger: with no natural increase, Apply adds no second count of the turn's CBS arrivals. They are
    // counted once, as Cities.Arrive's records, so the turn's total rises by the arrived units times people_per_unit only.
    [Fact]
    public void ApplyCountsTheArrivalsOnceAsTheRecords()
    {
        var c = NoNaturalIncrease();
        var r = c.RulesAt(From).Mvp!;
        var s = Fixture(c);
        Assert.NotEmpty(PopulationSystem.Arrivals(c, From, To));
        var population = s.Population.ToList();
        var total = s.TotalPopulation;
        var why = new List<Cause>();
        Cities.Arrive(c, s, r, To, why);
        var arrivedWhy = why.Count;
        var ledger = s.PopulationLedger.Canonical().ToList();

        PopulationSystem.Apply(c, s, To, 31 / 365.0, why);

        Assert.Equal(population, s.Population.ToList());
        Assert.Equal(ledger, s.PopulationLedger.Canonical());
        Assert.Equal(arrivedWhy, why.Count);
        s.PopulationLedger.Reconcile();
        Assert.Equal(total + Cities.ArrivedUnits(c, From, To, r.PeoplePerUnit) * r.PeoplePerUnit, s.TotalPopulation);
    }

    // R-POP-002: only a ledger initialized with mode Historical is let through. An Open ledger, like a synthetic one (the
    // boundary tests' rows population.apply and cities.arrive), is still refused by both, with the state unchanged.
    [Theory]
    [InlineData("population.apply")] [InlineData("cities.arrive")]
    public void AnOpenLedgerIsStillRefused(string call)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: Mode.Open);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var why = new List<Cause>();
        var before = s.Describe(); var hash = s.Hash();
        void Invoke()
        {
            if (call == "population.apply") PopulationSystem.Apply(c, s, s.Date.AddMonths(1), .1, why);
            else Cities.Arrive(c, s, new MvpRules(), s.Date.AddMonths(1), why);
        }
        Assert.Contains("integration is held", Assert.Throws<NotSupportedException>(Invoke).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    /// <summary>PopulationLedger.RegisterArrivals takes only PopulationInventoryRow items, and a row is refused at
    /// construction for each item RegisterArrival refuses: a blank source, a negative ordinal, or malformed UTF-16. So
    /// the batch registration makes the same per-item checks without repeating them.</summary>
    [Theory]
    [InlineData("", 0)]
    [InlineData("   ", 0)]
    [InlineData("ship", -1)]
    public void AnArrivalRowRefusesWhatRegisterArrivalRefuses(string source, int ordinal) =>
        Assert.ThrowsAny<ArgumentException>(() => new PopulationInventoryRow(source, ordinal, PopulationLedgerTests.Person()));

    /// <summary>The malformed UTF-16 case, built in code: xUnit's test data turns a lone surrogate into U+FFFD, which is
    /// valid text, so it cannot be an InlineData value.</summary>
    [Fact]
    public void AnArrivalRowRefusesALoneSurrogateSource() =>
        Assert.ThrowsAny<ArgumentException>(() => new PopulationInventoryRow("ship" + (char)0xD800, 0, PopulationLedgerTests.Person()));
}
