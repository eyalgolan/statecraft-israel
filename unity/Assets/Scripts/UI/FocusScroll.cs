#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>Keeps keyboard focus in sight (accessibility audit H1, #41). The Cabinet's cards and candidates and the
    /// Budget's rows are ScrollViews, and Tab moved focus onto rows below the visible area without moving the view. One
    /// FocusInEvent callback at the UI root scrolls the focused element's nearest ScrollView to it when it is not wholly
    /// inside the viewport: every ScrollView, the ones added later included. Focus that follows a pointer press is left
    /// alone, so a click on a half-visible card never jolts the list: a pointer-down marks the press, and the next key or
    /// navigation event clears the mark. Programmatic focus (a card taking focus, focus returned after a screen closes)
    /// scrolls as keyboard focus does, unless the last input was a pointer press: a mouse player's lists never move. A target with no layout yet (rows just rebuilt) is checked again once it is
    /// laid out.</summary>
    public sealed class FocusScroll : System.IDisposable
    {
        readonly VisualElement root;
        readonly EventCallback<PointerDownEvent> onDown;
        readonly EventCallback<KeyDownEvent> onKey;
        readonly EventCallback<NavigationMoveEvent> onMove;
        readonly EventCallback<FocusInEvent> onFocus;
        bool pointer;

        public FocusScroll(VisualElement root)
        {
            this.root = root;
            onDown = _ => pointer = true;
            onKey = _ => pointer = false;
            onMove = _ => pointer = false;
            onFocus = e => { if (!pointer && e.target is VisualElement t) Follow(t); };
            root.RegisterCallback(onDown, TrickleDown.TrickleDown);
            root.RegisterCallback(onKey, TrickleDown.TrickleDown);
            root.RegisterCallback(onMove, TrickleDown.TrickleDown);
            root.RegisterCallback(onFocus, TrickleDown.TrickleDown);
        }

        public void Dispose()
        {
            root.UnregisterCallback(onDown, TrickleDown.TrickleDown);
            root.UnregisterCallback(onKey, TrickleDown.TrickleDown);
            root.UnregisterCallback(onMove, TrickleDown.TrickleDown);
            root.UnregisterCallback(onFocus, TrickleDown.TrickleDown);
        }

        /// <summary>Scrolls the target's nearest ScrollView to it when it is not wholly in view. When that can't be settled
        /// at once (no layout yet, or ScrollTo clamped by a scroll range that lags a layout that just grew the list), checks
        /// again whenever the target, the view's content or its viewport is laid out again (registered after the
        /// ScrollView's own callbacks, so its range is up to date by then), and on each of the next few updates, until the
        /// target is in view or loses the focus.</summary>
        static void Follow(VisualElement target)
        {
            if (Settled(target)) return;
            var view = target.GetFirstAncestorOfType<ScrollView>()!;
            var watched = new[] { target, view.contentContainer, view.contentViewport };
            int ticks = 0, layouts = 0; bool done = false;
            EventCallback<GeometryChangedEvent>? onLayout = null;
            void Stop() { done = true; foreach (var e in watched) e.UnregisterCallback(onLayout!); }
            void Check(bool tick)
            {
                if (done) return;
                if (tick && ++ticks > TickRetries) return;                 // the per-update backup is spent; layouts still count
                if (!tick && ++layouts > LayoutRetries) { Stop(); return; }
                if (target.focusController?.focusedElement != target || Settled(target)) Stop();
            }
            onLayout = _ => Check(false);
            foreach (var e in watched) e.RegisterCallback(onLayout);
            target.schedule.Execute(() => Check(true)).Every(0).Until(() => done || ticks > TickRetries);
        }

        /// <summary>How many updates, and how many layouts of the watched elements, Follow keeps checking.</summary>
        const int TickRetries = 10, LayoutRetries = 20;

        /// <summary>True when there is nothing (more) to do: the target has no ScrollView, or it is wholly in view. Otherwise
        /// scrolls to it if it and its view are laid out, and returns false so the caller checks again.</summary>
        static bool Settled(VisualElement target)
        {
            var view = target.GetFirstAncestorOfType<ScrollView>();
            // The view's own scroll bar takes Tab too (#41, keyboard-only turn): it is always in sight, and ScrollTo throws
            // for anything outside the content.
            if (view == null || !view.contentContainer.Contains(target)) return true;
            Rect r = target.worldBound, v = view.contentViewport.worldBound;
            if (!LaidOut(r) || !LaidOut(v)) return false;
            if (r.yMin >= v.yMin - 0.5f && r.yMax <= v.yMax + 0.5f && r.xMin >= v.xMin - 0.5f && r.xMax <= v.xMax + 0.5f) return true;
            view.ScrollTo(target);
            return false;
        }

        static bool LaidOut(Rect r) => !float.IsNaN(r.width) && !float.IsNaN(r.height) && r.width > 0f && r.height > 0f;

        /// <summary>The views whose bar is the only way a keyboard player can scroll them, so it stays in the Tab
        /// order. `KeyScroll` gives PageUp, PageDown, Home and End to the open screen's main view and to the decision
        /// card's text, but it has no target for these three (`KeyScroll.MainViews`), and `GameRoot` turns those keys
        /// off entirely while the turn report or the end screen is open (`GameRoot`, the `report?.IsOpen != true &&
        /// end?.IsOpen != true` guard). Their rows are plain labels, so Tab reaches nothing inside them either. Taking
        /// their bar out would leave no way at all to scroll them once they overflow, which is worse than the stray
        /// stop. Giving `KeyScroll` a route to these is the better answer and is its own change; until then they stay
        /// out of the sweep (#41 K6, the owner's review).</summary>
        static readonly string[] OnlyReachableByTheirBar = { "turn-report-rows", "end-screen-rows", "capture-scroll" };

        /// <summary>Takes every scroll bar under <paramref name="root"/> out of the Tab order (accessibility audit, #41,
        /// K3 widened), except the few in <see cref="OnlyReachableByTheirBar"/>. #1453 did the Cabinet's two views,
        /// which is where the audit found the stop: the seat list is the one long enough to show its bar in a 1280x720
        /// window. Every other view's slider stayed focusable, and a ScrollView hides its scroller only while the
        /// content fits, so the stop came back as soon as a view overflowed: a shorter window, or enough content.
        /// Measured at 1280x420, the Budget's bar and Defence's each took a Tab stop, and Defence has no control of its
        /// own at the opening turn, so its bar was very nearly all a keyboard player could land on.
        ///
        /// The same reasoning as K3, no wider: a bar is always in sight and a list is reached by Tabbing its rows, and
        /// for the lists that hold labels only (why-rows, defence-body, the event card's text) there is nothing to Tab
        /// to at all, while KeyScroll already gives PageUp, PageDown, Home and End with or without focus. Dragging the
        /// bar and the wheel are untouched: this changes focusability, not behaviour.</summary>
        public static void NoScrollBarIsATabStop(VisualElement root)
        {
            foreach (var view in root.Query<ScrollView>().Build())
                if (System.Array.IndexOf(OnlyReachableByTheirBar, view.name) < 0)
                    BarsAreNotTabStops(view);
        }

        /// <summary>Takes a scroll view's own scroll bars out of the Tab order (accessibility audit, #41, the keyboard-only
        /// turn's "Still open" list). A scroll bar is always in sight and the list it belongs to is reached by Tabbing
        /// through its rows, so a stop on the bar only adds a press between the last row and the next control. Dragging it
        /// with the pointer and the mouse wheel are untouched: this changes focusability, not behaviour. Call it after the
        /// view exists; UI Toolkit rebuilds the bars' children with the view, so the sliders are set as well as the
        /// scrollers.</summary>
        public static void BarsAreNotTabStops(params ScrollView?[] views)
        {
            foreach (var view in views)
            {
                if (view == null) continue;
                foreach (var scroller in new[] { view.verticalScroller, view.horizontalScroller })
                {
                    if (scroller == null) continue;
                    scroller.focusable = false;
                    if (scroller.slider != null) scroller.slider.focusable = false;
                    if (scroller.lowButton != null) scroller.lowButton.focusable = false;
                    if (scroller.highButton != null) scroller.highButton.focusable = false;
                }
            }
        }
    }
}
