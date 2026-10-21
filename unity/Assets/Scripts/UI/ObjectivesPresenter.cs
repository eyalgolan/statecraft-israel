#nullable enable
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The Objectives screen (#1189): every objective the content has, from the sim's Objectives.Status, each with
    /// its name, whether it is done and the player's date beside the historical one. No score on it (owner, 20 October 2026).
    /// It follows the bridge, as the Why screen does, so a turn that completes an objective shows here at once.</summary>
    public sealed class ObjectivesPresenter : System.IDisposable
    {
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text;

        public ObjectivesPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            screen = root.Q<VisualElement>("screen-objectives"); this.bridge = bridge; this.text = text;
            screen.Q<Label>("objectives-title").text = text["objectives.title"];
            bridge.Changed += Render;
            Render();
        }

        public void Dispose() { bridge.Changed -= Render; }

        string Date(System.DateTime d) => text.Format("date.day_month_year", d.Day, text["date.month_" + d.Month], d.Year);

        /// <summary>Rebuilds the rows from the state as it is now (two objectives today, a handful at most, so no diffing).</summary>
        public void Render()
        {
            var rows = screen.Q<ScrollView>("objectives-rows");
            rows.Clear();
            foreach (var o in Objectives.Status(bridge.Content, bridge.State))
            {
                var row = new VisualElement { name = "objectives-row-" + o.Id };
                row.AddToClassList("objectives-row");
                // A new objective (#791) with no strings key yet still shows, under its objectives.yaml name.
                var key = "objectives.name." + o.Id;
                var name = new Label(text.Has(key) ? text[key] : o.Name);
                name.AddToClassList("objectives-name");
                var status = new Label(o.Completed is System.DateTime done
                    ? text.Format("objectives.done", Date(done), Date(o.Historical))
                    : text.Format("objectives.open", Date(o.Historical)));
                status.AddToClassList("objectives-status");
                row.Add(name); row.Add(status);
                rows.Add(row);
            }
        }
    }
}
