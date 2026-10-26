using Statecraft.Sim;
using System.Globalization;

/// <summary>#1689 foundation only: synthetic inventory; no playable campaign or replay claim.</summary>
public class PopulationLedgerTests
{
    internal static Content Content(string ruleset = SaveGame.Gdd03Ruleset) => new Content {
        Eras = new List<Era> { new Era { Systems = new EraSystems { Loop = EraSystems.Gdd, Ruleset = ruleset } } },
        Gdd03Balance = new Gdd03BalanceCatalogue { Ruleset = SaveGame.Gdd03Ruleset, Values = new() {
            ["population.people_per_unit"] = new Gdd03DesignValue { Basis = "Agreed", Values = new() { 10000 }, Note = "pinned catalogue" },
            ["reception.turns"] = new Gdd03DesignValue { Basis = "Agreed", Values = new() { 6 }, Note = "pinned reception.turns from the Agreed catalogue" }
        } }
    };
    internal static PopulationBindings Bindings(bool reverse = false) => new PopulationBindings(
        reverse ? new[] { new PopulationLocation("south", 1, 0, "port"), new PopulationLocation("north", 0, 0, "camp") }
                : new[] { new PopulationLocation("north", 0, 0, "camp"), new PopulationLocation("south", 1, 0, "port") },
        new[] { new PopulationTarget("field", PopulationAssignmentKind.Work, "north"),
                new PopulationTarget("course", PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget("teacher", PopulationAssignmentKind.Teaching, "north"),
                new PopulationTarget("service", PopulationAssignmentKind.ActiveService, "north"),
                new PopulationTarget("travel", PopulationAssignmentKind.Travel, "south"),
                new PopulationTarget("reception", PopulationAssignmentKind.Reception, "north") },
        new[] { "captor" }, new[] { "basic", "trained" });
    internal static PopulationUnit Person(int id = 0) => new PopulationUnit(id, PopulationStatus.FreeCivilian,
        1, new PopulationLocation("north", 0, 0, "camp"), PopulationAssignment.Unassigned,
        CivilianProfession.Farmer, CivilianTier.Trained, 7, "basic", 9,
        new PopulationEquipment("tools", true, "arms", true));
    internal static GameState State(params PopulationInventoryRow[] rows) =>
        PopulationLedger.CreateFixture(Content(), Bindings(), rows);
    static PopulationInventoryRow Row(string key = "start", int ordinal = 0, PopulationUnit? person = null) =>
        new PopulationInventoryRow(key, ordinal, person ?? Person());

    // R-POP-001, R-POP-002: ids change only on registered creation/loss; transitions preserve histories.
    [Fact] public void LifecycleKeepsIdentityAndHistoryAndReconcilesExactIds()
    {
        var s = State(Row()); var l = s.PopulationLedger;
        var p = l.Get(0); var home = p.Location!;
        foreach (var status in new[] { PopulationStatus.Refugee, PopulationStatus.CivilianInTransit,
            PopulationStatus.Trainee, PopulationStatus.ActiveSoldier, PopulationStatus.Captured, PopulationStatus.FreeCivilian })
        {
            var assignment = status switch {
                PopulationStatus.CivilianInTransit => new PopulationAssignment(PopulationAssignmentKind.Travel, "travel"),
                PopulationStatus.Trainee => new PopulationAssignment(PopulationAssignmentKind.Course, "course", 1),
                PopulationStatus.ActiveSoldier => new PopulationAssignment(PopulationAssignmentKind.ActiveService, "service"),
                _ => PopulationAssignment.Unassigned };
            var location = status == PopulationStatus.Captured ? null : status == PopulationStatus.CivilianInTransit
                ? new PopulationLocation("south", 1, 0, "port") : home;
            l.Transition(0, status, location, assignment, status == PopulationStatus.Captured ? "captor" : null, home);
            l.Reconcile(); Assert.Equal(new[] { 0 }, l.Units.Select(x => x.Id));
            Assert.Equal(l.Units.Count, Enum.GetValues<PopulationStatus>().Sum(l.Count));
            var now = l.Get(0);
            Assert.Equal(p.Profession, now.Profession); Assert.Equal(p.Tier, now.Tier);
            Assert.Equal(p.WorkXp, now.WorkXp); Assert.Equal(p.Qualification, now.Qualification);
            Assert.Equal(p.CombatXp, now.CombatXp); Assert.Equal(p.ReceptionTicks, now.ReceptionTicks);
            Assert.Same(p.Equipment, now.Equipment);
        }
        Assert.Equal(1, l.Count(PopulationStatus.FreeCivilian, "north"));
        l.RegisterCombatLoss(0, "fixture-loss"); Assert.Empty(l.Units);
        l.RegisterArrival("ship", 0, Person()); Assert.Equal(1, l.Get(1).Id);
        Assert.Equal(2, l.NextPopulationId); Assert.Single(l.Units); l.Reconcile();
        Assert.Equal(10000, s.TotalPopulation);
    }

    // R-POP-001, R-POP-002: refusal must leave the entire state unchanged, including allocator/provenance.
    [Fact] public void InvalidOperationsAreAtomicAndIdsNeverRecycle()
    {
        var s = State(Row()); var l = s.PopulationLedger;
        void Refuses(Action action) { var before = s.Describe(); Assert.ThrowsAny<ArgumentException>(action); Assert.Equal(before, s.Describe()); }
        Refuses(() => l.Move(99, new PopulationLocation("north", 0, 0, "camp")));
        Refuses(() => l.Move(0, new PopulationLocation("south", 0, 0, "camp")));
        Refuses(() => l.Transition(0, PopulationStatus.ActiveSoldier, l.Get(0).Location, PopulationAssignment.Unassigned));
        Refuses(() => l.Transition(0, PopulationStatus.FreeCivilian, l.Get(0).Location,
            new PopulationAssignment(PopulationAssignmentKind.Work, "missing")));
        Refuses(() => l.RegisterArrival("start", 0, Person()));
        Refuses(() => l.RegisterArrival("ship", 0, Person())); // duplicate issued equipment
        Refuses(() => l.RegisterCombatLoss(99, "loss"));
        Refuses(() => l.Replace(l.Get(0).WithReception(7)));
        Refuses(() => l.Replace(new PopulationUnit(0, PopulationStatus.FreeCivilian, 0, l.Get(0).Location,
            PopulationAssignment.Unassigned, CivilianProfession.None, CivilianTier.Expert, 0, "", 0, PopulationEquipment.None)));
        l.ChangeAssignment(0, new PopulationAssignment(PopulationAssignmentKind.Work, "field"));
        Refuses(() => l.ChangeAssignment(0, new PopulationAssignment(PopulationAssignmentKind.Work, "field")));
        var exhausted = PopulationLedger.CreateFixture(Content(), Bindings(), Array.Empty<PopulationInventoryRow>(), int.MaxValue);
        var hash = exhausted.Hash(); Assert.Throws<OverflowException>(() => exhausted.PopulationLedger.RegisterArrival("ship", 0, Person()));
        Assert.Equal(hash, exhausted.Hash());
    }

    // R-POP-003: canonical source ordering, clone isolation, exact culture-independent field serialization.
    [Fact] public void FixtureOrderCloneAndCultureDoNotChangeCanonicalState()
    {
        var emptyEquipment = new PopulationUnit(0, PopulationStatus.Refugee, 0,
            new PopulationLocation("north", 0, 0, "camp"), PopulationAssignment.Unassigned,
            CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None);
        var a = State(Row("z", 0, emptyEquipment), Row("a"));
        var b = PopulationLedger.CreateFixture(Content(), Bindings(true), new[] { Row("a"), Row("z", 0, emptyEquipment) });
        Assert.Equal(a.Hash(), b.Hash()); Assert.Equal(CivilianProfession.Farmer, a.PopulationLedger.Get(0).Profession);
        var before = a.Hash(); var clone = a.Clone();
        clone.PopulationLedger.RegisterCombatLoss(0, "loss"); Assert.Equal(before, a.Hash()); Assert.NotEqual(before, clone.Hash());
        var culture = CultureInfo.CurrentCulture;
        try { foreach (var name in new[] { "de-DE", "ar-SA", "" }) { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name); Assert.Equal(before, a.Hash()); } }
        finally { CultureInfo.CurrentCulture = culture; }
        var withHistory = State(Row()); withHistory.PopulationLedger.RegisterCombatLoss(0, "loss");
        Assert.NotEqual(State().Hash(), withHistory.Hash());
    }

