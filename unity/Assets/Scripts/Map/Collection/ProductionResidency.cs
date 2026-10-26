#nullable enable
using System;
namespace Statecraft.Game.Map.Collection
{
    /// <summary>Presentation-only residency configuration, passed before any original-model request.</summary>
    internal sealed class ProductionResidency
    {
        internal const string Evidence = "owner ruling, 26 Oct 2026, #1273: Yes, build it";
        // No craft policy: owner ruling, 26 Oct 2026, #1273, "Craft visible at 60 km (Recommended)". The sea/air craft are
        // asked once a camera is in view, as before residency (SeaAirCollection), and are not unloaded on leave.
        internal readonly CollectionResidencyPolicy People, Rural, Special, Units, SharedRoad;
        internal readonly TownPolicy Towns;
        internal bool Diagnostics { get; }
        internal const int Version = 1;
        internal sealed class TownPolicy
        {
            internal readonly bool Enabled;
            internal readonly float Enter, Exit, Edge, ViewEnter, ViewExit;
            internal readonly int MaxEdges, Grace;
            internal readonly string Evidence;
            internal TownPolicy(bool enabled, float enter, float exit, float edge, int maxEdges, int grace, float viewEnter, float viewExit, string evidence)
            {
                if (!enabled || !float.IsFinite(enter) || !float.IsFinite(exit) || !float.IsFinite(edge)
                    || !float.IsFinite(viewEnter) || !float.IsFinite(viewExit) || enter < 0 || exit < enter || edge < 0
                    || maxEdges < 0 || grace < 0 || viewEnter < 0 || viewExit < viewEnter || string.IsNullOrWhiteSpace(evidence))
                    throw new ArgumentException("Explicit finite, enabled and ordered town residency policy required.");
                Enabled = enabled; Enter = enter; Exit = exit; Edge = edge; MaxEdges = maxEdges; Grace = grace;
                ViewEnter = viewEnter; ViewExit = viewExit; Evidence = evidence;
            }
        }
        internal ProductionResidency(CollectionResidencyPolicy people, CollectionResidencyPolicy rural,
            CollectionResidencyPolicy special, CollectionResidencyPolicy units,
            CollectionResidencyPolicy sharedRoad, TownPolicy towns, bool diagnostics = false)
        {
            People = people ?? throw new ArgumentNullException(nameof(people)); Rural = rural ?? throw new ArgumentNullException(nameof(rural));
            Special = special ?? throw new ArgumentNullException(nameof(special));
            Units = units ?? throw new ArgumentNullException(nameof(units)); SharedRoad = sharedRoad ?? throw new ArgumentNullException(nameof(sharedRoad));
            Towns = towns ?? throw new ArgumentNullException(nameof(towns)); Diagnostics = diagnostics;
        }
        internal static ProductionResidency Approved()
        {
            CollectionResidencyPolicy Category() => new CollectionResidencyPolicy(6000, 8000, 30000, 40000, 1, Evidence);
            return new ProductionResidency(Category(), Category(), Category(), Category(), Category(),
                new TownPolicy(true, 0, 0, 0, 0, 1, 120000, 160000, Evidence));
        }
    }
}
