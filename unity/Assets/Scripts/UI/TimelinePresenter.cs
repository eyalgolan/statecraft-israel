#nullable enable
using System.Linq;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The resolved course beside the record (#955). All ordering, divergence and rejoin answers come from the sim.</summary>
    public sealed class TimelinePresenter : System.IDisposable
    {
        readonly VisualElement screen;
        readonly ScrollView list;
        readonly SimBridge bridge;
        readonly UiText text;
        GameSession? shownSession;
        GameState? shownState;

        public TimelinePresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            this.bridge = bridge; this.text = text;
            screen = root.Q<VisualElement>("screen-timeline");
            list = screen.Q<ScrollView>("timeline-list");
            screen.Q<Label>("timeline-screen-title").text = text["timeline.title"];
            screen.Q<Label>("timeline-yours-heading").text = text["timeline.yours"];
            screen.Q<Label>("timeline-record-heading").text = text["timeline.record"];
            ScreenNav.OnOpened(screen, Render);
            bridge.Changed += OnChanged;
        }

        void OnChanged() { if (ScreenNav.IsOpen(screen)) Render(); }

        /// <summary>Queued orders leave the resolved state unchanged, so they neither add rows nor reset the reader's scroll.</summary>
        public void Render()
        {
            if (shownSession == bridge.Session && shownState == bridge.State) return;
            shownSession = bridge.Session; shownState = bridge.State;
            var c = bridge.Content; var s = bridge.State;
            list.Clear();
            foreach (var entry in Timeline.Entries(c, s))
            {
                var ev = c.Events.First(e => e.Id == entry.Event);
                var row = new VisualElement { userData = entry.Event };
                row.AddToClassList("timeline-row");
                row.EnableInClassList("timeline-diverged", entry.Diverged);
                Label Line(string name, string words)
                {
                    var label = new Label(words) { name = name };
                    label.AddToClassList("timeline-line");
                    return label;
                }
                var date = EventSystem.FillDate(ev, s.Date);
                var variant = Timeline.Variant(c, s, ev);
                bool hebrew = text["ui.lang"] == UiLanguage.Hebrew;
                // Match the decision card: each existing Hebrew field falls back independently; variants have no Hebrew title.
                string Local(string english, string he) => hebrew && he != "" ? Cabinet.Fill(c, s, he, date, p => ContentNames.Of(text, p)) : english;
                string Answer(string id)
                {
                    var option = ev.Options.First(o => o.Id == id);
                    return Local(Cabinet.Fill(c, s, option.Text, date), option.TextHe);
                }
                row.Add(Line("timeline-date", Statecraft.Game.Map.Control1948.DateText(ev.From, k => text[k])));
                row.Add(Line("timeline-title", Local(EventSystem.Title(c, s, ev), variant == null || variant.Title == "" ? ev.TitleHe : "")));
                var columns = new VisualElement(); columns.AddToClassList("timeline-columns");
                columns.Add(Line("timeline-yours", Answer(entry.Chosen)));
                columns.Add(Line("timeline-record", entry.Historical == "" ? text["timeline.no_record"] : Answer(entry.Historical)));
                row.Add(columns);
                var alternative = ev.Options.First(o => o.Id == entry.Chosen).Alternative;
                if (alternative?.Basis == OptionAlternative.Design)
                    row.Add(Line("timeline-design", text["timeline.alt_design"]));
                else if (alternative != null && alternative.IsConsidered && alternative.Date.HasValue)
                    row.Add(Line("timeline-attribution", text.Format(
                        alternative.Basis == OptionAlternative.Proposed ? "timeline.alt_proposed" : "timeline.alt_debated",
                        alternative.By, Statecraft.Game.Map.Control1948.DateText(alternative.Date.Value, k => text[k]))));
                // A visible sign supplements the rim: the difference is not conveyed by colour alone.
                if (entry.Diverged) row.Add(Line("timeline-difference", "≠"));
                if (entry.RejoinedBy != "") row.Add(Line("timeline-rejoined", text["timeline.rejoined"]));
                list.Add(row);
            }
        }

        public void Dispose()
        {
            bridge.Changed -= OnChanged;
            if (screen.userData is System.Action) screen.userData = null;
        }
    }
}
