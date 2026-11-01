using Statecraft.Sim;
using System.Globalization;

// #1699 adapter/spec only. All inventory sizes, ids and XP below are test-only synthetic.
public class Gdd03MilitaryCatalogueTests
{
    static Gdd03BalanceCatalogue Catalogue() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!;
    static readonly string[] Keys = {
        "camp_capacity", "basic_turns", "issue_arms", "draft_money", "draft_wait",
        "demobilise_money", "demobilise_wait", "demobilise_return", "repeat_issue", "training_money", "active_money"
    };

    // R-TRAIN-001, R-TRAIN-002, R-ARMS-003: read settled data, do not activate conversion.
    [Fact] public void ReadsEverySettledNonCombatTermAsAnImmutableSnapshot()
    {
        var c = Catalogue(); var terms = Gdd03MilitaryCatalogue.Read(c);
        // content/gdd03-balance.yaml:307-354; #1668 balance.md:129-137.
        // camp_capacity: #1668's 5 places at 10,000 people a unit is 50 units at 1,000 a unit (owner, 1 November 2026:
        // "Main's base x 0.3's tiers (Recommended)"; #2112).
        Assert.Equal(50, terms.CampCapacity); Assert.Equal(3, terms.BasicTurns);
        Assert.Equal(1, terms.IssueArms); Assert.Equal(0, terms.RepeatIssueArms);
        Assert.Equal(0m, terms.DraftMoney); Assert.Equal(0, terms.DraftWait);
        Assert.Equal(0m, terms.DemobiliseMoney); Assert.Equal(0, terms.DemobiliseWait);
        Assert.Equal(1, terms.DemobiliseReturnArms); Assert.Equal(0m, terms.TrainingMoney);
        Assert.Equal(1m, terms.ActiveMoney);
        c.Values["military.issue_arms"].Values[0] = 99; // synthetic mutation after read
        Assert.Equal(1, terms.IssueArms);
        Assert.Equal("Deferred", c.Values["start.army"].Basis);
        Assert.Equal("A", c.Values["start.army"].Expression);
    }

