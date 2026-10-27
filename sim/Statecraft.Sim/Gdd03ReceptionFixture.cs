using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Statecraft.Sim
{
    /// <summary>Explicit test-only facility input. No map lookup, capacity inference or operating-date policy.
    /// Several artificial camp targets may share the exact same synthetic location.</summary>
    internal sealed class SyntheticReceptionCamp
    {
        internal string Target { get; }
        internal PopulationLocation Location { get; }
        internal bool Operating { get; }
        internal SyntheticReceptionCamp(string target, PopulationLocation location, bool operating)
        {
            if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("Explicit synthetic reception target required.");
            PopulationText.ValidateText(target);
            Target = target; Location = location ?? throw new ArgumentNullException(nameof(location)); Operating = operating;
        }
    }

    /// <summary>A new fixture state plus opening eligibility. Completing reception grants no work in this step.
    /// This is not a monthly settlement receipt or a replayable command.</summary>
    internal sealed class SyntheticReceptionTurn
    {
        internal GameState State { get; }
        internal IReadOnlyList<int> WorkEligibleAtOpening { get; }
        internal SyntheticReceptionTurn(GameState state, IEnumerable<int> eligible)
        {
            State = state;
            WorkEligibleAtOpening = new ReadOnlyCollection<int>(eligible.ToArray());
        }
    }

    /// <summary>#1696 ordinary-bed fixture only; R-CITY-004's proposed exact-gdd-0.3 exception.
    /// No production caller. Shared course reservations, travel, arrival producers, settlement and military
    /// credit are excluded. Uses the existing population ledger as the only person/progress authority.</summary>
    internal static class Gdd03ReceptionFixture
    {
        // Synthetic fixture eligibility: completed reception, free civilian, unassigned.
        // not proof of a job/field/project/course slot or permission to change an existing assignment.
        internal static bool CanStartCivilianActivity(Content content, PopulationUnit person)
        {
            var turns = content.RequirePopulationInteger("reception.turns");
            if (person == null) throw new ArgumentNullException(nameof(person));
            return person.ReceptionTicks == turns && person.Status == PopulationStatus.FreeCivilian
                && person.Assignment.Kind == PopulationAssignmentKind.Unassigned;
        }

        /// <summary>Pure fixture step: caller supplies an opening snapshot and the facilities operating throughout
        /// this whole turn. Only retained opening bed occupants earn a tick. New claims are made at the end,
        /// without work/progress; repeated evaluation of the same input produces the same candidate.
        /// No turn/date/RNG/resources are advanced. The future #1716 owner must commit exactly once per turn.</summary>
        internal static SyntheticReceptionTurn ResolveWholeTurn(Content content, GameState opening,
            IEnumerable<SyntheticReceptionCamp> syntheticCamps)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (opening == null) throw new ArgumentNullException(nameof(opening));
            if (syntheticCamps == null) throw new ArgumentNullException(nameof(syntheticCamps));
            if (!content.UsesPopulationLedger || !opening.UsesPopulationLedger || !opening.PopulationLedger.IsInitialized)
                throw new InvalidOperationException("Reception requires an initialized exact-gdd-0.3 synthetic fixture.");
            // M content/gdd03-balance.yaml:214-221: Agreed capacity 3 and duration 6; no fallback or duplicate constants.
            var capacity = content.RequirePopulationInteger("reception.capacity");
            var turns = content.RequirePopulationInteger("reception.turns");
            opening.PopulationLedger.Reconcile();
            var camps = new SortedDictionary<string, SyntheticReceptionCamp>(StringComparer.Ordinal);
            foreach (var camp in syntheticCamps)
            {
                if (camp == null) throw new ArgumentException("Null synthetic camp.");
                camps.Add(camp.Target, camp);
            }
            var people = opening.PopulationLedger.Units.OrderBy(p => p.Id).ToArray();
            if (people.Any(p => p.Assignment.Kind == PopulationAssignmentKind.Course
                || p.Assignment.Kind == PopulationAssignmentKind.Teaching))
                throw new NotSupportedException("Shared course/teacher reservation fixtures are outside ordinary reception.");
            var occupied = camps.Keys.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
            foreach (var p in people)
            {
                if (p.ReceptionTicks > turns) throw new ArgumentException("Reception exceeds the supplied catalogue limit.");
                if (p.Assignment.Kind != PopulationAssignmentKind.Reception) continue;
                // Invalid retained claims refuse atomically: no invented interruption, relocation or preemption.
                if (p.Status != PopulationStatus.Refugee || p.ReceptionTicks >= turns
                    || !camps.TryGetValue(p.Assignment.Target!, out var camp) || !camp.Operating
                    || !Colocated(p, camp) || ++occupied[camp.Target] > capacity)
                    throw new ArgumentException("Invalid retained ordinary-bed reservation; interruption policy is excluded.");
            }
            var eligible = people.Where(p => CanStartCivilianActivity(content, p)).Select(p => p.Id).ToArray();
            var candidate = opening.Clone();
            var ledger = candidate.PopulationLedger;
            foreach (var p in people.Where(p => p.Assignment.Kind == PopulationAssignmentKind.Reception))
            {
                // Every admitted incomplete occupant is below the positive integer limit: +1 cannot overflow.
                var progressed = p.WithReception(p.ReceptionTicks + 1);
                if (progressed.ReceptionTicks == turns)
                {
                    progressed = progressed.WithPlacement(PopulationStatus.FreeCivilian, p.Location,
                        PopulationAssignment.Unassigned, p.Captor, p.Home);
                    occupied[p.Assignment.Target!]--;
                }
                ledger.Replace(progressed);
            }
            // Synthetic fixture ordering: ascending unit id, then ordinal camp target.
            // New claims never progress here, including a bed released by completion in this step.
            foreach (var p in people.Where(p => p.Status == PopulationStatus.Refugee
                && p.ReceptionTicks < turns && p.Assignment.Kind == PopulationAssignmentKind.Unassigned))
            {
                var camp = camps.Values.FirstOrDefault(c => c.Operating && Colocated(p, c) && occupied[c.Target] < capacity);
                if (camp == null) continue;
                ledger.ChangeAssignment(p.Id, new PopulationAssignment(PopulationAssignmentKind.Reception, camp.Target));
                occupied[camp.Target]++;
            }
            ledger.Reconcile();
            return new SyntheticReceptionTurn(candidate, eligible);
        }

        static bool Colocated(PopulationUnit person, SyntheticReceptionCamp camp) =>
            person.Location != null && person.Location.Canonical() == camp.Location.Canonical();
    }
}
