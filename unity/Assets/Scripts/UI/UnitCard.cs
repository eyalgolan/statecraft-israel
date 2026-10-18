#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel's text for a selected unit (read only; no orders: the panel links to the Defence screen):
    /// its name, number and service; where it stands, and since when only from a sourced start (the anchor's `since`,
    /// never the game's first day: review I1); its formation and first commander where sourced; the sector in the
    /// source's words; on Israel's units, the national values its token reflects (the reserve call-up sets its figures, the
    /// security level whether its aircraft fly); and its sources, with a draft note that says "Wikipedia only" when the
    /// position's own sources are (review M8).</summary>
    public sealed class UnitCard
    {
        public string Title = "", Subtitle = "", Body = "", Footer = "", Note = "";

        public static UnitCard For(PlacedToken placed, GameState state, UiText text)
        {
            var t = placed.Token; var u = t.Unit; var a = t.Anchor;
            var lines = new List<string>();
            if (u.Service == UnitData.Air)
            {
                var aircraft = text[t.ArtType == UnitShowcase.SpitfireType ? "unit.aircraft.spitfire" : "unit.aircraft.s199"];
                lines.Add(text.Format(t.Flying ? "unit.flying" : "unit.parked", aircraft));
            }
            lines.Add(string.IsNullOrWhiteSpace(a.Since) ? text.Format("unit.position_plain", a.Place) : text.Format("unit.position", a.Place, a.Since!));
            if (!string.IsNullOrEmpty(u.Formed)) lines.Add(text.Format("unit.formed", u.Formed!));
            if (!string.IsNullOrEmpty(u.FirstCommander)) lines.Add(text.Format("unit.commander", u.FirstCommander!));
            lines.Add(u.Sector);
            var hosts = u.AllSources().Select(s => new Uri(s.Url).Host).Distinct(StringComparer.Ordinal).OrderBy(h => h, StringComparer.Ordinal);
            return new UnitCard
            {
                Title = u.Name,
                Subtitle = text.Format("unit.subtitle", u.Number, text["unit.service." + u.Service]),
                Body = string.Join("\n", lines),
                // Israel's call-up and security level set only Israel's tokens; an Arab force's token reflects neither (#665).
                Footer = (u.Side == UnitData.Israel ? text.Format("unit.national", PlayerText.Percent(state.Mobilization), state.Rung) + "\n" : "")
                    + text.Format("unit.sources", string.Join(", ", hosts)),
                Note = UnitData.IsWikipediaOnly(a.Sources) ? text["unit.draft_wikipedia"] : text["unit.draft"],
            };
        }
    }
}
