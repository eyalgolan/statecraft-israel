using System.Globalization;
using Statecraft.Sim;

/// <summary>#1725 PR 7, producer draft 08 (R-UNIT-009 on the ledger): on a game with an initialized Historical ledger, a
/// map/units_1948.yaml brigade dated after the start (today the 7th Brigade) arrives as its unit, as in the aggregate game,
/// and as one active-soldier record per strength unit serving in it. Owner, 8 November 2026: "Soldiers of brigade
/// (Recommended)", "1, like the others", "New people (Recommended)" and "Apart from Arms (Recommended)". Coordinator calls,
/// 8 November 2026 (not owner rulings): the dated brigade is bound at the start, its records are in its anchor hex's
/// region, and a replay adds nobody.</summary>
public class LedgerBrigadeArrivalTests
{
    static Content Gdd03() => HistoricalPopulationTests.Real(true);

    const string Row = "brigade_7";
    static readonly string Target = HistoricalPopulation.FormationPrefix + Row;
    // R-UNIT-009: "Today the 7th Brigade arrives: 1,650 on 24 May 1948 at Hulda, 2 units from the 15 June 1948 turn."
    static readonly DateTime Next = new DateTime(1948, 6, 15);

    static GameState Dark(Content c) => Simulation.NewGame(c, 1, Mode.Historical, darkStart: true);

    static List<PopulationUnit> Serving(GameState s) => s.PopulationLedger.Units
        .Where(p => p.Assignment.Target == Target).ToList();

    static int Index(Content c) => c.StartingForces.FindIndex(f => f.Row == Row);

    // R-UNIT-009, R-UNIT-006, R-POP-004: the 7th Brigade's sourced 1,650 men are its strength in population units, the
    // nearest unit with a half up (2 at rules.yaml's people_per_unit 1,000), as for a starting brigade, and the ledger
    // counts each record at the same 1,000.
    [Fact]
    public void TheSeventhBrigadeIsTwoUnitsOfOneThousandFromItsSourcedStrength()
    {
        var c = Gdd03();
        var f = c.StartingForces[Index(c)];
        var ppu = c.RulesAt(c.Rules.Start).Mvp!.PeoplePerUnit;
        Assert.Equal(1000, ppu);
        Assert.Equal(new DateTime(1948, 5, 24), f.Arrives);
        Assert.Equal(Movement.PlayerSide, f.Side);
        Assert.Equal(Cities.StartUnits(1650m, ppu), f.Strength);
        Assert.Equal(2, f.Strength);
        Assert.Equal(ppu, Dark(c).PopulationLedger.PeoplePerUnit);
    }

    // Owner, 8 November 2026: "Soldiers of brigade (Recommended)", "1, like the others", "New people (Recommended)",
    // "Apart from Arms (Recommended)". Coordinator call (Q6 (a)): the records are in the region of the arrival anchor's hex.
    [Fact]
    public void TheSeventhBrigadeArrivesAsNewSoldierRecordsOfItsBrigade()
    {
        var c = Gdd03();
        var s = Dark(c);
        var i = Index(c);
        var f = c.StartingForces[i];
        var twin = Simulation.NewGame(c, 1, Mode.Historical);
        var next = s.PopulationLedger.NextPopulationId;
        var count = s.PopulationLedger.Units.Count;
        var total = s.TotalPopulation;
        var arms = s.Arms;
        var civilians = s.PopulationLedger.Units.Where(p => p.Status != PopulationStatus.ActiveSoldier).Select(p => p.Canonical()).ToList();
        Assert.Empty(Serving(s));
        Assert.False(s.Units.ContainsKey(i));

        StartingForces.Arrive(c, s, Next);
        StartingForces.Arrive(c, twin, Next);

        // The unit is the aggregate game's: id i, its type, hex and strength, combat XP 0 (R-POP-004: units keep 0).
        Assert.Equal(twin.Units[i].Canonical(), s.Units[i].Canonical());
        Assert.Equal(f.Strength, s.Units[i].Strength);
        Assert.Equal(0, s.Units[i].CombatXp);
        var serving = Serving(s);
        Assert.Equal(f.Strength, serving.Count);
        Assert.Equal(Enumerable.Range(next, f.Strength), serving.Select(p => p.Id));   // new people, new ids
        Assert.Equal(count + f.Strength, s.PopulationLedger.Units.Count);
        Assert.Equal(civilians, s.PopulationLedger.Units.Where(p => p.Status != PopulationStatus.ActiveSoldier).Select(p => p.Canonical()));
        var cls = c.UnitTypes.Single(t => t.Id == f.Type).Class;
        var region = c.PlayerRegions.AtHex(f.Q, f.R)!.Id;
        Assert.Equal("central_plain", region);   // Hulda's hex (-4, 25), regions.yaml central_plain member_hexes
        for (int k = 0; k < serving.Count; k++)
        {
            var p = serving[k];
            Assert.Equal(PopulationStatus.ActiveSoldier, p.Status);
            Assert.Equal(PopulationAssignmentKind.ActiveService, p.Assignment.Kind);
            Assert.Equal(cls, p.Qualification);
            Assert.Equal(1, p.CombatXp);   // owner, 8 November 2026: "1, like the others"
            Assert.Equal(region, p.Location!.Region);
            Assert.Null(p.Location.City);
            Assert.Equal(0, p.ReceptionTicks);
            Assert.Equal(CivilianProfession.None, p.Profession);
            Assert.Equal(Target + "/" + k.ToString(CultureInfo.InvariantCulture), p.Equipment.MilitaryId);
            Assert.True(p.Equipment.MilitaryPaid);
            Assert.True(s.PopulationLedger.HasArrived(Target, k));
        }
        Assert.Equal(arms, s.Arms);   // "Apart from Arms (Recommended)"
        Assert.Equal(twin.Arms, s.Arms);
        Assert.Equal(total + f.Strength * s.PopulationLedger.PeoplePerUnit, s.TotalPopulation);
        s.PopulationLedger.Reconcile();
    }

