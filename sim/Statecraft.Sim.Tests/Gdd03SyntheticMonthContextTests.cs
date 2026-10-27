using Statecraft.Sim;
using System.Globalization;

// Synthetic #1716 contract tests. All ids, counts, progress, capacities and amounts below are
// test-only synthetic values, not starting-state or balance data.
public class Gdd03SyntheticMonthContextTests
{
    static PopulationUnit Person(int id, PopulationAssignmentKind kind, string? target = null) =>
        new PopulationUnit(id, kind == PopulationAssignmentKind.Course ? PopulationStatus.Trainee
            : kind == PopulationAssignmentKind.Reception ? PopulationStatus.Refugee : PopulationStatus.FreeCivilian, 0, null,
            new PopulationAssignment(kind, target), CivilianProfession.None, CivilianTier.None,
            0, "", 0, PopulationEquipment.None);

    static Gdd03SyntheticFacilitySnapshot Facility(string id = "synthetic-work", bool available = true) =>
        new Gdd03SyntheticFacilitySnapshot(id, available, 2);

    static Gdd03SyntheticMonthContext Context(IEnumerable<PopulationUnit>? people = null,
        IEnumerable<Gdd03SyntheticFacilitySnapshot>? facilities = null,
        IEnumerable<int>? residents = null, IEnumerable<Gdd03SyntheticPendingEntry>? entries = null,
        string identity = SaveGame.Gdd03Ruleset, int month = 0) =>
        Gdd03SyntheticMonthContext.CreateSynthetic(identity, month,
            people ?? Array.Empty<PopulationUnit>(), facilities ?? Array.Empty<Gdd03SyntheticFacilitySnapshot>(),
            residents ?? Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(),
            entries ?? Array.Empty<Gdd03SyntheticPendingEntry>());

    // RED-FIRST 1: R-LOOP-001/002. No old-identity contract and no implicit identity fallback.
    [Theory]
    [InlineData("legacy")]
    [InlineData("gdd-0.1")]
    [InlineData("GDD-0.3")]
    [InlineData("")]
    public void RejectsEveryIdentityExceptExactGdd03(string identity) =>
        Assert.Throws<ArgumentException>(() => Context(identity: identity));

    // RED-FIRST 2: R-LOOP-001/002; GDD loop-switch:17,24-28.
    [Fact] public void RejectsDuplicatePeopleAndUnresolvedAssignmentsOrResidents()
    {
        var p = Person(7, PopulationAssignmentKind.Work, "synthetic-work");
        Assert.Throws<ArgumentException>(() => Context(new[] { p, p }, new[] { Facility() }));
        Assert.Throws<ArgumentException>(() => Context(new[] { p }));
        Assert.Throws<ArgumentException>(() => Context(residents: new[] { 7 }));
        Assert.Throws<ArgumentException>(() => Context(new[] { p }, new[] { Facility() }, new[] { 7, 7 }));
        Assert.Throws<ArgumentException>(() => Context(facilities: new[] { Facility(), Facility() }));
        Assert.Throws<ArgumentNullException>(() => Gdd03SyntheticMonthContext.CreateSynthetic(
            SaveGame.Gdd03Ruleset, 0, null!, Array.Empty<Gdd03SyntheticFacilitySnapshot>(),
            Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>()));
        Assert.Throws<ArgumentException>(() => Context(new PopulationUnit[] { null! }));
        Assert.Throws<ArgumentException>(() => Context(month: -1));
        Assert.Throws<ArgumentException>(() => new Gdd03SyntheticFacilitySnapshot(" ", true, 2));
        Assert.Throws<ArgumentException>(() => new Gdd03SyntheticFacilitySnapshot("synthetic-work", true, -1));
        Assert.Throws<ArgumentException>(() => new Gdd03SyntheticHousingSnapshot("synthetic-house", -0.1m));
        Assert.Throws<ArgumentException>(() => new Gdd03SyntheticHousingSnapshot("synthetic-house", 1.1m));
        Assert.Throws<ArgumentException>(() => new Gdd03SyntheticPendingEntry("", "synthetic-unit", 2m));
        Assert.Throws<ArgumentException>(() => Context(entries: new[] {
            new Gdd03SyntheticPendingEntry("synthetic-key", "synthetic-unit", 2m),
            new Gdd03SyntheticPendingEntry("synthetic-key", "synthetic-other-unit", -3m) }));
    }

