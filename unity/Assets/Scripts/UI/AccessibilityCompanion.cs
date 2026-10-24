#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>#1119 B: a read-only snapshot of presented text. Commands and localization stay with the existing UI.
    /// One scheduled read after UI handlers, with no presenter subscriptions to maintain or browser action receiver.</summary>
    public sealed class AccessibilityCompanion : IDisposable
    {
        [Serializable] public sealed class Choice
        {
            public string text = "";
            public bool enabled;
            public string[] notes = Array.Empty<string>();
        }
        [Serializable] public sealed class Snapshot
        {
            public string language = "en", screen = "", focus = "", title = "", body = "", status = "";
            public int turn;
            public string[] context = Array.Empty<string>();
            public Choice[] choices = Array.Empty<Choice>();
        }

        readonly VisualElement root;
        readonly SimBridge bridge;
        readonly string language;
        readonly Action<string>? sink;
        readonly Action later;
        readonly Action<string> onRefused;
        readonly Action<IReadOnlyList<Rejection>> onTurn;
        readonly EventCallback<ClickEvent> onClick;
        readonly EventCallback<NavigationSubmitEvent> onSubmit;
        readonly EventCallback<NavigationCancelEvent> onCancel;
        readonly EventCallback<FocusInEvent> onFocus;
        readonly EventCallback<FocusOutEvent> onBlur;
        readonly EventCallback<KeyUpEvent> onKey;
        readonly EventCallback<GeometryChangedEvent> onGeometry;
        readonly EventCallback<ChangeEvent<string>> onText;
        IVisualElementScheduledItem? pending;
        bool disposed;
        string? lastJson;

        /// <param name="push">EditMode seam: receives the exact serialized payload; null uses the guarded WebGL transport.</param>
        public AccessibilityCompanion(VisualElement root, SimBridge bridge, UiText text, Action<string>? push = null)
        {
            this.root = root; this.bridge = bridge; sink = push;
            language = text["ui.lang"] == UiLanguage.Hebrew ? "he" : "en";
            later = Schedule; onRefused = _ => Schedule(); onTurn = _ => Schedule();
            onClick = _ => Schedule(); onSubmit = _ => Schedule(); onCancel = _ => Schedule();
            onFocus = _ => Schedule(); onBlur = _ => Schedule(); onKey = _ => Schedule();
            onGeometry = _ => Schedule(); // root resizing only: GeometryChangedEvent does not propagate from descendants
            onText = e => { if (e.target is TextElement) Schedule(); }; // late media/save notices, without a bridge event
            root.RegisterCallback(onClick, TrickleDown.TrickleDown);
            root.RegisterCallback(onSubmit, TrickleDown.TrickleDown);
            root.RegisterCallback(onCancel, TrickleDown.TrickleDown);
            root.RegisterCallback(onFocus, TrickleDown.TrickleDown);
            root.RegisterCallback(onBlur, TrickleDown.TrickleDown);
            root.RegisterCallback(onKey, TrickleDown.TrickleDown);
            root.RegisterCallback(onGeometry, TrickleDown.TrickleDown);
            root.RegisterCallback(onText, TrickleDown.TrickleDown);
            bridge.Changed += later; bridge.Refused += onRefused; bridge.TurnEnded += onTurn;
            Schedule(); // first frame, never a synchronous boot-path read
        }

        void Schedule()
        {
            if (!disposed && pending == null) pending = root.schedule.Execute(AfterLayout);
        }

        void AfterLayout()
        {
            // Scheduling during a scheduler tick queues the read for the next UI tick, after this tick's style/layout
            // pass. A single Execute can run before the start screen's new visibility has resolved.
            pending = disposed ? null : root.schedule.Execute(Publish);
        }

        void Publish()
        {
            pending = null;
            if (disposed) return;
            var json = JsonUtility.ToJson(Read());
            if (json == lastJson) return;
            if (Send(json)) lastJson = json;
            else Schedule(); // the template mounts only after createUnityInstance resolves
        }

        bool Send(string json)
        {
            if (sink != null) { sink(json); return true; }
            return DefaultPush(json);
        }

        Snapshot Read()
        {
            var p = new Snapshot { language = language, turn = bridge.State.Turn };
            var card = root.Q("event-card");
            if (Visible(card) && card.enabledInHierarchy)
            {
                p.title = Text("event-title"); p.body = Text("event-text");
                p.context = new[] { "event-office", "event-alt-banner", "event-objective", "event-delegate", "event-count" }
                    .Select(Text).Where(s => s.Length > 0).ToArray();
                var choices = new List<Choice>();
                var options = root.Q("event-options");
                if (options != null)
                    foreach (var element in options.Children())
                    {
                        if (!Visible(element)) continue;
                        if (element is Button b)
                        {
                            var notes = new List<string>();
                            foreach (var child in b.Children()) notes.AddRange(Words(child));
                            choices.Add(new Choice { text = b.text, enabled = b.enabledInHierarchy, notes = notes.ToArray() });
                        }
                        else if (choices.Count > 0)
                            choices[choices.Count - 1].notes = choices[choices.Count - 1].notes.Concat(Words(element)).ToArray();
                    }
                p.choices = choices.ToArray();
            }
            var focus = root.panel?.focusController.focusedElement as VisualElement;
            if (focus != null && Visible(focus))
                p.focus = focus == card ? p.title : focus is TextElement t ? t.text
                    : !string.IsNullOrEmpty(focus.tooltip) ? focus.tooltip : string.Join("\n", Words(focus));
            var status = new List<string>();
            var toast = Text("toast"); if (toast.Length > 0) status.Add(toast);
            var report = root.Q(TurnReportPresenter.ElementName);
            if (Visible(report) && report.enabledInHierarchy)
            {
                p.screen = Text("turn-report-title");
                status.AddRange(Words(report).Where(s => s != Text("turn-report-close") && s != Text("turn-report-all")));
            }
            p.status = string.Join("\n", status);
            return p;
        }

        string Text(string name)
        {
            var t = root.Q<TextElement>(name);
            return Visible(t) ? t.text : "";
        }

        static IEnumerable<string> Words(VisualElement element)
        {
            if (!Visible(element)) yield break;
            if (element is TextElement t && t.text.Length > 0) yield return t.text;
            foreach (var child in element.Children()) foreach (var word in Words(child)) yield return word;
        }

        static bool Visible(VisualElement? element)
        {
            if (element == null) return false;
            for (var e = element; e != null; e = e.parent)
                if (e.style.display.value == DisplayStyle.None || e.resolvedStyle.display == DisplayStyle.None ||
                    e.resolvedStyle.visibility == Visibility.Hidden) return false;
            return true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true; pending?.Pause(); pending = null;
            bridge.Changed -= later; bridge.Refused -= onRefused; bridge.TurnEnded -= onTurn;
            root.UnregisterCallback(onClick, TrickleDown.TrickleDown);
            root.UnregisterCallback(onSubmit, TrickleDown.TrickleDown);
            root.UnregisterCallback(onCancel, TrickleDown.TrickleDown);
            root.UnregisterCallback(onFocus, TrickleDown.TrickleDown);
            root.UnregisterCallback(onBlur, TrickleDown.TrickleDown);
            root.UnregisterCallback(onKey, TrickleDown.TrickleDown);
            root.UnregisterCallback(onGeometry, TrickleDown.TrickleDown);
            root.UnregisterCallback(onText, TrickleDown.TrickleDown);
            Send(JsonUtility.ToJson(new Snapshot { language = language }));
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")] static extern int StatecraftCompanionPush(string json);
        static bool DefaultPush(string json) => StatecraftCompanionPush(json) == 1;
#else
        static bool DefaultPush(string json) => true;
#endif
    }
}
