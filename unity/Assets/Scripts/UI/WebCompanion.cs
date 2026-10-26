#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>One-way WebGL mirror of the presenter's words. It never receives game commands or changes focus.
    /// Periodic sampling also catches toast writes, focus moves and controls rebuilt without a sim change.
    /// Owned by GameRoot: a restart/language switch disposes it and clears the page before creating the next mirror.</summary>
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
            public string title = "", focus = "", status = "";
            public string[] body = Array.Empty<string>();
            public Choice[] choices = Array.Empty<Choice>();
        }

        readonly VisualElement root;
        readonly UiText text;
        readonly Func<bool> hasEvent;
        readonly Action<string> send;
        readonly IVisualElementScheduledItem pending;
        string? previous;
        bool disposed;

        public WebCompanion(VisualElement root, UiText text, Func<bool> hasEvent, Action<string>? send = null)
        {
            this.root = root; this.text = text; this.hasEvent = hasEvent; this.send = send ?? Publish;
            pending = root.schedule.Execute(Refresh).Every(100);
            Refresh();
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
            };
            var toast = root.Q<Label>("toast");
            snapshot.status = Visible(toast) ? toast!.text : "";
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
                    if (child is Button b) choices.Add(new Choice { text = b.text, available = b.enabledSelf });
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
            previous = json;
            send(json);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            pending.Pause();
            send("null");
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