    // RED-FIRST: GDD loop-switch:17,24. Structural references do not replace owner validation.
    [Fact] public void RejectsIncompatibleStatusAndUnresolvedTeacherAndDuplicateHousing()
    {
        PopulationUnit Pupil(int teacher) => new PopulationUnit(7, PopulationStatus.Trainee, 0, null,
            new PopulationAssignment(PopulationAssignmentKind.Course, "synthetic-course", 1, teacher),
            CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None);
        var invalid = new PopulationUnit(7, PopulationStatus.Captured, 0, null,
            new PopulationAssignment(PopulationAssignmentKind.Work, "synthetic-work"),
            CivilianProfession.None, CivilianTier.None, 0, "", 0, PopulationEquipment.None);
        Assert.Throws<ArgumentException>(() => Context(new[] { invalid }, new[] { Facility() }));
        Assert.Throws<ArgumentException>(() => Context(new[] { Pupil(99) }, new[] { Facility("synthetic-course") }));
        Assert.Throws<ArgumentException>(() => Context(new[] { Person(1, PopulationAssignmentKind.Unassigned) }, residents: new[] { -1 }));
        var teacher = Person(2, PopulationAssignmentKind.Teaching, "synthetic-course");
        var valid = Context(new[] { teacher, Pupil(2) }, new[] { Facility("synthetic-course") });
        Assert.Equal(new[] { 7 }, valid.CourseEligibleIds);
        Assert.Empty(valid.WorkEligibleIds);
        Assert.Throws<ArgumentException>(() => Gdd03SyntheticMonthContext.CreateSynthetic(
            SaveGame.Gdd03Ruleset, 0, Array.Empty<PopulationUnit>(), Array.Empty<Gdd03SyntheticFacilitySnapshot>(),
            Array.Empty<int>(), new[] { new Gdd03SyntheticHousingSnapshot("synthetic-house", 0m),
                new Gdd03SyntheticHousingSnapshot("synthetic-house", 1m) }, Array.Empty<Gdd03SyntheticPendingEntry>()));
        var zero = new Gdd03SyntheticFacilitySnapshot("synthetic-work", true, 0);
        Assert.Empty(Context(new[] { Person(1, PopulationAssignmentKind.Work, zero.Id) }, new[] { zero }).WorkEligibleIds);
    }

