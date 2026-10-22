#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The capture dialog (GDD Phase 6 PR 10, #802; docs/gdd/settlement-control.md): when a settlement's record
    /// awaits the player's choice (SettlementControl.AwaitingChoice), the first such place in the sim's ordinal order, with
    /// its name in the UI's language, its target row's operation line, and one button per option that
    /// SettlementControl.CaptureOptions offers, in the helper's order: the option's name (the GDD's words, Q-UI-1 (b)), its
    /// plain description, and the helper's preview of what it changes at once (Q-UI-2 (a)). An option the helper refuses
    /// (today Occupy, #798, and Deport, #800, "not built yet") is not shown, so a newly built one appears by itself. The
    /// dialog holds no order, cost or rule of its own (Q-TECH-5): it formats the sim's numbers as the top bar does, and
    /// nothing more.
    /// <para>It sits in the decision card's place and style (Game.uxml "capture-card"), modal like it (OpenClass; ScreenNav.
    /// DisableCovered), and waits while a decision card is open: one window at a time. A choice is an order
    /// (ResolveCapture, through SimBridge.Queue): it takes effect at End Turn, and the dialog goes on to the next capture.
    /// Opening puts the focus on the dialog itself, so no option is lit as if recommended; Tab reaches the options in order,
    /// then Hide. Hide, or Escape, collapses it to "Decision waiting: place" without choosing; an unanswered capture waits,
    /// its units staying (R-CTRL-008). No picture, no sound, no video (impact plan section 4.1 item 6).</para></summary>
    public sealed class CaptureDialogPresenter : IDisposable
    {
        /// <summary>On the "root" element while the dialog shows: Statecraft.uss shows the decision layer's backdrop, and
        /// ScreenNav.DisableCovered takes the panels under it out of reach.</summary>
        public const string OpenClass = "capture-open";
        /// <summary>The class on each option button, beside the decision card's EventCardPresenter.OptionClass look.</summary>
        public const string OptionClass = "capture-option";

        readonly VisualElement card, layer, hudRoot, options; readonly Label title, operation, intro, none, count;
        readonly Button hide, reopen; readonly Button? endTurn;
        readonly SimBridge bridge; readonly UiText text; readonly EventCardPresenter? eventCard;
        readonly IReadOnlyDictionary<string, string>? namesHe;
        readonly Func<Content, GameState, string, IReadOnlyList<CaptureOption>> optionsFor;
        readonly Action onHide, onReopen, onUncovered, onRender;
        readonly EventCallback<PointerDownEvent> onHidePointer;
        readonly EventCallback<NavigationSubmitEvent> onHideSubmit;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly PressSource presses;
        /// <summary>The last Hide came from the pointer (PointerBlur's rule: no focus fill left behind for a mouse click).</summary>
        bool hideByPointer;
        /// <summary>The session the dialog last showed: a new one (New Game, a load) forgets what was collapsed or focused.</summary>
        GameSession? session;
        /// <summary>The place the player hid the dialog on, or null; another place opens it again.</summary>
        string? collapsedFor;
        /// <summary>The place whose dialog last took the focus, so a dialog already open never takes it again.</summary>
        string? focusedFor;

        /// <param name="eventCard">The decision card, which goes first: the dialog shows only while it is not open, and follows
        /// it (EventCardPresenter.Rendered). Null: no card (the dialog then follows SimBridge.Changed itself).</param>
        /// <param name="namesHe">Each settlement's Hebrew name, by id (the map's parse of settlements_1948.yaml, as the info
        /// panel gets it; ContentNames.Settlement).</param>
        /// <param name="helper">Test seam: the options helper; null is SettlementControl.CaptureOptions.</param>
        public CaptureDialogPresenter(VisualElement root, SimBridge bridge, UiText text, EventCardPresenter? eventCard,
            IReadOnlyDictionary<string, string>? namesHe = null,
            Func<Content, GameState, string, IReadOnlyList<CaptureOption>>? helper = null)
        {
            this.bridge = bridge; this.text = text; this.eventCard = eventCard; this.namesHe = namesHe;
            optionsFor = helper ?? SettlementControl.CaptureOptions;
            card = root.Q<VisualElement>("capture-card"); layer = card.parent; hudRoot = root.Q("root") ?? root;
            endTurn = hudRoot.Q<Button>("end-turn");
            title = card.Q<Label>("capture-title"); operation = card.Q<Label>("capture-operation");
            intro = card.Q<Label>("capture-intro"); none = card.Q<Label>("capture-none"); count = card.Q<Label>("capture-count");
            options = card.Q<VisualElement>("capture-options");
            hide = card.Q<Button>("capture-hide"); reopen = layer.Q<Button>("capture-reopen");
            hide.text = text["event.hide"];
            none.text = text["capture.none"];
            onHide = Collapse; onReopen = Reopen; onRender = Render;
            onUncovered = () => { focusedFor = null; if (Showing) FocusCard(Shown!, force: true); };
            onHidePointer = _ => hideByPointer = true;
            onHideSubmit = _ => hideByPointer = false;
            onCancel = e => { if (Escape()) e.StopPropagation(); };
            hide.clicked += onHide; reopen.clicked += onReopen;
            card.focusable = true; card.tabIndex = 0;   // the dialog takes the focus itself (FocusCard); Statecraft.uss .event-card:focus
            hide.RegisterCallback(onHidePointer, TrickleDown.TrickleDown);
            hide.RegisterCallback(onHideSubmit, TrickleDown.TrickleDown);
            card.RegisterCallback(onCancel);   // Escape on the open dialog, or on anything in it, is Hide (GameRoot polls it too)
            ScreenNav.AlsoOnUncovered(layer, onUncovered);   // a screen or the start screen over the layer has closed
            presses = new PressSource(options);
            if (eventCard != null) eventCard.Rendered += onRender; else bridge.Changed += onRender;
            Render();
        }

        /// <summary>The place awaiting the choice that the dialog is for (open or hidden), or null when every capture awaiting
        /// the choice has a queued answer, or there is none.</summary>
        public string? Shown { get; private set; }

        /// <summary>True while the dialog is hidden behind "Decision waiting: place" (Hide, Escape).</summary>
        public bool Collapsed => Shown != null && collapsedFor == Shown;

        /// <summary>The options the dialog shows: the helper's, those it does not refuse, in its order.</summary>
        public IReadOnlyList<CaptureOption> Offered { get; private set; } = Array.Empty<CaptureOption>();

        /// <summary>The decision card is open: the dialog waits behind it.</summary>
        bool CardOpen => eventCard?.Shown != null && !eventCard.Collapsed;

        bool Showing => Shown != null && !Collapsed && !CardOpen && layer.enabledInHierarchy;

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            if (bridge.Session != session) { session = bridge.Session; collapsedFor = null; focusedFor = null; }
            // The sim's order (Settlements is ordinal); a place with a queued answer has had its choice.
            var waiting = s.Settlements.Where(kv => kv.Value.State == SettlementControl.AwaitingChoice
                && bridge.QueuedLike(new ResolveCapture(kv.Key, "")) == null).Select(kv => kv.Key).ToList();
            Shown = waiting.FirstOrDefault();
            if (Shown == null) collapsedFor = null;
            var shown = Shown != null && !Collapsed && !CardOpen;
            if (!shown) focusedFor = null;   // hidden (behind the card, or by Hide): shown again, it takes the focus again
            ScreenNav.Show(card, shown);
            card.SetEnabled(shown);   // a child of an undisplayed element can still take the focus (#400)
            // In Decision waiting's place, so only while the card has no button there (it is neither open nor waiting).
            reopen.style.display = Collapsed && eventCard?.Shown == null ? DisplayStyle.Flex : DisplayStyle.None;
            hudRoot.EnableInClassList(OpenClass, shown);
            ScreenNav.DisableCovered(hudRoot);
            options.Clear();
            if (Shown == null) { Offered = Array.Empty<CaptureOption>(); return; }
            var id = Shown;
            var name = ContentNames.Settlement(text, c, id, namesHe);
            title.text = name;
            operation.text = c.SettlementTargets.FirstOrDefault(t => t.Id == id)?.Operation ?? "";
            operation.style.display = operation.text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            intro.text = text.Format("capture.intro", name);
            reopen.text = text.Format("capture.reopen", name);
            Offered = optionsFor(c, s, id).Where(o => o.Refusal == null && o.Preview != null).ToList();
            foreach (var o in Offered)
            {
                var outcome = o.Outcome;
                var b = new Button(() => Choose(id, outcome)) { name = "capture-option-" + outcome, text = text["capture.option." + outcome] };
                b.AddToClassList(EventCardPresenter.OptionClass); b.AddToClassList(OptionClass);
                options.Add(b);
                // Under the option, not in it: the button's label stays the choice itself, and a Label takes no Tab stop.
                var about = new Label(text["capture.about." + outcome]) { name = "capture-about-" + outcome };
                about.AddToClassList("capture-about");
                options.Add(about);
                var preview = new Label(PreviewText(c, s, o.Preview!, text)) { name = "capture-preview-" + outcome };
                preview.AddToClassList("muted"); preview.AddToClassList("capture-preview");
                options.Add(preview);
            }
            none.style.display = Offered.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            count.text = waiting.Count > 1 ? text.Format("event.more", waiting.Count - 1) : "";
            if (shown && focusedFor != id) FocusCard(id, force: false);
        }

        /// <summary>The preview's lines (Q-UI-2 (a)): a heading, then each quantity the option changes at once, in
        /// CapturePreview's order, in the turn report's form (report.line: the caption, then the change as the top bar shows
        /// it): Attention; Money, the net change of the Treasury less the debt, as the bar's "Money (net)"; Food; Arms; Social
        /// cohesion and International support as the difference of the two shown 0-100 values; the score. A change the bar
        /// would show as nothing is left out, as there; with none left, capture.changes_none. Every number is the helper's.</summary>
        public static string PreviewText(Content c, GameState s, CapturePreview p, UiText t)
        {
            var lines = new List<string>();
            void Line(string caption, string change) => lines.Add(t.Format("report.line", t[caption], change));
            if (p.Attention != 0) Line("capture.caption.attention", t.Format("capture.delta_attention", p.Attention));
            var money = Math.Round(p.Money - p.Debt, 1, MidpointRounding.AwayFromZero);
            if (money != 0) Line("hud.caption.treasury", PlayerText.MoneyChange(c, "treasury", money));
            if (p.Food != 0) Line("capture.caption.food", t.Format("capture.delta_count", p.Food));
            if (p.Arms != 0) Line("capture.caption.arms", t.Format("capture.delta_count", p.Arms));
            void Points(string caption, string indicator, double change)
            {
                var before = s.Indicators.TryGetValue(indicator, out var v) ? v : 0;
                if (PlayerText.ShownPoints(before + change) != PlayerText.ShownPoints(before)) Line(caption, PlayerText.PercentChangeShown(before, before + change));
            }
            Points("hud.caption.cohesion", "morale", p.SocialCohesion);
            Points("hud.caption.standing", "intl_standing", p.InternationalSupport);
            if (p.Score != 0) Line("capture.caption.score", t.Format("capture.delta_count", p.Score));
            return lines.Count == 0 ? t["capture.changes_none"] : t["capture.changes"] + "\n" + string.Join("\n", lines);
        }

        /// <summary>Hide: the dialog gives the map back and waits behind "Decision waiting: place", which takes the focus when
        /// Hide came from the keyboard. Nothing is chosen.</summary>
        public void Collapse()
        {
            if (Shown == null) return;
            collapsedFor = Shown;
            Render();
            if (hideByPointer) { hideByPointer = false; return; }
            reopen.Focus();
            // Tried again once styles resolve: the button was hidden a moment ago and may not take it yet.
            reopen.schedule.Execute(() => { if (Collapsed && reopen.focusController?.focusedElement != reopen) reopen.Focus(); });
        }

        /// <summary>Escape: Hide, when the dialog is open and in reach. True when it did.</summary>
        public bool Escape()
        {
            if (!Showing) return false;
            hideByPointer = false;
            Collapse();
            return true;
        }

        /// <summary>"Decision waiting: place": the dialog opens again and takes the focus.</summary>
        public void Reopen()
        {
            collapsedFor = null; focusedFor = null;
            Render();
        }

        /// <summary>A newly shown dialog puts the keyboard focus on itself (as the decision card, #41): the next Tab reaches its
        /// first option, and no option is lit before the player picks. Not while the layer is out of reach: the dialog takes
        /// the focus when the cover closes (onUncovered, <paramref name="force"/>). Tried again once styles resolve, from
        /// nothing or from End turn: the decision card hands the focus there when its last event is answered from the keyboard
        /// (#41 M1), which is the moment this dialog opens.</summary>
        void FocusCard(string id, bool force)
        {
            if (!layer.enabledInHierarchy) return;
            focusedFor = id;
            card.Focus();
            card.schedule.Execute(() =>
            {
                if (Shown != id || !Showing) { if (focusedFor == id && !layer.enabledInHierarchy) focusedFor = null; return; }
                var now = card.focusController?.focusedElement;
                if (card.panel != null && now != card && (force || now == null || now == endTurn)) card.Focus();
            });
        }

        void Choose(string id, string outcome)
        {
            // The bridge raises Refused (the toast shows it) or Changed (the dialog goes on to the next capture, which takes
            // the focus). Answered from the keyboard with no capture left, the focus goes to End turn (#41 M1).
            bridge.Queue(new ResolveCapture(id, outcome));
            if (Shown == null && presses.ByKeyboard) FocusReturn.To(endTurn);
        }

        public void Dispose()
        {
            if (eventCard != null) eventCard.Rendered -= onRender; else bridge.Changed -= onRender;
            hide.clicked -= onHide; reopen.clicked -= onReopen;
            hide.UnregisterCallback(onHidePointer, TrickleDown.TrickleDown); hide.UnregisterCallback(onHideSubmit, TrickleDown.TrickleDown);
            card.UnregisterCallback(onCancel);
            ScreenNav.NotOnUncovered(layer, onUncovered);
            presses.Dispose();
            options.Clear(); card.style.display = DisplayStyle.None;
            reopen.style.display = DisplayStyle.None;
            hudRoot.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);
        }
    }
}
