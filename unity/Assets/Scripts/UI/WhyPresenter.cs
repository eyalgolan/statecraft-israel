#nullable enable
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>"Why did this happen?" (spec section 4, step 6): every cause the last turn reported, grouped by the system
    /// that produced it, in a fixed order (what was decided first, then its consequences).</summary>
    public sealed class WhyPresenter : System.IDisposable
    {
        public static readonly string[] Order = { "era", "events", "politics", "cabinet", "security", "economy", "population", "city", "map", "policy", "effects", "situations" };
        readonly VisualElement screen; readonly SimBridge bridge; readonly UiText text;
        IReadOnlyList<UnitEntry> units = new List<UnitEntry>();
        public IReadOnlyList<UnitEntry> Units
        {
            get => units;
            set { units = value ?? new List<UnitEntry>(); shown = null; Render(); }
        }
        IReadOnlyDictionary<string, string>? townNamesHe;
        /// <summary>Each settlement's Hebrew name, by id (the map's parse), for a raised unit's home city in Hebrew.</summary>
        public IReadOnlyDictionary<string, string>? TownNamesHe
        {
            get => townNamesHe;
            set { townNamesHe = value; shown = null; Render(); }
        }

        public WhyPresenter(VisualElement root, SimBridge bridge, UiText text)
        {
            screen = root.Q<VisualElement>("screen-why"); this.bridge = bridge; this.text = text;
            screen.Q<Label>("why-screen-title").text = text["why.title"];
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge (the owner calls this when disabled).</summary>
        public void Dispose() { bridge.Changed -= Render; }

        GameSession? shownSession; StepResult? shown;

        /// <summary>Shows the session's last turn. Does nothing when that same result is already on screen.</summary>
        public void Render() => Show(bridge.Session.Last);

        /// <summary>Test seam (internal; the tests see it through InternalsVisibleTo). Shows <paramref name="last"/> (a turn's result, or null for "nothing yet"). The same session and result
        /// as last time are not rebuilt; a new turn, a new game or a loaded save are.</summary>
        internal void Show(StepResult? last)
        {
            if (shownSession == bridge.Session && shown == last && screen.Q<ScrollView>("why-rows").childCount > 0) return;
            shownSession = bridge.Session; shown = last;
            var rows = screen.Q<ScrollView>("why-rows");
            rows.Clear();
            var prev = bridge.Session.Previous;
            screen.Q<Label>("why-turn").text = last == null || prev == null ? "" : text.Format("why.turn",
                Statecraft.Game.Map.Control1948.DateText(prev.Date, k => text[k]), Statecraft.Game.Map.Control1948.DateText(bridge.State.Date, k => text[k]));   // PlayerText.Date is always English (#701)
            if (last == null) { rows.Add(new Label(text["why.empty"]) { name = "why-empty" }); return; }
            if (last.Why.Count == 0) { rows.Add(new Label(text["why.nothing"]) { name = "why-nothing" }); return; }
            int i = 0;
            // Known systems in their fixed order, then any other system last under "Other" (never its raw id).
            foreach (var system in Order.Concat(last.Why.Select(w => w.System).Distinct().Where(sys => !Order.Contains(sys))))
            {
                var lines = last.Why.Where(w => w.System == system).ToList();
                if (lines.Count == 0) continue;
                var header = new Label(Order.Contains(system) ? text["why." + system] : text["why.other"]) { name = "why-group-" + system };
                header.AddToClassList("why-group");
                rows.Add(header);
                foreach (var w in lines)
                {
                    var line = new Label(CauseText.Line(w, bridge.Content, units, text, townNamesHe)) { name = "why-" + i++ };
                    line.AddToClassList("why-line");
                    rows.Add(line);
                }
            }
        }
    }
}
