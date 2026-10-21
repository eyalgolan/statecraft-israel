#nullable enable
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The side-menu buttons open the Cabinet, Budget, Defence, Why, How to play, Objectives and Settings screens, one at a time; pressing the open
    /// screen's button again closes it. Every press toggles, synchronously, with no debounce: two presses in one frame (a
    /// double click, or a click plus Enter) open and close again, so the result depends only on the number of presses
    /// (HudTests.TwoPressesInOneFrameToggleTwice; playtest 1, item 11).</summary>
    public sealed class ScreenNav : System.IDisposable
    {
        public static readonly string[] Screens = { "cabinet", "budget", "defence", "why", "how-to-play", "objectives", "settings" };
        /// <summary>The class on the open screen's button (Statecraft.uss lights its ring).</summary>
        public const string OpenClass = "nav-open";
        /// <summary>The class on the "root" element while a wide screen (class <see cref="WideClass"/>: Cabinet, Defence) is
        /// open: Statecraft.uss hides the info panel it covers, so its buttons take no focus and no clicks.</summary>
        public const string WideOpenClass = "wide-screen-open", WideClass = "screen-wide";
        /// <summary>The class on the "root" element while any screen is open: Statecraft.uss hides the decision layer under it,
        /// and in the two-row (narrow) HUD, where every screen takes the full width, the minimap column and the info panel
        /// too (#210).</summary>
        public const string ScreenOpenClass = "screen-open";
        /// <summary>#1106: the classes on a window (a side-menu screen, the decision card) while it opens and while it
        /// closes (Statecraft.uss holds the transitions).</summary>
        public const string OpeningClass = "window-opening", ClosingClass = "window-closing";

        /// <summary>Disables each HUD panel that a class on the "root" element hides, and enables it again when none does: a
        /// child of an undisplayed element can still take the keyboard focus, so hiding alone leaves its buttons in Tab's
        /// reach (#400). The start screen covers the top bar, the minimap column, the decision layer and the info panel; any
        /// open screen covers the decision layer (it sits where the card does), and in the two-row HUD also the minimap
        /// column and the info panel; a wide screen covers the info panel; the open decision card's backdrop
        /// (EventCardPresenter.OpenClass, with no start screen or screen over it: ModalClass, set here) covers the minimap
        /// column and the info panel; an End Turn outcome clip (OutcomePresenter.OpenClass) covers the top bar, the minimap
        /// column, the info panel and the decision layer. The turn report (TurnReportPresenter.OpenClass, #1037) covers them
        /// too, and is itself enabled only while it shows and no start screen or outcome clip covers it; when it comes out it
        /// takes the focus (OnUncovered on its element). Called wherever one of those classes changes (StartScreenPresenter,
        /// Open, HudPresenter's tier, EventCardPresenter, OutcomePresenter, TurnReportPresenter), so no caller re-enables a
        /// panel another still covers.</summary>
        public static void DisableCovered(VisualElement root)
        {
            var r = root.Q("root") ?? root;
            bool start = r.ClassListContains(StartScreenPresenter.OpenClass);
            bool screen = r.ClassListContains(ScreenOpenClass);
            bool full = screen && r.ClassListContains(HudPresenter.TwoRowClass);
            bool outcome = r.ClassListContains(OutcomePresenter.OpenClass);   // an End Turn outcome clip covers all but itself
            bool end = r.ClassListContains(EndScreenPresenter.OpenClass);   // the end screen (#812) covers the HUD and the report
            bool report = r.ClassListContains(TurnReportPresenter.OpenClass) || end;   // the turn report covers the HUD under it
            bool modal = r.ClassListContains(EventCardPresenter.OpenClass) && !start && !screen && !outcome;
            r.EnableInClassList(EventCardPresenter.ModalClass, modal);
            r.Q("top-bar")?.SetEnabled(!(start || outcome || report));
            r.Q("left-column")?.SetEnabled(!(start || outcome || report));   // the screens' buttons: usable wherever the top bar is
            r.Q("right-column")?.SetEnabled(!(start || full || modal || outcome || report));
            bool infoCovered = start || full || modal || outcome || report || r.ClassListContains(WideOpenClass);
            r.Q("info-panel")?.SetEnabled(!infoCovered);
            // the map's keyboard stop (#41 B1b): out of reach exactly when the info panel is, so a pick is never refused (#210)
            r.Q("map-focus")?.SetEnabled(!infoCovered);
            var layer = r.Q("event-layer");
            if (layer != null)
            {
                bool was = layer.enabledSelf, now = !(start || screen || outcome || report);
                layer.SetEnabled(now);
                if (!was && now) (layer.userData as System.Action)?.Invoke();   // EventCardPresenter: the open card takes the focus
            }
            var panel = r.Q(TurnReportPresenter.ElementName);
            if (panel != null)
            {
                bool was = panel.enabledSelf, now = r.ClassListContains(TurnReportPresenter.OpenClass) && !(start || outcome || end);
                panel.SetEnabled(now);
                if (!was && now) (panel.userData as System.Action)?.Invoke();   // TurnReportPresenter: Close takes the focus
            }
            var over = r.Q(EndScreenPresenter.ElementName);
            if (over != null)
            {
                bool was = over.enabledSelf, now = end && !(start || outcome);
                over.SetEnabled(now);
                if (!was && now) (over.userData as System.Action)?.Invoke();   // EndScreenPresenter: Close takes the focus
            }
        }

        /// <summary>Asks to be told, synchronously, whenever the decision layer comes out from under the start screen, a
        /// screen or an outcome clip (DisableCovered enables it again); null stops it.</summary>
        public static void OnUncovered(VisualElement layer, System.Action? action) => layer.userData = action;

        /// <summary>Asks to be told, synchronously, whenever <paramref name="screen"/> is opened (a presenter that skips
        /// rendering while its screen is hidden catches up then).</summary>
        public static void OnOpened(VisualElement screen, System.Action action) => screen.userData = action;

        /// <summary>True when <paramref name="screen"/> is open (ScreenNav shows it inline; the stylesheet hides it), and not
        /// shrinking away (<see cref="Show"/>).</summary>
        public static bool IsOpen(VisualElement screen) =>
            screen.style.display.keyword == StyleKeyword.Undefined && screen.style.display.value == DisplayStyle.Flex
            && !screen.ClassListContains(ClosingClass);

        /// <summary>Test seam: whether windows move. Only in play mode: an EditMode panel does not tick (EventImageCatalog's
        /// note), so there a window shows and hides at once and the layout tests measure it at full size; HudTests turns
        /// the motion on to see the classes switch.</summary>
        internal static System.Func<bool> Moves = () => UnityEngine.Application.isPlaying;

        /// <summary>Shows or hides a window (a side-menu screen, the decision card) with #1106's motion, all of it USS
        /// transitions: a hidden window opens in OpeningClass (small and transparent) for one layout, then grows and fades
        /// in to its normal style over 0.5 s; Unity runs no transition on an element coming out of display: none, hence the
        /// start class. A shown window closes in ClosingClass, disabled so nothing in it takes a click or the focus while it
        /// shrinks over 0.3 s, and leaves the layout when that scale transition ends. Opened again while it shrinks, it grows back
        /// from where it is; closed before its first layout (two presses in one frame), it just hides.</summary>
        public static void Show(VisualElement window, bool shown)
        {
            if (!Moves()) { window.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None; return; }
            bool inView = window.style.display.keyword == StyleKeyword.Undefined && window.style.display.value == DisplayStyle.Flex;
            if (shown)
            {
                window.SetEnabled(true);
                window.RemoveFromClassList(ClosingClass);   // grows back, if it was shrinking
                if (inView) return;
                window.AddToClassList(OpeningClass);
                window.RegisterCallback<GeometryChangedEvent>(EndOpening);
                window.style.display = DisplayStyle.Flex;
                return;
            }
            if (!inView || window.ClassListContains(OpeningClass)) { Hide(window); return; }
            if (window.ClassListContains(ClosingClass)) return;
            window.AddToClassList(ClosingClass);
            window.SetEnabled(false);
            window.RegisterCallback<TransitionEndEvent>(EndClosing);
            // a safety net for a shrink that never reports its end (an ancestor hidden meanwhile, a panel that stops):
            // the window must not stay in the layout, disabled. A net left from an earlier close can cut a quick
            // close-reopen-close's shrink short; it never leaves a window stuck.
            window.schedule.Execute(() => { if (window.ClassListContains(ClosingClass)) Hide(window); }).StartingIn(ClosingNetMs);
        }

        /// <summary>The safety net's delay: the 0.3 s shrink of .window-closing, plus a margin.</summary>
        internal const long ClosingNetMs = 400;

        static void Hide(VisualElement window)
        {
            window.UnregisterCallback<GeometryChangedEvent>(EndOpening);
            window.UnregisterCallback<TransitionEndEvent>(EndClosing);
            window.RemoveFromClassList(OpeningClass);
            window.RemoveFromClassList(ClosingClass);
            window.style.display = DisplayStyle.None;
        }

        /// <summary>The window's first layout out of display: none: its start state is resolved, so it can grow.</summary>
        static void EndOpening(GeometryChangedEvent e)
        {
            var window = (VisualElement)e.currentTarget;
            window.UnregisterCallback<GeometryChangedEvent>(EndOpening);
            window.RemoveFromClassList(OpeningClass);
        }

        /// <summary>The shrink ended: the window leaves the layout, unless it was opened again meanwhile. Only the scale
        /// counts: closed within 0.5 s of opening, the grow's opacity transition ends during the shrink. A cancelled
        /// transition does not count: closing mid-grow cancels the grow's scale, and a shrink that stops is the safety
        /// net's (Show).</summary>
        static void EndClosing(TransitionEndEvent e) => EndClosing((VisualElement)e.currentTarget, e.target, e.AffectsProperty(Scale));
        static readonly StylePropertyName Scale = new StylePropertyName("scale");
        static void EndClosing(VisualElement window, IEventHandler target, bool scale)
        {
            if (scale && target == window && window.ClassListContains(ClosingClass)) Hide(window);
        }
        readonly VisualElement root;
        readonly List<(Button, System.Action)> hooks = new List<(Button, System.Action)>();
        readonly List<Button> closes = new List<Button>();
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly EventCallback<GeometryChangedEvent> onGeometry;

        /// <summary>A layer over the screens that takes Escape first (GameRoot: the report panel, #1169, and the share panel, #1170): true when it used the
        /// key, and the open screen under it stays open. This handler trickles down from the root, so it runs before the layer's own.</summary>
        public System.Func<bool>? TakesEscape;

        public ScreenNav(VisualElement root)
        {
            this.root = root;
            // Escape (the UI map's Cancel, from any focused element or the panel) closes the open screen. NavigationGuard
            // handles only NavigationMoveEvent, so the two never meet. GameRoot also polls the Escape key for the case where
            // no element has the focus (CloseOnEscape).
            onCancel = e => { if (TakesEscape?.Invoke() == true || Close()) e.StopPropagation(); };
            root.RegisterCallback(onCancel, TrickleDown.TrickleDown);
            onGeometry = e => FitCloseButtons();
            root.RegisterCallback(onGeometry);
            foreach (var s in Screens)
            {
                var screen = s;
                var b = root.Q<Button>("nav-" + screen);
                System.Action h = () => Toggle(screen);
                b.clicked += h;
                hooks.Add((b, h));

                // an X on the screen itself closes it: a phone has no Escape, and re-tapping the lit rail button is not
                // discoverable (playtest v0.1.1). The same Close() as Escape, so the focus return is identical.
                var header = root.Q("screen-" + screen)?.Q(className: "screen-header");
                if (header == null) continue;
                var x = new Button(() => Close()) { name = "screen-close-" + screen, text = "X" };
                x.AddToClassList("screen-close");
                header.Add(x);
                closes.Add(x);
            }
            Open(null);
            FitCloseButtons();
        }

        /// <summary>The X's hit area: 44 screen px at every window size (#1035's ruling), so in panel units it grows as
        /// the panel shrinks (<paramref name="scale"/> is screen px per panel px; the 1280x720 window scales about 2/3,
        /// so 44 panel px would render as about 29 px there). On the web a screen pixel is a CSS pixel: the canvas is
        /// drawn in device pixels, so the target is <paramref name="dpr"/> device px per CSS px (HudPresenter.cs's text
        /// floor counts the same way, DevicePixelRatio.jslib). Off the web <paramref name="dpr"/> is 1.</summary>
        internal static float CloseTargetPanelPx(float scale, float dpr = 1f) =>
            scale > 0f ? 44f * (dpr > 0f ? dpr : 1f) / scale : 44f;

#if UNITY_WEBGL && !UNITY_EDITOR
        // The panel scales by the canvas's device pixels, so a screen pixel here is a CSS pixel: the device's pixels / the
        // browser's devicePixelRatio (DevicePixelRatio.jslib; the same import as HudPresenter's text floor).
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern float StatecraftDevicePixelRatio();
        static float DevicePixelRatio() { var r = StatecraftDevicePixelRatio(); return r > 0f ? r : 1f; }
#else
        static float DevicePixelRatio() => 1f;
#endif

        void FitCloseButtons()
        {
            var px = CloseTargetPanelPx(root.panel?.scaledPixelsPerPoint ?? 1f, DevicePixelRatio());
            var r = new StyleLength(new Length(px / 2f));
            foreach (var x in closes)
            {
                x.style.width = px; x.style.height = px;
                x.style.borderTopLeftRadius = r; x.style.borderTopRightRadius = r;
                x.style.borderBottomLeftRadius = r; x.style.borderBottomRightRadius = r;

                // Pad both sides of the header here, not in the stylesheet: a grown X (66 panel px at 1280x720, 89 at
                // 390x844) would cover the title's end under the old fixed 52 px, and one inline rule serves both
                // languages. Also grow the header so the X (top 6 px + px) fits inside it, with its 4 px bottom
                // padding and 1 px border.
                var header = x.parent;
                if (header == null) continue;
                header.style.paddingLeft = new StyleLength(px + 8f);
                header.style.paddingRight = new StyleLength(px + 8f);
                header.style.minHeight = new StyleLength(px + 6f + 5f);
            }
        }

        public void Dispose()
        {
            foreach (var (b, h) in hooks) b.clicked -= h;
            hooks.Clear();
            foreach (var x in closes) x.RemoveFromHierarchy();
            closes.Clear();
            root.UnregisterCallback(onCancel, TrickleDown.TrickleDown);
            root.UnregisterCallback(onGeometry);
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
                Show(element, s == screen);   // #1106: grows in, shrinks away
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
