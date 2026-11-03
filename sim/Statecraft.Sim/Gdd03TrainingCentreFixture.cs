using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Synthetic centre facts supplied by a test. Operating state and upgrade completion are owned by
    /// #1696 (reception) and #1700/#1713 (construction); this fixture only reads them. No real camp, host or region.</summary>
    internal sealed class Gdd03FixtureTrainingCentre
    {
        internal string Id { get; }
        internal string ParentCamp { get; }
        internal bool ParentOperating { get; }
        internal bool UpgradeComplete { get; }
        internal Gdd03FixtureTrainingCentre(string id, string parentCamp, bool parentOperating, bool upgradeComplete)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(parentCamp) || string.Equals(id, parentCamp, StringComparison.Ordinal))
                throw new ArgumentException("A centre needs its own course target id and a distinct parent reception camp id.");
            PopulationText.ValidateText(id); PopulationText.ValidateText(parentCamp);
            Id = id; ParentCamp = parentCamp; ParentOperating = parentOperating; UpgradeComplete = upgradeComplete;
        }
    }

    /// <summary>R-TRAIN-005 to R-TRAIN-008: gdd-0.3 farmer/pioneer course lifecycle over the #1689 population ledger,
    /// synthetic fixtures only. No command, save, loader, resolver, Money, Food or UI caller. GDD 0.3 (docs/GDD.md, 1 November
    /// 2026 answers): "A course occupies its people from its first turn (Q43(a)); they are released in the turn its last
    /// course turn counts and can be assigned from the next planning, or at once on cancellation, and work starts the turn
    /// after (Q43(b), Q43(c))." So the enrolment month is course turn 1 and the last counted turn graduates. Cases this
    /// slice has not built (#1668 Q42, the rest of Q43 and Q44, decided in the GDD's 1 November 2026 answers, so these
    /// refusals are implementation gaps, not open design answers; and the 27 Oct list Q9) are refused with a "Deferred:"
    /// NotSupportedException.</summary>
    internal sealed class Gdd03TrainingCentreFixture
    {
        readonly Content content;
        readonly GameState state;
        readonly SortedDictionary<string, Gdd03FixtureTrainingCentre> centres;
        readonly int courseTurns, pupilsPerTeacher, bedsPerCamp, receptionTurns;
        // Per fixture instance only (not saved): month ordinals of each pupil's enrolment and last counted course turn
        // (-1 before the first), and of the month in which a graduation released each person.
        readonly SortedDictionary<int, (int enrolment, int lastCounted)> months = new SortedDictionary<int, (int, int)>();
        readonly SortedDictionary<int, int> graduated = new SortedDictionary<int, int>();
        // Per fixture instance only: the month ordinal and parent camp of every course started, kept after a cancel or a
        // graduation, so a bed a course took this month is not reused that month (R-TRAIN-008).
        readonly List<(int month, string camp)> starts = new List<(int, string)>();

        internal Gdd03TrainingCentreFixture(Content content, GameState state, IEnumerable<Gdd03FixtureTrainingCentre> centres)
        {
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Training centre courses require an initialized gdd-0.3 synthetic fixture.");
            this.content = content; this.state = state;
            courseTurns = content.RequirePopulationInteger("profession.course_turns");
            pupilsPerTeacher = content.RequirePopulationInteger("profession.trainees_per_teacher");
            bedsPerCamp = content.RequirePopulationInteger("reception.capacity");
            receptionTurns = content.RequirePopulationInteger("reception.turns");
            if (receptionTurns != state.PopulationLedger.ReceptionLimit)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.reception.turns",
                    "must match the initialized population ledger");
            this.centres = new SortedDictionary<string, Gdd03FixtureTrainingCentre>(StringComparer.Ordinal);
            var parents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var centre in centres)
            {
                this.centres.Add(centre.Id, centre); // a duplicate id throws ArgumentException
                if (!parents.Add(centre.ParentCamp)) throw new ArgumentException("This fixture supports one centre upgrade per parent camp.");
            }
            ValidateInventory();
        }

        static NotSupportedException Deferred(string question) => new NotSupportedException("Deferred: " + question);
        const string Q42 = "#1668 Q42 (teachers per centre and an idle centre's teacher housing).";
        // Q43 is decided (GDD, 1 November 2026 answers). The one Q43 case left Deferred is not built here: a person who held
        // another assignment at the month's opening (other than a pupil released from work, or a cancelled course or teaching)
        // starting a course that month. The ruled Q43 refusals below are plain ArgumentExceptions citing the ruling.
        const string Q43 = "#1668 Q43 (decided, not built here: starting a course after holding another assignment at the month's opening).";
        internal const string BedWaitsReason = "No reception bed was free at the month's opening: a bed freed this month is reused from the next month (#1668 Q43(b) with Q41, GDD 1 Nov 2026).";
        internal const string GraduationMonthReason = "A person released by a graduation can start a course from the next month (#1668 Q43(b), GDD 1 Nov 2026).";
        internal const string CourseAtOpeningReason = "After the enrolment month, a course turn counts only when the month's opening holds the course with the same teacher (#1668 Q43(a), GDD 1 Nov 2026).";
        const string Q44 = "#1668 Q44 and the GDD novice lock-in decision (when a free entrant becomes a novice; profession change after a cancelled course).";
        const string Q9 = "#1668 27 Oct list Q9 (return after cancellation or camp loss).";

        bool Received(PopulationUnit p) => p.Status == PopulationStatus.FreeCivilian && p.ReceptionTicks == receptionTurns;
        static bool FarmerOrPioneer(CivilianProfession p) => p == CivilianProfession.Farmer || p == CivilianProfession.Pioneer;
        IEnumerable<PopulationUnit> At(PopulationAssignmentKind kind, string target)
            => state.PopulationLedger.Units.Where(p => p.Assignment.Kind == kind && string.Equals(p.Assignment.Target, target, StringComparison.Ordinal));
        int Pupils(int teacher) => state.PopulationLedger.Units.Count(p => p.Assignment.Kind == PopulationAssignmentKind.Course
            && p.Assignment.TeacherId == teacher && centres.ContainsKey(p.Assignment.Target!));
        // Agreed reception.capacity counts refugee and course-trainee beds per camp; the teacher's housing is separate.
        int BedsTaken(Gdd03FixtureTrainingCentre centre)
            => At(PopulationAssignmentKind.Reception, centre.ParentCamp).Count() + At(PopulationAssignmentKind.Course, centre.Id).Count();

        void ValidateInventory()
        {
            state.PopulationLedger.Reconcile();
            foreach (var centre in centres.Values)
            {
                var teachers = At(PopulationAssignmentKind.Teaching, centre.Id).ToArray();
                if (teachers.Length > 1) throw Deferred(Q42);
                foreach (var t in teachers)
                {
                    int n = Pupils(t.Id);
                    if (n == 0) throw Deferred(Q42);
                    if (n > pupilsPerTeacher || !FarmerOrPioneer(t.Profession) || t.Tier != CivilianTier.Veteran)
                        throw new ArgumentException("Centre teacher must be a matching veteran within the pupil limit.");
                }
                foreach (var p in At(PopulationAssignmentKind.Course, centre.Id))
                    if (p.Assignment.TeacherId is not int teacher || state.PopulationLedger.Get(teacher).Profession != p.Profession
                        || p.Tier != CivilianTier.Novice || p.Assignment.ProgressTicks >= courseTurns)
                        throw new ArgumentException("Centre pupil must be a matching novice with a teacher and an unfinished course.");
                if (BedsTaken(centre) > bedsPerCamp) throw new ArgumentException("Parent camp reception beds are over capacity.");
            }
        }

        Gdd03FixtureTrainingCentre CentreOf(PopulationUnit trainee)
        {
            if (trainee.Status != PopulationStatus.Trainee || trainee.Assignment.Kind != PopulationAssignmentKind.Course
                || trainee.Assignment.Target == null || !centres.TryGetValue(trainee.Assignment.Target, out var centre))
                throw new ArgumentException("Not a pupil of a fixture training centre course.");
            return centre;
        }

        static PopulationUnit Opening(Gdd03SyntheticMonthContext month, int id)
            => (month ?? throw new ArgumentNullException(nameof(month))).OpeningPeople.FirstOrDefault(p => p.Id == id)
               ?? throw new ArgumentException("Participant is not in the month's opening snapshot.");

        // Every check runs before the first write; Replace validates each record and a failure restores earlier writes.
        void Commit(params PopulationUnit[] replacements)
        {
            var ledger = state.PopulationLedger; var previous = new List<PopulationUnit>();
            try { foreach (var r in replacements) { var old = ledger.Get(r.Id); ledger.Replace(r); previous.Add(old); } }
            catch { for (int i = previous.Count - 1; i >= 0; i--) ledger.Replace(previous[i]); throw; }
        }
        static PopulationUnit Released(PopulationUnit p)
            => p.WithPlacement(PopulationStatus.FreeCivilian, p.Location, PopulationAssignment.Unassigned, p.Captor, p.Home);

        /// <summary>R-TRAIN-005: enrol a matching equipped novice with a free matching veteran teacher at an operating,
        /// upgraded centre that has a free bed in its own parent camp. No payment: the tools were paid on entry.</summary>
        internal void Start(int traineeId, int teacherId, string centreId, Gdd03SyntheticMonthContext month)
        {
            ValidateInventory();
            if (centreId == null || !centres.TryGetValue(centreId, out var centre)) throw new ArgumentException("Unknown fixture centre.");
            if (!centre.ParentOperating || !centre.UpgradeComplete)
                throw new ArgumentException("A course needs an operating parent camp and a completed centre upgrade.");
            if (traineeId == teacherId) throw new ArgumentException("Teacher and trainee must be different population units.");
            var ledger = state.PopulationLedger; var trainee = ledger.Get(traineeId); var teacher = ledger.Get(teacherId);

            bool teachingHere = teacher.Assignment.Kind == PopulationAssignmentKind.Teaching
                && string.Equals(teacher.Assignment.Target, centre.Id, StringComparison.Ordinal);
            if (!Received(teacher) || !FarmerOrPioneer(teacher.Profession) || teacher.Tier != CivilianTier.Veteran)
                throw new ArgumentException("The teacher must be a received matching veteran farmer or pioneer.");
            if (teachingHere ? Pupils(teacher.Id) >= pupilsPerTeacher : teacher.Assignment.Kind != PopulationAssignmentKind.Unassigned)
                throw new ArgumentException("The teacher is occupied and refuses another pupil.");

            if (!Received(trainee) || trainee.Assignment.Kind != PopulationAssignmentKind.Unassigned)
                throw new ArgumentException("The trainee must be a received, unassigned free civilian.");
            if (trainee.Profession == CivilianProfession.Craftsman)
                throw new ArgumentException("A course cannot replace another civilian profession.");
            if (trainee.Profession != CivilianProfession.None && trainee.Tier != CivilianTier.Novice)
                throw new ArgumentException("Only a novice can gain the trained tier from a course.");
            if (trainee.Profession != CivilianProfession.None && trainee.Equipment.ToolsId == null)
                throw new ArgumentException("A novice needs matching tools to enrol.");
            if (trainee.Profession != teacher.Profession) throw Deferred(Q44);

            if (At(PopulationAssignmentKind.Teaching, centre.Id).Any(t => t.Id != teacher.Id)) throw Deferred(Q42);
            if (BedsTaken(centre) >= bedsPerCamp) throw new ArgumentException("No free reception bed in the centre's parent camp.");

            var traineeOpening = Opening(month, trainee.Id); var teacherOpening = Opening(month, teacher.Id);
            // #1668 Q43(b) (#1697, 1 Nov 2026): "on graduation the people are released in the turn the last course turn
            // counts and can be assigned from the next turn's planning", so neither starts a course in that month.
            if (graduated.TryGetValue(trainee.Id, out var g) && month.MonthOrdinal <= g
                || graduated.TryGetValue(teacher.Id, out g) && month.MonthOrdinal <= g)
                throw new ArgumentException(GraduationMonthReason);
            // #1716 month snapshot (owner-relayed Q43(a) and Q34, 28 Oct 2026): a course occupies the pupil from the turn it
            // starts and a job change is immediate, so a pupil who worked at the opening and was released this turn may start,
            // and this month's work/course snapshot shows the course. #1716 Q43(b) release (owner-approved answer, 1 Nov 2026,
            // #1668): "Cancellation: they are released at once, as civilians without a job." A pupil who held a course, or a
            // teacher who taught, at the opening and is free now was released by a cancel this turn (only Cancel ends those
            // exclusive assignments here), so either may start again this month. A teacher who worked at the opening and was
            // released this turn is not built here and stays Deferred.
            if ((traineeOpening.Assignment.Kind != PopulationAssignmentKind.Unassigned
                    && traineeOpening.Assignment.Kind != PopulationAssignmentKind.Work
                    && traineeOpening.Assignment.Kind != PopulationAssignmentKind.Course)
                || (teacherOpening.Assignment.Kind != PopulationAssignmentKind.Unassigned
                    && teacherOpening.Assignment.Kind != PopulationAssignmentKind.Teaching))
                throw Deferred(Q43);
            // A bed freed after the month's opening (for example by a cancel) is not reused that month: Q43(b), Q41. So this
            // month's new courses in the camp fit only in the beds free at its opening.
            int openingBeds = month.OpeningPeople.Count(p => (p.Assignment.Kind == PopulationAssignmentKind.Reception
                    && string.Equals(p.Assignment.Target, centre.ParentCamp, StringComparison.Ordinal))
                || (p.Assignment.Kind == PopulationAssignmentKind.Course
                    && string.Equals(p.Assignment.Target, centre.Id, StringComparison.Ordinal)));
            int startedThisMonth = starts.Count(x => x.month == month.MonthOrdinal
                && string.Equals(x.camp, centre.ParentCamp, StringComparison.Ordinal));
            if (openingBeds + startedThisMonth >= bedsPerCamp) throw new ArgumentException(BedWaitsReason);

            var course = new PopulationAssignment(PopulationAssignmentKind.Course, centre.Id, 0, teacher.Id);
            var pupil = trainee.WithPlacement(PopulationStatus.Trainee, trainee.Location, course, trainee.Captor, trainee.Home);
            if (teachingHere) Commit(pupil);
            else Commit(teacher.WithPlacement(teacher.Status, teacher.Location,
                new PopulationAssignment(PopulationAssignmentKind.Teaching, centre.Id), teacher.Captor, teacher.Home), pupil);
            months[trainee.Id] = (month.MonthOrdinal, -1);
            starts.Add((month.MonthOrdinal, centre.ParentCamp));
        }

        /// <summary>R-TRAIN-006: count one completed course turn, once per month ordinal: in the enrolment month (Q43(a), "A
        /// course occupies its people from its first turn"), or in a later month whose opening already holds the course with
        /// the same teacher. The turn that reaches Agreed `profession.course_turns` graduates: the pupil takes the trained
        /// tier (R-QUAL-008), and pupil and teacher are released in that turn as civilians without a job (Q43(b)).</summary>
        internal void Advance(int traineeId, Gdd03SyntheticMonthContext month)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var trainee = ledger.Get(traineeId); var centre = CentreOf(trainee);
            if (!centre.ParentOperating || !centre.UpgradeComplete) throw Deferred(Q9);
            var opening = Opening(month, trainee.Id);
            bool tracked = months.TryGetValue(trainee.Id, out var known);
            // Q43(a): the enrolment month counts, though its opening snapshot was taken before the course started.
            bool enrolmentMonth = tracked && month.MonthOrdinal == known.enrolment;
            if (!enrolmentMonth && (opening.Status != PopulationStatus.Trainee || opening.Assignment.Kind != PopulationAssignmentKind.Course
                || !string.Equals(opening.Assignment.Target, centre.Id, StringComparison.Ordinal)
                || opening.Assignment.TeacherId != trainee.Assignment.TeacherId))
                throw new ArgumentException(CourseAtOpeningReason);
            if (tracked && (month.MonthOrdinal < known.enrolment || month.MonthOrdinal <= known.lastCounted))
                throw new ArgumentException("This month ordinal was already counted or is earlier.");
            if (enrolmentMonth
                    ? !month.Facilities.Any(f => string.Equals(f.Id, centre.Id, StringComparison.Ordinal) && f.AvailableAtMonthFreeze && f.OpeningCapacity > 0)
                    : !month.CourseEligibleIds.Contains(trainee.Id))
                throw Deferred(Q9);
            if (!enrolmentMonth && trainee.Assignment.ProgressTicks != opening.Assignment.ProgressTicks)
                throw new ArgumentException("This month's course turn was already counted.");

            int progress = checked(trainee.Assignment.ProgressTicks + 1);
            if (progress < courseTurns)
            {
                Commit(trainee.WithPlacement(trainee.Status, trainee.Location, new PopulationAssignment(PopulationAssignmentKind.Course,
                    centre.Id, progress, trainee.Assignment.TeacherId), trainee.Captor, trainee.Home));
                if (tracked) months[trainee.Id] = (known.enrolment, month.MonthOrdinal);
                return;
            }
            // Graduation (Q43(b)): the trained tier from the pupil's pre-release course record, then pupil and teacher are
            // released in one commit, so no teacher is left teaching with no pupil.
            var graduate = Released(CivilianProfessionRules.AfterMatchingCourse(content, state, trainee.Id, trainee.Profession));
            var teacher = ledger.Get(trainee.Assignment.TeacherId!.Value);
            bool teacherReleased = Pupils(teacher.Id) == 1;
            if (teacherReleased) Commit(graduate, Released(teacher)); else Commit(graduate);
            months.Remove(trainee.Id);
            graduated[trainee.Id] = month.MonthOrdinal;
            if (teacherReleased) graduated[teacher.Id] = month.MonthOrdinal;
        }

        /// <summary>R-TRAIN-007: cancel keeps the id, profession, tier, tools and their payment fact and reception credit;
        /// it clears the unfinished course and releases the teacher and the bed. Nothing is refunded or granted and no one
        /// moves. The released people can take another assignment at once, in this month's snapshot (#1716, #1668 Q43(b),
        /// answered 1 Nov 2026); the freed bed still waits for the next month's opening (R-TRAIN-008).</summary>
        internal void Cancel(int traineeId)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var trainee = ledger.Get(traineeId); var centre = CentreOf(trainee);
            if (!centre.ParentOperating || !centre.UpgradeComplete) throw Deferred(Q9);
            var teacher = ledger.Get(trainee.Assignment.TeacherId!.Value);
            if (Pupils(teacher.Id) > 1) Commit(Released(trainee)); else Commit(Released(trainee), Released(teacher));
            months.Remove(trainee.Id);
        }
    }
}
