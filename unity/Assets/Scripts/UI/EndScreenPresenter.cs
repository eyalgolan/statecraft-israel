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
    /// historical date, the two national parameters and the population beside its CBS anchor. It formats the report only; no
    /// number is computed here. It shows when a turn ends the game and when an ended game is loaded or rebuilt in the other
    /// language; Close leaves the map to inspect (End Turn stays disabled and SimBridge refuses orders once the game is over),
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
        readonly Action<IReadOnlyList<Rejection>> onTurn; readonly Action onChanged;
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
                try { if (bridge.State.GameOver != null) Show(); }
                catch (Exception e) { UnityEngine.Debug.LogException(e); Hide(); }
            };
            bridge.TurnEnded += onTurn;
            onChanged = () => { if (bridge.State.GameOver == null) Hide(); };   // a new game (New Game's second press) is not over
            bridge.Changed += onChanged;
            if (bridge.State.GameOver != null && show) Show(); else Hide();   // a loaded or rebuilt ended game opens on its end screen
        }

        /// <summary>True while the end screen shows.</summary>
        public bool IsOpen => hudRoot.ClassListContains(OpenClass);

        /// <summary>True in the frame the screen closed (GameRoot's Escape poll, as TurnReportPresenter.ClosedThisFrame).</summary>
        public bool ClosedThisFrame => closedAt == UnityEngine.Time.frameCount;

        /// <summary>The screen's lines for an ended game, in order: the outcome, the score, one per objective, Social cohesion,
        /// International Support and the population with its CBS anchor. Every value is the report's.</summary>
        public static IReadOnlyList<(string Name, string Text)> Lines(Content c, GameState s, UiText t)
        {
            var r = EndReport.For(c, s);
            string Date(DateTime d) => t.Format("date.day_month_year", d.Day, t["date.month_" + d.Month], d.Year);
            // The GDD loop ends only as the MVP end or a defeat (R-END-002, R-END-005); the legacy codes get the generic line.
            var outcome = "end.outcome." + r.Outcome;
            var lines = new List<(string, string)>
            {
                ("outcome", t.Has(outcome) ? t[outcome] : t["order.game_over"]),
                ("score", t.Format("hud.score", r.Score)),
            };
            foreach (var o in r.Objectives)
            {
                var title = EventSystem.Title(c, s, c.Events.First(e => e.Id == o.Card));   // as the objective's card shows it
                lines.Add(("objective-" + o.Id, o.Completed is DateTime done
                    ? t.Format("end.objective_done", title, Date(done), Date(o.Historical))
                    : t.Format("end.objective_open", title, Date(o.Historical))));
            }
            lines.Add(("cohesion", t.Format("end.parameter", t["hud.caption.cohesion"], PlayerText.Percent(r.SocialCohesion))));
            lines.Add(("standing", t.Format("end.parameter", t["hud.caption.standing"], PlayerText.Percent(r.InternationalSupport))));
            string People(double n) => n.ToString("N0", CultureInfo.InvariantCulture);
            lines.Add(("population", r.PopulationHistory is PopulationAnchor a
                ? t.Format("end.population_history", People(r.Population), Date(a.Date), People(a.Jews))
                : t.Format("end.population", People(r.Population))));
            return lines;
        }

        void Show()
        {
            rows.Clear();
            foreach (var (name, line) in Lines(bridge.Content, bridge.State, text))
            {
                var label = new Label(line) { name = "end-screen-" + name };
                label.AddToClassList(name == "outcome" ? "end-screen-outcome" : "end-screen-line");
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
            bridge.Changed -= onChanged;
            close.clicked -= OnClose;
            newGame.clicked -= OnNewGame;
            screen.UnregisterCallback(onCancel);
            ScreenNav.OnUncovered(screen, null);
            Hide();
        }
    }
}
