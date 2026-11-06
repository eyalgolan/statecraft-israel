using Statecraft.Sim;
using System.Globalization;

// R-FIELD-002 under gdd-0.3, fixture-only (R-POP-004). The ruling: owner, 3 November 2026, #1694, "One farmer per field
// (Recommended)"; the option read: "Each field takes one farmer; an unworked field produces nothing." The fields are the
// shipped regions.yaml list (Q36). People, regional pools and Israeli-held sets below are test data.
public class Gdd03FieldFixtureTests
{
    const string Valleys = "valleys", TelAviv = "tel_aviv_jaffa", Captor = "synthetic-captor";
    // Three of the six authored valleys fields (regions.yaml; Gdd03RegionAuthoringTests.ApprovedFields).
    const string V1 = "field/7,11", V2 = "field/7,12", V3 = "field/8,13";

    static Content Real() =>
        PlayerRegionsContentTests.Load(File.ReadAllText(Path.Combine(RealContentTests.ContentRoot(), "regions.yaml")));
    static MvpRules Rules(Content c) => c.RulesAt(c.Rules.Start).Mvp!;

    static PopulationUnit Person(PopulationLocation at, CivilianProfession profession = CivilianProfession.Farmer,
        CivilianTier tier = CivilianTier.Novice) =>
        new(0, PopulationStatus.FreeCivilian, 0, at, PopulationAssignment.Unassigned, profession, tier, 0, "", 0, PopulationEquipment.None);
    // A farmer living on the field's own hex, a member hex of its region (Gdd03RegionAuthoringTests).
    static PopulationLocation At(string region, int q, int r) => new(region, q, r);
    static PopulationLocation InValleys => At(Valleys, 7, 11);

