#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel's text for a selected unit (its one order so far, Move, is the panel's: InfoPanelPresenter; the panel also links to the Defence screen):
    /// its name, number and service; where it stands, and since when only from a sourced start (the anchor's `since`,
    /// never the game's first day: review I1); its formation and first commander where sourced; the sector in the
    /// source's words; on Israel's units, the national values its token reflects (legacy loop: the reserve call-up sets an
    /// army token's figures, the security level whether the aircraft fly; GDD loop: the unit's strength sets the figures,
    /// Q-UI-3, so only the squadron's card keeps a line, the security level); and its sources, with a draft note that says
    /// "Wikipedia only" when the position's own sources are (review M8).</summary>
    public sealed class UnitCard
    {
        public string Title = "", Subtitle = "", Body = "", Footer = "", Note = "";

        /// <summary>The sim unit <paramref name="row"/>'s token stands for in the GDD loop: unit i of a new game is
        /// <c>c.StartingForces[i]</c> (R-UNIT-006), as UnitRoster.FromState draws it. Null for a row with no starting force (and
        /// in the legacy loop, which has none), or once that unit has left the state. The card's orders go to this id (#754).</summary>
        public static int? SimUnit(Content c, GameState s, UnitEntry row)
        {
            for (int i = 0; i < c.StartingForces.Count; i++)
                if (c.StartingForces[i].Row == row.Id) return s.Units.ContainsKey(i) ? i : (int?)null;
            return null;
        }

        public static UnitCard For(PlacedToken placed, GameState state, UiText text, bool gddLoop = false)
        {
            var t = placed.Token; var u = t.Unit; var a = t.Anchor;
            bool hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            // In Hebrew each prose text shows its Hebrew counterpart where the row has one, else its English (#558).
            string Local(string english, string? he) => hebrew && !string.IsNullOrEmpty(he) ? he! : english;
            var lines = new List<string>();
            if (u.Service == UnitData.Air)
            {
                var aircraft = text[t.ArtType == UnitShowcase.SpitfireType ? "unit.aircraft.spitfire" : "unit.aircraft.s199"];
                lines.Add(text.Format(t.Flying ? "unit.flying" : "unit.parked", aircraft));
            }
            // A unit that moved in play (GDD loop) stands on a hex no source names: no position line, and the row's sources.
            bool sourced = a.Place != "";
            if (sourced)
            {
                var place = Local(a.Place, a.PlaceHe);
                lines.Add(string.IsNullOrWhiteSpace(a.Since) ? text.Format("unit.position_plain", place) : text.Format("unit.position", place, a.Since!));
            }
            if (!string.IsNullOrEmpty(u.Formed)) lines.Add(text.Format("unit.formed", u.Formed!));
            // The English decides whether the line exists: a Hebrew commander never adds one (owner, #558).
            if (!string.IsNullOrEmpty(u.FirstCommander)) lines.Add(text.Format("unit.commander", Local(u.FirstCommander!, u.FirstCommanderHe)));
            lines.Add(Local(u.Sector, u.SectorHe));
            // Israel's call-up and security level set only Israel's tokens; an Arab force's token reflects neither (#665).
            var national = u.Side != UnitData.Israel ? ""
                : !gddLoop ? text.Format("unit.national", PlayerText.Percent(state.Mobilization), state.Rung)
                : u.Service == UnitData.Air ? text.Format("unit.security", state.Rung) : "";
            var hosts = u.AllSources().Select(s => new Uri(s.Url).Host).Distinct(StringComparer.Ordinal).OrderBy(h => h, StringComparer.Ordinal);
            return new UnitCard
            {
                Title = text["ui.lang"] == UiLanguage.Hebrew && !string.IsNullOrEmpty(u.NameHe) ? u.NameHe : u.Name,
                Subtitle = text.Format("unit.subtitle", u.Number, text["unit.service." + u.Service]),
                Body = string.Join("\n", lines),
                Footer = (national == "" ? "" : national + "\n")
                    + text.Format("unit.sources", string.Join(", ", hosts)),
                Note = (sourced ? UnitData.IsWikipediaOnly(a.Sources) : UnitData.IsWikipediaOnly(u)) ? text["unit.draft_wikipedia"] : text["unit.draft"],
            };
        }
    }
}
