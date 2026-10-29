using Statecraft.Sim;

// #1697 first slice: synthetic fixtures only. No real centre, camp, region, host or game caller is exercised.
public class Gdd03TrainingCentreFixtureTests
{
    static readonly Lazy<Gdd03BalanceCatalogue> Real = new(() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!);
    const string CentreA = "fixture-centre-a", CampA = "fixture-camp-a", CentreB = "fixture-centre-b", CampB = "fixture-camp-b";
    const string CentreSouth = "fixture-centre-south", CampSouth = "fixture-camp-south", Field = "fixture-field";
    const string Service = "fixture-service";
    static readonly string[] Facilities = { CampA, CampB, CampSouth, CentreA, CentreB, CentreSouth, Field };

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

    static PopulationBindings Bindings() => new(
        new[] { new PopulationLocation("north"), new PopulationLocation("south") },
        new[] { new PopulationTarget(CampA, PopulationAssignmentKind.Reception, "north"),
                new PopulationTarget(CampB, PopulationAssignmentKind.Reception, "north"),
                new PopulationTarget(CampSouth, PopulationAssignmentKind.Reception, "south"),
                new PopulationTarget(CentreA, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(CentreB, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(CentreSouth, PopulationAssignmentKind.Course, "south", 3),
                new PopulationTarget(Field, PopulationAssignmentKind.Work, "north"),
                new PopulationTarget(Service, PopulationAssignmentKind.ActiveService, "north") },
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
    static GameState State(Content c, params PopulationUnit[] extra)
    {
        var people = new List<PopulationUnit> {
            Teacher(CivilianProfession.Farmer, "tools-0"), Novice(CivilianProfession.Farmer, "tools-1"),
            Teacher(CivilianProfession.Pioneer, "tools-2"), Novice(CivilianProfession.Pioneer, "tools-3"),
            Teacher(CivilianProfession.Farmer, "tools-4"), Novice(CivilianProfession.Farmer, "tools-5") };
        people.AddRange(extra);
        return PopulationLedger.CreateFixture(c, Bindings(), people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)));
    }
    static Gdd03FixtureTrainingCentre[] Centres(bool operating = true, bool upgraded = true) => new[] {
        new Gdd03FixtureTrainingCentre(CentreA, CampA, operating, upgraded),
        new Gdd03FixtureTrainingCentre(CentreB, CampB, true, true),
        new Gdd03FixtureTrainingCentre(CentreSouth, CampSouth, true, true) };
    static Gdd03SyntheticMonthContext Month(GameState s, int ordinal, bool centresAvailable = true)
        => Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, ordinal, s.PopulationLedger.Units,
            Facilities.Select(f => new Gdd03SyntheticFacilitySnapshot(f, centresAvailable || !f.Contains("centre", StringComparison.Ordinal), 1)),
            Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());
    static (int teacher, int trainee) Pair(CivilianProfession p) => p == CivilianProfession.Farmer ? (0, 1) : (2, 3);

    static void Unchanged(GameState s, Action act, Type expected, string? deferredPrefix = null)
    {
        var before = s.Describe();
        var ex = Assert.Throws(expected, act);
        if (deferredPrefix != null) Assert.StartsWith(deferredPrefix, ex.Message, StringComparison.Ordinal);
        else Assert.False(ex.Message.StartsWith("Deferred:", StringComparison.Ordinal), ex.Message);
        Assert.Equal(before, s.Describe());
    }

    // R-TRAIN-005, R-TRAIN-006, R-TRAIN-008: a matching equipped novice and veteran teacher start a course; a counted course
    // turn keeps the same id and every record field, creates no person and earns no work XP. The turn that would bring the
    // course within one turn of its end (the first one where the two answers to #1668 Q43(a) differ) is Deferred.
    [Theory]
    [InlineData(CivilianProfession.Farmer)]
    [InlineData(CivilianProfession.Pioneer)]
    public void MatchingNoviceCountsTurnsUntilGraduationTimingIsDeferred(CivilianProfession profession)
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var (teacher, trainee) = Pair(profession); var l = s.PopulationLedger;
        var before = l.Get(trainee); var teacherBefore = l.Get(teacher);
        int count = l.Units.Count, next = l.NextPopulationId; var people = l.TotalPeople;

        f.Start(trainee, teacher, CentreA, Month(s, 0));
        var p = l.Get(trainee);
        Assert.Equal(PopulationStatus.Trainee, p.Status);
        Assert.Equal(PopulationAssignmentKind.Course, p.Assignment.Kind);
        Assert.Equal(CentreA, p.Assignment.Target); Assert.Equal(0, p.Assignment.ProgressTicks); Assert.Equal(teacher, p.Assignment.TeacherId);
        Assert.Equal(PopulationAssignmentKind.Teaching, l.Get(teacher).Assignment.Kind);
        Assert.Equal(CentreA, l.Get(teacher).Assignment.Target);

