using Statecraft.Sim;

// #1697: synthetic fixtures only. No real centre, camp, region, host or game caller is exercised.
public class Gdd03TrainingCentreFixtureTests
{
    static readonly Lazy<Gdd03BalanceCatalogue> Real = new(() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!);
    const string CentreA = "fixture-centre-a", CampA = "fixture-camp-a", CentreB = "fixture-centre-b", CampB = "fixture-camp-b";
    const string CentreSouth = "fixture-centre-south", CampSouth = "fixture-camp-south", Field = "fixture-field";
    const string Service = "fixture-service", Workshop = "fixture-workshop";
    static readonly string[] Facilities = { CampA, CampB, CampSouth, CentreA, CentreB, CentreSouth, Field, Workshop };

    static Content Catalogue(string? key = null, Gdd03DesignValue? replacement = null, string ruleset = SaveGame.Gdd03Ruleset)
    {
        var c = PopulationLedgerTests.Content(ruleset);
        var values = new Dictionary<string, Gdd03DesignValue>(Real.Value.Values, StringComparer.Ordinal);
        if (key != null) { if (replacement == null) values.Remove(key); else values[key] = replacement; }
        c.Gdd03Balance = new Gdd03BalanceCatalogue { Ruleset = Real.Value.Ruleset, Values = values };
        return c;
    }
    static Gdd03DesignValue Agreed(decimal v) => new() { Basis = "Agreed", Values = new() { v }, Note = "synthetic test value" };
    static Gdd03DesignValue Deferred() => new() { Basis = "Deferred", Expression = "X", Note = "synthetic deferred value" };

