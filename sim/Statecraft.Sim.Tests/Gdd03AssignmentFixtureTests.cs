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
        Assert.Equal(Gdd03FixtureAssignmentResult.DeferredSameSlot, f.Assign(0, Slots[0].Id)); Assert.Equal(same, s.Describe());
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
                Assert.Equal(Gdd03FixtureAssignmentResult.DeferredSameSlot, f.Assign(id, target)); Assert.Equal(before, s.Describe());
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
}
