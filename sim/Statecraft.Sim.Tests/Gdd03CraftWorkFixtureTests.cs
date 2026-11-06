using Statecraft.Sim;

// #2472, R-CRAFT-002 under gdd-0.3, synthetic fixtures only: craftsman work XP from the month's snapshot. The recorded
// course rule (docs/gdd/training.md): "Apprentices become experts after `craft.expert_work_turns` actual turns in Industry
// or an arms workshop. Each turn counts once, regardless of output/work rate." The catalogue's threshold is 36.
// People, slots and month ordinals below are test data.
public class Gdd03CraftWorkFixtureTests
{
    static readonly Lazy<Gdd03BalanceCatalogue> Real = new(() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!);
    const string Industry = "fixture-industry", Workshop = "fixture-workshop", Builder = "fixture-builder", Field = "fixture-field",
        Course = "fixture-course";
    static readonly Gdd03FixtureSlot[] Slots = {
        new(Industry, Gdd03FixtureWork.Industry, 9, "industry"), new(Workshop, Gdd03FixtureWork.Workshop, 9, "workshop"),
        new(Builder, Gdd03FixtureWork.Builder, 9, "builder"), new(Field, Gdd03FixtureWork.Farm, 9, "field") };

    static Content Content()
    {
        var c = PopulationLedgerTests.Content();
        c.Gdd03Balance = Real.Value;
        return c;
    }