    // R-POP-003: mutate every field independently, including every nested payload.
    [Fact] public void EveryRecordFieldReachesHash()
    {
        var p = Person();
        var variants = new[] {
            new PopulationUnit(0, PopulationStatus.Refugee, 1, p.Location, p.Assignment, p.Profession, p.Tier, 7, "basic", 9, p.Equipment),
            p.WithReception(2),
            new PopulationUnit(0, p.Status, 1, new PopulationLocation("south", 1, 0, "port"), p.Assignment, p.Profession, p.Tier, 7, "basic", 9, p.Equipment),
            new PopulationUnit(0, p.Status, 1, p.Location, p.Assignment, CivilianProfession.Pioneer, p.Tier, 7, "basic", 9, p.Equipment),
            new PopulationUnit(0, p.Status, 1, p.Location, p.Assignment, p.Profession, CivilianTier.Veteran, 7, "basic", 9, p.Equipment),
            new PopulationUnit(0, p.Status, 1, p.Location, p.Assignment, p.Profession, p.Tier, 8, "basic", 9, p.Equipment),
            new PopulationUnit(0, p.Status, 1, p.Location, p.Assignment, p.Profession, p.Tier, 7, "trained", 9, p.Equipment),
            new PopulationUnit(0, p.Status, 1, p.Location, p.Assignment, p.Profession, p.Tier, 7, "basic", 10, p.Equipment),
            new PopulationUnit(0, p.Status, 1, p.Location, new PopulationAssignment(PopulationAssignmentKind.Work,"field"), p.Profession, p.Tier, 7, "basic", 9, p.Equipment)
        };
        var original = State(Row()).Hash();
        foreach (var v in variants) Assert.NotEqual(original, State(Row(person: v)).Hash());
        foreach (var equipment in new[] { new PopulationEquipment("other",true,"arms",true), new PopulationEquipment("tools",false,"arms",true),
            new PopulationEquipment("tools",true,"other",true), new PopulationEquipment("tools",true,"arms",false) })
        {
            var v = new PopulationUnit(0,p.Status,1,p.Location,p.Assignment,p.Profession,p.Tier,7,"basic",9,equipment);
            Assert.NotEqual(original,State(Row(person:v)).Hash());
        }
        var a = State(Row()); var b = a.Clone();
        a.PopulationLedger.Transition(0,PopulationStatus.Captured,null,PopulationAssignment.Unassigned,"captor",p.Location);
        b.PopulationLedger.Transition(0,PopulationStatus.Captured,null,PopulationAssignment.Unassigned,"captor",new PopulationLocation("south",1,0,"port"));
        Assert.NotEqual(a.Hash(),b.Hash());
    }

