#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The turn report (#1037; the first outside play-test, #1038: "I see the values changing, but I don't really
    /// understand what's happening"). When a turn has run, a short panel names each top-bar number that changed, with the
    /// change the bar shows (HudModel.From) and its main causes, the sim's own causes in the player's language
    /// (StepResult.Why, through HudModel.Causes and CauseText, B1). "See all" opens the Why screen with every cause; Close, Esc or Cancel
    /// closes it. Nothing is computed or reworded here, so no rule lives in the UI.
    /// <para>While it shows, the root has <see cref="OpenClass"/>: ScreenNav.DisableCovered disables the top bar, the panels
    /// and the decision layer under it, as under an End Turn outcome clip, which is drawn over the report (Game.uxml order)
    /// and disables it in turn; the report takes the keyboard focus when it is uncovered. A turn the sim refused outright
    /// (SimBridge.EndTurn's failure: Session.Last does not move) shows no report; a turn with a refused order does.</para></summary>
    public sealed class TurnReportPresenter : IDisposable
    {
        /// <summary>On the "root" element while the report shows.</summary>
        public const string OpenClass = "turn-report-open";
        public const string ElementName = "turn-report";
        /// <summary>The id of the row after the meters' that lists the enemy's orders this turn (#1293).</summary>
        public const string EnemyRow = "enemy";

        /// <summary>The units_1948.yaml rows, which name the enemy's units in that row (GameRoot sets them once the map has read
        /// them); without a row a unit is named by its id (report.unit).</summary>
        public IReadOnlyList<UnitEntry>? Units { get; set; }

        readonly VisualElement hudRoot, report; readonly ScrollView rows; readonly SimBridge bridge; readonly UiText text;
        readonly ScreenNav nav; readonly Button close, all;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        /// <summary>The turn last reported (or the session's last turn when this was made: a loaded game reports nothing).</summary>
        StepResult? seen;
        int closedAt = -1;

        public TurnReportPresenter(VisualElement root, SimBridge bridge, UiText text, ScreenNav nav)
        {
            this.bridge = bridge; this.text = text; this.nav = nav;
            hudRoot = root.Q("root") ?? root;
            report = root.Q<VisualElement>(ElementName);
            rows = report.Q<ScrollView>("turn-report-rows");
            report.Q<Label>("turn-report-title").text = text["report.title"];
            report.Q<Label>("turn-report-intro").text = text["report.intro"];
            close = report.Q<Button>("turn-report-close"); close.text = text["report.close"];
            all = report.Q<Button>("turn-report-all"); all.text = text["report.all"];
            close.clicked += OnClose;
            all.clicked += SeeAll;
            onCancel = e => { Close(); e.StopPropagation(); };
            report.RegisterCallback(onCancel);
            // Close takes the focus when the report comes out uncovered; again once styles resolve, as an element shown this
            // frame may not take it yet (OutcomePresenter's layer does the same).
            ScreenNav.OnUncovered(report, () =>
            {
                close.Focus();
                close.schedule.Execute(() => { if (IsOpen && report.enabledSelf && report.focusController?.focusedElement != close) close.Focus(); });
            });
            seen = bridge.Session.Last;
            onTurn = _ =>
            {
                // Guarded as a whole, as OutcomePresenter is: nothing here may throw out of TurnEnded, whose later handlers
                // include the turn's autosave.
                try
                {
                    var last = bridge.Session.Last;
                    if (last == null || ReferenceEquals(last, seen)) return;
                    seen = last;
                    Show();
                }
                catch (Exception e) { UnityEngine.Debug.LogException(e); Hide(); }
            };
            bridge.TurnEnded += onTurn;
            Hide();
        }

        /// <summary>True while the report shows.</summary>
        public bool IsOpen => hudRoot.ClassListContains(OpenClass);

        /// <summary>True in the frame the report closed: GameRoot's Escape poll then leaves the card alone (the same Escape
        /// key already closed the report through the Cancel event), as for OutcomePresenter.ClosedThisFrame.</summary>
        public bool ClosedThisFrame => closedAt == UnityEngine.Time.frameCount;

        /// <summary>One line of the report: the meter's id, its caption with the change the bar shows, and its main causes.</summary>
        public readonly struct Line
        {
            public readonly string Meter, Heading; public readonly IReadOnlyList<string> Causes;
            public Line(string meter, string heading, IReadOnlyList<string> causes) { Meter = meter; Heading = heading; Causes = causes; }
        }

        /// <summary>The report's lines for the session's last turn: each top-bar meter whose shown change is not empty, in
        /// the bar's order (the meters have different units, so no order by size is given without a rule); then, under
        /// <see cref="EnemyRow"/>, the enemy's orders (#1293).</summary>
        public static IReadOnlyList<Line> Lines(SimBridge bridge, UiText text, IReadOnlyList<UnitEntry>? units = null)
        {
            var m = HudModel.From(bridge, text); var lines = new List<Line>();
            foreach (var id in HudModel.MeterIds)
            {
                if (id == "cohesion" && !m.ShowsCohesion) continue;
                var meter = m.For(id);
                if (meter.Delta.Length == 0) continue;
                lines.Add(new Line(id, text.Format("report.line", text["hud.caption." + id], meter.Delta), HudModel.Causes(bridge.Session.Last, id,
                    w => CauseText.Line(w, bridge.Content, units ?? Array.Empty<UnitEntry>(), text))));
            }
            var enemy = EnemyOrders(bridge, text, units);
            if (enemy.Count > 0) lines.Add(new Line(EnemyRow, text["report.enemy"], enemy));
            return lines;
        }

        /// <summary>#1293: every order the enemy AI gave in the last turn, and every attack its units made, a standing one too
        /// (#1297), as the sim names it on its cause (Cause.EnemyOrder, R-ENEMY-001), in the sim's order, in the player's
        /// language: Message is English only. The unit is named by its row
        /// (unit i of a new game is StartingForces[i], R-UNIT-006), in Hebrew by its name_he where it has one.</summary>
        static IReadOnlyList<string> EnemyOrders(SimBridge bridge, UiText text, IReadOnlyList<UnitEntry>? units)
        {
            var turn = bridge.Session.Last;
            if (turn == null) return Array.Empty<string>();
            string Name(Cause cause, int id)
            {
                var snapshot = cause.Units.FirstOrDefault(u => u.Id == id);
                if (snapshot != null) return UnitNames.Of(snapshot, bridge.Content, units ?? Array.Empty<UnitEntry>(), text);
                return text.Format("report.unit", id);
            }
            // An engagement (#1297) names its enemy attackers, one line each, and gives no line if it names none: its Target is
            // a hex, not a unit. An order names its unit, Target.
            return turn.Why.Where(w => w.EnemyOrder != "")
                .SelectMany(w => (w.EnemyOrder == Combat.Attack ? w.EnemyAttackers : new[] { int.Parse(w.Target, CultureInfo.InvariantCulture) })
                    .Select(id => text.Format("report.enemy_" + w.EnemyOrder, Name(w, id)))).ToList();
        }

        void Show()
        {
            rows.Clear();
            foreach (var line in Lines(bridge, text, Units))
            {
                var row = new VisualElement { name = "turn-report-" + line.Meter };
                row.AddToClassList("turn-report-row");
                var heading = new Label(line.Heading) { name = "turn-report-heading-" + line.Meter };
                heading.AddToClassList("turn-report-heading");
                row.Add(heading);
                if (line.Causes.Count > 0)
                {
                    // One label a cause, so each takes its own direction (UiDirection): in Hebrew a line with a key (B1) is
                    // right to left and a line still in English left to right.
                    var causes = new VisualElement { name = "turn-report-causes-" + line.Meter };
                    causes.AddToClassList("turn-report-causes");
                    foreach (var cause in line.Causes)
                    {
                        var label = new Label(cause);
                        label.AddToClassList("turn-report-cause");
                        causes.Add(label);
                    }
                    row.Add(causes);
                }
                rows.Add(row);
            }
            if (rows.childCount == 0) { Hide(); return; }   // nothing in the bar moved: no report to dismiss
            nav.Close();
            report.style.display = DisplayStyle.Flex;
            hudRoot.AddToClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);   // the report comes out uncovered (or under an outcome clip) and takes the focus then
        }

        void OnClose() => Close();

        /// <summary>Closes the report; false when it was not open (GameRoot's Escape chain).</summary>
        public bool Close()
        {
            if (!IsOpen) return false;
            Hide();
            return true;
        }

        void Hide()
        {
            if (IsOpen) closedAt = UnityEngine.Time.frameCount;
            report.style.display = DisplayStyle.None;
            hudRoot.RemoveFromClassList(OpenClass);
            ScreenNav.DisableCovered(hudRoot);   // the decision layer comes back: an open card takes the focus (OnUncovered)
        }

        void SeeAll()
        {
            Hide();
            nav.Open("why");
        }

        public void Dispose()
        {
            bridge.TurnEnded -= onTurn;
            close.clicked -= OnClose;
            all.clicked -= SeeAll;
            report.UnregisterCallback(onCancel);
            ScreenNav.OnUncovered(report, null);
            Hide();
        }
    }
}
