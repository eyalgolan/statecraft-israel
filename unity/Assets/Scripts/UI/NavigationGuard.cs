#nullable enable
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Keeps the camera keys off the UI (integration bug B6). The project's UI action map, which UI Toolkit reads for
    /// navigation, no longer binds WASD or the arrows (they pan the camera); this guard also drops any directional
    /// NavigationMoveEvent (up, down, left, right) at the root before it can move focus, whatever device sent it, so panning
    /// the map never walks focus along the HUD and never lands it on New Game or End Turn. Tab and Shift+Tab (Next and
    /// Previous) still move focus, so the HUD stays keyboard-reachable; Enter and Space still press the focused button.</summary>
    public sealed class NavigationGuard : System.IDisposable
    {
        readonly VisualElement root;

        static readonly EventCallback<NavigationMoveEvent> OnMove = e =>
        {
            if (e.direction == NavigationMoveEvent.Direction.Next || e.direction == NavigationMoveEvent.Direction.Previous) return;
            (e.target as VisualElement)?.focusController?.IgnoreEvent(e);
            e.StopImmediatePropagation();
        };

        public NavigationGuard(VisualElement root) { this.root = root; root.RegisterCallback(OnMove, TrickleDown.TrickleDown); }

        public void Dispose() => root.UnregisterCallback(OnMove, TrickleDown.TrickleDown);
    }
}
