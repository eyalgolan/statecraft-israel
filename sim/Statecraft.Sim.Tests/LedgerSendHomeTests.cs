using Statecraft.Sim;

/// <summary>#1699 part 2, #1725 PR 7: Send home (R-UNIT-005, Demobilization.Give) runs on a game with an initialized
/// Historical ledger, the dark start (R-POP-002). The owner's rulings, 8 November 2026 (#1725 issuecomment-6726421643 and
/// issuecomment-6727007834):
/// - "Every soldier in it (Recommended)": every soldier record serving the formation's target leaves service.
/// - "Refuse with a reason (Recommended)": a formation whose strength differs from its serving records is refused with a
///   plain reason, and nothing changes.
/// - "Nearest city, unemployed (Recommended)": the records become free civilians without an assignment in the nearest of the
///   twelve cities (R-UNIT-005's nearest-city rule), so that city houses them from the same turn.
/// - "Back to Arms stock (Recommended)": each record's issued military item is cleared and returned to the stock once.
/// - "Settled in (Recommended)": the records' reception ticks are reception.turns.
/// - "Keep both (Recommended)": the military qualification and combat XP stay on the record.
/// - "No, records only (Recommended)": no aggregate city field is written.
/// The owner's ruling, 8 November 2026, "Yes, one Why line (Recommended)": Send home writes one Why line naming the Arms
/// returned to the stock, as training's Cancel does (R-TRAIN-002).
/// The tests call Demobilization.Give directly: Simulation.Step and TurnResolver.Resolve still refuse a ledger game.</summary>
public class LedgerSendHomeTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    // Every armistice signed, so any of the player's units may go home (R-UNIT-005's Check passes).
    static GameState Signed(GameState s)
    {
        foreach (var f in Prisoners.Fronts) s.ChosenOptions["armistice_" + f] = "sign";
        return s;
    }

    static GameState Dark(Content c) => Signed(Simulation.NewGame(c, 1, Mode.Historical, darkStart: true));

    static GameState Aggregate(Content c) => Signed(Simulation.NewGame(c, 1, Mode.Historical));

    // The first of the player's opening formations (unit id i is c.StartingForces[i], StartingForces.Init).
    static int Opening(Content c, GameState s) => s.Units.Values
        .Where(u => u.Side == Movement.PlayerSide && u.Id < c.StartingForces.Count).Min(u => u.Id);

    static string Target(Content c, int unit) => HistoricalPopulation.FormationPrefix + c.StartingForces[unit].Row;

    static List<PopulationUnit> Serving(GameState s, string target) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target == target).OrderBy(p => p.Id).ToList();

    static int Issued(GameState s) => s.PopulationLedger.Units.Count(p => p.Equipment.MilitaryId != null);

    static string Cities(GameState s) => string.Join(";", s.Cities.Values.Select(x => x.Canonical()));

    static IEnumerable<(string, string, double, string, string)> Lines(IEnumerable<Cause> why) =>
        why.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key));

    // "Every soldier in it", "Nearest city, unemployed", "Settled in", "Keep both": each serving record keeps its id,
    // qualification, combat XP, profession, tier and work XP, and becomes a settled-in free civilian without an assignment in
    // the nearest city (region plus city). The unit leaves the map, no id is made or lost, and the first Why line is the
    // aggregate game's.
    [Fact]
    public void SendHomeTurnsTheFormationsSoldiersIntoSettledCiviliansOfTheNearestCity()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var unit = Opening(c, s); var u = s.Units[unit];
        var serving = Serving(s, Target(c, unit));
        Assert.Equal(u.Strength, serving.Count);
        Assert.NotEmpty(serving);
        var city = Demobilization.Nearest(c, u.Q, u.R);
        var region = c.PlayerRegions.AtLocation(city)!.Id;
        var next = s.PopulationLedger.NextPopulationId; var count = s.PopulationLedger.Units.Count;
        var housed = Statecraft.Sim.Cities.Housed(s, s.Cities[city]);
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Assert.Null(Demobilization.Give(c, s, unit, null, why));
        Assert.Null(Demobilization.Give(c, twin, unit, null, twinWhy));

        Assert.False(s.Units.ContainsKey(unit));
        Assert.Equal(Lines(twinWhy), Lines(why.Take(twinWhy.Count)));
        Assert.Equal((next, count), (s.PopulationLedger.NextPopulationId, s.PopulationLedger.Units.Count));
        Assert.Empty(Serving(s, Target(c, unit)));
        var received = c.RequirePopulationInteger("reception.turns");
        foreach (var was in serving)
        {
            var p = s.PopulationLedger.Get(was.Id);
            Assert.Equal(PopulationStatus.FreeCivilian, p.Status);
            Assert.Equal(PopulationAssignmentKind.Unassigned, p.Assignment.Kind);
            Assert.Equal((region, city), (p.Location!.Region, p.Location.City));
            Assert.Equal(received, p.ReceptionTicks);
            Assert.Equal((was.Qualification, was.CombatXp), (p.Qualification, p.CombatXp));
            Assert.NotEqual("", p.Qualification);
            Assert.Equal((was.Profession, was.Tier, was.WorkXp, was.Equipment.ToolsId), (p.Profession, p.Tier, p.WorkXp, p.Equipment.ToolsId));
        }
        Assert.Equal(housed + serving.Count, Statecraft.Sim.Cities.Housed(s, s.Cities[city]));
        s.PopulationLedger.Reconcile();
    }

    // "Back to Arms stock": each record's issued military item leaves the record and the stock rises by those items, once.
    // Spare plus issued stays the same. (Known effect: the opening items were issued apart from the stock, so this adds
    // Arms the stock never paid for.) The aggregate game returns none.
    [Fact]
    public void SendHomeReturnsEachIssuedItemToTheStockOnce()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var unit = Opening(c, s);
        var serving = Serving(s, Target(c, unit));
        var items = serving.Count(p => p.Equipment.MilitaryId != null);
        Assert.Equal(serving.Count, items);
        var arms = s.Arms; var issued = Issued(s); var twinArms = twin.Arms;
        var city = Demobilization.Nearest(c, s.Units[unit].Q, s.Units[unit].R);
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Assert.Null(Demobilization.Give(c, s, unit, null, why));
        Assert.Null(Demobilization.Give(c, twin, unit, null, twinWhy));

        Assert.Equal(arms + items, s.Arms);
        Assert.Equal(issued - items, Issued(s));
        Assert.Equal(arms + issued, s.Arms + Issued(s));
        Assert.All(serving, was => Assert.Null(s.PopulationLedger.Get(was.Id).Equipment.MilitaryId));
        Assert.All(serving, was => Assert.False(s.PopulationLedger.Get(was.Id).Equipment.MilitaryPaid));
        Assert.Equal(twinArms, twin.Arms);

        // "Yes, one Why line (Recommended)": one line, after the aggregate game's, names the Arms returned to the stock,
        // its delta the Arms added, as Cancel's training_arms_returned line does. The aggregate game writes none.
        Assert.Equal(twinWhy.Count + 1, why.Count);
        var line = why.Last();
        Assert.Equal(("city", "arms", (double)items, FormattableString.Invariant($"{PlayerText.Settlement(c, city)}: unit {unit} returned {items} Arms"), "sent_home_arms_returned"),
            (line.System, line.Target, line.Delta, line.Message, line.Key));
        Assert.Equal(new[] { city, unit.ToString(System.Globalization.CultureInfo.InvariantCulture) }, line.Args);
        Assert.Single(why, x => x.Key == "sent_home_arms_returned");
        Assert.DoesNotContain(twinWhy, x => x.Key == "sent_home_arms_returned");
    }

    // "Yes, one Why line": a formation whose records hold no issued military item returns no Arms, so no Arms line is
    // written and the Why lines are the aggregate game's.
    [Fact]
    public void SendHomeWithNoIssuedItemWritesNoArmsLine()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var unit = Opening(c, s);
        var bare = Serving(s, Target(c, unit)).Select(p => new PopulationUnit(p.Id, p.Status, p.ReceptionTicks, p.Location,
            p.Assignment, p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp,
            new PopulationEquipment(p.Equipment.ToolsId, p.Equipment.ToolsPaid), p.Captor, p.Home)).ToList();
        s.PopulationLedger.Commit(bare, Array.Empty<PopulationTarget>());
        var arms = s.Arms;
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Assert.Null(Demobilization.Give(c, s, unit, null, why));
        Assert.Null(Demobilization.Give(c, twin, unit, null, twinWhy));

        Assert.Equal(arms, s.Arms);
        Assert.Equal(Lines(twinWhy), Lines(why));
        Assert.DoesNotContain(why, x => x.Key == "sent_home_arms_returned");
    }

    // "No, records only": the aggregate city fields are not written on a ledger game (the aggregate game adds the strength).
    [Fact]
    public void SendHomeWritesNoAggregateCityField()
    {
        var c = Gdd03();
        var s = Dark(c);
        var unit = Opening(c, s);
        var cities = Cities(s);
        Assert.Null(Demobilization.Give(c, s, unit, null, new List<Cause>()));
        Assert.Equal(cities, Cities(s));
    }

    // "Refuse with a reason": a formation whose strength differs from its serving records is refused with a plain reason,
    // not an exception, and the state, ledger and hash are unchanged.
    [Fact]
    public void AStrengthMismatchIsRefusedWithAReasonAndChangesNothing()
    {
        var c = Gdd03();
        var s = Dark(c);
        var unit = Opening(c, s); var u = s.Units[unit];
        var serving = Serving(s, Target(c, unit));
        s.PopulationLedger.RegisterCombatLoss(serving[0].Id, "test/mismatch");
        var before = s.Describe(); var hash = s.Hash(); var ledger = s.PopulationLedger.Canonical().ToList();
        var why = new List<Cause>();

        Assert.Equal(string.Format(System.Globalization.CultureInfo.InvariantCulture, Training.LedgerRefusedStrength,
            unit, serving.Count - 1, u.Strength), Demobilization.Give(c, s, unit, null, why));

        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash());
        Assert.Equal(ledger, s.PopulationLedger.Canonical().ToList());
        Assert.True(s.Units.ContainsKey(unit));
        Assert.Empty(why);
    }

    // R-UNIT-005's own refusals are unchanged on the ledger: with no armistice signed, the reason is main's and nothing
    // changes.
    [Fact]
    public void NoArmisticeIsRefusedWithMainsReasonOnTheLedger()
    {
        var c = Gdd03();
        var s = Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);
        var twin = Simulation.NewGame(c, 1, Mode.Historical);
        var unit = Opening(c, s);
        var before = s.Describe(); var hash = s.Hash(); var why = new List<Cause>();
        var reason = Demobilization.Give(c, s, unit, null, why);
        Assert.NotNull(reason);
        Assert.Equal(Demobilization.Give(c, twin, unit, null, new List<Cause>()), reason);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    // Each of the twelve cities is a bound ledger location, so the nearest city can always hold the records.
    [Fact]
    public void EveryCityIsABoundLedgerLocation()
    {
        var c = Gdd03();
        var s = Dark(c);
        var cities = s.PopulationLedger.Units.Where(p => p.Location?.City != null).Select(p => p.Location!.City!).ToHashSet();
        Assert.All(c.Cities, x => Assert.Contains(x.Id, cities));
    }

    // R-POP-002: an Open or synthetic ledger is still refused, with the state unchanged.
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AnOpenOrSyntheticLedgerIsStillRefused(bool open)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(),
            new[] { new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: open ? Mode.Open : null);
        Assert.Equal((true, false), (s.PopulationLedger.IsInitialized, s.PopulationLedger.IsHistorical));
        var why = new List<Cause>();
        var before = s.Describe(); var hash = s.Hash();
        Assert.Contains("integration is held",
            Assert.Throws<NotSupportedException>(() => Demobilization.Give(c, s, 0, null, why)).Message);
        Assert.Equal(before, s.Describe()); Assert.Equal(hash, s.Hash()); Assert.Empty(why);
    }

    // R-POP-003: the same order on the same seed gives the same ledger and state hash, and a clone's run matches.
    [Fact]
    public void SendHomeIsDeterministic()
    {
        var c = Gdd03();
        var a = Dark(c); var b = Dark(c); var clone = a.Clone();
        var unit = Opening(c, a);
        Assert.Null(Demobilization.Give(c, a, unit, null, new List<Cause>()));
        Assert.Null(Demobilization.Give(c, b, unit, null, new List<Cause>()));
        Assert.Null(Demobilization.Give(c, clone, unit, null, new List<Cause>()));
        Assert.Equal(a.PopulationLedger.Canonical(), b.PopulationLedger.Canonical());
        Assert.Equal(a.Hash(), b.Hash()); Assert.Equal(a.Hash(), clone.Hash());
    }
}