    // RED-FIRST 3: GDD loop-switch:17,24-28,33. Frozen membership is not a work executor.
    [Fact] public void FreezesDisjointOpeningEligibilityAndOmitsUnavailableCapacity()
    {
        var people = new List<PopulationUnit> {
            Person(9, PopulationAssignmentKind.Work, "synthetic-work"),
            Person(3, PopulationAssignmentKind.Course, "synthetic-course"),
            Person(5, PopulationAssignmentKind.Reception, "synthetic-reception"),
            Person(8, PopulationAssignmentKind.Work, "synthetic-closed") };
        var facilities = new List<Gdd03SyntheticFacilitySnapshot> {
            Facility(), Facility("synthetic-course"), Facility("synthetic-reception"), Facility("synthetic-closed", false) };
        var context = Context(people, facilities, new[] { 9 });
        people.Clear(); facilities.Clear();
        Assert.Equal(new[] { 3, 5, 8, 9 }, context.OpeningPeople.Select(p => p.Id));
        Assert.Equal(new[] { 9 }, context.WorkEligibleIds);
        Assert.Equal(new[] { 3 }, context.CourseEligibleIds);
        Assert.Equal(new[] { 5 }, context.ReceptionEligibleIds);
        Assert.Equal(new[] { 9 }, context.PreArrivalResidentIds);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)context.WorkEligibleIds).Add(11));
        Assert.Throws<NotSupportedException>(() => ((IList<PopulationUnit>)context.OpeningPeople).Clear());
        Assert.Equal(PopulationAssignmentKind.Course, context.OpeningPeople[0].Assignment.Kind);
        Assert.Equal(0, context.OpeningPeople[0].Assignment.ProgressTicks);
    }

    // RED-FIRST 4: R-LOOP-002; GDD loop-switch:29,39. No persistence/hash claim.
    [Theory] [InlineData("he-IL")] [InlineData("en-US")]
    public void SortsOrdinallyAndCopiesEveryContainerUnderBothCultures(string name)
    {
        var previous = CultureInfo.CurrentCulture; var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            var pupil = new PopulationUnit(20, PopulationStatus.Trainee, 1, null,
                new PopulationAssignment(PopulationAssignmentKind.Course, "synthetic-I", 3),
                CivilianProfession.Farmer, CivilianTier.Trained, 7, "synthetic-qualification", 5,
                new PopulationEquipment("synthetic-tools", true, "synthetic-kit", true));
            var people = new[] { pupil, Person(2, PopulationAssignmentKind.Reception, "synthetic-i") };
            var keys = new[] { "synthetic-é", "synthetic-i", "synthetic-I", "synthetic_!", "synthetic-!" };
            var expectedKeys = new[] { "synthetic-!", "synthetic-I", "synthetic-i", "synthetic-é", "synthetic_!" };
            var facilities = keys.Select(id => Facility(id)).ToList();
            var residents = new List<int> { 20, 2 };
            var housing = keys.Select(id => new Gdd03SyntheticHousingSnapshot(id, id == "synthetic-é" ? 0.4m : 0.7m)).ToList();
            var pending = keys.Select(id => new Gdd03SyntheticPendingEntry(id, "synthetic-unit", id == "synthetic-é" ? -1.25m : 2m)).ToList();
            Gdd03SyntheticMonthContext Make(bool reverse) => Gdd03SyntheticMonthContext.CreateSynthetic(
                SaveGame.Gdd03Ruleset, 4, reverse ? people.Reverse() : people,
                reverse ? facilities.AsEnumerable().Reverse() : facilities,
                reverse ? residents.AsEnumerable().Reverse() : residents,
                reverse ? housing.AsEnumerable().Reverse() : housing,
                reverse ? pending.AsEnumerable().Reverse() : pending);
            var a = Make(false); var b = Make(true);
            people[0] = Person(99, PopulationAssignmentKind.Unassigned);
            facilities.Clear(); residents.Clear(); housing.Clear(); pending.Clear();
            foreach (var context in new[] { a, b })
            {
                Assert.Equal(new[] { 2, 20 }, context.OpeningPeople.Select(p => p.Id));
                Assert.Equal(new[] { 2, 20 }, context.PreArrivalResidentIds);
                Assert.Equal(expectedKeys, context.Facilities.Select(f => f.Id));
                Assert.Equal(expectedKeys, context.Housing.Select(h => h.Id));
                Assert.Equal(expectedKeys, context.PendingEntries.Select(e => e.Key));
                Assert.All(context.Facilities, f => { Assert.True(f.AvailableAtMonthFreeze); Assert.Equal(2, f.OpeningCapacity); });
                Assert.Empty(context.WorkEligibleIds);
                Assert.Equal(new[] { 20 }, context.CourseEligibleIds);
                Assert.Equal(new[] { 2 }, context.ReceptionEligibleIds);
                var retained = context.OpeningPeople[1];
                Assert.Equal(CivilianProfession.Farmer, retained.Profession);
                Assert.Equal(CivilianTier.Trained, retained.Tier);
                Assert.Equal(7, retained.WorkXp);
                Assert.Equal(5, retained.CombatXp);
                Assert.Equal("synthetic-qualification", retained.Qualification);
                Assert.Equal(1, retained.ReceptionTicks);
                Assert.Equal(3, retained.Assignment.ProgressTicks);
                Assert.Equal("synthetic-tools", retained.Equipment.ToolsId);
                Assert.True(retained.Equipment.ToolsPaid);
                Assert.Equal("synthetic-kit", retained.Equipment.MilitaryId);
                Assert.True(retained.Equipment.MilitaryPaid);
                Assert.Equal(0.4m, context.Housing[3].ConditionBeforeDeterioration);
                Assert.Equal(-1.25m, context.PendingEntries[3].Amount);
                Assert.Equal("synthetic-unit", context.PendingEntries[3].Unit);
                Assert.Equal("-1.25", context.PendingEntries[3].Amount.ToString(CultureInfo.InvariantCulture));
                Assert.Throws<NotSupportedException>(() => ((IList<PopulationUnit>)context.OpeningPeople).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<Gdd03SyntheticFacilitySnapshot>)context.Facilities).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<Gdd03SyntheticHousingSnapshot>)context.Housing).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<Gdd03SyntheticPendingEntry>)context.PendingEntries).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<int>)context.PreArrivalResidentIds).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<int>)context.WorkEligibleIds).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<int>)context.CourseEligibleIds).Clear());
                Assert.Throws<NotSupportedException>(() => ((IList<int>)context.ReceptionEligibleIds).Clear());
            }
        }
        finally { CultureInfo.CurrentCulture = previous; CultureInfo.CurrentUICulture = previousUi; }
    }
}