    static GameState State(Content c, PopulationUnit[] people, Gdd03FixtureSlot[]? other = null, int telAvivIndustry = 0)
    {
        var pools = c.PlayerRegions.Regions.Keys.Select(id =>
            new PlayerRegionPool(id, 0, 0m, id == TelAviv ? telAvivIndustry : 0, 0, Array.Empty<PlayerRegionFacility>()));
        var targets = Gdd03FieldFixture.Targets(c).Concat((other ?? Array.Empty<Gdd03FixtureSlot>())
            .Select(x => new PopulationTarget(x.Id, PopulationAssignmentKind.Work, TelAviv)));
        return PlayerRegions.CreateFixture(c, pools, people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)),
            targets, new[] { Captor }, Array.Empty<string>());
    }

    // The month's snapshot (#1716, 27M-2), taken now; every field is available at the freeze unless listed as not held.
    static Gdd03SyntheticMonthContext Month(Content c, GameState s, params string[] notHeldAtFreeze) => MonthAt(c, s, 0, notHeldAtFreeze);
    static Gdd03SyntheticMonthContext MonthAt(Content c, GameState s, int ordinal, params string[] notHeldAtFreeze) =>
        Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, ordinal, s.PopulationLedger.Units,
            Gdd03FieldFixture.Fields(c).Select(f => new Gdd03SyntheticFacilitySnapshot(f.Id, !notHeldAtFreeze.Contains(f.Id), 1)),
            Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());
    static HashSet<string> AllHeld(Content c) => Gdd03FieldFixture.Fields(c).Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
    static void Refused(GameState s, Action a, string? message = null)
    {
        var before = s.Describe();
        var e = Assert.Throws<ArgumentException>(a);
        if (message != null) Assert.Contains(message, e.Message);
        Assert.Equal(before, s.Describe());
    }

    // Q36 and the ruling: the fields are the authored twenty (2/6/6/2/2/0/0/1/1), each a slot for one farmer
    // (`field.slots` 1), and the ids keep a plain minus sign under the Hebrew locale (#2402).
    [Theory] [InlineData("he-IL")] [InlineData("en-US")]
    public void TheFieldsAreTheAuthoredTwentyEachForOneFarmer(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var c = Real();
            var fields = Gdd03FieldFixture.Fields(c);
            Assert.Equal(20, fields.Count);
            Assert.Equal(fields.Select(f => f.Id).OrderBy(x => x, StringComparer.Ordinal), fields.Select(f => f.Id));
            var split = new Dictionary<string, int>(StringComparer.Ordinal) {
                ["galilee"] = 2, ["valleys"] = 6, ["haifa_sharon"] = 6, ["tel_aviv_jaffa"] = 2, ["central_plain"] = 2,
                ["jerusalem_hills"] = 0, ["northern_hills"] = 0, ["southern_coast"] = 1, ["negev"] = 1 };
            foreach (var region in c.PlayerRegions.Regions.Values)
            {
                Assert.Equal(split[region.Id], fields.Count(f => f.Region == region.Id));
                foreach (var h in region.FieldHexes)
                    Assert.Single(fields, f => f.Region == region.Id && f.Q == h.Q && f.R == h.R
                        && f.Id == "field/" + h.Q.ToString(CultureInfo.InvariantCulture) + "," + h.R.ToString(CultureInfo.InvariantCulture));
            }
            Assert.Contains(fields, f => f.Id == "field/-11,31" && f.Region == "southern_coast");
            Assert.All(fields, f => Assert.DoesNotContain('‎', f.Id));
            Assert.Equal(1, Gdd03FieldRates.FromCatalogue(c.Gdd03Balance!).SlotsPerField);
            Assert.All(Gdd03FieldFixture.Slots(c), slot => { Assert.Equal(1, slot.Capacity); Assert.Equal(Gdd03FixtureWork.Farm, slot.Work); });
            Assert.Equal(fields.Select(f => (f.Id, f.Region)),
                Gdd03FieldFixture.Targets(c).Select(t => (t.Id, t.Region)));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    // The ruling: "an unworked field produces nothing". All twenty held, nobody on them: no Food and no Money.
    [Fact] public void AnUnworkedFieldYieldsNothing()
    {
        var c = Real(); var s = State(c, new[] { Person(InValleys) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        var y = f.Yield(Rules(c), Month(c, s), AllHeld(c));
        Assert.Equal(20, y.Fields.Count);
        Assert.All(y.Fields.Values, food => Assert.Equal(0, food));
        Assert.Equal(0, y.Food); Assert.Equal(0, y.Money);
    }

    // GDD.md:385 (owner, 1 November 2026, "Main's base x 0.3's tiers (Recommended)"): farmer Food 2x / 3x / 5x
    // `field_food_per_turn` by tier (#2112), with a test-only base of 2 as Gdd03PerUnitRatesTests does. No Money.
    [Theory] [InlineData(1, 2, 3, 5)] [InlineData(2, 4, 6, 10)]
    public void AFarmerYieldsTwoThreeFiveTimesFieldFoodPerTurn(int baseFood, int novice, int trained, int veteran)
    {
        var c = Real(); var r = Rules(c);
        Assert.Equal(1, r.FieldFoodPerTurn);
        r.FieldFoodPerTurn = baseFood; // test-only base
        var s = State(c, new[] { Person(InValleys, tier: CivilianTier.Novice), Person(InValleys, tier: CivilianTier.Trained),
            Person(InValleys, tier: CivilianTier.Veteran) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        f.Staff(0, V1, israeliHeld: true); f.Staff(1, V2, israeliHeld: true); f.Staff(2, V3, israeliHeld: true);
        var before = s.Describe();
        var y = f.Yield(r, Month(c, s), AllHeld(c));
        Assert.Equal(novice, y.Fields[V1]); Assert.Equal(trained, y.Fields[V2]); Assert.Equal(veteran, y.Fields[V3]);
        Assert.Equal(17, y.Fields.Values.Count(food => food == 0));
        Assert.Equal(novice + trained + veteran, y.Food); Assert.Equal(0, y.Money);
        Assert.Equal(before, s.Describe()); // a quote: no Food, XP or state change
    }

    // The ruling: each field takes one farmer. A second farmer is refused and the first keeps the field; the same
    // farmer again is the Q34 no-op. A caller's other Work slot shares the one inventory (an Industry worker too).
    [Fact] public void ASecondFarmerOnAFieldIsRefused()
    {
        var c = Real();
        var industry = new Gdd03FixtureSlot("synthetic-industry", Gdd03FixtureWork.Industry, 1, "winery");
        var s = State(c, new[] { Person(InValleys), Person(InValleys),
            Person(At(TelAviv, -1, 20), CivilianProfession.None, CivilianTier.None) }, new[] { industry }, telAvivIndustry: 1);
        var f = new Gdd03FieldFixture(c, s, new[] { industry });
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Staff(0, V1, israeliHeld: true));
        Refused(s, () => f.Staff(1, V1, israeliHeld: true), "Fixture destination is full");
        Assert.Equal(V1, s.PopulationLedger.Get(0).Assignment.Target);
        var same = s.Describe();
        Assert.Equal(Gdd03FixtureAssignmentResult.NoOp, f.Staff(0, V1, israeliHeld: true)); Assert.Equal(same, s.Describe());
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Staff(1, V2, israeliHeld: true));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, new Gdd03AssignmentFixture(c, s, Gdd03FieldFixture.Slots(c).Append(industry))
            .Assign(2, industry.Id));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Release(0));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Staff(1, V1, israeliHeld: true)); // a freed field takes one farmer
        Refused(s, () => f.Release(2), "not on a field"); // the field fixture releases only farmers
        Assert.Equal(industry.Id, s.PopulationLedger.Get(2).Assignment.Target);
    }

    // Q27 ("farmers live in the region they farm", docs/GDD.md:373): a farmer living in Tel Aviv-Jaffa cannot take a
    // valleys field; the ledger's region check refuses it before any change.
    [Fact] public void AFarmerMustLiveInTheFieldsRegion()
    {
        var c = Real(); var s = State(c, new[] { Person(At(TelAviv, -1, 20)) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        Refused(s, () => f.Staff(0, V1, israeliHeld: true), "Assignment region/progress is inconsistent");
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Staff(0, "field/-1,20", israeliHeld: true));
    }

    // Q37 ("Only an Israeli-held hex can be farmed", docs/GDD.md:373): a field that is not Israeli-held is refused
    // before any change, and so is an unknown field.
    [Fact] public void OnlyAnIsraeliHeldFieldCanBeStaffed()
    {
        var c = Real(); var s = State(c, new[] { Person(InValleys) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        Refused(s, () => f.Staff(0, V1, israeliHeld: false), "Israeli-held");
        Refused(s, () => f.Staff(0, "field/0,0", israeliHeld: true), "Unknown field");
        Assert.Equal(PopulationAssignmentKind.Unassigned, s.PopulationLedger.Get(0).Assignment.Kind);
    }

    // Q37 and #1694 ("Enemy control ... disables the slot"): a field staffed at the freeze but no longer Israeli-held
    // yields nothing while the others still yield. The assignment is not changed; retaking is in the settlement test below.
    [Fact] public void AFieldNoLongerIsraeliHeldYieldsNothing()
    {
        var c = Real(); var s = State(c, new[] { Person(InValleys), Person(InValleys) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        f.Staff(0, V1, israeliHeld: true); f.Staff(1, V2, israeliHeld: true);
        var month = Month(c, s); var held = AllHeld(c); held.Remove(V1);
        var y = f.Yield(Rules(c), month, held);
        Assert.Equal(0, y.Fields[V1]); Assert.Equal(2, y.Fields[V2]); Assert.Equal(2, y.Food);
        Assert.Equal(V1, s.PopulationLedger.Get(0).Assignment.Target);
        // Not held at the freeze either: the snapshot does not count the farmer as working (#1716).
        Assert.Equal(0, f.Yield(Rules(c), Month(c, s, V1), AllHeld(c)).Fields[V1]);
    }

    // R-FIELD-002: only a farmer holds a field. Owner ruling, 4 November 2026, #1694 (Q22), asked "May only a unit with
    // the Farmer profession staff a field?", answered "yes" (issuecomment-6623124984). A civilian with no profession, a
    // pioneer and a craftsman are refused with the state unchanged; a farmer then takes the same field.
    [Fact] public void OnlyAFarmerWorksAField()
    {
        var c = Real();
        var s = State(c, new[] { Person(InValleys, CivilianProfession.None, CivilianTier.None),
            Person(InValleys, CivilianProfession.Pioneer, CivilianTier.Veteran),
            Person(InValleys, CivilianProfession.Craftsman, CivilianTier.Expert), Person(InValleys) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        foreach (var id in new[] { 0, 1, 2 }) Refused(s, () => f.Staff(id, V1, israeliHeld: true), "Only a farmer");
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Staff(3, V1, israeliHeld: true));
        Assert.Equal(V1, s.PopulationLedger.Get(3).Assignment.Target);
    }

    // 27M-2 ("Farming counts whole months from the month's snapshot") and R-VET-002 (the turn-start tier): a farmer
    // staffed after the freeze yields nothing that month, and a promotion after the freeze does not change the yield.
    [Fact] public void FarmingCountsTheMonthsSnapshot()
    {
        var c = Real(); var s = State(c, new[] { Person(InValleys, tier: CivilianTier.Novice), Person(InValleys) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        f.Staff(0, V1, israeliHeld: true);
        var month = Month(c, s);
        f.Staff(1, V2, israeliHeld: true);
        var p = s.PopulationLedger.Get(0);
        s.PopulationLedger.Replace(new PopulationUnit(p.Id, p.Status, p.ReceptionTicks, p.Location, p.Assignment,
            p.Profession, CivilianTier.Veteran, p.WorkXp, p.Qualification, p.CombatXp, p.Equipment));
        var y = f.Yield(Rules(c), month, AllHeld(c));
        Assert.Equal(2, y.Fields[V1]); Assert.Equal(0, y.Fields[V2]); Assert.Equal(2, y.Food);
        Assert.Equal(5 + 2, f.Yield(Rules(c), Month(c, s), AllHeld(c)).Food); // next month's snapshot has both
    }

    // Guard: a captured person holds no field (PopulationLedger: captives have no local assignment), so #1689 Q2
    // (captured units in Food demand) is not involved; this slice produces Food and computes no demand.
    [Fact] public void CapturedPeopleHoldNoField()
    {
        var c = Real(); var s = State(c, new[] { Person(InValleys) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        f.Staff(0, V1, israeliHeld: true);
        var p = s.PopulationLedger.Get(0);
        Refused(s, () => s.PopulationLedger.Replace(p.WithPlacement(PopulationStatus.Captured, null, p.Assignment, Captor, p.Location)));
        s.PopulationLedger.Replace(p.WithPlacement(PopulationStatus.Captured, null, PopulationAssignment.Unassigned, Captor, p.Location));
        var month = Month(c, s);
        Assert.Empty(month.WorkEligibleIds);
        Assert.Equal(0, f.Yield(Rules(c), month, AllHeld(c)).Food);
        Refused(s, () => f.Staff(0, V1, israeliHeld: true));
    }

    // #2468, R-VET-001 and R-VET-002: farmer work XP from the month's staffed fields, the farmer counterpart of #2421's
    // pioneer producer. A received farmer (reception.turns done) on a field worked in the snapshot and still Israeli-held.
    static PopulationUnit Farmer(Content c, CivilianTier tier = CivilianTier.Trained, int tools = 0) =>
        new(0, PopulationStatus.FreeCivilian, c.RequirePopulationInteger("reception.turns"), InValleys, PopulationAssignment.Unassigned,
            CivilianProfession.Farmer, tier, 0, "infantry", 4, new PopulationEquipment(toolsId: "tools/" + tools, toolsPaid: true));
    const string Course = "course-c", Road = "road-t";
    // The field fixture's targets plus a valleys course, a road and an Industry slot, so a farmer can be on each.
    static GameState WorkState(Content c, Gdd03FixtureSlot industry, params PopulationUnit[] people)
    {
        var pools = c.PlayerRegions.Regions.Keys.Select(id =>
            new PlayerRegionPool(id, 0, 0m, id == Valleys ? 1 : 0, 0, Array.Empty<PlayerRegionFacility>()));
        var targets = Gdd03FieldFixture.Targets(c).Concat(new[] {
            new PopulationTarget(industry.Id, PopulationAssignmentKind.Work, Valleys),
            new PopulationTarget(Course, PopulationAssignmentKind.Course, Valleys, 3),
            new PopulationTarget(Road, PopulationAssignmentKind.Travel, Valleys) });
        return PlayerRegions.CreateFixture(c, pools, people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)),
            targets, new[] { Captor }, new[] { "infantry" });
    }
    static Gdd03SyntheticMonthContext WorkMonth(Content c, GameState s, string industry, params string[] notHeldAtFreeze) =>
        Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, 0, s.PopulationLedger.Units,
            Gdd03FieldFixture.Fields(c).Select(f => new Gdd03SyntheticFacilitySnapshot(f.Id, !notHeldAtFreeze.Contains(f.Id), 1))
                .Concat(new[] { new Gdd03SyntheticFacilitySnapshot(industry, true, 1), new Gdd03SyntheticFacilitySnapshot(Course, true, 3) }),
            Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());

    // R-VET-001, R-VET-002 (#2468): one worked, still-held field month gives its farmer exactly one work XP. A field lost
    // after the freeze, one not held at the freeze, a farmer moved to another field after the freeze (the wrong target), an
    // Industry worker, idle, travelling, teaching and training farmers, and every unstaffed field give none. The turn-start
    // state is not changed, and the same snapshot resolved twice from it gives the same single tick (no double credit).
    [Fact] public void AWorkedFieldMonthGivesItsFarmerOneWorkXp()
    {
        var c = Real();
        var valleys = Gdd03FieldFixture.Fields(c).Where(f => f.Region == Valleys).Select(f => f.Id).ToArray();
        var industry = new Gdd03FixtureSlot("synthetic-industry", Gdd03FixtureWork.Industry, 1, "winery");
        var s = WorkState(c, industry, Enumerable.Range(0, 9).Select(i => Farmer(c, tools: i)).ToArray());
        var f = new Gdd03FieldFixture(c, s, new[] { industry });
        f.Staff(0, valleys[0], israeliHeld: true);   // worked and held: the one tick
        f.Staff(1, valleys[1], israeliHeld: true);   // lost after the freeze
        f.Staff(2, valleys[2], israeliHeld: true);   // not held at the freeze
        f.Staff(3, valleys[3], israeliHeld: true);   // moved to another field after the freeze
        s.PopulationLedger.ChangeAssignment(4, new PopulationAssignment(PopulationAssignmentKind.Work, industry.Id));
        s.PopulationLedger.ChangeAssignment(5, new PopulationAssignment(PopulationAssignmentKind.Teaching, Course));
        s.PopulationLedger.Transition(6, PopulationStatus.Trainee, InValleys, new PopulationAssignment(PopulationAssignmentKind.Course, Course));
        s.PopulationLedger.Transition(7, PopulationStatus.CivilianInTransit, InValleys, new PopulationAssignment(PopulationAssignmentKind.Travel, Road));
        // 8 stays idle.
        var month = WorkMonth(c, s, industry.Id, valleys[2]);
        f.Release(3); f.Staff(3, valleys[4], israeliHeld: true);
        var held = AllHeld(c); held.Remove(valleys[1]);
        var before = s.Describe();

        var once = Gdd03FieldFixture.ResolveWorkXp(c, s, month, held);
        Assert.Equal(new[] { 1, 0, 0, 0, 0, 0, 0, 0, 0 }, once.PopulationLedger.Units.Select(p => p.WorkXp));
        Assert.All(once.PopulationLedger.Units, p => Assert.Equal(CivilianTier.Trained, p.Tier));
        Assert.Equal(before, s.Describe());
        Assert.Equal(once.Describe(), Gdd03FieldFixture.ResolveWorkXp(c, s, month, held).Describe());
    }

    // R-VET-001 (#2468): novice and trained farmers become veteran at profession.work_to_veteran (9) worked months. The
    // ninth month's Food is quoted at its snapshot tier (R-VET-002); the record is veteran from the next month, with the
    // same id, assignment, placement, tools and military history.
    [Theory] [InlineData(CivilianTier.Novice)] [InlineData(CivilianTier.Trained)]
    public void TheNinthWorkedMonthPromotesTheSameFarmerToVeteran(CivilianTier tier)
    {
        var c = Real(); var r = Rules(c);
        Assert.Equal(9, c.RequirePopulationInteger("profession.work_to_veteran"));
        var industry = new Gdd03FixtureSlot("synthetic-industry", Gdd03FixtureWork.Industry, 1, "winery");
        var s = WorkState(c, industry, Farmer(c, tier));
        var f = new Gdd03FieldFixture(c, s, new[] { industry });
        f.Staff(0, V1, israeliHeld: true);
        var start = s.PopulationLedger.Get(0);
        for (int i = 0; i < 8; i++) s = Gdd03FieldFixture.ResolveWorkXp(c, s, WorkMonth(c, s, industry.Id), AllHeld(c));
        Assert.Equal((8, tier), (s.PopulationLedger.Get(0).WorkXp, s.PopulationLedger.Get(0).Tier));
        var ninth = WorkMonth(c, s, industry.Id);
        Assert.Equal(tier == CivilianTier.Novice ? 2 : 3, f.Yield(r, ninth, AllHeld(c)).Fields[V1]);
        s = Gdd03FieldFixture.ResolveWorkXp(c, s, ninth, AllHeld(c));
        var vet = s.PopulationLedger.Get(0);
        Assert.Equal((9, CivilianTier.Veteran), (vet.WorkXp, vet.Tier));
        Assert.Equal(start.Id, vet.Id); Assert.Equal(start.Status, vet.Status);
        Assert.Equal(start.Assignment.Canonical(), vet.Assignment.Canonical());
        Assert.Equal(start.Location!.Canonical(), vet.Location!.Canonical());
        Assert.Equal(start.Equipment.Canonical(), vet.Equipment.Canonical());
        Assert.Equal((start.Qualification, start.CombatXp), (vet.Qualification, vet.CombatXp));
        Assert.Equal(5, f.Yield(r, WorkMonth(c, s, industry.Id), AllHeld(c)).Fields[V1]);
    }

    // R-VET-001 (#2804 item 12): the producer checks for a veteran before AfterWork, so a veteran farmer at int.MaxValue
    // work XP (the ledger allows any nonnegative work XP) keeps its XP instead of overflowing, and its field still yields
    // the veteran 5. TEST-ONLY SYNTHETIC work XP.
    [Fact] public void AVeteranFarmerAtMaxWorkXpKeepsItsXp()
    {
        var c = Real(); var r = Rules(c);
        var industry = new Gdd03FixtureSlot("synthetic-industry", Gdd03FixtureWork.Industry, 1, "winery");
        var s = WorkState(c, industry, new PopulationUnit(0, PopulationStatus.FreeCivilian, c.RequirePopulationInteger("reception.turns"),
            InValleys, PopulationAssignment.Unassigned, CivilianProfession.Farmer, CivilianTier.Veteran, int.MaxValue, "infantry", 4,
            new PopulationEquipment(toolsId: "tools/0", toolsPaid: true)));
        var f = new Gdd03FieldFixture(c, s, new[] { industry });
        f.Staff(0, V1, israeliHeld: true);
        var month = WorkMonth(c, s, industry.Id);
        var before = s.Describe();
        var next = Gdd03FieldFixture.ResolveWorkXp(c, s, month, AllHeld(c));
        Assert.Equal((int.MaxValue, CivilianTier.Veteran), (next.PopulationLedger.Get(0).WorkXp, next.PopulationLedger.Get(0).Tier));
        Assert.Equal(before, s.Describe());
        Assert.Equal(5, f.Yield(r, month, AllHeld(c)).Fields[V1]);
    }

    // #2468 review: a snapshot with more working farmers on a field than its `field.slots` (1) is refused whole, as
    // Yield refuses it, so no farmer is credited for a slot another farmer holds; the turn-start state is unchanged.
    [Fact] public void AnOverOccupiedFieldMonthIsRefused()
    {
        var c = Real();
        var industry = new Gdd03FixtureSlot("synthetic-industry", Gdd03FixtureWork.Industry, 1, "winery");
        var s = WorkState(c, industry, Farmer(c, tools: 0), Farmer(c, tools: 1), Farmer(c, tools: 2));
        var f = new Gdd03FieldFixture(c, s, new[] { industry });
        f.Staff(0, V1, israeliHeld: true);
        f.Staff(2, V2, israeliHeld: true);
        // The fixture refuses a second farmer (ASecondFarmerOnAFieldIsRefused), so the over-occupied record is written
        // straight to the ledger, as a malformed synthetic month would carry it.
        s.PopulationLedger.ChangeAssignment(1, new PopulationAssignment(PopulationAssignmentKind.Work, V1));
        var month = WorkMonth(c, s, industry.Id);
        Refused(s, () => Gdd03FieldFixture.ResolveWorkXp(c, s, month, AllHeld(c)), "more than one farmer");
    }

    // R-FIELD-002 and R-CITY-010 under gdd-0.3 (#2471): a staffed field still Israeli-held at settlement adds its 2 / 3 / 5
    // to the national Food stock once, in field-id order, one cause each; an unstaffed field adds nothing. A field lost
    // (at the freeze or only by settlement) adds nothing and keeps its farmer, tier and assignment (D6: "its people stay");
    // retaken, it yields again from the next snapshot with no new assignment (D6: "everything resumes and nothing is deleted").
    [Fact] public void StaffedHeldFieldsSettleIntoTheFoodStockAndALostFieldResumesOnRetaking()
    {
        var c = Real();
        var s = State(c, new[] { Person(InValleys, tier: CivilianTier.Novice), Person(InValleys, tier: CivilianTier.Trained),
            Person(InValleys, tier: CivilianTier.Veteran) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        f.Staff(2, V3, israeliHeld: true); f.Staff(0, V1, israeliHeld: true); f.Staff(1, V2, israeliHeld: true);
        var people = s.PopulationLedger.Units;
        s.Food = 7;

        var why = new List<Cause>();
        var settled = f.Settle(Rules(c), MonthAt(c, s, 0), AllHeld(c), why);
        Assert.Equal(10, settled.Food);
        Assert.Equal(17, settled.Fields.Values.Count(food => food == 0)); // the unstaffed fields: no Food and no cause
        Assert.Equal(17, s.Food);
        Assert.Equal(new[] { (V1, 2.0), (V2, 3.0), (V3, 5.0) }, why.Select(x => (x.Args[0], x.Delta)));
        Assert.All(why, x => Assert.Equal(("economy", "food", CauseKeys.FieldFood), (x.System, x.Target, x.Key)));

        // Lost after the freeze: control is checked again at settlement.
        var held = AllHeld(c); held.Remove(V1);
        why.Clear();
        f.Settle(Rules(c), MonthAt(c, s, 1), held, why);
        Assert.Equal(17 + 8, s.Food);
        Assert.Equal(new[] { V2, V3 }, why.Select(x => x.Args[0]));
        // Lost at the freeze: the snapshot does not count the farmer as working.
        f.Settle(Rules(c), MonthAt(c, s, 2, V1), AllHeld(c), new List<Cause>()); // freeze-only loss: settlement-time control is held
        Assert.Equal(17 + 8 + 8, s.Food);
        Assert.Equal(people, s.PopulationLedger.Units); // same ids, tiers and assignments

        // Retaken: the next snapshot yields again, with no Staff call.
        f.Settle(Rules(c), MonthAt(c, s, 3), AllHeld(c), new List<Cause>());
        Assert.Equal(17 + 8 + 8 + 10, s.Food);
        Assert.Equal(people, s.PopulationLedger.Units);
    }

    // R-CITY-010: a month is credited once per fixture; a second Settle for the same month is refused and changes nothing.
    [Fact] public void ASettledMonthIsRefusedAndTheFoodStockStaysPut()
    {
        var c = Real();
        var s = State(c, new[] { Person(InValleys, tier: CivilianTier.Novice) });
        var f = new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>());
        f.Staff(0, V1, israeliHeld: true);
        s.Food = 7;
        f.Settle(Rules(c), MonthAt(c, s, 4), AllHeld(c), new List<Cause>());
        Assert.Equal(9, s.Food);
        var why = new List<Cause>();
        Refused(s, () => f.Settle(Rules(c), MonthAt(c, s, 4), AllHeld(c), why), "already settled");
        Assert.Empty(why);
        f.Settle(Rules(c), MonthAt(c, s, 5), AllHeld(c), new List<Cause>()); // the next month still settles
        Assert.Equal(11, s.Food);
    }

    // R-FIELD-002 under gdd-0.1 is unchanged: fields need no workers there, and this fixture refuses that ruleset.
    [Fact] public void Gdd01FieldsAreUnchanged()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(SaveGame.GddRuleset, c.Ruleset);
        var s = new GameState(c); var before = s.Describe();
        Assert.Throws<InvalidOperationException>(() => Gdd03FieldFixture.Fields(c));
        Assert.Throws<InvalidOperationException>(() => new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>()));
        Assert.Throws<InvalidOperationException>(() => Gdd03FieldFixture.ResolveWorkXp(c, s,
            Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, 0, Array.Empty<PopulationUnit>(),
                Array.Empty<Gdd03SyntheticFacilitySnapshot>(), Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(),
                Array.Empty<Gdd03SyntheticPendingEntry>()), new HashSet<string>()));
        Assert.Equal(before, s.Describe());
    }
}