    [Fact] public void EveryRequiredTermRefusesMissingDeferredAndMalformedDataWithoutFallback()
    {
        foreach (var suffix in Keys)
        {
            string key = "military." + suffix;
            void Reject(Action<Gdd03BalanceCatalogue> change)
            {
                var c = Catalogue(); change(c);
                var ex = Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.Read(c));
                Assert.Equal(Gdd03BalanceCatalogue.FileName, ex.File);
            }
            Reject(c => c.Values.Remove(key));
            Reject(c => c.Values[key] = null!);
            Reject(c => c.Values[key].Basis = "Default");
            Reject(c => c.Values[key] = new Gdd03DesignValue { Basis = "Deferred", Expression = "unresolved", Note = "synthetic deferred test" });
            Reject(c => c.Values[key].Values = null!);
            Reject(c => c.Values[key].Values.Clear());
            Reject(c => c.Values[key].Values.Add(0));
            Reject(c => c.Values[key].Values[0] = -1);
            Reject(c => c.Values[key].Expression = "unresolved");
            Reject(c => c.Values[key].Parts = null!);
            Reject(c => c.Values[key].Parts["extra"] = new Gdd03DesignValue());
            Reject(c => c.Values[key].Note = "");
        }
        foreach (var suffix in new[] { "camp_capacity", "basic_turns", "issue_arms", "draft_wait", "demobilise_wait", "demobilise_return", "repeat_issue" })
        {
            var c = Catalogue(); c.Values["military." + suffix].Values[0] = 0.5m;
            Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.Read(c));
            c.Values["military." + suffix].Values[0] = (decimal)int.MaxValue + 1;
            Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.Read(c));
        }
        Assert.Throws<ArgumentNullException>(() => Gdd03MilitaryCatalogue.Read(null!));
        var wrong = Catalogue(); wrong.Ruleset = SaveGame.GddRuleset;
        Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.Read(wrong));
        wrong = Catalogue(); wrong.Values = null!;
        Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.Read(wrong));
    }

    // R-TRAIN-002, R-ARMS-003: hypothetical equipment transfers only, no camp/course lifecycle.
    [Theory][InlineData("he-IL")][InlineData("en-US")]
    public void SyntheticClosedWorldConservesAfterEveryAcceptedOrRefusedStep(string locale)
    {
        var old = CultureInfo.CurrentCulture; var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
            var terms = Gdd03MilitaryCatalogue.Read(Catalogue());
            var initial = new EquipmentSpec(terms, 1, new[] { Person(10, false), Person(20, true) });
            void Explore(EquipmentSpec state, int depth)
            {
                if (depth == 0) return;
                foreach (var id in new[] { 10, 20, 404 }) // missing id is synthetic
                foreach (var action in new[] { "issue", "cancel", "return" })
                foreach (var eligible in new[] { true, false })
                {
                    var before = state.Snapshot();
                    var next = state.Step(id, action, eligible, out bool accepted);
                    Assert.Equal(initial.Total, next.Total);
                    Assert.Equal(initial.People.Select(p => p.Id), next.People.Select(p => p.Id));
                    for (int i = 0; i < next.People.Length; i++)
                        Assert.Equal(initial.People[i].Canonical(), CopyEquipment(next.People[i], initial.People[i].Equipment).Canonical());
                    Assert.Equal(before, state.Snapshot()); // immutable input, including accepted operations
                    if (!accepted) Assert.Equal(before, next.Snapshot());
                    Explore(next, depth - 1);
                }
            }
            Explore(initial, 3); // synthetic bounded enumeration, no arrivals/losses/imports/output
            var issued = initial.Step(10, "issue", true, out _);
            var cancelled = issued.Step(10, "cancel", true, out _);
            Assert.Equal(issued.Snapshot(), cancelled.Snapshot());
            var repeated = cancelled.Step(10, "issue", true, out _);
            Assert.Equal(issued.Snapshot(), repeated.Snapshot());
            var returned = repeated.Step(10, "return", true, out bool ok); Assert.True(ok);
            var twice = returned.Step(10, "return", true, out ok); Assert.False(ok);
            Assert.Equal(returned.Snapshot(), twice.Snapshot());
            var empty = new EquipmentSpec(terms, 0, initial.People);
            Assert.Same(empty, empty.Step(10, "issue", true, out ok)); Assert.False(ok);
            Assert.Same(empty, empty.Step(20, "issue", true, out ok)); Assert.True(ok);
            var full = new EquipmentSpec(terms, int.MaxValue, initial.People);
            Assert.Same(full, full.Step(20, "return", true, out ok)); Assert.False(ok);
            Assert.Equal((long)int.MaxValue + 1, full.Total); // wide invariant, no saturation
        }
        finally { CultureInfo.CurrentCulture = old; CultureInfo.CurrentUICulture = oldUi; }
    }

    static PopulationUnit Person(int id, bool armed) => new PopulationUnit(id, PopulationStatus.FreeCivilian,
        2, null, PopulationAssignment.Unassigned, CivilianProfession.Craftsman, CivilianTier.Apprentice,
        7, "synthetic-qualified", 4, new PopulationEquipment("synthetic-tools-" + id.ToString(CultureInfo.InvariantCulture), true,
            armed ? "synthetic-arms-" + id.ToString(CultureInfo.InvariantCulture) : null, armed));
    static PopulationUnit CopyEquipment(PopulationUnit p, PopulationEquipment e) => new PopulationUnit(p.Id, p.Status,
        p.ReceptionTicks, p.Location, p.Assignment, p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp, e, p.Captor, p.Home);

    // Test-only oracle. Eligibility is an explicit synthetic receipt, never a camp/access policy.
    // This has no GameState, ledger mutation, command, qualification or reception-completion capability.
    sealed class EquipmentSpec
    {
        readonly Gdd03MilitaryCatalogue terms;
        internal int Spare { get; }
        internal PopulationUnit[] People { get; }
        internal long Total => (long)Spare + People.LongCount(p => p.Equipment.MilitaryId != null);
        internal EquipmentSpec(Gdd03MilitaryCatalogue terms, int spare, PopulationUnit[] people)
        { this.terms = terms; Spare = spare; People = (PopulationUnit[])people.Clone(); }
        internal string Snapshot() => Spare.ToString(CultureInfo.InvariantCulture) + ";" + string.Join(";", People.Select(p => p.Canonical()));
        internal EquipmentSpec Step(int id, string action, bool eligible, out bool accepted)
        {
            accepted = false;
            int index = Array.FindIndex(People, p => p.Id == id);
            if (!eligible || index < 0) return this;
            var p = People[index]; var e = p.Equipment;
            if (action == "cancel") { accepted = true; return this; }
            if (action == "issue" && e.MilitaryId != null)
            { Assert.Equal(0, terms.RepeatIssueArms); accepted = true; return this; }
            int spare; string? item;
            if (action == "issue")
            {
                if (Spare < terms.IssueArms) return this;
                spare = checked(Spare - terms.IssueArms);
                item = "synthetic-arms-" + id.ToString(CultureInfo.InvariantCulture);
            }
            else if (action == "return")
            {
                if (e.MilitaryId == null || Spare > int.MaxValue - terms.DemobiliseReturnArms) return this;
                spare = checked(Spare + terms.DemobiliseReturnArms); item = null;
            }
            else return this;
            var people = (PopulationUnit[])People.Clone();
            people[index] = CopyEquipment(p, new PopulationEquipment(e.ToolsId, e.ToolsPaid, item, e.MilitaryPaid));
            accepted = true; return new EquipmentSpec(terms, spare, people);
        }
    }

    // R-TRAIN-002: refusal must be explicit, not merely an accepted no-op with unchanged stock.
    [Fact] public void MissingIdsAndIneligibleRequestsAreExplicitRefusals()
    {
        var state = new EquipmentSpec(Gdd03MilitaryCatalogue.Read(Catalogue()), 1,
            new[] { Person(10, false), Person(20, true) });
        foreach (var action in new[] { "issue", "cancel", "return" })
        foreach (var request in new[] { (Id: 404, Eligible: true), (Id: 10, Eligible: false), (Id: 20, Eligible: false) })
        {
            var before = state.Snapshot();
            var result = state.Step(request.Id, action, request.Eligible, out var accepted);
            Assert.False(accepted); Assert.Equal(before, result.Snapshot()); Assert.Equal(before, state.Snapshot());
        }
    }

    // R-TRAIN-001, R-ARMS-003: the four positive-integer terms exclude zero; sourced zero waits still read normally.
    [Fact] public void PositiveIntegerTermsRejectZero()
    {
        foreach (var suffix in new[] { "camp_capacity", "basic_turns", "issue_arms", "demobilise_return" })
        {
            var catalogue = Catalogue(); catalogue.Values["military." + suffix].Values[0] = 0;
            var error = Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.Read(catalogue));
            Assert.Equal("values.military." + suffix, error.Field);
        }
    }

    // R-TRAIN-001: the shared capacity reader validates only its own catalogue key.
    [Fact] public void CampCapacityReadNeedsNoUnrelatedMilitaryRows()
    {
        var catalogue = Catalogue(); var capacity = catalogue.Values["military.camp_capacity"];
        catalogue.Values.Clear(); catalogue.Values.Add("military.camp_capacity", capacity);
        Assert.Equal(50, Gdd03MilitaryCatalogue.ReadCampCapacity(catalogue)); // #2112: 50 units at 1,000 people a unit
        Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.Read(catalogue));
        capacity.Values[0] = 0;
        Assert.Throws<ContentException>(() => Gdd03MilitaryCatalogue.ReadCampCapacity(catalogue));
    }
}
