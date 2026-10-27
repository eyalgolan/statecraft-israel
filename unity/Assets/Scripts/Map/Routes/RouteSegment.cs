#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>Marks a ribbon of a route a corridor follows (GDD Phase 4, #792): <see cref="Index"/> -1 for the whole route
    /// (the dated ribbon, drawn outside the GDD loop), or a segment's 0-based index (drawn in the GDD loop once it is built).
    /// RouteLayer.ShowAt switches them.</summary>
    public sealed class RouteSegment : MonoBehaviour
    {
        public string Route = "";
        public int Index = -1;
    }
}