    // R-POP-001, R-POP-003, R-LOOP-002, R-CITY-001, R-CITY-008: old identities neither gain state nor accept helpers.
    [Theory] [InlineData("legacy")] [InlineData("gdd-0.1")]
    public void OldIdentitiesKeepCanonicalTextAndSaveBytes(string identity)
    {
        var s = new GameState(Content(identity)); var before = s.Describe();
        Assert.False(s.UsesPopulationLedger); Assert.DoesNotContain("population-ledger", before);
        Assert.Throws<InvalidOperationException>(()=>s.PopulationLedger.RegisterArrival("ship",0,Person()));
        Assert.Equal(before,s.Describe()); Assert.Equal(before,s.Clone().Describe());
        var save = new SaveGame(new string('a',64),1,Mode.Historical,identity,new string('b',64));
        Assert.Equal($"statecraft-save 3\ncontent {new string('a',64)}\nseed 1\nmode Historical\nruleset {identity}\nend {new string('b',64)}\n",save.Write());
    }

    // R-POP-001, R-LOOP-005: no numeric fallback from the old common scale or Deferred start.army.
    [Fact] public void ScaleIsIdentityAwareAndInvalidCatalogueValuesAreRefused()
    {
        var c=Content(); Assert.True(c.UsesPopulationLedger); Assert.Equal(10000,c.PopulationPeoplePerUnit);
        foreach (var value in new[] { 0m, -1m, 1.5m, (decimal)int.MaxValue+1 }) {
            c.Gdd03Balance!.Values["population.people_per_unit"].Values[0]=value;
            Assert.Throws<ContentException>(()=> { _ = c.PopulationPeoplePerUnit; });
        }
        c=Content(); c.Gdd03Balance!.Values["population.people_per_unit"].Basis="Deferred";
        Assert.Throws<ContentException>(()=> { _ = c.PopulationPeoplePerUnit; });
        c.Gdd03Balance.Values.Remove("population.people_per_unit"); Assert.Throws<ContentException>(()=> { _ = c.PopulationPeoplePerUnit; });
        foreach(var identity in new[]{SaveGame.LegacyRuleset,SaveGame.GddRuleset}) {
            var old=Content(identity); old.Rules.Mvp=new MvpRules { PeoplePerUnit=1000 };
            old.Gdd03Balance=null; Assert.Equal(1000,old.PopulationPeoplePerUnit);
        }
    }

