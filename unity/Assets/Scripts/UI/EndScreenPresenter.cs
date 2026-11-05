#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The end screen (GDD Phase 7 PR 9, #812; D-H4): once the game is over it shows the sim's end report
    /// (EndReport.For, #810) and nothing else: the outcome, the score, each objective's card with the player's date beside the
    /// historical date, any Deport card whose Deport came on the last turn (R-EVT-007), the two national parameters and the
    /// population beside its CBS anchor. It formats the report only; no number is computed here. It shows when a turn ends
    /// the game and when an ended game is loaded or rebuilt in the other language; Close leaves the map to inspect (End Turn
    /// stays disabled and SimBridge refuses orders once the game is over),
    /// and New Game is the existing two presses (NewGamePresenter.Press), so showing the screen never deletes the save.
    /// <para>While it shows, the root has <see cref="OpenClass"/>: ScreenNav.DisableCovered covers the HUD under it as under
    /// the turn report, and the screen takes the keyboard focus when it is uncovered.</para></summary>
    public sealed class EndScreenPresenter : IDisposable
    {
        /// <summary>On the "root" element while the end screen shows.</summary>
        public const string OpenClass = "end-screen-open";
        public const string ElementName = "end-screen";

        readonly VisualElement hudRoot, screen; readonly ScrollView rows; readonly SimBridge bridge; readonly UiText text;
        readonly Button close, newGame; readonly Action? pressNewGame;
        readonly Action<IReadOnlyList<Rejection>> onTurn; readonly Action onChanged, onCardsSeen;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        int closedAt = -1;

        /// <param name="pressNewGame">NewGamePresenter.Press; null in a smoke run, which has no New Game (GameRoot).</param>
        /// <param name="show">False keeps an ended game's screen closed: the language switch rebuilds the UI over a game whose
        /// end screen the player had closed (GameRoot.SwitchLanguage).</param>
        public EndScreenPresenter(VisualElement root, SimBridge bridge, UiText text, Action? pressNewGame, bool show = true)
        {
            this.bridge = bridge; this.text = text; this.pressNewGame = pressNewGame;
            hudRoot = root.Q("root") ?? root;
            screen = root.Q<VisualElement>(ElementName);
            rows = screen.Q<ScrollView>("end-screen-rows");
            screen.Q<Label>("end-screen-title").text = text["end.title"];
            close = screen.Q<Button>("end-screen-close"); close.text = text["report.close"];
            newGame = screen.Q<Button>("end-screen-new-game"); newGame.text = text["hud.new_game"];
            newGame.style.display = pressNewGame == null ? DisplayStyle.None : DisplayStyle.Flex;
            close.clicked += OnClose;
            newGame.clicked += OnNewGame;
            onCancel = e => { Close(); e.StopPropagation(); };
            screen.RegisterCallback(onCancel);
            ScreenNav.OnUncovered(screen, () =>
            {
                close.Focus();
                close.schedule.Execute(() => { if (IsOpen && screen.enabledSelf && screen.focusController?.focusedElement != close) close.Focus(); });
            });
            // Guarded as TurnReportPresenter's handler is: TurnEnded's later handlers include the turn's autosave.
            onTurn = _ =>
            {
                // #2536: an objective's card raised on the ending turn shows first; the screen follows its answer.
                try { if (bridge.State.GameOver != null && !bridge.EndingCardWaiting) Show(); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); Hide(); }
            };
            bridge.TurnEnded += onTurn;
            onCardsSeen = () =>
            {
                try { Show(); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); Hide(); }
            };
            bridge.EndingCardsSeen += onCardsSeen;
            onChanged = () => { if (bridge.State.GameOver == null) Hide(); };   // a new game (New Game's second press) is not over
            bridge.Changed += onChanged;
            // A loaded or rebuilt ended game opens on its end screen, after any card of its ending turn still waiting (#2536).
            if (bridge.State.GameOver != null && show && !bridge.EndingCardWaiting) Show(); else Hide();
        }

        /// <summary>True while the end screen shows.</summary>
        public bool IsOpen => hudRoot.ClassListContains(OpenClass);

        /// <summary>True in the frame the screen closed (GameRoot's Escape poll, as TurnReportPresenter.ClosedThisFrame).</summary>
        public bool ClosedThisFrame => closedAt == UnityEngine.Time.frameCount;

        /// <summary>The screen's lines for an ended game, in order: the outcome, its history note when it has one, the score,
        /// the reference score, one per objective, each Deport card that never fired (its title, then its text), Social
        /// cohesion, International Support, the population with its CBS anchor and, when some went home, the prisoners
        /// returned. Every value is the report's.</summary>
        public static IReadOnlyList<(string Name, string Text)> Lines(Content c, GameState s, UiText t)
        {
            var r = EndReport.For(c, s);
            // A Hebrew date goes between RLE and PDF (#2060): left bare in a line laid out left to right, the bidi rules pull
            // its day onto the next word and its year to the line's far end. Not RLI/PDI: UI Toolkit's advanced text generator
            // still drew the date in that order with the isolates (measured on 6000.3), the embedding keeps it whole.
            // English dates are left as they are.
            string Date(DateTime d)
            {
                var value = t.Format("date.day_month_year", d.Day, t["date.month_" + d.Month], d.Year);
                return UiDirection.HasRightToLeft(value) ? "\u202B" + value + "\u202C" : value;
            }
            // The GDD loop ends only as the MVP end or a defeat (R-END-002, R-END-005); the legacy codes get the generic line.
            var outcome = "end.outcome." + r.Outcome;
            var lines = new List<(string, string)>
            {
                ("outcome", t.Has(outcome) ? t[outcome] : t["order.game_over"]),
            };
            // The sourced history note under the cause's line (Q-DEF-4 (b)); only a cause whose note is in the strings has one,
            // and only under that cause's own line, never under the generic one.
            var note = "end.note." + r.Outcome;
            if (t.Has(outcome) && t.Has(note)) lines.Add(("note", t[note]));
            lines.Add(("score", t.Format("hud.score", r.Score)));
            // Q-SCORE-2: beside the score, what a run on the historical dates scores (the sim's number; nothing computed here).
            if (r.ReferenceScore > 0) lines.Add(("score-reference", t.Format("end.reference_score", r.ReferenceScore)));
            foreach (var o in r.Objectives)
            {
                var card = c.Events.First(e => e.Id == o.Card);
                var variant = Timeline.Variant(c, s, card);
                // As the objective's card shows it (EventCardPresenter), in the UI's language (#2041): the Hebrew title_he
                // where there is no variant title (a variant has no Hebrew), the English otherwise.
                var title = ContentNames.Fill(t, c, s, s.Date, EventSystem.Title(c, s, card), variant == null || variant.Title == "" ? card.TitleHe : "");
                lines.Add(("objective-" + o.Id, o.Completed is DateTime done
                    ? t.Format("end.objective_done", title, Date(done), Date(o.Historical))
                    : t.Format("end.objective_open", title, Date(o.Historical))));
            }
            // R-EVT-007: a Deport card that never fired, its Deport on the turn that ended the game (owner, 2 November 2026,
            // #2197: "Show on the end screen (Recommended)"), shown as its card shows it, title then text, in the UI's language.
            foreach (var id in r.DeportCards)
            {
                var card = c.Events.First(e => e.Id == id);
                lines.Add(("deport-" + id, ContentNames.Fill(t, c, s, s.Date, EventSystem.Title(c, s, card), card.TitleHe)));
                lines.Add(("deport-" + id + "-text", ContentNames.Fill(t, c, s, s.Date, EventSystem.Render(c, s, card), card.TextHe)));
            }
            lines.Add(("cohesion", t.Format("end.parameter", t["hud.caption.cohesion"], PlayerText.Percent(r.SocialCohesion))));
            lines.Add(("standing", t.Format("end.parameter", t["hud.caption.standing"], PlayerText.Percent(r.InternationalSupport))));
            string People(double n) => n.ToString("N0", CultureInfo.InvariantCulture);
            lines.Add(("population", r.PopulationHistory is PopulationAnchor a
                ? t.Format("end.population_history", People(r.Population), Date(a.Date), People(a.Jews))
                : t.Format("end.population", People(r.Population))));
            if (r.PrisonersReturned > 0) lines.Add(("prisoners", t.Format("defence.prisoners_returned", r.PrisonersReturned)));   // Q-END-3; Defence's own string
            return lines;
        }

        void Show()
        {
            rows.Clear();
            foreach (var (name, line) in Lines(bridge.Content, bridge.State, text))
            {
                var label = new Label(line) { name = "end-screen-" + name };
                label.AddToClassList(name == "outcome" ? "end-screen-outcome" : name == "note" ? "end-screen-note" : "end-screen-line");
                rows.Add(label);
            }
            screen.style.display = DisplayStyle.Flex;
            hudRoot.AddToClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);   // uncovered (no start screen over it), it takes the focus
        }

        void OnClose() => Close();

        /// <summary>Closes the screen, leaving the map to inspect; false when it was not open (GameRoot's Escape chain).</summary>
        public bool Close()
        {
            if (!IsOpen) return false;
            Hide();
            return true;
        }

        void OnNewGame() => pressNewGame?.Invoke();

        void Hide()
        {
            if (IsOpen) closedAt = UnityEngine.Time.frameCount;
            screen.style.display = DisplayStyle.None;
            hudRoot.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);
        }

        public void Dispose()
        {
            bridge.TurnEnded -= onTurn;
            bridge.EndingCardsSeen -= onCardsSeen;
            bridge.Changed -= onChanged;
            close.clicked -= OnClose;
            newGame.clicked -= OnNewGame;
            screen.UnregisterCallback(onCancel);
            ScreenNav.OnUncovered(screen, null);
            Hide();
        }
    }
}
