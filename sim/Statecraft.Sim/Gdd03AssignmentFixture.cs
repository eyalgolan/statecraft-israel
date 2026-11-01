using System;
using System.Collections.Generic;
using System.Linq;

namespace Statecraft.Sim
{
    // All entry points are internal. Only synthetic tests opt in; no command, loader or turn hookup.
    internal enum Gdd03FixtureWork { Industry, Builder, Farm, Workshop }
    internal enum Gdd03FixtureAssignmentResult { Applied, NoOp }

    internal sealed class Gdd03FixtureSlot
    {
        internal string Id { get; }
        internal Gdd03FixtureWork Work { get; }
        internal int Capacity { get; }
        internal string Label { get; }
        internal Gdd03FixtureSlot(string id, Gdd03FixtureWork work, int capacity, string label)
        {
            PopulationText.ValidateText(id); PopulationText.ValidateText(label);
            if (string.IsNullOrWhiteSpace(id) || capacity < 0 || !Enum.IsDefined(typeof(Gdd03FixtureWork), work))
                throw new ArgumentException("Explicit fixture slot identity, work kind and nonnegative capacity required.");
            Id = id; Work = work; Capacity = capacity; Label = label;
        }
    }

    /// <summary>Frozen typed catalogue vector. Quotes rates; never awards Money or work receipts.</summary>
    internal sealed class Gdd03IndustryRates
    {
        readonly decimal ordinary, apprentice, expert;
        Gdd03IndustryRates(decimal ordinary, decimal apprentice, decimal expert)
        { this.ordinary = ordinary; this.apprentice = apprentice; this.expert = expert; }
        internal static Gdd03IndustryRates Read(Content content)
        {
            if (!content.UsesPopulationLedger) throw new InvalidOperationException("Industry fixture rates require gdd-0.3.");
            return Read(content.Gdd03Balance);
        }
        internal static Gdd03IndustryRates Read(Gdd03BalanceCatalogue? catalogue)
        {
            if (catalogue == null || catalogue.Ruleset != SaveGame.Gdd03Ruleset || catalogue.Values == null
                || !catalogue.Values.TryGetValue("industry.income", out var row) || row == null
                || row.Basis != "Agreed" || row.Values == null || row.Values.Count != 3 || row.Values.Any(x => x <= 0)
                || row.Parts == null || row.Parts.Count != 0 || !string.IsNullOrEmpty(row.Expression))
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.industry.income",
                    "requires three positive Agreed decimal rates; no Deferred or legacy fallback");
            return new Gdd03IndustryRates(row.Values[0], row.Values[1], row.Values[2]);
        }
        internal decimal For(CivilianTier tier) => tier switch {
            CivilianTier.None or CivilianTier.Novice or CivilianTier.Trained or CivilianTier.Veteran => ordinary,
            CivilianTier.Apprentice => apprentice,
            CivilianTier.Expert => expert,
            _ => throw new ArgumentException("Unknown fixture tier.", nameof(tier)) };
    }

    /// <summary>R-CITY-001/002: frozen synthetic capacity overlay, never a second population ledger.
    /// Only explicit reception-complete free civilians moving among generic Work targets are supported.
    /// Feature-owned course/teaching/service transitions remain outside this fixture. Reassigning the same Work slot is a no-op.
    /// Single-threaded like the ledger; callers cannot reuse this overlay as a production authority.</summary>
    internal sealed class Gdd03AssignmentFixture
    {
        readonly GameState state;
        readonly SortedDictionary<string, Gdd03FixtureSlot> slots;
        readonly Gdd03IndustryRates rates;
        readonly int receptionTurns;
        internal Gdd03AssignmentFixture(Content content, GameState state, IEnumerable<Gdd03FixtureSlot> inventory)
        {
            if (!content.UsesPopulationLedger || !state.UsesPopulationLedger || !state.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Assignments require an initialized gdd-0.3 synthetic fixture.");
            this.state = state;
            rates = Gdd03IndustryRates.Read(content);
            receptionTurns = content.RequirePopulationInteger("reception.turns");
            if (receptionTurns != state.PopulationLedger.ReceptionLimit)
                throw new ContentException(Gdd03BalanceCatalogue.FileName, "values.reception.turns",
                    "must match the initialized population ledger");
            slots = new SortedDictionary<string, Gdd03FixtureSlot>(StringComparer.Ordinal);
            foreach (var slot in inventory) slots.Add(slot.Id, slot); // duplicate aliases cannot add capacity
            ValidateInventory();
        }
        void ValidateInventory()
        {
            state.PopulationLedger.Reconcile();
            foreach (var p in state.PopulationLedger.Units.Where(p => p.Assignment.Kind == PopulationAssignmentKind.Work))
                if (p.Assignment.Target == null || !slots.ContainsKey(p.Assignment.Target))
                    throw new ArgumentException("Work reservation is outside the frozen fixture inventory.");
            foreach (var slot in slots.Values)
                if (state.PopulationLedger.Units.Count(p => p.Assignment.Kind == PopulationAssignmentKind.Work && p.Assignment.Target == slot.Id) > slot.Capacity)
                    throw new ArgumentException("Fixture inventory is over capacity.");
            // Synthetic composition only: the ledger owns workers; immutable regional pools bound pooled job occupancy.
            if (state.PlayerRegions.Count == 0) return;
            foreach (var group in state.PopulationLedger.Units.Where(p => p.Assignment.Kind == PopulationAssignmentKind.Work)
                .Where(p => slots[p.Assignment.Target!].Work == Gdd03FixtureWork.Industry || slots[p.Assignment.Target!].Work == Gdd03FixtureWork.Workshop)
                .GroupBy(p => (Region: p.Location!.Region, Work: slots[p.Assignment.Target!].Work)))
            {
                if (!state.PlayerRegions.TryGetValue(group.Key.Region, out var pool))
                    throw new ArgumentException("Fixture worker has no initialized regional pool.");
                var capacity = group.Key.Work == Gdd03FixtureWork.Industry ? pool.IndustrySlots : pool.WorkshopSlots;
                if (group.Count() > capacity) throw new ArgumentException("Fixture regional job occupancy is over capacity.");
            }
        }
        bool Eligible(PopulationUnit p) => p.Status == PopulationStatus.FreeCivilian && p.ReceptionTicks == receptionTurns;
        internal Gdd03FixtureAssignmentResult Assign(int id, string? target)
        {
            ValidateInventory();
            var ledger = state.PopulationLedger; var person = ledger.Get(id);
            if (!Eligible(person) || (person.Assignment.Kind != PopulationAssignmentKind.Unassigned && person.Assignment.Kind != PopulationAssignmentKind.Work))
                throw new ArgumentException("Only reception-complete free civilian work transitions are in this fixture.");
            if (target != null)
            {
                if (!slots.TryGetValue(target, out var slot)) throw new ArgumentException("Unknown fixture slot.");
                // Owner, 28 October 2026 (#1668 Q34): assigning the same job is a no-op.
                if (person.Assignment.Target == target) return Gdd03FixtureAssignmentResult.NoOp;
                if (ledger.Units.Count(p => p.Assignment.Kind == PopulationAssignmentKind.Work && p.Assignment.Target == target) >= slot.Capacity)
                    throw new ArgumentException("Fixture destination is full.");
                if (state.PlayerRegions.Count > 0 && (slot.Work == Gdd03FixtureWork.Industry || slot.Work == Gdd03FixtureWork.Workshop))
                {
                    if (person.Location == null || !state.PlayerRegions.TryGetValue(person.Location.Region, out var pool))
                        throw new ArgumentException("Fixture worker has no initialized regional pool.");
                    var used = ledger.Units.Count(p => p.Id != id && p.Assignment.Kind == PopulationAssignmentKind.Work
                        && p.Location?.Region == person.Location.Region && slots[p.Assignment.Target!].Work == slot.Work);
                    var capacity = slot.Work == Gdd03FixtureWork.Industry ? pool.IndustrySlots : pool.WorkshopSlots;
                    if (used >= capacity) throw new ArgumentException("Fixture regional destination is full.");
                }
            }
            var assignment = target == null ? PopulationAssignment.Unassigned : new PopulationAssignment(PopulationAssignmentKind.Work, target);
            // Replace validates the complete candidate before publishing it. Never release first.
            ledger.Replace(person.WithPlacement(person.Status, person.Location, assignment, person.Captor, person.Home));
            return Gdd03FixtureAssignmentResult.Applied;
        }
        /// <summary>Rate quotation for an explicitly supplied frozen fixture, not a month snapshot or payable receipt.
        /// Label is never identity. No Money/Food/XP/state mutation, time scaling or production consumer.</summary>
        internal decimal IndustryRate(string target)
        {
            ValidateInventory();
            if (!slots.TryGetValue(target, out var slot)) throw new ArgumentException("Unknown fixture slot.");
            if (slot.Work != Gdd03FixtureWork.Industry) return 0m;
            decimal total = 0m;
            foreach (var person in state.PopulationLedger.Units.Where(p => p.Assignment.Kind == PopulationAssignmentKind.Work && p.Assignment.Target == target).OrderBy(p => p.Id))
                if (Eligible(person)) total = checked(total + rates.For(person.Tier));
            return total;
        }
    }
}