    // R-POP-002/003: course references and every payload field are validated and canonical.
    [Fact] public void CoursePayloadReferencesAndProvenanceAreCanonicalAndAtomic()
    {
        PopulationUnit Teacher() => new PopulationUnit(0, PopulationStatus.FreeCivilian, 1, Person().Location,
            new PopulationAssignment(PopulationAssignmentKind.Teaching,"course"), CivilianProfession.Pioneer,
            CivilianTier.Trained, 4,"basic",2,PopulationEquipment.None);
        PopulationUnit Pupil(int progress, int? teacher) => new PopulationUnit(0,PopulationStatus.Trainee,1,Person().Location,
            new PopulationAssignment(PopulationAssignmentKind.Course,"course",progress,teacher),CivilianProfession.None,
            CivilianTier.None,0,"",0,PopulationEquipment.None);
        GameState Course(int progress, int? teacher) => State(Row("a",0,Teacher()),Row("a",1,Teacher()),Row("z",0,Pupil(progress,teacher)));
        var a=Course(0,null); var b=Course(1,null); var c=Course(0,0); var d=Course(0,1);
        Assert.Equal(4,new HashSet<string>{a.Hash(),b.Hash(),c.Hash(),d.Hash()}.Count);
        var before=c.Describe();
        Assert.Throws<ArgumentException>(()=>c.PopulationLedger.RegisterCombatLoss(0,"loss"));
        Assert.Equal(before,c.Describe());
        Assert.Throws<ArgumentException>(()=>Course(4,null)); Assert.Throws<ArgumentException>(()=>Course(0,99));
        Assert.Throws<ArgumentException>(()=>Course(0,2));
        Assert.Throws<ArgumentException>(()=>State(Row("a"),Row("a")));
        Assert.Throws<ArgumentException>(()=>PopulationLedger.CreateFixture(Content(),new PopulationBindings(
            new[]{new PopulationLocation("north",0,0),new PopulationLocation("south",0,0)},
            Array.Empty<PopulationTarget>(),Array.Empty<string>(),Array.Empty<string>()),Array.Empty<PopulationInventoryRow>()));
        Assert.NotEqual(State(Row("start")).Hash(),State(Row("start|\n",1)).Hash());
        var lost=State(Row());lost.PopulationLedger.RegisterCombatLoss(0,"loss");var hash=lost.Hash();
        Assert.Throws<ArgumentException>(()=>lost.PopulationLedger.RegisterArrival("start",0,Person()));
        Assert.Equal(hash,lost.Hash());
        Assert.NotEqual(State().Hash(),PopulationLedger.CreateFixture(Content(),Bindings(),Array.Empty<PopulationInventoryRow>(),1).Hash());
        Assert.Contains("population-ledger",new GameState(Content()).Describe());
    }

