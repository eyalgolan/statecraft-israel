using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Synthetic military camp facts supplied by a test: the camp's course target id, its region and whether it
    /// operates. Real camps are #1719's; this fixture only reads these facts. A test sets <see cref="Operating"/> false to
    /// stand for a camp lost during a course (27M-9).</summary>
    internal sealed class Gdd03FixtureMilitaryCamp
    {
        internal string Id { get; }
        internal string Region { get; }
        internal bool Operating { get; set; }
        internal Gdd03FixtureMilitaryCamp(string id, string region, bool operating)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(region))
                throw new ArgumentException("A camp needs its own course target id and a region.");
            PopulationText.ValidateText(id); PopulationText.ValidateText(region);
            Id = id; Region = region; Operating = operating;
        }
    }

    /// <summary>A synthetic formation supplied by a test: an ActiveService target already bound in the ledger fixture, and its
    /// region. A cohort graduates into the first free pre-bound formation in deterministic order (owner ruling, 4 November
    /// 2026, Q25: "do the recommended for all"; the chosen option read: "Allocate it to the first free pre-bound formation in
    /// deterministic order"; https://github.com/eyalgolan/statecraft-israel/issues/1699#issuecomment-6623136049). Limiting
    /// it to the camp's region, and what makes a formation free, are 53's maintainer calls, not owner rulings. The formations
    /// are "pre-bound, empty ActiveService targets supplied by the tests; no ledger API change" (53's maintainer call,
    /// 3 November 2026).</summary>
    internal sealed class Gdd03FixtureFormation
    {
        internal string Id { get; }
        internal string Region { get; }
        internal Gdd03FixtureFormation(string id, string region)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(region))
                throw new ArgumentException("A formation needs its own ActiveService target id and a region.");
            PopulationText.ValidateText(id); PopulationText.ValidateText(region);
            Id = id; Region = region;
        }
    }

    /// <summary>#1699 first slice: a recruit's basic training starts on the #1689 population ledger and holds its issued
    /// Arms (ARMS-1, ARMS-2, 27M-7, R-TRAIN-001, R-ARMS-003), synthetic fixtures only. No command, save, loader, resolver,
    /// Food or UI caller. Like <see cref="Gdd03ProjectBuildFixture"/> it owns its spare Arms, Money and Debt, so the ledger
    /// never touches resources (R-POP-002). Cancel returns an issued item to the spare Arms (R-TRAIN-002, R-ARMS-003,
    /// docs/GDD.md:372), and a camp that stops operating cancels every course at it the same way (27M-9). The last course
    /// turn graduates the record into its cohort's formation (R-TRAIN-003, R-UNIT-003), and each formation it graduated pays upkeep from its graduation month (27M-8, R-UNIT-010).</summary>
    internal sealed class Gdd03BasicTrainingFixture
    {
        /// <summary>Issued item ids are this prefix plus the population id. It is disjoint from the census and units_1948
        /// source prefixes (<see cref="HistoricalPopulation.CensusPrefix"/>, <see cref="HistoricalPopulation.FormationPrefix"/>),
        /// so no id can equal a starting formation's item; a record holds at most one military item, so no two records are issued the same id.</summary>
        internal const string IssuedPrefix = "basic_training/";

        readonly GameState state;
        readonly SortedDictionary<string, Gdd03FixtureMilitaryCamp> camps = new SortedDictionary<string, Gdd03FixtureMilitaryCamp>(StringComparer.Ordinal);
        readonly int capacity, basicTurns;
        readonly decimal moneyPerCourse, activeMoney;
        // A graduate's military qualification: the course's unit class, content/unit_types.yaml `recruit` `class` (infantry).
        // Value: the course's unit class (unit_types.yaml); maintainer call on the source of the value, not an owner ruling.
        readonly string graduateQualification;
        readonly SortedDictionary<string, Gdd03FixtureFormation> formations = new SortedDictionary<string, Gdd03FixtureFormation>(StringComparer.Ordinal);
        // Per fixture instance only (not saved): each trainee's enrolment month and last counted month ordinal.
        readonly SortedDictionary<int, (int enrolment, int lastCounted)> months = new SortedDictionary<int, (int, int)>();
        // Per fixture instance only (not saved): the trainees whose item this fixture issued from its spare Arms.
        readonly SortedSet<int> issued = new SortedSet<int>();
        // Per fixture instance only (not saved). 53's maintainer call (not an owner ruling, 3 November 2026): one Start
        // order is one cohort and graduates into one formation. Each current trainee's cohort, and each cohort's formation
        // once its first member graduates.
        readonly SortedDictionary<int, int> cohortOf = new SortedDictionary<int, int>();
        readonly SortedDictionary<int, string> cohortFormation = new SortedDictionary<int, string>();
        // Per fixture instance only (not saved): each claimed formation's graduation month, the month its first member
        // graduated (27M-8, R-UNIT-010).
        readonly SortedDictionary<string, int> graduationMonth = new SortedDictionary<string, int>(StringComparer.Ordinal);
        int nextCohort;
        int? lastPaidMonth;
        internal int SpareArms { get; private set; }
        internal decimal Money { get; private set; }
        internal decimal Debt { get; private set; }

        internal Gdd03BasicTrainingFixture(Content content, GameState state, IEnumerable<Gdd03FixtureMilitaryCamp> camps,
            IEnumerable<Gdd03FixtureFormation> formations, int spareArms, decimal money)
        {
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Basic training requires an initialized gdd-0.3 synthetic fixture.");
            var terms = Gdd03MilitaryCatalogue.Read(content.Gdd03Balance!);
            // One issued item per record (R-POP-002): any other issue or repeat count cannot be represented.
            if (terms.IssueArms != 1)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.military.issue_arms", "must be 1: one item per population unit");
            if (terms.RepeatIssueArms != 0)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.military.repeat_issue", "must be 0: never a second issue");
            var recruit = content.UnitTypes.FirstOrDefault(t => string.Equals(t.Id, "recruit", StringComparison.Ordinal));
            if (recruit == null || string.IsNullOrWhiteSpace(recruit.Class))
                throw new ContentException("unit_types.yaml", "types.recruit.class", "required: a basic training graduate's qualification");
            PopulationText.ValidateText(recruit.Class);
            if (spareArms < 0 || money < 0) throw new ArgumentException("Spare Arms and Money must be nonnegative.");
            this.state = state; graduateQualification = recruit.Class;
            capacity = CampRecords(terms.CampCapacity, PopulationLedger.HistoricalPeoplePerUnit(content), state.PopulationLedger.PeoplePerUnit);
            basicTurns = terms.BasicTurns; moneyPerCourse = terms.TrainingMoney; activeMoney = terms.ActiveMoney;
            foreach (var camp in camps) this.camps.Add(camp.Id, camp); // a duplicate id throws ArgumentException
            foreach (var formation in formations)
            {
                if (this.camps.ContainsKey(formation.Id)) throw new ArgumentException("A formation cannot be a camp.");
                this.formations.Add(formation.Id, formation); // a duplicate id throws ArgumentException
            }
            SpareArms = spareArms; Money = money;
            ValidateInventory();
        }

        /// <summary>#2739, option 1 (owner, 7 November 2026): <c>military.camp_capacity</c> counts population units at D2's
        /// scale, rules.yaml's start-day <c>people_per_unit</c> (1,000), so 50 is 50,000 people (the catalogue note: "#1668's
        /// 5 places at 10,000 people a unit is 50 units at D2's 1,000 people a unit"). The fixture holds that many people in
        /// records at the ledger's own scale: 50 at 1,000 a record, 5 at 10,000. Rounding rule (proposed in #2739's PR, not
        /// an owner ruling): a scale that doesn't divide the people rounds down, so a camp never holds more people than the
        /// cap, and a scale where not even one record fits is refused.</summary>
        static int CampRecords(int capacity, int capacityPeoplePerUnit, int ledgerPeoplePerUnit)
        {
            long records = (long)capacity * capacityPeoplePerUnit / ledgerPeoplePerUnit;
            if (records < 1)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.military.camp_capacity",
                    "holds less than one population unit at this ledger's scale");
            return (int)Math.Min(records, int.MaxValue);
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
        /// <c>military.camp_capacity</c>'s people in records at the ledger's scale (<see cref="CampRecords"/>). Then, after
        /// the other checks and before paying, each unequipped record needs one
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
            foreach (var p in recruits) cohortOf[p.Id] = nextCohort;
            nextCohort++;
        }

        /// <summary>R-TRAIN-001 "counting the turn it was ordered in": count one course turn a month ordinal, the enrolment
        /// month included, each ordinal once and never an earlier one. The turn that reaches <c>military.basic_turns</c>
        /// graduates the record (<see cref="Graduate"/>). At a camp that stops operating the turn counts nothing and cancels
        /// every course at that camp at once, as <see cref="Cancel"/> does (R-TRAIN-002; docs/GDD.md:374, 27M-9: "After a
        /// cancellation or camp loss, people go to their city as civilians without a job"), the would-be graduation turn
        /// included. Maintainer reading (not an owner ruling): the loss is checked before the month ordinal, as a lost
        /// training centre's is (R-TRAIN-007), and it cancels only the trainees this fixture started.</summary>
        internal void Advance(int traineeId, int monthOrdinal)
        {
            ValidateInventory();
            var trainee = state.PopulationLedger.Get(traineeId);
            if (trainee.Status != PopulationStatus.Trainee || trainee.Assignment.Target == null
                || !camps.TryGetValue(trainee.Assignment.Target, out var camp) || !months.TryGetValue(traineeId, out var known))
                throw new ArgumentException("Not a trainee this fixture started.");
            if (!camp.Operating)
            {
                Release(Trainees(camp.Id).Where(p => months.ContainsKey(p.Id)).ToArray());
                return;
            }
            if (monthOrdinal < known.enrolment || monthOrdinal <= known.lastCounted)
                throw new ArgumentException("This month ordinal was already counted or is before enrolment.");
            int progress = checked(trainee.Assignment.ProgressTicks + 1);
            if (progress >= basicTurns) { Graduate(trainee, camp, monthOrdinal); return; }
            Commit(new[] { trainee.WithPlacement(trainee.Status, trainee.Location,
                new PopulationAssignment(PopulationAssignmentKind.Course, camp.Id, progress), trainee.Captor, trainee.Home) });
            months[traineeId] = (known.enrolment, monthOrdinal);
        }

        /// <summary>R-TRAIN-002 under gdd-0.3: cancel one trainee's course. "`CancelTraining(cohort)` returns the cohort's
        /// people to its city as civilians without a job and refunds none of the Money paid"; docs/GDD.md:372 "Money and tools
        /// are not refunded and Arms return to the pool". The same record stays in place as a free civilian without a job and
        /// its course progress ends. Only an item this fixture issued goes back to the spare Arms (R-ARMS-003 "the Arms it
        /// took"); a carried item stays on the record. Money and Debt are unchanged. A record that is not a current trainee of
        /// this fixture is refused with nothing changed. The carried item stays by our reading of R-ARMS-003 ('the Arms it
        /// took'); the issued-item guard below is a maintainer call (not an owner ruling).</summary>
        internal void Cancel(int traineeId)
        {
            ValidateInventory();
            var trainee = state.PopulationLedger.Get(traineeId); // an unknown id throws ArgumentException
            if (trainee.Status != PopulationStatus.Trainee || trainee.Assignment.Kind != PopulationAssignmentKind.Course
                || trainee.Assignment.Target == null || !camps.ContainsKey(trainee.Assignment.Target) || !months.ContainsKey(traineeId))
                throw new ArgumentException("Not a trainee this fixture started.");
            Release(new[] { trainee });
        }

        // Cancel's release of the given trainees, all or nothing: every record is checked and the spare stock computed
        // before the first write, so a camp loss that cannot release one trainee changes no record, Arms, Money or Debt.
        void Release(IReadOnlyList<PopulationUnit> trainees)
        {
            int spare = SpareArms; var released = new List<PopulationUnit>();
            foreach (var trainee in trainees)
            {
                var e = trainee.Equipment;
                bool refund = issued.Contains(trainee.Id);
                // Maintainer call (not an owner ruling): no rule covers a trainee whose issued item is gone, and refusing neither
                // refunds an item the record no longer holds nor loses that Arms. In the tests only a direct ledger write gets here.
                if (refund && e.MilitaryId != IssuedPrefix + trainee.Id.ToString(CultureInfo.InvariantCulture))
                    throw new InvalidOperationException("Trainee " + trainee.Id.ToString(CultureInfo.InvariantCulture) + " no longer holds the item this fixture issued.");
                if (refund) spare = checked(spare + 1);
                var person = !refund ? trainee : new PopulationUnit(trainee.Id, trainee.Status, trainee.ReceptionTicks, trainee.Location,
                    trainee.Assignment, trainee.Profession, trainee.Tier, trainee.WorkXp, trainee.Qualification, trainee.CombatXp,
                    new PopulationEquipment(e.ToolsId, e.ToolsPaid, null, false), trainee.Captor, trainee.Home);
                released.Add(person.WithPlacement(PopulationStatus.FreeCivilian, trainee.Location, PopulationAssignment.Unassigned,
                    trainee.Captor, trainee.Home));
            }
            Commit(released);
            SpareArms = spare;
            foreach (var trainee in trainees) { issued.Remove(trainee.Id); months.Remove(trainee.Id); cohortOf.Remove(trainee.Id); }
        }

        /// <summary>R-TRAIN-003 and R-UNIT-003 under gdd-0.3: the month that counts the last course turn releases the
        /// trainee into its cohort's formation (Q43(b), "released in the turn its last course turn counts"). The same
        /// record, in place (27M-7), becomes an active soldier serving that formation; it keeps its item, and nothing is
        /// charged or refunded ("Its reserved Arms are neither charged again nor refunded on completion"). Its military
        /// qualification becomes the course's unit class (docs/GDD.md:372: a returned captive "keeps their id and military
        /// qualification"), so <see cref="Start"/> refuses it basic training again. Value: the course's unit class
        /// (unit_types.yaml); maintainer call on the source of the value, not an owner ruling. The cohort's first graduate
        /// claims the first free formation in deterministic order (owner ruling, 4 November 2026, Q25, #1699 comment
        /// 6623136049). The camp's-region limit and what counts as free (supplied to this fixture, not claimed by another
        /// cohort, and serving no one) are 53's maintainer call, not an owner ruling. A graduation in a month
        /// whose upkeep was already paid is refused (53's maintainer call, not an owner ruling: Advance, then PayUpkeep, in a
        /// month), so a formation always pays in its graduation month (27M-8).</summary>
        void Graduate(PopulationUnit trainee, Gdd03FixtureMilitaryCamp camp, int monthOrdinal)
        {
            if (lastPaidMonth is int paid && monthOrdinal <= paid)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Upkeep was already paid for month {0}: graduate with Advance before that month's PayUpkeep (27M-8).", paid));
            int cohort = cohortOf[trainee.Id];
            if (!cohortFormation.TryGetValue(cohort, out var formation))
            {
                var ledger = state.PopulationLedger.Units;
                formation = formations.Values.Where(f => string.Equals(f.Region, camp.Region, StringComparison.Ordinal)
                        && !cohortFormation.ContainsValue(f.Id)
                        && !ledger.Any(p => string.Equals(p.Assignment.Target, f.Id, StringComparison.Ordinal)))
                    .Select(f => f.Id).FirstOrDefault()
                    ?? throw new ArgumentException("No free formation in region " + camp.Region + " for this cohort.");
            }
            Commit(new[] { new PopulationUnit(trainee.Id, PopulationStatus.ActiveSoldier, trainee.ReceptionTicks, trainee.Location,
                new PopulationAssignment(PopulationAssignmentKind.ActiveService, formation), trainee.Profession, trainee.Tier,
                trainee.WorkXp, graduateQualification, trainee.CombatXp, trainee.Equipment, trainee.Captor, trainee.Home) });
            if (!cohortFormation.ContainsKey(cohort)) graduationMonth.Add(formation, monthOrdinal);
            cohortFormation[cohort] = formation;
            months.Remove(trainee.Id); issued.Remove(trainee.Id); cohortOf.Remove(trainee.Id);
        }

        /// <summary>R-UNIT-010 under gdd-0.3, the fixture's step 7: each formation this fixture graduated that still has an
        /// active soldier pays <c>military.active_money</c> once, independent of strength ("IL 0.05m per formation per
        /// turn, independent of strength"), from its graduation month (27M-8: "A formation pays military upkeep from the
        /// turn it graduates"; R-UNIT-010: "including a formation that graduated this turn"), so a month before a
        /// formation's graduation month charges it nothing. Trainees pay none. Only this fixture's formations count (53's
        /// maintainer call, not an owner ruling); it owns its Money. The shortfall is borrowed (R-MONEY-003); no Arms or
        /// strength change. Each month ordinal is paid once and never an earlier one; the cost is computed before any
        /// write.</summary>
        internal void PayUpkeep(int monthOrdinal)
        {
            ValidateInventory();
            if (monthOrdinal < 0) throw new ArgumentException("The month ordinal must be nonnegative.");
            if (lastPaidMonth is int paid && monthOrdinal <= paid)
                throw new ArgumentException("This month's upkeep was already paid, or the month is before the last one paid.");
            var serving = new HashSet<string>(state.PopulationLedger.Units
                .Where(p => p.Status == PopulationStatus.ActiveSoldier).Select(p => p.Assignment.Target!), StringComparer.Ordinal);
            int count = cohortFormation.Values.Count(f => serving.Contains(f) && graduationMonth[f] <= monthOrdinal);
            decimal cost = activeMoney * count;
            decimal paidNow = Math.Min(Money, cost), debt = Debt + (cost - paidNow);
            Money -= paidNow; Debt = debt; lastPaidMonth = monthOrdinal;
        }
    }
}
