using System.Globalization;
using Statecraft.Sim;

// #1699 first slice: a recruit's basic training starts on the population ledger and holds its issued Arms. Synthetic camps
// only; no real camp (#1719), region, Food or game caller is exercised. Rules quoted below, at main aa29a9b11:
// - docs/GDD.md:375 (ARMS-1/ARMS-2): "Arms equip new soldiers in training and replacements; attacks and artillery spend
//   none. A recruit course cannot start without its Arms, and all training stays in 0.3 (ARMS-1 to ARMS-3)."
// - docs/GDD.md:374 (27M-7): "Training is ordered in the camp's city, with no travel (27M-7)."
// - R-TRAIN-001 (docs/rules/mvp.md): "for the type's `training_turns`, counting the turn it was ordered in. The Treasury
//   pays `n * training_cost` at once. When the Treasury holds less, the shortfall is borrowed, as for map actions: it is
//   added to the debt and the Treasury stops at 0"
// - R-ARMS-003 (docs/rules/mvp.md): "Recruit training (R-TRAIN-001) takes `arms_per_recruit_group` (1) Arms for each
//   group when it starts, after its other checks and before it pays. A stock short of that refuses the order with a
//   reason naming the need and the stock"
// - R-POP-002 (docs/rules/mvp.md): "distinct issued civilian/military equipment and paid facts. ... duplicate
//   issued-item ownership are refused before mutation. ... Ledger changes never touch resources, causes, RNG or Attention."
// Cancel (#1699 second slice), quoted at ca599e4b6:
// - docs/GDD.md:372: "A cancelled or slot-losing course is cancelled like `CancelTraining`, people return as civilians, Money
//   and tools are not refunded and Arms return to the pool (Q44, Q52, 27M-3, 27M-4)."
// - R-TRAIN-002 (docs/rules/mvp.md): "`CancelTraining(cohort)` returns the cohort's people to its city as civilians without
//   a job and refunds none of the Money paid (D-A3); a recruit cohort's Arms go back to the pool (R-ARMS-003). A cohort that
//   does not exist, or has finished, is refused with the state unchanged."
// - R-ARMS-003 (docs/rules/mvp.md): "The cohort keeps the Arms it took, and the state hash writes them with it; cancelling
//   the cohort (R-TRAIN-002) returns them to the pool, up to the most the stock can hold, although the Money stays paid"
// Completion and upkeep (#1699 third slice), quoted at 3b18babc1:
// - R-UNIT-003 (docs/rules/mvp.md): "A finished recruit cohort becomes exactly one Israeli infantry formation at its city's
//   settlement hex, with `Strength` equal to the cohort's `Count` in population units ... Its reserved Arms are neither
//   charged again nor refunded on completion (R-ARMS-003)."
// - docs/GDD.md:372 (Q43(b)): "they are released in the turn its last course turn counts".
// - docs/GDD.md:374 (27M-8): "A formation pays military upkeep from the turn it graduates (27M-8)."
// - R-UNIT-010 (docs/rules/mvp.md): "each Israeli military formation pays `military_upkeep_per_turn` Money, including a
//   formation that graduated this turn. The owner-accepted initial rate is IL 0.05m per formation per turn, independent of
//   strength and turn length"; content/gdd03-balance.yaml `military.active_money` [0.05].
// Maintainer calls by coordinator 53 (3 November 2026), not owner rulings: formations are pre-bound, empty ActiveService
// targets supplied by the tests; one Start order is one cohort and one formation, its strength its count; only formations
// this fixture graduated pay; a graduating Advance after that month's PayUpkeep is refused. A graduate's military
// qualification (docs/GDD.md:372: a returned captive "keeps their id and military qualification") is the course's unit
// class, content/unit_types.yaml `recruit` `class` (infantry); value: the course's unit class (unit_types.yaml); maintainer
// call on the source of the value, not an owner ruling.
public class Gdd03BasicTrainingFixtureTests
{
    static readonly Lazy<Content> RealContent = new(() => ContentLoader.Load(RealContentTests.ContentRoot()));
    static readonly Lazy<Gdd03BalanceCatalogue> Real = new(() => RealContent.Value.Gdd03Balance!);
    const string CampNorth = "fixture-camp-north", CampSouth = "fixture-camp-south", Field = "fixture-field", Service = "fixture-service";
    const string Infantry = "infantry", FixtureClass = "fixture-class", Carried = "fixture-carried-item";
    const string FormationNorthA = "fixture-formation-north-a", FormationNorthB = "fixture-formation-north-b",
        FormationSouth = "fixture-formation-south";

    static Content Catalogue(string? key = null, Gdd03DesignValue? replacement = null, string ruleset = SaveGame.Gdd03Ruleset)
    {
        var c = PopulationLedgerTests.Content(ruleset);
        var values = new Dictionary<string, Gdd03DesignValue>(Real.Value.Values, StringComparer.Ordinal);
        if (key != null) { if (replacement == null) values.Remove(key); else values[key] = replacement; }
        c.Gdd03Balance = new Gdd03BalanceCatalogue { Ruleset = Real.Value.Ruleset, Values = values };
        c.UnitTypes = RealContent.Value.UnitTypes;
        c.Rules = RealContent.Value.Rules; // rules.yaml's people_per_unit: the scale military.camp_capacity is counted at (#2739)
        return c;
    }
    // The real unit types with `recruit` replaced by one of the given class, or without `recruit` when the class is null.
    static Content WithRecruitClass(Content c, string? recruitClass)
    {
        c.UnitTypes = RealContent.Value.UnitTypes.Where(t => t.Id != "recruit").ToList();
        if (recruitClass != null) c.UnitTypes.Add(new UnitType { Id = "recruit", Kind = "unit", Side = "israel", Class = recruitClass });
        return c;
    }
    static Gdd03DesignValue Agreed(decimal v) => new() { Basis = "Agreed", Values = new() { v }, Note = "synthetic test value" };
    static Gdd03DesignValue Deferred() => new() { Basis = "Deferred", Expression = "X", Note = "synthetic deferred value" };