    // R-POP-003: each nullable/reference component has an independent canonical representation.
    [Fact] public void LocationCaptorAndTargetTokensCannotAlias()
    {
        var p=Person();
        PopulationUnit Placed(PopulationLocation location) => new PopulationUnit(0,p.Status,1,location,p.Assignment,p.Profession,p.Tier,7,"basic",9,p.Equipment);
        var locations=new[]{new PopulationLocation("north"),new PopulationLocation("north",0,0),
            new PopulationLocation("north",1,0),new PopulationLocation("north",0,1),new PopulationLocation("north",0,0,"camp"),
            new PopulationLocation("north",0,0,"port|\n")};
        var bindings=new PopulationBindings(locations,new[]{new PopulationTarget("field",PopulationAssignmentKind.Work,"north"),
            new PopulationTarget("field|\n",PopulationAssignmentKind.Work,"north")},new[]{"captor","captor|\n"},new[]{"basic"});
        GameState S(PopulationUnit unit) => PopulationLedger.CreateFixture(Content(),bindings,new[]{Row(person:unit)});
        Assert.Equal(locations.Length,locations.Select(l=>S(Placed(l)).Hash()).Distinct().Count());
        var a=S(p);var b=a.Clone();
        a.PopulationLedger.Transition(0,PopulationStatus.Captured,null,PopulationAssignment.Unassigned,"captor",p.Location);
        b.PopulationLedger.Transition(0,PopulationStatus.Captured,null,PopulationAssignment.Unassigned,"captor|\n",p.Location);
        Assert.NotEqual(a.Hash(),b.Hash());
        a=S(p);b=a.Clone();a.PopulationLedger.ChangeAssignment(0,new PopulationAssignment(PopulationAssignmentKind.Work,"field"));
        b.PopulationLedger.ChangeAssignment(0,new PopulationAssignment(PopulationAssignmentKind.Work,"field|\n"));
        Assert.NotEqual(a.Hash(),b.Hash());
        Assert.Throws<ArgumentException>(()=>S(new PopulationUnit(0,p.Status,1,p.Location,p.Assignment,p.Profession,p.Tier,-1,"basic",9,p.Equipment)));
        Assert.Throws<ArgumentException>(()=>S(new PopulationUnit(0,p.Status,1,p.Location,p.Assignment,p.Profession,p.Tier,7,"unknown",9,p.Equipment)));
    }

    // R-POP-002: fixture-only initialization must never enter old turn/command producers.
    [Fact] public void FixturesRefuseAggregateSimulationEntryPoints()
    {
        var s=State(Row());var c=Content();var hash=s.Hash();
        Assert.Throws<NotSupportedException>(()=>Simulation.Step(c,s,Array.Empty<Command>()));
        Assert.Throws<NotSupportedException>(()=>Simulation.Preview(c,s,Array.Empty<Command>()));
        Assert.Throws<NotSupportedException>(()=>Simulation.DelegatedTo(c,s,Array.Empty<Command>(),"missing"));
        Assert.Equal(hash,s.Hash());
    }
}
