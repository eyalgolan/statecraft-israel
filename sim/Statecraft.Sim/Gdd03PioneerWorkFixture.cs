using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>One resolved pioneer turn: the end-of-turn fixture state and the pioneer work quoted per advancing target.
    /// The quote is not applied to any project here; the future site/corridor integration owns that.</summary>
    internal sealed class PioneerWorkTurn
    {
        internal GameState State { get; }
        internal IReadOnlyDictionary<string, decimal> WorkByTarget { get; }
        internal PioneerWorkTurn(GameState state, SortedDictionary<string, decimal> work)
        {
            State = state;
            WorkByTarget = new ReadOnlyDictionary<string, decimal>(work);
        }
    }

    /// <summary>#1693 pioneer slice: the turn-end producer R-VET-001 holds, on initialized gdd-0.3 ledger fixtures only.
    /// No game-loop caller: an initialized ledger refuses the aggregate systems (R-POP-002), and the switch is #1689's last
    /// slice. Farmer work XP follows #1694; the tools charge is post-MVP (owner, 3 November 2026, answer 1).</summary>
    internal static class Gdd03PioneerWorkFixture
    {
        /// <summary>The caller supplies the turn-start state and the pioneer projects being built this turn, yishuv sites
        /// and corridor segments alike (owner, 3 November 2026, answer 5). Each pioneer record at Work on one of them
        /// gives completed-work evidence at its turn-start <c>pioneer.work_rate</c> (R-VET-002), and its
        /// <see cref="CivilianProfessionRules.AfterWork"/> result is committed once into a copy, in ascending id. The
        /// turn-start state is not changed, so resolving the same snapshot twice gives the same single tick (R-VET-001).
        /// Idle, travel, course and teaching records give no evidence and keep their tier and work XP (answer 4).</summary>
        internal static PioneerWorkTurn ResolveTurnEnd(Content content, GameState turnStart, IEnumerable<string> advancingProjects)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (turnStart == null) throw new ArgumentNullException(nameof(turnStart));
            if (advancingProjects == null) throw new ArgumentNullException(nameof(advancingProjects));
            if (!content.UsesPopulationLedger || !turnStart.UsesPopulationLedger || !turnStart.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Pioneer work XP requires an initialized gdd-0.3 ledger fixture.");
            var advancing = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in advancingProjects)
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Advancing projects need explicit target ids.");
                else advancing.Add(id);
            turnStart.PopulationLedger.Reconcile();

            var candidate = turnStart.Clone();
            var work = new SortedDictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var p in turnStart.PopulationLedger.Units)   // ascending id
            {
                if (p.Profession != CivilianProfession.Pioneer || p.Assignment.Kind != PopulationAssignmentKind.Work
                    || p.Assignment.Target == null || !advancing.Contains(p.Assignment.Target)) continue;
                var target = p.Assignment.Target;
                var quote = CivilianProfessionRules.OutputAtTurnStart(content, turnStart, p.Id);
                var next = CivilianProfessionRules.AfterWork(content, turnStart, p.Id,
                    new[] { new CivilianWorkEvidence(p.Id, target, CivilianWorkKind.PioneerAdvancement, quote) });
                // AfterWork returns the same record when it is not a working pioneer (e.g. reception not finished): no work.
                if (ReferenceEquals(next, p)) continue;
                work[target] = (work.TryGetValue(target, out var sum) ? sum : 0m) + quote;
                candidate.PopulationLedger.Replace(next);
            }
            candidate.PopulationLedger.Reconcile();
            return new PioneerWorkTurn(candidate, work);
        }
    }
}
