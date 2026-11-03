using Statecraft.Sim;

// #1698 first slice (R-CRAFT-003): synthetic fixtures only. No real course, Industry slot, city, region or game caller is
// exercised. The 1 November 2026 answers quoted below are agent-drafted answers the owner approved (#1668,
// https://github.com/eyalgolan/statecraft-israel/pull/1668#issuecomment-6553884547).
public class Gdd03CraftCourseFixtureTests
{
    static readonly Lazy<Gdd03BalanceCatalogue> Real = new(() => ContentLoader.Load(RealContentTests.ContentRoot()).Gdd03Balance!);
    const string CraftA = "fixture-craft-a", CraftB = "fixture-craft-b", CraftSouth = "fixture-craft-south";
    const string Field = "fixture-field", Centre = "fixture-centre";
    static readonly string[] Facilities = { CraftA, CraftB, CraftSouth, Field, Centre };
    static readonly string[] Courses = { CraftA, CraftB, CraftSouth };

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
        new[] { new PopulationTarget(CraftA, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(CraftB, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(CraftSouth, PopulationAssignmentKind.Course, "south", 3),
                new PopulationTarget(Centre, PopulationAssignmentKind.Course, "north", 3),
                new PopulationTarget(Field, PopulationAssignmentKind.Work, "north") },
        Array.Empty<string>(), new[] { "basic" });

    static PopulationUnit Person(CivilianProfession profession, CivilianTier tier, string? tools,
        PopulationStatus status = PopulationStatus.FreeCivilian, int reception = 6, PopulationAssignment? assignment = null,
        string region = "north")
        => new(0, status, reception, new PopulationLocation(region), assignment ?? PopulationAssignment.Unassigned,
            profession, tier, profession == CivilianProfession.None ? 0 : 4, "basic", 2,
            new PopulationEquipment(tools, tools != null));

    // Ids follow the row order.
    const int Free = 0, NoviceFarmer = 1, NovicePioneer = 2, TrainedFarmer = 3, VeteranPioneer = 4, Apprentice = 5,
        Expert = 6, Worker = 7, RefugeeId = 8, NotReceived = 9, NoviceCraftsman = 10, SouthFree = 11;
    static GameState State(Content c, params PopulationUnit[] extra)
    {
        var people = new List<PopulationUnit> {
            Person(CivilianProfession.None, CivilianTier.None, null),
            Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-1"),
            Person(CivilianProfession.Pioneer, CivilianTier.Novice, "tools-2"),
            Person(CivilianProfession.Farmer, CivilianTier.Trained, "tools-3"),
            Person(CivilianProfession.Pioneer, CivilianTier.Veteran, "tools-4"),
            Person(CivilianProfession.Craftsman, CivilianTier.Apprentice, null),
            Person(CivilianProfession.Craftsman, CivilianTier.Expert, null),
            Person(CivilianProfession.None, CivilianTier.None, null, assignment: new PopulationAssignment(PopulationAssignmentKind.Work, Field)),
            Person(CivilianProfession.None, CivilianTier.None, null, PopulationStatus.Refugee, 2),
            Person(CivilianProfession.None, CivilianTier.None, null, reception: 3),
            Person(CivilianProfession.Craftsman, CivilianTier.Novice, null),
            Person(CivilianProfession.None, CivilianTier.None, null, region: "south") };
        people.AddRange(extra);
        return PopulationLedger.CreateFixture(c, Bindings(), people.Select((p, i) => new PopulationInventoryRow("synthetic", i, p)));
    }
    static Gdd03SyntheticMonthContext Month(GameState s, int ordinal, bool coursesAvailable = true)
        => Gdd03SyntheticMonthContext.CreateSynthetic(SaveGame.Gdd03Ruleset, ordinal, s.PopulationLedger.Units,
            Facilities.Select(f => new Gdd03SyntheticFacilitySnapshot(f, coursesAvailable || !f.Contains("craft", StringComparison.Ordinal), 1)),
            Array.Empty<int>(), Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());

    const string GraduationMonth = Gdd03TrainingCentreFixture.GraduationMonthReason;
    const string CourseAtOpening = Gdd03CraftCourseFixture.CourseAtOpeningReason;
    // The whole D3 reason is pinned: it must say the case is deferred and not ruled, and quote both sides unresolved.
    const string QualifiedDeferred = "Deferred: #1668 D3 (deferred, not ruled: a trained or veteran farmer or pioneer starting the craftsman course. The #1698 body, agent-recorded scope, says \"The course cannot replace another trained profession\"; docs/GDD.md:349 (D3) says \"a Train order can take a qualified civilian, who then loses the qualification (R-QUAL-006)\"; see R-QUAL-006. No answer is chosen here.)";
    const string CraftsmanDeferred = "Deferred: #1668 27M-5 (ruled: \"graduates may re-enrol at any time\", docs/gdd/training.md:66; not built here: no ruled text says which tier", SlotDeferred = "Deferred: #1668 Q52";

    static void Unchanged(GameState s, Action act, Type expected, string? messagePrefix = null)
    {
        var before = s.Describe();
        var ex = Assert.Throws(expected, act);
        if (messagePrefix != null) Assert.StartsWith(messagePrefix, ex.Message, StringComparison.Ordinal);
        else Assert.False(ex.Message.StartsWith("Deferred:", StringComparison.Ordinal), ex.Message);
        Assert.Equal(before, s.Describe());
    }
    static void AssertKept(PopulationUnit before, PopulationUnit after)
    {
        Assert.Equal(before.Id, after.Id); Assert.Equal(before.ReceptionTicks, after.ReceptionTicks);
        Assert.Same(before.Location, after.Location); Assert.Same(before.Equipment, after.Equipment);
        Assert.Equal(before.Qualification, after.Qualification); Assert.Equal(before.CombatXp, after.CombatXp);
        Assert.Equal(before.Captor, after.Captor); Assert.Equal(before.Home, after.Home);
    }

    // R-CRAFT-003, #1698 follow-up ("When the craftsman course starts"), 27M-3 ("Can a not-yet-qualified farmer or pioneer
    // start a craftsman course? | Yes (two-way professions, D3). The course replaces the novice profession, and paid tools
    // are not refunded."): a received free civilian, a novice farmer and a novice pioneer each start the course as a novice
    // craftsman under the same population id. Work XP is reset to 0 (a maintainer call: the course replaces the novice
    // profession); tools and their paid fact are kept. No person is created.
    [Theory]
    [InlineData(Free)]
    [InlineData(NoviceFarmer)]
    [InlineData(NovicePioneer)]
    public void CourseStartMakesANoviceCraftsmanWithTheSameId(int id)
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03CraftCourseFixture(c, s, Courses);
        var before = l.Get(id); int count = l.Units.Count, next = l.NextPopulationId;
        f.Start(id, CraftA, Month(s, 0));
        var p = l.Get(id);
        Assert.Equal(PopulationStatus.Trainee, p.Status);
        Assert.Equal(PopulationAssignmentKind.Course, p.Assignment.Kind); Assert.Equal(CraftA, p.Assignment.Target);
        Assert.Equal(0, p.Assignment.ProgressTicks); Assert.Null(p.Assignment.TeacherId);
        Assert.Equal(CivilianProfession.Craftsman, p.Profession); Assert.Equal(CivilianTier.Novice, p.Tier);
        Assert.Equal(0, p.WorkXp);
        AssertKept(before, p);
        Assert.Equal(before.Equipment.ToolsId != null, p.Equipment.ToolsPaid);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        l.Reconcile();
    }

