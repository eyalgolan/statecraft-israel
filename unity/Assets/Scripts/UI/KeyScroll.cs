#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>PageUp, PageDown, Home and End scroll the open screen, or the open decision card's text, from the keyboard
    /// (accessibility audit H3, #41). The Why list, the event text and the Defence body hold labels only, so Tab never
    /// reaches into them, and the arrows pan the camera and never move focus (NavigationGuard, ruling B6). GameRoot polls
    /// the keys beside its Escape poll, so they work with or without focus.</summary>
    public static class KeyScroll
    {
        public enum Key { PageUp, PageDown, Home, End }

        /// <summary>Each screen's main view (ScreenNav.Screens): what the keys scroll unless the focus is in another list
        /// on that screen, such as the Cabinet's candidates.</summary>
        static readonly Dictionary<string, string> MainViews = new Dictionary<string, string>
        {
            ["cabinet"] = "cabinet-seats", ["budget"] = "budget-rows", ["defence"] = "defence-body", ["why"] = "why-rows",
        };

        /// <summary>A page is the view's height less about one line of text, so the line at the edge stays in sight.</summary>
        const float Line = 24f;

        /// <summary>Scrolls the target view one page up or down, or to its top or bottom; false when there is no target or
        /// it is already there. The target: the ScrollView around the focused element if that is inside the open screen,
        /// else the open screen's main view, else (no screen open) the decision card's text while the card is open and not
        /// hidden. Nothing while the start screen shows.</summary>
        public static bool Press(VisualElement root, string? screen, bool cardOpen, Key key)
        {
            var view = Target(root, screen, cardOpen);
            if (view == null) return false;
            float high = view.verticalScroller.highValue, y = view.scrollOffset.y;
            if (float.IsNaN(high) || high <= 0f) return false;   // everything fits: nothing to scroll
            float page = Mathf.Max(Line, view.contentViewport.layout.height - Line);
            float to = Mathf.Clamp(key switch { Key.PageUp => y - page, Key.PageDown => y + page, Key.Home => 0f, _ => high }, 0f, high);
            if (Mathf.Abs(to - y) < 0.5f) return false;
            view.scrollOffset = new Vector2(view.scrollOffset.x, to);
            return true;
        }

        static ScrollView? Target(VisualElement root, string? screen, bool cardOpen)
        {
            if ((root.Q("root") ?? root).ClassListContains(StartScreenPresenter.OpenClass)) return null;
            if (screen == null) return cardOpen ? root.Q<ScrollView>("event-scroll") : null;
            var open = root.Q<VisualElement>("screen-" + screen);
            var around = (root.panel?.focusController?.focusedElement as VisualElement)?.GetFirstAncestorOfType<ScrollView>();
            if (around != null && open != null && open.Contains(around)) return around;
            return MainViews.TryGetValue(screen, out var main) ? root.Q<ScrollView>(main) : null;
        }
    }
}
