using Statecraft.Sim;
using System.Globalization;

// #1696 slice 1, R-CITY-004 / R-POP-002 / R-POP-003.
// All ids, locations, counts, histories and operating flags below are test-only synthetic values.
public class Gdd03ReceptionFixtureTests
{
    static readonly PopulationLocation Site = new("synthetic-context", facilityOrPort: "synthetic-site");
    static readonly PopulationLocation Elsewhere = new("synthetic-context", facilityOrPort: "synthetic-other-site");
    static Content Catalogue()
    {
        var c = PopulationLedgerTests.Content();
        // content/gdd03-balance.yaml:214-221 at the pinned base: Agreed 3 beds, 6 turns.
        c.Gdd03Balance!.Values["reception.capacity"] = new Gdd03DesignValue {
            Basis = "Agreed", Values = new() { 3 }, Note = "Agreed catalogue reception.capacity" };
        return c;
    }
    static SyntheticReceptionCamp Camp(string id = "synthetic-a", bool operating = true) => new(id, Site, operating);
    static PopulationUnit Person(int ticks = 0, string? bed = null, PopulationLocation? location = null,
        PopulationStatus status = PopulationStatus.Refugee) => new(0, status, ticks, location ?? Site,
        bed == null ? PopulationAssignment.Unassigned : new(PopulationAssignmentKind.Reception, bed),
        CivilianProfession.Farmer, CivilianTier.Trained, 7, "basic", 9, PopulationEquipment.None);
    static GameState State(params PopulationUnit[] people)
    {
        var bindings = new PopulationBindings(new[] { Site, Elsewhere },
            new[] { new PopulationTarget("synthetic-a", PopulationAssignmentKind.Reception, Site.Region),
                new PopulationTarget("synthetic-b", PopulationAssignmentKind.Reception, Site.Region),
                new PopulationTarget("synthetic-work", PopulationAssignmentKind.Work, Site.Region),
                new PopulationTarget("synthetic-course", PopulationAssignmentKind.Course, Site.Region, 6) },
            Array.Empty<string>(), new[] { "basic" });
        return PopulationLedger.CreateFixture(Catalogue(), bindings, people.Select((p, i) =>
            new PopulationInventoryRow("synthetic-start", i, p)));
    }

