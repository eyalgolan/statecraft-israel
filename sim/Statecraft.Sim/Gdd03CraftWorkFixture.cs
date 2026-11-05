using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>One craftsman's actual work in a month (#2472): the population id, the month ordinal and the kind of work.</summary>
    internal sealed class Gdd03CraftWorkReceipt
    {
        internal int PopulationId { get; }
        internal int MonthOrdinal { get; }
        internal CraftTickActivity Activity { get; }
        internal Gdd03CraftWorkReceipt(int populationId, int monthOrdinal, CraftTickActivity activity)
        {
            PopulationId = populationId; MonthOrdinal = monthOrdinal; Activity = activity;
        }
    }

    /// <summary>R-CRAFT-002 under exact gdd-0.3, synthetic fixtures only (#2472): craftsman work XP over the #1689 ledger. No
    /// command, loader, turn or Money hookup. docs/gdd/training.md: "Apprentices become experts after
    /// `craft.expert_work_turns` actual turns in Industry or an arms workshop. Each turn counts once, regardless of
    /// output/work rate." The caller supplies the Work slots; only its Industry and Workshop slots count.</summary>
    internal sealed class Gdd03CraftWorkFixture
    {
        readonly GameState state;
        readonly Gdd03CraftArithmetic arithmetic;
        readonly SortedDictionary<string, CraftTickActivity> counting = new SortedDictionary<string, CraftTickActivity>(StringComparer.Ordinal);
        // Per fixture instance only (not saved), as in Gdd03CraftCourseFixture: the (id, month) receipts already applied.
        readonly SortedSet<(int Id, int Month)> applied = new SortedSet<(int Id, int Month)>();

        internal Gdd03CraftWorkFixture(Content content, GameState state, IEnumerable<Gdd03FixtureSlot> slots)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (slots == null) throw new ArgumentNullException(nameof(slots));
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Craftsman work XP requires an initialized gdd-0.3 ledger fixture.");
            arithmetic = Gdd03CraftArithmetic.Read(content.Gdd03Balance
                ?? throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.craft.expert_work_turns", "required under gdd-0.3"));
            this.state = state;
            foreach (var slot in slots)
                if (slot.Work == Gdd03FixtureWork.Industry) counting.Add(slot.Id, CraftTickActivity.Industry);
                else if (slot.Work == Gdd03FixtureWork.Workshop) counting.Add(slot.Id, CraftTickActivity.Workshop);
        }

        /// <summary>One receipt per apprentice or expert craftsman the month's snapshot has working an Industry or workshop
        /// slot (#1716 WorkEligibleIds), in ascending id. Idle, travel, course, teaching, service, building and farming
        /// give none, and so does a novice craftsman's workshop work (Q55: "No. Only the stated apprentice-to-expert
        /// progression counts").</summary>
        internal IReadOnlyList<Gdd03CraftWorkReceipt> Receipts(Gdd03SyntheticMonthContext month)
        {
            if (month == null) throw new ArgumentNullException(nameof(month));
            var working = new HashSet<int>(month.WorkEligibleIds);
            var receipts = new List<Gdd03CraftWorkReceipt>();
            foreach (var p in month.OpeningPeople.OrderBy(p => p.Id))
                if (working.Contains(p.Id) && p.Profession == CivilianProfession.Craftsman
                    && (p.Tier == CivilianTier.Apprentice || p.Tier == CivilianTier.Expert)
                    && p.Assignment.Target != null && counting.TryGetValue(p.Assignment.Target, out var activity))
                    receipts.Add(new Gdd03CraftWorkReceipt(p.Id, month.MonthOrdinal, activity));
            return receipts.AsReadOnly();
        }

        /// <summary>R-CRAFT-002: receipts are deduplicated by population id and month first, against each other and against
        /// those this fixture already applied; then each adds one work-XP tick (<see cref="Gdd03CraftArithmetic.WorkTick"/>),
        /// whatever the output or pay, in ascending id and month. An apprentice whose tick reaches `craft.expert_work_turns`
        /// is an expert from the next snapshot; the month's snapshot keeps the apprentice tier for that month's output. Only
        /// the tier and work XP change. Every receipt is checked before any record changes.</summary>
        internal void Apply(IEnumerable<Gdd03CraftWorkReceipt> receipts)
        {
            if (receipts == null) throw new ArgumentNullException(nameof(receipts));
            var fresh = new SortedDictionary<(int Id, int Month), CraftTickActivity>();
            foreach (var r in receipts)
            {
                if (r == null) throw new ArgumentException("A craftsman work receipt is missing.");
                // Only Industry and workshop months are receipts: any other would take the (id, month) key with no tick.
                if (r.Activity != CraftTickActivity.Industry && r.Activity != CraftTickActivity.Workshop)
                    throw new ArgumentException("A craftsman work receipt is for Industry or an arms workshop.");
                var key = (r.PopulationId, r.MonthOrdinal);
                if (!applied.Contains(key) && !fresh.ContainsKey(key)) fresh.Add(key, r.Activity);
            }
            state.PopulationLedger.Reconcile();
            var xp = new Dictionary<int, (CivilianTier Tier, int Xp)>();
            foreach (var (key, activity) in fresh)   // ascending id, then month
            {
                var p = state.PopulationLedger.Get(key.Id);
                if (p.Profession != CivilianProfession.Craftsman)
                    throw new ArgumentException("Only a craftsman earns craftsman work XP.");
                var (tier, workXp) = xp.TryGetValue(key.Id, out var sum) ? sum : (p.Tier, p.WorkXp);
                var tick = arithmetic.WorkTick(key.Id, tier, workXp, activity, true);
                xp[key.Id] = (tick.TierAfter, tick.WorkXp);
            }
            foreach (var (id, after) in xp)
            {
                var p = state.PopulationLedger.Get(id);
                state.PopulationLedger.Replace(new PopulationUnit(p.Id, p.Status, p.ReceptionTicks, p.Location, p.Assignment,
                    p.Profession, after.Tier, after.Xp, p.Qualification, p.CombatXp, p.Equipment, p.Captor, p.Home));
            }
            foreach (var key in fresh.Keys) applied.Add(key);
        }
    }
}