    // The reception camps are bound only so that refugees can be placed in one; no centre refers to a camp.
    static PopulationBindings Bindings(bool camps = true) => new(
        new[] { new PopulationLocation("north"), new PopulationLocation("south") },
        (camps ? new[] { new PopulationTarget(CampA, PopulationAssignmentKind.Reception, "north"),
                new PopulationTarget(CampB, PopulationAssignmentKind.Reception, "north"),
                new PopulationTarget(CampSouth, PopulationAssignmentKind.Reception, "south") } : Array.Empty<PopulationTarget>()).Concat(new[] {
                new PopulationTarget(CentreA, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(CentreB, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(CentreSouth, PopulationAssignmentKind.Course, "south", 3),
                new PopulationTarget(Field, PopulationAssignmentKind.Work, "north"),
                new PopulationTarget(Workshop, PopulationAssignmentKind.Work, "north"),
                new PopulationTarget(Service, PopulationAssignmentKind.ActiveService, "north") }),
        Array.Empty<string>(), Array.Empty<string>());

    static PopulationUnit Person(CivilianProfession profession, CivilianTier tier, string? tools,
        PopulationStatus status = PopulationStatus.FreeCivilian, int reception = 6, PopulationAssignment? assignment = null,
        string region = "north")
        => new(0, status, reception, new PopulationLocation(region), assignment ?? PopulationAssignment.Unassigned,
            profession, tier, profession == CivilianProfession.None ? 0 : 4, "", 2,
            new PopulationEquipment(tools, tools != null));
    static PopulationUnit Teacher(CivilianProfession p, string tools) => Person(p, CivilianTier.Veteran, tools);
    static PopulationUnit Novice(CivilianProfession p, string tools) => Person(p, CivilianTier.Novice, tools);
    static PopulationUnit Refugee(string camp = CampA) => Person(CivilianProfession.None, CivilianTier.None, null,
        PopulationStatus.Refugee, 2, new PopulationAssignment(PopulationAssignmentKind.Reception, camp));

    // Ids follow the row order: 0 farmer teacher, 1 farmer novice, 2 pioneer teacher, 3 pioneer novice,
    // 4 second farmer teacher, 5 second farmer novice, then any extra rows.
    static GameState State(Content c, params PopulationUnit[] extra) => State(c, Bindings(), extra);
    static GameState State(Content c, PopulationBindings bindings, params PopulationUnit[] extra)
    {
        var people = new List<PopulationUnit> {
            Teacher(CivilianProfession.Farmer, "tools-0"), Novice(CivilianProfession.Farmer, "tools-1"),
            Teacher(CivilianProfession.Pioneer, "tools-2"), Novice(CivilianProfession.Pioneer, "tools-3"),
            Teacher(CivilianProfession.Farmer, "tools-4"), Novice(CivilianProfession.Farmer, "tools-5") };
        people.AddRange(extra);
        return PopulationLedger.CreateFixture(c, bindings, people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)));
    }
    // R-TRAIN-005, Q42: each centre is a facility in its host city's region; two centres share the north.
    static Gdd03FixtureTrainingCentre[] Centres(bool operating = true) => new[] {
        new Gdd03FixtureTrainingCentre(CentreA, "north", operating),
        new Gdd03FixtureTrainingCentre(CentreB, "north", true),
        new Gdd03FixtureTrainingCentre(CentreSouth, "south", true) };
    static Gdd03SyntheticMonthContext Month(GameState s, int ordinal, bool centresAvailable = true)
        => Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, ordinal, s.PopulationLedger.Units,
            Facilities.Select(f => new Gdd03SyntheticFacilitySnapshot(f, centresAvailable || !f.Contains("centre", StringComparison.Ordinal), 1)),
            Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());
    static (int teacher, int trainee) Pair(CivilianProfession p) => p == CivilianProfession.Farmer ? (0, 1) : (2, 3);

    // A ruled #1668 Q42 or Q43 refusal is a plain ArgumentException with its reason; only unbuilt cases say "Deferred:".
    const string OneTeacherOnePupil = Gdd03TrainingCentreFixture.OneTeacherOnePupilReason;
    const string TeacherNeedsAPupil = Gdd03TrainingCentreFixture.TeacherNeedsAPupilReason;
    const string TeacherCity = Gdd03TrainingCentreFixture.TeacherCityReason;
    const string PupilCity = Gdd03TrainingCentreFixture.PupilCityReason;
    const string GraduationMonth = Gdd03TrainingCentreFixture.GraduationMonthReason;
    const string CourseAtOpening = Gdd03TrainingCentreFixture.CourseAtOpeningReason;

    static void Unchanged(GameState s, Action act, Type expected, string? messagePrefix = null)
    {
        var before = s.Describe();
        var ex = Assert.Throws(expected, act);
        if (messagePrefix != null) Assert.StartsWith(messagePrefix, ex.Message, StringComparison.Ordinal);
        else Assert.False(ex.Message.StartsWith("Deferred:", StringComparison.Ordinal), ex.Message);
        Assert.Equal(before, s.Describe());
    }

    // R-TRAIN-005, R-TRAIN-006, R-QUAL-008: a matching equipped novice and veteran teacher start a course; a counted course
    // turn keeps the same id and every record field, creates no person and earns no work XP. GDD 0.3 (docs/GDD.md:372): "A
    // course occupies its people from its first turn (Q43(a)); they are released in the turn its last course turn counts and
    // can be assigned from the next planning, or at once on cancellation, and work starts the turn after (Q43(b), Q43(c))."
    // #1697: "A free civilian or a matching equipped novice may enrol; completion gives the trained tier." So the enrolment
    // month is course turn 1 and the third counted turn graduates: the pupil takes the trained tier and pupil and teacher
    // are released as civilians without a job, keeping their ids and every other record field.
    [Theory]
    [InlineData(CivilianProfession.Farmer)]
    [InlineData(CivilianProfession.Pioneer)]
    public void MatchingNoviceCountsThreeTurnsFromEnrolmentAndGraduates(CivilianProfession profession)
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var (teacher, trainee) = Pair(profession); var l = s.PopulationLedger;
        var before = l.Get(trainee); var teacherBefore = l.Get(teacher);
        int count = l.Units.Count, next = l.NextPopulationId; var people = l.TotalPeople;

        var enrolment = Month(s, 0);
        f.Start(trainee, teacher, CentreA, enrolment);
        var p = l.Get(trainee);
        Assert.Equal(PopulationStatus.Trainee, p.Status);
        Assert.Equal(PopulationAssignmentKind.Course, p.Assignment.Kind);
        Assert.Equal(CentreA, p.Assignment.Target); Assert.Equal(0, p.Assignment.ProgressTicks); Assert.Equal(teacher, p.Assignment.TeacherId);
        Assert.Equal(PopulationAssignmentKind.Teaching, l.Get(teacher).Assignment.Kind);
        Assert.Equal(CentreA, l.Get(teacher).Assignment.Target);

        f.Advance(trainee, enrolment);
        var after = l.Get(trainee); var teacherAfter = l.Get(teacher);
        Assert.Equal(1, after.Assignment.ProgressTicks); Assert.Equal(PopulationStatus.Trainee, after.Status);
        Assert.Equal(CivilianTier.Novice, after.Tier); Assert.Equal(profession, after.Profession);
        Assert.Equal(before.WorkXp, after.WorkXp); Assert.Equal(before.ReceptionTicks, after.ReceptionTicks);
        Assert.Same(before.Equipment, after.Equipment); Assert.Same(before.Location, after.Location);
        Assert.Equal(before.CombatXp, after.CombatXp); Assert.Equal(before.Qualification, after.Qualification);
        Assert.Equal(CivilianTier.Veteran, teacherAfter.Tier); Assert.Equal(teacherBefore.WorkXp, teacherAfter.WorkXp);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId); Assert.Equal(people, l.TotalPeople);

        f.Advance(trainee, Month(s, 1));
        Assert.Equal(2, l.Get(trainee).Assignment.ProgressTicks);
        Assert.Equal(PopulationStatus.Trainee, l.Get(trainee).Status); Assert.Equal(CivilianTier.Novice, l.Get(trainee).Tier);

        f.Advance(trainee, Month(s, 2));
        var graduate = l.Get(trainee); var released = l.Get(teacher);
        Assert.Equal(PopulationStatus.FreeCivilian, graduate.Status); Assert.Same(PopulationAssignment.Unassigned, graduate.Assignment);
        Assert.Equal(CivilianTier.Trained, graduate.Tier); Assert.Equal(profession, graduate.Profession);
        Assert.Equal(before.WorkXp, graduate.WorkXp); Assert.Equal(before.ReceptionTicks, graduate.ReceptionTicks);
        Assert.Same(before.Equipment, graduate.Equipment); Assert.True(graduate.Equipment.ToolsPaid);
        Assert.Same(before.Location, graduate.Location);
        Assert.Equal(before.CombatXp, graduate.CombatXp); Assert.Equal(before.Qualification, graduate.Qualification);
        Assert.Equal(PopulationStatus.FreeCivilian, released.Status); Assert.Same(PopulationAssignment.Unassigned, released.Assignment);
        Assert.Equal(CivilianTier.Veteran, released.Tier); Assert.Equal(teacherBefore.WorkXp, released.WorkXp);
        Assert.Equal(teacherBefore.Equipment, released.Equipment); Assert.Same(teacherBefore.Location, released.Location);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId); Assert.Equal(people, l.TotalPeople);
        l.Reconcile();
    }

    // R-TRAIN-006: the Agreed course length is read from the catalogue, not a literal: with a synthetic 4, the course
    // graduates on its fourth counted turn, the enrolment month included (Q43(a), docs/GDD.md:372: "A course occupies its
    // people from its first turn"), not on the third.
    [Fact] public void CourseLengthComesFromTheCatalogue()
    {
        var c = Catalogue("profession.course_turns", Agreed(4)); var s = State(c); var l = s.PopulationLedger;
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        f.Advance(1, Month(s, 0)); f.Advance(1, Month(s, 1)); f.Advance(1, Month(s, 2));
        Assert.Equal(3, l.Get(1).Assignment.ProgressTicks);
        Assert.Equal(PopulationStatus.Trainee, l.Get(1).Status); Assert.Equal(CivilianTier.Novice, l.Get(1).Tier);
        f.Advance(1, Month(s, 3));
        Assert.Equal(PopulationStatus.FreeCivilian, l.Get(1).Status); Assert.Equal(CivilianTier.Trained, l.Get(1).Tier);
    }

    // R-TRAIN-006: one month counts once, the enrolment month included (Q43(a)); a stale or repeated snapshot cannot add
    // a second course turn.
    [Fact] public void AMonthCountsOnceForACourse()
    {
        var c = Catalogue("profession.course_turns", Agreed(5)); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0);
        f.Advance(1, m0);
        Assert.Equal(1, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, m0), typeof(ArgumentException));
        Unchanged(s, () => f.Advance(1, Month(s, 0)), typeof(ArgumentException));
        var m1 = Month(s, 1); f.Advance(1, m1);
        Assert.Equal(2, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, m1), typeof(ArgumentException));
        // A fresh snapshot with an ordinal that was already counted, or an earlier one, is refused too.
        Unchanged(s, () => f.Advance(1, Month(s, 1)), typeof(ArgumentException));
        f.Advance(1, Month(s, 3));
        Unchanged(s, () => f.Advance(1, Month(s, 2)), typeof(ArgumentException));
        Assert.Equal(3, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
    }

    // R-TRAIN-006: a snapshot taken after enrolment but carrying the enrolment month's ordinal is still the enrolment
    // month, so it counts once, as course turn 1 (#1668 Q43(a), docs/GDD.md:372: "A course occupies its people from its
    // first turn"). A snapshot with an ordinal before the enrolment month is refused.
    [Fact] public void TheEnrolmentOrdinalCountsOnce()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 1));
        Unchanged(s, () => f.Advance(1, Month(s, 0)), typeof(ArgumentException));
        f.Advance(1, Month(s, 1));
        Assert.Equal(1, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, Month(s, 1)), typeof(ArgumentException));
    }

    // R-TRAIN-005: a busy teacher refuses a second pupil (Agreed profession.trainees_per_teacher = 1).
    [Fact] public void BusyTeacherRefusesASecondPupil()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => f.Start(5, 0, CentreA, Month(s, 1)), typeof(ArgumentException));
        Unchanged(s, () => f.Start(5, 0, CentreB, Month(s, 1)), typeof(ArgumentException));
    }

    // R-TRAIN-005, Q42 (docs/GDD.md:372: "One veteran teacher and one pupil per centre"): a centre holds one pupil
    // whatever the catalogue's per-teacher limit says. With a synthetic profession.trainees_per_teacher of 2 (the
    // catalogue's is 1), a second pupil for the same teacher at the same centre is refused with the Q42 reason, in the
    // same month or a later one, the state unchanged; an inventory with two pupils at one centre is refused the same way.
    [Fact] public void ACentreTakesOnePupilWhateverThePerTeacherLimit()
    {
        var c = Catalogue("profession.trainees_per_teacher", Agreed(2)); var s = State(c);
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => f.Start(5, 0, CentreA, Month(s, 0)), typeof(ArgumentException), OneTeacherOnePupil);
        Unchanged(s, () => f.Start(5, 0, CentreA, Month(s, 1)), typeof(ArgumentException), OneTeacherOnePupil);
        Assert.Equal(0, s.PopulationLedger.Get(1).Assignment.TeacherId); Assert.Equal(PopulationStatus.FreeCivilian, s.PopulationLedger.Get(5).Status);

        // Id 6 teaches at centre A with two pupils, 7 and 8.
        var two = State(c,
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-6", assignment: new PopulationAssignment(PopulationAssignmentKind.Teaching, CentreA)),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-7", PopulationStatus.Trainee,
                assignment: new PopulationAssignment(PopulationAssignmentKind.Course, CentreA, 0, 6)),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-8", PopulationStatus.Trainee,
                assignment: new PopulationAssignment(PopulationAssignmentKind.Course, CentreA, 0, 6)));
        Unchanged(two, () => new Gdd03TrainingCentreFixture(c, two, Centres()), typeof(ArgumentException), OneTeacherOnePupil);
    }

    // R-TRAIN-005: the MVP has no reception-bed layer (owner, 1 November 2026, "No reception-bed layer (Recommended)";
    // docs/gdd/training.md:38), so refugees in a camp take no course place. With three refugees in camp A (the old
    // reception.capacity) and one in camp B, both northern centres still start a course.
    [Fact] public void RefugeesInAReceptionCampTakeNoCoursePlace()
    {
        var c = Catalogue();
        var s = State(c, Refugee(), Refugee(), Refugee(), Refugee(CampB));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(1).Status);
        f.Start(3, 2, CentreB, Month(s, 0));
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(3).Status);
        s.PopulationLedger.Reconcile();
    }

    // R-TRAIN-005, Q42 (docs/GDD.md:372): a centre is a facility in its host city and needs no reception camp: with no
    // camp bound at all, a matching novice and veteran start, and the course counts.
    [Fact] public void ACentreInItsHostCityNeedsNoReceptionCamp()
    {
        var c = Catalogue(); var s = State(c, Bindings(camps: false)); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0); f.Advance(1, m0);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(1).Status);
        Assert.Equal(1, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
    }

    // R-TRAIN-005, #1693 owner answers of 3 November 2026, answer 1: "Tools in the MVP: no." A novice with no tools
    // enrols and keeps an empty tools record.
    [Fact] public void ANoviceWithoutToolsEnrols()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Farmer, CivilianTier.Novice, null));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(6, 0, CentreA, Month(s, 0));
        var p = s.PopulationLedger.Get(6);
        Assert.Equal(PopulationStatus.Trainee, p.Status); Assert.Null(p.Equipment.ToolsId); Assert.False(p.Equipment.ToolsPaid);
    }

    // R-TRAIN-005, docs/GDD.md:374: "Refugees cannot train until they become civilians". A free civilian is one: the
    // course reads no reception progress, so a teacher and a novice with none teach and enrol.
    [Fact] public void AFreeCivilianNeedsNoReceptionProgress()
    {
        var c = Catalogue(); var s = State(c,
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-6", reception: 0),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-7", reception: 0));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(7, 6, CentreA, Month(s, 0));
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(7).Status);
        Assert.Equal(PopulationAssignmentKind.Teaching, s.PopulationLedger.Get(6).Assignment.Kind);
    }

    // R-TRAIN-005: the centre's limits are read from no reception value. Without reception.capacity, and with a
    // reception.turns that differs from the ledger's, the fixture builds and a course starts.
    [Fact] public void TheCentreReadsNoReceptionValues()
    {
        var s = State(Catalogue());
        var c = Catalogue("reception.capacity"); c.Gdd03Balance!.Values["reception.turns"] = Agreed(s.PopulationLedger.ReceptionLimit + 1);
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(1).Status);
    }

    // R-TRAIN-005: settled refusals leave the state unchanged and are not reported as Deferred.
    // An unequipped novice and a working trainee or teacher may start now (ANoviceWithoutToolsEnrols,
    // PeopleWithAJobStartACourseAndLeaveItInTheSameCommit); the teacher busy elsewhere is now teaching at centre B.
    public static IEnumerable<object[]> SettledRefusals() => new[] {
        new object[] { "trained trainee", 0, 6, CentreA, true },
        new object[] { "refugee", 0, 8, CentreA, true },
        new object[] { "craftsman", 0, 10, CentreA, true },
        new object[] { "active soldier", 0, 11, CentreA, true },
        new object[] { "trained teacher", 12, 1, CentreA, true },
        new object[] { "teacher busy elsewhere", 13, 1, CentreA, true },
        new object[] { "same person", 1, 1, CentreA, true },
        new object[] { "unknown centre", 0, 1, "fixture-unknown", true },
        new object[] { "centre not operating", 0, 1, CentreA, false },
        // The teacher is housed in the centre's city (Q42) and, by our reading, the pupil trains there; the region stands
        // in for the city.
        new object[] { "other region", 0, 1, CentreSouth, true },
        new object[] { "trainee in another region", 0, 14, CentreA, true },
        new object[] { "teacher in another region", 16, 1, CentreA, true },
    };
    [Theory, MemberData(nameof(SettledRefusals))]
    public void IneligibleStartsAreRefusedWithoutMutation(string why, int teacher, int trainee, string centre, bool operating)
    {
        Assert.False(string.IsNullOrEmpty(why));
        var c = Catalogue();
        var s = State(c,
            Person(CivilianProfession.Farmer, CivilianTier.Trained, "tools-6"),                         // 6
            Person(CivilianProfession.Farmer, CivilianTier.Novice, null),                              // 7
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-8", PopulationStatus.Refugee, 5), // 8
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-9", assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)), // 9
            Person(CivilianProfession.Craftsman, CivilianTier.Apprentice, "tools-10"),                 // 10
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-11", PopulationStatus.ActiveSoldier, assignment: new PopulationAssignment(PopulationAssignmentKind.ActiveService, Service)), // 11
            Person(CivilianProfession.Farmer, CivilianTier.Trained, "tools-12"),                        // 12
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-13", assignment: new PopulationAssignment(PopulationAssignmentKind.Teaching, CentreB)), // 13
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-14", region: "south"),       // 14
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-15", PopulationStatus.Trainee,
                assignment: new PopulationAssignment(PopulationAssignmentKind.Course, CentreB, 0, 13)),  // 15, pupil of 13
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-16", region: "south"));     // 16
        var f = new Gdd03TrainingCentreFixture(c, s, Centres(operating));
        Unchanged(s, () => f.Start(trainee, teacher, centre, Month(s, 0)), typeof(ArgumentException));
    }

    // R-TRAIN-005, Q42 (docs/GDD.md:372: "the teacher housed in the centre's city"): a teacher outside the centre's region
    // is refused with the Q42 reason; a pupil outside it with our reading's reason (Q42 rules only on the teacher). Both
    // are refused before the ledger is asked, the state unchanged. The region stands in for the city.
    [Fact] public void ATeacherOrPupilOutsideTheHostRegionIsRefused()
    {
        var c = Catalogue(); var s = State(c,
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-6", region: "south"),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-7", region: "south"));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        Unchanged(s, () => f.Start(1, 6, CentreA, Month(s, 0)), typeof(ArgumentException), TeacherCity);
        Unchanged(s, () => f.Start(7, 0, CentreA, Month(s, 0)), typeof(ArgumentException), PupilCity);
        Unchanged(s, () => f.Start(1, 0, CentreSouth, Month(s, 0)), typeof(ArgumentException), TeacherCity);
        Unchanged(s, () => f.Start(1, 6, CentreSouth, Month(s, 0)), typeof(ArgumentException), PupilCity);
        f.Start(7, 6, CentreSouth, Month(s, 0));
        Assert.Equal(CentreSouth, s.PopulationLedger.Get(7).Assignment.Target);
    }

    // R-TRAIN-007: cancel at every course turn before graduation keeps id, the tools fields and reception progress;
    // grants nothing unfinished; clears progress; releases the teacher and the centre's place; moves no one.
    [Theory]
    [InlineData(CivilianProfession.Farmer, 0)]
    [InlineData(CivilianProfession.Farmer, 1)]
    [InlineData(CivilianProfession.Farmer, 2)]
    [InlineData(CivilianProfession.Pioneer, 0)]
    [InlineData(CivilianProfession.Pioneer, 1)]
    [InlineData(CivilianProfession.Pioneer, 2)]
    public void CancelAtEveryTurnRetainsPaidStateAndReleasesTeacherAndCentre(CivilianProfession profession, int counted)
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger;
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var (teacher, trainee) = Pair(profession); var before = l.Get(trainee); int count = l.Units.Count, next = l.NextPopulationId;
        f.Start(trainee, teacher, CentreA, Month(s, 0));
        for (int m = 1; m <= counted; m++) f.Advance(trainee, Month(s, m));
        Assert.Equal(counted, l.Get(trainee).Assignment.ProgressTicks);
        f.Cancel(trainee);
        var after = l.Get(trainee);
        Assert.Equal(PopulationStatus.FreeCivilian, after.Status); Assert.Same(PopulationAssignment.Unassigned, after.Assignment);
        Assert.Equal(CivilianTier.Novice, after.Tier); Assert.Equal(profession, after.Profession);
        Assert.Same(before.Equipment, after.Equipment); Assert.True(after.Equipment.ToolsPaid);
        Assert.Equal(before.ReceptionTicks, after.ReceptionTicks); Assert.Equal(before.WorkXp, after.WorkXp);
        Assert.Same(PopulationAssignment.Unassigned, l.Get(teacher).Assignment);
        Assert.Same(before.Location, after.Location);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        // Teacher and centre are free again: centre A's one place takes a new pupil with another teacher.
        f.Start(5, 4, CentreA, Month(s, 10));
        Assert.Equal(PopulationStatus.Trainee, l.Get(5).Status);
    }

    // R-TRAIN-007: a repeated cancel, or a cancel of someone not in a course, is refused; a restart is a full new course.
    [Fact] public void CancelIsOnceAndRestartIsAFullNewCourse()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger;
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0)); f.Advance(1, Month(s, 1));
        f.Cancel(1);
        Unchanged(s, () => f.Cancel(1), typeof(ArgumentException));
        Unchanged(s, () => f.Cancel(5), typeof(ArgumentException));
        f.Start(1, 0, CentreA, Month(s, 2));
        Assert.Equal(0, l.Get(1).Assignment.ProgressTicks);
        f.Advance(1, Month(s, 3));
        Assert.Equal(1, l.Get(1).Assignment.ProgressTicks);
        // The restart's count starts again: the old course's turn does not carry over, so month 4 does not graduate.
        f.Advance(1, Month(s, 4));
        Assert.Equal(2, l.Get(1).Assignment.ProgressTicks); Assert.Equal(CivilianTier.Novice, l.Get(1).Tier);
    }

    // R-TRAIN-005: teacher and trainee hold exclusive assignments; other assignment paths refuse them.
    [Fact] public void TeacherAndTraineeCannotTakeOtherAssignments()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger;
        new Gdd03TrainingCentreFixture(c, s, Centres()).Start(1, 0, CentreA, Month(s, 0));
        var work = new Gdd03AssignmentFixture(c, s, new[] { new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 5, "fixture") });
        foreach (var id in new[] { 0, 1 })
        {
            Unchanged(s, () => work.Assign(id, Field), typeof(ArgumentException));
            Unchanged(s, () => l.ChangeAssignment(id, new PopulationAssignment(PopulationAssignmentKind.Work, Field)), typeof(ArgumentException));
        }
    }

    // R-TRAIN-005, R-TRAIN-008, Q42 (docs/GDD.md:372: "One veteran teacher and one pupil per centre"): a second teacher
    // at a centre is refused with the Q42 reason, not Deferred, the state unchanged, whether or not the pupil already
    // works; an inventory with two teachers at one centre is refused the same way.
    [Fact] public void ASecondTeacherAtACentreIsRefused()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-6",
            assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => f.Start(5, 4, CentreA, Month(s, 0)), typeof(ArgumentException), OneTeacherOnePupil);
        Unchanged(s, () => f.Start(6, 4, CentreA, Month(s, 0)), typeof(ArgumentException), OneTeacherOnePupil);
        Assert.Equal(Field, s.PopulationLedger.Get(6).Assignment.Target);

        // Ids 6 and 7 teach at centre A, each with a pupil (8 and 9).
        var two = State(c,
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-6", assignment: new PopulationAssignment(PopulationAssignmentKind.Teaching, CentreA)),
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-7", assignment: new PopulationAssignment(PopulationAssignmentKind.Teaching, CentreA)),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-8", PopulationStatus.Trainee,
                assignment: new PopulationAssignment(PopulationAssignmentKind.Course, CentreA, 0, 6)),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-9", PopulationStatus.Trainee,
                assignment: new PopulationAssignment(PopulationAssignmentKind.Course, CentreA, 0, 7)));
        Unchanged(two, () => new Gdd03TrainingCentreFixture(c, two, Centres()), typeof(ArgumentException), OneTeacherOnePupil);
    }

    // R-TRAIN-005, R-TRAIN-008, Q42: a centre's teacher teaches its one pupil, so a Teaching record with no pupil is an
    // invalid inventory, refused with the Q42 reason, not Deferred.
    [Fact] public void ATeachingRecordWithNoPupilIsInvalid()
    {
        var c = Catalogue();
        var idle = State(c, new PopulationUnit(0, PopulationStatus.FreeCivilian, 6, new PopulationLocation("north"),
            new PopulationAssignment(PopulationAssignmentKind.Teaching, CentreB), CivilianProfession.Farmer, CivilianTier.Veteran,
            4, "", 2, new PopulationEquipment("tools-6", true)));
        Unchanged(idle, () => new Gdd03TrainingCentreFixture(c, idle, Centres()), typeof(ArgumentException), TeacherNeedsAPupil);
    }

    // R-TRAIN-006, R-TRAIN-008: #1668 Q43(a) (docs/GDD.md:372: "A course occupies its people from its first turn"). The
    // enrolment month counts once even though its opening snapshot predates the course, unless the centre was not
    // available at that month's freeze (27 Oct list Q9 stays Deferred). Every later month still needs the course at its
    // opening: a month-1 snapshot taken before the course started is refused with the Q43(a) reason (a ruled rule).
    [Fact] public void TheEnrolmentMonthCountsAndLaterMonthsNeedTheCourseAtTheirOpening()
    {
        var c = Catalogue(); var s = State(c);
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var enrolment = Month(s, 0); var closedEnrolment = Month(s, 0, centresAvailable: false); var staleLater = Month(s, 1);
        f.Start(1, 0, CentreA, enrolment);
        Unchanged(s, () => f.Advance(1, closedEnrolment), typeof(NotSupportedException), "Deferred: #1668 27 Oct list Q9");
        Unchanged(s, () => f.Advance(1, staleLater), typeof(ArgumentException), CourseAtOpening);
        f.Advance(1, enrolment);
        Assert.Equal(1, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, enrolment), typeof(ArgumentException));
        Unchanged(s, () => f.Advance(1, staleLater), typeof(ArgumentException), CourseAtOpening);
    }

    // R-TRAIN-006, 27 Oct list Q9: in the enrolment month the centre's availability is read from the month's facility
    // list (the opening may predate the course). A centre listed with no free place (opening capacity 0) at that month's
    // freeze counts no course turn: refused Deferred, state unchanged; the next month with a place counts as before.
    [Fact] public void TheEnrolmentMonthCountsNothingWhenTheCentreHasNoPlace()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var noPlace = Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, 0, s.PopulationLedger.Units,
            Facilities.Select(id => new Gdd03SyntheticFacilitySnapshot(id, true, id == CentreA ? 0 : 1)),
            Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());
        f.Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => f.Advance(1, noPlace), typeof(NotSupportedException), "Deferred: #1668 27 Oct list Q9");
        Assert.Equal(0, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
        f.Advance(1, Month(s, 1));
        Assert.Equal(1, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
    }

    // R-TRAIN-006, R-QUAL-008, Q42: graduation reuses the trained-tier transform and charges nothing. A centre has one
    // pupil (Q42), so even with a synthetic two pupils per teacher (the catalogue's is 1) no second pupil joins the
    // teacher's centre, and the graduation releases the teacher with its one pupil.
    [Fact] public void AGraduationReleasesTheCentresOneTeacher()
    {
        var c = Catalogue("profession.trainees_per_teacher", Agreed(2)); var s = State(c); var l = s.PopulationLedger;
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0); f.Advance(1, m0);
        Unchanged(s, () => f.Start(5, 0, CentreA, Month(s, 1)), typeof(ArgumentException), OneTeacherOnePupil);
        f.Advance(1, Month(s, 1));
        f.Advance(1, Month(s, 2));
        Assert.Equal(CivilianTier.Trained, l.Get(1).Tier); Assert.Same(PopulationAssignment.Unassigned, l.Get(1).Assignment);
        Assert.Same(PopulationAssignment.Unassigned, l.Get(0).Assignment);
        Assert.Equal(PopulationStatus.FreeCivilian, l.Get(5).Status);
        l.Reconcile();
    }

    // R-TRAIN-007, R-TRAIN-008, #1668 Q43(b) (#1697: "on graduation the people are released in the turn the last course
    // turn counts and can be assigned from the next turn's planning"): in the graduation month neither the graduate nor the
    // released teacher can start a course, with any snapshot of that month; from the next month the teacher can.
    [Fact] public void GraduatesAndTheirTeacherCannotStartACourseInTheGraduationMonth()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0); f.Advance(1, m0); f.Advance(1, Month(s, 1));
        var graduation = Month(s, 2);
        f.Advance(1, graduation);
        Assert.Equal(CivilianTier.Trained, l.Get(1).Tier);
        Unchanged(s, () => f.Start(5, 0, CentreB, graduation), typeof(ArgumentException), GraduationMonth);
        Unchanged(s, () => f.Start(5, 0, CentreB, Month(s, 2)), typeof(ArgumentException), GraduationMonth);
        Unchanged(s, () => f.Start(5, 0, CentreA, Month(s, 2)), typeof(ArgumentException), GraduationMonth);
        Unchanged(s, () => f.Start(5, 0, CentreB, Month(s, 1)), typeof(ArgumentException), GraduationMonth);
        // The graduate is trained now, so no course takes them as a pupil (R-TRAIN-006), this month or later.
        Unchanged(s, () => f.Start(1, 4, CentreB, graduation), typeof(ArgumentException));
        Unchanged(s, () => f.Start(1, 4, CentreB, Month(s, 3)), typeof(ArgumentException));
        f.Start(5, 0, CentreB, Month(s, 3));
        Assert.Equal(PopulationStatus.Trainee, l.Get(5).Status); Assert.Equal(0, l.Get(5).Assignment.TeacherId);
    }

    // #1716 month snapshot, #1668 Q43(c) (#1697: "a person whose course ends this turn starts work the next turn"): the
    // graduation month's opening snapshot still has the graduate on the course, not at work; a job given after the
    // graduation is in the next month's snapshot.
    [Fact] public void AGraduateWorksFromTheNextMonthsSnapshot()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var work = new Gdd03AssignmentFixture(c, s, new[] { new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 2, "fixture") });
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0); f.Advance(1, m0); f.Advance(1, Month(s, 1));
        var graduation = Month(s, 2);
        f.Advance(1, graduation);
        Assert.DoesNotContain(1, graduation.WorkEligibleIds); Assert.Contains(1, graduation.CourseEligibleIds);
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(1, Field));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(0, Field));
        var next = Month(s, 3);
        Assert.Contains(1, next.WorkEligibleIds); Assert.Contains(0, next.WorkEligibleIds);
        Assert.DoesNotContain(1, next.CourseEligibleIds);
    }

    // R-TRAIN-008, #1668 Q43(b), Q42: graduation releases the people, not the centre for a month. With no reception beds
    // (Q41's bed reuse is post-MVP, docs/GDD.md:377), centre A is empty once its pupil graduates, so another teacher and
    // pupil start there in the graduation month; the released teacher still waits for the next month.
    [Fact] public void ACentreEmptiedByAGraduationTakesAnotherTeacherThatMonth()
    {
        var c = Catalogue(); var s = State(c, Refugee(), Refugee()); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0); f.Advance(1, m0); f.Advance(1, Month(s, 1));
        var graduation = Month(s, 2);
        f.Advance(1, graduation);
        Unchanged(s, () => f.Start(5, 0, CentreA, graduation), typeof(ArgumentException), GraduationMonth);
        f.Start(5, 4, CentreA, graduation);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(5).Status);
        Assert.Equal(4, s.PopulationLedger.Get(5).Assignment.TeacherId);
    }

    // R-TRAIN-007: a graduate is no longer a pupil, so a further course turn or a cancel is refused unchanged, and the
    // released teacher leaves no idle Teaching assignment behind (a new fixture over the state validates).
    [Fact] public void AGraduateIsNoLongerAPupil()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0); f.Advance(1, m0); f.Advance(1, Month(s, 1)); f.Advance(1, Month(s, 2));
        Unchanged(s, () => f.Advance(1, Month(s, 3)), typeof(ArgumentException));
        Unchanged(s, () => f.Cancel(1), typeof(ArgumentException));
        _ = new Gdd03TrainingCentreFixture(c, s, Centres());
    }

    // #1716 Q43(b) release, R-TRAIN-007, R-TRAIN-008 (owner-approved answer, 1 Nov 2026, #1668 Q43(b): "Cancellation: they
    // are released at once, as civilians without a job."). A course held at the month's opening is cancelled this turn:
    // this month's snapshot already shows pupil and teacher off the course and without a job, and a job given to them this
    // turn is in the same snapshot. Characterisation: Cancel and the work fixture already behaved so before #1716's
    // follow-up; this pins it as the ruled behaviour (the restart test below is the one that fails without the change).
    [Fact] public void ACancelReleasesPupilAndTeacherAtOnceInThisMonthsSnapshot()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var work = new Gdd03AssignmentFixture(c, s, new[] { new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 2, "fixture") });
        f.Start(1, 0, CentreA, Month(s, 0));
        Assert.Contains(1, Month(s, 1).CourseEligibleIds);

        f.Cancel(1);
        var month = Month(s, 1);
        foreach (var id in new[] { 0, 1 })
        {
            var p = month.OpeningPeople.Single(x => x.Id == id);
            Assert.Equal(PopulationStatus.FreeCivilian, p.Status);
            Assert.Equal(PopulationAssignmentKind.Unassigned, p.Assignment.Kind);
            Assert.DoesNotContain(id, month.CourseEligibleIds); Assert.DoesNotContain(id, month.WorkEligibleIds);
        }

        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(1, Field));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(0, Field));
        var working = Month(s, 1);
        Assert.Contains(0, working.WorkEligibleIds); Assert.Contains(1, working.WorkEligibleIds);
    }

    // #1716 Q43(b) release, R-TRAIN-005, R-TRAIN-007, R-TRAIN-008 (owner-approved answer, 1 Nov 2026, #1668 Q43(b):
    // "Cancellation: they are released at once"). Both people of a course held at the month's opening and cancelled this
    // turn may start a new course in the same month: the pupil restarts as a full new course with another teacher, and the
    // teacher takes another pupil at another centre. The restart's own enrolment month counts as its first course turn
    // (Q43(a), docs/GDD.md:372: "A course occupies its people from its first turn"), once.
    [Fact] public void PeopleReleasedByACancelMayStartAnotherCourseThatMonth()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        var opening = Month(s, 1);
        f.Cancel(1);

        f.Start(1, 4, CentreA, opening);
        f.Start(5, 0, CentreB, opening);
        foreach (var (pupil, teacher, centre) in new[] { (1, 4, CentreA), (5, 0, CentreB) })
        {
            var p = l.Get(pupil);
            Assert.Equal(PopulationStatus.Trainee, p.Status); Assert.Equal(PopulationAssignmentKind.Course, p.Assignment.Kind);
            Assert.Equal(centre, p.Assignment.Target); Assert.Equal(teacher, p.Assignment.TeacherId); Assert.Equal(0, p.Assignment.ProgressTicks);
            Assert.Equal(PopulationAssignmentKind.Teaching, l.Get(teacher).Assignment.Kind); Assert.Equal(centre, l.Get(teacher).Assignment.Target);
        }
        var month = Month(s, 1);
        Assert.Contains(1, month.CourseEligibleIds); Assert.Contains(5, month.CourseEligibleIds);
        f.Advance(1, month);
        Assert.Equal(1, l.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, month), typeof(ArgumentException));
        f.Advance(1, Month(s, 2));
        Assert.Equal(2, l.Get(1).Assignment.ProgressTicks);
    }

    // #1716 Q43(b) release, R-TRAIN-006, R-TRAIN-007: the same pupil restarts with the same teacher at the same centre in
    // the month of the cancel. It is a full new course, and its enrolment month counts once as its first course turn
    // (Q43(a), docs/GDD.md:372: "A course occupies its people from its first turn"), even with the month snapshot taken
    // before the cancel, which still shows the old course.
    [Fact] public void TheSamePupilAndTeacherRestartAtTheSameCentreAfterACancel()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        var opening = Month(s, 1);
        f.Cancel(1);
        var tools = l.Get(1).Equipment;
        f.Start(1, 0, CentreA, opening);
        var p = l.Get(1);
        Assert.Equal(PopulationStatus.Trainee, p.Status); Assert.Equal(CentreA, p.Assignment.Target);
        Assert.Equal(0, p.Assignment.TeacherId); Assert.Equal(0, p.Assignment.ProgressTicks);
        Assert.Equal(tools, p.Equipment);
        f.Advance(1, opening);
        Assert.Equal(1, l.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, opening), typeof(ArgumentException));
    }

    // R-TRAIN-005, R-TRAIN-007: only a cancel ends a course here. A teacher still teaching and a pupil still on a course
    // cannot start another course, and the work fixture refuses both.
    [Fact] public void PeopleStillOnACourseCannotStartAnotherOrTakeAJob()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var work = new Gdd03AssignmentFixture(c, s, new[] { new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 2, "fixture") });
        f.Start(1, 0, CentreA, Month(s, 0));
        var month = Month(s, 1);
        Unchanged(s, () => f.Start(5, 0, CentreB, month), typeof(ArgumentException));
        Unchanged(s, () => f.Start(1, 4, CentreB, month), typeof(ArgumentException));
        Unchanged(s, () => work.Assign(1, Field), typeof(ArgumentException));
        Unchanged(s, () => work.Assign(0, Field), typeof(ArgumentException));
    }

    // R-TRAIN-007, R-TRAIN-008, #1716 Q43(b), Q42: a place freed by a cancel is free at once, and it is the centre's one
    // place. With a synthetic two pupils per teacher (the catalogue's is 1), the same pupil restarts in the month of the
    // cancel, and a second pupil is still refused with the Q42 reason, the state unchanged.
    [Fact] public void APlaceFreedByACancelIsFreeAtOnce()
    {
        var c = Catalogue("profession.trainees_per_teacher", Agreed(2)); var s = State(c, Refugee(CampA),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-7"));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        var opening = Month(s, 1);
        f.Cancel(1);
        f.Start(1, 0, CentreA, opening);
        Assert.Equal(0, s.PopulationLedger.Get(1).Assignment.TeacherId); Assert.Equal(CentreA, s.PopulationLedger.Get(1).Assignment.Target);
        Unchanged(s, () => f.Start(5, 0, CentreA, opening), typeof(ArgumentException), OneTeacherOnePupil);
        Unchanged(s, () => f.Start(7, 0, CentreA, opening), typeof(ArgumentException), OneTeacherOnePupil);
    }

    // R-TRAIN-007, #1716 Q43(b): refugees in a camp of the centre's region do not hold a cancelled pupil's place, so with
    // two refugees in camp A (the old full camp) the restart is allowed in the month of the cancel.
    [Fact] public void ARestartIsNotHeldBackByRefugeesInTheRegion()
    {
        var c = Catalogue(); var s = State(c, Refugee(CampA), Refugee(CampA));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        var opening = Month(s, 1);
        f.Cancel(1);
        f.Start(1, 0, CentreA, opening);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(1).Status);
        Assert.Equal(CentreA, s.PopulationLedger.Get(1).Assignment.Target);
    }

    // R-TRAIN-005, Q42: each centre has its own one teacher and one pupil, with no regional or camp limit shared between
    // centres. Centres A and B are both in the north; a course started at A this month takes nothing from B.
    [Fact] public void TwoCentresInOneRegionEachTakeTheirOwnPupil()
    {
        var c = Catalogue(); var s = State(c, Refugee(CampB), Refugee(CampB));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var month = Month(s, 0);
        f.Start(1, 0, CentreA, month);
        f.Start(3, 2, CentreB, month);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(3).Status);
        Assert.Equal(CentreB, s.PopulationLedger.Get(3).Assignment.Target);
    }

    // #1716 month snapshot, R-CITY-001/002 (owner-relayed Q34, 28 Oct 2026: a job change takes effect at once and the
    // same job again is a no-op). The month's work and course snapshot is taken after this turn's orders.
    [Fact] public void AJobChangeThisTurnIsInThisMonthsSnapshotAndTheSameJobAgainChangesNothing()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.None, CivilianTier.None, null,
            assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)));
        var work = new Gdd03AssignmentFixture(c, s, new[] {
            new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 1, "fixture"),
            new Gdd03FixtureSlot(Workshop, Gdd03FixtureWork.Workshop, 1, "fixture") });
        Assert.Equal(Field, Month(s, 0).OpeningPeople.Single(p => p.Id == 6).Assignment.Target);

        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(6, Workshop));
        var month = Month(s, 1);
        Assert.Equal(Workshop, month.OpeningPeople.Single(p => p.Id == 6).Assignment.Target);
        Assert.Contains(6, month.WorkEligibleIds);

        var hash = s.Hash(); var described = s.Describe();
        Assert.Equal(Gdd03FixtureAssignmentResult.NoOp, work.Assign(6, Workshop));
        Assert.Equal(hash, s.Hash()); Assert.Equal(described, s.Describe());
        var again = Month(s, 1);
        Assert.Equal(month.OpeningPeople, again.OpeningPeople); Assert.Equal(month.WorkEligibleIds, again.WorkEligibleIds);
    }

    // #1716 month snapshot, R-TRAIN-005, R-TRAIN-008 (owner-relayed Q43(a), 28 Oct 2026: a course occupies people from
    // the turn it starts). A novice who worked at the month's opening is released from work (an immediate job change) and
    // starts a course this turn; this month's snapshot has them on the course and off work.
    [Fact] public void ACourseStartedThisTurnTakesTheTraineeOffWorkInThisMonthsSnapshot()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-6",
            assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var work = new Gdd03AssignmentFixture(c, s, new[] { new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 1, "fixture") });
        var opening = Month(s, 0);
        Assert.Contains(6, opening.WorkEligibleIds);

        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(6, null));
        f.Start(6, 4, CentreB, opening);
        var month = Month(s, 0);
        Assert.DoesNotContain(6, month.WorkEligibleIds);
        Assert.Contains(6, month.CourseEligibleIds);
        Assert.Equal(PopulationAssignmentKind.Course, month.OpeningPeople.Single(p => p.Id == 6).Assignment.Kind);
    }

    // R-TRAIN-005, R-TRAIN-008 (a reading of docs/gdd/cities.md:42, atomic reassignment, with docs/GDD.md:372 Q43(b)/(c)
    // and docs/gdd/training.md:66: what a person held at the month's opening is no bar to a course): a teacher who worked at the month's opening and was released afterwards starts
    // teaching that month, and the course counts that month.
    [Fact] public void ATeacherWhoWorkedAtTheOpeningStartsThatMonth()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-6",
            assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var opening = Month(s, 0);
        s.PopulationLedger.ChangeAssignment(6, PopulationAssignment.Unassigned);
        f.Start(5, 6, CentreB, opening);
        Assert.Equal(PopulationAssignmentKind.Teaching, s.PopulationLedger.Get(6).Assignment.Kind);
        f.Advance(5, opening);
        Assert.Equal(1, s.PopulationLedger.Get(5).Assignment.ProgressTicks);
    }

    // R-TRAIN-005 (docs/gdd/cities.md:42: "Reassignment releases old capacity atomically.", read with docs/GDD.md:372
    // Q43(b)/(c) and docs/gdd/training.md:66): a pupil and a teacher who both hold a job start a course; the jobs are released in the same commit,
    // so the work slots are free at once and this month's snapshot shows them on the course and off work.
    [Fact] public void PeopleWithAJobStartACourseAndLeaveItInTheSameCommit()
    {
        var c = Catalogue(); var s = State(c,
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-6", assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)),
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-7", assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Workshop)),
            Person(CivilianProfession.None, CivilianTier.None, null));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var work = new Gdd03AssignmentFixture(c, s, new[] {
            new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 1, "fixture"), new Gdd03FixtureSlot(Workshop, Gdd03FixtureWork.Workshop, 1, "fixture") });
        var opening = Month(s, 0);
        Assert.Contains(6, opening.WorkEligibleIds); Assert.Contains(7, opening.WorkEligibleIds);
        int count = s.PopulationLedger.Units.Count;

        f.Start(6, 7, CentreA, opening);
        var pupil = s.PopulationLedger.Get(6); var teacher = s.PopulationLedger.Get(7);
        Assert.Equal(PopulationStatus.Trainee, pupil.Status); Assert.Equal(CentreA, pupil.Assignment.Target); Assert.Equal(7, pupil.Assignment.TeacherId);
        Assert.Equal(PopulationAssignmentKind.Teaching, teacher.Assignment.Kind); Assert.Equal(CentreA, teacher.Assignment.Target);
        Assert.Equal(count, s.PopulationLedger.Units.Count);
        var month = Month(s, 0);
        Assert.DoesNotContain(6, month.WorkEligibleIds); Assert.DoesNotContain(7, month.WorkEligibleIds); Assert.Contains(6, month.CourseEligibleIds);
        // The released jobs' places are free at once: each one-place slot takes another worker.
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(8, Field));
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(8, Workshop));
        s.PopulationLedger.Reconcile();
    }

    // R-TRAIN-008, #1668 Q43(b): the ruling that lets a job-holder start does not reach people released by a graduation.
    // A graduate's teacher who takes a job in the graduation month still cannot start a course that month, and the
    // refusal leaves the job in place; from the next month the teacher can, leaving the job.
    [Fact] public void AGraduationReleasedTeacherWithAJobStillWaitsForTheNextMonth()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var work = new Gdd03AssignmentFixture(c, s, new[] { new Gdd03FixtureSlot(Field, Gdd03FixtureWork.Farm, 1, "fixture") });
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0); f.Advance(1, m0); f.Advance(1, Month(s, 1));
        var graduation = Month(s, 2);
        f.Advance(1, graduation);
        Assert.Equal(Gdd03FixtureAssignmentResult.Applied, work.Assign(0, Field));
        Unchanged(s, () => f.Start(5, 0, CentreB, graduation), typeof(ArgumentException), GraduationMonth);
        Assert.Equal(Field, l.Get(0).Assignment.Target);
        f.Start(5, 0, CentreB, Month(s, 3));
        Assert.Equal(PopulationAssignmentKind.Teaching, l.Get(0).Assignment.Kind);
    }

    // R-TRAIN-007, R-TRAIN-008, #1716 Q43(b): with no reception beds (Q41's bed reuse is post-MVP, docs/GDD.md:377), a
    // course started and cancelled in the same month leaves nothing held: another teacher and pupil, and then the first
    // pair again, start at the centre that month.
    [Fact] public void ACourseStartedAndCancelledThisMonthHoldsNothing()
    {
        var c = Catalogue(); var s = State(c, Refugee(), Refugee()); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var m0 = Month(s, 0);
        f.Start(1, 0, CentreA, m0);
        f.Cancel(1);
        f.Start(5, 4, CentreA, m0);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(5).Status);
        f.Cancel(5);
        f.Start(1, 0, CentreA, m0);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(1).Status);
    }

    // R-TRAIN-007, R-TRAIN-008, #1716 Q43(b), Q42: a place freed after the month's opening (here by a cancel) is free at
    // once, so another teacher and pupil take the centre that month.
    [Fact] public void AnotherTeacherTakesTheCentreInTheMonthACancelFreesIt()
    {
        var c = Catalogue(); var s = State(c, Refugee(), Refugee()); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        var m1 = Month(s, 1);
        f.Cancel(1);
        f.Start(5, 4, CentreA, m1);
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(5).Status);
        Assert.Equal(4, s.PopulationLedger.Get(5).Assignment.TeacherId);
    }

    // R-TRAIN-008: #1668 Q44 and the GDD's novice lock-in stay open: no free entrant becomes a novice here, and no
    // novice (cancelled or not) changes profession through a course.
    [Fact] public void LockInAndProfessionChangeStayDeferred()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.None, CivilianTier.None, null));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        Unchanged(s, () => f.Start(6, 0, CentreA, Month(s, 0)), typeof(NotSupportedException), "Deferred: #1668 Q44");
        Unchanged(s, () => f.Start(1, 2, CentreA, Month(s, 0)), typeof(NotSupportedException), "Deferred: #1668 Q44");
        f.Start(1, 0, CentreA, Month(s, 0)); f.Cancel(1);
        Unchanged(s, () => f.Start(1, 2, CentreA, Month(s, 1)), typeof(NotSupportedException), "Deferred: #1668 Q44");
    }

    // R-TRAIN-008: 27 Oct #1668 list question 9 stays open: no course turn or cancellation once the centre stops
    // operating.
    [Fact] public void CentreLossStaysDeferred()
    {
        var c = Catalogue(); var s = State(c);
        new Gdd03TrainingCentreFixture(c, s, Centres()).Start(1, 0, CentreA, Month(s, 0));
        var closed = new Gdd03TrainingCentreFixture(c, s, Centres(operating: false));
        Unchanged(s, () => closed.Advance(1, Month(s, 1)), typeof(NotSupportedException), "Deferred: #1668 27 Oct list Q9");
        Unchanged(s, () => closed.Cancel(1), typeof(NotSupportedException), "Deferred: #1668 27 Oct list Q9");
        var open = new Gdd03TrainingCentreFixture(c, s, Centres());
        Unchanged(s, () => open.Advance(1, Month(s, 1, centresAvailable: false)), typeof(NotSupportedException), "Deferred: #1668 27 Oct list Q9");
    }

    // R-TRAIN-005: exact gdd-0.3 only; legacy and gdd-0.1 states are refused and unchanged in the same run.
    [Fact] public void OtherIdentitiesAndUninitializedStatesAreRefused()
    {
        foreach (var identity in new[] { SaveGame.LegacyRuleset, SaveGame.GddRuleset })
        {
            var c = Catalogue(ruleset: identity); var s = new GameState(c); var before = s.Describe();
            Assert.Throws<InvalidOperationException>(() => new Gdd03TrainingCentreFixture(c, s, Centres()));
            Assert.Equal(before, s.Describe());
        }
        var g = Catalogue(); var empty = new GameState(g);
        Assert.Throws<InvalidOperationException>(() => new Gdd03TrainingCentreFixture(g, empty, Centres()));
    }

    // R-TRAIN-005, R-TRAIN-006: missing or Deferred catalogue values fail closed, with no legacy fallback.
    [Theory]
    [InlineData("profession.course_turns")]
    [InlineData("profession.trainees_per_teacher")]
    public void MissingOrDeferredValuesFailClosed(string key)
    {
        foreach (var replacement in new[] { null, Deferred() })
        {
            var c = Catalogue(key, replacement); var s = State(Catalogue());
            Assert.Throws<ContentException>(() => new Gdd03TrainingCentreFixture(c, s, Centres()));
        }
    }

    // R-TRAIN-005: centre inputs need an explicit id and host region, and ids are distinct. A centre whose stated region
    // is not where its teacher and pupil are bound is refused with the Q42 reason.
    [Fact] public void CentreInputsAreValidated()
    {
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureTrainingCentre("", "north", true));
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureTrainingCentre(CentreA, " ", true));
        var c = Catalogue(); var s = State(c);
        Assert.Throws<ArgumentException>(() => new Gdd03TrainingCentreFixture(c, s, new[] {
            new Gdd03FixtureTrainingCentre(CentreA, "north", true), new Gdd03FixtureTrainingCentre(CentreA, "south", true) }));
        new Gdd03TrainingCentreFixture(c, s, Centres()).Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => new Gdd03TrainingCentreFixture(c, s, new[] { new Gdd03FixtureTrainingCentre(CentreA, "south", true) }),
            typeof(ArgumentException), TeacherCity);
    }

    // R-TRAIN-006: deterministic and isolated: centre input order does not matter and a clone keeps its own course state.
    [Fact] public void OrderIndependentAndCloneIsolated()
    {
        var c = Catalogue(); var a = State(c); var b = State(c);
        var fa = new Gdd03TrainingCentreFixture(c, a, Centres());
        var fb = new Gdd03TrainingCentreFixture(c, b, Centres().OrderByDescending(x => x.Id, StringComparer.Ordinal));
        fa.Start(1, 0, CentreA, Month(a, 0)); fb.Start(1, 0, CentreA, Month(b, 0));
        fa.Start(3, 2, CentreB, Month(a, 0)); fb.Start(3, 2, CentreB, Month(b, 0));
        var clone = a.Clone(); var cloned = clone.Describe();
        fa.Advance(1, Month(a, 1)); fb.Advance(1, Month(b, 1));
        Assert.Equal(a.Describe(), b.Describe());
        Assert.Equal(cloned, clone.Describe());
        Assert.NotEqual(cloned, a.Describe());
    }

}