    // R-CRAFT-003, a maintainer reading (not an owner ruling) of docs/gdd/cities.md:42, "Reassignment releases old capacity
    // atomically.", with docs/GDD.md:372 Q43(b)/(c), as #1697's second slice reads them: what a person held at the month's
    // opening is no bar, and a job held now is released in the same commit as the course start.
    [Fact] public void AWorkerStartsTheCourseAndLeavesTheJobInTheSameCommit()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03CraftCourseFixture(c, s, Courses);
        var opening = Month(s, 0);
        Assert.Contains(Worker, opening.WorkEligibleIds);
        var before = l.Get(Worker);
        f.Start(Worker, CraftA, opening);
        var p = l.Get(Worker);
        Assert.Equal(PopulationAssignmentKind.Course, p.Assignment.Kind); Assert.Equal(CivilianTier.Novice, p.Tier);
        AssertKept(before, p);
        var month = Month(s, 0);
        Assert.DoesNotContain(Worker, month.WorkEligibleIds); Assert.Contains(Worker, month.CourseEligibleIds);
        // Released from work this turn after working at the opening: also no bar.
        var s2 = State(c); var f2 = new Gdd03CraftCourseFixture(c, s2, Courses); var opening2 = Month(s2, 0);
        s2.PopulationLedger.ChangeAssignment(Worker, PopulationAssignment.Unassigned);
        f2.Start(Worker, CraftB, opening2);
        Assert.Equal(CraftB, s2.PopulationLedger.Get(Worker).Assignment.Target);
        s2.PopulationLedger.Reconcile();
    }

    // R-CRAFT-003: every other start is refused with the state unchanged. Plain refusals: a refugee, a free civilian not
    // yet received, an unknown course, a course outside the person's region, a person already on a course. Deferred, not
    // ruled (no answer chosen): a trained or veteran farmer or pioneer. The #1698 body (agent-recorded scope) says "The
    // course cannot replace another trained profession"; docs/GDD.md:349 (D3) says "a Train order can take a qualified
    // civilian, who then loses the qualification (R-QUAL-006)"; see R-QUAL-006. Deferred too: any craftsman. Re-enrolment is ruled
    // (docs/gdd/training.md:66, "graduates may re-enrol at any time"; 27M-5), but no ruled text says which tier a
    // craftsman holds during and after a new craftsman course, so that part stays Deferred with that reason.
    // R-CRAFT-003: every trained or veteran farmer or pioneer is refused with the pinned "deferred, not ruled" D3 reason and
    // the state unchanged (see the comment above IneligibleStartsAreRefusedWithoutMutation for the three sources).
    [Theory]
    [InlineData(CivilianProfession.Farmer, CivilianTier.Trained)]
    [InlineData(CivilianProfession.Farmer, CivilianTier.Veteran)]
    [InlineData(CivilianProfession.Pioneer, CivilianTier.Trained)]
    [InlineData(CivilianProfession.Pioneer, CivilianTier.Veteran)]
    public void QualifiedFarmersAndPioneersStayDeferredNotRuled(CivilianProfession profession, CivilianTier tier)
    {
        var c = Catalogue(); var s = State(c, Person(profession, tier, "tools-q"));
        var f = new Gdd03CraftCourseFixture(c, s, Courses);
        Unchanged(s, () => f.Start(12, CraftA, Month(s, 0)), typeof(NotSupportedException), QualifiedDeferred);
        Assert.Equal(QualifiedDeferred, Assert.Throws<NotSupportedException>(() => f.Start(12, CraftA, Month(s, 0))).Message);
    }

    [Fact] public void IneligibleStartsAreRefusedWithoutMutation()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03CraftCourseFixture(c, s, Courses); var m = Month(s, 0);
        Unchanged(s, () => f.Start(RefugeeId, CraftA, m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(NotReceived, CraftA, m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(Free, "fixture-unknown", m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(Free, Centre, m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(Free, CraftSouth, m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(SouthFree, CraftA, m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(99, CraftA, m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(Free, CraftA, null!), typeof(ArgumentNullException));
        Unchanged(s, () => f.Start(TrainedFarmer, CraftA, m), typeof(NotSupportedException), QualifiedDeferred);
        Unchanged(s, () => f.Start(VeteranPioneer, CraftA, m), typeof(NotSupportedException), QualifiedDeferred);
        Unchanged(s, () => f.Start(Apprentice, CraftA, m), typeof(NotSupportedException), CraftsmanDeferred);
        Unchanged(s, () => f.Start(Expert, CraftA, m), typeof(NotSupportedException), CraftsmanDeferred);
        Unchanged(s, () => f.Start(NoviceCraftsman, CraftA, m), typeof(NotSupportedException), CraftsmanDeferred);
        f.Start(Free, CraftA, m);
        Unchanged(s, () => f.Start(Free, CraftB, m), typeof(ArgumentException));
        Unchanged(s, () => f.Start(Free, CraftA, Month(s, 1)), typeof(ArgumentException));
    }

    // R-CRAFT-003, craft.course_turns (3), Q43(a) as in #2391 (docs/GDD.md:372: "A course occupies its people from its first
    // turn ... they are released in the turn its last course turn counts"): the enrolment month is course turn 1 and the third
    // counted turn makes an apprentice, released as a civilian without a job, with the same id and every other field.
    [Fact] public void ThirdCountedTurnMakesAnApprentice()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03CraftCourseFixture(c, s, Courses);
        var before = l.Get(NovicePioneer); int count = l.Units.Count, next = l.NextPopulationId;
        var m0 = Month(s, 0);
        f.Start(NovicePioneer, CraftA, m0);
        f.Advance(NovicePioneer, m0);
        Assert.Equal(1, l.Get(NovicePioneer).Assignment.ProgressTicks); Assert.Equal(CivilianTier.Novice, l.Get(NovicePioneer).Tier);
        f.Advance(NovicePioneer, Month(s, 1));
        Assert.Equal(2, l.Get(NovicePioneer).Assignment.ProgressTicks);
        Assert.Equal(PopulationStatus.Trainee, l.Get(NovicePioneer).Status); Assert.Equal(CivilianTier.Novice, l.Get(NovicePioneer).Tier);
        var graduation = Month(s, 2);
        f.Advance(NovicePioneer, graduation);
        var g = l.Get(NovicePioneer);
        Assert.Equal(PopulationStatus.FreeCivilian, g.Status); Assert.Same(PopulationAssignment.Unassigned, g.Assignment);
        Assert.Equal(CivilianProfession.Craftsman, g.Profession); Assert.Equal(CivilianTier.Apprentice, g.Tier);
        Assert.Equal(0, g.WorkXp);
        AssertKept(before, g); Assert.True(g.Equipment.ToolsPaid);
        // Q43(c): the graduation month's opening still shows the course, so work starts from the next month.
        Assert.Contains(NovicePioneer, graduation.CourseEligibleIds);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        l.Reconcile();
        Unchanged(s, () => f.Advance(NovicePioneer, Month(s, 3)), typeof(ArgumentException));
        Unchanged(s, () => f.Cancel(NovicePioneer), typeof(ArgumentException));
    }

    // R-CRAFT-003, Q43(b) as in #2391 (Gdd03TrainingCentreFixture.GraduationMonthReason): a person released by a graduation
    // cannot start a course in the graduation month, with any snapshot of it. From the next month an apprentice's
    // re-enrolment is ruled (27M-5) but its tiers are not, so it stays Deferred.
    [Fact] public void AGraduateCannotStartACourseInTheGraduationMonth()
    {
        var c = Catalogue(); var s = State(c); var f = new Gdd03CraftCourseFixture(c, s, Courses);
        var m0 = Month(s, 0);
        f.Start(Free, CraftA, m0); f.Advance(Free, m0); f.Advance(Free, Month(s, 1));
        var graduation = Month(s, 2);
        f.Advance(Free, graduation);
        Unchanged(s, () => f.Start(Free, CraftB, graduation), typeof(ArgumentException), GraduationMonth);
        Unchanged(s, () => f.Start(Free, CraftA, Month(s, 2)), typeof(ArgumentException), GraduationMonth);
        Unchanged(s, () => f.Start(Free, CraftB, Month(s, 1)), typeof(ArgumentException), GraduationMonth);
        Unchanged(s, () => f.Start(Free, CraftB, Month(s, 3)), typeof(NotSupportedException), CraftsmanDeferred);
    }

    // R-CRAFT-003, #1698 follow-up ("On cancelling before qualifying they are an ordinary civilian again."), 27M-4 ("It is
    // cancelled like CancelTraining, with no Money refund."), Q44 ("Tools are not refunded."): a cancel at any counted turn
    // before graduation gives profession none, tier none and work XP 0, keeps the id, tools and their paid fact, and
    // releases the person at once (Q43(b)), so they may start a full new course that month.
    [Theory]
    [InlineData(NoviceFarmer, 0)]
    [InlineData(NoviceFarmer, 1)]
    [InlineData(NoviceFarmer, 2)]
    [InlineData(Free, 1)]
    [InlineData(Worker, 2)]
    public void CancelBeforeQualifyingIsAnOrdinaryCivilianAgain(int id, int counted)
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03CraftCourseFixture(c, s, Courses);
        var before = l.Get(id); int count = l.Units.Count, next = l.NextPopulationId;
        f.Start(id, CraftA, Month(s, 0));
        for (int i = 0; i < counted; i++) f.Advance(id, Month(s, i));
        Assert.Equal(counted, l.Get(id).Assignment.ProgressTicks);
        f.Cancel(id);
        var p = l.Get(id);
        Assert.Equal(PopulationStatus.FreeCivilian, p.Status); Assert.Same(PopulationAssignment.Unassigned, p.Assignment);
        Assert.Equal(CivilianProfession.None, p.Profession); Assert.Equal(CivilianTier.None, p.Tier); Assert.Equal(0, p.WorkXp);
        AssertKept(before, p);
        Assert.Equal(before.Equipment.ToolsPaid, p.Equipment.ToolsPaid);
        Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
        l.Reconcile();
        Unchanged(s, () => f.Cancel(id), typeof(ArgumentException));
        Unchanged(s, () => f.Advance(id, Month(s, counted)), typeof(ArgumentException));
        // A restart is a full new course: its enrolment month counts as turn 1 again.
        int month = Math.Max(counted - 1, 0);
        f.Start(id, CraftB, Month(s, month));
        f.Advance(id, Month(s, month));
        Assert.Equal(1, l.Get(id).Assignment.ProgressTicks); Assert.Equal(CivilianTier.Novice, l.Get(id).Tier);
    }

    // R-CRAFT-003, #1698 Done when: "The course adds no mentor population unit (ledger count unchanged)." No teacher is read
    // or created at any step, and an inventory with a teaching record at a craftsman course is refused.
    [Fact] public void CourseAddsNoMentorRecord()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03CraftCourseFixture(c, s, Courses);
        int count = l.Units.Count, next = l.NextPopulationId; var ids = l.Units.Select(p => p.Id).ToArray();
        void Same()
        {
            Assert.Equal(count, l.Units.Count); Assert.Equal(next, l.NextPopulationId);
            Assert.Equal(ids, l.Units.Select(p => p.Id)); l.Reconcile();
            Assert.DoesNotContain(l.Units, p => p.Assignment.Kind == PopulationAssignmentKind.Teaching);
            Assert.DoesNotContain(l.Units, p => p.Assignment.TeacherId != null);
        }
        var m0 = Month(s, 0);
        f.Start(Free, CraftA, m0); Same();
        f.Start(NoviceFarmer, CraftA, m0); Same();
        f.Advance(Free, m0); f.Advance(NoviceFarmer, m0); Same();
        f.Cancel(NoviceFarmer); Same();
        f.Advance(Free, Month(s, 1)); f.Advance(Free, Month(s, 2)); Same();
        Assert.Equal(CivilianTier.Apprentice, l.Get(Free).Tier);

        foreach (var mentor in new[] {
            Person(CivilianProfession.Farmer, CivilianTier.Veteran, "tools-m", assignment: new PopulationAssignment(PopulationAssignmentKind.Teaching, CraftA)),
            Person(CivilianProfession.Craftsman, CivilianTier.Novice, null, assignment: new PopulationAssignment(PopulationAssignmentKind.Teaching, CraftA)) })
        {
            var withMentor = State(c, mentor);
            Unchanged(withMentor, () => new Gdd03CraftCourseFixture(c, withMentor, Courses), typeof(ArgumentException));
        }
    }

    // R-CRAFT-003: the Agreed course length is read from craft.course_turns, not a literal and not profession.course_turns.
    [Fact] public void CourseLengthComesFromTheCatalogue()
    {
        var c = Catalogue("craft.course_turns", Agreed(4)); var s = State(c); var l = s.PopulationLedger;
        var f = new Gdd03CraftCourseFixture(c, s, Courses);
        var m0 = Month(s, 0);
        f.Start(NoviceFarmer, CraftA, m0); f.Advance(NoviceFarmer, m0); f.Advance(NoviceFarmer, Month(s, 1)); f.Advance(NoviceFarmer, Month(s, 2));
        Assert.Equal(3, l.Get(NoviceFarmer).Assignment.ProgressTicks);
        Assert.Equal(CivilianTier.Novice, l.Get(NoviceFarmer).Tier); Assert.Equal(PopulationStatus.Trainee, l.Get(NoviceFarmer).Status);
        f.Advance(NoviceFarmer, Month(s, 3));
        Assert.Equal(CivilianTier.Apprentice, l.Get(NoviceFarmer).Tier);

        var c2 = Catalogue("profession.course_turns", Agreed(2)); var s2 = State(c2); var f2 = new Gdd03CraftCourseFixture(c2, s2, Courses);
        var n0 = Month(s2, 0);
        f2.Start(Free, CraftA, n0); f2.Advance(Free, n0); f2.Advance(Free, Month(s2, 1));
        Assert.Equal(CivilianTier.Novice, s2.PopulationLedger.Get(Free).Tier);
        f2.Advance(Free, Month(s2, 2));
        Assert.Equal(CivilianTier.Apprentice, s2.PopulationLedger.Get(Free).Tier);
    }

    // R-CRAFT-003, Q43(a) as in #2391: one month ordinal counts once, the enrolment month included; an earlier ordinal is
    // refused; a later month counts only when its opening already holds the course.
    [Fact] public void AMonthCountsOnceAndLaterMonthsNeedTheCourseAtTheirOpening()
    {
        var c = Catalogue("craft.course_turns", Agreed(4)); var s = State(c); var l = s.PopulationLedger;
        var f = new Gdd03CraftCourseFixture(c, s, Courses);
        var preStart = Month(s, 2);
        var m1 = Month(s, 1);
        f.Start(Free, CraftA, m1);
        Unchanged(s, () => f.Advance(Free, Month(s, 0)), typeof(ArgumentException));
        Unchanged(s, () => f.Advance(Free, preStart), typeof(ArgumentException), CourseAtOpening);
        f.Advance(Free, m1);
        Assert.Equal(1, l.Get(Free).Assignment.ProgressTicks);
        Unchanged(s, () => f.Advance(Free, m1), typeof(ArgumentException));
        Unchanged(s, () => f.Advance(Free, Month(s, 1)), typeof(ArgumentException));
        var m2 = Month(s, 2); f.Advance(Free, m2);
        Unchanged(s, () => f.Advance(Free, m2), typeof(ArgumentException));
        f.Advance(Free, Month(s, 4));
        Unchanged(s, () => f.Advance(Free, Month(s, 3)), typeof(ArgumentException));
        Assert.Equal(3, l.Get(Free).Assignment.ProgressTicks);
    }

    // R-CRAFT-003, Q52 ("An Industry course loses its slot ... | The course is cancelled like CancelTraining."): decided,
    // not built here (the Industry slot is #1692's). A course turn when the course is not available at the month's freeze is
    // refused Deferred with the state unchanged; the next available month counts as normal.
    [Fact] public void AnUnavailableCourseStaysDeferred()
    {
        var c = Catalogue(); var s = State(c); var l = s.PopulationLedger; var f = new Gdd03CraftCourseFixture(c, s, Courses);
        f.Start(Free, CraftA, Month(s, 0));
        Unchanged(s, () => f.Advance(Free, Month(s, 0, coursesAvailable: false)), typeof(NotSupportedException), SlotDeferred);
        Unchanged(s, () => f.Advance(Free, Month(s, 1, coursesAvailable: false)), typeof(NotSupportedException), SlotDeferred);
        f.Advance(Free, Month(s, 1));
        Assert.Equal(1, l.Get(Free).Assignment.ProgressTicks);
    }

    // R-CRAFT-003: exact gdd-0.3 only; legacy and gdd-0.1 states are refused and unchanged.
    [Fact] public void OtherIdentitiesAndUninitializedStatesAreRefused()
    {
        foreach (var identity in new[] { SaveGame.LegacyRuleset, SaveGame.GddRuleset })
        {
            var c = Catalogue(ruleset: identity); var s = new GameState(c); var before = s.Describe();
            Assert.Throws<InvalidOperationException>(() => new Gdd03CraftCourseFixture(c, s, Courses));
            Assert.Equal(before, s.Describe());
        }
        var g = Catalogue(); var empty = new GameState(g);
        Assert.Throws<InvalidOperationException>(() => new Gdd03CraftCourseFixture(g, empty, Courses));
    }

    // R-CRAFT-003: a missing or Deferred craft.course_turns fails closed, with no legacy fallback.
    [Fact] public void MissingOrDeferredCourseTurnsFailClosed()
    {
        foreach (var replacement in new[] { null, Deferred() })
        {
            var c = Catalogue("craft.course_turns", replacement); var s = State(Catalogue());
            Assert.Throws<ContentException>(() => new Gdd03CraftCourseFixture(c, s, Courses));
        }
    }

    // R-CRAFT-003: course ids are explicit and unique, and an inventory pupil at a craftsman course must be a novice
    // craftsman with no teacher and an unfinished course.
    [Fact] public void CourseInputsAreValidated()
    {
        var c = Catalogue(); var s = State(c);
        Assert.Throws<ArgumentException>(() => new Gdd03CraftCourseFixture(c, s, new[] { CraftA, " " }));
        Assert.Throws<ArgumentException>(() => new Gdd03CraftCourseFixture(c, s, new[] { CraftA, CraftA }));
        Assert.Throws<ArgumentNullException>(() => new Gdd03CraftCourseFixture(c, s, null!));
        var farmerPupil = Person(CivilianProfession.Farmer, CivilianTier.Novice, "tools-p", PopulationStatus.Trainee,
            assignment: new PopulationAssignment(PopulationAssignmentKind.Course, CraftA));
        var bad = State(c, farmerPupil);
        Assert.Throws<ArgumentException>(() => new Gdd03CraftCourseFixture(c, bad, Courses));
        // The same record at a course the fixture does not own is not its business.
        new Gdd03CraftCourseFixture(c, bad, new[] { CraftB });
        var pupil = Person(CivilianProfession.Craftsman, CivilianTier.Novice, null, PopulationStatus.Trainee,
            assignment: new PopulationAssignment(PopulationAssignmentKind.Course, CraftA, 2));
        var good = State(c, pupil); var f = new Gdd03CraftCourseFixture(c, good, Courses);
        // An inventory pupil not started by this fixture counts a month whose opening holds the course.
        f.Advance(12, Month(good, 5));
        Assert.Equal(CivilianTier.Apprentice, good.PopulationLedger.Get(12).Tier);
    }
}
