#nullable enable
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The top-bar buttons open the Cabinet, Budget, Defence and Why screens, one at a time; pressing the open
    /// screen's button again closes it. Every press toggles, synchronously, with no debounce: two presses in one frame (a
    /// double click, or a click plus Enter) open and close again, so the result depends only on the number of presses
    /// (HudTests.TwoPressesInOneFrameToggleTwice; playtest 1, item 11).</summary>
    public sealed class ScreenNav : System.IDisposable
    {
        public static readonly string[] Screens = { "cabinet", "budget", "defence", "why" };
        /// <summary>The class on the open screen's button (Statecraft.uss lights its ring).</summary>
        public const string OpenClass = "nav-open";
        /// <summary>The class on the "root" element while a wide screen (class <see cref="WideClass"/>: Cabinet, Defence) is
        /// open: Statecraft.uss hides the info panel it covers, so its buttons take no focus and no clicks.</summary>
        public const string WideOpenClass = "wide-screen-open", WideClass = "screen-wide";
        /// <summary>The class on the "root" element while any screen is open: Statecraft.uss hides the decision layer under it,
        /// and in the two-row (narrow) HUD, where every screen takes the full width, the minimap column and the info panel
        /// too (#210).</summary>
        public const string ScreenOpenClass = "screen-open";

        /// <summary>Disables each HUD panel that a class on the "root" element hides, and enables it again when none does: a
        /// child of an undisplayed element can still take the keyboard focus, so hiding alone leaves its buttons in Tab's
        /// reach (#400). The start screen covers the top bar, the minimap column, the decision layer and the info panel; any
        /// open screen covers the decision layer (it sits where the card does), and in the two-row HUD also the minimap
        /// column and the info panel; a wide screen covers the info panel; the open decision card's backdrop
        /// (EventCardPresenter.OpenClass, with no start screen or screen over it: ModalClass, set here) covers the minimap
        /// column and the info panel; an End Turn outcome clip (OutcomePresenter.OpenClass) covers the top bar, the minimap
        /// column, the info panel and the decision layer. Called wherever one of those classes changes (StartScreenPresenter,
        /// Open, HudPresenter's tier, EventCardPresenter, OutcomePresenter), so no caller re-enables a panel another still
        /// covers.</summary>
        public static void DisableCovered(VisualElement root)
        {
            var r = root.Q("root") ?? root;
            bool start = r.ClassListContains(StartScreenPresenter.OpenClass);
            bool screen = r.ClassListContains(ScreenOpenClass);
            bool full = screen && r.ClassListContains(HudPresenter.TwoRowClass);
            bool outcome = r.ClassListContains(OutcomePresenter.OpenClass);   // an End Turn outcome clip covers all but itself
            bool modal = r.ClassListContains(EventCardPresenter.OpenClass) && !start && !screen && !outcome;
            r.EnableInClassList(EventCardPresenter.ModalClass, modal);
            r.Q("top-bar")?.SetEnabled(!(start || outcome));
            r.Q("left-column")?.SetEnabled(!(start || outcome));   // the screens' buttons: usable wherever the top bar is
            r.Q("right-column")?.SetEnabled(!(start || full || modal || outcome));
            r.Q("info-panel")?.SetEnabled(!(start || full || modal || outcome || r.ClassListContains(WideOpenClass)));
            var layer = r.Q("event-layer");
            if (layer != null)
            {
                bool was = layer.enabledSelf, now = !(start || screen || outcome);
                layer.SetEnabled(now);
                if (!was && now) (layer.userData as System.Action)?.Invoke();   // EventCardPresenter: the open card takes the focus
            }
        }

        /// <summary>Asks to be told, synchronously, whenever the decision layer comes out from under the start screen, a
        /// screen or an outcome clip (DisableCovered enables it again); null stops it.</summary>
        public static void OnUncovered(VisualElement layer, System.Action? action) => layer.userData = action;

        /// <summary>Asks to be told, synchronously, whenever <paramref name="screen"/> is opened (a presenter that skips
        /// rendering while its screen is hidden catches up then).</summary>
        public static void OnOpened(VisualElement screen, System.Action action) => screen.userData = action;

        /// <summary>True when <paramref name="screen"/> is open (ScreenNav shows it inline; the stylesheet hides it).</summary>
        public static bool IsOpen(VisualElement screen) =>
            screen.style.display.keyword == StyleKeyword.Undefined && screen.style.display.value == DisplayStyle.Flex;
        readonly VisualElement root;
        readonly List<(Button, System.Action)> hooks = new List<(Button, System.Action)>();
        readonly EventCallback<NavigationCancelEvent> onCancel;

        public ScreenNav(VisualElement root)
        {
            this.root = root;
            // Escape (the UI map's Cancel, from any focused element or the panel) closes the open screen. NavigationGuard
            // handles only NavigationMoveEvent, so the two never meet. GameRoot also polls the Escape key for the case where
            // no element has the focus (CloseOnEscape).
            onCancel = e => { if (Close()) e.StopPropagation(); };
            root.RegisterCallback(onCancel, TrickleDown.TrickleDown);
            foreach (var s in Screens)
            {
                var screen = s;
                var b = root.Q<Button>("nav-" + screen);
                System.Action h = () => Toggle(screen);
                b.clicked += h;
                hooks.Add((b, h));
            }
            Open(null);
        }

        public void Dispose()
        {
            foreach (var (b, h) in hooks) b.clicked -= h;
            hooks.Clear();
            root.UnregisterCallback(onCancel, TrickleDown.TrickleDown);
            var r = root.Q("root") ?? root;
            r.RemoveFromClassList(WideOpenClass); r.RemoveFromClassList(ScreenOpenClass);
            DisableCovered(root);
        }

        /// <summary>Closes the open screen (Escape); false when none was open. When the focus was on the screen's contents it
        /// goes to the screen's top-bar button, so the next Tab carries on from there (#41 M1); an open decision card
        /// takes it instead (EventCardPresenter, when its cover closes).</summary>
        public bool Close()
        {
            if (Current == null) return false;
            var screen = root.Q<VisualElement>("screen-" + Current); var button = root.Q<Button>("nav-" + Current);
            var focused = root.panel?.focusController?.focusedElement as VisualElement;
            var inside = focused != null && (focused == screen || screen.Contains(focused));
            Open(null);
            if (inside && !(root.Q("root") ?? root).ClassListContains(EventCardPresenter.ModalClass)) FocusReturn.To(button);
            return true;
        }

        /// <summary>The open screen's name, or null.</summary>
        public string? Current { get; private set; }

        public void Toggle(string screen)
        {
            Boot.DiagLog.NavLine($"toggle {screen}: {Current ?? "none"} -> {(Current == screen ? "none" : screen)}");
            Open(Current == screen ? null : screen);
        }

        public void Open(string? screen)
        {
            Current = screen;
            var wide = false;
            foreach (var s in Screens)
            {
                var element = root.Q<VisualElement>("screen-" + s);
                element.style.display = s == screen ? DisplayStyle.Flex : DisplayStyle.None;
                root.Q<Button>("nav-" + s).EnableInClassList(OpenClass, s == screen);   // the open screen's button is lit
                if (s == screen) wide = element.ClassListContains(WideClass);
            }
            (root.Q("root") ?? root).EnableInClassList(WideOpenClass, wide);
            (root.Q("root") ?? root).EnableInClassList(ScreenOpenClass, screen != null);
            DisableCovered(root);
            if (screen != null) (root.Q<VisualElement>("screen-" + screen).userData as System.Action)?.Invoke();
        }
    }
}
