#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Keeps the camera keys off the UI (integration bug B6). The project's UI action map, which UI Toolkit reads for
    /// navigation, no longer binds WASD or the arrows (they pan the camera); this guard also drops any directional
    /// NavigationMoveEvent (up, down, left, right) at the root before it can move focus, whatever device sent it, so panning
    /// the map never walks focus along the HUD and never lands it on New Game or End Turn. Tab and Shift+Tab (Next and
    /// Previous) still move focus, so the HUD stays keyboard-reachable; Enter and Space still press the focused button.
    /// One exception, accessibility audit L4: while a screen is open, Up and Down move the focus between that screen's rows
    /// (<see cref="Step"/>), so a long list is not Tab-only. The owner's answer on #2340, Q27: "do the recommended for all"
    /// (https://github.com/eyalgolan/statecraft-israel/issues/2340#issuecomment-6623136558); the recommended option let
    /// Up/Down move between rows only while a screen is open. Left and Right stay dropped, and with no screen open all four
    /// are, as B6 ruled.</summary>
    public sealed class NavigationGuard : System.IDisposable
    {
        readonly VisualElement root;
        readonly System.Func<string?>? openScreen;
        readonly EventCallback<NavigationMoveEvent> onMove;

        /// <param name="openScreen">The open screen's name (ScreenNav.Current), or null for none. Without it every
        /// directional move is dropped.</param>
        public NavigationGuard(VisualElement root, System.Func<string?>? openScreen = null)
        {
            this.root = root; this.openScreen = openScreen;
            onMove = e =>
            {
                if (e.direction == NavigationMoveEvent.Direction.Next || e.direction == NavigationMoveEvent.Direction.Previous) return;
                if (e.direction == NavigationMoveEvent.Direction.Up || e.direction == NavigationMoveEvent.Direction.Down)
                    Step(e.direction == NavigationMoveEvent.Direction.Up);
                (e.target as VisualElement)?.focusController?.IgnoreEvent(e);
                e.StopImmediatePropagation();
            };
            root.RegisterCallback(onMove, TrickleDown.TrickleDown);
        }

        public void Dispose() => root.UnregisterCallback(onMove, TrickleDown.TrickleDown);

        /// <summary>Up or Down while a screen is open (audit L4): focuses the nearest visible, enabled control in the row above
        /// or below the focused one, the one closest to it across, and never leaves the screen or presses anything. With the
        /// focus outside the screen, Down takes the screen's first control and Up its last, in document order. GameRoot calls it
        /// for the arrow keys, which the UI map does not read (B6). False when no screen is open or nothing moved.</summary>
        public bool Step(bool up)
        {
            var name = openScreen?.Invoke();
            var screen = name == null ? null : root.Q<VisualElement>("screen-" + name);
            if (screen == null) return false;
            var rows = new List<VisualElement>();
            screen.Query<VisualElement>().ForEach(e => { if (Reachable(e, screen)) rows.Add(e); });
            if (rows.Count == 0) return false;
            var at = root.panel?.focusController?.focusedElement as VisualElement;
            var to = at == null || !rows.Contains(at) ? rows[up ? rows.Count - 1 : 0] : Beyond(rows, at.worldBound, up);
            if (to == null || to == at) return false;
            to.Focus();
            return true;
        }

        /// <summary>The control in the nearest row wholly above (or below) <paramref name="from"/>: the closest edge picks the
        /// row, everything overlapping it vertically is in that row, and the one nearest across wins (the first in
        /// document order on a tie). A 1 px overlap is allowed for touching borders.</summary>
        static VisualElement? Beyond(List<VisualElement> rows, Rect from, bool up)
        {
            VisualElement? edge = null;
            foreach (var r in rows)
            {
                var b = r.worldBound;
                if (up ? b.yMax > from.yMin + 1f : b.yMin < from.yMax - 1f) continue;
                if (edge == null || (up ? b.yMax > edge.worldBound.yMax : b.yMin < edge.worldBound.yMin)) edge = r;
            }
            if (edge == null) return null;
            Rect row = edge.worldBound;
            VisualElement? best = null; float bestDx = float.MaxValue;
            foreach (var r in rows)
            {
                var b = r.worldBound;
                if (b.yMin >= row.yMax || b.yMax <= row.yMin) continue;
                if (up ? b.yMax > from.yMin + 1f : b.yMin < from.yMax - 1f) continue;
                var dx = Mathf.Abs(b.center.x - from.center.x);
                if (dx < bestDx) { best = r; bestDx = dx; }
            }
            return best;
        }

        /// <summary>A control Tab could reach: focusable, enabled, laid out, and neither it nor anything up to the screen
        /// hidden.</summary>
        static bool Reachable(VisualElement e, VisualElement screen)
        {
            if (!e.focusable || !e.canGrabFocus || e.tabIndex < 0 || !e.enabledInHierarchy) return false;
            for (var p = e; p != null && p != screen.parent; p = p.parent)
                if (p.resolvedStyle.display == DisplayStyle.None || p.resolvedStyle.visibility == Visibility.Hidden) return false;
            var b = e.worldBound;
            return b.width > 0f && b.height > 0f && !float.IsNaN(b.x) && !float.IsNaN(b.y);
        }
    }
}
