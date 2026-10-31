using Statecraft.Sim;
using System.Globalization;

// Synthetic-only inventories, capacities, ids, labels, XP and random seed below are test data.
public class Gdd03AssignmentFixtureTests
{
    static Content Content(string identity = SaveGame.Gdd03Ruleset)
    {
        var c = PopulationLedgerTests.Content(identity);
        c.Gdd03Balance!.Values["industry.income"] = new Gdd03DesignValue {
            Basis = "Agreed", Note = "content/gdd03-balance.yaml:138-141", Values = new() { 1m, 2m, 3m } };
        return c;
    }
    static readonly PopulationLocation Place = new("synthetic-place");
    static readonly Gdd03FixtureSlot[] Slots = {
        new("synthetic-industry", Gdd03FixtureWork.Industry, 1, "winery"),
        new("synthetic-builder", Gdd03FixtureWork.Builder, 1, "builder"),
        new("synthetic-farm", Gdd03FixtureWork.Farm, 1, "farm"),
        new("synthetic-workshop", Gdd03FixtureWork.Workshop, 1, "workshop") };
    static PopulationUnit Person(CivilianProfession profession = CivilianProfession.None, CivilianTier tier = CivilianTier.None) =>
        new(0, PopulationStatus.FreeCivilian, 6, Place, PopulationAssignment.Unassigned,
            profession, tier, profession == CivilianProfession.None ? 0 : 7, "", 0, PopulationEquipment.None);
    static GameState State(Content c, params PopulationUnit[] people) => PopulationLedger.CreateFixture(c,
        new PopulationBindings(new[] { Place }, Slots.Select(x => new PopulationTarget(x.Id, PopulationAssignmentKind.Work, Place.Region)),
            Array.Empty<string>(), Array.Empty<string>()), people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)));

    // R-CITY-001/002/006: synthetic composition of the existing regional pool and assignment fixture: occupancy, not a second inventory.
    [Theory] [InlineData(false)] [InlineData(true)]
    public void RegionalPoolRefusesAThirdWorkerWithoutReleasingTheirAssignment(bool workshop)
    {
        const string region = "ramla_rehovot";
        var c = PlayerRegionsContentTests.Load();
        c.Gdd03Balance!.Values["industry.income"] = new Gdd03DesignValue {
            Basis = "Agreed", Note = "synthetic regional fixture rates", Values = new() { 1m, 2m, 3m } };
        var work = workshop ? Gdd03FixtureWork.Workshop : Gdd03FixtureWork.Industry;
        var slots = Enumerable.Range(0, 3).Select(i => new Gdd03FixtureSlot("synthetic-regional-" + i, work, 1, "job")).ToArray();
        var place = new PopulationLocation(region, -4, 24);
        var people = Enumerable.Range(0, 3).Select(i => new PopulationInventoryRow("synthetic", i,
            new PopulationUnit(0, PopulationStatus.FreeCivilian, c.RequirePopulationInteger("reception.turns"), place,
                PopulationAssignment.Unassigned, CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None)));
        var state = PlayerRegions.CreateFixture(c,
            new[] { new PlayerRegionPool(region, 3, 1m, 2, 2, Array.Empty<PlayerRegionFacility>()) }, people,
            slots.Select(x => new PopulationTarget(x.Id, PopulationAssignmentKind.Work, region)), Array.Empty<string>(), Array.Empty<string>());
        var fixture = new Gdd03AssignmentFixture(c, state, slots);
        fixture.Assign(0, slots[0].Id); fixture.Assign(1, slots[1].Id);
        var before = state.Describe();
        Assert.Throws<ArgumentException>(() => fixture.Assign(2, slots[2].Id));
        Assert.Equal(before, state.Describe());
        Assert.Equal(2, PlayerRegions.Assigned(state, region, PopulationAssignmentKind.Work));
        state.PopulationLedger.Reconcile();
    }

    // R-CITY-001, R-POP-002: assignment and reception share the initialized ledger's completion limit.
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void ReceptionLimitMustMatchTheInitializedLedger(int delta)
    {
        var c = Content(); var s = State(c, Person());
        var before = s.Describe();
        c.Gdd03Balance!.Values["reception.turns"].Values = new() { s.PopulationLedger.ReceptionLimit + delta };
        var error = Assert.Throws<ContentException>(() => new Gdd03AssignmentFixture(c, s, Slots));
        Assert.Contains("values.reception.turns", error.Message);
        Assert.Equal(before, s.Describe());
    }

    // R-CITY-001, R-CITY-002: destination validation must precede any release or resource mutation.
    [Fact] public void FullMissingAndIneligibleDestinationsPreserveWholeState()
    {
        var c = Content(); var s = State(c, Person(), Person());
        var f = new Gdd03AssignmentFixture(c, s, Slots);
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(0, Slots[0].Id));
        f.Assign(1, Slots[1].Id);
        foreach (var target in new[] { Slots[0].Id, "missing" }) {
            var before = s.Describe(); Assert.Throws<ArgumentException>(() => f.Assign(1, target)); Assert.Equal(before, s.Describe());
        }
        var same = s.Describe();
        Assert.Equal(Gdd03FixtureAssignmentResult.NoOp, f.Assign(0, Slots[0].Id)); Assert.Equal(same, s.Describe());
        s.PopulationLedger.Replace(s.PopulationLedger.Get(1).WithReception(0));
        same = s.Describe(); Assert.Throws<ArgumentException>(() => f.Assign(1, Slots[2].Id)); Assert.Equal(same, s.Describe());
        Assert.Throws<ArgumentException>(() => new Gdd03AssignmentFixture(c, s, Slots.Concat(new[] { Slots[0] })));
    }

    // R-CITY-001: independent seeded model checks capacity, exclusivity and conservation after every step.
    [Fact] public void GeneratedReassignmentsUseOnlyTheAuthoritativeLedger()
    {
        var c = Content(); var s = State(c, Person(), Person(), Person());
        var f = new Gdd03AssignmentFixture(c, s, Slots); var model = new string?[3]; var rng = new Random(1692);
        for (var step = 0; step < 500; step++) {
            var id = rng.Next(3); var index = rng.Next(6); var target = index < 4 ? Slots[index].Id : index == 4 ? null : "missing";
            var before = s.Describe();
            var invalid = target == "missing" || (target != null && model.Where((_, i) => i != id).Contains(target));
            if (invalid) { Assert.Throws<ArgumentException>(() => f.Assign(id, target)); Assert.Equal(before, s.Describe()); }
            else if (target != null && model[id] == target) {
                Assert.Equal(Gdd03FixtureAssignmentResult.NoOp, f.Assign(id, target)); Assert.Equal(before, s.Describe());
            } else { Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(id, target)); model[id] = target; }
            Assert.Equal(model, s.PopulationLedger.Units.Select(p => p.Assignment.Target).ToArray());
            Assert.Equal(new[] { 0, 1, 2 }, s.PopulationLedger.Units.Select(p => p.Id)); s.PopulationLedger.Reconcile();
        }
    }

    // R-CITY-002, R-CRAFT-002: tier quotation only, no production or Money settlement.
    [Theory] [InlineData("he-IL")] [InlineData("en-US")]
    public void RatesAndLabelsHaveNoSecondPaymentOrProfessionChange(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            foreach (var item in new[] {
                (CivilianProfession.None, CivilianTier.None, 1m),
                (CivilianProfession.Farmer, CivilianTier.Novice, 1m),
                (CivilianProfession.Farmer, CivilianTier.Trained, 1m),
                (CivilianProfession.Pioneer, CivilianTier.Veteran, 1m),
                (CivilianProfession.Craftsman, CivilianTier.Apprentice, 2m),
                (CivilianProfession.Craftsman, CivilianTier.Expert, 3m) }) {
                var c = Content(); var s = State(c, Person(item.Item1, item.Item2)); var before = s.PopulationLedger.Get(0);
                var f = new Gdd03AssignmentFixture(c, s, Slots); Assert.Equal(0m, f.IndustryRate(Slots[0].Id));
                f.Assign(0, Slots[0].Id); var canonical = s.Describe(); Assert.Equal(item.Item3, f.IndustryRate(Slots[0].Id));
                var relabelled = Slots.Select(x => new Gdd03FixtureSlot(x.Id, x.Work, x.Capacity, "generic"));
                Assert.Equal(item.Item3, new Gdd03AssignmentFixture(c, s, relabelled).IndustryRate(Slots[0].Id)); Assert.Equal(canonical, s.Describe());
                foreach (var slot in Slots.Skip(1)) { f.Assign(0, slot.Id); Assert.Equal(0m, f.IndustryRate(slot.Id)); Assert.Equal(0m, f.IndustryRate(Slots[0].Id)); }
                var after = s.PopulationLedger.Get(0); Assert.Equal(before.Profession, after.Profession); Assert.Equal(before.WorkXp, after.WorkXp);
                Assert.Same(before.Equipment, after.Equipment); Assert.Equal(before.Tier, after.Tier);
            }
        } finally { CultureInfo.CurrentCulture = previous; }
    }

    // R-CITY-001, R-POP-002: an assigned worker's reassignment to a bound, non-full, same-kind slot in a
    // foreign region is refused at the ledger region check inside the no-release-first Replace path.
    [Fact] public void AssignedWorkerForeignRegionReassignmentKeepsOriginalJob()
    {
        var c = Content();
        var own = new PopulationLocation("synthetic-own"); var foreign = new PopulationLocation("synthetic-foreign");
        var ownIndustry = new Gdd03FixtureSlot("synthetic-own-industry", Gdd03FixtureWork.Industry, 1, "generic");
        var foreignFarm = new Gdd03FixtureSlot("synthetic-foreign-farm", Gdd03FixtureWork.Farm, 1, "generic");
        var bindings = new PopulationBindings(new[] { own, foreign }, new[] {
                new PopulationTarget(ownIndustry.Id, PopulationAssignmentKind.Work, own.Region),
                new PopulationTarget(foreignFarm.Id, PopulationAssignmentKind.Work, foreign.Region) },
            Array.Empty<string>(), Array.Empty<string>());
        var worker = new PopulationUnit(0, PopulationStatus.FreeCivilian, 6, own, PopulationAssignment.Unassigned,
            CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None);
        var s = PopulationLedger.CreateFixture(c, bindings, new[] { new PopulationInventoryRow("synthetic", 0, worker) });
        var f = new Gdd03AssignmentFixture(c, s, new[] { ownIndustry, foreignFarm });

        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(0, ownIndustry.Id));
        var before = s.Describe();
        var error = Assert.Throws<ArgumentException>(() => f.Assign(0, foreignFarm.Id));
        Assert.Contains("Assignment region/progress is inconsistent", error.Message); // region branch, not unknown/full/unbound
        Assert.Equal(before, s.Describe());
        Assert.Equal(ownIndustry.Id, s.PopulationLedger.Get(0).Assignment.Target);
    }

    // R-CITY-001, R-POP-002: foundation refusal rolls back a valid-capacity destination too.
    [Fact] public void FoundationRefusalAndAggregateGuardsRemainAtomic()
    {
        var c = Content(); var s = State(c, Person());
        var invalidBinding = new Gdd03FixtureSlot("synthetic-unbound", Gdd03FixtureWork.Industry, 1, "generic");
        var f = new Gdd03AssignmentFixture(c, s, Slots.Concat(new[] { invalidBinding }));
        f.Assign(0, Slots[0].Id); var before = s.Describe();
        Assert.Throws<ArgumentException>(() => f.Assign(0, invalidBinding.Id)); Assert.Equal(before, s.Describe());
        Assert.Throws<NotSupportedException>(() => Simulation.Step(c, s, Array.Empty<Command>())); Assert.Equal(before, s.Describe());
        var cloned = s.Clone(); var cloneFixture = new Gdd03AssignmentFixture(c, cloned, Slots);
        cloneFixture.Assign(0, Slots[1].Id); Assert.Equal(before, s.Describe());
        Assert.Equal(Slots[1].Id, cloned.PopulationLedger.Get(0).Assignment.Target);
    }

    // R-CITY-002: typed reader has no missing/Deferred/malformed/old-mode fallback.
    [Fact] public void InvalidRatesAndOldIdentitiesAreRefused()
    {
        foreach (var numbers in new[] { new List<decimal>(), new() { 1m, 2m }, new() { 1m, 0m, 3m }, new() { 1m, 2m, 3m, 4m } }) {
            var c = Content(); c.Gdd03Balance!.Values["industry.income"].Values = numbers;
            var ex = Assert.Throws<ContentException>(() => Gdd03IndustryRates.Read(c)); Assert.Contains("values.industry.income", ex.Message);
        }
        foreach (var mutation in new Action<Content>[] {
            c => c.Gdd03Balance!.Values.Remove("industry.income"),
            c => c.Gdd03Balance!.Values["industry.income"].Basis = "Deferred",
            c => c.Gdd03Balance!.Values["industry.income"].Expression = "unknown",
            c => c.Gdd03Balance = null }) {
            var c = Content(); mutation(c); Assert.Throws<ContentException>(() => Gdd03IndustryRates.Read(c));
        }
        foreach (var identity in new[] { SaveGame.LegacyRuleset, SaveGame.GddRuleset }) {
            var c = Content(identity); var s = new GameState(c); var before = s.Describe();
            Assert.Throws<InvalidOperationException>(() => new Gdd03AssignmentFixture(c, s, Slots));
            Assert.Throws<InvalidOperationException>(() => Gdd03IndustryRates.Read(c)); Assert.Equal(before, s.Describe());
        }
    }

    // R-CITY-002: larger synthetic capacity tests accumulation and frozen catalogue ownership.
    [Fact] public void MixedTierSubtotalUsesFrozenRatesAfterCatalogueMutation()
    {
        var c = Content();
        var s = State(c, Person(), Person(CivilianProfession.Craftsman, CivilianTier.Apprentice),
            Person(CivilianProfession.Craftsman, CivilianTier.Expert));
        var slots = Slots.Select(x => new Gdd03FixtureSlot(x.Id, x.Work,
            x.Id == Slots[0].Id ? 3 : x.Capacity, x.Label)).ToArray();
        var f = new Gdd03AssignmentFixture(c, s, slots);
        var rates = Gdd03IndustryRates.Read(c);
        foreach (var id in new[] { 0, 1, 2 }) f.Assign(id, Slots[0].Id);
        var before = s.Describe();
        Assert.Equal(6m, f.IndustryRate(Slots[0].Id)); // independently supplied 1 + 2 + 3 subtotal
        c.Gdd03Balance!.Values["industry.income"].Values = new List<decimal> { 10m, 20m, 30m }; // synthetic mutation
        Assert.Equal(2m, rates.For(CivilianTier.Apprentice));
        Assert.Equal(20m, Gdd03IndustryRates.Read(c).For(CivilianTier.Apprentice));
        Assert.Equal(6m, f.IndustryRate(Slots[0].Id));
        Assert.Equal(before, s.Describe());
    }

    // R-CITY-001: direct ledger mutation cannot silently invalidate the frozen fixture's capacity.
    [Fact] public void DirectLedgerOverfillIsRefusedWithoutFurtherStateMutation()
    {
        var c = Content(); var s = State(c, Person(), Person());
        var f = new Gdd03AssignmentFixture(c, s, Slots); f.Assign(0, Slots[0].Id);
        var person = s.PopulationLedger.Get(1);
        s.PopulationLedger.Replace(person.WithPlacement(person.Status, person.Location,
            new PopulationAssignment(PopulationAssignmentKind.Work, Slots[0].Id), person.Captor, person.Home));
        var before = s.Describe();
        Assert.Throws<ArgumentException>(() => f.IndustryRate(Slots[0].Id)); Assert.Equal(before, s.Describe());
        Assert.Throws<ArgumentException>(() => f.Assign(1, Slots[1].Id)); Assert.Equal(before, s.Describe());
        Assert.Throws<ArgumentException>(() => new Gdd03AssignmentFixture(c, s, Slots)); Assert.Equal(before, s.Describe());
    }
    // R-CITY-001: actual-region pool occupancy over existing ledger locations and slot kinds. Synthetic quantities.
    const string RegionA = "ramla_rehovot", RegionB = "central_plain";
    static Content RegionalContent()
    {
        var c = PlayerRegionsContentTests.Load("player_regions:\n" + PlayerRegionsContentTests.RamlaRehovot + PlayerRegionsContentTests.Central);
        c.Gdd03Balance!.Values["industry.income"] = new Gdd03DesignValue {
            Basis = "Agreed", Note = "synthetic", Values = new() { 1m, 2m, 3m } };
        return c;
    }
    static (GameState, Gdd03AssignmentFixture) Regional(int industry, int workshop, Gdd03FixtureSlot[] slots, int people)
    {
        var c = RegionalContent(); var ticks = c.RequirePopulationInteger("reception.turns");
        var at = new PopulationLocation(RegionA, -4, 24);
        var rows = Enumerable.Range(0, people).Select(i => new PopulationInventoryRow("synthetic", i,
            new PopulationUnit(0, PopulationStatus.FreeCivilian, ticks, at, PopulationAssignment.Unassigned,
                CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None)));
        var s = PlayerRegions.CreateFixture(c, new[] {
                new PlayerRegionPool(RegionA, 0, 0m, industry, workshop, Array.Empty<PlayerRegionFacility>()),
                new PlayerRegionPool(RegionB, 0, 0m, 0, 0, Array.Empty<PlayerRegionFacility>()) },
            rows, slots.Select(x => new PopulationTarget(x.Id, PopulationAssignmentKind.Work, RegionA)),
            Array.Empty<string>(), Array.Empty<string>());
        return (s, new Gdd03AssignmentFixture(c, s, slots));
    }
    static Gdd03FixtureSlot S(string id, Gdd03FixtureWork w) => new(id, w, 1, "generic");
    static void Refused(GameState s, Action a) { var b = s.Describe(); Assert.Throws<ArgumentException>(a); Assert.Equal(b, s.Describe()); }

    [Fact] public void IndustryAndWorkshopGroupsAreIndependentWithMoveOutAndNoOp()
    {
        var slots = new[] { S("i1", Gdd03FixtureWork.Industry), S("i2", Gdd03FixtureWork.Industry),
            S("w1", Gdd03FixtureWork.Workshop), S("w2", Gdd03FixtureWork.Workshop), S("farm", Gdd03FixtureWork.Farm) };
        var (s, f) = Regional(1, 1, slots, 3);
        f.Assign(0, "i1");
        Refused(s, () => f.Assign(1, "i2"));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(1, "w1"));   // other group unaffected
        Refused(s, () => f.Assign(0, "w2"));
        var same = s.Describe();
        Assert.Equal(Gdd03FixtureAssignmentResult.NoOp, f.Assign(0, "i1")); Assert.Equal(same, s.Describe());
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(0, "i2"));   // mover excluded from its own group
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(2, "farm")); // Farm not pooled
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(0, null));   // move-out frees the group
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(2, "i1"));
    }

    [Fact] public void ZeroRegionalCapacityRefusesPooledKindsOnly()
    {
        var slots = new[] { S("i1", Gdd03FixtureWork.Industry), S("w1", Gdd03FixtureWork.Workshop),
            S("farm", Gdd03FixtureWork.Farm), S("builder", Gdd03FixtureWork.Builder) };
        var (s, f) = Regional(0, 0, slots, 2);
        Refused(s, () => f.Assign(0, "i1")); Refused(s, () => f.Assign(0, "w1"));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(0, "farm"));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, f.Assign(1, "builder"));
    }

    [Fact] public void ExistingRegionalOverfillIsRefusedAndClonesStayIndependent()
    {
        var slots = new[] { S("i1", Gdd03FixtureWork.Industry), S("i2", Gdd03FixtureWork.Industry), S("farm", Gdd03FixtureWork.Farm) };
        var (s, f) = Regional(1, 0, slots, 2);
        f.Assign(0, "i1");
        var clone = s.Clone(); var cf = new Gdd03AssignmentFixture(RegionalContent(), clone, slots);
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, cf.Assign(0, null));
        Assert.Equal("i1", s.PopulationLedger.Get(0).Assignment.Target);
        // Direct ledger write bypasses the fixture; the region now holds two Industry occupants.
        s.PopulationLedger.ChangeAssignment(1, new PopulationAssignment(PopulationAssignmentKind.Work, "i2"));
        Refused(s, () => f.IndustryRate("i1"));
        Refused(s, () => f.Assign(1, "farm"));
        Refused(s, () => new Gdd03AssignmentFixture(RegionalContent(), s, slots));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, cf.Assign(1, "i2")); // clone unaffected
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void RegionalOccupancySeparatesRegionsAndUsesFacilityContributions(bool workshop)
    {
        var content = RegionalContent();
        var kind = workshop ? Gdd03FixtureWork.Workshop : Gdd03FixtureWork.Industry;
        var facilityKind = workshop ? PlayerRegionFacilityKind.Workshop : PlayerRegionFacilityKind.Industry;
        var slots = Enumerable.Range(0, 4).Select(i => S("synthetic-regional-boundary-" + i, kind)).ToArray();
        var locations = new[] { new PopulationLocation(RegionA, -4, 24), new PopulationLocation(RegionB, -4, 23) };
        var rows = Enumerable.Range(0, 4).Select(i => new PopulationInventoryRow("synthetic", i,
            new PopulationUnit(0, PopulationStatus.FreeCivilian, content.RequirePopulationInteger("reception.turns"), locations[i / 2],
                PopulationAssignment.Unassigned, CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None)));
        var state = PlayerRegions.CreateFixture(content, new[] {
            new PlayerRegionPool(RegionA, 2, 0m, 0, 0, new[] { new PlayerRegionFacility("synthetic-a-facility", "ramla", facilityKind, 1) }),
            new PlayerRegionPool(RegionB, 2, 0m, 0, 0, new[] { new PlayerRegionFacility("synthetic-b-facility", "rishon_lezion", facilityKind, 1) }) },
            rows, slots.Select((slot, i) => new PopulationTarget(slot.Id, PopulationAssignmentKind.Work, i < 2 ? RegionA : RegionB)),
            Array.Empty<string>(), Array.Empty<string>());
        var fixture = new Gdd03AssignmentFixture(content, state, slots);
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, fixture.Assign(0, slots[0].Id));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, fixture.Assign(2, slots[2].Id));
        var before = state.Describe();
        Assert.Equal(Gdd03FixtureAssignmentResult.NoOp, fixture.Assign(0, slots[0].Id));
        Assert.Equal(Gdd03FixtureAssignmentResult.NoOp, fixture.Assign(2, slots[2].Id));
        Refused(state, () => fixture.Assign(1, slots[1].Id)); Refused(state, () => fixture.Assign(3, slots[3].Id));
        Assert.Equal(workshop ? 0m : 1m, fixture.IndustryRate(slots[0].Id));
        Assert.Equal(before, state.Describe());
        Assert.Equal(1, PlayerRegions.Assigned(state, RegionA, PopulationAssignmentKind.Work));
        Assert.Equal(1, PlayerRegions.Assigned(state, RegionB, PopulationAssignmentKind.Work));
        state.PopulationLedger.Reconcile();
    }

}