    // R-UNIT-009: only the turn that starts before the day and ends on or after it brings the brigade.
    [Fact]
    public void ATurnThatEndsBeforeItsDayAddsNobody()
    {
        var c = Gdd03();
        var s = Dark(c);
        var hash = s.Hash();
        StartingForces.Arrive(c, s, new DateTime(1948, 5, 23));
        Assert.Equal(hash, s.Hash());
        Assert.Empty(Serving(s));
    }

    // Coordinator call (Q7 (a)), as #2954's arrivals: a replay of the turn on the state it produced adds nobody and leaves
    // the state as it was; the same turn from the same start gives the same state.
    [Fact]
    public void AReplayAddsNobodyTwice()
    {
        var c = Gdd03();
        var s = Dark(c);
        var again = Dark(c);
        StartingForces.Arrive(c, s, Next);
        StartingForces.Arrive(c, again, Next);
        Assert.Equal(s.Hash(), again.Hash());
        var hash = s.Hash();
        StartingForces.Arrive(c, s, Next);
        Assert.Equal(hash, s.Hash());
        Assert.Equal(c.StartingForces[Index(c)].Strength, Serving(s).Count);
        s.PopulationLedger.Reconcile();
    }

    // Coordinator call (Q5 (a)): the dark start binds every own dated brigade's service target in its arrival region, with
    // no record, so the arrival is a plain registration; the start inventory still has no record of it (R-POP-004).
    [Fact]
    public void TheDatedBrigadeIsBoundAtTheStartWithNoRecord()
    {
        var c = Gdd03();
        var s = Dark(c);
        Assert.Empty(Serving(s));
        Assert.Contains(s.PopulationLedger.Canonical(), line => line.StartsWith(PopulationText.Fields("target") + "|", StringComparison.Ordinal)
            && line.Contains(Target, StringComparison.Ordinal) && line.Contains("central_plain", StringComparison.Ordinal));
        Assert.DoesNotContain(HistoricalPopulation.Inventory(c), r => r.Source == Target);
    }

    // R-ENEMY-004: an enemy force that bypassed the loader is still refused before anything is added, ledger included.
    [Fact]
    public void AnEnemyArrivalIsStillRefusedWithTheLedgerUnchanged()
    {
        var c = Gdd03();
        var s = Dark(c);
        c.StartingForces.Add(new StartingForce
        {
            Row = "late_enemy", Type = c.StartingForces.First(f => f.Side != Movement.PlayerSide).Type, Side = "egypt",
            Strength = 2, Q = 0, R = 0, Arrives = new DateTime(1948, 6, 1),
        });
        var hash = s.Hash();
        Assert.Throws<InvalidOperationException>(() => StartingForces.Arrive(c, s, Next));
        Assert.Equal(hash, s.Hash());
        Assert.Empty(Serving(s));
    }

    // R-POP-002: a synthetic or Open ledger is still refused, with the state unchanged.
    [Theory]
    [InlineData(null)]
    [InlineData(Mode.Open)]
    public void ASyntheticOrOpenLedgerIsStillRefused(Mode? mode)
    {
        var c = PopulationLedgerTests.Content();
        var s = PopulationLedger.CreateFixture(c, PopulationLedgerTests.Bindings(), new[] {
            new PopulationInventoryRow("fixture", 0, PopulationLedgerTests.Person()) }, mode: mode);
        var hash = s.Hash();
        Assert.Throws<NotSupportedException>(() => StartingForces.Arrive(c, s, s.Date.AddMonths(1)));
        Assert.Equal(hash, s.Hash());
    }
}
