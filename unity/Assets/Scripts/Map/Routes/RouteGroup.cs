#nullable enable
using UnityEngine;

namespace Statecraft.Game.Map.Routes
{
    /// <summary>Marks a group of route ribbons with its display window (RouteLayer.ShowAt switches the group on while the
    /// game date is inside it).</summary>
    public sealed class RouteGroup : MonoBehaviour
    {
        public RouteWindow Window;
    }
}
