using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Synthetic military camp facts supplied by a test: the camp's course target id, its region and whether it
    /// operates. Real camps are #1719's; this fixture only reads these facts.</summary>
    internal sealed class Gdd03FixtureMilitaryCamp
    {
        internal string Id { get; }
        internal string Region { get; }
        internal bool Operating { get; }
        internal Gdd03FixtureMilitaryCamp(string id, string region, bool operating)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(region))
                throw new ArgumentException("A camp needs its own course target id and a region.");
            PopulationText.ValidateText(id); PopulationText.ValidateText(region);
            Id = id; Region = region; Operating = operating;
        }
    }

    /// <summary>#1699 first slice: a recruit's basic training starts on the #1689 population ledger and holds its issued
    /// Arms (ARMS-1, ARMS-2, 27M-7, R-TRAIN-001, R-ARMS-003), synthetic fixtures only. No command, save, loader, resolver,
    /// Food or UI caller. Like <see cref="Gdd03ProjectBuildFixture"/> it owns its spare Arms, Money and Debt, so the ledger
    /// never touches resources (R-POP-002). Completion into a formation is refused with a "Deferred:" NotSupportedException.
    /// Cancel returns an issued item to the spare Arms (R-TRAIN-002, R-ARMS-003, docs/GDD.md:372).</summary>
    internal sealed class Gdd03BasicTrainingFixture
    {
        /// <summary>Issued item ids are this prefix plus the population id. It is disjoint from the census and units_1948
        /// source prefixes (<see cref="HistoricalPopulation.CensusPrefix"/>, <see cref="HistoricalPopulation.FormationPrefix"/>),
        /// so no id can equal a starting formation's item; a record holds at most one military item, so no two records are issued the same id.</summary>
        internal const string IssuedPrefix = "basic_training/";
        const string Completion = "Deferred: completing basic training into a formation (#1699; R-TRAIN-003, R-UNIT-003).";
        const string CampLoss = "Deferred: a camp that stops operating during a course (#1699, 27M-9).";

        readonly GameState state;
        readonly SortedDictionary<string, Gdd03FixtureMilitaryCamp> camps = new SortedDictionary<string, Gdd03FixtureMilitaryCamp>(StringComparer.Ordinal);
        readonly int capacity, basicTurns;
        readonly decimal moneyPerCourse;
        // Per fixture instance only (not saved): each trainee's enrolment month and last counted month ordinal.
        readonly SortedDictionary<int, (int enrolment, int lastCounted)> months = new SortedDictionary<int, (int, int)>();
        // Per fixture instance only (not saved): the trainees whose item this fixture issued from its spare Arms.
        readonly SortedSet<int> issued = new SortedSet<int>();
        internal int SpareArms { get; private set; }
        internal decimal Money { get; private set; }
        internal decimal Debt { get; private set; }

        internal Gdd03BasicTrainingFixture(Content content, GameState state, IEnumerable<Gdd03FixtureMilitaryCamp> camps,
            int spareArms, decimal money)
        {
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Basic training requires an initialized gdd-0.3 synthetic fixture.");
            var terms = Gdd03MilitaryCatalogue.Read(content.Gdd03Balance!);
            // One issued item per record (R-POP-002): any other issue or repeat count cannot be represented.
            if (terms.IssueArms != 1)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.military.issue_arms", "must be 1: one item per population unit");
            if (terms.RepeatIssueArms != 0)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.military.repeat_issue", "must be 0: never a second issue");
            if (spareArms < 0 || money < 0) throw new ArgumentException("Spare Arms and Money must be nonnegative.");
            this.state = state;
            capacity = terms.CampCapacity; basicTurns = terms.BasicTurns; moneyPerCourse = terms.TrainingMoney;
            foreach (var camp in camps) this.camps.Add(camp.Id, camp); // a duplicate id throws ArgumentException
            SpareArms = spareArms; Money = money;
            ValidateInventory();
        }

        IEnumerable<PopulationUnit> Trainees(string campId) => state.PopulationLedger.Units.Where(p
            => p.Assignment.Kind == PopulationAssignmentKind.Course && string.Equals(p.Assignment.Target, campId, StringComparison.Ordinal));

        void ValidateInventory()
        {
            state.PopulationLedger.Reconcile();
            foreach (var camp in camps.Values)
            {
                var trainees = Trainees(camp.Id).ToArray();
                if (trainees.Length > capacity) throw new ArgumentException("Camp " + camp.Id + " is over its capacity.");
                if (trainees.Any(p => p.Equipment.MilitaryId == null || p.Assignment.TeacherId != null || p.Assignment.ProgressTicks >= basicTurns))
                    throw new ArgumentException("A basic trainee must hold an issued item, have no teacher and an unfinished course.");
            }
        }

        // Every check runs before the first write; Replace validates each record and a failure restores earlier writes.
        void Commit(IReadOnlyList<PopulationUnit> replacements)
        {
            var ledger = state.PopulationLedger; var previous = new List<PopulationUnit>();
            try { foreach (var r in replacements) { var old = ledger.Get(r.Id); ledger.Replace(r); previous.Add(old); } }
            catch { for (int i = previous.Count - 1; i >= 0; i--) ledger.Replace(previous[i]); throw; }
        }

        /// <summary>R-TRAIN-001 and R-ARMS-003 under gdd-0.3: start basic training for the given records at an operating camp,
        /// all or nothing, ids in ascending order. Each must be a free civilian with no assignment and no military
        /// qualification, in the camp's region (27M-7, no travel), and the camp's trainees may not pass
        /// <c>military.camp_capacity</c>. Then, after the other checks and before paying, each unequipped record needs one
        /// spare Arms (ARMS-2): a short stock refuses the whole order with the need and the stock. An equipped record keeps
        /// its item and takes none. Each course pays <c>military.training_money</c>, borrowing the shortfall (R-MONEY-003).</summary>
        internal void Start(IEnumerable<int> ids, string campId, int monthOrdinal)
        {
            ValidateInventory();
            var order = (ids ?? throw new ArgumentNullException(nameof(ids))).OrderBy(id => id).ToArray();
            if (order.Length == 0 || order.Distinct().Count() != order.Length)
                throw new ArgumentException("Name at least one population unit, each once.");
            if (monthOrdinal < 0) throw new ArgumentException("The month ordinal must be nonnegative.");
            if (campId == null || !camps.TryGetValue(campId, out var camp)) throw new ArgumentException("Unknown fixture camp.");
            if (!camp.Operating) throw new ArgumentException("Camp " + camp.Id + " is not operating.");

            var ledger = state.PopulationLedger;
            var recruits = order.Select(ledger.Get).ToArray(); // an unknown id throws ArgumentException
            foreach (var p in recruits)
            {
                if (p.Status != PopulationStatus.FreeCivilian || p.Assignment.Kind != PopulationAssignmentKind.Unassigned)
                    throw new ArgumentException("Unit " + p.Id.ToString(CultureInfo.InvariantCulture) + " is not a civilian without a job.");
                if (p.Qualification != "")
                    throw new ArgumentException("Unit " + p.Id.ToString(CultureInfo.InvariantCulture) + " is already a qualified soldier.");
                if (!string.Equals(p.Location!.Region, camp.Region, StringComparison.Ordinal))
                    throw new ArgumentException("Unit " + p.Id.ToString(CultureInfo.InvariantCulture) + " is not in the camp's region.");
            }
            int room = capacity - Trainees(camp.Id).Count();
            if (recruits.Length > room)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Camp {0} has room for {1} more trainees (capacity {2}).", camp.Id, room, capacity));
            int need = recruits.Count(p => p.Equipment.MilitaryId == null);
            if (need > SpareArms)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Basic training: not enough Arms (need {0}, have {1}).", need, SpareArms));
            // R-MONEY-003: Money stops at 0 and the shortfall moves to debt. The cost and both new balances are computed
            // before any write, so a cost that cannot be represented (an OverflowException) leaves everything unchanged.
            decimal cost = moneyPerCourse * recruits.Length;
            decimal paid = Math.Min(Money, cost), debt = Debt + (cost - paid);

            var course = new PopulationAssignment(PopulationAssignmentKind.Course, camp.Id);
            Commit(recruits.Select(p =>
            {
                var e = p.Equipment;
                var trainee = e.MilitaryId != null ? p : new PopulationUnit(p.Id, p.Status, p.ReceptionTicks, p.Location, p.Assignment,
                    p.Profession, p.Tier, p.WorkXp, p.Qualification, p.CombatXp,
                    new PopulationEquipment(e.ToolsId, e.ToolsPaid, IssuedPrefix + p.Id.ToString(CultureInfo.InvariantCulture), true),
                    p.Captor, p.Home);
                return trainee.WithPlacement(PopulationStatus.Trainee, p.Location, course, p.Captor, p.Home);
            }).ToArray());
            SpareArms -= need;
            Money -= paid; Debt = debt;
            foreach (var p in recruits) months[p.Id] = (monthOrdinal, monthOrdinal - 1);
            foreach (var p in recruits) if (p.Equipment.MilitaryId == null) issued.Add(p.Id);
        }

        /// <summary>R-TRAIN-001 "counting the turn it was ordered in": count one course turn a month ordinal, the enrolment
        /// month included, each ordinal once and never an earlier one. The turn that would reach <c>military.basic_turns</c>
        /// completes the course and is Deferred.</summary>
        internal void Advance(int traineeId, int monthOrdinal)
        {
            ValidateInventory();
            var trainee = state.PopulationLedger.Get(traineeId);
            if (trainee.Status != PopulationStatus.Trainee || trainee.Assignment.Target == null
                || !camps.TryGetValue(trainee.Assignment.Target, out var camp) || !months.TryGetValue(traineeId, out var known))
                throw new ArgumentException("Not a trainee this fixture started.");
            if (!camp.Operating) throw new NotSupportedException(CampLoss);
            if (monthOrdinal < known.enrolment || monthOrdinal <= known.lastCounted)
                throw new ArgumentException("This month ordinal was already counted or is before enrolment.");
            int progress = checked(trainee.Assignment.ProgressTicks + 1);
            if (progress >= basicTurns) throw new NotSupportedException(Completion);
            Commit(new[] { trainee.WithPlacement(trainee.Status, trainee.Location,
                new PopulationAssignment(PopulationAssignmentKind.Course, camp.Id, progress), trainee.Captor, trainee.Home) });
            months[traineeId] = (known.enrolment, monthOrdinal);
        }

        /// <summary>R-TRAIN-002 under gdd-0.3: cancel one trainee's course. "`CancelTraining(cohort)` returns the cohort's
        /// people to its city as civilians without a job and refunds none of the Money paid"; docs/GDD.md:372 "Money and tools
        /// are not refunded and Arms return to the pool". The same record stays in place as a free civilian without a job and
        /// its course progress ends. Only an item this fixture issued goes back to the spare Arms (R-ARMS-003 "the Arms it
        /// took"); a carried item stays on the record. Money and Debt are unchanged. A record that is not a current trainee of
        /// this fixture is refused with nothing changed. The carried item stays per R-ARMS-003 ('the Arms it took'); the
        /// issued-item guard below is a maintainer call (not an owner ruling).</summary>
        internal void Cancel(int traineeId)
        {
            ValidateInventory();
            var trainee = state.PopulationLedger.Get(traineeId); // an unknown id throws ArgumentException
            if (trainee.Status != PopulationStatus.Trainee || trainee.Assignment.Kind != PopulationAssignmentKind.Course
                || trainee.Assignment.Target == null || !camps.ContainsKey(trainee.Assignment.Target) || !months.ContainsKey(traineeId))
                throw new ArgumentException("Not a trainee this fixture started.");
            var e = trainee.Equipment;
            bool refund = issued.Contains(traineeId);
            // Maintainer call (not an owner ruling): no rule covers a trainee whose issued item is gone, and refusing neither
            // refunds an item the record no longer holds nor loses that Arms. In the tests only a direct ledger write gets here.
            if (refund && e.MilitaryId != IssuedPrefix + traineeId.ToString(CultureInfo.InvariantCulture))
                throw new InvalidOperationException("Trainee " + traineeId.ToString(CultureInfo.InvariantCulture) + " no longer holds the item this fixture issued.");
            int spare = refund ? checked(SpareArms + 1) : SpareArms;
            var person = !refund ? trainee : new PopulationUnit(trainee.Id, trainee.Status, trainee.ReceptionTicks, trainee.Location,
                trainee.Assignment, trainee.Profession, trainee.Tier, trainee.WorkXp, trainee.Qualification, trainee.CombatXp,
                new PopulationEquipment(e.ToolsId, e.ToolsPaid, null, false), trainee.Captor, trainee.Home);
            Commit(new[] { person.WithPlacement(PopulationStatus.FreeCivilian, trainee.Location, PopulationAssignment.Unassigned,
                trainee.Captor, trainee.Home) });
            SpareArms = spare;
            issued.Remove(traineeId); months.Remove(traineeId);
        }
    }
}
