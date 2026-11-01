#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>One-way WebGL mirror of the presenter's words. It never receives game commands or changes focus.
    /// It publishes in the frame after a sim change, an End Turn, a refused order, a click, a submit or a focus move, once
    /// the presenters have rendered; periodic sampling also catches toast writes and controls rebuilt without any of those.
    /// Nothing is published before the first frame. Owned by GameRoot: a restart/language switch disposes it and clears the
    /// page before creating the next mirror.</summary>
    public sealed class WebCompanion : IDisposable
    {
        [Serializable]
        public sealed class Choice
        {
            public string text = "", note = "";
            public bool available;
        }

        [Serializable]
        public sealed class Snapshot
        {
            public string lang = "", heading = "", help = "", focusHeading = "", statusHeading = "", choicesHeading = "", empty = "", unavailable = "";
            public string title = "", focus = "", status = "", reportTitle = "";
            /// <summary>The game turn: the page announces the same status again when it comes on a new turn.</summary>
            public int turn;
            public string[] body = Array.Empty<string>();
            /// <summary>The open turn report's introduction, then one entry a row: its heading and its causes, one a line.</summary>
            public string[] report = Array.Empty<string>();
            public Choice[] choices = Array.Empty<Choice>();
        }

        readonly VisualElement root;
        readonly UiText text;
        readonly SimBridge? bridge;
        readonly Func<bool> hasEvent;
        readonly Action<string> send;
        readonly IVisualElementScheduledItem poll;
        readonly Action later;
        readonly Action<IReadOnlyList<Statecraft.Sim.Rejection>> onTurn;
        readonly Action<string> onRefused;
        readonly EventCallback<ClickEvent> onClick;
        readonly EventCallback<NavigationSubmitEvent> onSubmit;
        readonly EventCallback<FocusInEvent> onFocus;
        IVisualElementScheduledItem? next;
        string? previous;
        bool disposed;

        /// <param name="bridge">The game's bridge, for the turn and its events; null in tests of a bare tree.</param>
        public WebCompanion(VisualElement root, UiText text, SimBridge? bridge, Func<bool> hasEvent, Action<string>? send = null)
        {
            this.root = root; this.text = text; this.bridge = bridge; this.hasEvent = hasEvent; this.send = send ?? Publish;
            // One publication in the next frame however many notifications arrive in this one (as UiDirection schedules).
            later = () => { if (disposed) return; next?.Pause(); next = root.schedule.Execute(Refresh); };
            onTurn = _ => later(); onRefused = _ => later();
            onClick = _ => later(); onSubmit = _ => later(); onFocus = _ => later();
            if (bridge != null) { bridge.Changed += later; bridge.TurnEnded += onTurn; bridge.Refused += onRefused; }
            root.RegisterCallback(onClick, TrickleDown.TrickleDown);
            root.RegisterCallback(onSubmit, TrickleDown.TrickleDown);
            root.RegisterCallback(onFocus, TrickleDown.TrickleDown);
            poll = root.schedule.Execute(Refresh).Every(100);
            later();
        }

        /// <summary>Copies rendered/localised strings, not event IDs or an independently formatted sim projection.
        /// A collapsed/covered current event stays readable; hasEvent prevents the presenter's retained labels from
        /// leaking the previous event after the final answer. Disabled choices remain ordinary readable list items.</summary>
        internal Snapshot Capture(VisualElement? focused = null)
        {
            var snapshot = new Snapshot
            {
                lang = text["ui.lang"], heading = text["companion.title"], help = text["companion.help"],
                focusHeading = text["companion.focus"], statusHeading = text["companion.status"],
                choicesHeading = text["companion.choices"], empty = text["companion.empty"], unavailable = text["companion.unavailable"],
                turn = bridge?.State.Turn ?? 0,
            };
            var toast = root.Q<Label>("toast");
            var status = new List<string>();
            if (Visible(toast) && toast!.text.Length > 0) status.Add(toast.text);
            // The End Turn result, whether or not a decision waits: the status line names the open report, and the report
            // section carries its rows as the panel shows them.
            var report = root.Q(TurnReportPresenter.ElementName);
            if (Visible(report))
            {
                snapshot.reportTitle = Words("turn-report-title");
                if (snapshot.reportTitle.Length > 0) status.Add(snapshot.reportTitle);
                var lines = new List<string>();
                var intro = Words("turn-report-intro");
                if (intro.Length > 0) lines.Add(intro);
                var rows = report!.Q<ScrollView>("turn-report-rows");
                if (rows != null)
                    foreach (var row in rows.Children())
                    {
                        var words = new List<string>();
                        row.Query<Label>().ForEach(l => { if (l.text.Length > 0) words.Add(l.text); });
                        if (words.Count > 0) lines.Add(string.Join("\n", words));
                    }
                snapshot.report = lines.ToArray();
            }
            snapshot.status = string.Join("\n", status);
            focused ??= root.focusController?.focusedElement as VisualElement;
            if (focused != null && root.Contains(focused) && Visible(focused))
            {
                if (focused == root.Q("map-focus")) snapshot.focus = text["companion.map"];
                else if (focused.name == "event-card" && hasEvent()) snapshot.focus = Words("event-title");
                else if (!string.IsNullOrEmpty(focused.tooltip)) snapshot.focus = focused.tooltip;
                else if (focused is TextElement label) snapshot.focus = label.text;
                else
                {
                    var labels = new List<string>();
                    focused.Query<Label>().ForEach(l => { if (Visible(l) && l.text.Length > 0) labels.Add(l.text); });
                    snapshot.focus = string.Join("\n", labels);
                }
            }
            if (!hasEvent()) return snapshot;
            snapshot.title = Words("event-title");
            var body = new List<string>();
            foreach (var name in new[] { "event-draft", "event-office", "event-alt-banner", "event-text", "event-objective", "event-delegate", "event-count" })
            {
                var label = root.Q<Label>(name);
                // Test only the label itself: collapsing the event must not erase its text from the companion.
                if (label != null && label.style.display.value != DisplayStyle.None && label.text.Length > 0) body.Add(label.text);
            }
            snapshot.body = body.ToArray();
            var choices = new List<Choice>();
            var options = root.Q("event-options");
            if (options != null)
                foreach (var child in options.Children())
                {
                    if (child is Button b) choices.Add(new Choice { text = b.text, available = b.enabledInHierarchy });
                    else if (child is Label note && note.ClassListContains(EventCardPresenter.RejoinClass) && choices.Count > 0)
                        choices[choices.Count - 1].note = note.text;
                }
            snapshot.choices = choices.ToArray();
            return snapshot;
        }

        string Words(string name) => root.Q<Label>(name)?.text ?? "";

        static bool Visible(VisualElement? element)
        {
            if (element == null) return false;
            for (var e = element; e != null; e = e.parent)
                if (e.style.display.value == DisplayStyle.None || e.resolvedStyle.display == DisplayStyle.None ||
                    e.style.visibility.value == Visibility.Hidden || e.resolvedStyle.visibility == Visibility.Hidden) return false;
            return true;
        }

        internal void Refresh()
        {
            if (disposed) return;
            var json = JsonUtility.ToJson(Capture());
            if (json == previous) return;
            send(json);
            previous = json;   // only once it has gone out: a publication that threw is tried again
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            poll.Pause(); next?.Pause(); next = null;
            if (bridge != null) { bridge.Changed -= later; bridge.TurnEnded -= onTurn; bridge.Refused -= onRefused; }
            root.UnregisterCallback(onClick, TrickleDown.TrickleDown);
            root.UnregisterCallback(onSubmit, TrickleDown.TrickleDown);
            root.UnregisterCallback(onFocus, TrickleDown.TrickleDown);
            // Last, and never thrown on: GameRoot disposes its owned presenters in one loop, and a page or test transport
            // that throws here must not leave the rest of them undisposed.
            try { send("null"); }
            catch (Exception e) { Debug.LogException(e); }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void StatecraftCompanion(string json);
#endif
        static void Publish(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StatecraftCompanion(json);
#endif
        }
    }
}
