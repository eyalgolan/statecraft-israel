#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The first-turn guide (#42; Q-ONB-1, owner, 17 October 2026, #784; in v0.1.3 by the owner's ruling of
    /// 20 October 2026): six short panels, Attention, a city's housing and jobs, training, moving a unit (#1141, owner,
    /// 21 October 2026), an event card and the objectives, each with a gold ring round its part of the HUD as it is today
    /// (<see cref="Targets"/>).
    /// <para>It shows by itself once, on the first new game (<see cref="NewGame"/>): never on a Continue, and never without a
    /// settings file to remember that it was seen (automated runs and EditMode tests have none). It can be skipped at any
    /// step (Skip, Escape) and opened again from the How to play screen.</para>
    /// <para>Presentation only: it reads the state and changes the UI alone (the card's Hide, the hex the info panel shows,
    /// the "seen" line in the settings file, SettingsStore), never the sim, the queued orders or the save. Steps 2 and 3
    /// show a city with a training facility (<see cref="CityHex"/>) in the info panel, step 4 a unit that can move
    /// (<see cref="UnitToShow"/>) with its card's Move button, and the selection the guide found is put back when it
    /// moves on to a step that does not explain it, or closes; the decision card is hidden ("Decision waiting") except on
    /// step 5 and put back as it was when the guide closes. Step 5 is left out while there is no card to explain (the
    /// guide then counts five steps), and answering the card on it moves the guide on (owner's report on v0.1.4, #1323).
    /// The guide takes no pointer outside its panel, so the HUD stays usable under it (the browser checks press End Turn
    /// with it open); End Turn closes it as the turn advances.</para></summary>
    public sealed class FirstTurnGuidePresenter : IDisposable
    {
        /// <summary>On the "root" element while the guide shows (Statecraft.uss shows its layer).</summary>
        public const string OpenClass = "guide-open";
        /// <summary>On the dock while the panel sits at the top of the window (the step's target is in the lower half).</summary>
        public const string TopClass = "guide-dock-top";
        /// <summary>The settings file's line (SettingsStore) once the guide has shown by itself.</summary>
        public const string SeenKey = "first_turn_guide";
        /// <summary>The steps' string keys, in order (guide.&lt;key&gt;.title, guide.&lt;key&gt;.text).</summary>
        public static readonly string[] Steps = { "attention", "city", "training", "move", "event", "objectives" };
        /// <summary>The event card's step (the card shows on it and is hidden on the others).</summary>
        public const int EventStep = 4;
        /// <summary>The camera's farthest distance on the move step, which centres it on <see cref="UnitToShow"/>'s hex (GameRoot;
        /// a nearer camera stays as near). At the start's 90 km, the view's fit to the map's edge (CameraController.Settle) left
        /// a unit near the edge, the Negev Brigade at Nir Am, under the info panel (#1392).</summary>
        public const float MoveStepDistanceM = 40000f;
        /// <summary>The ring's gap round its target, in panel px. Under Attention (step 1) End Turn's border is kept clear of
        /// it by Statecraft.uss (.guide-open #attention), not by this gap: the two sit 3 px apart in the bar.</summary>
        const float RingGap = 4f;
        /// <summary>The guide's text sizes in Statecraft.uss (.guide-count, .guide-title, .guide-text, and Button for Skip and
        /// Next), in panel px: kept while they are at least the owner's text floor on screen (HudPresenter.TextFloorPx, the
        /// top bar's rule), raised where the panel's scale is small (a phone held upright, about 0.49; owner, 21 October 2026).</summary>
        public const float CountPx = 18f, TitlePx = 29f, TextPx = 22f, ButtonPx = 19f;

        readonly VisualElement hudRoot, layer, ring, dock;
        readonly Label count, title, words;
        readonly Button next, skip, reopenButton;
        readonly UiText text;
        readonly SettingsStore? store;
        readonly EventCardPresenter? card;
        readonly Action? showCity, closeScreens, showUnit;
        readonly Func<Action>? keepSelection;
        readonly SimBridge? bridge;
        readonly Action onNext, onSkip, onReopen;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly Action onChanged;
        readonly IVisualElementScheduledItem follow;
        readonly (VisualElement element, float px)[] floored;
        /// <summary>The scale the text floor was last applied for (0: not yet).</summary>
        float flooredFor;
        bool disposed;
        /// <summary>The card was open (not hidden) when the guide opened: it is put back so when the guide closes.</summary>
        bool cardWasOpen;
        /// <summary>The event the card showed when the guide opened (null: none), and the turn it opened on.</summary>
        string? cardEvent;
        int openTurn;
        /// <summary>Puts back the selection the guide found, once it has shown its city or unit (null: it has not).</summary>
        Action? putBack;
        /// <summary>The event step has shown since the guide opened, so the count keeps it after its card is answered.</summary>
        bool eventShown;
        /// <summary>The frame a step was shown on: the panel is placed, and shown, only once that frame's layout has run.</summary>
        int shownFrame = -1;

        /// <param name="store">The settings file, where the guide notes that it has shown (null: it never shows by itself).</param>
        /// <param name="card">The decision card, hidden except on the event step (null: none).</param>
        /// <param name="showCity">Shows <see cref="CityHex"/>'s city in the info panel, as a click on it would (null: nothing).</param>
        /// <param name="closeScreens">Closes an open side-menu screen when the guide opens (null: nothing).</param>
        /// <param name="bridge">The game: the guide closes when its turn advances (End Turn under the open guide; null: never).</param>
        /// <param name="keepSelection">Notes the selection (hex or unit) just before the guide first shows its city, and
        /// returns what puts it back when the guide closes (null: the guide's city or unit stays shown).</param>
        /// <param name="showUnit">Shows <see cref="UnitToShow"/>'s unit and its card in the info panel, as a click on it would
        /// (null: nothing).</param>
        public FirstTurnGuidePresenter(VisualElement root, UiText text, SettingsStore? store, EventCardPresenter? card = null,
            Action? showCity = null, Action? closeScreens = null, SimBridge? bridge = null, Func<Action>? keepSelection = null, Action? showUnit = null)
        {
            this.text = text; this.store = store; this.card = card; this.showCity = showCity; this.closeScreens = closeScreens;
            this.bridge = bridge; this.keepSelection = keepSelection; this.showUnit = showUnit;
            hudRoot = root.Q("root") ?? root;
            layer = root.Q<VisualElement>("first-turn-guide");
            ring = layer.Q<VisualElement>("guide-ring");
            dock = layer.Q<VisualElement>("guide-dock");
            count = layer.Q<Label>("guide-count"); title = layer.Q<Label>("guide-title"); words = layer.Q<Label>("guide-text");
            next = layer.Q<Button>("guide-next"); skip = layer.Q<Button>("guide-skip");
            skip.text = text["guide.skip"];
            floored = new (VisualElement, float)[] { (count, CountPx), (title, TitlePx), (words, TextPx), (next, ButtonPx), (skip, ButtonPx) };
            reopenButton = root.Q<Button>("how-to-play-guide");
            reopenButton.text = text["how_to_play.guide"];
            onNext = Next; onSkip = Close; onReopen = Open;
            next.clicked += onNext; skip.clicked += onSkip; reopenButton.clicked += onReopen;
            onTurn = _ => TurnEnded();
            onChanged = GameChanged;
            if (bridge != null) { bridge.TurnEnded += onTurn; bridge.Changed += onChanged; }
            // The targets move with the layout (a window resized, the info panel re-rendered after an order), so the ring
            // follows them while the guide shows.
            follow = layer.schedule.Execute(Place).Every(100);
            follow.Pause();
            hudRoot.RemoveFromClassList(OpenClass);
        }

        public bool IsOpen => hudRoot.ClassListContains(OpenClass);

        /// <summary>The step shown (0 to 5), or -1 when the guide is closed.</summary>
        public int Step { get; private set; } = -1;

        /// <summary>Whether the guide has shown by itself before (the settings file's line); true with no file to read, so it
        /// never shows by itself there.</summary>
        public bool Seen => store == null || store.Read(SeenKey) == "seen";

        /// <summary>A new game has begun: the guide opens if it has never shown by itself, and notes that it has. Not under
        /// the start screen: GameRoot calls this again when the screen closes on a new game.</summary>
        public void NewGame()
        {
            if (disposed || IsOpen || Seen || hudRoot.ClassListContains(StartScreenPresenter.OpenClass)) return;
            try { store!.Write(SeenKey, "seen"); }
            catch (Exception e) { Debug.LogWarning("The first-turn guide could not note that it was seen: " + e.Message); }
            Open();
        }

        /// <summary>Opens the guide at its first step (How to play's button; NewGame). An open side-menu screen closes.</summary>
        public void Open()
        {
            if (disposed) return;
            closeScreens?.Invoke();
            if (!IsOpen)
            {
                cardWasOpen = card?.Shown != null && !card.Collapsed;
                cardEvent = card?.Shown?.Id;
                openTurn = bridge?.State.Turn ?? 0;
            }
            hudRoot.AddToClassList(OpenClass);
            follow.Resume();
            eventShown = false;
            Show(0);
        }

        /// <summary>Next, or Done on the last step.</summary>
        public void Next()
        {
            if (!IsOpen) return;
            var next = NextStep(Step);
            if (next < 0) Close(); else Show(next);
        }

        /// <summary>Whether a step is in the tour now: every step but the event card's, which is left out while there is no
        /// card to explain (answered before the guide reached it; owner's report on v0.1.4).</summary>
        bool InTour(int step) => step != EventStep || card?.Shown != null;

        /// <summary>The step after <paramref name="step"/> in the tour, or -1 after the last.</summary>
        int NextStep(int step)
        {
            for (int s = step + 1; s < Steps.Length; s++) if (InTour(s)) return s;
            return -1;
        }

        /// <summary>The game changed under the open guide (an order queued, the card answered): on the event step, once the
        /// card it explains has closed with no other card to show, the guide goes on to the next step, as Next would, and Next
        /// takes the focus back from the answer the player pressed.</summary>
        void GameChanged()
        {
            if (disposed || !IsOpen || Step != EventStep || card?.Shown != null) return;
            Next();
        }

        /// <summary>Escape: skips the guide. True when it was open.</summary>
        public bool Escape()
        {
            if (!IsOpen) return false;
            Close();
            return true;
        }

        /// <summary>Closes the guide (Skip, Done, Escape) and puts the decision card and the selection back as they were when
        /// the guide opened.</summary>
        public void Close() => Close(restoreCard: true);

        /// <summary>The turn has ended (End Turn, which the guide leaves in reach): when it advanced, the guide closes, as Skip
        /// does. Only the card the guide found is put back as it was; a new turn's card is left to its own rule (a new event
        /// opens), so it neither stays hidden nor shows its backdrop under the guide (review of #1209). A refused turn (the
        /// state unchanged) leaves the guide open.</summary>
        void TurnEnded()
        {
            if (!IsOpen || bridge!.State.Turn == openTurn) return;
            Close(restoreCard: card?.Shown?.Id == cardEvent);
        }

        void Close(bool restoreCard)
        {
            if (!IsOpen) return;
            Step = -1;
            follow.Pause();
            hudRoot.RemoveFromClassList(OpenClass);
            var focused = layer.focusController?.focusedElement as VisualElement;
            bool hadFocus = focused != null && layer.Contains(focused);
            if (hadFocus) focused!.Blur();
            if (restoreCard && card?.Shown != null)
            {
                if (cardWasOpen && card.Collapsed) card.Reopen();   // the card takes the focus itself
                else if (!cardWasOpen && !card.Collapsed) card.Collapse(takeFocus: false);
            }
            // From the keyboard, the next Tab carries on from End Turn (#41 M1), unless the card is open and took the focus.
            if (hadFocus && (card?.Shown == null || card.Collapsed)) FocusReturn.To(hudRoot.Q<Button>("end-turn"));
            var back = putBack; putBack = null;
            back?.Invoke();
        }

        void Show(int step)
        {
            Step = step;
            var key = Steps[step];
            if (step == EventStep) eventShown = true;
            // Counted as the player meets them: the event step counts while it has a card or once it has shown.
            bool Counted(int s) => s == EventStep ? eventShown || InTour(s) : true;
            int number = 0, total = 0;
            for (int s = 0; s < Steps.Length; s++) if (Counted(s)) { total++; if (s <= step) number++; }
            count.text = text.Format("guide.step", number, total);
            title.text = text["guide." + key + ".title"];
            words.text = text["guide." + key + ".text"];
            next.text = text[NextStep(step) < 0 ? "guide.done" : "guide.next"];
            // A step that does not explain the city or the unit puts away the one an earlier step showed (as Close does), so
            // no stale panel stays under the event card or Objectives.
            if (key != "city" && key != "training" && key != "move")
            {
                var back = putBack; putBack = null;
                back?.Invoke();
            }
            // The card shows only on its own step: elsewhere its backdrop would dim the parts the other steps point at.
            if (card?.Shown != null)
            {
                if (step == EventStep && card.Collapsed) card.Reopen();
                else if (step != EventStep && !card.Collapsed) card.Collapse(takeFocus: false);
            }
            if (key == "city" || key == "training" || key == "move")
            {
                putBack ??= keepSelection?.Invoke();   // what the guide found, before it first shows its city or unit
                if (key == "move") showUnit?.Invoke(); else showCity?.Invoke();
            }
            // The panel is placed once this frame's layout has run (the city or unit just shown has its size only then), and
            // transparent until then, so it never shows for a frame in the half its target is about to take (steps 2 and 3).
            shownFrame = Frame();
            if (shownFrame < 0) Place();   // EditMode: laid out on demand
            else
            {
                dock.style.opacity = 0f;   // transparent, not hidden: a hidden Next could not take the focus below
                ring.style.display = DisplayStyle.None;   // nor the ring round the last step's target
                layer.schedule.Execute(PlaceWhenLaidOut);
            }
            FocusReturn.To(next);   // Enter goes on to the next step; again once styles resolve, as the layer may only now show
        }

        /// <summary>The frame count in play (the layout runs once a frame, after the scheduler), or -1 in EditMode, where the
        /// tests lay panels out on demand and Place runs at once.</summary>
        static int Frame() => Application.isPlaying ? Time.frameCount : -1;

        void PlaceWhenLaidOut()
        {
            if (disposed || !IsOpen) return;
            if (shownFrame >= 0 && Frame() == shownFrame) { layer.schedule.Execute(PlaceWhenLaidOut); return; }
            Place();
        }

        /// <summary>The elements a step points at, as the HUD has them now (the ring goes round the shown ones): Attention in
        /// the top bar; the city's housing, jobs and workers rows in the info panel (GddHexPanel); its training rows; the unit
        /// card's Move button (#1265; it shows only while the card's unit can move); the card's options, or "Decision
        /// waiting" while it is hidden; and the side menu's Objectives button (#1189).</summary>
        public IReadOnlyList<VisualElement> Targets(int step)
        {
            var found = new List<VisualElement?>();
            switch (Steps[step])
            {
                case "attention": found.Add(hudRoot.Q("attention")); break;
                case "city":
                    var gdd = hudRoot.Q(GddHexPanel.HostName);
                    foreach (var name in new[] { "city-housing", "city-jobs-row", "city-workers-row" }) found.Add(gdd?.Q(name));
                    break;
                case "training":
                    var host = hudRoot.Q(GddHexPanel.HostName);
                    if (host != null) found.AddRange(host.Query<VisualElement>().Where(e => e.name.StartsWith("city-train-", StringComparison.Ordinal) && e.name.EndsWith("-row", StringComparison.Ordinal)).ToList());
                    break;
                case "move": found.Add(hudRoot.Q("info-move")); break;
                case "event":
                    found.Add(card?.Shown != null && card.Collapsed ? hudRoot.Q("event-reopen") : hudRoot.Q("event-options"));
                    break;
                case "objectives": found.Add(hudRoot.Q("nav-objectives")); break;
            }
            return found.Where(e => e != null && Shown(e)).Select(e => e!).ToList();
        }

        /// <summary>The window area the ring surrounds (the union of the step's shown targets), or null when none shows.</summary>
        public Rect? TargetArea()
        {
            if (Step < 0) return null;
            Rect? area = null;
            foreach (var e in Targets(Step))
            {
                var r = e.worldBound;
                area = area is Rect a ? Rect.MinMaxRect(Mathf.Min(a.xMin, r.xMin), Mathf.Min(a.yMin, r.yMin), Mathf.Max(a.xMax, r.xMax), Mathf.Max(a.yMax, r.yMax)) : r;
            }
            return area;
        }

        /// <summary>Puts the ring round the step's targets, and the panel in the other half of the window, so it never covers
        /// them. With no target shown (a card already answered, a city panel the player closed), no ring and the panel at the
        /// bottom.</summary>
        internal void Place()
        {
            if (!IsOpen) return;
            if (shownFrame >= 0 && Frame() == shownFrame) return;   // not before the step's layout (PlaceWhenLaidOut)
            shownFrame = -1;
            dock.style.opacity = StyleKeyword.Null;
            ApplyTextFloor();
            var area = TargetArea();
            ring.style.display = area == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (area is not Rect a) { dock.RemoveFromClassList(TopClass); return; }
            var topLeft = layer.WorldToLocal(new Vector2(a.xMin, a.yMin)) - new Vector2(RingGap, RingGap);
            var bottomRight = layer.WorldToLocal(new Vector2(a.xMax, a.yMax)) + new Vector2(RingGap, RingGap);
            // Inside the window: a target at its edge (Attention under the top of the window) gets a narrower gap there.
            var size = layer.layout.size;
            if (size.x > 0 && size.y > 0)
            {
                topLeft = Vector2.Max(topLeft, Vector2.zero);
                bottomRight = Vector2.Min(bottomRight, size);
            }
            ring.style.left = topLeft.x; ring.style.top = topLeft.y;
            ring.style.width = bottomRight.x - topLeft.x; ring.style.height = bottomRight.y - topLeft.y;
            dock.EnableInClassList(TopClass, a.center.y > layer.worldBound.center.y);
        }

        /// <summary>The top bar's text floor on the guide's text (HudPresenter.FlooredTextSize): each at its stylesheet size, or
        /// larger where that would be under the floor on screen. Run with Place, so it follows a resized window while the guide
        /// shows; the stylesheet's size comes back (no inline size) wherever the floor does not raise it.</summary>
        void ApplyTextFloor()
        {
            if (layer.panel == null) return;
            var scale = HudPresenter.ScreenPxPerPanelPx(layer.panel);
            if (scale == flooredFor) return;
            flooredFor = scale;
            foreach (var (element, px) in floored)
            {
                var size = HudPresenter.FlooredTextSize(px, scale);
                element.style.fontSize = size > px ? new StyleLength(size) : new StyleLength(StyleKeyword.Null);
            }
        }

        /// <summary>Shown on the panel: laid out with a size, and neither it nor any parent hidden.</summary>
        static bool Shown(VisualElement e)
        {
            if (e.panel == null || !(e.worldBound.width > 0 && e.worldBound.height > 0)) return false;
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None || v.resolvedStyle.visibility == Visibility.Hidden) return false;
            return true;
        }

        /// <summary>The city the guide shows in the info panel: the first city (cities.yaml order) with a training facility at
        /// the start (Training.HasFacility, the sim's helper), else the first city; its settlement hex, or null with none.</summary>
        public static (int q, int r)? CityHex(Content c, GameState s)
        {
            var cities = c.Cities.Where(x => s.Cities.ContainsKey(x.Id)).ToList();
            var city = cities.FirstOrDefault(x => c.UnitTypes.Any(t => t.Facility != "" && Training.HasFacility(c, s, x.Id, t.Facility)))
                ?? cities.FirstOrDefault();
            if (city == null) return null;
            foreach (var h in c.SettlementHexes) if (h.Id == city.Id) return (h.Q, h.R);
            return null;
        }

        /// <summary>The unit the guide shows on its move step: the first of <paramref name="units"/> (the drawn tokens' rows, in
        /// units.yaml order) whose sim unit has a hex in Movement.Reachable now, so its card shows Move
        /// (InfoPanelPresenter.MoveTargets' rule); its id, or null with none (the legacy loop, or no unit able to move).</summary>
        public static string? UnitToShow(Content c, GameState s, IEnumerable<UnitEntry> units) =>
            units.FirstOrDefault(u => UnitCard.SimUnit(c, s, u) is int id && Movement.Reachable(c, s, id).Count > 0)?.Id;

        /// <summary>The drawn-token form used by the game. A raised token carries its sim id even without a starting row.</summary>
        public static string? UnitToShow(Content c, GameState s, IEnumerable<UnitToken> units) =>
            units.FirstOrDefault(u => UnitCard.SimUnit(c, s, u) is int id && Movement.Reachable(c, s, id).Count > 0)?.Unit.Id;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            follow.Pause();
            Step = -1;
            putBack = null;   // a game going away: nothing to put back
            hudRoot.RemoveFromClassList(OpenClass);
            if (bridge != null) { bridge.TurnEnded -= onTurn; bridge.Changed -= onChanged; }
            next.clicked -= onNext; skip.clicked -= onSkip; reopenButton.clicked -= onReopen;
        }
    }
}
