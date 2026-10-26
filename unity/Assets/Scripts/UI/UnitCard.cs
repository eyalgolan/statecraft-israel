#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Statecraft.Game.Map.Units;
using Statecraft.Sim;

namespace Statecraft.Game.UI
{
    /// <summary>The info panel's text for a selected unit (its orders so far, Move, Fortify, Hold and Attack, are the panel's: InfoPanelPresenter; the panel also links to the Defence screen):
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

        /// <summary>The sim unit a drawn token represents. GDD tokens carry the id directly, including units raised after
        /// the starting forces; row lookup remains the fallback for callers that construct an older token by hand.</summary>
        public static int? SimUnit(Content c, GameState s, UnitToken token) =>
            token.SimId is int id ? s.Units.ContainsKey(id) ? id : (int?)null : SimUnit(c, s, token.Unit);

        /// <summary>A raised unit's type on screen (#1396), or null for a type no raised unit has: the card's title. The Why
        /// lines name the unit as one formation instead (unit.name.&lt;type&gt;, UnitNames).</summary>
        public static string? RaisedType(string type, UiText text) => type switch
        {
            "recruit" => text["city.track_recruit"],
            StartingForces.IsraeliBrigadeType => text["unit.type.veteran_soldier"],
            "artillery" => text["city.track_artillery"],
            "armoured_car" => text["city.track_armoured_car"],
            _ => null,
        };

        public static UnitCard For(PlacedToken placed, GameState state, UiText text, bool gddLoop = false, Content? content = null,
            IReadOnlyDictionary<string, string>? settlementNamesHe = null)
        {
            var t = placed.Token; var u = t.Unit; var a = t.Anchor;
            bool hebrew = text["ui.lang"] == UiLanguage.Hebrew;
            // In Hebrew each prose text shows its Hebrew counterpart where the row has one, else its English (#558).
            string Local(string english, string? he) => hebrew && !string.IsNullOrEmpty(he) ? he! : english;
            // In Hebrew a formed or since date that is a plain day or month ("24 May 1948", "March 1948") names its month
            // through the date strings, as the event cards' dates do (#1292); a date in prose ("late 1947") shows its
            // formed_he or since_he where the row has one, else its English.
            string When(string english)
            {
                if (!hebrew) return english;
                if (DateTime.TryParseExact(english, "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    return text.Format("date.day_month_year", d.Day, text["date.month_" + d.Month], d.Year);
                return DateTime.TryParseExact(english, "MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var m)
                    ? text.Format("date.month_year", text["date.month_" + m.Month], m.Year) : english;
            }
            var lines = new List<string>();
            string? raisedType = t.Raised && t.StateUnit != null ? RaisedType(t.StateUnit.Type, text) : null;
            string home = t.StateUnit != null && t.StateUnit.HomeCity != "" && content != null
                && content.SettlementNames.TryGetValue(t.StateUnit.HomeCity, out var city) ? city : "";
            if (hebrew && t.StateUnit != null && settlementNamesHe != null
                && settlementNamesHe.TryGetValue(t.StateUnit.HomeCity, out var cityHe) && !string.IsNullOrWhiteSpace(cityHe)) home = cityHe;
            if (raisedType != null && t.StateUnit != null)
            {
                lines.Add(text.Format("unit.strength", t.StateUnit.Strength));
                if (home != "") lines.Add(text.Format("unit.home_city", home));
            }
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
                lines.Add(string.IsNullOrWhiteSpace(a.Since) ? text.Format("unit.position_plain", place) : text.Format("unit.position", place, Local(When(a.Since!), a.SinceHe)));
            }
            if (!string.IsNullOrEmpty(u.Formed)) lines.Add(text.Format("unit.formed", Local(When(u.Formed!), u.FormedHe)));
            // The English decides whether the line exists: a Hebrew commander never adds one (owner, #558).
            if (!string.IsNullOrEmpty(u.FirstCommander)) lines.Add(text.Format("unit.commander", Local(u.FirstCommander!, u.FirstCommanderHe)));
            if (!t.Raised) lines.Add(Local(u.Sector, u.SectorHe));   // a raised unit has no sourced sector
            // Israel's call-up and security level set only Israel's tokens; an Arab force's token reflects neither (#665).
            var national = u.Side != UnitData.Israel ? ""
                : !gddLoop ? text.Format("unit.national", PlayerText.Percent(state.Mobilization), state.Rung)
                : u.Service == UnitData.Air ? text.Format("unit.security", state.Rung) : "";
            var hosts = u.AllSources().Select(s => new Uri(s.Url).Host).Distinct(StringComparer.Ordinal).OrderBy(h => h, StringComparer.Ordinal);
            return new UnitCard
            {
                // A raised unit has no name or number: its type is the title, the service the subtitle (#1396).
                Title = raisedType ?? (hebrew && !string.IsNullOrEmpty(u.NameHe) ? u.NameHe : u.Name),
                Subtitle = raisedType == null ? text.Format("unit.subtitle", u.Number, text["unit.service." + u.Service])
                    : text["unit.service." + u.Service],
                Body = string.Join("\n", lines),
                Footer = t.Raised ? "" : (national == "" ? "" : national + "\n")
                    + text.Format("unit.sources", string.Join(", ", hosts)),
                Note = t.Raised ? "" : (sourced ? UnitData.IsWikipediaOnly(a.Sources) : UnitData.IsWikipediaOnly(u)) ? text["unit.draft_wikipedia"] : text["unit.draft"],
            };
        }
    }
}
