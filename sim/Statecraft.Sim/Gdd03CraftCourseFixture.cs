using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>R-CRAFT-003: gdd-0.3 craftsman course over the #1689 population ledger, synthetic fixtures only (#1698). No
    /// command, save, loader, resolver, Money, Food, Industry capacity, course income or UI caller; the Industry slot and
    /// the course income stay with #1692. The test supplies the course ids, Course targets of the ledger bindings. Course
    /// turns follow Gdd03TrainingCentreFixture's Q43 timing (#2391; docs/GDD.md:372, from the 1 November 2026 answers the
    /// owner approved): the enrolment month is course turn 1, and the turn that reaches Agreed `craft.course_turns` makes
    /// an apprentice, released at once. No teacher is read or created ("The course adds no mentor population unit").
    /// Cases decided but not built here are refused with a "Deferred:" NotSupportedException.</summary>
    internal sealed class Gdd03CraftCourseFixture
    {
        readonly GameState state;
        readonly SortedSet<string> courses = new SortedSet<string>(StringComparer.Ordinal);
        readonly int courseTurns;
        // Per fixture instance only (not saved), as in Gdd03TrainingCentreFixture: month ordinals of each pupil's enrolment
        // and last counted course turn (-1 before the first), and of the month in which a graduation released each person.
        readonly SortedDictionary<int, (int enrolment, int lastCounted)> months = new SortedDictionary<int, (int, int)>();
        readonly SortedDictionary<int, int> graduated = new SortedDictionary<int, int>();

        internal Gdd03CraftCourseFixture(Content content, GameState state, IEnumerable<string> courseIds)
        {
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Craftsman courses require an initialized gdd-0.3 synthetic fixture.");
            this.state = state;
            courseTurns = content.RequirePopulationInteger("craft.course_turns");
            foreach (var id in courseIds ?? throw new ArgumentNullException(nameof(courseIds)))
            {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A craftsman course needs a course target id.");
                PopulationText.ValidateText(id);
                if (!courses.Add(id)) throw new ArgumentException("Duplicate craftsman course id.");
            }
            ValidateInventory();
        }

        static NotSupportedException Deferred(string question) => new NotSupportedException("Deferred: " + question);
        const string D3 = "#1668 D3 (decided, not built here: a trained or veteran farmer or pioneer starting the craftsman course; the #1698 body says the course cannot replace another trained profession, and D3's two-way professions let a Train order take a qualified civilian).";
        // Re-enrolment is ruled; the tiers during and after a new course are not, so only that part stays Deferred.
        const string Q27M5 = "#1668 27M-5 (ruled: \"graduates may re-enrol at any time\", docs/gdd/training.md:66; not built here: no ruled text says which tier a craftsman holds during and after a new craftsman course).";
        const string Q52 = "#1668 Q52 (decided, not built here: a course whose Industry slot is lost is cancelled like CancelTraining; the slot is #1692's).";
        internal const string CourseAtOpeningReason = "After the enrolment month, a course turn counts only when the month's opening holds the course (#1668 Q43(a), GDD 1 Nov 2026).";

        IEnumerable<PopulationUnit> AtCourses()
            => state.PopulationLedger.Units.Where(p => p.Assignment.Target != null && courses.Contains(p.Assignment.Target));

        void ValidateInventory()
        {
            state.PopulationLedger.Reconcile();
            foreach (var p in AtCourses())
                if (p.Assignment.Kind != PopulationAssignmentKind.Course || p.Assignment.TeacherId != null
                    || p.Profession != CivilianProfession.Craftsman || p.Tier != CivilianTier.Novice || p.Assignment.ProgressTicks >= courseTurns)
                    throw new ArgumentException("A craftsman course holds only novice craftsmen, with no teacher and an unfinished course.");
        }

        string CourseOf(PopulationUnit pupil)
        {
            if (pupil.Status != PopulationStatus.Trainee || pupil.Assignment.Kind != PopulationAssignmentKind.Course
                || pupil.Assignment.Target == null || !courses.Contains(pupil.Assignment.Target))
                throw new ArgumentException("Not a pupil of a fixture craftsman course.");
            return pupil.Assignment.Target;
        }

        static PopulationUnit Opening(Gdd03SyntheticMonthContext month, int id)
            => (month ?? throw new ArgumentNullException(nameof(month))).OpeningPeople.FirstOrDefault(p => p.Id == id)
               ?? throw new ArgumentException("Participant is not in the month's opening snapshot.");

        static PopulationUnit WithProfession(PopulationUnit p, PopulationStatus status, PopulationAssignment assignment,
            CivilianProfession profession, CivilianTier tier, int workXp)
            => new PopulationUnit(p.Id, status, p.ReceptionTicks, p.Location, assignment, profession, tier, workXp,
                p.Qualification, p.CombatXp, p.Equipment, p.Captor, p.Home);

        /// <summary>R-CRAFT-003: a received free civilian with no profession, or a novice farmer or pioneer (27M-3), starts
        /// the course as a novice craftsman (#1698 follow-up) under the same id. Work XP resets to 0 because the course
        /// replaces the novice profession (a maintainer call); tools and their paid fact are kept (27M-3, Q44). A job held
        /// now is released in the same commit, and what the person held at the month's opening is no bar: a maintainer
        /// reading of docs/gdd/cities.md:42 ("Reassignment releases old capacity atomically.") with docs/GDD.md:372
        /// Q43(b)/(c), as #1697's second slice reads them, not an owner ruling. A person released by a graduation cannot
        /// start in that month (Q43(b), as in #2391).</summary>
        internal void Start(int traineeId, string courseId, Gdd03SyntheticMonthContext month)
        {
            ValidateInventory();
            if (courseId == null || !courses.Contains(courseId)) throw new ArgumentException("Unknown fixture craftsman course.");
            var ledger = state.PopulationLedger; var p = ledger.Get(traineeId);
            Opening(month, p.Id); // the person must be in the month's opening snapshot, as in #2391
            if (graduated.TryGetValue(p.Id, out var g) && month.MonthOrdinal <= g)
                throw new ArgumentException(Gdd03TrainingCentreFixture.GraduationMonthReason);
            if (p.Status != PopulationStatus.FreeCivilian || p.ReceptionTicks != ledger.ReceptionLimit)
                throw new ArgumentException("The pupil must be a received free civilian.");
            // A course or teaching would have to be cancelled by the new start (docs/gdd/training.md:66); not done here.
            if (p.Assignment.Kind != PopulationAssignmentKind.Unassigned && p.Assignment.Kind != PopulationAssignmentKind.Work)
                throw new ArgumentException("Only a free civilian without a job or with a job can start the craftsman course.");
            if (p.Profession == CivilianProfession.Craftsman) throw Deferred(Q27M5);
            if (p.Profession != CivilianProfession.None && p.Tier != CivilianTier.Novice) throw Deferred(D3);

            ledger.Replace(WithProfession(p, PopulationStatus.Trainee, new PopulationAssignment(PopulationAssignmentKind.Course, courseId),
                CivilianProfession.Craftsman, CivilianTier.Novice, 0));
            months[p.Id] = (month.MonthOrdinal, -1);
        }

        /// <summary>R-CRAFT-003: count one course turn, once per month ordinal: in the enrolment month (Q43(a)), or in a later
        /// month whose opening already holds the course. The turn that reaches Agreed `craft.course_turns` makes an
        /// apprentice craftsman, released in that turn as a civilian without a job (Q43(b)); work starts the next month.</summary>
        internal void Advance(int traineeId, Gdd03SyntheticMonthContext month)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var p = ledger.Get(traineeId); var course = CourseOf(p);
            var opening = Opening(month, p.Id);
            bool tracked = months.TryGetValue(p.Id, out var known);
            bool enrolmentMonth = tracked && month.MonthOrdinal == known.enrolment;
            if (!enrolmentMonth && (opening.Status != PopulationStatus.Trainee || opening.Assignment.Kind != PopulationAssignmentKind.Course
                || !string.Equals(opening.Assignment.Target, course, StringComparison.Ordinal)))
                throw new ArgumentException(CourseAtOpeningReason);
            if (tracked && (month.MonthOrdinal < known.enrolment || month.MonthOrdinal <= known.lastCounted))
                throw new ArgumentException("This month ordinal was already counted or is earlier.");
            if (enrolmentMonth
                    ? !month.Facilities.Any(f => string.Equals(f.Id, course, StringComparison.Ordinal) && f.AvailableAtMonthFreeze && f.OpeningCapacity > 0)
                    : !month.CourseEligibleIds.Contains(p.Id))
                throw Deferred(Q52);
            if (!enrolmentMonth && p.Assignment.ProgressTicks != opening.Assignment.ProgressTicks)
                throw new ArgumentException("This month's course turn was already counted.");

            int progress = checked(p.Assignment.ProgressTicks + 1);
            if (progress < courseTurns)
            {
                ledger.Replace(p.WithPlacement(p.Status, p.Location,
                    new PopulationAssignment(PopulationAssignmentKind.Course, course, progress), p.Captor, p.Home));
                if (tracked) months[p.Id] = (known.enrolment, month.MonthOrdinal);
                return;
            }
            ledger.Replace(WithProfession(p, PopulationStatus.FreeCivilian, PopulationAssignment.Unassigned,
                CivilianProfession.Craftsman, CivilianTier.Apprentice, p.WorkXp));
            months.Remove(p.Id);
            graduated[p.Id] = month.MonthOrdinal;
        }

        /// <summary>R-CRAFT-003: cancelling before qualifying makes an ordinary civilian again (#1698 follow-up): profession
        /// and tier none, work XP 0, released at once as a civilian without a job (Q43(b)). Same id; tools and their paid
        /// fact are kept and nothing is refunded (27M-4, Q44).</summary>
        internal void Cancel(int traineeId)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var p = ledger.Get(traineeId); CourseOf(p);
            ledger.Replace(WithProfession(p, PopulationStatus.FreeCivilian, PopulationAssignment.Unassigned,
                CivilianProfession.None, CivilianTier.None, 0));
            months.Remove(p.Id);
        }
    }
}
