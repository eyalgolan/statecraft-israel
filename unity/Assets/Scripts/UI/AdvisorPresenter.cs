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
        public Action? ShowCity, ShowUnit;
        public Func<Action>? KeepSelection;
        public Func<Rect?>? MapTarget;
        public Action? CloseScreens;
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
        /// <summary>Statecraft.uss sizes (.advisor-name, .advisor-role, .advisor-text, Button, .advisor-count, .advisor-quote, .advisor-quote-source), in panel px,
        /// raised where they would be under the text floor on screen (HudPresenter.FlooredTextSize).</summary>
        public const float NamePx = 24f, RolePx = 15f, TextPx = 22f, ButtonPx = 19f, CountPx = 15f, QuotePx = 20f, QuoteSourcePx = 15f;
        /// <summary>The fade out (Statecraft.uss .advisor-popup.window-closing), in ms.</summary>
        public const long HideMs = 150;
        public const string RingClass = "advisor-ring-open";
        public const long RingOnlyMs = 4000;
        public const string WatchingClass = "advisor-watching", OverlayOpenClass = "advisor-overlay-open";
        /// <summary>On the popup while a line with no picture plays its narration: the portrait row stays (owner, 24 Oct 2026, #1547).</summary>
        public const string NarratingClass = "advisor-narrating";
        AdvisorMedia? media;
        public AdvisorMedia? Media
        {
            get => media;
            set { if (ReferenceEquals(media, value)) return; StopWatching(); media?.Dispose(); media = value; UpdateWatch(); }
        }
        internal bool? ForceInline { get; set; }
        readonly VisualElement mediaPicture, overlay, overlayPicture, mediaHead;
        readonly Label mediaLabel, mediaCredit, overlayCaption, overlayLabel, overlayCredit;
        readonly Button watch, overlayClose;
        readonly Action onWatch, onOverlayClose;
        readonly EventCallback<GeometryChangedEvent> onMediaGeometry;
        string watchScreens = "";
        VisualElement? mediaOpener;
        bool watchStoppedOnPress;
        int overlayClosedAt = -1;
        internal bool OverlayClosedThisFrame => overlayClosedAt == Time.frameCount;
        readonly List<(VisualElement element, bool enabled)> overlayCoveredScreens = new List<(VisualElement, bool)>();
        readonly EventCallback<PointerDownEvent> onMediaPointer;
        readonly EventCallback<NavigationSubmitEvent> onMediaSubmit;
        readonly EventCallback<NavigationCancelEvent> onMediaCancel;

        readonly VisualElement ring;
        string? target;
        Action? putBack;
        /// <summary>The selection a rebuilt presenter takes over from the one before it (ChainState.PutBack).</summary>
        Action? adopted;
        IVisualElementScheduledItem? ringOnly;

        readonly VisualElement hudRoot, layer, popup, portrait;
        readonly Label name, role, words, count, quote, quoteSource;
        readonly Button showMe, more, notNow, gotIt, close;
        readonly Button? reopen;
        readonly Action onReopen;
        GameSession? openingGame;
        readonly SimBridge bridge; readonly UiText text; readonly AdvisorSession session; readonly PortraitCatalog? portraits;
        readonly AdvisorHooks hooks; readonly bool hebrew;
        readonly Action onChanged, onCover, onShowMe, onGotIt, onNotNow, onClose, onAttention, onLevel;
        readonly IReadOnlyList<AdvisorCatalog.Message> chain;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly EventCallback<GeometryChangedEvent> onLaidOut;
        readonly (VisualElement element, float px)[] floored;
        readonly IVisualElementScheduledItem follow;
        /// <summary>The latest hide's fade end, paused by a Show or Dispose so an earlier one never acts.</summary>
        IVisualElementScheduledItem? fading;
        float flooredFor;
        bool disposed, hidden = true;
        /// <summary>The frame Escape closed it in (FeedbackPresenter's pattern): GameRoot's poll then knows the key is used.</summary>
        int escapedAt = -1;
        /// <summary>The message a layer covered, to show again or drop when the layer closes (spec 4.2).</summary>
        AdvisorCatalog.Message? covered;

        public AdvisorPresenter(VisualElement root, SimBridge bridge, UiText text, AdvisorSession session,
            PortraitCatalog? portraits = null, AdvisorHooks? hooks = null)
        {
            this.bridge = bridge; this.text = text; this.session = session; this.portraits = portraits;
            this.hooks = hooks ?? new AdvisorHooks();
            chain = OrderedChain(session.Catalog);
            hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            hudRoot = root.Q("root") ?? root;
            layer = root.Q<VisualElement>(LayerName);
            popup = layer.Q<VisualElement>("advisor-popup");
            ring = layer.Q("advisor-ring");
            mediaHead = popup.Q(className: "advisor-head");
            mediaPicture = popup.Q("advisor-media"); mediaLabel = popup.Q<Label>("advisor-media-label"); mediaCredit = popup.Q<Label>("advisor-media-credit");
            overlay = hudRoot.Q("advisor-overlay"); overlayPicture = overlay.Q("advisor-overlay-media");
            overlayCaption = overlay.Q<Label>("advisor-overlay-caption"); overlayLabel = overlay.Q<Label>("advisor-overlay-label"); overlayCredit = overlay.Q<Label>("advisor-overlay-credit");
            watch = popup.Q<Button>("advisor-watch"); overlayClose = overlay.Q<Button>("advisor-overlay-close");
            watch.text = text["advisor.watch"]; overlayClose.text = text["advisor.close"];
            onWatch = () => { if (watchStoppedOnPress) { watchStoppedOnPress = false; return; } if (Media?.Playing == true) StopWatching(); else if (Shown?.Media is AdvisorCatalog.Media row) Watch(row, "advisor." + Shown.Id); };
            onOverlayClose = () => CloseOverlay();
            watch.clicked += onWatch; overlayClose.clicked += onOverlayClose;
            onMediaPointer = e => StopBeforeButton(e); onMediaSubmit = e => StopBeforeButton(e);
            onMediaCancel = e => { if (CloseOverlay()) e.StopPropagation(); };
            hudRoot.RegisterCallback(onMediaPointer, TrickleDown.TrickleDown); hudRoot.RegisterCallback(onMediaSubmit, TrickleDown.TrickleDown);
            overlay.RegisterCallback(onMediaCancel);

            onMediaGeometry = _ =>
            {
                SizeMedia(); ApplyTextFloor();
                if (Media?.Playing == true && ForceInline == null && popup.ClassListContains(WatchingClass) && layer.layout.width > 0
                    && !AdvisorMedia.Inline(AdvisorLayout.Tier(hudRoot), Slot().height)) StopWatching();
            };
            hudRoot.RegisterCallback(onMediaGeometry); mediaPicture.RegisterCallback(onMediaGeometry); overlayPicture.RegisterCallback(onMediaGeometry);

            portrait = popup.Q<VisualElement>("advisor-portrait");
            name = popup.Q<Label>("advisor-name"); role = popup.Q<Label>("advisor-role");
            words = popup.Q<Label>("advisor-text"); count = popup.Q<Label>("advisor-count");
            quote = popup.Q<Label>("advisor-quote"); quoteSource = popup.Q<Label>("advisor-quote-source");
            showMe = popup.Q<Button>("advisor-show-me"); more = popup.Q<Button>("advisor-more");
            notNow = popup.Q<Button>("advisor-not-now"); gotIt = popup.Q<Button>("advisor-got-it"); close = popup.Q<Button>("advisor-close");
            showMe.text = text["advisor.show_me"]; more.text = text["advisor.more"]; notNow.text = text["advisor.not_now"];
            gotIt.text = text["advisor.got_it"]; close.text = text["advisor.close"];
            floored = new (VisualElement, float)[] { (name, NamePx), (role, RolePx), (words, TextPx), (showMe, ButtonPx), (more, ButtonPx), (notNow, ButtonPx), (gotIt, ButtonPx), (close, ButtonPx), (count, CountPx), (mediaLabel, 15f), (mediaCredit, 15f), (overlayCaption, TextPx), (overlayLabel, 15f), (overlayCredit, 15f), (watch, 15f), (overlayClose, ButtonPx), (quote, QuotePx), (quoteSource, QuoteSourcePx) };
            onShowMe = ShowMe; onGotIt = GotIt; onNotNow = NotNow;
            onClose = () => { if (session.Chain.Step >= 0) CloseChain(); else GotIt(); };
            reopen = root.Q<Button>("how-to-play-guide");
            onReopen = OpenChain;                                       // which closes How to play
            if (reopen != null)
            {
                reopen.text = text["how_to_play.guide"]; reopen.clicked += onReopen;
                Display(reopen, chain.Any(m => m.ApprovedIn(hebrew)));  // no button that opens nothing (pending Hebrew)
            }
            showMe.clicked += onShowMe; gotIt.clicked += onGotIt; close.clicked += onClose; notNow.clicked += onNotNow;
            onAttention = () => Notice(AdvisorSignals.AttentionRefused);
            bridge.RefusedForAttention += onAttention;
            onChanged = Evaluate; onTurn = _ => Notice(AdvisorSignals.TurnEnded); onCover = CoverChanged; onLaidOut = PlaceWhenLaidOut;
            bridge.Changed += onChanged; bridge.TurnEnded += onTurn;
            onLevel = Evaluate; session.Settings.LevelChanged += onLevel;   // a new help level applies at once (spec 6)
            layer.userData = onCover;                                   // ScreenNav.DisableCovered calls it
            // The slot moves with the layout (a window resized, "Decision waiting" or the toast shown), so the popup follows
            // it while it shows, as its ring does.
            follow = layer.schedule.Execute(Place).Every(100);
            follow.Pause();
            hudRoot.RemoveFromClassList(OpenClass);
            if (session.Chain.Step >= 0)
            {
                openingGame = bridge.Session;
                adopted = session.Chain.PutBack; session.Chain.PutBack = null;
                if (chain[session.Chain.Step].ApprovedIn(hebrew) && this.hooks.Card?.Shown != null && !this.hooks.Card.Collapsed)
                    this.hooks.Card.Collapse(takeFocus: false);
                RenderChain();
            }
        }

        /// <summary>A modal layer is open (spec 4.2): the start screen, the open decision card, the capture dialog, the turn
        /// report, an outcome clip, the end screen; and, on the two-row HUD, any side screen (it takes the full width).</summary>
        public static bool Covered(VisualElement hudRoot) =>
            hudRoot.ClassListContains(StartScreenPresenter.OpenClass) || hudRoot.ClassListContains(EventCardPresenter.OpenClass)
            || hudRoot.ClassListContains(CaptureDialogPresenter.OpenClass) || hudRoot.ClassListContains(TurnReportPresenter.OpenClass)
            || hudRoot.ClassListContains(OutcomePresenter.OpenClass) || hudRoot.ClassListContains(EndScreenPresenter.OpenClass)
            || (hudRoot.ClassListContains(HudPresenter.TwoRowClass) && hudRoot.ClassListContains(ScreenNav.ScreenOpenClass));

        /// <summary>Showing a message: on screen and not fading out.</summary>
        public bool IsOpen => !disposed && hudRoot.ClassListContains(OpenClass) && !popup.ClassListContains(ScreenNav.ClosingClass);

        /// <summary>The message on screen, or null.</summary>
        public AdvisorCatalog.Message? Shown { get; private set; }

        int Turn => bridge.State.Turn;
        AdvisorContext Context() => new AdvisorContext(bridge, hooks.Card);

        /// <summary>A new game (GameRoot: the start screen closing on New Game, or the top bar's New game): nothing waits,
        /// the history starts empty, and new_game fires.</summary>
        public void NewGame()
        {
            if (disposed) return;
            // StartScreen's close and NewGamePresenter.Started can report the same new session (Task7).
            if (session.Chain.Step >= 0 && ReferenceEquals(openingGame, bridge.Session)) return;
            session.Queue.Reset(); session.Fired.Clear(); session.FiredTurn = Turn;
            Shown = null; covered = null;
            session.Chain.Step = -1; session.Chain.PutBack = null; adopted = null;
            ClearTarget();
            Notice(AdvisorSignals.NewGame);
            if (session.AutoShow && session.Settings.HasFile && !session.Settings.OpeningSeen
                && !hudRoot.ClassListContains(StartScreenPresenter.OpenClass) && chain.Any(InTour))
            {
                session.Settings.MarkOpeningSeen();
                OpenChain();
            }
        }

        /// <summary>A loaded game (Continue closed the start screen): the warnings that already hold are offered at once, not
        /// only after the next change.</summary>
        public void Resume() => Evaluate();

        /// <summary>The Budget screen's unsubmitted state changed (BudgetPresenter.UnsubmittedChanged): an edit not submitted
        /// fires budget_unsubmitted; Submit takes it back.</summary>
        public void BudgetUnsubmitted(bool on)
        {
            if (on) { Notice(AdvisorSignals.BudgetUnsubmitted); return; }
            if (disposed) return;
            session.Fired.Remove(AdvisorSignals.BudgetUnsubmitted);
            session.Queue.Withdraw(AdvisorSignals.BudgetUnsubmitted, Turn, bridge.State.Date);
            if (covered?.Signal == AdvisorSignals.BudgetUnsubmitted) covered = null;
            Render();
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
            if (session.Chain.Step >= 0) { RenderChain(); return; }
            if (Covered(hudRoot)) { if (Shown != null) covered = Shown; Hide(); return; }
            var m = session.Queue.Head(Turn, hebrew);
            if (m == null) { Shown = null; ClearTarget(); Hide(); return; }
            if (Shown?.Id != m.Id) ClearTarget();
            Fill(m);
            Show();
        }

        void Fill(AdvisorCatalog.Message m)
        {
            if (Shown?.Id != m.Id) StopWatching();
            Shown = m;
            gotIt.text = text["advisor.got_it"];
            name.text = text[m.Advisor == "meyerson" && !session.Introduced(m.Advisor) ? "advisor.meyerson.name_first" : "advisor." + m.Advisor + ".name"];
            role.text = text["advisor." + m.Advisor + ".role"];
            if (!Captioning) words.text = text["advisor." + m.Id + ".text"];
            var quoteKey = "advisor.quote." + m.Quote;
            bool verifiedQuote = AdvisorQuotes.Ids.Contains(m.Quote) && text.Has(quoteKey + ".text") && text.Has(quoteKey + ".source");
            quote.text = verifiedQuote ? text[quoteKey + ".text"] : "";
            quoteSource.text = verifiedQuote ? text[quoteKey + ".source"] : "";
            Display(quote, verifiedQuote); Display(quoteSource, verifiedQuote);
            UpdateWatch();
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
            fading?.Pause();
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
            if (Place(TargetRect()) == null) layer.RegisterCallback(onLaidOut);
        }

        /// <summary>Once, on the layer's first layout after Show: places the popup, which makes it visible.</summary>
        void PlaceWhenLaidOut(GeometryChangedEvent e)
        {
            if (Place(TargetRect()) != null) layer.UnregisterCallback(onLaidOut);
        }

        void EndOpening(GeometryChangedEvent e)
        {
            popup.UnregisterCallback<GeometryChangedEvent>(EndOpening);
            popup.RemoveFromClassList(ScreenNav.OpeningClass);
        }

        void Hide()
        {
            StopWatching();
            if (hidden) return;
            hidden = true;
            ring.style.display = DisplayStyle.None;
            follow.Pause();
            layer.UnregisterCallback(onLaidOut);
            var focused = layer.focusController?.focusedElement as VisualElement;
            if (focused != null && layer.Contains(focused)) { focused.Blur(); FocusReturn.To(hudRoot.Q<Button>("end-turn")); }
            if (!ScreenNav.Moves()) { hudRoot.RemoveFromClassList(OpenClass); return; }
            popup.AddToClassList(ScreenNav.ClosingClass);
            fading?.Pause();
            fading = popup.schedule.Execute(() =>
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
            if (Media?.Playing == true && (HardMediaCover() || CurrentScreens() != watchScreens)) StopWatching();
            if (hudRoot.ClassListContains(OverlayOpenClass)) return;
            if (session.Chain.Step >= 0) { RenderChain(); return; }
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

        /// <summary>Draws the ring round <paramref name="name"/>'s elements, showing its unit or city first as a click would
        /// (the selection it finds is put back by ClearTarget). Never carries out the advised action (spec 7.3).</summary>
        public void ShowTarget(string name)
        {
            if (disposed) return;
            ClearTarget();
            target = name;
            if (AdvisorTargets.ShowsCity(name) || AdvisorTargets.ShowsUnit(name))
            {
                putBack = adopted ?? hooks.KeepSelection?.Invoke();   // after a rebuild, the selection found before it
                adopted = null;
                (AdvisorTargets.ShowsUnit(name) ? hooks.ShowUnit : hooks.ShowCity)?.Invoke();
            }
            hudRoot.AddToClassList(RingClass);
            follow.Resume();
            if (Shown == null) ringOnly = ring.schedule.Execute(ClearTarget).StartingIn(RingOnlyMs);   // the encyclopedia's ring fades by itself
            Place();
        }

        public void ClearTarget()
        {
            ringOnly?.Pause(); ringOnly = null;
            target = null;
            hudRoot.RemoveFromClassList(RingClass);
            if (!hudRoot.ClassListContains(OpenClass)) follow.Pause();
            var back = putBack; putBack = null;
            back?.Invoke();
        }

        Rect? TargetRect()
        {
            if (target == null) return null;
            var world = AdvisorTargets.Area(AdvisorTargets.Find(hudRoot, target, hooks.Card));
            Rect? local = world is Rect a ? Local(a) : (Rect?)null;
            ring.style.display = local == null || Covered(hudRoot) ? DisplayStyle.None : StyleKeyword.Null;
            if (local is Rect area)
            {
                var tl = Vector2.Max(area.min - new Vector2(4, 4), Vector2.zero);
                var br = Vector2.Min(area.max + new Vector2(4, 4), layer.layout.size);
                ring.style.left = tl.x; ring.style.top = tl.y; ring.style.width = br.x - tl.x; ring.style.height = br.y - tl.y;
            }
            // The ring stays on the named HUD control; the popup also keeps clear of its selected map hex (spec7.3).
            if ((AdvisorTargets.ShowsUnit(target) || AdvisorTargets.ShowsCity(target)) && hooks.MapTarget?.Invoke() is Rect map)
                return Local(map);
            return local;
        }

        public const int EventStep = 5;
        public IReadOnlyList<AdvisorCatalog.Message> Chain => chain;
        public bool InChain => session.Chain.Step >= 0 && IsOpen;
        public int ChainStep => InChain ? session.Chain.Step : -1;
        public static IReadOnlyList<string> ChainIds(AdvisorCatalog catalog) => OrderedChain(catalog).Select(m => m.Id).ToList();

        static List<AdvisorCatalog.Message> OrderedChain(AdvisorCatalog catalog)
        {
            var steps = catalog.Messages.Where(m => m.Signal == AdvisorSignals.Opening).ToList();
            var line = new List<AdvisorCatalog.Message>();
            var next = steps.FirstOrDefault(m => m.Requires.Count == 0);
            while (next != null && !line.Contains(next))
            {
                line.Add(next);
                next = steps.FirstOrDefault(m => m.Requires.Contains(line[line.Count - 1].Id));
            }
            return line;
        }

        bool InTour(AdvisorCatalog.Message message) => message.ApprovedIn(hebrew) && AdvisorSignals.When(Context(), message.When);
        int NextChainStep(int after)
        {
            for (int i = after + 1; i < chain.Count; i++) if (InTour(chain[i])) return i;
            return -1;
        }

        /// <summary>Manual reopen starts at the greeting; the opening card and selection are restored on close.</summary>
        public void OpenChain()
        {
            if (disposed) return;
            var first = NextChainStep(-1);
            if (first < 0) return;                         // no unreviewed Hebrew, even when explicitly asked
            hooks.CloseScreens?.Invoke();
            openingGame = bridge.Session;
            if (session.Chain.Step < 0)
            {
                session.Chain.CardWasOpen = hooks.Card?.Shown != null && !hooks.Card.Collapsed;
                session.Chain.CardEvent = hooks.Card?.Shown?.Id;
                session.Chain.OpenTurn = Turn;
            }
            session.Chain.EventShown = false;
            session.Chain.Step = first;
            if (hooks.Card?.Shown != null && !hooks.Card.Collapsed) hooks.Card.Collapse(takeFocus: false);
            ShowStep(first);
            FocusReturn.To(gotIt);                          // the player opens it, so Enter goes on (spec 16)
        }

        public void NextStep()
        {
            int step = session.Chain.Step;
            if (disposed || step < 0 || step >= chain.Count || !chain[step].ApprovedIn(hebrew)) return;
            session.Queue.Done(chain[step], Turn, bridge.State.Date);
            int next = NextChainStep(step);
            if (next < 0) CloseChain(); else ShowStep(next);
        }

        public void CloseChain() => CloseChain(restoreCard: true);
        void CloseChain(bool restoreCard)
        {
            if (disposed || session.Chain.Step < 0) return;
            session.Chain.Step = -1; session.Chain.PutBack = null; adopted = null;
            Shown = null; covered = null;
            ClearTarget();
            Hide();
            if (restoreCard && hooks.Card?.Shown != null)
            {
                if (session.Chain.CardWasOpen && hooks.Card.Collapsed) hooks.Card.Reopen();
                else if (!session.Chain.CardWasOpen && !hooks.Card.Collapsed) hooks.Card.Collapse(takeFocus: false);
            }
            Render();
        }

        void ShowStep(int step)
        {
            session.Chain.Step = step;
            if (!chain[step].ApprovedIn(hebrew)) { Shown = null; Hide(); return; }
            if (Covered(hudRoot)) { Hide(); return; }
            // The same step drawn again (the game changed under it, or a layer over it closed): its city or unit stays as
            // the player has it now, rather than being put back and shown again on every change.
            bool same = ReferenceEquals(Shown, chain[step]) && target != null;
            if (!same) ClearTarget();
            if (step == EventStep) session.Chain.EventShown = true;
            Fill(chain[step]);
            bool Counted(int i) => chain[i].ApprovedIn(hebrew) && (i == EventStep ? session.Chain.EventShown || InTour(chain[i]) : InTour(chain[i]));
            int number = Enumerable.Range(0, step + 1).Count(Counted), total = Enumerable.Range(0, chain.Count).Count(Counted);
            count.text = text.Format("advisor.step", number, total);
            gotIt.text = text[NextChainStep(step) < 0 ? "advisor.done" : "advisor.next"];
            Display(notNow, false);
            Display(showMe, chain[step].Actions.Any(a => a.OpenCard));
            if (!same) foreach (var action in chain[step].Actions) if (action.ShowMe != "") ShowTarget(action.ShowMe);
            Show();
        }

        void RenderChain()
        {
            if (session.Chain.Step < 0) return;
            if (Turn != session.Chain.OpenTurn) { CloseChain(hooks.Card?.Shown?.Id == session.Chain.CardEvent); return; }
            if (!chain[session.Chain.Step].ApprovedIn(hebrew)) { Shown = null; Hide(); return; }
            if (Covered(hudRoot)) { Hide(); return; }
            if (session.Chain.Step == EventStep && hooks.Card?.Shown == null)
            {
                NextStep();                                 // the card was answered: on to the next step
                if (InChain) TakeFocusFromTheCard();
            }
            else ShowStep(session.Chain.Step);
        }

        /// <summary>The answered card hands the focus to End turn (#41 M1) while the popup, hidden under the card, cannot take
        /// it yet: Next takes it now if it can and again on the next tick, so Enter goes on rather than ending the turn (#1323).</summary>
        void TakeFocusFromTheCard()
        {
            FocusReturn.To(gotIt);
            gotIt.schedule.Execute(() => { if (!disposed && InChain) gotIt.Focus(); });
        }

        void ShowMe()
        {
            StopWatching();
            if (Shown == null) return;
            foreach (var a in Shown.Actions)
                if (a.ShowMe != "") ShowTarget(a.ShowMe);
                else if (a.OpenScreen != "") hooks.OpenScreen?.Invoke(a.OpenScreen);
                else if (a.OpenCard && hooks.Card?.Shown != null) hooks.Card.Reopen();
        }

        /// <summary>Got it, the X, or Escape from inside: seen, and the next message (if any) shows.</summary>
        public void GotIt()
        {
            if (Shown == null || !IsOpen) return;                       // final S1: no action during fade
            StopWatching();
            if (session.Chain.Step >= 0) { NextStep(); return; }
            ClearTarget();
            session.Queue.Done(Shown, Turn, bridge.State.Date);
            Shown = null;
            Render();
        }

        /// <summary>Not now: off to the next turn and into Counsel's history (spec 7.1). Not for teaching or critical messages.</summary>
        public void NotNow()
        {
            if (Shown == null || !IsOpen || Shown.Teaching || Shown.Critical) return;
            StopWatching();
            ClearTarget();
            session.Queue.PutOff(Shown, Turn, bridge.State.Date);
            Shown = null;
            Render();
        }

        bool HardMediaCover() => hudRoot.ClassListContains(StartScreenPresenter.OpenClass) || hudRoot.ClassListContains(EventCardPresenter.OpenClass)
            || hudRoot.ClassListContains(CaptureDialogPresenter.OpenClass) || hudRoot.ClassListContains(TurnReportPresenter.OpenClass)
            || hudRoot.ClassListContains(OutcomePresenter.OpenClass) || hudRoot.ClassListContains(EndScreenPresenter.OpenClass)
            || hudRoot.ClassListContains(FeedbackPresenter.OpenClass) || hudRoot.ClassListContains(SharePresenter.OpenClass);

        void StopBeforeButton(EventBase e)
        {
            var element = e.target as VisualElement;
            var button = element as Button ?? element?.GetFirstAncestorOfType<Button>();
            if (button == null) return;
            if (button == watch) watchStoppedOnPress = Media?.Playing == true;
            if (Media?.Playing == true) StopWatching(); // before another window snapshots the covered UI states
        }

        string CurrentScreens() => string.Join("|", ScreenNav.Screens.Where(n => hudRoot.Q("screen-" + n) is VisualElement screen && ScreenNav.IsOpen(screen)));

        /// <summary>Spec 12, per line: a clip or a supplied still with its narration, or the narration alone with the portrait.
        /// Either needs the current language's narration; with none there is no Watch control.</summary>
        bool Watchable(AdvisorCatalog.Media? row) => Media != null && row != null && row.Narration.ContainsKey(hebrew ? "he" : "en")
            && !session.MediaFailed.Contains(FailureKey(row));
        /// <summary>The file a failure is remembered by: the picture, or a voice-only line's own narration (never an empty name,
        /// which every voice-only line would share).</summary>
        string FailureKey(AdvisorCatalog.Media row) =>
            string.IsNullOrEmpty(row.Picture) ? row.Narration.TryGetValue(hebrew ? "he" : "en", out var voice) ? voice : "" : row.Picture!;
        bool Captioning => popup.ClassListContains(WatchingClass) || popup.ClassListContains(NarratingClass);
        void UpdateWatch()
        {
            bool canWatch = Watchable(Shown?.Media) && Shown!.ApprovedIn(hebrew);
            Display(watch, canWatch); popup.EnableInClassList("advisor-has-media", canWatch);
        }

        internal void Watch(AdvisorCatalog.Media row, string keyStem) => PlayMedia(row, keyStem, overlayOnly: false);
        /// <summary>For the encyclopedia and Counsel adapters once their owners land; their source row must be approved. Supplied
        /// pictures only: a voice-only item keeps its own screen's portrait (spec 12), which those screens' owners draw.</summary>
        internal void WatchOverlay(AdvisorCatalog.Media row, string keyStem) => PlayMedia(row, keyStem, overlayOnly: true);

        void PlayMedia(AdvisorCatalog.Media row, string keyStem, bool overlayOnly)
        {
            bool hasPicture = !string.IsNullOrEmpty(row.Picture);
            if (disposed || !Watchable(row) || HardMediaCover() || (overlayOnly && !hasPicture)) return;
            StopWatching(); watchScreens = CurrentScreens(); mediaOpener = hudRoot.focusController?.focusedElement as VisualElement;
            // Without a picture the portrait row stays and the caption takes the text's place, never the overlay (spec 12).
            bool inline = !hasPicture || (!overlayOnly && (ForceInline ?? AdvisorMedia.Inline(AdvisorLayout.Tier(hudRoot), Slot().height)));
            var picture = inline ? mediaPicture : overlayPicture;
            var caption = inline ? words : overlayCaption;
            var label = inline ? mediaLabel : overlayLabel;
            var credit = inline ? mediaCredit : overlayCredit;
            label.text = text[row.Label]; credit.text = row.Credit == "" ? "" : text[row.Credit];
            // Each clip's credit starts at its first line, wherever the last one was scrolled to.
            if (credit.GetFirstAncestorOfType<ScrollView>() is ScrollView credits) credits.scrollOffset = Vector2.zero;
            popup.EnableInClassList(WatchingClass, inline && hasPicture);
            popup.EnableInClassList(NarratingClass, !hasPicture);
            if (inline && hasPicture) mediaHead.BringToFront();
            hudRoot.EnableInClassList(OverlayOpenClass, !inline);
            if (!inline)
            {
                foreach (var name in ScreenNav.Screens)
                    if (hudRoot.Q("screen-" + name) is VisualElement screen) { overlayCoveredScreens.Add((screen, screen.enabledSelf)); screen.SetEnabled(false); }
                ScreenNav.DisableCovered(hudRoot); FocusReturn.To(overlayClose);
            }
            SizeMedia(); ApplyTextFloor();
            Media!.Play(row, keyStem, hebrew, picture, caption, StopWatching, _ =>
            {
                session.MediaFailed.Add(FailureKey(row)); StopWatching(); UpdateWatch();
            });
        }

        void SizeMedia()
        {
            void Fit(VisualElement e, float ceiling)
            {
                float width = e.layout.width;
                float height = Mathf.Min(width / EventCardPresenter.MediaAspect, ceiling);
                if (width > 0 && Mathf.Abs(e.layout.height - height) > 0.5f) e.style.height = height;
            }
            Fit(mediaPicture, float.MaxValue);
            float availableHeight = Mathf.Max(0, hudRoot.layout.height - 220);
            overlay.Q("advisor-overlay-frame").style.width = Mathf.Min(1000, Mathf.Max(0, hudRoot.layout.width - 32), availableHeight * EventCardPresenter.MediaAspect + 40);
            Fit(overlayPicture, float.MaxValue);
        }

        void StopWatching()
        {
            bool overlayWasOpen = hudRoot.ClassListContains(OverlayOpenClass);
            Media?.Stop();
            if (popup.ClassListContains(WatchingClass)) mediaHead.SendToBack();
            popup.RemoveFromClassList(WatchingClass); popup.RemoveFromClassList(NarratingClass); hudRoot.RemoveFromClassList(OverlayOpenClass);
            if (Shown != null) words.text = text["advisor." + Shown.Id + ".text"];
            if (overlayWasOpen)
            {
                overlayClosedAt = Time.frameCount;
                foreach (var (element, enabled) in overlayCoveredScreens) element.SetEnabled(enabled);
                overlayCoveredScreens.Clear();
                ScreenNav.DisableCovered(hudRoot);
                if (!disposed) FocusReturn.To(mediaOpener ?? gotIt);
                mediaOpener = null;
            }
        }

        public bool CloseOverlay()
        {
            if (!hudRoot.ClassListContains(OverlayOpenClass)) return false;
            StopWatching(); return true;
        }

        /// <summary>Escape: closes it as the X does, only while the focus is inside it (spec 16). True when it did, and
        /// also in the frame ScreenNav's Cancel already closed it, so GameRoot's poll does not close the screen under it too.
        /// That frame check comes first: Got it can bring the next message up at once, and one Escape dismisses one message.</summary>
        public bool Escape()
        {
            if (escapedAt == Time.frameCount) return true;
            var focused = layer.focusController?.focusedElement as VisualElement;
            if (!IsOpen || focused == null || !popup.Contains(focused)) return false;
            escapedAt = Time.frameCount;
            if (session.Chain.Step >= 0) CloseChain(); else GotIt();
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
        internal void Place() => Place(TargetRect());

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
            // A rebuild (a language switch) resumes the chain: the selection it found goes on to the rebuilt presenter.
            if (session.Chain.Step >= 0) { session.Chain.PutBack = putBack ?? adopted; putBack = null; }
            ClearTarget();
            disposed = true;
            StopWatching(); Media?.Dispose();
            watch.clicked -= onWatch; overlayClose.clicked -= onOverlayClose;
            hudRoot.UnregisterCallback(onMediaGeometry); mediaPicture.UnregisterCallback(onMediaGeometry); overlayPicture.UnregisterCallback(onMediaGeometry);
            hudRoot.UnregisterCallback(onMediaPointer, TrickleDown.TrickleDown); hudRoot.UnregisterCallback(onMediaSubmit, TrickleDown.TrickleDown);
            overlay.UnregisterCallback(onMediaCancel);
            follow.Pause();
            fading?.Pause();
            layer.UnregisterCallback(onLaidOut);
            bridge.RefusedForAttention -= onAttention;
            bridge.Changed -= onChanged; bridge.TurnEnded -= onTurn;
            session.Settings.LevelChanged -= onLevel;
            if (layer.userData == (object)onCover) layer.userData = null;
            if (reopen != null) reopen.clicked -= onReopen;
            showMe.clicked -= onShowMe; gotIt.clicked -= onGotIt; close.clicked -= onClose; notNow.clicked -= onNotNow;
            hudRoot.RemoveFromClassList(OpenClass);
        }
    }
}
