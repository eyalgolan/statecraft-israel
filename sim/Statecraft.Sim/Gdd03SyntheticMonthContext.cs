using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Synthetic #1716 contract, built by test fixtures (<see cref="CreateSynthetic"/>) and, for a game with an
    /// initialized Historical ledger, by <see cref="CreateLive"/> (#1725 PR 7), which no turn calls yet.
    /// GDD 605277df docs/gdd/loop-switch.md:17-39, except its pre-arrival residents: housing effects count residents after
    /// the step's arrivals (27M-15, owner, 1 November 2026), which an opening snapshot cannot hold, so it holds none.
    /// Frozen eligibility is only an upper bound: future consumers must recheck survival,
    /// location, control and facility availability immediately before using an entry.
    /// This object neither executes phases nor authorizes work, settlement or persistence.</summary>
    internal sealed class Gdd03SyntheticMonthContext
    {
        internal int MonthOrdinal { get; }
        internal IReadOnlyList<PopulationUnit> OpeningPeople { get; }
        internal IReadOnlyList<int> WorkEligibleIds { get; }
        internal IReadOnlyList<int> CourseEligibleIds { get; }
        internal IReadOnlyList<int> ReceptionEligibleIds { get; }
        internal IReadOnlyList<Gdd03SyntheticFacilitySnapshot> Facilities { get; }
        internal IReadOnlyList<Gdd03SyntheticHousingSnapshot> Housing { get; }
        internal IReadOnlyList<Gdd03SyntheticPendingEntry> PendingEntries { get; }

        private Gdd03SyntheticMonthContext(int monthOrdinal, PopulationUnit[] people,
            Gdd03SyntheticFacilitySnapshot[] facilities, Gdd03SyntheticHousingSnapshot[] housing,
            Gdd03SyntheticPendingEntry[] pending)
        {
            MonthOrdinal = monthOrdinal;
            OpeningPeople = Freeze(people);
            Facilities = Freeze(facilities); Housing = Freeze(housing); PendingEntries = Freeze(pending);
            var available = new HashSet<string>(facilities.Where(f => f.AvailableAtMonthFreeze && f.OpeningCapacity > 0)
                .Select(f => f.Id), StringComparer.Ordinal);
            int[] Eligible(PopulationAssignmentKind kind) => people.Where(p => p.Assignment.Kind == kind
                && p.Assignment.Target != null && available.Contains(p.Assignment.Target)).Select(p => p.Id).ToArray();
            // One immutable PopulationAssignment per distinct population id makes these sets disjoint.
            WorkEligibleIds = Freeze(Eligible(PopulationAssignmentKind.Work));
            CourseEligibleIds = Freeze(Eligible(PopulationAssignmentKind.Course));
            ReceptionEligibleIds = Freeze(Eligible(PopulationAssignmentKind.Reception));
        }

        // Sole constructor entry point; callers exist only in synthetic tests. No GameState,
        // resolver, Content, save, RNG, region binding or production-ledger dependency.
        // People retain #1689 PopulationUnit ids/immutable assignments, never parallel city counts.
        // This structural boundary does not replace PopulationLedger's full binding validation.
        internal static Gdd03SyntheticMonthContext CreateSynthetic(string ruleset, int monthOrdinal,
            IEnumerable<PopulationUnit> openingPeople,
            IEnumerable<Gdd03SyntheticFacilitySnapshot> facilities,
            IEnumerable<Gdd03SyntheticHousingSnapshot> housing,
            IEnumerable<Gdd03SyntheticPendingEntry> pendingEntries)
        {
            if (!string.Equals(ruleset, SaveGame.Gdd03Ruleset, StringComparison.Ordinal) || monthOrdinal < 0)
                throw new ArgumentException("Synthetic monthly context requires exact gdd-0.3 identity and a nonnegative month ordinal.");
            var people = Materialize(openingPeople);
            var capacity = Materialize(facilities);
            var conditions = Materialize(housing);
            var pending = Materialize(pendingEntries);
            if (people.Select(p => p.Id).Distinct().Count() != people.Length)
                throw new ArgumentException("Population ids must be unique.");
            var peopleById = people.ToDictionary(p => p.Id);
            Unique(capacity.Select(f => f.Id)); Unique(conditions.Select(h => h.Id)); Unique(pending.Select(e => e.Key));
            var targets = new HashSet<string>(capacity.Select(f => f.Id), StringComparer.Ordinal);
            foreach (var person in people)
            {
                var kind = person.Assignment.Kind;
                if ((kind == PopulationAssignmentKind.Work || kind == PopulationAssignmentKind.Course
                    || kind == PopulationAssignmentKind.Reception || kind == PopulationAssignmentKind.Teaching)
                    && (person.Assignment.Target == null || !targets.Contains(person.Assignment.Target)))
                    throw new ArgumentException("Monthly work/course/reception/teacher target must exist in the synthetic opening inventory.");
                if ((kind == PopulationAssignmentKind.Work || kind == PopulationAssignmentKind.Teaching)
                    && person.Status != PopulationStatus.FreeCivilian
                    || kind == PopulationAssignmentKind.Course && person.Status != PopulationStatus.Trainee
                    || kind == PopulationAssignmentKind.Reception && person.Status != PopulationStatus.Refugee
                        && person.Status != PopulationStatus.FreeCivilian)
                    throw new ArgumentException("Monthly assignment has an incompatible population status.");
                var teacher = person.Assignment.TeacherId;
                if (teacher.HasValue && (!peopleById.TryGetValue(teacher.Value, out var t)
                    || t.Assignment.Kind != PopulationAssignmentKind.Teaching
                    || t.Assignment.Target != person.Assignment.Target))
                    throw new ArgumentException("Course teacher must reference an opening teacher at the same target.");
            }
            return new Gdd03SyntheticMonthContext(monthOrdinal, people.OrderBy(p => p.Id).ToArray(),
                capacity.OrderBy(f => f.Id, StringComparer.Ordinal).ToArray(),
                conditions.OrderBy(h => h.Id, StringComparer.Ordinal).ToArray(),
                pending.OrderBy(e => e.Key, StringComparer.Ordinal).ToArray());
        }

        /// <summary>#1725 PR 7 (#1716): the month's frozen opening snapshot of a game with an initialized Historical ledger.
        /// Owner, 8 November 2026, "One snapshot per turn (Recommended)": one month snapshot is taken at the start of the
        /// turn, after the player's orders, and harvest, Industry income and Food all read it. The opening people are the
        /// ledger's living own records (<see cref="PopulationLedger.Units"/>), in id order; the facility entries are the
        /// ledger's bound Work targets that are authored fields (Gdd03FieldFixture ids,
        /// field/q,r), each available at the freeze with `field.slots`
        /// capacity, and (#1725 PR 3) its Industry targets, each available with its own capacity, in ordinal order. Owner, 8 November 2026, "Yes, recheck (Recommended)": that availability is only an upper bound, and a
        /// consumer rechecks an entry (its id and opening record are kept) against the live state before using it. The
        /// month ordinal is the turn (<see cref="GameState.Turn"/>). Every check comes before anything is built, and it
        /// writes nothing: the state, its hash and the ledger are unchanged. A synthetic or Open ledger, an aggregate game
        /// and any other content are refused (R-POP-002); so is a ledger with a Work, Course, Reception or Teaching target
        /// that is neither a field nor an Industry target, whose entry no ruling defines yet (<see cref="CreateSynthetic"/>'s inventory check).</summary>
        internal static Gdd03SyntheticMonthContext CreateLive(Content content, GameState state)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (state == null) throw new ArgumentNullException(nameof(state));
            var ledger = state.PopulationLedger;
            if (!content.UsesPopulationLedger || !ledger.IsInitialized || !ledger.IsHistorical)
                throw new NotSupportedException("A live month snapshot needs an initialized Historical gdd-0.3 population ledger (R-POP-002).");
            ledger.Reconcile();
            var slots = Gdd03FieldRates.FromCatalogue(content.Gdd03Balance
                ?? throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.field.slots", "required under gdd-0.3")).SlotsPerField;
            var authored = new HashSet<string>(Gdd03FieldFixture.Fields(content).Select(f => f.Id), StringComparer.Ordinal);
            // #1725 PR 3, an agent's reading (not an owner ruling): an Industry target is an entry as a field is, available at the freeze with its own
            // capacity (the city's job_slots), an upper bound that its consumer (#2469's Industry income) rechecks.
            var fields = ledger.Bindings.Targets.Where(t => t.Kind == PopulationAssignmentKind.Work
                    && (authored.Contains(t.Id) || t.WorkKind == PopulationWorkKind.Industry))
                .Select(t => new Gdd03SyntheticFacilitySnapshot(t.Id, true, t.Capacity ?? slots)).ToArray();
            return CreateSynthetic(SaveGame.Gdd03Ruleset, state.Turn, ledger.Units, fields,
                Array.Empty<Gdd03SyntheticHousingSnapshot>(), Array.Empty<Gdd03SyntheticPendingEntry>());
        }

        static T[] Materialize<T>(IEnumerable<T> source) where T : class
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var copy = source.ToArray();
            if (copy.Any(item => item == null)) throw new ArgumentException("Null snapshot records are not allowed.");
            return copy;
        }
        static IReadOnlyList<T> Freeze<T>(T[] ownedCopy) => new ReadOnlyCollection<T>(ownedCopy);
        static void Unique(IEnumerable<string> ids)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in ids) if (!seen.Add(id)) throw new ArgumentException("Duplicate monthly key.");
        }
        internal static string Reference(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Explicit synthetic reference required.");
            PopulationText.ValidateText(value);
            return value;
        }
    }

    /// <summary>Opaque synthetic assignment target; no region, host, project kind or balance default.
    /// Capacity/availability are opening inputs, never updated on same-month completion.
    /// Owners still define capacity units, staffing limits and completion-turn upkeep.</summary>
    internal sealed class Gdd03SyntheticFacilitySnapshot
    {
        internal string Id { get; }
        internal bool AvailableAtMonthFreeze { get; }
        internal int OpeningCapacity { get; }
        internal Gdd03SyntheticFacilitySnapshot(string id, bool availableAtMonthFreeze, int openingCapacity)
        {
            if (openingCapacity < 0) throw new ArgumentException("Opening capacity must be nonnegative.");
            Id = Gdd03SyntheticMonthContext.Reference(id);
            AvailableAtMonthFreeze = availableAtMonthFreeze; OpeningCapacity = openingCapacity;
        }
    }

    /// <summary>Opaque synthetic housing handle, not a regional record. GDD balance.md:100 at 605277df
    /// defines housing.condition_max = 1 (Agreed fraction). No surcharge/decay calculation.</summary>
    internal sealed class Gdd03SyntheticHousingSnapshot
    {
        internal string Id { get; }
        internal decimal ConditionBeforeDeterioration { get; }
        internal Gdd03SyntheticHousingSnapshot(string id, decimal conditionBeforeDeterioration)
        {
            if (conditionBeforeDeterioration < 0m || conditionBeforeDeterioration > 1m)
                throw new ArgumentException("Condition must be a fraction within the catalogue maximum.");
            Id = Gdd03SyntheticMonthContext.Reference(id); ConditionBeforeDeterioration = conditionBeforeDeterioration;
        }
    }

    /// <summary>Keyed opaque synthetic fact, not a Money/indicator settlement interface.
    /// No sign convention, currency conversion, clamp, summation or application is defined here.
    /// Key uniqueness prevents ambiguous duplicate inputs; persistent one-time tracking belongs to owners.</summary>
    internal sealed class Gdd03SyntheticPendingEntry
    {
        internal string Key { get; }
        internal string Unit { get; }
        internal decimal Amount { get; }
        internal Gdd03SyntheticPendingEntry(string key, string unit, decimal amount)
        {
            Key = Gdd03SyntheticMonthContext.Reference(key); Unit = Gdd03SyntheticMonthContext.Reference(unit); Amount = amount;
        }
    }
}