    static PopulationBindings Bindings() => new(
        new[] { new PopulationLocation("north"), new PopulationLocation("south") },
        new[] { new PopulationTarget(CampNorth, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(CampSouth, PopulationAssignmentKind.Course, "south", 3),
                new PopulationTarget(Field, PopulationAssignmentKind.Work, "north"),
                new PopulationTarget(Service, PopulationAssignmentKind.ActiveService, "north"),
                new PopulationTarget(FormationNorthA, PopulationAssignmentKind.ActiveService, "north"),
                new PopulationTarget(FormationNorthB, PopulationAssignmentKind.ActiveService, "north"),
                new PopulationTarget(FormationSouth, PopulationAssignmentKind.ActiveService, "south") },
        Array.Empty<string>(), new[] { Infantry, FixtureClass });

    static PopulationUnit Person(PopulationStatus status = PopulationStatus.FreeCivilian, string region = "north",
        PopulationAssignment? assignment = null, string qualification = "", string? military = null, string? tools = null)
        => new(0, status, 0, new PopulationLocation(region), assignment ?? PopulationAssignment.Unassigned,
            CivilianProfession.None, CivilianTier.None, 0, qualification, 0, new PopulationEquipment(tools, tools != null, military, military != null));

    // Ids follow the row order: 0, 1, 2 unequipped civilians in the north (2 holds tools); 3 a civilian carrying an Arms item;
    // 4 a qualified (demobilised) soldier; 5 a civilian in the south; 6 a refugee; 7 a worker; 8 an active soldier.
    static GameState State(Content c, params PopulationUnit[] extra)
    {
        var people = new List<PopulationUnit> {
            Person(), Person(), Person(tools: "fixture-tools"), Person(military: Carried),
            Person(qualification: Infantry), Person(region: "south"), Person(PopulationStatus.Refugee),
            Person(assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)),
            Person(PopulationStatus.ActiveSoldier, assignment: new PopulationAssignment(PopulationAssignmentKind.ActiveService, Service),
                qualification: Infantry, military: "fixture-soldier-item") };
        people.AddRange(extra);
        return PopulationLedger.CreateFixture(c, Bindings(), people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)));
    }
    static Gdd03FixtureMilitaryCamp[] Camps(bool northOperating = true) => new[] {
        new Gdd03FixtureMilitaryCamp(CampNorth, "north", northOperating), new Gdd03FixtureMilitaryCamp(CampSouth, "south", true) };
    static Gdd03FixtureFormation[] Formations() => new[] {
        new Gdd03FixtureFormation(FormationNorthA, "north"), new Gdd03FixtureFormation(FormationNorthB, "north"),
        new Gdd03FixtureFormation(FormationSouth, "south") };
    static Gdd03BasicTrainingFixture Fixture(Content c, GameState s, int spare = 10, decimal money = 10m, bool northOperating = true,
        Gdd03FixtureFormation[]? formations = null)
        => new(c, s, Camps(northOperating), formations ?? Formations(), spare, money);
    static int[] Soldiers(GameState s, string formation) => s.PopulationLedger.Units
        .Where(p => p.Status == PopulationStatus.ActiveSoldier && p.Assignment.Target == formation).Select(p => p.Id).ToArray();

    static int Issued(GameState s) => s.PopulationLedger.Units.Count(p => p.Equipment.MilitaryId != null);
    static string Snapshot(GameState s, Gdd03BasicTrainingFixture f)
        => s.Describe() + "|spare=" + f.SpareArms.ToString(CultureInfo.InvariantCulture)
           + "|money=" + f.Money.ToString(CultureInfo.InvariantCulture) + "|debt=" + f.Debt.ToString(CultureInfo.InvariantCulture);
    static Exception Unchanged(GameState s, Gdd03BasicTrainingFixture f, Action act, Type expected, string? deferredPrefix = null)
    {
        var before = Snapshot(s, f);
        var ex = Assert.Throws(expected, act);
        if (deferredPrefix != null) Assert.StartsWith(deferredPrefix, ex.Message, StringComparison.Ordinal);
        else Assert.False(ex.Message.StartsWith("Deferred:", StringComparison.Ordinal), ex.Message);
        Assert.Equal(before, Snapshot(s, f));
        return ex;
    }

    // ARMS-1 (docs/GDD.md:375, "Arms equip new soldiers in training"); R-ARMS-003 (docs/rules/mvp.md, "takes
    // `arms_per_recruit_group` (1) Arms for each group when it starts"); content/gdd03-balance.yaml:315-318
    // `military.issue_arms` [1], "Arms issued once to an unequipped unit". Each unequipped record takes one distinct item
    // from the fixture's spare Arms; the same ids keep every other field; no person is created.
    [Fact] public void ARecruitCourseTakesOneArmsPerUnequippedUnit()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 5); var l = s.PopulationLedger;
        var before = new[] { l.Get(0), l.Get(1), l.Get(2) }; int count = l.Units.Count, next = l.NextPopulationId;

        f.Start(new[] { 2, 0, 1 }, CampNorth, 0);

        Assert.Equal(2, f.SpareArms);
        var items = new HashSet<string>(StringComparer.Ordinal);
        foreach (var b in before)
        {
            var p = l.Get(b.Id);
            Assert.Equal(PopulationStatus.Trainee, p.Status);
            Assert.Equal(PopulationAssignmentKind.Course, p.Assignment.Kind);
            Assert.Equal(CampNorth, p.Assignment.Target); Assert.Equal(0, p.Assignment.ProgressTicks); Assert.Null(p.Assignment.TeacherId);
            Assert.NotNull(p.Equipment.MilitaryId); Assert.True(items.Add(p.Equipment.MilitaryId!));
            Assert.True(p.Equipment.MilitaryPaid);
            Assert.Equal(b.Equipment.ToolsId, p.Equipment.ToolsId); Assert.Equal(b.Equipment.ToolsPaid, p.Equipment.ToolsPaid);
            Assert.Same(b.Location, p.Location); Assert.Equal(b.Qualification, p.Qualification); Assert.Equal(b.CombatXp, p.CombatXp);
            Assert.Equal(b.ReceptionTicks, p.ReceptionTicks); Assert.Equal(b.Profession, p.Profession); Assert.Equal(b.WorkXp, p.WorkXp);
        }
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        l.Reconcile();
    }

    // ARMS-2 (docs/GDD.md:375): "A recruit course cannot start without its Arms"; R-ARMS-003 (docs/rules/mvp.md): "A
    // stock short of that refuses the order with a reason naming the need and the stock". The WHOLE order is refused, not
    // the part the stock covers; issuing the last spare then succeeds (#1699 comment of 28 October 2026: "Issue of the last
    // spare succeeds; a further unavailable issue refuses atomically").
    [Fact] public void ShortArmsRefusesTheWholeOrderWithAReason()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 2, money: 10m);
        var ex = Unchanged(s, f, () => f.Start(new[] { 0, 1, 2 }, CampNorth, 0), typeof(ArgumentException));
        Assert.Contains("need 3, have 2", ex.Message, StringComparison.Ordinal);
        Assert.All(new[] { 0, 1, 2 }, id => Assert.Equal(PopulationStatus.FreeCivilian, s.PopulationLedger.Get(id).Status));

        f.Start(new[] { 0, 1 }, CampNorth, 0);
        Assert.Equal(0, f.SpareArms);
        var after = Unchanged(s, f, () => f.Start(new[] { 2 }, CampNorth, 0), typeof(ArgumentException));
        Assert.Contains("need 1, have 0", after.Message, StringComparison.Ordinal);
    }

    // #1699 What to do: "Arms: issue once to an unequipped unit; never a second issue"; content/gdd03-balance.yaml:339-342
    // `military.repeat_issue` [0]. A record that already carries an item keeps it and takes none, even from an empty stock.
    [Fact] public void AnEquippedRecordIsNeverIssuedASecondItem()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 0);
        f.Start(new[] { 3 }, CampNorth, 0);
        var p = s.PopulationLedger.Get(3);
        Assert.Equal(PopulationStatus.Trainee, p.Status);
        Assert.Equal(Carried, p.Equipment.MilitaryId); Assert.True(p.Equipment.MilitaryPaid);
        Assert.Equal(0, f.SpareArms);

        // A mixed order needs Arms only for its unequipped records.
        var s2 = State(c); var f2 = Fixture(c, s2, spare: 1);
        f2.Start(new[] { 0, 3 }, CampNorth, 0);
        Assert.Equal(0, f2.SpareArms);
        Assert.Equal(Carried, s2.PopulationLedger.Get(3).Equipment.MilitaryId);
    }

    // #1699 Done when: "a basic-training order for an already qualified soldier is refused (it cannot be used to avoid
    // active upkeep)". A qualified civilian, or an active soldier, refuses the whole order.
    [Fact] public void AQualifiedSoldierIsRefusedBasicTraining()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s);
        Unchanged(s, f, () => f.Start(new[] { 4 }, CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 8 }, CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 0, 4 }, CampNorth, 0), typeof(ArgumentException));
        Assert.Equal(PopulationStatus.FreeCivilian, s.PopulationLedger.Get(0).Status);
    }

    // 27M-7 (docs/GDD.md:374): "Training is ordered in the camp's city, with no travel (27M-7)." A civilian in another region
    // is refused, never moved; a camp that is not operating refuses the order.
    [Fact] public void TrainingIsOrderedInTheCampsRegion()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, northOperating: false);
        Unchanged(s, f, () => f.Start(new[] { 5 }, CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 0 }, CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 0 }, "fixture-unknown-camp", 0), typeof(ArgumentException));
        f.Start(new[] { 5 }, CampSouth, 0);
        var p = s.PopulationLedger.Get(5);
        Assert.Equal(CampSouth, p.Assignment.Target); Assert.Equal("south", p.Location!.Region);
    }

    // 27M-7 (docs/GDD.md:374): "Training is ordered in the camp's city, with no travel (27M-7)." With both camps operating,
    // stock and room to spare, a recruit from another region is refused by the region rule alone, with the fixture's own
    // reason before any write (the ledger's later target-region check, PopulationLedger.cs:242, is only a backstop), and
    // nothing changes.
    [Fact] public void AWrongRegionIsRefusedAtAnOperatingCampWithStock()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 10, money: 10m);
        var ex = Unchanged(s, f, () => f.Start(new[] { 5 }, CampNorth, 0), typeof(ArgumentException));
        Assert.Equal("Unit 5 is not in the camp's region.", ex.Message);
        ex = Unchanged(s, f, () => f.Start(new[] { 0, 5 }, CampSouth, 0), typeof(ArgumentException));
        Assert.Equal("Unit 0 is not in the camp's region.", ex.Message);
        Assert.Equal("south", s.PopulationLedger.Get(5).Location!.Region);
    }

    // R-TRAIN-001 (docs/rules/mvp.md): takes "the city's civilians without a job"; docs/GDD.md:374: "Refugees cannot
    // train until they become civilians". A refugee or a worker refuses the whole order, as do a repeated id and an empty order.
    [Fact] public void RefugeesWorkersAndMalformedOrdersAreRefused()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s);
        Unchanged(s, f, () => f.Start(new[] { 6 }, CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 0, 7 }, CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 0, 0 }, CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(Array.Empty<int>(), CampNorth, 0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 99 }, CampNorth, 0), typeof(ArgumentException));
    }

    // content/gdd03-balance.yaml:307-310 `military.camp_capacity` [50], "simultaneous trainee population units per camp".
    // Read from the catalogue, not a literal: with a synthetic 20 (2 records at the synthetic 10,000 a record, #2739), a third
    // trainee in the camp refuses its whole order.
    [Fact] public void CampCapacityComesFromTheCatalogue()
    {
        var c = Catalogue("military.camp_capacity", Agreed(20)); var s = State(c); var f = Fixture(c, s);
        f.Start(new[] { 0 }, CampNorth, 0);
        Unchanged(s, f, () => f.Start(new[] { 1, 2 }, CampNorth, 0), typeof(ArgumentException));
        f.Start(new[] { 1 }, CampNorth, 0);
        Unchanged(s, f, () => f.Start(new[] { 2 }, CampNorth, 0), typeof(ArgumentException));
        // The cap is per camp: the south camp is still free.
        f.Start(new[] { 5 }, CampSouth, 0);

        var real = Catalogue(); var s2 = State(real); var f2 = Fixture(real, s2);
        f2.Start(new[] { 0, 1, 2, 3 }, CampNorth, 0); // 4 of the real 50, which is 5 records at 10,000 a record
    }

    // content/gdd03-balance.yaml:347-350 `military.training_money` [0.5], "Money per basic course. Owner ruling, 30 October
    // 2026 ... "Keep the set values (Recommended)""; docs/gdd/training.md:15 "Basic military training costs 0.5 per course
    // in Money". R-TRAIN-001 (docs/rules/mvp.md): "The Treasury pays `n * training_cost` at once. When the Treasury holds
    // less, the shortfall is borrowed"; R-MONEY-003 (docs/rules/mvp.md): "the Treasury stops at 0". One record is one
    // course; an equipped record pays the course too. The fixture owns its Money and Debt, as Gdd03ProjectBuildFixture does,
    // because R-POP-002 says "Ledger changes never touch resources".
    [Fact] public void EachRecruitPaysTheCatalogueMoneyAndTheShortfallIsBorrowed()
    {
        Assert.Equal(0.5m, Gdd03MilitaryCatalogue.Read(Real.Value).TrainingMoney);
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0, 1, 3 }, CampNorth, 0);
        Assert.Equal(8.5m, f.Money); Assert.Equal(0m, f.Debt);

        var s2 = State(c); var f2 = Fixture(c, s2, money: 1m);
        f2.Start(new[] { 0, 1, 2 }, CampNorth, 0);
        Assert.Equal(0m, f2.Money); Assert.Equal(0.5m, f2.Debt);

        var two = Catalogue("military.training_money", Agreed(2m)); var s3 = State(two); var f3 = Fixture(two, s3, money: 10m);
        f3.Start(new[] { 0, 1 }, CampNorth, 0);
        Assert.Equal(6m, f3.Money);
    }

    // R-TRAIN-001 (docs/rules/mvp.md): "counting the turn it was ordered in"; content/gdd03-balance.yaml:311-314
    // `military.basic_turns` [3]. The enrolment month is course turn 1 and each month ordinal counts once. The month that
    // counts the last course turn graduates the record (Q43(b), R-TRAIN-003, R-UNIT-003): the same id, in place, becomes an
    // active soldier of a formation in the camp's region, keeping its issued item; nothing is charged or refunded, and no
    // person is created.
    [Fact] public void TheEnrolmentMonthCountsAndTheLastTurnGraduates()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s); var l = s.PopulationLedger;
        f.Start(new[] { 0 }, CampNorth, 4);
        var item = l.Get(0).Equipment.MilitaryId;
        f.Advance(0, 4);
        Assert.Equal(1, l.Get(0).Assignment.ProgressTicks);
        Unchanged(s, f, () => f.Advance(0, 4), typeof(ArgumentException));
        Unchanged(s, f, () => f.Advance(0, 3), typeof(ArgumentException));
        f.Advance(0, 5);
        Assert.Equal(2, l.Get(0).Assignment.ProgressTicks);
        Assert.Equal(PopulationStatus.Trainee, l.Get(0).Status);
        var before = l.Get(0); int count = l.Units.Count, next = l.NextPopulationId;
        int spare = f.SpareArms; decimal money = f.Money, debt = f.Debt;

        f.Advance(0, 6);

        var p = l.Get(0);
        Assert.Equal(PopulationStatus.ActiveSoldier, p.Status);
        Assert.Equal(PopulationAssignmentKind.ActiveService, p.Assignment.Kind);
        Assert.Equal(FormationNorthA, p.Assignment.Target);
        Assert.Equal(0, p.Assignment.ProgressTicks); Assert.Null(p.Assignment.TeacherId);
        Assert.Equal(item, p.Equipment.MilitaryId); Assert.True(p.Equipment.MilitaryPaid);
        Assert.Equal(Infantry, p.Qualification);
        Assert.Same(before.Location, p.Location);
        Assert.Equal(before.Equipment.ToolsId, p.Equipment.ToolsId); Assert.Equal(before.Profession, p.Profession);
        Assert.Equal(before.Tier, p.Tier); Assert.Equal(before.WorkXp, p.WorkXp); Assert.Equal(before.CombatXp, p.CombatXp);
        Assert.Equal(before.ReceptionTicks, p.ReceptionTicks);
        Assert.Equal(spare, f.SpareArms); Assert.Equal(money, f.Money); Assert.Equal(debt, f.Debt);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        Unchanged(s, f, () => f.Advance(0, 7), typeof(ArgumentException)); // a graduate is not a trainee
        Unchanged(s, f, () => f.Advance(1, 6), typeof(ArgumentException)); // never started
        l.Reconcile();
    }

    // content/gdd03-balance.yaml:311-314 `military.basic_turns` [3], read from the catalogue, not a literal: with a synthetic
    // 2 or 4, the enrolment month and every later month count, the record staying a trainee, until the month that reaches
    // that value, which graduates it.
    [Theory, InlineData(2), InlineData(4)]
    public void TheGraduationTurnComesFromTheCatalogue(int turns)
    {
        var c = Catalogue("military.basic_turns", Agreed(turns)); var s = State(c); var f = Fixture(c, s); var l = s.PopulationLedger;
        f.Start(new[] { 0 }, CampNorth, 0);
        for (int month = 0; month < turns - 1; month++)
        {
            f.Advance(0, month);
            Assert.Equal(PopulationStatus.Trainee, l.Get(0).Status);
        }
        Assert.Equal(turns - 1, l.Get(0).Assignment.ProgressTicks);
        f.Advance(0, turns - 1);
        Assert.Equal(PopulationStatus.ActiveSoldier, l.Get(0).Status);
        Assert.Equal(FormationNorthA, l.Get(0).Assignment.Target);
        l.Reconcile();
    }

    // R-MONEY-003 (docs/rules/mvp.md): "A shortfall changes nothing but the Treasury and the debt, and a refused order
    // changes nothing". The order's Money is computed before anything is written, so a cost that
    // cannot be represented throws with the ledger, spare Arms, Money and Debt all unchanged, not a half-applied order.
    [Fact] public void ACostThatOverflowsChangesNothing()
    {
        var c = Catalogue("military.training_money", Agreed(decimal.MaxValue)); var s = State(c); var f = Fixture(c, s, spare: 5);
        Unchanged(s, f, () => f.Start(new[] { 0, 1 }, CampNorth, 0), typeof(OverflowException));
        Assert.Equal(PopulationStatus.FreeCivilian, s.PopulationLedger.Get(0).Status);
        Assert.Equal(5, f.SpareArms);
    }

    // #1699 Done when: "Draft, train, cancel and demobilise in any order never change total units or total Arms (spare +
    // issued)" and "The conservation test runs on a fixture with no arrivals, combat losses, imports or workshop output",
    // restricted to this slice's start and advance. Accepted and refused steps alike keep spare + issued and the record
    // count constant, in en-US and he-IL.
    [Theory, InlineData("en-US"), InlineData("he-IL")]
    public void SpareAndIssuedArmsAreConservedAfterEveryStep(string culture)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            var c = Catalogue("military.camp_capacity", Agreed(30)); var s = State(c); var f = Fixture(c, s, spare: 3, money: 0.75m);
            int total = f.SpareArms + Issued(s), units = s.PopulationLedger.Units.Count;
            var steps = new Action[] {
                () => f.Start(new[] { 0, 1, 2, 3 }, CampNorth, 0), // over capacity: refused
                () => f.Start(new[] { 0, 4 }, CampNorth, 0),       // qualified: refused
                () => f.Start(new[] { 0, 3 }, CampNorth, 0),       // one issue, one carried item
                () => f.Start(new[] { 1, 2 }, CampNorth, 0),       // capacity 30, 3 records: refused
                () => f.Start(new[] { 5 }, CampSouth, 0),          // one issue
                () => f.Start(new[] { 1 }, CampNorth, 0),          // one issue, the last spare
                () => f.Start(new[] { 2 }, CampSouth, 1),          // wrong region: refused
                () => f.Advance(0, 0), () => f.Advance(0, 0),      // second count refused
                () => f.Advance(0, 1), () => f.Advance(0, 2),      // 0 graduates, keeping its item
                () => f.Advance(0, 3),                             // a graduate: refused
                () => f.Advance(3, 1),
            };
            int accepted = 0;
            foreach (var step in steps)
            {
                try { step(); accepted++; } catch (ArgumentException) { } catch (NotSupportedException) { }
                Assert.Equal(total, f.SpareArms + Issued(s));
                Assert.Equal(units, s.PopulationLedger.Units.Count);
                s.PopulationLedger.Reconcile();
            }
            Assert.Equal(7, accepted);
            Assert.Equal(0, f.SpareArms);
            Assert.Equal(1.25m, f.Debt); // 4 courses x 0.5 = 2, of which 0.75 was in hand
            Assert.Equal(PopulationStatus.ActiveSoldier, s.PopulationLedger.Get(0).Status);
            Assert.Equal(new[] { 0 }, Soldiers(s, FormationNorthA));
            Assert.Equal(Gdd03BasicTrainingFixture.IssuedPrefix + "0", s.PopulationLedger.Get(0).Equipment.MilitaryId);
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }

    // Item ids: PR #2383 (open) gives starting formation records `units_1948/<row>/<ordinal>` (HistoricalPopulation.FormationPrefix
    // on main, sim/Statecraft.Sim/HistoricalPopulation.cs:32). This fixture's ids use a disjoint prefix, so no issued id can
    // equal one of those or a census source; and R-POP-002's "duplicate issued-item ownership are refused before mutation"
    // still guards a clash with an item already in the ledger: the whole order is refused and nothing is written.
    [Fact] public void IssuedItemIdsCannotCollideWithStartingFormationItems()
    {
        foreach (var other in new[] { HistoricalPopulation.FormationPrefix, HistoricalPopulation.CensusPrefix })
        {
            Assert.False(Gdd03BasicTrainingFixture.IssuedPrefix.StartsWith(other, StringComparison.Ordinal));
            Assert.False(other.StartsWith(Gdd03BasicTrainingFixture.IssuedPrefix, StringComparison.Ordinal));
        }
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s);
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        Assert.Equal(Gdd03BasicTrainingFixture.IssuedPrefix + "0", s.PopulationLedger.Get(0).Equipment.MilitaryId);
        Assert.Equal(Gdd03BasicTrainingFixture.IssuedPrefix + "1", s.PopulationLedger.Get(1).Equipment.MilitaryId);
        Assert.All(s.PopulationLedger.Units.Select(p => p.Equipment.MilitaryId).Where(m => m != null && m != Carried),
            m => Assert.False(m!.StartsWith(HistoricalPopulation.FormationPrefix, StringComparison.Ordinal)));

        // A record (id 9) already holding the id record 2 would get: the order with 2 is refused whole and rolled back.
        var clash = State(c, Person(military: Gdd03BasicTrainingFixture.IssuedPrefix + "2"));
        var g = Fixture(c, clash);
        Unchanged(clash, g, () => g.Start(new[] { 0, 2 }, CampNorth, 0), typeof(ArgumentException));
        Assert.Equal(PopulationStatus.FreeCivilian, clash.PopulationLedger.Get(0).Status);
    }

    // Exact gdd-0.3 only, on an initialized ledger; the catalogue terms fail closed with no fallback; issue_arms other than 1
    // or repeat_issue other than 0 cannot be represented by one item per record and are refused.
    [Fact] public void OtherIdentitiesUninitializedStatesAndBadTermsAreRefused()
    {
        foreach (var identity in new[] { SaveGame.LegacyRuleset, SaveGame.GddRuleset })
        {
            var c = Catalogue(ruleset: identity); var s = new GameState(c); var before = s.Describe();
            Assert.Throws<InvalidOperationException>(() => new Gdd03BasicTrainingFixture(c, s, Camps(), Formations(), 1, 1m));
            Assert.Equal(before, s.Describe());
        }
        var g = Catalogue();
        Assert.Throws<InvalidOperationException>(() => new Gdd03BasicTrainingFixture(g, new GameState(g), Camps(), Formations(), 1, 1m));
        var state = State(g);
        foreach (var key in new[] { "military.camp_capacity", "military.basic_turns", "military.issue_arms", "military.training_money", "military.active_money" })
            foreach (var replacement in new[] { null, Deferred() })
                Assert.Throws<ContentException>(() => new Gdd03BasicTrainingFixture(Catalogue(key, replacement), state, Camps(), Formations(), 1, 1m));
        Assert.Throws<ContentException>(() => new Gdd03BasicTrainingFixture(Catalogue("military.issue_arms", Agreed(2)), state, Camps(), Formations(), 1, 1m));
        Assert.Throws<ContentException>(() => new Gdd03BasicTrainingFixture(Catalogue("military.repeat_issue", Agreed(1)), state, Camps(), Formations(), 1, 1m));
        foreach (var recruitClass in new[] { null, "" })
            Assert.Throws<ContentException>(() => new Gdd03BasicTrainingFixture(WithRecruitClass(Catalogue(), recruitClass), state,
                Camps(), Formations(), 1, 1m));
        Assert.Throws<ArgumentException>(() => new Gdd03BasicTrainingFixture(g, state, Camps(), Formations(), -1, 1m));
        Assert.Throws<ArgumentException>(() => new Gdd03BasicTrainingFixture(g, state, Camps(), Formations(), 1, -1m));
        Assert.Throws<ArgumentException>(() => new Gdd03BasicTrainingFixture(g, state, Camps().Concat(Camps()), Formations(), 1, 1m));
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureMilitaryCamp(" ", "north", true));
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureMilitaryCamp(CampNorth, "", true));
        Assert.Throws<ArgumentException>(() => new Gdd03BasicTrainingFixture(g, state, Camps(), Formations().Concat(Formations()), 1, 1m));
        Assert.Throws<ArgumentException>(() => new Gdd03BasicTrainingFixture(g, state, Camps(),
            new[] { new Gdd03FixtureFormation(CampNorth, "north") }, 1, 1m));
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureFormation(" ", "north"));
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureFormation(FormationNorthA, ""));
    }

    // docs/GDD.md:372: "Arms return to the pool"; R-ARMS-003 (docs/rules/mvp.md): "cancelling the cohort (R-TRAIN-002)
    // returns them to the pool". The cancelled record's issued item goes back to the spare Arms; another trainee keeps theirs.
    [Fact] public void CancelReturnsTheIssuedArmsToTheSpareStock()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 5); var l = s.PopulationLedger;
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        Assert.Equal(3, f.SpareArms);

        f.Cancel(0);

        Assert.Equal(4, f.SpareArms);
        Assert.Null(l.Get(0).Equipment.MilitaryId); Assert.False(l.Get(0).Equipment.MilitaryPaid);
        Assert.Equal(Gdd03BasicTrainingFixture.IssuedPrefix + "1", l.Get(1).Equipment.MilitaryId);
        Assert.Equal(PopulationStatus.Trainee, l.Get(1).Status);
        l.Reconcile();
    }

    // R-TRAIN-002 (docs/rules/mvp.md): "returns the cohort's people to its city as civilians without a job"; docs/GDD.md:374
    // (27M-9): "After a cancellation or camp loss, people go to their city as civilians without a job"; docs/GDD.md:372: "Money
    // and tools are not refunded" and "can be assigned from the next planning, or at once on cancellation" (Q43(b)). The same
    // record stays in place with its tools, its course progress goes with the course, no person is created, and it can be
    // given a job at once.
    [Fact] public void CancelReleasesACivilianWithoutAJobInPlace()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s); var l = s.PopulationLedger;
        f.Start(new[] { 0, 2 }, CampNorth, 0);
        f.Advance(2, 0);
        Assert.Equal(1, l.Get(2).Assignment.ProgressTicks);
        var before = l.Get(2); int count = l.Units.Count, next = l.NextPopulationId;

        f.Cancel(2);

        var p = l.Get(2);
        Assert.Equal(PopulationStatus.FreeCivilian, p.Status);
        Assert.Equal(PopulationAssignmentKind.Unassigned, p.Assignment.Kind);
        Assert.Null(p.Assignment.Target); Assert.Equal(0, p.Assignment.ProgressTicks);
        Assert.Same(before.Location, p.Location); Assert.Equal("", p.Qualification);
        Assert.Equal("fixture-tools", p.Equipment.ToolsId); Assert.True(p.Equipment.ToolsPaid);
        Assert.Equal(before.Profession, p.Profession); Assert.Equal(before.Tier, p.Tier); Assert.Equal(before.WorkXp, p.WorkXp);
        Assert.Equal(before.CombatXp, p.CombatXp); Assert.Equal(before.ReceptionTicks, p.ReceptionTicks);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        l.Reconcile();

        l.ChangeAssignment(2, new PopulationAssignment(PopulationAssignmentKind.Work, Field));
        Assert.Equal(Field, l.Get(2).Assignment.Target);
    }

    // R-TRAIN-002 (docs/rules/mvp.md): "refunds none of the Money paid"; docs/GDD.md:372: "Money and tools are not
    // refunded". Neither Money nor a borrowed shortfall is paid back.
    [Fact] public void CancelRefundsNoMoney()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        Assert.Equal(9m, f.Money);
        f.Cancel(0);
        Assert.Equal(9m, f.Money); Assert.Equal(0m, f.Debt);

        var s2 = State(c); var f2 = Fixture(c, s2, money: 0.25m);
        f2.Start(new[] { 0 }, CampNorth, 0);
        Assert.Equal(0m, f2.Money); Assert.Equal(0.25m, f2.Debt);
        f2.Cancel(0);
        Assert.Equal(0m, f2.Money); Assert.Equal(0.25m, f2.Debt);
    }

    // R-ARMS-003 (docs/rules/mvp.md): "The cohort keeps the Arms it took ... cancelling the cohort (R-TRAIN-002) returns
    // them to the pool". A recruit who arrived equipped took no Arms, so the carried item stays on the record and the spare
    // stock is unchanged. A record that arrived carrying an id with this fixture's own prefix is no exception: the refund
    // follows what this fixture issued, not the item's name. The carried item stays by our reading of R-ARMS-003 ('the
    // Arms it took').
    [Fact] public void AnEquippedRecruitKeepsTheirItemOnCancel()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 0);
        f.Start(new[] { 3 }, CampNorth, 0);
        f.Cancel(3);
        var p = s.PopulationLedger.Get(3);
        Assert.Equal(PopulationStatus.FreeCivilian, p.Status);
        Assert.Equal(Carried, p.Equipment.MilitaryId); Assert.True(p.Equipment.MilitaryPaid);
        Assert.Equal(0, f.SpareArms);

        var own = Gdd03BasicTrainingFixture.IssuedPrefix + "9";
        var s2 = State(c, Person(military: own)); var f2 = Fixture(c, s2, spare: 0);
        f2.Start(new[] { 9 }, CampNorth, 0);
        f2.Cancel(9);
        Assert.Equal(own, s2.PopulationLedger.Get(9).Equipment.MilitaryId);
        Assert.Equal(0, f2.SpareArms);
        s2.PopulationLedger.Reconcile();
    }

    // R-TRAIN-002 (docs/rules/mvp.md): "A cohort that does not exist, or has finished, is refused with the state unchanged."
    // A second cancel, an unknown id, a record never started, an active soldier and another fixture's trainee are refused,
    // with nothing changed.
    [Fact] public void CancellingTwiceOrAnUnknownIdChangesNothing()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s);
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        f.Cancel(0);
        Unchanged(s, f, () => f.Cancel(0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Cancel(99), typeof(ArgumentException));
        Unchanged(s, f, () => f.Cancel(5), typeof(ArgumentException));
        Unchanged(s, f, () => f.Cancel(8), typeof(ArgumentException));

        var g = Fixture(c, s); var first = Snapshot(s, f);
        Unchanged(s, g, () => g.Cancel(1), typeof(ArgumentException));
        Assert.Equal(first, Snapshot(s, f));
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(1).Status);
    }

    // #1699 comment of 28 October 2026: "Restarted courses begin at zero and require a fresh full course". After a cancel the
    // old course cannot be advanced; enrolling again through Start issues a new item from a spare, pays the course again and
    // starts from 0.
    [Fact] public void CancelThenAdvanceIsRefusedAndReEnrolmentStartsAFreshCourse()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 5, money: 10m); var l = s.PopulationLedger;
        f.Start(new[] { 0 }, CampNorth, 0);
        f.Advance(0, 0);
        f.Cancel(0);
        Unchanged(s, f, () => f.Advance(0, 1), typeof(ArgumentException));
        Assert.Equal(5, f.SpareArms); Assert.Equal(9.5m, f.Money);

        f.Start(new[] { 0 }, CampNorth, 1);
        Assert.Equal(4, f.SpareArms); Assert.Equal(9m, f.Money);
        Assert.Equal(Gdd03BasicTrainingFixture.IssuedPrefix + "0", l.Get(0).Equipment.MilitaryId);
        Assert.Equal(0, l.Get(0).Assignment.ProgressTicks);
        f.Advance(0, 1);
        Assert.Equal(1, l.Get(0).Assignment.ProgressTicks);
    }

    // #1699 Done when: "Draft, train, cancel and demobilise in any order never change total units or total Arms (spare +
    // issued)" and "The conservation test runs on a fixture with no arrivals, combat losses, imports or workshop output",
    // restricted to start, advance and cancel. Accepted and refused steps alike keep spare + issued and the record count
    // constant, in en-US and he-IL.
    [Theory, InlineData("en-US"), InlineData("he-IL")]
    public void SpareAndIssuedArmsAreConservedThroughCancel(string culture)
    {
        var saved = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            var c = Catalogue("military.camp_capacity", Agreed(30)); var s = State(c); var f = Fixture(c, s, spare: 3, money: 0.75m);
            int total = f.SpareArms + Issued(s), units = s.PopulationLedger.Units.Count;
            var steps = new Action[] {
                () => f.Start(new[] { 0, 3 }, CampNorth, 0),       // one issue, one carried item
                () => f.Start(new[] { 5 }, CampSouth, 0),          // one issue
                () => f.Advance(0, 0),
                () => f.Cancel(0),                                 // issued item back to the spare
                () => f.Cancel(0),                                 // second cancel: refused
                () => f.Cancel(3),                                 // carried item stays on the record
                () => f.Cancel(99),                                // unknown id: refused
                () => f.Cancel(1),                                 // never started: refused
                () => f.Start(new[] { 0, 1, 2 }, CampNorth, 1),    // need 3, have 2: refused
                () => f.Start(new[] { 0, 1 }, CampNorth, 1),       // re-enrol 0, two issues
                () => f.Advance(0, 1),
                () => f.Cancel(5),                                 // issued item back to the spare
                () => f.Start(new[] { 2 }, CampNorth, 2),          // one issue, the last spare
                () => f.Start(new[] { 3 }, CampNorth, 2),          // capacity 30, 3 records: refused
            };
            int accepted = 0;
            foreach (var step in steps)
            {
                try { step(); accepted++; } catch (ArgumentException) { } catch (NotSupportedException) { }
                Assert.Equal(total, f.SpareArms + Issued(s));
                Assert.Equal(units, s.PopulationLedger.Units.Count);
                s.PopulationLedger.Reconcile();
            }
            Assert.Equal(9, accepted);
            Assert.Equal(0, f.SpareArms);
            Assert.Equal(Carried, s.PopulationLedger.Get(3).Equipment.MilitaryId);
            Assert.Equal(0m, f.Money);
            Assert.Equal(2.25m, f.Debt); // 6 courses x 0.5 = 3, of which 0.75 was in hand; cancels refund none
        }
        finally { CultureInfo.CurrentCulture = saved; }
    }

    // #2739, option 1 (owner, 7 November 2026, relayed: "all your picks"): the cap is people, not records.
    // content/gdd03-balance.yaml `military.camp_capacity` [50]: "#1668's 5 places at 10,000 people a unit is 50 units at D2's
    // 1,000 people a unit", so 50,000 people. A synthetic ledger (10,000 a record) and a Historical one (rules.yaml's 1,000)
    // take the same number of people into a camp and refuse the next record.
    [Fact] public void TheCampCapCountsTheSamePeopleAtEveryScale()
    {
        var c = Catalogue();
        foreach (var (mode, perRecord) in new[] { ((Mode?)null, 10_000), (Mode.Historical, 1_000) })
        {
            var s = Crowd(c, mode, 60); var f = Fixture(c, s, spare: 60);
            int records = 50_000 / perRecord;
            f.Start(Enumerable.Range(Crowded, records), CampNorth, 0);
            Assert.Equal(50_000.0, s.PopulationLedger.Units.Count(p => p.Status == PopulationStatus.Trainee) * (double)perRecord);
            Unchanged(s, f, () => f.Start(new[] { Crowded + records }, CampNorth, 0), typeof(ArgumentException));
        }
    }

    // #2739's rounding rule (owner, 7 Nov 2026, "Round down, refuse 0"; re-confirmed 11 Nov 2026): when the ledger's scale
    // doesn't divide the cap's people, the camp takes the whole records that fit, rounded down, so it never holds more people
    // than the cap. At 3,000 a record 50,000 people is 16 records (48,000), and the 17th is refused. A scale where not even
    // one record fits refuses the fixture, rather than leaving a camp that can train no one.
    [Fact] public void AnUnevenScaleRoundsTheCampCapDown()
    {
        var c = Catalogue("population.people_per_unit", Agreed(3_000)); var s = Crowd(c, null, 20); var f = Fixture(c, s, spare: 20);
        f.Start(Enumerable.Range(Crowded, 16), CampNorth, 0);
        Unchanged(s, f, () => f.Start(new[] { Crowded + 16 }, CampNorth, 0), typeof(ArgumentException));

        var exact = Catalogue("population.people_per_unit", Agreed(50_000)); var one = Fixture(exact, Crowd(exact, null, 2));
        one.Start(new[] { Crowded }, CampNorth, 0);
        Assert.Throws<ArgumentException>(() => one.Start(new[] { Crowded + 1 }, CampNorth, 0));

        var huge = Catalogue("population.people_per_unit", Agreed(50_001));
        var ex = Assert.Throws<ContentException>(() => Fixture(huge, Crowd(huge, null, 1)));
        Assert.Contains("military.camp_capacity", ex.Message, StringComparison.Ordinal);
    }

    // The State rows plus `extra` unequipped civilians in the north, from id Crowded on, at the given mode's scale.
    const int Crowded = 9;
    static GameState Crowd(Content c, Mode? mode, int extra)
    {
        var people = State(c).PopulationLedger.Units.Concat(Enumerable.Range(0, extra).Select(_ => Person()));
        return PopulationLedger.CreateFixture(c, Bindings(), people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)), mode: mode);
    }

    // content/gdd03-balance.yaml `military.camp_capacity`, here a synthetic 20 (2 records at 10,000 a record): a cancelled trainee no longer holds a place, so
    // a camp that was full takes another recruit.
    [Fact] public void CancelFreesCampCapacity()
    {
        var c = Catalogue("military.camp_capacity", Agreed(20)); var s = State(c); var f = Fixture(c, s);
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        Unchanged(s, f, () => f.Start(new[] { 2 }, CampNorth, 0), typeof(ArgumentException));
        f.Cancel(1);
        f.Start(new[] { 2 }, CampNorth, 0);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(2).Status);
    }

    static PopulationUnit WithMilitary(PopulationUnit p, string item) => new(p.Id, p.Status, p.ReceptionTicks, p.Location,
        p.Assignment, p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp,
        new PopulationEquipment(p.Equipment.ToolsId, p.Equipment.ToolsPaid, item, true), p.Captor, p.Home);

    // R-ARMS-003 (docs/rules/mvp.md): "cancelling the cohort (R-TRAIN-002) returns them to the pool", where "them" is
    // "the Arms it took". If the issued item was swapped on the ledger after Start, the record no longer holds what this
    // fixture issued: cancel throws and changes nothing, rather than returning to the spare an item it no longer finds.
    // Maintainer call (not an owner ruling): no rule covers this state, and refusing neither duplicates nor loses an Arms.
    [Fact] public void CancelIsRefusedWhenTheIssuedItemWasReplaced()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 5); var l = s.PopulationLedger;
        f.Start(new[] { 0 }, CampNorth, 0);
        l.Replace(WithMilitary(l.Get(0), "fixture-swapped-item"));
        Unchanged(s, f, () => f.Cancel(0), typeof(InvalidOperationException));
        Assert.Equal("fixture-swapped-item", l.Get(0).Equipment.MilitaryId);
        Assert.Equal(4, f.SpareArms);
    }

    // R-ARMS-003 (docs/rules/mvp.md): "The cohort keeps the Arms it took". A cancel ends what this fixture issued for that
    // course: if the same record later enrols carrying an item it got another way, it took no Arms, so cancelling that
    // second course keeps the carried item and the spare stock unchanged, by our reading of R-ARMS-003 ('the Arms it
    // took').
    [Fact] public void ACancelledRecruitWhoReEnrolsEquippedKeepsTheItemOnASecondCancel()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 5); var l = s.PopulationLedger;
        f.Start(new[] { 0 }, CampNorth, 0);
        f.Cancel(0);
        Assert.Equal(5, f.SpareArms);
        l.Replace(WithMilitary(l.Get(0), Carried + "-0"));
        f.Start(new[] { 0 }, CampNorth, 1);
        Assert.Equal(5, f.SpareArms);

        f.Cancel(0);

        Assert.Equal(PopulationStatus.FreeCivilian, l.Get(0).Status);
        Assert.Equal(Carried + "-0", l.Get(0).Equipment.MilitaryId);
        Assert.Equal(5, f.SpareArms);
        l.Reconcile();
    }

    // R-TRAIN-002 (docs/rules/mvp.md): "`CancelTraining(cohort)` returns the cohort's people to its city as civilians without
    // a job and refunds none of the Money paid"; docs/GDD.md:374 (27M-9): "After a cancellation or camp loss, people go to
    // their city as civilians without a job"; docs/GDD.md:372: "Money and tools are not refunded and Arms return to the pool".
    // A camp that stops operating cancels every course at it in one step: both cohorts' records stay in place as civilians
    // without a job, fixture-issued items go back to the spare Arms, the carried item stays, Money and debt are unchanged, and
    // another camp's trainee is untouched. The loss is applied by the next Advance of any trainee there, the would-be
    // graduation turn included, so no one graduates. Maintainer reading (not an owner ruling): the loss cancels before the
    // month ordinal is checked, as a lost training centre does (R-TRAIN-007).
    [Fact] public void CampLossCancelsEveryTraineeAtTheCamp()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var camps = Camps();
        var f = new Gdd03BasicTrainingFixture(c, s, camps, Formations(), 10, 1m);
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        f.Start(new[] { 2, 3 }, CampNorth, 0);
        f.Start(new[] { 5 }, CampSouth, 0);
        f.Advance(0, 0); f.Advance(0, 1); f.Advance(2, 0);
        Assert.Equal(2, l.Get(0).Assignment.ProgressTicks);
        Assert.Equal(6, f.SpareArms); Assert.Equal(0m, f.Money); Assert.Equal(1.5m, f.Debt);
        int total = f.SpareArms + Issued(s), count = l.Units.Count, next = l.NextPopulationId;
        var before = new[] { 0, 1, 2, 3 }.Select(l.Get).ToArray();

        camps[0].Operating = false;
        f.Advance(0, 2);

        foreach (var old in before)
        {
            var p = l.Get(old.Id);
            Assert.Equal(PopulationStatus.FreeCivilian, p.Status);
            Assert.Equal(PopulationAssignmentKind.Unassigned, p.Assignment.Kind);
            Assert.Null(p.Assignment.Target); Assert.Equal(0, p.Assignment.ProgressTicks);
            Assert.Same(old.Location, p.Location); Assert.Equal("", p.Qualification);
            Assert.Equal(old.Equipment.ToolsId, p.Equipment.ToolsId);
        }
        Assert.Null(l.Get(0).Equipment.MilitaryId); Assert.Null(l.Get(1).Equipment.MilitaryId); Assert.Null(l.Get(2).Equipment.MilitaryId);
        Assert.Equal(Carried, l.Get(3).Equipment.MilitaryId); Assert.True(l.Get(3).Equipment.MilitaryPaid);
        Assert.Equal(9, f.SpareArms); Assert.Equal(total, f.SpareArms + Issued(s));
        Assert.Equal(0m, f.Money); Assert.Equal(1.5m, f.Debt);
        Assert.Equal(PopulationStatus.Trainee, l.Get(5).Status);
        Assert.Empty(Soldiers(s, FormationNorthA)); Assert.Empty(Soldiers(s, FormationNorthB));
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        l.Reconcile();

        Unchanged(s, f, () => f.Advance(1, 2), typeof(ArgumentException));
        Unchanged(s, f, () => f.Cancel(2), typeof(ArgumentException));
        Unchanged(s, f, () => f.Start(new[] { 0 }, CampNorth, 2), typeof(ArgumentException));
        f.Advance(5, 0);
        Assert.Equal(1, l.Get(5).Assignment.ProgressTicks);
    }

    // R-TRAIN-002 (docs/rules/mvp.md): "A cohort that does not exist, or has finished, is refused with the state unchanged";
    // 27M-9 camp loss. Every affected trainee is checked before the first write: when one of them no longer holds the item
    // this fixture issued (as CancelIsRefusedWhenTheIssuedItemWasReplaced), the camp loss is refused and no trainee, Arms,
    // Money or debt changes. Maintainer reading (not an owner ruling): the refusal covers the whole camp, not only that record.
    [Fact] public void CampLossThatCannotReleaseOneTraineeChangesNothing()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var camps = Camps();
        var f = new Gdd03BasicTrainingFixture(c, s, camps, Formations(), 5, 10m);
        f.Start(new[] { 0, 1, 3 }, CampNorth, 0);
        l.Replace(WithMilitary(l.Get(1), "fixture-swapped-item"));
        camps[0].Operating = false;

        Unchanged(s, f, () => f.Advance(0, 1), typeof(InvalidOperationException));

        foreach (var id in new[] { 0, 1, 3 }) Assert.Equal(PopulationStatus.Trainee, l.Get(id).Status);
        Assert.Equal(Gdd03BasicTrainingFixture.IssuedPrefix + "0", l.Get(0).Equipment.MilitaryId);
        Assert.Equal(3, f.SpareArms);
    }

    static void Graduate(Gdd03BasicTrainingFixture f, int id, int enrolment, int turns = 3)
    {
        for (int month = enrolment; month < enrolment + turns; month++) f.Advance(id, month);
    }

    // R-UNIT-003 (docs/rules/mvp.md): "A finished recruit cohort becomes exactly one Israeli infantry formation ... with
    // `Strength` equal to the cohort's `Count` in population units"; the live path makes one unit per cohort with
    // `strength: k.Count` (sim/Statecraft.Sim/Training.cs:244-246). 53's maintainer call (not an owner ruling): one Start
    // order is one cohort and one formation. Two orders in the same camp and month are two cohorts, so two formations; a
    // carried item stays on its record and the spare stock is unchanged by graduation.
    [Fact] public void AStartOrderGraduatesAsOneFormationWithItsCountAsStrength()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 10); var l = s.PopulationLedger;
        f.Start(new[] { 0, 1, 2 }, CampNorth, 0);
        f.Start(new[] { 3 }, CampNorth, 0);
        Assert.Equal(7, f.SpareArms);
        foreach (var id in new[] { 0, 1, 2, 3 }) Graduate(f, id, 0);

        Assert.Equal(new[] { 0, 1, 2 }, Soldiers(s, FormationNorthA));
        Assert.Equal(new[] { 3 }, Soldiers(s, FormationNorthB));
        Assert.Equal(Carried, l.Get(3).Equipment.MilitaryId);
        Assert.Equal(7, f.SpareArms);
        l.Reconcile();
    }

    // 53's maintainer call (not an owner ruling): the cohort is the Start order, so a member whose last course turn counts
    // in a later month still joins the formation its cohort already graduated into, not a new one.
    [Fact] public void ALateMemberJoinsItsCohortsFormation()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s);
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        Graduate(f, 0, 0);
        f.Advance(1, 0); f.Advance(1, 1);
        Assert.Equal(new[] { 0 }, Soldiers(s, FormationNorthA));
        f.Advance(1, 4);
        Assert.Equal(new[] { 0, 1 }, Soldiers(s, FormationNorthA));
        Assert.Empty(Soldiers(s, FormationNorthB));
        s.PopulationLedger.Reconcile();
    }

    // R-TRAIN-002 (docs/rules/mvp.md): "A cohort that does not exist, or has finished, is refused with the state unchanged."
    // A graduate cannot be cancelled or advanced, and cannot take basic training again (#1699 Done when row 2: "a
    // basic-training order for an already qualified soldier is refused (it cannot be used to avoid active upkeep)"): Start
    // takes only free civilians.
    [Fact] public void AGraduateIsRefusedCancelAdvanceAndBasicTraining()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s);
        f.Start(new[] { 0 }, CampNorth, 0);
        Graduate(f, 0, 0);
        Unchanged(s, f, () => f.Cancel(0), typeof(ArgumentException));
        Unchanged(s, f, () => f.Advance(0, 3), typeof(ArgumentException));
        var ex = Unchanged(s, f, () => f.Start(new[] { 0 }, CampNorth, 3), typeof(ArgumentException));
        Assert.Contains("is not a civilian without a job", ex.Message, StringComparison.Ordinal);
    }

    // #1699 Done when: "a basic-training order for an already qualified soldier is refused (it cannot be used to avoid
    // active upkeep)". Graduation gives the record the course's unit class as its military qualification (infantry for
    // `recruit`, content/unit_types.yaml), so a graduate who is a free civilian without a job again is refused because of
    // that qualification, not its status. No fixture path demobilises a graduate, so the test moves the record directly.
    [Fact] public void AGraduateBackInCivilianLifeIsRefusedBasicTraining()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s); var l = s.PopulationLedger;
        f.Start(new[] { 0 }, CampNorth, 0);
        Graduate(f, 0, 0);
        var graduate = l.Get(0);
        l.Replace(graduate.WithPlacement(PopulationStatus.FreeCivilian, graduate.Location, PopulationAssignment.Unassigned,
            graduate.Captor, graduate.Home));
        Assert.Equal(Infantry, l.Get(0).Qualification);
        var ex = Unchanged(s, f, () => f.Start(new[] { 0 }, CampNorth, 3), typeof(ArgumentException));
        Assert.Contains("is already a qualified soldier", ex.Message, StringComparison.Ordinal);
    }

    // The qualification is read from the content, not a literal: with `recruit` given a synthetic class, a graduate holds
    // that class.
    [Fact] public void TheGraduatesQualificationIsTheCoursesUnitClass()
    {
        var c = WithRecruitClass(Catalogue(), FixtureClass); var s = State(c); var f = Fixture(c, s);
        f.Start(new[] { 0 }, CampNorth, 0);
        Graduate(f, 0, 0);
        Assert.Equal(FixtureClass, s.PopulationLedger.Get(0).Qualification);
    }

    // R-TRAIN-002 and docs/GDD.md:372 (cancel); R-UNIT-003 (strength is the cohort's count). A cancelled trainee is out of its
    // course and never graduates; the rest of its cohort graduates without it. A cohort cancelled whole claims no formation,
    // so the next cohort in that region still finds one.
    [Fact] public void ACancelledTraineeNeverGraduates()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s); var l = s.PopulationLedger;
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        f.Advance(0, 0); f.Advance(0, 1); f.Advance(1, 0); f.Advance(1, 1);
        f.Cancel(1);
        Unchanged(s, f, () => f.Advance(1, 2), typeof(ArgumentException));
        f.Advance(0, 2);
        Assert.Equal(new[] { 0 }, Soldiers(s, FormationNorthA));
        Assert.Equal(PopulationStatus.FreeCivilian, l.Get(1).Status);

        f.Start(new[] { 5 }, CampSouth, 0);
        f.Cancel(5);
        f.Start(new[] { 5 }, CampSouth, 1);
        Graduate(f, 5, 1);
        Assert.Equal(new[] { 5 }, Soldiers(s, FormationSouth));
        l.Reconcile();
    }

    // 53's maintainer call (not an owner ruling): formations are "pre-bound, empty ActiveService targets supplied by the
    // tests". A cohort takes the first one in its camp's region that is neither claimed nor serving anyone (the region
    // limit and this meaning of free are also 53's call; only "the first free pre-bound formation in deterministic order"
    // is the owner's ruling, 4 November 2026, Q25, #1699 comment 6623136049); with none, the graduating month is refused
    // with the state unchanged and the trainee keeps its course.
    [Fact] public void GraduationNeedsAFreeFormationInTheCampsRegion()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger;
        var f = Fixture(c, s, formations: new[] { new Gdd03FixtureFormation(Service, "north"), new Gdd03FixtureFormation(FormationSouth, "south") });
        f.Start(new[] { 0 }, CampNorth, 0);
        f.Advance(0, 0); f.Advance(0, 1);
        Unchanged(s, f, () => f.Advance(0, 2), typeof(ArgumentException)); // Service already holds soldier 8
        Assert.Equal(PopulationStatus.Trainee, l.Get(0).Status);
        Assert.Equal(2, l.Get(0).Assignment.ProgressTicks);

        var s2 = State(c); var f2 = Fixture(c, s2, formations: new[] { new Gdd03FixtureFormation(FormationNorthA, "north") });
        f2.Start(new[] { 0 }, CampNorth, 0);
        f2.Start(new[] { 1 }, CampNorth, 0);
        Graduate(f2, 0, 0);
        f2.Advance(1, 0); f2.Advance(1, 1);
        Unchanged(s2, f2, () => f2.Advance(1, 2), typeof(ArgumentException)); // its region's only formation is taken
        Assert.Equal(PopulationStatus.Trainee, s2.PopulationLedger.Get(1).Status);
    }

    // 27M-8 (docs/GDD.md:374): "A formation pays military upkeep from the turn it graduates"; R-UNIT-010 (docs/rules/mvp.md)
    // "including a formation that graduated this turn"; `military.active_money` [0.05]. Nothing is paid before graduation; the
    // graduation month pays, and so does every later month.
    [Fact] public void AFormationPaysUpkeepFromItsGraduationMonth()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0 }, CampNorth, 4);
        Assert.Equal(9.5m, f.Money);
        f.Advance(0, 4); f.PayUpkeep(4);
        f.Advance(0, 5); f.PayUpkeep(5);
        Assert.Equal(9.5m, f.Money);
        f.Advance(0, 6); f.PayUpkeep(6);
        Assert.Equal(9.45m, f.Money);
        f.PayUpkeep(7);
        Assert.Equal(9.4m, f.Money); Assert.Equal(0m, f.Debt);
    }

    // R-UNIT-010 (docs/rules/mvp.md): "including a formation that graduated this turn"; 27M-8 (docs/GDD.md:374): "from the
    // turn it graduates". A formation's upkeep starts at its graduation month even when the first PayUpkeep comes after the
    // graduation but names an earlier month: that month charges nothing, and the graduation month pays.
    [Fact] public void UpkeepForAMonthBeforeGraduationChargesNothing()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0 }, CampNorth, 4);
        f.Advance(0, 4); f.Advance(0, 5); f.Advance(0, 6);
        Assert.Equal(PopulationStatus.ActiveSoldier, s.PopulationLedger.Get(0).Status);
        f.PayUpkeep(5);
        Assert.Equal(9.5m, f.Money); Assert.Equal(0m, f.Debt);
        f.PayUpkeep(6);
        Assert.Equal(9.45m, f.Money);
    }

    // R-UNIT-010 (docs/rules/mvp.md): "IL 0.05m per formation per turn, independent of strength". A formation of three and
    // a formation of one pay 0.05 each.
    [Fact] public void UpkeepIsPerFormationNotPerSoldier()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0, 1, 2 }, CampNorth, 0);
        f.Start(new[] { 5 }, CampSouth, 0);
        foreach (var id in new[] { 0, 1, 2, 5 }) Graduate(f, id, 0);
        Assert.Equal(8m, f.Money);
        f.PayUpkeep(2);
        Assert.Equal(7.9m, f.Money);
    }

    // #1699 body: "trainees and captured units pay none"; 53's maintainer call (not an owner ruling): only formations this
    // fixture graduated pay. Trainees pay nothing, and the active soldier 8 of a formation this fixture did not make is not
    // charged here.
    [Fact] public void TraineesAndOtherFormationsPayNoUpkeep()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0, 1 }, CampNorth, 0);
        f.Advance(0, 0); f.Advance(1, 0);
        f.PayUpkeep(0);
        Assert.Equal(9m, f.Money); Assert.Equal(0m, f.Debt);
        Assert.Equal(PopulationStatus.ActiveSoldier, s.PopulationLedger.Get(8).Status);
    }

    // R-MONEY-003 (docs/rules/mvp.md): the Treasury stops at 0 and the shortfall is borrowed; R-UNIT-010: "R-MONEY-003
    // borrows a shortfall; no strength is removed or Arms consumed for upkeep".
    [Fact] public void UpkeepBorrowsTheShortfall()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, spare: 4, money: 0.52m);
        f.Start(new[] { 0 }, CampNorth, 0);
        Graduate(f, 0, 0);
        var ledger = s.Describe();
        f.PayUpkeep(2);
        Assert.Equal(0m, f.Money); Assert.Equal(0.03m, f.Debt);
        Assert.Equal(3, f.SpareArms);
        Assert.Equal(ledger, s.Describe());
    }

    // Each month's upkeep is paid once and never for an earlier month, like Advance's month ordinals.
    [Fact] public void AMonthIsPaidOnceAndInOrder()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0 }, CampNorth, 0);
        Graduate(f, 0, 0);
        f.PayUpkeep(3);
        Unchanged(s, f, () => f.PayUpkeep(3), typeof(ArgumentException));
        Unchanged(s, f, () => f.PayUpkeep(2), typeof(ArgumentException));
        Unchanged(s, f, () => f.PayUpkeep(-1), typeof(ArgumentException));
        Assert.Equal(9.45m, f.Money);
    }

    // 53's maintainer call (Q5, not an owner ruling): within a month the order is Advance, then PayUpkeep, so 27M-8 always
    // holds. In that order the graduation month pays. A graduating Advance for a month whose upkeep already ran is a caller
    // error: it is refused with nothing changed, and the trainee graduates in a later month, which then pays.
    [Fact] public void AGraduationAfterThatMonthsUpkeepIsRefused()
    {
        var c = Catalogue(); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0 }, CampNorth, 0);
        f.Advance(0, 0); f.Advance(0, 1); f.Advance(0, 2);
        f.PayUpkeep(2);
        Assert.Equal(9.45m, f.Money);

        var s2 = State(c); var f2 = Fixture(c, s2, money: 10m); var l = s2.PopulationLedger;
        f2.Start(new[] { 0, 1 }, CampNorth, 0);
        f2.Advance(0, 0); f2.Advance(0, 1); f2.Advance(1, 0);
        f2.PayUpkeep(2);
        Unchanged(s2, f2, () => f2.Advance(0, 2), typeof(ArgumentException));
        Assert.Equal(PopulationStatus.Trainee, l.Get(0).Status);
        f2.Advance(1, 2); // not graduating (course turn 2): allowed
        Assert.Equal(2, l.Get(1).Assignment.ProgressTicks);
        f2.Advance(0, 3);
        Assert.Equal(PopulationStatus.ActiveSoldier, l.Get(0).Status);
        f2.PayUpkeep(3);
        Assert.Equal(8.95m, f2.Money);
    }

    // `military.active_money` is read from the catalogue, not a literal (a synthetic 0 and 0.2); a cost that cannot be
    // represented throws before anything is written (R-MONEY-003, "a refused order changes nothing").
    [Theory, InlineData("0"), InlineData("0.2")]
    public void UpkeepReadsTheCatalogue(string rate)
    {
        var value = decimal.Parse(rate, CultureInfo.InvariantCulture);
        var c = Catalogue("military.active_money", Agreed(value)); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0 }, CampNorth, 0);
        Graduate(f, 0, 0);
        f.PayUpkeep(2);
        Assert.Equal(9.5m - value, f.Money);
    }

    [Fact] public void AnUpkeepThatOverflowsChangesNothing()
    {
        var c = Catalogue("military.active_money", Agreed(decimal.MaxValue)); var s = State(c); var f = Fixture(c, s, money: 10m);
        f.Start(new[] { 0 }, CampNorth, 0);
        f.Start(new[] { 5 }, CampSouth, 0);
        Graduate(f, 0, 0); Graduate(f, 5, 0);
        Unchanged(s, f, () => f.PayUpkeep(2), typeof(OverflowException));
        Assert.Equal(9m, f.Money); Assert.Equal(0m, f.Debt);
    }
}
