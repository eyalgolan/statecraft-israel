#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>PageUp, PageDown, Home and End scroll the open screen, or the open decision card's text, from the keyboard
    /// (accessibility audit H3, #41). The Why list, the event text and the Defence body hold labels only, so Tab never
    /// reaches into them, and the arrows pan the camera (NavigationGuard, ruling B6; Up and Down reach only an open screen's
    /// controls, audit L4). GameRoot polls the keys beside its Escape poll, so they work with or without focus. The ending,
    /// the turn report, the capture dialog and the info panel are labels too, so they get the keys as well (H3, K6, #2479),
    /// and their bars leave the Tab order (FocusScroll.NoScrollBarIsATabStop).</summary>
    public static class KeyScroll
    {
        public enum Key { PageUp, PageDown, Home, End }

        /// <summary>Each screen's main view (ScreenNav.Screens): what the keys scroll unless the focus is in another list
        /// on that screen, such as the Cabinet's candidates.</summary>
        static readonly Dictionary<string, string> MainViews = new Dictionary<string, string>
        {
            ["cabinet"] = "cabinet-seats", ["budget"] = "budget-rows", ["defence"] = "defence-body", ["why"] = "why-rows",
            ["encyclopedia"] = "encyclopedia-list", ["timeline"] = "timeline-list", ["objectives"] = "objectives-rows",
        };

        /// <summary>A page is the view's height less about one line of text, so the line at the edge stays in sight.</summary>
        const float Line = 24f;

        /// <summary>Scrolls the target view one page up or down, or to its top or bottom; false when there is no target or
        /// it is already there. The target: the ScrollView around the focused element if that is inside the open screen,
        /// else the open screen's main view, else (no screen open) the decision card's text while the card is open and not
        /// hidden, else the capture dialog's while it shows, else the info panel while it is in reach. Over all of those, the
        /// topmost of the ending and the turn report. Nothing while the start screen shows.</summary>
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
            if ((root.Q("root") ?? root).ClassListContains(OpeningPresenter.OpenClass)) return root.Q<ScrollView>("opening-scroll");
            if ((root.Q("root") ?? root).ClassListContains(StartScreenPresenter.OpenClass)) return null;
            // Topmost first, the order Escape closes them in (GameRoot.Update): the ending is over the turn report, and both
            // cover the screens, the card, the capture dialog and the info panel (ScreenNav.DisableCovered).
            if ((root.Q("root") ?? root).ClassListContains(EndScreenPresenter.OpenClass)) return root.Q<ScrollView>("end-screen-rows");
            if ((root.Q("root") ?? root).ClassListContains(TurnReportPresenter.OpenClass)) return root.Q<ScrollView>("turn-report-rows");
            if (screen == null)
            {
                if (cardOpen) return root.Q<ScrollView>("event-scroll");
                if ((root.Q("root") ?? root).ClassListContains(CaptureDialogPresenter.OpenClass)) return root.Q<ScrollView>("capture-scroll");
                // A modal, a wide screen or the start screen disables the panel (ScreenNav.DisableCovered); the USS hides it.
                var info = root.Q<ScrollView>("info-panel");
                return info != null && info.enabledInHierarchy && info.resolvedStyle.display != DisplayStyle.None ? info : null;
            }
            var open = root.Q<VisualElement>("screen-" + screen);
            var around = (root.panel?.focusController?.focusedElement as VisualElement)?.GetFirstAncestorOfType<ScrollView>();
            if (around != null && open != null && open.Contains(around)) return around;
            // An open encyclopedia page hides the list, so its body is the view then (#2095 review).
            if (screen == "encyclopedia" && root.Q<ScrollView>("encyclopedia-list") is ScrollView list && list.style.display.value == DisplayStyle.None)
                return root.Q<ScrollView>("encyclopedia-page-body");
            return MainViews.TryGetValue(screen, out var main) ? root.Q<ScrollView>(main) : null;
        }
    }
}
