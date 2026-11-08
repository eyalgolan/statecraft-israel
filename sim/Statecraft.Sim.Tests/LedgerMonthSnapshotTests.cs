using Statecraft.Sim;

/// <summary>#1725 PR 7 (#1716): the live month snapshot of a game with an initialized Historical ledger,
/// <c>Gdd03SyntheticMonthContext.CreateLive</c>. Owner, 8 November 2026, "One snapshot per turn (Recommended)": one month
/// snapshot is taken at the start of the turn, after the player's orders, and harvest, Industry income and Food all read
/// it. "Yes, recheck (Recommended)": a consumer rechecks an entry against the live state before using it, so the
/// snapshot keeps each entry's id and opening record. These tests build the snapshot only; no turn takes one yet.</summary>
public class LedgerMonthSnapshotTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static string Ledger(GameState s) => string.Join("\n", s.PopulationLedger.Canonical());

    // The synthetic month the field fixtures build for the same people: every authored field available, one slot each.
    static Gdd03SyntheticMonthContext Synthetic(Content c, GameState s) => Gdd03SyntheticMonthContext.CreateSynthetic(
        SaveGame.Gdd03Ruleset, s.Turn, s.PopulationLedger.Units,
        Gdd03FieldFixture.Fields(c).Select(f => new Gdd03SyntheticFacilitySnapshot(f.Id, true, 1)),
        Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());

    // #1716, R-FOOD-002 (#2470): the snapshot holds every living own record of the ledger once, in id order, with its
    // opening record; the facility entries are the ledger's bound field Work targets, in ordinal order, so each farmer
    // on a field is work-eligible (an upper bound: the consumer rechecks it).
    [Fact]
    public void TheSnapshotHoldsEveryLivingOwnRecordOnce()
    {
        var c = Gdd03();
        var s = Dark(c);
        var month = Gdd03SyntheticMonthContext.CreateLive(c, s);
        var units = s.PopulationLedger.Units;
        Assert.True(units.Count > 0);
        Assert.Equal(units.Select(u => u.Id), month.OpeningPeople.Select(p => p.Id));
        Assert.Equal(units, month.OpeningPeople);
        Assert.Equal(units.Count, Pools.Rations(month.OpeningPeople));
        Assert.Equal(s.Turn, month.MonthOrdinal);
        var fields = units.Where(u => u.Assignment.Kind == PopulationAssignmentKind.Work).Select(u => u.Assignment.Target!)
            .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.NotEmpty(fields);
        Assert.All(fields, f => Assert.StartsWith("field/", f));
        Assert.Equal(fields, month.Facilities.Select(f => f.Id));
        Assert.All(month.Facilities, f => Assert.True(f.AvailableAtMonthFreeze && f.OpeningCapacity == 1));
        Assert.Equal(units.Where(u => u.Assignment.Kind == PopulationAssignmentKind.Work).Select(u => u.Id), month.WorkEligibleIds);
        Assert.Empty(month.CourseEligibleIds); Assert.Empty(month.ReceptionEligibleIds);
        Assert.Empty(month.Housing); Assert.Empty(month.PendingEntries);
    }

    // #2470: "a ledger change before step 7 (a combat loss, an arrival) does not move that month's need". The snapshot is
    // frozen: a later loss or arrival changes the live ledger, not the snapshot. "Yes, recheck (Recommended)": the lost
    // record's id is still in the snapshot, so a consumer can find it gone from the live ledger.
    [Fact]
    public void TheSnapshotIsFrozenAgainstALaterLossOrArrival()
    {
        var c = Gdd03();
        var s = LedgerArrivalsTests.Fixture(c);
        var month = Gdd03SyntheticMonthContext.CreateLive(c, s);
        var ids = month.OpeningPeople.Select(p => p.Id).ToList();
        var soldier = s.PopulationLedger.Units.First(u => u.Status == PopulationStatus.ActiveSoldier).Id;

        s.PopulationLedger.RegisterCombatLoss(soldier, "test/loss");
        var r = c.RulesAt(LedgerArrivalsTests.From).Mvp!;
        Cities.Arrive(c, s, r, LedgerArrivalsTests.To, new List<Cause>());

        Assert.True(LedgerArrivalsTests.Arrived(s).Count > 0);
        Assert.NotEqual(ids, s.PopulationLedger.Units.Select(u => u.Id));
        Assert.Equal(ids, month.OpeningPeople.Select(p => p.Id));
        Assert.Contains(month.OpeningPeople, p => p.Id == soldier);
        Assert.DoesNotContain(s.PopulationLedger.Units, u => u.Id == soldier);
        Assert.Equal(ids.Count, Pools.Rations(month.OpeningPeople));
    }

    // Read-only: building the snapshot changes neither the state's hash nor the ledger's canonical text.
    [Fact]
    public void BuildingTheSnapshotChangesNothing()
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash(); var ledger = Ledger(s); var described = s.Describe();
        Gdd03SyntheticMonthContext.CreateLive(c, s);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(ledger, Ledger(s));
        Assert.Equal(described, s.Describe());
    }

    // Determinism: two builds, and a build on a clone, are equal entry for entry.
    [Fact]
    public void TwoBuildsAreEqual()
    {
        var c = Gdd03();
        var s = Dark(c);
        var a = Gdd03SyntheticMonthContext.CreateLive(c, s);
        var b = Gdd03SyntheticMonthContext.CreateLive(c, s.Clone());
        Assert.Equal(a.MonthOrdinal, b.MonthOrdinal);
        Assert.Equal(a.OpeningPeople, b.OpeningPeople);
        Assert.Equal(a.Facilities.Select(f => (f.Id, f.AvailableAtMonthFreeze, f.OpeningCapacity)),
            b.Facilities.Select(f => (f.Id, f.AvailableAtMonthFreeze, f.OpeningCapacity)));
        Assert.Equal(a.WorkEligibleIds, b.WorkEligibleIds);
    }

    // R-POP-002: only an initialized Historical ledger has a live snapshot. An Open ledger, a synthetic one (no mode) and
    // an aggregate game are refused before anything is built, with the state unchanged.
    [Theory]
    [InlineData("open")] [InlineData("synthetic")] [InlineData("aggregate")]
    public void AnOpenSyntheticOrAggregateGameIsRefused(string kind)
    {
        var c = kind == "aggregate" ? Gdd03() : PopulationLedgerTests.Content();
        var s = kind == "aggregate" ? Simulation.NewGame(c, 1, Mode.Historical)
            : PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
                new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: kind == "open" ? Mode.Open : null);
        Assert.False(s.PopulationLedger.IsHistorical);
        var hash = s.Hash();
        Assert.Throws<NotSupportedException>(() => Gdd03SyntheticMonthContext.CreateLive(c, s));
        Assert.Equal(hash, s.Hash());
    }

    // R-FOOD-002 (#2470): Pools.Consume's month overload computes the same need, stock and causes given this live snapshot
    // as given the synthetic one of the same people.
    [Fact]
    public void ConsumeReadsTheLiveSnapshotAsTheSyntheticOne()
    {
        var c = Gdd03();
        var live = Dark(c); live.Food = 10;
        var synthetic = live.Clone();
        var r = c.RulesAt(live.Date).Mvp!;
        var liveWhy = new List<Cause>(); var syntheticWhy = new List<Cause>();

        Pools.Consume(c, live, r, Gdd03SyntheticMonthContext.CreateLive(c, live), liveWhy);
        Pools.Consume(c, synthetic, r, Synthetic(c, synthetic), syntheticWhy);

        Assert.Equal(live.PopulationLedger.Units.Count, live.FoodNeed);
        Assert.Equal((synthetic.Food, synthetic.FoodNeed, synthetic.FoodUnmet), (live.Food, live.FoodNeed, live.FoodUnmet));
        Assert.Equal(syntheticWhy.Select(x => (x.Key, x.Delta, x.Message)), liveWhy.Select(x => (x.Key, x.Delta, x.Message)));
        Assert.Equal(synthetic.Hash(), live.Hash());
    }
}
