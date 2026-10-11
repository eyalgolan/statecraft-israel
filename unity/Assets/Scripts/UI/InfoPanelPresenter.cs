#nullable enable
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map;
using Statecraft.Sim;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel (bottom-left): the selected hex's land, height and the settlements it owns, what the player
    /// built there, and the two map actions (transit camp, housing project) with the reason when one is not possible
    /// (MapActions.Check). Tile yields are deliberately not shown (owner decision, M3 rulings 2).</summary>
    public sealed class InfoPanelPresenter : System.IDisposable
    {
        readonly VisualElement panel; readonly SimBridge bridge; readonly UiText text;
        readonly Button campButton, housingButton; readonly System.Action onCamp, onHousing;
        readonly Label title, subtitle, settlements, placement, reason;
        readonly UiIcon thumb;
        readonly IReadOnlyDictionary<string, string> settlementNames;

        public InfoPanelPresenter(VisualElement root, SimBridge bridge, UiText text, IReadOnlyDictionary<string, string> settlementNames)
        {
            panel = root.Q<VisualElement>("info-panel"); this.bridge = bridge; this.text = text;
            this.settlementNames = settlementNames;
            title = panel.Q<Label>("info-title"); subtitle = panel.Q<Label>("info-subtitle"); settlements = panel.Q<Label>("info-settlements");
            placement = panel.Q<Label>("info-placement"); reason = panel.Q<Label>("info-reason");
            thumb = panel.Q<UiIcon>("info-thumb");
            campButton = panel.Q<Button>("build-camp"); housingButton = panel.Q<Button>("build-housing");
            onCamp = () => Build(MapActions.TransitCamp); onHousing = () => Build(MapActions.Housing);
            campButton.clicked += onCamp; housingButton.clicked += onHousing;
            bridge.Changed += Render;
            Render();
        }

        /// <summary>Unsubscribes from the bridge and the buttons (the owner calls this when disabled).</summary>
        public void Dispose() { bridge.Changed -= Render; campButton.clicked -= onCamp; housingButton.clicked -= onHousing; }

        public (int q, int r)? Selected { get; private set; }

        public void Show(int q, int r) { Selected = (q, r); Render(); }

        void Build(string action)
        {
            if (Selected == null) return;
            // The bridge reports a refusal itself; the sim enforces legality and the per-turn cap when the turn runs.
            bridge.Queue(new MapAction(action, Selected.Value.q, Selected.Value.r));
        }

        /// <summary>The terrain sign: the biome's glyph on the biome's own map colour (MapBuilder.BiomeColours), hidden when no
        /// hex (or no hex on the map) is selected.</summary>
        void ShowThumb(string? biome)
        {
            var known = biome != null && MapBuilder.BiomeColours.ContainsKey(biome);
            thumb.style.display = known ? DisplayStyle.Flex : DisplayStyle.None;
            thumb.Glyph = known ? "biome-" + biome : "";
            thumb.style.backgroundColor = known ? new StyleColor(MapBuilder.BiomeColours[biome!]) : new StyleColor(StyleKeyword.Null);
        }

        public void Render()
        {
            var c = bridge.Content; var s = bridge.State;
            var camp = campButton; var housing = housingButton;
            panel.EnableInClassList("info-empty", Selected == null);
            if (Selected == null)
            {
                title.text = text["info.hint"];
                subtitle.text = settlements.text = placement.text = reason.text = "";
                camp.style.display = DisplayStyle.None; housing.style.display = DisplayStyle.None;
                ShowThumb(null);
                return;
            }
            var (q, r) = Selected.Value;
            var hex = c.HexAt(q, r);
            ShowThumb(hex?.Biome);
            var owned = c.SettlementHexes.Where(x => x.Q == q && x.R == r).Select(x => settlementNames[x.Id]).ToList();   // strict: every settlement hex has a name (final review M4)
            title.text = owned.Count > 0 ? owned[0] : hex == null ? text["info.off_map"] : text["info.biome." + hex.Biome];
            subtitle.text = hex == null ? "" : text.Format("info.hex", text["info.biome." + hex.Biome], hex.ElevM);   // no debug coordinates in player text (playtest 2, P2-6)
            settlements.text = owned.Count > 0 ? text.Format("info.owned", string.Join(", ", owned)) : "";
            var here = MapActions.At(s, q, r);
            var queued = bridge.QueuedLike(new MapAction(MapActions.Housing, q, r)) as MapAction;
            placement.text = queued != null ? text.Format("info.queued", PlayerText.Capitalised(PlayerText.MapAction(c, queued.Action)))
                : here != null ? text.Format("info.built", PlayerText.Capitalised(PlayerText.MapAction(c, here.Action)), PlayerText.Month(here.Date)) : "";
            var campWhy = MapActions.Check(c, s, MapActions.TransitCamp, q, r);
            var housingWhy = MapActions.Check(c, s, MapActions.Housing, q, r);
            camp.style.display = DisplayStyle.Flex; housing.style.display = DisplayStyle.Flex;
            camp.SetEnabled(campWhy == null); housing.SetEnabled(housingWhy == null);
            // with no rule for an action there is no cost to show (it is disabled with the sim's reason anyway)
            camp.text = c.Rules.MapActions.TryGetValue(MapActions.TransitCamp, out var cr) ? text.Format("info.build_camp", PlayerText.Money(c, "treasury", cr.Cost)) : text["info.build_camp_plain"];
            housing.text = c.Rules.MapActions.TryGetValue(MapActions.Housing, out var hr) ? text.Format("info.build_housing", PlayerText.Money(c, "treasury", hr.Cost)) : text["info.build_housing_plain"];
            reason.text = string.Join("\n", new[] { campWhy, housingWhy }.Where(x => x != null).Distinct());   // the sim's reasons, each once
        }
    }
}
