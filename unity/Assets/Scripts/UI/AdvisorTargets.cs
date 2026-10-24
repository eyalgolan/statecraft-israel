#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statecraft.Game.UI
{
    /// <summary>The guide's real HUD targets and selection rules, shared by advisor Show me (spec7.3).</summary>
    public static class AdvisorTargets
    {
        public const float MoveStepDistanceM = 40000f;
        public const string Attention = "attention", CityHousing = "city_housing", CityTraining = "city_training",
            UnitMove = "unit_move", DecisionWaiting = "decision_waiting";

        public static string Nav(string screen) => "nav_" + screen.Replace('-', '_');

        public static IReadOnlyList<string> Names =>
            new[] { Attention, CityHousing, CityTraining, UnitMove, DecisionWaiting }.Concat(ScreenNav.Screens.Select(Nav)).ToList();

        public static bool ShowsCity(string name) => name == CityHousing || name == CityTraining;
        public static bool ShowsUnit(string name) => name == UnitMove;

        /// <summary>The shown elements a name points at, as the HUD has them now (the guide's rules, #1265, #1189).</summary>
        public static IReadOnlyList<VisualElement> Find(VisualElement hudRoot, string name, EventCardPresenter? card)
        {
            var found = new List<VisualElement?>();
            var host = hudRoot.Q(GddHexPanel.HostName);
            switch (name)
            {
                case Attention: found.Add(hudRoot.Q("attention")); break;
                case CityHousing: foreach (var n in new[] { "city-housing", "city-jobs-row", "city-workers-row" }) found.Add(host?.Q(n)); break;
                case CityTraining:
                    if (host != null) found.AddRange(host.Query<VisualElement>().Where(e => e.name.StartsWith("city-train-", StringComparison.Ordinal) && e.name.EndsWith("-row", StringComparison.Ordinal)).ToList());
                    break;
                case UnitMove: found.Add(hudRoot.Q("info-move")); break;
                case DecisionWaiting: found.Add(card?.Shown != null && card.Collapsed ? hudRoot.Q("event-reopen") : hudRoot.Q("event-options")); break;
                default:
                    var screen = ScreenNav.Screens.FirstOrDefault(s => Nav(s) == name);
                    if (screen != null) found.Add(hudRoot.Q("nav-" + screen));
                    break;
            }
            return found.Where(e => e != null && Shown(e)).Select(e => e!).ToList();
        }

        public static Rect? Area(IEnumerable<VisualElement> targets)
        {
            Rect? area = null;
            foreach (var e in targets)
            {
                var r = e.worldBound;
                area = area is Rect a ? Rect.MinMaxRect(Mathf.Min(a.xMin, r.xMin), Mathf.Min(a.yMin, r.yMin), Mathf.Max(a.xMax, r.xMax), Mathf.Max(a.yMax, r.yMax)) : r;
            }
            return area;
        }
        /// <summary>Shown on the panel: laid out with a size, and neither it nor any parent hidden.</summary>
        public static bool Shown(VisualElement e)
        {
            if (e.panel == null || !(e.worldBound.width > 0 && e.worldBound.height > 0)) return false;
            for (var v = e; v != null; v = v.parent)
                if (v.resolvedStyle.display == DisplayStyle.None || v.resolvedStyle.visibility == Visibility.Hidden) return false;
            return true;
        }

        /// <summary>The city the guide shows in the info panel: the first city (cities.yaml order) with a training facility at
        /// the start (Training.HasFacility, the sim's helper), else the first city; its settlement hex, or null with none.</summary>
        public static (int q, int r)? CityHex(Content c, GameState s)
        {
            var cities = c.Cities.Where(x => s.Cities.ContainsKey(x.Id)).ToList();
            var city = cities.FirstOrDefault(x => c.UnitTypes.Any(t => t.Facility != "" && Training.HasFacility(c, s, x.Id, t.Facility)))
                ?? cities.FirstOrDefault();
            if (city == null) return null;
            foreach (var h in c.SettlementHexes) if (h.Id == city.Id) return (h.Q, h.R);
            return null;
        }

        /// <summary>The unit the guide shows on its move step: the first of <paramref name="units"/> (the drawn tokens' rows, in
        /// units.yaml order) whose sim unit has a hex in Movement.Reachable now, so its card shows Move
        /// (InfoPanelPresenter.MoveTargets' rule); its id, or null with none (the legacy loop, or no unit able to move).</summary>
        public static string? UnitToShow(Content c, GameState s, IEnumerable<UnitEntry> units) =>
            units.FirstOrDefault(u => UnitCard.SimUnit(c, s, u) is int id && Movement.Reachable(c, s, id).Count > 0)?.Id;

        /// <summary>The drawn-token form used by the game. A raised token carries its sim id even without a starting row.</summary>
        public static string? UnitToShow(Content c, GameState s, IEnumerable<UnitToken> units) =>
            units.FirstOrDefault(u => UnitCard.SimUnit(c, s, u) is int id && Movement.Reachable(c, s, id).Count > 0)?.Unit.Id;

    }
}
