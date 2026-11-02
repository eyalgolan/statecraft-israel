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
    /// synthetic fixtures only. No command, save, loader, resolver, Money, Food or UI caller. Graduation is not reachable
    /// while #1668 Q43(a) is open: no turn that would bring a course within one turn of its end is counted. Open design points
    /// (#1668 Q42, Q43, Q44 and the 27 Oct list Q9) are refused with a "Deferred:" NotSupportedException.</summary>
    internal sealed class Gdd03TrainingCentreFixture
    {
        readonly GameState state;
        readonly SortedDictionary<string, Gdd03FixtureTrainingCentre> centres;
        readonly int courseTurns, pupilsPerTeacher, bedsPerCamp, receptionTurns;
        // Per fixture instance only (not saved): month ordinals of each pupil's enrolment and last counted course turn.
        readonly SortedDictionary<int, (int enrolment, int lastCounted)> months = new SortedDictionary<int, (int, int)>();

        internal Gdd03TrainingCentreFixture(Content content, GameState state, IEnumerable<Gdd03FixtureTrainingCentre> centres)
        {
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Training centre courses require an initialized gdd-0.3 synthetic fixture.");
            this.state = state;
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
        const string Q43 = "#1668 Q43 (course turn boundaries: whether the enrolment turn counts, and in which month's snapshot participants stop or resume other assignments).";
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
            // #1716 month snapshot (owner-relayed Q43(a) and Q34, 28 Oct 2026): a course occupies the pupil from the turn it
            // starts and a job change is immediate, so a pupil who worked at the opening and was released this turn may start,
            // and this month's work/course snapshot shows the course. #1716 Q43(b) release (owner-approved answer, 1 Nov 2026,
            // #1668): "Cancellation: they are released at once, as civilians without a job." A pupil who held a course, or a
            // teacher who taught, at the opening and is free now was released by a cancel this turn (only Cancel ends those
            // exclusive assignments here), so either may start again this month. A teacher who worked at the opening and was
            // released this turn stays Deferred.
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
            int startedThisMonth = months.Count(m => m.Value.enrolment == month.MonthOrdinal
                && string.Equals(CentreOf(ledger.Get(m.Key)).ParentCamp, centre.ParentCamp, StringComparison.Ordinal));
            if (openingBeds + startedThisMonth >= bedsPerCamp) throw Deferred(Q43);

            var course = new PopulationAssignment(PopulationAssignmentKind.Course, centre.Id, 0, teacher.Id);
            var pupil = trainee.WithPlacement(PopulationStatus.Trainee, trainee.Location, course, trainee.Captor, trainee.Home);
            if (teachingHere) Commit(pupil);
            else Commit(teacher.WithPlacement(teacher.Status, teacher.Location,
                new PopulationAssignment(PopulationAssignmentKind.Teaching, centre.Id), teacher.Captor, teacher.Home), pupil);
            months[trainee.Id] = (month.MonthOrdinal, month.MonthOrdinal);
        }

        /// <summary>R-TRAIN-006: count one completed course turn for a course already held at the month's opening, once per
        /// month ordinal. Counting the turn that would leave one turn or none to go depends on whether the enrolment turn
        /// counts (#1668 Q43(a)), so it is refused: graduation timing is Deferred and no trained tier is granted here.</summary>
        internal void Advance(int traineeId, Gdd03SyntheticMonthContext month)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var trainee = ledger.Get(traineeId); var centre = CentreOf(trainee);
            if (!centre.ParentOperating || !centre.UpgradeComplete) throw Deferred(Q9);
            var opening = Opening(month, trainee.Id);
            if (opening.Status != PopulationStatus.Trainee || opening.Assignment.Kind != PopulationAssignmentKind.Course
                || !string.Equals(opening.Assignment.Target, centre.Id, StringComparison.Ordinal)
                || opening.Assignment.TeacherId != trainee.Assignment.TeacherId)
                throw Deferred(Q43);
            bool tracked = months.TryGetValue(trainee.Id, out var known);
            if (tracked)
            {
                if (month.MonthOrdinal == known.enrolment) throw Deferred(Q43);
                if (month.MonthOrdinal <= known.lastCounted) throw new ArgumentException("This month ordinal was already counted or is earlier.");
            }
            if (!month.CourseEligibleIds.Contains(trainee.Id)) throw Deferred(Q9);
            if (trainee.Assignment.ProgressTicks != opening.Assignment.ProgressTicks)
                throw new ArgumentException("This month's course turn was already counted.");

            int progress = checked(trainee.Assignment.ProgressTicks + 1);
            if (progress > courseTurns - 2) throw Deferred(Q43);
            Commit(trainee.WithPlacement(trainee.Status, trainee.Location, new PopulationAssignment(PopulationAssignmentKind.Course,
                centre.Id, progress, trainee.Assignment.TeacherId), trainee.Captor, trainee.Home));
            if (tracked) months[trainee.Id] = (known.enrolment, month.MonthOrdinal);
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