    static PopulationBindings Bindings() => new(
        new[] { new PopulationLocation("north"), new PopulationLocation("south") },
        new[] { new PopulationTarget(Industry, PopulationAssignmentKind.Work, "north"), new PopulationTarget(Workshop, PopulationAssignmentKind.Work, "north"),
                new PopulationTarget(Builder, PopulationAssignmentKind.Work, "north"), new PopulationTarget(Field, PopulationAssignmentKind.Work, "north"),
                new PopulationTarget(Course, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget("teacher", PopulationAssignmentKind.Teaching, "north"),
                new PopulationTarget("travel", PopulationAssignmentKind.Travel, "south"),
                new PopulationTarget("service", PopulationAssignmentKind.ActiveService, "north") },
        Array.Empty<string>(), new[] { "basic" });

    static PopulationUnit Craftsman(CivilianTier tier, int xp, PopulationAssignment assignment,
        PopulationStatus status = PopulationStatus.FreeCivilian, CivilianProfession profession = CivilianProfession.Craftsman,
        string region = "north") =>
        new(0, status, 6, new PopulationLocation(region), assignment, profession, tier, xp, "basic", 2, PopulationEquipment.None);
    static PopulationAssignment At(string target) => new(PopulationAssignmentKind.Work, target);

    static GameState State(params PopulationUnit[] people) => PopulationLedger.CreateFixture(Content(), Bindings(),
        people.Select((p, i) => new PopulationInventoryRow("synthetic", i, new PopulationUnit(0, p.Status, p.ReceptionTicks, p.Location,
            p.Assignment, p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp, new PopulationEquipment($"tools-{i}", true)))));

    static Gdd03SyntheticMonthContext Month(GameState s, int ordinal) => Gdd03SyntheticMonthContext.CreateSynthetic(
        SaveGame.Gdd03Ruleset, ordinal, s.PopulationLedger.Units,
        new[] { Industry, Workshop, Builder, Field, Course, "teacher" }.Select(f => new Gdd03SyntheticFacilitySnapshot(f, true, 9)),
        Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());

    // R-CRAFT-002: an apprentice at 35 Industry or workshop months reaches 36 and turns expert. That month's output is
    // quoted at the snapshot's apprentice tier (2x); the next month's snapshot holds an expert (3x). Id, assignment,
    // location, equipment and military history are kept.
    [Fact]
    public void AnApprenticeTurnsExpertAtTheThirtySixthWorkMonth_RCraft002()
    {
        var c = Content();
        var s = State(Craftsman(CivilianTier.Apprentice, 35, At(Industry)), Craftsman(CivilianTier.Apprentice, 35, At(Workshop)));
        var before = s.PopulationLedger.Units;
        var f = new Gdd03CraftWorkFixture(c, s, Slots);
        var month = Month(s, 4);
        f.Apply(f.Receipts(month));
        var rates = Gdd03IndustryRates.Read(c);
        Assert.All(month.OpeningPeople, p => Assert.Equal(CivilianTier.Apprentice, p.Tier));
        Assert.Equal(2m, rates.For(month.OpeningPeople[0].Tier));          // this month's output: the snapshot tier
        foreach (var p in before)
        {
            var after = s.PopulationLedger.Get(p.Id);
            Assert.Equal((CivilianTier.Expert, 36), (after.Tier, after.WorkXp));
            Assert.Equal((p.Status, p.Location, p.Assignment, p.Equipment, p.Qualification, p.CombatXp, p.Profession),
                (after.Status, after.Location, after.Assignment, after.Equipment, after.Qualification, after.CombatXp, after.Profession));
        }
        Assert.Equal(3m, rates.For(Month(s, 5).OpeningPeople[0].Tier));   // the next snapshot: expert
    }

    // R-CRAFT-002: at most one tick per population id per month. A repeated receipt in one batch, or the same month applied
    // again, adds nothing and cannot promote twice; the next month ticks once more.
    [Fact]
    public void DuplicateReceiptsAwardOneTick_RCraft002()
    {
        var c = Content();
        var s = State(Craftsman(CivilianTier.Apprentice, 34, At(Industry)));
        var f = new Gdd03CraftWorkFixture(c, s, Slots);
        var receipts = f.Receipts(Month(s, 7));
        Assert.Single(receipts);
        f.Apply(receipts.Concat(receipts));
        Assert.Equal((CivilianTier.Apprentice, 35), (s.PopulationLedger.Get(0).Tier, s.PopulationLedger.Get(0).WorkXp));
        f.Apply(receipts);
        f.Apply(f.Receipts(Month(s, 7)));
        Assert.Equal(35, s.PopulationLedger.Get(0).WorkXp);
        f.Apply(f.Receipts(Month(s, 8)));
        Assert.Equal((CivilianTier.Expert, 36), (s.PopulationLedger.Get(0).Tier, s.PopulationLedger.Get(0).WorkXp));
    }

    // R-CRAFT-002 (#2472): idle, course, teaching, travel, service, building and farming months give no receipt, and a
    // novice craftsman's workshop month changes no qualification (Q55), nor does a civilian with no profession earn
    // craftsman XP in Industry. Nobody's record changes.
    [Fact]
    public void NonIndustryMonthsAwardNoWorkXp_RCraft002()
    {
        var c = Content();
        var s = State(
            Craftsman(CivilianTier.Apprentice, 35, PopulationAssignment.Unassigned),
            Craftsman(CivilianTier.Apprentice, 35, At(Builder)),
            Craftsman(CivilianTier.Apprentice, 35, At(Field)),
            Craftsman(CivilianTier.Apprentice, 35, new PopulationAssignment(PopulationAssignmentKind.Course, Course, 1), PopulationStatus.Trainee),
            Craftsman(CivilianTier.Expert, 40, new PopulationAssignment(PopulationAssignmentKind.Teaching, "teacher")),
            Craftsman(CivilianTier.Apprentice, 35, new PopulationAssignment(PopulationAssignmentKind.Travel, "travel"), PopulationStatus.CivilianInTransit, region: "south"),
            Craftsman(CivilianTier.Apprentice, 35, new PopulationAssignment(PopulationAssignmentKind.ActiveService, "service"), PopulationStatus.ActiveSoldier),
            Craftsman(CivilianTier.Novice, 0, At(Workshop)),
            Craftsman(CivilianTier.None, 0, At(Industry), profession: CivilianProfession.None));
        var before = s.Describe();
        var f = new Gdd03CraftWorkFixture(c, s, Slots);
        var receipts = f.Receipts(Month(s, 0));
        Assert.Empty(receipts);
        f.Apply(receipts);
        Assert.Equal(before, s.Describe());
    }

    // A batch with a bad receipt (building work, a person who is not a craftsman) is refused whole: no record changes and
    // the good receipt in it is not used up, so it still ticks once on its own.
    [Fact]
    public void ABadReceiptRefusesTheWholeBatch()
    {
        var c = Content();
        var s = State(Craftsman(CivilianTier.Apprentice, 10, At(Industry)), Craftsman(CivilianTier.None, 0, At(Industry), profession: CivilianProfession.None));
        var f = new Gdd03CraftWorkFixture(c, s, Slots);
        var good = f.Receipts(Month(s, 3));
        var before = s.Describe();
        Assert.Throws<ArgumentException>(() => f.Apply(good.Append(new Gdd03CraftWorkReceipt(0, 4, CraftTickActivity.Construction))));
        Assert.Throws<ArgumentException>(() => f.Apply(good.Append(new Gdd03CraftWorkReceipt(1, 3, CraftTickActivity.Industry))));
        Assert.Equal(before, s.Describe());
        f.Apply(good);
        Assert.Equal(11, s.PopulationLedger.Get(0).WorkXp);
    }

    // A novice craftsman earns no work XP: the tier check in WorkTick refuses the receipt before any change.
    [Fact]
    public void ANoviceCraftsmanReceiptIsRefusedBeforeAnyChange()
    {
        var s = State(Craftsman(CivilianTier.Apprentice, 10, At(Industry)), Craftsman(CivilianTier.Novice, 0, At(Industry)));
        var f = new Gdd03CraftWorkFixture(Content(), s, Slots);
        var before = s.Describe();
        Assert.Throws<ArgumentOutOfRangeException>(() => f.Apply(new[]
        {
            new Gdd03CraftWorkReceipt(0, 3, CraftTickActivity.Industry),
            new Gdd03CraftWorkReceipt(1, 3, CraftTickActivity.Industry),
        }));
        Assert.Equal(before, s.Describe());
    }

    // gdd-0.1 and legacy are unchanged: the fixture refuses a state without an initialized gdd-0.3 ledger.
    [Fact]
    public void Gdd01IsRefused()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        var s = new GameState(c); var before = s.Describe();
        Assert.Throws<InvalidOperationException>(() => new Gdd03CraftWorkFixture(c, s, Slots));
        Assert.Equal(before, s.Describe());
    }
}