        f.Advance(trainee, Month(s, 1));
        var after = l.Get(trainee); var teacherAfter = l.Get(teacher);
        Assert.Equal(1, after.Assignment.ProgressTicks); Assert.Equal(PopulationStatus.Trainee, after.Status);
        Assert.Equal(CivilianTier.Novice, after.Tier); Assert.Equal(profession, after.Profession);
        Assert.Equal(before.WorkXp, after.WorkXp); Assert.Equal(before.ReceptionTicks, after.ReceptionTicks);
        Assert.Same(before.Equipment, after.Equipment); Assert.Same(before.Location, after.Location);
        Assert.Equal(before.CombatXp, after.CombatXp); Assert.Equal(before.Qualification, after.Qualification);
        Assert.Equal(CivilianTier.Veteran, teacherAfter.Tier); Assert.Equal(teacherBefore.WorkXp, teacherAfter.WorkXp);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId); Assert.Equal(people, l.TotalPeople);

        Unchanged(s, () => f.Advance(trainee, Month(s, 2)), typeof(NotSupportedException), "Deferred: #1668 Q43");
        Assert.Equal(CivilianTier.Novice, l.Get(trainee).Tier);
        l.Reconcile();
    }

    // R-TRAIN-006: the Agreed course length is read from the catalogue, not a literal: with a synthetic 4, two turns
    // count before the Deferred graduation-timing turn.
    [Fact] public void CourseLengthComesFromTheCatalogue()
    {
        var c = Catalogue("profession.course_turns", Agreed(4)); var s = State(c);
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        f.Advance(1, Month(s, 1)); f.Advance(1, Month(s, 2));
        Assert.Equal(2, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, Month(s, 3)), typeof(NotSupportedException), "Deferred: #1668 Q43");
    }

    // R-TRAIN-006: one month counts once; a stale or repeated snapshot cannot add a second course turn.
    [Fact] public void AMonthCountsOnceForACourse()
    {
        var c = Catalogue("profession.course_turns", Agreed(5)); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        var m1 = Month(s, 1); f.Advance(1, m1);
        Assert.Equal(1, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(1, m1), typeof(ArgumentException));
        // A fresh snapshot with an ordinal that was already counted, or an earlier one, is refused too.
        Unchanged(s, () => f.Advance(1, Month(s, 1)), typeof(ArgumentException));
        f.Advance(1, Month(s, 3));
        Unchanged(s, () => f.Advance(1, Month(s, 2)), typeof(ArgumentException));
        Assert.Equal(2, s.PopulationLedger.Get(1).Assignment.ProgressTicks);
    }

    // R-TRAIN-008: a snapshot taken after enrolment but carrying the enrolment month's ordinal is still the enrolment
    // month, so it is not counted (#1668 Q43(a)).
    [Fact] public void TheEnrolmentOrdinalIsNeverCounted()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => f.Advance(1, Month(s, 0)), typeof(NotSupportedException), "Deferred: #1668 Q43");
    }

    // R-TRAIN-005: a busy teacher refuses a second pupil (Agreed profession.trainees_per_teacher = 1).
    [Fact] public void BusyTeacherRefusesASecondPupil()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => f.Start(5, 0, CentreA, Month(s, 1)), typeof(ArgumentException));
        Unchanged(s, () => f.Start(5, 0, CentreB, Month(s, 1)), typeof(ArgumentException));
    }

    // R-TRAIN-005: the pupil limit is the catalogue value; a synthetic 2 admits a second pupil for the same teacher.
    // This only proves the value is read from the catalogue: the GDD allows one pupil per teacher (training.md:31).
    [Fact] public void PupilLimitComesFromTheCatalogue()
    {
        var c = Catalogue("profession.trainees_per_teacher", Agreed(2)); var s = State(c);
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        f.Start(5, 0, CentreA, Month(s, 0));
        Assert.Equal(0, s.PopulationLedger.Get(5).Assignment.TeacherId);
        f.Cancel(1);
        Assert.Equal(PopulationAssignmentKind.Teaching, s.PopulationLedger.Get(0).Assignment.Kind);
        f.Cancel(5);
        Assert.Same(PopulationAssignment.Unassigned, s.PopulationLedger.Get(0).Assignment);
    }

    // R-TRAIN-005: the trainee needs a free bed of the centre's own reception camp (Agreed reception.capacity = 3);
    // another camp's beds or ordinary work places do not stand in, and the teacher's housing is not a reception bed.
    [Fact] public void TraineeNeedsTheParentCampsOwnReceptionBed()
    {
        var c = Catalogue();
        var full = State(c, Refugee(), Refugee(), Refugee());
        var f = new Gdd03TrainingCentreFixture(c, full, Centres());
        Unchanged(full, () => f.Start(1, 0, CentreA, Month(full, 0)), typeof(ArgumentException));

        var twoTaken = State(c, Refugee(), Refugee(), Refugee(CampB));
        var g = new Gdd03TrainingCentreFixture(c, twoTaken, Centres());
        g.Start(1, 0, CentreA, Month(twoTaken, 0)); // 2 refugees + 1 trainee; the teacher takes no bed
        Assert.Equal(PopulationStatus.Trainee, twoTaken.PopulationLedger.Get(1).Status);
        g.Start(3, 2, CentreB, Month(twoTaken, 0)); // camp B still has two free beds
        Assert.Equal(PopulationStatus.Trainee, twoTaken.PopulationLedger.Get(3).Status);
    }

    // R-TRAIN-005: settled refusals leave the state unchanged and are not reported as Deferred.
    public static IEnumerable<object[]> SettledRefusals() => new[] {
        new object[] { "trained trainee", 0, 6, CentreA, true, true },
        new object[] { "unequipped novice", 0, 7, CentreA, true, true },
        new object[] { "refugee before reception ends", 0, 8, CentreA, true, true },
        new object[] { "trainee already working", 0, 9, CentreA, true, true },
        new object[] { "craftsman", 0, 10, CentreA, true, true },
        new object[] { "active soldier", 0, 11, CentreA, true, true },
        new object[] { "trained teacher", 12, 1, CentreA, true, true },
        new object[] { "teacher busy elsewhere", 13, 1, CentreA, true, true },
        new object[] { "same person", 1, 1, CentreA, true, true },
        new object[] { "unknown centre", 0, 1, "fixture-unknown", true, true },
        new object[] { "parent not operating", 0, 1, CentreA, false, true },
        new object[] { "upgrade incomplete", 0, 1, CentreA, true, false },
        // The two region rows are refused by the population ledger's own binding checks, not by a #1697 rule.
        new object[] { "other region", 0, 1, CentreSouth, true, true },
        new object[] { "trainee in another region", 0, 14, CentreA, true, true },
    };
    [Theory, MemberData(nameof(SettledRefusals))]
    public void IneligibleStartsAreRefusedWithoutMutation(string why, int teacher, int trainee, string centre, bool operating, bool upgraded)
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
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-13", assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)), // 13
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-14", region: "south"));      // 14
        var f = new Gdd03TrainingCentreFixture(c, s, Centres(operating, upgraded));
        Unchanged(s, () => f.Start(trainee, teacher, centre, Month(s, 0)), typeof(ArgumentException));
    }

    // R-TRAIN-007: cancel at every course turn this slice can reach keeps id, tools, payment fact and reception credit;
    // grants nothing unfinished; clears progress; releases the teacher and the bed; moves no one.
    [Theory]
    [InlineData(CivilianProfession.Farmer, 0)]
    [InlineData(CivilianProfession.Farmer, 1)]
    [InlineData(CivilianProfession.Pioneer, 0)]
    [InlineData(CivilianProfession.Pioneer, 1)]
    public void CancelAtEveryTurnRetainsPaidStateAndReleasesTeacherAndBed(CivilianProfession profession, int counted)
    {
        var c = Catalogue(); var s = State(c, Refugee(), Refugee()); var l = s.PopulationLedger;
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
        // Teacher and bed are free again: the camp's last bed takes a new pupil with another teacher.
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
        Unchanged(s, () => f.Advance(1, Month(s, 4)), typeof(NotSupportedException), "Deferred: #1668 Q43");
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

    // R-TRAIN-008: #1668 Q42 stays open: a second teacher at one centre, or an idle teacher kept at a centre, is refused.
    [Fact] public void TeachersPerCentreAndIdleTeacherHousingStayDeferred()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        Unchanged(s, () => f.Start(5, 4, CentreA, Month(s, 0)), typeof(NotSupportedException), "Deferred: #1668 Q42");

        var idle = State(c, new PopulationUnit(0, PopulationStatus.FreeCivilian, 6, new PopulationLocation("north"),
            new PopulationAssignment(PopulationAssignmentKind.Teaching, CentreB), CivilianProfession.Farmer, CivilianTier.Veteran,
            4, "", 2, new PopulationEquipment("tools-6", true)));
        Unchanged(idle, () => new Gdd03TrainingCentreFixture(c, idle, Centres()), typeof(NotSupportedException), "Deferred: #1668 Q42");
    }

    // R-TRAIN-008: #1668 Q43 stays open: the enrolment month is not counted, and a participant who held another
    // assignment at the month's opening cannot start that month.
    [Fact] public void CourseTurnBoundariesStayDeferred()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-6",
            assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var enrolment = Month(s, 0);
        f.Start(1, 0, CentreA, enrolment);
        Unchanged(s, () => f.Advance(1, enrolment), typeof(NotSupportedException), "Deferred: #1668 Q43");

        var opening = Month(s, 1);
        s.PopulationLedger.ChangeAssignment(6, PopulationAssignment.Unassigned);
        Unchanged(s, () => f.Start(6, 4, CentreB, opening), typeof(NotSupportedException), "Deferred: #1668 Q43");

        f.Cancel(1);
        Unchanged(s, () => f.Start(1, 0, CentreA, opening), typeof(NotSupportedException), "Deferred: #1668 Q43");
        // A teacher released by that cancel cannot teach at another centre in the same month.
        Unchanged(s, () => f.Start(5, 0, CentreB, opening), typeof(NotSupportedException), "Deferred: #1668 Q43");
    }

    // R-TRAIN-008: #1668 Q43(b): a teacher who worked at the month's opening and was released afterwards cannot start
    // teaching that month.
    [Fact] public void TeacherWhoWorkedAtTheOpeningCannotStartThatMonth()
    {
        var c = Catalogue(); var s = State(c, Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-6",
            assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)));
        var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        var opening = Month(s, 0);
        s.PopulationLedger.ChangeAssignment(6, PopulationAssignment.Unassigned);
        Unchanged(s, () => f.Start(5, 6, CentreB, opening), typeof(NotSupportedException), "Deferred: #1668 Q43");
    }

    // R-TRAIN-008: #1668 Q43(b) and Q41: a bed freed after the month's opening (here by a cancel) is not reused that month.
    [Fact] public void ABedFreedAfterTheOpeningIsNotReusedThatMonth()
    {
        var c = Catalogue(); var s = State(c, Refugee(), Refugee()); var f = new Gdd03TrainingCentreFixture(c, s, Centres());
        f.Start(1, 0, CentreA, Month(s, 0));
        var m1 = Month(s, 1);
        f.Cancel(1);
        Unchanged(s, () => f.Start(5, 4, CentreA, m1), typeof(NotSupportedException), "Deferred: #1668 Q43");
        f.Start(5, 4, CentreA, Month(s, 2));
        Assert.Equal(PopulationStatus.Trainee, s.PopulationLedger.Get(5).Status);
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

    // R-TRAIN-008: 27 Oct #1668 list question 9 stays open: no course turn or cancellation once the parent camp or
    // centre stops operating.
    [Fact] public void CampLossStaysDeferred()
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
    [InlineData("reception.capacity")]
    public void MissingOrDeferredValuesFailClosed(string key)
    {
        foreach (var replacement in new[] { null, Deferred() })
        {
            var c = Catalogue(key, replacement); var s = State(Catalogue());
            Assert.Throws<ContentException>(() => new Gdd03TrainingCentreFixture(c, s, Centres()));
        }
    }

    // R-TRAIN-005: centre inputs need explicit distinct ids; one centre upgrade per parent camp in this fixture.
    [Fact] public void CentreInputsAreValidated()
    {
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureTrainingCentre("", CampA, true, true));
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureTrainingCentre(CentreA, " ", true, true));
        Assert.Throws<ArgumentException>(() => new Gdd03FixtureTrainingCentre(CampA, CampA, true, true));
        var c = Catalogue(); var s = State(c);
        Assert.Throws<ArgumentException>(() => new Gdd03TrainingCentreFixture(c, s, new[] {
            new Gdd03FixtureTrainingCentre(CentreA, CampA, true, true), new Gdd03FixtureTrainingCentre(CentreB, CampA, true, true) }));
        Assert.Throws<ArgumentException>(() => new Gdd03TrainingCentreFixture(c, s, new[] {
            new Gdd03FixtureTrainingCentre(CentreA, CampA, true, true), new Gdd03FixtureTrainingCentre(CentreA, CampB, true, true) }));
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

    // R-TRAIN-005: the initialized ledger owns completion; consistent content is required before any course action.
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void ReceptionLimitMustMatchTheInitializedLedger(int delta)
    {
        var c = Catalogue(); var state = State(c); var before = state.Describe();
        c.Gdd03Balance!.Values["reception.turns"] = Agreed(6 + delta);
        var error = Assert.Throws<ContentException>(() => new Gdd03TrainingCentreFixture(c, state, Centres()));
        Assert.Equal(Gdd03BalanceCatalogue.FileName, error.File);
        Assert.Equal("values.reception.turns", error.Field);
        Assert.Equal(before, state.Describe());
    }
}
