using System;
using System.Globalization;

namespace Statecraft.Sim
{
    // Pricing categories only, NOT an eligibility matrix or a profession/qualification mutation.
    public enum SyntheticWorkshopTier { Ordinary, Novice, Apprentice, Expert }

    /// <summary>Explicit opt-in synthetic rules snapshot; no loader registration or game-loop caller.</summary>
    public sealed class SyntheticWorkshopRules
    {
        public int StaffedTurns { get; }
        readonly decimal ordinaryCost, apprenticeCost, expertCost;

        SyntheticWorkshopRules(int turns, decimal ordinary, decimal apprentice, decimal expert)
        {
            StaffedTurns = turns;
            ordinaryCost = ordinary; apprenticeCost = apprentice; expertCost = expert;
        }

        public static SyntheticWorkshopRules FromCatalogue(Gdd03BalanceCatalogue catalogue)
        {
            if (catalogue == null) throw new ArgumentNullException(nameof(catalogue));
            if (catalogue.Ruleset != SaveGame.Gdd03Ruleset || catalogue.Values == null)
                throw new ArgumentException("Requires the gdd-0.3 catalogue.", nameof(catalogue));
            var turns = Read(catalogue, "workshop.staffed_turns", 1)[0];
            if (turns != decimal.Truncate(turns) || turns > int.MaxValue)
                throw new ArgumentException("Staffed duration must be a positive integer.", nameof(catalogue));
            var costs = Read(catalogue, "workshop.cost", 3);
            return new SyntheticWorkshopRules((int)turns, costs[0], costs[1], costs[2]);
        }

        static decimal[] Read(Gdd03BalanceCatalogue catalogue, string key, int count)
        {
            if (!catalogue.Values.TryGetValue(key, out var row) || row == null || row.Basis != "Agreed"
                || row.Values == null || row.Values.Count != count || !string.IsNullOrEmpty(row.Expression)
                || row.Parts == null || row.Parts.Count != 0)
                throw new ArgumentException("Missing or unresolved workshop rule: " + key, nameof(catalogue));
            var result = row.Values.ToArray();
            foreach (var value in result)
                if (value <= 0) throw new ArgumentException("Workshop quantities must be positive: " + key, nameof(catalogue));
            return result;
        }

        public decimal Price(SyntheticWorkshopTier tier)
        {
            switch (tier)
            {
                case SyntheticWorkshopTier.Ordinary:
                case SyntheticWorkshopTier.Novice: return ordinaryCost;
                case SyntheticWorkshopTier.Apprentice: return apprenticeCost;
                case SyntheticWorkshopTier.Expert: return expertCost;
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }
    }

    /// <summary>
    /// Immutable synthetic cycle, unrelated to GameState, facility validity, assignment or Money ledgers.
    /// Caller supplies reservation and staffed-turn facts. This is not a command, receipt validator,
    /// monthly scheduler, replay format, affordability evaluator or national spare-stock writer.
    /// </summary>
    public sealed class SyntheticWorkshopCycle
    {
        readonly SyntheticWorkshopRules rules;
        public string FixtureFacilityId { get; }
        public string? WorkerId { get; }
        public SyntheticWorkshopTier? WorkerTier { get; }
        public SyntheticWorkshopTier? PaidTier { get; }
        public decimal? PaidCost { get; }
        public int Progress { get; }
        public int CompletedArms { get; }
        public decimal? NextPrice => WorkerTier.HasValue ? rules.Price(WorkerTier.Value) : (decimal?)null;

        SyntheticWorkshopCycle(SyntheticWorkshopRules rules, string facilityId, string? workerId,
            SyntheticWorkshopTier? workerTier, SyntheticWorkshopTier? paidTier, decimal? paidCost,
            int progress, int completedArms)
        {
            this.rules = rules; FixtureFacilityId = facilityId; WorkerId = workerId; WorkerTier = workerTier;
            PaidTier = paidTier; PaidCost = paidCost; Progress = progress; CompletedArms = completedArms;
        }

        public static SyntheticWorkshopCycle Empty(SyntheticWorkshopRules rules, string fixtureFacilityId)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (string.IsNullOrWhiteSpace(fixtureFacilityId)) throw new ArgumentException("Synthetic facility ID required.", nameof(fixtureFacilityId));
            return new SyntheticWorkshopCycle(rules, fixtureFacilityId, null, null, null, null, 0, 0);
        }

        public SyntheticWorkshopCycle WithWorker(string fixturePersonId, SyntheticWorkshopTier tier)
        {
            if (string.IsNullOrWhiteSpace(fixturePersonId)) throw new ArgumentException("Synthetic person ID required.", nameof(fixturePersonId));
            rules.Price(tier); // validate category, without deciding eligibility
            return new SyntheticWorkshopCycle(rules, FixtureFacilityId, fixturePersonId, tier, PaidTier, PaidCost, Progress, CompletedArms);
        }

        public SyntheticWorkshopCycle WithoutWorker() =>
            new SyntheticWorkshopCycle(rules, FixtureFacilityId, null, null, PaidTier, PaidCost, Progress, CompletedArms);

        /// <summary>Acknowledges a caller-supplied synthetic reservation; never debits or borrows Money.
        /// Active cycles refuse a second reservation. No timing or automatic retry policy is implied.</summary>
        public SyntheticWorkshopCycle ReserveExplicitly(decimal reservedCost)
        {
            if (PaidCost.HasValue || !WorkerTier.HasValue)
                throw new InvalidOperationException("Requires a worker and no paid cycle.");
            if (reservedCost != NextPrice) throw new ArgumentException("Reservation must match the current tier quote.", nameof(reservedCost));
            return new SyntheticWorkshopCycle(rules, FixtureFacilityId, WorkerId, WorkerTier, WorkerTier, reservedCost, 0, CompletedArms);
        }

        /// <summary>One explicit synthetic staffed-turn event. Caller owns event uniqueness/timing.
        /// Empty or paused cycles do nothing. Completion reports one synthetic Arms, with no restart.</summary>
        public SyntheticWorkshopCycle StaffedTurn()
        {
            if (!PaidCost.HasValue || WorkerId == null) return this;
            int next = checked(Progress + 1);
            if (next == rules.StaffedTurns)
                return new SyntheticWorkshopCycle(rules, FixtureFacilityId, WorkerId, WorkerTier, null, null, 0, checked(CompletedArms + 1));
            return new SyntheticWorkshopCycle(rules, FixtureFacilityId, WorkerId, WorkerTier, PaidTier, PaidCost, next, CompletedArms);
        }

        // Diagnostic fixture text only; not a save, canonical state or player-facing string.
        public string ProgressText() => string.Format(CultureInfo.InvariantCulture, "{0}/{1}; paid={2}; arms={3}",
            Progress, rules.StaffedTurns, PaidCost.HasValue ? PaidCost.Value.ToString(CultureInfo.InvariantCulture) : "none", CompletedArms);
    }
}
