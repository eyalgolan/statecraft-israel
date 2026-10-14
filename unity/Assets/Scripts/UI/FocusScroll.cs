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
            if (view == null) return true;
            Rect r = target.worldBound, v = view.contentViewport.worldBound;
            if (!LaidOut(r) || !LaidOut(v)) return false;
            if (r.yMin >= v.yMin - 0.5f && r.yMax <= v.yMax + 0.5f && r.xMin >= v.xMin - 0.5f && r.xMax <= v.xMax + 0.5f) return true;
            view.ScrollTo(target);
            return false;
        }

        static bool LaidOut(Rect r) => !float.IsNaN(r.width) && !float.IsNaN(r.height) && r.width > 0f && r.height > 0f;
    }
}
