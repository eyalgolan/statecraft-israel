using Statecraft.Sim;

/// <summary>#1725 PR 7, producer draft 02, natural increase: on a game with an initialized Historical ledger,
/// <c>PopulationSystem.Apply</c> grows the ledger at the national rate (docs/GDD.md, Q11: natural increase "stays the national
/// rate, 0.0169 a year") in whole records. The owner, 8 November 2026: "Carry the fraction (Recommended)" (the remainder is
/// kept from turn to turn and a record is added each time it reaches one), "Same split as arrivals (Recommended)" (the new
/// records are split over the cities by <c>Cities.Apportion</c>), "Free civilian (Recommended)" (each is a free civilian of
/// that city with no work experience and no reception wait), "Everyone alive (Recommended)" (the base is every living record).
/// R-CITY-008.</summary>
public class LedgerNaturalIncreaseTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    // A week: 0.0169 a year of the dark start's living records is below one record, so the carry crosses one after a few turns.
    const double Week = 7 / 365.0;

    // The increase a turn owes, in records, as the owner's carry describes it: the rate times the turn's share of a year
    // times every living record, held as decimal (as Cities.Apportion is, "so exact on every runtime").
    static decimal Owed(Content c, GameState s, double yearFraction) =>
        (decimal)c.RulesAt(s.Date).NaturalIncreasePerYear * (decimal)yearFraction * s.PopulationLedger.Units.Count;

    // "Carry the fraction (Recommended)": turns whose increase stays below one record add none and carry it; the turn whose
    // carried sum first reaches one adds exactly one record and carries the rest. "Same split as arrivals (Recommended)"
    // and "Free civilian (Recommended)": the record is a free civilian of the city Apportion gives its first unit, reception
    // done, no work experience, keyed increase/<city> with that city's ordinal.
    [Fact]
    public void TheCarryAddsOneRecordAtTheTurnItCrossesOne()
    {
        var c = Gdd03();
        var s = LedgerArrivalsTests.Fixture(c);
        var living = s.PopulationLedger.Units.Count;
        var next = s.PopulationLedger.NextPopulationId;
        var per = Owed(c, s, Week);
        Assert.True(per > 0 && per < 1);
        var crossing = (int)Math.Ceiling(1 / per);
        Assert.True(crossing > 1);
        var carry = 0m;
        for (var turn = 1; turn < crossing; turn++)
        {
            PopulationSystem.Apply(c, s, s.Date.AddDays(7), Week, new List<Cause>());
            carry += per;
            Assert.Equal(living, s.PopulationLedger.Units.Count);
            Assert.Equal(carry, s.PopulationLedger.NaturalIncreaseCarry);
        }
        var why = new List<Cause>();
        PopulationSystem.Apply(c, s, s.Date.AddDays(7), Week, why);
        Assert.Equal(living + 1, s.PopulationLedger.Units.Count);
        Assert.Equal(carry + per - 1, s.PopulationLedger.NaturalIncreaseCarry);
        Assert.Empty(why);
        var born = s.PopulationLedger.Get(next);
        var city = Cities.Apportion(c, 1).Single(kv => kv.Value == 1).Key;
        Assert.Equal(PopulationStatus.FreeCivilian, born.Status);
        Assert.Equal(c.RequirePopulationInteger("reception.turns"), born.ReceptionTicks);
        Assert.Equal((city, c.PlayerRegions.AtLocation(city)!.Id), (born.Location!.City, born.Location.Region));
        Assert.Equal((PopulationAssignmentKind.Unassigned, CivilianProfession.None, CivilianTier.None, 0),
            (born.Assignment.Kind, born.Profession, born.Tier, born.WorkXp));
        Assert.True(s.PopulationLedger.HasArrived(PopulationSystem.IncreasePrefix + city, 0));
        s.PopulationLedger.Reconcile();
    }

    // "Everyone alive (Recommended)": the base is every living record, soldiers included, and a record lost in combat no
    // longer counts.
    [Fact]
    public void TheBaseIsEveryLivingRecord()
    {
        var c = Gdd03();
        var s = LedgerArrivalsTests.Fixture(c);
        Assert.Contains(s.PopulationLedger.Units, u => u.Status == PopulationStatus.ActiveSoldier);
        var lost = s.Clone();
        lost.PopulationLedger.RegisterCombatLoss(lost.PopulationLedger.Units[0].Id, "fixture-loss");
        PopulationSystem.Apply(c, s, s.Date.AddDays(7), Week, new List<Cause>());
        PopulationSystem.Apply(c, lost, lost.Date.AddDays(7), Week, new List<Cause>());
        var rate = (decimal)c.RulesAt(s.Date).NaturalIncreasePerYear * (decimal)Week;
        Assert.Equal(rate * s.PopulationLedger.Units.Count, s.PopulationLedger.NaturalIncreaseCarry);
        Assert.Equal(rate * (s.PopulationLedger.Units.Count - 1), lost.PopulationLedger.NaturalIncreaseCarry);
    }

    // R-POP-003: Clone keeps the carry and the records it has added, the gdd-0.3 ledger section writes them, and a replay
    // from the clone adds the same records with the same keys (nobody twice). A ledger with no increase yet, and every
    // gdd-0.1 game, writes no natural-increase line, so their hashes are as before.
    [Fact]
    public void CloneAndTheCanonicalTextKeepTheCarry()
    {
        var c = Gdd03();
        var s = LedgerArrivalsTests.Fixture(c);
        Assert.DoesNotContain("natural-increase", s.Describe());
        Assert.DoesNotContain("natural-increase", Simulation.NewGame(HistoricalPopulationTests.Real(false), 1, Mode.Historical).Describe());
        PopulationSystem.Apply(c, s, s.Date.AddDays(7), Week, new List<Cause>());
        Assert.Contains("natural-increase", s.Describe());
        var saved = s.Clone();
        Assert.Equal(s.PopulationLedger.NaturalIncreaseCarry, saved.PopulationLedger.NaturalIncreaseCarry);
        Assert.Equal(s.Hash(), saved.Hash());
        // Two turns that add no record and differ only in the carry hash apart.
        var shorter = LedgerArrivalsTests.Fixture(c);
        PopulationSystem.Apply(c, shorter, shorter.Date.AddDays(7), Week / 2, new List<Cause>());
        Assert.Equal(s.PopulationLedger.Units.Select(u => u.Id), shorter.PopulationLedger.Units.Select(u => u.Id));
        Assert.NotEqual(s.PopulationLedger.NaturalIncreaseCarry, shorter.PopulationLedger.NaturalIncreaseCarry);
        Assert.NotEqual(s.Hash(), shorter.Hash());
        for (var i = 0; i < 6; i++)
        {
            PopulationSystem.Apply(c, s, s.Date.AddDays(7), Week, new List<Cause>());
            PopulationSystem.Apply(c, saved, saved.Date.AddDays(7), Week, new List<Cause>());
        }
        Assert.True(s.PopulationLedger.NaturalIncreaseRecords > 0);
        Assert.Equal(s.PopulationLedger.Canonical(), saved.PopulationLedger.Canonical());
        Assert.Equal(s.Hash(), saved.Hash());
    }

    // R-CITY-003, R-CITY-008: a turn's CBS arrivals are counted once, as Cities.Arrive's refugee records, and its natural
    // increase once, as Apply's free civilian records on every living record (the turn's arrivals included, since Arrive
    // runs first); GameState.Population is not touched, and the total rises by exactly those records' people.
    [Fact]
    public void ArrivalsAndIncreaseAreEachCountedOnce()
    {
        var c = Gdd03();
        var r = c.RulesAt(LedgerArrivalsTests.From).Mvp!;
        var s = LedgerArrivalsTests.Fixture(c);
        var to = LedgerArrivalsTests.From.AddMonths(3);   // a quarter: both the arrivals and the increase are above one record
        var population = s.Population.ToList();
        var total = s.TotalPopulation;
        var yf = (to - LedgerArrivalsTests.From).Days / 365.0;
        Cities.Arrive(c, s, r, to, new List<Cause>());
        var units = Cities.ArrivedUnits(c, LedgerArrivalsTests.From, to, r.PeoplePerUnit);
        Assert.Equal(units, LedgerArrivalsTests.Arrived(s).Count);
        var owed = Owed(c, s, yf);
        var whole = (int)decimal.Floor(owed);
        Assert.True(units > 0 && whole > 0);
        PopulationSystem.Apply(c, s, to, yf, new List<Cause>());
        Assert.Equal(units, LedgerArrivalsTests.Arrived(s).Count);
        Assert.Equal(whole, s.PopulationLedger.NaturalIncreaseRecords);
        Assert.Equal(owed - whole, s.PopulationLedger.NaturalIncreaseCarry);
        Assert.Equal(population, s.Population.ToList());
        s.PopulationLedger.Reconcile();
        Assert.Equal(total + (units + whole) * r.PeoplePerUnit, s.TotalPopulation);
    }
}
