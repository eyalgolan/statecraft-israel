using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Synthetic centre facts supplied by a test: a facility in its host city, with no reception camp, upgrade or
    /// reception bed (GDD 1 November 2026 answers, docs/GDD.md:372, Q42: "One veteran teacher and one pupil per centre, the
    /// teacher housed in the centre's city"; owner, 1 November 2026, "No reception-bed layer (Recommended)"). The population
    /// ledger locates people by region only, so the host city is the centre's region here. Operating state is owned by
    /// #1700/#1713 (construction); this fixture only reads it. No real centre, host or region.</summary>
    internal sealed class Gdd03FixtureTrainingCentre
    {
        internal string Id { get; }
        internal string Region { get; }
        internal bool Operating { get; }
        internal Gdd03FixtureTrainingCentre(string id, string region, bool operating)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(region))
                throw new ArgumentException("A centre needs its course target id and its host region.");
            PopulationText.ValidateText(id); PopulationText.ValidateText(region);
            Id = id; Region = region; Operating = operating;
        }
    }

    /// <summary>R-TRAIN-005 to R-TRAIN-008: gdd-0.3 farmer/pioneer course lifecycle over the #1689 population ledger,
    /// synthetic fixtures only. No command, save, loader, resolver, Money, Food or UI caller. GDD 0.3 (docs/GDD.md, 1 November
    /// 2026 answers): "A course occupies its people from its first turn (Q43(a)); they are released in the turn its last
    /// course turn counts and can be assigned from the next planning, or at once on cancellation, and work starts the turn
    /// after (Q43(b), Q43(c))." So the enrolment month is course turn 1 and the last counted turn graduates. Cases this
    /// slice has not built (Q44, decided in the GDD's 1 November 2026 answers, so the refusal is an implementation gap, not
    /// an open design answer; and the 27 Oct list Q9) are refused with a "Deferred:" NotSupportedException.</summary>
    internal sealed class Gdd03TrainingCentreFixture
    {
        readonly Content content;
        readonly GameState state;
        readonly SortedDictionary<string, Gdd03FixtureTrainingCentre> centres;
        readonly int courseTurns, pupilsPerTeacher;
        // Per fixture instance only (not saved): month ordinals of each pupil's enrolment and last counted course turn
        // (-1 before the first), and of the month in which a graduation released each person.
        readonly SortedDictionary<int, (int enrolment, int lastCounted)> months = new SortedDictionary<int, (int, int)>();
        readonly SortedDictionary<int, int> graduated = new SortedDictionary<int, int>();

        internal Gdd03TrainingCentreFixture(Content content, GameState state, IEnumerable<Gdd03FixtureTrainingCentre> centres)
        {
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Training centre courses require an initialized gdd-0.3 synthetic fixture.");
            this.content = content; this.state = state;
            courseTurns = content.RequirePopulationInteger("profession.course_turns");
            pupilsPerTeacher = content.RequirePopulationInteger("profession.trainees_per_teacher");
            this.centres = new SortedDictionary<string, Gdd03FixtureTrainingCentre>(StringComparer.Ordinal);
            foreach (var centre in centres) this.centres.Add(centre.Id, centre); // a duplicate id throws ArgumentException
            ValidateInventory();
        }

        static NotSupportedException Deferred(string question) => new NotSupportedException("Deferred: " + question);
        // Q42 (GDD 1 November 2026 answers, docs/GDD.md:372): "One veteran teacher and one pupil per centre, the teacher
        // housed in the centre's city". Its refusals, like the ruled Q43 ones, are plain ArgumentExceptions citing the ruling.
        internal const string OneTeacherReason = "A centre has one veteran teacher and one pupil (#1668 Q42, GDD 1 Nov 2026).";
        internal const string TeacherNeedsAPupilReason = "A centre's teacher teaches its pupil: a teaching record with no pupil is invalid (#1668 Q42, GDD 1 Nov 2026).";
        internal const string HostCityReason = "The teacher and the pupil are housed in the centre's city, so both must be in its region (#1668 Q42, GDD 1 Nov 2026).";
        internal const string GraduationMonthReason = "A person released by a graduation can start a course from the next month (#1668 Q43(b), GDD 1 Nov 2026).";
        internal const string CourseAtOpeningReason = "After the enrolment month, a course turn counts only when the month's opening holds the course with the same teacher (#1668 Q43(a), GDD 1 Nov 2026).";
        const string Q44 = "#1668 Q44 and the GDD novice lock-in decision (when a free entrant becomes a novice; profession change after a cancelled course).";
        const string Q9 = "#1668 27 Oct list Q9 (return after cancellation or camp loss).";

        // GDD 1 November 2026 answers (docs/GDD.md:374): "Refugees cannot train until they become civilians". In the MVP a
        // refugee becomes a civilian on the housing step (R-CITY-004), so no reception progress is read here.
        static bool Civilian(PopulationUnit p) => p.Status == PopulationStatus.FreeCivilian;
        // The old assignment a course may replace: none, or a job, released in the same commit (docs/gdd/cities.md:42:
        // "Reassignment releases old capacity atomically."). Only a cancel or a graduation ends a course or teaching here.
        static bool FreeOrWorking(PopulationUnit p)
            => p.Assignment.Kind == PopulationAssignmentKind.Unassigned || p.Assignment.Kind == PopulationAssignmentKind.Work;
        static bool FarmerOrPioneer(CivilianProfession p) => p == CivilianProfession.Farmer || p == CivilianProfession.Pioneer;
        IEnumerable<PopulationUnit> At(PopulationAssignmentKind kind, string target)
            => state.PopulationLedger.Units.Where(p => p.Assignment.Kind == kind && string.Equals(p.Assignment.Target, target, StringComparison.Ordinal));
        int Pupils(int teacher) => state.PopulationLedger.Units.Count(p => p.Assignment.Kind == PopulationAssignmentKind.Course
            && p.Assignment.TeacherId == teacher && centres.ContainsKey(p.Assignment.Target!));

        void ValidateInventory()
        {
            state.PopulationLedger.Reconcile();
            foreach (var centre in centres.Values)
            {
                var teachers = At(PopulationAssignmentKind.Teaching, centre.Id).ToArray();
                if (teachers.Length > 1) throw new ArgumentException(OneTeacherReason);
                foreach (var t in teachers)
                {
                    int n = Pupils(t.Id);
                    if (n == 0) throw new ArgumentException(TeacherNeedsAPupilReason);
                    if (n > pupilsPerTeacher || !FarmerOrPioneer(t.Profession) || t.Tier != CivilianTier.Veteran)
                        throw new ArgumentException("Centre teacher must be a matching veteran within the pupil limit.");
                }
                foreach (var p in At(PopulationAssignmentKind.Course, centre.Id))
                    if (p.Assignment.TeacherId is not int teacher || state.PopulationLedger.Get(teacher).Profession != p.Profession
                        || p.Tier != CivilianTier.Novice || p.Assignment.ProgressTicks >= courseTurns)
                        throw new ArgumentException("Centre pupil must be a matching novice with a teacher and an unfinished course.");
                if (teachers.Concat(At(PopulationAssignmentKind.Course, centre.Id)).Any(p => p.Location!.Region != centre.Region))
                    throw new ArgumentException(HostCityReason);
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

        /// <summary>R-TRAIN-005: enrol a matching novice with a matching veteran teacher at an operating centre, both in the
        /// centre's region (Q42). Either may leave a job to do so, released in the same commit. No payment, and no tools:
        /// tools are post-MVP (#1693, owner answers of 3 November 2026, answer 1).</summary>
        internal void Start(int traineeId, int teacherId, string centreId, Gdd03SyntheticMonthContext month)
        {
            ValidateInventory();
            if (centreId == null || !centres.TryGetValue(centreId, out var centre)) throw new ArgumentException("Unknown fixture centre.");
            if (!centre.Operating) throw new ArgumentException("A course needs an operating centre.");
            if (traineeId == teacherId) throw new ArgumentException("Teacher and trainee must be different population units.");
            var ledger = state.PopulationLedger; var trainee = ledger.Get(traineeId); var teacher = ledger.Get(teacherId);

            bool teachingHere = teacher.Assignment.Kind == PopulationAssignmentKind.Teaching
                && string.Equals(teacher.Assignment.Target, centre.Id, StringComparison.Ordinal);
            if (!Civilian(teacher) || !FarmerOrPioneer(teacher.Profession) || teacher.Tier != CivilianTier.Veteran)
                throw new ArgumentException("The teacher must be a free civilian, a veteran farmer or pioneer.");
            if (teachingHere ? Pupils(teacher.Id) >= pupilsPerTeacher : !FreeOrWorking(teacher))
                throw new ArgumentException("The teacher is occupied and refuses another pupil.");

            if (!Civilian(trainee) || !FreeOrWorking(trainee))
                throw new ArgumentException("The trainee must be a free civilian with no assignment but a job.");
            if (trainee.Profession == CivilianProfession.Craftsman)
                throw new ArgumentException("A course cannot replace another civilian profession.");
            if (trainee.Profession != CivilianProfession.None && trainee.Tier != CivilianTier.Novice)
                throw new ArgumentException("Only a novice can gain the trained tier from a course.");
            if (trainee.Profession != teacher.Profession) throw Deferred(Q44);

            if (teacher.Location!.Region != centre.Region || trainee.Location!.Region != centre.Region)
                throw new ArgumentException(HostCityReason);
            if (At(PopulationAssignmentKind.Teaching, centre.Id).Any(t => t.Id != teacher.Id)) throw new ArgumentException(OneTeacherReason);

            // Both must be in the month's snapshot. What either held at its opening does not matter, a reading of
            // docs/gdd/cities.md:42 ("Reassignment releases old capacity atomically.") with docs/GDD.md:372 Q43(b)/(c)
            // ("can be assigned from the next planning, or at once on cancellation") and docs/gdd/training.md:66 ("at
            // once"): a job, or a course or teaching cancelled this turn, is no bar; only a graduation is.
            Opening(month, trainee.Id); Opening(month, teacher.Id);
            // #1668 Q43(b) (#1697, 1 Nov 2026): "on graduation the people are released in the turn the last course turn
            // counts and can be assigned from the next turn's planning", so neither starts a course in that month.
            if (graduated.TryGetValue(trainee.Id, out var g) && month.MonthOrdinal <= g
                || graduated.TryGetValue(teacher.Id, out g) && month.MonthOrdinal <= g)
                throw new ArgumentException(GraduationMonthReason);

            // #1716 month snapshot (owner-relayed Q43(a) and Q34, 28 Oct 2026): a course occupies its people from the turn it
            // starts and a job change is immediate, so this month's work/course snapshot shows the course. A job either held
            // is replaced in this one commit (docs/gdd/cities.md:42: "Reassignment releases old capacity atomically.").
            var course = new PopulationAssignment(PopulationAssignmentKind.Course, centre.Id, 0, teacher.Id);
            var pupil = trainee.WithPlacement(PopulationStatus.Trainee, trainee.Location, course, trainee.Captor, trainee.Home);
            if (teachingHere) Commit(pupil);
            else Commit(teacher.WithPlacement(teacher.Status, teacher.Location,
                new PopulationAssignment(PopulationAssignmentKind.Teaching, centre.Id), teacher.Captor, teacher.Home), pupil);
            months[trainee.Id] = (month.MonthOrdinal, -1);
        }

        /// <summary>R-TRAIN-006: count one completed course turn, once per month ordinal: in the enrolment month (Q43(a), "A
        /// course occupies its people from its first turn"), or in a later month whose opening already holds the course with
        /// the same teacher. The turn that reaches Agreed `profession.course_turns` graduates: the pupil takes the trained
        /// tier (R-QUAL-008), and pupil and teacher are released in that turn as civilians without a job (Q43(b)).</summary>
        internal void Advance(int traineeId, Gdd03SyntheticMonthContext month)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var trainee = ledger.Get(traineeId); var centre = CentreOf(trainee);
            if (!centre.Operating) throw Deferred(Q9);
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

        /// <summary>R-TRAIN-007: cancel keeps the id, profession, tier, the record's tools fields and its reception progress;
        /// it clears the unfinished course and releases the teacher and the centre's place. Nothing is refunded or granted
        /// and no one moves. The released people, and the centre, can take another assignment at once, in this month's
        /// snapshot (#1716, #1668 Q43(b), answered 1 Nov 2026).</summary>
        internal void Cancel(int traineeId)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var trainee = ledger.Get(traineeId); var centre = CentreOf(trainee);
            if (!centre.Operating) throw Deferred(Q9);
            var teacher = ledger.Get(trainee.Assignment.TeacherId!.Value);
            if (Pupils(teacher.Id) > 1) Commit(Released(trainee)); else Commit(Released(trainee), Released(teacher));
            months.Remove(trainee.Id);
        }
    }
}
