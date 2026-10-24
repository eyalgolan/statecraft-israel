#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>What the advisor popup may reach outside itself. GameRoot fills it; a test leaves out what it does not use.</summary>
    public sealed class AdvisorHooks
    {
        public EventCardPresenter? Card;
        /// <summary>Opens a side screen by ScreenNav's name (an action's open_screen).</summary>
        public Action<string>? OpenScreen;
    }

    /// <summary>The advisors' popup (spec 4.2, 7.1): one message at a time, in the right column's free upper part or a sheet
    /// under the bar on an upright phone (AdvisorLayout). It waits while any modal layer is open (Covered) and hides at once
    /// when one opens over it: ScreenNav.DisableCovered, which every layer calls, calls it back. Not modal: it takes no focus
    /// when it appears, and Escape closes it only while the focus is inside it. Presentation only: it reads the state and
    /// the bridge, writes only its settings lines, and issues no command.</summary>
    public sealed class AdvisorPresenter : IDisposable
    {
        public const string OpenClass = "advisor-open", LayerName = "advisor-layer", CriticalClass = "advisor-critical",
            NoPortraitClass = "advisor-no-portrait";
        /// <summary>Statecraft.uss sizes (.advisor-name, .advisor-role, .advisor-text, Button, .advisor-count), in panel px,
        /// raised where they would be under the text floor on screen (HudPresenter.FlooredTextSize).</summary>
        public const float NamePx = 24f, RolePx = 15f, TextPx = 22f, ButtonPx = 19f, CountPx = 15f;
        /// <summary>The fade out (Statecraft.uss .advisor-popup.window-closing), in ms.</summary>
        public const long HideMs = 150;

        readonly VisualElement hudRoot, layer, popup, portrait;
        readonly Label name, role, words, count;
        readonly Button showMe, more, notNow, gotIt, close;
        readonly SimBridge bridge; readonly UiText text; readonly AdvisorSession session; readonly PortraitCatalog? portraits;
        readonly AdvisorHooks hooks; readonly bool hebrew;
        readonly Action onChanged, onCover, onShowMe, onGotIt, onNotNow;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly EventCallback<GeometryChangedEvent> onLaidOut;
        readonly (VisualElement element, float px)[] floored;
        readonly IVisualElementScheduledItem follow;
        float flooredFor;
        bool disposed, hidden = true;
        /// <summary>The message a layer covered, to show again or drop when the layer closes (spec 4.2).</summary>
        AdvisorCatalog.Message? covered;

        public AdvisorPresenter(VisualElement root, SimBridge bridge, UiText text, AdvisorSession session,
            PortraitCatalog? portraits = null, AdvisorHooks? hooks = null)
        {
            this.bridge = bridge; this.text = text; this.session = session; this.portraits = portraits;
            this.hooks = hooks ?? new AdvisorHooks();
            hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            hudRoot = root.Q("root") ?? root;
            layer = root.Q<VisualElement>(LayerName);
            popup = layer.Q<VisualElement>("advisor-popup");
            portrait = popup.Q<VisualElement>("advisor-portrait");
            name = popup.Q<Label>("advisor-name"); role = popup.Q<Label>("advisor-role");
            words = popup.Q<Label>("advisor-text"); count = popup.Q<Label>("advisor-count");
            showMe = popup.Q<Button>("advisor-show-me"); more = popup.Q<Button>("advisor-more");
            notNow = popup.Q<Button>("advisor-not-now"); gotIt = popup.Q<Button>("advisor-got-it"); close = popup.Q<Button>("advisor-close");
            showMe.text = text["advisor.show_me"]; more.text = text["advisor.more"]; notNow.text = text["advisor.not_now"];
            gotIt.text = text["advisor.got_it"]; close.text = text["advisor.close"];
            floored = new (VisualElement, float)[] { (name, NamePx), (role, RolePx), (words, TextPx), (showMe, ButtonPx), (more, ButtonPx), (notNow, ButtonPx), (gotIt, ButtonPx), (count, CountPx) };
            onShowMe = ShowMe; onGotIt = GotIt; onNotNow = NotNow;
            showMe.clicked += onShowMe; gotIt.clicked += onGotIt; close.clicked += onGotIt; notNow.clicked += onNotNow;
            onChanged = Evaluate; onTurn = _ => Notice(AdvisorSignals.TurnEnded); onCover = CoverChanged; onLaidOut = PlaceWhenLaidOut;
            bridge.Changed += onChanged; bridge.TurnEnded += onTurn;
            layer.userData = onCover;                                   // ScreenNav.DisableCovered calls it
            // The slot moves with the layout (a window resized, "Decision waiting" or the toast shown), so the popup follows
            // it while it shows, as the guide's ring does.
            follow = layer.schedule.Execute(Place).Every(100);
            follow.Pause();
            hudRoot.RemoveFromClassList(OpenClass);
        }

        /// <summary>A modal layer is open (spec 4.2): the start screen, the open decision card, the capture dialog, the turn
        /// report, an outcome clip, the end screen; and, on the two-row HUD, any side screen (it takes the full width).</summary>
        public static bool Covered(VisualElement hudRoot) =>
            hudRoot.ClassListContains(StartScreenPresenter.OpenClass) || hudRoot.ClassListContains(EventCardPresenter.OpenClass)
            || hudRoot.ClassListContains(CaptureDialogPresenter.OpenClass) || hudRoot.ClassListContains(TurnReportPresenter.OpenClass)
            || hudRoot.ClassListContains(OutcomePresenter.OpenClass) || hudRoot.ClassListContains(EndScreenPresenter.OpenClass)
            || (hudRoot.ClassListContains(HudPresenter.TwoRowClass) && hudRoot.ClassListContains(ScreenNav.ScreenOpenClass));

        /// <summary>Showing a message: on screen and not fading out.</summary>
        public bool IsOpen => hudRoot.ClassListContains(OpenClass) && !popup.ClassListContains(ScreenNav.ClosingClass);

        /// <summary>The message on screen, or null.</summary>
        public AdvisorCatalog.Message? Shown { get; private set; }

        int Turn => bridge.State.Turn;
        AdvisorContext Context() => new AdvisorContext(bridge, hooks.Card);

        /// <summary>A new game (GameRoot: the start screen closing on New Game, or the top bar's New game): nothing waits,
        /// the history starts empty, and new_game fires.</summary>
        public void NewGame()
        {
            if (disposed) return;
            session.Queue.Reset(); session.Fired.Clear(); session.FiredTurn = Turn;
            Shown = null; covered = null;
            Notice(AdvisorSignals.NewGame);
        }

        /// <summary>An event signal fired (spec 5): it holds for this turn.</summary>
        public void Notice(string signal)
        {
            if (disposed) return;
            if (session.FiredTurn != Turn) { session.Fired.Clear(); session.FiredTurn = Turn; }
            session.Fired.Add(signal);
            Evaluate();
        }

        /// <summary>On Changed and TurnEnded only (spec 16): offers what holds, then shows the head of the queue.</summary>
        void Evaluate()
        {
            if (disposed) return;
            if (session.AutoShow)
            {
                var ctx = Context();
                session.Queue.Offer(AdvisorSignals.Holding(ctx, session), w => AdvisorSignals.When(ctx, w), Turn);
            }
            Render();
        }

        void Render()
        {
            if (disposed) return;
            if (Covered(hudRoot)) { if (Shown != null) covered = Shown; Hide(); return; }
            var m = session.Queue.Head(Turn, hebrew);
            if (m == null) { Shown = null; Hide(); return; }
            Fill(m);
            Show();
        }

        void Fill(AdvisorCatalog.Message m)
        {
            Shown = m;
            name.text = text[m.Advisor == "meyerson" && !session.Introduced(m.Advisor) ? "advisor.meyerson.name_first" : "advisor." + m.Advisor + ".name"];
            role.text = text["advisor." + m.Advisor + ".role"];
            words.text = text["advisor." + m.Id + ".text"];
            var picture = portraits?.Get(session.Catalog.AdvisorOf(m.Advisor).Person);
            popup.EnableInClassList(NoPortraitClass, picture == null);
            portrait.style.backgroundImage = picture == null ? new StyleBackground(StyleKeyword.Null) : new StyleBackground(picture);
            popup.EnableInClassList(CriticalClass, m.Critical);
            Display(showMe, m.Actions.Any(a => a.ShowMe != "" || a.OpenScreen != "" || a.OpenCard));
            Display(more, false);                                       // Task 13: a page to open
            Display(notNow, !m.Teaching && !m.Critical);
            var (number, total) = session.Queue.Position(Turn, hebrew);
            count.text = total > 1 ? text.Format("advisor.count", number, total) : "";
        }

        static void Display(VisualElement e, bool shown) => e.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        void Show()
        {
            hidden = false;
            popup.RemoveFromClassList(ScreenNav.ClosingClass);
            if (!hudRoot.ClassListContains(OpenClass))
            {
                // The layer was display:none, so it has no size until the next layout: unseen until Place puts it in its slot
                // (else it would draw at the layer's top-left for a frame or more).
                popup.style.visibility = Visibility.Hidden;
                if (ScreenNav.Moves())
                {
                    popup.AddToClassList(ScreenNav.OpeningClass);
                    popup.RegisterCallback<GeometryChangedEvent>(EndOpening);
                }
            }
            hudRoot.AddToClassList(OpenClass);
            follow.Resume();
            layer.UnregisterCallback(onLaidOut);
            if (Place(null) == null) layer.RegisterCallback(onLaidOut);
        }

        /// <summary>Once, on the layer's first layout after Show: places the popup, which makes it visible.</summary>
        void PlaceWhenLaidOut(GeometryChangedEvent e)
        {
            if (Place(null) != null) layer.UnregisterCallback(onLaidOut);
        }

        void EndOpening(GeometryChangedEvent e)
        {
            popup.UnregisterCallback<GeometryChangedEvent>(EndOpening);
            popup.RemoveFromClassList(ScreenNav.OpeningClass);
        }

        void Hide()
        {
            if (hidden) return;
            hidden = true;
            follow.Pause();
            layer.UnregisterCallback(onLaidOut);
            var focused = layer.focusController?.focusedElement as VisualElement;
            if (focused != null && layer.Contains(focused)) { focused.Blur(); FocusReturn.To(hudRoot.Q<Button>("end-turn")); }
            if (!ScreenNav.Moves()) { hudRoot.RemoveFromClassList(OpenClass); return; }
            popup.AddToClassList(ScreenNav.ClosingClass);
            popup.schedule.Execute(() =>
            {
                if (!hidden) return;                                    // shown again meanwhile
                popup.RemoveFromClassList(ScreenNav.ClosingClass);
                hudRoot.RemoveFromClassList(OpenClass);
            }).StartingIn(HideMs);
        }

        /// <summary>ScreenNav.DisableCovered: a layer opened (hide at once) or closed (the covered message comes back if its
        /// signal still holds this turn, else it goes to the history; spec 4.2).</summary>
        void CoverChanged()
        {
            if (disposed) return;
            if (Covered(hudRoot)) { if (Shown != null) covered = Shown; Hide(); return; }
            if (covered is AdvisorCatalog.Message m)
            {
                covered = null;
                var ctx = Context();
                if (!(AdvisorSignals.Holding(ctx, session).Contains(m.Signal) && AdvisorSignals.When(ctx, m.When)))
                    session.Queue.Drop(m, Turn, bridge.State.Date);
            }
            Render();
        }

        void ShowMe()
        {
            if (Shown == null) return;
            foreach (var a in Shown.Actions)
                if (a.OpenScreen != "") hooks.OpenScreen?.Invoke(a.OpenScreen);
                else if (a.OpenCard && hooks.Card?.Shown != null) hooks.Card.Reopen();
        }

        /// <summary>Got it, the X, or Escape from inside: seen, and the next message (if any) shows.</summary>
        public void GotIt()
        {
            if (Shown == null) return;
            session.Queue.Done(Shown, Turn, bridge.State.Date);
            Shown = null;
            Render();
        }

        /// <summary>Not now: off to the next turn and into Counsel's history (spec 7.1). Not for teaching or critical messages.</summary>
        public void NotNow()
        {
            if (Shown == null || Shown.Teaching || Shown.Critical) return;
            session.Queue.PutOff(Shown, Turn, bridge.State.Date);
            Shown = null;
            Render();
        }

        /// <summary>Escape: closes it as the X does, only while the focus is inside it (spec 16). True when it did.</summary>
        public bool Escape()
        {
            var focused = layer.focusController?.focusedElement as VisualElement;
            if (!IsOpen || focused == null || !popup.Contains(focused)) return false;
            GotIt();
            return true;
        }

        /// <summary>The slot in the layer's space (AdvisorLayout.Slot from the HUD as it is laid out now).</summary>
        internal Rect Slot()
        {
            Rect L(string n) => Local(hudRoot.Q(n)!.worldBound);
            Rect? Visible(string n) { var e = hudRoot.Q(n); return e != null && e.resolvedStyle.display == DisplayStyle.Flex && e.worldBound.width > 0 ? Local(e.worldBound) : (Rect?)null; }
            var window = new Rect(Vector2.zero, layer.layout.size);
            return AdvisorLayout.Slot(AdvisorLayout.Tier(hudRoot), window, L("right-column"), L("left-column"), L("minimap"),
                Visible("event-reopen"), Visible("toast"), Visible("info-panel"), Visible("map-layer"));
        }

        Rect Local(Rect world)
        {
            var min = layer.WorldToLocal(world.min); var max = layer.WorldToLocal(world.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>In its slot, at the top (Task 8 passes Show me's target to keep it off), with the text floor applied.</summary>
        internal void Place() => Place(null);

        /// <summary>Places the popup and returns where (in the layer's space), or null when it is not shown or not laid out.</summary>
        internal Rect? Place(Rect? avoid)
        {
            if (!hudRoot.ClassListContains(OpenClass) || layer.panel == null || !(layer.layout.width > 0)) return null;
            ApplyTextFloor();
            var slot = OnPixels(Slot());
            var at = AdvisorLayout.Place(slot, popup.resolvedStyle.height > 0 ? popup.resolvedStyle.height : slot.height, avoid);
            popup.style.left = at.xMin; popup.style.top = at.yMin;
            popup.style.width = Mathf.Max(0f, at.width); popup.style.maxHeight = Mathf.Max(0f, slot.height);
            popup.style.visibility = Visibility.Visible;
            return at;
        }

        /// <summary>The slot shrunk to whole screen pixels: the layout rounds each edge to the nearest one, which on a small
        /// window (2.5 panel px a pixel at 844 x 390) would push the popup's edges past the slot's.</summary>
        Rect OnPixels(Rect r)
        {
            float s = layer.panel.scaledPixelsPerPoint;
            if (!(s > 0f)) return r;
            var o = layer.worldBound.position;
            float In(float v, float origin) => Mathf.Ceil((v + origin) * s - 0.01f) / s - origin;
            float Out(float v, float origin) => Mathf.Floor((v + origin) * s + 0.01f) / s - origin;
            return Rect.MinMaxRect(In(r.xMin, o.x), In(r.yMin, o.y), Out(r.xMax, o.x), Out(r.yMax, o.y));
        }

        void ApplyTextFloor()
        {
            var scale = HudPresenter.ScreenPxPerPanelPx(layer.panel);
            if (scale == flooredFor) return;
            flooredFor = scale;
            foreach (var (element, px) in floored)
            {
                var size = HudPresenter.FlooredTextSize(px, scale);
                element.style.fontSize = size > px ? new StyleLength(size) : new StyleLength(StyleKeyword.Null);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            follow.Pause();
            layer.UnregisterCallback(onLaidOut);
            bridge.Changed -= onChanged; bridge.TurnEnded -= onTurn;
            if (layer.userData == (object)onCover) layer.userData = null;
            showMe.clicked -= onShowMe; gotIt.clicked -= onGotIt; close.clicked -= onGotIt; notNow.clicked -= onNotNow;
            hudRoot.RemoveFromClassList(OpenClass);
        }
    }
}
