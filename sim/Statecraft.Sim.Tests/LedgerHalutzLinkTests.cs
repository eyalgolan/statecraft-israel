using Statecraft.Sim;

/// <summary>#1725 PR 7: the halutz map unit's link to its pioneer records, and the pioneer's return (R-QUAL-003) and
/// reassignment (R-QUAL-005) on a game with an initialized Historical ledger, the dark start (R-POP-002). The owner's
/// rulings, 9 November 2026:
/// - "Add the link first (Recommended)": the halutz course records which pioneer records form each map unit, and Return
///   and ReassignPioneers use exactly those records.
/// - "List on the unit (Recommended)": the map unit keeps a sorted list of its records' ids, hashed only when set, like a
///   cohort's members; the records stay free, unassigned pioneers.
/// - "City counts, as today (Recommended)": a returned pioneer's service stays in City.Qualified, as on the aggregate game.
/// - "Nearest city, settled (Recommended)": a returned record goes to the nearest city, unemployed, reception ticks =
///   reception.turns, as Send home does.
/// - "Free pioneers by service (Recommended)": Reassign takes the city's free, unassigned pioneer records, most service
///   first then lowest id, and they go back to region-only with no city, as at graduation.
/// - "Yes, keep the link (Recommended)": a veteran pioneer unit made by promotion keeps the link, and a returned veteran
///   record counts for the veteran track.
/// The tests call Training and Qualification directly: Simulation.Step still refuses a ledger game (R-POP-002).</summary>
public class LedgerHalutzLinkTests
{
    const string TelAviv = "tel_aviv";

    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    static GameState Dark(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);
        s.Arms = 200; s.Treasury = 100; s.Debt = 0;
        return s;
    }

    static GameState Aggregate(Content c)
    {
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        s.Arms = 200; s.Treasury = 100; s.Debt = 0;
        return s;
    }

    static string Region(Content c, string city) => c.PlayerRegions.AtLocation(city)!.Id;

    static IEnumerable<(string, string, double, string, string)> Lines(IEnumerable<Cause> why) => why.Select(x => (x.System, x.Target, x.Delta, x.Message, x.Key));

    // The training order's pick ("Unskilled first, keep (Rec.)"): free civilians without an assignment in the city,
    // Profession None first, then ascending id.
    static List<int> Idle(GameState s, string city, int n) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned && p.Location?.City == city)
        .OrderBy(p => p.Profession == CivilianProfession.None ? 0 : 1).ThenBy(p => p.Id).Take(n).Select(p => p.Id).ToList();

    // "Free pioneers by service (Recommended)": the city's free, unassigned pioneer records, ascending id.
    static List<int> FreePioneers(GameState s, string city) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned
            && p.Profession == CivilianProfession.Pioneer && p.Location?.City == city)
        .Select(p => p.Id).OrderBy(x => x).ToList();

    /// <summary>Trains <paramref name="n"/> halutz groups in Tel Aviv to graduation. Returns the records picked (ascending
    /// id on the ledger, empty on the aggregate game) and the new map units, ascending id.</summary>
    static (List<int> Records, List<int> Units) Graduate(Content c, GameState s, int n, List<Cause> why)
    {
        var picked = s.PopulationLedger.IsInitialized ? Idle(s, TelAviv, n).OrderBy(x => x).ToList() : new List<int>();
        var before = s.Units.Keys.ToHashSet();
        Assert.Null(Training.Start(c, s, TelAviv, "halutz", n, why));
        var turns = c.UnitTypes.First(t => t.Id == "halutz").TrainingTurns;
        for (int t = 0; t < turns; t++) Training.Progress(c, s, why);
        return (picked, s.Units.Keys.Where(k => !before.Contains(k)).OrderBy(k => k).ToList());
    }

    // A tuple compares an array by reference, so record lists are compared as text.
    static string Ids(params int[] ids) => string.Join(",", ids);
    static string Ids(IReadOnlyList<int> ids) => string.Join(",", ids);

    static void SetService(GameState s, int id, int service, string? type = null)
    {
        var u = s.Units[id];
        s.Units[id] = new Unit(u.Id, type ?? u.Type, u.Q, u.R, service, u.Side, u.Strength, u.Order, u.OrderTarget, u.HomeCity, u.Full, u.CombatXp, u.Records);
    }

    static Dictionary<int, string> Others(GameState s, ICollection<int> except) => s.PopulationLedger.Units
        .Where(p => !except.Contains(p.Id)).ToDictionary(p => p.Id, p => string.Join("|", p.Assignment.Canonical(), p.Location?.Canonical(), p.Status, p.ReceptionTicks, p.Profession, p.Tier, p.WorkXp));

    // "Add the link first" and "List on the unit": the cohort's records, ascending, form its map units in id order, one
    // record per group; the records stay free, unassigned pioneers ("Free pioneer, not housed (Rec.)"). The unit's list is
    // in the state hash only when set, so the aggregate game's units hash as before.
    [Fact]
    public void AGraduateHalutzUnitIsLinkedToItsRecord()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var (records, units) = Graduate(c, s, 2, new List<Cause>());
        var (_, twinUnits) = Graduate(c, twin, 2, new List<Cause>());
        Assert.Equal(2, units.Count);
        Assert.Equal(twinUnits, units);
        for (int i = 0; i < units.Count; i++)
        {
            var u = s.Units[units[i]];
            Assert.Equal(new[] { records[i] }, u.Records);
            Assert.Equal(twin.Units[units[i]].Canonical() + "|records=" + records[i], u.Canonical());
            var p = s.PopulationLedger.Get(records[i]);
            Assert.Equal((PopulationStatus.FreeCivilian, PopulationAssignmentKind.Unassigned, CivilianProfession.Pioneer, (string?)null),
                (p.Status, p.Assignment.Kind, p.Profession, p.Location!.City));
        }
        Assert.All(twinUnits, id => Assert.Null(twin.Units[id].Records));
        Assert.DoesNotContain("records=", string.Join(";", twin.Units.Values.Select(x => x.Canonical())));
        s.PopulationLedger.Reconcile();
    }

    // R-QUAL-003 on the ledger. "Nearest city, settled": exactly the unit's record becomes a free civilian without a job in
    // the nearest city (its region plus the city), reception ticks = reception.turns, its profession, tier and work XP kept.
    // "City counts, as today": the unit's service goes into the city's qualified halutz groups, as the aggregate game does;
    // the civilian count is the records'. No other record changes, and the Why line is the aggregate game's.
    [Fact]
    public void ReturnOnTheLedgerMovesExactlyTheUnitsRecord()
    {
        var c = Gdd03();
        var s = Dark(c); var twin = Aggregate(c);
        var (records, units) = Graduate(c, s, 2, new List<Cause>());
        Graduate(c, twin, 2, new List<Cause>());
        SetService(s, units[0], 3); SetService(twin, units[0], 3);
        var before = s.PopulationLedger.Get(records[0]);
        var others = Others(s, new[] { records[0] });
        var civilians = s.Cities[TelAviv].Civilians;
        var why = new List<Cause>(); var twinWhy = new List<Cause>();

        Assert.Null(Qualification.Return(c, s, units[0], why));
        Assert.Null(Qualification.Return(c, twin, units[0], twinWhy));

        Assert.False(s.Units.ContainsKey(units[0]));
        Assert.Equal(Lines(twinWhy), Lines(why));
        var p = s.PopulationLedger.Get(records[0]);
        Assert.Equal((PopulationStatus.FreeCivilian, PopulationAssignmentKind.Unassigned, c.RequirePopulationInteger("reception.turns")),
            (p.Status, p.Assignment.Kind, p.ReceptionTicks));
        Assert.Equal((Region(c, TelAviv), (string?)TelAviv), (p.Location!.Region, p.Location.City));
        Assert.Equal((before.Profession, before.Tier, before.WorkXp, before.Qualification, before.CombatXp),
            (p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp));
        Assert.Equal(others, Others(s, new[] { records[0] }));
        Assert.Equal(new[] { 3 }, s.Cities[TelAviv].Qualified.Halutzim);
        Assert.Equal(twin.Cities[TelAviv].Qualified.Canonical(), s.Cities[TelAviv].Qualified.Canonical());
        Assert.Equal(civilians, s.Cities[TelAviv].Civilians);
        s.PopulationLedger.Reconcile();
    }

    // R-QUAL-005 on the ledger. "Free pioneers by service": the city's free, unassigned pioneer records in ascending id are
    // paired with its qualified halutz groups, most service first; each new unit is linked to its record, which goes back
    // to region-only with no city, as at graduation. Asking for more than the qualified groups is refused, state unchanged.
    [Fact]
    public void ReassignPioneersOnTheLedgerUsesExactlyThoseRecords()
    {
        var c = Gdd03();
        var s = Dark(c);
        var (records, units) = Graduate(c, s, 2, new List<Cause>());
        SetService(s, units[0], 1); SetService(s, units[1], 3);
        var why = new List<Cause>();
        Assert.Null(Qualification.Return(c, s, units[0], why));
        Assert.Null(Qualification.Return(c, s, units[1], why));
        var free = FreePioneers(s, TelAviv);
        Assert.True(records.All(free.Contains));
        var civilians = s.Cities[TelAviv].Civilians;

        var first = s.NextId;
        Assert.Null(Qualification.ReassignPioneers(c, s, TelAviv, "halutz", 1, why));
        var u = s.Units[first];
        Assert.Equal(("halutz", 3, Ids(free[0])), (u.Type, u.Service, Ids(u.Records!)));
        var hex = c.SettlementHexes.First(h => h.Id == TelAviv);
        Assert.Equal((hex.Q, hex.R), (u.Q, u.R));
        var p = s.PopulationLedger.Get(free[0]);
        Assert.Equal((PopulationStatus.FreeCivilian, PopulationAssignmentKind.Unassigned, CivilianProfession.Pioneer, Region(c, TelAviv), (string?)null),
            (p.Status, p.Assignment.Kind, p.Profession, p.Location!.Region, p.Location.City));
        Assert.Equal(new[] { 1 }, s.Cities[TelAviv].Qualified.Halutzim);
        Assert.Equal(civilians, s.Cities[TelAviv].Civilians);

        var others = Others(s, new[] { free[1] });
        var second = s.NextId;
        Assert.Null(Qualification.ReassignPioneers(c, s, TelAviv, "halutz", 1, why));
        Assert.Equal((1, Ids(free[1])), (s.Units[second].Service, Ids(s.Units[second].Records!)));
        Assert.Equal(others, Others(s, new[] { free[1] }));
        Assert.Empty(s.Cities[TelAviv].Qualified.Halutzim);

        var hash = s.Hash();
        Assert.Equal("Tel Aviv has 0 qualified halutzim", Qualification.ReassignPioneers(c, s, TelAviv, "halutz", 1, why));
        Assert.Equal(hash, s.Hash());
        s.PopulationLedger.Reconcile();
    }

    // R-QUAL-006 on the ledger (owner, 9 November 2026, "Yes, lower it (Recommended)"): a Train order that takes a returned
    // pioneer record lowers the city's qualified pioneer count by one, halutz groups with the least service first, so the
    // count and the city's pioneer records stay equal and a later Reassign finds its records.
    [Fact]
    public void ATrainOrderThatTakesAReturnedPioneerLowersTheQualifiedCount()
    {
        var c = Gdd03();
        var s = Dark(c);
        var (records, units) = Graduate(c, s, 2, new List<Cause>());
        SetService(s, units[0], 1); SetService(s, units[1], 3);
        var why = new List<Cause>();
        Assert.Null(Qualification.Return(c, s, units[0], why));
        Assert.Null(Qualification.Return(c, s, units[1], why));
        var free = FreePioneers(s, TelAviv);
        Assert.Equal(2, free.Count);
        var unskilled = s.PopulationLedger.Units.Count(p => p.Status == PopulationStatus.FreeCivilian && p.Assignment.Kind == PopulationAssignmentKind.Unassigned
            && p.Location?.City == TelAviv && p.Profession == CivilianProfession.None);

        // Only unqualified civilians taken: the count stays.
        Assert.Null(Training.Start(c, s, TelAviv, "halutz", unskilled, why));
        Assert.Equal(new[] { 1, 3 }, s.Cities[TelAviv].Qualified.Halutzim);

        // One more takes the lowest-id pioneer record: the group with the least service loses its qualification.
        Assert.Null(Training.Start(c, s, TelAviv, "halutz", 1, why));
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(free[0]).Status);
        Assert.Equal(new[] { 3 }, s.Cities[TelAviv].Qualified.Halutzim);
        Assert.Equal(FreePioneers(s, TelAviv).Count, s.Cities[TelAviv].Qualified.Halutzim.Count + s.Cities[TelAviv].Qualified.Veterans);

        var id = s.NextId;
        Assert.Null(Qualification.ReassignPioneers(c, s, TelAviv, "halutz", 1, why));
        Assert.Equal((3, Ids(free[1])), (s.Units[id].Service, Ids(s.Units[id].Records!)));
        s.PopulationLedger.Reconcile();
    }

    // "Yes, keep the link": a veteran pioneer unit keeps its record; returned, the record counts for the veteran track, and
    // reassigned as a veteran it comes back with veteran_halutz_service and the same link rule.
    [Fact]
    public void AVeteranPioneerOnTheLedgerReturnsAndIsReassignedWithItsRecord()
    {
        var c = Gdd03();
        var s = Dark(c);
        var (records, units) = Graduate(c, s, 1, new List<Cause>());
        var service = c.RulesAt(s.Date).Mvp!.VeteranHalutzService!.Value;
        SetService(s, units[0], service, Projects.VeteranType);
        var why = new List<Cause>();
        Assert.Null(Qualification.Return(c, s, units[0], why));
        Assert.Equal((1, 0), (s.Cities[TelAviv].Qualified.Veterans, s.Cities[TelAviv].Qualified.Halutzim.Count));
        Assert.Equal(TelAviv, s.PopulationLedger.Get(records[0]).Location!.City);
        var free = FreePioneers(s, TelAviv);
        var id = s.NextId;
        Assert.Null(Qualification.ReassignPioneers(c, s, TelAviv, Projects.VeteranType, 1, why));
        Assert.Equal((Projects.VeteranType, service, Ids(free[0])), (s.Units[id].Type, s.Units[id].Service, Ids(s.Units[id].Records!)));
        Assert.Null(s.PopulationLedger.Get(free[0]).Location!.City);
        Assert.Equal(0, s.Cities[TelAviv].Qualified.Veterans);
        s.PopulationLedger.Reconcile();
    }

    // "Yes, keep the link": the promotion to veteran pioneer (R-VET-001) keeps the unit's record list, as do the other
    // changes of a unit (an order, a move). Run on the GDD fixture, where Simulation.Step runs.
    [Fact]
    public void ThePromotionKeepsTheUnitsRecords()
    {
        var files = Fixture.Gdd();
        files["rules.yaml"] = files["rules.yaml"].Replace("veteran_halutz_service: 4", "veteran_halutz_service: 2");
        var c = ContentLoader.Load(Fixture.Write(files));
        var s = Simulation.NewGame(c, 1, Mode.Historical);
        var id = s.NextId++;
        s.Units.Add(id, new Unit(id, "halutz", 1, 0, records: new[] { 7 }));
        s = Simulation.Step(c, s, new Command[] { new PioneerWork(id, Projects.BuildYishuv, "site_a") }).State;
        s = Simulation.Step(c, s, Array.Empty<Command>()).State;
        Assert.Equal((Projects.VeteranType, Ids(7)), (s.Units[id].Type, Ids(s.Units[id].Records!)));
        Assert.EndsWith("|records=7", s.Units[id].Canonical());
    }

    // The link, the return and the reassignment are deterministic: the same orders on two dark starts give the same state
    // hash and the same ledger text.
    [Fact]
    public void TheLinkAndItsOrdersAreDeterministic()
    {
        var c = Gdd03();
        string Run(out string ledger)
        {
            var s = Dark(c);
            var why = new List<Cause>();
            var (_, units) = Graduate(c, s, 2, why);
            Assert.Null(Qualification.Return(c, s, units[1], why));
            Assert.Null(Qualification.ReassignPioneers(c, s, TelAviv, "halutz", 1, why));
            ledger = string.Join("\n", s.PopulationLedger.Canonical());
            Assert.Contains("|records=", string.Join(";", s.Units.Values.Select(x => x.Canonical())));
            return s.Hash();
        }
        var a = Run(out var la); var b = Run(out var lb);
        Assert.Equal(a, b);
        Assert.Equal(la, lb);
    }
}