    // R-POP-002: the hashed ledger duration and the caller catalogue must agree.
    [Theory] [InlineData(4)] [InlineData(8)]
    public void DifferentAgreedDurationRefusesBeforeProgressOrEligibility(int turns)
    {
        var state = State(Person(3, "synthetic-a"));
        var before = state.Hash();
        var content = Catalogue();
        content.Gdd03Balance!.Values["reception.turns"].Values = new() { turns };
        Assert.Throws<ContentException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(
            content, state, new[] { Camp() }));
        Assert.Equal(before, state.Hash());
        var completed = State(Person(4, status: PopulationStatus.FreeCivilian));
        Assert.Throws<ContentException>(() => Gdd03ReceptionFixture.CanStartCivilianActivity(
            content, completed.PopulationLedger, completed.PopulationLedger.Get(0)));
        Assert.False(Gdd03ReceptionFixture.CanStartCivilianActivity(
            Catalogue(), completed.PopulationLedger, completed.PopulationLedger.Get(0)));
    }

    // R-POP-002: eligibility reads the current record of this ledger only.
    [Theory] [InlineData(false)] [InlineData(true)]
    public void ForeignOrStalePersonCannotBorrowAnotherLedgerEligibility(bool stale)
    {
        var state = State(Person(6, status: PopulationStatus.FreeCivilian));
        var person = state.PopulationLedger.Get(0);
        if (stale)
            state.PopulationLedger.ChangeAssignment(0, new(PopulationAssignmentKind.Work, "synthetic-work"));
        else
            state = State(Person());
        var before = state.Hash();
        Assert.Throws<ArgumentException>(() => Gdd03ReceptionFixture.CanStartCivilianActivity(
            Catalogue(), state.PopulationLedger, person));
        Assert.Equal(before, state.Hash());
    }

    [Fact] public void SixthWholeBedTurnCompletesSamePersonWithoutCompletionTurnWork()
    {
        var refugee = Person(bed: "synthetic-a");
        refugee = new PopulationUnit(0, refugee.Status, 0, refugee.Location, refugee.Assignment,
            refugee.Profession, refugee.Tier, refugee.WorkXp, refugee.Qualification, refugee.CombatXp,
            new PopulationEquipment("synthetic-tools", true, "synthetic-arms", true), home: Site);
        var s = State(refugee); var opening = s; var original = s.Hash();
        var p = s.PopulationLedger.Get(0);
        for (int turn = 1; turn <= 6; turn++)
        {
            var result = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp() });
            Assert.Empty(result.WorkEligibleAtOpening);
            s = result.State;
            Assert.Equal(turn, s.PopulationLedger.Get(0).ReceptionTicks);
            Assert.Equal(turn == 6 ? PopulationStatus.FreeCivilian : PopulationStatus.Refugee,
                s.PopulationLedger.Get(0).Status);
            s.PopulationLedger.Reconcile();
        }
        var done = s.PopulationLedger.Get(0);
        Assert.Equal(p.Id, done.Id); Assert.Equal(p.Profession, done.Profession); Assert.Equal(p.Tier, done.Tier);
        Assert.Equal(p.WorkXp, done.WorkXp); Assert.Equal(p.CombatXp, done.CombatXp);
        Assert.Equal(p.Qualification, done.Qualification); Assert.Same(p.Equipment, done.Equipment);
        Assert.Same(p.Location, done.Location); Assert.Same(p.Home, done.Home); Assert.Equal(PopulationAssignmentKind.Unassigned, done.Assignment.Kind);
        Assert.Equal(new[] { 0 }, Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp() }).WorkEligibleAtOpening);
        Assert.Equal(original, opening.Hash());
        Assert.Equal(opening.Turn, s.Turn); Assert.Equal(opening.RngState, s.RngState);
        Assert.Equal(opening.Treasury, s.Treasury); Assert.Equal(opening.Debt, s.Debt);
    }

    [Fact] public void ThreeDedicatedBedsAndStableUnitCampOrderGiveNoAdmissionTurnCredit()
    {
        var s = State(Enumerable.Range(0, 7).Select(_ => Person()).ToArray()); var before = s.Hash();
        var a = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp("synthetic-b"), Camp() });
        var b = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s.Clone(), new[] { Camp(), Camp("synthetic-b") });
        Assert.Equal(a.State.Hash(), b.State.Hash()); Assert.Equal(before, s.Hash());
        Assert.All(a.State.PopulationLedger.Units, p => Assert.Equal(0, p.ReceptionTicks));
        Assert.All(a.State.PopulationLedger.Units.Take(3), p => Assert.Equal("synthetic-a", p.Assignment.Target));
        Assert.All(a.State.PopulationLedger.Units.Skip(3).Take(3), p => Assert.Equal("synthetic-b", p.Assignment.Target));
        Assert.Equal(PopulationAssignmentKind.Unassigned, a.State.PopulationLedger.Get(6).Assignment.Kind);
        var next = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), a.State, new[] { Camp(), Camp("synthetic-b") });
        Assert.All(next.State.PopulationLedger.Units.Take(6), p => Assert.Equal(1, p.ReceptionTicks));
        Assert.Equal(0, next.State.PopulationLedger.Get(6).ReceptionTicks);
    }

    [Fact] public void RetainedBedWinsAndReleasedBedDoesNotGiveReplacementAnotherTurn()
    {
        var s = State(Person(), Person(5, "synthetic-a"), Person(2, "synthetic-a"), Person(2, "synthetic-a"), Person());
        var result = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp() });
        Assert.Equal(6, result.State.PopulationLedger.Get(1).ReceptionTicks);
        Assert.Equal(PopulationStatus.FreeCivilian, result.State.PopulationLedger.Get(1).Status);
        Assert.Equal("synthetic-a", result.State.PopulationLedger.Get(0).Assignment.Target);
        Assert.Equal(0, result.State.PopulationLedger.Get(0).ReceptionTicks);
        Assert.Equal(3, result.State.PopulationLedger.Get(2).ReceptionTicks);
        Assert.Equal(3, result.State.PopulationLedger.Get(3).ReceptionTicks);
        Assert.Null(result.State.PopulationLedger.Get(4).Assignment.Target);
    }

    [Fact] public void OnlyExplicitColocationAndOperatingBedsAllowAdmission()
    {
        var s = State(Person(location: Elsewhere), Person());
        var unavailable = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp(operating: false) });
        Assert.All(unavailable.State.PopulationLedger.Units, p => Assert.Null(p.Assignment.Target));
        var available = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp() });
        Assert.Null(available.State.PopulationLedger.Get(0).Assignment.Target);
        Assert.Equal("synthetic-a", available.State.PopulationLedger.Get(1).Assignment.Target);
        var none = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, Array.Empty<SyntheticReceptionCamp>());
        Assert.Equal(s.Hash(), none.State.Hash());
    }

    [Fact] public void InvalidRetainedReservationsRefuseAtomicallyWithoutChoosingInterruptionPolicy()
    {
        var cases = new[] {
            (State(Person(bed: "synthetic-a")), new[] { Camp(operating: false) }),
            (State(Person(bed: "synthetic-a", location: Elsewhere)), new[] { Camp() }),
            (State(Person(bed: "synthetic-a")), Array.Empty<SyntheticReceptionCamp>()),
            (State(Enumerable.Range(0, 4).Select(_ => Person(bed: "synthetic-a")).ToArray()), new[] { Camp() }),
            (State(Person()), new[] { Camp(), Camp() }) };
        foreach (var (s, camps) in cases)
        {
            var before = s.Hash();
            Assert.ThrowsAny<ArgumentException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, camps));
            Assert.Equal(before, s.Hash());
        }
        var unknown = State(Person()); var hash = unknown.Hash();
        Assert.Throws<ArgumentException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), unknown,
            new[] { Camp("synthetic-unknown") }));
        Assert.Equal(hash, unknown.Hash());
    }

    [Fact] public void PredicateRequiresCompletedFreeUnassignedCivilianForEveryFutureWorkConsumer()
    {
        foreach (var ticks in new[] { 0, 5 })
        {
            Assert.False(Gdd03ReceptionFixture.CanStartCivilianActivity(Catalogue(), State().PopulationLedger, Person(ticks)));
            Assert.False(Gdd03ReceptionFixture.CanStartCivilianActivity(Catalogue(), State().PopulationLedger, Person(ticks, status: PopulationStatus.FreeCivilian)));
        }
        Assert.True(Gdd03ReceptionFixture.CanStartCivilianActivity(Catalogue(), State().PopulationLedger, Person(6, status: PopulationStatus.FreeCivilian)));
        Assert.False(Gdd03ReceptionFixture.CanStartCivilianActivity(Catalogue(), State().PopulationLedger, Person(6)));
        var s = State(Person(6, status: PopulationStatus.FreeCivilian));
        s.PopulationLedger.ChangeAssignment(0, new(PopulationAssignmentKind.Work, "synthetic-work"));
        Assert.False(Gdd03ReceptionFixture.CanStartCivilianActivity(Catalogue(), State().PopulationLedger, s.PopulationLedger.Get(0)));
        // This tests the proposed shared predicate; job/farm/build/course entry points are deliberately unwired.
    }

    [Fact] public void CourseFixturesRefuseRatherThanPretendToOwnSharedReservations()
    {
        var s = State(Person());
        s.PopulationLedger.Transition(0, PopulationStatus.Trainee, Site,
            new(PopulationAssignmentKind.Course, "synthetic-course"));
        var before = s.Hash();
        Assert.Throws<NotSupportedException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp() }));
        Assert.Equal(before, s.Hash());
    }

    [Fact] public void ExactIdentityInitializedLedgerAndAgreedIntegralCatalogueAreRequired()
    {
        var s = State(Person()); var before = s.Hash();
        foreach (var identity in new[] { SaveGame.LegacyRuleset, SaveGame.GddRuleset })
            Assert.Throws<InvalidOperationException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(
                PopulationLedgerTests.Content(identity), s, new[] { Camp() }));
        Assert.Throws<InvalidOperationException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(
            Catalogue(), new GameState(Catalogue()), new[] { Camp() }));
        foreach (var key in new[] { "reception.capacity", "reception.turns" })
        {
            var missing = Catalogue(); missing.Gdd03Balance!.Values.Remove(key);
            Assert.Throws<ContentException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(missing, s, new[] { Camp() }));
            foreach (var value in new[] { 0m, -1m, 1.5m, 2147483648m }) // test-only malformed inputs
            {
                var c = Catalogue(); c.Gdd03Balance!.Values[key].Values = new() { value };
                Assert.Throws<ContentException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(c, s, new[] { Camp() }));
            }
            var deferred = Catalogue(); var row = deferred.Gdd03Balance!.Values[key];
            row.Basis = "Deferred"; row.Values.Clear(); row.Expression = "Deferred";
            Assert.Throws<ContentException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(deferred, s, new[] { Camp() }));
        }
        Assert.Equal(before, s.Hash());
    }

    [Theory] [InlineData("he-IL")] [InlineData("en-US")]
    public void SyntheticProgressHasCultureIndependentCloneAndCanonicalState(string cultureName)
    {
        var s = State(Person(5, "synthetic-a"), Person());
        var expected = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s, new[] { Camp() }).State.Hash();
        var old = CultureInfo.CurrentCulture; var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.CurrentCulture;
            var result = Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), s.Clone(), new[] { Camp() });
            Assert.Equal(expected, result.State.Hash()); result.State.PopulationLedger.Reconcile();
            Assert.Equal(5, s.PopulationLedger.Get(0).ReceptionTicks);
        }
        finally { CultureInfo.CurrentCulture = old; CultureInfo.CurrentUICulture = oldUi; }
    }

    // R-CITY-004: representable retained claims still require an incomplete refugee.
    [Theory]
    [InlineData(PopulationStatus.Refugee, 6)]
    [InlineData(PopulationStatus.FreeCivilian, 0)]
    public void CompletedOrFreeCivilianRetainedClaimsRefuseWithoutMutation(PopulationStatus status, int ticks)
    {
        var state = State(Person(ticks, "synthetic-a", status: status));
        var before = state.Describe();
        Assert.Throws<ArgumentException>(() => Gdd03ReceptionFixture.ResolveWholeTurn(Catalogue(), state, new[] { Camp() }));
        Assert.Equal(before, state.Describe());
    }
}
