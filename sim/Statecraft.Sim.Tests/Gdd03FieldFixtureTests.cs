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
    static Gdd03SyntheticMonthContext Month(Content c, GameState s, params string[] notHeldAtFreeze) =>
        Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, 0, s.PopulationLedger.Units,
            Gdd03FieldFixture.Fields(c).Select(f => new Gdd03SyntheticFacilitySnapshot(f.Id, !notHeldAtFreeze.Contains(f.Id), 1)),
            Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());
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
    // yields nothing while the others still yield. The assignment is not changed: loss and retake (27M-1) are not here.
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

    // R-FIELD-002 under gdd-0.1 is unchanged: fields need no workers there, and this fixture refuses that ruleset.
    [Fact] public void Gdd01FieldsAreUnchanged()
    {
        var c = ContentLoader.Load(RealContentTests.ContentRoot());
        Assert.Equal(SaveGame.GddRuleset, c.Ruleset);
        var s = new GameState(c); var before = s.Describe();
        Assert.Throws<InvalidOperationException>(() => Gdd03FieldFixture.Fields(c));
        Assert.Throws<InvalidOperationException>(() => new Gdd03FieldFixture(c, s, Array.Empty<Gdd03FixtureSlot>()));
        Assert.Equal(before, s.Describe());
    }
}
